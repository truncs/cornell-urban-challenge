#include "TargetOccupancyGrid.h"

TargetOccupancyGrid::TargetOccupancyGrid(int iNumRows, int iNumCols)
{
	/*
	Default constructor for the occupancy grid.  Initializes the grid
	and sets everything to invalid values.

	INPUTS:
		iNumRows, iNumCols - number of rows and columns in the occupancy 
			grid.

	OUTPUTS:
		none.
	*/

	//initialize with an empty grid
	mNumRows = iNumRows;
	if (mNumRows < 1)
	{
		printf("Warning: occupancy grid initialized with %d rows, defaulting to 1.\n", mNumRows);
		mNumRows = 1;
	}
	mNumCols = iNumCols;
	if (mNumCols < 1)
	{
		printf("Warning: occupancy grid initialized with %d columns, defaulting to 1.\n", mNumCols);
		mNumCols = 1;
	}
	mCenterRow = mNumRows / 2;
	mCenterCol = mNumCols / 2;
	mOccupancyGrid = new int[mNumRows*mNumCols];
	mDilatedOccupancyGrid = new int[mNumRows*mNumCols];
	ResetOccupancyGrid();
	mNumPoints = 0;
	mIsValid = false;

	mCenterX = 0.0;
	mCenterY = 0.0;
	mSpanX = 0.0;
	mSpanY = 0.0;
	mCellLengthX = 0.0;
	mCellLengthY = 0.0;

	return;
}

TargetOccupancyGrid::~TargetOccupancyGrid()
{
	/*
	Occupancy grid destructor.  Frees memory allocated and prepares
	the occupancy grid for deletion.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	//delete memory allocated in the occupancy grid
	delete [] mOccupancyGrid;
	mOccupancyGrid = NULL;
	delete [] mDilatedOccupancyGrid;
	mDilatedOccupancyGrid = NULL;

	//mark the grid as invalid
	mIsValid = false;

	return;
}

void TargetOccupancyGrid::ResetOccupancyGrid()
{
	/*
	Resets the occupancy grid to empty

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	mIsValid = false;

	if (mNumRows*mNumCols > 0)
	{
		//reset the occupancy grid to empty
		memset(mOccupancyGrid, TOG_EMPTY, mNumRows*mNumCols*sizeof(int));
		memset(mDilatedOccupancyGrid, TOG_EMPTY, mNumRows*mNumCols*sizeof(int));
		//erase the ego-vehicle points defining the grid
		mNumPoints = 0;
		mOccupancyPoints.clear();
	}

	return;
}

void TargetOccupancyGrid::SetOccupancyGrid(int iNumPoints, double* iPoints, 
	double iAnchorX, double iAnchorY, double iOrientation)
{
	/*
	Sets the occupancy grid for a set of target points.

	INPUTS:
		iNumPoints - number of cluster points for the target
		iPoints - the actual target points, in object storage frame
		iAnchorX, iAnchorY - the target's anchor point, in ego-vehicle
			coordinates
		iOrientation - the target's orientation angle

	OUTPUTS:
		none.
	*/

	int i;

	mIsValid = false;
	ResetOccupancyGrid();

	if (iNumPoints < 1)
	{
		//can't create an occupancy grid without points
		return;
	}

	double cosOrient = cos(iOrientation);
	double sinOrient = sin(iOrientation);

	//store the points in ego-vehicle coordinates
	mNumPoints = iNumPoints;
	mOccupancyPoints.resize(iNumPoints);
	double wt = 1.0 / ((double) iNumPoints);

	//these will be built up as the points are transformed to ego-vehicle
	mCenterX = 0.0;
	mCenterY = 0.0;
	double mMinX = DBL_MAX;
	double mMaxX = -DBL_MAX;
	double mMinY = DBL_MAX;
	double mMaxY = -DBL_MAX;

	//convert all points to ego-vehicle coordinates
	for (i = 0; i < iNumPoints; i++)
	{
		//extract each target point in object storage frme
		double osx = iPoints[midx(i, 0, iNumPoints)];
		double osy = iPoints[midx(i, 1, iNumPoints)];
		//convert to ego vehicle coordinates
		double evx;
		double evy;
		ObjectToEgoVehicle(evx, evy, osx, osy, cosOrient, sinOrient, iAnchorX, iAnchorY);

		//build up the center of mass as the grid center
		mCenterX += wt*evx;
		mCenterY += wt*evy;

		//keep track of the bounding box
		if (evx < mMinX)
		{
			mMinX = evx;
		}
		if (evx > mMaxX)
		{
			mMaxX = evx;
		}
		if (evy < mMinY)
		{
			mMinY = evy;
		}
		if (evy > mMaxY)
		{
			mMaxY = evy;
		}

		//store the ego-vehicle transformations for later
		mOccupancyPoints[i] = make_pair(evx, evy);
	}

	//calculate the span of the occupancy grid
	mSpanX = fabs(mMaxX - mMinX) + 2.0*TOG_PADSIZE;
	mSpanY = fabs(mMaxY - mMinY) + 2.0*TOG_PADSIZE;
	//calculate the cell lengths of the occupancy grid
	mCellLengthX = mSpanX / ((double) mNumCols);
	mCellLengthY = mSpanY / ((double) mNumRows);

	//populate the occupancy grid
	int nc = mNumRows*mNumCols;
	for (i = 0; i < iNumPoints; i++)
	{
		//pull each point
		DoublePair curpt = mOccupancyPoints[i];
		//and add it to the occupancy grid
		int r;
		int c;
		OccupancyGridCellRowColumn(r, c, curpt.first, curpt.second);
		//mark this cell as occupied
		if (r >= 0 && r < mNumRows && c >= 0 && c < mNumCols)
		{
			mOccupancyGrid[OccupancyGridCellIndex(r, c)] = TOG_OCCUPIED;
		}
	}

	//when done populating the grid, it is valid again
	mIsValid = true;

	return;
}

void TargetOccupancyGrid::DilateOccupancyGrid(double iDilateRadius)
{
	/*
	Dilates the current occupancy grid by a specified radius

	INPUTS:
		iDilateRadius - the radius of the dilation (m).  Dilation
			is rounded to the nearest cell in each direction.

	OUTPUTS:
		none.
	*/

	int h;
	int i;
	int j;
	int k;

	if (mNumRows*mNumCols == 0)
	{
		//can't dilate an empty grid
		return;
	}

	//reset the dilated grid to empty
	memset(mDilatedOccupancyGrid, TOG_EMPTY, mNumRows*mNumCols*sizeof(int));

	//calculate the number of rows and columns to dilate
	int dr = (int) (Round(iDilateRadius / mCellLengthY));
	int dc = (int) (Round(iDilateRadius / mCellLengthX));

	for (h = 0; h < mNumRows; h++)
	{
		for (i = 0; i < mNumCols; i++)
		{
			//for each occupied cell, mark its neighbors to occupied
			if (mOccupancyGrid[OccupancyGridCellIndex(h, i)] == TOG_EMPTY)
			{
				//cell is not occupied
				continue;
			}

			//if the cell is occupied, mark all neighbors as occupied
			for (j = h-dr; j <= h+dr; j++)
			{
				for (k = i-dc; k <= i+dc; k++)
				{
					if (j >= 0 && j < mNumRows && k >= 0 && k < mNumCols)
					{
						//if this cell is actually in the occupancy grid, set it
						mDilatedOccupancyGrid[OccupancyGridCellIndex(j, k)] = TOG_OCCUPIED;
					}
				}
			}
		}
	}

	return;
}

bool TargetOccupancyGrid::IsInOccupancyGrid(double iX, double iY)
{
	/*
	Quickly determines whether the test point is inside the occupancy grid, 
	i.e. whether the grid can be used to evaluate occupancy

	INPUTS:
		iX, iY - point to test

	OUTPUTS:
		rIsInGrid - true if the test point is covered by the grid, false
			otherwise.
	*/

	bool rIsInGrid = false;

	if (mIsValid == false)
	{
		//can't do anything if grid is invalid
		return rIsInGrid;
	}

	//extract the corners of the grid
	double ulx;
	double uly;
	//upper left corner
	OccupancyGridCellXY(ulx, uly, 0, 0);
	double lrx;
	double lry;
	//lower right corner
	OccupancyGridCellXY(lrx, lry, mNumRows-1, mNumCols-1);

	//extract the minimum and maximum bounds of the grid
	double maxX = lrx + 0.5*mCellLengthX;
	double minX = ulx - 0.5*mCellLengthX;
	double maxY = uly + 0.5*mCellLengthY;
	double minY = lry - 0.5*mCellLengthY;

	//test whether the point is inside the grid
	if (iX >= minX && iX <= maxX && iY >= minY && iY <= maxY)
	{
		rIsInGrid = true;
	}

	return rIsInGrid;
}

int TargetOccupancyGrid::DilatedOccupancy(double iX, double iY)
{
	/*
	Evaluates the occupancy status of the dilated occupancy grid
	at a particular x, y location

	INPUTS:
		iX, iY - the location to test

	OUTPUTS:
		rOccupancy - see TargetOccupancyGrid.h for occupancy values
	*/

	int rOccupancy = TOG_UNKNOWN;

	if (IsInOccupancyGrid(iX, iY) == true)
	{
		//if the point is in the occupancy grid, evaluate occupancy
		//at the cell containing this point
		int r;
		int c;
		OccupancyGridCellRowColumn(r, c, iX, iY);

		if (r >= 0 && r < mNumRows && c >= 0 && c < mNumCols)
		{
			rOccupancy = mDilatedOccupancyGrid[OccupancyGridCellIndex(r, c)];
		}
	}

	return rOccupancy;
}

double TargetOccupancyGrid::OverlapPercentage(TargetOccupancyGrid* iComparisonGrid)
{
	/*
	Compares this occupancy grid with a grid generated by another target, 
	returning the percentage of points in the comparison grid that are also
	occupied in this grid

	INPUTS:
		iComparisonGrid - the grid that will be compared with the points stored
			here.

	OUTPUTS:
		rOverlap - percentage of points in this grid that lie in occupied cells
			in the comparison grid
	*/

	double rOverlap = 0.0;

	int i;
	int no = 0;
	for (i = 0; i < mNumPoints; i++)
	{
		DoublePair curpt = mOccupancyPoints[i];

		//for each point making up this occupancy grid, check if it is also
		//occupied on the comparison occupancy grid
		if (iComparisonGrid->DilatedOccupancy(curpt.first, curpt.second) == TOG_OCCUPIED)
		{
			//ith test point is also occupied in the comparison grid
			no++;
		}
	}

	if (mNumPoints > 0)
	{
		//return the fraction of points occupied in the comparison grid
		rOverlap = ((double) no) / ((double) mNumPoints);
	}

	return rOverlap;
}
