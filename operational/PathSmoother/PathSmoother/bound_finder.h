#include "boundary.h"
#include "ref_obj.h"

#ifndef _BOUND_FINDER_H
#define _BOUND_FINDER_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

#include <vector>

using namespace std;

#define MAX_ABS_BOUND 16

class bound_finder : public ref_obj {
private:
	vector<boundary> bounds;
	bound_side side;

public:
	bound_finder(const vector<boundary>& bounds, bound_side side);

	// determines the boundary at each point p[i] with search vector u[i]. 
	// b is populated with the bounds for the appropriate side
	// s is populated with the desired spacing value
	// alpha_s is populated with the spacing weight factor
	// NOTE: assumes that b is already populated with some worst-case max value
	void get_bounds(const dpvector2* p, const dpvector2* u, int n, double veh_width, double d, double* b, double* s, double* alpha_s, int* hit_index);
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif