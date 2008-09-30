#include "LocalRoad.h"

LocalRoad::LocalRoad()
{
	/*
	Default constructor for the LocalRoad class.  Initializes the variables to 
	default values.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	mIsInitialized = false;
	mIsValid = false;
	mLocalRoadTime = -DBL_MAX;

	//when created, simply reset all the variables to default values
	ResetRoadModel();

	return;
}

void LocalRoad::ResetRoadModel()
{
	/*
	Resets the road model to initial values.  Used during initialization, 
	and when the road estimate is found to be invalid.

	INPUTS:
		none.

	OUTPUTS:
		none.  Resets the state estimate variables to default values.
	*/

	int i;
	int j;

	//mark the road model as invalid
	mIsValid = false;

	//reset the road model

	//parameters that describe the road shape
	mModelCorrectProb = 0.0;

	mRoadOffset = LR_DEFAULTOFFSET;
	mRoadHeading = LR_DEFAULTHEADING;
	mRoadCurvature = LR_DEFAULTCURVATURE;

	for (i = 0; i < LR_NUMSTATES; i++)
	{
		for (j = 0; j < LR_NUMSTATES; j++)
		{
			mRoadCovariance[midx(i, j, LR_NUMSTATES)] = 0.0;
		}
	}
	mRoadCovariance[midx(0, 0, LR_NUMSTATES)] = LR_DEFAULTOFFSETVAR;
	mRoadCovariance[midx(1, 1, LR_NUMSTATES)] = LR_DEFAULTHEADINGVAR;
	mRoadCovariance[midx(2, 2, LR_NUMSTATES)] = LR_DEFAULTCURVATUREVAR;

	//reset the lane model

	//parameters that describe the center lane of the road
	mCenterLaneProb = LR_DEFAULTLANEPROB;
	mCenterLaneWidth = LR_DEFAULTLANEWIDTH;
	mCenterLaneWidthVar = LR_DEFAULTLANEVAR;

	//parameters that describe the left lane of the road
	mLeftLaneProb = LR_DEFAULTLANEPROB;
	mLeftLaneWidth = LR_DEFAULTLANEWIDTH;
	mLeftLaneWidthVar = LR_DEFAULTLANEVAR;

	//parameters that describe the right lane of the road
	mRightLaneProb = LR_DEFAULTLANEPROB;
	mRightLaneWidth = LR_DEFAULTLANEWIDTH;
	mRightLaneWidthVar = LR_DEFAULTLANEVAR;

	return;
}

void LocalRoad::ResetRoadModelForTransmit()
{
	/*
	Resets the transmitted road model to initial values.  Used during initialization, 
	and when the road estimate is found to be invalid.

	INPUTS:
		none.

	OUTPUTS:
		none.  Resets the state estimate variables to default values.
	*/

	int i;
	int j;

	//parameters that describe the road shape
	mAIModelCorrectProb = 0.0;

	//reset road model

	mAIRoadOffset = LR_DEFAULTOFFSET;
	mAIRoadHeading = LR_DEFAULTHEADING;
	mAIRoadCurvature = LR_DEFAULTCURVATURE;

	for (i = 0; i < LR_NUMSTATES; i++)
	{
		for (j = 0; j < LR_NUMSTATES; j++)
		{
			mAIRoadCovariance[midx(i, j, LR_NUMSTATES)] = 0.0;
		}
	}
	mAIRoadCovariance[midx(0, 0, LR_NUMSTATES)] = LR_DEFAULTOFFSETVAR;
	mAIRoadCovariance[midx(1, 1, LR_NUMSTATES)] = LR_DEFAULTHEADINGVAR;
	mAIRoadCovariance[midx(2, 2, LR_NUMSTATES)] = LR_DEFAULTCURVATUREVAR;

	//reset the lane model

	//parameters that describe the center lane of the road
	mAICenterLaneProb = LR_DEFAULTLANEPROB;
	mAICenterLaneWidth = LR_DEFAULTLANEWIDTH;
	mAICenterLaneWidthVar = LR_DEFAULTLANEVAR;

	//parameters that describe the left lane of the road
	mAILeftLaneProb = LR_DEFAULTLANEPROB;
	mAILeftLaneWidth = LR_DEFAULTLANEWIDTH;
	mAILeftLaneWidthVar = LR_DEFAULTLANEVAR;

	//parameters that describe the right lane of the road
	mAIRightLaneProb = LR_DEFAULTLANEPROB;
	mAIRightLaneWidth = LR_DEFAULTLANEWIDTH;
	mAIRightLaneWidthVar = LR_DEFAULTLANEVAR;

	return;
}

void LocalRoad::Initialize(double iInitialTime)
{
	/*
	Initialized or reinitialized the road model at a particular time.

	INPUTS:
		iInitialTime - the time at which the model is initialized.

	OUTPUTS:
		none.
	*/

	if (iInitialTime < 0.0)
	{
		return;
	}

	if (mIsInitialized == false)
	{
		printf("LocalRoad initialized.\n");
	}

	mIsInitialized = true;
	mLocalRoadTime = iInitialTime;

	//reset all the variables to default values
	ResetRoadModel();

	return;
}

void LocalRoad::AlignLanes()
{
	/*
	Aligns the local road estimate to the lanes properly, accounting for lane
	changes and direction changes

	INPUTS:
		none.

	OUTPUTS:
		none.  Adjusts the road model based on direction and lane offset
	*/

	double temp;

	//check if road heading is off far enough to reverse the road model
	if (fabs(mRoadHeading) > PIOTWO)
	{
		//reverse the direction of the local road
		mRoadOffset = -mRoadOffset;
		mRoadHeading = UnwrapAngle(PI - mRoadHeading);
		mRoadCurvature = -mRoadCurvature;

		//the right and left lanes now switch places
		temp = mLeftLaneProb;
		mLeftLaneProb = mRightLaneProb;
		mRightLaneProb = temp;
		
		temp = mLeftLaneWidth;
		mLeftLaneWidth = mRightLaneWidth;
		mRightLaneWidth = temp;

		temp = mLeftLaneWidthVar;
		mLeftLaneWidthVar = mRightLaneWidthVar;
		mRightLaneWidthVar = temp;
	}

	//check if a lane shift needs to happen
	if (mLeftLaneProb > LR_MINLANEEXISTENCEPROB && mRoadOffset < -0.5*mCenterLaneWidth)
	{
		//shift the model to the left: store the left lane as the new center lane
		//and the center lane as the new right lane
		mRoadOffset += 0.5*(mLeftLaneWidth + mCenterLaneWidth);

		temp = mCenterLaneProb;
		mCenterLaneProb = mLeftLaneProb;
		mRightLaneProb = temp;

		temp = mCenterLaneWidth;
		mCenterLaneWidth = mLeftLaneWidth;
		mRightLaneWidth = temp;

		temp = mCenterLaneWidthVar;
		mCenterLaneWidthVar = mLeftLaneWidthVar;
		mRightLaneWidthVar = temp;

		//and reset the estimate on the left lane
		mLeftLaneProb = LR_DEFAULTLANEPROB;
		mLeftLaneWidth = LR_DEFAULTLANEWIDTH;
		mLeftLaneWidthVar = LR_DEFAULTLANEVAR;
	}
	else if (mRightLaneProb > LR_MINLANEEXISTENCEPROB && mRoadOffset > 0.5*mCenterLaneWidth)
	{
		//shift the model to the right: store the right lane as the new center lane
		//and the center lane as the new left lane
		mRoadOffset -= 0.5*(mRightLaneWidth + mCenterLaneWidth);

		temp = mCenterLaneProb;
		mCenterLaneProb = mRightLaneProb;
		mLeftLaneProb = temp;

		temp = mCenterLaneWidth;
		mCenterLaneWidth = mRightLaneWidth;
		mLeftLaneWidth = temp;

		temp = mCenterLaneWidthVar;
		mCenterLaneWidthVar = mRightLaneWidthVar;
		mLeftLaneWidthVar = temp;

		//and reset the estimate on the right lane
		mRightLaneProb = LR_DEFAULTLANEPROB;
		mRightLaneWidth = LR_DEFAULTLANEWIDTH;
		mRightLaneWidthVar = LR_DEFAULTLANEVAR;
	}

	return;
}

void LocalRoad::AlignLanesForTransmit()
{
	/*
	Aligns the local road estimate to the lanes properly, accounting for lane
	changes and direction changes

	INPUTS:
		none.

	OUTPUTS:
		none.  Adjusts the road model based on direction and lane offset
	*/

	double temp;

	//check if road heading is off far enough to reverse the road model
	if (fabs(mAIRoadHeading) > PIOTWO)
	{
		//reverse the direction of the local road
		mAIRoadOffset = -mAIRoadOffset;
		mAIRoadHeading = UnwrapAngle(PI - mAIRoadHeading);
		mAIRoadCurvature = -mAIRoadCurvature;

		//the right and left lanes now switch places
		temp = mAILeftLaneProb;
		mAILeftLaneProb = mAIRightLaneProb;
		mAIRightLaneProb = temp;
		
		temp = mAILeftLaneWidth;
		mAILeftLaneWidth = mAIRightLaneWidth;
		mAIRightLaneWidth = temp;

		temp = mAILeftLaneWidthVar;
		mAILeftLaneWidthVar = mAIRightLaneWidthVar;
		mAIRightLaneWidthVar = temp;
	}

	//check if a lane shift needs to happen
	if (mAILeftLaneProb > LR_MINLANEEXISTENCEPROB && mAIRoadOffset < -0.5*mAICenterLaneWidth)
	{
		//shift the model to the left: store the left lane as the new center lane
		//and the center lane as the new right lane
		mAIRoadOffset += 0.5*(mAILeftLaneWidth + mAICenterLaneWidth);

		temp = mAICenterLaneProb;
		mAICenterLaneProb = mAILeftLaneProb;
		mAIRightLaneProb = temp;

		temp = mAICenterLaneWidth;
		mAICenterLaneWidth = mAILeftLaneWidth;
		mAIRightLaneWidth = temp;

		temp = mAICenterLaneWidthVar;
		mAICenterLaneWidthVar = mAILeftLaneWidthVar;
		mAIRightLaneWidthVar = temp;

		//and reset the estimate on the left lane
		mAILeftLaneProb = LR_DEFAULTLANEPROB;
		mAILeftLaneWidth = LR_DEFAULTLANEWIDTH;
		mAILeftLaneWidthVar = LR_DEFAULTLANEVAR;
	}
	else if (mAIRightLaneProb > LR_MINLANEEXISTENCEPROB && mAIRoadOffset > 0.5*mAICenterLaneWidth)
	{
		//shift the model to the right: store the right lane as the new center lane
		//and the center lane as the new left lane
		mAIRoadOffset -= 0.5*(mAIRightLaneWidth + mAICenterLaneWidth);

		temp = mAICenterLaneProb;
		mAICenterLaneProb = mAIRightLaneProb;
		mAILeftLaneProb = temp;

		temp = mAICenterLaneWidth;
		mAICenterLaneWidth = mAIRightLaneWidth;
		mAILeftLaneWidth = temp;

		temp = mAICenterLaneWidthVar;
		mAICenterLaneWidthVar = mAIRightLaneWidthVar;
		mAILeftLaneWidthVar = temp;

		//and reset the estimate on the right lane
		mAIRightLaneProb = LR_DEFAULTLANEPROB;
		mAIRightLaneWidth = LR_DEFAULTLANEWIDTH;
		mAIRightLaneWidthVar = LR_DEFAULTLANEVAR;
	}

	return;
}

void LocalRoad::Predict(double iPredictTime, VehicleOdometry* iVehicleOdometry)
{
	/*
	Predicts the local road model forward given vehicle odometry and a desired
	time for prediction.

	INPUTS:
		iPredictTime - time to which the model is to be predicted.
		iVehicleOdometry - vehicle odometry structure containing information
			on how the vehicle moved during the prediction.

	OUTPUTS:
		none.  Predicts the state variables and variances forward to iPredictTime.
	*/

	if (iVehicleOdometry->IsValid == false)
	{
		//do not predict if odometry is invalid
		return;
	}

	if (mIsInitialized == false)
	{
		//do not predict if the local road is not initialized
		return;
	}

	//extract relevant odometry information
	double dt = iPredictTime - mLocalRoadTime;
	if (dt <= 0.0)
	{
		//do not predict backwards
		return;
	}

	int i;
	int j;

	//***PREDICT THE ROAD MODEL

	int nx = LR_NUMSTATES;
	int nv = LR_NUMSTATES + 3;

	//accumulate all the variables and perform the prediction
	double x[3] = {mRoadOffset, mRoadHeading, mRoadCurvature};
	double xbar[3];
	double* P = mRoadCovariance;
	double Pbar[LR_NUMSTATES*LR_NUMSTATES];
	double Q[(LR_NUMSTATES + 3)*(LR_NUMSTATES + 3)];

	//set the process noise matrix
	for (i = 0; i < nv; i++)
	{
		for (j = 0; j < nv; j++)
		{
			Q[midx(i, j, nv)] = 0.0;
		}
	}
	Q[midx(0, 0, nv)] = LR_ROADOFFSETVAR / dt;
	Q[midx(1, 1, nv)] = LR_ROADHEADINGVAR / dt;
	Q[midx(2, 2, nv)] = LR_ROADCURVATUREVAR / dt;
	Q[midx(3, 3, nv)] = iVehicleOdometry->qvx / dt;
	Q[midx(4, 4, nv)] = iVehicleOdometry->qvy / dt;
	Q[midx(5, 5, nv)] = iVehicleOdometry->qwz / dt;

	//do the prediction and copy over the state and covariance
	KalmanPredict(xbar, Pbar, nx, nv, dt, x, P, Q, iVehicleOdometry, &RoadModelDynamics);

	mRoadOffset = xbar[0];
	mRoadHeading = UnwrapAngle(xbar[1]);
	mRoadCurvature = xbar[2];

	for (i = 0; i < nx; i++)
	{
		for (j = 0; j < nx; j++)
		{
			mRoadCovariance[midx(i, j, nx)] = Pbar[midx(i, j, nx)];
		}
	}

	//also predict the road model correctness

	//note: this just models exponential motion toward 0% model correctness
	//x' = -ax + bu, with u = constant and x converging to 0%
	double a = 3.0 / LR_T95PCT;
	//double bu = LR_DEFAULTMODELPROB * a;
	double buoa = 0.0;
	//mModelCorrectProb = exp(-a*dt)*mModelCorrectProb + bu/a*(1.0 - exp(-a*dt));
	mModelCorrectProb = exp(-a*dt)*(mModelCorrectProb - buoa) + buoa;

	//***PREDICT THE LANE MODEL

	//note: the lane width estimates don't change, but the variances do
	mCenterLaneWidthVar += LR_LANEWIDTHVAR * dt;
	mLeftLaneWidthVar += LR_LANEWIDTHVAR * dt;
	mRightLaneWidthVar += LR_LANEWIDTHVAR * dt;

	//also predict the lane probabilities

	//note: this just models exponential motion toward LR_DEFAULTLANEPROB lane probability
	//x' = -ax + bu, with u = constant and x converging to LR_DEFAULTLANEPROB
	buoa = LR_DEFAULTLANEPROB;
	mLeftLaneProb = exp(-a*dt)*(mLeftLaneProb - buoa) + buoa;
	mCenterLaneProb = exp(-a*dt)*(mCenterLaneProb - buoa) + buoa;
	mRightLaneProb = exp(-a*dt)*(mRightLaneProb - buoa) + buoa;

	//realign the lanes, accounting for possible lane changes during the prediction
	AlignLanes();

	if (mModelCorrectProb < LR_MINROADCONFIDENCE)
	{
		//reset the entire model if its confidence is too low
		ResetRoadModel();
	}

	//set the current time equal to the prediction time
	mLocalRoadTime = iPredictTime;

	return;
}

