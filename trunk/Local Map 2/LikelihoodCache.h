#ifndef LIKELIHOODCACHE_H
#define LIKELIHOODCACHE_H

#include "acml.h"
#include "LocalMapFunctions.h"
#include "MatrixIndex.h"

#include <FLOAT.H>
#include <MATH.H>
#include <MEMORY.H>
#include <STDIO.H>
#include <STDLIB.H>

#define LC_DEFAULTID -1
#define LC_DEFAULTMM -1
#define LC_DEFAULTNM 0
#define LC_DEFAULTTIME -DBL_MAX

class LikelihoodCache
{
	//A class for caching the output of a call to a likelihood function to 
	//avoid expensive recomputation of the same correlation and covariance
	//matrices repeatedly.

private:

	//MEASUREMENT FLAGS
	//timestamp at which the cache is valid
	double mCacheTime;
	//number of measurements that have been applied to the target so far
	int mNumMeasurements;
	//ID of the sensor that was used to compute the cache data
	int mSensorID;
	//metameasurement type that was computed
	int mMetaMeasurementType;

	//MEASUREMENT CACHE
	//the cached size of the measurement
	int nz;
	//the cached size of the state
	int nx;
	//the cached expected measurement
	double* zbar;
	//the cached cross correlation between the state and measurement (Pxz)
	double* PbarHt;
	//the cached measurement correlation with the state (H*Pbar*H')
	double* HPbarHt;

public:

	LikelihoodCache();
	~LikelihoodCache();

	double FastLikelihood(double* nu, double* S, double* W, double* z, bool* zwrap, double* R, double iChi2Gate = DBL_MAX);
	bool IsCached(double iCurrentTime, int iSensorID, int iMetaMeasurementType, int iNumMeasurements, int inz, int inx);
	void ResetCache();
	void SetCache(double iCacheTime, int iSensorID, int iMetaMeasurementType, int iNumMeasurements, int inz, int inx, 
		double* inu, double* iS, double* iW, double* iz, double* iR);
};

#endif //LIKELIHOODCACHE_H
