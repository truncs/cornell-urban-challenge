#include "line_list.h"

#ifdef __cplusplus_cli
#pragma managed(off)
#endif

#include <float.h>

int shape::shape_count = 0;

line_list::line_list(const dpvector2* pts, const int n_pts) {
	this->n_pts = n_pts;
	alloc_space(n_pts);

	for (int i = 0; i < n_pts; i++) {
		this->pts[i] = pts[i];
	}
}

//line_list::line_list(const vec2* pts, const int n_pts) {
//	this->n_pts = n_pts;
//	alloc_space(n_pts);
//
//	for (int i = 0; i < n_pts; i++) {
//		this->pts[i] = dpvector2(pts[i].x, pts[i].y);
//	}
//}

line_list::line_list(const std::vector<vec2>& pts) {
	this->n_pts = (int)pts.size();
	alloc_space(n_pts);

#if defined(_DEBUG) && defined(PRINT_DEBUG)
	printf("line list created -- %d points\n", n_pts);
#endif
	for (int i = 0; i < n_pts; i++) {
		this->pts[i] = dpvector2(pts[i].x, pts[i].y);

#if defined(_DEBUG) && defined(PRINT_DEBUG)
		printf("%3d%6.2f%6.2f\n", i, this->pts[i].x(), this->pts[i].y());
#endif
	}
}

line_list::~line_list() { 
	_aligned_free(pts);
}

void line_list::alloc_space(int n) {
	pts = (dpvector2*)_aligned_malloc(sizeof(dpvector2)*n, __alignof(dpvector2));
}

const dpvector2* line_list::get_pts() const { return pts; }
int line_list::get_num_pts() const { return n_pts; }

bool line_list::intersect(const dpvector2& p, const dpvector2& u, const double off_dist, const intersection_mode im, dpvector2& t) const {
	bool did_intersect = false;

	double d = abs(off_dist);
	double d_sign = (off_dist < 0) ? -1 : 1;

	switch (im) {
		case im_min:
		case im_min_abs:
			t = dpvector2(DBL_MAX,0);
			break;

		case im_max:
		case im_max_abs:
			t = dpvector2(-DBL_MAX,0);
	}

	int intersection_index = -1;

	for (int i = 0; i < n_pts-1; i++) {
		dpvector2 s = pts[i];
		dpvector2 v = pts[i+1]-pts[i];

		dpvector2 test;
		if (intersect_helper(p, u, s, v, test)) {
			switch (im) {
				case im_min:
					if (test.x() < t.x()) {
						t = test;
						intersection_index = i;
					}
					break;

				case im_min_abs:
					if (abs(test.x()) < abs(t.x())) {
						t = test;
						intersection_index = i;
					}
					break;

				case im_max:
					if (test.x() > t.x()) {
						t = test;
						intersection_index = i;
					}
					break;

				case im_max_abs:
					if (abs(test.x()) > abs(t.x())) {
						t = test;
						intersection_index = i;
					}
					break;
			}

			did_intersect = true;
		}

		if (d != 0) {
			// for each point, now project on to the line from from p by u
			double projected_dist = (s-p).dot(u);
			// determine if the point is "ahead" of the vector
			double cross_prod = u.cross(s-p);
			// get the off-projection distance
			double off_dist = (p+u*projected_dist - pts[i]).mag();
			// check if we're within distance of the stuff
			if ((off_dist < d && d_sign*cross_prod < 0) || (off_dist < std::min(0.5,d) && d_sign*cross_prod > 0)) {
				// this counts as an intersection
				switch (im) {
					case im_min:
						if (projected_dist < t.x()) {
							t = dpvector2(projected_dist, 0);
							intersection_index = i;
						}
						break;

					case im_min_abs:
						if (abs(projected_dist) < abs(t.x())) {
							t = dpvector2(projected_dist, 0);
							intersection_index = i;
						}
						break;

					case im_max:
						if (projected_dist > t.x()) {
							t = dpvector2(projected_dist, 0);
							intersection_index = i;
						}
						break;

					case im_max_abs:
						if (abs(projected_dist) > abs(t.x())) {
							t = dpvector2(projected_dist, 0);
							intersection_index = i;
						}
						break;
				}

				did_intersect = true;
			}
		}
	}

#if defined(_DEBUG) && defined(PRINT_DEBUG)
	if (did_intersect){
		printf("line_list intersection: index = %d, t.x = %.4f, t.y = %.4f\n", intersection_index, t.x(), t.y());
	}
	else {
		printf("line_list - no intersection\n");
	}
#endif

	return did_intersect;
}