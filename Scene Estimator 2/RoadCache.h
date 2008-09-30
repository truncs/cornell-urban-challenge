#ifndef ROADCACHE_H
#define ROADCACHE_H

#include "RoadPartition.h"
#include "RoadPoint.h"
#include "SceneEstimatorFunctions.h"
#include "StringIntLUT.H"

#include <MATH.H>
#include <STDIO.H>
#include <STRING.H>
#include <vector>

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

using namespace std;

typedef vector<RoadPartition*> RoadCacheCell;

//the amount of space to pad around the road cache (m)
#define RC_PADSIZE 1000.0
//maximum length of a file name opened for a road cache
#define RC_NAMELENGTH 128

class RoadCache
{
	//A road cache for quickly computing the closest partition to any point

private:

	//the name of the road cache file storing this road cache
	char mRoadCacheName[RC_NAMELENGTH];
	//number of rows in the road cache grid (east axis)
	int mNumRows;
	//number of columns in the road cache grid (north axis)
	int mNumCols;
	//the index of the center row
	int mCenterRow;
	//the index of the center column
	int mCenterCol;
	//the CRC for the road graph file
	unsigned short mCRC;
	//the road cache grid
	vector<RoadCacheCell> mRoadCacheGrid;
	//whether the road cache is valid (successfully loaded) or not
	bool mIsValid;

	//grid center point in the east axis (m)
	double mEastCenter;
	//grid center point in the north axis (m)
	double mNorthCenter;
	//total span in the east axis (m)
	double mEastSpan;
	//total span in the north axis (m)
	double mNorthSpan;
	//span of each cache cell in the east axis (m)
	double mCellLengthEast;
	//span of each cache cell in the north axis (m)
	double mCellLengthNorth;

	bool IsInRoadCache(double iEast, double iNorth);
	int RoadCacheCellGridIndex(int r, int c) {return (c*mNumRows + r);}
	void RoadCacheCellEastNorth(double& e, double& n, int r, int c)
	{
		/*
		Computes the east-north location of a grid cell by index

		INPUTS:
			e, n - populated with the east-north location on exit
			r, c - the row-column index of the cell

		OUTPUTS:
			e, n - the east-north location of the center of the grid cell
		*/

		e = mCellLengthEast*((double) (c - mCenterCol)) + mEastCenter;
		n = mCellLengthNorth*((double) (mCenterRow - r)) + mNorthCenter;
		return;
	}
	void RoadCacheCellRowColumn(int& r, int& c, double e, double n)
	{
		/*
		Computes the row-column index of a particular point

		INPUTS:
			r, c - populated with the cell row and column indices on exit
			e, n - the desired east-north location to test

		OUTPUTS:
			r, c - the cell row and column indices
		*/

		c = ((int) Round((e-mEastCenter) / mCellLengthEast)) + mCenterCol;
		r = ((int) Round((mNorthCenter-n) / mCellLengthNorth)) + mCenterRow;
		return;
	}

public:

	RoadCache();
	//NOTE: no destructor necessary- no special memory allocated

	bool IsValid() {return mIsValid;}
	bool LoadRoadCacheFromFile(char* iRoadGraphName, unsigned short iCRC, int iNumPartitions, RoadPartition* iRoadPartitions);
	bool LoadRoadCacheFromScratch(int iNumRows, int iNumCols, char* iRoadGraphName, unsigned short iCRC, 
		int iNumPoints, RoadPoint* iRoadPoints, int iNumPartitions, RoadPartition* iRoadPartitions);
	void ResetRoadCache();
	RoadPartition* ClosestPartition(double iEast, double iNorth);
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //ROADCACHE_H
