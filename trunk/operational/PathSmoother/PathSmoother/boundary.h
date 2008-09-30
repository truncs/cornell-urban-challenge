#include "shape.h"
#include "shared_ptr.h"

#ifndef _BOUNDARY_H
#define _BOUNDARY_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

enum bound_side {
	side_left,
	side_right,
	side_path,
};

class boundary {
private:
	shared_ptr<shape> _shape;
	bound_side _side;
	double _desired_spacing;
	double _min_spacing;
	double _alpha_s;
	bool _check_small_obs;
	int _index;

public:
	boundary(shared_ptr<shape> shape, bound_side side, double desired_spacing, double min_spacing, double alpha_s, int index, bool check_small_obs) 
		: _shape(shape), _side(side), _desired_spacing(desired_spacing), _min_spacing(min_spacing), _alpha_s(alpha_s), _index(index), _check_small_obs(check_small_obs) {}

	shared_ptr<const shape> shape() const { return const_ptr(_shape); }
	bound_side side() const { return _side; }

	double desired_spacing() const { return _desired_spacing; }
	double min_spacing() const { return _min_spacing; }

	bool check_small_obs() const { return _check_small_obs; }

	double alpha_s() const { return _alpha_s; }

	int index() const { return _index; }
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif