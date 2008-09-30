#ifndef ROADPARTITION_H
#define ROADPARTITION_H

#include "MatrixIndex.h"
#include "RoadPoint.h"
#include "SceneEstimatorFunctions.h"

#include <FLOAT.H>
#include <MATH.H>
#include <STDLIB.H>

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

#define ROADPARTITION_FIELDSIZE 128

//types of partitions allowed
#define RP_LANE 1
#define RP_ZONE 2
#define RP_INTERCONNECT 3

//types of fits allowed
#define RP_LINE 11
#define RP_POLYGON 12

//types of lane boundaries
#define RP_NOBOUNDARY 100
#define RP_DOUBLEYELLOW 101
#define RP_SOLIDYELLOW 102
#define RP_SOLIDWHITE 103
#define RP_BROKENWHITE 104

//invalid lane
#define RP_INVALIDLANEWIDTH -99.0

class RoadPartition;

class RoadPartition
{
	//The road partition class.  Stores one road partition (a chunk of ground on the RNDF).

private:

	//the partition's ID
	char mPartitionID[ROADPARTITION_FIELDSIZE];

	//the partition's type (lane, etc.)
	int mPartitionType;

	//whether the partition is sparse or not
	bool mIsSparse;

	//the partition's spline fit type
	int mFitType;
	//will store the fitting parameters for this partition
	double* mFitParameters;

	//stores the lanewidth (m)
	double mLaneWidth;

	//stores the line types on the left and right
	int mLeftBoundary;
	int mRightBoundary;

	//number and array of road points in this partition
	int mNumPoints;
	RoadPoint** mPartitionPoints;

	//number and array of road points that are nearby stoplines
	int mNumNearbyStoplines;
	RoadPoint** mNearbyStoplines;

	//number and array of adjacent partitions in the same lane
	int mNumLaneAdjacentPartitions;
	RoadPartition** mLaneAdjacentPartitions;

	//number and array of adjacent partitions in the left lane
	int mNumLeftLaneAdjacentPartitions;
	RoadPartition** mLeftLaneAdjacentPartitions;

	//number and array of adjacent partitions in the right lane
	int mNumRightLaneAdjacentPartitions;
	RoadPartition** mRightLaneAdjacentPartitions;

	//number and array of nearby partitions
	int mNumNearbyPartitions;
	RoadPartition** mNearbyPartitions;

public:

	RoadPartition(void);
	~RoadPartition(void);

	bool IsSparse(void) {return mIsSparse;}
	int FitType(void) {return mFitType;}
	double FitParameter(int iIdx) {return mFitParameters[iIdx];}
	double LaneWidth(void) {return mLaneWidth;}
	int LeftBoundary(void) {return mLeftBoundary;}
	char* PartitionID(void) {return mPartitionID;}
	int PartitionType(void) {return mPartitionType;}
	int RightBoundary(void) {return mRightBoundary;}

	int NumNearbyPartitions(void) {return mNumNearbyPartitions;}
	RoadPartition* NearbyPartition(int iIdx) {return mNearbyPartitions[iIdx];}
	int NumNearbyStoplines(void) {return mNumNearbyStoplines;}
	RoadPoint* NearbyStopline(int iIdx) {return mNearbyStoplines[iIdx];}

	int NumPoints(void) {return mNumPoints;}
	RoadPoint* GetPoint(int iIdx) {return mPartitionPoints[iIdx];}

	void SetPartitionData(char* iPartitionID, int iPartitionType, bool iIsSparse, int iFitType, double* iFitParameters, 
		double iLaneWidth, int iLeftBoundary, int iRightBoundary, int iNumPoints, RoadPoint** iPartitionPoints);
	void SetPartitionConnections(int iNumNearbyStoplines, RoadPoint** iNearbyStoplines, int iNumLaneAdjacentPartitions, 
		RoadPartition** iLaneAdjacentPartitions, int iNumLeftLaneAdjacentPartitions, RoadPartition** iLeftLaneAdjacentPartitions, 
		int iNumRightLaneAdjacentPartitions, RoadPartition** iRightLaneAdjacentPartitions, int iNumNearbyPartitions, 
		RoadPartition** iNearbyPartitions);

	double DistanceToPartition(double iEast, double iNorth);
	bool IsAdjacent(double iEast, double iNorth);
	bool IsInSameDirection(double iEast, double iNorth, double iHeading);
	bool IsOnPartition(double iEast, double iNorth);
	void LaneOffsets(double* oLaneOffset, double* oHeadingOffset, double iEast, double iNorth, double iHeading);	
	RoadPartition* LeftLanePartition(double iEast, double iNorth);
	RoadPartition* NextPartitionInLane(void);
	//void PartitionPolygon(int& oNumPoints, double*& oPolygonPoints);
	RoadPartition* PreviousPartitionInLane(void);
	RoadPartition* RightLanePartition(double iEast, double iNorth);
	double RoadHeading(double iEast, double iNorth);
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //ROADPARTITION_H
