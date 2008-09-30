#include "loqo_smoother.h"

#ifdef __cplusplus_cli
#pragma managed(off)
#endif

#include <windows.h>

#pragma comment(lib, "loqo.lib")

loqo_smoother* loqo_smoother::inst = NULL;
bool loqo_smoother::did_set_env = false;

loqo_smoother::loqo_smoother() : lq(NULL) {
	if (InterlockedCompareExchangePointer((volatile PVOID *)&inst, this, NULL) != NULL) {
		printf("can't initialize loqo_smoother: instance already running\n");
		return;
	}

	if (!did_set_env) {
		do_set_env();
	}
}

loqo_smoother::~loqo_smoother() {
	inst = NULL;
}

void loqo_smoother::do_set_env() {
	if (did_set_env)
		return;

	WCHAR wstr[2048];
	DWORD length = 2048;
	// try to get the environment variable
	length = GetEnvironmentVariable(L"LOQODIR", wstr, length);
	if (length == 0) {
		length = 2048;
		length = GetModuleFileName(NULL, wstr, length); 
		// get the position of the last slash
		while (wstr[length] != L'\\' && length > 0) 
			length--;

		if (length != 0) {
			// null terminate the string
			wstr[length] = 0;

			SetEnvironmentVariable(L"LOQODIR", wstr);
		}
	}
	
	did_set_env = true;
}

solve_result loqo_smoother::smooth_iter(double mu0, double* w, bool w0, double* y, bool y0, double* z_l, double* z_u, bool z0) {
	if (inst != this) {
		return sr_error;
	}

	lq = openlp();
	if (lq == NULL) {
		return sr_error;
	}

	lq->n = sm->get_num_vars();
	lq->m = sm->get_num_constraints();
	lq->nz = sm->get_nz_A();
	lq->qnz = sm->get_nz_Q();

	// allocate space for A and sparsity structure
	lq->A = new double[lq->nz];
	lq->iA = new int[lq->nz];
	lq->kA = new int[lq->n+1];
	sm->get_A_sparsity(lq->iA, lq->kA);

	lq->Q = new double[lq->qnz];
	lq->iQ = new int[lq->qnz];
	lq->kQ = new int[lq->n+1];
	sm->get_Q_sparsity(lq->iQ, lq->kQ);

	// allocate space for constraint bounds
	lq->b = new double[lq->m];
	lq->r = new double[lq->m];

	sm->get_constr_bounds(lq->b, lq->r);

	// reduce r by b
	for (int i = 0; i < lq->m; i++){ 
		if (lq->r[i] >= CONSTR_INF)
			lq->r[i] = HUGE_VAL;
		else {
			lq->r[i] -= lq->b[i];
		}
	}

	// linear coefficients in objective approximation
	lq->c = new double[lq->n];

	// lower and upper bounds on variables
	lq->l = new double[lq->n];
	lq->u = new double[lq->n];

	sm->get_var_bounds(lq->l, lq->u);

	for (int i = 0; i < lq->n; i++) {
		if (lq->l[i] <= -CONSTR_INF)
			lq->l[i] = -HUGE_VAL;

		if (lq->u[i] >= CONSTR_INF)
			lq->u[i] = HUGE_VAL;
	}

	// zero out storage
	for (int k = 0; k < lq->nz;  k++) { lq->A[k] = 0; }
	for (int j = 0; j < lq->n;   j++) { lq->c[j] = 0; }
	for (int k = 0; k < lq->qnz; k++) { lq->Q[k] = 0; }

	lq->objval = objval;
	lq->objgrad = objgrad;
	lq->hessian = hessian;
	lq->conval = conval;
	lq->congrad = congrad;

	if (sm->cancelled) {
		closelp(lq);
		lq = NULL;

		return sr_cancelled;
	}

	nlsetup(lq);

	if (sm->time_limit.is_valid()) {
		lq->timlim = sm->time_limit.total_secs();
	}

	// set the iteration limit
	// LOQO doesn't directly tell if a problem is infeasible but just runs out of iterations
	// 250 was chosen because loqo will almost always converge in far fewer iterations than this
	// if the problem is feasible. 
	lq->itnlim = 200;
	lq->sf_req = 7;

	// allocate storage for vars
	lq->x = new double[lq->n];
	
	if (w0) {
		for (int i = 0; i < lq->n; i++)
			lq->x[i] = w[i];
	}
	else {
		for (int i = 0; i < lq->n; i++)
			lq->x[i] = 0;
	}

#if defined(_DEBUG) && defined(PRINT_DEBUG)
	lq->verbose = 2;
#else
	lq->verbose = 0;
#endif

	if (sm->cancelled) {
		inv_clo();
		closelp(lq);
		lq = NULL;

		return sr_cancelled;
	}

	int status = solvelp(lq);

#if defined(_DEBUG) && defined(PRINT_DEBUG)
	printf("loqo result: %d, iter: %d\n", status, lq->iter);
	fflush(stdout);
#endif

	inv_clo();  /* frees memory associated with matrix factorization */

	// retrieve the solution
	for (int i = 0; i < lq->n; i++) {
		w[i] = lq->x[i];
	}

	closelp(lq);	/* frees memory used to store the problem */
	lq = NULL;

	if (status == 0) {
		return sr_success;
	}
	else if (status == 2) {
		return sr_infeasible;
	}
	else {
		return sr_gen_failure;
	}

	// free all the variables
	/*delete [] lq->A;
	delete [] lq->iA;
	delete [] lq->kA;
	
	delete [] lq->Q;
	delete [] lq->iQ;
	delete [] lq->kQ;

	delete [] lq->b;
	delete [] lq->r;
	
	delete [] lq->c;

	delete [] lq->l;
	delete [] lq->u;

	delete [] lq->x;*/
}

double loqo_smoother::objval(double *x) {
	double obj_val;
	inst->sm->eval_obj(x, true, obj_val);
	return obj_val;
}

void loqo_smoother::objgrad(double *c, double *x) {
	inst->sm->eval_grad_f(x, true, c);
}

void loqo_smoother::hessian(double *Q, double *x, double *y) {
	inst->sm->eval_h(x, true, 1, y, Q);
}

void loqo_smoother::conval(double *h, double *x) {
	inst->sm->eval_g(x, true, h);
}

void loqo_smoother::congrad(double *A, double *At, double *x) {
	inst->sm->eval_grad_g(x, true, A);
	atnum(inst->lq->m, inst->lq->n, inst->lq->kA, inst->lq->iA, A, inst->lq->kAt, inst->lq->iAt, At);
}