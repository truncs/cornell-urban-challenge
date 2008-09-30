#ifndef TARGETOCCUPANCYGRID_H
#define TARGETOCCUPANCYGRID_H

#include "LocalMapFunctions.h"
#include "MatrixIndex.h"

#include <MATH.H>
#include <MEMORY.H>
#include <STDIO.H>
#include <STDLIB.H>
#include <vector>

using namespace std;

typedef pair<double, double> DoublePair;

//constant used to indicate a cell is empty
#define TOG_EMPTY 0x00
//constant used to indicate a cell is full
#define TOG_OCCUPIED 0x01
//constant used to indicate a cell is unknown
#define TOG_UNKNOWN 0x02

//amount of padding around the edge of the occupancy grid (m)
#define TOG_PADSIZE 2.0

//need this predefine because TargetOccupancyGrid has a member that
//takes a TargetOccupancyGrid as input
class TargetOccupancyGrid;

class TargetOccupancyGrid
{
	//An occupancy grid class for storing a target's cluster
	//points for comparison with other targets.

private:

	//number of rows in the occupancy grid
	int mNumRows;
	//number of columns in the occupancy grid
	int mNumCols;
	//the index of the center row
	int mCenterRow;
	//the index of the center column
	int mCenterCol;
	//the occupancy grid
	int* mOccupancyGrid;
	//the dilated occupancy grid
	int* mDilatedOccupancyGrid;
	//the number of points in the grid
	int mNumPoints;
	//the vector of points in the grid
	vector<DoublePair> mOccupancyPoints;
	//whether the occupancy grid is valid or not
	bool mIsValid;

	//grid center point in the x axis (m)
	double mCenterX;
	//grid center point in the y axis (m)
	double mCenterY;
	//total span in the x axis (m)
	double mSpanX;
	//total span in the y axis (m)
	double mSpanY;
	//span of each cell in the x axis (m)
	double mCellLengthX;
	//span of each cell in the y axis (m)
	double mCellLengthY;

	bool IsInOccupancyGrid(double iX, double iY);
	int OccupancyGridCellIndex(int r, int c) {return (c*mNumRows + r);}
	void OccupancyGridCellXY(double& x, double& y, int r, int c)
	{
		/*
		Computes the x-y location of a grid cell by index

		INPUTS:
			x, y - populated with the x-y location on exit
			r, c - the row-column index of the cell

		OUTPUTS:
			x, y - the x-y location of the occupancy grid cell
		*/

		x = mCellLengthX*((double) (c - mCenterCol)) + mCenterX;
		y = mCellLengthY*((double) (mCenterRow - r)) + mCenterY;
		return;
	}
	void OccupancyGridCellRowColumn(int& r, int& c, double x, double y)
	{
		/*
		Computes the row-column index of a particular point

		INPUTS:
			r, c - populated with the cell row and column indices on exit
			x, y - the desired x-y location to test

		OUTPUTS:
			r, c - the cell row and column indices
		*/

		c = ((int) Round((x-mCenterX) / mCellLengthX)) + mCenterCol;
		r = ((int) Round((mCenterY-y) / mCellLengthY)) + mCenterRow;
		return;
	}

public:

	TargetOccupancyGrid(int iNumRows, int iNumCols);
	~TargetOccupancyGrid();

	double CenterX() {return mCenterX;}
	double CenterY() {return mCenterY;}
	int DilatedOccupancy(double iX, double iY);
	void DilateOccupancyGrid(double iDilateRadius);
	bool IsValid() {return mIsValid;}
	double OverlapPercentage(TargetOccupancyGrid* iComparisonGrid);
	void ResetOccupancyGrid();
	void SetOccupancyGrid(int iNumPoints, double* iPoints, double iAnchorX, 
		double iAnchorY, double iOrientation);
};

#endif //OCCUPANCYGRID_H
