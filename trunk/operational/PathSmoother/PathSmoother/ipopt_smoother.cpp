#include "ipopt_smoother.h"

#ifdef __cplusplus_cli
#pragma managed(off)
#endif

#ifdef _DLL
#pragma comment(lib,"ipopt_md.lib")
#else
#pragma comment(lib,"ipopt_mt.lib")
#endif

ipopt_smoother::ipopt_smoother(const shared_ptr<line_list>& base_path, const shared_ptr<bound_finder>& ub, const shared_ptr<bound_finder>& lb) 
: smoother(base_path, ub, lb, constr_rhs, true)
{
#ifdef _DEBUG
	app = new IpoptApplication();
#else
	app = new IpoptApplication(false, false);
#endif

#ifdef _DEBUG
	// enable derivative checks
	app->Options()->SetStringValue("derivative_test", "second-order");
	app->Options()->SetNumericValue("derivative_test_tol", 1e-2);
#endif
	// set the mu update strategy
	app->Options()->SetStringValue("mu_strategy", "monotone");
	// set the tolerance
	app->Options()->SetNumericValue("tol", 1e-6);
	// set the max iter
	app->Options()->SetIntegerValue("max_iter", 1000);
	// various other options to be decided later

#ifdef _DEBUG
	printf("initializing app\n");
	fflush(stdout);
#endif

	// initialize with no options file
	ApplicationReturnStatus status = app->Initialize("");

	// i don't like this smart pointer bullshit, but ipopt requires it
	// could cause some sketch-ballz problems
	this_ptr = this;

	// i'm going to add a reference to myself so that the delete method isn't called by smart ptr
	AddRef(NULL);
}

/** default destructor */
ipopt_smoother::~ipopt_smoother() {
	delete app;
}

/**@name Overloaded from TNLP */
//@{
/** Method to return some info about the nlp */
bool ipopt_smoother::get_nlp_info(Index& n, Index& m, Index& nnz_jac_g,
																	Index& nnz_h_lag, IndexStyleEnum& index_style) {
	n = get_num_vars();
	m = get_num_constraints();
	nnz_jac_g = get_nz_A();
	nnz_h_lag = get_nz_Q();

	index_style = TNLP::C_STYLE;

	return true;
}

/** Method to return the bounds for my problem */
bool ipopt_smoother::get_bounds_info(Index n, Number* x_l, Number* x_u,
																		 Index m, Number* g_l, Number* g_u) {
	get_var_bounds(x_l, x_u);
	
  get_constr_bounds(g_l, g_u);
	for (int i = 0; i < m; i++) {
		if (g_l[i] <= -CONSTR_INF)
			g_l[i] = -2e19;
		
		if (g_u[i] >= CONSTR_INF)
			g_u[i] = 2e19;
	}

	return true;
}

/** Method to return the starting point for the algorithm */
bool ipopt_smoother::get_starting_point(Index n, bool init_x, Number* x,
																				bool init_z, Number* z_L, Number* z_U,
																				Index m, bool init_lambda,
																				Number* lambda) {
	if (init_x) {
		if (has_w0) {
			for (int i = 0; i < n; i++) {
				x[i] = this->w[i];
			}
		}
		else {
			for (int i = 0; i < n; i++) {
				x[i] = 0;
			}
		}
	}

	if (init_z) {
		if (has_z0) {
			for (int i = 0; i < n; i++) {
				z_L[i] = this->z_l[i];
				z_U[i] = this->z_u[i];
			}
		}
		else {
			printf("can't init z -- don't have values\n");
			for (int i = 0; i < n; i++) {
				z_L[i] = 0;
				z_U[i] = 0;
			}
		}
	}

	if (init_lambda) {
		if (has_lambda0) {
			for (int i = 0; i < m; i++) {
				lambda[i] = this->lambda[i];
			}
		}
		else {
			printf("can't init lambda -- don't have values\n");
			for (int i = 0; i < m; i++) {
				lambda[i] = 0;
			}
		}
	}

	return true;
}

bool ipopt_smoother::get_variables_linearity(Index n, LinearityType* var_types) {
	var_types = new LinearityType[n];
	bool* lin = new bool[n];
	get_var_linearity(lin);

	for (int i = 0; i < n; i++) {
		if (lin[i])
			var_types[i] = LINEAR;
		else
			var_types[i] = NON_LINEAR;
	}
	
	delete [] lin;

	return true;
}

bool ipopt_smoother::get_constraints_linearity(Index m, LinearityType* const_types) {
	const_types = new LinearityType[m];
	bool* lin = new bool[m];
	
	get_constr_linearity(lin);

	for (int i = 0; i < m; i++) {
		if (lin[i])
			const_types[i] = LINEAR;
		else
			const_types[i] = NON_LINEAR;
	}
	
	delete [] lin;

	return true;
}

/** Method to return the objective value */
bool ipopt_smoother::eval_f(Index n, const Number* x, bool new_x, Number& obj_value) {
	return smoother::eval_obj(x, new_x, obj_value);
}

/** Method to return the gradient of the objective */
bool ipopt_smoother::eval_grad_f(Index n, const Number* x, bool new_x, Number* grad_f) {
	return smoother::eval_grad_f(x, new_x, grad_f);
}

/** Method to return the constraint residuals */
bool ipopt_smoother::eval_g(Index n, const Number* x, bool new_x, Index m, Number* g) {
	return smoother::eval_g(x, new_x, g);
}

/** Method to return:
 *   1) The structure of the jacobian (if "values" is NULL)
 *   2) The values of the jacobian (if "values" is not NULL)
 */
bool ipopt_smoother::eval_jac_g(Index n, const Number* x, bool new_x,
																Index m, Index nele_jac, Index* iRow, Index *jCol,
																Number* values) {

	if (values == NULL) {
		// get the sparsity structure
		// allocate space for the column matrix
		int* kA = new int[n+1];
		get_A_sparsity(iRow, kA);

		// iterate through the columns and fill in the jCol shits
		for (int i = 0; i < n; i++) {
			for (int j = kA[i]; j < kA[i+1]; j++) {
				jCol[j] = i;
			}
		}

		// free temporary storage
		delete kA;

		// return success
		return true;
	}
	else {
		return smoother::eval_grad_g(x, new_x, values);
	}

}

/** Method to return:
 *   1) The structure of the hessian of the lagrangian (if "values" is NULL)
 *   2) The values of the hessian of the lagrangian (if "values" is not NULL)
 */
bool ipopt_smoother::eval_h(Index n, const Number* x, bool new_x,
														Number obj_factor, Index m, const Number* lambda,
														bool new_lambda, Index nele_hess, Index* iRow,
														Index* jCol, Number* values) {
	if (values == NULL) {
		// get the sparsity structure 
		// allocate space for the column matrix
		int* kQ = new int[n+1];
		get_Q_sparsity(iRow, kQ);

		// interate through the column index array and fill in jCol
		for (int i = 0; i < n; i++) {
			for (int j = kQ[i]; j < kQ[i+1]; j++) {
				jCol[j] = i;
			}
		}

		// free temporary storage
		delete kQ;

		// return success 
		return true;
	}
	else {
		return smoother::eval_h(x, new_x, obj_factor, lambda, values);
	}
}

// evaluation routine called by base smoother class
solve_result ipopt_smoother::smooth_iter(double mu0, double* w, bool w0, double* y, bool y0, double* z_l, double* z_u, bool z0) {
	// using adaptive mu update strategy, so mu0 is irrelevant
	start_time = timestamp::cur();

	int n = get_num_vars();
	int m = get_num_constraints();
	this->w = w;
	this->lambda = y;
	this->z_l = z_l;
	this->z_u = z_u;

	this->has_w0 = w0;
	this->has_lambda0 = y0;
	this->has_z0 = z0;

	ApplicationReturnStatus status = app->OptimizeTNLP(this_ptr);
	solve_result sr = sr_gen_failure;

	printf("ipopt result: %d\n", (int)status);
	fflush(stdout);

	switch (status) {
		case Solve_Succeeded:
		case Solved_To_Acceptable_Level:
			sr = sr_success;
			break;
			
		case Infeasible_Problem_Detected:
			sr = sr_infeasible;
			break;

		case Search_Direction_Becomes_Too_Small:
		case Diverging_Iterates:
		case Restoration_Failed:
		case Maximum_Iterations_Exceeded:
			sr = sr_failed_to_converge;
			break;

		case User_Requested_Stop:
		case Error_In_Step_Computation:
		case Not_Enough_Degrees_Of_Freedom:
		case Invalid_Problem_Definition:
		case Invalid_Option:
		case Invalid_Number_Detected:
		case Unrecoverable_Exception:
		case NonIpopt_Exception_Thrown:
		case Insufficient_Memory:
		case Internal_Error:
		default:
			sr = sr_error;
			break;
	}

	return sr;
}

//@}

/** @name Solution Methods */
//@{
/** This method is called when the algorithm is complete so the TNLP can store/write the solution */
void ipopt_smoother::finalize_solution(SolverReturn status,
																			 Index n, const Number* x, const Number* z_L, const Number* z_U,
																			 Index m, const Number* g, const Number* lambda,
																			 Number obj_value,
																			 const IpoptData* ip_data,
																			 IpoptCalculatedQuantities* ip_cq) {
	// copy in w, y, skip z
	for (int i = 0; i < n; i++) {
		if (w != NULL)
			w[i] = x[i];

		if (z_u != NULL)
			z_u[i] = z_U[i];

		if (z_l != NULL)
			z_l[i] = z_L[i];
	}

	if (this->lambda != NULL) {
		for (int j = 0; j < m; j++) {
			this->lambda[i] = lambda[i];
		}
	}

	printf("iterations: %d\n", ip_data->iter_count());
}

bool ipopt_smoother::intermediate_callback(AlgorithmMode mode,
																					 Index iter, Number obj_value,
																					 Number inf_pr, Number inf_du,
																					 Number mu, Number d_norm,
																					 Number regularization_size,
																					 Number alpha_du, Number alpha_pr,
																					 Index ls_trials,
																					 const IpoptData* ip_data,
																					 IpoptCalculatedQuantities* ip_cq) {
	if (time_limit.is_valid() && timestamp::cur() - start_time > time_limit) {
		return false;
	}
	else {
		return true;
	}
}