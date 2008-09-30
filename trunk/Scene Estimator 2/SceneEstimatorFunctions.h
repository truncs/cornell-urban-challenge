#ifndef SCENEESTIMATORFUNCTIONS_H
#define SCENEESTIMATORFUNCTIONS_H

#include "acml.h"
#include "Cluster.h"
#include "EventCodes.h"
#include "MatrixIndex.h"
#include "PosteriorPosePosition.h"
#include "SceneEstimatorConstants.h"
#include "Sensor.h"
#include "time/timing.h"
#include "VehicleOdometry.h"

#include <FLOAT.H>
#include <MATH.H>
#include <MALLOC.H>
#include <STDIO.H>
#include <STRING.H>
#include <WINDOWS.H>

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

//a series of helper utility functions used everywhere

//GENERAL HELPER FUNCTIONS
//reads one line from an open file
bool GetNextLine(FILE* iOpenFile, int iBuffSize, char* oBuff);
//computes the 16-bit CRC of a file
bool ComputeCRC(unsigned short& oCRC, char* iFileName);
//reads a CSV string into a given array
int StringToArrayOfDoubles(double* oArray, int iNumToRead, const char* iCSVBuffer);
//checks if a directory exists
bool DirectoryExists(const char* iDirectory);
//unwraps an angle onto -pi to pi
double UnwrapAngle(double iAngle);
//wraps an angle until it is within pi of a target angle
double WrapAngle(double iAngle, double iTargetAngle);
inline double Round(double iRoundNumber)
{
	/*
	Rounds a number to the nearest integer.

	INPUTS:
		iRoundNumber - the number to round

	OUTPUTS:
		rRounded - the number, rounded to the nearest integer
	*/

	double rRounded = floor(iRoundNumber);
	if (iRoundNumber - rRounded >= 0.5)
	{
		rRounded += 1.0;
	}

	return rRounded;
}
inline int IntegerCompare(void* pvlocale, const void* arg1, const void* arg2)
{
	/*
	Comparison function for integers for qsort_s.

	INPUTS:
		pvlocale - a locale variable; not used
		arg1 - the first integer
		arg2 - the second integer

	OUTPUTS:
		rCompare - returns -1 if arg1 < arg2, 0 if they're equal, and +1 if arg1 > arg2
	*/

	int int1 = *((int*) arg1);
	int int2 = *((int*) arg2);

	if (int1 < int2)
	{
		return -1;
	}
	else if (int1 > int2)
	{
		return 1;
	}
	else
	{
		return 0;
	}
}
//initializes the print screen
void SceneEstimatorScreenInit();

//POINT FUNCTIONS
//predicts a set of 2d points forward in ego-vehicle coordinates
void PredictPoints(double iDt, int iNumPoints, double* iPoints, VehicleOdometry* iVehicleOdometry);
//point prediction dynamics function
void PointDynamics(double* xdot, double* x, VehicleOdometry* iVehicleOdometry);

//POLYGON FUNCTIONS
//calculates the area of a closed polygon
double PolygonArea(int iNumPoints, double* iPolygonPoints);
//tests whether a point is in the given polygon
bool PointInPolygon(double iXtest, double iYtest, int iNumPoints, double* iPolygonPoints);
//calculates the intersection of two line segments (if they intersect)
bool LineIntersect(double& oIntersectX, double& oIntersectY, double x0, double y0, double x1, double y1, double u0, double v0, double u1, double v1);

//TRANSFORMATION FUNCTIONS
//a helper to convert points from ego-vehicle coordinates to sensor coordinates
inline void EgoVehicleToSensor(double& oSensorX, double& oSensorY, double iEgoX, double iEgoY, double icosSyaw, double isinSyaw, double iSx, double iSy)
{
	/*
	Converts a point in ego vehicle coordinates to sensor coordinates

	INPUTS:
		oSensorX, oSensorY - will contain the transformed points on output
		iEgoX, iEgoY - the input point, in ego-vehicle coordinates
		icosSyaw, isinSyaw - cosine and sine of the sensor's yaw wrt ego-vehicle coordinates
		iSx, iSy - location of the sensor, in ego-vehicle coordinates

	OUTPUTS:
		oSensorX, oSensorY - will contain the transformed points on output
	*/

	//shift to sensor position
	double evx = iEgoX - iSx;
	double evy = iEgoY - iSy;

	//rotate the coordinate frame to sensor coordinates from ego vehicle coordinates
	oSensorX = icosSyaw*evx + isinSyaw*evy;
	oSensorY = -isinSyaw*evx + icosSyaw*evy;

	return;
}
//a helper to convert points from sensor coordinates to ego-vehicle coordinates 
inline void SensorToEgoVehicle(double& oEgoX, double& oEgoY, double iSensorX, double iSensorY, double icosSyaw, double isinSyaw, double iSx, double iSy)
{
	/*
	Converts a point in sensor coordinates to ego vehicle coordinates

	INPUTS:
		oEgoX, oEgoY - will contain the transformed points on output
		iSensorX, iSensorY - the input point, in sensor coordinates
		icosSyaw, isinSyaw - cosine and sine of the sensor's yaw wrt ego-vehicle coordinates
		iSx, iSy - location of the sensor, in ego-vehicle coordinates

	OUTPUTS:
		oEgoX, oEgoY - will contain the transformed points on output
	*/

	//rotate the coordinate frame to ego vehicle coordinates from sensor coordinates
	//NOTE: since Syaw rotates ego coordinates to sensor, the reverse rotation is performed here
	oEgoX = icosSyaw*iSensorX - isinSyaw*iSensorY;
	oEgoY = isinSyaw*iSensorX + icosSyaw*iSensorY;

	//shift from sensor position to ego-vehicle origin
	oEgoX += iSx;
	oEgoY += iSy;

	return;
}
//a helper to convert points from object storage frame to ego-vehicle coordinates
inline void ObjectToEgoVehicle(double& oEgoX, double& oEgoY, double iObjectX, double iObjectY, double icosOrient, double isinOrient, double iAx, double iAy)
{
	/*
	Converts a point in the object storage frame to ego vehicle coordinates

	INPUTS:
		oEgoX, oEgoY - will contain the transformed points on output
		iObjectX, iObjectY - the input point, in object storage frame
		icosOrient, isinOrient - cosine and sine of the orientation angle 
			(angle from ego vehicle to the object storage frame)
		iAx, iAy - origin of the object storage frame, in ego-vehicle coordinates

	OUTPUTS:
		oEgoX, oEgoY - will contain the transformed points on output
	*/

	//rotate the coordinate frame from object storage to ego vehicle
	//NOTE: since orientation is the angle from ego vehicle to object storage, perform the reverse rotation here
	oEgoX = icosOrient*iObjectX - isinOrient*iObjectY;
	oEgoY = isinOrient*iObjectX + icosOrient*iObjectY;

	//shift from object storage origin to ego-vehicle origin
	oEgoX += iAx;
	oEgoY += iAy;

	return;
}
//a helper to convert points from ego-vehicle coordinates to object storage frame
inline void EgoVehicleToObject(double& oObjectX, double& oObjectY, double iEgoX, double iEgoY, double icosOrient, double isinOrient, double iAx, double iAy)
{
	/*
	Converts a point in ego vehicle coordinates to the object storage frame

	INPUTS:
		oObjectX, oObjectY - will contain the point transformed into object 
			storage frame on output
		iEgoX, iEgoY - the input point, in ego vehicle coordinates
		icosOrient, isinOrient - cosine and sine of the orientation angle 
			(angle from ego vehicle to the object storage frame)
		iAx, iAy - origin of the object storage frame, in ego-vehicle coordinates

	OUTPUTS:
		oObjectX, oObjectY - will contain the point transformed into object 
			storage frame on output
	*/

	//shift from ego-vehicle origin to object storage origin
	double osx = iEgoX - iAx;
	double osy = iEgoY - iAy;

	//rotate the coordinate frame from ego vehicle to object storage
	oObjectX = icosOrient*osx + isinOrient*osy;
	oObjectY = -isinOrient*osx + icosOrient*osy;

	return;
}
//a helper to convert points from ego-vehicle coordinates to absolute
inline void EgoVehicleToAbsolute(double& oEast, double& oNorth, double iEgoX, double iEgoY, double icosVehHeading, double isinVehHeading, double iVehEast, double iVehNorth)
{
	/*
	Converts a point in ego vehicle coordinates to absolute coordinates

	INPUTS:
		oEast, oNorth - will contain the point transformed to absolute
			coordinates on output
		iEgoX, iEgoY - the input point, in ego-vehicle coordinates
		icosVehHeading, isinVehHeading- cosine and sine of the ego-
			vehicle's current heading, measured CCW East
		iVehEast, iVehNorth - the ego-vehicle's absolute position,
			in easting and northing

	OUTPUTS:
		oEast, oNorth - will contain the point transformed to absolute
			coordinates on output
	*/

	//rotate the coordinate frame from ego vehicle to absolute
	//NOTE: since heading is the angle from absolute to ego vehicle heading,
	//rotate the frame by -heading
	oEast = icosVehHeading*iEgoX - isinVehHeading*iEgoY;
	oNorth = isinVehHeading*iEgoX + icosVehHeading*iEgoY;

	//shift from ego-vehicle centered to absolute frame
	oEast += iVehEast;
	oNorth += iVehNorth;

	return;
}
//a helper to convert track covariance from ego-vehicle to absolute, accounting for posterior pose
void TrackCovarianceToAbsolute(double* oAbsCov, int nxt, double iTrackX, double iTrackY, double iTrackH, double* iTrackCov, PosteriorPosePosition* iPosteriorPose);

//TRACK CORRESPONDENCE AND DYNAMICS FUNCTIONS
//calculates bearing-bearing-range measurements for a cluster: [cwb, ccwb, minr]
void ClusterBcwBccwRmin(double z[3], double x[5], Cluster* iCluster, Sensor* iSensor = NULL);
//calculates the x-y locations of extreme points for a cluster [cwx, cwy, ccwx, ccwy, cpx, cpy]
void ClusterExtremePoints(double z[6], double x[5], Cluster* iCluster, Sensor* iSensor = NULL);
//calculates the expected cluster measurement [cwb, ccwb, minr] and covariance
void ClusterPositionMeasurement(double z[3], double Pzz[3*3], double Pxz[5*3], double x[5], double P[5*5], Cluster* iCluster);
//track dynamics function for prediction
void TrackDynamics(double* xdot, double* Jx, double* Jv, double* x, VehicleOdometry* iVehicleOdometry);
//predicts a posterior pose solution to a desired time
void PredictPosteriorPoseSolution(double iPredictTime, PosteriorPosePosition* iPosteriorPosePosition, VehicleOdometry* iVehicleOdometry);
//posterior pose dynamics function for prediction
void PosteriorPoseDynamics(double* xdot, double* Jx, double* Jv, double* x, VehicleOdometry* iVehicleOdometry);
//dynamics function for a point with a velocity
void DynamicPointDynamics(double* xdot, double* Jx, double* Jv, double* x, VehicleOdometry* iVehicleOdometry);

//KALMAN FILTER FUNCTIONS
//the Kalman prediction function
void KalmanPredict(double* xbar, double* Pbar, int nx, int nv, double dt, double* x, double* P, double* Q, VehicleOdometry* iVehicleOdometry, void (*DynamicsFunction) (double* xdot, double* Jx, double* Jv, double* x, VehicleOdometry* iVehicleOdometry));
//the Kalman likelihood function (when measurements are easy and linear)
double KalmanLikelihood(double* nu, double* S, double* W, int nx, int nz, double* z, double* R, double* H, double* xbar, double* Pbar, double iKalmanGate = DBL_MAX);
//the Kalman update function (when everything is already precomputed)
void KalmanUpdate(double* xhat, double* Phat, int nx, int nz, double* xbar, double* Pbar, double* nu, double* S, double* W);

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //SCENEESTIMATORFUNCTIONS_H
