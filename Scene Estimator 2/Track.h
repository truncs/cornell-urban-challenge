#ifndef TRACK_H
#define TRACK_H

#include "Cluster.h"
#include "MatrixIndex.h"
#include "PosteriorPosePosition.h"
#include "RoadGraph.h"
#include "RoadPartition.h"
#include "RoadPoint.h"
#include "SceneEstimatorConstants.h"
#include "SceneEstimatorFunctions.h"
#include "Target.h"
#include "VehicleOdometry.h"

#include <FLOAT.H>
#include <MATH.H>
#include <STDIO.H>
#include <STDLIB.H>

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

//track invalid ID
#define T_INVALIDID -1

//track status flags
#define T_STATUSACTIVE 1
#define T_STATUSFULLOCCLUDED 2
#define T_STATUSPARTOCCLUDED 3
#define T_STATUSDELETED -99

//track occlusion types: no occlusion, partial occlusion, full occlusion
#define T_OCCLUSIONNONE 0
#define T_OCCLUSIONPART 10
#define T_OCCLUSIONFULL 20

//number of states in the track
#define T_NUMSTATES 5
//number of position measurements made for the track
#define T_NUMTPMEAS 3
//number of states in the auxiliary velocity estimator
#define T_NUMAVESTATES 4
//number of measurements for the auxiliary velocity estimator
#define T_NUMAVEMEAS 2

class Track;

class Track
{
	//The track class.  Stores one persistent obstacle track.

private:

	//TRACK FLAGS AND DATA
	//persistent and unique track ID
	int mID;
	//track status flag (active, occluded, deleted)
	int mStatusFlag;
	//time since the track was last updated (NOTE: this gets reset to 0 every time the track is occluded)
	double mTimeSinceLastUpdate;
	//absolute time since the track was last updated (NOTE: this doesn't get reset when the track is occluded)
	double mAbsoluteTimeSinceLastUpdate;
	//time since the target has been created
	double mTimeSinceCreation;
	//number of measurements the track has received
	int mNumMeasurements;
	//closest partition in the RNDF
	RoadPartition* mClosestPartition;
	//whether the track has been initialized with data
	bool mIsInitialized;

	//TRACK STATES
	//XY location of the track anchor point, in ego-vehicle coordinates
	double mX;
	double mY;
	//orientation angle for transforming tracks from object storage to vehicle frame
	double mOrientation;
	//ground speed (m/s)
	double mSpeed;
	//track heading relative to ego vehicle heading
	double mHeading;

	//TRACK STATE COVARIANCE MATRIX
	double mCovariance[T_NUMSTATES*T_NUMSTATES];

	//TRACK PROBABILITIES
	//probability that the track is a car (based on object size))
	double mCarProbability;
	//probability that the track is currently stopped (not moving)
	double mStoppedProbability;

	//TRACK POINTS
	//number of points stored for the class
	int mNumTrackPoints;
	//the track points stored for the class, in object storage frame
	double* mTrackPoints;

	//THE TRACK POSITION MEASUREMENT
	//flag whether the measurement is accurate at the present time
	bool mtpmIsCurrent;
	//counter-clockwise and clockwise occlusion bearings (rad.)
	double mtpmBCCW;
	double mtpmBCW;
	//closest range
	double mtpmRMIN;
	//measurement variance
	double mtpmR[T_NUMTPMEAS*T_NUMTPMEAS];
	//measurement-state covariance
	double mtpmPHt[T_NUMSTATES*T_NUMTPMEAS];

	//THE TRACK AUXILIARY VELOCITY ESTIMATOR
	//state of the auxiliary velocity estimator (X, Y, S, H)
	double maveState[T_NUMAVESTATES];
	//covariance of the auxiliary velocity estimator
	double maveCovariance[T_NUMAVESTATES*T_NUMAVESTATES];

public:

	//elements for implementing a linked list of tracks
	Track* PrevTrack;
	Track* NextTrack;

	//accessor methods
	double AbsoluteTimeSinceLastUpdate() {return mAbsoluteTimeSinceLastUpdate;}
	double CarProbability() {return mCarProbability;}
	RoadPartition* ClosestPartition() {return mClosestPartition;}
	double Covariance(int r, int c) {return mCovariance[midx(r, c, T_NUMSTATES)];}
	double Heading() {return mHeading;}
	int ID() {return mID;}
	bool IsInitialized() {return mIsInitialized;}
	int NumMeasurements() {return mNumMeasurements;}
	int NumTrackPoints() {return mNumTrackPoints;}
	double Orientation() {return mOrientation;}
	double Speed() {return mSpeed;}
	int StatusFlag() {return mStatusFlag;}
	double StoppedProbability() {return mStoppedProbability;}
	double TimeSinceCreation() {return mTimeSinceCreation;}
	double TimeSinceLastUpdate() {return mTimeSinceLastUpdate;}
	double TrackPoints(int r, int c) {return mTrackPoints[midx(r, c, mNumTrackPoints)];}
	double X() {return mX;}
	double Y() {return mY;}

	//accessor methods for track position measurement
	bool tpmIsCurrent() {return mtpmIsCurrent;}
	double tpmBCCW() {return mtpmBCCW;}
	double tpmBCW() {return mtpmBCW;}
	double tpmRMIN() {return mtpmRMIN;}
	double tpmR(int r, int c) {return mtpmR[midx(r, c, T_NUMTPMEAS)];}
	double tpmPHt(int r, int c) {return mtpmPHt[midx(r, c, T_NUMSTATES)];}

	//accessor methods for auxiliary velocity estimator
	double aveX() {return maveState[0];}
	double aveY() {return maveState[1];}
	double aveSpeed() {return maveState[2];}
	double aveHeading() {return maveState[3];}
	double aveCovariance(int r, int c) {return maveCovariance[midx(r, c, T_NUMAVESTATES)];}

	//constructors and destructor
	Track(int iID);
	Track(Track* iTrackToCopy);
	~Track();

	//track member functions
	bool CanBeAssociated(Target* iTarget);
	bool HeadingIsValid();
	bool IsNearStopline(PosteriorPosePosition* iPosteriorPosePosition);
	bool IsOccluded();
	double Likelihood(double& oPMDist, double& oSHMDist, Target* iTarget);
	void MaintainTrack();
	void MarkForDeletion() {mStatusFlag = T_STATUSDELETED; return;}
	void MarkForOcclusion(int iOcclusionType);
	void Predict(double iPredictDt, VehicleOdometry* iVehicleOdometry, 
		PosteriorPosePosition* iPosteriorPosePosition, RoadGraph* iRoadGraph);
	void SetTrackPositionMeasurement();
	bool SpeedIsValid();
	Cluster TrackPointsCluster();
	void Update(Target* iTarget, PosteriorPosePosition* iPosteriorPosePosition, RoadGraph* iRoadGraph);
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //TRACK_H
