#include "polygon.h"

#ifdef __cplusplus_cli
#pragma managed(off)
#endif

#include <float.h>

using namespace std;

polygon::polygon(const dpvector2* pts, int n_pts) {
	this->n_pts = n_pts;
	alloc_space(n_pts+1);

	for (int i = 0; i < n_pts; i++) {
		this->pts[i] = pts[i];
	}

	this->pts[n_pts] = this->pts[0];

	find_bounding_circle();
}

polygon::polygon(const vec2* pts, int n_pts) {
	this->n_pts = n_pts;
	alloc_space(n_pts+1);

	for (int i = 0; i < n_pts; i++) {
		this->pts[i] = dpvector2(pts[i].x, pts[i].y);
	}

	this->pts[n_pts] = this->pts[0];

	find_bounding_circle();
}

polygon::polygon(const std::vector<vec2>& pts) {
	this->n_pts = (int)pts.size();
	alloc_space(n_pts+1);

	for (int i = 0; i < n_pts; i++) {
		this->pts[i] = dpvector2(pts[i].x, pts[i].y);
	}

	this->pts[n_pts] = this->pts[0];

	find_bounding_circle();
}

void polygon::alloc_space(int n) {
	pts = (dpvector2*)_aligned_malloc(sizeof(dpvector2)*n, __alignof(dpvector2));
}

void polygon::find_bounding_circle() {
	dpvector2 sum(0,0);

	for (int i = 0; i < n_pts; i++) {
		sum += pts[i];
	}

	sum /= (double)n_pts;

	cen = sum;

	r = 0.1;
	for (int i = 0; i < n_pts; i++) {
		r = max(r, (cen - pts[i]).mag()); 
	}
}

bool polygon::intersect(const dpvector2& p, const dpvector2& u, const double d, const intersection_mode im, dpvector2& t) const {
	// intersect with each thing
	bool did_intersect = false;

	switch (im) {
		case im_min:
		case im_min_abs:
			t = dpvector2(DBL_MAX,0);
			break;

		case im_max:
		case im_max_abs:
			t = dpvector2(-DBL_MAX,0);
	}

	for (int i = 0; i < n_pts; i++) {
		dpvector2 s = pts[i];
		dpvector2 v = pts[i+1]-pts[i];

		dpvector2 test;
		if (intersect_helper(p, u, s, v, test)) {
			switch (im) {
				case im_min:
					if (test.x() < t.x()) {
						t = test;
					}
					break;

				case im_min_abs:
					if (abs(test.x()) < abs(t.x())) {
						t = test;
					}
					break;

				case im_max:
					if (test.x() > t.x()) {
						t = test;
					}
					break;

				case im_max_abs:
					if (abs(test.x()) > abs(t.x())) {
						t = test;
					}
					break;
			}

			did_intersect = true;
		}
	}

	return did_intersect;
}