#include <stdlib.h>
#include <float.h>
#include <time.h>

#include "smoother.h"

#ifdef __cplusplus_cli
#pragma managed(off)
#endif



dpvector2* to_arr(const vector<vec2>& pts) {
	int n = (int)pts.size();
	dpvector2* ret = (dpvector2*)_aligned_malloc(sizeof(dpvector2)*n, __alignof(dpvector2));
	for (int i = 0; i < n; i++) {
		ret[i] = dpvector2(pts[i].x, pts[i].y);
	}
	return ret;
}

// initializes the class with the specified parameters
smoother::smoother(const shared_ptr<line_list>& base_path, const shared_ptr<bound_finder>& target_paths, const shared_ptr<bound_finder>& ub, const shared_ptr<bound_finder>& lb, smoother_engine* engine) {
	this->p = NULL;
	this->u = NULL;
	this->z = NULL;

	this->lb = NULL;
	this->ub = NULL;
	this->c = NULL;
	this->w_target = NULL;
	this->alpha_w_target = NULL;
	this->d = NULL;
	this->s_l = NULL;
	this->s_u = NULL;
	this->alpha_s_l = NULL;
	this->alpha_s_u = NULL;
	this->K2 = NULL;
	this->hit_index_l = NULL;
	this->hit_index_u = NULL;
	this->lamda = NULL;
	this->q_u = NULL;
	this->q_l = NULL;

	this->hess_map = NULL;
	this->cg_map = NULL;

	this->orig_path = base_path;
	this->target_path = target_paths;
	this->ub_finder = ub;
	this->lb_finder = lb;

	this->engine = engine;

	this->cf = engine->get_constr_form();
	this->ll_hess = engine->get_ll_hess();
	engine->set_smoother(this);

	n_pts = 0;

	cancelled = false;
}

// cleans up resources
smoother::~smoother() {
	_aligned_free(p);
	_aligned_free(u);
	_aligned_free(z);

	free(lb);
	free(ub);
	free(c);
	free(w_target);
	free(alpha_w_target);
	free(d);
	free(s_u);
	free(s_l);
	free(alpha_s_u);
	free(alpha_s_l);
	free(K2);
	free(hit_index_u);
	free(hit_index_l);
	free(lamda);
	free(q_u);
	free(q_l);

	delete [] cg_map;
	delete [] hess_map;
}

void smoother::prep_run(const dpvector2* pts, int n) {
	// store the current number of path points
	int orig_pts = n_pts;

	// linearize the path
	linearize_path(pts, n);

	// check if the number of points has changed
	if (n_pts != orig_pts) {
		// allocate space for all the temp variables
		alloc_space();

		// build the hessian/gradient maps
		build_A_map();
		build_Q_map();
	}

	// get the upper/lower bound
	find_ub();
	find_lb();
	// set the bounds on where the point cross each other
	find_cross();
	// set the w-targets
	find_wt();
}

void smoother::linearize_path(const dpvector2* pts, int n) {
	// figure out the total length of the path
	double tot_len = 0;
	for (int i = 0; i < n-1; i++) {
		tot_len += (pts[i+1]-pts[i]).mag();
	}

	// determine the number of points
	n_pts = (int)ceil(tot_len / NOM_PT_SPACING);
	// determine actual spacing
	double act_spacing = tot_len / n_pts;
	// add a few more
	n_pts += 20;

	// allocate storage for points and displacement vectors
	if (p != NULL) _aligned_free(p);
	p = (dpvector2*)_aligned_malloc(sizeof(dpvector2)*n_pts, __alignof(dpvector2));
	if (u != NULL) _aligned_free(u);
	u = (dpvector2*)_aligned_malloc(sizeof(dpvector2)*n_pts, __alignof(dpvector2));

	// fill in the first point
	p[0] = pts[0];

	// keep track of a point index
	int j = 1;

	// track the remaining distance to the next point we want to place
	double rem_dist = act_spacing;
	for (int i = 0; i < n-1; i++) {
		// get the starting point of the line segment
		dpvector2 s = pts[i];
		// get the vector of the line segment
		dpvector2 v = pts[i+1]-s;
		double len = v.mag();
		// normalize
		v = v/len;
		
		// iterate while there is still sufficient length
		while (len >= rem_dist) {
			// store the point
			p[j] = s + v*rem_dist;
			// update the starting point
			s = p[j];
			// increment the point index
			j++;

			// decrement the remaining length on this segment
			len -= rem_dist;
			
			// reset the remaining distance to the next point
			rem_dist = act_spacing;
		}

		rem_dist -= len;
	}
	
	if (rem_dist != act_spacing && (p[j-1] - pts[n-1]).mag() > 0.1) {
		// set the end point
		p[j] = pts[n-1];
		j++;
	}
	// check that we're at the correct index
	n_pts = j;

	// fill in displacement vectors
	// do the first vector
	u[0] = (p[1]-p[0]).norm().rot90();

	// iterate through
	for (int i = 1; i < n_pts-1; i++) {
		// make the displacement vector the bisector of the previous and next point
		u[i] = (p[i+1]-p[i-1]).norm().rot90();
	}
	
	// do the last vector
	u[n_pts-1] = (p[n_pts-1]-p[n_pts-2]).norm().rot90();

	if (d != NULL) {
		delete [] d;
	}

	d = new double[n_pts];

	for (int i = 0; i < n_pts-1; i++) {
		d[i] = (p[i+1]-p[i]).mag();
	}
}

void smoother::find_cross() {
	// go through each of the points and intersect the displacement vector with all other
	// displacement vectors to figure out upper/lower bounds on intersection
	for (int i = 0; i < n_pts; i++) {
		for (int j = i+1; j < n_pts; j++) {
			// check if there is any intersection
			if (abs(u[i].cross(u[j])) > 1e-4) {
				// find the intersection point
				dpmatrix2 A(
					u[i].x(), -u[j].x(),
					u[i].y(), -u[j].y());

				// take the inverse of A
				A.inv();
				
				// determine intersection scaling values
				dpvector2 t = A*(p[j]-p[i]);
				// scale t down a little so that the points don't go exactly to intersection
				t *= 0.9;

				// assign the upper and lower bounds to i and j 
				// if (t.y() >= lb[j] && t.y() <= ub[j])
				if (t.x() > 0) {
					ub[i] = min(ub[i],t.x());
				}
				else {
					lb[i] = max(lb[i],t.x());
				}

				// if (t.x() >= lb[i] && t.x() <= ub[i])
				if (t.y() > 0) {
					ub[j] = min(ub[j],t.y());
				}
				else {
					lb[j] = max(lb[j],t.y());
				}
			}
		}
	}
}

void smoother::find_lb() {
	for (int i = 0; i < n_pts; i++) { 
		lb[i] = -BOUND_INIT;
		s_l[i] = MIN_SPACING; 
		alpha_s_l[i] = -1;
		hit_index_l[i] = -1;
	}

	lb_finder->get_bounds(p, u, n_pts, 2.0, bound_d, lb, s_l, alpha_s_l, hit_index_l);

	for (int i = 0; i < n_pts; i++) {
		if (s_l[i] < MIN_SPACING)
			s_l[i] = MIN_SPACING;

		if (alpha_s_u[i] < 0) {
			alpha_s_u[i] = alpha_s_def;
		}
	}
}

void smoother::find_ub() {
	for (int i = 0; i < n_pts; i++) { 
		ub[i] = BOUND_INIT;
		s_u[i] = MIN_SPACING; 
		alpha_s_u[i] = -1;
		hit_index_u[i] = -1;
	}

	ub_finder->get_bounds(p, u, n_pts, 2.0, bound_d, ub, s_u, alpha_s_u, hit_index_u);

	for (int i = 0; i < n_pts; i++) {
		if (s_u[i] < MIN_SPACING)
			s_u[i] = MIN_SPACING;

		if (alpha_s_u[i] < 0) {
			alpha_s_u[i] = alpha_s_def;
		}
	}
}

void smoother::find_wt() {
	// initialize w_target
	for (int i = 0; i < n_pts; i++) {
		w_target[i] = W_TARGET_INVALID;
		alpha_w_target[i] = 0;
	}

	target_path->get_bounds(p, u, n_pts, 0, 0, w_target, NULL, alpha_w_target, NULL);
}

void smoother::alloc_space() {
	// initialize the lower and upper bounds
	if (lb != NULL) free(lb);
	lb = (double*)malloc(sizeof(double)*n_pts);

	if (ub != NULL) free(ub);
	ub = (double*)malloc(sizeof(double)*n_pts);

	if (s_l != NULL) free(s_l);
	s_l = (double*)malloc(sizeof(double)*n_pts);

	if (s_u != NULL) free(s_u);
	s_u = (double*)malloc(sizeof(double)*n_pts);

	if (alpha_s_l != NULL) free(alpha_s_l);
	alpha_s_l = (double*)malloc(sizeof(double)*n_pts);

	if (alpha_s_u != NULL) free(alpha_s_u);
	alpha_s_u = (double*)malloc(sizeof(double)*n_pts);

	if (hit_index_l != NULL) free(hit_index_l);
	hit_index_l = (int*)malloc(sizeof(int)*n_pts);

	if (hit_index_u != NULL) free(hit_index_u);
	hit_index_u = (int*)malloc(sizeof(int)*n_pts);

	if (lamda != NULL) free(lamda);
	lamda = (double*)malloc(sizeof(double)*get_num_constraints());

	// allocate space for temporary vectors
	if (z != NULL) _aligned_free(z);
	z = (dpvector2*)_aligned_malloc(sizeof(dpvector2)*n_pts, __alignof(dpvector2));

	if (c != NULL) free(c);
	c = (double*)malloc(sizeof(double)*n_pts);

	if (K2 != NULL) free(K2);
	K2 = (double*)malloc(sizeof(double)*n_pts);

	if (w_target != NULL) free(w_target);
	w_target = (double*)malloc(sizeof(double)*n_pts);

	if (alpha_w_target != NULL) free(alpha_w_target);
	alpha_w_target = (double*)malloc(sizeof(double)*n_pts);
}

// evaluates the objective value at w
bool smoother::eval_obj(const double *x, bool new_x, double& obj_val) {
	/* AMPL objective:
	  	minimize obj:
			sum {i in 2..N-1} alpha_c*c[i]^2 +              # minimize (approximate) curvature
			sum {i in 2..N-2} alpha_d*(c[i+1]-c[i])^2 +     # minimize (approximate) change in curvature
			sum {i in 1..N}   alpha_w*(w[i]-w_tar[i])^2 +   # minimize deviation from base path
			sum {i in 1..N}   alpha_s*(q_l[i] + q_u[i]) +   # minimize spacing violation
			sum {i in 1..N-1} alpha_a*af2[i] +              # minimize squared forward acceleration
			sum {i in 1..N}  -alpha_v*v[i];                 # maximize speed

			af2[i] = ((v[i+1]^2 - v[i]^2)/(2*d[i]))^2
	 * */
	obj_val = 0;

	// compute z values (actual points)
	if (new_x) {
		for (int i = 0; i < n_pts; i++) {
			z[i] = p[i] + x[W_IND(i)]*u[i];
		}

		// compute cross-products
		for (int i = 1; i < n_pts-1; i++) {
			dpvector2 d01 = z[i-1]-z[i  ];
			dpvector2 d21 = z[i+1]-z[i  ];
			dpvector2 d20 = z[i+1]-z[i-1];
			c[i] = d01.cross(d21);
			K2[i] = 4*POW2(c[i])/(d01.magsq()*d21.magsq()*d20.magsq());
		}
	}

	// add in w^2, velocity, spacing
	for (int i = 0; i < n_pts; i++) {
		if (w_target[i] < W_TARGET_INVALID) {
			obj_val += alpha_w_target[i]*POW2(x[W_IND(i)]-w_target[i]);
		}
#ifdef OPT_VEL
		obj_val -= alpha_v*x[V_IND(i)];
#endif
#ifdef OPT_SPACING
		obj_val += alpha_s_l[i]*x[Q_L(i)];
		obj_val += alpha_s_u[i]*x[Q_U(i)];
#endif
	}

#ifdef OPT_VEL
	// compute acceleration penalty
	for (int i = 0; i < n_pts-1; i++) {
		double af2 = POW2((POW2(x[V_IND(i+1)])-POW2(x[V_IND(i)]))/(2*d[i]));
		obj_val += alpha_a*af2;
	}
#endif

	// compute cross-product squared and difference in cross-product squared penalties
	for (int i = 1; i < n_pts-2; i++) {
		obj_val += alpha_c*POW2(c[i]);
		obj_val += alpha_d*POW2(c[i+1]-c[i]);
	}

	// add in final cross-product squared penalty
	obj_val += alpha_c*POW2(c[n_pts-2]);
	
	return true;
}

// evaluates the gradient of the objective at w
bool smoother::eval_grad_f(const double *x, bool new_x, double* g) {
	if (new_x) {
		// compute z-values (actual points)
		for (int i = 0; i < n_pts; i++) {
			z[i] = p[i] + x[W_IND(i)]*u[i];
		}

		// pre-compute cross products
		for (int i = 1; i < n_pts-1; i++) {
			dpvector2 d01 = z[i-1]-z[i  ];
			dpvector2 d21 = z[i+1]-z[i  ];
			dpvector2 d20 = z[i+1]-z[i-1];
			c[i] = d01.cross(d21);
			K2[i] = 4*POW2(c[i])/(d01.magsq()*d21.magsq()*d20.magsq());
		}
	}

	// gradient for w^2 penalty, spacing, velocity
	for (int i = 0; i < n_pts; i++) {
		if (w_target[i] < W_TARGET_INVALID) {
			g[W_IND(i)] = alpha_w_target[i]*2*(x[W_IND(i)]-w_target[i]);
		}
		else {
			g[W_IND(i)] = 0;
		}
#ifdef OPT_VEL
		g[V_IND(i)] = -alpha_v;
#endif
#ifdef OPT_SPACING
		g[Q_U(i)] = alpha_s_u[i];
		g[Q_L(i)] = alpha_s_l[i];
#endif
	}

#ifdef OPT_VEL
	// gradient for acceleration
	if (alpha_a > 0) {
		for (int i = 0; i < n_pts-1; i++) {
			g[V_IND(i)] += alpha_a*x[V_IND(i)]*(POW2(x[V_IND(i)])-POW2(x[V_IND(i+1)]))/POW2(d[i]);
			g[V_IND(i+1)] += alpha_a*x[V_IND(i+1)]*(POW2(x[V_IND(i+1)])-POW2(x[V_IND(i)]))/POW2(d[i]);
		}
	}
#endif
	
	// gradient for cross product
	for (int i = 1; i < n_pts-1; i++) {
		dpvector2 d02 = z[i-1]-z[i+1];
		dpvector2 d21 = z[i+1]-z[i  ];
		dpvector2 d10 = z[i  ]-z[i-1];

		double alpha = alpha_c + alpha_d;
		if (i > 1 && i < n_pts-2)
			alpha += alpha_d;

    // dc_i^2/dw_i-1 = 2*c_i*(u_i-1 x (z_i+1 - z_i))
		g[W_IND(i-1)] += alpha*2*c[i]*(u[i-1].cross(d21));
    // dc_i^2/dw_i = 2*c_i*(u_i x (z_i-1 - z_i+1))
		g[W_IND(i  )] += alpha*2*c[i]*(u[i  ].cross(d02));
    // dc_i^2/dw_i+1 = 2*c_i*(u_i+1 x (z_i - z_i-1)
		g[W_IND(i+1)] += alpha*2*c[i]*(u[i+1].cross(d10));
	}

	// gradient for differences
	if (alpha_d > 0) {
		for (int i = 1; i < n_pts-2; i++) {
			dpvector2 d21 = z[i+1]-z[i  ];
			dpvector2 d32 = z[i+2]-z[i+1];
			dpvector2 d10 = z[i  ]-z[i-1];
			dpvector2 d13 = z[i  ]-z[i+2];
			dpvector2 d02 = z[i-1]-z[i+1];

			dpvector2 u0 = u[i-1];
			dpvector2 u1 = u[i];
			dpvector2 u2 = u[i+1];
			dpvector2 u3 = u[i+2];

			double c1 = c[i];
			double c2 = c[i+1];
			
			// add in gradient alpha_d*d_i
			g[W_IND(i-1)] -= alpha_d*2*(c2*u0.cross(d21));
			g[W_IND(i  )] -= alpha_d*2*(c2*u1.cross(d02) + c1*u1.cross(d32));
			g[W_IND(i+1)] -= alpha_d*2*(c1*u2.cross(d13) + c2*u2.cross(d10));
			g[W_IND(i+2)] -= alpha_d*2*(c1*u3.cross(d21));
		}
	}

	return true;
}

// evaluates the constraints at w
bool smoother::eval_g(const double *x, bool new_x, double* h) {
	if (new_x) {
		// compute z-values (actual points)
		for (int i = 0; i < n_pts; i++) {
			z[i] = p[i] + x[W_IND(i)]*u[i];
		}

		// pre-compute cross products
		for (int i = 1; i < n_pts-1; i++) {
			dpvector2 d01 = z[i-1]-z[i  ];
			dpvector2 d21 = z[i+1]-z[i  ];
			dpvector2 d20 = z[i+1]-z[i-1];
			c[i] = d01.cross(d21);
			K2[i] = 4*POW2(c[i])/(d01.magsq()*d21.magsq()*d20.magsq());
		}
	}

	double mult = (cf == constr_lhs) ? -1 : 1;

	// compute curvature constraint
	for (int i = 1; i < n_pts-1; i++) {
		// constraint is negative curvature squared (since LOQO defines constraints as f_i(x) >= b_i
		h[CT_K_MAX(i)] = mult*K2[i];
	}

#ifdef OPT_VEL
#pragma warning (CHANGED TO COMPILE, BUT HAVE NO IDEA WHAT THIS NEXT LINE DOES!)
	h[CT_A_LIM(0)] = mult*(POW2((POW2(x[V_IND(1)])-POW2(x[V_IND(0)]))/(2*d[0]*a_brake_max)));
	// compute acceleration constraint
	for (int i = 1; i < n_pts-1; i++) {
		h[CT_A_LIM(i)] = mult*(POW2((POW2(x[V_IND(i+1)])-POW2(x[V_IND(i)]))/(2*d[i]*a_brake_max)) + POW2(POW2(x[V_IND(i)])/a_lat_max)*K2[i]);
	}
#endif

#ifdef OPT_SPACING
	// compute upper/lower slack constraints
	for (int i = 0; i < n_pts; i++) {
		h[CT_S_U(i)] = mult*(x[W_IND(i)]-x[Q_U(i)]);
		h[CT_S_L(i)] = x[W_IND(i)]+x[Q_L(i)];
	}
#endif

#ifdef OPT_WDIFF
	// compute w-diff constraints
	for (int i = 0; i < n_pts-1; i++) {
		h[CT_DW(i)] = x[W_IND(i+1)]-x[W_IND(i)];
	}
	
	if (options.set_final_heading) {
		double ctheta = cos(options.final_heading);
		double stheta = sin(options.final_heading);
		dpvector2 u1 = u[n_pts-1];
		dpvector2 u0 = u[n_pts-2];
		h[CT_DW(n_pts-2)] = x[W_IND(n_pts-1)]*(stheta*u1.x()-ctheta*u1.y()) - x[W_IND(n_pts-2)]*(stheta*u0.x()-ctheta*u0.y());
	}

#else
	if (options.set_final_heading) {
		double ctheta = cos(options.final_heading);
		double stheta = sin(options.final_heading);
		dpvector2 u1 = u[n_pts-1];
		dpvector2 u0 = u[n_pts-2];
		h[CT_HDG] = x[W_IND(n_pts-1)]*(stheta*u1.x()-ctheta*u1.y()) - x[W_IND(n_pts-2)]*(stheta*u0.x()-ctheta*u0.y());
	}
	else {
		h[CT_HDG] = x[W_IND(n_pts-1)]-x[W_IND(n_pts-2)];
	}
#endif

	return true;
}

// evaluates the jacobian of the constraints at w
bool smoother::eval_grad_g(const double* x, bool new_x, double* A) {
	if (new_x) {
		// compute z-values (actual points)
		for (int i = 0; i < n_pts; i++) {
			z[i] = p[i] + x[W_IND(i)]*u[i];
		}

		// pre-compute cross products
		for (int i = 1; i < n_pts-1; i++) {
			dpvector2 d01 = z[i-1]-z[i  ];
			dpvector2 d21 = z[i+1]-z[i  ];
			dpvector2 d20 = z[i+1]-z[i-1];
			c[i] = d01.cross(d21);
			K2[i] = 4*POW2(c[i])/(d01.magsq()*d21.magsq()*d20.magsq());
		}
	}

	double mult = (cf == constr_lhs) ? -1 : 1;

#ifdef OPT_VEL
	A[GRAD_IND(CT_A_LIM(0),V_IND(0))] = mult*(x[V_IND(0)]/POW2(d[0]*a_brake_max)*(POW2(x[V_IND(0)])-POW2(x[V_IND(1)])));
	A[GRAD_IND(CT_A_LIM(0),V_IND(1))] = mult*(x[V_IND(1)]/POW2(d[0]*a_brake_max)*(POW2(x[V_IND(1)])-POW2(x[V_IND(0)])));
#endif

	// compute gradient of curvature constraints and acceleration constraints
	for (int i = 1; i < n_pts-1; i++) {
		// precompute difference vectors
		dpvector2 d21 = z[i+1] - z[i  ];
		dpvector2 d10 = z[i  ] - z[i-1];
		dpvector2 d01 = z[i-1] - z[i  ];
		dpvector2 d02 = z[i-1] - z[i+1];
		dpvector2 d20 = z[i+1] - z[i-1];
		
		double mi2_inv = 1.0/(d21.magsq()*d10.magsq()*d02.magsq());

		dpvector2 u0 = u[i-1];
		dpvector2 u1 = u[i];
		dpvector2 u2 = u[i+1];

		// fill in curvature gradient and transpose
		// NOTE: negative sign if there because LOQO defines constraints as f_i(x) >= b_i
		double dK2_dwi_1 = mult*8*c[i]*mi2_inv*(u0.cross(d21) + c[i]*mi2_inv*d21.magsq()*(u0.dot(d20)*d01.magsq() - u0.dot(d01)*d20.magsq()));
		double dK2_dwi   = mult*8*c[i]*mi2_inv*(u1.cross(d02) + c[i]*mi2_inv*d20.magsq()*(u1.dot(d21)*d01.magsq() + u1.dot(d01)*d21.magsq()));
		double dK2_dwi1  = mult*8*c[i]*mi2_inv*(u2.cross(d10) - c[i]*mi2_inv*d10.magsq()*(u2.dot(d20)*d21.magsq() + u2.dot(d21)*d20.magsq()));
		A[GRAD_IND(CT_K_MAX(i),W_IND(i-1))] = dK2_dwi_1;
		A[GRAD_IND(CT_K_MAX(i),W_IND(i  ))] = dK2_dwi;
		A[GRAD_IND(CT_K_MAX(i),W_IND(i+1))] = dK2_dwi1;

#ifdef OPT_VEL
		// acceleration limits
		double v_coeff = POW2(POW2(x[V_IND(i)])/a_lat_max);
		A[GRAD_IND(CT_A_LIM(i),W_IND(i-1))] = v_coeff*dK2_dwi_1;
		A[GRAD_IND(CT_A_LIM(i),W_IND(i  ))] = v_coeff*dK2_dwi;
		A[GRAD_IND(CT_A_LIM(i),W_IND(i+1))] = v_coeff*dK2_dwi1;
		A[GRAD_IND(CT_A_LIM(i),V_IND(i  ))] = mult*(x[V_IND(i)]/POW2(d[i]*a_brake_max)*(POW2(x[V_IND(i)])-POW2(x[V_IND(i+1)])) + POW2(2/a_lat_max)*pow(x[V_IND(i)],3)*K2[i]);
		A[GRAD_IND(CT_A_LIM(i),V_IND(i+1))] = mult*(x[V_IND(i+1)]/POW2(d[i]*a_brake_max)*(POW2(x[V_IND(i+1)])-POW2(x[V_IND(i)])));
#endif
	}

#ifdef OPT_SPACING
	// compute gradient of spacing constraints
	for (int i = 0; i < n_pts; i++) {
		A[GRAD_IND(CT_S_U(i),W_IND(i))] = mult;
		A[GRAD_IND(CT_S_U(i),Q_U(i))] = -mult;
		A[GRAD_IND(CT_S_L(i),W_IND(i))] = 1;
		A[GRAD_IND(CT_S_L(i),Q_L(i))] = 1;
	}
#endif

#ifdef OPT_WDIFF
	// compute gradient of w-diff constraints
	for (int i = 0; i < n_pts-1; i++) {
		A[GRAD_IND(CT_DW(i),W_IND(i))] = -1;
		A[GRAD_IND(CT_DW(i),W_IND(i+1))] = 1;
	}

	if (options.set_final_heading) {
		double ctheta = cos(options.final_heading);
		double stheta = sin(options.final_heading);
		dpvector2 u1 = u[n_pts-1];
		dpvector2 u0 = u[n_pts-2];

		A[GRAD_IND(CT_DW(n_pts-2),W_IND(n_pts-2))] = -(stheta*u0.x()-ctheta*u0.y());
		A[GRAD_IND(CT_DW(n_pts-2),W_IND(n_pts-1))] = stheta*u1.x()-ctheta*u1.y();
	}
#else
	if (options.set_final_heading) {
		double ctheta = cos(options.final_heading);
		double stheta = sin(options.final_heading);
		dpvector2 u1 = u[n_pts-1];
		dpvector2 u0 = u[n_pts-2];

		A[GRAD_IND(CT_HDG,W_IND(n_pts-2))] = -(stheta*u0.x()-ctheta*u0.y());
		A[GRAD_IND(CT_HDG,W_IND(n_pts-1))] = stheta*u1.x()-ctheta*u1.y();
	}
	else {
		A[GRAD_IND(CT_HDG,W_IND(n_pts-2))] = -1;
		A[GRAD_IND(CT_HDG,W_IND(n_pts-1))] = 1;
	}
#endif

	return true;
}

// evaluates the hessian of the lagrangian at w
bool smoother::eval_h(const double* x, bool new_x, double omega_f, const double* y, double* Q) {
	double h;

	// initialize Q to zeros
	int nz_q = get_nz_Q();
	for (int i = 0; i < nz_q; i++) {
		Q[i] = 0;
	}

	if (new_x) {
		// compute z-values (actual points)
		// add diagonal entry for w^2 penalty
		for (int i = 0; i < n_pts; i++) {
			z[i] = p[i] + x[W_IND(i)]*u[i];
		}

		// pre-compute cross products
		for (int i = 1; i < n_pts-1; i++) {
			dpvector2 d01 = z[i-1]-z[i  ];
			dpvector2 d21 = z[i+1]-z[i  ];
			dpvector2 d20 = z[i+1]-z[i-1];
			c[i] = d01.cross(d21);
			K2[i] = 4*POW2(c[i])/(d01.magsq()*d21.magsq()*d20.magsq());
		}
	}

	for (int i = 0; i < n_pts; i++) {
		if (w_target[i] < W_TARGET_INVALID) {
			Q[HESS_IND(W_IND(i),W_IND(i))] = omega_f*2*alpha_w_target[i];
		}
	}

	// add in cross-product terms
	for (int i = 1; i < n_pts-1; i++) {
		double alpha = alpha_c + alpha_d;
		if (i > 1 && i < n_pts-2)
			alpha += alpha_d;

		alpha *= omega_f;

		dpvector2 d21 = z[i+1] - z[i  ];
    dpvector2 d02 = z[i-1] - z[i+1];
    dpvector2 d10 = z[i  ] - z[i-1];

		dpvector2 u0 = u[i-1];
		dpvector2 u1 = u[i];
		dpvector2 u2 = u[i+1];

		double c1 = c[i];
    
    // fill in the diagonals first
		Q[HESS_IND(W_IND(i-1),W_IND(i-1))] += alpha*2*POW2(u0.cross(d21));
		Q[HESS_IND(W_IND(i  ),W_IND(i  ))] += alpha*2*POW2(u1.cross(d02));
		Q[HESS_IND(W_IND(i+1),W_IND(i+1))] += alpha*2*POW2(u2.cross(d10));

    // off diagonals
    // i-1,i
		h = alpha*2*(u0.cross(d21)*u1.cross(d02) + c1*u1.cross(u0)); 
		if (!ll_hess) Q[HESS_IND(W_IND(i-1),W_IND(i  ))] += h; 
		Q[HESS_IND(W_IND(i  ),W_IND(i-1))] += h;
    // i-1,i+1
		h = alpha*2*(u2.cross(d10)*u0.cross(d21) + c1*u0.cross(u2)); 
		if (!ll_hess) Q[HESS_IND(W_IND(i-1),W_IND(i+1))] += h; 
		Q[HESS_IND(W_IND(i+1),W_IND(i-1))] += h; 
    // i,i+1
		h = alpha*2*(u2.cross(d10)*u1.cross(d02) + c1*u2.cross(u1)); 
		if (!ll_hess) Q[HESS_IND(W_IND(i  ),W_IND(i+1))] += h; 
		Q[HESS_IND(W_IND(i+1),W_IND(i  ))] += h;
	}

	// add in diff cross-product terms
	if (alpha_d > 0) {
		for (int i =  1; i < n_pts-2; i++) {
			dpvector2 d21 = z[i+1] - z[i  ]; 
			dpvector2 d02 = z[i-1] - z[i+1]; 
			dpvector2 d10 = z[i  ] - z[i-1]; 
			dpvector2 d32 = z[i+2] - z[i+1]; 
			dpvector2 d13 = z[i  ] - z[i+2]; 

			dpvector2 u0 = u[i-1];
			dpvector2 u1 = u[i  ];
			dpvector2 u2 = u[i+1];
			dpvector2 u3 = u[i+2];

			double c1 = c[i];
			double c2 = c[i+1];

			double alpha = omega_f*alpha_d;

			// d_i term
			// diagonals first
			// i-1,i-1 and i+2,i+2 = 0
			// i diagonal
			Q[HESS_IND(W_IND(i  ),W_IND(i  ))] += -alpha*4*(u1.cross(d02))*u1.cross(d32);
			// i+1 diagonal
			Q[HESS_IND(W_IND(i+1),W_IND(i+1))] += -alpha*4*(u2.cross(d13))*u2.cross(d10);
	    
			// off diagonals
			// i-1, i
			h = -alpha*2*(c2*u1.cross(u0) + u0.cross(d21)*u1.cross(d32));
			if (!ll_hess) Q[HESS_IND(W_IND(i-1),W_IND(i  ))] += h; 
			Q[HESS_IND(W_IND(i  ),W_IND(i-1))] += h;
			// i-1, i+1
			h = -alpha*2*(c2*u0.cross(u2) + u0.cross(d21)*u2.cross(d13));
			if (!ll_hess) Q[HESS_IND(W_IND(i-1),W_IND(i+1))] += h; 
			Q[HESS_IND(W_IND(i+1),W_IND(i-1))] += h; 
			// i-1, i+2
			h = -alpha*2*(u0.cross(d21)*u3.cross(d21));
			if (!ll_hess) Q[HESS_IND(W_IND(i-1),W_IND(i+2))] += h; 
			Q[HESS_IND(W_IND(i+2),W_IND(i-1))] += h;
			// i, i+1
			h = -alpha*2*((c1 + c2)*u2.cross(u1) + u1.cross(d32)*u2.cross(d10) + u2.cross(d13)*u1.cross(d02));
			if (!ll_hess) Q[HESS_IND(W_IND(i  ),W_IND(i+1))] += h; 
			Q[HESS_IND(W_IND(i+1),W_IND(i  ))] += h; 
			// i, i+2
			h = -alpha*2*(c1*u1.cross(u3) + u3.cross(d21)*u1.cross(d02));
			if (!ll_hess) Q[HESS_IND(W_IND(i  ),W_IND(i+2))] += h; 
			Q[HESS_IND(W_IND(i+2),W_IND(i  ))] += h; 
			// i+2, i+2
			h = -alpha*2*(c1*u3.cross(u2) + u3.cross(d21)*u2.cross(d10));
			if (!ll_hess) Q[HESS_IND(W_IND(i+1),W_IND(i+2))] += h; 
			Q[HESS_IND(W_IND(i+2),W_IND(i+1))] += h; 
		}
	}

#ifdef OPT_VEL
	if (alpha_a > 0) {
		// add in acceleration hessian
		for (int i = 0; i < n_pts-1; i++) {
			double alpha = alpha_a*omega_f;
			// v_i, v_i
			Q[HESS_IND(V_IND(i),V_IND(i))] += alpha*((3*POW2(x[V_IND(i)]) - POW2(x[V_IND(i+1)]))/POW2(d[i]));
			
			// v_i+1,v_i+1
			Q[HESS_IND(V_IND(i+1),V_IND(i+1))] += alpha*((3*POW2(x[V_IND(i+1)]) - POW2(x[V_IND(i)]))/POW2(d[i]));

			// v_i,v_i+1
			h = -2*alpha/POW2(d[i])*x[V_IND(i)]*x[V_IND(i+1)];
			if (!ll_hess) Q[HESS_IND(V_IND(i),V_IND(i+1))] += h;
			Q[HESS_IND(V_IND(i+1),V_IND(i))] += h;
		}
	}
#endif

	// hessian for curvature constraints
	for (int i = 1; i < n_pts-1; i++) {
		dpvector2 d21 = z[i+1] - z[i  ]; double m2d21_inv = 1.0/d21.magsq();
    dpvector2 d02 = z[i-1] - z[i+1]; 
		dpvector2 d20 = z[i+1] - z[i-1]; double m2d20_inv = 1.0/d20.magsq();
    dpvector2 d10 = z[i  ] - z[i-1];
		dpvector2 d01 = z[i-1] - z[i  ]; double m2d01_inv = 1.0/d01.magsq();

		double m_inv = m2d21_inv*m2d20_inv*m2d01_inv;

		double c1 = c[i];
		double c2 = POW2(c1);
		dpvector2 u0 = u[i-1];
		dpvector2 u1 = u[i  ];
		dpvector2 u2 = u[i+1];

#ifdef OPT_VEL
		double yi = y[CT_K_MAX(i)] + y[CT_A_LIM(i)]*POW2(POW2(x[V_IND(i)])/a_lat_max);
#else
		double yi = y[CT_K_MAX(i)];
#endif

		// diagonal terms
		Q[HESS_IND(W_IND(i-1),W_IND(i-1))] += yi*8*m_inv*(
			+ POW2(u0.cross(d21))
			- c2*m2d20_inv - c2*m2d01_inv 
			+ 4*c2*POW2(u0.dot(d20)*m2d20_inv) 
			+ 4*c2*POW2(u0.dot(d01)*m2d01_inv)
			+ 4*c1*u0.cross(d21)*u0.dot(d20)*m2d20_inv 
			- 4*c1*u0.cross(d21)*u0.dot(d01)*m2d01_inv
			- 4*c2*u0.dot(d01)*u0.dot(d20)*m2d01_inv*m2d20_inv);
		Q[HESS_IND(W_IND(i  ),W_IND(i  ))] += yi*8*m_inv*(
			+ POW2(u1.cross(d02))
			- c2*m2d21_inv - c2*m2d01_inv 
			+ 4*c2*POW2(u1.dot(d21)*m2d21_inv) 
			+ 4*c2*POW2(u1.dot(d01)*m2d01_inv)	
			+ 4*c1*u1.cross(d02)*u1.dot(d21)*m2d21_inv 
			+ 4*c1*u1.cross(d02)*u1.dot(d01)*m2d01_inv
			+ 4*c2*u1.dot(d01)*u1.dot(d21)*m2d01_inv*m2d21_inv);
		Q[HESS_IND(W_IND(i+1),W_IND(i+1))] += yi*8*m_inv*(
			+ POW2(u2.cross(d01))
			- c2*m2d20_inv - c2*m2d21_inv
			+ 4*c2*POW2(u2.dot(d20)*m2d20_inv) 
			+ 4*c2*POW2(u2.dot(d21)*m2d21_inv)
			+ 4*c1*u2.cross(d01)*u2.dot(d20)*m2d20_inv 
			+ 4*c1*u2.cross(d01)*u2.dot(d21)*m2d21_inv
			+ 4*c2*u2.dot(d20)*u2.dot(d21)*m2d20_inv*m2d21_inv);

		// off diagonal terms
		// i-1,i
		h = yi*8*m_inv*(
			+ u1.cross(d02)*u0.cross(d21)
			+ c1*u1.cross(u0)
			+ c2*u0.dot(u1)*m2d01_inv
			+ 2*c1*u1.cross(d02)*u0.dot(d20)*m2d20_inv 
			- 2*c1*u1.cross(d02)*u0.dot(d01)*m2d01_inv
			+ 2*c1*u0.cross(d21)*u1.dot(d01)*m2d01_inv
			+ 2*c1*u0.cross(d21)*u1.dot(d21)*m2d21_inv
			- 2*c2*u0.dot(d01)*u1.dot(d21)*m2d01_inv*m2d21_inv
			+ 2*c2*u0.dot(d20)*u1.dot(d21)*m2d20_inv*m2d21_inv
			+ 2*c2*u0.dot(d20)*u1.dot(d01)*m2d20_inv*m2d01_inv
			- 4*c2*u0.dot(d01)*u1.dot(d01)*m2d01_inv*m2d01_inv);
		if (!ll_hess) Q[HESS_IND(W_IND(i-1),W_IND(i  ))] += h;
		Q[HESS_IND(W_IND(i  ),W_IND(i-1))] += h;
		// i-1,i+1
		h = yi*8*m_inv*(
			- u0.cross(d21)*u2.cross(d01)
			+ c1*u0.cross(u2)
			+ c2*u0.dot(u2)*m2d20_inv
			- 2*c1*u0.cross(d21)*u2.dot(d21)*m2d21_inv
			- 2*c1*u0.cross(d21)*u2.dot(d20)*m2d20_inv
			- 2*c1*u2.cross(d01)*u0.dot(d20)*m2d20_inv
			+ 2*c1*u2.cross(d01)*u0.dot(d01)*m2d01_inv
			- 2*c2*u0.dot(d20)*u2.dot(d21)*m2d20_inv*m2d21_inv
			+ 2*c2*u0.dot(d01)*u2.dot(d21)*m2d01_inv*m2d21_inv
			+ 2*c2*u0.dot(d01)*u2.dot(d20)*m2d01_inv*m2d20_inv
			- 4*c2*u0.dot(d20)*u2.dot(d20)*m2d20_inv*m2d20_inv);
		if (!ll_hess) Q[HESS_IND(W_IND(i-1),W_IND(i+1))] += h;
		Q[HESS_IND(W_IND(i+1),W_IND(i-1))] += h;
		// i,i+1
		h = yi*8*m_inv*(
			- u1.cross(d02)*u2.cross(d01)
			+ c1*u2.cross(u1)
			+ c2*u1.dot(u2)*m2d21_inv
			- 2*c1*u1.cross(d02)*u2.dot(d20)*m2d20_inv
			- 2*c1*u1.cross(d02)*u2.dot(d21)*m2d21_inv
			- 2*c1*u2.cross(d01)*u1.dot(d21)*m2d21_inv
			- 2*c1*u2.cross(d01)*u1.dot(d01)*m2d01_inv
			- 2*c2*u1.dot(d21)*u2.dot(d20)*m2d21_inv*m2d20_inv
			- 2*c2*u1.dot(d01)*u2.dot(d20)*m2d01_inv*m2d20_inv
			- 2*c2*u1.dot(d01)*u2.dot(d21)*m2d01_inv*m2d21_inv
			- 4*c2*u1.dot(d21)*u2.dot(d21)*m2d21_inv*m2d21_inv);
		if (!ll_hess) Q[HESS_IND(W_IND(i  ),W_IND(i+1))] += h;
		Q[HESS_IND(W_IND(i+1),W_IND(i  ))] += h;

#ifdef OPT_VEL
		// do the acceleration constraint velocity partial derivatives
		yi = y[CT_A_LIM(i)];
		// v_i, v_i
		Q[HESS_IND(V_IND(i),V_IND(i))] += yi*((3*POW2(x[V_IND(i)]) - POW2(x[V_IND(i+1)]))/POW2(d[i]*a_brake_max) + 12*POW2(x[V_IND(i)]/a_lat_max)*K2[i]);
		
		// v_i+1,v_i+1
		Q[HESS_IND(V_IND(i+1),V_IND(i+1))] += yi*((3*POW2(x[V_IND(i+1)]) - POW2(x[V_IND(i)]))/POW2(d[i]*a_brake_max));

		// v_i,v_i+1
		h = -2*yi/POW2(d[i]*a_brake_max)*x[V_IND(i)]*x[V_IND(i+1)];
		if (!ll_hess) Q[HESS_IND(V_IND(i),V_IND(i+1))] += h;
		Q[HESS_IND(V_IND(i+1),V_IND(i))] += h;
	
		double fact = yi*8*c[i]*m_inv*POW2(2/a_lat_max)*pow(x[V_IND(i)],3);
		// v_i,w_i-1
		h = fact*(u0.cross(d21) + c[i]*m_inv*d21.magsq()*(u0.dot(d20)*d01.magsq() - u0.dot(d01)*d20.magsq()));
		if (!ll_hess) Q[HESS_IND(V_IND(i),W_IND(i-1))] += h;
		Q[HESS_IND(W_IND(i-1),V_IND(i))] += h;

		// v_i,w_i
		h = fact*(u1.cross(d02) + c[i]*m_inv*d20.magsq()*(u1.dot(d21)*d01.magsq() + u1.dot(d01)*d21.magsq()));
		if (!ll_hess) Q[HESS_IND(V_IND(i),W_IND(i))] += h;
		Q[HESS_IND(W_IND(i),V_IND(i))] += h;

		// v_i,w_i+1
		h = fact*(u2.cross(d10) - c[i]*m_inv*d10.magsq()*(u2.dot(d20)*d21.magsq() + u2.dot(d21)*d20.magsq()));
		if (!ll_hess) Q[HESS_IND(V_IND(i),W_IND(i+1))] += h;
		Q[HESS_IND(W_IND(i+1),V_IND(i))] += h;
#endif
	}

#ifdef OPT_VEL
	// do the acceleration constraint velocity partial derivatives
	double y0 = y[CT_A_LIM(0)];
	// v_i, v_i
	Q[HESS_IND(V_IND(0),V_IND(0))] += y0*((3*POW2(x[V_IND(0)]) - POW2(x[V_IND(1)]))/POW2(d[0]*a_brake_max));
	
	// v_i+1,v_i+1
	Q[HESS_IND(V_IND(1),V_IND(1))] += y0*((3*POW2(x[V_IND(1)]) - POW2(x[V_IND(0)]))/POW2(d[0]*a_brake_max));

	// v_i,v_i+1
	h = -2*y0/POW2(d[0]*a_brake_max)*x[V_IND(0)]*x[V_IND(1)];
	if (!ll_hess) Q[HESS_IND(V_IND(0),V_IND(1))] += h;
	Q[HESS_IND(V_IND(1),V_IND(0))] += h;

#endif

	return true;
}

// gets the number of variables
int smoother::get_num_vars() {
	return 
		n_pts 
#ifdef OPT_VEL
		+ n_pts
#endif
#ifdef OPT_SPACING
		+ 2*n_pts
#endif
		;
}

// gets the number of constraints
int smoother::get_num_constraints() {
	// acceleration constraints, spacing constraints, w-diff constraints
	return 
		(n_pts-2)				// max curvature
#ifdef OPT_VEL
		+ (n_pts-1)			// acceleration limits
#endif
#ifdef OPT_SPACING
		+ 2*n_pts				// spacing constraints
#endif
#ifdef OPT_WDIFF
		+ (n_pts-1)			// w-diff constraints
#else
		+ 1							// final heading constraint
#endif
		;
}

// gets the sparsity structure for the constraint jacobian
int smoother::get_nz_A() {
	return nz_cg;
}

void smoother::get_A_sparsity(int* iA, int* kA) {
	int n_c = get_num_constraints();
	int n_v = get_num_vars();

	// iterate through the hessian map by column and fill the stuff
	int ind_last = 0;
	for (int j = 0; j < n_v; j++) {
		bool first = true;
		for (int i = 0; i < n_c; i++) {
			int ind = cg_map[CGM_IND(i,j)];
			if (ind != -1) {
				if (first) {
					kA[j] = ind;
					first = false;
				}

				iA[ind] = i;
				ind_last = ind+1;
			}
		}

		if (first) {
			kA[j] = ind_last;
		}
	}

	// set the last kA entry
	kA[n_v] = ind_last;
}

