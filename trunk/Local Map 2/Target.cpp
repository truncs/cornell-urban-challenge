#include "Target.h"

Target::Target()
{
	/*
	Default constructor for the target class.  Initializes variables to
	default values.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	int i;
	int j;

	//initialize status flags
	mTargetType = T_INVALID;
	mIsInitialized = false;

	mLastUpdateTime = -DBL_MAX;
	mLastIbeoUpdateTime = -DBL_MAX;
	mLastMobileyeUpdateTime = -DBL_MAX;
	mLastRadarUpdateTime = -DBL_MAX;
	mLastSideSickUpdateTime = -DBL_MAX;

	mNumMeasurements = 0;
	mNumIbeoMeasurements = 0;
	mNumPartialIbeoMeasurements = 0;
	mNumMobileyeMeasurements = 0;
	mNumRadarMeasurements = 0;
	mNumSideSickMeasurements = 0;

	PrevTarget = NULL;
	NextTarget = NULL;

	//initialize the status estimates
	mExistenceProbability = 0.5;

	//initialize state vector
	mX = 0.0;
	mY = 0.0;
	mOrientation = 0.0;
	mSpeed = 0.0;
	mHeading = 0.0;
	mWidth = 0.0;

	//initialize covariance
	for (i = 0; i < T_NUMSTATES; i++)
	{
		for (j = 0; j < T_NUMSTATES; j++)
		{
			mCovariance[midx(i, j, T_NUMSTATES)] = 0.0;
		}
	}

	//initialize cluster points
	mNumPoints = 0;
	mTargetPoints = NULL;
	mTargetGrid = new TargetOccupancyGrid(TARGET_OGNUMROWS, TARGET_OGNUMCOLS);

	//initialize placeholder variables
	mMeasurementType = MM_INVALID;
	mLambda = 0.0;
	nu = NULL;
	S = NULL;
	W = NULL;

	return;
}

Target::Target(Target* iTarget2Copy)
{
	/*
	Copy constructor for the target class.  Takes a pointer to an
	existing target and copies all its values over.  Memory is declared
	in this construction.

	NOTE: measurement placeholder variables are NOT copied over.

	INPUTS:
		iTarget2Copy - the target to be copied.

	OUTPUTS:
		none.
	*/

	//copy status flags
	mTargetType = iTarget2Copy->TargetType();
	mIsInitialized = iTarget2Copy->IsInitialized();

	mLastUpdateTime = iTarget2Copy->LastUpdateTime();
	mLastIbeoUpdateTime = iTarget2Copy->LastIbeoUpdateTime();
	mLastMobileyeUpdateTime = iTarget2Copy->LastMobileyeUpdateTime();
	mLastRadarUpdateTime = iTarget2Copy->LastRadarUpdateTime();
	mLastSideSickUpdateTime = iTarget2Copy->LastSideSickUpdateTime();

	mNumMeasurements = iTarget2Copy->NumMeasurements();
	mNumIbeoMeasurements = iTarget2Copy->NumIbeoMeasurements();
	mNumPartialIbeoMeasurements = iTarget2Copy->NumPartialIbeoMeasurements();
	mNumMobileyeMeasurements = iTarget2Copy->NumMobileyeMeasurements();
	mNumRadarMeasurements = iTarget2Copy->NumRadarMeasurements();
	mNumSideSickMeasurements = iTarget2Copy->NumSideSickMeasurements();

	PrevTarget = NULL;
	NextTarget = NULL;

	//copy state estimates
	mExistenceProbability = iTarget2Copy->ExistenceProbability();

	mX = iTarget2Copy->X();
	mY = iTarget2Copy->Y();
	mOrientation = iTarget2Copy->Orientation();
	mSpeed = iTarget2Copy->Speed();
	mHeading = iTarget2Copy->Heading();
	mWidth = iTarget2Copy->Width();

	//copy covariance
	int i;
	int j;
	for (i = 0; i < T_NUMSTATES; i++)
	{
		for (j = 0; j < T_NUMSTATES; j++)
		{
			mCovariance[midx(i, j, T_NUMSTATES)] = iTarget2Copy->Covariance(i, j);
		}
	}

	//copy target points
	mNumPoints = iTarget2Copy->NumPoints();
	if (mNumPoints > 0)
	{
		mTargetPoints = new double[2*mNumPoints];
		for (i = 0; i < mNumPoints; i++)
		{
			mTargetPoints[midx(i, 0, mNumPoints)] = iTarget2Copy->TargetPoints(i, 0);
			mTargetPoints[midx(i, 1, mNumPoints)] = iTarget2Copy->TargetPoints(i, 1);
		}
	}
	else
	{
		mTargetPoints = NULL;
	}
	//initialize the occupancy grid
	mTargetGrid = new TargetOccupancyGrid(TARGET_OGNUMROWS, TARGET_OGNUMCOLS);

	//NOTE: measurement placeholder variables are NOT copied
	mMeasurementType = MM_INVALID;
	mLambda = 0.0;
	nu = NULL;
	S = NULL;
	W = NULL;

	return;
}

Target::~Target()
{
	/*
	Default destructor for the target class.  Frees memory and sets the target
	to invalid.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	mTargetType = T_INVALID;

	//delete the target points memory
	mNumPoints = 0;
	delete [] mTargetPoints;
	mTargetPoints = NULL;
	//delete the target occupancy grid
	delete mTargetGrid;

	delete [] nu;
	delete [] S;
	delete [] W;

	return;
}

Cluster Target::TargetPointsCluster()
{
	/*
	Converts the target points into a cluster structure and returns them

	INPUTS:
		none.

	OUTPUTS:
		rCluster - returns a structure containing pointers to the target data
	*/

	Cluster rCluster;
	rCluster.NumPoints = mNumPoints;
	rCluster.Points = mTargetPoints;

	return rCluster;
}

void Target::PrepareForTransmit()
{
	/*
	Prepares the target for transmit.  For targets with no points, creates a 
	set of (fake) target points for transmit.  For all targets, makes sure the
	covariance matrix is fully populated.  NOTE: this overwrites any existing 
	set of points, so DO NOT use it in the regular particle filter.  ONLY use 
	it on a particle that will be transmitted and deleted.

	INPUTS:
		none.

	OUTPUTS:
		none.  Prepares the target for transmit.
	*/

	int i;
	int j;

	switch (mTargetType)
	{
	case T_IBEO:
		{
			//for an ibeo target, the points are already created, but
			//the speed and heading uncertainties are not set

			mCovariance[midx(3, 3, T_NUMSTATES)] = IBEO_INITSPEEDVAR;
			mCovariance[midx(4, 4, T_NUMSTATES)] = IBEO_INITHEADINGVAR;
		}

	case T_IBEOMATURE:
	case T_MATURE:
		{
			//NOTE: T_IBEO cascades into here!
			//for all targets with cluster points, set the width variance based
			//on how far away that target is from the ego vehicle

			//extract the cluster extreme bearings and range
			double zt[3];
			double xt[3] = {mX, mY, mOrientation};
			Cluster iCluster = TargetPointsCluster();
			//generate a fake sensor centered at the IMU for transformations
			Sensor iSensor;
			iSensor.SensorX = 0.0;
			iSensor.SensorY = 0.0;
			iSensor.SensorZ = 0.0;
			iSensor.SensorYaw = 0.0;
			iSensor.SensorPitch = 0.0;
			iSensor.SensorRoll = 0.0;
			BcwBccwRminClusterMeasurement(zt, 3, xt, &iSensor, NULL, &iCluster);
			double da = fabs(UnwrapAngle(zt[1] - zt[0]));
			
			//calculate range variance from target position covariance
			double rng = sqrt(mX*mX + mY*mY);
			double oor = 1.0;
			if (fabs(rng) > 0.0)
			{
				oor = 1.0/rng;
			}
			double Jr[2];
			Jr[midx(0, 0, 1)] = mX*oor;
			Jr[midx(0, 1, 1)] = mY*oor;

			double Pr = 0.0;
			for (i = 0; i < 2; i++)
			{
				for (j = 0; j < 2; j++)
				{
					Pr += Jr[midx(0, i, 1)] * mCovariance[midx(i, j, T_NUMSTATES)] * Jr[midx(0, j, 1)];
				}
			}

			//extract cluster angular spread variance as twice the ibeo clustering bearing variance
			double Pb = 2.0*IBEO_CLUSTERBEARINGVAR;

			//calculate variance of mWidth = 2*rng*tan(0.5*da)
			double tan_opfda = tan(0.5*da);
			double Jw[2];
			Jw[midx(0, 0, 1)] = 2.0*tan_opfda;
			Jw[midx(0, 1, 1)] = rng*(1.0 + tan_opfda*tan_opfda);

			mWidth = 2.0*rng*tan_opfda;
			double Pw = Jw[0]*Pr*Jw[0] + Jw[1]*Pb*Jw[1];
			mCovariance[midx(5, 5, T_NUMSTATES)] = Pw;
		}

		break;

	case T_MOBILEYE:
	case T_QUASIMATURE:
		{
			//for mobileye and quasi-mature targets, create a box using width
			//and an assumed aspect ratio
			mNumPoints = 4;
			mTargetPoints = new double[2*mNumPoints];

			//set fake orientation to vehicle heading, and set uncertainty appropriately
			mOrientation = mHeading;
			mCovariance[midx(2, 2, T_NUMSTATES)] = mCovariance[midx(4, 4, T_NUMSTATES)];

			//calculate fake target points
			double mLength = TARGET_ASPECTRATIO*mWidth;
			double ta = atan2(mY, mX);
			double uwa = UnwrapAngle(mHeading - ta);

			if (fabs(uwa) < PIOFOUR)
			{
				//seeing the back of the target

				mTargetPoints[midx(0, 0, mNumPoints)] = 0.0;
				mTargetPoints[midx(0, 1, mNumPoints)] = 0.5*mWidth;
				mTargetPoints[midx(1, 0, mNumPoints)] = 0.0;
				mTargetPoints[midx(1, 1, mNumPoints)] = -0.5*mWidth;
				mTargetPoints[midx(2, 0, mNumPoints)] = mLength;
				mTargetPoints[midx(2, 1, mNumPoints)] = 0.5*mWidth;
				mTargetPoints[midx(3, 0, mNumPoints)] = mLength;
				mTargetPoints[midx(3, 1, mNumPoints)] = -0.5*mWidth;
			}
			else if (fabs(uwa) > 3.0*PIOFOUR)
			{
				//seeing the front of the target

				mTargetPoints[midx(0, 0, mNumPoints)] = 0.0;
				mTargetPoints[midx(0, 1, mNumPoints)] = 0.5*mWidth;
				mTargetPoints[midx(1, 0, mNumPoints)] = 0.0;
				mTargetPoints[midx(1, 1, mNumPoints)] = -0.5*mWidth;
				mTargetPoints[midx(2, 0, mNumPoints)] = -mLength;
				mTargetPoints[midx(2, 1, mNumPoints)] = 0.5*mWidth;
				mTargetPoints[midx(3, 0, mNumPoints)] = -mLength;
				mTargetPoints[midx(3, 1, mNumPoints)] = -0.5*mWidth;
			}
			else if (uwa > 0.0)
			{
				//seeing the left side of the target

				mTargetPoints[midx(0, 0, mNumPoints)] = -0.5*mLength;
				mTargetPoints[midx(0, 1, mNumPoints)] = 0.0;
				mTargetPoints[midx(1, 0, mNumPoints)] = 0.5*mLength;
				mTargetPoints[midx(1, 1, mNumPoints)] = 0.0;
				mTargetPoints[midx(2, 0, mNumPoints)] = -0.5*mLength;
				mTargetPoints[midx(2, 1, mNumPoints)] = -mWidth;
				mTargetPoints[midx(3, 0, mNumPoints)] = 0.5*mLength;
				mTargetPoints[midx(3, 1, mNumPoints)] = -mWidth;
			}
			else
			{
				//seeing the right side of the target

				mTargetPoints[midx(0, 0, mNumPoints)] = -0.5*mLength;
				mTargetPoints[midx(0, 1, mNumPoints)] = 0.0;
				mTargetPoints[midx(1, 0, mNumPoints)] = 0.5*mLength;
				mTargetPoints[midx(1, 1, mNumPoints)] = 0.0;
				mTargetPoints[midx(2, 0, mNumPoints)] = -0.5*mLength;
				mTargetPoints[midx(2, 1, mNumPoints)] = mWidth;
				mTargetPoints[midx(3, 0, mNumPoints)] = 0.5*mLength;
				mTargetPoints[midx(3, 1, mNumPoints)] = mWidth;
			}
		}

		break;

	case T_RADAR:
		{
			//for radar only targets, create a square of default width
			mNumPoints = 4;
			mTargetPoints = new double[2*mNumPoints];

			//set fake orientation to vehicle heading, and set uncertainty appropriately
			mOrientation = mHeading;
			mCovariance[midx(2, 2, T_NUMSTATES)] = mCovariance[midx(4, 4, T_NUMSTATES)];
			//set fake width for creating points
			mWidth = TARGET_DEFAULTWIDTH;

			//calculate fake target points
			double mLength = mWidth;
			double ta = atan2(mY, mX);
			double uwa = UnwrapAngle(mHeading - ta);

			if (fabs(uwa) < PIOFOUR)
			{
				//seeing the back of the vehicle

				mTargetPoints[midx(0, 0, mNumPoints)] = 0.0;
				mTargetPoints[midx(0, 1, mNumPoints)] = 0.5*mWidth;
				mTargetPoints[midx(1, 0, mNumPoints)] = 0.0;
				mTargetPoints[midx(1, 1, mNumPoints)] = -0.5*mWidth;
				mTargetPoints[midx(2, 0, mNumPoints)] = mLength;
				mTargetPoints[midx(2, 1, mNumPoints)] = 0.5*mWidth;
				mTargetPoints[midx(3, 0, mNumPoints)] = mLength;
				mTargetPoints[midx(3, 1, mNumPoints)] = -0.5*mWidth;
			}
			else if (fabs(uwa) > 3.0*PIOFOUR)
			{
				//seeing the front of the vehicle

				mTargetPoints[midx(0, 0, mNumPoints)] = 0.0;
				mTargetPoints[midx(0, 1, mNumPoints)] = 0.5*mWidth;
				mTargetPoints[midx(1, 0, mNumPoints)] = 0.0;
				mTargetPoints[midx(1, 1, mNumPoints)] = -0.5*mWidth;
				mTargetPoints[midx(2, 0, mNumPoints)] = -mLength;
				mTargetPoints[midx(2, 1, mNumPoints)] = 0.5*mWidth;
				mTargetPoints[midx(3, 0, mNumPoints)] = -mLength;
				mTargetPoints[midx(3, 1, mNumPoints)] = -0.5*mWidth;
			}
			else if (uwa > 0.0)
			{
				//seeing the left side of the vehicle

				mTargetPoints[midx(0, 0, mNumPoints)] = -0.5*mLength;
				mTargetPoints[midx(0, 1, mNumPoints)] = 0.0;
				mTargetPoints[midx(1, 0, mNumPoints)] = 0.5*mLength;
				mTargetPoints[midx(1, 1, mNumPoints)] = 0.0;
				mTargetPoints[midx(2, 0, mNumPoints)] = -0.5*mLength;
				mTargetPoints[midx(2, 1, mNumPoints)] = -mWidth;
				mTargetPoints[midx(3, 0, mNumPoints)] = 0.5*mLength;
				mTargetPoints[midx(3, 1, mNumPoints)] = -mWidth;
			}
			else
			{
				//seeing the right side of the vehicle

				mTargetPoints[midx(0, 0, mNumPoints)] = -0.5*mLength;
				mTargetPoints[midx(0, 1, mNumPoints)] = 0.0;
				mTargetPoints[midx(1, 0, mNumPoints)] = 0.5*mLength;
				mTargetPoints[midx(1, 1, mNumPoints)] = 0.0;
				mTargetPoints[midx(2, 0, mNumPoints)] = -0.5*mLength;
				mTargetPoints[midx(2, 1, mNumPoints)] = mWidth;
				mTargetPoints[midx(3, 0, mNumPoints)] = 0.5*mLength;
				mTargetPoints[midx(3, 1, mNumPoints)] = mWidth;
			}
		}

		//also set the default width variance
		mCovariance[midx(5, 5, T_NUMSTATES)] = TARGET_DEFAULTWIDTHVAR;

		break;
	}

	return;
}