bool LocalRoad::PredictForTransmit(double iPredictTime, VehicleOdometry* iVehicleOdometry)
{
	/*
	Predicts the LocalRoad model forward to the current time for transmit.
	Current values of the LocalRoad state are copied into the AI set of variables, 
	which are then predicted forward to the time of transmit.

	INPUTS:
		iPredictTime - the current vehicle time (time to which the LocalRoad is to
			be predicted)
		iVehicleOdometry - vehicle odometry structure valid over the prediction window

	OUTPUTS:
		rSuccess - true if the prediction was successful, false otherwise.  If true, sets 
			the mAI* variables to the current LocalRoad model predicted to the present time
	*/

	bool rSuccess = false;

	if (iVehicleOdometry->IsValid == false)
	{
		//do not predict if odometry is invalid
		return rSuccess;
	}

	if (mIsInitialized == false)
	{
		//do not predict if the local road is not initialized
		return rSuccess;
	}

	//extract relevant odometry information
	double dt = iPredictTime - mLocalRoadTime;
	if (dt < 0.0)
	{
		//do not predict backwards
		return rSuccess;
	}

	int i;
	int j;

	//***INITIALIZE THE mAI* variables to the current LocalRoad

	mAILocalRoadTime = mLocalRoadTime;
	mAIModelCorrectProb = mModelCorrectProb;

	mAIRoadOffset = mRoadOffset;
	mAIRoadHeading = mRoadHeading;
	mAIRoadCurvature = mRoadCurvature;

	for (i = 0; i < LR_NUMSTATES; i++)
	{
		for (j = 0; j < LR_NUMSTATES; j++)
		{
			mAIRoadCovariance[midx(i, j, LR_NUMSTATES)] = mRoadCovariance[midx(i, j, LR_NUMSTATES)];
		}
	}

	mAILeftLaneWidth = mLeftLaneWidth;
	mAICenterLaneWidth = mCenterLaneWidth;
	mAIRightLaneWidth = mRightLaneWidth;

	mAICenterLaneWidthVar = mCenterLaneWidthVar;
	mAILeftLaneWidthVar = mLeftLaneWidthVar;
	mAIRightLaneWidthVar = mRightLaneWidthVar;

	mAILeftLaneProb = mLeftLaneProb;
	mAICenterLaneProb = mCenterLaneProb;
	mAIRightLaneProb = mRightLaneProb;

	if (dt > 0.0)
	{
		//***PREDICT THE ROAD MODEL

		int nx = LR_NUMSTATES;
		int nv = LR_NUMSTATES + 3;

		//accumulate all the variables and perform the prediction
		double x[3] = {mAIRoadOffset, mAIRoadHeading, mAIRoadCurvature};
		double xbar[3];
		double* P = mAIRoadCovariance;
		double Pbar[LR_NUMSTATES*LR_NUMSTATES];
		double Q[6*6];

		//set the process noise matrix
		for (i = 0; i < nv; i++)
		{
			for (j = 0; j < nv; j++)
			{
				Q[midx(i, j, nv)] = 0.0;
			}
		}
		Q[midx(0, 0, nv)] = LR_ROADOFFSETVAR / dt;
		Q[midx(1, 1, nv)] = LR_ROADHEADINGVAR / dt;
		Q[midx(2, 2, nv)] = LR_ROADCURVATUREVAR / dt;
		Q[midx(3, 3, nv)] = iVehicleOdometry->qvx / dt;
		Q[midx(4, 4, nv)] = iVehicleOdometry->qvy / dt;
		Q[midx(5, 5, nv)] = iVehicleOdometry->qwz / dt;

		//do the prediction and copy over the state and covariance
		KalmanPredict(xbar, Pbar, nx, nv, dt, x, P, Q, iVehicleOdometry, &RoadModelDynamics);

		mAIRoadOffset = xbar[0];
		mAIRoadHeading = UnwrapAngle(xbar[1]);
		mAIRoadCurvature = xbar[2];

		for (i = 0; i < nx; i++)
		{
			for (j = 0; j < nx; j++)
			{
				mAIRoadCovariance[midx(i, j, nx)] = Pbar[midx(i, j, nx)];
			}
		}

		//also predict the road model correctness

		//note: this just models exponential motion toward 0% model correctness
		//x' = -ax + bu, with u = constant and x converging to 0%
		double a = 3.0 / LR_T95PCT;
		//double bu = 0.0 * a;
		double buoa = 0.0;
		//mModelCorrectProb = exp(-a*dt)*mModelCorrectProb + bu/a*(1.0 - exp(-a*dt));
		mAIModelCorrectProb = exp(-a*dt)*(mAIModelCorrectProb - buoa) + buoa;

		//***PREDICT THE LANE MODEL

		//note: the lane width estimates don't change, but the variances do
		mAICenterLaneWidthVar += LR_LANEWIDTHVAR * dt;
		mAILeftLaneWidthVar += LR_LANEWIDTHVAR * dt;
		mAIRightLaneWidthVar += LR_LANEWIDTHVAR * dt;

		//also predict the lane probabilities

		//note: this just models exponential motion toward LR_DEFAULTLANEPROB lane probability
		//x' = -ax + bu, with u = constant and x converging to LR_DEFAULTLANEPROB
		buoa = LR_DEFAULTLANEPROB;
		mAILeftLaneProb = exp(-a*dt)*(mAILeftLaneProb - buoa) + buoa;
		mAICenterLaneProb = exp(-a*dt)*(mAICenterLaneProb - buoa) + buoa;
		mAIRightLaneProb = exp(-a*dt)*(mAIRightLaneProb - buoa) + buoa;
	}

	//realign the lanes, accounting for possible lane changes during the prediction
	AlignLanesForTransmit();

	if (mAIModelCorrectProb < LR_MINROADCONFIDENCE)
	{
		//reset the road model if its confidence is too low
		ResetRoadModelForTransmit();
	}

	//set the current time equal to the prediction time
	mAILocalRoadTime = iPredictTime;

	rSuccess = true;
	return rSuccess;
}

void LocalRoad::UpdateWithMobileye(double iMobileyeTime, double* iMobileyeBuff, Sensor* iMobileyeSensor)
{
	/*
	Updates the local road representation with a mobileye environment packet

	INPUTS:
		iMobileyeTime - timestamp on the packet
		iMobileyeBuff - the mobileye measurement buffer
		iMobileyeSensor - structure containing the mobileye camera location 
			parameters

	OUTPUTS:
		none.  Updates the local road model with the mobileye measurement
	*/

	if (mIsInitialized == false)
	{
		//do not update if the local road is not initialized
		return;
	}

	int i;
	int j;

	//***VALIDATE THE MEASUREMENT***

	//extract the lowest confidence from the mobileye to gate the measurement
	//minconf is 0 - 3
	if (iMobileyeBuff[17] < MOBILEYE_MIN_ROADCONF || iMobileyeBuff[18] < MOBILEYE_MIN_ROADCONF || iMobileyeBuff[21] < MOBILEYE_MIN_VALIDDISTANCE)
	{
		//mobileye could not find a road: decrease model correctness probability
		#ifdef LM_COMMONDEBUGMSGS
			printf("Mobileye road model rejected due to lack of confidence.\n");
		#endif

		//NOTE: mobileye saying there's no lane doesn't give good negative evidence, so don't use it to decrease model probability
		/*
		mModelCorrectProb = (1.0 - MOBILEYE_LANEACCURACY)*mModelCorrectProb / ((1.0 - MOBILEYE_LANEACCURACY)*mModelCorrectProb + (1.0 - MOBILEYE_LANEFPRATE)*(1.0 - mModelCorrectProb));

		//also decrease all the lanes' probabilities
		mLeftLaneProb = (1.0 - MOBILEYE_LANEACCURACY)*mLeftLaneProb / ((1.0 - MOBILEYE_LANEACCURACY)*mLeftLaneProb + (1.0 - MOBILEYE_LANEFPRATE)*(1.0 - mLeftLaneProb));
		mCenterLaneProb = (1.0 - MOBILEYE_LANEACCURACY)*mCenterLaneProb / ((1.0 - MOBILEYE_LANEACCURACY)*mCenterLaneProb + (1.0 - MOBILEYE_LANEFPRATE)*(1.0 - mCenterLaneProb));
		mRightLaneProb = (1.0 - MOBILEYE_LANEACCURACY)*mRightLaneProb / ((1.0 - MOBILEYE_LANEACCURACY)*mRightLaneProb + (1.0 - MOBILEYE_LANEFPRATE)*(1.0 - mRightLaneProb));
		*/

		return;
	}

	//***EXTRACT THE MEASUREMENT***

	double dllb = iMobileyeBuff[5];
	double dlb = iMobileyeBuff[3];
	double drb = iMobileyeBuff[4];
	double drrb = iMobileyeBuff[6];

	double lwidth = -DBL_MAX;
	double cwidth = -DBL_MAX;
	double rwidth = -DBL_MAX;

	//extract lane width measurements from the mobileye buffer
	if (iMobileyeBuff[19] >= MOBILEYE_MIN_LANECONF)
	{
		//left lane was valid, so calculate left lane width
		lwidth = fabs(dlb - dllb);
	}
	//center lane width
	cwidth = fabs(drb - dlb);
	if (iMobileyeBuff[20] >= MOBILEYE_MIN_LANECONF)
	{
		//right lane was valid, so calculate right lane width
		rwidth = fabs(drrb - drb);
	}

	//account for mobileye combining lanes
	if (cwidth > MOBILEYE_MAX_LANEWIDTH)
	{
		//the center lane was too big... split it into two
		if (fabs(dlb) > fabs(drb))
		{
			//the left lane line is farther away: split the lane into a center lane and a left lane
			dllb = dlb;
			dlb = 0.5*(drb + dlb);
			//and now there are new width measurements
			lwidth = fabs(dlb - dllb);
			cwidth = fabs(drb - dlb);
		}
		else
		{
			//the right lane line is farther away: split the lane into a center lane and a right lane
			drrb = drb;
			drb = 0.5*(drb + dlb);
			//and now there are new width measurements
			cwidth = fabs(drb - dlb);
			rwidth = fabs(drrb - drb);
		}
	}

	//check for valid lane widths
	if (lwidth < MOBILEYE_MIN_LANEWIDTH || lwidth > MOBILEYE_MAX_LANEWIDTH)
	{
		lwidth = -DBL_MAX;
	}
	if (cwidth < MOBILEYE_MIN_LANEWIDTH || cwidth > MOBILEYE_MAX_LANEWIDTH)
	{
		cwidth = -DBL_MAX;
	}
	if (rwidth < MOBILEYE_MIN_LANEWIDTH || rwidth > MOBILEYE_MAX_LANEWIDTH)
	{
		rwidth = -DBL_MAX;
	}

	//extract road measurements from the mobileye buffer
	double zmobileye[3];
	//lane tracking estimate of lane center wrt mobileye camera (vehicle coordinates)
	zmobileye[0] = -0.5*(dlb + drb);
	//lane tracking estimate of lane heading wrt mobileye camera (vehicle coordinates)
	zmobileye[1] = -atan(iMobileyeBuff[7]);
	//lane tracking estimate of lane curvature wrt mobileye camera (vehicle coordinates)
	zmobileye[2] = -iMobileyeBuff[8];

	double Rmobileye[9];
	for (i = 0; i < 3; i++)
	{
		for (j = 0; j < 3; j++)
		{
			Rmobileye[midx(i, j, 3)] = 0.0;
		}
	}
	Rmobileye[midx(0, 0, 3)] = MOBILEYE_LANEOFST_VAR;
	Rmobileye[midx(1, 1, 3)] = MOBILEYE_HDGOFST_VAR;
	Rmobileye[midx(2, 2, 3)] = MOBILEYE_CRVOFST_VAR;

	double zmobileyelane[3] = {lwidth, cwidth, rwidth};

	//NOTE: lane width covariance is doubled because two measurements are subtracted
	double Rmobileyelane = 2.0*MOBILEYE_LANEOFST_VAR;

	//***ADJUST FOR CAMERA POSITION***

	double sy = iMobileyeSensor->SensorY;
	double syaw = iMobileyeSensor->SensorYaw;

	if (fabs(syaw) > PIOTWO)
	{
		//the sensor is pointed backwards, so reverse the signs on the measurements
		zmobileye[0] = -zmobileye[0];
		zmobileye[1] = -zmobileye[1];
		zmobileye[2] = -zmobileye[2];

		//and flip the lanes
		double temp = zmobileyelane[0];
		zmobileyelane[0] = zmobileyelane[2];
		zmobileyelane[2] = temp;
	}

	//account for camera offset from car centerline for lane offset
	//note: this calculation assumes the cameras can only measure the road at small angles (which is true)
	zmobileye[0] += sy;

	//account for camera yaw offset from the car's forward direction for lane direction
	zmobileye[1] = UnwrapAngle(zmobileye[1] + syaw);

	//account for mobileye measuring the wrong lane
	if (mLeftLaneProb > LR_MINLANEEXISTENCEPROB && zmobileye[0] > 0.5*mCenterLaneWidth)
	{
		//mobileye's "center" lane is actually the left lane: shift the measurement to the center lane
		zmobileye[0] -= 0.5*(mCenterLaneWidth + mLeftLaneWidth);

		//also shift the lane width measurements
		zmobileyelane[0] = zmobileyelane[1];
		zmobileyelane[1] = zmobileyelane[2];
		zmobileyelane[2] = -DBL_MAX;
	}
	if (mRightLaneProb > LR_MINLANEEXISTENCEPROB && zmobileye[0] < -0.5*mCenterLaneWidth)
	{
		//mobileye's "center" lane is actually the right lane: shift the measurement to the center lane
		zmobileye[0] += 0.5*(mCenterLaneWidth + mRightLaneWidth);

		//also shift the lane width measurements
		zmobileyelane[2] = zmobileyelane[1];
		zmobileyelane[1] = zmobileyelane[0];
		zmobileyelane[0] = -DBL_MAX;
	}

	if (mIsValid == true)
	{
		//if the road model is already valid, perform regular Kalman likelihoods and updates

		//***PERFORM KALMAN UPDATE ON ROAD MODEL***

		int nx = 3;
		int nz = 3;

		double H[9];
		double lambda;
		double nu[3];
		double Pbar[9];
		double Phat[9];
		double S[9];
		double W[9];
		double xbar[3] = {mRoadOffset, mRoadHeading, mRoadCurvature};
		double xhat[3];

		for (i = 0; i < nz; i++)
		{
			for (j = 0; j < nx; j++)
			{
				//set the measurement matrix to identity
				if (i == j)
				{
					H[midx(i, j, nz)] = 1.0;
				}
				else
				{
					H[midx(i, j, nz)] = 0.0;
				}
			}
		}

		for (i = 0; i < nx; i++)
		{
			for (j = 0; j < nx; j++)
			{
				//initialize the prediction covariance
				Pbar[midx(i, j, nx)] = mRoadCovariance[midx(i, j, nx)];
			}
		}

		//compute measurement likelihood
		lambda = KalmanLikelihood(nu, S, W, nx, nz, zmobileye, Rmobileye, H, xbar, Pbar, MOBILEYE_ROADCHI2GATE);

		if (fabs(lambda) > 0.0)
		{
			//perform the measurement update
			nu[1] = UnwrapAngle(nu[1]);
			KalmanUpdate(xhat, Phat, nx, nz, xbar, Pbar, nu, S, W);

			mRoadOffset = xhat[0];
			mRoadHeading = UnwrapAngle(xhat[1]);
			mRoadCurvature = xhat[2];

			for (i = 0; i < nx; i++)
			{
				for (j = 0; j < nx; j++)
				{
					mRoadCovariance[midx(i, j, nx)] = Phat[midx(i, j, nx)];
				}
			}

			//also update the road model probability if the measurement was gated ok
			mModelCorrectProb = lambda * mModelCorrectProb / (lambda * mModelCorrectProb + LR_UNIFORMMODEL * (1.0 - mModelCorrectProb));
		}
		else
		{
			//mobileye measurement failed the gating, so use this as evidence against model validity
			#ifdef LM_COMMONDEBUGMSGS
				printf("Mobileye road model failed Chi2 test.\n");
			#endif

			mModelCorrectProb = (1.0 - MOBILEYE_LANEACCURACY)*mModelCorrectProb / ((1.0 - MOBILEYE_LANEACCURACY)*mModelCorrectProb + (1.0 - MOBILEYE_LANEFPRATE)*(1.0 - mModelCorrectProb));
		}

		//done with the model state update

		//***PERFORM KALMAN UPDATE ON LANE MODEL***

		if (zmobileyelane[0] != -DBL_MAX)
		{
			//mobileye measured the left lane

			double nu = zmobileyelane[0] - mLeftLaneWidth;
			double S = mLeftLaneWidthVar + Rmobileyelane;
			double W = mLeftLaneWidthVar/S;
			if (nu*nu/S <= MOBILEYE_LANECHI2GATE)
			{
				//the lane measurement passed the gate test
				mLeftLaneWidth += W * nu;
				mLeftLaneWidthVar -= W*S*W;

				//update with evidence that the lane exists
				mLeftLaneProb = MOBILEYE_LANEACCURACY*mLeftLaneProb / (MOBILEYE_LANEACCURACY*mLeftLaneProb + MOBILEYE_LANEFPRATE*(1.0 - mLeftLaneProb));
			}
			else
			{
				//the lane measurement failed the gate test... evidence the lane doesn't exist
				mLeftLaneProb = (1.0 - MOBILEYE_LANEACCURACY)*mLeftLaneProb / ((1.0 - MOBILEYE_LANEACCURACY)*mLeftLaneProb + (1.0 - MOBILEYE_LANEFPRATE)*(1.0 - mLeftLaneProb));
			}
		}
		else
		{
			//update with evidence that the lane doesn't exist
			mLeftLaneProb = (1.0 - MOBILEYE_LANEACCURACY)*mLeftLaneProb / ((1.0 - MOBILEYE_LANEACCURACY)*mLeftLaneProb + (1.0 - MOBILEYE_LANEFPRATE)*(1.0 - mLeftLaneProb));
		}

		if (zmobileyelane[1] != -DBL_MAX)
		{
			//mobileye measured the center lane

			double nu = zmobileyelane[1] - mCenterLaneWidth;
			double S = mCenterLaneWidthVar + Rmobileyelane;
			double W = mCenterLaneWidthVar/S;

			if (nu*nu/S <= MOBILEYE_LANECHI2GATE)
			{
				//the lane measurement passed the gate test
				mCenterLaneWidth += W * nu;
				mCenterLaneWidthVar -= W*S*W;

				//update with evidence that the lane exists
				mCenterLaneProb = MOBILEYE_LANEACCURACY*mCenterLaneProb / (MOBILEYE_LANEACCURACY*mCenterLaneProb + MOBILEYE_LANEFPRATE*(1.0 - mCenterLaneProb));
			}
			else
			{
				//the lane measurement failed the gate test... evidence the lane doesn't exist
				mCenterLaneProb = (1.0 - MOBILEYE_LANEACCURACY)*mCenterLaneProb / ((1.0 - MOBILEYE_LANEACCURACY)*mCenterLaneProb + (1.0 - MOBILEYE_LANEFPRATE)*(1.0 - mCenterLaneProb));
			}
		}
		else
		{
			//update with evidence that the lane doesn't exist
			mCenterLaneProb = (1.0 - MOBILEYE_LANEACCURACY)*mCenterLaneProb / ((1.0 - MOBILEYE_LANEACCURACY)*mCenterLaneProb + (1.0 - MOBILEYE_LANEFPRATE)*(1.0 - mCenterLaneProb));
		}

		if (zmobileyelane[2] != -DBL_MAX)
		{
			//mobileye measured the right lane

			double nu = zmobileyelane[2] - mRightLaneWidth;
			double S = mRightLaneWidthVar + Rmobileyelane;
			double W = mRightLaneWidthVar / S;

			if (nu*nu/S <= MOBILEYE_LANECHI2GATE)
			{
				//the lane measurement passed the gate test

				mRightLaneWidth += W * nu;
				mRightLaneWidthVar -= W*S*W;

				//update with evidence that the lane exists
				mRightLaneProb = MOBILEYE_LANEACCURACY*mRightLaneProb / (MOBILEYE_LANEACCURACY*mRightLaneProb + MOBILEYE_LANEFPRATE*(1.0 - mRightLaneProb));
			}
			else
			{
				//update with evidence that the lane doesn't exist
				mRightLaneProb = (1.0 - MOBILEYE_LANEACCURACY)*mRightLaneProb / ((1.0 - MOBILEYE_LANEACCURACY)*mRightLaneProb + (1.0 - MOBILEYE_LANEFPRATE)*(1.0 - mRightLaneProb));
			}
		}
		else
		{
			//update with evidence that the lane doesn't exist
			mRightLaneProb = (1.0 - MOBILEYE_LANEACCURACY)*mRightLaneProb / ((1.0 - MOBILEYE_LANEACCURACY)*mRightLaneProb + (1.0 - MOBILEYE_LANEFPRATE)*(1.0 - mRightLaneProb));
		}
	}
	else
	{
		//if the model is not valid, use the measurement to initialize it

		//set the filter likelihood to the measurement likelihood at perfect innovation
		double detR = Rmobileye[midx(0, 0, 3)]*Rmobileye[midx(1, 1, 3)]*Rmobileye[midx(2, 2, 3)];
		double lambda = 1.0 / sqrt(pow(TWOPI, 3.0) * detR);

		//initialize the road state
		mRoadOffset = zmobileye[0];
		mRoadHeading = zmobileye[1];
		mRoadCurvature = zmobileye[2];

		//initialize the road covariance
		for (i = 0; i < LR_NUMSTATES; i++)
		{
			for (j = 0; j < LR_NUMSTATES; j++)
			{
				mRoadCovariance[midx(i, j, LR_NUMSTATES)] = Rmobileye[midx(i, j, LR_NUMSTATES)];
			}
		}

		//also initialize the road model probability

		mModelCorrectProb = LR_DEFAULTMODELPROB;
		mModelCorrectProb = lambda * mModelCorrectProb / (lambda * mModelCorrectProb + LR_UNIFORMMODEL * (1.0 - mModelCorrectProb));

		//***PERFORM KALMAN UPDATE ON LANE MODEL***

		if (zmobileyelane[0] != -DBL_MAX)
		{
			//mobileye measured the left lane

			mLeftLaneWidth = zmobileyelane[0];
			mLeftLaneWidthVar = Rmobileyelane;

			//update with evidence that the lane exists
			mLeftLaneProb = LR_DEFAULTLANEPROB;
			mLeftLaneProb = MOBILEYE_LANEACCURACY*mLeftLaneProb / (MOBILEYE_LANEACCURACY*mLeftLaneProb + MOBILEYE_LANEFPRATE*(1.0 - mLeftLaneProb));
		}
		else
		{
			//update with evidence that the lane doesn't exist
			mLeftLaneProb = LR_DEFAULTLANEPROB;
			mLeftLaneProb = (1.0 - MOBILEYE_LANEACCURACY)*mLeftLaneProb / ((1.0 - MOBILEYE_LANEACCURACY)*mLeftLaneProb + (1.0 - MOBILEYE_LANEFPRATE)*(1.0 - mLeftLaneProb));
		}

		if (zmobileyelane[1] != -DBL_MAX)
		{
			//mobileye measured the center lane

			mCenterLaneWidth = zmobileyelane[1];
			mCenterLaneWidthVar = Rmobileyelane;

			//update with evidence that the lane exists
			mCenterLaneProb = LR_DEFAULTLANEPROB;
			mCenterLaneProb = MOBILEYE_LANEACCURACY*mCenterLaneProb / (MOBILEYE_LANEACCURACY*mCenterLaneProb + MOBILEYE_LANEFPRATE*(1.0 - mCenterLaneProb));
		}
		else
		{
			//update with evidence that the lane doesn't exist
			mCenterLaneProb = LR_DEFAULTLANEPROB;
			mCenterLaneProb = (1.0 - MOBILEYE_LANEACCURACY)*mCenterLaneProb / ((1.0 - MOBILEYE_LANEACCURACY)*mCenterLaneProb + (1.0 - MOBILEYE_LANEFPRATE)*(1.0 - mCenterLaneProb));
		}

		if (zmobileyelane[2] != -DBL_MAX)
		{
			//mobileye measured the right lane

			mRightLaneWidth = zmobileyelane[2];
			mRightLaneWidthVar = Rmobileyelane;

			//update with evidence that the lane exists
			mRightLaneProb = LR_DEFAULTLANEPROB;
			mRightLaneProb = MOBILEYE_LANEACCURACY*mRightLaneProb / (MOBILEYE_LANEACCURACY*mRightLaneProb + MOBILEYE_LANEFPRATE*(1.0 - mRightLaneProb));
		}
		else
		{
			//update with evidence that the lane doesn't exist
			mRightLaneProb = LR_DEFAULTLANEPROB;
			mRightLaneProb = (1.0 - MOBILEYE_LANEACCURACY)*mRightLaneProb / ((1.0 - MOBILEYE_LANEACCURACY)*mRightLaneProb + (1.0 - MOBILEYE_LANEFPRATE)*(1.0 - mRightLaneProb));
		}

		mIsValid = true;
	}

	//***REALIGN LANES AFTER UPDATE***

	AlignLanes();

	if (mModelCorrectProb < LR_MINROADCONFIDENCE)
	{
		//reset the road model if its confidence is too low
		ResetRoadModel();
	}

	mLocalRoadTime = iMobileyeTime;

	return;
}

