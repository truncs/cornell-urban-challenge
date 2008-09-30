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

#include <exception>
using namespace std;

#include "../coords/dpvector2.h"
#include "../coords/dpmatrix2.h"

#include "timestamp.h"
#include "car_timestamp.h"
#include "timing_estimator.h"

#ifndef _CAR_TIMESTAMP_SYNC_H
#define _CAR_TIMESTAMP_SYNC_H

extern timestamp t_last_send;

class car_timestamp_sync {
private:
	const static double sigma_b;
	const static double sigma_d;
	const static double r;

	WSADATA wsadata;
	bool wsa_init;

	SOCKET sock;
	
	HANDLE thread;
	bool running;

	timing_estimator est;
	
	bool init_wsa();
	bool init_socket();
	bool init_thread();

	void cleanup_wsa();
	void cleanup_socket();
	void cleanup_thread();

	static DWORD WINAPI rx_thread(LPVOID lpparam);

public:
	car_timestamp_sync(bool run_listener = true, bool hypo_test = true);
	~car_timestamp_sync();

	// returns the best estimate of the current car time
	// will return an invalid car timestamp if no estimate is available yet
	car_timestamp current_time() const;

	timestamp translate(const car_timestamp& ts) const;
	car_timestamp translate(const timestamp& ts) const;
	// return the number of car seconds from the local timespan
	timespan translate_from_local(const timespan& ts) const;
	timespan translate_to_local(const timespan& ts) const;

	// call update to do manual updates if you want 
	void update(const timestamp& lt, const car_timestamp& ct);

	double bias() { return est.bias(); }
	double drift() { return est.drift(); }

	// resets the states
	void reset();

	bool has_sync() const;
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif