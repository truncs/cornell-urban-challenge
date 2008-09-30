#include "tcp_server.h"

#pragma comment (lib,"ws2_32.lib")
#pragma comment (lib,"Mswsock.lib")

#define COMP_KEY_ACC_SOCKET 1
#define COMP_KEY_CLOSE 2
#define COMP_KEY_CLIENT 3

tcp_server::tcp_server(int num_threads) {
	// initialize critical section
	InitializeCriticalSection(&cs);

	// initialize client objects
	for (int i = 0; i < TCP_SERVER_MAX_CLIENTS; i++){
		clients[i].state = CLIENT_STATE_FREE;
		clients[i].read_cbk = NULL;
		clients[i].read_arg = NULL;
	}

	// allocate space for thread handles
	threads = new HANDLE[num_threads];
	cthreads = num_threads;

	// initialize WSA
	if (WSAStartup(MAKEWORD(2,2), &wsadata) != NO_ERROR) {
    printf("error at WSAStartup\n");
		return;
	}

	// create the completion port
	comp_port = CreateIoCompletionPort(INVALID_HANDLE_VALUE, NULL, 0, cthreads);
	if (comp_port == NULL) {
		printf("error creating completion port: %d\n", GetLastError());
		return;
	}

	// create all the threads
	for (int i = 0; i < cthreads; i++) {
		threads[i] = CreateThread(NULL, 0, listener_thread, this, 0, NULL);
		if (threads[i] == NULL)
			printf("error creating thread\n");
	}
}

tcp_server::~tcp_server() {
	// tell all the threads to close down
	for (int i = 0; i < cthreads; i++) {
		// post events to each queue telling them to shut down
		PostQueuedCompletionStatus(comp_port, 0, COMP_KEY_CLOSE, NULL);
	}

	// close the listen socket
	closesocket(acc_sock);

	lock();
	// close all the sockets
	for (int i = 0; i < TCP_SERVER_MAX_CLIENTS; i++) {
		if (clients[i].state == CLIENT_STATE_CONNECTED || clients[i].state == CLIENT_STATE_PENDING) {
			closesocket(clients[i].sock);
		}
	}
	unlock();

	// destroy all the threads
	while (cthreads > 0) {
		DWORD res = WaitForMultipleObjects(cthreads, threads, false, INFINITE);
		// figure out which thread
		DWORD thread_ind = res - WAIT_OBJECT_0;
		CloseHandle(threads[thread_ind]);

		// move all the threads above down
		for (int i = thread_ind + 1; i < cthreads; i++) {
			threads[i-1] = threads[i];
		}

		// remove that thread
		cthreads--;
	}

	// delete thread array
	delete [] threads;

	// clean up the critical section
	DeleteCriticalSection(&cs);

	// close the completion port
	CloseHandle(comp_port);

	// cleanup winsock
	WSACleanup();
}

tcp_server_client* tcp_server::get_free_entry() {
	// lock the list
	lock();
	// find the first free entry
	for (int i = 0; i < TCP_SERVER_MAX_CLIENTS; i++) {
		if (clients[i].state == CLIENT_STATE_FREE) {
			// mark it as locked
			clients[i].state = CLIENT_STATE_LOCKED;
			// clear out the read callback
			clients[i].read_cbk = NULL;
			clients[i].read_arg = NULL;
			clients[i].close_cbk = NULL;
			// unlock and return
			unlock();
			return &clients[i];
		}
	}
	unlock();
	// couldn't find an entry, return null
	return NULL;
}

void tcp_server::free_entry(tcp_server_client *c) {
	// mark the entry as free
	c->state = CLIENT_STATE_FREE;
}

bool tcp_server::begin_listen(unsigned short port, accept_callback acc_cbk, void *arg) {
	// assign the accept callback, argument
	this->acc_cbk = acc_cbk;
	this->acc_arg = arg;

	// create the accept socket
	acc_sock = WSASocket(AF_INET, SOCK_STREAM, IPPROTO_TCP, NULL, 0, WSA_FLAG_OVERLAPPED);
	if (acc_sock == INVALID_SOCKET) {
		printf("error creating listen socket: %d\n", WSAGetLastError());
		return false;
	}

	// associated the accept socket with the completion port
	CreateIoCompletionPort((HANDLE)acc_sock, comp_port, COMP_KEY_ACC_SOCKET, 0);

	// bind to the ANY address
	sockaddr_in server_in;
	server_in.sin_family = AF_INET;
	server_in.sin_port = htons(port);
	server_in.sin_addr.s_addr = INADDR_ANY;

	// bind 
	if (bind(acc_sock, (sockaddr*)&server_in, sizeof(server_in)) == SOCKET_ERROR) {
		printf("error binding socket: %d\n", WSAGetLastError());
		return false;
	}

	// start listening on socket
	if (listen(acc_sock, TCP_SERVER_MAX_CLIENTS) == SOCKET_ERROR) {
		printf("error listening: %d\n", WSAGetLastError());
		return false;
	}

	// start the accept process
	return start_accept();
}

bool tcp_server::start_accept() {
	bool did_accept = false;
	do {
		// get a free entry
		lock();
		tcp_server_client *client = get_free_entry();
		if (client == NULL) {
			unlock();
			printf("no free client entries\n");
			return false;
		}
		client->state = CLIENT_STATE_PENDING;
		client->sock = WSASocket(AF_INET, SOCK_STREAM, IPPROTO_TCP, NULL, 0, WSA_FLAG_OVERLAPPED);
		unlock();

		if (client->sock == INVALID_SOCKET) {
			// free the entry
			free_entry(client);
			printf("could not create accept socket\n");
			return false;
		}

		// clear out the overlapped structure
		memset(&ov_accept, 0, sizeof(ov_accept));
		ov_accept.client = client;

		// being the accept process
		DWORD bytes_read = 0;
		if (AcceptEx(acc_sock, client->sock, acc_buf, 0, sizeof(sockaddr_in) + 16, sizeof(sockaddr_in) + 16, &bytes_read, &ov_accept.ov)) {
			// handle the new connection
			handle_accept(client);
			// mark that we did accept
			did_accept = true;
		}
		else {
			DWORD err = WSAGetLastError();
			did_accept = false;
			if (err != ERROR_IO_PENDING) {
				// some sort of socket error
				printf("error trying to accept new connection: %d\n", err);
				return false;
			}
		}
	} while (did_accept);

	return true;
}

void tcp_server::handle_accept(tcp_server_client *client) {
	// assign the values to the client structure
	client->state = CLIENT_STATE_CONNECTED;
	// get the local/remote address
	sockaddr_in *local_addr_ptr = NULL, *remote_addr_ptr = NULL;
	int local_addr_len, remote_addr_len;
	GetAcceptExSockaddrs(acc_buf, 0, sizeof(sockaddr_in) + 16, sizeof(sockaddr_in) + 16, (sockaddr **)&local_addr_ptr, &local_addr_len, (sockaddr **)&remote_addr_ptr, &remote_addr_len);
	
	// assign the local/remote address if possible
	sockaddr_in local_addr;
	if (local_addr_ptr != NULL) {
		// cache the local value
		local_addr = *local_addr_ptr;
	}

	if (remote_addr_ptr != NULL) {
		// assign the remote address to the client struct
		client->client_addr = *remote_addr_ptr;
	}
	else {
		// zero out the memory if we don't have a remote address
		memset(&client->client_addr, 0, sizeof(client->client_addr));
	}

	// invoke the callback if present
	if (acc_cbk != NULL) {
		// call the callback
		acc_cbk(client, local_addr, acc_arg);
	}

	// bind to the completion port
	CreateIoCompletionPort((HANDLE)client->sock, comp_port, COMP_KEY_CLIENT, 0);

	// begin reading
	do_read(client);
}

