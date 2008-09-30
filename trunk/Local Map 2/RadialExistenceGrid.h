#ifndef RADIALEXISTENCEGRID_H
#define RADIALEXISTENCEGRID_H

#include "LocalMapFunctions.h"

#include <FLOAT.H>
#include <STDIO.H>
#include <STDLIB.H>

//constants for point status returns
//radial grid can't determine a point's status
#define REG_UNKNOWN -1
//radial grid denies the existence of this point
#define REG_FAIL 0
//radial grid confirms the existence of this point
#define REG_PASS 1

//the default invalid range value
#define REG_INVALIDRANGE -1.0

class RadialExistenceGrid
{
	//A radial existence grid for testing the existence of obstacles.

private:

	//number of bins in the radial grid
	int mNumBins;
	//number of bins divided by 2
	int mNumBinso2;
	//the actual radial bins
	double* mRadialBins;
	//bearing of the middle grid point
	double mMiddleBearing;
	//the maximum difference between 
	double mMaxDeltaBearing;
	//1/(the angle occupied by each grid point)
	double mooDeltaBearing;

	int GridIndex(double iBearing);

public:

	RadialExistenceGrid(int iNumBins, double iMiddleBearing, double iBearingSpan);
	~RadialExistenceGrid();

	void AddPointToGrid(double iPointX, double iPointY);
	int PointTest(double iRange, double iBearing, double iRangeTolerance);
};

#endif //RADIALEXISTENCEGRID_H
