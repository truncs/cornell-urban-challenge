#include "Track.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

Track::Track(int iID)
{
	/*
	Default track constructor.  Initializes a new track with empty values
	and a valid ID.

	INPUTS:
		iID - a valid and unique track ID that will be assigned to this track.

	OUTPUTS:
		none.
	*/

	int i;
	int j;

	//assign ID and flags
	mID = iID;
	mStatusFlag = T_STATUSACTIVE;
	mTimeSinceLastUpdate = 0.0;
	mAbsoluteTimeSinceLastUpdate = 0.0;
	mTimeSinceCreation = 0.0;
	mNumMeasurements = 0;
	mClosestPartition = NULL;
	mIsInitialized = false;

	//assign states
	mX = 0.0;
	mY = 0.0;
	mOrientation = 0.0;
	mSpeed = 0.0;
	mHeading = 0.0;

	for (i = 0; i < T_NUMSTATES; i++)
	{
		for (j = 0; j < T_NUMSTATES; j++)
		{
			mCovariance[midx(i, j, T_NUMSTATES)] = 0.0;
		}
	}

	//initialize probability flags
	mCarProbability = TG_INITCARPROB;
	mStoppedProbability = TG_INITSTOPPROB;

	//initialize track points
	mNumTrackPoints = 0;
	mTrackPoints = NULL;

	//initialize track measurement
	mtpmIsCurrent = false;
	mtpmBCCW = 0.0;
	mtpmBCW = 0.0;
	mtpmRMIN = 0.0;
	memset(mtpmR, 0x00, T_NUMTPMEAS*T_NUMTPMEAS*sizeof(double));
	memset(mtpmPHt, 0x00, T_NUMSTATES*T_NUMTPMEAS*sizeof(double));

	//initialize auxiliary velocity estimator
	memset(maveState, 0x00, T_NUMAVESTATES*sizeof(double));
	memset(maveCovariance, 0x00, T_NUMAVESTATES*T_NUMAVESTATES*sizeof(double));

	PrevTrack = NULL;
	NextTrack = NULL;

	return;
}

Track::Track(Track* iTrackToCopy)
{
	/*
	Copy constructor for a track.  Sets this track to be exactly
	the same as iTrackToCopy.

	INPUTS:
		iTrackToCopy - the track that will be copied into this one

	OUTPUTS:
		none.
	*/

	int i;
	int j;

	mID = iTrackToCopy->ID();
	mStatusFlag = iTrackToCopy->StatusFlag();
	mTimeSinceLastUpdate = iTrackToCopy->TimeSinceLastUpdate();
	mAbsoluteTimeSinceLastUpdate = iTrackToCopy->AbsoluteTimeSinceLastUpdate();
	mTimeSinceCreation = iTrackToCopy->TimeSinceCreation();
	mNumMeasurements = iTrackToCopy->NumMeasurements();
	mClosestPartition = iTrackToCopy->ClosestPartition();
	mIsInitialized = iTrackToCopy->IsInitialized();

	mX = iTrackToCopy->X();
	mY = iTrackToCopy->Y();
	mOrientation = iTrackToCopy->Orientation();
	mSpeed = iTrackToCopy->Speed();
	mHeading = iTrackToCopy->Heading();

	for (i = 0; i < T_NUMSTATES; i++)
	{
		for (j = 0; j < T_NUMSTATES; j++)
		{
			mCovariance[midx(i, j, T_NUMSTATES)] = iTrackToCopy->Covariance(i, j);
		}
	}

	mCarProbability = iTrackToCopy->CarProbability();
	mStoppedProbability = iTrackToCopy->StoppedProbability();

	mNumTrackPoints = iTrackToCopy->NumTrackPoints();
	mTrackPoints = NULL;
	if (mNumTrackPoints > 0)
	{
		mTrackPoints = new double[2*mNumTrackPoints];
		for (i = 0; i < mNumTrackPoints; i++)
		{
			mTrackPoints[midx(i, 0, mNumTrackPoints)] = iTrackToCopy->TrackPoints(i, 0);
			mTrackPoints[midx(i, 1, mNumTrackPoints)] = iTrackToCopy->TrackPoints(i, 1);
		}
	}

	mtpmIsCurrent = iTrackToCopy->tpmIsCurrent();
	mtpmBCCW = iTrackToCopy->tpmBCCW();
	mtpmBCW = iTrackToCopy->tpmBCW();
	mtpmRMIN = iTrackToCopy->tpmRMIN();
	for (i = 0; i < T_NUMTPMEAS; i++)
	{
		for (j = 0; j < T_NUMTPMEAS; j++)
		{
			mtpmR[midx(i, j, T_NUMTPMEAS)] = iTrackToCopy->tpmR(i, j);
		}
	}
	for (i = 0; i < T_NUMSTATES; i++)
	{
		for (j = 0; j < T_NUMTPMEAS; j++)
		{
			mtpmPHt[midx(i, j, T_NUMSTATES)] = iTrackToCopy->tpmPHt(i, j);
		}
	}

	maveState[0] = iTrackToCopy->aveX();
	maveState[1] = iTrackToCopy->aveY();
	maveState[2] = iTrackToCopy->aveSpeed();
	maveState[3] = iTrackToCopy->aveHeading();
	for (i = 0; i < T_NUMAVESTATES; i++)
	{
		for (j = 0; j < T_NUMAVESTATES; j++)
		{
			maveCovariance[midx(i, j, T_NUMAVESTATES)] = iTrackToCopy->aveCovariance(i, j);
		}
	}

	PrevTrack = iTrackToCopy->PrevTrack;
	NextTrack = iTrackToCopy->NextTrack;

	return;
}

Track::~Track()
{
	/*
	Track destructor.  Frees memory allocated in the track.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	//mark the target as deleted, just to be obvious
	mStatusFlag = T_STATUSDELETED;

	//delete memory stored in track points
	mNumTrackPoints = 0;
	delete [] mTrackPoints;
	mTrackPoints = NULL;

	return;
}

void Track::MarkForOcclusion(int iOcclusionType)
{
	/*
	Marks a track either as occluded or not occluded.

	INPUTS:
		iOcclusionType - type of occlusion; see Track.h for values

	OUTPUTS:
		none.
	*/

	if (mStatusFlag == T_STATUSDELETED)
	{
		//do not allow deleted tracks to be marked as occluded
		return;
	}

	switch (iOcclusionType)
	{
	case T_OCCLUSIONNONE:
		//return the track to active when it becomes unoccluded
		mStatusFlag = T_STATUSACTIVE;
		break;

	case T_OCCLUSIONPART:
		//mark a track as partially occluded and reset the counter for the last time it was updated
		mTimeSinceLastUpdate = 0.0;
		mStatusFlag = T_STATUSPARTOCCLUDED;
		break;

	case T_OCCLUSIONFULL:
		//mark a track as fully occluded and reset the counter for the last time it was updated
		mTimeSinceLastUpdate = 0.0;
		mStatusFlag = T_STATUSFULLOCCLUDED;
		break;

	default:
		//default to active status
		mStatusFlag = T_STATUSACTIVE;
		break;
	}

	return;
}

bool Track::IsOccluded()
{
	/*
	Returns true if the car is occluded, false otherwise.

	INPUTS:
		none.

	OUTPUTS:
		rIsOccluded - true if the car is occluded, false otherwise.
	*/

	bool rIsOccluded = false;

	switch (mStatusFlag)
	{
	case T_STATUSFULLOCCLUDED:
	case T_STATUSPARTOCCLUDED:
		//return occlusion only for occluded track statuses
		rIsOccluded = true;
		break;

	default:
		rIsOccluded = false;
		break;
	}

	return rIsOccluded;
}

bool Track::SpeedIsValid()
{
	/*
	Determines whether speed is valid based on speed variance.

	INPUTS:
		none.

	OUTPUTS:
		rSpeedIsValid - true if speed is being estimated, false otherwise.
	*/

	bool rSpeedIsValid = false;

	if (mIsInitialized == true)
	{
		double Vs = mCovariance[midx(3, 3, T_NUMSTATES)];
		if (fabs(Vs) <= TG_MAXSPEEDVAR)
		{
			rSpeedIsValid = true;
		}
	}

	return rSpeedIsValid;
}

bool Track::HeadingIsValid()
{
	/*
	Determines whether heading is valid based on heading variance and speed.

	INPUTS:
		none.

	OUTPUTS:
		rHeadingIsValid - true if heading is being estimated, false otherwise.
	*/

	bool rHeadingIsValid = false;

	if (mIsInitialized == true)
	{
		double Vh = mCovariance[midx(4, 4, T_NUMSTATES)];

		if (fabs(Vh) <= TG_MAXHEADINGVAR && fabs(mSpeed) >= TG_MINSPDFORHDG)
		{
			rHeadingIsValid = true;
		}
	}

	return rHeadingIsValid;
}

bool Track::IsNearStopline(PosteriorPosePosition* iPosteriorPosePosition)
{
	/*
	Returns true if the track is near a stopline, false otherwise.  A
	track is marked as near a stopline if any of its points are within
	a small radius of a stopline waypoint.

	INPUTS:

	OUTPUTS:
		rIsNearStopline - true if the track is near a stopline, false
			otherwise.
	*/

	bool rIsNearStopline = false;

	if (mClosestPartition == NULL)
	{
		//can't be near a stopline if there's no valid partition
		return rIsNearStopline;
	}
	if (iPosteriorPosePosition->IsValid == false)
	{
		//can't be near a stopline if the posterior pose solution is invalid
		return rIsNearStopline;
	}

	int i;
	int j;
	int ns = mClosestPartition->NumNearbyStoplines();
	int np = mNumTrackPoints;

	//precache trig values for coordinate transformations
	double cosOrient = cos(mOrientation);
	double sinOrient = sin(mOrientation);

	double ae = iPosteriorPosePosition->EastMMSE;
	double an = iPosteriorPosePosition->NorthMMSE;
	double cosHeading = cos(iPosteriorPosePosition->HeadingMMSE);
	double sinHeading = sin(iPosteriorPosePosition->HeadingMMSE);

	//calculate the location of the anchor point in absolute coordinates
	double ape;
	double apn;
	EgoVehicleToAbsolute(ape, apn, mX, mY, cosHeading, sinHeading, ae, an);

	for (i = 0; i < ns; i++)
	{
		//extract each nearby stopline and test for proximity
		RoadPoint* curstop = mClosestPartition->NearbyStopline(i);

		//calculate distance to the track's anchor point as an initial test
		double danchr = curstop->GetDistanceToPoint(ape, apn);
		if (fabs(danchr) <= TG_FASTSTOPLINEDIST)
		{
			//the track's anchor is close enough to the stopline to test carefully

			for (j = 0; j < np; j++)
			{
				//extract the jth track point in object storage frame
				double osx = TrackPoints(j, 0);
				double osy = TrackPoints(j, 1);

				//convert to ego vehicle coordinates
				double evx;
				double evy;
				ObjectToEgoVehicle(evx, evy, osx, osy, cosOrient, sinOrient, mX, mY);

				//convert to absolute coordinates
				double abe;
				double abn;
				EgoVehicleToAbsolute(abe, abn, evx, evy, cosHeading, sinHeading, ae, an);

				//test whether the point is close to the stopline
				double dpt = curstop->GetDistanceToPoint(abe, abn);

				if (fabs(dpt) <= TG_PRECISESTOPLINEDIST)
				{
					//found a point close to a stopline
					rIsNearStopline = true;
					break;
				}
			}
		}
	}

	return rIsNearStopline;
}

Cluster Track::TrackPointsCluster()
{
	/*
	Converts the track points into a cluster structure and returns them

	INPUTS:
		none.

	OUTPUTS:
		rCluster - returns a structure containing pointers to the track data
	*/

	Cluster rCluster;
	rCluster.NumPoints = mNumTrackPoints;
	rCluster.Points = mTrackPoints;

	return rCluster;
}

bool Track::CanBeAssociated(Target* iTarget)
{
	/*
	Applies a heuristic threshold to see whether iTarget could possibly be
	associated with this track.
	
	INPUTS:
		iTarget - a LocalMap target structure that will be tested for
			possible association.

	OUTPUTS:
		rCanBeAssociated - true if the track can possibly be associated, 
			false otherwise
	*/

	bool rCanBeAssociated = false;

	double tx = iTarget->X;
	double ty = iTarget->Y;

	double dx = (mX - tx);
	double dy = (mY - ty);

	double rng = sqrt(dx*dx + dy*dy);

	if (rng <= TG_MAXASSOCDIST)
	{
		//check whether the target is anywhere close to this track
		rCanBeAssociated = true;
	}

	return rCanBeAssociated;
}

