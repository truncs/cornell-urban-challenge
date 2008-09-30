#include "car_timestamp_sync.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

#define LISTEN_PORT 30

timestamp t_last_send = timestamp::invalid();

// set the process noise
const double car_timestamp_sync::sigma_b = (0.01/1000.0/3);
const double car_timestamp_sync::sigma_d = (0.005/1000.0/3);

// set the measurement noise
const double car_timestamp_sync::r = (0.04/1000.0);

#pragma pack(1)
struct sync_packet {
	unsigned char cmd;
	unsigned short secs;
	unsigned short ticks;
};
#pragma pack()

car_timestamp_sync::car_timestamp_sync(bool run_listener, bool hypo_test) : est(r, sigma_b, sigma_d, hypo_test) {
	wsa_init = false;

	if (!run_listener)
		return;

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
}

car_timestamp_sync::~car_timestamp_sync() {
	cleanup_thread();
	cleanup_socket();
	cleanup_wsa();
}

bool car_timestamp_sync::init_wsa() {
	if (WSAStartup(MAKEWORD(2,2), &wsadata) != NO_ERROR) {
		printf("could not initialize winsock\n");
		return false;
	}
	
	wsa_init = true;
	return true;
}

bool car_timestamp_sync::init_socket() {
	sock = WSASocket(AF_INET, SOCK_DGRAM, IPPROTO_UDP, NULL, 0, 0);
	if (sock == INVALID_SOCKET) {
		printf("could not create socket\n");
		return false;
	}

	sockaddr_in service;
	service.sin_family = AF_INET;
	service.sin_addr.s_addr = INADDR_ANY;
	service.sin_port = htons(LISTEN_PORT);

	if (bind(sock, (SOCKADDR*) &service, sizeof(service)) == SOCKET_ERROR) {
		printf("could not bind socket\n");
		cleanup_socket();
		return false;
	}

	return true;
}

bool car_timestamp_sync::init_thread() {
	running = true;

	thread = CreateThread(NULL, 0, rx_thread, this, 0, NULL);
	if (thread == NULL) {
		printf("could not create thread\n");
		return false;
	}

	return true;
}

void car_timestamp_sync::cleanup_wsa() {
	if (wsa_init) {
		wsa_init = false;
		WSACleanup();
	}
}

void car_timestamp_sync::cleanup_socket() {
	closesocket(sock);
	sock = INVALID_SOCKET;
}

void car_timestamp_sync::cleanup_thread() {
	if (thread != NULL) {
		if (WaitForSingleObject(thread, 500) != WAIT_OBJECT_0) {
			TerminateThread(thread, 11);
		}

		thread = NULL;
	}
}

car_timestamp car_timestamp_sync::current_time() const {
	return translate(timestamp::cur());
}

timestamp car_timestamp_sync::translate(const car_timestamp &ts) const {
	double t = est.translate_to_local(ts.total_secs());
	if (_isnan(t))
		return timestamp::invalid();
	else
		return timestamp::from_elapsed_time(timespan::from_secs(t));
}

car_timestamp car_timestamp_sync::translate(const timestamp &ts) const {
	double t = est.translate_from_local(ts.elapsed_time().total_secs());
	if (_isnan(t))
		return car_timestamp::invalid();
	else
		return car_timestamp::from_secs(t);
}

timespan car_timestamp_sync::translate_from_local(const timespan& ts) const {
	double t = est.translate_span_from_local(ts.total_secs());
	if (_isnan(t))
		return timespan::invalid();
	else
		return timespan::from_secs(t);
}

timespan car_timestamp_sync::translate_to_local(const timespan& ts) const {
	double t = est.translate_span_to_local(ts.total_secs());
	if (_isnan(t))
		return timespan::invalid();
	else
		return timespan::from_secs(t);
}

bool car_timestamp_sync::has_sync() const {
	return est.has_sync();
}

void car_timestamp_sync::reset() {
	est.reset();
}

void car_timestamp_sync::update(const timestamp& lt, const car_timestamp& ct) {
	est.update(lt.elapsed_time().total_secs(), ct.total_secs());
}

DWORD WINAPI car_timestamp_sync::rx_thread(LPVOID lpparam) {
	car_timestamp_sync *s = (car_timestamp_sync *)lpparam;
	SetThreadPriority(GetCurrentThread(), THREAD_PRIORITY_TIME_CRITICAL);

	sync_packet p;

	timespan sum_diff(0);
	int n_diff = 0;

	bool first = true;

	while (s->running) {
		sockaddr_in addr;
		addr.sin_family = AF_INET;
		addr.sin_port = 0;
		addr.sin_addr.s_addr = INADDR_ANY;
		
		int from_len = sizeof(sockaddr_in);

		if (recvfrom(s->sock, (char*)&p, sizeof(p), 0, (sockaddr*)&addr, &from_len) != SOCKET_ERROR) {
			if (p.cmd != 0) {
				continue;
			}

			if (first) {
				first = false;
				continue;
			}

			timestamp t_cur = timestamp::cur();
			p.secs = ntohs(p.secs);
			p.ticks = ntohs(p.ticks);

			s->update(t_cur, car_timestamp(p.secs, p.ticks));
		}
	}

	return 0;
}