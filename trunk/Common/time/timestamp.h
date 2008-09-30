#ifndef _TIMESTAMP_H
#define _TIMESTAMP_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

#include <windows.h>
#include <time.h>
#include <limits>
#include <cassert>
#include <ostream>
#include <float.h>
//#include <intrin.h>

using namespace std;

// The timestamp and timespan classes provide wrappers around the system high performance counter. This
// is the most accurate timing mechanism on a PC, as the system time only updates approximately every 10 ms. 
// The classes represent times as 64-bit integers and the translation of those integers to actual time units
// is dependent on the machine (see QueryPerformanceFrequency). This allows the timing to be carried out at the
// resolution of the system. Note that times will be quantized to the system's resolution, so extremely small 
// time differences may not be representable by the timespan class. 
//
// Some common uses of the class:
// a) Get the current timestamp:
//    timestamp t = timestamp::cur();
// b) Create an invalid timestamp 
//    timestamp t = timestamp::invalid();
//		note: default constructor also creates an invalid timestamp
// c) Check if a timestamp is invalid
//    if (t.is_invalid()) 
// d) Convert the timestamp to seconds elapsed since the program started
//    double secs = t.elapsed_time().total_secs(); 
//		alternatively (but less desirable)
//    double secs = t.secs_elapsed();
// e) Create a timespan from a seconds count
//    timespan dt = timespan::from_secs(5.2);
// f) Add a timespan to a timestamp
//    timestamp t_new = t + dt;
// 
// Note that if the classes are serialized and then read back on a different machine, the tick frequency will 
// likely be different. The best way to serialize a timespan is to convert it to .Net ticks (100-ns increments) 
// using dot_net_ticks() and then recreate using from_dot_net_ticks(). This will give a 64-bit integer that can be
// serialized. To serialize a timestamp, it's best to use elapsed_time() and convert the resulting timespan using
// dot_net_ticks(). The value value of a timestamp is simply the value returned from QueryPerformanceCounter and the
// zero point is undefined. elapsed_time() will have the zero point as the time of program start. Using this method, a
// timestamp can be recreated by calling timestamp::from_elapsed_time(timespan::from_dot_net_ticks(..)). 
//
// You can also convert timestamps to absolute time. Use the to_time_t() function to convert to a C time_t value. The
// relevant C time functions can then be used to deal with this value.
//
// Overloads are provided for outputting formatted timestamps/spans using << and an ostream. 

// Provides matlab-like functions for quickly clocking how long something takes. These are thread-dependent functions, but
// nested calls are not supported.
void tic();
void toc(const char* label = NULL);

// If USE_TSC is defined, the system TSC register will be queried for time values instead of the HighPerformanceCounter. This
// is not recommended as the TSC is not guaranteed to provide a consistent time measurement. On some processors, the TSC does 
// not increment when the processor is in an idle state and the rate of increment is dependent on the current power level/clock
// speed of the processor. If the use of TSC is desired, the timing class will go through a calibration phase on startup and
// will determine the conversion from TSC counts to secs using the HighPerformanceCounter. This can take several seconds and the 
// program will appear to hang.
//#define USE_TSC
//#define CAL_RDTSC

// Internal conversion and constants class. A single global instance is defined in timestamp.cpp
class __timeref {
public:
	__timeref(void);

	__int64 freq_s;
	__int64 freq_ms;
	double	conv_s;
	double  conv_ms;
	double  conv_net; // conversion to .Net ticks

	__int64 dnt_start;
	__int64 start;
	__int64 start_elapsed;
};

extern __timeref __ref;

typedef __int64 ticktype;

class timespan;

class timestamp {
	friend class timespan;

private:
	ticktype t;

	const static __int64 _invalid;

public:
	inline static timestamp cur(void);
	inline static timestamp invalid(void) { return timestamp(_invalid); }
	inline static timestamp from_time_t(const time_t t) { return timestamp(t * __ref.freq_s - __ref.start); }
	inline static timestamp from_elapsed_time(const timespan& t);

	inline timestamp() : t(_invalid) {}
	inline timestamp(const ticktype ticks) : t(ticks) {}
	inline timestamp(const timestamp& c) : t(c.t) {}

	inline timestamp& operator = (const timestamp& c) { t = c.t; return *this; }

	inline ticktype ticks() const { return t; }
	inline ticktype dot_net_ticks() const { return (ticktype)((t - __ref.start_elapsed)*__ref.conv_net) + __ref.dnt_start; }

	// returns a timespan representing the time elapsed since the start of the program
	inline timespan elapsed_time() const;
	// returns the number of seconds since the program started
	// not recommended for actual use, just for convenience
	inline double secs_elapsed() const { assert(is_valid()); return ( t - __ref.start_elapsed ) * __ref.conv_s; }
	inline time_t to_time_t() const { assert(is_valid()); return (t + __ref.start) / __ref.freq_s; }

	inline bool operator == (const timestamp& c) const { return t == c.t; }
	inline bool operator != (const timestamp& c) const { return t != c.t; }
	inline bool operator >  (const timestamp& c) const { return t > c.t; }
	inline bool operator >= (const timestamp& c) const { return t >= c.t; }
	inline bool operator <  (const timestamp& c) const { return t < c.t; }
	inline bool operator <= (const timestamp& c) const { return t <= c.t; }

	friend timespan operator - (const timestamp&, const timestamp&);
	friend timestamp operator + (const timestamp&, const timespan&);
	friend timestamp operator + (const timespan&, const timestamp&);
	friend timestamp operator - (const timestamp&, const timespan&);

	timestamp& operator += (const timespan& c);
	timestamp& operator -= (const timespan& c);

	inline bool is_valid() const { return t != _invalid; }
	inline bool is_invalid() const { return t == _invalid; }
	inline void invalidate() { t = _invalid; }
};

class timespan {
	friend class timestamp;
	
private:
	ticktype t;
	const static ticktype _invalid;

public:
	inline static timespan invalid() { return timespan(_invalid); }

	inline timespan() : t(_invalid) {}
	inline timespan(ticktype ticks) : t(ticks) {}
	inline timespan(const timespan& c) : t(c.t) {}

	inline timespan& operator = (const timespan& c) { t = c.t; return *this; }
	
	inline static timespan from_secs(const double secs);
	inline static timespan from_ms(const double ms);
	inline static timespan from_min(const double min);
	inline static timespan from_time(const double hours, const double min, const double s, const double ms);
	inline static timespan from_dot_net_ticks(const ticktype ticks) { return timespan((ticktype)(ticks / __ref.conv_net)); }

	inline double total_ms() const;
	inline double total_secs() const;
	inline double total_min() const;
	inline double total_hours() const;

	inline ticktype ticks() const { return t; }
	inline ticktype dot_net_ticks() const { return (ticktype)(t * __ref.conv_net); }

	inline bool operator == (const timespan& c) { return t == c.t; }
	inline bool operator != (const timespan& c) { return t != c.t; }
	inline bool operator <  (const timespan& c) { return t < c.t; }
	inline bool operator <= (const timespan& c) { return t <= c.t; }
	inline bool operator >  (const timespan& c) { return t > c.t; }
	inline bool operator >= (const timespan& c) { return t >= c.t; }

	friend timestamp operator + (const timestamp&, const timespan&);
	friend timestamp operator + (const timespan&, const timestamp&);
	friend timestamp operator - (const timestamp&, const timespan&); 
	friend timespan operator + (const timespan&, const timespan&);
	friend timespan operator - (const timespan&, const timespan&);

	friend timespan abs(const timespan&);

	inline timespan& operator += (const timespan& c) { assert(is_valid() && c.is_valid()); t += c.t; return *this; }
	inline timespan& operator -= (const timespan& c) { assert(is_valid() && c.is_valid()); t -= c.t; return *this; }
	inline timespan& operator *= (const double c) { assert(is_valid()); t = (ticktype)(t * c); return *this; }
	inline timespan& operator *= (const int c) { assert(is_valid()); t *= c; return *this; }
	inline timespan& operator *= (const long c) { assert(is_valid()); t *= c; return *this; }
	inline timespan& operator *= (const long long c) { assert(is_valid()); t *= c; return *this; }
	inline timespan& operator *= (const short c) { assert(is_valid()); t *= c; return *this; }
	inline timespan& operator *= (const unsigned short c) { assert(is_valid()); t *= c; return *this; }
	inline timespan& operator *= (const unsigned int c) { assert(is_valid()); t *= c; return *this; }
	inline timespan& operator /= (const double c) { assert(is_valid()); t = (ticktype)(t / c); return *this; }
	inline timespan& operator /= (const long c) { assert(is_valid()); t /= c; return *this; }
	inline timespan& operator /= (const long long c) { assert(is_valid()); t /= c; return *this; }

	inline timespan operator * (const double c) const { assert(is_valid()); return timespan((ticktype)(t * c)); }
	inline timespan operator * (const long long c) const { assert(is_valid()); return timespan(t * c); }
	inline timespan operator * (const long c) const { assert(is_valid()); return timespan(t * c); }
	inline timespan operator * (const short c) const { assert(is_valid()); return timespan(t * c); }
	inline timespan operator * (const unsigned short c) const { assert(is_valid()); return timespan(t * c); }
	inline timespan operator * (const int c) const { assert(is_valid()); return timespan(t * c); }
	inline timespan operator * (const unsigned int c) const { assert(is_valid()); return timespan(t * c); }
	inline timespan operator / (const double c) const { assert(is_valid()); return timespan((ticktype)(t / c)); }
	inline timespan operator / (const long long c) const { assert(is_valid()); return timespan(t / c); }
	inline timespan operator / (const long c) const { assert(is_valid()); return timespan(t / c); }

	inline timespan operator - () const { assert(is_valid()); return timespan(-t); }

	inline bool is_invalid() const { return t == _invalid; }
	inline bool is_valid() const { return t != _invalid; }
	inline void invalidate() { t = _invalid; }

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

	inline static timespan max_ts() { return timespan(numeric_limits<ticktype>::max()); }
	inline static timespan min_ts() { return timespan(numeric_limits<ticktype>::min()); }

#ifdef _MAX_DEF_
#pragma pop_macro("max")
#endif

#ifdef _MIN_DEF_
#pragma pop_macro("min")
#endif
};

inline timespan abs(const timespan& r) {
	if (r.t < 0)
		return timespan(-r.t);
	else
		return timespan(r.t);
}

inline timespan timestamp::elapsed_time() const {
	assert(is_valid());
	return timespan(t - __ref.start_elapsed);
}

inline timestamp timestamp::from_elapsed_time(const timespan &t) {
	assert(t.is_valid());
	return timestamp(__ref.start_elapsed + t.t);
}

inline timespan operator - (const timestamp& a, const timestamp& b) { 
	assert(a.is_valid() && b.is_valid()); 
	return timespan(a.t - b.t); 
}

inline timestamp operator + (const timestamp& a, const timespan& b) {
	assert(a.is_valid() && b.is_valid());
	return timestamp(a.t + b.t);
}

inline timestamp operator + (const timespan& a, const timestamp& b) {
	assert(a.is_valid() && b.is_valid());
	return timestamp(a.t + b.t);
}

inline timestamp operator - (const timestamp& a, const timespan& b) {
	assert(a.is_valid() && b.is_valid());
	return timestamp(a.t - b.t);
}

inline timestamp& timestamp::operator += (const timespan& c) {
	assert(is_valid() && c.is_valid());
	t += c.t;
	return *this;
}

inline timestamp& timestamp::operator -= (const timespan& c) {
	assert(is_valid() && c.is_valid());
	t -= c.t;
	return *this;
}

inline timespan operator + (const timespan& a, const timespan& b) {
	assert(a.is_valid() && b.is_valid());
	return timespan(a.t + b.t);
}

inline timespan operator - (const timespan& a, const timespan& b) {
	assert(a.is_valid() && b.is_valid());
	return timespan(a.t - b.t);
}

inline timestamp timestamp::cur(void) {
#ifdef USE_TSC
	return timestamp(__rdtsc());
#else
	LARGE_INTEGER f;
	QueryPerformanceCounter(&f);
	return timestamp(f.QuadPart);
#endif
}

inline timespan timespan::from_secs(const double secs) { return timespan((ticktype)(secs * __ref.freq_s)); }

inline timespan timespan::from_ms(const double ms) { return timespan((ticktype)(ms * __ref.freq_ms)); }

inline timespan timespan::from_min(const double min) { return timespan((ticktype)(min * __ref.freq_s) * 60); }

inline timespan timespan::from_time(const double hours, const double min, const double s, const double ms) {
	return timespan((ticktype)((hours * 3600 + min * 60 + s) * __ref.freq_s + ms * __ref.freq_ms)); 
}

inline double timespan::total_ms() const { return t * __ref.conv_ms; }

inline double timespan::total_secs() const { return t * __ref.conv_s; }

inline double timespan::total_min() const { return t * __ref.conv_s / 60.0; }

inline double timespan::total_hours() const { return t * __ref.conv_s / 3600.0; }

inline ostream& operator << (ostream& os, const timestamp& t) {
	// outputs the timestamp as a local time (max precision is seconds)
	char buf[50];
	time_t tt = t.to_time_t();
	ctime_s(buf, 50, &tt);
	os << buf;
	return os;
}

inline ostream& operator << (ostream& os, const timespan& t) {
	// outputs the timespan in seconds
	if (t.is_invalid())
		os << "{invalid}";
	else {
		double total_secs = t.total_secs();
		bool neg = false;
		bool sec_only = false;
		if (total_secs < 0) {
			total_secs = -total_secs;
			neg = true;
		}
		int hr = (int)floor(total_secs / 3600);
		total_secs -= hr * 3600;
		int min = (int)floor(total_secs / 60);
		total_secs -= min * 60;
		double ms = total_secs - floor(total_secs);
		total_secs -= ms;
		ms *= 1000;
		
		ios_base::fmtflags orig_flags = os.setf(ios_base::right, ios_base::adjustfield);
		char orig_fill = os.fill('0');
		streamsize orig_width = os.width();

		if (hr != 0) {
			if (neg) os << "-";

			os << hr << ":";
			os.width(2);
			os << min << ":" << total_secs;
		}
		else if (min != 0) {
			if (neg) os << "-";

			os << min << ":" ;
			os.width(2);
			os << total_secs;
		}
		else {
			if (neg) os << "-";

			os << total_secs;

			sec_only = true;
		}

		if (ms != 0) {
			os.width(5);
			os << "." << ms;
		}

		if (sec_only)
			os << "s";

		os.setf(orig_flags);
		os.fill(orig_fill);
		os.width(orig_width);
	}

	return os;
}

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif