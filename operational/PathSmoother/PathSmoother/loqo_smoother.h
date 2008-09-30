#ifndef _LOQO_SMOOTHER_H
#define _LOQO_SMOOTHER_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

#include "smoother.h"

#ifdef __cplusplus
extern "C" {
#endif

#include "loqo.h"

#ifdef __cplusplus
}
#endif

class loqo_smoother : public smoother_engine {
private:
	static loqo_smoother* inst;
	static bool did_set_env;

	smoother* sm;

public:
	loqo_smoother();

	virtual ~loqo_smoother();

	virtual solve_result smooth_iter(double mu0, double* w, bool w0, double* y, bool y0, double* z_l, double* z_u, bool z0);

	virtual void cancel() {  
		if (lq != NULL) {
			lq->itnlim = 1; 
		}
	}

	virtual constr_form get_constr_form() const { return constr_lhs; }
	virtual bool get_ll_hess() const { return false; }

	virtual void set_smoother(smoother* sm) { this->sm = sm; }

private:
	LOQO* lq;

	static void do_set_env();

	static double objval(double *x);
	static void objgrad(double *c, double *x);
	static void hessian(double *Q, double *x, double *y);
	static void conval(double *h, double *x);
	static void congrad(double *A, double *At, double *x);
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif