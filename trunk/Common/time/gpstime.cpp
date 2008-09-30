#include "gpstime.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

const gpstime gpstime::invalid = gpstime(-1,-1);

int gpstime::week_offset = 0;
bool gpstime::_init = gpstime::init();

bool gpstime::init() {
	__int64 now;
	GetSystemTimeAsFileTime((FILETIME*)&now);

	// fill in origin of GPS time
	SYSTEMTIME gps;
	gps.wYear = 1980;
	gps.wMonth = 1;
	gps.wDay = 6;
	gps.wDayOfWeek = 0;
	gps.wHour = 0;
	gps.wMinute = 0;
	gps.wSecond = 0;
	gps.wMilliseconds = 0;

	__int64 gps_ft;
	SystemTimeToFileTime(&gps, (FILETIME*)&gps_ft);

	// figure out the difference
	__int64 diff = now - gps_ft;

	// calculate the number of 1024 weeks
	week_offset = (int)(diff / 6193152000000000);
	week_offset *= 1024;

	return true;
}

int gpstime::correct_week(int weeknum) {
	if (weeknum < 1024) {
		return weeknum + week_offset;
	}
	else {
		return weeknum;
	}
}