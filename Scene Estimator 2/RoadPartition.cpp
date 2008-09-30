#include "RoadPartition.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

RoadPartition::RoadPartition(void)
{
	/*
	Default constructor for a road partition.  Creates an empty road edge

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	//initialize variables
	mPartitionID[0] = NULL;
	mPartitionType = RP_LANE;

	mIsSparse = false;

	mFitType = RP_LINE;
	mFitParameters = NULL;

	mLeftBoundary = RP_NOBOUNDARY;
	mRightBoundary = RP_NOBOUNDARY;

	mNumPoints = 0;
	mPartitionPoints = NULL;

	mNumLaneAdjacentPartitions = 0;
	mLaneAdjacentPartitions = NULL;

	mNumLeftLaneAdjacentPartitions = 0;
	mLeftLaneAdjacentPartitions = NULL;

	mNumRightLaneAdjacentPartitions = 0;
	mRightLaneAdjacentPartitions = NULL;

	//number and array of nearby partitions
	mNumNearbyPartitions = 0;
	mNearbyPartitions = NULL;

	return;
}

RoadPartition::~RoadPartition(void)
{
	/*
	Default destructor for a road partition.  Frees memory allocated
	and disassociates the edge from any road points.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	//frees all the memory allocated within the partition
	delete [] mFitParameters;
	delete [] mPartitionPoints;
	delete [] mNearbyStoplines;
	delete [] mLaneAdjacentPartitions;
	delete [] mLeftLaneAdjacentPartitions;
	delete [] mRightLaneAdjacentPartitions;
	delete [] mNearbyPartitions;

	return;
}

void RoadPartition::SetPartitionData(char* iPartitionID, int iPartitionType, bool iIsSparse, int iFitType, double* iFitParameters, 
	double iLaneWidth, int iLeftBoundary, int iRightBoundary, int iNumPoints, RoadPoint** iPartitionPoints)
{
	/*
	Stores input road partition data in this instance of the class

	INPUTS:
		iPartitionID - partition's string ID
		iPartitionType - type of partition (see RoadPartition.h for values)
		iIsSparse - true if the partition is sparse, false otherwise
		iFitType - type of spline interpolation (see RoadPartition.h for values)
		iFitParameters - array of spline parameters for interpolation
		iLaneWidth - lane width, in meters
		iLeftBoundary, iRightBoundary - type of boundary (see RoadPartition.h for values)
		iNumPoints, iPartitionPoints - number and array of points defining this partition

	OUTPUTS:
		none.
	*/

	strcpy_s(mPartitionID, ROADPOINT_FIELDSIZE, iPartitionID);
	mPartitionType = iPartitionType;
	mIsSparse = iIsSparse;
	mFitType = iFitType;
	mFitParameters = iFitParameters;
	mLaneWidth = iLaneWidth;
	mLeftBoundary = iLeftBoundary;
	mRightBoundary = iRightBoundary;
	mNumPoints = iNumPoints;
	mPartitionPoints = iPartitionPoints;

	return;
}

void RoadPartition::SetPartitionConnections(int iNumNearbyStoplines, RoadPoint** iNearbyStoplines, int iNumLaneAdjacentPartitions, 
	RoadPartition** iLaneAdjacentPartitions, int iNumLeftLaneAdjacentPartitions, RoadPartition** iLeftLaneAdjacentPartitions, 
	int iNumRightLaneAdjacentPartitions, RoadPartition** iRightLaneAdjacentPartitions, int iNumNearbyPartitions, 
	RoadPartition** iNearbyPartitions)
{
	/*
	Sets all the links between this partition and others.

	INPUTS:
		iNumNearbyStoplines, iNearbyStoplines - number and array of nearby stoplines
		iNumLaneAdjacentPartitions, iLaneAdjacentPartitions - number and array of adjacent partitions
			in the same lane
		iNumLeftLaneAdjacentPartitions, iLeftLaneAdjacentPartitions - number and array of adjacent
			partitions in the left lane
		iNumRightLaneAdjacentPartitions, iRightLaneAdjacentPartitions - number and array of adjacent
			partitions in the right lane
		iNumNearbyPartitions, iNearbyPartitions - number and array of nearby partitions

	OUTPUTS:
		none.
	*/

	mNumNearbyStoplines = iNumNearbyStoplines;
	mNearbyStoplines = iNearbyStoplines;
	mNumLaneAdjacentPartitions = iNumLaneAdjacentPartitions;
	mLaneAdjacentPartitions = iLaneAdjacentPartitions;
	mNumLeftLaneAdjacentPartitions = iNumLeftLaneAdjacentPartitions;
	mLeftLaneAdjacentPartitions = iLeftLaneAdjacentPartitions;
	mNumRightLaneAdjacentPartitions = iNumRightLaneAdjacentPartitions;
	mRightLaneAdjacentPartitions = iRightLaneAdjacentPartitions;
	mNumNearbyPartitions = iNumNearbyPartitions;
	mNearbyPartitions = iNearbyPartitions;

	return;
}

