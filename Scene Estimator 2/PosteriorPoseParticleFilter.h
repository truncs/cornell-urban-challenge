#ifndef POSTERIORPOSEPARTICLEFILTER_H
#define POSTERIORPOSEPARTICLEFILTER_H

#include "acml.h"
#include "EnuEcef.h"
#include "MatrixIndex.h"
#include "Particle.h"
#include "PosteriorPosePosition.h"
#include "RandomNumberCache.h"
#include "RandomNumberGenerator.h"
#include "RoadGraph.h"
#include "RoadPartition.h"
#include "SceneEstimatorFunctions.h"
#include "Sensor.h"
#include "time/timing.h"
#include "VehicleOdometry.h"

//for communications
#include "ArbiterMessage.h"
#include "OperationalMessage.h"
#include "sceneEstimatorInterface\sceneEstimatorInterface.h"
#include "SceneEstimatorPublisher.h"

#include <FLOAT.H>
#include <MATH.H>
#include <STDIO.H>
#include <STDLIB.H>

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

//number of states in the particle filter
#define PF_NUMSTATES 5

class PosteriorPoseParticleFilter
{
	//The PosteriorPose particle filter.

private:

	//whether the filter has successfully been initialized by a measurement
	bool mIsInitialized;

	//number of particles in the filter
	int mNumParticles;
	//the particles in the filter
	Particle* mParticles;

	//the filter's timestamp (time when the particles are valid)
	double mPFTime;
	//the filter's current GPS mode
	int mGPSMode;

	//the random number generator for drawing random numbers
	RandomNumberGenerator* mPPGenerator;
	//the cache of Gaussian random numbers
	RandomNumberCache mGaussianCache;
	//the cache of uniform random numbers
	RandomNumberCache mUniformCache;

	//the filter's MMSE estimate
	double mEastMMSE;
	double mNorthMMSE;
	double mHeadingMMSE;
	double mGPSBiasEastMMSE;
	double mGPSBiasNorthMMSE;

	//the filter's MMSE covariance matrix (order is the same as the states listed above)
	double mCovMMSE[PF_NUMSTATES*PF_NUMSTATES];

	//pointer to the road graph used by this particle filter
	RoadGraph* mRoadGraph;
	//closest road partition based on MMSE estimate
	RoadPartition* mRoadLocation;

	//***LOCAL ROAD MODEL***
	//local map local road model
	bool mLRIsInitialized;
	double mLRTime;
	double mLRModelProb;
	double mLRLeftLaneProb;
	double mLRLeftLaneWidth;
	double mLRLeftLaneWidthVar;
	double mLRCenterLaneProb;
	double mLRCenterLaneWidth;
	double mLRCenterLaneWidthVar;
	double mLRRightLaneProb;
	double mLRRightLaneWidth;
	double mLRRightLaneWidthVar;
	int mLRNumLeftLanePoints;
	double* mLRLeftLanePoints;
	double* mLRLeftLaneVars;
	int mLRNumCenterLanePoints;
	double* mLRCenterLanePoints;
	double* mLRCenterLaneVars;
	int mLRNumRightLanePoints;
	double* mLRRightLanePoints;
	double* mLRRightLaneVars;

	//***VARIABLES TO STORE INFORMATION TO TRANSMIT TO AI***
	double mAITime;
	//arbiter information
	double mAISparseWaypoints;
	Particle* mAIParticles;
	double mAIEast;
	double mAINorth;
	double mAIHeading;
	double mAIGPSBiasEast;
	double mAIGPSBiasNorth;
	double mAICovariance[PF_NUMSTATES*PF_NUMSTATES];
	RoadPartition* mAIRoadLocation;
	int mAINumLikelyPartitions;
	UnmanagedArbiterPartition mAILikelyPartitions[ARBITER_MAX_PARTITIONS];

	//RNDF local road model
	bool mAIStoplineExists;
	double mAIDistanceToStopline;
	double mAIDistanceToStoplineVar;
	bool mAIRoadModelIsValid;
	double mAIRoadHeading;
	double mAIRoadHeadingVar;
	double mAIRoadCurvature;
	double mAIRoadCurvatureVar;
	bool mAICenterLaneExists;
	bool mAILeftLaneExists;
	bool mAIRightLaneExists;
	char mAICenterLaneID[ROADGRAPH_FIELDSIZE];
	char mAILeftLaneID[ROADGRAPH_FIELDSIZE];
	char mAIRightLaneID[ROADGRAPH_FIELDSIZE];
	double mAIDistToCenterLane;
	double mAIDistToCenterLaneVar;
	double mAIDistToLeftLane;
	double mAIDistToLeftLaneVar;
	double mAIDistToRightLane;
	double mAIDistToRightLaneVar;
	double mAICenterLaneWidth;
	double mAICenterLaneWidthVar;
	double mAILeftLaneWidth;
	double mAILeftLaneWidthVar;
	double mAIRightLaneWidth;
	double mAIRightLaneWidthVar;

	//local map local road model
	double mAILRModelProb;
	double mAILRLeftLaneProb;
	double mAILRLeftLaneWidth;
	double mAILRLeftLaneWidthVar;
	double mAILRCenterLaneProb;
	double mAILRCenterLaneWidth;
	double mAILRCenterLaneWidthVar;
	double mAILRRightLaneProb;
	double mAILRRightLaneWidth;
	double mAILRRightLaneWidthVar;
	int mAILRNumLeftLanePoints;
	double* mAILRLeftLanePoints;
	double* mAILRLeftLaneVars;
	int mAILRNumCenterLanePoints;
	double* mAILRCenterLanePoints;
	double* mAILRCenterLaneVars;
	int mAILRNumRightLanePoints;
	double* mAILRRightLanePoints;
	double* mAILRRightLaneVars;
	//***END VARIABLES TO STORE INFORMATION TO TRANSMIT TO AI***

	void ComputeBestEstimate();
	void ComputeBestEstimateForTransmit();
	double RandGaussian() {return mGaussianCache.RandomNumber();}
	double RandUniform() {return mUniformCache.RandomNumber();}
	void ReplaceGaussian() {mGaussianCache.ReplaceRandomNumber(mPPGenerator->RandGaussian()); return;}
	void ReplaceUniform() {mUniformCache.ReplaceRandomNumber(mPPGenerator->RandUniform()); return;}

public:

	//constructor / destructor
	PosteriorPoseParticleFilter(int iNumParticles, RandomNumberGenerator* iRandomNumberGenerator);
	~PosteriorPoseParticleFilter();

	//accessor functions
	bool IsInitialized() {return mIsInitialized;}
	double PosteriorPoseTime() {return mPFTime;}

	//functions for input / output
	void DisplayArbiterMessage();
	void DisplayOperationalMessage();
	void GetPosteriorPosePositionForTransmit(PosteriorPosePosition& oPosteriorPose);
	bool PredictForTransmit(double iPredictTime, VehicleOdometry* iVehicleOdometry);
	bool PredictLocalRoadForTransmit(double iPredictTime, VehicleOdometry* iVehicleOdometry);
	void PrintParticles(FILE* iParticleFile);
	void SetArbiterMessage(UnmanagedArbiterPositionMessage* oArbiterMsg);
	bool SetLocalRoadForTransmit();
	void SetLocalRoadMessage(LocalRoadModelEstimateMsg* oLocalRoadMessage);
	void SetOperationalMessage(UnmanagedOperationalMessage* oOperationalMsg);
	void SetPartitionsForTransmit();
	void SetViewerMessage(SceneEstimatorParticlePointsMsg* oViewerMessage);

	//functions for particle filtering
	void Initialize(double iInitializeTime, double* iPoseBuff, RoadGraph* iRoadGraph);
	void Predict(double iPredictTime, VehicleOdometry* iVehicleOdometry);
	void PredictLocalRoad(double iPredictTime, VehicleOdometry* iVehicleOdometry);
	void Resample();
	void ResetFilter();
	void UpdateWithAaronStopline(double iStoplineTime, int iNumStoplines, double* iAaronBuff, Sensor* iAaronSensor);
	void UpdateWithJasonLane(double iJasonTime, int iNumSegmentations, double* iJasonBuff, Sensor* iJasonSensor);
	void UpdateWithMobileyeLane(double iMobileyeTime, double* iMobileyeBuff, Sensor* iMobileyeSensor);
	void UpdateWithMobileyeLines(double iMobileyeTime, double* iMobileyeBuff, Sensor* iMobileyeSensor);
	void UpdateWithPose(double iPoseTime, double* iPoseBuff, Sensor* iPoseSensor);
	void UpdateWithLocalRoad(double iLocalRoadTime, int iNumDataRows, double* iLocalRoadPacket, int iNumPoints, double* iLocalRoadPoints);

};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //POSTERIORPOSEPARTICLEFILTER_H