void Target::RepairAnchorPoint()
{
	/*
	Repairs the anchor point to make sure it is inside or near the obstacle
	points.  Does not touch the anchor point if the target doesn't have any
	cluster points, or if the target does not need an anchor point reset.

	INPUTS:
		none.

	OUTPUTS:
		none.  Determines whether the target needs to have its anchor point
			reinitialized, and does so if inecessary.
	*/

	if (mNumPoints == 0)
	{
		//the target doesn't have any cluster points, so can't do anything
		return;
	}

	//determine the boundaries of the target by calculating its measurements
	double bmin = DBL_MAX;
	double bmax = -DBL_MAX;
	double ravg = 0.0;
	double wt = 1.0 / ((double) mNumPoints);

	int i;
	int j;
	int k;
	double wraptarget;

	//precompute trig values for the transformation
	double cosOrient = cos(mOrientation);
	double sinOrient = sin(mOrientation);
	//these will hold the new anchor point, if it is used
	double mXnew = 0.0;
	double mYnew = 0.0;

	for (i = 0; i < mNumPoints; i++)
	{
		//extract each target point
		double ox = mTargetPoints[midx(i, 0, mNumPoints)];
		double oy = mTargetPoints[midx(i, 1, mNumPoints)];
		//transform to ego vehicle coordinates
		double evx;
		double evy;
		ObjectToEgoVehicle(evx, evy, ox, oy, cosOrient, sinOrient, mX, mY);

		//these will store the new anchor point (but it may not be used)
		mXnew += wt * evx;
		mYnew += wt * evy;

		//convert to range and bearing
		double tr = sqrt(evx*evx + evy*evy);
		double tb = atan2(evy, evx);

		//make sure to wrap all angles toward the same 2pi interval
		if (i == 0)
		{
			wraptarget = tb;
		}
		tb = WrapAngle(tb, wraptarget);

		//calculate the bounds for the cluster: extreme angles, and average range
		if (tb < bmin)
		{
			bmin = tb;
		}
		if (tb > bmax)
		{
			bmax = tb;
		}
		ravg += wt*tr;
	}

	//calculate the range and bearing of the anchor point
	double ranc = sqrt(mX*mX + mY*mY);
	double banc = atan2(mY, mX);

	if (UnwrapAngle(bmin - banc) > TARGET_ANCHORANGLETOL || 
		UnwrapAngle(banc - bmax) > TARGET_ANCHORANGLETOL || 
		fabs(ravg - ranc) > TARGET_ANCHORRANGETOL)
	{
		//need to reset the anchor point

		//calculate this point's XY uncertainty in ego vehicle coordinates using
		//uncertainty in the transformation from object storage to ego vehicle.
		//NOTE: this approximates the new spatial uncertainty as the uncertainty
		//of the new anchor point under the old transformation.

		//the current anchor x, anchor y, and orientation are uncertain
		double Pxyo[9];
		for (i = 0; i < 3; i++)
		{
			for (j = 0; j < 3; j++)
			{
				Pxyo[midx(i, j, 3)] = mCovariance[midx(i, j, T_NUMSTATES)];
			}
		}
		//map these uncertainties into the new anchor point's location
		double J[2*3];
		J[midx(0, 0, 2)] = 1.0;
		J[midx(0, 1, 2)] = 0.0;
		J[midx(0, 2, 2)] = -sinOrient*mXnew - cosOrient*mYnew;
		J[midx(1, 0, 2)] = 0.0;
		J[midx(1, 1, 2)] = 1.0;
		J[midx(1, 2, 2)] = cosOrient*mXnew - sinOrient*mYnew;

		double JP[2*3];
		for (i = 0; i < 2; i++)
		{
			for (j = 0; j < 3; j++)
			{
				JP[midx(i, j, 2)] = 0.0;
				for (k = 0; k < 3; k++)
				{
					JP[midx(i, j, 2)] += J[midx(i, k, 2)]*Pxyo[midx(k, j, 3)];
				}
			}
		}

		//Pxynew = J*P*J'
		double Pxynew[4];
		for (i = 0; i < 2; i++)
		{
			for (j = 0; j < 2; j++)
			{
				Pxynew[midx(i, j, 2)] = 0.0;
				for (k = 0; k < 3; k++)
				{
					Pxynew[midx(i, j, 2)] += JP[midx(i, k, 2)]*J[midx(j, k, 2)];
				}
			}
		}

		//reset the anchor covariance and cross correlations with position too
		for (i = 0; i < 2; i++)
		{
			for (j = 0; j < T_NUMSTATES; j++)
			{
				mCovariance[midx(i, j, T_NUMSTATES)] = 0.0;
			}
		}
		for (i = 0; i < T_NUMSTATES; i++)
		{
			for (j = 0; j < 2; j++)
			{
				mCovariance[midx(i, j, T_NUMSTATES)] = 0.0;
			}
		}
		//copy the new XY spatial uncertainty
		for (i = 0; i < 2; i++)
		{
			for (j = 0; j < 2; j++)
			{
				mCovariance[midx(i, j, T_NUMSTATES)] = Pxynew[midx(i, j, 2)];
			}
		}

		for (i = 0; i < mNumPoints; i++)
		{
			//transform the point to the new object storage frame
			//extract the old obstacle point
			double ox = mTargetPoints[midx(i, 0, mNumPoints)];
			double oy = mTargetPoints[midx(i, 1, mNumPoints)];
			//transform to ego vehicle coordinates
			double evx;
			double evy;
			ObjectToEgoVehicle(evx, evy, ox, oy, cosOrient, sinOrient, mX, mY);

			//transform into the new object storage frame
			double osx;
			double osy;
			EgoVehicleToObject(osx, osy, evx, evy, cosOrient, sinOrient, mXnew, mYnew);
			//store the obstacle point in the new obstacle storage frame
			mTargetPoints[midx(i, 0, mNumPoints)] = osx;
			mTargetPoints[midx(i, 1, mNumPoints)] = osy;
		}

		//overwrite the old anchor point
		mX = mXnew;
		mY = mYnew;
	}

	return;
}

void Target::Initialize(MetaMeasurement* iMeasurement, Sensor* iSensor, VehicleOdometry* iVehicleOdometry)
{
	/*
	Initializes a target from a given measurement, sensor, and vehicle odometry

	INPUTS:
		iMeasurement - the measurement that will initialize the target
		iSensor - the sensor structure giving the sensor's location on the car
		iVehicleOdometry - the vehicle odometry structure

	OUTPUTS:
		none.  Initializes the target.
	*/

	if (mIsInitialized == true)
	{
		//target has already been initialized
		return;
	}

	int i;
	int j;

	double vx = iVehicleOdometry->vx;
	double vy = iVehicleOdometry->vy;
	double wz = iVehicleOdometry->wz;

	mLastUpdateTime = iMeasurement->MeasurementTime();
	mNumMeasurements = 1;

	//initialize the target depending on what type of measurement is received
	switch (iMeasurement->MeasurementType())
	{
	case MM_IBEOCLUSTER_00:
		{
			//initialize the target with a stable, fully visible ibeo cluster

			if (iMeasurement->NumDataPoints() == 0)
			{
				//can't initialize with an empty packet
				return;
			}

			//STATUS FLAGS
			mTargetType = T_IBEO;
			mIsInitialized = true;
			mLastIbeoUpdateTime = iMeasurement->MeasurementTime();
			mNumIbeoMeasurements = 1;

			//store the cluster points in the target
			double* iPacket = iMeasurement->DataPoints();
			mNumPoints = iMeasurement->NumDataPoints();
			mTargetPoints = new double[2*mNumPoints];

			//TARGET POINT DATA
			double sx = iSensor->SensorX;
			double sy = iSensor->SensorY;
			double syaw = iSensor->SensorYaw;
			double cosSyaw = cos(syaw);
			double sinSyaw = sin(syaw);
			for (i = 0; i < mNumPoints; i++)
			{
				//extract the point from the metameasurement
				double px = iPacket[midx(i, 0, mNumPoints)];
				double py = iPacket[midx(i, 1, mNumPoints)];
				//transform the point into vehicle coordinates
				double evx;
				double evy;
				SensorToEgoVehicle(evx, evy, px, py, cosSyaw, sinSyaw, sx, sy);

				//store the point temporarily in ego-vehicle coordinates
				mTargetPoints[midx(i, 0, mNumPoints)] = evx;
				mTargetPoints[midx(i, 1, mNumPoints)] = evy;
			}

			//STATE VECTOR
			double wt = 1.0 / ((double) mNumPoints);
			mX = 0.0;
			mY = 0.0;
			for (i = 0; i < mNumPoints; i++)
			{
				//calculate the center of mass in vehicle coordinates as the initial anchor point
				mX += wt * mTargetPoints[midx(i, 0, mNumPoints)];
				mY += wt * mTargetPoints[midx(i, 1, mNumPoints)];
			}
			//initialize the object storage frame to the current vantage point
			mOrientation = 0.0;
			double cosOrient = 1.0;
			double sinOrient = 0.0;
			for (i = 0; i < mNumPoints; i++)
			{
				//transform points from ego vehicle to object storage frame
				double px = mTargetPoints[midx(i, 0, mNumPoints)];
				double py = mTargetPoints[midx(i, 1, mNumPoints)];
				double osx;
				double osy;
				EgoVehicleToObject(osx, osy, px, py, cosOrient, sinOrient, mX, mY);
				mTargetPoints[midx(i, 0, mNumPoints)] = osx;
				mTargetPoints[midx(i, 1, mNumPoints)] = osy;
			}

			//COVARIANCE MATRIX
			//set the initial covariance
			for (i = 0; i < T_NUMSTATES; i++)
			{
				for (j = 0; j < T_NUMSTATES; j++)
				{
					mCovariance[midx(i, j, T_NUMSTATES)] = 0.0;
				}
			}

			double Pxy[4];
			double wt2 = wt*wt;
			for (i = 0; i < mNumPoints; i++)
			{
				//calculate initial variance for the anchor point
				double px = iPacket[midx(i, 0, mNumPoints)];
				double py = iPacket[midx(i, 1, mNumPoints)];

				XYVarFromRB(Pxy, px, py, IBEO_RANGEVAR, IBEO_BEARINGVAR, cosSyaw, sinSyaw);
				mCovariance[midx(0, 0, T_NUMSTATES)] += wt2 * Pxy[midx(0, 0, 2)];
				mCovariance[midx(0, 1, T_NUMSTATES)] += wt2 * Pxy[midx(0, 1, 2)];
				mCovariance[midx(1, 0, T_NUMSTATES)] += wt2 * Pxy[midx(1, 0, 2)];
				mCovariance[midx(1, 1, T_NUMSTATES)] += wt2 * Pxy[midx(1, 1, 2)];
			}
			//NOTE: orientation is also used for static objects
			mCovariance[midx(2, 2, T_NUMSTATES)] = IBEO_INITORIENTVAR;
		}

		break;

	case MM_MOBILEYEOBSTACLE:
		{
			//initialize the target with a mobileye obstacle measurement

			//STATUS FLAGS
			mTargetType = T_MOBILEYE;
			mIsInitialized = true;
			mLastMobileyeUpdateTime = iMeasurement->MeasurementTime();
			mNumMobileyeMeasurements = 1;

			double sx = iSensor->SensorX;
			double sy = iSensor->SensorY;
			double syaw = iSensor->SensorYaw;
			double cosSyaw = cos(syaw);
			double sinSyaw = sin(syaw);

			//STATE VECTOR
			//initialize position based on the sensor's car position
			double* zmobileye = iMeasurement->MeasurementData();
			double tsx = zmobileye[0];
			double tsy = zmobileye[1];
			SensorToEgoVehicle(mX, mY, tsx, tsy, cosSyaw, sinSyaw, sx, sy);
			//initialize speed and heading based on the sign of speed and ego motion
			double tss = zmobileye[2];
			//compute velocity of the sensor in ego vehicle coordinates
			double sevx = vx - wz*sy;
			double sevy = vy + wz*sx;
			//compute the absolute velocity of the sensor in sensor coordinates
			double svx = cosSyaw*sevx + sinSyaw*sevy;
			double svy = -sinSyaw*sevx + cosSyaw*sevy;
			//calculate initial speed as measured speed + component of sensor speed in the forward direction
			mSpeed = tss + svx;
			if (mSpeed >= 0.0)
			{
				//obstacle moving away from ego-vehicle along sensor forward direction
				mHeading = syaw;
			}
			else
			{
				//obstacle moving toward ego-vehicle along sensor forward direction
				mSpeed = -mSpeed;
				mHeading = UnwrapAngle(syaw + PI);
			}
			//initialize width based on width measurement
			double tsw = zmobileye[3];
			mWidth = tsw;

			//COVARIANCE MATRIX
			//set the initial covariance
			for (i = 0; i < T_NUMSTATES; i++)
			{
				for (j = 0; j < T_NUMSTATES; j++)
				{
					mCovariance[midx(i, j, T_NUMSTATES)] = 0.0;
				}
			}
			//T_MOBILEYE targets track all but orientation

			//extract mobileye xy covariance and transform to ego vehicle coordinates
			double* Rmobileye = iMeasurement->MeasurementCovariance();
			int nz = iMeasurement->MeasurementLength();
			double Psxsy[4];
			Psxsy[midx(0, 0, 2)] = Rmobileye[midx(0, 0, nz)];
			Psxsy[midx(0, 1, 2)] = Rmobileye[midx(0, 1, nz)];
			Psxsy[midx(1, 0, 2)] = Rmobileye[midx(1, 0, nz)];
			Psxsy[midx(1, 1, 2)] = Rmobileye[midx(1, 1, nz)];

			//initialize xy covariance from mobileye xy
			double Pxy[4];
			EgoVarFromSensorXY(Pxy, Psxsy, cosSyaw, sinSyaw);
			for (i = 0; i < 2; i++)
			{
				for (j = 0; j < 2; j++)
				{
					mCovariance[midx(i, j, T_NUMSTATES)] = Pxy[midx(i, j, 2)];
				}
			}
			//initialize speed variance from mobileye speed and vehicle odometry
			mCovariance[midx(3, 3, T_NUMSTATES)] = Rmobileye[midx(2, 2, nz)];
			//initialize heading variance from external parameters
			double Jevx = cosSyaw;
			double Jevy = sinSyaw;
			double Jewz = -sy*cosSyaw + sx*sinSyaw;
			mCovariance[midx(4, 4, T_NUMSTATES)] = MOBILEYE_INITHEADINGVAR + 
				Jevx*Jevx*iVehicleOdometry->qvx + Jevy*Jevy*iVehicleOdometry->qvy + 
				Jewz*Jewz*iVehicleOdometry->qwz;
			//initialize width variance from mobileye width
			mCovariance[midx(5, 5, T_NUMSTATES)] = Rmobileye[midx(3, 3, nz)];
		}

		break;

	case MM_RADAROBSTACLE:
		{
			//initialize the target with a radar obstacle measurement

			//STATUS FLAGS
			mTargetType = T_RADAR;
			mIsInitialized = true;
			mLastRadarUpdateTime = iMeasurement->MeasurementTime();
			mNumRadarMeasurements = 1;

			double sx = iSensor->SensorX;
			double sy = iSensor->SensorY;
			double syaw = iSensor->SensorYaw;
			double cosSyaw = cos(syaw);
			double sinSyaw = sin(syaw);

			//STATE VECTOR
			//initialize position based on the sensor's car position
			double* zradar = iMeasurement->MeasurementData();
			//extract range and bearing and convert to ego-vehicle coordinates
			double tsr = zradar[0];
			double tsb = zradar[1];
			double cosTsb = cos(tsb);
			double sinTsb = sin(tsb);
			SensorToEgoVehicle(mX, mY, tsr*cosTsb, tsr*sinTsb, cosSyaw, sinSyaw, sx, sy);
			//initialize speed and heading based on the sign of speed and ego motion
			double tsrr = zradar[2];
			//compute velocity of the sensor in ego vehicle coordinates
			double sevx = vx - wz*sy;
			double sevy = vy + wz*sx;
			//compute the velocity of the sensor in sensor coordinates
			double svx = sevx*cosSyaw + sevy*sinSyaw;
			double svy = -sevx*sinSyaw + sevy*cosSyaw;
			//calculate initial speed as measured range rate + component of sensor speed in the radial direction
			mSpeed = tsrr + svx*cosTsb + svy*sinTsb;
			if (mSpeed >= 0.0)
			{
				//obstacle moving away from ego-vehicle along radial direction
				mHeading = atan2(mY, mX);
			}
			else
			{
				//obstacle moving toward ego-vehicle along radial direction
				mSpeed = -mSpeed;
				mHeading = UnwrapAngle(atan2(mY, mX) + PI);
			}

			//COVARIANCE MATRIX
			//set the initial covariance
			for (i = 0; i < T_NUMSTATES; i++)
			{
				for (j = 0; j < T_NUMSTATES; j++)
				{
					mCovariance[midx(i, j, T_NUMSTATES)] = 0.0;
				}
			}
			//T_RADAR targets track position, speed, and heading

			//extract mobileye xy covariance and transform to ego vehicle coordinates
			double* Rradar = iMeasurement->MeasurementCovariance();
			int nz = iMeasurement->MeasurementLength();

			//initialize xy covariance from radar range and bearing variance
			double Pxy[4];
			XYVarFromRB(Pxy, tsr*cosTsb, tsr*sinTsb, Rradar[midx(0, 0, nz)], Rradar[midx(1, 1, nz)], cosSyaw, sinSyaw);
			for (i = 0; i < 2; i++)
			{
				for (j = 0; j < 2; j++)
				{
					mCovariance[midx(i, j, T_NUMSTATES)] = Pxy[midx(i, j, 2)];
				}
			}
			//initialize speed variance from radar speed and vehicle odometry
			double Jetb = -svx*sinTsb + svy*cosTsb;
			double Jevx = cosSyaw*cosTsb - sinSyaw*sinTsb;
			double Jevy = sinSyaw*cosTsb + cosSyaw*sinTsb;
			double Jewz = (-sy*cosSyaw + sx*sinSyaw)*cosTsb + (sy*sinSyaw + sx*cosSyaw)*sinTsb;
			mCovariance[midx(3, 3, T_NUMSTATES)] = Rradar[midx(2, 2, nz)] + 
				Jetb*Jetb*Rradar[midx(1, 1, nz)] + Jevx*Jevx*iVehicleOdometry->qvx + 
				Jevy*Jevy*iVehicleOdometry->qvy + Jewz*Jewz*iVehicleOdometry->qwz;
			mCovariance[midx(4, 4, T_NUMSTATES)] = RADAR_INITHEADINGVAR;
		}

		break;

	case MM_SIDESICKOBSTACLE:
		{
			//initialize the target with a side LIDAR detection

			//STATUS FLAGS
			mTargetType = T_IBEO;
			mIsInitialized = true;
			mLastSideSickUpdateTime = iMeasurement->MeasurementTime();
			mNumSideSickMeasurements = 1;

			//create a fake set of 4 points to symbolize the target
			double* z = iMeasurement->MeasurementData();
			double mind = z[0];
			double pcx = mind;
			double pcy = 0.0;
			double mw = SIDESICK_DEFAULTWIDTH;
			double hw = 0.5*mw;

			//create a fake set of sensor points
			double* iPacket = new double[4*2];
			iPacket[midx(0, 0, 4)] = pcx;
			iPacket[midx(0, 1, 4)] = pcy + hw;
			iPacket[midx(1, 0, 4)] = pcx;
			iPacket[midx(1, 1, 4)] = pcy - hw;
			iPacket[midx(2, 0, 4)] = pcx + mw;
			iPacket[midx(2, 1, 4)] = pcy + hw;
			iPacket[midx(3, 0, 4)] = pcx + mw;
			iPacket[midx(3, 1, 4)] = pcy - hw;

			mNumPoints = 4;
			mTargetPoints = new double[2*mNumPoints];

			//TARGET POINT DATA
			double sx = iSensor->SensorX;
			double sy = iSensor->SensorY;
			double syaw = iSensor->SensorYaw;
			double cosSyaw = cos(syaw);
			double sinSyaw = sin(syaw);
			for (i = 0; i < mNumPoints; i++)
			{
				//extract the point from the metameasurement
				double px = iPacket[midx(i, 0, mNumPoints)];
				double py = iPacket[midx(i, 1, mNumPoints)];
				//transform the point into vehicle coordinates
				double evx;
				double evy;
				SensorToEgoVehicle(evx, evy, px, py, cosSyaw, sinSyaw, sx, sy);

				//store the point temporarily in ego-vehicle coordinates
				mTargetPoints[midx(i, 0, mNumPoints)] = evx;
				mTargetPoints[midx(i, 1, mNumPoints)] = evy;
			}

			//STATE VECTOR
			double wt = 1.0 / ((double) mNumPoints);
			mX = 0.0;
			mY = 0.0;
			for (i = 0; i < mNumPoints; i++)
			{
				//calculate the center of mass in vehicle coordinates as the initial anchor point
				mX += wt * mTargetPoints[midx(i, 0, mNumPoints)];
				mY += wt * mTargetPoints[midx(i, 1, mNumPoints)];
			}
			//initialize the object storage frame to the current vantage point
			mOrientation = 0.0;
			double cosOrient = 1.0;
			double sinOrient = 0.0;
			for (i = 0; i < mNumPoints; i++)
			{
				//transform points from ego vehicle to object storage frame
				double px = mTargetPoints[midx(i, 0, mNumPoints)];
				double py = mTargetPoints[midx(i, 1, mNumPoints)];
				double osx;
				double osy;
				EgoVehicleToObject(osx, osy, px, py, cosOrient, sinOrient, mX, mY);
				mTargetPoints[midx(i, 0, mNumPoints)] = osx;
				mTargetPoints[midx(i, 1, mNumPoints)] = osy;
			}

			//COVARIANCE MATRIX
			//set the initial covariance
			for (i = 0; i < T_NUMSTATES; i++)
			{
				for (j = 0; j < T_NUMSTATES; j++)
				{
					mCovariance[midx(i, j, T_NUMSTATES)] = 0.0;
				}
			}

			double Pxy[4];
			double wt2 = wt*wt;
			for (i = 0; i < mNumPoints; i++)
			{
				//calculate initial variance for the anchor point
				double px = iPacket[midx(i, 0, mNumPoints)];
				double py = iPacket[midx(i, 1, mNumPoints)];

				XYVarFromRB(Pxy, px, py, SIDESICK_RANGEVAR, SIDESICK_BEARINGVAR, cosSyaw, sinSyaw);
				mCovariance[midx(0, 0, T_NUMSTATES)] += wt2 * Pxy[midx(0, 0, 2)];
				mCovariance[midx(0, 1, T_NUMSTATES)] += wt2 * Pxy[midx(0, 1, 2)];
				mCovariance[midx(1, 0, T_NUMSTATES)] += wt2 * Pxy[midx(1, 0, 2)];
				mCovariance[midx(1, 1, T_NUMSTATES)] += wt2 * Pxy[midx(1, 1, 2)];
			}
			//NOTE: orientation is also used for static objects
			mCovariance[midx(2, 2, T_NUMSTATES)] = IBEO_INITORIENTVAR;

			//delete the temporary measurement data
			delete [] iPacket;
		}

		break;
	}

	//also initialize target existence probability
	mExistenceProbability = TARGET_INITEXISTENCEPROB;

	switch (iMeasurement->MeasurementType())
	{
	case MM_IBEOCLUSTER_00:
		{
			//use ibeo sensor constants to initialize target probability

			mExistenceProbability = IBEO_ACCURACY*mExistenceProbability + IBEO_FPRATE*(1.0 - mExistenceProbability);
		}

		break;

	case MM_MOBILEYEOBSTACLE:
		{
			//use mobileye sensor constants to initialize target probability

			mExistenceProbability = MOBILEYE_OBSACCURACY*mExistenceProbability + MOBILEYE_OBSFPRATE*(1.0 - mExistenceProbability);
		}

		break;

	case MM_RADAROBSTACLE:
		{
			//use radar sensor constants to initialize target probability

			mExistenceProbability = RADAR_OBSACCURACY*mExistenceProbability + RADAR_OBSFPRATE*(1.0 - mExistenceProbability);
		}

		break;

	case MM_SIDESICKOBSTACLE:
		{
			//use side lidar constants to initialize target probability
			mExistenceProbability = SIDESICK_ACCURACY*mExistenceProbability + SIDESICK_FPRATE*(1.0 - mExistenceProbability);
		}
	}

	return;
}

bool Target::CanBeAssociated(MetaMeasurement* iMeasurement, Sensor* iSensor, VehicleOdometry* iVehicleOdometry)
{
	/*
	A fast determination as to whether the given measurement could possibly (reasonably)
	be assigned to this target.  This is a quick way to determine whether the measurement
	likelihood needs to be calculated for the measurement

	INPUTS:
		iMeasurement - the measurement for which likelihood will be computed
		iSensor - the sensor structure giving the sensor's location on the car
		iVehicleOdometry - the vehicle odometry structure

	OUTPUTS:
		rCanBeAssociated - true if the measurement could possibly be associated, false otherwise.
	*/

	bool rCanBeAssociated = true;

	//extract the measurement
	double* z = iMeasurement->MeasurementData();
	double* R = iMeasurement->MeasurementCovariance();

	//extract the sensor calibration parameters
	double cosSyaw = cos(iSensor->SensorYaw);
	double sinSyaw = sin(iSensor->SensorYaw);
	double sx = iSensor->SensorX;
	double sy = iSensor->SensorY;

	switch (iMeasurement->MeasurementType())
	{
	case MM_IBEOCLUSTER_00:
		{
			//check the average position to see if the target is anywhere near the measurement

			double bavg = 0.5*(z[0] + WrapAngle(z[1], z[0]));
			double rmin = z[2];

			//calculate the average position in sensor coordinates
			double xavg = rmin*cos(bavg);
			double yavg = rmin*sin(bavg);

			//check this against the target's average position in sensor coordinates
			double scx;
			double scy;
			EgoVehicleToSensor(scx, scy, mX, mY, cosSyaw, sinSyaw, sx, sy);

			if (sqrt(pow(xavg - scx, 2.0) + pow(yavg - scy, 2.0)) > TARGET_MAXASSOCDIST)
			{
				//this target is nowhere near the measurement
				rCanBeAssociated = false;
			}
		}

		break;

	case MM_IBEOCLUSTER_01:
	case MM_IBEOCLUSTER_02:
		{
			//check if the bearing is anywhere close to the target

			//extract the measurement angle
			double bnom = z[0];

			//calculate the target angle in ego vehicle coordinates
			double scx;
			double scy;
			EgoVehicleToSensor(scx, scy, mX, mY, cosSyaw, sinSyaw, sx, sy);

			double bt = atan2(scy, scx);

			if (fabs(UnwrapAngle(bnom - bt)) > TARGET_MAXASSOCANGLE)
			{
				//the target is nowhere near the measurement
				rCanBeAssociated = false;
			}
		}

		break;

	case MM_MOBILEYEOBSTACLE:
		{
			//check if the x-y position is anywhere close to the target

			//extract the target's nominal position in sensor coordinates
			double xm = z[0];
			double ym = z[1];

			//check this against the target's average position in sensor coordinates
			double scx;
			double scy;
			EgoVehicleToSensor(scx, scy, mX, mY, cosSyaw, sinSyaw, sx, sy);

			if (sqrt(pow(xm - scx, 2.0) + pow(ym - scy, 2.0)) > TARGET_MAXASSOCDIST)
			{
				//this target is nowhere near the measurement
				rCanBeAssociated = false;
			}
		}

		break;

	case MM_RADAROBSTACLE:
		{
			//check the average position to see if the target is anywhere near the measurement

			double rr = z[0];
			double br = z[1];

			//calculate the average position in sensor coordinates
			double xr = rr*cos(br);
			double yr = rr*sin(br);

			//check this against the target's average position in sensor coordinates
			double scx;
			double scy;
			EgoVehicleToSensor(scx, scy, mX, mY, cosSyaw, sinSyaw, sx, sy);

			if (sqrt(pow(xr - scx, 2.0) + pow(yr - scy, 2.0)) > TARGET_MAXASSOCDIST)
			{
				//this target is nowhere near the measurement
				rCanBeAssociated = false;
			}
		}

		break;

	case MM_SIDESICKOBSTACLE:
		{
			//check whether the target is even in line of sight of the sensor

			//NOTE: IsInLineOfSight is called here, so it doesn't need to be
			//called again in Likelihood.
			if (IsInLineOfSight(iSensor) == false)
			{
				rCanBeAssociated = false;
			}
		}

		break;
	}

	return rCanBeAssociated;
}

