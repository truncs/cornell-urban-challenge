#include "bound_finder.h"

#ifdef __cplusplus_cli
#pragma managed(off)
#endif

bound_finder::bound_finder(const vector<boundary>& bounds, bound_side side) {
	this->bounds = bounds;
	this->side = side;
}

// determines the boundary at each point p[i] with search vector u[i]. 
// b is populated with the bounds for the appropriate side
// s is populated with the desired spacing value
void bound_finder::get_bounds(const dpvector2* p, const dpvector2* u, int n, double veh_width, double d, double* b, double* s, double* alpha_s, int* hit_index) {
	intersection_mode im;
	if (side == side_left) {
		// upper bound, we want the minimum point
		im = im_min;
	}
	else if (side == side_right) {
		// lower bound, we want the maximum point
		im = im_max;
	}
	else if (side == side_path) {
		im = im_min_abs;
	}

	veh_width /= 2;

	// iterate through the boundaries and intersect with each one
	for (int i = 0; i < n; i++) {

#if defined(_DEBUG) && defined(PRINT_DEBUG)
		printf("bound finder: point %d, p = (%.4f, %.4f), u = (%.4f, %.4f)\n", i, p[i].x(), p[i].y(), u[i].x(), u[i].y());
#endif

		for (vector<boundary>::iterator j = bounds.begin(); j != bounds.end(); j++) {
			double d_shape = (j->check_small_obs() && side != side_path) ? d : 0;
			dpvector2 t;
			// test for intersection
			if (j->shape()->intersect(p[i], u[i], d_shape, im, t)) {

#if defined(_DEBUG) && defined(PRINT_DEBUG)
				printf("bound finder: intersection %d, t.x = %.4f, t.y = %.4f\n", i, t.x(), t.y());
#endif

				if (im == im_min) {
					// adjust the distance by the minimum spacing and the vehicle half-width
					double dist = t.x() - j->min_spacing() - veh_width;
					if (abs(dist) < MAX_ABS_BOUND && dist < b[i]) {
						b[i] = dist;
						if (s != NULL) {
							s[i] = j->desired_spacing() - j->min_spacing();
						}
						if (alpha_s != NULL) {
							alpha_s[i] = j->alpha_s();
						}
						if (hit_index != NULL) {
							hit_index[i] = j->index();
						}
					}
				}
				else if (im == im_max) {
					// adjust the distance by the minimum spacing and the vehicle half-width
					double dist = t.x() + j->min_spacing() + veh_width;
					if (abs(dist) < MAX_ABS_BOUND && dist > b[i]) {
						b[i] = dist;
						if (s != NULL) {
							s[i] = j->desired_spacing() - j->min_spacing();
						}
						if (alpha_s != NULL) {
							alpha_s[i] = j->alpha_s();
						}
						if (hit_index != NULL) {
							hit_index[i] = j->index();
						}
					}
				}
				else if (im == im_min_abs) {
					// adjust the distance by the minimum spacing and the vehicle half-width
					double dist = abs(t.x()) - j->min_spacing() - veh_width;
					if (dist < abs(b[i])) {
						b[i] = ((t.x() < 0) ? -1 : 1) * dist;
						if (s != NULL) {
							s[i] = j->desired_spacing() - j->min_spacing();
						}
						if (alpha_s != NULL) {
							alpha_s[i] = j->alpha_s();
						}
						if (hit_index != NULL) {
							hit_index[i] = j->index();
						}
					}
				}
			}

#if defined(_DEBUG) && defined(PRINT_DEBUG)
			else{
				printf("bound finder: no intersection %d\n", i);
			}
#endif

		}
	}
}