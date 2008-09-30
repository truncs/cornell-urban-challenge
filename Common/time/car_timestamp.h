#ifndef _CAR_TIMESTAMP_H
#define _CAR_TIMESTAMP_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

#include "timestamp.h"
#include <limits>

using namespace std;

#define TICK_PERIOD 0.0001 // in secs
#define TICK_FREQUENCY 10000

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

class car_timestamp {
private:
	unsigned short s;
	unsigned int t;

public:
	inline car_timestamp() { s = 0xffff; t = 0xffffffff; }
	inline car_timestamp(unsigned short secs, unsigned int ticks) { s = secs; t = ticks; }

	inline car_timestamp& operator = (const car_timestamp& c) { s = c.s; t = c.t; return *this; }

	inline static car_timestamp invalid() { return car_timestamp(); }
	
	inline bool is_valid() const { return s != 0xffff || t != 0xffffffff; }
	inline bool is_invalid() const { return s == 0xffff || t == 0xffffffff; }
	inline void invalidate() { s = 0xffff; t = 0xffffffff; }

	inline static car_timestamp from_secs(double secs) {
		int s = (int)floor(secs);
		if (s > numeric_limits<unsigned short>::max()) {
			s = numeric_limits<unsigned short>::max();
		}
		unsigned int t = (unsigned int)(floor((secs - s) * TICK_FREQUENCY));
		return car_timestamp((unsigned short)s, (unsigned int)t);
	}

	inline int secs() const { return s; }
	inline int ticks() const { return t; }

	inline double total_secs() const { return s + t * TICK_PERIOD; }
	inline int total_ticks() const { return s * TICK_FREQUENCY + t; }
	
	inline bool operator == (const car_timestamp& c) const { return s == c.s && t == c.t; }
	inline bool operator != (const car_timestamp& c) const { return s != c.s || t != c.t; }
	inline bool operator >  (const car_timestamp& c) const { return total_ticks() > c.total_ticks(); }
	inline bool operator >= (const car_timestamp& c) const { return total_ticks() >= c.total_ticks(); }
	inline bool operator <  (const car_timestamp& c) const { return total_ticks() < c.total_ticks(); }
	inline bool operator <= (const car_timestamp& c) const { return total_ticks() <= c.total_ticks(); }

	friend timespan operator - (const car_timestamp&, const car_timestamp&);
	friend car_timestamp operator + (const car_timestamp&, const timespan&);
	friend car_timestamp operator + (const timespan&, const car_timestamp&);
	friend car_timestamp operator - (const car_timestamp&, const timespan&);
};

inline timespan operator - (const car_timestamp& l, const car_timestamp& r) { 
	assert(l.is_valid() && r.is_valid());
	return timespan::from_secs(l.total_secs() - r.total_secs()); 
}

inline car_timestamp operator + (const car_timestamp& l, const timespan& r) { 
	assert(l.is_valid() && r.is_valid());
	double ts = l.total_secs() + r.total_secs();

	assert(ts > 0 && ts < 495031.7296);

	int s = (int)floor(ts);
	if (s > numeric_limits<unsigned short>::max()) {
		s = numeric_limits<unsigned short>::max();
	}
	unsigned int t = (unsigned int)(floor((ts - s) * TICK_FREQUENCY));
	return car_timestamp((unsigned short)s, (unsigned int)t);
}

inline car_timestamp operator + (const timespan& l, const car_timestamp& r) { return r + l; }
inline car_timestamp operator - (const car_timestamp& l, const timespan& r) { return l + (-r); }

#ifdef _MAX_DEF_
#pragma pop_macro("max")
#endif

#ifdef _MIN_DEF_
#pragma pop_macro("min")
#endif

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif