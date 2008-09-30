#include "car_timestamp_sender.h"
#include "car_timestamp_sync.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

#pragma comment( lib, "winmm.lib" )

#define PORT 131

#pragma pack(1)
struct sync_packet {
	unsigned char bullstuff;
	unsigned short secs;
	unsigned short ticks;
};
#pragma pack()


car_timestamp_sender::car_timestamp_sender() {
	wsa_init = false;
	if (!init_wsa()) {
		throw exception("init wsa failed\n");
	}
	
	if (!init_socket()) {
		cleanup_wsa();
		throw exception("init socket failed\n");
	}

	if (!init_thread()) {
		cleanup_socket();
		cleanup_wsa();
		throw exception("init thread failed\n");
	}

	timeBeginPeriod(1);
}

car_timestamp_sender::~car_timestamp_sender() {
	cleanup_thread();
	cleanup_socket();
	cleanup_wsa();

	timeEndPeriod(1);
}

bool car_timestamp_sender::init_wsa() {
	if (WSAStartup(MAKEWORD(2,2), &wsadata) != NO_ERROR) {
		printf("could not initialize winsock\n");
		return false;
	}
	
	wsa_init = true;
	return true;
}

bool car_timestamp_sender::init_socket() {
	sock = WSASocket(AF_INET, SOCK_DGRAM, IPPROTO_UDP, NULL, 0, 0);
	if (sock == INVALID_SOCKET) {
		printf("could not create socket\n");
		return false;
	}

	sockaddr_in service;
	service.sin_family = AF_INET;
	service.sin_addr.s_addr = INADDR_ANY;
	service.sin_port = htons(PORT - 1);

	if (bind(sock, (SOCKADDR*) &service, sizeof(service)) == SOCKET_ERROR) {
		printf("could not bind socket\n");
		cleanup_socket();
		return false;
	}

	int tval = 1;
	if (setsockopt(sock, SOL_SOCKET, SO_BROADCAST, (char*)&tval, (int)sizeof(tval)) == SOCKET_ERROR) {
		printf("could not set broadcast option, error: %d\r\n", WSAGetLastError());
		cleanup_socket();
		return false;
	}

	return true;
}

bool car_timestamp_sender::init_thread() {
	running = true;

	thread = CreateThread(NULL, 0, send_thread, this, 0, NULL);
	if (thread == NULL) {
		printf("could not create thread\n");
		return false;
	}

	return true;
}

void car_timestamp_sender::cleanup_wsa() {
	if (wsa_init) {
		wsa_init = false;
		WSACleanup();
	}
}

void car_timestamp_sender::cleanup_socket() {
	closesocket(sock);
	sock = INVALID_SOCKET;
}

void car_timestamp_sender::cleanup_thread() {
	if (thread != NULL) {
		if (WaitForSingleObject(thread, 500) != WAIT_OBJECT_0) {
			TerminateThread(thread, 11);
		}

		thread = NULL;
	}
}

DWORD WINAPI car_timestamp_sender::send_thread(LPVOID lpparam) {
	car_timestamp_sender *s = (car_timestamp_sender *)lpparam;

	SetThreadPriority(GetCurrentThread(), THREAD_PRIORITY_HIGHEST);

	sockaddr_in addr;
	addr.sin_family = AF_INET;
	addr.sin_port = htons(PORT);
	addr.sin_addr.s_addr = INADDR_BROADCAST;

	int sec_count = 0;
	while (s->running) {
		sync_packet sp;
		sp.bullstuff = 0;
		sp.secs = htons(++sec_count);
		sp.ticks = 0;

		t_last_send = timestamp::cur();
		sendto(s->sock, (char*)&sp, sizeof(sp), 0, (sockaddr*)&addr, sizeof(addr));

		Sleep(250);
	}

	return 0;
}
