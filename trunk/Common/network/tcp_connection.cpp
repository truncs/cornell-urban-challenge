#include "tcp_connection.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

#pragma comment( lib, "WS2_32.lib" )

tcp_connect_params::tcp_connect_params() : remote_ip(0), remote_port(0) {
}

tcp_connect_params::tcp_connect_params(unsigned long rip, unsigned short rp) : remote_ip(rip), remote_port(rp) {
}

tcp_client::tcp_client(const tcp_connect_params &cp) : connect_params(cp) {
	is_connected = false;

	bool success = init_wsa();

	if (success) success = init_socket();
	
	if (!success) {
		printf("tcp_client: could not initialize properly\r\n");
	}
}

tcp_client::~tcp_client() {
	close();
}

bool tcp_client::init_wsa() {
	int result = WSAStartup(MAKEWORD(2,2), &wsadata);
  if (result != NO_ERROR) {
		printf("tcp_client: error during winsock startup: %d\r\n", result);
    this->sock = INVALID_SOCKET;
    return false;
  }
	else {
		return true;
	}
}

bool tcp_client::init_socket() {
	this->sock = WSASocket(AF_INET, SOCK_STREAM, IPPROTO_TCP, NULL, 0, WSA_FLAG_OVERLAPPED);

	if (sock == INVALID_SOCKET) {
		printf("tcp_client: error creating socket: %d\r\n", WSAGetLastError());
    sock = INVALID_SOCKET;
		WSACleanup();
		return false;
	}
	return true;
}

bool tcp_client::connect() {
	if (sock == INVALID_SOCKET && !init_socket()) {
		return false;
	}

	sockaddr_in r;

	r.sin_family = AF_INET;
	r.sin_addr.s_addr = connect_params.remote_ip;
	r.sin_port = htons(connect_params.remote_port);

	if (WSAConnect(sock, (SOCKADDR*)&r, sizeof(r), NULL, NULL, NULL, NULL) == SOCKET_ERROR) {
		printf("tcp_client: connect failed (%d)\r\n", WSAGetLastError());
		return false;
	}

	is_connected = true;
	return true;
}

bool tcp_client::connected() const {
	return is_connected;
}

void tcp_client::close() {
	if (sock != INVALID_SOCKET) {
		closesocket(sock);
		sock = INVALID_SOCKET;
		is_connected = false;
		WSACleanup();
	}
}

int tcp_client::read(unsigned char *buffer, const int length) {
	if (sock == INVALID_SOCKET)
		return -1;

	if (!is_connected)
		return -1;

	if (length == 0)
		return 0;

	int result = recv(sock, (char*)buffer, length, 0);

	if (result == SOCKET_ERROR) {
		printf("tcp_client: socket read failed (%d)\r\n", WSAGetLastError());
		return -1;
	}
	else if (result == 0) {
		// the socket was closed from the other end
		// close down on our end
		close();
	}

	return result;
}

int tcp_client::write(unsigned char *buffer, const int length) {
	if (sock == INVALID_SOCKET)
		return -1;

	if (!is_connected)
		return -1;

	if (length == 0)
		return 0;

	int result = send(sock, (char*)buffer, length, 0);

	if (result == SOCKET_ERROR) {
		printf("tcp_client: socket write failed (%d)\r\n", WSAGetLastError());
		return -1;
	}
	
	return result;
}