double RoadPartition::DistanceToPartition(double iEast, double iNorth)
{
	/*
	Computes the distance to this partition from a particular E-N location.
	Distance computed is Euclidean, but the exact answer will depend on the
	type of partition this is, and the type of fit being done.

	INPUTS:
		iEast, iNorth - E-N point of interest.

	OUTPUTS:
		rDist - returns the distance (m) from iEast, iNorth to the partition.
	*/

	double rDist = DBL_MAX;

	switch (mFitType)
	{
	case RP_LINE:
		{
			//for lines, project the EN position onto the line

			RoadPoint* firstpt = mPartitionPoints[0];
			RoadPoint* lastpt = mPartitionPoints[1];

			//calculate the origin of the linear projection
			double eorig = 0.5*(firstpt->East() + lastpt->East());
			double norig = 0.5*(firstpt->North() + lastpt->North());
			//calculate the alongtrack vector
			double de = 0.5*(lastpt->East() - firstpt->East());
			double dn = 0.5*(lastpt->North() - firstpt->North());
			//calculate the segment half-length
			double shlen = sqrt(de*de + dn*dn);

			//project the EN position onto the line
			double erel = iEast - eorig;
			double nrel = iNorth - norig;

			if (fabs(shlen) == 0.0)
			{
				printf("Warning: partition %s is 0-length.\n", PartitionID());
				//the segment is length zero: compute distance to the waypoint
				de = firstpt->East() - iEast;
				dn = firstpt->North() - iNorth;
				rDist = sqrt(de*de + dn*dn);

				break;
			}

			//calculate alongtrack and offtrack components
			double dat = (de*erel + dn*nrel) / shlen;
			double dot = (-dn*erel + de*nrel) / shlen;

			if (dat > shlen)
			{
				//the EN location is off the segment: compute distance to the waypoint
				de = lastpt->East() - iEast;
				dn = lastpt->North() - iNorth;
				rDist = sqrt(de*de + dn*dn);
			}
			else if (dat < -shlen)
			{
				//the EN location is off the segment: compute distance to the waypoint
				de = firstpt->East() - iEast;
				dn = firstpt->North() - iNorth;
				rDist = sqrt(de*de + dn*dn);
			}
			else
			{
				//the EN location projects onto the segment: compute offtrack distance
				rDist = fabs(dot);
			}
		}

		break;

	case RP_POLYGON:
		{
			//if the point of interest is inside the polygon, return 0 distance
			//otherwise, return the perpendicular distance to the closest perimeter segment
			//otherwise return the distance to the closest perimeter point

			/*
			if (mIsSparse == false && ((int) mFitParameters[0]) == mNumPoints)
			{
				//if the polygon is not sparse and the number of partition points equals the number of polygon vertices,
				//use the old zone code

				int i;
				int np = mNumPoints;
				double de1;
				double dn1;
				double de2;
				double dn2;
				//count the number of crossings
				int numcrossings = 0;

				//check the last point and the first point to initialize the algorithm
				dn1 = mPartitionPoints[np-1]->North() - iNorth;
				dn2 = mPartitionPoints[0]->North() - iNorth;
				if (dn1*dn2 <= 0.0)
				{
					//the points differ in sign (or are on the ray)

					if (fabs(dn1) > 0.0 || fabs(dn2) > 0.0)
					{
						//one point is above the ray and one point is below (or on) the ray: test for explicit intersection
						double w = -dn1 / (dn2 - dn1);
						de1 = mPartitionPoints[np-1]->East() - iEast;
						de2 = mPartitionPoints[0]->East() - iEast;

						if (de1 + w*(de2 - de1) > 0.0)
						{
							//line intersection occurs on the ray to the right of the test point
							numcrossings++;
						}
					}
					//NOTE: if both points are on the ray, that edge is ignored as a "length 0" edge
				}
				for (i = 1; i < np; i++)
				{
					//check successive pairs of points for y-axis crossings (DARPA gives CW ordering of zone points)

					dn1 = dn2;
					dn2 = mPartitionPoints[i]->North() - iNorth;
					if (dn1*dn2 <= 0.0)
					{
						//the points differ in sign (or are on the ray)

						if (fabs(dn1) > 0.0 || fabs(dn2) > 0.0)
						{
							//one point is above the ray and one point is below (or on) the ray: test for explicit intersection
							double w = -dn1 / (dn2 - dn1);
							de1 = mPartitionPoints[i-1]->East() - iEast;
							de2 = mPartitionPoints[i]->East() - iEast;

							if (de1 + w*(de2 - de1) > 0.0)
							{
								//line intersection occurs on the ray to the right of the test point
								numcrossings++;
							}
						}
						//NOTE: if both points are on the ray, that edge is ignored as a "length 0" edge
					}
				}

				//check if the number of crossings is odd (point in zone) or even (point outside zone)
				if (numcrossings % 2 == 1)
				{
					//query point is in the zone polygon
					rDist = 0.0;
				}
				else
				{
					//query point is outsize of the zone polygon: 
					//return the perpendicular distance to a perimeter segment,
					//or the distance to the closest perimeterpoint

					rDist = DBL_MAX;

					for (i = 0; i < np; i++)
					{
						//test to see if the point can be projected onto any perimeter segment

						RoadPoint* firstpt;
						RoadPoint* lastpt;
						if (i > 0)
						{
							firstpt = mPartitionPoints[i-1];
							lastpt = mPartitionPoints[i];
						}
						else
						{
							//first test the first point and the last point
							firstpt = mPartitionPoints[np-1];
							lastpt = mPartitionPoints[0];
						}

						//project the EN position onto the line from firstpt to lastpt

						//calculate the origin of the linear projection
						double eorig = 0.5*(firstpt->East() + lastpt->East());
						double norig = 0.5*(firstpt->North() + lastpt->North());
						//calculate the alongtrack vector
						double de = 0.5*(lastpt->East() - firstpt->East());
						double dn = 0.5*(lastpt->North() - firstpt->North());
						//calculate the segment half-length
						double shlen = sqrt(de*de + dn*dn);

						//project the EN position onto the line
						double erel = iEast - eorig;
						double nrel = iNorth - norig;

						if (fabs(shlen) > 0.0)
						{
							//calculate alongtrack and offtrack components
							double dat = (de*erel + dn*nrel) / shlen;
							double dot = (-dn*erel + de*nrel) / shlen;

							dot = fabs(dot);
							if (fabs(dat) <= shlen)
							{
								//the EN location projects onto the segment: return offtrack distance

								if (dot < rDist)
								{
									rDist = dot;
								}
							}
						}
					}

					//also compute distance to each perimeterpoint, in case one of those is closer

					for (i = 0; i < np; i++)
					{
						double de = iEast - mPartitionPoints[i]->East();
						double dn = iNorth - mPartitionPoints[i]->North();

						double dcur = sqrt(de*de + dn*dn);
						if (dcur < rDist)
						{
							//found a closer perimeterpoint
							rDist = dcur;
						}
					}
				}
			}
			//else
			*/

			//use the polygon vertices specified in the fit parameters to determine the distance to the polygon

			int i;
			int np = (int) mFitParameters[0] + 1;

			//extract the points for the polygon vertices
			double* pp = new double[2*np];
			for (i = 0; i < np-1; i++)
			{
				pp[midx(0, i, 2)] = mFitParameters[2*i+1];
				pp[midx(1, i, 2)] = mFitParameters[2*i+2];
			}
			//set the polygon so the first and last points are the same
			pp[midx(0, np-1, 2)] = mFitParameters[1];
			pp[midx(1, np-1, 2)] = mFitParameters[2];

			if (PointInPolygon(iEast, iNorth, np, pp) == true)
			{
				//query point is in the polygon
				rDist = 0.0;
			}
			else
			{
				//query point is outsize of the polygon: 
				//return the perpendicular distance to a perimeter segment, or the distance to the closest vertex

				rDist = DBL_MAX;

				for (i = 0; i < np-1; i++)
				{
					//test to see if the point can be projected onto any perimeter segment

					double firstE = pp[midx(0, i, 2)];
					double firstN = pp[midx(1, i, 2)];
					double lastE = pp[midx(0, i+1, 2)];
					double lastN = pp[midx(1, i+1, 2)];

					//project the EN position onto the line from firstpt to lastpt

					//calculate the origin of the linear projection
					double eorig = 0.5*(firstE + lastE);
					double norig = 0.5*(firstN + lastN);
					//calculate the alongtrack vector
					double de = 0.5*(lastE - firstE);
					double dn = 0.5*(lastN - firstN);
					//calculate the segment half-length
					double shlen = sqrt(de*de + dn*dn);

					//project the EN position onto the line
					double erel = iEast - eorig;
					double nrel = iNorth - norig;

					if (fabs(shlen) > 0.0)
					{
						//calculate alongtrack and offtrack components
						double dat = (de*erel + dn*nrel) / shlen;
						double dot = (-dn*erel + de*nrel) / shlen;

						dot = fabs(dot);
						if (fabs(dat) <= shlen)
						{
							//the EN location projects onto the segment: return offtrack distance

							if (dot < rDist)
							{
								rDist = dot;
							}
						}
					}
				}

				//also compute distance to each perimeter vertex, in case one of those is closer
				for (i = 0; i < np; i++)
				{
					double de = iEast - pp[midx(0, i, 2)];
					double dn = iNorth - pp[midx(1, i, 2)];

					double dcur = sqrt(de*de + dn*dn);
					if (dcur < rDist)
					{
						//found a closer perimeter vertex
						rDist = dcur;
					}
				}
			}

			//free memory storing polygon vertices
			delete [] pp;
		}

		break;
	}

	return rDist;
}

