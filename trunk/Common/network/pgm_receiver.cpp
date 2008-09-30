#include "pgm_receiver.h"

#pragma comment (lib, "WS2_32.lib")

pgm_receiver::pgm_receiver(const char* group_addr, unsigned short session_port) {
	sock_session = INVALID_SOCKET;
	sock_client = INVALID_SOCKET;

	if (!init_wsa())
		return;

	if (!init_socket(inet_addr(group_addr), session_port)) {
		sock_client = INVALID_SOCKET;
		return;
	}

	if (!listen_socket()) {
		close_socket();
		return;
	}
}

pgm_receiver::~pgm_receiver() {
	close_socket();
	close_wsa();
}

bool pgm_receiver::init_wsa() {
	int result = WSAStartup( MAKEWORD(2,2), &wsa_data );
  if (result != NO_ERROR) {
		printf("error during winsock startup: %d\r\n", result);;
    return false;
  }

	return true;
}

bool pgm_receiver::init_socket(unsigned int group_addr, unsigned short session_port) {
	sockaddr_in salocal;

	sock_client = socket(AF_INET, SOCK_RDM, IPPROTO_RM);
	if (sock_client == INVALID_SOCKET) {
		printf("error creating pgm socket: %d\n", WSAGetLastError());
		return false;
	}

	//
	// The bind port (dwSessionPort) specified should match that
	// which the sender specified in the connect call
	//
	salocal.sin_family = AF_INET;
	salocal.sin_port = htons(session_port);	
	salocal.sin_addr.s_addr = group_addr;

	if (bind(sock_client, (SOCKADDR *)&salocal, sizeof(salocal)) == SOCKET_ERROR) {
		printf("error binding pgm socket: %d\n", WSAGetLastError());
		closesocket(sock_client);
		return false;
	}

	return true;
}

bool pgm_receiver::listen_socket() {
	if (listen(sock_client, 10) == SOCKET_ERROR) {
		printf("error listening on socket: %d\n", WSAGetLastError());
		return false;
	}

	return true;
}

void pgm_receiver::close_wsa() {
	WSACleanup();
}

void pgm_receiver::close_socket() {
	if (sock_session != INVALID_SOCKET) {
		closesocket(sock_session);
		sock_session = INVALID_SOCKET;
	}

	if (sock_client != INVALID_SOCKET) {
		closesocket(sock_client);
		sock_client = INVALID_SOCKET;
	}
}

int pgm_receiver::receive(void* buf, unsigned int &buf_len) {
	if (sock_client == INVALID_SOCKET) {
		return PGM_ERROR_NOT_INIT;
	}

	if (sock_session == INVALID_SOCKET) {
		int sz_addr = sizeof(remote_addr);
		if ((sock_session = accept(sock_client, (sockaddr*)&remote_addr, &sz_addr)) == INVALID_SOCKET) {
			printf("error accepting remote connection: %d\n", WSAGetLastError());
			return PGM_ERROR_OTHER;
		}
		else {
			printf("connected to %s on port %d\n", inet_ntoa(remote_addr.sin_addr), (int)remote_addr.sin_port);
		}
	}

	int result = recv(sock_session, (char*)buf, buf_len, 0);
	if (result == SOCKET_ERROR) {
		buf_len = 0;
		int last_err = WSAGetLastError();
		if (last_err == WSAEDISCON) {
			// reset the session socket
			closesocket(sock_session);
			sock_session = INVALID_SOCKET;
			// start listening
			listen_socket();

			return PGM_ERROR_SESSION_CLOSED;
		}
		else {
			printf("error receiving from remote connection: %d\n", last_err);
			return PGM_ERROR_OTHER;
		}
	}
	else {
		// everything is fine
		buf_len = result;
		return PGM_ERROR_OK;
	}
}