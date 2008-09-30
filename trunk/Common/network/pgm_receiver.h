#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif

#include <windows.h>
#include <winsock2.h>
#include <Wsrm.h>
#include <Iphlpapi.h>
#include <stdio.h>
#include <stdlib.h>

#ifndef _PGM_RECEIVER_H
#define _PGM_RECEIVER_H

#define PGM_ERROR_OK 0
#define PGM_ERROR_SESSION_CLOSED 1
#define PGM_ERROR_NOT_INIT 2
#define PGM_ERROR_OTHER 3

class pgm_receiver {
public:
	pgm_receiver(const char* group_addr, unsigned short session_port);
	~pgm_receiver();

	int receive(void* buf, unsigned int &buf_len);

private:
	SOCKET sock_client;
	SOCKET sock_session;
	WSADATA wsa_data;

	sockaddr_in remote_addr;

	bool init_wsa();
	bool init_socket(unsigned int group_addr, unsigned short session_port);
	bool listen_socket();

	void close_wsa();
	void close_socket();
};

#endif