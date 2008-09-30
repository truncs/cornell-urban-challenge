#ifndef _WIN32_WINNT 
#define _WIN32_WINNT 0x0500
#endif
#include <winsock2.h>
#include <windows.h>
#include <stdio.h>

#ifndef _TCP_CONNECTION_H
#define _TCP_CONNECTION_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

class tcp_connect_params {
public:
	tcp_connect_params();
	tcp_connect_params(unsigned long remote_ip, unsigned short remote_port);

	unsigned long remote_ip;
	unsigned short remote_port;
};

class tcp_client {
private:
	SOCKET sock;
	bool is_connected;
	
	tcp_connect_params connect_params;
	WSADATA wsadata;

	bool init_wsa();
	bool init_socket();

public:
	tcp_client(const tcp_connect_params& connect_params);
	~tcp_client(void);

	bool connect();
	bool connected() const;

	void close();

	int read(unsigned char* buffer, const int length);
	int write(unsigned char* buffer, const int length);
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif