#ifndef _SHAPE_H
#define _SHAPE_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

#include "coords/dpvector2.h"
#include "ref_obj.h"

enum intersection_mode {
	im_min,
	im_max,
	im_min_abs,
	im_max_abs
};

class shape : public ref_obj {
public:
	static int shape_count;

	shape() { shape_count++; }

	virtual ~shape() { shape_count--; }

	// tests for intersection of the shape with the search vector starting at p width direction u as well as testing
	// for the closest point to the line segment defined by p, u' and d
	// p - path point
	// u - search vector
	// d - distance of line segment to look for intersection, direction is u'
	virtual bool intersect(const dpvector2& p, const dpvector2& u, const double d, const intersection_mode im, dpvector2& t) const = 0;
};

inline bool intersect_helper(const dpvector2& p, const dpvector2& u, const dpvector2& s, const dpvector2& v, dpvector2& t) {
	dpmatrix2 A(
		u.x(), -v.x(),
		u.y(), -v.y()
		);

	if (abs(A.inv()) > 1e-4) {
		t = A*(s-p);

		return (t.y() >= 0 && t.y() < 1);
	}
	else {
		return false;
	}
}

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif