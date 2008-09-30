#include "shape.h"
#include "vec2.h"

#ifndef _POLYGON_H
#define _POLYGON_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

#include <vector>

class polygon : public shape {
private:
	dpvector2* pts;
	int n_pts;

	// center and radius of bounding circle
	dpvector2 cen;
	double r;

	void alloc_space(int n);
	void find_bounding_circle();

public:
	polygon(const dpvector2* pts, int n_pts);
	polygon(const vec2* pts, int n_pts);
	polygon(const std::vector<vec2>& pts);

	virtual bool intersect(const dpvector2& p, const dpvector2& u, const double d, const intersection_mode im, dpvector2& t) const;
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif