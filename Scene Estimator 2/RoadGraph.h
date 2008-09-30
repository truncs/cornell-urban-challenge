#ifndef ROADGRAPH_H
#define ROADGRAPH_H

#include "RoadCache.h"
#include "RoadPartition.h"
#include "RoadPoint.h"
#include "SceneEstimatorConstants.h"
#include "SceneEstimatorFunctions.h"
#include "StringIntLUT.h"
#include "time/timing.h"

#include <FLOAT.H>
#include <MATH.H>
#include <STDIO.H>
#include <STDLIB.H>
#include <STRING.H>

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

//size of a road graph string buffer
#define ROADGRAPH_FIELDSIZE 128

class RoadGraph
{
	//The road graph class.  Reads a road graph file (*.rgp) into a road
	//graph structure for finding locations in an RNDF quickly.

private:

	//the name of the road graph file
	char mRoadGraphFileName[ROADGRAPH_FIELDSIZE];
	//the name of the RNDF
	char mRNDFName[ROADGRAPH_FIELDSIZE];
	//the RNDF creation date
	char mRNDFCreationDate[ROADGRAPH_FIELDSIZE];
	//the road graph CRC
	unsigned short mCRC;

	//latitude and longitude origin of the planar projection used (radians)
	double mLatOrigin;
	double mLonOrigin;

	//number of road points in the road graph
	int mNumPoints;
	//number of road partitions in the road graph
	int mNumPartitions;

	//whether the road graph is valid or not
	bool mIsValid;

	//array of all the waypoints / perimeterpoints in the RNDF
	RoadPoint* mRoadPoints;
	//array of all the road edges in the RNDF
	RoadPartition* mRoadPartitions;

	//the road cache, stored for computing fast distances to partitions
	RoadCache mRoadCache;

public:

	RoadGraph(char *iRoadGraphFileName, int iNumCacheRows, int iNumCacheCols);
	~RoadGraph(void);

	bool IsValid(void) {return mIsValid;}
	double LatOrigin(void) {return mLatOrigin;}
	double LonOrigin(void) {return mLonOrigin;}
	int NumPartitions(void) {return mNumPartitions;}

	RoadPartition* ClosestPartition(double iEast, double iNorth, RoadPartition* iBasePartition = NULL, double iOffGraphDistance = DBL_MAX);
	RoadPoint* ClosestUpcomingStopline(double iEast, double iNorth, double iHeading, double iMaxAngle, 
		bool iUseDirectionality, RoadPartition* iBasePartition);
	bool LocalRoadRepresentation(double* oCurvature, double* oCurvatureVar, 
		double iEast, double iNorth, double iHeading, RoadPartition* iClosestPartition);
	void TestRoadGraph();
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //ROADGRAPH_H
