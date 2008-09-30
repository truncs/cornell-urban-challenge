#include "PosteriorPoseParticleFilter.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

PosteriorPoseParticleFilter::PosteriorPoseParticleFilter(int iNumParticles, RandomNumberGenerator* iRandomNumberGenerator)
{
	/*
	Constructor for PPose particle filter.  Creates a filter and initializes its
	random number generator either with the supplied seed file, or with a random
	seed file.

	INPUTS:
		iNumParticles - number of particles to use in the filter
		iRandomNumberGenerator - pointer to the random number generator (already
			initialized) that will be used to draw random values slowly.

	OUTPUTS:
		none.
	*/

	int i;
	int j;

	//declare space for the particles
	mNumParticles = iNumParticles;
	if (mNumParticles < 1)
	{
		mNumParticles = 1;
	}
	mParticles = new Particle[mNumParticles];
	mAIParticles = new Particle[mNumParticles];

	//store the random number generator
	mPPGenerator = iRandomNumberGenerator;
	//reserve a set of uniform random numbers
	while (mUniformCache.AddNumber(mPPGenerator->RandUniform()) == false)
	{
		//don't do anything!
	}
	//reserve a set of gaussian random numbers
	while (mGaussianCache.AddNumber(mPPGenerator->RandGaussian()) == false)
	{
		//don't do anything!
	}

	//initialize the MMSE estimate
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

	//initialize the road graph to null
	mRoadGraph = NULL;
	//initialize the road location to null
	mRoadLocation = NULL;

	//initialize local road variables
	mLRIsInitialized = false;
	mLRModelProb = 0.0;
	mLRLeftLaneProb = 0.0;
	mLRLeftLaneWidth = 0.0;
	mLRLeftLaneWidthVar = 0.0;
	mLRCenterLaneProb = 0.0;
	mLRCenterLaneWidth = 0.0;
	mLRCenterLaneWidthVar = 0.0;
	mLRRightLaneProb = 0.0;
	mLRRightLaneWidth = 0.0;
	mLRRightLaneWidthVar = 0.0;

	mLRNumLeftLanePoints = 0;
	mLRLeftLanePoints = NULL;
	mLRLeftLaneVars = NULL;

	mLRNumCenterLanePoints = 0;
	mLRCenterLanePoints = NULL;
	mLRCenterLaneVars = NULL;

	mLRNumRightLanePoints = 0;
	mLRRightLanePoints = NULL;
	mLRRightLaneVars = NULL;

	//initialize local road transmit variables
	mAILRModelProb = 0.0;
	mAILRLeftLaneProb = 0.0;
	mAILRLeftLaneWidth = 0.0;
	mAILRLeftLaneWidthVar = 0.0;
	mAILRCenterLaneProb = 0.0;
	mAILRCenterLaneWidth = 0.0;
	mAILRCenterLaneWidthVar = 0.0;
	mAILRRightLaneProb = 0.0;
	mAILRRightLaneWidth = 0.0;
	mAILRRightLaneWidthVar = 0.0;

	mAILRNumLeftLanePoints = 0;
	mAILRLeftLanePoints = NULL;
	mAILRLeftLaneVars = NULL;

	mAILRNumCenterLanePoints = 0;
	mAILRCenterLanePoints = NULL;
	mAILRCenterLaneVars = NULL;

	mAILRNumRightLanePoints = 0;
	mAILRRightLanePoints = NULL;
	mAILRRightLaneVars = NULL;

	//note: the PF isn't initialized until it gets a first measurement
	mPFTime = -DBL_MAX;
	mGPSMode = POSE_GPSSPS;
	mIsInitialized = false;

	return;
}

PosteriorPoseParticleFilter::~PosteriorPoseParticleFilter(void)
{
	/*
	Particle filter destructor.  Frees memory allocated to the PF.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	mIsInitialized = false;
	mNumParticles = 0;

	//delete memory allocated in particles
	delete [] mParticles;
	delete [] mAIParticles;

	//delete memory allocated in the local road
	delete [] mLRLeftLanePoints;
	delete [] mLRLeftLaneVars;
	delete [] mLRCenterLanePoints;
	delete [] mLRCenterLaneVars;
	delete [] mLRRightLanePoints;
	delete [] mLRRightLaneVars;

	//delete memory allocated in transmit variables
	delete [] mAILRLeftLanePoints;
	delete [] mAILRLeftLaneVars;
	delete [] mAILRCenterLanePoints;
	delete [] mAILRCenterLaneVars;
	delete [] mAILRRightLanePoints;
	delete [] mAILRRightLaneVars;

	return;
}

void PosteriorPoseParticleFilter::ResetFilter()
{
	/*
	Completely resets the particle filter and returns it to a state where it
	is ready to accept new measurements for reinitialization.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	int i;
	int j;

	//initialize the MMSE estimate
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

	//initialize the road graph to null
	mRoadGraph = NULL;
	//initialize the road location to null
	mRoadLocation = NULL;

	//initialize local road variables
	mLRIsInitialized = false;
	mLRModelProb = 0.0;
	mLRLeftLaneProb = 0.0;
	mLRLeftLaneWidth = 0.0;
	mLRLeftLaneWidthVar = 0.0;
	mLRCenterLaneProb = 0.0;
	mLRCenterLaneWidth = 0.0;
	mLRCenterLaneWidthVar = 0.0;
	mLRRightLaneProb = 0.0;
	mLRRightLaneWidth = 0.0;
	mLRRightLaneWidthVar = 0.0;

	mLRNumLeftLanePoints = 0;
	delete [] mLRLeftLanePoints;
	mLRLeftLanePoints = NULL;
	delete [] mLRLeftLaneVars;
	mLRLeftLaneVars = NULL;

	mLRNumCenterLanePoints = 0;
	delete [] mLRCenterLanePoints;
	mLRCenterLanePoints = NULL;
	delete [] mLRCenterLaneVars;
	mLRCenterLaneVars = NULL;

	mLRNumRightLanePoints = 0;
	delete [] mLRRightLanePoints;
	mLRRightLanePoints = NULL;
	delete [] mLRRightLaneVars;
	mLRRightLaneVars = NULL;

	//initialize local road transmit variables
	mAILRModelProb = 0.0;
	mAILRLeftLaneProb = 0.0;
	mAILRLeftLaneWidth = 0.0;
	mAILRLeftLaneWidthVar = 0.0;
	mAILRCenterLaneProb = 0.0;
	mAILRCenterLaneWidth = 0.0;
	mAILRCenterLaneWidthVar = 0.0;
	mAILRRightLaneProb = 0.0;
	mAILRRightLaneWidth = 0.0;
	mAILRRightLaneWidthVar = 0.0;

	mAILRNumLeftLanePoints = 0;
	delete [] mAILRLeftLanePoints;
	mAILRLeftLanePoints = NULL;
	delete [] mAILRLeftLaneVars;
	mAILRLeftLaneVars = NULL;

	mAILRNumCenterLanePoints = 0;
	delete [] mAILRCenterLanePoints;
	mAILRCenterLanePoints = NULL;
	delete [] mAILRCenterLaneVars;
	mAILRCenterLaneVars = NULL;

	mAILRNumRightLanePoints = 0;
	delete [] mAILRRightLanePoints;
	mAILRRightLanePoints = NULL;
	delete [] mAILRRightLaneVars;
	mAILRRightLaneVars = NULL;

	//note: the PF isn't initialized until it gets a first measurement
	mPFTime = -DBL_MAX;
	mGPSMode = POSE_GPSSPS;
	mIsInitialized = false;

	return;
}

void PosteriorPoseParticleFilter::ComputeBestEstimate(void)
{
	/*
	Computes the filter's aggregate estimates at the present time.  Should be
	rerun each time the particles are changed.

	INPUTS:
		none.

	OUTPUTS:
		none.  Sets the filter's "best" estimate variables
	*/

	int i;
	int j;
	int k;
	int np = mNumParticles;
	double wt;

	mEastMMSE = 0.0;
	mNorthMMSE = 0.0;
	mHeadingMMSE = 0.0;
	mGPSBiasEastMMSE = 0.0;
	mGPSBiasNorthMMSE = 0.0;

	double ta = mParticles[0].Heading();
	for (i = 0; i < np; i++)
	{
		//calculate the MMSE weighted average of the particles
		wt = mParticles[i].Weight();
		mEastMMSE += wt * mParticles[i].East();
		mNorthMMSE += wt * mParticles[i].North();
		mHeadingMMSE += wt * WrapAngle(mParticles[i].Heading(), ta);
		mGPSBiasEastMMSE += wt * mParticles[i].GPSBiasEast();
		mGPSBiasNorthMMSE += wt * mParticles[i].GPSBiasNorth();
	}
	mHeadingMMSE = UnwrapAngle(mHeadingMMSE);

	//calculate the mean square error matrix for the particles
	for (i = 0; i < PF_NUMSTATES; i++)
	{
		for (j = 0; j < PF_NUMSTATES; j++)
		{
			mCovMMSE[midx(i, j, PF_NUMSTATES)] = 0.0;
		}
	}

	double dvec[PF_NUMSTATES];
	for (i = 0; i < PF_NUMSTATES; i++)
	{
		dvec[i] = 0.0;
	}
	for (i = 0; i < np; i++)
	{
		wt = mParticles[i].Weight();
		dvec[0] = mParticles[i].East() - mEastMMSE;
		dvec[1] = mParticles[i].North() - mNorthMMSE;
		dvec[2] = UnwrapAngle(mParticles[i].Heading() - mHeadingMMSE);
		dvec[3] = mParticles[i].GPSBiasEast() - mGPSBiasEastMMSE;
		dvec[4] = mParticles[i].GPSBiasNorth() - mGPSBiasNorthMMSE;

		for (j = 0; j < PF_NUMSTATES; j++)
		{
			for (k = j; k < PF_NUMSTATES; k++)
			{
				mCovMMSE[midx(j, k, PF_NUMSTATES)] += wt*dvec[j]*dvec[k];
			}
		}
	}
	for (j = 0; j < PF_NUMSTATES; j++)
	{
		for (k = 0; k < j; k++)
		{
			mCovMMSE[midx(j, k, PF_NUMSTATES)] = mCovMMSE[midx(k, j, PF_NUMSTATES)];
		}
	}

	if (mRoadGraph != NULL)
	{
		//compute best location on the RNDF
		mRoadLocation = mRoadGraph->ClosestPartition(mEastMMSE, mNorthMMSE, mRoadLocation, PP_OFFRNDFDIST);
	}
	else
	{
		//no road graph
		mRoadLocation = NULL;
	}

	return;
}

void PosteriorPoseParticleFilter::ComputeBestEstimateForTransmit(void)
{
	/*
	Computes the filter's final estimate to be sent to the planner.  Should
	be rerun each time the particle filter's estimate is needed.

	INPUTS:
		none.

	OUTPUTS:
		none.  Sets the filter's best estimate variables
	*/

	int i;
	int j;
	int k;
	int np = mNumParticles;
	double wt;

	mAIEast = 0.0;
	mAINorth = 0.0;
	mAIHeading = 0.0;
	mAIGPSBiasEast = 0.0;
	mAIGPSBiasNorth = 0.0;

	double ta = mAIParticles[0].Heading();
	for (i = 0; i < np; i++)
	{
		//calculate the MMSE weighted average of the particles
		wt = mAIParticles[i].Weight();
		mAIEast += wt * mAIParticles[i].East();
		mAINorth += wt * mAIParticles[i].North();
		mAIHeading += wt * WrapAngle(mAIParticles[i].Heading(), ta);
		mAIGPSBiasEast += wt * mAIParticles[i].GPSBiasEast();
		mAIGPSBiasNorth += wt * mAIParticles[i].GPSBiasNorth();
	}
	mAIHeading = UnwrapAngle(mAIHeading);

	//calculate the mean square error matrix for the particles
	for (i = 0; i < PF_NUMSTATES; i++)
	{
		for (j = 0; j < PF_NUMSTATES; j++)
		{
			mAICovariance[midx(i, j, PF_NUMSTATES)] = 0.0;
		}
	}

	double dvec[PF_NUMSTATES];
	for (i = 0; i < PF_NUMSTATES; i++)
	{
		dvec[i] = 0.0;
	}
	for (i = 0; i < np; i++)
	{
		wt = mAIParticles[i].Weight();
		dvec[0] = mParticles[i].East() - mEastMMSE;
		dvec[1] = mParticles[i].North() - mNorthMMSE;
		dvec[2] = UnwrapAngle(mParticles[i].Heading() - mHeadingMMSE);
		dvec[3] = mParticles[i].GPSBiasEast() - mGPSBiasEastMMSE;
		dvec[4] = mParticles[i].GPSBiasNorth() - mGPSBiasNorthMMSE;

		for (j = 0; j < PF_NUMSTATES; j++)
		{
			for (k = j; k < PF_NUMSTATES; k++)
			{
				mAICovariance[midx(j, k, PF_NUMSTATES)] += wt*dvec[j]*dvec[k];
			}
		}
	}
	for (j = 0; j < PF_NUMSTATES; j++)
	{
		for (k = 0; k < j; k++)
		{
			mAICovariance[midx(j, k, PF_NUMSTATES)] = mAICovariance[midx(k, j, PF_NUMSTATES)];
		}
	}

	if (mRoadGraph != NULL)
	{
		//compute best location on the RNDF
		mAIRoadLocation = mRoadGraph->ClosestPartition(mAIEast, mAINorth, mRoadLocation, PP_OFFRNDFDIST);
	}
	else
	{
		//no road graph
		mAIRoadLocation = NULL;
	}

	return;
}

void PosteriorPoseParticleFilter::Initialize(double iInitializeTime, double* iPoseBuff, RoadGraph* iRoadGraph)
{
	/*
	Initializes the particle filter with a valid pose measurement.  NOTE: can be used to
	reinitialize the particle filter.

	INPUTS:
		iPoseBuff - a single absolute pose measurement to use for initialization
		iRoadGraph - the road graph used for monitoring RNDF position

	OUTPUTS:
		none.
	*/

	if (mIsInitialized == false)
	{
		printf("Initializing PosteriorPose particle filter...\n");
	}
	else
	{
		#ifdef SE_COMMONDEBUGMSGS
			printf("Reinitializing PosteriorPose particle filter...\n");
		#endif
	}

	int i;
	int j;
	int k;
	int idx;

	//first set GPS mode based on the packet's quality of service
	int pmode = (int) iPoseBuff[1];
	switch (pmode)
	{
	case 0:
		mGPSMode = POSE_GPSNONE;
		break;
	case 1:
		mGPSMode = POSE_GPSSPS;
		break;
	case 2:
		mGPSMode = POSE_GPSWAAS;
		break;
	case 3:
		mGPSMode = POSE_GPSVBS;
		break;
	case 4:
		mGPSMode = POSE_GPSHP;
		break;
	}

	//extract initializing pose from posebuff
	double yaw = iPoseBuff[2];
	double pECEF[3];
	pECEF[0] = iPoseBuff[5];
	pECEF[1] = iPoseBuff[6];
	pECEF[2] = iPoseBuff[7];

	//extract variances and covariances
	double Pyaw = iPoseBuff[11];
	double Syaw = sqrt(Pyaw);
	double Ppos[9];
	//populate the lower triangle here, then the upper triangle after
	idx = 17;
	for (i = 0; i < 3; i++)
	{
		for (j = 0; j <= i; j++)
		{
			Ppos[midx(i, j, 3)] = iPoseBuff[idx];
			idx++;
		}
	}
	//fill in the upper triangle
	for (i = 0; i < 3; i++)
	{
		for (j = i+1; j < 3; j++)
		{
			Ppos[midx(i, j, 3)] = Ppos[midx(j, i, 3)];
		}
	}

	//convert intial pose to ENU in the RNDF
	double pENU[3];
	EnuEcefTransform(pENU, pECEF, false, iRoadGraph->LatOrigin(), iRoadGraph->LonOrigin(), 0.0, true);

	//convert pose covariance to ENU
	double Recef2enu[9];
	GetRecef2enu(Recef2enu, iRoadGraph->LatOrigin(), iRoadGraph->LonOrigin());
	double Recef2enuPpos[9];
	double PposENU[9];
	//Ppos = Recef2enu*Ppos*(Recef2enu');
	for (i = 0; i < 3; i++)
	{
		for (j = 0; j < 3; j++)
		{
			Recef2enuPpos[midx(i, j, 3)] = 0.0;
			for (k = 0; k < 3; k++)
			{
				Recef2enuPpos[midx(i, j, 3)] += Recef2enu[midx(i, k, 3)] * Ppos[midx(k, j, 3)];
			}
		}
	}
	for (i = 0; i < 3; i++)
	{
		for (j = 0; j < 3; j++)
		{
			PposENU[midx(i, j, 3)] = 0.0;
			for (k = 0; k < 3; k++)
			{
				PposENU[midx(i, j, 3)] += Recef2enuPpos[midx(i, k, 3)] * Recef2enu[midx(j, k, 3)];
			}
		}
	}
	//extract east-north covariance matrix
	double PposEN[4];
	PposEN[midx(0, 0, 2)] = PposENU[midx(0, 0, 3)];
	PposEN[midx(0, 1, 2)] = PposENU[midx(0, 1, 3)];
	PposEN[midx(1, 0, 2)] = PposENU[midx(1, 0, 3)];
	PposEN[midx(1, 1, 2)] = PposENU[midx(1, 1, 3)];

	//cholesky factorize PposEN
	double SposEN[4];
	SposEN[midx(0, 0, 2)] = PposEN[midx(0, 0, 2)];
	SposEN[midx(0, 1, 2)] = PposEN[midx(0, 1, 2)];
	SposEN[midx(1, 0, 2)] = PposEN[midx(1, 0, 2)];
	SposEN[midx(1, 1, 2)] = PposEN[midx(1, 1, 2)];
	int info;
	//Cholesky factorize SposEN = chol(PposEN)
	dpotrf('U', 2, SposEN, 2, &info);
	if (info != 0)
	{
		printf("Warning: dpotrf error in PosteriorPoseParticleFilter::Initialize.\n");
		return;
	}
	//also clear out the lower triangle, which should be zeros anyway
	SposEN[midx(1, 0, 2)] = 0.0;

	//standard dev of the GPS bias process (depends on what mode of service GPS has)
	double Sbias;
	switch (mGPSMode)
	{
	case POSE_GPSNONE:
		Sbias = 0.0;
		break;
	case POSE_GPSSPS:
		Sbias = sqrt(GPSBIASSPS_VARIANCE);
		break;
	case POSE_GPSWAAS:
		Sbias = sqrt(GPSBIASWAAS_VARIANCE);
		break;
	case POSE_GPSVBS:
		Sbias = sqrt(GPSBIASVBS_VARIANCE);
		break;
	case POSE_GPSHP:
		Sbias = sqrt(GPSBIASHP_VARIANCE);
		break;
	}

	//ready to begin initialization

	int np = mNumParticles;
	double iwt = 1.0 / np;
	double iEN[2];
	double iH;
	double iBe;
	double iBn;
	double rval[2];

	for (i = 0; i < np; i++)
	{
		//initialize the pose in each particle

		iEN[0] = pENU[0];
		iEN[1] = pENU[1];
		iH = yaw;
		iBe = 0.0;
		iBn = 0.0;

		//draw east-north from the rotated Gaussian distribution
		rval[0] = RandGaussian();
		rval[1] = RandGaussian();

		for (j = 0; j < 2; j++)
		{
			for (k = 0; k < 2; k++)
			{
				iEN[j] += SposEN[midx(k, j, 2)] * rval[k];
			}
		}

		//draw heading from the univariate Gaussian distribution
		iH += Syaw * RandGaussian();

		//draw GPS biases from univariate Gaussian distribution
		#ifdef PP_GPSBIASES
			iBe += Sbias * RandGaussian();
			iBn += Sbias * RandGaussian();
		#endif

		//store the particle's initial state
		mParticles[i].SetWeight(iwt);
		mParticles[i].SetState(iEN[0], iEN[1], iH, iBe, iBn);
	}

	//erase the road location (will be computed again in ComputeEstimate)
	mRoadLocation = NULL;
	//store the road graph for the filter
	mRoadGraph = iRoadGraph;

	//store the timestamp on the particles
	mPFTime = iPoseBuff[0];

	//replace a gaussian random number in the reserve
	ReplaceGaussian();

	//calculate the MMSE estimates for the filter
	ComputeBestEstimate();

	if (mIsInitialized == false)
	{
		mIsInitialized = true;
		printf("PosteriorPose particle filter initialized.\n");
	}
	else
	{
		#ifdef SE_COMMONDEBUGMSGS
			printf("PosteriorPose particle filter reinitialized.\n");
		#endif
	}

	return;
}

void PosteriorPoseParticleFilter::Resample(void)
{
	/*
	Determines whether the particles should be resampled or not, and 
	performs resampling if necessary.

	INPUTS:
		none.

	OUTPUTS:
		none.  Resamples the particles if necessary.
	*/

	int i;
	int j;

	if (mIsInitialized == false)
	{
		//do not resample until the filter is initialized
		return;
	}

	//calculate the effective number of particles to decide if resampling is necessary
	double npeff = 0.0;
	double w;
	for (i = 0; i < mNumParticles; i++)
	{
		//compute sum of squares of weights
		w = mParticles[i].Weight();
		npeff += (w * w);
	}
	//invert to find effective number of particles
	npeff = 1.0 / npeff;

	//test to see if resampling is necessary
	if (npeff < PP_NPEFFRESAMPLE)
	{
		//resampling is necessary

		//store a pointer to the old particle array
		Particle* OldParticles = mParticles;
		//create space for the new particles
		mParticles = new Particle[mNumParticles];
		//calculate the new weight for all resampled particles
		w = 1.0 / ((double) mNumParticles);
		
		//sample from the existing particles
		for (i = 0; i < mNumParticles; i++)
		{
			double csum = 0.0;
			double rval = RandUniform();

			for (j = 0; j < mNumParticles; j++)
			{
				csum += OldParticles[j].Weight();
				if (rval < csum || j == mNumParticles - 1)
				{
					//the jth particle is chosen to copy as the new ith particle
					mParticles[i].CopyParticle(&(OldParticles[j]));
					//reset the particle's weight to uniform
					mParticles[i].SetWeight(w);
					break;
				}
			}
		}

		//calculate the MMSE estimates for the filter (may have changed slightly)
		ComputeBestEstimate();

		//replenish a uniform number in the cache
		ReplaceUniform();

		//delete all memory associated with the old particles
		delete [] OldParticles;
	}

	return;
}

void PosteriorPoseParticleFilter::Predict(double iPredictTime, VehicleOdometry* iVehicleOdometry)
{
	/*
	Predicts the main particles in the filter forward by one specified step.

	INPUTS:
		iPredictTime - final timestamp after the prediction (used for 
			calculating prediction interval)
		iVehicleOdometry - vehicle odometry information for the prediction.

	OUTPUTS:
		none.
	*/

	if (iVehicleOdometry->IsValid == false)
	{
		//do not predict if odometry is invalid
		return;
	}

	if (mIsInitialized == false)
	{
		//can't update until ppose is initialized
		return;
	}

	//extract relevant odometry information
	double dt = iPredictTime - mPFTime;
	if (dt <= 0.0)
	{
		//do not predict particles backwards
		return;
	}

	//convert odometry and noises to discrete time deltas and white noise
	double dx = iVehicleOdometry->vx * dt;
	double dy = iVehicleOdometry->vy * dt;
	double dh = iVehicleOdometry->wz * dt;

	double Qxy[4];
	Qxy[midx(0, 0, 2)] = iVehicleOdometry->qvx * dt;
	Qxy[midx(0, 1, 2)] = 0.0;
	Qxy[midx(1, 0, 2)] = 0.0;
	Qxy[midx(1, 1, 2)] = iVehicleOdometry->qvy * dt;
	double Sh = sqrt(iVehicleOdometry->qwz * dt);

	//convert gps bias process noises to discrete time
	//depending on the level of service available
	double Sbias;
	double blambda;
	switch (mGPSMode)
	{
	case POSE_GPSNONE:
		blambda = 0.0;
		Sbias = 0.0;
		break;
	case POSE_GPSSPS:
		blambda = exp(-dt/GPSBIASSPS_CORRELATIONTIME);
		Sbias = sqrt((1.0 - blambda*blambda) * GPSBIASSPS_VARIANCE);
		break;
	case POSE_GPSWAAS:
		blambda = exp(-dt/GPSBIASWAAS_CORRELATIONTIME);
		Sbias = sqrt((1.0 - blambda*blambda) * GPSBIASWAAS_VARIANCE);
		break;
	case POSE_GPSVBS:
		blambda = exp(-dt/GPSBIASVBS_CORRELATIONTIME);
		Sbias = sqrt((1.0 - blambda*blambda) * GPSBIASVBS_VARIANCE);
		break;
	case POSE_GPSHP:
		blambda = exp(-dt/GPSBIASHP_CORRELATIONTIME);
		Sbias = sqrt((1.0 - blambda*blambda) * GPSBIASHP_VARIANCE);
		break;
	}

	int h;
	int i;
	int j;
	int k;

	//variables needed for coordinate transformations
	double oe;
	double on;
	double oh;
	double obe;
	double obn;
	double cosoh;
	double sinoh;
	double Rv2e[4];
	double de;
	double dn;
	double Rv2eQxy[4];
	double Qv[4];
	double Sv[4];
	double rval[2];

	//***BEGIN PREDICTION***

	double cosmh = cos(mHeadingMMSE);
	double sinmh = sin(mHeadingMMSE);

	//1. predict pose forward
	for (i = 0; i < mNumParticles; i++)
	{
		//extract the particle's old pose
		oe = mParticles[i].East();
		on = mParticles[i].North();
		oh = mParticles[i].Heading();
		obe = mParticles[i].GPSBiasEast();
		obn = mParticles[i].GPSBiasNorth();

		//compute a taylor approximation to sin and cos for speed
		double dhp = UnwrapAngle(oh - mHeadingMMSE);
		double dhp2 = dhp*dhp;
		cosoh = cosmh - sinmh*dhp - 0.5*cosmh*dhp2;
		sinoh = sinmh + cosmh*dhp - 0.5*sinmh*dhp2;
		//cosoh = cos(oh);
		//sinoh = sin(oh);

		//rotate delta-positions into EN (RNDF coordinates) using the particle's heading
		Rv2e[midx(0, 0, 2)] = cosoh;
		Rv2e[midx(0, 1, 2)] = -sinoh;
		Rv2e[midx(1, 0, 2)] = sinoh;
		Rv2e[midx(1, 1, 2)] = cosoh;

		//calculate delta position
		de = Rv2e[midx(0, 0, 2)]*dx + Rv2e[midx(0, 1, 2)]*dy;
		dn = Rv2e[midx(1, 0, 2)]*dx + Rv2e[midx(1, 1, 2)]*dy;

		//Qv = Rv2e*Qxy*Rv2e'
		for (j = 0; j < 2; j++)
		{
			for (k = 0; k < 2; k++)
			{
				Rv2eQxy[midx(j, k, 2)] = 0.0;
				for (h = 0; h < 2; h++)
				{
					Rv2eQxy[midx(j, k, 2)] += Rv2e[midx(j, h, 2)] * Qxy[midx(h, k, 2)];
				}
			}
		}
		for (j = 0; j < 2; j++)
		{
			for (k = 0; k < 2; k++)
			{
				Qv[midx(j, k, 2)] = 0.0;
				for (h = 0; h < 2; h++)
				{
					Qv[midx(j, k, 2)] += Rv2eQxy[midx(j, h, 2)] * Rv2e[midx(k, h, 2)];
				}
			}
		}

		//Cholesky factorize Qv and store in Sv... do the chol by hand for efficiency.
		double a = sqrt(Qv[midx(0, 0, 2)]);
		if (fabs(a) == 0.0)
		{
			//in this case, best just to continue on the particle (it'll be resampled later)
			continue;
		}
		double b = Qv[midx(0, 1, 2)] / a;
		double temp = Qv[midx(1, 1, 2)] - b*b;
		if (temp < 0.0)
		{
			//in this case, best just to continue on the particle (it'll be resampled later)
			continue;
		}
		double c = sqrt(temp);
		Sv[midx(0, 0, 2)] = a;
		Sv[midx(0, 1, 2)] = b;
		Sv[midx(1, 0, 2)] = 0.0;
		Sv[midx(1, 1, 2)] = c;

		//predict pose forward assuming Gaussian process noise
		rval[0] = RandGaussian();
		rval[1] = RandGaussian();

		oe = oe + de + rval[0]*Sv[midx(0, 0, 2)] + rval[1]*Sv[midx(1, 0, 2)];
		on = on + dn + rval[0]*Sv[midx(0, 1, 2)] + rval[1]*Sv[midx(1, 1, 2)];
		oh = UnwrapAngle(oh + dh + Sh*RandGaussian());
		#ifdef PP_GPSBIASES
			obe = blambda*obe + Sbias*RandGaussian();
			obn = blambda*obn + Sbias*RandGaussian();
		#endif

		//store the particle's updated state
		mParticles[i].SetState(oe, on, oh, obe, obn);
	}

	//predict the LocalRoad model too, if present
	PredictLocalRoad(iPredictTime, iVehicleOdometry);

	//***END PREDICTION***

	//store the timestamp on the particles
	mPFTime = iPredictTime;

	//replace a gaussian random number in the reserve
	ReplaceGaussian();

	//calculate the MMSE estimates for the filter
	ComputeBestEstimate();

	return;
}

void PosteriorPoseParticleFilter::PredictLocalRoad(double iPredictTime, VehicleOdometry* iVehicleOdometry)
{
	/*
	Predicts the LocalRoad model forward.

	INPUTS:
		iPredictTime - time to which the local road model is predicted
		iVehicleOdometry - vehicle odometry structure, valid during the prediction

	OUTPUTS:
		none.  Predicts the mLR* variables to the desired time.
	*/

	if (mLRIsInitialized == false)
	{
		//can't predict anything if the local road model isn't initialized
		return;
	}

	if (iVehicleOdometry->IsValid == false)
	{
		//can't predict if odometry is invalid
		return;
	}

	double dt = iPredictTime - mLRTime;
	if (dt <= 0.0)
	{
		//don't predict backwards
		return;
	}

	if (mLRNumLeftLanePoints > 0)
	{
		//predict left lane points forward
		PredictPoints(dt, mLRNumLeftLanePoints, mLRLeftLanePoints, iVehicleOdometry);
	}
	if (mLRNumCenterLanePoints > 0)
	{
		//predict center lane points forward
		PredictPoints(dt, mLRNumCenterLanePoints, mLRCenterLanePoints, iVehicleOdometry);
	}
	if (mLRNumRightLanePoints > 0)
	{
		//predict right lane points forward
		PredictPoints(dt, mLRNumRightLanePoints, mLRRightLanePoints, iVehicleOdometry);
	}

	mLRTime = iPredictTime;

	return;
}

bool PosteriorPoseParticleFilter::PredictForTransmit(double iPredictTime, VehicleOdometry* iVehicleOdometry)
{
	/*
	Predicts the auxiliary particles forward to a desired time for sending the particle
	filter solution.  Makes a copy of the existing particles and stores them as the
	auxiliary particle set before transmitting.

	INPUTS:
		iPredictTime - timestamp at the end of the prediction (for calculating dt)
		iVehicleOdometry - vehicle odometry structure that gives the relevant vehicle
			motion during the time of the prediction.

	OUTPUTS:
		rSuccess - true if the prediction was successful, false otherwise.
			If true, variables are set for transmission.
	*/

	bool rSuccess = false;

	int h;
	int i;
	int j;
	int k;

	//initialize the arbiter message to garbage
	mAITime = 0.0;
	mAIEast = 0.0;
	mAINorth = 0.0;
	mAIHeading = 0.0;
	mAIGPSBiasEast = 0.0;
	mAIGPSBiasNorth = 0.0;
	for (i = 0; i < PF_NUMSTATES; i++)
	{
		for (j = 0; j < PF_NUMSTATES; j++)
		{
			mAICovariance[midx(i, j, PF_NUMSTATES)] = 0.0;
		}
	}
	mAIRoadLocation = NULL;

	if (mIsInitialized == false)
	{
		//do not predict until filter is initialized
		return rSuccess;
	}

	if (iVehicleOdometry->IsValid == false)
	{
		//do not predict if odometry is invalid
		return rSuccess;
	}

	//extract relevant odometry information, predicting from the particle filter time
	double dt = iPredictTime - mPFTime;
	if (dt < 0.0)
	{
		//do not predict particles backwards
		return rSuccess;
	}

	//copy particles into auxiliary set for temporary prediction
	for (i = 0; i < mNumParticles; i++)
	{
		mAIParticles[i].CopyParticle(&(mParticles[i]));
	}

	if (dt > 0.0)
	{
		//convert odometry and noises to discrete time deltas and white noise
		double dx = iVehicleOdometry->vx * dt;
		double dy = iVehicleOdometry->vy * dt;
		double dh = iVehicleOdometry->wz * dt;

		double Qxy[4];
		Qxy[midx(0, 0, 2)] = iVehicleOdometry->qvx * dt;
		Qxy[midx(0, 1, 2)] = 0.0;
		Qxy[midx(1, 0, 2)] = 0.0;
		Qxy[midx(1, 1, 2)] = iVehicleOdometry->qvy * dt;
		double Sh = sqrt(iVehicleOdometry->qwz * dt);

		//convert gps bias process noises to discrete time
		//depending on the level of service available
		double Sbias;
		double blambda;
		switch (mGPSMode)
		{
		case POSE_GPSNONE:
			blambda = 0.0;
			Sbias = 0.0;
			break;
		case POSE_GPSSPS:
			blambda = exp(-dt/GPSBIASSPS_CORRELATIONTIME);
			Sbias = sqrt((1.0 - blambda*blambda) * GPSBIASSPS_VARIANCE);
			break;
		case POSE_GPSWAAS:
			blambda = exp(-dt/GPSBIASWAAS_CORRELATIONTIME);
			Sbias = sqrt((1.0 - blambda*blambda) * GPSBIASWAAS_VARIANCE);
			break;
		case POSE_GPSVBS:
			blambda = exp(-dt/GPSBIASVBS_CORRELATIONTIME);
			Sbias = sqrt((1.0 - blambda*blambda) * GPSBIASVBS_VARIANCE);
			break;
		case POSE_GPSHP:
			blambda = exp(-dt/GPSBIASHP_CORRELATIONTIME);
			Sbias = sqrt((1.0 - blambda*blambda) * GPSBIASHP_VARIANCE);
			break;
		}

		//variables needed for coordinate transformations
		double oe;
		double on;
		double oh;
		double obe;
		double obn;
		double cosoh;
		double sinoh;
		double Rv2e[4];
		double de;
		double dn;
		double Rv2eQxy[4];
		double Qv[4];
		double Sv[4];
		double rval[2];

		//***BEGIN PREDICTION***

		double cosmh = cos(mHeadingMMSE);
		double sinmh = sin(mHeadingMMSE);

		//1. predict pose forward
		for (i = 0; i < mNumParticles; i++)
		{
			//extract the particle's old pose
			oe = mAIParticles[i].East();
			on = mAIParticles[i].North();
			oh = mAIParticles[i].Heading();
			obe = mAIParticles[i].GPSBiasEast();
			obn = mAIParticles[i].GPSBiasNorth();

			//compute a taylor approximation to sin and cos for speed
			double dhp = UnwrapAngle(oh - mHeadingMMSE);
			double dhp2 = dhp*dhp;
			cosoh = cosmh - sinmh*dhp - 0.5*cosmh*dhp2;
			sinoh = sinmh + cosmh*dhp - 0.5*sinmh*dhp2;
			//cosoh = cos(oh);
			//sinoh = sin(oh);

			//rotate delta-positions into EN (RNDF coordinates) using the particle's heading
			Rv2e[midx(0, 0, 2)] = cosoh;
			Rv2e[midx(0, 1, 2)] = -sinoh;
			Rv2e[midx(1, 0, 2)] = sinoh;
			Rv2e[midx(1, 1, 2)] = cosoh;

			//calculate delta
			de = Rv2e[midx(0, 0, 2)]*dx + Rv2e[midx(0, 1, 2)]*dy;
			dn = Rv2e[midx(1, 0, 2)]*dx + Rv2e[midx(1, 1, 2)]*dy;
			//Qv = Rv2e*Qxy*Rv2e'
			for (j = 0; j < 2; j++)
			{
				for (k = 0; k < 2; k++)
				{
					Rv2eQxy[midx(j, k, 2)] = 0.0;
					for (h = 0; h < 2; h++)
					{
						Rv2eQxy[midx(j, k, 2)] += Rv2e[midx(j, h, 2)] * Qxy[midx(h, k, 2)];
					}
				}
			}
			for (j = 0; j < 2; j++)
			{
				for (k = 0; k < 2; k++)
				{
					Qv[midx(j, k, 2)] = 0.0;
					for (h = 0; h < 2; h++)
					{
						Qv[midx(j, k, 2)] += Rv2eQxy[midx(j, h, 2)] * Rv2e[midx(k, h, 2)];
					}
				}
			}
			for (j = 0; j < 2; j++)
			{
				for (k = 0; k < 2; k++)
				{
					Sv[midx(j, k, 2)] = Qv[midx(j, k, 2)];
				}
			}

			//Cholesky factorize Qv and store in Sv... do the chol by hand for efficiency.
			double a = sqrt(Qv[midx(0, 0, 2)]);
			if (fabs(a) == 0.0)
			{
				//in this case, best just to continue on the particle (it'll be resampled later)
				continue;
			}
			double b = Qv[midx(0, 1, 2)] / a;
			double temp = Qv[midx(1, 1, 2)] - b*b;
			if (temp < 0.0)
			{
				//in this case, best just to continue on the particle (it'll be resampled later)
				continue;
			}
			double c = sqrt(temp);
			Sv[midx(0, 0, 2)] = a;
			Sv[midx(0, 1, 2)] = b;
			Sv[midx(1, 0, 2)] = 0.0;
			Sv[midx(1, 1, 2)] = c;

			//predict pose forward assuming Gaussian process noise
			rval[0] = RandGaussian();
			rval[1] = RandGaussian();

			oe += de + rval[0]*Sv[midx(0, 0, 2)] + rval[1]*Sv[midx(1, 0, 2)];
			on += dn + rval[0]*Sv[midx(0, 1, 2)] + rval[1]*Sv[midx(1, 1, 2)];
			oh = UnwrapAngle(oh + dh + Sh*RandGaussian());
			#ifdef PP_GPSBIASES
				obe = blambda*obe + Sbias*RandGaussian();
				obn = blambda*obn + Sbias*RandGaussian();
			#endif

			//store the particle's updated state
			mAIParticles[i].SetState(oe, on, oh, obe, obn);
		}
	}

	//also predict the local road model forward
	if (PredictLocalRoadForTransmit(iPredictTime, iVehicleOdometry) == false)
	{
		#ifdef SE_TRANSMITMSGS
			return rSuccess;
		#endif
	}

	//***END PREDICTION***

	//store the timestamp on the particles
	mAITime = iPredictTime;

	//replace a gaussian random number in the reserve
	ReplaceGaussian();

	//calculate the aggregate estimates for the filter
	ComputeBestEstimateForTransmit();
	//and calculate the list of most likely partitions
	SetPartitionsForTransmit();

	//successfully completed the transmit calculations
	rSuccess = true;

	return rSuccess;
}

bool PosteriorPoseParticleFilter::PredictLocalRoadForTransmit(double iPredictTime, VehicleOdometry* iVehicleOdometry)
{
	/*
	Predicts the LocalRoad model forward for transmit.

	INPUTS:
		iPredictTime - time to which the local road model is predicted
		iVehicleOdometry - vehicle odometry structure, valid during the prediction

	OUTPUTS:
		rSuccess - true if the prediction was successful, false otherwise.  If true,
			predicts the mAILR* variables to the desired time.
	*/

	bool rSuccess = false;

	if (iVehicleOdometry->IsValid == false)
	{
		//can't predict if odometry is invalid
		return rSuccess;
	}

	double dt = iPredictTime - mLRTime;
	if (dt < 0.0)
	{
		//don't predict backwards
		return rSuccess;
	}

	//if the code gets here, the prediction will be done.
	//NOTE: the local road model might be invalid though
	rSuccess = true;

	if (mLRIsInitialized == false)
	{
		//can't predict anything if the local road model isn't initialized
		return rSuccess;
	}

	if (mLRNumLeftLanePoints == 0 || mLRNumCenterLanePoints == 0 || mLRNumRightLanePoints == 0)
	{
		//can't do a prediction without points
		return rSuccess;
	}

	//if the code gets here, there is actual predicting to do

	//delete existing memory allocated to the LocalRoad transmit variables
	delete [] mAILRLeftLanePoints;
	delete [] mAILRLeftLaneVars;
	delete [] mAILRCenterLanePoints;
	delete [] mAILRCenterLaneVars;
	delete [] mAILRRightLanePoints;
	delete [] mAILRRightLaneVars;

	//copy all localroad data into the mAILR* variables
	mAILRModelProb = mLRModelProb;
	mAILRLeftLaneProb = mLRLeftLaneProb;
	mAILRLeftLaneWidth = mLRLeftLaneWidth;
	mAILRLeftLaneWidthVar = mLRLeftLaneWidthVar;
	mAILRCenterLaneProb = mLRCenterLaneProb;
	mAILRCenterLaneWidth = mLRCenterLaneWidth;
	mAILRCenterLaneWidthVar = mLRCenterLaneWidthVar;
	mAILRRightLaneProb = mLRRightLaneProb;
	mAILRRightLaneWidth = mLRRightLaneWidth;
	mAILRRightLaneWidthVar = mLRRightLaneWidthVar;
	mAILRNumLeftLanePoints = mLRNumLeftLanePoints;
	mAILRNumCenterLanePoints = mLRNumCenterLanePoints;
	mAILRNumRightLanePoints = mLRNumRightLanePoints;

	mAILRLeftLanePoints = new double[2*mAILRNumLeftLanePoints];
	mAILRLeftLaneVars = new double[mAILRNumLeftLanePoints];
	mAILRCenterLanePoints = new double[2*mAILRNumCenterLanePoints];
	mAILRCenterLaneVars = new double[mAILRNumCenterLanePoints];
	mAILRRightLanePoints = new double[2*mAILRNumRightLanePoints];
	mAILRRightLaneVars = new double[mAILRNumRightLanePoints];

	int i;
	int idx;

	//copy the existing points into the transmit variables
	for (i = 0; i < mAILRNumLeftLanePoints; i++)
	{
		idx = midx(i, 0, mAILRNumLeftLanePoints);
		mAILRLeftLanePoints[idx] = mLRLeftLanePoints[idx];
		idx = midx(i, 1, mAILRNumLeftLanePoints);
		mAILRLeftLanePoints[idx] = mLRLeftLanePoints[idx];
		mAILRLeftLaneVars[i] = mLRLeftLaneVars[i];
	}
	for (i = 0; i < mAILRNumCenterLanePoints; i++)
	{
		idx = midx(i, 0, mAILRNumCenterLanePoints);
		mAILRCenterLanePoints[idx] = mLRCenterLanePoints[idx];
		idx = midx(i, 1, mAILRNumCenterLanePoints);
		mAILRCenterLanePoints[idx] = mLRCenterLanePoints[idx];
		mAILRCenterLaneVars[i] = mLRCenterLaneVars[i];
	}
	for (i = 0; i < mAILRNumRightLanePoints; i++)
	{
		idx = midx(i, 0, mAILRNumRightLanePoints);
		mAILRRightLanePoints[idx] = mLRRightLanePoints[idx];
		idx = midx(i, 1, mAILRNumRightLanePoints);
		mAILRRightLanePoints[idx] = mLRRightLanePoints[idx];
		mAILRRightLaneVars[i] = mLRRightLaneVars[i];
	}

	//predict the transmit variables forward
	PredictPoints(dt, mAILRNumLeftLanePoints, mAILRLeftLanePoints, iVehicleOdometry);
	PredictPoints(dt, mAILRNumCenterLanePoints, mAILRCenterLanePoints, iVehicleOdometry);
	PredictPoints(dt, mAILRNumRightLanePoints, mAILRRightLanePoints, iVehicleOdometry);

	return rSuccess;
}

void PosteriorPoseParticleFilter::SetPartitionsForTransmit(void)
{
	/*
	Calculates the most likely partitions that the vehicle could be on, along
	with the probability the vehicle is on those partitions.

	INPUTS:
		none.

	OUTPUTS:
		none.  Sets mAINumLikelyPartitions and mAILikelyPartitions to the most
			likely partitions the car may be traveling on at present.
	*/

	int i;
	int idx;

	RoadPartition* rpbest;

	double wsparse = 0.0;
	double wt;
	double wtot = 0.0;

	mAINumLikelyPartitions = 0;
	//clear the lookup table of existing likely partitions
	StringIntLUT mailpLUT;
	mailpLUT.ClearLUT();

	for (i = 0; i < mNumParticles; i++)
	{
		//find the closest partition for each particle
		rpbest = mRoadGraph->ClosestPartition(mAIParticles[i].East(), mAIParticles[i].North(), mAIRoadLocation, PP_OFFRNDFDIST);

		if (mailpLUT.FindIndex(&idx, rpbest->PartitionID()) == false)
		{
			//encountered a new likely partition
			if (mAINumLikelyPartitions >= ARBITER_MAX_PARTITIONS)
			{
				//too many likely partitions... need to ignore some
				printf("Warning: some likely partitions are being ignored.\n");
				continue;
			}

			idx = mAINumLikelyPartitions;

			//add the new partition to the list and the LUT
			mAILikelyPartitions[idx].id = string(rpbest->PartitionID());

			switch (rpbest->PartitionType())
			{
			case RP_LANE:
				mAILikelyPartitions[idx].partitionType = PARTITIONTYPE_LANE;
				break;

			case RP_INTERCONNECT:
				mAILikelyPartitions[idx].partitionType = PARTITIONTYPE_INTERCONNECT;
				break;

			case RP_ZONE:
				mAILikelyPartitions[idx].partitionType = PARTITIONTYPE_ZONE;
				break;
			}

			//initialize the partition's probability
			wt = mAIParticles[i].Weight();
			mAILikelyPartitions[idx].confidence = wt;
			wtot += wt;

			mailpLUT.AddEntry(rpbest->PartitionID(), idx);
			//increment the total number of likely partitions
			mAINumLikelyPartitions++;
		}
		else
		{
			//encountered an existing partition

			//update the partition's probability
			wt = mAIParticles[i].Weight();
			mAILikelyPartitions[idx].confidence += wt;
			wtot += wt;
		}

		if (rpbest->IsSparse() == true)
		{
			//sum up the total weight of the particles that are on sparse segments
			wsparse += wt;
		}
	}

	for (i = 0; i < mAINumLikelyPartitions; i++)
	{
		//normalize all the probabilities of the likely partitions

		mAILikelyPartitions[i].confidence /= wtot;
	}

	//normalize the sparse weight into a probability of being on a sparse segment
	mAISparseWaypoints = wsparse / wtot;

	return;
}

bool PosteriorPoseParticleFilter::SetLocalRoadForTransmit(void)
{
	/*
	Retrieves the local road representation from the RNDF for transmission to AI.

	INPUTS:
		none.

	OUTPUTS:
		rSuccess - true if the local road representation was successfully created, 
			false otherwise.  If true, all AI variables are set to correct values.
	*/

	bool rSuccess = false;

	//initialize to garbage values
	mAIStoplineExists = false;
	mAIDistanceToStopline = 0.0;
	mAIDistanceToStoplineVar = 0.0;
	mAIRoadModelIsValid = false;
	mAIRoadHeading = 0.0;
	mAIRoadHeadingVar = 0.0;
	mAIRoadCurvature = 0.0;
	mAIRoadCurvatureVar = 0.0;
	mAICenterLaneExists = false;
	mAILeftLaneExists = false;
	mAIRightLaneExists = false;
	mAICenterLaneID[0] = '\0';
	mAILeftLaneID[0] = '\0';
	mAIRightLaneID[0] = '\0';
	mAIDistToCenterLane = 0.0;
	mAIDistToCenterLaneVar = 0.0;
	mAIDistToLeftLane = 0.0;
	mAIDistToLeftLaneVar = 0.0;
	mAIDistToRightLane = 0.0;
	mAIDistToRightLaneVar = 0.0;
	mAICenterLaneWidth = 0.0;
	mAICenterLaneWidthVar = 0.0;
	mAILeftLaneWidth = 0.0;
	mAILeftLaneWidthVar = 0.0;
	mAIRightLaneWidth = 0.0;
	mAIRightLaneWidthVar = 0.0;

	if (mRoadGraph == NULL)
	{
		//can't do anything without a valid road graph
		return rSuccess;
	}

	int i;

	//determine whether there's a nearby stopline in the direction of travel
	RoadPoint* rptstop = mRoadGraph->ClosestUpcomingStopline(mAIEast, mAINorth, mAIHeading, PIOTWO, true, mAIRoadLocation);
	if (rptstop == NULL)
	{
		mAIStoplineExists = false;
	}
	else
	{
		mAIStoplineExists = true;
		//calculate the distance to the stopline if it exists
		mAIDistanceToStopline = rptstop->GetDistanceToPoint(mAIEast, mAINorth);
		//calculate the variance of the stopline distance
		for (i = 0; i < mNumParticles; i++)
		{
			//calculate the distance for this particle
			double wt = mAIParticles[i].Weight();
			double ds = rptstop->GetDistanceToPoint(mAIParticles[i].East(), mAIParticles[i].North());
			ds -= mAIDistanceToStopline;

			mAIDistanceToStoplineVar += wt * ds*ds;
		}
	}

	//determine whether there is a central lane
	switch (mAIRoadLocation->PartitionType())
	{
	case RP_LANE:
	case RP_INTERCONNECT:
		{
			//lanes and interconnects are both valid center lanes
			mAICenterLaneExists = true;

			//set the center lane's partition id
			strcpy_s(mAICenterLaneID, ROADGRAPH_FIELDSIZE, mAIRoadLocation->PartitionID());

			//compute road heading and local road curvature
			mAIRoadModelIsValid = mRoadGraph->LocalRoadRepresentation(&mAIRoadCurvature, &mAIRoadCurvatureVar, 
				mAIEast, mAINorth, mAIHeading, mAIRoadLocation);

			double ail;
			double aih;

			//compute signed distance to the center lane
			//also compute heading wrt the center lane, which is the heading of the road
			mAIRoadLocation->LaneOffsets(&ail, &aih, mAIEast, mAINorth, mAIHeading);
			//send road heading as a tangent (slope)
			mAIRoadHeading = tan(aih);
			mAIDistToCenterLane = ail;
			//and compute its variance
			for (i = 0; i < mNumParticles; i++)
			{
				double wt = mAIParticles[i].Weight();
				mAIRoadLocation->LaneOffsets(&ail, &aih, mAIParticles[i].East(), mAIParticles[i].North(), mAIParticles[i].Heading());

				double dp = ail - mAIDistToCenterLane;
				mAIDistToCenterLaneVar += wt * dp*dp;

				double dh = tan(aih) - mAIRoadHeading;
				mAIRoadHeadingVar += wt * dh*dh;
			}
			//compute the center lane's width
			mAICenterLaneWidth = mAIRoadLocation->LaneWidth();
			//compute the center lane's width variance
			mAICenterLaneWidthVar = 0.0;

			RoadPartition* rpbest;

			//determine whether there is a left lane
			if (mAIRoadLocation->IsInSameDirection(mAIEast, mAINorth, mAIHeading) == true)
			{
				rpbest = mAIRoadLocation->LeftLanePartition(mAIEast, mAINorth);
			}
			else
			{
				//if the viewpoint is opposite the direction of travel, take the right lane
				rpbest = mAIRoadLocation->RightLanePartition(mAIEast, mAINorth);
			}
			if (rpbest != NULL)
			{
				//a left lane exists
				mAILeftLaneExists = true;
				//set the left lane's partition id
				strcpy_s(mAILeftLaneID, ROADGRAPH_FIELDSIZE, rpbest->PartitionID());
				//set the distance to the left lane
				rpbest->LaneOffsets(&ail, &aih, mAIEast, mAINorth, mAIHeading);
				mAIDistToLeftLane = ail;
				//calculate its variance
				for (i = 0; i < mNumParticles; i++)
				{
					double wt = mAIParticles[i].Weight();
					rpbest->LaneOffsets(&ail, &aih, mAIParticles[i].East(), mAIParticles[i].North(), mAIParticles[i].Heading());
					double dp = ail - mAIDistToLeftLane;
					mAIDistToLeftLaneVar += wt * dp*dp;
				}

				//compute the left lane's width
				mAILeftLaneWidth = rpbest->LaneWidth();
				//compute the left lane's width variance
				mAILeftLaneWidthVar = 0.0;
			}

			//determine whether there is a right lane
			if (mAIRoadLocation->IsInSameDirection(mAIEast, mAINorth, mAIHeading) == true)
			{
				rpbest = mAIRoadLocation->RightLanePartition(mAIEast, mAINorth);
			}
			else
			{
				//if the viewpoint is opposite the direction of travel, take the left lane
				rpbest = mAIRoadLocation->LeftLanePartition(mAIEast, mAINorth);
			}
			if (rpbest != NULL)
			{
				//a right lane exists
				mAIRightLaneExists = true;
				//set the left lane's partition id
				strcpy_s(mAIRightLaneID, ROADGRAPH_FIELDSIZE, rpbest->PartitionID());
				//set the distance to the right lane
				rpbest->LaneOffsets(&ail, &aih, mAIEast, mAINorth, mAIHeading);
				mAIDistToRightLane = ail;
				//calculate its variance
				for (i = 0; i < mNumParticles; i++)
				{
					double wt = mAIParticles[i].Weight();
					rpbest->LaneOffsets(&ail, &aih, mAIParticles[i].East(), mAIParticles[i].North(), mAIParticles[i].Heading());
					double dp = ail - mAIDistToRightLane;
					mAIDistToRightLaneVar += wt * dp*dp;
				}

				//compute the right lane's width
				mAIRightLaneWidth = rpbest->LaneWidth();
				//compute the right lane's width variance
				mAIRightLaneWidthVar = 0.0;
			}
		}

		break;

	case RP_ZONE:
		//zones do not have valid lanes
		mAICenterLaneExists = false;
		mAILeftLaneExists = false;
		mAIRightLaneExists = false;

		break;
	}


	//successful local road extraction
	rSuccess = true;

	return rSuccess;
}

void PosteriorPoseParticleFilter::UpdateWithPose(double iPoseTime, double* iPoseBuff, Sensor* iPoseSensor)
{
	/*
	Updates the particle filter with an absolute pose measurement

	INPUTS:
		iPoseTime - timestamp of the pose update
		iPoseBuff - the absolute pose measurement information
		iPoseSensor - the pose sensor struct (the location and orientation of the pose 
			sensor on the vehicle)

	OUTPUTS:
		none.  Updates the weights of the particles with the absolute pose information.
	*/

	if (mIsInitialized == false || mRoadGraph == NULL)
	{
		//can't update until ppose is initialized and has a road graph
		return;
	}

	int i;
	int j;
	int k;
	int idx;

	//***EXTRACT THE MEASUREMENT***

	//first set GPS mode based on the packet's quality of service
	int pmode = (int) iPoseBuff[1];
	switch (pmode)
	{
	case 0:
		mGPSMode = POSE_GPSNONE;
		break;
	case 1:
		mGPSMode = POSE_GPSSPS;
		break;
	case 2:
		mGPSMode = POSE_GPSWAAS;
		break;
	case 3:
		mGPSMode = POSE_GPSVBS;
		break;
	case 4:
		mGPSMode = POSE_GPSHP;
		break;
	default:
		printf("Warning: received unknown GPS quality of service flag: %d.\n", pmode);
		mGPSMode = POSE_GPSNONE;
		break;
	}

	//check whether pose should be applied
	if (mGPSMode == POSE_GPSNONE)
	{
		//don't apply pose updates in blackouts
		#ifdef SE_COMMONDEBUGMSGS
			printf("Warning: pose update rejected due to inadequate GPS service.\n");
		#endif
		return;
	}

	//extract the measurements from the pose buffer
	double zecef[3];
	zecef[0] = iPoseBuff[5];
	zecef[1] = iPoseBuff[6];
	zecef[2] = iPoseBuff[7];
	double zenu[3];
	EnuEcefTransform(zenu, zecef, false, mRoadGraph->LatOrigin(), mRoadGraph->LonOrigin(), 0.0, true);

	//pose measurement is [E; N; H] of the vehicle
	double zpose[3];
	zpose[0] = zenu[0];
	zpose[1] = zenu[1];
	zpose[2] = iPoseBuff[2];

	//construct the measurement ENH covariance matrix
	double Recef[9];
	idx = 17;
	for (i = 0; i < 3; i++)
	{
		for (j = 0; j <= i; j++)
		{
			Recef[midx(i, j, 3)] = iPoseBuff[idx];
			idx++;
		}
	}
	//fill in the  upper triangle
	for (i = 0; i < 3; i++)
	{
		for (j = i+1; j < 3; j++)
		{
			Recef[midx(i, j, 3)] = Recef[midx(j, i, 3)];
		}
	}

	double Recef2enu[9];
	GetRecef2enu(Recef2enu, mRoadGraph->LatOrigin(), mRoadGraph->LonOrigin());
	double Recef2enuRecef[9];
	for (i = 0; i < 3; i++)
	{
		for (j = 0; j < 3; j++)
		{
			Recef2enuRecef[midx(i, j, 3)] = 0.0;
			for (k = 0; k < 3; k++)
			{
				Recef2enuRecef[midx(i, j, 3)] += Recef2enu[midx(i, k, 3)] * Recef[midx(k, j, 3)];
			}
		}
	}
	double Renu[9];
	//Renu = Recef2enu*Recef*(Recef2enu');
	for (i = 0; i < 3; i++)
	{
		for (j = 0; j < 3; j++)
		{
			Renu[midx(i, j, 3)] = 0.0;
			for (k = 0; k < 3; k++)
			{
				Renu[midx(i, j, 3)] += Recef2enuRecef[midx(i, k, 3)] * Recef2enu[midx(j, k, 3)];
			}
		}
	}

	//set the pose measurement covariance matrix
	double Rpose[9];
	Rpose[midx(0, 0, 3)] = Renu[midx(0, 0, 3)] + POSE_ADDLENVAR;
	Rpose[midx(0, 1, 3)] = Renu[midx(0, 1, 3)];
	Rpose[midx(1, 0, 3)] = Renu[midx(1, 0, 3)];
	Rpose[midx(1, 1, 3)] = Renu[midx(1, 1, 3)] + POSE_ADDLENVAR;
	Rpose[midx(0, 2, 3)] = 0.0;
	Rpose[midx(1, 2, 3)] = 0.0;
	Rpose[midx(2, 0, 3)] = 0.0;
	Rpose[midx(2, 1, 3)] = 0.0;
	Rpose[midx(2, 2, 3)] = iPoseBuff[11] + POSE_ADDLHDGVAR;

	//invert the Rpose matrix
	double invRpose[9];
	int ipiv[3];
	int info;
	for (i = 0; i < 3; i++)
	{
		for (j = 0; j < 3; j++)
		{
			invRpose[midx(i, j, 3)] = Rpose[midx(i, j, 3)];
		}
	}
	//LU decomposition for matrix inversion
	dgetrf(3, 3, invRpose, 3, ipiv, &info);
	//check for validity of LU decomposition
	if (info != 0)
	{
		printf("Warning: dgetrf error in PosteriorPoseParticleFilter::UpdateWithPose.\n");
		//need to stop the update if the inverse fails
		return;
	}
	//calculate the determinant of Rpose before destroying the LU decomposition
	double detRpose = 1.0;
	for (i = 0; i < 3; i++)
	{
		if (ipiv[i] > i+1)
		{
			//negate the determinant because a row pivot took place
			detRpose *= -invRpose[midx(i, i, 3)];
		}
		else
		{
			//don't negate the determinant because the ith row was either not pivoted
			//or it was pivoted (but we counted it already)
			detRpose *= invRpose[midx(i, i, 3)];
		}
	}
	//invert Rpose
	dgetri(3, invRpose, 3, ipiv, &info);
	if (info != 0)
	{
		printf("Warning: dgetri error in PosteriorPoseParticleFilter::UpdateWithPose.\n");
		//need to stop the update if the inverse fails
		return;
	}

	//***HYPOTHESIS TEST***

	//do a hypothesis test on the MMSE solution to see if pose can be applied

	//extract the EN location of pose using the vehicle
	double coshmmse = cos(mHeadingMMSE);
	double sinhmmse = sin(mHeadingMMSE);

	double* pmcache = new double[3*mNumParticles];

	double zemmse = 0.0;
	double znmmse = 0.0;
	double zhmmse = 0.0;
	double wraptarget;
	for (i = 0; i < mNumParticles; i++)
	{
		//calculate and cache the expected measurement for each particle
		double zpe;
		double zpn;
		double zph;

		#ifndef PP_GPSBIASES
			zpe = mParticles[i].East() + coshmmse*iPoseSensor->SensorX - sinhmmse*iPoseSensor->SensorY;
			zpn = mParticles[i].North() + sinhmmse*iPoseSensor->SensorX + coshmmse*iPoseSensor->SensorY;
			zph = mParticles[i].Heading() + iPoseSensor->SensorYaw;
		#else
			zpe = mParticles[i].East() + mParticles[i].GPSBiasEast() + coshmmse*iPoseSensor->SensorX - sinhmmse*iPoseSensor->SensorY;
			zpn = mParticles[i].North() + mParticles[i].GPSBiasNorth() + sinhmmse*iPoseSensor->SensorX + coshmmse*iPoseSensor->SensorY;
			zph = mParticles[i].Heading() + iPoseSensor->SensorYaw;
		#endif

		if (i == 0)
		{
			wraptarget = zph;
		}
		zph = WrapAngle(zph, wraptarget);

		//cache the measurements for each particle
		pmcache[midx(i, 0, mNumParticles)] = zpe;
		pmcache[midx(i, 1, mNumParticles)] = zpn;
		pmcache[midx(i, 2, mNumParticles)] = zph;

		//calculate the mmse measurement
		double pwt = mParticles[i].Weight();
		zemmse += pwt*zpe;
		znmmse += pwt*zpn;
		zhmmse += pwt*zph;
	}

	//calculate the innovation from the expected measurement
	double nu[3];
	nu[0] = zpose[0] - zemmse;
	nu[1] = zpose[1] - znmmse;
	nu[2] = UnwrapAngle(zpose[2] - zhmmse);

	//compute measurement variance
	double HPHt[9];
	for (i = 0; i < 3; i++)
	{
		for (j = 0; j < 3; j++)
		{
			HPHt[midx(i, j, 3)] = 0.0;
		}
	}
	for (i = 0; i < mNumParticles; i++)
	{
		//calculate the expected measurement from each particle to compute measurement variance

		//pull expected road offsets for this particle
		double pe = pmcache[midx(i, 0, mNumParticles)];
		double pn = pmcache[midx(i, 1, mNumParticles)];
		double ph = pmcache[midx(i, 2, mNumParticles)];

		//calculate deviations from the MMSE estimate
		double wt = mParticles[i].Weight();
		double de = pe - zemmse;
		double dn = pn - znmmse;
		double dh = ph - zhmmse;
		//use deviations to calculate measurement covariance
		HPHt[midx(0, 0, 3)] += wt*de*de;
		HPHt[midx(0, 1, 3)] += wt*de*dn;
		HPHt[midx(0, 2, 3)] += wt*de*dh;
		HPHt[midx(1, 1, 3)] += wt*dn*dn;
		HPHt[midx(1, 2, 3)] += wt*dn*dh;
		HPHt[midx(2, 2, 3)] += wt*dh*dh;
	}
	HPHt[midx(1, 0, 3)] = HPHt[midx(0, 1, 3)];
	HPHt[midx(2, 0, 3)] = HPHt[midx(0, 2, 3)];
	HPHt[midx(2, 1, 3)] = HPHt[midx(1, 2, 3)];

	double invS[9];
	for (i = 0; i < 3; i++)
	{
		for (j = 0; j < 3; j++)
		{
			invS[midx(i, j, 3)] = HPHt[midx(i, j, 3)] + Rpose[midx(i, j, 3)];
		}
	}

	//invert the S matrix
	//LU decomposition for matrix inversion
	dgetrf(3, 3, invS, 3, ipiv, &info);
	//check for validity of LU decomposition
	if (info != 0)
	{
		printf("Warning: dgetrf error in PosteriorPoseParticleFilter::UpdateWithPose.\n");
		//need to stop the update if the inverse fails
		delete [] pmcache;
		return;
	}
	//calculate the determinant before destroying the LU decomposition
	double detS = 1.0;
	for (i = 0; i < 3; i++)
	{
		if (ipiv[i] > i+1)
		{
			//negate the determinant because a row pivot took place
			detS *= -invS[midx(i, i, 3)];
		}
		else
		{
			//don't negate the determinant because the ith row was either not pivoted
			//or it was pivoted (but we counted it already)
			detS *= invS[midx(i, i, 3)];
		}
	}
	dgetri(3, invS, 3, ipiv, &info);
	if (info != 0)
	{
		printf("Warning: dgetri error in PosteriorPoseParticleFilter::UpdateWithPose.\n");
		//need to stop the update if the inverse fails
		delete [] pmcache;
		return;
	}

	//calculate the MMSE innovation statistic: nu'*inv(S)*nu
	double chi2S = 0.0;
	for (i = 0; i < 3; i++)
	{
		for (j = 0; j < 3; j++)
		{
			chi2S += nu[i]*invS[midx(i, j, 3)]*nu[j];
		}
	}

	//chi2S should be chi2 with 3 dof
	if (chi2S > POSE_HYPOTEST)
	{
		printf("Rejected pose update: %lg vs. %lg.\n", chi2S, POSE_HYPOTEST);
		delete [] pmcache;
		return;
	}

	//***PERFORM THE UPDATE***

	//if code gets here, the update can be performed
	double lambdamax = -DBL_MAX;
	double twopi3 = pow(TWOPI, 3.0);
	for (i = 0; i < mNumParticles; i++)
	{
		//calculate the updated log-likelihood of each particle

		//extract the location of the pose sensor from the cache
		double pe = pmcache[midx(i, 0, mNumParticles)];
		double pn = pmcache[midx(i, 1, mNumParticles)];
		double ph = pmcache[midx(i, 2, mNumParticles)];

		//calculate the "innovation" on this particle
		nu[0] = zpose[0] - pe;
		nu[1] = zpose[1] - pn;
		nu[2] = UnwrapAngle(zpose[2] - ph);

		//calculate nu'*inv(Rpose)*nu
		double mdist = 0.0;
		for (j = 0; j < 3; j++)
		{
			for (k = 0; k < 3; k++)
			{
				mdist += nu[j]*invRpose[midx(j, k, 3)]*nu[k];
			}
		}

		//temporarily overwrite this particle's weight with its log-likelihood weight
		mParticles[i].SetWeight(log(mParticles[i].Weight()) - 0.5*mdist - log(sqrt(twopi3 * detRpose)));
		//also keep track of the maximum log-likelihood
		if (mParticles[i].Weight() > lambdamax)
		{
			lambdamax = mParticles[i].Weight();
		}
	}

	//now reweight and renormalize particles from log likelihood
	//note: this is better-conditioned numerically than a multiplicative reweighting
	double sumw = 0.0;
	for (i = 0; i < mNumParticles; i++)
	{
		mParticles[i].SetWeight(exp(mParticles[i].Weight() - lambdamax));
		sumw += mParticles[i].Weight();
	}
	//renormalize weights to sum to unity
	for (i = 0; i < mNumParticles; i++)
	{
		mParticles[i].SetWeight(mParticles[i].Weight() / sumw);
	}

	//***END UPDATE***

	//store the timestamp on the particles
	mPFTime = iPoseTime;

	//calculate the MMSE estimates for the filter
	ComputeBestEstimate();

	delete [] pmcache;
	return;
}

void PosteriorPoseParticleFilter::UpdateWithMobileyeLane(double iMobileyeTime, double* iMobileyeBuff, Sensor* iMobileyeSensor)
{
	/*
	Applies one mobileye lane measurement update: just heading and position wrt the lane.

	INPUTS:
		iMobileyeTime - timestamp of the mobileye update.
		iMobileyeBuff - the complete mobileye packet.  Not all of the packet is used here.
		iMobileyeSensor - the mobileye sensor structure, which tells where the camera is
			with respect to the vehicle's coordinate frame.

	OUTPUTS:
		none.  Updates the particle weights with mobileye lane information.
	*/

	if (mIsInitialized == false || mRoadGraph == NULL)
	{
		//can't update until ppose is initialized and has a road graph
		return;
	}

	int i;
	int j;

	//***EXTRACT THE MEASUREMENT***

	//extract the from the mobileye to gate the measurement
	if (iMobileyeBuff[17] < MOBILEYE_MIN_ROADCONF || iMobileyeBuff[18] < MOBILEYE_MIN_ROADCONF || iMobileyeBuff[21] < MOBILEYE_MIN_VALIDDISTANCE)
	{
		//mobileye not confident enough to update the particles
		#ifdef SE_COMMONDEBUGMSGS
			printf("Rejected mobileye lane update due to lack of confidence.\n");
		#endif
		return;
	}

	//extract road measurements from the mobileye buffer
	double zmobileye[2];
	//lane tracking estimate of lane center wrt mobileye camera (vehicle coordinates)
	zmobileye[0] = -0.5*(iMobileyeBuff[3] + iMobileyeBuff[4]);
	//lane tracking estimate of lane heading wrt mobileye camera (vehicle coordinates)
	zmobileye[1] = -atan(iMobileyeBuff[7]);

	double Rmobileye[4];
	for (i = 0; i < 2; i++)
	{
		for (j = 0; j < 2; j++)
		{
			Rmobileye[midx(i, j, 2)] = 0.0;
		}
	}
	Rmobileye[midx(0, 0, 2)] = MOBILEYE_LANEOFST_VAR;
	Rmobileye[midx(1, 1, 2)] = MOBILEYE_HDGOFST_VAR;

	//***HYPOTHESIS TEST THE MEASUREMENT***

	//NOTE: the hypothesis test is approximate, to weed out cases where
	//the mobileye has obviously mismatched lanes

	//simultaneously calculate two tests: probability that there's no lane, and innovation test

	//calculate the MMSE road offsets for the particles
	double plnMMSE = 0.0;
	double phdMMSE = 0.0;
	double wMMSE = 0.0;

	double pnoroad = 0.0;
	double* pmcache = new double[7*mNumParticles];

	for (i = 0; i < mNumParticles; i++)
	{
		//compute the ENH of ith particle's camera's location
		double pe = mParticles[i].East();
		double pn = mParticles[i].North();
		double ph = mParticles[i].Heading();
		double cosph = cos(ph);
		double sinph = sin(ph);
		pe += cosph*iMobileyeSensor->SensorX - sinph*iMobileyeSensor->SensorY;
		pn += sinph*iMobileyeSensor->SensorX + cosph*iMobileyeSensor->SensorY;
		ph += iMobileyeSensor->SensorYaw;
		//note: the camera is actually looking at a portion of the road in front of it
		pe += cos(ph)*MOBILEYE_CAMVIEWDIST;
		pn += sin(ph)*MOBILEYE_CAMVIEWDIST;

		//find which partition the particle's viewpoint is closest to
		RoadPartition* rpcenter = mRoadGraph->ClosestPartition(pe, pn, mRoadLocation);

		switch (rpcenter->PartitionType())
		{
		case RP_LANE:
		case RP_INTERCONNECT:
			{
				//lanes and interconnects can be correctly detected by the mobileye

				switch (rpcenter->FitType())
				{
				case RP_LINE:
					//calculate expected road offsets for this particle
					double pln;
					double phd;
					rpcenter->LaneOffsets(&pln, &phd, pe, pn, ph);

					//cache the closest lane measurements for later
					pmcache[midx(i, 0, mNumParticles)] = phd;
					pmcache[midx(i, 1, mNumParticles)] = pln;

					//use this measurement to contribute to the MMSE
					wMMSE += mParticles[i].Weight();
					plnMMSE += mParticles[i].Weight() * pln;
					phdMMSE += mParticles[i].Weight() * phd;

					RoadPartition* rpbest;

					//determine whether there is a left lane
					if (rpcenter->IsInSameDirection(pe, pn, ph) == true)
					{
						rpbest = rpcenter->LeftLanePartition(pe, pn);
					}
					else
					{
						rpbest = rpcenter->RightLanePartition(pe, pn);
					}

					if (rpbest != NULL)
					{
						//a left lane exists- set distance to it
						rpbest->LaneOffsets(&pln, &phd, pe, pn, ph);
						pmcache[midx(i, 2, mNumParticles)] = pln;
						//also set distance to left+center
						pmcache[midx(i, 4, mNumParticles)] = 0.5 * (pmcache[midx(i, 1, mNumParticles)] + pmcache[midx(i, 2, mNumParticles)]);
					}
					else
					{
						//mark the left lane with garbage values
						pmcache[midx(i, 2, mNumParticles)] = DBL_MAX;
						//also mark any combinations including left lane as garbage
						pmcache[midx(i, 4, mNumParticles)] = DBL_MAX;
						pmcache[midx(i, 6, mNumParticles)] = DBL_MAX;
					}

					//determine whether there is a right lane
					if (rpcenter->IsInSameDirection(pe, pn, ph) == true)
					{
						rpbest = rpcenter->RightLanePartition(pe, pn);
					}
					else
					{
						rpbest = rpcenter->LeftLanePartition(pe, pn);
					}

					if (rpbest != NULL)
					{
						//a right lane exists- set distance to it
						rpbest->LaneOffsets(&pln, &phd, pe, pn, ph);
						pmcache[midx(i, 3, mNumParticles)] = pln;
						//also set distance to right+center
						pmcache[midx(i, 5, mNumParticles)] = 0.5 * (pmcache[midx(i, 1, mNumParticles)] + pmcache[midx(i, 3, mNumParticles)]);
					}
					else
					{
						//mark the right lane with garbage values
						pmcache[midx(i, 3, mNumParticles)] = DBL_MAX;
						//also mark any combinations including right lane as garbage
						pmcache[midx(i, 5, mNumParticles)] = DBL_MAX;
						pmcache[midx(i, 6, mNumParticles)] = DBL_MAX;
					}

					if (pmcache[midx(i, 2, mNumParticles)] != DBL_MAX && pmcache[midx(i, 3, mNumParticles)] != DBL_MAX)
					{
						//set distance to left+right+center if all are valid
						pmcache[midx(i, 6, mNumParticles)] = 0.5 * (pmcache[midx(i, 2, mNumParticles)] + pmcache[midx(i, 3, mNumParticles)]);
					}

					break;

				default:
					//any lane lines for non-road fit types will be false detections

					//this particle contributes to the fp probability
					pnoroad += mParticles[i].Weight();

					//cache the closest lane measurements as garbage values
					pmcache[midx(i, 0, mNumParticles)] = DBL_MAX;
					pmcache[midx(i, 1, mNumParticles)] = DBL_MAX;
					//and mark that left and right lanes can't exist either
					pmcache[midx(i, 2, mNumParticles)] = DBL_MAX;
					pmcache[midx(i, 3, mNumParticles)] = DBL_MAX;
					pmcache[midx(i, 4, mNumParticles)] = DBL_MAX;
					pmcache[midx(i, 5, mNumParticles)] = DBL_MAX;
					pmcache[midx(i, 6, mNumParticles)] = DBL_MAX;

					break;
				}
			}

			break;

		default:
			//any lane lines in non-road partition types will be false detections

			{
				//this particle contributes to the fp probability
				pnoroad += mParticles[i].Weight();

				//cache the closest lane measurements as garbage values
				pmcache[midx(i, 0, mNumParticles)] = DBL_MAX;
				pmcache[midx(i, 1, mNumParticles)] = DBL_MAX;
				//and mark that left and right lanes can't exist either
				pmcache[midx(i, 2, mNumParticles)] = DBL_MAX;
				pmcache[midx(i, 3, mNumParticles)] = DBL_MAX;
				pmcache[midx(i, 4, mNumParticles)] = DBL_MAX;
				pmcache[midx(i, 5, mNumParticles)] = DBL_MAX;
				pmcache[midx(i, 6, mNumParticles)] = DBL_MAX;
			}

			break;
		}
	}

	if (pnoroad > MOBILEYE_NOROAD_THRESH)
	{
		//probability of a false positive is too high to apply update
		printf("Mobileye lane update rejected as a false positive.\n");
		delete [] pmcache;
		return;
	}
	if (fabs(wMMSE) == 0.0)
	{
		//all particles agree that no lane exists
		printf("Mobileye lane update rejected as a false positive.\n");
		delete [] pmcache;
		return;
	}

	plnMMSE /= wMMSE;
	phdMMSE /= wMMSE;
	//plnMMSE and phdMMSE now contain the MMSE road offsets

	//compute measurement variance
	double wHPHt = 0.0;
	double HPHt[4] = {0.0, 0.0, 0.0, 0.0};
	for (i = 0; i < mNumParticles; i++)
	{
		//calculate the expected measurement from each particle to compute measurement variance

		//pull expected road offsets for this particle
		double pln = pmcache[midx(i, 1, mNumParticles)];
		double phd = pmcache[midx(i, 0, mNumParticles)];
		if (pln == DBL_MAX && phd == DBL_MAX)
		{
			//this particle doesn't see any lanes
			continue;
		}

		//calculate deviations from the MMSE estimate
		double wt = mParticles[i].Weight();
		double dln = pln - plnMMSE;
		double dhd = UnwrapAngle(phd - phdMMSE);
		wHPHt += wt;
		//use deviations to calculate measurement covariance
		HPHt[midx(0, 0, 2)] += wt*dln*dln;
		HPHt[midx(0, 1, 2)] += wt*dln*dhd;
		HPHt[midx(1, 1, 2)] += wt*dhd*dhd;
	}
	HPHt[midx(1, 0, 2)] = HPHt[midx(0, 1, 2)];
	for (i = 0; i < 2; i++)
	{
		for (j = 0; j < 2; j++)
		{
			HPHt[midx(i, j, 2)] /= wHPHt;
		}
	}

	//compute the innovation covariance matrix
	double S[4];
	for (i = 0; i < 2; i++)
	{
		for (j = 0; j < 2; j++)
		{
			S[midx(i, j, 2)] = HPHt[midx(i, j, 2)] + Rmobileye[midx(i, j, 2)];
		}
	}

	//invert the innovation covariance matrix
	double detS = S[midx(0, 0, 2)]*S[midx(1, 1, 2)] - S[midx(0, 1, 2)]*S[midx(1, 0, 2)];
	if (fabs(detS) == 0.0)
	{
		//have to reject the measurement if the matrix is singular
		printf("Mobileye lane update rejected due to singular S matrix.\n");
		delete [] pmcache;
		return;
	}

	double invS[4];
	invS[midx(0, 0, 2)] = S[midx(1, 1, 2)]/detS;
	invS[midx(1, 1, 2)] = S[midx(0, 0, 2)]/detS;
	invS[midx(0, 1, 2)] = -S[midx(0, 1, 2)]/detS;
	invS[midx(1, 0, 2)] = -S[midx(1, 0, 2)]/detS;

	//compute the innovation statistic nu'*invS*nu from the MMSE estimate
	double nu[2] = {zmobileye[0] - plnMMSE, zmobileye[1] - phdMMSE};
	double chi2S = 0.0;
	for (i = 0; i < 2; i++)
	{
		for (j = 0; j < 2; j++)
		{
			chi2S += nu[i]*invS[midx(i, j, 2)]*nu[j];
		}
	}

	if (chi2S > MOBILEYE_LANE_HYPOTEST)
	{
		printf("Mobileye lane update rejected due to chi2 test.\n");
		delete [] pmcache;
		return;
	}

	//***APPLY MEASUREMENT UPDATE***

	//once the filter decides to apply the update, move to a more complicated measurement model
	//for each particle, use a multimodal gaussian with centers at:
	//1. heading wrt closest lane
	//2. dist to closest lane
	//3. dist to left lane
	//4. dist to right lane
	//5. dist to left and center lane combined
	//6. dist to right and center lane combined
	//7. dist to all three lanes combined
	//also model a uniform fp probability

	double lambdamax = -DBL_MAX;
	double lambdahfact = 1.0 / sqrt(TWOPI * MOBILEYE_HDGOFST_VAR);
	//model the variance of the lane measurement as increasing linearly with the number of lanes combined
	double R1 = MOBILEYE_LANEOFST_VAR;
	double R2 = 2.0*R1;
	double R3 = 3.0*R1;
	double lambdal1fact = 1.0 / sqrt(TWOPI * R1);
	double lambdal2fact = 1.0 / sqrt(TWOPI * R2);
	double lambdal3fact = 1.0 / sqrt(TWOPI * R3);
	for (i = 0; i < mNumParticles; i++)
	{
		//extract the measurements for this particle from the cache
		//lane heading wrt camera
		double zbarH = pmcache[midx(i, 0, mNumParticles)];
		//center, left, right lane offsets wrt camera
		double zbarC = pmcache[midx(i, 1, mNumParticles)];
		double zbarL = pmcache[midx(i, 2, mNumParticles)];
		double zbarR = pmcache[midx(i, 3, mNumParticles)];
		//center+left, center+right lane offsets wrt camera
		double zbarCL = pmcache[midx(i, 4, mNumParticles)];
		double zbarCR = pmcache[midx(i, 5, mNumParticles)];
		//center+left+right lane offset wrt camera
		double zbarCLR = pmcache[midx(i, 6, mNumParticles)];

		//calculate the measurement likelihood
		//model as gaussian in heading multiplying a multimodal gaussian in lane center location
		double lambdaH = exp(-0.5*pow(zmobileye[1] - zbarH, 2.0) / MOBILEYE_HDGOFST_VAR) * lambdahfact;
		double lambdaL = 0.0;
		//accumulate the sum of gaussians measurement likelihood for all existing lanes
		if (zbarC != DBL_MAX)
		{
			lambdaL += MOBILEYE_CORRECTLANE_PROB * exp(-0.5*pow(zmobileye[0] - zbarC, 2.0) / R1) * lambdal1fact;
		}
		if (zbarL != DBL_MAX)
		{
			lambdaL += MOBILEYE_INCORRECTLANE_PROB * exp(-0.5*pow(zmobileye[0] - zbarL, 2.0) / R1) * lambdal1fact;
		}
		if (zbarR != DBL_MAX)
		{
			lambdaL += MOBILEYE_INCORRECTLANE_PROB * exp(-0.5*pow(zmobileye[0] - zbarR, 2.0) / R1) * lambdal1fact;
		}
		if (zbarCL != DBL_MAX)
		{
			lambdaL += MOBILEYE_TWOLANE_PROB * exp(-0.5*pow(zmobileye[0] - zbarCL, 2.0) / R2) * lambdal2fact;
		}
		if (zbarCR != DBL_MAX)
		{
			lambdaL += MOBILEYE_TWOLANE_PROB * exp(-0.5*pow(zmobileye[0] - zbarCL, 2.0) / R2) * lambdal2fact;
		}
		if (zbarCLR != DBL_MAX)
		{
			lambdaL += MOBILEYE_THREELANE_PROB * exp(-0.5*pow(zmobileye[0] - zbarCLR, 2.0) / R3) * lambdal3fact;
		}
		//also accumulate the uniform probability of a false positive measurement
		lambdaL += MOBILEYE_FPLANE_PROB * MOBILEYE_FPLIKELIHOOD;

		//temporarily overwrite this particle's weight with its log-likelihood weight
		mParticles[i].SetWeight(log(mParticles[i].Weight()) + log(lambdaH) + log(lambdaL));
		//also keep track of the maximum log-likelihood
		if (mParticles[i].Weight() > lambdamax)
		{
			lambdamax = mParticles[i].Weight();
		}
	}

	//now reweight and renormalize particles from log likelihood
	//note: this is better-conditioned numerically than a multiplicative reweighting
	double sumw = 0.0;
	for (i = 0; i < mNumParticles; i++)
	{
		mParticles[i].SetWeight(exp(mParticles[i].Weight() - lambdamax));
		sumw += mParticles[i].Weight();
	}
	//renormalize weights to sum to unity
	for (i = 0; i < mNumParticles; i++)
	{
		mParticles[i].SetWeight(mParticles[i].Weight() / sumw);
	}

	//***END UPDATE***

	//store the timestamp on the particles
	mPFTime = iMobileyeTime;

	//calculate the MMSE estimates for the filter
	ComputeBestEstimate();

	delete [] pmcache;

	return;
}

void PosteriorPoseParticleFilter::UpdateWithMobileyeLines(double iMobileyeTime, double* iMobileyeBuff, Sensor *iMobileyeSensor)
{
	/*
	Updates the particle filter with mobileye measurements of lane line types.

	INPUTS:
		iMobileyeTime - timestamp of the mobileye update
		iMobileyeBuff - buffer containing the mobileye's measurement
		iMobileyeSensor - sensor structure telling how the camera is oriented on the car

	OUTPUTS:
		none.  Reweights the particles according to the lane lines they see.
	*/

	if (mIsInitialized == false || mRoadGraph == NULL)
	{
		//can't update until ppose is initialized and has a road graph
		return;
	}

	//***EXTRACT THE MEASUREMENT***

	//extract the confidence from the mobileye to gate the measurement
	if (iMobileyeBuff[17] < MOBILEYE_MIN_ROADCONF || iMobileyeBuff[18] < MOBILEYE_MIN_ROADCONF || iMobileyeBuff[11] < MOBILEYE_MIN_VALIDDISTANCE)
	{
		//mobileye not confident enough to update the particles
		#ifdef SE_COMMONDEBUGMSGS
			printf("Rejected mobileye line update due to lack of confidence.\n");
		#endif
		return;
	}

	//check to make sure mobileye is measuring the lane the vehicle is in
	if (iMobileyeBuff[3]*iMobileyeBuff[4] > 0.0)
	{
		//mobileye is measuring some lane that the vehicle's not in
		#ifdef SE_COMMONDEBUGMSGS
			printf("Rejected mobileye line update due to incorrect lane measurement.\n");
		#endif
		return;
	}

	//extract left and right lane measurements from the mobileye buffer
	//(and match them to road partition constants)
	int zmobileye[2];

	//lane's left boundary type
	switch ((int)(iMobileyeBuff[11]))
	{
	case 0:
	case 3:
	case 5:
		//mobileye's "none," "virtual," and "road edge" boundaries - no line present
		zmobileye[0] = LL_NOLINE;
		break;

	case 1:
		//mobileye's "solid" boundary - a solid line
		zmobileye[0] = LL_SOLIDLINE;
		break;

	case 2:
	case 4:
		//mobileye's "dashed" and "botts dots" boundaries - a dashed line
		zmobileye[0] = LL_DASHEDLINE;
		break;

	case 6:
		//mobileye's "double" boundary - a double line
		zmobileye[0] = LL_DOUBLELINE;
	}

	//lane's right boundary type
	switch ((int)(iMobileyeBuff[12]))
	{
	case 0:
	case 3:
	case 5:
		//mobileye's "none," "virtual," and "road edge" boundaries - no line present
		zmobileye[1] = LL_NOLINE;
		break;

	case 1:
		//mobileye's "solid" boundary - a solid line
		zmobileye[1] = LL_SOLIDLINE;
		break;

	case 2:
	case 4:
		//mobileye's "dashed" and "botts dots" boundaries - a dashed line
		zmobileye[1] = LL_DASHEDLINE;
		break;

	case 6:
		//mobileye's "double" boundary - a double line
		zmobileye[1] = LL_DOUBLELINE;
	}

	//***HYPOTHESIS TEST THE MEASUREMENT***

	//do two simultaneous tests: whether a lane exists, and whether the lines are measured correctly
	int i;
	double pnoroad = 0.0;
	double pwronglines = 0.0;
	int* zparticles = new int[2*mNumParticles];

	for (i = 0; i < mNumParticles; i++)
	{
		//calculate the expected measurement from each particle

		//compute the ENH of this particle's camera's location
		double pe = mParticles[i].East();
		double pn = mParticles[i].North();
		double ph = mParticles[i].Heading();
		double cosph = cos(ph);
		double sinph = sin(ph);
		pe += cosph*iMobileyeSensor->SensorX - sinph*iMobileyeSensor->SensorY;
		pn += sinph*iMobileyeSensor->SensorX + cosph*iMobileyeSensor->SensorY;
		ph += iMobileyeSensor->SensorYaw;
		//note: the camera is actually looking at a portion of the road in front of it
		pe += cos(ph)*MOBILEYE_CAMVIEWDIST;
		pn += sin(ph)*MOBILEYE_CAMVIEWDIST;

		//find the partition closest to the camera's viewpoint location
		RoadPartition* rpcur = mRoadGraph->ClosestPartition(pe, pn, mRoadLocation);

		//check whether this partition should have lane lines
		switch (rpcur->PartitionType())
		{
		case RP_ZONE:
			//this viewpoint shouldn't be seeing lane lines at all
			pnoroad += mParticles[i].Weight();
			break;
		}

		//retrieve the left and right boundaries, converting into lane line descriptors
		int lbound;
		int rbound;
		if (rpcur->IsInSameDirection(pe, pn, ph) == true)
		{
			//camera's point of view is same direction as the partition
			lbound = rpcur->LeftBoundary();
			rbound = rpcur->RightBoundary();
		}
		else
		{
			//camera's point of view is backwards: reverse the boundaries
			lbound = rpcur->RightBoundary();
			rbound = rpcur->LeftBoundary();
		}

		switch (lbound)
		{
		case RP_NOBOUNDARY:
			zparticles[midx(i, 0, mNumParticles)] = LL_NOLINE;
			break;

		case RP_SOLIDYELLOW:
		case RP_SOLIDWHITE:
			zparticles[midx(i, 0, mNumParticles)] = LL_SOLIDLINE;
			break;

		case RP_BROKENWHITE:
			zparticles[midx(i, 0, mNumParticles)] = LL_DASHEDLINE;
			break;

		case RP_DOUBLEYELLOW:
			zparticles[midx(i, 0, mNumParticles)] = LL_DOUBLELINE;
			break;
		}

		switch (rbound)
		{
		case RP_NOBOUNDARY:
			zparticles[midx(i, 1, mNumParticles)] = LL_NOLINE;
			break;

		case RP_SOLIDYELLOW:
		case RP_SOLIDWHITE:
			zparticles[midx(i, 1, mNumParticles)] = LL_SOLIDLINE;
			break;

		case RP_BROKENWHITE:
			zparticles[midx(i, 1, mNumParticles)] = LL_DASHEDLINE;
			break;

		case RP_DOUBLEYELLOW:
			zparticles[midx(i, 1, mNumParticles)] = LL_DOUBLELINE;
			break;
		}

		//accumulate the probability that both the measurements are wrong
		if (zparticles[midx(i, 0, mNumParticles)] != zmobileye[0] && zparticles[midx(i, 1, mNumParticles)] != zmobileye[1])
		{
			//neither the left nor the right boundaries match the measured boundaries
			pwronglines += mParticles[i].Weight();
		}
	}

	if (pnoroad > MOBILEYE_NOROAD_THRESH)
	{
		//mobileye should not be seeing a road
		printf("Rejected mobileye line update as a false positive.\n");
		delete [] zparticles;
		return;
	}
	if (pwronglines > MOBILEYE_WRONGLINE_THRESH)
	{
		//probability that the lane measurement is wrong is too large
		printf("Rejected mobileye line update due to failed hypothesis test.\n");
		delete [] zparticles;
		return;
	}

	//***APPLY THE MEASUREMENT***

	double lambdamax = -DBL_MAX;
	for (i = 0; i < mNumParticles; i++)
	{
		//compute measurement likelihood for each particle

		//probability of correct measurement
		double accL;
		//probability of measuring something other than the correct line type
		double fpL;

		switch (zparticles[midx(i, 0, mNumParticles)])
		{
		case LL_NOLINE:
			accL = MOBILEYE_NOLINE_ACCURACY;
			break;

		case LL_DASHEDLINE:
			accL = MOBILEYE_DASHEDLINE_ACCURACY;
			break;

		case LL_SOLIDLINE:
			accL = MOBILEYE_SINGLELINE_ACCURACY;
			break;

		case LL_DOUBLELINE:
			accL = MOBILEYE_DOUBLELINE_ACCURACY;
			break;
		}

		//model all incorrect measurements as equally likely
		fpL = (1.0 - accL) / 3.0;

		//probability of correct measurement
		double accR;
		//probability of measuring something other than the correct line type
		double fpR;

		switch (zparticles[midx(i, 1, mNumParticles)])
		{
		case LL_NOLINE:
			accR = MOBILEYE_NOLINE_ACCURACY;
			break;

		case LL_DASHEDLINE:
			accR = MOBILEYE_DASHEDLINE_ACCURACY;
			break;

		case LL_SOLIDLINE:
			accR = MOBILEYE_SINGLELINE_ACCURACY;
			break;

		case LL_DOUBLELINE:
			accR = MOBILEYE_DOUBLELINE_ACCURACY;
			break;
		}

		//model all incorrect measurements as equally likely
		fpR = (1.0 - accR) / 3.0;

		//update the particle's likelihood
		//temporarily overwrite this particle's weight with its log-likelihood weight
		mParticles[i].SetWeight(log(mParticles[i].Weight()));
		if (zmobileye[0] == zparticles[midx(i, 0, mNumParticles)])
		{
			//left lane measurement agrees with the particle
			mParticles[i].SetWeight(mParticles[i].Weight() + log(accL));
		}
		else
		{
			//left lane measurement disagrese with the particle
			mParticles[i].SetWeight(mParticles[i].Weight() + log(fpL));
		}

		if (zmobileye[1] == zparticles[midx(i, 1, mNumParticles)])
		{
			//right lane measurement agrees with the particle
			mParticles[i].SetWeight(mParticles[i].Weight() + log(accR));
		}
		else
		{
			//right lane measurement disagrese with the particle
			mParticles[i].SetWeight(mParticles[i].Weight() + log(fpR));
		}

		//also keep track of the maximum log-likelihood
		if (mParticles[i].Weight() > lambdamax)
		{
			lambdamax = mParticles[i].Weight();
		}
	}

	//now reweight and renormalize particles from log likelihood
	//note: this is better-conditioned numerically than a multiplicative reweighting
	double sumw = 0.0;
	for (i = 0; i < mNumParticles; i++)
	{
		mParticles[i].SetWeight(exp(mParticles[i].Weight() - lambdamax));
		sumw += mParticles[i].Weight();
	}
	//renormalize weights to sum to unity
	for (i = 0; i < mNumParticles; i++)
	{
		mParticles[i].SetWeight(mParticles[i].Weight() / sumw);
	}

	//***END UPDATE***

	//store the timestamp on the particles
	mPFTime = iMobileyeTime;

	//calculate the MMSE estimates for the filter
	ComputeBestEstimate();

	delete [] zparticles;

	return;
}