bool RoadPartition::IsOnPartition(double iEast, double iNorth)
{
	/*
	Calculates whether a point is "on" this partition, returning true if it is
	and false otherwise.

	INPUTS:
		iEast, iNorth - the query point

	OUTPUTS:
		rIsOnPartition - true if the point is "on" the partition, false otherwise
	*/

	bool rIsOnPartition = false;

	switch (mFitType)
	{
	case RP_LINE:
		{
			//NOTE: line fits are treated as a rectangle with 
			//width equal to the lane and length equal to the partition length

			//project the EN position onto the line

			RoadPoint* firstpt = mPartitionPoints[0];
			RoadPoint* lastpt = mPartitionPoints[1];

			//extract the lane half-width
			double shwid = 0.5*mLaneWidth;

			//calculate the origin of the linear projection
			double eorig = 0.5*(firstpt->East() + lastpt->East());
			double norig = 0.5*(firstpt->North() + lastpt->North());
			//calculate the alongtrack vector
			double de = 0.5*(lastpt->East() - firstpt->East());
			double dn = 0.5*(lastpt->North() - firstpt->North());
			//calculate the segment half-length
			double shlen = sqrt(de*de + dn*dn);

			//project the EN position onto the line
			double erel = iEast - eorig;
			double nrel = iNorth - norig;

			//calculate alongtrack and offtrack components
			double dat = (de*erel + dn*nrel) / shlen;
			double dot = (-dn*erel + de*nrel) / shlen;

			if (fabs(dat) <= shlen)
			{
				//the EN location is on the segment according to alongtrack distance

				if (fabs(dot) <= shwid)
				{
					//the EN location is on the segmnet according to offtrack distance also
					rIsOnPartition = true;
				}
			}
		}

		break;

	case RP_POLYGON:
		{
			//if the point of interest is inside the polygon, return the result
			//of a point-in-polygon test

			/*
			if (mIsSparse == false && ((int) mFitParameters[0]) == mNumPoints)
			{
				int i;
				int np = mNumPoints;
				double de1;
				double dn1;
				double de2;
				double dn2;
				//count the number of crossings
				int numcrossings = 0;

				//check the last point and the first point to initialize the algorithm
				dn1 = mPartitionPoints[np-1]->North() - iNorth;
				dn2 = mPartitionPoints[0]->North() - iNorth;
				if (dn1*dn2 <= 0.0)
				{
					//the points differ in sign (or are on the ray)

					if (fabs(dn1) > 0.0 || fabs(dn2) > 0.0)
					{
						//one point is above the ray and one point is below (or on) the ray: test for explicit intersection
						double w = -dn1 / (dn2 - dn1);
						de1 = mPartitionPoints[np-1]->East() - iEast;
						de2 = mPartitionPoints[0]->East() - iEast;

						if (de1 + w*(de2 - de1) > 0.0)
						{
							//line intersection occurs on the ray to the right of the test point
							numcrossings++;
						}
					}
					//NOTE: if both points are on the ray, that edge is ignored as a "length 0" edge
				}
				for (i = 1; i < np; i++)
				{
					//check successive pairs of points for y-axis crossings (DARPA gives CW ordering of zone points)

					dn1 = dn2;
					dn2 = mPartitionPoints[i]->North() - iNorth;
					if (dn1*dn2 <= 0.0)
					{
						//the points differ in sign (or are on the ray)

						if (fabs(dn1) > 0.0 || fabs(dn2) > 0.0)
						{
							//one point is above the ray and one point is below (or on) the ray: test for explicit intersection
							double w = -dn1 / (dn2 - dn1);
							de1 = mPartitionPoints[i-1]->East() - iEast;
							de2 = mPartitionPoints[i]->East() - iEast;

							if (de1 + w*(de2 - de1) > 0.0)
							{
								//line intersection occurs on the ray to the right of the test point
								numcrossings++;
							}
						}
						//NOTE: if both points are on the ray, that edge is ignored as a "length 0" edge
					}
				}

				//check if the number of crossings is odd (point in zone) or even (point outside zone)
				if (numcrossings % 2 == 1)
				{
					//query point is in the zone polygon
					rIsOnPartition = true;
				}
			}
			//else
			*/

			int i;
			int np = (int) mFitParameters[0] + 1;

			//extract the points for the polygon vertices
			double* pp = new double[2*np];
			for (i = 0; i < np-1; i++)
			{
				pp[midx(0, i, 2)] = mFitParameters[2*i+1];
				pp[midx(1, i, 2)] = mFitParameters[2*i+2];
			}
			//set the polygon so the first and last points are the same
			pp[midx(0, np-1, 2)] = mFitParameters[1];
			pp[midx(1, np-1, 2)] = mFitParameters[2];

			//test whether the point is in the polygon (on the partition) or not
			rIsOnPartition = PointInPolygon(iEast, iNorth, np, pp);

			//delete memory allocated in partition points
			delete [] pp;
		}

		break;
	}

	return rIsOnPartition;
}