void tcp_server::do_read(tcp_server_client *client) {
	bool did_read = false;
	do {
		// zero out the overlapped struct
		memset(&client->ov_read, 0, sizeof(client->ov_read));

		// start reading
		DWORD bytes_read, flags = 0;
		if (WSARecv(client->sock, &client->wsa_buf, 1, &bytes_read, &flags, &client->ov_read, NULL) == 0) {
			// mark that we did a read
			did_read = true;

			// invoke the callback
			if (client->read_cbk != NULL) {
				client->read_cbk(client, client->read_buf, bytes_read);
			}

			// if the number of bytes read is zero, the connection is donzod
			if (bytes_read == 0) {
				if (client->close_cbk != NULL) {
					client->close_cbk(client);
				}
				client->state = CLIENT_STATE_CLEANING;
				// close the socket
				closesocket(client->sock);
				// free the entry
				free_entry(client);
				// mark that we didn't actually do a read
				did_read = false;
			}
		}
		else {
			// mark that we didn't do a read
			did_read = false;

			// check what the error was
			DWORD error = WSAGetLastError();
			if (error != ERROR_IO_PENDING) {
				if (client->close_cbk != NULL) {
					client->close_cbk(client);
				}

				// this connection is bad, close it down
				client->state = CLIENT_STATE_CLEANING;
				closesocket(client->sock);
				free_entry(client);
			}
		}
	} while (did_read);
}

void tcp_server::end_listen() {
	// close the listen socket
	closesocket(acc_sock);

	// clear out the callback structure
	acc_cbk = NULL;
	acc_arg = NULL;
}

void tcp_server::send(tcp_server_client *client, unsigned char *data, int cdata) {
	// use a blocking send
	::send(client->sock, (char*)data, cdata, 0);
}

void tcp_server::close(tcp_server_client *client) {
	closesocket(client->sock);
	free_entry(client);
}

DWORD WINAPI tcp_server::listener_thread(LPVOID lpparam) {
	// get the server object from the lpparam
	tcp_server *server = (tcp_server *)lpparam;

	DWORD bytes_read = 0;
	ULONG_PTR key = 0;
	LPOVERLAPPED ov = NULL;
	while (true) {
		BOOL result = GetQueuedCompletionStatus(server->comp_port, &bytes_read, &key, &ov, INFINITE);
		if (result == 0) {
			// there was an error reading
			if (ov == NULL) {
				printf("error reading from completion port: %d\n", GetLastError());
			}
			else {
				// check what type of event
				if (key == COMP_KEY_CLIENT) {
					printf("error while reading from client: %d\n", GetLastError());

					// get the tcp_client
					tcp_server_client *client = CONTAINING_RECORD(ov, tcp_server_client, ov_read);

					if (client->close_cbk != NULL) {
						client->close_cbk(client);
					}

					// close it down
					client->state = CLIENT_STATE_CLEANING;
					closesocket(client->sock);
					// free the stuffs
					server->free_entry(client);
				}
				else if (key == COMP_KEY_ACC_SOCKET) {
					// show that there was an error
					printf("error while accepting: %d\n", GetLastError());
					// in this case, not sure what to do
					// for now, jsut stop accepting
				}
			}
		}
		else {
			// got an object, figure out what the deal is
			if (key == COMP_KEY_CLOSE) {
				// we're donzo, return 
				return 0;
			}
			else if (key == COMP_KEY_ACC_SOCKET) {
				// this is an accept event
				// get the accept object
				ACC_OVERLAPPED *acc_ov = CONTAINING_RECORD(ov, ACC_OVERLAPPED, ov);

				// handle the accept
				server->handle_accept(acc_ov->client);

				// start a new accept
				server->start_accept();
			}
			else if (key == COMP_KEY_CLIENT) {
				// this is a client read
				// get the tcp_client
				tcp_server_client *client = CONTAINING_RECORD(ov, tcp_server_client, ov_read);

				// invoke the callback
				if (client->read_cbk != NULL) {
					client->read_cbk(client, client->read_buf, bytes_read);
				}
				
				// check if we read anything
				if (bytes_read == 0) {
					// we didn't read anything which means the connection is donzo
					if (client->close_cbk != NULL) {
						client->close_cbk(client);
					}
					// close the socket, free the client
					client->state = CLIENT_STATE_CLEANING;
					closesocket(client->sock);
					server->free_entry(client);
				}
				else {
					// queue up the next read
					server->do_read(client);
				}
			}
		}
	}
}
