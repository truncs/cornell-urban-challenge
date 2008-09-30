#include "stdmat.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

dpmatrix3 stdmat::Recef2enu(const double px, const double py, const double pz) {
	double Rxy = sqrt(px*px + py*py);
	double gc_lon = atan2(py, px);
	double gc_lat = atan2(pz, Rxy);
	return Recef2enu(gc_lat, gc_lon);
}

dpmatrix3 stdmat::Recef2enu(const double gc_lat, const double gc_lon) {
	double slon = sin(gc_lon), clon = cos(gc_lon);
	double slat = sin(gc_lat), clat = cos(gc_lat);

	return dpmatrix3(
		-slon, clon, 0, 
		-clon*slat, -slon*slat, clat, 
		clon*clat, slon*clat, slat);
}

dpmatrix3 stdmat::Renu2imu(const double yaw, const double pitch, const double roll) {
	double cy = cos(yaw), sy = sin(yaw);
	double cp = cos(pitch), sp = sin(pitch);
	double cr = cos(roll), sr = sin(roll);

	return dpmatrix3(
		cp*cy, cp*sy, -sp, 
		-cr*sy + sr*sp*cy, cr*cy + sr*sp*sy, sr*cp,
		sr*sy + cr*sp*cy, -sr*cy + cr*sp*sy, cr*cp);
}

dpmatrix3 stdmat::dRenu2imudy(const double yaw, const double pitch, const double roll) {
	double sy = sin(yaw), cy = cos(yaw);
	double sp = sin(pitch), cp = cos(pitch);
	double sr = sin(roll), cr = cos(roll);

	return dpmatrix3(
		-cp*sy           ,  cp*cy           , 0,
		-cr*cy - sr*sp*sy, -cr*sy + sr*sp*cy, 0,
		 sr*cy - cr*sp*sy,  sr*sy + cr*sp*cy, 0);
}

dpmatrix3 stdmat::dRenu2imudp(const double yaw, const double pitch, const double roll) {
	double sy = sin(yaw), cy = cos(yaw);
	double sp = sin(pitch), cp = cos(pitch);
	double sr = sin(roll), cr = cos(roll);

	return dpmatrix3(
		-sp*cy   , -sp*sy   , -cp   ,
		 sr*cp*cy,  sr*cp*sy, -sr*sp,
		 cr*cp*cy,  cr*cp*sy, -cr*sp);
}

dpmatrix3 stdmat::dRenu2imudr(const double yaw, const double pitch, const double roll) {
	double sy = sin(yaw), cy = cos(yaw);
	double sp = sin(pitch), cp = cos(pitch);
	double sr = sin(roll), cr = cos(roll);

	return dpmatrix3(
		0               ,  0               ,  0    ,
		sr*sy + cr*sp*cy, -sr*cy + cr*sp*sy,  cr*cp,
		cr*sy - sr*sp*cy, -cr*cy - sr*sp*sy, -sr*cp);
}
	
dpmatrix3 stdmat::dRecef2enudlon(const double gc_lat, const double gc_lon) {
	double slon = sin(gc_lon), clon = cos(gc_lon);
	double slat = sin(gc_lat), clat = cos(gc_lat);

	return dpmatrix3(
		-clon     , -slon     , 0,
		 slon*slat, -clon*slat, 0,
		-slon*clat,  clon*clat, 0);
}

dpmatrix3 stdmat::dRecef2enudlat(const double gc_lat, const double gc_lon) {
	double slon = sin(gc_lon), clon = cos(gc_lon);
	double slat = sin(gc_lat), clat = cos(gc_lat);

	return dpmatrix3(
		 0        ,  0        ,  0    ,
		-clon*clat, -slon*clat, -slat,
		-clon*slat, -slon*slat,  clat);
}