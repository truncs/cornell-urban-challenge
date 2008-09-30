#ifndef _WIN32_WINNT 
#define _WIN32_WINNT 0x0500
#endif
#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

#include <winsock2.h>
#include <windows.h>
#include <Mmsystem.h>

#include <exception>
using namespace std;

#include "../coords/dpvector2.h"
#include "../coords/dpmatrix2.h"

#include "timestamp.h"
#include "car_timestamp.h"

#ifndef _CAR_TIMESTAMP_SENDER_H
#define _CAR_TIMESTAMP_SENDER_H

_MM_ALIGN16 class car_timestamp_sender {
private:
	const static double sigma_b;
	const static double sigma_d;
	const static double r;

	WSADATA wsadata;
	bool wsa_init;

	SOCKET sock;
	
	HANDLE thread;
	bool running;
	
	bool init_wsa();
	bool init_socket();
	bool init_thread();

	void cleanup_wsa();
	void cleanup_socket();
	void cleanup_thread();

	static DWORD WINAPI send_thread(LPVOID lpparam);

public:
	car_timestamp_sender();
	~car_timestamp_sender();
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif