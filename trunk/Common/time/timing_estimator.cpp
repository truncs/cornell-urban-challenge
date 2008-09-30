#include "timing_estimator.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

timing_estimator::timing_estimator(double sigma_r, double sigma_b, double sigma_d, bool hypo_test) {
	this->sigma_r = sigma_r;
	this->sigma_b = sigma_b*sigma_b;
	this->sigma_d = sigma_d*sigma_d;
	this->hypo_test = hypo_test;

	// use aligned malloc as the some of the members use SSE ops and need 16-byte alignment
	state = (ts_state*)_aligned_malloc(2*sizeof(ts_state),__alignof(ts_state));
	cur_state = NULL;

	InitializeCriticalSection(&update_lock);
}

timing_estimator::~timing_estimator() {
	_aligned_free(state);
	DeleteCriticalSection(&update_lock);
}

bool timing_estimator::update(const double lt, const double rt) {
	return update(lt, rt, sigma_r);
}

bool timing_estimator::update(const double lt, const double rt, const double sigma_r) {
	// figure out the next state holder we're putting this into
	// do this swapping mechanism so we don't need locks on the translation functions
	lock();
	ts_state *ns, *cs = cur_state;
	if (cs == &state[0]) {
		ns = &state[1];
	}
	else {
		ns = &state[0];
	}

	// calcualte measurement
	double y = lt - rt;

	// check if the filter is currently initialized
	if (cs == NULL) {
		ns->t_update = lt;
		ns->xhat = dpvector2(y, 0);
		// initialize to very high variance values
		ns->Phat = dpmatrix2(10, 0, 0, 10);

		cur_state = ns;
	}
	else {
		// time since last update
		double dt = lt - cs->t_update;
		
		// build discretized state transition matrix
		dpmatrix2 F(1, dt, 0, 1);
		// calculate predicted state
		dpvector2 xbar = F * cs->xhat;

		// build discretized noise-state matrix
		dpmatrix2 Gamma(dt, 0.5*dt*dt, 0, dt);
		// build noise intesity matrix.
		dpmatrix2 Qk(dt*sigma_b + dt*dt*dt*sigma_d/3, dt*dt*sigma_d*0.5, dt*dt*sigma_d*0.5, dt*sigma_d);
		dpmatrix2 Ft = F.transpose();
		dpmatrix2 Gammat = Gamma.transpose();
		// calculate predicted covariance matrix
		dpmatrix2 Pbar = F*(cs->Phat)*Ft + Gamma*Qk*Gammat;

		// expected measurement is the predicted bias
		double ybar = xbar.x();
		double Pyy = Pbar(0,0) + sigma_r*sigma_r; // sigma_r passed in as std dev

		// calculate the measurement innovation
		double innov = y - ybar;

		// do a chi2 test on the innovation (99% confidence level)
		if (hypo_test && innov*innov/Pyy > 6.634896601021234) {
			// don't apply the update, didn't pass hypothesis test
			unlock();
			return false;
		}
		
		// compute new xhat, Phat
		dpvector2 Pxy = Pbar.col(0);
		ns->xhat = xbar + Pxy*((y - ybar)/Pyy);
		ns->Phat = Pbar - (Pxy/Pyy).outerproduct(Pxy);
		ns->t_update = lt;

		// swap in the current state 
		cur_state = ns;
	}

	unlock();
	return true;
}

double timing_estimator::translate_to_local(const double ts) const {
	ts_state* cs = cur_state;
	if (cs == NULL)
		return numeric_limits<double>::quiet_NaN();

	return (ts + cs->xhat.x() - cs->xhat.y()*cs->t_update)/(1 - cs->xhat.y());
}

double timing_estimator::translate_from_local(const double ts) const {
	ts_state* cs = cur_state;
	if (cs == NULL)
		return numeric_limits<double>::quiet_NaN();

	return ts - (cs->xhat.x() + cs->xhat.y()*(ts - cs->t_update));
}

double timing_estimator::translate_span_to_local(const double dt) const {
	ts_state* cs = cur_state;
	if (cs == NULL)
		return numeric_limits<double>::quiet_NaN();

	return dt / (1 - cs->xhat.y());
}

double timing_estimator::translate_span_from_local(const double dt) const {
	ts_state* cs = cur_state;
	if (cs == NULL)
		return numeric_limits<double>::quiet_NaN();

	return dt * (1 - cs->xhat.y());
}

// resets the states
void timing_estimator::reset() {
	cur_state = NULL;
}

// determines if the system is observable and sync std dev is within a millisecond
bool timing_estimator::has_sync() const {
	ts_state* cs = cur_state;
	if (cs == NULL) {
		return false;
	}

	// if the std dev of the trace is over 10 ms, not converged
	if (cs->Phat.trace() > (0.09*0.09)) {
		return false;
	}

	return true;
}

// returns the estimator's bias/drift standard deviation
double timing_estimator::bias_sigma() const {
	ts_state* cs = cur_state;
	if (cs == NULL)
		return numeric_limits<double>::quiet_NaN();

	return sqrt(cs->Phat(0,0));
}

double timing_estimator::drift_sigma() const {
	ts_state* cs = cur_state;
	if (cs == NULL)
		return numeric_limits<double>::quiet_NaN();

	return sqrt(cs->Phat(1,1));
}

double timing_estimator::bias() const {
	ts_state* cs = cur_state;
	if (cs == NULL)
		return numeric_limits<double>::quiet_NaN();

	return cs->xhat.x();
}

double timing_estimator::drift() const {
	ts_state* cs = cur_state;
	if (cs == NULL)
		return numeric_limits<double>::quiet_NaN();

	return cs->xhat.y();
}