/*
void RoadPartition::PartitionPolygon(int& oNumPoints, double*& oPolygonPoints)
{
	/
	Generates a list of vertices that represent the polygon of the road partition.
	The list of vertices is returned as an array, where the last point and the first
	point in the array are the same (a closed polygon).

	INPUTS:
		oNumPoints - will contain the number of points returned on output
		oPolygonPoints - will contain the polygon points, stored in a 
			2 x oNumPoints matrix, where each column is an [E; N] polygon
			vertex.

	OUTPUTS:
		oNumPoints, oPolygonPoints - the number and list of polygon points returned.
			Polygon points are returned sorted in counterclockwise order.

			NOTE: memory is allocated for oPolygonPoints in this function, so it MUST
			be freed elsewhere.
	/

	oNumPoints = 0;
	oPolygonPoints = NULL;

	int i;

	switch (mFitType)
	{
	case RP_LINE:
		{
			//return a rectangle for line fits
			oNumPoints = 5;
			oPolygonPoints = new double[2*oNumPoints];

			RoadPoint* firstpt = mPartitionPoints[0];
			RoadPoint* lastpt = mPartitionPoints[1];

			//extract the half lane width
			double hlw = 0.5*mLaneWidth;

			//calculate the origin of the linear projection
			double eorig = 0.5*(firstpt->East() + lastpt->East());
			double norig = 0.5*(firstpt->North() + lastpt->North());
			//calculate the alongtrack vector
			double de = 0.5*(lastpt->East() - firstpt->East());
			double dn = 0.5*(lastpt->North() - firstpt->North());
			//calculate the segment half-length
			double shlen = sqrt(de*de + dn*dn);
			//calculate the offtrack vector
			double deo = -hlw*dn/shlen;
			double dno = hlw*de/shlen;

			//set the polygon
			oPolygonPoints[midx(0, 0, 2)] = eorig + de - deo;
			oPolygonPoints[midx(1, 0, 2)] = norig + dn - dno;
			oPolygonPoints[midx(0, 1, 2)] = eorig + de + deo;
			oPolygonPoints[midx(1, 1, 2)] = norig + dn + dno;
			oPolygonPoints[midx(0, 2, 2)] = eorig - de + deo;
			oPolygonPoints[midx(1, 2, 2)] = norig - dn + dno;
			oPolygonPoints[midx(0, 3, 2)] = eorig - de - deo;
			oPolygonPoints[midx(1, 3, 2)] = norig - dn - dno;
			oPolygonPoints[midx(0, 4, 2)] = oPolygonPoints[midx(0, 0, 2)];
			oPolygonPoints[midx(1, 4, 2)] = oPolygonPoints[midx(1, 0, 2)];
		}

		break;

	case RP_POLYGON:
		{

			/
			if (mIsSparse == false && ((int) mFitParameters[0]) == mNumPoints)
			{
				oNumPoints = mNumPoints + 1;
				oPolygonPoints = new double[oNumPoints*2];

				for (i = 0; i < mNumPoints; i++)
				{
					//copy each point into the polygon in reversed order (counterclockwise)
					oPolygonPoints[midx(0, i, 2)] = mPartitionPoints[mNumPoints-i-1]->East();
					oPolygonPoints[midx(1, i, 2)] = mPartitionPoints[mNumPoints-i-1]->North();
				}
				//duplicate the first point in the last point to close the polygon
				oPolygonPoints[midx(0, oNumPoints-1, 2)] = oPolygonPoints[midx(0, 0, 2)];
				oPolygonPoints[midx(1, oNumPoints-1, 2)] = oPolygonPoints[midx(1, 0, 2)];
			}
			else
			/

			oNumPoints = (int) mFitParameters[0] + 1;

			//extract the points for the polygon vertices
			oPolygonPoints = new double[2*oNumPoints];
			for (i = 0; i < oNumPoints-1; i++)
			{
				oPolygonPoints[midx(0, i, 2)] = mFitParameters[2*i+1];
				oPolygonPoints[midx(1, i, 2)] = mFitParameters[2*i+2];
			}
			//set the polygon so the first and last points are the same
			oPolygonPoints[midx(0, oNumPoints-1, 2)] = mFitParameters[1];
			oPolygonPoints[midx(1, oNumPoints-1, 2)] = mFitParameters[2];
		}

		break;
	}

	return;
}
*/

void RoadPartition::LaneOffsets(double* oLaneOffset, double* oHeadingOffset, double iEast, double iNorth, double iHeading)
{
	/*
	Computes location wrt the lane and heading wrt the lane of a particular ENH for this 
	road partition.

	INPUTS:
		oLaneOffset, oHeadingOffset - offset of the point of interest wrt the lane
			and heading of the road.
		iEast, iNorth, iHeading - the point at which offsets are to be computed

	OUTPUTS:
		oLaneOffset, oHeadingOffset - will be populated on return

	NOTE: output arguments are as a camera would see them.  That is, the road's direction
	is taken to be whatever is closest to the given ENH.  Sign convention is consistent
	with vehicle coordinates, with positive to the left.
	*/

	switch (mFitType)
	{
	case RP_LINE:
		{
			//project the EN position onto the line

			RoadPoint* firstpt = mPartitionPoints[0];
			RoadPoint* lastpt = mPartitionPoints[1];

			//calculate the origin of the linear projection
			double eorig = 0.5*(firstpt->East() + lastpt->East());
			double norig = 0.5*(firstpt->North() + lastpt->North());
			//calculate the alongtrack vector
			double de = 0.5*(lastpt->East() - firstpt->East());
			double dn = 0.5*(lastpt->North() - firstpt->North());
			//calculate the segment half-length
			double shlen = sqrt(de*de + dn*dn);
			//calculate the segment's heading (+CCW E)
			double sh = mFitParameters[0];

			//project the EN position onto the line
			double erel = iEast - eorig;
			double nrel = iNorth - norig;

			//calculate alongtrack and offtrack components
			double dat = (de*erel + dn*nrel) / shlen;
			//NOTE: the version below is for a right-handed segment coordinate frame
			//double dot = (-dn*erel + de*nrel) / shlen;
			//NOTE: the version below is for a vehicle coordinate frame
			double dot = -(-dn*erel + de*nrel) / shlen;

			//calculate vehicle relative position and heading
			double hrel = UnwrapAngle(sh - iHeading);
			//check if camera is pointed wrong way down the partition
			if (IsInSameDirection(iEast, iNorth, iHeading) == false)
			{
				//camera is pointed wrong way: flip the road's direction
				hrel = UnwrapAngle(PI + hrel);
				dot = -dot;
			}

			//set return arguments
			*oLaneOffset = dot;
			*oHeadingOffset = hrel;
		}

		break;

	case RP_POLYGON:

		//road, lane, and heading offsets are undefined for perimeter type partitions
		*oLaneOffset = DBL_MAX;
		*oHeadingOffset = DBL_MAX;

		break;
	}

	return;
}

bool RoadPartition::IsInSameDirection(double iEast, double iNorth, double iHeading)
{
	/*
	Returns true if the implied direction of this partition is the same as the direction
	of travel, false otherwise.

	INPUTS:
		iEast, iNorth, iHeading - the viewpoint direction of travel.

	OUTPUTS:
		rIsInSameDirection - true if the partition's implied direction is close to the
			direction of travel, false otherwise.
	*/

	bool rIsInSameDirection = false;

	switch (mFitType)
	{
	case RP_LINE:
		//for line type fits, heading is valid

		if (fabs(UnwrapAngle(RoadHeading(iEast, iNorth) - iHeading)) <= PIOTWO)
		{
			rIsInSameDirection = true;
		}

		break;
	}

	return rIsInSameDirection;
}

bool RoadPartition::IsAdjacent(double iEast, double iNorth)
{
	/*
	Determines whether this partition is adjacent to the input point of view.
	Adjacent partitions are parallel to (or equal to) the closest partition, 
	but possibly shifted by some lateral offset.

	INPUTS:
		iEast, iNorth - coordinates for the point of interest.

	OUTPUTS:
		rIsAdjacent - true if the point of interest is adjacent to this partition, 
			false otherwise.
	*/

	bool rIsAdjacent = false;

	switch (mFitType)
	{
	case RP_LINE:
		//for line fits, project the EN position onto the line

		RoadPoint* firstpt = mPartitionPoints[0];
		RoadPoint* lastpt = mPartitionPoints[1];

		//calculate the origin of the linear projection
		double eorig = 0.5*(firstpt->East() + lastpt->East());
		double norig = 0.5*(firstpt->North() + lastpt->North());
		//calculate the alongtrack vector
		double de = 0.5*(lastpt->East() - firstpt->East());
		double dn = 0.5*(lastpt->North() - firstpt->North());
		//calculate the segment half-length
		double shlen = sqrt(de*de + dn*dn);

		//project the EN position onto the line
		double erel = iEast - eorig;
		double nrel = iNorth - norig;

		//calculate alongtrack and offtrack components
		double dat = (de*erel + dn*nrel) / shlen;

		if (fabs(dat) <= shlen)
		{
			//the EN location projects onto the segment
			rIsAdjacent = true;
		}

		break;
	}

	return rIsAdjacent;
}

