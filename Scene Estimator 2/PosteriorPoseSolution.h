#ifndef POSTERIORPOSESOLUTION_H
#define POSTERIORPOSESOLUTION_H

#include "MatrixIndex.h"
#include "PosteriorPoseParticleFilter.h"
#include "RoadPartition.h"

#include <WINDOWS.H>

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

class PosteriorPoseSolution
{
	//The PosteriorPoseSolution class.  Used to store the posterior pose solution
	//in a thread-safe manner so that anybody can access it appropriately.

private:

	//whether the posterior pose solution is valid
	bool mIsValid;

	//the posterior pose solution (MMSE)
	double mPPTime;
	double mEastMMSE;
	double mNorthMMSE;
	double mHeadingMMSE;
	double mGPSBiasEastMMSE;
	double mGPSBiasNorthMMSE;

	//the ppose MMSE covariance matrix (order is the same as the states listed above)
	double mCovMMSE[PF_NUMSTATES*PF_NUMSTATES];
	//closest road partition based on MMSE estimate
	RoadPartition* mRoadLocation;

	//the critical section used to lock the posterior pose solution safely
	CRITICAL_SECTION mPPlock;

public:

	PosteriorPoseSolution();
	~PosteriorPoseSolution();

	bool GetPosteriorPosePosition(double& oEastMMSE, double& oNorthMMSE, double& oHeadingMMSE);
	bool IsValid();
	void SetPosteriorPoseSolution(double iPPTime, double iEastMMSE, double iNorthMMSE, double iHeadingMMSE, 
		double iGPSBiasEastMMSE, double iGPSBiasNorthMMSE, double iCovMMSE[PF_NUMSTATES*PF_NUMSTATES], 
		RoadPartition* iRoadLocation);
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //POSTERIORPOSESOLUTION_H
