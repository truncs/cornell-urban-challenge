#ifndef METAMEASUREMENT_H
#define METAMEASUREMENT_H

#include "LocalMapConstants.h"

#include <STDLIB.H>

//METAMEASUREMENT TYPES
#define MM_INVALID -1
//cluster with both sides visible
#define MM_IBEOCLUSTER_00 8300
//cluster with only cw side visible
#define MM_IBEOCLUSTER_01 8301
//cluster with only ccw side visible
#define MM_IBEOCLUSTER_02 8302
//mobileye obstacle
#define MM_MOBILEYEOBSTACLE 5700
//radar obstacle
#define MM_RADAROBSTACLE 10000
//side SICK obstacle
#define MM_SIDESICKOBSTACLE 1333

class MetaMeasurement
{
	//The metameasurement class.  Stores one metameasurement.

private:

	//the type of metameasurement
	int mMeasurementType;
	//the timestamp on the metameasurement
	double mMeasurementTime;

	//the length of the metameasurement
	int mMeasurementLength;
	//the measurement data
	double* mMeasurementData;
	//the measurement covariance matrix
	double* mMeasurementCovariance;

	//the number of data points
	int mNumDataPoints;
	//the raw data points
	double* mDataPoints;

public:

	MetaMeasurement();
	~MetaMeasurement();

	int MeasurementType() {return mMeasurementType;}
	double MeasurementTime() {return mMeasurementTime;}
	int MeasurementLength() {return mMeasurementLength;}
	double* MeasurementData() {return mMeasurementData;}
	double* MeasurementCovariance() {return mMeasurementCovariance;}
	int NumDataPoints() {return mNumDataPoints;}
	double* DataPoints() {return mDataPoints;}

	void SetMeasurementData(int iMeasurementType, double iMeasurementTime, int iMeasurementLength, double* iMeasurementData, 
		double* iMeasurementCovariance, int iNumDataPoints = 0, double* iDataPoints = NULL);
};

#endif //METAMEASUREMENT_H