double RoadPartition::RoadHeading(double iEast, double iNorth)
{
	/*
	Computes the heading of the road at a particular point.

	INPUTS:
		iEast, iNorth - point of interest

	OUTPUTS:
		rRoadHeading - returns the heading of the road (CCW E) at the point
			of interest
	*/

	double rRoadHeading = 0.0;

	switch (mFitType)
	{
	case RP_LINE:
		//for a line, the road heading is constant
		rRoadHeading = mFitParameters[0];
		break;
	}

	return rRoadHeading;
}

RoadPartition* RoadPartition::NextPartitionInLane(void)
{
	/*
	Gets the next partition in the lane, following the lane's implied
	direction of travel

	INPUTS:
		none.

	OUTPUTS:
		rNextPartition - returns the next partition in the lane.  If the 
			next partition is ambiguous or doesn't exist, returns NULL.
	*/

	RoadPartition* rNextPartition = NULL;

	if (mPartitionType == RP_ZONE)
	{
		//can't compute a next partition for a zone
		return rNextPartition;
	}
	if (mFitType != RP_LINE)
	{
		//can't compute a next partition for anything but a line fit
		return rNextPartition;
	}

	int i;
	int np = mNumLaneAdjacentPartitions;
	RoadPartition* rptst;
	RoadPoint* rptcur = mPartitionPoints[1];
	RoadPoint* rpttst;
	if (np <= 3)
	{
		//only look for an adjacent partition if there are 1 or 2 adjacent ones + this partition
		//(any more than that would be ambiguous)
		for (i = 0; i < np; i++)
		{
			//check to see whether the first point of rptst matches the last point in this partition
			rptst = mLaneAdjacentPartitions[i];
			switch (rptst->PartitionType())
			{
			case RP_LANE:
			case RP_INTERCONNECT:

				switch (rptst->FitType())
				{
				case RP_LINE:
					rpttst = rptst->GetPoint(0);
					if (strcmp(rpttst->WaypointID(), rptcur->WaypointID()) == 0)
					{
						//found the next partition
						rNextPartition = rptst;
					}
					break;
				}

				break;
			}
		}
	}

	return rNextPartition;
}

