#include "MetaMeasurement.h"

MetaMeasurement::MetaMeasurement()
{
	/*
	Default constructor for the metameasurement.  Initializes elements to invalid values.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	//initialize to invalid values
	mMeasurementType = MM_INVALID;

	mMeasurementLength = 0;
	mMeasurementData = NULL;
	mMeasurementCovariance = NULL;

	mNumDataPoints = 0;
	mDataPoints = NULL;

	return;
}

MetaMeasurement::~MetaMeasurement()
{
	/*
	Destructor for the metameasurement.  Frees memory.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	//free memory and set to invalid values.
	mMeasurementType = MM_INVALID;

	mMeasurementLength = 0;
	delete [] mMeasurementData;
	mMeasurementData = NULL;
	delete [] mMeasurementCovariance;
	mMeasurementCovariance = NULL;

	mNumDataPoints = 0;
	delete [] mDataPoints;
	mDataPoints = NULL;

	return;
}

void MetaMeasurement::SetMeasurementData(int iMeasurementType, double iMeasurementTime, int iMeasurementLength, double* iMeasurementData, double* iMeasurementCovariance, int iNumDataPoints, double* iDataPoints)
{
	/*
	Sets the measurement data for a particular mm.

	INPUTS:
		iMeasurementType - the type of measurement; see MetaMeasurement.h for valid values
		iMeasurementLength - number of elements in the measurement vector
		iMeasurementData - vector containing the measurement information
		iMeasurementCovariance - vector containing the measurement covariance
		iNumDataPoints - number of data points stored
		iDataPoints - raw sensor data points (for clustering)

	OUTPUTS:
		none.  NOTE: memory is NOT copied, so make sure inputs are preallocated.
	*/

	mMeasurementType = iMeasurementType;
	mMeasurementTime = iMeasurementTime;

	mMeasurementLength = iMeasurementLength;
	mMeasurementData = iMeasurementData;
	mMeasurementCovariance = iMeasurementCovariance;

	mNumDataPoints = iNumDataPoints;
	mDataPoints = iDataPoints;

	return;
}
