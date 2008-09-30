#ifndef _GPSTIME_H
#define _GPSTIME_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

#include "timestamp.h"
#include <windows.h>

//#define CORRECT_ROLLOVER_WK

#define WEEK_SEC 604800
#define DAY_SEC 86400

class gpstime {
private:
	static int week_offset;
	static bool _init;
	static bool init();

	int _weeknum;
	double _ts;

	static int correct_week(int weeknum);

public:
	const static gpstime invalid;

	inline gpstime() : _weeknum(-1), _ts(-1) {}

	inline gpstime(int weeknum, double ts) {
		_weeknum = weeknum % 1024;
		_ts = ts;
	}

	static gpstime from_week_secs(int weeknum, double tsec) { 
#ifdef CORRECT_ROLLOVER_WK
		// handle rollover to get absolute week since 1980
		weeknum = correct_week(weeknum);
#endif
		return gpstime(weeknum, tsec); 
	}

	inline static gpstime from_week_ms(int weeknum, double tms) {
#ifdef CORRECT_ROLLOVER_WK
		// handle rollover to get absolute week since 1980
		weeknum = correct_week(weeknum);
#endif
		return gpstime(weeknum, tms / 1000);
	}

	inline static gpstime from_week_day(int weeknum, int day) {
		// day is based off sunday -- 0 = sunday, 1 = monday, etc
#ifdef CORRECT_ROLLOVER_WK
		// handle rollover to get absolute week since 1980
		weeknum = correct_week(weeknum);
#endif
		return gpstime(weeknum, day * DAY_SEC); // turn days into seconds
	}

	inline int weeknum() const { return _weeknum; }
	inline int proper_weeknum() const { return _weeknum % 1024; }
	inline double week_secs() const { return _ts; }
	inline double day_of_week() const { return floor(_ts / DAY_SEC); }
	inline double time_of_day() const { return fmod(_ts, DAY_SEC); }
	inline double total_secs() const { return _weeknum * WEEK_SEC + _ts; }

	inline bool is_valid() const { return _weeknum != -1 || _ts != -1; }
	inline bool is_invalid() const { return _weeknum == -1 && _ts == -1; }
	inline void invalidate() { _weeknum = -1; _ts = -1; }

	inline bool operator == (const gpstime& c) const { return _weeknum == c._weeknum && _ts == c._ts; }
	inline bool operator != (const gpstime& c) const { return _weeknum != c._weeknum || _ts != c._ts; }
	inline bool operator <  (const gpstime& c) const { 
		return _weeknum < c._weeknum || (_weeknum == c._weeknum && _ts < c._ts); 
	}
	inline bool operator <= (const gpstime& c) const {
		return _weeknum < c._weeknum || (_weeknum == c._weeknum && _ts <= c._ts); 
	}
	inline bool operator >  (const gpstime& c) const {
		return _weeknum > c._weeknum || (_weeknum == c._weeknum && _ts > c._ts); 
	}
	inline bool operator >= (const gpstime& c) const {
		return _weeknum > c._weeknum || (_weeknum == c._weeknum && _ts >= c._ts); 
	}

	friend gpstime operator + (const gpstime&, const double);
	friend gpstime operator + (const gpstime&, const timespan&);
	friend gpstime operator + (const double, const gpstime&);
	friend gpstime operator + (const timespan&, const gpstime&);
	friend gpstime operator - (const gpstime&, const double);
	friend gpstime operator - (const gpstime&, const timespan&);
	friend double operator - (const gpstime&, const gpstime&);

	inline gpstime& operator += (const double t) { 
		assert(is_valid());

		_ts += t;

		if (_ts >= WEEK_SEC) { // check for roll-over
			_ts -= WEEK_SEC; 
			_weeknum++;				// increment the week
		}
		else if (_ts < 0) {
			_ts += WEEK_SEC;
			_weeknum--;
		}

		return *this;
	}

	inline gpstime& operator += (const timespan& t) {	return operator += (t.total_secs()); }

	inline gpstime& operator -= (const double t) {
		assert(is_valid());

		_ts -= t;

		if (_ts >= WEEK_SEC) { // check for roll-over
			_ts -= WEEK_SEC; 
			_weeknum++;				// increment the week
		}
		else if (_ts < 0) {
			_ts += WEEK_SEC;
			_weeknum--;
		}

		return *this;
	}

	inline gpstime& operator -= (const timespan& t) { return operator -= (t.total_secs()); }
};

inline gpstime operator + (const gpstime& g, const double t) {
	assert(g.is_valid());

	double tms = g.week_secs() + t;
	int weeknum = g.weeknum();

	if (tms >= WEEK_SEC) { // check for roll-over
		tms -= WEEK_SEC; 
		weeknum++;				// increment the week
	}
	else if (tms < 0) {
		tms += WEEK_SEC;
		weeknum--;
	}

	return gpstime(weeknum, tms);
}

inline gpstime operator + (const gpstime& g, const timespan& t) { 
	return g + t.total_secs();
}

inline gpstime operator + (const double t, const gpstime& g) {
	return g + t;
}

inline gpstime operator + (const timespan& t, const gpstime& g) {
	return g + t.total_secs(); 
}

inline gpstime operator - (const gpstime& g, const double t) {
	assert(g.is_valid());
	
	double tms = g.week_secs() - t;
	int weeknum = g.weeknum();

	if (tms >= WEEK_SEC) { // check for roll-over
			tms -= WEEK_SEC; 
			weeknum++;				// increment the week
		}
		else if (tms < 0) {
			tms += WEEK_SEC;
			weeknum--;
		}

	return gpstime(weeknum, tms);
}

inline gpstime operator - (const gpstime& g, const timespan& t) {
	return g - t.total_secs();
}

inline double operator - (const gpstime& g, const gpstime& h) {
	assert(g.is_valid() && h.is_valid());

	return (g.weeknum() - h.weeknum()) * WEEK_SEC + (g.week_secs() - h.week_secs());
}

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif