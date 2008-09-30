#include "PosteriorPoseSolution.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

PosteriorPoseSolution::PosteriorPoseSolution(void)
{
	/*
	Default constructor for posterior pose solution object

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	int i;
	int j;

	//initialize the posterior pose solution lock
	InitializeCriticalSection(&mPPlock);
	EnterCriticalSection(&mPPlock);

	//mark the solution as being invalid
	mIsValid = false;
	//initialize all the variables for the posterior pose solution
	mPPTime = 0.0;
	mEastMMSE = 0.0;
	mNorthMMSE = 0.0;
	mHeadingMMSE = 0.0;
	mGPSBiasEastMMSE = 0.0;
	mGPSBiasNorthMMSE = 0.0;

	for (i = 0; i < PF_NUMSTATES; i++)
	{
		for (j = 0; j < PF_NUMSTATES; j++)
		{
			mCovMMSE[midx(i, j, PF_NUMSTATES)] = 0.0;
		}
	}

	mRoadLocation = NULL;

	LeaveCriticalSection(&mPPlock);

	return;
}

PosteriorPoseSolution::~PosteriorPoseSolution(void)
{
	/*
	Destructor for posterior pose solution object

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	EnterCriticalSection(&mPPlock);

	mIsValid = false;
	//delete the posterior pose solution lock
	DeleteCriticalSection(&mPPlock);

	return;
}

bool PosteriorPoseSolution::IsValid(void)
{
	/*
	Accessor function for mIsValid, the validity variable

	INPUTS:
		none.

	OUTPUTS:
		rIsValid - returns mIsValid.
	*/

	bool rIsValid;

	EnterCriticalSection(&mPPlock);
	rIsValid = mIsValid;
	LeaveCriticalSection(&mPPlock);

	return rIsValid;
}

void PosteriorPoseSolution::SetPosteriorPoseSolution(double iPPTime, double iEastMMSE, double iNorthMMSE, double iHeadingMMSE, 
	double iGPSBiasEastMMSE, double iGPSBiasNorthMMSE, double iCovMMSE[PF_NUMSTATES*PF_NUMSTATES], 
	RoadPartition* iRoadLocation)
{
	/*
	Sets the posterior pose solution with input values.

	INPUTS:
		iPPtime - current posterior pose time
		iEastMMSE, iNorthMMSE, iHeadingMMSE - MMSE position estimate
		iGPSBiasEastMMSE, iGPSBiasNorthMMSE - MMSE gps bias estimate
		iCovMMSE - MMSE covariance (MSE) matrix

	OUTPUTS:
		none.
	*/

	int i;
	int j;

	//acquire the posterior pose lock
	EnterCriticalSection(&mPPlock);

	if (iPPTime > mPPTime || mIsValid == false)
	{
		//copy over all the variables
		mPPTime = iPPTime;
		mEastMMSE = iEastMMSE;
		mNorthMMSE = iNorthMMSE;
		mHeadingMMSE = iHeadingMMSE;
		mGPSBiasEastMMSE = iGPSBiasEastMMSE;
		mGPSBiasNorthMMSE = iGPSBiasNorthMMSE;

		for (i = 0; i < PF_NUMSTATES; i++)
		{
			for (j = 0; j < PF_NUMSTATES; j++)
			{
				mCovMMSE[midx(i, j, PF_NUMSTATES)] = iCovMMSE[midx(i, j, PF_NUMSTATES)];
			}
		}

		mRoadLocation = iRoadLocation;

		//solution becomes valid once information is stored in it
		mIsValid = true;
	}
	else
	{
		printf("Warning: SetPosteriorPoseSolution called with an old solution.\n");
	}

	LeaveCriticalSection(&mPPlock);

	return;
}

bool PosteriorPoseSolution::GetPosteriorPosePosition(double& oEastMMSE, double& oNorthMMSE, double& oHeadingMMSE)
{
	/*
	Retrieves the posterior pose position solution.

	INPUTS:
		oEastMMSE, oNorthMMSE, oHeadingMMSE - will contain the ppose position estimate
			on a successful return.

	OUTPUTS:
		rSuccess - true if successful, false otherwise.  If true, input variables are
			populated with the posterior pose solution.  If false, they are not touched.
	*/

	bool rSuccess = false;

	EnterCriticalSection(&mPPlock);

	if (mIsValid == true)
	{
		//retrieve the ppose position solution
		oEastMMSE = mEastMMSE;
		oNorthMMSE = mNorthMMSE;
		oHeadingMMSE = mHeadingMMSE;

		rSuccess = true;
	}

	LeaveCriticalSection(&mPPlock);

	return rSuccess;
}