void LocalRoad::UpdateWithJason(double iJasonTime, int iNumSegmentations, double* iJasonBuff, Sensor* iJasonSensor)
{
	/*
	Updates the local road representation with a jason roadfinder packet

	INPUTS:
		iJasonTime - timestamp on the packet
		iNumSegmentations - number of found road segmentations
		iJasonBuff - the roadfinder measurement buffer
		iJasonSensor - structure containing the camera location parameters

	OUTPUTS:
		none.  Updates the local road model with the roadfinder measurement
	*/

	if (mIsInitialized == false)
	{
		//do not update if the local road is not initialized
		return;
	}

	//***VALIDATE MEASUREMENTS***

	int i;
	int j;
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
		//no segmentations were good enough to be considered: decrease model correctness probability
		#ifdef LM_COMMONDEBUGMSGS
			printf("Jason road fit rejected due to lack of confidence.\n");
		#endif

		/*
		mModelCorrectProb = (1.0 - JASON_LANEACCURACY)*mModelCorrectProb / 
			((1.0 - JASON_LANEACCURACY)*mModelCorrectProb + (1.0 - JASON_LANEFPRATE)*(1.0 - mModelCorrectProb));

		//also decrease the probability of each lane
		mLeftLaneProb = (1.0 - JASON_LANEACCURACY)*mLeftLaneProb / ((1.0 - JASON_LANEACCURACY)*mLeftLaneProb + (1.0 - JASON_LANEFPRATE)*(1.0 - mLeftLaneProb));
		mCenterLaneProb = (1.0 - JASON_LANEACCURACY)*mCenterLaneProb / ((1.0 - JASON_LANEACCURACY)*mCenterLaneProb + (1.0 - JASON_LANEFPRATE)*(1.0 - mCenterLaneProb));
		mRightLaneProb = (1.0 - JASON_LANEACCURACY)*mRightLaneProb / ((1.0 - JASON_LANEACCURACY)*mRightLaneProb + (1.0 - JASON_LANEFPRATE)*(1.0 - mRightLaneProb));
		*/

		delete [] use;
		return;
	}

	//***EXTRACT THE MEASUREMENTS***

	//convert jason's lane lines to a lane center position, heading, and curvature
	double* zjason = new double[3*nj];
	double* zjasonlane = new double[3*nj];

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
			//this segmentation found no lanes, just one line (or no lines)
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

		//check that the two boundaries make a valid lane
		int bl;
		double dlb;
		int br;
		double drb;
		if (db1 > db2)
		{
			bl = b1;
			dlb = db1;
			br = b2;
			drb = db2;
		}
		else
		{
			bl = b2;
			dlb = db2;
			br = b1;
			drb = db1;
		}

		//extract measurements of lane widths also
		double lwidth = -DBL_MAX;
		double cwidth = dlb - drb;
		double rwidth = -DBL_MAX;
		if (cwidth < JASON_MIN_LANEWIDTH)
		{
			//the lane found is too small
			use[i] = false;
			continue;
		}
		if (bl > 4)
		{
			//there was another boundary to the left
			lwidth = iJasonBuff[midx(i, bl-1, nj)] - dlb;
		}
		if (br - 3 < nb)
		{
			//there was another boundary to the right
			rwidth = drb - iJasonBuff[midx(i, br+1, nj)];
		}

		//construct a center lane measurement from the left bound and the right bound
		if (cwidth > JASON_MAX_LANEWIDTH)
		{
			//split the lane into two

			if (fabs(dlb) > fabs(drb))
			{
				//the new lane will be the right boundary and the centerline

				//and now there's a left lane measurement as well
				lwidth = dlb - 0.5*(dlb + drb);
				//and a new center lane width
				cwidth = 0.5*(dlb + drb) - drb;

				dlb = 0.5*(dlb + drb);
			}
			else
			{
				//the new lane will be the left boundary and the centerline

				//and now there's a right lane measurement as well
				rwidth = 0.5*(dlb + drb) - drb;
				//and a new center lane width
				cwidth = dlb - 0.5*(dlb + drb);

				drb = 0.5*(dlb + drb);
			}
		}

		//check each found lane's width for validity
		if (lwidth < JASON_MIN_LANEWIDTH || lwidth > JASON_MAX_LANEWIDTH)
		{
			lwidth = -DBL_MAX;
		}
		if (cwidth < JASON_MIN_LANEWIDTH || cwidth > JASON_MAX_LANEWIDTH)
		{
			cwidth = -DBL_MAX;
		}
		if (rwidth < JASON_MIN_LANEWIDTH || rwidth > JASON_MAX_LANEWIDTH)
		{
			rwidth = -DBL_MAX;
		}

		//store location of the lane centerline wrt the camera (vehicle coordinates)
		zjason[midx(i, 0, nj)] = 0.5*(dlb + drb);
		//store the heading of the road wrt the camera (vehicle coordinates)
		zjason[midx(i, 1, nj)] = -atan(iJasonBuff[midx(i, 12, nj)]);
		//store the curvature of the road (vehicle coordinates)
		zjason[midx(i, 2, nj)] = -(iJasonBuff[midx(i, 13, nj)]);

		//store the lane width measurements
		zjasonlane[midx(i, 0, nj)] = lwidth;
		zjasonlane[midx(i, 1, nj)] = cwidth;
		zjasonlane[midx(i, 2, nj)] = rwidth;

		//count this measurement as extracted successfully
		use[i] = true;
		nused++;
	}

	if (nused == 0)
	{
		//couldn't find a lane pair with a valid lane width: decrease model correctness probability
		mModelCorrectProb = (1.0 - JASON_LANEACCURACY)*mModelCorrectProb / 
			((1.0 - JASON_LANEACCURACY)*mModelCorrectProb + (1.0 - JASON_LANEFPRATE)*(1.0 - mModelCorrectProb));

		//also decrease the probability of each lane
		mLeftLaneProb = (1.0 - JASON_LANEACCURACY)*mLeftLaneProb / ((1.0 - JASON_LANEACCURACY)*mLeftLaneProb + (1.0 - JASON_LANEFPRATE)*(1.0 - mLeftLaneProb));
		mCenterLaneProb = (1.0 - JASON_LANEACCURACY)*mCenterLaneProb / ((1.0 - JASON_LANEACCURACY)*mCenterLaneProb + (1.0 - JASON_LANEFPRATE)*(1.0 - mCenterLaneProb));
		mRightLaneProb = (1.0 - JASON_LANEACCURACY)*mRightLaneProb / ((1.0 - JASON_LANEACCURACY)*mRightLaneProb + (1.0 - JASON_LANEFPRATE)*(1.0 - mRightLaneProb));

		delete [] use;
		delete [] zjason;
		delete [] zjasonlane;

		#ifdef LM_COMMONDEBUGMSGS
			printf("Jason lane update rejected due to invalid lane widths.\n");
		#endif

		return;
	}

	//now pull the most confident road measurement
	int ibest = 0;
	double cbest = -DBL_MAX;
	for (i = 0; i < nj; i++)
	{
		if (use[i] == false)
		{
			continue;
		}

		if (iJasonBuff[midx(i, 14, nj)] > cbest)
		{
			//found a new better measurement
			ibest = i;
			cbest = iJasonBuff[midx(i, 14, nj)];
		}
	}

	//define the measurement covariance matrix for correct detections
	double Rroad[9];
	for (i = 0; i < 3; i++)
	{
		for (j = 0; j < 3; j++)
		{
			Rroad[midx(i, j, 3)] = 0.0;
		}
	}
	Rroad[midx(0, 0, 3)] = JASON_LANEOFST_VAR;
	Rroad[midx(1, 1, 3)] = JASON_HDGOFST_VAR;
	Rroad[midx(2, 2, 3)] = JASON_CRVOFST_VAR;

	//NOTE: lane width covariance is doubled because two measurements are subtracted
	double Rlane = 2.0*JASON_LANEOFST_VAR;

	//measurements in row ibest are the best measurements and will be used
	double zroad[3] = {zjason[midx(ibest, 0, nj)], zjason[midx(ibest, 1, nj)], zjason[midx(ibest, 2, nj)]};
	double zlane[3] = {zjasonlane[midx(ibest, 0, nj)], zjasonlane[midx(ibest, 1, nj)], zjasonlane[midx(ibest, 2, nj)]};

	//***ADJUST FOR CAMERA POSITION***

	double sy = iJasonSensor->SensorY;
	double syaw = iJasonSensor->SensorYaw;

	if (fabs(syaw) > PIOTWO)
	{
		//the sensor is pointed backwards, so reverse the signs on the measurements
		zroad[0] = -zroad[0];
		zroad[1] = -zroad[1];
		zroad[2] = -zroad[2];

		//and flip the lanes
		double temp = zlane[0];
		zlane[0] = zlane[2];
		zlane[2] = temp;
	}

	//account for camera offset from car centerline for lane offset
	//note: this calculation assumes the cameras can only measure the road at small angles (which is true)
	zroad[0] += sy;

	//account for camera yaw offset from the car's forward direction for lane direction
	zroad[1] = UnwrapAngle(zroad[1] + syaw);

	//account for jason measuring the wrong lane
	if (mLeftLaneProb > LR_MINLANEEXISTENCEPROB && zroad[0] > 0.5*mCenterLaneWidth)
	{
		//jason's "center" lane is actually the left lane: shift the measurement to the center lane
		zroad[0] -= 0.5*(mCenterLaneWidth + mLeftLaneWidth);

		//also shift the lane width measurements
		zlane[0] = zlane[1];
		zlane[1] = zlane[2];
		zlane[2] = -DBL_MAX;
	}
	if (mRightLaneProb > LR_MINLANEEXISTENCEPROB && zroad[0] < -0.5*mCenterLaneWidth)
	{
		//jason's "center" lane is actually the right lane: shift the measurement to the center lane
		zroad[0] += 0.5*(mCenterLaneWidth + mRightLaneWidth);

		//also shift the lane width measurements
		zlane[2] = zlane[1];
		zlane[1] = zlane[0];
		zlane[0] = -DBL_MAX;
	}

	if (mIsValid == true)
	{
		//***PERFORM KALMAN UPDATE ON ROAD MODEL***

		int nx = 3;
		int nz = 3;

		double H[9];
		double nu[3];
		double lambda;
		double Pbar[9];
		double Phat[9];
		double S[9];
		double W[9];
		double xbar[3] = {mRoadOffset, mRoadHeading, mRoadCurvature};
		double xhat[3];

		for (i = 0; i < nz; i++)
		{
			for (j = 0; j < nx; j++)
			{
				//set the measurement matrix to identity
				if (i == j)
				{
					H[midx(i, j, nz)] = 1.0;
				}
				else
				{
					H[midx(i, j, nz)] = 0.0;
				}
			}
		}

		for (i = 0; i < nx; i++)
		{
			for (j = 0; j < nx; j++)
			{
				//initialize the prediction covariance
				Pbar[midx(i, j, nx)] = mRoadCovariance[midx(i, j, nx)];
			}
		}

		//compute measurement likelihood
		lambda = KalmanLikelihood(nu, S, W, nx, nz, zroad, Rroad, H, xbar, Pbar, JASON_ROADCHI2GATE);

		if (fabs(lambda) > 0.0)
		{
			//perform the measurement update
			nu[1] = UnwrapAngle(nu[1]);
			KalmanUpdate(xhat, Phat, nx, nz, xbar, Pbar, nu, S, W);

			mRoadOffset = xhat[0];
			mRoadHeading = UnwrapAngle(xhat[1]);
			mRoadCurvature = xhat[2];

			for (i = 0; i < nx; i++)
			{
				for (j = 0; j < nx; j++)
				{
					mRoadCovariance[midx(i, j, nx)] = Phat[midx(i, j, nx)];
				}
			}

			//also update the road model probability if the measurement passed the gate

			mModelCorrectProb = lambda * mModelCorrectProb / (lambda * mModelCorrectProb + LR_UNIFORMMODEL * (1.0 - mModelCorrectProb));
		}
		else
		{
			//measurement failed the gating, so use this as evidence against model validity
			#ifdef LM_COMMONDEBUGMSGS
				printf("Jason road model failed Chi2 test.\n");
			#endif

			mModelCorrectProb = (1.0 - JASON_LANEACCURACY)*mModelCorrectProb / 
				((1.0 - JASON_LANEACCURACY)*mModelCorrectProb + (1.0 - JASON_LANEFPRATE)*(1.0 - mModelCorrectProb));
		}

		//***PERFORM KALMAN UPDATE ON LANE MODEL***

		//also update lane model probability

		if (zlane[0] != -DBL_MAX)
		{
			//jason measured the left lane

			double nu = zlane[0] - mLeftLaneWidth;
			double S = mLeftLaneWidthVar + Rlane;
			double W = mLeftLaneWidthVar/S;

			if (nu*nu/S <= JASON_LANECHI2GATE)
			{
				//update with the lane width when it passes the gate
				mLeftLaneWidth += W * nu;
				mLeftLaneWidthVar -= W*S*W;

				//update with evidence that the lane exists
				mLeftLaneProb = JASON_LANEACCURACY*mLeftLaneProb / (JASON_LANEACCURACY*mLeftLaneProb + JASON_LANEFPRATE*(1.0 - mLeftLaneProb));
			}
			else
			{
				//decrease the lane's probability when it fails the gate
				mLeftLaneProb = (1.0 - JASON_LANEACCURACY)*mLeftLaneProb / ((1.0 - JASON_LANEACCURACY)*mLeftLaneProb + (1.0 - JASON_LANEFPRATE)*(1.0 - mLeftLaneProb));
			}
		}
		else
		{
			//update with evidence that the lane doesn't exist
			mLeftLaneProb = (1.0 - JASON_LANEACCURACY)*mLeftLaneProb / ((1.0 - JASON_LANEACCURACY)*mLeftLaneProb + (1.0 - JASON_LANEFPRATE)*(1.0 - mLeftLaneProb));
		}
		if (zlane[1] != -DBL_MAX)
		{
			//jason measured the center lane

			double nu = zlane[1] - mCenterLaneWidth;
			double S = mCenterLaneWidthVar + Rlane;
			double W = mCenterLaneWidthVar/S;

			if (nu*nu/S <= JASON_LANECHI2GATE)
			{
				//update with the lane width when it passes the gate
				mCenterLaneWidth += W * nu;
				mCenterLaneWidthVar -= W*S*W;

				//update with evidence that the lane exists
				mCenterLaneProb = JASON_LANEACCURACY*mCenterLaneProb / (JASON_LANEACCURACY*mCenterLaneProb + JASON_LANEFPRATE*(1.0 - mCenterLaneProb));
			}
			else
			{
				//decrease the lane's probability when it fails the gate
				mCenterLaneProb = (1.0 - JASON_LANEACCURACY)*mCenterLaneProb / ((1.0 - JASON_LANEACCURACY)*mCenterLaneProb + (1.0 - JASON_LANEFPRATE)*(1.0 - mCenterLaneProb));
			}
		}
		else
		{
			//update with evidence that the lane doesn't exist
			mCenterLaneProb = (1.0 - JASON_LANEACCURACY)*mCenterLaneProb / ((1.0 - JASON_LANEACCURACY)*mCenterLaneProb + (1.0 - JASON_LANEFPRATE)*(1.0 - mCenterLaneProb));
		}
		if (zlane[2] != -DBL_MAX)
		{
			//jason measured the right lane

			double nu = zlane[2] - mRightLaneWidth;
			double S = mRightLaneWidthVar + Rlane;
			double W = mRightLaneWidthVar / S;
			
			if (nu*nu/S <= JASON_LANECHI2GATE)
			{
				//update with the lane width when it passes the gate
				mRightLaneWidth += W * nu;
				mRightLaneWidthVar -= W*S*W;

				//update with evidence that the lane exists
				mRightLaneProb = JASON_LANEACCURACY*mRightLaneProb / (JASON_LANEACCURACY*mRightLaneProb + JASON_LANEFPRATE*(1.0 - mRightLaneProb));
			}
			else
			{
				//decrease the lane's probability when it fails the gate
				mRightLaneProb = (1.0 - JASON_LANEACCURACY)*mRightLaneProb / ((1.0 - JASON_LANEACCURACY)*mRightLaneProb + (1.0 - JASON_LANEFPRATE)*(1.0 - mRightLaneProb));
			}
		}
		else
		{
			//update with evidence that the lane doesn't exist
			mRightLaneProb = (1.0 - JASON_LANEACCURACY)*mRightLaneProb / ((1.0 - JASON_LANEACCURACY)*mRightLaneProb + (1.0 - JASON_LANEFPRATE)*(1.0 - mRightLaneProb));
		}
	}
	else
	{
		//if the model is not valid, use the measurement to initialize it

		//set the filter likelihood to the measurement likelihood at perfect innovation
		double detR = Rroad[midx(0, 0, 3)]*Rroad[midx(1, 1, 3)]*Rroad[midx(2, 2, 3)];
		double lambda = 1.0 / sqrt(pow(TWOPI, 3.0) * detR);

		//initialize the road state
		mRoadOffset = zjason[0];
		mRoadHeading = zjason[1];
		mRoadCurvature = zjason[2];

		//initialize the road covariance
		for (i = 0; i < LR_NUMSTATES; i++)
		{
			for (j = 0; j < LR_NUMSTATES; j++)
			{
				mRoadCovariance[midx(i, j, LR_NUMSTATES)] = Rroad[midx(i, j, LR_NUMSTATES)];
			}
		}

		//also initialize the road model probability

		mModelCorrectProb = LR_DEFAULTMODELPROB;
		mModelCorrectProb = lambda * mModelCorrectProb / (lambda * mModelCorrectProb + LR_UNIFORMMODEL * (1.0 - mModelCorrectProb));

		//***PERFORM KALMAN UPDATE ON LANE MODEL***

		if (zjasonlane[0] != -DBL_MAX)
		{
			//jason measured the left lane

			mLeftLaneWidth = zjasonlane[0];
			mLeftLaneWidthVar = Rlane;

			//update with evidence that the lane exists
			mLeftLaneProb = LR_DEFAULTLANEPROB;
			mLeftLaneProb = JASON_LANEACCURACY*mLeftLaneProb / (JASON_LANEACCURACY*mLeftLaneProb + JASON_LANEFPRATE*(1.0 - mLeftLaneProb));
		}
		else
		{
			//update with evidence that the lane doesn't exist
			mLeftLaneProb = LR_DEFAULTLANEPROB;
			mLeftLaneProb = (1.0 - JASON_LANEACCURACY)*mLeftLaneProb / ((1.0 - JASON_LANEACCURACY)*mLeftLaneProb + (1.0 - JASON_LANEFPRATE)*(1.0 - mLeftLaneProb));
		}

		if (zjasonlane[1] != -DBL_MAX)
		{
			//jason measured the center lane

			mCenterLaneWidth = zjasonlane[1];
			mCenterLaneWidthVar = Rlane;

			//update with evidence that the lane exists
			mCenterLaneProb = LR_DEFAULTLANEPROB;
			mCenterLaneProb = JASON_LANEACCURACY*mCenterLaneProb / (JASON_LANEACCURACY*mCenterLaneProb + JASON_LANEFPRATE*(1.0 - mCenterLaneProb));
		}
		else
		{
			//update with evidence that the lane doesn't exist
			mCenterLaneProb = LR_DEFAULTLANEPROB;
			mCenterLaneProb = (1.0 - JASON_LANEACCURACY)*mCenterLaneProb / ((1.0 - JASON_LANEACCURACY)*mCenterLaneProb + (1.0 - JASON_LANEFPRATE)*(1.0 - mCenterLaneProb));
		}

		if (zjasonlane[2] != -DBL_MAX)
		{
			//jason measured the right lane

			mRightLaneWidth = zjasonlane[2];
			mRightLaneWidthVar = Rlane;

			//update with evidence that the lane exists
			mRightLaneProb = LR_DEFAULTLANEPROB;
			mRightLaneProb = JASON_LANEACCURACY*mRightLaneProb / (JASON_LANEACCURACY*mRightLaneProb + JASON_LANEFPRATE*(1.0 - mRightLaneProb));
		}
		else
		{
			//update with evidence that the lane doesn't exist
			mRightLaneProb = LR_DEFAULTLANEPROB;
			mRightLaneProb = (1.0 - JASON_LANEACCURACY)*mRightLaneProb / ((1.0 - JASON_LANEACCURACY)*mRightLaneProb + (1.0 - JASON_LANEFPRATE)*(1.0 - mRightLaneProb));
		}

		mIsValid = true;
	}

	//***REALIGN LANES AFTER UPDATE***

	AlignLanes();

	if (mModelCorrectProb < LR_MINROADCONFIDENCE)
	{
		//reset the road model if its confidence is too low
		ResetRoadModel();
	}

	mLocalRoadTime = iJasonTime;

	return;
}