void smoother::build_A_map() {
	if (cg_map != NULL) {
		delete [] cg_map;
	}

	int n_v = get_num_vars();
	int n_c = get_num_constraints();

	cg_map = new int[n_v*n_c];

	// iterate through the cg_map and set all entries to -1
	for (int j = 0; j < n_v; j++) {
		for (int i = 0; i < n_c; i++) {
			cg_map[CGM_IND(i,j)] = -1;
		}
	}

	// handle the curvature bound constraints
	for (int i = 1; i < n_pts-1; i++) {
		for (int j = i-1; j <= i+1; j++) {	
			cg_map[CGM_IND(CT_K_MAX(i),W_IND(j))] = 1;
		}
	}

#ifdef OPT_VEL
	cg_map[CGM_IND(CT_A_LIM(0),V_IND(0))] = 1;
	cg_map[CGM_IND(CT_A_LIM(0),V_IND(1))] = 1;
	// acceleration limits
	for (int i = 1; i < n_pts-1; i++) {
		// displacement parameter terms
		for (int j = i-1; j <= i+1; j++) {
			cg_map[CGM_IND(CT_A_LIM(i),W_IND(j))] = 1;
		}

		// velocity terms
		for (int j = i; j <= i+1; j++) {
			cg_map[CGM_IND(CT_A_LIM(i),V_IND(j))] = 1;
		}
	}
#endif

#ifdef OPT_SPACING
	// upper slack constraints
	for (int i = 0; i < n_pts; i++) {
		// upper slack constraints
		// displacement parameter term
		cg_map[CGM_IND(CT_S_U(i),W_IND(i))] = 1;
		// upper slack term
		cg_map[CGM_IND(CT_S_U(i),Q_U(i))] = 1;

		// lower slack constraints
		// displacement parameter term
		cg_map[CGM_IND(CT_S_L(i),W_IND(i))] = 1;
		// lower slack term
		cg_map[CGM_IND(CT_S_L(i),Q_L(i))] = 1;
	}
#endif

#ifdef OPT_WDIFF
	// w-diff constraints
	for (int i = 0; i < n_pts-1; i++) {
		cg_map[CGM_IND(CT_DW(i),W_IND(i))] = 1;
		cg_map[CGM_IND(CT_DW(i),W_IND(i+1))] = 1;
	}
#else
	cg_map[CGM_IND(CT_HDG,W_IND(n_pts-2))] = 1;
	cg_map[CGM_IND(CT_HDG,W_IND(n_pts-1))] = 1;
#endif

	// assign indicies
	// count up the number of non-zeros and assign index
	int ind = 0;
	for (int j = 0; j < n_v; j++) {
		for (int i = 0; i < n_c; i++) {
			if (cg_map[CGM_IND(i,j)] != -1) {
				cg_map[CGM_IND(i,j)] = ind++;
			}
		}
	}

	// update the count 
	nz_cg = ind;
}

// gets the sparsity structure for the hessian 
int smoother::get_nz_Q() {
	return nz_hess;
}

void smoother::get_Q_sparsity(int* iQ, int* kQ) {
	// iterate through the hessian map by column and fill the stuff
	int ind_last = 0;
	int n_hess = n_pts;
#ifdef OPT_VEL
	n_hess += n_pts;
#endif

	for (int j = 0; j < n_hess; j++) {
		bool first = true;
		for (int i = 0; i < n_hess; i++) {
			int ind = hess_map[HM_IND(i,j)];
			if (ind != -1) {
				if (first) {
					kQ[j] = ind;
					first = false;
				}

				iQ[ind] = i;
				ind_last = ind+1;
			}
		}

		if (first) {
			kQ[j] = ind_last;
		}
	}

#ifdef OPT_SPACING
	// iterate through the remaining columns and set the kQ entry
	for (int i = n_hess; i <= n_hess + 2*n_pts; i++) {
		kQ[i] = nz_hess;
	}
#else
	kQ[n_hess] = nz_hess;
#endif
}

void smoother::build_Q_map() {
	// allocate space for map
	if (hess_map != NULL) {
		delete [] hess_map;
	}

	int n_hess = n_pts;
#ifdef OPT_VEL
	n_hess += n_pts;
#endif

	hess_map = new int[n_hess*n_hess];
	
	// initialize the map to -1
	for (int j = 0; j < n_hess; j++) {
		for (int i = 0; i < n_hess; i++) {
			hess_map[HM_IND(i,j)] = -1;
		}
	}

#ifdef OPT_VEL
	// fill in the velocity pattern with ones to mark where non-zeros are
	for (int j = 0; j < n_pts; j++) {
		int i_min = ll_hess ? j : max(j-1,0);
		int i_max = min(j+1,n_pts-1);
		for (int i = i_min; i <= i_max; i++) {
			hess_map[HM_IND(V_IND(i),V_IND(j))] = 1;
		}
	}
#endif

	// fill in the displacement patter with ones to mark where non-zeros are
	for (int j = 0; j < n_pts; j++) {
		int i_min = ll_hess ? j : max(j-3,0);
		int i_max = min(j+3,n_pts-1);
		for (int i = i_min; i <= i_max; i++) {
			hess_map[HM_IND(W_IND(i),W_IND(j))] = 1;
		}
	}

#ifdef OPT_VEL
	// fill in the cross velocity/displacement pattern
	for (int j = 1; j < n_pts-1; j++) {
		// j is the velocity index
		for (int i = j-1; i <= j+1; i++) {
			// i is the displacement index
			hess_map[HM_IND(W_IND(i),V_IND(j))] = 1;
			// mark the upper right corner if necessary
			if (!ll_hess)
				hess_map[HM_IND(V_IND(j),W_IND(i))] = 1;
		}
	}
#endif
	
	// count up the number of non-zeros and assign index
	int ind = 0;
	for (int j = 0; j < n_hess; j++) {
		for (int i = 0; i < n_hess; i++) {
			if (hess_map[HM_IND(i,j)] != -1) {
				hess_map[HM_IND(i,j)] = ind++;
			}
		}
	}

	// update the count 
	nz_hess = ind;
}

void smoother::get_constr_bounds(double* g_lb, double* g_ub) {
	if (cf == constr_lhs) {
		for (int i = 1; i < n_pts-1; i++) {
			g_lb[CT_K_MAX(i)] = -POW2(k_max);
			g_ub[CT_K_MAX(i)] = CONSTR_INF;
		}

#ifdef OPT_VEL
		for (int i = 0; i < n_pts-1; i++) {
			g_lb[CT_A_LIM(i)] = -1;
			g_ub[CT_A_LIM(i)] = CONSTR_INF;
		}
#endif

#ifdef OPT_SPACING
		// add in slack constraints
		for (int i = 0; i < n_pts; i++) {
			g_lb[CT_S_U(i)] = s_u[i] - ub[i];
			g_ub[CT_S_U(i)] = CONSTR_INF;

			g_lb[CT_S_L(i)] = lb[i] + s_l[i];
			g_ub[CT_S_L(i)] = CONSTR_INF;
		}
#endif
	}
	else if (cf == constr_rhs) {
		for (int i = 1; i < n_pts-1; i++) {
			g_lb[CT_K_MAX(i)] = -CONSTR_INF;
			g_ub[CT_K_MAX(i)] = POW2(k_max);

#ifdef OPT_VEL
			g_lb[CT_A_LIM(i)] = -CONSTR_INF;
			g_ub[CT_A_LIM(i)] = 1;
#endif
		}

#ifdef OPT_SPACING
		// add in slack constraints
		for (int i = 0; i < n_pts; i++) {
			g_lb[CT_S_U(i)] = -CONSTR_INF;
			g_ub[CT_S_U(i)] = ub[i] - s_u[i];

			g_lb[CT_S_L(i)] = lb[i] + s_l[i];
			g_ub[CT_S_L(i)] = CONSTR_INF;
		}
#endif
	}

#ifdef OPT_WDIFF
	for (int i = 0; i < n_pts-1; i++) {
		g_lb[CT_DW(i)] = -w_diff;
		g_ub[CT_DW(i)] = w_diff;
	}
#endif

	if (options.set_final_heading) {
		double stheta = sin(options.final_heading);
		double ctheta = cos(options.final_heading);
		dpvector2 dp = p[n_pts-1]-p[n_pts-2];
		double w = ctheta*dp.y()-stheta*dp.x();
#ifdef OPT_WDIFF
		// bound the final heading
		g_lb[CT_DW(n_pts-2)] = w;
		g_ub[CT_DW(n_pts-2)] = w;
#else
		g_lb[CT_HDG] = w;
		g_ub[CT_HDG] = w;
#endif
	}
#ifndef OPT_WDIFF
	else {
		// set it to something large
		g_lb[CT_HDG] = -1;
		g_ub[CT_HDG] = 1;
	}
#endif

	// note: steering limits are not supported right now
}

