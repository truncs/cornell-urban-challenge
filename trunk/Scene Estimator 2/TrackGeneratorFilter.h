#ifndef TRACKGENERATORFILTER_H
#define TRACKGENERATORFILTER_H

#include "IDGenerator.h"
#include "MatrixIndex.h"
#include "PosteriorPosePosition.h"
#include "RandomNumberCache.h"
#include "RandomNumberGenerator.h"
#include "RoadGraph.h"
#include "SceneEstimatorFunctions.h"
#include "Sensor.h"
#include "Target.h"
#include "Track.h"
#include "VehicleOdometry.h"

//include for interface to AI and Operational
#include "sceneEstimatorInterface/sceneEstimatorInterface.h"

#include <FLOAT.H>
#include <MATH.H>
#include <MEMORY.H>
#include <SEARCH.H>
#include <STDLIB.H>
#include <STRING.H>

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

//default lengths of string buffers used in printing tracks
#define TG_LINESIZE 1024
#define TG_FIELDSIZE 128

class TrackGeneratorFilter
{
	//The track generator class.  Implements the track generator.

private:

	//the current TrackGenerator timestamp
	double mTrackGeneratorTime;
	//whether the TrackGenerator has been initialized
	bool mIsInitialized;
	//pointer to the road graph structure used in scene estimator
	RoadGraph* mRoadGraph;
	//pointer to the random number generator used to draw random numbers
	RandomNumberGenerator* mTGGenerator;
	//a cache of Gaussian random numbers
	RandomNumberCache mGaussianCache;

	//UNUSED OBSTACLE DATA:
	//number of loose obstacle points
	int mNumLoosePoints;
	//array of cluster memberships- for each point, which cluster it belongs to
	int* mLooseClusterIDs;
	//array of loose point heights (see CLUSTER CONSTANTS for values)
	int* mLoosePointHeights;
	//array of loose (unused) obstacle points
	//(each row is an x, y pair for a single point, in ego-vehicle coordinates)
	double* mLoosePoints;

	//TRACKED OBSTACLE DATA:
	//the unique ID generator for the tracks
	IDGenerator mIDGenerator;
	//the number of tracks stored in the generator
	int mNumTracks;
	//the first track in the list of tracks stored in the generator
	Track* mFirstTrack;

	//TRANSMIT VARIABLES
	//the timestamp of the data being sent to AI
	double mAITime;
	//the tracks being sent to AI
	Track* mAIFirstTrack;
	//the unused obstacle data being sent to AI
	int mAINumLoosePoints;
	int* mAILooseClusterIDs;
	int* mAILoosePointHeights;
	double* mAILoosePoints;

	void AddTrack(Track* iNewTrack);
	double BirthLikelihood(Target* iTarget);
	double RandGaussian() {return mGaussianCache.RandomNumber();}
	void RemoveAllTracks();
	void RemoveTrack(Track* iOldTrack);
	void ReplaceGaussian() {mGaussianCache.ReplaceRandomNumber(mTGGenerator->RandGaussian()); return;}

public:

	TrackGeneratorFilter(RandomNumberGenerator* iRandomNumberGenerator, RoadGraph* iRoadGraph = NULL);
	~TrackGeneratorFilter();

	bool IsInitialized() {return mIsInitialized;}
	int NumTracks() {return mNumTracks;}
	double TrackGeneratorTime() {return mTrackGeneratorTime;}

	//input / output functions
	void GenerateTrackedClusterMessage(SceneEstimatorTrackedClusterMsg* oTrackedClusterMsg, PosteriorPosePosition* iPosteriorPose);
	void GenerateUntrackedClusterMessage(SceneEstimatorUntrackedClusterMsg* oUntrackedClusterMsg);
	bool PredictForTransmit(double iPredictTime, VehicleOdometry* iVehicleOdometry, 
		PosteriorPosePosition* iPosteriorPosePosition);
	void PrintTracks(FILE* iTrackFile, PosteriorPosePosition* iPosteriorPosePosition);
	void PrintLoosePoints(FILE* iLoosePointsFile, PosteriorPosePosition* iPosteriorPosePosition);

	//filtering functions
	void Initialize(double iInitialTime);
	void MaintainTracks(Sensor* iFrontSensor, Sensor* iBackSensor);
	void Predict(double iPredictTime, VehicleOdometry* iVehicleOdometry, 
		PosteriorPosePosition* iPosteriorPosePosition);
	void UpdateWithLocalMapPoints(double iUpdateTime, int iNumPoints, double* iLocalMapPacket);
	void UpdateWithLocalMapTargets(double iUpdateTime, int iNumTargets, double* iLocalMapTargets, 
		double* iLocalMapCovariances, int iNumPoints, double* iLocalMapPoints, 
		PosteriorPosePosition* iPosteriorPosePosition);
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //TRACKGENERATORFILTER_H
