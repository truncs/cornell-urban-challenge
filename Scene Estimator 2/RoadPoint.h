#ifndef ROADPOINT_H
#define ROADPOINT_H

#include "SceneEstimatorFunctions.h"

#include <MATH.H>
#include <STDLIB.H>
#include <STRING.H>

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

#define ROADPOINT_FIELDSIZE 128

class RoadPartition;

class RoadPoint
{
	//The road point class.  Stores one waypoint or perimeterpoint on the RNDF.

private:

	//the segment, way, lane, and waypoint IDs
	char mWaypointID[ROADPOINT_FIELDSIZE];

	//the East-North coordinates of the road point
	double mEast;
	double mNorth;

	//whether this point is a stop line or not
	bool mIsStop;

	//the number of partitions this point belongs to
	int mNumMemberPartitions;
	//array of pointers to partitions that this road point belongs to
	RoadPartition** mMemberPartitions;

public:

	RoadPoint(void);
	~RoadPoint(void);

	char* WaypointID(void) {return mWaypointID;}

	double East(void) {return mEast;}
	double North(void) {return mNorth;}

	bool IsStop(void) {return mIsStop;}	
	int NumMemberPartitions(void) {return mNumMemberPartitions;}
	RoadPartition* GetMemberPartition(int iIdx) {return mMemberPartitions[iIdx];}

	double GetDistanceToPoint(double iEast, double iNorth);
	bool IsInView(double iEast, double iNorth, double iHeading, double iMaxViewAngle);

	void SetPointData(char* iWaypointID, double iEast, double iNorth, bool iIsStop);
	void SetMemberPartitions(int iNumMemberPartitions, RoadPartition** iMemberPartitions);
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //ROADPOINT_H