void smoother::get_var_bounds(double* lb, double* ub) {
	for (int i = 0; i < n_pts; i++) {
		lb[W_IND(i)] = this->lb[i];
		ub[W_IND(i)] = this->ub[i];

#ifdef OPT_VEL
		lb[V_IND(i)] = options.min_velocity;
		if (i > 2) {
			ub[V_IND(i)] = options.max_velocity;
		}
		else {
			ub[V_IND(i)] = (options.max_velocity > 13.3) ? options.max_velocity : 13.3;
		}
#endif

#ifdef OPT_SPACING
		lb[Q_L(i)] = 0;
		ub[Q_L(i)] = s_l[i];

		lb[Q_U(i)] = 0;
		ub[Q_U(i)] = s_u[i];
#endif
	}
	
	// fix the initial offset
	lb[W_IND(0)] = 0;
	ub[W_IND(0)] = 0;

	// fix the second waypoint
	if (options.set_init_heading) {
		double stheta = sin(options.init_heading);
		double ctheta = cos(options.init_heading);
		dpvector2 dp = p[1]-p[0];
		double w1 = (ctheta*dp.y()-stheta*dp.x())/(stheta*u[1].x()-ctheta*u[1].y());
		lb[W_IND(1)] = w1;
		ub[W_IND(1)] = w1;
	}

	// fix the final offset
	if (options.set_final_offset) {
		lb[W_IND(n_pts-1)] = options.final_offset_min;
		ub[W_IND(n_pts-1)] = options.final_offset_max;
	}

#ifdef OPT_VEL
	if (options.set_min_init_velocity) {
		lb[V_IND(0)] = options.min_init_velocity;

		if (options.min_init_velocity > options.max_velocity) {
			// adjust the max velocities so that deceleration is within tolerance
			double curMaxVel = options.min_init_velocity;
			int i = 0;
			do {
				// calculate new velocity given initial velocity, acceleration and distance
				double vf2 = POW2(curMaxVel) - 2*(options.a_brake_max*0.8)*d[i];
				if (vf2 > POW2(options.max_velocity)) {
					ub[V_IND(++i)] = curMaxVel = sqrt(vf2);
				}
				else {
					break;
				}
			} while (curMaxVel > options.max_velocity && i < n_pts-1);
		}
	}
	
	if (options.set_max_init_velocity){
		ub[V_IND(0)] = options.max_init_velocity;
	}

	if (options.set_final_velocity_max) {
		ub[V_IND(n_pts-1)] = options.final_velocity_max;
	}
#endif
}

void smoother::get_var_linearity(bool* lin) {
	for (int i = 0; i < n_pts; i++) {
		lin[W_IND(i)] = false;
#ifdef OPT_VEL
		lin[V_IND(i)] = false;
#endif
#ifdef OPT_SPACING
		lin[Q_U(i)] = true;
		lin[Q_L(i)] = true;
#endif
	}
}

void smoother::get_constr_linearity(bool* lin) {
	for (int i = 1; i < n_pts-1; i++) {
		lin[CT_K_MAX(i)] = false;
#ifdef OPT_VEL
		lin[CT_A_LIM(i)] = false;
#endif
	}

#ifdef OPT_SPACING
	for (int i = 0; i < n_pts; i++) {
		lin[CT_S_L(i)] = true;
		lin[CT_S_U(i)] = true;
	}
#endif

#ifdef OPT_WDIFF
	for (int i = 0; i < n_pts-1; i++) {
		lin[CT_DW(i)] = true;
	}
#endif
}

inline int smoother::hess_ind(const int i, const int j) {
	int ind = hess_map[HM_IND(i,j)];
	assert(ind != -1);
	return ind;
}

inline int smoother::grad_ind(const int i, const int j) {
	int n_c = get_num_constraints();
	int ind = cg_map[CGM_IND(i,j)];
	assert(ind != -1);
	return ind;
}

// performs the path smoothing
solve_result smoother::smooth_path(vector<path_pt>& ret) {
	timestamp start = timestamp::cur();
	timespan total_time = timespan::from_ms(1000);
	
	if (cancelled)
		return sr_error;

	// set the coefficients from options
	alpha_c = options.alpha_c; // 10
	alpha_d = options.alpha_d; // 10
	alpha_w = options.alpha_w; // 0.00025
	alpha_s_def = options.alpha_s; // 10000
	alpha_v = options.alpha_v; // 10
	alpha_a = options.alpha_a; // 10

	// set k_max from options
	k_max = options.k_max;

	// set acceleration limits
	a_lat_max = options.a_lat_max;
	a_brake_max = options.a_brake_max;

	bound_d = options.reverse ? -1 : 1;
	w_diff = options.w_diff;

	//******************************************************
	//* FIRST PASS
	//******************************************************
	// prep the data
	prep_run(orig_path->get_pts(), orig_path->get_num_pts());

#if defined(_DEBUG) && defined(PRINT_DEBUG)
	printf("pass 1--alpha_c: %g, alpha_d: %g, alpha_w: %g, k_max: %g, a_brake_max: %g, a_lat_max: %g\n", alpha_c, alpha_d, alpha_w, k_max, a_brake_max, a_lat_max);
	printf("n_pts: %d, v0: %d, w0: %d, q_u0: %d, q_l0: %d\n", n_pts, V_IND(0), W_IND(0), Q_U(0), Q_L(0));
	printf("CT_K_MAX0: %d, CT_A_LIM0: %d, CT_S_L: %d, CT_S_U: %d\n", CT_K_MAX(1), CT_A_LIM(1), CT_S_L(0), CT_S_U(0));

	// print out the upper, lower bounds and
	printf("  i      lb      ub     s_l     s_u   v_min   v_max\n");
	for (int i = 0; i < n_pts; i++){
		printf("%3d%8.2f%8.2f%8.2f%8.2f%8.2f%8.2f\n", i, lb[i], ub[i], s_l[i], s_u[i], options.min_velocity, options.max_velocity);
	}
#endif

	// allocate space to store the results
	double* x1 = new double[get_num_vars()];

#ifdef RAND_INIT_SOLN
	srand((unsigned int)time(NULL));

	for (int i = 0; i < n_pts; i++) {
		w[W_IND(i)] = 0.1*(rand()/(double)RAND_MAX - 0.5);
	}

	for (int i = 0; i < n_pts; i++) {
		w[V_IND(i)] = 5 + (rand()/(double)RAND_MAX - 0.5);
	}

	for (int i = 0; i < n_pts; i++) {
		w[Q_U(i)] = 0.1*(rand()/(double)RAND_MAX);
		w[Q_L(i)] = 0.1*(rand()/(double)RAND_MAX);
	}
#else

	double init_v;
	if (options.set_max_init_velocity && options.max_init_velocity > 0) {
		init_v = options.max_init_velocity;
	}
	else if (options.set_min_init_velocity && options.min_init_velocity > 0) {
		init_v = options.min_init_velocity;
	}
	else {
		init_v = options.max_velocity;
	}

	for (int i = 0; i < n_pts; i++) {
		// find an initial solution for w
		// check the upper/lower bounds
		double init_w;
		if (ub[i] < 0 || lb[i] > 0) {
			init_w = 0;//(ub[i] + lb[i]) / 2;
		}
		else {
			init_w = 0;
		}
		x1[W_IND(i)] = init_w;
		x1[V_IND(i)] = init_v;
		x1[Q_L(i)] = 0;
		x1[Q_U(i)] = 0;
	}
#endif
	
#if defined(_DEBUG) && defined(PRINT_DEBUG)
	printf("calling smooth_iter\n");
	fflush(stdout);
#endif
#ifdef _DEBUG
	time_limit = timespan::invalid();
#else
	// set the time limit
	time_limit = timespan::invalid();//total_time*0.75;
#endif

	// solve the iteration
	solve_result sr = engine->smooth_iter(1, x1, true, lamda, false, NULL, NULL, false);

#if defined(_DEBUG) && defined(PRINT_DEBUG)
	printf("       w       v     q_l     q_u\n");
	for (int i = 0; i < n_pts; i++) {
		printf("%8.2f %7.2f %7.2f %7.2f\n", x1[W_IND(i)], x1[V_IND(i)], x1[Q_L(i)], x1[Q_U(i)]);
	}

	fflush(stdout);

	int nc = get_num_constraints();
	double *g1 = new double[nc];
	double *g_lb = new double[nc];
	double *g_ub = new double[nc];
	eval_g(x1, true, g1);
	get_constr_bounds(g_lb, g_ub);

	double one = 1;
	double zero = 0;
	double inf = one/zero;

	for (int i = 0; i < nc; i++){
		if (g_ub[i] >= CONSTR_INF){
			g_ub[i] = inf;
		}
	}

	printf("   k_max   a_max     s_l     s_u   k_max   a_max     s_l     s_u\n");
	for (int i = 0; i < n_pts; i++) {
		if (i == 0 || i == n_pts-1) {
			printf("       -       -%8.2f%8.2f       -       -%8.2f%8.2f%\n", g1[CT_S_L(i)], g1[CT_S_U(i)],g_lb[CT_S_L(i)],g_lb[CT_S_U(i)]);
		}
		else {
			printf("%8.4f%8.4f%8.2f%8.2f%8.4f%8.4f%8.2f%8.2f\n", g1[CT_K_MAX(i)], g1[CT_A_LIM(i)], g1[CT_S_L(i)], g1[CT_S_U(i)],
				g_lb[CT_K_MAX(i)],g_lb[CT_A_LIM(i)],g_lb[CT_S_L(i)],g_lb[CT_S_U(i)]);
		}
	}

	delete [] g1;
	delete [] g_lb;
	delete [] g_ub;

	fflush(stdout);

	//getchar();
#endif

	if (sr != sr_success || cancelled) {
		q_u = (double*)malloc(sizeof(double)*n_pts);
		q_l = (double*)malloc(sizeof(double)*n_pts);
		ret.clear();
		ret.reserve(n_pts);
		for (int i = 0; i < n_pts; i++) {
			dpvector2 pt = p[i] + x1[W_IND(i)]*u[i];
#ifdef OPT_VEL
			ret.push_back(path_pt(pt.x(), pt.y(), x1[V_IND(i)]));
#else
			ret.push_back(path_pt(pt.x(), pt.y(), 0));
#endif

			q_l[i] = x1[Q_L(i)];
			q_u[i] = x1[Q_U(i)];
		}

#if defined(_DEBUG) && defined(PRINT_DEBUG)
		//getchar();
#endif

		delete [] x1;

		return sr;
	}

	//******************************************************
	//* LATER PASSES
	//****************************************************** 
	
	// storage for solution
	// if we're only doing 1 pass, then setting x2 to x1 will work out
	// otherwise, we just overwrite what x2 points to 
	double* x2 = x1;

	bound_d = options.reverse ? -4.5 : 4.5;

	// perform the requested number of passes
	for (int i = 0; i < options.num_passes-1; i++) {
#if defined(_DEBUG) && defined(PRINT_DEBUG)
		printf("pass %d--alpha_c: %g, alpha_d: %g, alpha_w: %g, k_max: %g, a_brake_max: %g, a_lat_max: %g\n", i+2, alpha_c, alpha_d, alpha_w, k_max, a_brake_max, a_lat_max);
#else
		timespan used_time = timestamp::cur() - start;
		timespan rem_time = total_time - used_time;
		time_limit = timespan::invalid();//max(timespan::from_ms(20), rem_time);
#endif
	
#if defined(_DEBUG) && defined(PRINT_DEBUG)
		printf("z:     x       y\n");
#endif
		// store the current path into z
		for (int i = 0; i < n_pts; i++) {
			z[i] = p[i] + x1[W_IND(i)]*u[i];
#if defined(_DEBUG) && defined(PRINT_DEBUG)
			printf("%8.2f%8.2f\n", z[i].x(), z[i].y());
#endif
		}

		// store number of points in the path right now
		int orig_pts = n_pts;

		// re-linearize and set everything up
		prep_run(z, orig_pts);

		// allocate space to store the results
		x2 = new double[get_num_vars()];

#if defined(_DEBUG) && defined(PRINT_DEBUG)
		printf("orig_pts: %d, n_pts: %d\n", orig_pts, n_pts);
		printf("      ub      lb     s_l     s_u      wt      v0    q_l0    q_u0      vi     i_l     f_l     i_u     f_u\n");
#endif

		if (orig_pts == n_pts) {
			for (int i = 0; i < n_pts; i++) {
				x2[W_IND(i)] = 0;

				x2[V_IND(i)] = x1[V_IND(i)];
				x2[Q_L(i)] = x1[Q_L(i)];
				x2[Q_U(i)] = x1[Q_U(i)];

#if defined(_DEBUG) && defined(PRINT_DEBUG)
				double wt = w_target[i];
				if (wt >= W_TARGET_INVALID) {
					wt = 0;
				}
				printf("%8.2f%8.2f%8.2f%8.2f%8.2f%8.2f%8.3f%8.3f\n", ub[i], lb[i], s_l[i], s_u[i], wt, x2[V_IND(i)], x2[Q_L(i)], x2[Q_U(i)]);
#endif
			}
		}
		else {
			for (int i = 0; i < n_pts; i++) {
				x2[W_IND(i)] = 0;

				double virt_ind = i/(double)n_pts * orig_pts;

				int i_l = (int)floor(virt_ind);
				int i_u = (int)ceil(virt_ind);

				if (i_l >= orig_pts) {
					i_l = orig_pts-1;
				}

				if (i_u >= orig_pts) {
					i_u = orig_pts-1;
				}

				double f_l = 1 - (virt_ind - i_l);
				double f_u = (virt_ind - i_l);

				// do some sanity checks
				if (f_l + f_u < 1e-5) {
					f_l = 0.5;
					f_u = 0.5;
				}
				else if (f_l + f_u != 1) {
					double sum = f_l + f_u;
					f_l /= sum;
					f_u /= sum;
				}

				x2[V_IND(i)] = f_l*x1[i_l] + f_u*x1[i_u];
				x2[Q_L(i)] = f_l*x1[2*orig_pts+i_l] + f_u*x1[2*orig_pts+i_u];
				x2[Q_U(i)] = f_l*x1[3*orig_pts+i_l] + f_u*x1[3*orig_pts+i_u];

#if defined(_DEBUG) && defined(PRINT_DEBUG)
				double wt = w_target[i];
				if (wt >= W_TARGET_INVALID) {
					wt = 0;
				}

				printf("%8.2f%8.2f%8.2f%8.2f%8.2f%8.2f%8.2f%8.2f%8.2f%8d%8.2f%8d%8.2f\n", ub[i], lb[i], s_l[i], s_u[i], wt, x2[V_IND(i)], x2[Q_L(i)], x2[Q_U(i)], virt_ind,i_l,f_l,i_u,f_u);
#endif
			}
		}

		// update the final offset values
		if (options.set_final_offset) {
			options.final_offset_max -= x1[W_IND(orig_pts-1)];
			options.final_offset_min -= x1[W_IND(orig_pts-1)];
		}
		
#if defined(_DEBUG) && defined(PRINT_DEBUG)
		printf("calling smooth_iter\n");
		fflush(stdout);
#endif
		// solve the iteration
		sr = engine->smooth_iter(1, x2, true, NULL, false, NULL, NULL, false);

#if defined(_DEBUG) && defined(PRINT_DEBUG)
		printf("       w       v     q_l     q_u\n");
		for (int i = 0; i < n_pts; i++) {
			printf("%8.2f %7.2f %7.2f %7.2f\n", x2[W_IND(i)], x2[V_IND(i)], x2[Q_L(i)], x2[Q_U(i)]);
		}

		fflush(stdout);

		//getchar();
#endif

		delete [] x1;

		x1 = x2;

		if (sr != sr_success) {
			break;
		}
	}

#if defined(_DEBUG) && defined(PRINT_DEBUG)
	// get and print constraint values
	double *g = new double[get_num_constraints()];
	eval_g(x2, true, g);
	printf("   k_max   a_max     s_l     s_u\n");
	for (int i = 0; i < n_pts; i++) {
		if (i == 0 || i == n_pts-1) {
			printf("       -       -%8.2f%8.2f\n", g[CT_S_L(i)], g[CT_S_U(i)]);
		}
		else {
			printf("%8.4f%8.4f%8.2f%8.2f\n", g[CT_K_MAX(i)], g[CT_A_LIM(i)], g[CT_S_L(i)], g[CT_S_U(i)]);
		}
	}

	delete [] g;

	fflush(stdout);
#endif

	q_u = (double*)malloc(sizeof(double)*n_pts);
	q_l = (double*)malloc(sizeof(double)*n_pts);
	ret.clear();
	ret.reserve(n_pts);
	for (int i = 0; i < n_pts; i++) {
		dpvector2 pt = p[i] + x2[W_IND(i)]*u[i];
#ifdef OPT_VEL
		ret.push_back(path_pt(pt.x(), pt.y(), x2[V_IND(i)]));
#else
		ret.push_back(path_pt(pt.x(), pt.y(), 0));
#endif
		q_l[i] = x1[Q_L(i)];
		q_u[i] = x1[Q_U(i)];
	}

	delete [] x2;

	return sr;
}

void smoother::get_lb_points(vector<vec2>& v) {
	v.clear();
	v.reserve(n_pts);
	for (int i = 0; i < n_pts; i++){
		dpvector2 pt = p[i] + u[i]*(lb[i] - 1);
		v.push_back(vec2(pt.x(), pt.y()));
	}
}

void smoother::get_ub_points(vector<vec2>& v) {
	v.clear();
	v.reserve(n_pts);
	for (int i = 0; i < n_pts; i++){
		dpvector2 pt = p[i] + u[i]*(ub[i] + 1);
		v.push_back(vec2(pt.x(), pt.y()));
	}
}
