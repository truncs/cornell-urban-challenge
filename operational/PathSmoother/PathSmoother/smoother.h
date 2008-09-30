#ifndef _SMOOTHER_H
#define _SMOOTHER_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

#include <vector>

using namespace std;

#include "coords/dpvector2.h"
#include "time/timestamp.h"
#include "bound_finder.h"
#include "line_list.h"

//#define PRINT_DEBUG

// define this to enable velocity at each point
#define OPT_VEL
// define this to enable slacks on safety spacing
#define OPT_SPACING
// define this to enable w-diff constraints
#define OPT_WDIFF

#define NOM_PT_SPACING 0.5
#define K_MAX 0.2
#define BOUND_INIT 16
#define CONSTR_INF 1e20

#define MIN_SPACING 0.05
#define W_TARGET_INVALID 1e10

// options for defining squared
#define POW2(d) pow2(d)
inline double pow2(double d) { return d * d; }

// gets the index in the sparse hessian array structure for d^2L/(di dj)
#define HESS_IND(i,j) hess_ind(i,j)
// gets the index in the sparse gradient array structure for constraint i wrt j, where j is -1, 0, 1
#define GRAD_IND(i,j) grad_ind(i,j)

#ifdef OPT_VEL
#define HM_IND(i,j) ((j)*(2*n_pts)+(i))
#else
#define HM_IND(i,j) ((j)*(n_pts)+(i))
#endif
#define CGM_IND(i,j) ((j)*(n_c)+(i))

#define W_IND(i) (n_pts+(i))
#ifdef OPT_VEL
#define V_IND(i) (i)
#endif
#ifdef OPT_SPACING
#define Q_L(i) (2*n_pts+(i))
#define Q_U(i) (3*n_pts+(i))
#endif

#define CT_K_MAX(i) ((i)-1)
#ifdef OPT_VEL
#define CT_A_LIM(i) ((n_pts-2)+((i)))
#endif
#ifdef OPT_SPACING
#define CT_S_U(i)   ((2*(n_pts)-3)+(i))
#define CT_S_L(i)   ((3*(n_pts)-3)+(i))
#endif
#ifdef OPT_WDIFF
#define CT_DW(i)    ((4*(n_pts)-3)+(i))
#else
#define CT_HDG      ((4*(n_pts)-4))
#endif

struct path_pt {
	double x, y;
	double v;

	path_pt() {}
	path_pt(double x_, double y_, double v_) : x(x_), y(y_), v(v_) {}
};

enum solve_result {
	sr_success,
	sr_gen_failure,
	sr_infeasible,
	sr_failed_to_converge,
	sr_error,
	sr_cancelled
};

// constraint form specifies how the constraints are set up:
// lhs <= g_i(x) <= rhs
// loqo must have lhs constraints and then optionally rhs bounds
// ipopt allows either
enum constr_form {
	constr_lhs,
	constr_rhs
};

struct smoother_options {
	// weighting coefficients
	double alpha_c, alpha_d, alpha_w, alpha_s, alpha_v, alpha_a;

	// maximum lateral acceleration
	double a_lat_max;
	// maximum braking acceleration
	double a_brake_max;
	// maximum steering
	double k_max;

	// difference in offset terms
	double w_diff;

	// initial heading constraints
	bool set_init_heading;
	double init_heading;

	// final heading constraints
	bool set_final_heading;
	double final_heading;

	// final offset constraints
	// if true, will not allow the final point to move
	bool set_final_offset;
	double final_offset_min;
	double final_offset_max;

	// initial steering constraints
	bool set_init_steering;
	double init_steer_min;
	double init_steer_max;

	// initial velocity constraints
	bool set_min_init_velocity;
	double min_init_velocity;
	bool set_max_init_velocity;
	double max_init_velocity;

	// overall velocity constraints
	double max_velocity;
	double min_velocity;

	// final velocity constraints
	bool set_final_velocity_max;
	double final_velocity_max;

	// number of passes
	int num_passes;

	// indicates if we're going in reverse
	bool reverse;
};

class smoother;

class smoother_engine {
public:
	virtual ~smoother_engine() {}

	virtual solve_result smooth_iter(double mu0, double* w, bool w0, double* y, bool y0, double* z_l, double* z_u, bool z0) = 0;

	virtual void cancel() = 0;

	virtual constr_form get_constr_form() const = 0;
	virtual bool get_ll_hess() const = 0;

	virtual void set_smoother(smoother* sm) = 0;
};

class smoother {
public:
	// number of variables
	int n_pts;
	// base points
	dpvector2* p;
	// displacement vectors
	dpvector2* u;
	// lower bound of w values
	double* lb;
	// upper bound of w values
	double* ub;
	// target w values (where it would intersect original path)
	double* w_target;
	// target w weights
	double* alpha_w_target;

	// precomputed distances between base points
	double* d;

	// desired safety spacing from bounds
	double* s_u;
	double* s_l;

	// alpha_s from bounds
	double* alpha_s_u;
	double* alpha_s_l;

	// indicies of hits for upper and lower bound
	int* hit_index_u;
	int* hit_index_l;

	// constraint lamda's 
	double* lamda;

	// q_u, q_l values, populated after optimization completes
	double* q_u;
	double* q_l;

	// temporary storage for z values (displaced points)
	dpvector2* z;
	// temporary storage for c values (cross products)
	double* c;
	// temporary storage for curvature^2 values
	double* K2;

	// objective function weighting coefficients
	double alpha_c, alpha_d, alpha_w, alpha_s_def, alpha_v, alpha_a;

	// original path
	shared_ptr<line_list> orig_path;
	shared_ptr<bound_finder> target_path;
	// lower/upper bounds
	shared_ptr<bound_finder> ub_finder;
	shared_ptr<bound_finder> lb_finder;

	// hessian index map
	int* hess_map;
	int  nz_hess;

	// constraint gradient index map
	int* cg_map;
	int  nz_cg;

	// maximum lateral acceleration 
	double a_lat_max;
	// maximum braking acceleration
	double a_brake_max;

	// maximum curvature allowed
	double k_max;

	// actual optimization engine
	smoother_engine* engine;

	double bound_d;
	double w_diff;

	constr_form cf;
	// specifies if only the lower-left hand side of the hessian needs to be filled in
	bool ll_hess;

	timespan time_limit;

	// flag indicating if the processing has been cancelled
	volatile bool cancelled;

	// initializes the class with the specified parameters
	smoother(const shared_ptr<line_list>& base_path, const shared_ptr<bound_finder>& target_paths, const shared_ptr<bound_finder>& ub, const shared_ptr<bound_finder>& lb, smoother_engine* engine);
	
	// evaluates the objective value at w
	bool eval_obj(const double *w, bool new_w, double& obj_val);
	// evaluates the gradient of the objective at w
	bool eval_grad_f(const double *w, bool new_w, double* grad_f);
	// evaluates the constraints at w
	bool eval_g(const double *w, bool new_w, double* g);
	// evaluates the jacobian of the constraints at w
	bool eval_grad_g(const double* w, bool new_w, double* A);
	// evaluates the hessian of the lagrangian at w
	bool eval_h(const double* w, bool new_w, double omega_f, const double* y, double* Q);

	// gets the number of variables
	int get_num_vars();
	// gets the number of constraints
	int get_num_constraints();

	// gets the sparsity structure for the constraint jacobian
	int get_nz_A();
	void get_A_sparsity(int* iA, int* kA);
	// gets the sparsity structure for the hessian 
	int get_nz_Q();
	void get_Q_sparsity(int* iQ, int* kQ);

	// gets the lower and upper bounds on the constraints
	void get_constr_bounds(double* g_lb, double* g_ub);

	// gets the lower and upper bounds on the variables
	void get_var_bounds(double* lb, double* ub);

	// sets lin[i] = true if the corresponding variable is linear
	void get_var_linearity(bool* lin);
	// sets lin[i] = true if the corresponding constraint is linear
	void get_constr_linearity(bool* lin);

private:
	void linearize_path(const dpvector2* pts, int n);

	void find_cross();
	void find_lb();
	void find_ub();
	void find_wt();

	void alloc_space();

	void prep_run(const dpvector2* pts, int n);

	void build_A_map();
	void build_Q_map();

	inline int hess_ind(const int i, const int j);
	inline int grad_ind(const int i, const int j);

public:
	// options for smoothing
	smoother_options options;

	// cancel the processing
	virtual void cancel() { 
		cancelled = true;
		if (engine != NULL) {
			engine->cancel();
		}
	}

	// performs the path smoothing
	solve_result smooth_path(vector<path_pt>& ret);

	void get_lb_points(vector<vec2>& v);
	void get_ub_points(vector<vec2>& v);

	// cleans up resources
	virtual ~smoother();
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif