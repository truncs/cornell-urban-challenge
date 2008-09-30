#ifndef _LLACOORD_H
#define _LLACOORD_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

#include <cmath>

class llacoord {
private:
	double _lat, _lon, _alt;

public:
	inline llacoord(double lat = 0.0, double lon = 0.0, double alt = 0.0) {
		this->_lat = lat; this->_lon = lon; this->_alt = alt;
	};

	inline llacoord(const llacoord& v) {
		_lat = v._lat; _lon = v._lon; _alt = v._alt;
	};

	inline llacoord& operator = (const llacoord& v) {
		_lat = v._lat;
		_lon = v._lon;
		_alt = v._alt;

		return *this;
	};

	inline double lat() const { return _lat; };
	inline double lon() const { return _lon; };
	inline double alt() const { return _alt; };

	inline bool operator == (const llacoord& v) const { return _lat == v._lat && _lon == v._lon && _alt == v._alt; };
	inline bool operator != (const llacoord& v) const { return _lat != v._lat || _lon != v._lon || _alt != v._alt; };
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif