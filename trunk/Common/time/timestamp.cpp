#include "timestamp.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

#include <stack>

__timeref __ref = __timeref();

// handle min/max macro defines
#ifdef max
#pragma push_macro("max")
#undef max
#define _MAX_DEF_
#endif

#ifdef min
#pragma push_macro("min")
#undef min
#define _MIN_DEF_
#endif

const ticktype timestamp::_invalid = -1;
const ticktype timespan::_invalid = numeric_limits<ticktype>::min() >> 1; // arbitrary value, too big to actually be used

#ifdef HR_TIMESTAMP_FIXED
const hrticktype hrtimespan::invalid = numeric_limits<ticktype>::min() >> 1; // something like -3.5years
const hrticktype hrtimespan::freq_s = 1000000000000;	// 1 ps resolution
#endif

//const hrtimespan hrtimespan::zero = hrtimespan::from_secs(0);
//const hrtimespan hrtimespan::week = hrtimespan::from_weeks(1);

#ifdef _MAX_DEF_
#pragma pop_macro("max")
#endif

#ifdef _MIN_DEF_
#pragma pop_macro("min")
#endif

// turn off optimization to maximize floating point precision and leave empty loops
#pragma optimize("", off)

__timeref::__timeref() {
#ifdef USE_TSC
	LARGE_INTEGER f;
	QueryPerformanceFrequency(&f);

	// calibrate timing info: figure out time it takes to call QueryPerformanceCounter and figure out clock rate
	LARGE_INTEGER tpc_start, tpc_end, pc_start, pc_end;
	__int64 tsc_start, tsc_end, crap;

	printf("running timing calibration\n");

	int prev_pri = GetThreadPriority(GetCurrentThread());
	SetThreadPriority(GetCurrentThread(), THREAD_PRIORITY_TIME_CRITICAL);
	
	double avg_speed = 0, avg_tpc = 0;
	double var_speed = 0, var_tpc = 0;
	int n = 20;
	for (int i = 0; i < n; i++) {
		// first, measure time it takes to call QueryPerformanceCounter
		QueryPerformanceCounter(&tpc_start);
		QueryPerformanceCounter(&tpc_end);

		// next, try to calculate clock speed knowing that info
		tsc_start = __rdtsc();
		QueryPerformanceCounter(&pc_start);
		// spin for a while by reading the tsc
		for (int i = 0; i < 10000000; i++) {
			crap = i * 10;
		}
		QueryPerformanceCounter(&pc_end);
		tsc_end = __rdtsc();

		// we can now calculate the clock rate
		double tpc = (tpc_end.QuadPart - tpc_start.QuadPart) / (double)f.QuadPart;
		double deltat = (pc_end.QuadPart - pc_start.QuadPart) / (double)f.QuadPart + tpc;
		double clock_rate = (tsc_end - tsc_start) / deltat;

		avg_speed += clock_rate; var_speed += clock_rate * clock_rate;
		avg_tpc += tpc; var_tpc += tpc * tpc;
	}

	avg_speed /= n; avg_tpc /= n;
	var_speed = var_speed / n - avg_speed * avg_speed;
	if (var_speed < 0) var_speed = 0;
	var_tpc = var_tpc / n - avg_tpc * avg_tpc;
	if (var_tpc < 0) var_tpc = 0;

	double std_speed = sqrt(var_speed);
	double std_tpc = sqrt(var_tpc);

	double err_percentage = std_speed / avg_speed * 100.0;

#ifdef CAL_RDTSC
	// calculate time to call __rdtsc()
	// first, calculate time to execute a loop 
	const int n_loop = 10000000;
	QueryPerformanceCounter(&pc_start);
	for (int i = 0; i < n_loop; i++);
	QueryPerformanceCounter(&pc_end);

	double tloop = (pc_end.QuadPart - pc_start.QuadPart) / (double)f.QuadPart;

	// now, do again but call __rdtsc() each time
	QueryPerformanceCounter(&pc_start);
	for (int i = 0; i < n_loop; i++) crap = __rdtsc();
	QueryPerformanceCounter(&pc_end);

	double trdtsc = (pc_end.QuadPart - pc_start.QuadPart) / (double)f.QuadPart - tloop;
#else
	double trdtsc = 0.0;
#endif

	SetThreadPriority(GetCurrentThread(), prev_pri);

	printf("qpc freq: %I64d\n", f.QuadPart);
	printf("avg TSC speed: %.0f Hz, std: %.0f Hz (%f %% err)\n", avg_speed, std_speed, err_percentage);
	printf("avg time for qpc: %f ms, std: %f ms\n", avg_tpc * 1000, std_tpc * 1000);

#ifdef CAL_RDTSC
	printf("avg time for __rdtsc: %f, loop %f (per 10mil)\n", trdtsc, tloop);
	trdtsc /= n_loop;
#endif

	// this is commented out to ignore rounding for now
	// force avg_speed to have 0's up through the 10,000 place
	//avg_speed = floor(avg_speed / 10000) * 10000;
	//printf("rounded speed: %f\n", avg_speed);

	freq_s = (__int64)avg_speed;
	freq_ms = (__int64)(avg_speed / 1000);

	conv_s = 1.0 / avg_speed;
	conv_ms = 1000.0 / avg_speed;

	// there are 1x10^7 .Net ticks per second, so this will give us system ticks per .Net tick
	// not sure which will be more frequent, the high-performance couter or .Net ticks, so use a double
	conv_net = avg_speed / 10000000.0;

	_tzset();
	time_t now = time(NULL);
	__int64 now_tsc = __rdtsc();
	start = now * freq_s - now_tsc;
	start_elapsed = now_tsc;

#else
	LARGE_INTEGER f;
	QueryPerformanceFrequency(&f);

	freq_s = f.QuadPart;
	freq_ms = f.QuadPart / 1000;

	conv_s = 1.0 / (double)freq_s;
	conv_ms = 1000.0 / (double)freq_s;

	// there are 1x10^7 .Net ticks per second, so this will give us system ticks per .Net tick
	// not sure which will be more frequent, the high-performance couter or .Net ticks, so use a double
	conv_net = 10000000.0 / (double)freq_s; 

	_tzset();
	time_t now = time(NULL);
	start = now * freq_s;

	__int64 ft_now;
	GetSystemTimeAsFileTime((LPFILETIME)&ft_now);
	dnt_start = ft_now;

	QueryPerformanceCounter(&f);
	start -= f.QuadPart;
	start_elapsed = f.QuadPart;
	// start is the time in ticks of the current system time less the starting value for QueryPerformanceCounter
#endif	
}

#pragma optimize("", on)

// TODO: make this a stack so nested calls can work
__declspec ( thread ) __int64 tic_toc_cur = -1;
__declspec ( thread ) __int64 tt_f = -1;
__declspec ( thread ) __int64 tt_start;

void tic() {
	if (tt_f == -1) {
		QueryPerformanceFrequency((LARGE_INTEGER*)&tt_f);
	}

	QueryPerformanceCounter((LARGE_INTEGER*)&tt_start);

	tic_toc_cur = timestamp::cur().ticks();
}

void toc(const char* label) {
	timestamp now = timestamp::cur();
	LARGE_INTEGER end;
	QueryPerformanceCounter(&end);

	timespan dur = now - timestamp(tic_toc_cur);

	double dqpc = (end.QuadPart - tt_start) / (double)tt_f;

	if (label != NULL) {
		printf("%s took %f sec (%f)\n", label, dur.total_secs(), dqpc);
	}
	else {
		printf("operation took %f sec (%f)\n", dur.total_secs(), dqpc);
	}
}