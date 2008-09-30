#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif

#include <windows.h>
#include <winsock2.h>
#include <Wsrm.h>
#include <Iphlpapi.h>
#include <stdio.h>
#include <stdlib.h>

#ifndef _PGM_SENDER_H
#define _PGM_SENDER_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

struct pgm_sender_parameters {
	unsigned int group_addr;
	unsigned short group_port;

	unsigned int local_if_subnet;

	// data rate in kilobits-per-sec
	unsigned int data_rate;
	// window time in milliseconds
	unsigned int window_time;
};

class pgm_sender {
public:
	pgm_sender(const pgm_sender_parameters& params);

	bool send(const char* buf, unsigned int len);

private:
	bool init_wsa();
	bool init_socket();
	bool attach_adapter(unsigned int local_if_subnet);
	bool set_rate(unsigned int data_rate, unsigned int window_time);
	bool connect_socket(unsigned int group_addr, unsigned short group_port);

	void close_socket();
	void close_wsa();

	SOCKET s;
	WSAData wsa_data;

};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif