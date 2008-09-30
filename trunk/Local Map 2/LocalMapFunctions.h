#ifndef LOCALMAPFUNCTIONS_H
#define LOCALMAPFUNCTIONS_H

#include "acml.h"
#include "Cluster.h"
#include "LocalMapConstants.h"
#include "MatrixIndex.h"
#include "MetaMeasurement.h"
#include "Sensor.h"
#include "VehicleOdometry.h"

#include <FLOAT.H>
#include <MALLOC.H>
#include <MATH.H>
#include <STDIO.H>
#include <STRING.H>
#include <WINDOWS.H>

//a series of helper utility functions used everywhere

//HELPER FUNCTIONS
//reads one line from an open file
bool GetNextLine(FILE* iOpenFile, int iBuffSize, char* oBuff);
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
//initializes the print screen
void LocalMapScreenInit(void);

//predicts a set of points forward
void PredictPoints(double iDt, int iNumPoints, double* iPoints, VehicleOdometry* iVehicleOdometry);
//the specialized dynamics function for PredictPoints
void PointDynamics(double* xdot, double* x, VehicleOdometry* iVehicleOdometry);

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
//a helper to calculate XY variance from RB variance (for ibeo points)
void XYVarFromRB(double* oXYCov, double iPx, double iPy, double iVarR, double iVarB, double icosSyaw = 1.0, double isinSyaw = 0.0);
//a helper to perform a rotation on a covariance matrix (for coordinate transforms from sensor to vehicle)
void EgoVarFromSensorXY(double* oEgoCovXY, double* oSensorCovXY, double icosSyaw, double isinSyaw);

//DYNAMICS FUNCTIONS
//the road model dynamics function
void RoadModelDynamics(double* xdot, double* Jx, double* Jv, double* x, VehicleOdometry* iVehicleOdometry);
//the static target with points dynamics function
void StaticTargetWithPointsDynamics(double* xdot, double* Jx, double* Jv, double* x, VehicleOdometry* iVehicleOdometry);
///the dynamic target without size dynamics function
void DynamicTargetDynamics(double* xdot, double* Jx, double* Jv, double* x, VehicleOdometry* iVehicleOdometry);
//the dynamic target with points dynamics function
void DynamicTargetWithPointsDynamics(double* xdot, double* Jx, double* Jv, double* x, VehicleOdometry* iVehicleOdometry);
//the dynamic target with width dynamics function
void DynamicTargetWithWidthDynamics(double* xdot, double* Jx, double* Jv, double* x, VehicleOdometry* iVehicleOdometry);

//MEASUREMENT FUNCTIONS
//generates cw-ccw-range measurements for a cluster: [cwb, ccwb, minr]
void BcwBccwRminClusterMeasurement(double* z, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument);
//generates cw-ccw-range measurements for a non-cluster target: [cwb, ccwb, minr]
void BcwBccwRminMeasurement(double* z, double* H, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument);
//generates cw-ccw-range measurements for a non-cluster target with no width: [avgb, minr]
void BcwBccwRminNoWidthMeasurement(double* z, double* H, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument);
//generates cw measurements for a cluster target: [cwb]
void BcwClusterMeasurement(double* z, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument);
//generates cw measurements for a non-cluster target: [cwb]
void BcwMeasurement(double* z, double* H, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument);
//generates ccw measurements: [ccwb]
void BccwClusterMeasurement(double* z, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument);
//generates ccw measurements for a non-cluster target: [ccwb]
void BccwMeasurement(double* z, double* H, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument);
//generates mobileye measurements: [x, y, s, w]
void MobileyeMeasurement(double* z, double* H, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument);
//generates mobileye measurements without width: [x, y, s]
void MobileyeNoWidthMeasurement(double* z, double* H, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument);
//generates mobileye measurements for a cluster without speed: [x, y, w]
void MobileyeNoSpeedClusterMeasurement(double* z, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument);
//generates mobileye measurements for a cluster: [x, y, s, w]
void MobileyeClusterMeasurement(double* z, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument);
//generates radar measurements: [r, b, rr]
void RadarMeasurement(double* z, double* H, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument);
//generates a range bearing measurement from a cluster
void RangeBearingClusterMeasurement(double* z, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument);
//generates a range bearing speed measurement from a cluster
void RangeBearingRateClusterMeasurement(double* z, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument);
//generates a sensor directional distance measurement for a cluster
void SensorDirectionalDistanceCluster(double* z, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument);
//generates a sensor directional distance measurement for a target with no cluster points
void SensorDirectionalDistance(double* z, double* H, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument);
//generates a sensor directional distance measurement for a target with no width
void SensorDirectionalDistanceNoWidth(double* z, double* H, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument);

//KALMAN FILTER FUNCTIONS
//the Kalman prediction function
void KalmanPredict(double* xbar, double* Pbar, int nx, int nv, double dt, double* x, double* P, double* Q, VehicleOdometry* iVehicleOdometry, void (*DynamicsFunction) (double* xdot, double* Jx, double* Jv, double* x, VehicleOdometry* iVehicleOdometry));
//the Kalman likelihood function (when measurements are easy and linear)
double KalmanLikelihood(double* nu, double* S, double* W, int nx, int nz, double* z, double* R, double* H, double* xbar, double* Pbar, double iKalmanGate = DBL_MAX);
//the Kalman update function (when everything is already precomputed)
void KalmanUpdate(double* xhat, double* Phat, int nx, int nz, double* xbar, double* Pbar, double* nu, double* S, double* W);

//SIGMA POINT FILTER FUNCTIONS
//calculates the sigma point update likelihood
double SigmaPointLikelihood(double* nu, double* S, double* W, int nz, double* z, bool* zwrap, double* R, int nx, double* xbar, double* Pbar, void (*MeasurementFunction) (double* z, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument), Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument = NULL, double iSigmaPointGate = DBL_MAX);

//EXTENDED KALMAN FILTER FUNCTIONS
//calculates the extended kalman update likelihood
double ExtendedKalmanLikelihood(double* nu, double* S, double* W, int nz, double* z, bool* zwrap, double* R, int nx, double* xbar, double* Pbar, void (*MeasurementFunction) (double* z, double* H, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument), Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument = NULL, double iExtendedKalmanGate = DBL_MAX);

#endif //LOCALMAPFUNCTIONS_H
