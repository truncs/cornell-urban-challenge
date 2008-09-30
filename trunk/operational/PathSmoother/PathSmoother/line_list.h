#include "shape.h"
#include "vec2.h"

#ifndef _LINE_LIST_H
#define _LINE_LIST_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

#include <vector>

class line_list : public shape {
private:
	dpvector2* pts;
	int n_pts;

	void alloc_space(int n);

public:
	line_list(const dpvector2* pts, const int n_pts);
	//line_list(const vec2* pts, const int n_pts);
	line_list(const std::vector<vec2>& pts);

	virtual ~line_list();

	virtual bool intersect(const dpvector2& p, const dpvector2& u, const double d, const intersection_mode im, dpvector2& t) const; 

	const dpvector2* get_pts() const;
	int get_num_pts() const;
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif