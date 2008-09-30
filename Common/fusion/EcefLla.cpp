#include "EcefLla.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

void EcefLlaTransform(double pNEW[3], double pOLD[3], bool ToECEF)
{
	/*
	Transforms a point from ECEF to LLA or from LLA to ECEF.

	INPUTS:
		pNEW - will contain the newly-transformed coordinates.
		pOLD - contains the coordinates to transform
		ToECEF - true if the transformation is to ECEF, false otherwise.

	OUTPUTS:
		pNEW will be populated with the newly-transformed coordinates.
	*/

	//define Earth parameters
	double ae = 6378137.0;
	double be = 6356752.31424518;
	//double e = sqrt((ae*ae - be*be) / (ae*ae));
	double e = 0.0818191908426203;
	//double ep = sqrt((ae*ae - be*be) / (be*be));
	double ep = 0.0820944379496945;

	double beoae = 0.996647189335253;
	double aeobe = 1.00336408982098;

	if (ToECEF == true)
	{
		//converting LLA to ECEF
		double lat = pOLD[0];
		double lon = pOLD[1];
		double alt = pOLD[2];
	    
		double clat = cos(lat);
		double clon = cos(lon);
		double slat = sin(lat);
		double slon = sin(lon);
		
		double N = ae / sqrt(1.0 - e*e*pow(sin(lat), 2.0));
		pNEW[0] = (N + alt)*clat*clon;
		pNEW[1] = (N + alt)*clat*slon;
		pNEW[2] = (beoae*beoae*N + alt)*slat;
	}
	else
	{
		//converting ECEF to LLA
		double x = pOLD[0];
		double y = pOLD[1];
		double z = pOLD[2];
	    
		pNEW[1] = atan2(y, x);

		double p = sqrt(x*x + y*y);
		double theta = atan2(z*ae, p*be);
		pNEW[0] = atan2(z + ep*ep*be*pow(sin(theta), 3.0), p - e*e*ae*pow(cos(theta), 3.0));

		double N = ae / sqrt(1.0 - e*e*pow(sin(pNEW[0]), 2.0));
		pNEW[2] = p/cos(pNEW[0]) - N;
	}

	return;
}

void GetRecef2enu(double Recef2enu[9], double LatOrigin, double LonOrigin)
{
	/*
	Calculates the transformation matrix Recef2enu

	INPUTS:
		Recef2enu - will be populated with the rotation matrix
		LatOrigin - latitude of the origin of the transform
		LonOrigin - longitude of the origin of the transform

	OUTPUTS:
		Recef2enu will be populated.
	*/

	double clat = cos(LatOrigin);
	double clon = cos(LonOrigin);
	double slat = sin(LatOrigin);
	double slon = sin(LonOrigin);

	Recef2enu[midx(0, 0, 3)] = -slon;
	Recef2enu[midx(0, 1, 3)] = clon;
	Recef2enu[midx(0, 2, 3)] = 0.0;
	Recef2enu[midx(1, 0, 3)] = -clon*slat;
	Recef2enu[midx(1, 1, 3)] = -slon*slat;
	Recef2enu[midx(1, 2, 3)] = clat;
	Recef2enu[midx(2, 0, 3)] = clon*clat;
	Recef2enu[midx(2, 1, 3)] = slon*clat;
	Recef2enu[midx(2, 2, 3)] = slat;

	return;
}
