#include "RadialExistenceGrid.h"

RadialExistenceGrid::RadialExistenceGrid(int iNumBins, double iMiddleBearing, double iBearingSpan)
{
	/*
	Constructor for the radial existence grid.  Allocates memory and initializes the grid.

	INPUTS:
		iNumBins - number of bins to use for the radial grid
		iMiddleBearing - the bearing of the center grid point (usually 0)
		iBearingSpan - the total angular span of the grid

	OUTPUTS:
		none.  Initializes the grid
	*/

	int i;

	//declare memory for the grid
	mNumBins = iNumBins;
	if (mNumBins < 1)
	{
		printf("Warning: attempted creating a %d-bin grid.  Defaulting to 2 bins...\n", mNumBins);
		mNumBins = 2;
	}
	mNumBinso2 = mNumBins / 2;

	mRadialBins = new double[mNumBins];
	for (i = 0; i < mNumBins; i++)
	{
		mRadialBins[i] = REG_INVALIDRANGE;
	}

	//set the middle bearing and the maximum delta-bearing
	mMiddleBearing = iMiddleBearing;
	mMaxDeltaBearing = 0.5*iBearingSpan;

	//calculate the angle occupied by each grid
	double da = iBearingSpan / ((double) mNumBins);
	//invert this and store
	mooDeltaBearing = 1.0 / da;

	return;
}

RadialExistenceGrid::~RadialExistenceGrid()
{
	/*
	Radial existence grid destructor.  Frees memory from the grid and deletes it.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	//free the memory allocated to the bins
	mNumBins = 0;
	delete [] mRadialBins;
	mRadialBins = NULL;

	return;
}

int RadialExistenceGrid::GridIndex(double iBearing)
{
	/*
	Computes the index into the radial grid for a particular bearing.
	This function is private because it is not constrained to return
	a value in the grid (i.e. it could flow outside the grid)

	INPUTS:
		iBearing - the angle of interest

	OUTPUTS:
		rIdx - the index into mRadialBins for this bearing.
		
		NOTE: rIdx MUST be bound-tested before indexing.
	*/

	int rIdx = INT_MAX;

	//center the input bearing on the grid
	double ang = UnwrapAngle(iBearing - mMiddleBearing);
	//convert angle to number of bins
	rIdx = ((int) floor(ang * mooDeltaBearing)) + mNumBinso2;

	return rIdx;
}

void RadialExistenceGrid::AddPointToGrid(double iPointX, double iPointY)
{
	/*
	Adds a point to the grid.  NOTE: the point MUST be in the same coordinate
	frame (usually sensor coordinates) as the grid itself.

	INPUTS:
		iPointX, iPointY - the point to add to the grid.

	OUTPUTS:
		none.  Adds the point to the grid, using it to keep track of minimum
			range at each point in the grid.
	*/

	//calculate bearing of the point
	double pb = atan2(iPointY, iPointX);
	//calculate the index of the point
	int bidx = GridIndex(pb);

	//check to see if the index is actually in the grid
	if (bidx >= 0 && bidx < mNumBins)
	{
		//if the point is in the grid, calculate its range
		double pr = sqrt(iPointX*iPointX + iPointY*iPointY);

		if (mRadialBins[bidx] == REG_INVALIDRANGE || mRadialBins[bidx] > pr)
		{
			//store the closer real range in the radial grid
			mRadialBins[bidx] = pr;
		}
	}

	return;
}

int RadialExistenceGrid::PointTest(double iRange, double iBearing, double iRangeTolerance)
{
	/*
	Determines the occupancy status of a test point against the radial
	existence grid.  NOTE: the test point passed in must be in the same
	coordinate frame as the existence grid.

	INPUTS:
		iRange, iBearing - range and bearing of the point to test.
			NOTE: this test point MUST be in the same coordinate frame
			as the existence grid.
		iRangeTolerance - allowed tolerance in range for the test.

	OUTPUTS:
		rTest - the occupancy status of the point.  REG_UNKNOWN, 
			REG_PASS, or REG_FAIL, depending on whether the radial
			grid gives no information, confirms the point's existence,
			or denies the point's existence.
	*/

	int rTest = REG_UNKNOWN;

	//determine the bin index of the test point
	int bidx = GridIndex(iBearing);
	if (bidx >= 0 && bidx < mNumBins)
	{
		//if the test point lies in the grid, check if the grid is defined there
		if (mRadialBins[bidx] != REG_INVALIDRANGE)
		{
			//if the grid is defined here, test the point's range

			if (mRadialBins[bidx] - iRange > iRangeTolerance)
			{
				//the test point has evidence against it
				rTest = REG_FAIL;
			}
			else if (fabs(mRadialBins[bidx] - iRange) < iRangeTolerance)
			{
				//the test point is validated against the closest range
				rTest = REG_PASS;
			}
		}
	}

	return rTest;
}