void PosteriorPoseParticleFilter::UpdateWithJasonLane(double iJasonTime, int iNumSegmentations, double* iJasonBuff, Sensor* iJasonSensor)
{
	/*
	Updates the particle filter with a single Jason roadfinder road and angle measurement

	INPUTS:
		iJasonTime - timestamp of the roadfinder update
		iNumSegmentations - number of road candidates found by Jason's roadfinder algorithm
		iJasonBuff - buffer containing Jason's road candidates
		iJasonSensor - sensor structure telling how the camera is oriented on the car

	OUTPUTS:
		none.  Reweights the particles according to Jason's road finder
	*/

	if (mIsInitialized == false || mRoadGraph == NULL)
	{
		//can't update until ppose is initialized and has a road graph
		return;
	}

	//***VALIDATE MEASUREMENTS***

	int i;
	int j;
	int k;
	int nj = iNumSegmentations;

	//first determine which of jason's segmentations will be considered
	bool* use = new bool[nj];
	int nused = 0;
	for (i = 0; i < nj; i++)
	{
		use[i] = false;
		if (iJasonBuff[midx(i, 14, nj)] >= JASON_MIN_CONFIDENCE)
		{
			use[i] = true;
			nused++;
		}
	}
	if (nused == 0)
	{
		//no segmentations were good enough to be considered
		#ifdef SE_COMMONDEBUGMSGS
			printf("Jason lane update rejected due to lack of confidence.\n");
		#endif
		delete [] use;
		return;
	}

	//***EXTRACT THE MEASUREMENTS***

	//convert jason's lane lines to a lane center position and lane heading
	double* zjason = new double[2*nj];

	nused = 0;
	for (i = 0; i < nj; i++)
	{
		//find lane boundaries that surround the vehicle

		if (use[i] == false)
		{
			//don't use measurements that are already eliminated
			continue;
		}

		int nb = (int)(iJasonBuff[midx(i, 3, nj)]);
		if (nb < 2)
		{
			//this segmentation found no bounding lanes, just one line (or no lines)
			use[i] = false;
			continue;
		}

		int b1 = -1;
		double db1 = DBL_MAX;
		int b2 = -1;
		double db2 = DBL_MAX;
		double dbtemp;
		for (j = 0; j < nb; j++)
		{
			//find the two closest boundaries and use those as the road measurement
			dbtemp = iJasonBuff[midx(i, 4+j, nj)];
			if (fabs(dbtemp) < fabs(db2))
			{
				//found a new closer boundary
				if (fabs(dbtemp) < fabs(db1))
				{
					//found a new best boundary
					db2 = db1;
					b2 = b1;
					db1 = dbtemp;
					b1 = j + 4;
				}
				else
				{
					//found a new second best boundary
					db2 = dbtemp;
					b2 = j + 4;
				}
			}
		}

		int leftbound = -1;
		int rightbound = -1;
		//check that the two boundaries make a valid lane
		if (db1 > db2)
		{
			leftbound = b1;
			rightbound = b2;
		}
		else
		{
			leftbound = b2;
			rightbound = b1;
		}

		if (leftbound == -1 && rightbound == -1)
		{
			//this segmentation found no bounding lanes
			use[i] = false;
			continue;
		}

		//check to see if the lane found is a good width
		double dlb = iJasonBuff[midx(i, leftbound, nj)];
		double drb = iJasonBuff[midx(i, rightbound, nj)];
		double lwidth = dlb - drb;
		if (lwidth < JASON_MIN_LANEWIDTH)
		{
			//the lane found is too small
			use[i] = false;
			break;
		}

		//construct a lane measurement from the left bound and the right bound
		if (lwidth > JASON_MAX_LANEWIDTH)
		{
			//split the lane into two

			if (fabs(dlb) > fabs(drb))
			{
				//the new lane will be the right boundary and the centerline
				dlb = 0.5*(dlb + drb);
			}
			else
			{
				//the new lane will be the left boundary and the centerline
				drb = 0.5*(dlb + drb);
			}
		}

		//store location of the lane centerline wrt the camera (vehicle coordinates)
		zjason[midx(i, 0, nj)] = 0.5*(dlb + drb);
		//store the heading of the road wrt the camera (vehicle coordinates)
		zjason[midx(i, 1, nj)] = -atan(iJasonBuff[midx(i, 12, nj)]);

		//count this measurement as extracted
		use[i] = true;
		nused++;
	}

	if (nused == 0)
	{
		delete [] use;
		delete [] zjason;

		printf("Jason lane update rejected due to invalid lane widths.\n");
		return;
	}

	//define the measurement covariance matrix for correct detections
	double Rjason[4];
	Rjason[0] = JASON_LANEOFST_VAR;
	Rjason[1] = 0.0;
	Rjason[2] = 0.0;
	Rjason[3] = JASON_HDGOFST_VAR;

	//***HYPOTHESIS TEST THE MEASUREMENT***

	//NOTE: the hypothesis test is approximate, to weed out cases where
	//jason's roadfinder has obviously missed some lines, or locked onto bad lines

	//simultaneously calculate two tests: probability that there's no lane, and innovation test

	//calculate the MMSE road offsets for the particles
	double plnMMSE = 0.0;
	double phdMMSE = 0.0;
	double wMMSE = 0.0;

	double pnoroad = 0.0;
	double* pmcache = new double[7*mNumParticles];

	for (i = 0; i < mNumParticles; i++)
	{
		//compute the ENH of ith particle's camera's location
		double pe = mParticles[i].East();
		double pn = mParticles[i].North();
		double ph = mParticles[i].Heading();
		double cosph = cos(ph);
		double sinph = sin(ph);
		pe += cosph*iJasonSensor->SensorX - sinph*iJasonSensor->SensorY;
		pn += sinph*iJasonSensor->SensorX + cosph*iJasonSensor->SensorY;
		ph += iJasonSensor->SensorYaw;
		//note: the camera is actually looking at a portion of the road in front of it
		pe += cos(ph)*JASON_CAMVIEWDIST;
		pn += sin(ph)*JASON_CAMVIEWDIST;

		//find which partition the particle's viewpoint is closest to
		RoadPartition* rpcenter = mRoadGraph->ClosestPartition(pe, pn, mRoadLocation);

		switch (rpcenter->PartitionType())
		{
		case RP_LANE:
		case RP_INTERCONNECT:
			{
				//lanes and interconnects can be correctly detected by the roadfinder

				switch (rpcenter->FitType())
				{
				case RP_LINE:
					//calculate expected road offsets for this particle
					double pln;
					double phd;
					rpcenter->LaneOffsets(&pln, &phd, pe, pn, ph);

					//cache the closest lane measurements for later
					pmcache[midx(i, 0, mNumParticles)] = phd;
					pmcache[midx(i, 1, mNumParticles)] = pln;

					//use this measurement to contribute to the MMSE
					wMMSE += mParticles[i].Weight();
					plnMMSE += mParticles[i].Weight() * pln;
					phdMMSE += mParticles[i].Weight() * phd;

					RoadPartition* rpbest;

					//determine whether there is a left lane
					if (rpcenter->IsInSameDirection(pe, pn, ph) == true)
					{
						rpbest = rpcenter->LeftLanePartition(pe, pn);
					}
					else
					{
						rpbest = rpcenter->RightLanePartition(pe, pn);
					}

					if (rpbest != NULL)
					{
						//a left lane exists- set distance to it
						rpbest->LaneOffsets(&pln, &phd, pe, pn, ph);
						pmcache[midx(i, 2, mNumParticles)] = pln;
						//also set distance to left+center
						pmcache[midx(i, 4, mNumParticles)] = 0.5 * (pmcache[midx(i, 1, mNumParticles)] + pmcache[midx(i, 2, mNumParticles)]);
					}
					else
					{
						//mark the left lane with garbage values
						pmcache[midx(i, 2, mNumParticles)] = DBL_MAX;
						//also mark any combinations including left lane as garbage
						pmcache[midx(i, 4, mNumParticles)] = DBL_MAX;
						pmcache[midx(i, 6, mNumParticles)] = DBL_MAX;
					}

					//determine whether there is a right lane
					if (rpcenter->IsInSameDirection(pe, pn, ph) == true)
					{
						rpbest = rpcenter->RightLanePartition(pe, pn);
					}
					else
					{
						rpbest = rpcenter->LeftLanePartition(pe, pn);
					}

					if (rpbest != NULL)
					{
						//a right lane exists- set distance to it
						rpbest->LaneOffsets(&pln, &phd, pe, pn, ph);
						pmcache[midx(i, 3, mNumParticles)] = pln;
						//also set distance to right+center
						pmcache[midx(i, 5, mNumParticles)] = 0.5 * (pmcache[midx(i, 1, mNumParticles)] + pmcache[midx(i, 3, mNumParticles)]);
					}
					else
					{
						//mark the right lane with garbage values
						pmcache[midx(i, 3, mNumParticles)] = DBL_MAX;
						//also mark any combinations including right lane as garbage
						pmcache[midx(i, 5, mNumParticles)] = DBL_MAX;
						pmcache[midx(i, 6, mNumParticles)] = DBL_MAX;
					}

					if (pmcache[midx(i, 2, mNumParticles)] != DBL_MAX && pmcache[midx(i, 3, mNumParticles)] != DBL_MAX)
					{
						//set distance to left+right+center if all are valid
						pmcache[midx(i, 6, mNumParticles)] = 0.5 * (pmcache[midx(i, 2, mNumParticles)] + pmcache[midx(i, 3, mNumParticles)]);
					}

					break;

				default:
					//any lane lines found in non-road fit types will be false positives

					//this particle contributes to the fp probability
					pnoroad += mParticles[i].Weight();

					//cache the closest lane measurements as garbage values
					pmcache[midx(i, 0, mNumParticles)] = DBL_MAX;
					pmcache[midx(i, 1, mNumParticles)] = DBL_MAX;
					//and mark that left and right lanes can't exist either
					pmcache[midx(i, 2, mNumParticles)] = DBL_MAX;
					pmcache[midx(i, 3, mNumParticles)] = DBL_MAX;
					pmcache[midx(i, 4, mNumParticles)] = DBL_MAX;
					pmcache[midx(i, 5, mNumParticles)] = DBL_MAX;
					pmcache[midx(i, 6, mNumParticles)] = DBL_MAX;

					break;
				}
			}

			break;

		default:
			//any lane lines in non-road partition types will be false detections

			{
				//this particle contributes to the fp probability
				pnoroad += mParticles[i].Weight();

				//cache the closest lane measurements as garbage values
				pmcache[midx(i, 0, mNumParticles)] = DBL_MAX;
				pmcache[midx(i, 1, mNumParticles)] = DBL_MAX;
				//and mark that left and right lanes can't exist either
				pmcache[midx(i, 2, mNumParticles)] = DBL_MAX;
				pmcache[midx(i, 3, mNumParticles)] = DBL_MAX;
				pmcache[midx(i, 4, mNumParticles)] = DBL_MAX;
				pmcache[midx(i, 5, mNumParticles)] = DBL_MAX;
				pmcache[midx(i, 6, mNumParticles)] = DBL_MAX;
			}

			break;
		}
	}

	if (pnoroad > JASON_NOROAD_THRESH)
	{
		//probability of a false positive is too high to apply update
		printf("Jason lane update rejected as a false positive.\n");
		delete [] pmcache;
		delete [] zjason;
		delete [] use;
		return;
	}
	if (fabs(wMMSE) == 0.0)
	{
		//all particles agree that no lane exists
		printf("Jason lane update rejected as a false positive.\n");
		delete [] pmcache;
		delete [] zjason;
		delete [] use;
		return;
	}

	plnMMSE /= wMMSE;
	phdMMSE /= wMMSE;
	//plnMMSE and phdMMSE now contain the MMSE road offsets

	//compute measurement variance
	double wHPHt = 0.0;
	double HPHt[4] = {0.0, 0.0, 0.0, 0.0};
	for (i = 0; i < mNumParticles; i++)
	{
		//calculate the expected measurement from each particle to compute measurement variance

		//pull expected road offsets for this particle
		double pln = pmcache[midx(i, 1, mNumParticles)];
		double phd = pmcache[midx(i, 0, mNumParticles)];
		if (pln == DBL_MAX && phd == DBL_MAX)
		{
			//this particle doesn't see any lanes
			continue;
		}

		//calculate deviations from the MMSE estimate
		double wt = mParticles[i].Weight();
		double dln = pln - plnMMSE;
		double dhd = UnwrapAngle(phd - phdMMSE);
		wHPHt += wt;
		//use deviations to calculate measurement covariance
		HPHt[midx(0, 0, 2)] += wt*dln*dln;
		HPHt[midx(0, 1, 2)] += wt*dln*dhd;
		HPHt[midx(1, 1, 2)] += wt*dhd*dhd;
	}
	HPHt[midx(1, 0, 2)] = HPHt[midx(0, 1, 2)];
	for (i = 0; i < 2; i++)
	{
		for (j = 0; j < 2; j++)
		{
			HPHt[midx(i, j, 2)] /= wHPHt;
		}
	}

	//compute the innovation covariance matrix
	double S[4];
	for (i = 0; i < 2; i++)
	{
		for (j = 0; j < 2; j++)
		{
			S[midx(i, j, 2)] = HPHt[midx(i, j, 2)] + Rjason[midx(i, j, 2)];
		}
	}

	//invert the innovation covariance matrix
	double detS = S[midx(0, 0, 2)]*S[midx(1, 1, 2)] - S[midx(0, 1, 2)]*S[midx(1, 0, 2)];
	if (fabs(detS) == 0.0)
	{
		//have to reject the measurement if the matrix is singular
		printf("Jason lane update rejected due to singular S matrix.\n");
		delete [] pmcache;
		delete [] zjason;
		delete [] use;
		return;
	}

	double invS[4];
	invS[midx(0, 0, 2)] = S[midx(1, 1, 2)]/detS;
	invS[midx(1, 1, 2)] = S[midx(0, 0, 2)]/detS;
	invS[midx(0, 1, 2)] = -S[midx(0, 1, 2)]/detS;
	invS[midx(1, 0, 2)] = -S[midx(1, 0, 2)]/detS;

	//select the maximum likelihood measurement
	double nu[2];
	double chi2S;
	double lambdabest = -DBL_MAX;
	int ibest = -1;

	double lambdahfact = 1.0 / sqrt(TWOPI * JASON_HDGOFST_VAR);
	//model the variance of the lane measurement as increasing linearly with the number of lanes combined
	double R1 = JASON_LANEOFST_VAR;
	double R2 = 2.0*R1;
	double R3 = 3.0*R1;
	double lambdal1fact = 1.0 / sqrt(TWOPI * R1);
	double lambdal2fact = 1.0 / sqrt(TWOPI * R2);
	double lambdal3fact = 1.0 / sqrt(TWOPI * R3);

	for (i = 0; i < nj; i++)
	{
		//compute the likelihood for each measurement, and keep the most likely candidate
		//also compute the approximate innovation statistic nu'*invS*nu from the MMSE estimate

		if (use[i] == false)
		{
			//don't consider measurements that have already been eliminated
			continue;
		}

		//construct the measurement: location of lane center and road heading
		double zlane = zjason[midx(i, 0, nj)];
		double zhdg = zjason[midx(i, 1, nj)];

		//calculate the innovation
		nu[0] = zlane - plnMMSE;
		nu[1] = zhdg - phdMMSE;
		//calculate the innovation statistic for this measurement
		chi2S = 0.0;
		for (j = 0; j < 2; j++)
		{
			for (k = 0; k < 2; k++)
			{
				chi2S += nu[j] * invS[midx(j, k, 2)] * nu[k];
			}
		}

		if (chi2S > JASON_LANE_HYPOTEST)
		{
			//this road measurement doesn't pass the loose chi2 gating test
			continue;
		}

		//if the code gets here, this measurement is validated: compute its likelihood
		double lambda = 0.0;
		for (j = 0; j < mNumParticles; j++)
		{
			//extract the measurements for this particle from the cache
			//lane heading wrt camera
			double zbarH = pmcache[midx(j, 0, mNumParticles)];
			//center, left, right lane offsets wrt camera
			double zbarC = pmcache[midx(j, 1, mNumParticles)];
			double zbarL = pmcache[midx(j, 2, mNumParticles)];
			double zbarR = pmcache[midx(j, 3, mNumParticles)];
			//center+left, center+right lane offsets wrt camera
			double zbarCL = pmcache[midx(j, 4, mNumParticles)];
			double zbarCR = pmcache[midx(j, 5, mNumParticles)];
			//center+left+right lane offset wrt camera
			double zbarCLR = pmcache[midx(j, 6, mNumParticles)];

			//calculate the measurement likelihood
			//model as gaussian in heading multiplying a multimodal gaussian in lane center location
			double lambdaH = exp(-0.5*pow(zhdg - zbarH, 2.0) / JASON_HDGOFST_VAR) * lambdahfact;
			double lambdaL = 0.0;
			//accumulate the sum of gaussians measurement likelihood for all existing lanes
			if (zbarC != DBL_MAX)
			{
				lambdaL += JASON_CORRECTLANE_PROB * exp(-0.5*pow(zlane - zbarC, 2.0) / R1) * lambdal1fact;
			}
			if (zbarL != DBL_MAX)
			{
				lambdaL += JASON_INCORRECTLANE_PROB * exp(-0.5*pow(zlane - zbarL, 2.0) / R1) * lambdal1fact;
			}
			if (zbarR != DBL_MAX)
			{
				lambdaL += JASON_INCORRECTLANE_PROB * exp(-0.5*pow(zlane - zbarR, 2.0) / R1) * lambdal1fact;
			}
			if (zbarCL != DBL_MAX)
			{
				lambdaL += JASON_TWOLANE_PROB * exp(-0.5*pow(zlane - zbarCL, 2.0) / R2) * lambdal2fact;
			}
			if (zbarCR != DBL_MAX)
			{
				lambdaL += JASON_TWOLANE_PROB * exp(-0.5*pow(zlane - zbarCL, 2.0) / R2) * lambdal2fact;
			}
			if (zbarCLR != DBL_MAX)
			{
				lambdaL += JASON_THREELANE_PROB * exp(-0.5*pow(zlane - zbarCLR, 2.0) / R3) * lambdal3fact;
			}
			//also accumulate the uniform probability of a false positive measurement
			lambdaL += JASON_FPLANE_PROB * JASON_FPLIKELIHOOD;

			//accumulate this particle's contribution to the measurement likelihood
			lambda += mParticles[i].Weight() * (lambdaH*lambdaL);
		}

		if (lambda > lambdabest)
		{
			//found a more likely lane measurement
			lambdabest = lambda;
			ibest = i;
		}
	}

	if (ibest == -1)
	{
		//all the measurements were rejected in the chi2 gate
		printf("Jason lane update rejected due to Chi2 test.\n");
		delete [] pmcache;
		delete [] zjason;
		delete [] use;
		return;
	}

	//***APPLY MEASUREMENT UPDATE***
	
	//extract the most likely measurement, the one that will be applied
	double zlane = zjason[midx(ibest, 0, nj)];
	double zhdg = zjason[midx(ibest, 1, nj)];

	//once the filter decides to apply the update, move to a more complicated measurement model
	//for each particle, use a multimodal gaussian with centers at:
	//1. heading wrt closest lane
	//2. dist to closest lane
	//3. dist to left lane
	//4. dist to right lane
	//5. dist to left and center lane combined
	//6. dist to right and center lane combined
	//7. dist to all three lanes combined
	//also model a uniform fp probability

	double lambdamax = -DBL_MAX;
	for (i = 0; i < mNumParticles; i++)
	{
		//extract the measurements for this particle from the cache
		//lane heading wrt camera
		double zbarH = pmcache[midx(i, 0, mNumParticles)];
		//center, left, right lane offsets wrt camera
		double zbarC = pmcache[midx(i, 1, mNumParticles)];
		double zbarL = pmcache[midx(i, 2, mNumParticles)];
		double zbarR = pmcache[midx(i, 3, mNumParticles)];
		//center+left, center+right lane offsets wrt camera
		double zbarCL = pmcache[midx(i, 4, mNumParticles)];
		double zbarCR = pmcache[midx(i, 5, mNumParticles)];
		//center+left+right lane offset wrt camera
		double zbarCLR = pmcache[midx(i, 6, mNumParticles)];

		//calculate the measurement likelihood
		//model as gaussian in heading multiplying a multimodal gaussian in lane center location
		double lambdaH = exp(-0.5*pow(zhdg - zbarH, 2.0) / JASON_HDGOFST_VAR) * lambdahfact;
		double lambdaL = 0.0;
		//accumulate the sum of gaussians measurement likelihood for all existing lanes
		if (zbarC != DBL_MAX)
		{
			lambdaL += JASON_CORRECTLANE_PROB * exp(-0.5*pow(zlane - zbarC, 2.0) / R1) * lambdal1fact;
		}
		if (zbarL != DBL_MAX)
		{
			lambdaL += JASON_INCORRECTLANE_PROB * exp(-0.5*pow(zlane - zbarL, 2.0) / R1) * lambdal1fact;
		}
		if (zbarR != DBL_MAX)
		{
			lambdaL += JASON_INCORRECTLANE_PROB * exp(-0.5*pow(zlane - zbarR, 2.0) / R1) * lambdal1fact;
		}
		if (zbarCL != DBL_MAX)
		{
			lambdaL += JASON_TWOLANE_PROB * exp(-0.5*pow(zlane - zbarCL, 2.0) / R2) * lambdal2fact;
		}
		if (zbarCR != DBL_MAX)
		{
			lambdaL += JASON_TWOLANE_PROB * exp(-0.5*pow(zlane - zbarCL, 2.0) / R2) * lambdal2fact;
		}
		if (zbarCLR != DBL_MAX)
		{
			lambdaL += JASON_THREELANE_PROB * exp(-0.5*pow(zlane - zbarCLR, 2.0) / R3) * lambdal3fact;
		}
		//also accumulate the uniform probability of a false positive measurement
		lambdaL += JASON_FPLANE_PROB * JASON_FPLIKELIHOOD;

		//temporarily overwrite this particle's weight with its log-likelihood weight
		mParticles[i].SetWeight(log(mParticles[i].Weight()) + log(lambdaH) + log(lambdaL));
		//also keep track of the maximum log-likelihood
		if (mParticles[i].Weight() > lambdamax)
		{
			lambdamax = mParticles[i].Weight();
		}
	}

	//now reweight and renormalize particles from log likelihood
	//note: this is better-conditioned numerically than a multiplicative reweighting
	double sumw = 0.0;
	for (i = 0; i < mNumParticles; i++)
	{
		mParticles[i].SetWeight(exp(mParticles[i].Weight() - lambdamax));
		sumw += mParticles[i].Weight();
	}
	//renormalize weights to sum to unity
	for (i = 0; i < mNumParticles; i++)
	{
		mParticles[i].SetWeight(mParticles[i].Weight() / sumw);
	}

	//***END UPDATE***

	//store the timestamp on the particles
	mPFTime = iJasonTime;

	//calculate the MMSE estimates for the filter
	ComputeBestEstimate();

	delete [] pmcache;
	delete [] zjason;
	delete [] use;

	return;
}

void PosteriorPoseParticleFilter::UpdateWithAaronStopline(double iStoplineTime, int iNumStoplines, double* iAaronBuff, Sensor* iAaronSensor)
{
	/*
	Applies one stopline update generated by Aaron's stopline algorithm.

	INPUTS:
		iStoplineTime - timestamp of the stopline update
		iNumStoplines - number of stoplines (number of rows in iAaronBuff)
		iAaronBuff - buffer containing the stopline information
		iStoplineSensor - sensor structure locating the stopline camera on the car

	OUTPUTS:
		none.  Updates the particles with the stopline information.
	*/

	if (mIsInitialized == false || mRoadGraph == NULL)
	{
		//can't update until ppose is initialized and has a road graph
		return;
	}

	int i;
	int j;

	//***VALIDATE MEASUREMENTS***

	int ins = iNumStoplines;

	if (ins == 0)
	{
		//can't update if there are no stoplines
		return;
	}

	//first determine which of the stoplines will be considered
	bool* use = new bool[ins];
	int nused = 0;
	for (i = 0; i < ins; i++)
	{
		use[i] = false;
		if (iAaronBuff[midx(i, 4, ins)] > AARON_MINCONFIDENCE)
		{
			use[i] = true;
			nused++;
		}
	}
	if (nused == 0)
	{
		//no stoplines were good enough to be considered
		#ifdef SE_COMMONDEBUGMSGS
			printf("Aaron stopline update rejected due to lack of confidence.\n");
		#endif
		delete [] use;
		return;
	}

	//space for the measurement and its covariance
	double zstopline;
	double Rstopline = AARON_STOPLINEVAR;
	double detR = AARON_STOPLINEVAR;

	//***HYPOTHESIS TEST THE MEASUREMENTS***

	//first compute the probability that there's no stopline (false positive probability)
	double pwrong = 0.0;
	double* psdcache = new double[mNumParticles];
	RoadPoint* rpbest;
	for (i = 0; i < mNumParticles; i++)
	{
		//compute the ENH of ith particle's camera's location
		double pe = mParticles[i].East();
		double pn = mParticles[i].North();
		double ph = mParticles[i].Heading();
		double cosph = cos(ph);
		double sinph = sin(ph);
		pe += cosph*iAaronSensor->SensorX - sinph*iAaronSensor->SensorY;
		pn += sinph*iAaronSensor->SensorX + cosph*iAaronSensor->SensorY;
		ph += iAaronSensor->SensorYaw;

		//find the distance to the closest stopline for this particle
		rpbest = mRoadGraph->ClosestUpcomingStopline(pe, pn, ph, AARON_MAXVIEWANGLE, false, mRoadLocation);
		if (rpbest != NULL)
		{
			//compute the distance to the nearby stopline
			psdcache[i] = rpbest->GetDistanceToPoint(pe, pn);
		}
		else
		{
			//this particle doesn't see a stopline: it contributes to the fp probability
			psdcache[i] = -1.0;
			pwrong += mParticles[i].Weight();
		}
	}
	if (pwrong > AARON_STOPLINE_MAXFPPROB)
	{
		//probability of no stopline is too high
		printf("Aaron stopline update rejected as a false positive.\n");
		delete [] use;
		delete [] psdcache;
		return;
	}

	//if code gets here, filter is reasonably confident that a stopline exists
	//compute the MMSE distance to stopline
	double wstop = 0.0;
	double sdMMSE = 0.0;
	for (i = 0; i < mNumParticles; i++)
	{
		if (psdcache[i] != -1.0)
		{
			wstop += mParticles[i].Weight();
			sdMMSE += mParticles[i].Weight() * psdcache[i];
		}
	}
	//normalize the MMSE estimate by the total weight of particles that see a stopline
	if (wstop == 0.0)
	{
		//no particles saw a stopline
		printf("Aaron stopline update rejected as false positive.\n");
		delete [] use;
		delete [] psdcache;
		return;
	}
	sdMMSE /= wstop;
	//note: stoplines are projected as distance from the camera, so no correction is needed for camera viewpoint

	//compute measurement variance by computing each particle's distance to a stopline
	wstop = 0.0;
	double HPHt = 0.0;
	for (i = 0; i < mNumParticles; i++)
	{
		//use the expected measurement from each particle to compute measurement variance

		//pull the stopline distance from the cache
		double psd = psdcache[i];
		if (psd == -1.0)
		{
			//this particle isn't near any visible stoplines: ignore it
			continue;
		}

		//calculate deviations from the MMSE estimate
		double wt = mParticles[i].Weight();
		double dsd = psd - sdMMSE;
		//use deviations to calculate measurement covariance
		wstop += wt;
		HPHt += wt*dsd*dsd;
	}
	HPHt /= wstop;

	//compute the innovation covariance matrix
	double S = HPHt + Rstopline;
	double detS = S;
	//invert the innovation covariance matrix
	if (fabs(detS) == 0.0)
	{
		//have to reject the measurement if the matrix is singular
		printf("Aaron stopline update rejected due to singular S matrix.\n");
		delete [] use;
		delete [] psdcache;
		return;
	}
	double invS = 1.0 / S;

	double nu;
	double chi2S;
	double lambdabest = -DBL_MAX;
	int ibest = -1;
	double lfact = 1.0 / sqrt(TWOPI * detR);
	for (i = 0; i < ins; i++)
	{
		//compute the innovation statistic nu'*invS*nu from the MMSE estimate
		//for each stopline measurement, and keep the best candidate

		if (use[i] == false)
		{
			//don't consider measurements that have already been eliminated
			continue;
		}

		//construct the measurement: distance to the stopline
		zstopline = iAaronBuff[midx(i, 3, ins)];

		//calculate the innovation
		nu = zstopline - sdMMSE;
		//calculate the innovation statistic for this measurement
		chi2S = nu*invS*nu;

		if (chi2S > AARON_STOPLINE_HYPOTEST)
		{
			//this stopline doesn't pass the chi2 gating test
			continue;
		}

		//if the code gets here, this measurement is validated: compute its likelihood
		double lambda = 0.0;
		for (j = 0; j < mNumParticles; j++)
		{
			if (psdcache[i] == -1.0)
			{
				lambda += mParticles[i].Weight() * (1.0 - AARON_STOPLINE_ACCURACY)*AARON_STOPLINE_FPLIKELIHOOD;
			}
			else
			{
				double ds = zstopline - psdcache[i];
				lambda += mParticles[i].Weight() * (AARON_STOPLINE_ACCURACY*exp(-0.5*ds*ds/Rstopline) * lfact 
					+ (1.0 - AARON_STOPLINE_ACCURACY)*AARON_STOPLINE_FPLIKELIHOOD);
			}
		}

		if (lambda > lambdabest)
		{
			//found a more likely stopline measurement
			lambdabest = lambda;
			ibest = i;
		}
	}

	if (ibest == -1)
	{
		//all stopline candidates failed the hypo test
		printf("Aaron stopline update rejected for failing hypothesis test.\n");
		delete [] use;
		delete [] psdcache;
		return;
	}

	//extract the best measurement
	zstopline = iAaronBuff[midx(ibest, 3, ins)];

	//***APPLY MEASUREMENT UPDATE***

	double lambda;
	double lambdamax = -DBL_MAX;
	for (i = 0; i < mNumParticles; i++)
	{
		//extract the distance to closest stopline for this particle (-1.0 if no stopline exists)
		double psd = psdcache[i];

		if (psd == -1.0)
		{
			//this particle couldn't see a stopline, so it concludes the stopline was a FP
			lambda = (1.0 - AARON_STOPLINE_ACCURACY)*AARON_STOPLINE_FPLIKELIHOOD;
		}
		else
		{
			//the stopline is theoretically visible from this particle
			lambda = AARON_STOPLINE_ACCURACY * exp(-0.5*(zstopline - psd)*(zstopline - psd)/Rstopline) * lfact 
				+ (1.0 - AARON_STOPLINE_ACCURACY)*AARON_STOPLINE_FPLIKELIHOOD;
		}

		//temporarily overwrite this particle's weight with its log-likelihood weight
		mParticles[i].SetWeight(log(mParticles[i].Weight()) + log(lambda));
		//also keep track of the maximum log-likelihood
		if (mParticles[i].Weight() > lambdamax)
		{
			lambdamax = mParticles[i].Weight();
		}
	}

	//now reweight and renormalize particles from log likelihood
	//note: this is better-conditioned numerically than a multiplicative reweighting
	double sumw = 0.0;
	for (i = 0; i < mNumParticles; i++)
	{
		mParticles[i].SetWeight(exp(mParticles[i].Weight() - lambdamax));
		sumw += mParticles[i].Weight();
	}
	//renormalize weights to sum to unity
	for (i = 0; i < mNumParticles; i++)
	{
		mParticles[i].SetWeight(mParticles[i].Weight() / sumw);
	}

	//***END UPDATE***

	//store the timestamp on the particles
	mPFTime = iStoplineTime;

	//calculate the MMSE estimates for the filter
	ComputeBestEstimate();

	delete [] use;
	delete [] psdcache;

	return;
}

void PosteriorPoseParticleFilter::UpdateWithLocalRoad(double iLocalRoadTime, int iNumDataRows, double* iLocalRoadPacket, int iNumPoints, double* iLocalRoadPoints)
{
	/*
	Stores the most recent LocalRoad model in posterior pose

	INPUTS:
		iLocalRoadTime - timestamp of the local road measurement
		iNumDataRows - number of rows in the header packet
		iLocalRoadPacket - the local road header packet
		iNumPoints - number of road points (all points for the left, center, and right lanes)
		iLocalRoadPoints - the local road points packet

	OUTPUTS:
		none.  Stores the LocalRoad model in the particle filter.
	*/

	if (mLRIsInitialized == false)
	{
		printf("Initializing LocalRoad local road model...\n");
	}

	//set information from the local road header
	mLRTime = iLocalRoadTime;
	mLRModelProb = iLocalRoadPacket[1];
	mLRLeftLaneProb = iLocalRoadPacket[2];
	mLRLeftLaneWidth = iLocalRoadPacket[3];
	mLRLeftLaneWidthVar = iLocalRoadPacket[4];
	mLRCenterLaneProb = iLocalRoadPacket[5];
	mLRCenterLaneWidth = iLocalRoadPacket[6];
	mLRCenterLaneWidthVar = iLocalRoadPacket[7];
	mLRRightLaneProb = iLocalRoadPacket[8];
	mLRRightLaneWidth = iLocalRoadPacket[9];
	mLRRightLaneWidthVar = iLocalRoadPacket[10];

	//populate the number of points
	mLRNumLeftLanePoints = (int) iLocalRoadPacket[11];
	mLRNumCenterLanePoints = (int) iLocalRoadPacket[12];
	mLRNumRightLanePoints = (int) iLocalRoadPacket[13];

	int npL = mLRNumLeftLanePoints;
	int npC = mLRNumCenterLanePoints;
	int npR = mLRNumRightLanePoints;

	//delete old localroad memory and declare new memory for the points
	delete [] mLRLeftLanePoints;
	mLRLeftLanePoints = NULL;
	delete [] mLRLeftLaneVars;
	mLRLeftLaneVars = NULL;
	delete [] mLRCenterLanePoints;
	mLRCenterLanePoints = NULL;
	delete [] mLRCenterLaneVars;
	mLRCenterLaneVars = NULL;
	delete [] mLRRightLanePoints;
	mLRRightLanePoints = NULL;
	delete [] mLRRightLaneVars;
	mLRRightLaneVars = NULL;

	if (npL > 0)
	{
		mLRLeftLanePoints = new double[2*npL];
		mLRLeftLaneVars = new double[npL];
	}
	if (npC > 0)
	{
		mLRCenterLanePoints = new double[2*npC];
		mLRCenterLaneVars = new double[npC];
	}
	if (npR > 0)
	{
		mLRRightLanePoints = new double[2*npR];
		mLRRightLaneVars = new double[npR];
	}

	int i;
	int ofs;

	ofs = 0;
	for (i = 0; i < npL; i++)
	{
		//copy the left lane points from the packet
		mLRLeftLanePoints[midx(i, 0, npL)] = iLocalRoadPoints[midx(i+ofs, 4, iNumPoints)];
		mLRLeftLanePoints[midx(i, 1, npL)] = iLocalRoadPoints[midx(i+ofs, 5, iNumPoints)];
		mLRLeftLaneVars[i] = iLocalRoadPoints[midx(i+ofs, 6, iNumPoints)];
	}

	ofs = npL;
	for (i = 0; i < npC; i++)
	{
		//copy the center lane points from the packet
		mLRCenterLanePoints[midx(i, 0, npC)] = iLocalRoadPoints[midx(i+ofs, 4, iNumPoints)];
		mLRCenterLanePoints[midx(i, 1, npC)] = iLocalRoadPoints[midx(i+ofs, 5, iNumPoints)];
		mLRCenterLaneVars[i] = iLocalRoadPoints[midx(i+ofs, 6, iNumPoints)];
	}

	ofs = npL + npC;
	for (i = 0; i < npL; i++)
	{
		//copy the left lane points from the packet
		mLRRightLanePoints[midx(i, 0, npR)] = iLocalRoadPoints[midx(i+ofs, 4, iNumPoints)];
		mLRRightLanePoints[midx(i, 1, npR)] = iLocalRoadPoints[midx(i+ofs, 5, iNumPoints)];
		mLRRightLaneVars[i] = iLocalRoadPoints[midx(i+ofs, 6, iNumPoints)];
	}

	if (mLRIsInitialized == false)
	{
		mLRIsInitialized = true;
		printf("LocalRoad local road model initialized.\n");
	}

	return;
}

void PosteriorPoseParticleFilter::PrintParticles(FILE* iParticleFile)
{
	/*
	Prints all the particles to a file at the current particle filter time.

	INPUTS:
		iParticleFile - the open file to hold the printed data.

	OUTPUTS:
		none.  Prints to the desired file.
	*/

	if (mIsInitialized == false)
	{
		//don't print anything if the filter isn't initialized
		return;
	}

	int i;
	for (i = 0; i < mNumParticles; i++)
	{
		//find this particle's closest partition
		RoadPartition* rpcur = mRoadGraph->ClosestPartition(mAIParticles[i].East(), mAIParticles[i].North(), mAIRoadLocation, PP_OFFRNDFDIST);
		//print each particle separately
		fprintf(iParticleFile, "%.12lg,%d,%d,%.12lg,%.12lg,%.12lg,%.12lg,%.12lg,%.12lg,%s\n", mAITime, i, mNumParticles, mAIParticles[i].Weight(), 
			mAIParticles[i].East(), mAIParticles[i].North(), mAIParticles[i].Heading(), 
			mAIParticles[i].GPSBiasEast(), mAIParticles[i].GPSBiasNorth(), rpcur->PartitionID());
	}

	return;
}

void PosteriorPoseParticleFilter::SetArbiterMessage(UnmanagedArbiterPositionMessage* oArbiterMsg)
{
	/*
	Sets the arbiter message with the most current posterior pose information

	INPUTS:
		oArbiterMsg - pointer to an arbiter message structure that will contain the ppose information

	OUTPUTS:
		oArbiterMsg - will contain the arbiter ppose message on exit
	*/

	//set the output message for the arbiter
	oArbiterMsg->timestamp = mAITime;
	oArbiterMsg->isSparseWaypoints = mAISparseWaypoints;
	oArbiterMsg->eastMMSE = mAIEast;
	oArbiterMsg->northMMSE = mAINorth;
	oArbiterMsg->headingMMSE = mAIHeading;
	oArbiterMsg->ENCovariance[midx(0, 0, 2)] = mAICovariance[midx(0, 0, PF_NUMSTATES)];
	oArbiterMsg->ENCovariance[midx(1, 0, 2)] = mAICovariance[midx(1, 0, PF_NUMSTATES)];
	oArbiterMsg->ENCovariance[midx(0, 1, 2)] = mAICovariance[midx(0, 1, PF_NUMSTATES)];
	oArbiterMsg->ENCovariance[midx(1, 1, 2)] = mAICovariance[midx(1, 1, PF_NUMSTATES)];

	int i;

	oArbiterMsg->numberPartitions = mAINumLikelyPartitions;
	for (i = 0; i < mAINumLikelyPartitions; i++)
	{
		oArbiterMsg->partitions[i] = mAILikelyPartitions[i];
	}

	return;
}

void PosteriorPoseParticleFilter::SetOperationalMessage(UnmanagedOperationalMessage* oOperationalMsg)
{
	/*
	Sets the message to the operational layer with the most current posterior pose information

	INPUTS:
		oOperationalMsg - pointer to an operational message structure that will contain the ppose information

	OUTPUTS:
		oOperationalMsg - will contain the operational ppose message on exit
	*/

	//set the output message data for the operational layer
	oOperationalMsg->timestamp = mAITime;
	oOperationalMsg->isModelValid = mAIRoadModelIsValid;

	oOperationalMsg->stopLineExists = mAIStoplineExists;
	oOperationalMsg->distToStopline = mAIDistanceToStopline;
	oOperationalMsg->distToStoplineVar = mAIDistanceToStoplineVar;

	oOperationalMsg->centerLaneExists = mAICenterLaneExists;
	oOperationalMsg->leftLaneExists = mAILeftLaneExists;
	oOperationalMsg->rightLaneExists = mAIRightLaneExists;

	oOperationalMsg->centerLaneID = string(mAICenterLaneID);
	oOperationalMsg->leftLaneID = string(mAILeftLaneID);
	oOperationalMsg->rightLaneID = string(mAIRightLaneID);

	oOperationalMsg->centerLaneCenter = mAIDistToCenterLane;
	oOperationalMsg->centerLaneCenterVar = mAIDistToCenterLaneVar;
	oOperationalMsg->leftLaneCenter = mAIDistToLeftLane;
	oOperationalMsg->leftLaneCenterVar = mAIDistToLeftLaneVar;
	oOperationalMsg->rightLaneCenter = mAIDistToRightLane;
	oOperationalMsg->rightLaneCenterVar = mAIDistToRightLaneVar;

	oOperationalMsg->centerLaneWidth = mAICenterLaneWidth;
	oOperationalMsg->centerLaneWidthVar = mAICenterLaneWidthVar;
	oOperationalMsg->leftLaneWidth = mAILeftLaneWidth;
	oOperationalMsg->leftLaneWidthVar = mAILeftLaneWidthVar;
	oOperationalMsg->rightLaneWidth = mAIRightLaneWidth;
	oOperationalMsg->rightLaneWidthVar = mAIRightLaneWidthVar;

	oOperationalMsg->roadHeading = mAIRoadHeading;
	oOperationalMsg->roadHeadingVar = mAIRoadHeadingVar;
	oOperationalMsg->roadCurvature = mAIRoadCurvature;
	oOperationalMsg->roadCurvatureVar = mAIRoadCurvatureVar;

	return;
}

void PosteriorPoseParticleFilter::SetLocalRoadMessage(LocalRoadModelEstimateMsg* oLocalRoadMessage)
{
	/*
	Sets a given message with current local road information from LocalRoad
	for transmit to the operational layer.

	INPUTS:
		oLocalRoadMessage - the message that will be populated with data.

	OUTPUTS:
		oLocalRoadMessage - the message that will be populated with data.
	*/

	oLocalRoadMessage->timestamp = mAITime;

	//set the output message data for the operational layer
	oLocalRoadMessage->probabilityRoadModelValid = (float) mAILRModelProb;

	oLocalRoadMessage->probabilityLeftLaneExists = (float) mAILRLeftLaneProb;
	oLocalRoadMessage->laneWidthLeft = (float) mAILRLeftLaneWidth;
	oLocalRoadMessage->laneWidthLeftVariance = (float) mAILRLeftLaneWidthVar;

	oLocalRoadMessage->probabilityCenterLaneExists = (float) mAILRCenterLaneProb;
	oLocalRoadMessage->laneWidthCenter = (float) mAILRCenterLaneWidth;
	oLocalRoadMessage->laneWidthCenterVariance = (float) mAILRCenterLaneWidthVar;

	oLocalRoadMessage->probabilityRightLaneExists = (float) mAILRRightLaneProb;
	oLocalRoadMessage->laneWidthRight = (float) mAILRRightLaneWidth;
	oLocalRoadMessage->laneWidthRightVariance = (float) mAILRRightLaneWidthVar;

	oLocalRoadMessage->numPointsLeft = mAILRNumLeftLanePoints;
	oLocalRoadMessage->numPointsCenter = mAILRNumCenterLanePoints;
	oLocalRoadMessage->numPointsRight = mAILRNumRightLanePoints;

	int i;
	int np;

	np = mAILRNumLeftLanePoints;
	if (np > MAX_LANE_POINTS)
	{
		printf("Warning: truncating left lane points of LocalRoad model.\n");
	}
	for (i = 0; i < np; i++)
	{
		oLocalRoadMessage->LanePointsLeft[i] = LocalRoadModelLanePoint(mAILRLeftLanePoints[midx(i, 0, mAILRNumLeftLanePoints)], 
			mAILRLeftLanePoints[midx(i, 1, mAILRNumLeftLanePoints)], mAILRLeftLaneVars[i]);
	}

	np = mAILRNumCenterLanePoints;
	if (np > MAX_LANE_POINTS)
	{
		printf("Warning: truncating center lane points of LocalRoad model.\n");
	}
	for (i = 0; i < np; i++)
	{
		oLocalRoadMessage->LanePointsCenter[i] = LocalRoadModelLanePoint(mAILRCenterLanePoints[midx(i, 0, mAILRNumCenterLanePoints)], 
			mAILRCenterLanePoints[midx(i, 1, mAILRNumCenterLanePoints)], mAILRCenterLaneVars[i]);
	}

	np = mAILRNumRightLanePoints;
	if (np > MAX_LANE_POINTS)
	{
		printf("Warning: truncating right lane points of LocalRoad model.\n");
	}
	for (i = 0; i < np; i++)
	{
		oLocalRoadMessage->LanePointsRight[i] = LocalRoadModelLanePoint(mAILRRightLanePoints[midx(i, 0, mAILRNumRightLanePoints)], 
			mAILRRightLanePoints[midx(i, 1, mAILRNumRightLanePoints)], mAILRRightLaneVars[i]);
	}

	return;
}

void PosteriorPoseParticleFilter::DisplayArbiterMessage(void)
{
	/*
	Prints the particle filter's state to the screen (for debug)

	INPUTS:
		none.

	OUTPUTS:
		none.  Prints the state to the screen
	*/

	int i;
	int ibest = 0;
	double cbest = -DBL_MAX;

	for (i = 0; i < mAINumLikelyPartitions; i++)
	{
		if (mAILikelyPartitions[i].confidence > cbest)
		{
			ibest = i;
			cbest = mAILikelyPartitions[i].confidence;
		}
	}

	//print the time computed for the AI (the most recent time)
	printf("PosteriorPose time: %lg\n", mAITime);

	//print the state and state standard devs
	printf("Arbiter Message:\n");
	printf("State: %lgE, %lgN, %lgH\n", mAIEast, mAINorth, mAIHeading*180.0/PI);
	printf("StdDev: %lgE, %lgN, %lgH\n", sqrt(mAICovariance[midx(0, 0, PF_NUMSTATES)]), sqrt(mAICovariance[midx(1, 1, PF_NUMSTATES)]), sqrt(mAICovariance[midx(2, 2, PF_NUMSTATES)])*180.0/PI);
	printf("Partition: %s, Probability: %lg\n", mAILikelyPartitions[ibest].id.c_str(), mAILikelyPartitions[ibest].confidence);
	printf("\n");

	return;
}

void PosteriorPoseParticleFilter::DisplayOperationalMessage(void)
{
	/*
	Prints the operational message to the debug window.
	
	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	printf("Operational Message:\n");
	if (mAIRoadModelIsValid == true)
	{
		printf("Road heading: %lg, StdDev: %lg\n", atan(mAIRoadHeading)*180.0/PI, atan(sqrt(mAIRoadHeadingVar))*180.0/PI);
		printf("Road curvature: %lg, StdDev: %lg\n", mAIRoadCurvature, sqrt(mAIRoadCurvatureVar));
	}
	else
	{
		printf("Road model invalid.\n");
	}

	if (mAICenterLaneExists == true)
	{
		printf("Center lane %s: %lg, StdDev: %lg, Width: %lg\n", mAICenterLaneID, mAIDistToCenterLane, sqrt(mAIDistToCenterLaneVar), mAICenterLaneWidth);
	}
	else
	{
		printf("Center lane invalid.\n");
	}

	if (mAILeftLaneExists == true)
	{
		printf("Left lane %s: %lg, StdDev: %lg, Width: %lg\n", mAILeftLaneID, mAIDistToLeftLane, sqrt(mAIDistToLeftLaneVar), mAILeftLaneWidth);
	}
	else
	{
		printf("Left lane invalid.\n");
	}

	if (mAIRightLaneExists == true)
	{
		printf("Right lane %s: %lg, StdDev: %lg, Width: %lg\n", mAIRightLaneID, mAIDistToRightLane, sqrt(mAIDistToRightLaneVar), mAIRightLaneWidth);
	}
	else
	{
		printf("Right lane invalid.\n");
	}

	if (mAIStoplineExists == true)
	{
		printf("Distance to stopline: %lg, StdDev: %lg\n", mAIDistanceToStopline, sqrt(mAIDistanceToStoplineVar));
	}

	printf("\n");

	return;
}

void PosteriorPoseParticleFilter::SetViewerMessage(SceneEstimatorParticlePointsMsg *oViewerMessage)
{
	/*
	Sets the particles to a viewer message for transmission to a viewer program

	INPUTS:
		oViewerMessage - a scene estimator viewer message

	OUTPUTS:
		oViewerMessage - populated with the particles for transmission.
	*/

	int i;

	oViewerMessage->time = mAITime;
	oViewerMessage->numPoints = mNumParticles;

	for (i = 0; i < mNumParticles; i++)
	{
		oViewerMessage->points[i].weight = mAIParticles[i].Weight();
		oViewerMessage->points[i].x = mAIParticles[i].East();
		oViewerMessage->points[i].y = mAIParticles[i].North();
	}

	return;
}

void PosteriorPoseParticleFilter::GetPosteriorPosePositionForTransmit(PosteriorPosePosition& oPosteriorPose)
{
	/*
	Gets the current posterior pose position from the temporary particles set for transmit
	and stores it in a posterior pose position structure.  Used to package the current
	ppose solution to send to track generator.

	INPUTS:
		oPosteriorPose - structure passed by reference; will contain the current posterior
			pose solution on return.

	OUTPUTS:
		oPosteriorPose - will contain the current posterior pose solution on return.
	*/

	if (mIsInitialized == false)
	{
		//do not send a valid posterior pose position if not initialized
		oPosteriorPose.IsValid = false;
		return;
	}

	oPosteriorPose.PosteriorPoseTime = mAITime;
	oPosteriorPose.IsValid = true;
	oPosteriorPose.EastMMSE = mAIEast;
	oPosteriorPose.NorthMMSE = mAINorth;
	oPosteriorPose.HeadingMMSE = mAIHeading;

	int i;
	int j;
	int idx[3] = {0, 1, 2};
	for (i = 0; i < PPP_NUMSTATES; i++)
	{
		for (j = 0; j < PPP_NUMSTATES; j++)
		{
			oPosteriorPose.CovarianceMMSE[midx(i, j, PPP_NUMSTATES)] = mAICovariance[midx(idx[i], idx[j], PF_NUMSTATES)];
		}
	}

	return;
}