void Target::Likelihood(MetaMeasurement* iMeasurement, Sensor* iSensor, VehicleOdometry* iVehicleOdometry)
{
	/*
	Calculates the likelihood of a particular measurement being applied to this target.

	INPUTS:
		iMeasurement - the measurement for which likelihood will be computed
		iSensor - the sensor structure giving the sensor's location on the car
		iVehicleOdometry - the vehicle odometry structure

	OUTPUTS:
		none.  Sets lambda equal to the measurement's likelihood, with 0.0 for weird input 
			and errors.
	*/

	mLambda = 0.0;

	//store the new measurement type
	mMeasurementType = iMeasurement->MeasurementType();
	
	//delete the old measurement data
	delete [] nu;
	nu = NULL;
	delete [] S;
	S = NULL;
	delete [] W;
	W = NULL;

	if (CanBeAssociated(iMeasurement, iSensor, iVehicleOdometry) == false)
	{
		//this measurement can't be associated to this target, so don't bother
		//calculating a more complicated likelihood
		return;
	}

	int i;
	int j;
	int nz;
	int nx;

	//compute likelihood differenly based on measurement type
	switch (iMeasurement->MeasurementType())
	{
	case MM_IBEOCLUSTER_00:
		{
			//compute likelihood of the fully visible and stable cluster measurement

			//declare the measurement information for this type
			nz = 3;
			nu = new double[nz];
			S = new double[nz*nz];
			bool zwrap[3] = {true, true, false};

			switch (mTargetType)
			{
			case T_IBEO:
				{
					//applied to an immature ibeo target

					//declare the state matrices
					nx = 3;
					W = new double[nx*nz];

					Cluster iCluster;
					iCluster = TargetPointsCluster();

					//pull the relevant portions of the state
					double xbar[3] = {mX, mY, mOrientation};
					double Pbar[9];
					int idx[3] = {0, 1, 2};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					if (mBBRCache.IsCached(iMeasurement->MeasurementTime(), iSensor->SensorID, 
						iMeasurement->MeasurementType(), mNumMeasurements, nz, nx) == false)
					{
						//measurement is not cached, so need to brute force compute it
						mLambda = SigmaPointLikelihood(nu, S, W, nz, iMeasurement->MeasurementData(), zwrap, iMeasurement->MeasurementCovariance(), 
							nx, xbar, Pbar, &BcwBccwRminClusterMeasurement, iSensor, iVehicleOdometry, &iCluster, CHI2THRESH_3DOF);

						if (mLambda != 0.0)
						{
							//cache the measurement so it isn't computed again in this update
							mBBRCache.SetCache(iMeasurement->MeasurementTime(), iSensor->SensorID, 
								iMeasurement->MeasurementType(), mNumMeasurements, nz, nx, nu, S, W, 
								iMeasurement->MeasurementData(), iMeasurement->MeasurementCovariance());
						}
					}
					else
					{
						//measurement is already cached, so likelihood can be computed cheaply

						mLambda = mBBRCache.FastLikelihood(nu, S, W, iMeasurement->MeasurementData(), zwrap, 
							iMeasurement->MeasurementCovariance(), CHI2THRESH_3DOF);
					}
				}

				break;

			case T_IBEOMATURE:
			case T_MATURE:
				{
					//applied to a mature ibeo target

					//declare the state matrices
					nx = 5;
					W = new double[nx*nz];

					Cluster iCluster;
					iCluster = TargetPointsCluster();

					//pull the relevant portions of the state
					double xbar[5] = {mX, mY, mOrientation, mSpeed, mHeading};
					double Pbar[25];
					int idx[5] = {0, 1, 2, 3, 4};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					if (mBBRCache.IsCached(iMeasurement->MeasurementTime(), iSensor->SensorID, 
						iMeasurement->MeasurementType(), mNumMeasurements, nz, nx) == false)
					{
						//measurement is not cached, so need to brute force compute it
						mLambda = SigmaPointLikelihood(nu, S, W, nz, iMeasurement->MeasurementData(), zwrap, iMeasurement->MeasurementCovariance(), 
							nx, xbar, Pbar, &BcwBccwRminClusterMeasurement, iSensor, iVehicleOdometry, &iCluster, CHI2THRESH_3DOF);

						if (mLambda != 0.0)
						{
							//cache the measurement so it isn't computed again in this update
							mBBRCache.SetCache(iMeasurement->MeasurementTime(), iSensor->SensorID, 
								iMeasurement->MeasurementType(), mNumMeasurements, nz, nx, nu, S, W, 
								iMeasurement->MeasurementData(), iMeasurement->MeasurementCovariance());
						}
					}
					else
					{
						//measurement is already cached, so likelihood can be computed cheaply

						mLambda = mBBRCache.FastLikelihood(nu, S, W, iMeasurement->MeasurementData(), zwrap, 
							iMeasurement->MeasurementCovariance(), CHI2THRESH_3DOF);
					}
				}

				break;

			case T_MOBILEYE:
			case T_QUASIMATURE:
				{
					//applied to a mobileye target

					//declare the state matrices
					nx = 5;
					W = new double[nx*nz];

					//pull the relevant portions of the state
					double xbar[5] = {mX, mY, mSpeed, mHeading, mWidth};
					double Pbar[25];
					int idx[5] = {0, 1, 3, 4, 5};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					mLambda = ExtendedKalmanLikelihood(nu, S, W, nz, iMeasurement->MeasurementData(), zwrap, iMeasurement->MeasurementCovariance(), 
						nx, xbar, Pbar, &BcwBccwRminMeasurement, iSensor, iVehicleOdometry, NULL, CHI2THRESH_3DOF);
				}

				break;

			case T_RADAR:
				{
					//applied to a radar target

					//NOTE: immature radar targets don't use the measurement's
					//left and right bounds, only an average of the two...  so
					//the likelihood matrices need redefined
					int nzsmall = 2;
					delete [] nu;
					nu = new double[nzsmall];
					delete [] S;
					S = new double[nzsmall*nzsmall];

					//and only a portion of the measurement is needed
					double* z = iMeasurement->MeasurementData();
					double zsmall[2] = {0.5*(z[0] + z[1]), z[2]};
					bool zwrapsmall[2] = {true, false};
					double* R = iMeasurement->MeasurementCovariance();
					//note that since we're forming a new measurement, the covariance
					//matrix must change accordingly
					double Rsmall[4];
					Rsmall[midx(0, 0, 2)] = 0.25*(R[midx(0, 0, nz)] + R[midx(1, 1, nz)]) + 0.5*R[midx(0, 1, nz)];
					Rsmall[midx(0, 1, 2)] = 0.5*(R[midx(0, 2, nz)] + R[midx(1, 2, nz)]);
					Rsmall[midx(1, 0, 2)] = 0.5*(R[midx(2, 0, nz)] + R[midx(2, 1, nz)]);
					Rsmall[midx(1, 1, 2)] = R[midx(2, 2, nz)];

					//declare the state matrices
					nx = 4;
					W = new double[nx*nz];

					//pull the relevant portions of the state
					double xbar[4] = {mX, mY, mSpeed, mHeading};
					double Pbar[16];
					int idx[4] = {0, 1, 3, 4};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					mLambda = ExtendedKalmanLikelihood(nu, S, W, nzsmall, zsmall, zwrapsmall, Rsmall, 
						nx, xbar, Pbar, &BcwBccwRminNoWidthMeasurement, iSensor, iVehicleOdometry, NULL, CHI2THRESH_2DOF);
					//NOTE: since radar targets only use the average bearing and the closest range,
					//assume a uniform likelihood over the remaining measurement, which is the
					//angular spread (I've done a linear transformation on the measurements, transforming
					//left and right bearings into average bearing and angular spread)
					mLambda *= 1.0/IBEO_BSPAN;
				}

				break;
			}
		}

		break;

	case MM_IBEOCLUSTER_01:
		{
			//compute likelihood of an ibeo cluster with only a clockwise corner visible

			//declare the measurement information for this type
			nz = 1;
			nu = new double[nz];
			S = new double[nz*nz];
			bool zwrap[1] = {true};

			switch (mTargetType)
			{
			case T_IBEO:
				{
					//applied to an immature ibeo target

					//declare the state matrices
					nx = 3;
					W = new double[nx*nz];

					Cluster iCluster;
					iCluster = TargetPointsCluster();

					//pull the relevant portions of the state
					double xbar[3] = {mX, mY, mOrientation};
					double Pbar[9];
					int idx[3] = {0, 1, 2};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					if (mCWBCache.IsCached(iMeasurement->MeasurementTime(), iSensor->SensorID, 
						iMeasurement->MeasurementType(), mNumMeasurements, nz, nx) == false)
					{
						//measurement is not cached, so need to brute force compute it
						mLambda = SigmaPointLikelihood(nu, S, W, nz, iMeasurement->MeasurementData(), zwrap, 
							iMeasurement->MeasurementCovariance(), nx, xbar, Pbar, &BcwClusterMeasurement, 
							iSensor, iVehicleOdometry, &iCluster, CHI2THRESH_1DOF);

						if (mLambda != 0.0)
						{
							//cache the measurement so it isn't computed again in this update
							mCWBCache.SetCache(iMeasurement->MeasurementTime(), iSensor->SensorID, 
								iMeasurement->MeasurementType(), mNumMeasurements, nz, nx, nu, S, W, 
								iMeasurement->MeasurementData(), iMeasurement->MeasurementCovariance());
						}
					}
					else
					{
						//measurement is already cached, so likelihood can be computed cheaply

						mLambda = mCWBCache.FastLikelihood(nu, S, W, iMeasurement->MeasurementData(), zwrap, 
							iMeasurement->MeasurementCovariance(), CHI2THRESH_1DOF);
					}
				}

				break;

			case T_IBEOMATURE:
			case T_MATURE:
				{
					//applied to a mature ibeo target

					//declare the state matrices
					nx = 5;
					W = new double[nx*nz];

					Cluster iCluster;
					iCluster = TargetPointsCluster();

					//pull the relevant portions of the state
					double xbar[5] = {mX, mY, mOrientation, mSpeed, mHeading};
					double Pbar[25];
					int idx[5] = {0, 1, 2, 3, 4};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					if (mCWBCache.IsCached(iMeasurement->MeasurementTime(), iSensor->SensorID, 
						iMeasurement->MeasurementType(), mNumMeasurements, nz, nx) == false)
					{
						//measurement is not cached, so need to brute force compute it
						mLambda = SigmaPointLikelihood(nu, S, W, nz, iMeasurement->MeasurementData(), zwrap, 
							iMeasurement->MeasurementCovariance(), nx, xbar, Pbar, &BcwClusterMeasurement, 
							iSensor, iVehicleOdometry, &iCluster, CHI2THRESH_1DOF);

						if (mLambda != 0.0)
						{
							//cache the measurement so it isn't computed again in this update
							mCWBCache.SetCache(iMeasurement->MeasurementTime(), iSensor->SensorID, 
								iMeasurement->MeasurementType(), mNumMeasurements, nz, nx, nu, S, W, 
								iMeasurement->MeasurementData(), iMeasurement->MeasurementCovariance());
						}
					}
					else
					{
						//measurement is already cached, so likelihood can be computed cheaply

						mLambda = mCWBCache.FastLikelihood(nu, S, W, iMeasurement->MeasurementData(), zwrap, 
							iMeasurement->MeasurementCovariance(), CHI2THRESH_1DOF);
					}
				}

				break;

			case T_MOBILEYE:
			case T_QUASIMATURE:
				{
					//applied to a mobileye target

					//declare the state matrices
					nx = 5;
					W = new double[nx*nz];

					//pull the relevant portions of the state
					double xbar[5] = {mX, mY, mSpeed, mHeading, mWidth};
					double Pbar[25];
					int idx[5] = {0, 1, 3, 4, 5};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					mLambda = ExtendedKalmanLikelihood(nu, S, W, nz, iMeasurement->MeasurementData(), zwrap, iMeasurement->MeasurementCovariance(), 
						nx, xbar, Pbar, &BcwMeasurement, iSensor, iVehicleOdometry, NULL, CHI2THRESH_1DOF);
				}

				break;

			case T_RADAR:
				{
					//NOTE: a radar-only target can't be updated with a single corner measurement, since
					//the radar has no idea how big the target is.

					delete [] nu;
					nu = NULL;
					delete [] S;
					S = NULL;
					delete [] W;
					W = NULL;

					mLambda = 0.0;
				}

				break;
			}
		}

		break;

	case MM_IBEOCLUSTER_02:
		{
			//compute likelihood of ibeo cluster with only ccw boundary visible

			//declare the measurement information for this type
			nz = 1;
			nu = new double[nz];
			S = new double[nz*nz];
			bool zwrap[1] = {true};

			switch (mTargetType)
			{
			case T_IBEO:
				{
					//applied to an immature ibeo target

					//declare the state matrices
					nx = 3;
					W = new double[nx*nz];

					Cluster iCluster;
					iCluster = TargetPointsCluster();

					//pull the relevant portions of the state
					double xbar[3] = {mX, mY, mOrientation};
					double Pbar[9];
					int idx[3] = {0, 1, 2};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					if (mCCWBCache.IsCached(iMeasurement->MeasurementTime(), iSensor->SensorID, 
						iMeasurement->MeasurementType(), mNumMeasurements, nz, nx) == false)
					{
						//measurement is not cached, so need to brute force compute it
						mLambda = SigmaPointLikelihood(nu, S, W, nz, iMeasurement->MeasurementData(), zwrap, 
							iMeasurement->MeasurementCovariance(), nx, xbar, Pbar, &BccwClusterMeasurement, 
							iSensor, iVehicleOdometry, &iCluster, CHI2THRESH_1DOF);

						if (mLambda != 0.0)
						{
							//cache the measurement so it isn't computed again in this update
							mCCWBCache.SetCache(iMeasurement->MeasurementTime(), iSensor->SensorID, 
								iMeasurement->MeasurementType(), mNumMeasurements, nz, nx, nu, S, W, 
								iMeasurement->MeasurementData(), iMeasurement->MeasurementCovariance());
						}
					}
					else
					{
						//measurement is already cached, so likelihood can be computed cheaply

						mLambda = mCCWBCache.FastLikelihood(nu, S, W, iMeasurement->MeasurementData(), zwrap, 
							iMeasurement->MeasurementCovariance(), CHI2THRESH_1DOF);
					}
				}

				break;

			case T_IBEOMATURE:
			case T_MATURE:
				{
					//applied to a mature ibeo target

					//declare the state matrices
					nx = 5;
					W = new double[nx*nz];

					Cluster iCluster;
					iCluster = TargetPointsCluster();

					//pull the relevant portions of the state
					double xbar[5] = {mX, mY, mOrientation, mSpeed, mHeading};
					double Pbar[25];
					int idx[5] = {0, 1, 2, 3, 4};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					if (mCCWBCache.IsCached(iMeasurement->MeasurementTime(), iSensor->SensorID, 
						iMeasurement->MeasurementType(), mNumMeasurements, nz, nx) == false)
					{
						//measurement is not cached, so need to brute force compute it
						mLambda = SigmaPointLikelihood(nu, S, W, nz, iMeasurement->MeasurementData(), zwrap, 
							iMeasurement->MeasurementCovariance(), nx, xbar, Pbar, &BccwClusterMeasurement, 
							iSensor, iVehicleOdometry, &iCluster, CHI2THRESH_1DOF);

						if (mLambda != 0.0)
						{
							//cache the measurement so it isn't computed again in this update
							mCCWBCache.SetCache(iMeasurement->MeasurementTime(), iSensor->SensorID, 
								iMeasurement->MeasurementType(), mNumMeasurements, nz, nx, nu, S, W, 
								iMeasurement->MeasurementData(), iMeasurement->MeasurementCovariance());
						}
					}
					else
					{
						//measurement is already cached, so likelihood can be computed cheaply

						mLambda = mCCWBCache.FastLikelihood(nu, S, W, iMeasurement->MeasurementData(), zwrap, 
							iMeasurement->MeasurementCovariance(), CHI2THRESH_1DOF);
					}
				}

				break;

			case T_MOBILEYE:
			case T_QUASIMATURE:
				{
					//applied to a mobileye target

					//declare the state matrices
					nx = 5;
					W = new double[nx*nz];

					//pull the relevant portions of the state
					double xbar[5] = {mX, mY, mSpeed, mHeading, mWidth};
					double Pbar[25];
					int idx[5] = {0, 1, 3, 4, 5};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					mLambda = ExtendedKalmanLikelihood(nu, S, W, nz, iMeasurement->MeasurementData(), zwrap, iMeasurement->MeasurementCovariance(), 
						nx, xbar, Pbar, &BccwMeasurement, iSensor, iVehicleOdometry, NULL, CHI2THRESH_1DOF);
				}

				break;

			case T_RADAR:
				{
					//NOTE: a radar-only target can't be updated with a single corner measurement, since
					//the radar has no idea how big the target is.

					delete [] nu;
					nu = NULL;
					delete [] S;
					S = NULL;
					delete [] W;
					W = NULL;

					mLambda = 0.0;
				}

				break;
			}
		}

		break;

	case MM_MOBILEYEOBSTACLE:
		{
			//compute likelihood of mobileye obstacle measurement

			//declare the measurement information for this type
			nz = 4;
			nu = new double[nz];
			S = new double[nz*nz];
			bool zwrap[4] = {false, false, false, false};

			switch (mTargetType)
			{
			case T_IBEO:
				{
					//applied to an immature ibeo target

					//NOTE: immature ibeo targets don't use the mobileye's speed
					//measurement, so the likelihood matrices need redefined
					int nzsmall = 3;
					delete [] nu;
					nu = new double[nzsmall];
					delete [] S;
					S = new double[nzsmall*nzsmall];

					//and only a portion of the measurement is needed
					double* z = iMeasurement->MeasurementData();
					double zsmall[3] = {z[0], z[1], z[3]};
					bool zwrapsmall[3] = {false, false, false};
					double* R = iMeasurement->MeasurementCovariance();
					double Rsmall[9];
					int idxz[3] = {0, 1, 3};
					for (i = 0; i < nzsmall; i++)
					{
						for (j = 0; j < nzsmall; j++)
						{
							Rsmall[midx(i, j, nzsmall)] = R[midx(idxz[i], idxz[j], nz)];
						}
					}

					//declare the state matrices
					nx = 3;
					W = new double[nx*nz];

					Cluster iCluster;
					iCluster = TargetPointsCluster();

					//pull the relevant portions of the state
					double xbar[3] = {mX, mY, mOrientation};
					double Pbar[9];
					int idx[3] = {0, 1, 2};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					if (mMobileyeCache.IsCached(iMeasurement->MeasurementTime(), iSensor->SensorID, 
						iMeasurement->MeasurementType(), mNumMeasurements, nzsmall, nx) == false)
					{
						//measurement is not cached, so need to brute force compute it

						mLambda = SigmaPointLikelihood(nu, S, W, nzsmall, zsmall, zwrapsmall, Rsmall, 
							nx, xbar, Pbar, &MobileyeNoSpeedClusterMeasurement, iSensor, iVehicleOdometry, 
							&iCluster, CHI2THRESH_3DOF);
						//NOTE: since immature ibeo targets don't use the mobileye speed measurement,
						//assume a uniform likelihood over the mobileye's speed span
						mLambda *= 1.0/MOBILEYE_SSPAN;

						if (mLambda != 0.0)
						{
							//cache the measurement so it isn't computed again in this update
							mMobileyeCache.SetCache(iMeasurement->MeasurementTime(), iSensor->SensorID, 
								iMeasurement->MeasurementType(), mNumMeasurements, nzsmall, nx, nu, 
								S, W, zsmall, Rsmall);
						}
					}
					else
					{
						//measurement is already cached, so likelihood can be computed cheaply

						mLambda = mMobileyeCache.FastLikelihood(nu, S, W, zsmall, zwrapsmall, 
							Rsmall, CHI2THRESH_3DOF);
						//NOTE: since immature ibeo targets don't use the mobileye speed measurement,
						//assume a uniform likelihood over the mobileye's speed span
						mLambda *= 1.0/MOBILEYE_SSPAN;
					}
				}

				break;

			case T_IBEOMATURE:
			case T_MATURE:
				{
					//applied to a mature ibeo target
					//(mature ibeo targets use the entire mobileye update)

					//declare the state matrices
					nx = 5;
					W = new double[nx*nz];

					Cluster iCluster;
					iCluster = TargetPointsCluster();

					//pull the relevant portions of the state
					double xbar[5] = {mX, mY, mOrientation, mSpeed, mHeading};
					double Pbar[25];
					int idx[5] = {0, 1, 2, 3, 4};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					if (mMobileyeCache.IsCached(iMeasurement->MeasurementTime(), iSensor->SensorID, 
						iMeasurement->MeasurementType(), mNumMeasurements, nz, nx) == false)
					{
						//measurement is not cached, so need to brute force compute it

						mLambda = SigmaPointLikelihood(nu, S, W, nz, iMeasurement->MeasurementData(), zwrap, 
							iMeasurement->MeasurementCovariance(), nx, xbar, Pbar, &MobileyeClusterMeasurement, 
							iSensor, iVehicleOdometry, &iCluster, CHI2THRESH_4DOF);

						if (mLambda != 0.0)
						{
							//cache the measurement so it isn't computed again in this update
							mMobileyeCache.SetCache(iMeasurement->MeasurementTime(), iSensor->SensorID, 
								iMeasurement->MeasurementType(), mNumMeasurements, nz, nx, nu, 
								S, W, iMeasurement->MeasurementData(), iMeasurement->MeasurementCovariance());
						}
					}
					else
					{
						//measurement is already cached, so likelihood can be computed cheaply

						mLambda = mMobileyeCache.FastLikelihood(nu, S, W, iMeasurement->MeasurementData(), 
							zwrap, iMeasurement->MeasurementCovariance(), CHI2THRESH_4DOF);
					}
				}

				break;

			case T_MOBILEYE:
			case T_QUASIMATURE:
				{
					//applied to a mobileye-only object

					//declare the state matrices
					nx = 5;
					W = new double[nx*nz];

					//pull the relevant portions of the state
					double xbar[5] = {mX, mY, mSpeed, mHeading, mWidth};
					double Pbar[25];
					int idx[5] = {0, 1, 3, 4, 5};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					mLambda = ExtendedKalmanLikelihood(nu, S, W, nz, iMeasurement->MeasurementData(), zwrap, iMeasurement->MeasurementCovariance(), 
						nx, xbar, Pbar, &MobileyeMeasurement, iSensor, iVehicleOdometry, NULL, CHI2THRESH_4DOF);
				}

				break;

			case T_RADAR:
				{
					//applied to a radar-only object

					//NOTE: radar targets don't use the mobileye's width
					//measurement, so the likelihood matrices need redefined
					int nzsmall = 3;
					delete [] nu;
					nu = new double[nzsmall];
					delete [] S;
					S = new double[nzsmall*nzsmall];

					//and only a portion of the measurement is needed
					double* z = iMeasurement->MeasurementData();
					double zsmall[3] = {z[0], z[1], z[2]};
					bool zwrapsmall[3] = {false, false, false};
					double* R = iMeasurement->MeasurementCovariance();
					double Rsmall[9];
					int idxz[3] = {0, 1, 2};
					for (i = 0; i < nzsmall; i++)
					{
						for (j = 0; j < nzsmall; j++)
						{
							Rsmall[midx(i, j, nzsmall)] = R[midx(idxz[i], idxz[j], nz)];
						}
					}

					//declare the state matrices
					nx = 4;
					W = new double[nx*nzsmall];

					//pull the relevant portions of the state
					double xbar[4] = {mX, mY, mSpeed, mHeading};
					double Pbar[16];
					int idx[4] = {0, 1, 3, 4};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					mLambda = ExtendedKalmanLikelihood(nu, S, W, nzsmall, zsmall, zwrapsmall, Rsmall, 
						nx, xbar, Pbar, &MobileyeNoWidthMeasurement, iSensor, iVehicleOdometry, NULL, CHI2THRESH_3DOF);
					//since the width measurement is not used, model it as uniform over the sensor's range
					mLambda *= 1.0/MOBILEYE_WSPAN;
				}

				break;
			}
		}

		break;

	case MM_RADAROBSTACLE:
		{
			//compute likelihood of radar obstacle measurement

			//declare the measurement information for this type
			nz = 3;
			nu = new double[nz];
			S = new double[nz*nz];
			bool zwrap[3] = {false, true, false};

			switch (mTargetType)
			{
			case T_IBEO:
				{
					//applied to an immature ibeo target

					//NOTE: immature ibeo targets only use the radar's range and bearing
					//measurements, so the likelihood matrices need redefined
					int nzsmall = 2;
					delete [] nu;
					nu = new double[nzsmall];
					delete [] S;
					S = new double[nzsmall*nzsmall];

					//and only a portion of the measurement is needed
					double* z = iMeasurement->MeasurementData();
					double zsmall[2] = {z[0], z[1]};
					bool zwrapsmall[2] = {false, true};
					double* R = iMeasurement->MeasurementCovariance();
					int idxz[2] = {0, 1};
					double Rsmall[4];
					for (i = 0; i < nzsmall; i++)
					{
						for (j = 0; j < nzsmall; j++)
						{
							Rsmall[midx(i, j, nzsmall)] = R[midx(idxz[i], idxz[j], nz)];
						}
					}

					//declare the state matrices
					nx = 3;
					W = new double[nx*nzsmall];

					Cluster iCluster;
					iCluster = TargetPointsCluster();

					//pull the relevant portions of the state
					double xbar[3] = {mX, mY, mOrientation};
					double Pbar[9];
					int idx[3] = {0, 1, 2};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					if (mRadarCache.IsCached(iMeasurement->MeasurementTime(), iSensor->SensorID, 
						iMeasurement->MeasurementType(), mNumMeasurements, nzsmall, nx) == false)
					{
						//measurement is not cached, so need to brute force compute it

						mLambda = SigmaPointLikelihood(nu, S, W, nzsmall, zsmall, zwrapsmall, Rsmall, 
							nx, xbar, Pbar, &RangeBearingClusterMeasurement, iSensor, iVehicleOdometry, &iCluster, CHI2THRESH_2DOF);
						//since only the range and bearing of the radar measurement are used, model the 
						//range rate as uniform over the radar's valid values
						mLambda *= 1.0/RADAR_RRSPAN;

						if (mLambda != 0.0)
						{
							//cache the measurement so it isn't computed again in this update
							mRadarCache.SetCache(iMeasurement->MeasurementTime(), iSensor->SensorID, 
								iMeasurement->MeasurementType(), mNumMeasurements, nzsmall, nx, nu, 
								S, W, zsmall, Rsmall);
						}
					}
					else
					{
						//measurement is already cached, so likelihood can be computed cheaply

						mLambda = mRadarCache.FastLikelihood(nu, S, W, zsmall, zwrapsmall, Rsmall, CHI2THRESH_2DOF);
						//since only the range and bearing of the radar measurement are used, model the 
						//range rate as uniform over the radar's valid values
						mLambda *= 1.0/RADAR_RRSPAN;
					}
				}

				break;

			case T_IBEOMATURE:
			case T_MATURE:
				{
					//applied to a mature ibeo target

					//declare the state matrices
					nx = 5;
					W = new double[nx*nz];

					Cluster iCluster;
					iCluster = TargetPointsCluster();

					//pull the relevant portions of the state
					double xbar[5] = {mX, mY, mOrientation, mSpeed, mHeading};
					double Pbar[25];
					int idx[5] = {0, 1, 2, 3, 4};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					if (mRadarCache.IsCached(iMeasurement->MeasurementTime(), iSensor->SensorID, 
						iMeasurement->MeasurementType(), mNumMeasurements, nz, nx) == false)
					{
						//measurement is not cached, so need to brute force compute it

						mLambda = SigmaPointLikelihood(nu, S, W, nz, iMeasurement->MeasurementData(), zwrap, 
							iMeasurement->MeasurementCovariance(), nx, xbar, Pbar, &RangeBearingRateClusterMeasurement, 
							iSensor, iVehicleOdometry, &iCluster, CHI2THRESH_3DOF);

						if (mLambda != 0.0)
						{
							//cache the measurement so it isn't computed again in this update
							mRadarCache.SetCache(iMeasurement->MeasurementTime(), iSensor->SensorID, 
								iMeasurement->MeasurementType(), mNumMeasurements, nz, nx, nu, 
								S, W, iMeasurement->MeasurementData(), iMeasurement->MeasurementCovariance());
						}
					}
					else
					{
						//measurement is already cached, so likelihood can be computed cheaply

						mLambda = mRadarCache.FastLikelihood(nu, S, W, iMeasurement->MeasurementData(), zwrap, 
							iMeasurement->MeasurementCovariance(), CHI2THRESH_3DOF);
					}
				}

				break;

			case T_MOBILEYE:
			case T_QUASIMATURE:
				{
					//applied to a mobileye-only object

					//declare the state matrices
					nx = 5;
					W = new double[nx*nz];

					//pull the relevant portions of the state
					double xbar[5] = {mX, mY, mSpeed, mHeading, mWidth};
					double Pbar[25];
					int idx[5] = {0, 1, 3, 4, 5};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					mLambda = ExtendedKalmanLikelihood(nu, S, W, nz, iMeasurement->MeasurementData(), zwrap, iMeasurement->MeasurementCovariance(), 
						nx, xbar, Pbar, &RadarMeasurement, iSensor, iVehicleOdometry, NULL, CHI2THRESH_3DOF);
				}

				break;

			case T_RADAR:
				{
					//applied to a radar-only object

					//declare the state matrices
					nx = 4;
					W = new double[nx*nz];

					//pull the relevant portions of the state
					double xbar[4] = {mX, mY, mSpeed, mHeading};
					double Pbar[16];
					int idx[4] = {0, 1, 3, 4};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					mLambda = ExtendedKalmanLikelihood(nu, S, W, nz, iMeasurement->MeasurementData(), zwrap, iMeasurement->MeasurementCovariance(), 
						nx, xbar, Pbar, &RadarMeasurement, iSensor, iVehicleOdometry, NULL, CHI2THRESH_3DOF);
				}

				break;
			}
		}

		break;

	case MM_SIDESICKOBSTACLE:
		{
			//compute likelihood of side lidar obstacle measurement

			//declare the measurement information for this type
			nz = 1;
			nu = new double[nz];
			S = new double[nz*nz];
			bool zwrap[1] = {false};

			switch (mTargetType)
			{
			case T_IBEO:
				{
					//applied to an immature ibeo target

					//declare the state matrices
					nx = 3;
					W = new double[nx*nz];

					Cluster iCluster;
					iCluster = TargetPointsCluster();

					//pull the relevant portions of the state
					double xbar[3] = {mX, mY, mOrientation};
					double Pbar[9];
					int idx[3] = {0, 1, 2};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					//NOTE: IsInLineOfSight() is already confirmed to be true in CanBeAssociated...
					//if it were false, likelihood would already be zero.
					if (mSideSickCache.IsCached(iMeasurement->MeasurementTime(), iSensor->SensorID, 
						iMeasurement->MeasurementType(), mNumMeasurements, nz, nx) == false)
					{
						//measurement is not cached, so need to brute force compute it

						mLambda = SigmaPointLikelihood(nu, S, W, nz, iMeasurement->MeasurementData(), 
							zwrap, iMeasurement->MeasurementCovariance(), nx, xbar, Pbar, 
							&SensorDirectionalDistanceCluster, iSensor, iVehicleOdometry, 
							&iCluster, CHI2THRESH_1DOF);

						if (mLambda != 0.0)
						{
							//cache the measurement so it isn't computed again in this update
							mSideSickCache.SetCache(iMeasurement->MeasurementTime(), iSensor->SensorID, 
								iMeasurement->MeasurementType(), mNumMeasurements, nz, nx, nu, 
								S, W, iMeasurement->MeasurementData(), iMeasurement->MeasurementCovariance());
						}
					}
					else
					{
						//measurement is already cached, so likelihood can be computed cheaply

						mLambda = mSideSickCache.FastLikelihood(nu, S, W, iMeasurement->MeasurementData(), zwrap, 
							iMeasurement->MeasurementCovariance(), CHI2THRESH_1DOF);
					}
				}

				break;

			case T_IBEOMATURE:
			case T_MATURE:
				{
					//applied to a mature ibeo target

					//declare the state matrices
					nx = 5;
					W = new double[nx*nz];

					Cluster iCluster;
					iCluster = TargetPointsCluster();

					//pull the relevant portions of the state
					double xbar[5] = {mX, mY, mOrientation, mSpeed, mHeading};
					double Pbar[5*5];
					int idx[5] = {0, 1, 2, 3, 4};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					//NOTE: IsInLineOfSight() is already confirmed to be true in CanBeAssociated...
					//if it were false, likelihood would already be zero.
					if (mSideSickCache.IsCached(iMeasurement->MeasurementTime(), iSensor->SensorID, 
						iMeasurement->MeasurementType(), mNumMeasurements, nz, nx) == false)
					{
						//measurement is not cached, so need to brute force compute it

						mLambda = SigmaPointLikelihood(nu, S, W, nz, iMeasurement->MeasurementData(), 
							zwrap, iMeasurement->MeasurementCovariance(), nx, xbar, Pbar, 
							&SensorDirectionalDistanceCluster, iSensor, iVehicleOdometry, &iCluster, 
							CHI2THRESH_1DOF);

						if (mLambda != 0.0)
						{
							//cache the measurement so it isn't computed again in this update
							mSideSickCache.SetCache(iMeasurement->MeasurementTime(), iSensor->SensorID, 
								iMeasurement->MeasurementType(), mNumMeasurements, nz, nx, nu, 
								S, W, iMeasurement->MeasurementData(), iMeasurement->MeasurementCovariance());
						}
					}
					else
					{
						//measurement is already cached, so likelihood can be computed cheaply

						mLambda = mSideSickCache.FastLikelihood(nu, S, W, iMeasurement->MeasurementData(), zwrap, 
							iMeasurement->MeasurementCovariance(), CHI2THRESH_1DOF);
					}
				}

				break;

			case T_MOBILEYE:
			case T_QUASIMATURE:
				{
					//applied to a mobileye-only object

					//declare the state matrices
					nx = 5;
					W = new double[nx*nz];

					//pull the relevant portions of the state
					double xbar[5] = {mX, mY, mSpeed, mHeading, mWidth};
					double Pbar[5*5];
					int idx[5] = {0, 1, 3, 4, 5};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					//NOTE: IsInLineOfSight() is already confirmed to be true in CanBeAssociated...
					//if it were false, likelihood would already be zero.
					mLambda = ExtendedKalmanLikelihood(nu, S, W, nz, iMeasurement->MeasurementData(), zwrap, 
						iMeasurement->MeasurementCovariance(), nx, xbar, Pbar, &SensorDirectionalDistance, 
						iSensor, iVehicleOdometry, NULL, CHI2THRESH_1DOF);
				}

				break;

			case T_RADAR:
				{
					//applied to a radar-only object

					//declare the state matrices
					nx = 4;
					W = new double[nx*nz];

					//pull the relevant portions of the state
					double xbar[4] = {mX, mY, mSpeed, mHeading};
					double Pbar[4*4];
					int idx[4] = {0, 1, 3, 4};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					//NOTE: IsInLineOfSight() is already confirmed to be true in CanBeAssociated...
					//if it were false, likelihood would already be zero.
					mLambda = ExtendedKalmanLikelihood(nu, S, W, nz, iMeasurement->MeasurementData(), zwrap, 
						iMeasurement->MeasurementCovariance(), nx, xbar, Pbar, &SensorDirectionalDistanceNoWidth, 
						iSensor, iVehicleOdometry, NULL, CHI2THRESH_1DOF);
				}

				break;
			}
		}

		break;
	}

	return;
}

