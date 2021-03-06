#include "../coords/dpvector2.h"
#include "../coords/dpmatrix2.h"

#include <limits>
#include <cmath>
#include <windows.h>

using namespace std;

#ifndef _TIMING_EST_H
#define _TIMING_EST_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

// timing_estimator implements a Kalman filter to track the bias and drift of two clock sources. The
// time frames are referred to as local and remote, but there is no implicit need for the time sources
// to conform to the names--any two time sources can be used.
//
// The general equation governing the system is
// 
//   (1) t_rem = t_loc - (bias + dt*drift)
//
// where
//   dt = t_loc - t_upd
//    t_upd is the local time supplied during the last update
// 
// All conversions follow from this formula. 
//
// The estimates of bias and drift are computed by the update function which takes and local and remote time
// pair. These times should be for the same "instant" for the update to be valid. NOTE: to correct for known 
// lag, i.e. the local time is for 2 ms after the remote time, simply subtract from the local time. 
//
// The filter requires noises of the local clock in terms of a noise intensity on bias (sigma_b) and 
// drift (sigma_d). If the units of the system are in secs, these would have units of s/√s and (s/s)/√s 
// respectively. These certainly can't be measured but they can be played with to get desired results. 
// The filter also requires a measurement noise (sigma_r), which is the standard dev of the noise on the 
// measurements of the remote time, or more intuitively the std dev of the noise on the difference in local
// and remote time in calls to update.
// 
// The filter can also perform hypothesis testing on updates. If the innovation (i.e. deviation from
// what the filter expects the time difference to be and the measured time difference) is larger than 
// would be expected at a 99% confidence level, the measurement will be rejected. This can cause measurements
// to be overly rejected if the noises are tuned too low. 
//
// Remark on filter
// The basis of the estimator is that there is a relatively constant bias and drift. If this condition is not
// met, the filter can have poor performance, such as oscillating around as the bias initially takes up the 
// error with the drift slowly changing (depending on the noise parameters). Since the drift changes slowly,
// it will cause overshooting/undershooting of the estimated offset, hence the oscillation. If a known change
// occurs and the old estimates should be thrown out, call reset() to clear out the current estimate.
// 
// Functions are provided to translate between the two frames.
//   - translate_to_local
//     Converts the absolute time of the remote frame (specified as double) into the local frame
//   - translate_from_local
//		 Converts the absolute time of the local frame into the remote framew
//   - translate_span_to_local
//     Converts the timespan/time difference of a remote frame into the local frame. Only takes into 
//     account the drift rate and not the bias in the computation.
//   - translate_span_from_local
//     Converts the timespan/time difference of a local frame into the remote fame. Again, only the 
//     drift is taken into account.
//
// It is recommended that the times be in seconds, as has_sync() checks for convergence of the filter
// and will check that the standard dev is less than 0.001 of whatever unit. This will result in checking
// if the filter has converged to 1 ms one-sigma if the units are seconds. The filter will likely get boned
// if the two time sources are not close, i.e. the drift is relatively small. For example, if the units of
// the local time are seconds and the remote are milliseconds, then the drift will be in the thousands and 
// things will probably not work out right.
// 
// The class is thread-safe and any method can be called by any thread. update(...) and reset() are blocking
// but all translation functions are non-blocking. 

// internal state structure
#pragma pack(16)
_MM_ALIGN16 struct ts_state {
public:
	dpvector2 xhat;
	dpmatrix2 Phat;
	double t_update;
};
#pragma pack()

class timing_estimator {
private:
	ts_state *state;
	ts_state *cur_state;

	double sigma_b;
	double sigma_d;
	double sigma_r;

	CRITICAL_SECTION update_lock;
	void lock() { EnterCriticalSection(&update_lock); }
	void unlock() { LeaveCriticalSection(&update_lock); }

public:
	// Default values are tuned for the car time server sync pulse. They may need to be adjusted for different sources.
	// In particular, sigma_r (measurement noise) may need to be changed.
	timing_estimator(double sigma_r = 4.0e-5, double sigma_b = 3.33e-6, double sigma_d = 1.67e-6, bool hypo_test = true);
	~timing_estimator();

	// Change at any time to control if the estimator performs hypothesis tests
	bool hypo_test;

	// See comments above
	double translate_to_local(const double ts) const;
	double translate_from_local(const double ts) const;
	double translate_span_to_local(const double dt) const;
	double translate_span_from_local(const double dt) const;

	// Call update when a new timing measurement arrives. Units should be consistent between the two frames, and seconds
	// are the preferred unit. 
	// Returns true if the measurement was applied successfully, false if the measurement was rejected because of 
	// hypothesis testing
	bool update(const double local, const double remote);
	// Same as above, but provides ability to specify measurement noise different from constructor
	bool update(const double local, const double remote, const double sigma_r);

	// Resets the system state
	void reset();

	// Determines if the system is observable and sync std dev of the trace of the covariance matrix is less than a millisecond
	bool has_sync() const;

	// Returns the estimator's bias/drift standard deviation. These values are not really meaningful as the true noises
	// of the system and measurement are unknown. These do provide a relative measure of convergence quality.
	double bias_sigma() const;
	double drift_sigma() const;

	// Returns the estimator's bias/drift estimate. It is recommended to use the translation functions instead of dealing
	// with these values directly.
	double bias() const;
	double drift() const;
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif