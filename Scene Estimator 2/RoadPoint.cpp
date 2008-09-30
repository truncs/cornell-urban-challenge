#include "RoadPoint.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

RoadPoint::RoadPoint(void)
{
	/*
	Default constructor.  Creates an empty road point

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	//initialize waypoint ID
	mWaypointID[0] = NULL;

	//the East-North coordinates of the road point
	mEast = 0.0;
	mNorth = 0.0;

	//whether the road point is a stop line or not
	mIsStop = false;

	mNumMemberPartitions = 0;
	mMemberPartitions = NULL;

	return;
}

RoadPoint::~RoadPoint(void)
{
	/*
	Destructor for the road point class.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	mNumMemberPartitions = 0;
	delete [] mMemberPartitions;

	return;
}

void RoadPoint::SetPointData(char* iWaypointID, double iEast, double iNorth, bool iIsStop)
{
	/*
	Stores all the data for the road point.

	INPUTS:
		iWaypointID - the waypoint's unique ID
		iEast, iNorth - the point's east / north locations
		iIsStop - true if the road point is a stopline, false otherwise

	OUTPUTS:
		none.
	*/

	//copy over the ID string
	strcpy_s(mWaypointID, ROADPOINT_FIELDSIZE, iWaypointID);

	mEast = iEast;
	mNorth = iNorth;

	mIsStop = iIsStop;

	return;
}

void RoadPoint::SetMemberPartitions(int iNumMemberPartitions, RoadPartition** iMemberPartitions)
{
	/*
	Stores the member partitions for this road point.

	INPUTS:
		iNumMemberPartitions - number of partitions to which this point belongs
		iMemberPartitions - array of pointers to partitions to which this is a member

	OUTPUTS:
		none.
	*/

	mNumMemberPartitions = iNumMemberPartitions;
	mMemberPartitions = iMemberPartitions;

	return;
}

bool RoadPoint::IsInView(double iEast, double iNorth, double iHeading, double iMaxViewAngle)
{
	/*
	Returns true if this road point is in front of the given viewpoint, false otherwise.

	INPUTS:
		iEast, iNorth, iHeading - the viewpoint of interest
		iMaxViewAngle - maximum angular deviation (cone angle) from the viewpoint to
			be considered visible.

	OUTPUTS:
		rIsInView - true if the road point is in the viewing cone, false otherwise
	*/

	bool rIsInView = false;

	//check whether this point is visible
	double de = mEast - iEast;
	double dn = mNorth - iNorth;
	double ptangl = atan2(dn, de);
	double viewangl = UnwrapAngle(ptangl - iHeading);

	if (fabs(viewangl) <= iMaxViewAngle)
	{
		rIsInView = true;
	}

	return rIsInView;
}

double RoadPoint::GetDistanceToPoint(double iEast, double iNorth)
{
	/*
	Calculates the distance from a point of interest to this waypoint

	INPUTS:
		iEast, iNorth - point of interest

	OUTPUTS:
		rDistance - Euclidean distance from the point of interest to the waypoint
	*/

	double de = mEast - iEast;
	double dn = mNorth - iNorth;

	double rDistance = sqrt(de*de + dn*dn);

	return rDistance;
}