RoadPartition* RoadPartition::PreviousPartitionInLane(void)
{
	/*
	Gets the previous partition in the lane, following the lane's implied
	direction of travel backwards

	INPUTS:
		none.

	OUTPUTS:
		rPreviousPartition - returns the previous partition in the lane.  If the 
			partition is ambiguous or doesn't exist, returns NULL.
	*/

	RoadPartition* rPreviousPartition = NULL;

	if (mPartitionType == RP_ZONE)
	{
		//can't compute a previous partition for a zone
		return rPreviousPartition;
	}
	if (mFitType != RP_LINE)
	{
		//can't compute a previous partition for anything but a line fit
		return rPreviousPartition;
	}

	int i;
	int np = mNumLaneAdjacentPartitions;
	RoadPartition* rptst;
	RoadPoint* rptcur = mPartitionPoints[0];
	RoadPoint* rpttst;
	if (np <= 2)
	{
		//only look for an adjacent partition if there are 1 or 2 adjacent ones
		//(any more than that would be ambiguous)
		for (i = 0; i < np; i++)
		{
			//check to see whether the last point of rptst matches the first point in this partition
			rptst = mLaneAdjacentPartitions[i];
			switch (rptst->PartitionType())
			{
			case RP_LANE:
			case RP_INTERCONNECT:

				switch (rptst->FitType())
				{
				case RP_LINE:
					rpttst = rptst->GetPoint(1);
					if (strcmp(rpttst->WaypointID(), rptcur->WaypointID()) == 0)
					{
						//found the previous partition
						rPreviousPartition = rptst;
					}
					break;
				}

				break;
			}
		}
	}

	return rPreviousPartition;
}

RoadPartition* RoadPartition::LeftLanePartition(double iEast, double iNorth)
{
	/*
	Gets the closest partition to the viewpoint of interest that is one lane over
	to the left, defined by this partition's implied direction of travel.

	INPUTS:
		iEast, iNorth - viewpoint of interest

	OUTPUTS:
		rLeftLanePartition - a pointer to the closest partition to the left of 
			this partition, if it exists.  NULL if no such partition exists.
	*/

	RoadPartition* rLeftLanePartition = NULL;

	//determine whether there is a left lane
	int np = mNumLeftLaneAdjacentPartitions;

	if (np == 0)
	{
		//no left lane exists
		return rLeftLanePartition;
	}

	int i;
	double dbest = DBL_MAX;

	for (i = 0; i < np; i++)
	{
		//test the ith left lane adjacent partition
		RoadPartition* rpcur = mLeftLaneAdjacentPartitions[i];

		/*
		//note: should adjacency be tested here or not?
		if (rpcur->IsAdjacent(iEast, iNorth) == false)
		{
			continue;
		}
		*/

		//if the left lane is adjacent, compute distance to it
		double dcur = rpcur->DistanceToPartition(iEast, iNorth);
		if (dcur < dbest)
		{
			dbest = dcur;
			rLeftLanePartition = rpcur;
		}
	}

	return rLeftLanePartition;
}

RoadPartition* RoadPartition::RightLanePartition(double iEast, double iNorth)
{
	/*
	Gets the closest partition to the viewpoint of interest that is one lane over
	to the right, defined by this partition's implied direction of travel.

	INPUTS:
		iEast, iNorth - viewpoint of interest

	OUTPUTS:
		rRightLanePartition - a pointer to the closest partition to the right of 
			this partition, if it exists.  NULL if no such partition exists.
	*/

	RoadPartition* rRightLanePartition = NULL;

	//determine whether there is a right lane
	int np = mNumRightLaneAdjacentPartitions;

	if (np == 0)
	{
		//no right lane exists
		return rRightLanePartition;
	}

	int i;
	double dbest = DBL_MAX;

	for (i = 0; i < np; i++)
	{
		//test the ith right lane adjacent partition
		RoadPartition* rpcur = mRightLaneAdjacentPartitions[i];

		/*
		//note: should adjacency be tested here or not?
		if (rpcur->IsAdjacent(iEast, iNorth) == false)
		{
			continue;
		}
		*/

		//if the right lane is adjacent, compute distance to it
		double dcur = rpcur->DistanceToPartition(iEast, iNorth);
		if (dcur < dbest)
		{
			dbest = dcur;
			rRightLanePartition = rpcur;
		}
	}

	return rRightLanePartition;
}