double Track::Likelihood(double& oPMDist, double& oSHMDist, Target* iTarget)
{
	/*
	Computes the likelihood that the supplied measurement (a full object)
	is the same as this particular track.

	INPUTS:
		oPMDist - will contain the mahalanobis distance computed in the 
			likelihood due to position on output
		oSHMDist - will contain the mahalanobis distance computed in the
			likelihood due to speed and heading on output
		iTarget - LocalMap target structure containing the target information,
			with target points stored in object storage frame.

	OUTPUTS:
		rLambda - the correspondence likelihood.
		oPMDist - the position mahalanobis distance (for thresholding)
		oSHMDist - the speed / heading mahalanobis distance (for thresholding)
	*/

	//initialize output arguments
	oPMDist = DBL_MAX;
	oSHMDist = DBL_MAX;
	double rLambda = 0.0;

	if (mStatusFlag == T_STATUSDELETED)
	{
		//do not allow objects marked for deletion to be updated
		return rLambda;
	}

	if (CanBeAssociated(iTarget) == false)
	{
		//do not compute full likelihoods for associations that
		//are extremely unlikely
		return rLambda;
	}

	int i;
	int j;

	//extract the information from the target
	double iX = iTarget->X;
	double iY = iTarget->Y;
	double iOrientation = iTarget->Orientation;
	double iSpeed = iTarget->Speed;
	double iHeading = iTarget->Heading;
	double* iCovariance = iTarget->Covariance;
	int iNumTargetPoints = iTarget->NumPoints;
	double* iTargetPoints = iTarget->TargetPoints;

	//compute likelihood as a product of two terms: a position term based on
	//bearing-bearing-range calculated from target points, and a speed/heading
	//term calculated from speed and heading

	//POSITION TERM
	//calculate as a regular Gaussian using the bearing-bearing-range measurement

	//1. calculate the measurement and covariance for the input target
	//NOTE: this is now precalculated in TrackGeneratorFilter::UpdateWithLocalMapTargets
	double* zpit = iTarget->zp;
	double* Rpit = iTarget->Pzzp;

	//2. calculate the measurement and covariance for the track
	SetTrackPositionMeasurement();
	double zpt[T_NUMTPMEAS];
	double Rpt[T_NUMTPMEAS*T_NUMTPMEAS];
	//use the track position measurement
	zpt[0] = mtpmBCW;
	zpt[1] = mtpmBCCW;
	zpt[2] = mtpmRMIN;
	for (i = 0; i < T_NUMTPMEAS; i++)
	{
		for (j = 0; j < T_NUMTPMEAS; j++)
		{
			Rpt[midx(i, j, T_NUMTPMEAS)] = mtpmR[midx(i, j, T_NUMTPMEAS)];
		}
	}

	//calculate normal gaussian likelihood for the position component
	double nup[T_NUMTPMEAS];
	nup[0] = UnwrapAngle(zpit[0] - zpt[0]);
	nup[1] = UnwrapAngle(zpit[1] - zpt[1]);
	nup[2] = zpit[2] - zpt[2];

	double invSp[T_NUMTPMEAS*T_NUMTPMEAS];
	for (i = 0; i < T_NUMTPMEAS; i++)
	{
		for (j = 0; j < T_NUMTPMEAS; j++)
		{
			invSp[midx(i, j, T_NUMTPMEAS)] = Rpit[midx(i, j, T_NUMTPMEAS)] + Rpt[midx(i, j, T_NUMTPMEAS)];
		}
	}

	int ipiv[T_NUMTPMEAS];
	int info;
	//LU decomposition for matrix inversion
	dgetrf(T_NUMTPMEAS, T_NUMTPMEAS, invSp, T_NUMTPMEAS, ipiv, &info);
	if (info != 0)
	{
		printf("Warning: dgetrf error in Track::Likelihood.\n");
		return rLambda;
	}
	//calculate the determinant of S before destroying the LU decomposition
	double detSp = 1.0;
	for (i = 0; i < T_NUMTPMEAS; i++)
	{
		if (ipiv[i] > i+1)
		{
			//negate the determinant because a row pivot took place
			detSp *= -invSp[midx(i, i, T_NUMTPMEAS)];
		}
		else
		{
			//don't negate the determinant because the ith row either wasn't pivoted
			//or it was pivoted (but we counted it already)
			detSp *= invSp[midx(i, i, T_NUMTPMEAS)];
		}
	}
	//invert S and store in invSp
	dgetri(T_NUMTPMEAS, invSp, T_NUMTPMEAS, ipiv, &info);
	if (info != 0)
	{
		printf("Warning: dgetri error in Track::Likelihood.\n");
		return rLambda;
	}

	//compute the gaussian likelihood
	double pmDist = 0.0;
	for (i = 0; i < T_NUMTPMEAS; i++)
	{
		for (j = 0; j < T_NUMTPMEAS; j++)
		{
			pmDist += nup[i]*invSp[midx(i, j, T_NUMTPMEAS)]*nup[j];
		}
	}
	//double pLambda = exp(-0.5*pmDist) / sqrt(pow(TWOPI, 3.0)*detSp);
	//do this instead for a little bit more numerical robustness
	double logpLambda = -0.5*pmDist - 1.5*LNTWOPI - 0.5*log(detSp);
	double pLambda = exp(logpLambda);

	/*
	if (pmDist > TG_CHI2POSTHRESH)
	{
		//apply a Chi2 gate to position assignments
		pLambda = 0.0;
	}
	*/

	//SPEED - HEADING TERM
	//calculate as a regular Gaussian using the speed - heading submatrix from this track and the input
	double nush[2] = {iSpeed - mSpeed, UnwrapAngle(iHeading - mHeading)};
	double Ssh[4];
	int idxsh[2] = {3, 4};
	for (i = 0; i < 2; i++)
	{
		for (j = 0; j < 2; j++)
		{
			Ssh[midx(i, j, 2)] = mCovariance[midx(idxsh[i], idxsh[j], T_NUMSTATES)] + iCovariance[midx(idxsh[i], idxsh[j], 6)];
		}
	}
	//calculate 2x2 matrix inverse by hand
	double invSsh[4];
	double detSsh = Ssh[midx(0, 0, 2)]*Ssh[midx(1, 1, 2)] - Ssh[midx(0, 1, 2)]*Ssh[midx(1, 0, 2)];
	if (fabs(detSsh) == 0.0)
	{
		return rLambda;
	}
	double dfact = 1.0/detSsh;
	invSsh[midx(0, 0, 2)] = dfact*Ssh[midx(1, 1, 2)];
	invSsh[midx(1, 1, 2)] = dfact*Ssh[midx(0, 0, 2)];
	invSsh[midx(0, 1, 2)] = -dfact*Ssh[midx(1, 0, 2)];
	invSsh[midx(1, 0, 2)] = -dfact*Ssh[midx(0, 1, 2)];

	//calculate gaussian likelihood for speed
	double shmDist = 0.0;
	for (i = 0; i < 2; i++)
	{
		for (j = 0; j < 2; j++)
		{
			shmDist += nush[i]*invSsh[midx(i, j, 2)]*nush[j];
		}
	}
	//double shLambda = exp(-0.5*shmDist) / sqrt(pow(TWOPI, 2.0)*detSsh);
	//do this instead for a little bit more numerical robustness
	double logshLambda = -0.5*shmDist - LNTWOPI - 0.5*log(detSsh);
	double shLambda = exp(logshLambda);

	/*
	if (shmDist > TG_CHI2SPDTHRESH)
	{
		//apply a Chi2 gate to speed assignments
		shLambda = 0.0;
	}
	*/

	//finally, calculate the total likelihood
	oPMDist = pmDist;
	oSHMDist = shmDist;
	//rLambda = pLambda*shLambda;
	//do this for a little bit more numerical robustness
	rLambda = exp(logpLambda + logshLambda);

	return rLambda;
}

void Track::Predict(double iPredictDt, VehicleOdometry* iVehicleOdometry, PosteriorPosePosition* iPosteriorPosePosition, RoadGraph* iRoadGraph)
{
	/*
	Predicts the entire track forward by a specified time iDt.

	INPUTS:
		iPredictDt - prediction time interval, in seconds.
		iVehicleOdometry - vehicle odometry structure valid during
			the time of the prediction
		iPosteriorPosePosition - current posterior pose position, valid
			at the end of the prediction
		iRoadGraph - road graph used in the scene estimator

	OUTPUTS:
		none.  Predicts the track forward by iDt, updating its state
			and covariance matrix, and also probabilities.
	*/

	if (iVehicleOdometry->IsValid == false)
	{
		//don't predict with invalid odometry
		return;
	}

	if (iPredictDt <= 0.0)
	{
		//don't predict tracks backwards
		return;
	}

	//STATE PREDICTION
	int i;
	int j;
	int k;
	int nx;
	int nv;
	double dt = iPredictDt;

	//declare the state matrices
	nx = T_NUMSTATES;
	double x[T_NUMSTATES] = {mX, mY, mOrientation, mSpeed, mHeading};
	double P[T_NUMSTATES*T_NUMSTATES];
	int idx[T_NUMSTATES] = {0, 1, 2, 3, 4};
	for (i = 0; i < nx; i++)
	{
		for (j = 0; j < nx; j++)
		{
			P[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
		}
	}
	double xbar[T_NUMSTATES];
	double Pbar[T_NUMSTATES*T_NUMSTATES];

	//declare the noise matrices
	nv = T_NUMSTATES + 3;
	double Q[(T_NUMSTATES+3)*(T_NUMSTATES+3)];
	for (i = 0; i < nv; i++)
	{
		for (j = 0; j < nv; j++)
		{
			Q[midx(i, j, nv)] = 0.0;
		}
	}
	//create noise in discrete time
	Q[midx(0, 0, nv)] = TG_QX / dt;
	Q[midx(1, 1, nv)] = TG_QY / dt;
	Q[midx(2, 2, nv)] = TG_QORIENT / dt;
	Q[midx(3, 3, nv)] = TG_QSPEED / dt;
	Q[midx(4, 4, nv)] = TG_QHEADING / dt;
	Q[midx(5, 5, nv)] = ODOM_QVX / dt;
	Q[midx(6, 6, nv)] = ODOM_QVY / dt;
	Q[midx(7, 7, nv)] = ODOM_QWZ / dt;

	//perform the prediction
	KalmanPredict(xbar, Pbar, nx, nv, dt, x, P, Q, iVehicleOdometry, &TrackDynamics);

	//extract the state and store it
	mX = xbar[0];
	mY = xbar[1];
	mOrientation = xbar[2];
	mSpeed = xbar[3];
	mHeading = xbar[4];

	//extract the covariance and store it
	for (i = 0; i < nx; i++)
	{
		for (j = 0; j < nx; j++)
		{
			mCovariance[midx(idx[i], idx[j], T_NUMSTATES)] = Pbar[midx(i, j, nx)];
		}
	}

	//AUXILIARY VELOCITY ESTIMATOR PREDICTION

	{
		int avenx = T_NUMAVESTATES;
		int avenv = T_NUMAVESTATES + 3;

		double avex[T_NUMAVESTATES];
		memcpy(avex, maveState, avenx*sizeof(double));
		double aveP[T_NUMAVESTATES*T_NUMAVESTATES];
		memcpy(aveP, maveCovariance, avenx*avenx*sizeof(double));
		double avexbar[T_NUMAVESTATES];
		double avePbar[T_NUMAVESTATES*T_NUMAVESTATES];
		double aveQ[(T_NUMAVESTATES+3)*(T_NUMAVESTATES+3)];
		memset(aveQ, 0x00, avenv*avenv*sizeof(double));

		//create ave noise in discrete time
		aveQ[midx(0, 0, nv)] = TG_QX / dt;
		aveQ[midx(1, 1, nv)] = TG_QY / dt;
		aveQ[midx(2, 2, nv)] = TG_QSPEED / dt;
		aveQ[midx(3, 3, nv)] = TG_QHEADING / dt;
		aveQ[midx(4, 4, nv)] = ODOM_QVX / dt;
		aveQ[midx(5, 5, nv)] = ODOM_QVY / dt;
		aveQ[midx(6, 6, nv)] = ODOM_QWZ / dt;

		KalmanPredict(avexbar, avePbar, avenx, avenv, dt, avex, aveP, aveQ, iVehicleOdometry, &DynamicPointDynamics);

		memcpy(maveState, avexbar, avenx*sizeof(double));
		memcpy(maveCovariance, avePbar, avenx*avenx*sizeof(double));
	}

	//PROBABILITY PREDICTION

	//first calculate the number of atomic HMM intervals that have elapsed during the prediction
	double nai = Round(dt / TG_HMMDT);

	//next calculate the transition matrix for the stopped probability using eigenvalue diagonalization
	double V[4] = {0.5*SQRTTWO, 0.5*SQRTTWO, -0.5*SQRTTWO, 0.5*SQRTTWO};
	double Es[4] = {1.0, 0.0, 0.0, pow(2.0*TG_STOPSTAYPROB - 1.0, nai)};
	//calculate the transition matrix for the stopped probability
	double VEs[4] = {0.0, 0.0, 0.0, 0.0};
	for (i = 0; i < 2; i++)
	{
		for (j = 0; j < 2; j++)
		{
			for (k = 0; k < 2; k++)
			{
				VEs[midx(i, j, 2)] += V[midx(i, k, 2)] * Es[midx(k, j, 2)];
			}
		}
	}
	double Ps[4] = {0.0, 0.0, 0.0, 0.0};
	for (i = 0; i < 2; i++)
	{
		for (j = 0; j < 2; j++)
		{
			for (k = 0; k < 2; k++)
			{
				Ps[midx(i, j, 2)] += VEs[midx(i, k, 2)] * V[midx(j, k, 2)];
			}
		}
	}
	mStoppedProbability = Ps[midx(0, 0, 2)]*mStoppedProbability + Ps[midx(0, 1, 2)]*(1.0 - mStoppedProbability);

	//next calculate the transition matrix for the car probability using eigenvalue diagonalization
	double Ec[4] = {1.0, 0.0, 0.0, pow(2.0*TG_CARSTAYPROB - 1.0, nai)};
	//calculate the transition matrix for the stopped probability
	double VEc[4] = {0.0, 0.0, 0.0, 0.0};
	for (i = 0; i < 2; i++)
	{
		for (j = 0; j < 2; j++)
		{
			for (k = 0; k < 2; k++)
			{
				VEc[midx(i, j, 2)] += V[midx(i, k, 2)] * Ec[midx(k, j, 2)];
			}
		}
	}
	double Pc[4] = {0.0, 0.0, 0.0, 0.0};
	for (i = 0; i < 2; i++)
	{
		for (j = 0; j < 2; j++)
		{
			for (k = 0; k < 2; k++)
			{
				Pc[midx(i, j, 2)] += VEc[midx(i, k, 2)] * V[midx(j, k, 2)];
			}
		}
	}
	mCarProbability = Pc[midx(0, 0, 2)]*mCarProbability + Pc[midx(0, 1, 2)]*(1.0 - mCarProbability);

	//PARTITION PREDICTION

	//at the end of the prediction, find the track's closest position in the road graph
	double ae;
	double an;
	double eve = iPosteriorPosePosition->EastMMSE;
	double evn = iPosteriorPosePosition->NorthMMSE;
	double cosVh = cos(iPosteriorPosePosition->HeadingMMSE);
	double sinVh = sin(iPosteriorPosePosition->HeadingMMSE);
	//convert the track's anchor point to absolute
	EgoVehicleToAbsolute(ae, an, mX, mY, cosVh, sinVh, eve, evn);
	//and find its location in the road graph (NOTE: if mClosestPartition is null, this does a full search)
	if (iRoadGraph != NULL)
	{
		mClosestPartition = iRoadGraph->ClosestPartition(ae, an, mClosestPartition);
	}
	else
	{
		mClosestPartition = NULL;
	}

	//after the prediction, mark the track measurement as needing to be recomputed
	mtpmIsCurrent = false;

	//after the prediction, augment the time since the last update
	mTimeSinceLastUpdate += dt;
	mAbsoluteTimeSinceLastUpdate += dt;
	mTimeSinceCreation += dt;

	return;
}

void Track::Update(Target* iTarget, PosteriorPosePosition* iPosteriorPosePosition, RoadGraph* iRoadGraph)
{
	/*
	Updates a target track with a new set of track information.  This function throws
	away existing track information and replaces it with the input information.

	INPUTS:
		iTarget - LocalMap target structure containing the target information,
			with target points stored in object storage frame.
		iPosteriorPoseSolution - the posterior pose solution used to transform 
			the track to absolute coordinates (for finding absolute location).
		iRoadGraph - pointer to the road graph used to find the closest partition

	OUTPUTS:
		none.  Copies all data into class member variables to initialize the track.
	*/

	if (mStatusFlag == T_STATUSDELETED)
	{
		//do not allow objects marked for deletion to be updated
		return;
	}

	int i;
	int j;

	//extract the information from the target
	double iX = iTarget->X;
	double iY = iTarget->Y;
	double iOrientation = iTarget->Orientation;
	double iSpeed = iTarget->Speed;
	double iHeading = iTarget->Heading;
	double* iCovariance = iTarget->Covariance;
	int iNumTargetPoints = iTarget->NumPoints;
	double* iTargetPoints = iTarget->TargetPoints;

	//UPDATE TRACK STATE

	//replace the track state
	mX = iX;
	mY = iY;
	mOrientation = iOrientation;
	mSpeed = iSpeed;
	mHeading = iHeading;

	//replace covariance (ignore width covariance)
	for (i = 0; i < T_NUMSTATES; i++)
	{
		for (j = 0; j < T_NUMSTATES; j++)
		{
			mCovariance[midx(i, j, T_NUMSTATES)] = iCovariance[midx(i, j, 6)];
		}
	}

	/*
	//TEMPORARY!!!  TRY KALMAN UPDATE TO ESTIMATE SPEED AND HEADING
	int foo = 1.3;
	bool kfsuccess = false;
	if (mIsInitialized == true && (iCovariance[midx(3, 3, 6)] > TG_MAXSPEEDVAR || iCovariance[midx(4, 4, 6)] > TG_MAXHEADINGVAR))
	{
		//the target does not have a valid speed and heading measurement
		//use the target's position measurement to estimate speed and heading in a mini-KF

		//extract the cached position measurement for the target
		double* zpit = iTarget->zp;
		double* Rpit = iTarget->Pzzp;

		//extract the cached position measurement for the track
		SetTrackPositionMeasurement();
		double zpt[T_NUMTPMEAS];
		double HPHtpt[T_NUMTPMEAS*T_NUMTPMEAS];
		double PHtpt[T_NUMSTATES*T_NUMTPMEAS];
		//use the track position measurement
		zpt[0] = mtpmBCW;
		zpt[1] = mtpmBCCW;
		zpt[2] = mtpmRMIN;
		for (i = 0; i < T_NUMTPMEAS; i++)
		{
			for (j = 0; j < T_NUMTPMEAS; j++)
			{
				HPHtpt[midx(i, j, T_NUMTPMEAS)] = mtpmR[midx(i, j, T_NUMTPMEAS)];
			}
		}
		for (i = 0; i < T_NUMTPMEAS; i++)
		{
			for (j = 0; j < T_NUMTPMEAS; j++)
			{
				PHtpt[midx(i, j, T_NUMTPMEAS)] = mtpmPHt[midx(i, j, T_NUMTPMEAS)];
			}
		}

		//calculate innovation and traditional KF matrices
		double nup[T_NUMTPMEAS];
		nup[0] = UnwrapAngle(zpit[0] - zpt[0]);
		nup[1] = UnwrapAngle(zpit[1] - zpt[1]);
		nup[2] = zpit[2] - zpt[2];

		double S[T_NUMTPMEAS*T_NUMTPMEAS];
		double invS[T_NUMTPMEAS*T_NUMTPMEAS];
		for (i = 0; i < T_NUMTPMEAS; i++)
		{
			for (j = 0; j < T_NUMTPMEAS; j++)
			{
				S[midx(i, j, T_NUMTPMEAS)] = HPHtpt[midx(i, j, T_NUMTPMEAS)] + Rpit[midx(i, j, T_NUMTPMEAS)];
				invS[midx(i, j, T_NUMTPMEAS)] = HPHtpt[midx(i, j, T_NUMTPMEAS)] + Rpit[midx(i, j, T_NUMTPMEAS)];
			}
		}

		int ipiv[T_NUMTPMEAS];
		int info;
		//LU decomposition for matrix inversion
		dgetrf(T_NUMTPMEAS, T_NUMTPMEAS, invS, T_NUMTPMEAS, ipiv, &info);
		if (info != 0)
		{
			printf("Warning: dgetrf error in Track::Update.\n");
			goto KFFAIL;
		}
		//calculate the determinant of S before destroying the LU decomposition
		double detS = 1.0;
		for (i = 0; i < T_NUMTPMEAS; i++)
		{
			if (ipiv[i] > i+1)
			{
				//negate the determinant because a row pivot took place
				detS *= -invS[midx(i, i, T_NUMTPMEAS)];
			}
			else
			{
				//don't negate the determinant because the ith row either wasn't pivoted
				//or it was pivoted (but we counted it already)
				detS *= invS[midx(i, i, T_NUMTPMEAS)];
			}
		}
		//invert S and store in invS
		dgetri(T_NUMTPMEAS, invS, T_NUMTPMEAS, ipiv, &info);
		if (info != 0)
		{
			printf("Warning: dgetri error in Track::Update.\n");
			goto KFFAIL;
		}

		//calculate Kalman gain W = PHt*invS
		double W[T_NUMSTATES*T_NUMTPMEAS];
		memset(W, 0x00, T_NUMSTATES*T_NUMTPMEAS*sizeof(double));
		for (i = 0; i < T_NUMSTATES; i++)
		{
			for (j = 0; j < T_NUMTPMEAS; j++)
			{
				for (k = 0; k < T_NUMTPMEAS; k++)
				{
					W[midx(i, j, T_NUMSTATES)] += PHtpt[midx(i, k, T_NUMSTATES)]*invS[midx(k, j, T_NUMTPMEAS)];
				}
			}
		}

		//perform a Kalman update on the entire state
		double xbar[T_NUMSTATES] = {mX, mY, mOrientation, mSpeed, mHeading};
		double Pbar[T_NUMSTATES*T_NUMSTATES];
		memcpy(Pbar, mCovariance, T_NUMSTATES*T_NUMSTATES*sizeof(double));
		double xhat[T_NUMSTATES];
		double Phat[T_NUMSTATES*T_NUMSTATES];
		KalmanUpdate(xhat, Phat, T_NUMSTATES, T_NUMTPMEAS, xbar, Pbar, nup, S, W);

		//and only retain the update for speed and heading
		mX = iX;
		mY = iY;
		mOrientation = iOrientation;
		mSpeed = xhat[3];
		mHeading = UnwrapAngle(xhat[4]);

		//replace covariance (ignore width covariance)
		for (i = 0; i < T_NUMSTATES; i++)
		{
			for (j = 0; j < T_NUMSTATES; j++)
			{
				mCovariance[midx(i, j, T_NUMSTATES)] = iCovariance[midx(i, j, 6)];
			}
		}

		kfsuccess = true;
	}
	KFFAIL:
	if (kfsuccess == false)
	{
		//the target has a valid speed and heading measurement

		//replace the track state
		mX = iX;
		mY = iY;
		mOrientation = iOrientation;
		mSpeed = iSpeed;
		mHeading = iHeading;

		//replace covariance (ignore width covariance)
		for (i = 0; i < T_NUMSTATES; i++)
		{
			for (j = 0; j < T_NUMSTATES; j++)
			{
				mCovariance[midx(i, j, T_NUMSTATES)] = iCovariance[midx(i, j, 6)];
			}
		}
	}
	//END TEMPORARY!!!
	*/

	//REPLACE TRACK POINTS

	mNumTrackPoints = iNumTargetPoints;
	delete [] mTrackPoints;
	mTrackPoints = NULL;
	if (mNumTrackPoints > 0)
	{
		mTrackPoints = new double[2*mNumTrackPoints];

		for (i = 0; i < mNumTrackPoints; i++)
		{
			//copy each track point from the input
			//NOTE: points are already in the object storage frame
			mTrackPoints[midx(i, 0, mNumTrackPoints)] = iTargetPoints[midx(i, 0, iNumTargetPoints)];
			mTrackPoints[midx(i, 1, mNumTrackPoints)] = iTargetPoints[midx(i, 1, iNumTargetPoints)];
		}
	}
	else
	{
		printf("Warning: track %d has 0 track points.\n", mID);
	}

	//UPDATE AUXILIARY VELOCITY ESTIMATOR

	//calculate the center of the track's mass for updating
	double xcm = 0.0;
	double ycm = 0.0;

	if (mNumTrackPoints > 0)
	{
		//if the track has points, initialize the ave to center of mass
		double cosOrient = cos(mOrientation);
		double sinOrient = sin(mOrientation);
		double wt = 1.0 / ((double) mNumTrackPoints);
		for (i = 0; i < mNumTrackPoints; i++)
		{
			//extract each point
			double osx = mTrackPoints[midx(i, 0, mNumTrackPoints)];
			double osy = mTrackPoints[midx(i, 1, mNumTrackPoints)];
			double evx;
			double evy;
			ObjectToEgoVehicle(evx, evy, osx, osy, cosOrient, sinOrient, mX, mY);

			xcm += wt*evx;
			ycm += wt*evy;
		}
	}
	//generate the ave measurement vector and covariance matrix
	double zave[T_NUMAVEMEAS] = {xcm, ycm};
	double Rave[T_NUMAVEMEAS*T_NUMAVEMEAS];
	int idxave[2] = {0, 1};
	for (i = 0; i < T_NUMAVEMEAS; i++)
	{
		for (j = 0; j < T_NUMAVEMEAS; j++)
		{
			Rave[midx(i, j, T_NUMAVEMEAS)] = mCovariance[midx(idxave[i], idxave[j], T_NUMSTATES)];
		}
	}

	if (mIsInitialized == true)
	{
		//update the auxiliary velocity estimator

		//apply a traditional Kalman update
		int nz = T_NUMAVEMEAS;
		int nx = T_NUMAVESTATES;

		double nu[T_NUMAVEMEAS];
		double S[T_NUMAVEMEAS*T_NUMAVEMEAS];
		double W[T_NUMAVESTATES*T_NUMAVEMEAS];

		double H[T_NUMAVEMEAS*T_NUMAVESTATES];
		memset(H, 0x00, nz*nx*sizeof(double));
		H[midx(0, 0, nz)] = 1.0;
		H[midx(1, 1, nz)] = 1.0;

		double xbar[T_NUMAVESTATES];
		memcpy(xbar, maveState, nx*sizeof(double));
		double Pbar[T_NUMAVESTATES*T_NUMAVESTATES];
		memcpy(Pbar, maveCovariance, nx*nx*sizeof(double));
		double xhat[T_NUMAVESTATES];
		double Phat[T_NUMAVESTATES*T_NUMAVESTATES];

		double aveLambda = KalmanLikelihood(nu, S, W, nx, nz, zave, Rave, H, xbar, Pbar, TG_AVECHI2THRESH);
		if (aveLambda > 0.0)
		{
			//measurement made sense, so it is applied
			KalmanUpdate(xhat, Phat, nx, nz, xbar, Pbar, nu, S, W);

			memcpy(maveState, xhat, nx*sizeof(double));
			memcpy(maveCovariance, Phat, nx*nx*sizeof(double));
		}
		else
		{
			//measurement made no sense: reset the auxiliary velocity estimator

			//initialize the ave to the object's center of mass and pointed toward car
			maveState[0] = xcm;
			maveState[1] = ycm;
			maveState[2] = 0.0;
			maveState[3] = -atan2(ycm, xcm);
			//initialize the position covariance to be the same as the track's
			memset(maveCovariance, 0x00, T_NUMAVESTATES*T_NUMAVESTATES*sizeof(double));
			for (i = 0; i < 2; i++)
			{
				for (j = 0; j < 2; j++)
				{
					maveCovariance[midx(i, j, T_NUMAVESTATES)] = Rave[midx(i, j, T_NUMAVEMEAS)];
				}
			}
			//initialize the speed and heading to high
			maveCovariance[midx(2, 2, T_NUMAVESTATES)] = TG_INITSPEEDVAR;
			maveCovariance[midx(3, 3, T_NUMAVESTATES)] = TG_INITHEADINGVAR;
		}
	}
	else
	{
		//initialize the ave to the object's center of mass and pointed toward car
		maveState[0] = xcm;
		maveState[1] = ycm;
		maveState[2] = 0.0;
		maveState[3] = -atan2(ycm, xcm);
		//initialize the position covariance to be the same as the track's
		memset(maveCovariance, 0x00, T_NUMAVESTATES*T_NUMAVESTATES*sizeof(double));
		for (i = 0; i < 2; i++)
		{
			for (j = 0; j < 2; j++)
			{
				maveCovariance[midx(i, j, T_NUMAVESTATES)] = Rave[midx(i, j, T_NUMAVEMEAS)];
			}
		}
		//initialize the speed and heading to high
		maveCovariance[midx(2, 2, T_NUMAVESTATES)] = TG_INITSPEEDVAR;
		maveCovariance[midx(3, 3, T_NUMAVESTATES)] = TG_INITHEADINGVAR;
	}

	//the track position measurement is no longer current, because we just changed its points
	mtpmIsCurrent = false;

	//UPDATE TRACK PROBABILITIES BASE ON STATE EVIDENCE

	//1. update the is-car probability based on width
	if (iNumTargetPoints > 0)
	{
		//update the is-car probability using object width based on angular spread

		//use extreme angles and closest range to calculate track width
		double zt[3];
		double xt[5] = {mX, mY, mOrientation, mSpeed, mHeading};
		Cluster iCluster = TrackPointsCluster();
		ClusterBcwBccwRmin(zt, xt, &iCluster);

		double bmin = zt[0];
		double bmax = zt[1];
		double rmin = zt[2];
		//done extracting extreme cluster angles

		//calculate target projected width at rmin using angular spread
		double da = fabs(UnwrapAngle(bmax - bmin));
		double mWidth = 2.0*rmin*tan(0.5*da);
		//extract width standard dev from the target measurement
		double Sw = sqrt(iCovariance[midx(5, 5, 6)]);

		double lcar = exp(-0.5*pow(mWidth - TG_MEANCARWIDTH, 2.0) / pow(TG_SCALECARWIDTH + Sw, 2.0));
		double lncar = 1.0 - lcar;
		//perform the likelihood update
		mCarProbability = lcar*mCarProbability / (lcar*mCarProbability + lncar*(1.0 - mCarProbability));
	}

	//2. update the is-stopped probability based on the likelihood that the track
	//is stopped given the new track measurement
	if (SpeedIsValid() == true)
	{
		//only update the stopped HMM if speed is valid

		double Ss = sqrt(mCovariance[midx(3, 3, T_NUMSTATES)]);
		//calculate the likelihood of the current speed given that we're stopped
		double lstop = exp(-3.0 / (TG_STOPNS95PCT*Ss) * (mSpeed - 0.0));
		//calculate the likelihood of the current speed given that we're not stopped
		double lnstop = 1.0 - lstop;
		//perform the likelihood update
		mStoppedProbability = lstop*mStoppedProbability / (lstop*mStoppedProbability + lnstop*(1.0 - mStoppedProbability));
	}

	//at the end of the update, find the track's closest position in the road graph
	double ae;
	double an;
	double eve = iPosteriorPosePosition->EastMMSE;
	double evn = iPosteriorPosePosition->NorthMMSE;
	double cosVh = cos(iPosteriorPosePosition->HeadingMMSE);
	double sinVh = sin(iPosteriorPosePosition->HeadingMMSE);
	//convert the track's anchor point to absolute
	EgoVehicleToAbsolute(ae, an, mX, mY, cosVh, sinVh, eve, evn);
	//and find its location in the road graph (NOTE: if mClosestPartition is null, this does a full search)
	if (iRoadGraph != NULL)
	{
		mClosestPartition = iRoadGraph->ClosestPartition(ae, an, mClosestPartition);
	}
	else
	{
		mClosestPartition = NULL;
	}

	//when the update is done, the target is initialized
	//set status flag to an active track
	mStatusFlag = T_STATUSACTIVE;
	//reset the track's age on an update
	mTimeSinceLastUpdate = 0.0;
	mAbsoluteTimeSinceLastUpdate = 0.0;
	//mark the track as having been initialized
	mIsInitialized = true;

	//increment the number of measurements assigned after an update has been performed
	mNumMeasurements++;

	//maintain the track's states after the update
	MaintainTrack();

	return;
}

void Track::MaintainTrack()
{
	/*
	Maintains the track's data- it's state variables, its status flag, etc.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	int i;
	int j;

	//check orientation wrapping
	mOrientation = UnwrapAngle(mOrientation);

	//check sign of speed
	if (mSpeed < 0.0)
	{
		//reverse heading if speed is negative
		mSpeed = -mSpeed;
		mHeading = UnwrapAngle(mHeading + PI);

		//when sign of speed gets flipped, covariance needs to be flipped too
		//NOTE: adding pi to heading doesn't change the correlations.
		for (i = 0; i < T_NUMSTATES; i++)
		{
			mCovariance[midx(i, 3, T_NUMSTATES)] = -mCovariance[midx(i, 3, T_NUMSTATES)];
		}
		for (j = 0; j < T_NUMSTATES; j++)
		{
			mCovariance[midx(3, j, T_NUMSTATES)] = -mCovariance[midx(3, j, T_NUMSTATES)];
		}
		mCovariance[midx(3, 3, T_NUMSTATES)] = fabs(mCovariance[midx(3, 3, T_NUMSTATES)]);
	}

	//check heading wrapping
	mHeading = UnwrapAngle(mHeading);

	//check sign of speed in auxiliary velocity estimate
	if (maveState[2] < 0.0)
	{
		//reverse heading if speed is negative
		maveState[2] = -maveState[2];
		maveState[3] = UnwrapAngle(maveState[3] + PI);

		//when sign of speed gets flipped, covariance needs to be flipped too
		//NOTE: adding pi to heading doesn't change the correlations.
		for (i = 0; i < T_NUMAVESTATES; i++)
		{
			maveCovariance[midx(i, 2, T_NUMAVESTATES)] = -maveCovariance[midx(i, 2, T_NUMAVESTATES)];
		}
		for (j = 0; j < T_NUMAVESTATES; j++)
		{
			maveCovariance[midx(2, j, T_NUMAVESTATES)] = -maveCovariance[midx(2, j, T_NUMAVESTATES)];
		}
		maveCovariance[midx(2, 2, T_NUMAVESTATES)] = fabs(maveCovariance[midx(2, 2, T_NUMAVESTATES)]);
	}

	//check heading wrapping
	maveState[3] = UnwrapAngle(maveState[3]);

	return;
}

void Track::SetTrackPositionMeasurement()
{
	/*
	Sets the track position measurement variables (occlusion boundaries and ranges)
	if the are not set, otherwise this function returns immediately.

	INPUTS:
		none.

	OUTPUTS:
		none.  Calculates and sets mtpm* member variables if they are not already valid.
	*/

	int i;
	int j;

	if (mtpmIsCurrent == false)
	{
		//calculate the track position measurement and covariance matrix

		double zpt[3];
		double Rpt[3*3];
		double PHpt[5*3];
		double xpt[5] = {mX, mY, mOrientation, mSpeed, mHeading};
		double Ppt[5*5];
		//these are the states sent into the track position measurement
		int idxp[5] = {0, 1, 2, 3, 4};

		for (i = 0; i < 5; i++)
		{
			for (j = 0; j < 5; j++)
			{
				Ppt[midx(i, j, 5)] = mCovariance[midx(idxp[i], idxp[j], T_NUMSTATES)];
			}
		}

		Cluster iCluster;
		iCluster.NumPoints = mNumTrackPoints;
		iCluster.Points = mTrackPoints;

		ClusterPositionMeasurement(zpt, Rpt, PHpt, xpt, Ppt, &iCluster);

		//cache the computed measurement so it isn't recomputed later
		mtpmIsCurrent = true;

		mtpmBCW = zpt[0];
		mtpmBCCW = zpt[1];
		mtpmRMIN = zpt[2];

		for (i = 0; i < T_NUMTPMEAS; i++)
		{
			for (j = 0; j < T_NUMTPMEAS; j++)
			{
				mtpmR[midx(i, j, T_NUMTPMEAS)] = Rpt[midx(i, j, T_NUMTPMEAS)];
			}
		}

		for (i = 0; i < T_NUMSTATES; i++)
		{
			for (j = 0; j < T_NUMTPMEAS; j++)
			{
				mtpmPHt[midx(i, j, T_NUMSTATES)] = PHpt[midx(i, j, T_NUMSTATES)];
			}
		}
	}
	//NOTE: if the track position measurement is already calculated, no extra work is done

	return;
}