void Target::Predict(double iPredictDt, VehicleOdometry* iVehicleOdometry)
{
	/*
	Performs a prediction step on the target to advance its time.

	INPUTS:
		iPredictDt - time interval over which the prediction is to occur
		iVehicleOdometry - vehicle odometry structure describing the ego-vehicle's
			motion during the prediction

	OUTPUTS:
		none.  Updates the state variables and the state covariance over the
			prediction time interval
	*/

	if (iPredictDt <= 0.0)
	{
		//do not predict backwards
		return;
	}

	int i;
	int j;
	int nx;
	int nv;
	double dt = iPredictDt;

	switch (mTargetType)
	{
	case T_IBEO:
		{
			//immature ibeo-only targets don't move

			//declare the state matrices
			nx = 3;
			double x[3] = {mX, mY, mOrientation};
			double P[9];
			int idx[3] = {0, 1, 2};
			for (i = 0; i < nx; i++)
			{
				for (j = 0; j < nx; j++)
				{
					P[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
				}
			}
			double xbar[3];
			double Pbar[9];

			//declare the noise matrices
			nv = 6;
			double Q[36];
			for (i = 0; i < nv; i++)
			{
				for (j = 0; j < nv; j++)
				{
					Q[midx(i, j, nv)] = 0.0;
				}
			}
			//create noise in discrete time
			Q[midx(0, 0, nv)] = TARGET_QX / dt;
			Q[midx(1, 1, nv)] = TARGET_QY / dt;
			Q[midx(2, 2, nv)] = TARGET_QORIENT / dt;
			Q[midx(3, 3, nv)] = ODOM_QVX / dt;
			Q[midx(4, 4, nv)] = ODOM_QVY / dt;
			Q[midx(5, 5, nv)] = ODOM_QWZ / dt;

			//perform the prediction
			KalmanPredict(xbar, Pbar, nx, nv, dt, x, P, Q, iVehicleOdometry, &StaticTargetWithPointsDynamics);

			//extract the state and store it
			mX = xbar[0];
			mY = xbar[1];
			mOrientation = UnwrapAngle(xbar[2]);

			//extract the covariance and store it
			for (i = 0; i < nx; i++)
			{
				for (j = 0; j < nx; j++)
				{
					mCovariance[midx(idx[i], idx[j], T_NUMSTATES)] = Pbar[midx(i, j, nx)];
				}
			}
		}

		break;

	case T_IBEOMATURE:
	case T_MATURE:
		{
			//mature ibeo-only targets can move

			//declare the state matrices
			nx = 5;
			double x[5] = {mX, mY, mOrientation, mSpeed, mHeading};
			double P[25];
			int idx[5] = {0, 1, 2, 3, 4};
			for (i = 0; i < nx; i++)
			{
				for (j = 0; j < nx; j++)
				{
					P[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
				}
			}
			double xbar[5];
			double Pbar[25];

			//declare the noise matrices
			nv = 8;
			double Q[64];
			for (i = 0; i < nv; i++)
			{
				for (j = 0; j < nv; j++)
				{
					Q[midx(i, j, nv)] = 0.0;
				}
			}
			//create noise in discrete time
			Q[midx(0, 0, nv)] = TARGET_QX / dt;
			Q[midx(1, 1, nv)] = TARGET_QY / dt;
			Q[midx(2, 2, nv)] = TARGET_QORIENT / dt;
			Q[midx(3, 3, nv)] = TARGET_QSPEED / dt;
			Q[midx(4, 4, nv)] = TARGET_QHEADING / dt;
			Q[midx(5, 5, nv)] = ODOM_QVX / dt;
			Q[midx(6, 6, nv)] = ODOM_QVY / dt;
			Q[midx(7, 7, nv)] = ODOM_QWZ / dt;

			//perform the prediction
			KalmanPredict(xbar, Pbar, nx, nv, dt, x, P, Q, iVehicleOdometry, &DynamicTargetWithPointsDynamics);

			//extract the state and store it
			mX = xbar[0];
			mY = xbar[1];
			mOrientation = UnwrapAngle(xbar[2]);
			mSpeed = xbar[3];
			mHeading = UnwrapAngle(xbar[4]);

			//extract the covariance and store it
			for (i = 0; i < nx; i++)
			{
				for (j = 0; j < nx; j++)
				{
					mCovariance[midx(idx[i], idx[j], T_NUMSTATES)] = Pbar[midx(i, j, nx)];
				}
			}
		}

		break;

	case T_MOBILEYE:
	case T_QUASIMATURE:
		{
			//mobileye-only obstacles can move

			//declare the state matrices
			nx = 5;
			double x[5] = {mX, mY, mSpeed, mHeading, mWidth};
			double P[25];
			int idx[5] = {0, 1, 3, 4, 5};
			for (i = 0; i < nx; i++)
			{
				for (j = 0; j < nx; j++)
				{
					P[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
				}
			}
			double xbar[5];
			double Pbar[25];

			//declare the noise matrices
			nv = 8;
			double Q[64];
			for (i = 0; i < nv; i++)
			{
				for (j = 0; j < nv; j++)
				{
					Q[midx(i, j, nv)] = 0.0;
				}
			}
			//create noise in discrete time
			Q[midx(0, 0, nv)] = TARGET_QX / dt;
			Q[midx(1, 1, nv)] = TARGET_QY / dt;
			Q[midx(2, 2, nv)] = TARGET_QSPEED / dt;
			Q[midx(3, 3, nv)] = TARGET_QHEADING / dt;
			Q[midx(4, 4, nv)] = TARGET_QWIDTH / dt;
			Q[midx(5, 5, nv)] = ODOM_QVX / dt;
			Q[midx(6, 6, nv)] = ODOM_QVY / dt;
			Q[midx(7, 7, nv)] = ODOM_QWZ / dt;

			//perform the prediction
			KalmanPredict(xbar, Pbar, nx, nv, dt, x, P, Q, iVehicleOdometry, &DynamicTargetWithWidthDynamics);

			//extract the state and store it
			mX = xbar[0];
			mY = xbar[1];
			mSpeed = xbar[2];
			mHeading = UnwrapAngle(xbar[3]);
			mWidth = xbar[4];

			//extract the covariance and store it
			for (i = 0; i < nx; i++)
			{
				for (j = 0; j < nx; j++)
				{
					mCovariance[midx(idx[i], idx[j], T_NUMSTATES)] = Pbar[midx(i, j, nx)];
				}
			}
		}

		break;

	case T_RADAR:
		{
			//radar-only obstacles can move

			//declare the state matrices
			nx = 4;
			double x[4] = {mX, mY, mSpeed, mHeading};
			double P[16];
			int idx[4] = {0, 1, 3, 4};
			for (i = 0; i < nx; i++)
			{
				for (j = 0; j < nx; j++)
				{
					P[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
				}
			}
			double xbar[4];
			double Pbar[16];

			//declare the noise matrices
			nv = 7;
			double Q[49];
			for (i = 0; i < nv; i++)
			{
				for (j = 0; j < nv; j++)
				{
					Q[midx(i, j, nv)] = 0.0;
				}
			}
			//create noise in discrete time
			Q[midx(0, 0, nv)] = TARGET_QX / dt;
			Q[midx(1, 1, nv)] = TARGET_QY / dt;
			Q[midx(2, 2, nv)] = TARGET_QSPEED / dt;
			Q[midx(3, 3, nv)] = TARGET_QHEADING / dt;
			Q[midx(4, 4, nv)] = ODOM_QVX / dt;
			Q[midx(5, 5, nv)] = ODOM_QVY / dt;
			Q[midx(6, 6, nv)] = ODOM_QWZ / dt;

			//perform the prediction
			KalmanPredict(xbar, Pbar, nx, nv, dt, x, P, Q, iVehicleOdometry, &DynamicTargetDynamics);

			//extract the state and store it
			mX = xbar[0];
			mY = xbar[1];
			mSpeed = xbar[2];
			mHeading = UnwrapAngle(xbar[3]);

			//extract the covariance and store it
			for (i = 0; i < nx; i++)
			{
				for (j = 0; j < nx; j++)
				{
					mCovariance[midx(idx[i], idx[j], T_NUMSTATES)] = Pbar[midx(i, j, nx)];
				}
			}
		}

		break;
	}

	//also predict target existence probability
	//note: this just models exponential motion toward 0% existence
	//x' = -ax + bu, with u = constant and x converging to 0%
	double a = 3.0 / TARGET_T95PCT;
	//double bu = 0.0 * a;
	double buoa = 0.0;
	//mModelCorrectProb = exp(-a*dt)*mModelCorrectProb + bu/a*(1.0 - exp(-a*dt));
	mExistenceProbability = exp(-a*dt)*(mExistenceProbability - buoa) + buoa;

	return;
}

void Target::Update(MetaMeasurement* iMeasurement, Sensor* iSensor, VehicleOdometry* iVehicleOdometry)
{
	/*
	Updates a target with the measurement (which has already been internalized
	using the Likelihood function)

	INPUTS:
		iMeasurement - the measurement being applied.  NOTE: this must be the same
			measurement for which the likelihood was computed.
		iSensor - the sensor that was used to generate the measurement
		iVehicleOdometry - the vehicle odometry during the measurement update

	OUTPUTS:
		none.  Updates the state and covariance using data stored for the measurement
			and the measurement placeholder variables.
	*/

	if (mMeasurementType != iMeasurement->MeasurementType())
	{
		//this measurement isn't the same as the one that was calculated
		printf("Warning: incorrect MeasurementType used in Target::Update.\n");
		return;
	}

	if (fabs(mLambda) == 0.0 || nu == NULL || S == NULL || W == NULL)
	{
		printf("Warning: attempting to update a target with a measurement of likelihood 0.\n");
		return;
	}

	//store the measurement and increment the count
	mLastUpdateTime = iMeasurement->MeasurementTime();
	mNumMeasurements++;
	
	int i;
	int j;
	int nz;
	int nx;

	//compute likelihood differenly based on measurement type
	switch (mMeasurementType)
	{
	case MM_IBEOCLUSTER_00:
		{
			//fully visible and stable cluster measurement

			//count this as an ibeo measurement
			mLastIbeoUpdateTime = iMeasurement->MeasurementTime();
			mNumIbeoMeasurements++;

			nz = 3;

			switch (mTargetType)
			{
			case T_IBEO:
				{
					//applied to an immature ibeo target

					//pull the relevant portions of the state
					nx = 3;
					double xhat[3];
					double Phat[9];
					double xbar[3] = {mX, mY, mOrientation};
					double Pbar[9];
					int idx[3] = {0, 1, 2};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					//perform the update
					KalmanUpdate(xhat, Phat, nx, nz, xbar, Pbar, nu, S, W);

					//now put the updated states back into the member variables
					mX = xhat[0];
					mY = xhat[1];
					mOrientation = UnwrapAngle(xhat[2]);
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							mCovariance[midx(idx[i], idx[j], T_NUMSTATES)] = Phat[midx(i, j, nx)];
						}
					}

					//delete the old cluster points
					delete [] mTargetPoints;
					//create space for the new points
					double* iPacket = iMeasurement->DataPoints();
					mNumPoints = iMeasurement->NumDataPoints();
					mTargetPoints = new double[2*mNumPoints];

					//precompute trig values
					double cosOrient = cos(mOrientation);
					double sinOrient = sin(mOrientation);
					double cosSyaw = cos(iSensor->SensorYaw);
					double sinSyaw = sin(iSensor->SensorYaw);
					double sx = iSensor->SensorX;
					double sy = iSensor->SensorY;

					for (i = 0; i < mNumPoints; i++)
					{
						//transform the point to the object storage frame
						//extract the ibeo point
						double px = iPacket[midx(i, 0, mNumPoints)];
						double py = iPacket[midx(i, 1, mNumPoints)];
						//transform to ego vehicle coordinates
						double evx;
						double evy;
						SensorToEgoVehicle(evx, evy, px, py, cosSyaw, sinSyaw, sx, sy);
						//transform into object storage frame
						double osx;
						double osy;
						EgoVehicleToObject(osx, osy, evx, evy, cosOrient, sinOrient, mX, mY);
						mTargetPoints[midx(i, 0, mNumPoints)] = osx;
						mTargetPoints[midx(i, 1, mNumPoints)] = osy;
					}
				}

				break;

			case T_IBEOMATURE:
			case T_MATURE:
				{
					//applied to a mature ibeo target

					//pull the relevant portions of the state
					nx = 5;
					double xhat[5];
					double Phat[25];
					double xbar[5] = {mX, mY, mOrientation, mSpeed, mHeading};
					double Pbar[25];
					int idx[5] = {0, 1, 2, 3, 4};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					//perform the update
					KalmanUpdate(xhat, Phat, nx, nz, xbar, Pbar, nu, S, W);

					//now put the updated states back into the member variables
					mX = xhat[0];
					mY = xhat[1];
					mOrientation = UnwrapAngle(xhat[2]);
					mSpeed = xhat[3];
					mHeading = UnwrapAngle(xhat[4]);
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							mCovariance[midx(idx[i], idx[j], T_NUMSTATES)] = Phat[midx(i, j, nx)];
						}
					}

					//delete the old cluster points
					delete [] mTargetPoints;
					//create space for the new points
					double* iPacket = iMeasurement->DataPoints();
					mNumPoints = iMeasurement->NumDataPoints();
					mTargetPoints = new double[2*mNumPoints];

					//precompute trig values
					double cosOrient = cos(mOrientation);
					double sinOrient = sin(mOrientation);
					double cosSyaw = cos(iSensor->SensorYaw);
					double sinSyaw = sin(iSensor->SensorYaw);
					double sx = iSensor->SensorX;
					double sy = iSensor->SensorY;

					for (i = 0; i < mNumPoints; i++)
					{
						//transform the point to the object storage frame

						//extract the ibeo point
						double px = iPacket[midx(i, 0, mNumPoints)];
						double py = iPacket[midx(i, 1, mNumPoints)];
						//transform to ego vehicle coordinates
						double evx;
						double evy;
						SensorToEgoVehicle(evx, evy, px, py, cosSyaw, sinSyaw, sx, sy);

						//transform into object storage frame
						double osx;
						double osy;
						EgoVehicleToObject(osx, osy, evx, evy, cosOrient, sinOrient, mX, mY);

						mTargetPoints[midx(i, 0, mNumPoints)] = osx;
						mTargetPoints[midx(i, 1, mNumPoints)] = osy;
					}
				}

				break;

			case T_MOBILEYE:
			case T_QUASIMATURE:
				{
					//applied to a mobileye-only target

					//pull the relevant portions of the state
					nx = 5;
					double xhat[5];
					double Phat[25];
					double xbar[5] = {mX, mY, mSpeed, mHeading, mWidth};
					double Pbar[25];
					int idx[5] = {0, 1, 3, 4, 5};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					//perform the update
					KalmanUpdate(xhat, Phat, nx, nz, xbar, Pbar, nu, S, W);

					//now put the updated states back into the member variables
					mX = xhat[0];
					mY = xhat[1];
					mSpeed = xhat[2];
					mHeading = UnwrapAngle(xhat[3]);
					mWidth = xhat[4];
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							mCovariance[midx(idx[i], idx[j], T_NUMSTATES)] = Phat[midx(i, j, nx)];
						}
					}

					//NOTE: an ibeo measurement can be used to initialize a mobileye target's cluster data
					double* iPacket = iMeasurement->DataPoints();
					delete [] mTargetPoints;
					mNumPoints = iMeasurement->NumDataPoints();
					mTargetPoints = new double[2*mNumPoints];

					double sx = iSensor->SensorX;
					double sy = iSensor->SensorY;
					double syaw = iSensor->SensorYaw;
					double cosSyaw = cos(syaw);
					double sinSyaw = sin(syaw);
					for (i = 0; i < mNumPoints; i++)
					{
						//extract the point from the metameasurement
						double px = iPacket[midx(i, 0, mNumPoints)];
						double py = iPacket[midx(i, 1, mNumPoints)];
						//transform the point into vehicle coordinates
						double evx;
						double evy;
						SensorToEgoVehicle(evx, evy, px, py, cosSyaw, sinSyaw, sx, sy);

						//store the point temporarily in ego-vehicle coordinates
						mTargetPoints[midx(i, 0, mNumPoints)] = evx;
						mTargetPoints[midx(i, 1, mNumPoints)] = evy;
					}

					//initialize the object storage frame to the current vantage point
					mOrientation = 0.0;
					double cosOrient = 1.0;
					double sinOrient = 0.0;
					for (i = 0; i < mNumPoints; i++)
					{
						//transform points from ego vehicle to object storage frame
						double px = mTargetPoints[midx(i, 0, mNumPoints)];
						double py = mTargetPoints[midx(i, 1, mNumPoints)];
						double osx;
						double osy;
						EgoVehicleToObject(osx, osy, px, py, cosOrient, sinOrient, mX, mY);
						mTargetPoints[midx(i, 0, mNumPoints)] = osx;
						mTargetPoints[midx(i, 1, mNumPoints)] = osy;
					}
					mCovariance[midx(2, 2, T_NUMSTATES)] = IBEO_INITORIENTVAR;
				}

				break;

			case T_RADAR:
				{
					//NOTE: radar targets don't update with the ibeo angular spread
					int nzsmall = 2;

					//pull the relevant portions of the state
					nx = 4;
					double xhat[4];
					double Phat[16];
					double xbar[4] = {mX, mY, mSpeed, mHeading};
					double Pbar[16];
					int idx[4] = {0, 1, 3, 4};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					//perform the update
					KalmanUpdate(xhat, Phat, nx, nzsmall, xbar, Pbar, nu, S, W);

					//now put the updated states back into the member variables
					mX = xhat[0];
					mY = xhat[1];
					mSpeed = xhat[2];
					mHeading = UnwrapAngle(xhat[3]);
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							mCovariance[midx(idx[i], idx[j], T_NUMSTATES)] = Phat[midx(i, j, nx)];
						}
					}

					//NOTE: an ibeo measurement can be used to initialize a radar target's cluster data
					double* iPacket = iMeasurement->DataPoints();
					delete [] mTargetPoints;
					mNumPoints = iMeasurement->NumDataPoints();
					mTargetPoints = new double[2*mNumPoints];

					double sx = iSensor->SensorX;
					double sy = iSensor->SensorY;
					double syaw = iSensor->SensorYaw;
					double cosSyaw = cos(syaw);
					double sinSyaw = sin(syaw);
					for (i = 0; i < mNumPoints; i++)
					{
						//extract the point from the metameasurement
						double px = iPacket[midx(i, 0, mNumPoints)];
						double py = iPacket[midx(i, 1, mNumPoints)];
						//transform the point into vehicle coordinates
						double evx;
						double evy;
						SensorToEgoVehicle(evx, evy, px, py, cosSyaw, sinSyaw, sx, sy);

						//store the point temporarily in ego-vehicle coordinates
						mTargetPoints[midx(i, 0, mNumPoints)] = evx;
						mTargetPoints[midx(i, 1, mNumPoints)] = evy;
					}

					//initialize the object storage frame to the current vantage point
					mOrientation = 0.0;
					double cosOrient = 1.0;
					double sinOrient = 0.0;
					for (i = 0; i < mNumPoints; i++)
					{
						//transform points from ego vehicle to object storage frame
						double px = mTargetPoints[midx(i, 0, mNumPoints)];
						double py = mTargetPoints[midx(i, 1, mNumPoints)];
						double osx;
						double osy;
						EgoVehicleToObject(osx, osy, px, py, cosOrient, sinOrient, mX, mY);
						mTargetPoints[midx(i, 0, mNumPoints)] = osx;
						mTargetPoints[midx(i, 1, mNumPoints)] = osy;
					}
					mCovariance[midx(2, 2, T_NUMSTATES)] = IBEO_INITORIENTVAR;
				}

				break;
			}
		}

		break;

	case MM_IBEOCLUSTER_01:
	case MM_IBEOCLUSTER_02:
		{
			//cluster measurement with only cw or only ccw boundary visible

			//NOTE: do not count this as a full ibeo measurement update.
			//since it doesn't have a set of points attached, counting it as a full
			//update could potentially result in mature targets with no points.
			mLastIbeoUpdateTime = iMeasurement->MeasurementTime();
			mNumPartialIbeoMeasurements++;

			nz = 1;

			switch (mTargetType)
			{
			case T_IBEO:
				{
					//applied to an immature ibeo target

					//pull the relevant portions of the state
					nx = 3;
					double xhat[3];
					double Phat[9];
					double xbar[3] = {mX, mY, mOrientation};
					double Pbar[9];
					int idx[3] = {0, 1, 2};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					//perform the update
					KalmanUpdate(xhat, Phat, nx, nz, xbar, Pbar, nu, S, W);

					//now put the updated states back into the member variables
					mX = xhat[0];
					mY = xhat[1];
					mOrientation = UnwrapAngle(xhat[2]);
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							mCovariance[midx(idx[i], idx[j], T_NUMSTATES)] = Phat[midx(i, j, nx)];
						}
					}

					//NOTE: the cluster that generated this measurement wasn't complete, so it's
					//best to save the last complete cluster received.
				}

				break;

			case T_IBEOMATURE:
			case T_MATURE:
				{
					//applied to a mature ibeo target

					//pull the relevant portions of the state
					nx = 5;
					double xhat[5];
					double Phat[25];
					double xbar[5] = {mX, mY, mOrientation, mSpeed, mHeading};
					double Pbar[25];
					int idx[5] = {0, 1, 2, 3, 4};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					//perform the update
					KalmanUpdate(xhat, Phat, nx, nz, xbar, Pbar, nu, S, W);

					//now put the updated states back into the member variables
					mX = xhat[0];
					mY = xhat[1];
					mOrientation = UnwrapAngle(xhat[2]);
					mSpeed = xhat[3];
					mHeading = UnwrapAngle(xhat[4]);
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							mCovariance[midx(idx[i], idx[j], T_NUMSTATES)] = Phat[midx(i, j, nx)];
						}
					}

					//NOTE: the cluster that generated this measurement wasn't completely visible, 
					//so it's best to save the last complete cluster received.
				}

				break;

			case T_MOBILEYE:
			case T_QUASIMATURE:
				{
					//applied to a mobileye-only target

					//pull the relevant portions of the state
					nx = 5;
					double xhat[5];
					double Phat[25];
					double xbar[5] = {mX, mY, mSpeed, mHeading, mWidth};
					double Pbar[25];
					int idx[5] = {0, 1, 3, 4, 5};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					//perform the update
					KalmanUpdate(xhat, Phat, nx, nz, xbar, Pbar, nu, S, W);

					//now put the updated states back into the member variables
					mX = xhat[0];
					mY = xhat[1];
					mSpeed = xhat[2];
					mHeading = UnwrapAngle(xhat[3]);
					mWidth = xhat[4];
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							mCovariance[midx(idx[i], idx[j], T_NUMSTATES)] = Phat[midx(i, j, nx)];
						}
					}

					//NOTE: the cluster is not complete, so don't use it to initialize any cluster data
				}

				break;

				//NOTE: radar targets can't be updated with single-boundary measurements
			}
		}

		break;

	case MM_MOBILEYEOBSTACLE:
		{
			//mobileye obstacle measurement

			//count this as an ibeo measurement
			mLastMobileyeUpdateTime = iMeasurement->MeasurementTime();
			mNumMobileyeMeasurements++;

			nz = 4;

			switch (mTargetType)
			{
			case T_IBEO:
				{
					//applied to an immature ibeo target

					//NOTE: immature ibeo targets don't update with the mobileye width
					int nzsmall = 3;

					//pull the relevant portions of the state
					nx = 3;
					double xhat[3];
					double Phat[9];
					double xbar[3] = {mX, mY, mOrientation};
					double Pbar[9];
					int idx[3] = {0, 1, 2};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					//perform the update
					KalmanUpdate(xhat, Phat, nx, nzsmall, xbar, Pbar, nu, S, W);

					//now put the updated states back into the member variables
					mX = xhat[0];
					mY = xhat[1];
					mOrientation = UnwrapAngle(xhat[2]);
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							mCovariance[midx(idx[i], idx[j], T_NUMSTATES)] = Phat[midx(i, j, nx)];
						}
					}

					//NOTE: mobileye speed can be used to initialize immature ibeo speed and heading
					double* zmobileye = iMeasurement->MeasurementData();
					double* Rmobileye = iMeasurement->MeasurementCovariance();
					double syaw = iSensor->SensorYaw;
					double cosSyaw = cos(syaw);
					double sinSyaw = sin(syaw);
					double sx = iSensor->SensorX;
					double sy = iSensor->SensorY;
					double vx = iVehicleOdometry->vx;
					double vy = iVehicleOdometry->vy;
					double wz = iVehicleOdometry->wz;
					//initialize speed and heading based on the sign of speed and ego motion
					double tss = zmobileye[2];
					//compute velocity of the sensor in ego vehicle coordinates
					double sevx = vx - wz*sy;
					double sevy = vy + wz*sx;
					//compute the absolute velocity of the sensor in sensor coordinates
					double svx = cosSyaw*sevx + sinSyaw*sevy;
					double svy = -sinSyaw*sevx + cosSyaw*sevy;
					//calculate initial speed as measured speed + component of sensor speed in the forward direction
					mSpeed = tss + svx;
					if (mSpeed >= 0.0)
					{
						//obstacle moving away from ego-vehicle in sensor forward direction
						mHeading = syaw;
					}
					else
					{
						//obstacle moving toward ego-vehicle in sensor forward direction
						mSpeed = -mSpeed;
						mHeading = UnwrapAngle(syaw + PI);
					}

					//initialize speed variance from mobileye speed and vehicle odometry
					mCovariance[midx(3, 3, T_NUMSTATES)] = Rmobileye[midx(2, 2, nz)];
					//initialize heading variance from external parameters
					double Jevx = cosSyaw;
					double Jevy = sinSyaw;
					double Jewz = -sy*cosSyaw + sx*sinSyaw;
					mCovariance[midx(4, 4, T_NUMSTATES)] = MOBILEYE_INITHEADINGVAR + 
						Jevx*Jevx*iVehicleOdometry->qvx + Jevy*Jevy*iVehicleOdometry->qvy + 
						Jewz*Jewz*iVehicleOdometry->qwz;
				}

				break;

			case T_IBEOMATURE:
			case T_MATURE:
				{
					//applied to a mature ibeo target

					//pull the relevant portions of the state
					nx = 5;
					double xhat[5];
					double Phat[25];
					double xbar[5] = {mX, mY, mOrientation, mSpeed, mHeading};
					double Pbar[25];
					int idx[5] = {0, 1, 2, 3, 4};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					//perform the update
					KalmanUpdate(xhat, Phat, nx, nz, xbar, Pbar, nu, S, W);

					//now put the updated states back into the member variables
					mX = xhat[0];
					mY = xhat[1];
					mOrientation = UnwrapAngle(xhat[2]);
					mSpeed = xhat[3];
					mHeading = UnwrapAngle(xhat[4]);
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							mCovariance[midx(idx[i], idx[j], T_NUMSTATES)] = Phat[midx(i, j, nx)];
						}
					}
				}

				break;

			case T_MOBILEYE:
			case T_QUASIMATURE:
				{
					//applied to a mobileye-only target

					//pull the relevant portions of the state
					nx = 5;
					double xhat[5];
					double Phat[25];
					double xbar[5] = {mX, mY, mSpeed, mHeading, mWidth};
					double Pbar[25];
					int idx[5] = {0, 1, 3, 4, 5};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					//perform the update
					KalmanUpdate(xhat, Phat, nx, nz, xbar, Pbar, nu, S, W);

					//now put the updated states back into the member variables
					mX = xhat[0];
					mY = xhat[1];
					mSpeed = xhat[2];
					mHeading = UnwrapAngle(xhat[3]);
					mWidth = xhat[4];
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							mCovariance[midx(idx[i], idx[j], T_NUMSTATES)] = Phat[midx(i, j, nx)];
						}
					}
				}

				break;

			case T_RADAR:
				{
					//applied to a radar-only target

					//NOTE: radar targets don't update with the mobileye width
					int nzsmall = 3;

					//pull the relevant portions of the state
					nx = 4;
					double xhat[4];
					double Phat[16];
					double xbar[4] = {mX, mY, mSpeed, mHeading};
					double Pbar[16];
					int idx[4] = {0, 1, 3, 4};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					//perform the update
					KalmanUpdate(xhat, Phat, nx, nzsmall, xbar, Pbar, nu, S, W);

					//now put the updated states back into the member variables
					mX = xhat[0];
					mY = xhat[1];
					mSpeed = xhat[2];
					mHeading = UnwrapAngle(xhat[3]);
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							mCovariance[midx(idx[i], idx[j], T_NUMSTATES)] = Phat[midx(i, j, nx)];
						}
					}

					//NOTE: a mobileye update applied to a radar object can be used to initialize width
					double* zmobileye = iMeasurement->MeasurementData();
					double* Rmobileye = iMeasurement->MeasurementCovariance();
					mWidth = zmobileye[3];
					mCovariance[midx(5, 5, T_NUMSTATES)] = Rmobileye[midx(3, 3, nz)];
				}

				break;
			}
		}

		break;

	case MM_RADAROBSTACLE:
		{
			//radar obstacle measurement

			//count this as an ibeo measurement
			mLastRadarUpdateTime = iMeasurement->MeasurementTime();
			mNumRadarMeasurements++;

			nz = 3;

			switch (mTargetType)
			{
			case T_IBEO:
				{
					//applied to an immature ibeo target

					//NOTE: immature ibeo targets only use the radar's range and bearing
					//measurements, so the likelihood matrices need redefined
					int nzsmall = 2;

					//pull the relevant portions of the state
					nx = 3;
					double xhat[3];
					double Phat[9];
					double xbar[3] = {mX, mY, mOrientation};
					double Pbar[9];
					int idx[3] = {0, 1, 2};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					//perform the update
					KalmanUpdate(xhat, Phat, nx, nzsmall, xbar, Pbar, nu, S, W);

					//now put the updated states back into the member variables
					mX = xhat[0];
					mY = xhat[1];
					mOrientation = UnwrapAngle(xhat[2]);
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							mCovariance[midx(idx[i], idx[j], T_NUMSTATES)] = Phat[midx(i, j, nx)];
						}
					}

					//NOTE: a radar measurement applied to an ibeo target can be used
					//to initialize speed and heading
					double vx = iVehicleOdometry->vx;
					double vy = iVehicleOdometry->vy;
					double wz = iVehicleOdometry->wz;
					double syaw = iSensor->SensorYaw;
					double cosSyaw = cos(syaw);
					double sinSyaw = sin(syaw);
					double sx = iSensor->SensorX;
					double sy = iSensor->SensorY;

					double* zradar = iMeasurement->MeasurementData();
					double* Rradar = iMeasurement->MeasurementCovariance();
					//initialize speed and heading based on the sign of speed and ego motion
					double tsr = zradar[0];
					double tsb = zradar[1];
					double cosTsb = cos(tsb);
					double sinTsb = sin(tsb);
					double tsrr = zradar[2];
					//compute velocity of the sensor in ego vehicle coordinates
					double sevx = vx - wz*sy;
					double sevy = vy + wz*sx;
					//compute the velocity of the sensor in sensor coordinates (unrolled)
					double svx = sevx*cosSyaw + sevy*sinSyaw;
					double svy = -sevx*sinSyaw + sevy*cosSyaw;
					//calculate initial speed as measured range rate + component of sensor speed in the target's radial direction
					mSpeed = tsrr + svx*cosTsb + svy*sinTsb;
					if (mSpeed >= 0.0)
					{
						//obstacle moving away from ego-vehicle in radial direction
						mHeading = atan2(mY, mX);
					}
					else
					{
						//obstacle moving toward ego-vehicle in radial direction
						mSpeed = -mSpeed;
						mHeading = UnwrapAngle(atan2(mY, mX) + PI);
					}

					//initialize speed variance from radar speed and vehicle odometry
					double Jetb = -svx*sinTsb + svy*cosTsb;
					double Jevx = cosSyaw*cosTsb - sinSyaw*sinTsb;
					double Jevy = sinSyaw*cosTsb + cosSyaw*sinTsb;
					double Jewz = (-sy*cosSyaw + sx*sinSyaw)*cosTsb + (sy*sinSyaw + sx*cosSyaw)*sinTsb;
					mCovariance[midx(3, 3, T_NUMSTATES)] = Rradar[midx(2, 2, nz)] + 
						Jetb*Jetb*Rradar[midx(1, 1, nz)] + Jevx*Jevx*iVehicleOdometry->qvx + 
						Jevy*Jevy*iVehicleOdometry->qvy + Jewz*Jewz*iVehicleOdometry->qwz;
					mCovariance[midx(4, 4, T_NUMSTATES)] = RADAR_INITHEADINGVAR;
				}

				break;

			case T_IBEOMATURE:
			case T_MATURE:
				{
					//applied to a mature ibeo target

					//pull the relevant portions of the state
					nx = 5;
					double xhat[5];
					double Phat[25];
					double xbar[5] = {mX, mY, mOrientation, mSpeed, mHeading};
					double Pbar[25];
					int idx[5] = {0, 1, 2, 3, 4};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					//perform the update
					KalmanUpdate(xhat, Phat, nx, nz, xbar, Pbar, nu, S, W);

					//now put the updated states back into the member variables
					mX = xhat[0];
					mY = xhat[1];
					mOrientation = UnwrapAngle(xhat[2]);
					mSpeed = xhat[3];
					mHeading = UnwrapAngle(xhat[4]);
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							mCovariance[midx(idx[i], idx[j], T_NUMSTATES)] = Phat[midx(i, j, nx)];
						}
					}
				}

				break;

			case T_MOBILEYE:
			case T_QUASIMATURE:
				{
					//applied to a mobileye-only target

					//pull the relevant portions of the state
					nx = 5;
					double xhat[5];
					double Phat[25];
					double xbar[5] = {mX, mY, mSpeed, mHeading, mWidth};
					double Pbar[25];
					int idx[5] = {0, 1, 3, 4, 5};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					//perform the update
					KalmanUpdate(xhat, Phat, nx, nz, xbar, Pbar, nu, S, W);

					//now put the updated states back into the member variables
					mX = xhat[0];
					mY = xhat[1];
					mSpeed = xhat[2];
					mHeading = UnwrapAngle(xhat[3]);
					mWidth = xhat[4];
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							mCovariance[midx(idx[i], idx[j], T_NUMSTATES)] = Phat[midx(i, j, nx)];
						}
					}
				}

				break;

			case T_RADAR:
				{
					//applied to a radar-only target

					//pull the relevant portions of the state
					nx = 4;
					double xhat[4];
					double Phat[16];
					double xbar[4] = {mX, mY, mSpeed, mHeading};
					double Pbar[16];
					int idx[4] = {0, 1, 3, 4};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					//perform the update
					KalmanUpdate(xhat, Phat, nx, nz, xbar, Pbar, nu, S, W);

					//now put the updated states back into the member variables
					mX = xhat[0];
					mY = xhat[1];
					mSpeed = xhat[2];
					mHeading = UnwrapAngle(xhat[3]);
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							mCovariance[midx(idx[i], idx[j], T_NUMSTATES)] = Phat[midx(i, j, nx)];
						}
					}
				}

				break;
			}
		}

		break;

	case MM_SIDESICKOBSTACLE:
		{
			//side lidar measurement

			//count this as a side lidar measurement
			mLastSideSickUpdateTime = iMeasurement->MeasurementTime();
			mNumSideSickMeasurements++;

			nz = 1;

			switch (mTargetType)
			{
			case T_IBEO:
				{
					//applied to an immature ibeo target

					//pull the relevant portions of the state
					nx = 3;
					double xhat[3];
					double Phat[3*3];
					double xbar[3] = {mX, mY, mOrientation};
					double Pbar[3*3];
					int idx[3] = {0, 1, 2};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					//perform the update
					KalmanUpdate(xhat, Phat, nx, nz, xbar, Pbar, nu, S, W);

					//now put the updated states back into the member variables
					mX = xhat[0];
					mY = xhat[1];
					mOrientation = UnwrapAngle(xhat[2]);
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							mCovariance[midx(idx[i], idx[j], T_NUMSTATES)] = Phat[midx(i, j, nx)];
						}
					}
				}

				break;

			case T_IBEOMATURE:
			case T_MATURE:
				{
					//applied to a mature ibeo target

					//pull the relevant portions of the state
					nx = 5;
					double xhat[5];
					double Phat[5*5];
					double xbar[5] = {mX, mY, mOrientation, mSpeed, mHeading};
					double Pbar[5*5];
					int idx[5] = {0, 1, 2, 3, 4};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					//perform the update
					KalmanUpdate(xhat, Phat, nx, nz, xbar, Pbar, nu, S, W);

					//now put the updated states back into the member variables
					mX = xhat[0];
					mY = xhat[1];
					mOrientation = UnwrapAngle(xhat[2]);
					mSpeed = xhat[3];
					mHeading = UnwrapAngle(xhat[4]);
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							mCovariance[midx(idx[i], idx[j], T_NUMSTATES)] = Phat[midx(i, j, nx)];
						}
					}

					//NOTE: the cluster that generated this measurement wasn't completely visible, 
					//so it's best to save the last complete cluster received.
				}

				break;

			case T_MOBILEYE:
			case T_QUASIMATURE:
				{
					//applied to a mobileye-only target

					//pull the relevant portions of the state
					nx = 5;
					double xhat[5];
					double Phat[5*5];
					double xbar[5] = {mX, mY, mSpeed, mHeading, mWidth};
					double Pbar[5*5];
					int idx[5] = {0, 1, 3, 4, 5};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					//perform the update
					KalmanUpdate(xhat, Phat, nx, nz, xbar, Pbar, nu, S, W);

					//now put the updated states back into the member variables
					mX = xhat[0];
					mY = xhat[1];
					mSpeed = xhat[2];
					mHeading = UnwrapAngle(xhat[3]);
					mWidth = xhat[4];
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							mCovariance[midx(idx[i], idx[j], T_NUMSTATES)] = Phat[midx(i, j, nx)];
						}
					}

					//NOTE: the cluster is not complete, so don't use it to initialize any cluster data
				}

				break;

			case T_RADAR:
				{
					//applied to a radar-only target

					//pull the relevant portions of the state
					nx = 4;
					double xhat[4];
					double Phat[4*4];
					double xbar[4] = {mX, mY, mSpeed, mHeading};
					double Pbar[4*4];
					int idx[4] = {0, 1, 3, 4};
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							Pbar[midx(i, j, nx)] = mCovariance[midx(idx[i], idx[j], T_NUMSTATES)];
						}
					}

					//perform the update
					KalmanUpdate(xhat, Phat, nx, nz, xbar, Pbar, nu, S, W);

					//now put the updated states back into the member variables
					mX = xhat[0];
					mY = xhat[1];
					mSpeed = xhat[2];
					mHeading = UnwrapAngle(xhat[3]);
					for (i = 0; i < nx; i++)
					{
						for (j = 0; j < nx; j++)
						{
							mCovariance[midx(idx[i], idx[j], T_NUMSTATES)] = Phat[midx(i, j, nx)];
						}
					}

					//NOTE: the cluster is not complete, so don't use it to initialize any cluster data
				}

				break;
			}
		}

		break;
	}

	//now check the anchor point for validity (and repair it if necessary)
	RepairAnchorPoint();

	//also update existence (correctness) probability using likelihoods

	//likelihood of getting the measurement given the state
	double pZgX = mLambda;
	//likelihood of getting the measurement given a nonexistent target
	//NOTE: initialize with mLambda, then replace that with a better model
	double pZgNX = mLambda;

	switch (iMeasurement->MeasurementType())
	{
	case MM_IBEOCLUSTER_00:
		{
			//fully-visible, stable ibeo cluster

			//model as uniform in bearing and range
			pZgNX = (1.0/IBEO_RSPAN) * (1.0/IBEO_BSPAN) * (1.0/IBEO_BSPAN);
		}

		break;

	case MM_IBEOCLUSTER_01:
	case MM_IBEOCLUSTER_02:
		{
			//an ibeo cluster with only one corner visible

			//model as uniform in bearing
			pZgNX = (1.0/IBEO_BSPAN);
		}

	case MM_MOBILEYEOBSTACLE:
		{
			//mobileye obstacle measurement

			//model as uniform in x, y, s, w
			pZgNX = (1.0/MOBILEYE_XSPAN) * (1.0/MOBILEYE_YSPAN) * (1.0/MOBILEYE_SSPAN) * (1.0/MOBILEYE_WSPAN);
		}

		break;

	case MM_RADAROBSTACLE:
		{
			//radar obstacle measurement

			//model as uniform in r, b, rr
			pZgNX = (1.0/RADAR_RSPAN) * (1.0/RADAR_BSPAN) * (1.0/RADAR_RRSPAN);
		}

	case MM_SIDESICKOBSTACLE:
		{
			//side lidar obstacle measurement

			//model as uniform in range
			pZgNX = (1.0/SIDESICK_CRSPAN);
		}

		break;
	}

	//update existence probability with measurement data
	UpdateExistenceProbability(pZgX, pZgNX);

	return;
}

void Target::UpdateExistenceProbability(double ipSgE, double ipSgNE)
{
	/*
	Updates a target with evidence for / against its existence

	INPUTS:
		ipSgE - probability of getting the sensor measurement given that
			the target exists
		ipSgNE - probability of getting the sensor measurement given that
			the target doesn't exist

	OUTPUTS:
		none.  Updates mExistenceProbability to reflect the provided evidence
	*/

	//update existence probability with measurement data
	mExistenceProbability = ipSgE*mExistenceProbability / (ipSgE*mExistenceProbability + ipSgNE*(1.0-mExistenceProbability));

	return;
}

void Target::MaintainTarget()
{
	/*
	Maintains the target's status.  Checks to see if its target type can change
	based on how many measurements it has received.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	int i;
	int j;

	//check speed for correct sign (reverse not allowed)
	if (mSpeed < 0.0)
	{
		//if target speed is negative, flip its heading
		mSpeed = -mSpeed;
		mHeading += PI;

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
	mHeading = UnwrapAngle(mHeading);
	mOrientation = UnwrapAngle(mOrientation);

	//check width for correct sign (negative widths not allowed)
	if (mWidth < 0.0)
	{
		printf("Warning: target has a width of %.12lg m.\n", mWidth);
		mWidth = -mWidth;

		//when sign of width gets flipped, covariance needs to be flipped too
		for (i = 0; i < T_NUMSTATES; i++)
		{
			mCovariance[midx(i, 0, T_NUMSTATES)] = -mCovariance[midx(i, 0, T_NUMSTATES)];
		}
		for (j = 0; j < T_NUMSTATES; j++)
		{
			mCovariance[midx(0, j, T_NUMSTATES)] = -mCovariance[midx(0, j, T_NUMSTATES)];
		}
		mCovariance[midx(5, 5, T_NUMSTATES)] = fabs(mCovariance[midx(5, 5, T_NUMSTATES)]);
	}

	switch (mTargetType)
	{
	case T_IBEO:
		{
			//check to see if the target can mature

			if (mNumMobileyeMeasurements >= TARGET_MATUREMEASCOUNT ||
				mNumRadarMeasurements >= TARGET_MATUREMEASCOUNT)
			{
				//the target has received enough other measurements to fully mature
				mTargetType = T_MATURE;

				//NOTE: the target's full state should already be
				//initialized from the measurements it was assigned
			}
			else if (mNumIbeoMeasurements >= TARGET_IBEOMATUREMEASCOUNT)
			{
				//the target is stable enough to become a mature ibeo target
				mTargetType = T_IBEOMATURE;

				//initialize the target's full state, which is now used
				mSpeed = 0.0;
				mHeading = 0.0;

				//clear out any correlations with speed and heading
				int idx[2] = {3, 4};
				for (i = 0; i < 2; i++)
				{
					for (j = 0; j < T_NUMSTATES; j++)
					{
						mCovariance[midx(idx[i], j, T_NUMSTATES)] = 0.0;
					}
				}
				for (i = 0; i < T_NUMSTATES; i++)
				{
					for (j = 0; j < 2; j++)
					{
						mCovariance[midx(i, idx[j], T_NUMSTATES)] = 0.0;
					}
				}
				//set initial speed and orientation uncertainty
				mCovariance[midx(3, 3, T_NUMSTATES)] = IBEO_INITSPEEDVAR;
				mCovariance[midx(4, 4, T_NUMSTATES)] = IBEO_INITHEADINGVAR;
			}
		}

		break;

	case T_IBEOMATURE:
		{
			//check to see if a mature ibeo object can become a mature object

			if (mNumMobileyeMeasurements >= TARGET_MATUREMEASCOUNT ||
				mNumRadarMeasurements >= TARGET_MATUREMEASCOUNT)
			{
				//the target has received enough other measurements to fully mature
				mTargetType = T_MATURE;

				//NOTE: the target's full state should already be
				//initialized from the measurements it was assigned
			}
		}

		break;

	case T_MOBILEYE:
		{
			//check to see if a mobileye object can become a mature object

			if (mNumIbeoMeasurements >= TARGET_MATUREMEASCOUNT)
			{
				//the target has received enough measurements to fully mature
				mTargetType = T_MATURE;
			}
			else if (mNumRadarMeasurements >= TARGET_MATUREMEASCOUNT)
			{
				//the target has received enough radar measurements to become quasi-mature
				mTargetType = T_QUASIMATURE;
			}
		}

		break;

	case T_RADAR:
		{
			//check to see if a radar object can become a mature object

			if (mNumIbeoMeasurements >= TARGET_MATUREMEASCOUNT)
			{
				//the target has received enough measurements to fully mature
				mTargetType = T_MATURE;
			}
			else if (mNumMobileyeMeasurements >= TARGET_MATUREMEASCOUNT)
			{
				//the target has received enough mobileye measurements to become quasi-mature
				mTargetType = T_QUASIMATURE;
			}
		}

		break;

	case T_QUASIMATURE:
		{
			//check to see if a quasi-mature object can become a mature object

			if (mNumIbeoMeasurements >= TARGET_MATUREMEASCOUNT)
			{
				//the target has received enough measurements to fully mature
				mTargetType = T_MATURE;
			}
		}

		break;
	}

	if (mNumPoints == 0 && mTargetType == T_MATURE)
	{
		printf("Warning: zero target points in a mature object.\n");
	}

	return;
}

bool Target::IsInLineOfSight(Sensor* iSensor)
{
	/*
	Determines whether this target is reasonably in the sensor's line of sight.

	INPUTS:
		iSensor - the sensor to test for visibility.  The sensor's forward direction
			is tested by this function.

	OUTPUTS:
		rIsInLOS - true if the target is reasonably in the sensor's line of sight,
			false otherwise.
	*/

	//check to see whether the target has points that could straddle the sensor's LOS
	bool rIsInLOS = false;

	int h;
	int i;
	int j;
	int k;

	//these flags will store whether points have been found that lie on each side
	//of the sensor.  If they are both true, this function returns true.
	bool leftcover = false;
	bool rightcover = false;

	switch (mTargetType)
	{
	case T_IBEO:
	case T_IBEOMATURE:
	case T_MATURE:
		{
			//for targets with points, just test all the points

			int np = mNumPoints;

			double cosSyaw = cos(iSensor->SensorYaw);
			double sinSyaw = sin(iSensor->SensorYaw);
			double sx = iSensor->SensorX;
			double sy = iSensor->SensorY;

			double cosO = cos(mOrientation);
			double sinO = sin(mOrientation);

			//the jacobian on scx and scy as a function of uncertainty in mX, mY, mO
			double J[2*3];

			for (h = 0; h < np; h++)
			{
				//test each point for being on either side of the sensor
				double osx = mTargetPoints[midx(h, 0, np)];
				double osy = mTargetPoints[midx(h, 0, np)];
				//transform to ego vehicle coordinates
				double evx;
				double evy;
				ObjectToEgoVehicle(evx, evy, osx, osy, cosO, sinO, mX, mY);
				//transform to sensor coordinates
				double scx;
				double scy;
				EgoVehicleToSensor(scx, scy, evx, evy, cosSyaw, sinSyaw, sx, sy);

				//calculate the variance of the point in sensor coordinates
				J[midx(0, 0, 2)] = cosSyaw;
				J[midx(0, 1, 2)] = sinSyaw;
				J[midx(0, 2, 2)] = -cosSyaw*(sinO*osx + cosO*osy) + sinSyaw*(cosO*osx - sinO*osy);
				J[midx(1, 0, 2)] = -sinSyaw;
				J[midx(1, 1, 2)] = cosSyaw;
				J[midx(1, 2, 2)] = -sinSyaw*(sinO*osx + cosO*osy) + cosSyaw*(cosO*osx - sinO*osy);

				double JP[2*3];
				for (i = 0; i < 2; i++)
				{
					for (j = 0; j < 3; j++)
					{
						JP[midx(i, j, 2)] = 0.0;
						for (k = 0; k < 3; k++)
						{
							JP[midx(i, j, 2)] += J[midx(i, k, 2)] * mCovariance[midx(k, j, T_NUMSTATES)];
						}
					}
				}
				double Psc[2*2];
				for (i = 0; i < 2; i++)
				{
					for (j = 0; j < 2; j++)
					{
						Psc[midx(i, j, 2)] = 0.0;
						for (k = 0; k < 3; k++)
						{
							Psc[midx(i, j, 2)] += JP[midx(i, k, 2)] * J[midx(j, k, 2)];
						}
					}
				}

				//extract the variances in each sensor direction
				double sigma_scx = sqrt(Psc[midx(0, 0, 2)]);
				double sigma_scy = sqrt(Psc[midx(1, 1, 2)]);

				//test this point
				if (scx + 2.0*sigma_scx >= 0.0)
				{
					//at least the point is in front of the sensor

					//test whether any point +- 2-sigma lies on each side of the sensor LOS
					if (scy + 2.0*sigma_scy >= 0.0)
					{
						leftcover = true;
					}
					if (scy - 2.0*sigma_scy <= 0.0)
					{
						rightcover = true;
					}
				}

				if (leftcover == true && rightcover == true)
				{
					//found two points that straddle the sensor
					rIsInLOS = true;
					break;
				}
			}
		}

		break;

	case T_MOBILEYE:
	case T_QUASIMATURE:
		{
			//for targets that have width but no points, have to invent test points

			//approximate the object as a circle of radius mWidth for testing
			double cosSyaw = cos(iSensor->SensorYaw);
			double sinSyaw = sin(iSensor->SensorYaw);
			double sx = iSensor->SensorX;
			double sy = iSensor->SensorY;

			double cosO = cos(mOrientation);
			double sinO = sin(mOrientation);

			//the jacobian on scx and scy as a function of uncertainty in mX, mY
			double J[2*2];

			//transform the object center to sensor coordinates
			double scx;
			double scy;
			EgoVehicleToSensor(scx, scy, mX, mY, cosSyaw, sinSyaw, sx, sy);

			//calculate the object's XY uncertainty in ego-vehicle coordinates
			J[midx(0, 0, 2)] = cosSyaw;
			J[midx(0, 1, 2)] = sinSyaw;
			J[midx(1, 0, 2)] = -sinSyaw;
			J[midx(1, 1, 2)] = cosSyaw;

			double JP[2*2];
			for (i = 0; i < 2; i++)
			{
				for (j = 0; j < 2; j++)
				{
					JP[midx(i, j, 2)] = 0.0;
					for (k = 0; k < 2; k++)
					{
						JP[midx(i, j, 2)] += J[midx(i, k, 2)] * mCovariance[midx(k, j, T_NUMSTATES)];
					}
				}
			}
			double Psc[2*2];
			for (i = 0; i < 2; i++)
			{
				for (j = 0; j < 2; j++)
				{
					Psc[midx(i, j, 2)] = 0.0;
					for (k = 0; k < 2; k++)
					{
						Psc[midx(i, j, 2)] += JP[midx(i, k, 2)] * J[midx(j, k, 2)];
					}
				}
			}

			//extract the variances in each sensor direction
			double sigma_scx = sqrt(Psc[midx(0, 0, 2)]);
			double sigma_scy = sqrt(Psc[midx(1, 1, 2)]);
			double sigma_fmw = sqrt(mCovariance[midx(5, 5, T_NUMSTATES)]);

			//perform sensor tests
			double fmw = fabs(mWidth);
			if (scx + fmw + 2.0*(sigma_scx + sigma_fmw) >= 0.0)
			{
				//the object has a chance of being visible to the sensor
				if (scy + fmw + 2.0*(sigma_scy + sigma_fmw) >= 0.0)
				{
					leftcover = true;
				}
				if (scy - fmw - 2.0*(sigma_scy + sigma_fmw) <= 0.0)
				{
					rightcover = true;
				}
			}

			if (leftcover == true && rightcover == true)
			{
				rIsInLOS = true;
			}
		}

		break;

	case T_RADAR:
		{
			//for targets that have no width, use a default width

			//approximate the object as a circle of radius mWidth for testing
			double cosSyaw = cos(iSensor->SensorYaw);
			double sinSyaw = sin(iSensor->SensorYaw);
			double sx = iSensor->SensorX;
			double sy = iSensor->SensorY;

			double cosO = cos(mOrientation);
			double sinO = sin(mOrientation);

			//the jacobian on scx and scy as a function of uncertainty in mX, mY
			double J[2*2];

			//transform the object center to sensor coordinates
			double scx;
			double scy;
			EgoVehicleToSensor(scx, scy, mX, mY, cosSyaw, sinSyaw, sx, sy);

			//calculate the object's XY uncertainty in ego-vehicle coordinates
			J[midx(0, 0, 2)] = cosSyaw;
			J[midx(0, 1, 2)] = sinSyaw;
			J[midx(1, 0, 2)] = -sinSyaw;
			J[midx(1, 1, 2)] = cosSyaw;

			double JP[2*2];
			for (i = 0; i < 2; i++)
			{
				for (j = 0; j < 2; j++)
				{
					JP[midx(i, j, 2)] = 0.0;
					for (k = 0; k < 2; k++)
					{
						JP[midx(i, j, 2)] += J[midx(i, k, 2)] * mCovariance[midx(k, j, T_NUMSTATES)];
					}
				}
			}
			double Psc[2*2];
			for (i = 0; i < 2; i++)
			{
				for (j = 0; j < 2; j++)
				{
					Psc[midx(i, j, 2)] = 0.0;
					for (k = 0; k < 2; k++)
					{
						Psc[midx(i, j, 2)] += JP[midx(i, k, 2)] * J[midx(j, k, 2)];
					}
				}
			}

			//extract the variances in each sensor direction
			double sigma_scx = sqrt(Psc[midx(0, 0, 2)]);
			double sigma_scy = sqrt(Psc[midx(1, 1, 2)]);

			//perform sensor tests
			double fmw = TARGET_DEFAULTWIDTH;
			if (scx + fmw + 2.0*sigma_scx >= 0.0)
			{
				//the object has a chance of being visible to the sensor
				if (scy + fmw + 2.0*sigma_scy >= 0.0)
				{
					leftcover = true;
				}
				if (scy - fmw - 2.0*sigma_scy <= 0.0)
				{
					rightcover = true;
				}
			}

			if (leftcover == true && rightcover == true)
			{
				rIsInLOS = true;
			}
		}

		break;
	}

	return rIsInLOS;
}

