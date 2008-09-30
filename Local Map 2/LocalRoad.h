#ifndef LOCALROAD_H
#define LOCALROAD_H

#include "LocalMapConstants.h"
#include "LocalMapFunctions.h"
#include "MatrixIndex.h"
#include "Sensor.h"
#include "VehicleOdometry.h"

#include "..\Common\localMapInterface\localMapInterface.h"

#include <FLOAT.H>
#include <MATH.H>
#include <STDIO.H>

#define LR_NUMSTATES 3

class LocalRoad
{
	//The local road class.  Stores an estimate of the local road representation, generated
	//entirely by local sensing (i.e. independent of absolute anything).

private:

	//whether the local road estimate has been initialized successfully
	bool mIsInitialized;
	//whether the local road model is currently valid
	bool mIsValid;

	//the time at which the road model is valid
	double mLocalRoadTime;

	//parameters that describe the road shape
	double mModelCorrectProb;
	double mRoadOffset;
	double mRoadHeading;
	double mRoadCurvature;
	double mRoadCovariance[LR_NUMSTATES*LR_NUMSTATES];

	//parameters that describe the center lane of the road
	double mCenterLaneProb;
	double mCenterLaneWidth;
	double mCenterLaneWidthVar;

	//parameters that describe the left lane of the road
	double mLeftLaneProb;
	double mLeftLaneWidth;
	double mLeftLaneWidthVar;

	//parameters that describe the right lane of the road
	double mRightLaneProb;
	double mRightLaneWidth;
	double mRightLaneWidthVar;

	//auxiliary variables used to predict the LocalRoad forward for transmit
	double mAIRoadOffset;
	double mAIRoadHeading;
	double mAIRoadCurvature;

	double mAIRoadCovariance[LR_NUMSTATES*LR_NUMSTATES];

	double mAIModelCorrectProb;

	double mAILeftLaneWidth;
	double mAICenterLaneWidth;
	double mAIRightLaneWidth;

	double mAILeftLaneWidthVar;
	double mAICenterLaneWidthVar;
	double mAIRightLaneWidthVar;

	double mAILeftLaneProb;
	double mAICenterLaneProb;
	double mAIRightLaneProb;

	double mAILocalRoadTime;

public:

	LocalRoad();

	bool IsInitialized() {return mIsInitialized;}
	double LocalRoadTime() {return mLocalRoadTime;}

	void AlignLanes();
	void AlignLanesForTransmit();
	void GenerateLocalRoadMessage(LocalRoadModelEstimateMsg* oLocalRoadMessage);
	void Initialize(double iInitialTime);
	void Predict(double iPredictTime, VehicleOdometry* iVehicleOdometry);
	bool PredictForTransmit(double iPredictTime, VehicleOdometry* iVehicleOdometry);
	void PrintLocalRoad(FILE* iLocalRoadFile);
	void ResetRoadModel();
	void ResetRoadModelForTransmit();
	void UpdateWithJason(double iJasonTime, int iNumSegmentations, double* iJasonBuff, Sensor* iJasonSensor);
	void UpdateWithMobileye(double iMobileyeTime, double* iMobileyeBuff, Sensor* iMobileyeSensor);
};

#endif //LOCALROAD_H
