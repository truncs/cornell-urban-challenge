#ifndef _TCP_SERVER_H
#define _TCP_SERVER_H

#include <winsock2.h>
#include <windows.h>
#include <mswsock.h>
#include <stdio.h>
#include <stdlib.h>

#define TCP_SERVER_MAX_CLIENTS 10

#define CLIENT_STATE_FREE 0
#define CLIENT_STATE_LOCKED 1
#define CLIENT_STATE_PENDING 2
#define CLIENT_STATE_CONNECTED 3
#define CLIENT_STATE_CLEANING 4

class tcp_server_client;

typedef void (*accept_callback)(tcp_server_client *client, sockaddr_in local_addr, void *arg);
typedef void (*read_callback)(tcp_server_client *client, unsigned char *data, int cdata);
typedef void (*close_callback)(tcp_server_client *client);

class tcp_server_client {
public:
	int state;
	SOCKET sock;
	sockaddr_in client_addr;

	OVERLAPPED ov_read;
	unsigned char read_buf[4096];
	WSABUF wsa_buf;

	read_callback read_cbk;
	void *read_arg;

	close_callback close_cbk;

	tcp_server_client() {
		wsa_buf.buf = (char*)read_buf;
		wsa_buf.len = sizeof(read_buf);
	}
};


class tcp_server {
public:
	tcp_server(int num_threads);
	~tcp_server();

	bool begin_listen(unsigned short port, accept_callback acc_cbk, void *arg);
	void end_listen();

	void send(tcp_server_client *client, unsigned char *data, int cdata);

	void close(tcp_server_client *client);

private:
	struct ACC_OVERLAPPED {
		OVERLAPPED ov;
		tcp_server_client *client;
	};

	tcp_server_client clients[TCP_SERVER_MAX_CLIENTS];
	CRITICAL_SECTION cs;

	SOCKET acc_sock;
	accept_callback acc_cbk;
	void *acc_arg;
	unsigned char acc_buf[(sizeof(sockaddr_in)+16)*2];
	ACC_OVERLAPPED ov_accept;

	HANDLE comp_port;
	HANDLE *threads;
	int cthreads;

	WSADATA wsadata;

	void lock() { EnterCriticalSection(&cs); }
	void unlock() { LeaveCriticalSection(&cs); }

	tcp_server_client* get_free_entry();
	void free_entry(tcp_server_client *c);

	bool start_accept();
	void handle_accept(tcp_server_client *client);
	void do_read(tcp_server_client *client);

	static DWORD WINAPI listener_thread(LPVOID lpparam);
};

#endif