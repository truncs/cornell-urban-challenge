#ifndef _ECEFCOORD_H
#define _ECEFCOORD_H

#include <cmath>
#include "dpvector3.h"

class ecefcoord {
private:
	dpvector3 c;

public:
	inline ecefcoord(double x = 0, double y = 0, double z = 0) : c(x, y, z) {};
	inline ecefcoord(const ecefcoord& v) : c(v.c) {};
	inline ecefcoord(const dpvector3& v) : c(v) {};

	inline ecefcoord& operator = (const ecefcoord& v) { c = v.c; return *this; };

	inline double x() const { return c.x(); };
	inline double y() const { return c.y(); };
	inline double z() const { return c.z(); };

	inline double dot(ecefcoord& v) const { return c.dot(v.c); };
	inline ecefcoord cross(ecefcoord& v) const { return ecefcoord(c.cross(v.c)); };

	inline double length() const { return c.mag(); };
	inline double lengthsq() const { return c.magsq(); };

	inline ecefcoord normalize(double l = 1.0) const { return dpvector3(c.normalize(l)); };
	inline ecefcoord& normalize(double l = 1.0) { c.normalize(l); return *this; };

	inline ecefcoord operator - (void) const { return ecefcoord(-c); };
	inline ecefcoord operator + (const ecefcoord& v) const { return ecefcoord(c + v.c); };
	inline ecefcoord operator - (const ecefcoord& v) const { return ecefcoord(c - v.c); };
	inline ecefcoord operator * (const double v) const { return ecefcoord(c * v); };
	inline ecefcoord operator / (const double v) const { return ecefcoord(c / v); };

	inline ecefcoord& operator += (const ecefcoord& v) { c += v.c; return *this; };
	inline ecefcoord& operator -= (const ecefcoord& v) { c -= v.c; return *this; };
	inline ecefcoord& operator *= (const double v) { c *= v; return *this; };
	inline ecefcoord& operator /= (const double v) { c /= v; return *this; };

	inline bool operator == (const ecefcoord& v) { return c == v.c; };
	inline bool operator != (const ecefcoord& v) { return c != v.c; };

	inline operator dpvector3() { return c; };

	inline void get(double* v) { c.get(v); };

	inline ecefcoord rotate(const double theta_xy, const double theta_yz) const {
		double x = cos(theta_xy) * c.x() + sin(theta_xy) * c.y();
		double y = cos(theta_xy) * c.y() - sin(theta_xy) * c.x();
		double z = cos(theta_yz) * c.z() + sin(theta_yz) * y;
		y = cos(theta_yz) * y - sin(theta_yz) * c.z();
		return ecefcoord(x, y, z);
	}
};

#endif