#ifndef _STD_MAT_H
#define _STD_MAT_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

#include "dpmatrix3.h"

class stdmat {
public:
	static dpmatrix3 Recef2enu(const double px, const double py, const double pz);
	static dpmatrix3 Recef2enu(const double gc_lat, const double gc_lon);
	static dpmatrix3 Renu2imu(const double yaw, const double pitch, const double roll);

	static dpmatrix3 dRenu2imudy(const double yaw, const double pitch, const double roll);
	static dpmatrix3 dRenu2imudp(const double yaw, const double pitch, const double roll);
	static dpmatrix3 dRenu2imudr(const double yaw, const double pitch, const double roll);
	
	static dpmatrix3 dRecef2enudlon(const double gc_lat, const double gc_lon);
	static dpmatrix3 dRecef2enudlat(const double gc_lat, const double gc_lon);

};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif


#endif