void LocalRoad::PrintLocalRoad(FILE* iLocalRoadFile)
{
	/*
	Prints the local road message to the specified file

	INPUTS:
		iLocalRoadFile - file pointer to an open file for printing the
			local road message.

	OUTPUTS:
		none.  Prints a line to the local road file.
	*/

	if (mIsInitialized == false)
	{
		//don't print the local road until it is initialized
		return;
	}

	//print the road message
	fprintf(iLocalRoadFile, "%.12lg,%.12lg,%.12lg,%.12lg,%.12lg,%.12lg,%.12lg,%.12lg,",
		mAILocalRoadTime, mAIModelCorrectProb, mAIRoadOffset, mAIRoadHeading, mAIRoadCurvature, 
		mAIRoadCovariance[midx(0, 0, 3)], mAIRoadCovariance[midx(1, 1, 3)], mAIRoadCovariance[midx(2, 2, 3)]);

	//print the lane message (which is concatenated on the same line)
	fprintf(iLocalRoadFile, "%.12lg,%.12lg,%.12lg,%.12lg,%.12lg,%.12lg,%.12lg,%.12lg,%.12lg\n", mAICenterLaneProb, 
		mAILeftLaneProb, mAIRightLaneProb, mAICenterLaneWidth, mAILeftLaneWidth, mAIRightLaneWidth,
		mAICenterLaneWidthVar, mAILeftLaneWidthVar, mAIRightLaneWidthVar);

	return;
}

void LocalRoad::GenerateLocalRoadMessage(LocalRoadModelEstimateMsg* oLocalRoadMessage)
{
	/*
	Generates the local road message to be transmitted, using the mAI variables...
	NOTE: make sure you run PredictForTransmit before running this.

	INPUTS:
		oLocalRoadMessage - the local road message that will be populated on output.

	OUTPUTS:
		oLocalRoadMessage - populated on output.
	*/

	//populate the timestamp
	oLocalRoadMessage->timestamp = mAILocalRoadTime;

	//populate lane properties
	oLocalRoadMessage->probabilityLeftLaneExists = (float) mAILeftLaneProb;
	oLocalRoadMessage->laneWidthLeft = (float) mAILeftLaneWidth;
	oLocalRoadMessage->laneWidthLeftVariance = (float) mAILeftLaneWidthVar;

	oLocalRoadMessage->probabilityCenterLaneExists = (float) mAICenterLaneProb;
	oLocalRoadMessage->laneWidthCenter = (float) mAICenterLaneWidth;
	oLocalRoadMessage->laneWidthCenterVariance = (float) mAICenterLaneWidthVar;

	oLocalRoadMessage->probabilityRightLaneExists = (float) mAIRightLaneProb;
	oLocalRoadMessage->laneWidthRight = (float) mAIRightLaneWidth;
	oLocalRoadMessage->laneWidthRightVariance = (float) mAIRightLaneWidthVar;

	//populate the road model with points
	oLocalRoadMessage->probabilityRoadModelValid = (float) mAIModelCorrectProb;

	int h;
	int i;
	int j;
	int k;
	int np = MAX_LANE_POINTS;
	double gendist = LR_GENDISTANCE;
	double dx = gendist / ((double) np);
	double xcur = 0.0;
	double ycur;

	oLocalRoadMessage->numPointsLeft = np;
	oLocalRoadMessage->numPointsCenter = np;
	oLocalRoadMessage->numPointsRight = np;

	//extract the lane parameters
	double dll = 0.5*(mAILeftLaneWidth + mAICenterLaneWidth);
	double drl = -0.5*(mAICenterLaneWidth + mAIRightLaneWidth);

	//extract the road model parameters
	double ro = mAIRoadOffset;
	double rh = mAIRoadHeading;
	double cosrh = cos(rh);
	double sinrh = sin(rh);
	double rc = mAIRoadCurvature;

	double erx;
	double ery;
	double P[16];
	for (i = 0; i < 4; i++)
	{
		for (j = 0; j < 4; j++)
		{
			P[midx(i, j, 4)] = 0.0;
		}
	}
	for (i = 0; i < LR_NUMSTATES; i++)
	{
		for (j = 0; j < LR_NUMSTATES; j++)
		{
			P[midx(i, j, 4)] = mAIRoadCovariance[midx(i, j, LR_NUMSTATES)];
		}
	}

	for (h = 0; h < np; h++)
	{
		//generate the points for each lane

		double x2 = xcur*xcur;
		double J[8];
		double JP[8];
		double JPJt[4];

		//LEFT LANE POINTS
		ycur = dll + ro + rc*x2;
		//rotate the point into vehicle coordinates
		erx = cosrh*xcur - sinrh*ycur;
		ery = sinrh*xcur + cosrh*ycur;

		//calculate covariance for the point = J*P*J'
		J[midx(0, 0, 2)] = -sinrh;
		J[midx(0, 1, 2)] = -sinrh*xcur - cosrh*(dll + ro + rc*x2);
		J[midx(0, 2, 2)] = -sinrh*x2;
		J[midx(0, 3, 2)] = -sinrh;
		J[midx(1, 0, 2)] = cosrh;
		J[midx(1, 1, 2)] = cosrh*xcur - sinrh*(dll + ro + rc*x2);
		J[midx(1, 2, 2)] = cosrh*x2;
		J[midx(1, 3, 2)] = cosrh;
		P[midx(3, 3, 4)] = 0.25*(mAILeftLaneWidthVar + mAICenterLaneWidthVar);
		for (i = 0; i < 2; i++)
		{
			for (j = 0; j < 4; j++)
			{
				JP[midx(i, j, 2)] = 0.0;
				for (k = 0; k < 4; k++)
				{
					JP[midx(i, j, 2)] += J[midx(i, k, 2)] * P[midx(k, j, 4)];
				}
			}
		}
		for (i = 0; i < 2; i++)
		{
			for (j = 0; j < 2; j++)
			{
				JPJt[midx(i, j, 2)] = 0.0;
				for (k = 0; k < 4; k++)
				{
					JPJt[midx(i, j, 2)] += JP[midx(i, k, 2)] * J[midx(j, k, 2)];
				}
			}
		}
		//store the point
		oLocalRoadMessage->LanePointsLeft[h] = LocalRoadModelLanePoint(erx, ery, JPJt[midx(1, 1, 2)]);

		//CENTER LANE POINTS
		ycur = ro + rc*x2;
		//rotate the point into vehicle coordinates
		erx = cosrh*xcur - sinrh*ycur;
		ery = sinrh*xcur + cosrh*ycur;

		//calculate covariance for the point = J*P*J'
		J[midx(0, 0, 2)] = -sinrh;
		J[midx(0, 1, 2)] = -sinrh*xcur - cosrh*(ro + rc*x2);
		J[midx(0, 2, 2)] = -sinrh*x2;
		J[midx(0, 3, 2)] = -sinrh;
		J[midx(1, 0, 2)] = cosrh;
		J[midx(1, 1, 2)] = cosrh*xcur - sinrh*(ro + rc*x2);
		J[midx(1, 2, 2)] = cosrh*x2;
		J[midx(1, 3, 2)] = 0.0;
		P[midx(3, 3, 4)] = 0.0;
		for (i = 0; i < 2; i++)
		{
			for (j = 0; j < 4; j++)
			{
				JP[midx(i, j, 2)] = 0.0;
				for (k = 0; k < 4; k++)
				{
					JP[midx(i, j, 2)] += J[midx(i, k, 2)] * P[midx(k, j, 4)];
				}
			}
		}
		for (i = 0; i < 2; i++)
		{
			for (j = 0; j < 2; j++)
			{
				JPJt[midx(i, j, 2)] = 0.0;
				for (k = 0; k < 4; k++)
				{
					JPJt[midx(i, j, 2)] += JP[midx(i, k, 2)] * J[midx(j, k, 2)];
				}
			}
		}
		//store the point
		oLocalRoadMessage->LanePointsCenter[h] = LocalRoadModelLanePoint(erx, ery, JPJt[midx(1, 1, 2)]);

		//RIGHT LANE POINTS
		ycur = drl + ro + rc*x2;
		//rotate the point into vehicle coordinates
		erx = cosrh*xcur - sinrh*ycur;
		ery = sinrh*xcur + cosrh*ycur;

		//calculate covariance for the point = J*P*J'
		J[midx(0, 0, 2)] = -sinrh;
		J[midx(0, 1, 2)] = -sinrh*xcur - cosrh*(drl + ro + rc*x2);
		J[midx(0, 2, 2)] = -sinrh*x2;
		J[midx(0, 3, 2)] = -sinrh;
		J[midx(1, 0, 2)] = cosrh;
		J[midx(1, 1, 2)] = cosrh*xcur - sinrh*(drl + ro + rc*x2);
		J[midx(1, 2, 2)] = cosrh*x2;
		J[midx(1, 3, 2)] = cosrh;
		P[midx(3, 3, 4)] = 0.25*(mAICenterLaneWidthVar + mAIRightLaneWidthVar);
		for (i = 0; i < 2; i++)
		{
			for (j = 0; j < 4; j++)
			{
				JP[midx(i, j, 2)] = 0.0;
				for (k = 0; k < 4; k++)
				{
					JP[midx(i, j, 2)] += J[midx(i, k, 2)] * P[midx(k, j, 4)];
				}
			}
		}
		for (i = 0; i < 2; i++)
		{
			for (j = 0; j < 2; j++)
			{
				JPJt[midx(i, j, 2)] = 0.0;
				for (k = 0; k < 4; k++)
				{
					JPJt[midx(i, j, 2)] += JP[midx(i, k, 2)] * J[midx(j, k, 2)];
				}
			}
		}
		//store the point
		oLocalRoadMessage->LanePointsRight[h] = LocalRoadModelLanePoint(erx, ery, JPJt[midx(1, 1, 2)]);

		//increment the forward distance
		xcur += dx;
	}

	return;
}