void Target::ExtremePoints(double& oCWX, double& oCWY, double& oCCWX, double& oCCWY, double& oCPX, double& oCPY)
{
	/*
	Computes the clockwise, counter-clockwise, and closest extreme points.
	Returns them in ego-vehicle coordinates.

	INPUTS:
		oCWX, oCWY - will contain the clockwise extreme point
		oCCWX, oCCWY - will contain the counter-clockwise extreme point
		oCPX, oCPY - will contain the closest point extreme point

	OUTPUTS:
		oCWX, oCWY - clockwise extreme point
		oCCWX, oCCWY - counter-clockwise extreme point
		oCPX, oCPY - closest point extreme point
	*/

	int i;
	int np = mNumPoints;

	switch (mTargetType)
	{
	case T_IBEO:
	case T_IBEOMATURE:
	case T_MATURE:
		{
			//for targets with points, calculate the extreme points

			//extract the useful portion of the state
			double xref = mX;
			double yref = mY;
			double oref = mOrientation;
			double cosOrient = cos(oref);
			double sinOrient = sin(oref);

			//wrap all bearings to the bearing of the anchor point in ego-vehicle coordinates
			double wraptarget = atan2(mY, mX);

			//calculate components of x-target direction (toward anchor point) in sensor coordinates
			double xtx = mX;
			double xty = mY;
			double ntx = sqrt(xtx*xtx + xty*xty);
			if (fabs(ntx) == 0.0)
			{
				xtx = 1.0;
				xty = 0.0;
			}
			else
			{
				double oontx = 1.0/ntx;
				xtx *= oontx;
				xty *= oontx;
			}
			//calculate components of y-target direction
			double ytx = -xty;
			double yty = xtx;

			//these will store the locations of the extreme bearing points
			double maxb = -DBL_MAX;
			double maxs = -DBL_MAX;
			double maxevx;
			double maxevy;
			double minb = DBL_MAX;
			double mins = DBL_MAX;
			double minevx;
			double minevy;
			//this will store the location of the minimum range point
			double minr = DBL_MAX;

			for (i = 0; i < np; i++)
			{
				//extract and test each target point
				double px = mTargetPoints[midx(i, 0, np)];
				double py = mTargetPoints[midx(i, 1, np)];
				//transform the point to ego-vehicle coordinates
				double evx;
				double evy;
				ObjectToEgoVehicle(evx, evy, px, py, cosOrient, sinOrient, xref, yref);

				//compute the point's range
				double rng = sqrt(evx*evx + evy*evy);

				//project each target point into the target's coordinates to see if it is
				//an extreme point (arctan will be computed later only on these points)
				double pxp = xtx*evx + xty*evy;
				double pyp = ytx*evx + yty*evy;

				if (pxp > 0.0)
				{
					//if the slope can be calculated legitimately, use max and min slope to find extreme points
					double curs = pyp/pxp;
					if (curs > maxs)
					{
						maxs = curs;
						maxevx = evx;
						maxevy = evy;
					}
					if (curs < mins)
					{
						mins = curs;
						minevx = evx;
						minevy = evy;
					}
				}
				else
				{
					//a weird target that wraps over more than +-90o; have to calculate bearings brute force
					//NOTE: this is an odd case that doesn't happen too much

					//wrap the angle to the same 2pi branch
					double ang = WrapAngle(atan2(evy, evx), wraptarget);
					if (ang < minb)
					{
						minb = ang;
						oCWX = evx;
						oCWY = evy;
					}
					if (ang > maxb)
					{
						maxb = ang;
						oCCWX = evx;
						oCCWY = evy;
					}
				}

				//compare range to the measurements found so far
				if (rng < minr)
				{
					minr = rng;
					oCPX = evx;
					oCPY = evy;
				}
			}

			//calculate max and min bearing from all the normal points
			if (fabs(maxs) != DBL_MAX)
			{
				double ang = atan2(maxevy, maxevx);
				ang = WrapAngle(ang, wraptarget);
				if (ang < minb)
				{
					minb = ang;
					oCWX = maxevx;
					oCWY = maxevy;
				}
				if (ang > maxb)
				{
					maxb = ang;
					oCCWX = maxevx;
					oCCWY = maxevy;
				}
			}
			if (fabs(mins) != DBL_MAX)
			{
				double ang = atan2(minevy, minevx);
				ang = WrapAngle(ang, wraptarget);
				if (ang < minb)
				{
					minb = ang;
					oCWX = minevx;
					oCWY = minevy;
				}
				if (ang > maxb)
				{
					maxb = ang;
					oCCWX = minevx;
					oCCWY = minevy;
				}
			}
		}

		break;
	}

	return;
}
