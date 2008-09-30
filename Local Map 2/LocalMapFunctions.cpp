#include "LocalMapFunctions.h"

bool GetNextLine(FILE* iOpenFile, int iBuffSize, char* oBuff)
{
	/*
	Reads one line of a file into buff, keeping the \n character

	INPUTS:
		OpenFile - file pointer to an open file to read
		buffsize - size of buffer (number of characters)
		buff - will be populated with the information read from the file

	OUTPUTS:
		Returns true if successful, false otherwise.
		If successful, buff will be populated with the line read from the file.
	*/

	//read a line from the file
	if (fgets(oBuff, iBuffSize, iOpenFile) != NULL)
	{
		//remove the newline character if it exists
		char* newline = strchr(oBuff, '\n');

		if (newline != NULL)
		{
			//remove the newline character
			*newline = NULL;
		}

		return true;
	}

	return false;
}

int StringToArrayOfDoubles(double* oArray, int iNumToRead, const char* iCSVBuffer)
{
	/*
	Converts a given CSV string into an array of doubles.

	INPUTS:
		oArray - the output array.  Must have memory allocated
		iNumToRead - maximum number of elements to read into oArray
		iCSVBuffer - the string to be formatted to an array

	OUTPUTS:
		rNumRead - number of values read into the array.
	*/

	int i;
	int rNumRead = 0;

	//extract a string pointer to the front of the string
	int nr;
	char* sp = (char*) iCSVBuffer;

	for (i = 0; i < iNumToRead; i++)
	{
		//try to read the next entry in the array
		nr = sscanf_s(sp, "%lg", &(oArray[i]));

		if (nr <= 0)
		{
			//done reading the string
			break;
		}

		//if code gets here, one double was read successfully
		rNumRead++;

		//advance the pointer to the next separation character
		sp = (char*) strchr(sp, ',');
		if (sp == NULL)
		{
			break;
		}
		//skip over the comma if one was found
		sp++;
	}

	return rNumRead;
}

bool DirectoryExists(const char* iDirectory)
{
	/*
	Checks if a directory exists, and returns whether it does.

	INPUTS:
		iDirectory - a character string specifying the absolute path 
			to the directory
	*/

	bool rExists = false;
	if (iDirectory == NULL)
	{
		return rExists;
	}

	//extract the file attributes for the supposed directory
	DWORD dirattr = GetFileAttributesA(iDirectory);

	if (dirattr != INVALID_FILE_ATTRIBUTES)
	{
		//if the file attributes were found correctly, test for directory
		if ((dirattr & FILE_ATTRIBUTE_DIRECTORY) == FILE_ATTRIBUTE_DIRECTORY)
		{
			rExists = true;
		}
	}

	return rExists;
}

double UnwrapAngle(double iAngle)
{
	/*
	Unwraps an angle to the range -pi to pi

	INPUTS:
		iAngle - the angle, in radians

	OUTPUTS:
		rAngle - the new angle, wrapped from -pi to pi
	*/

	//note: fmod returns the signed remainder of iAngle / 2pi
	double rAngle = fmod(iAngle, TWOPI);
	if (rAngle > PI)
	{
		rAngle -= TWOPI;
	}
	if (rAngle <= -PI)
	{
		rAngle += TWOPI;
	}

	return rAngle;
}

double WrapAngle(double iAngle, double iTargetAngle)
{
	/*
	Wraps an input angle until it is within pi of a target angle

	INPUTS:
		iAngle - the angle to unwrap, in radians
		iTargetAngle - the angle to unwrap to, in radians

	OUTPUTS:
		rAngle - the new angle, unwrapped to be within pi of iTargetAngle
	*/

	//project the input angle down onto 0..2*pi
	double rAngle = fmod(iAngle, TWOPI);
	if (rAngle < 0.0)
	{
		rAngle += TWOPI;
	}

	double nwrap;
	//find out the number of full 2*pi wraps the target angle has made
	nwrap = floor(iTargetAngle / TWOPI);
	//wrap the input angle that many times
	rAngle += nwrap*TWOPI;

	//if rAngle isn't within PI of iTargetAngle, it's within one wrap
	if (iTargetAngle - rAngle > PI)
	{
		rAngle += TWOPI;
	}
	if (iTargetAngle - rAngle < -PI)
	{
		rAngle -= TWOPI;
	}

	return rAngle;
}

void LocalMapScreenInit()
{
	/*
	Print my dedication.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	printf("**************************************************************\n");
	printf("*                    written in memory of                    *\n");
	printf("*                                                            *\n");
	printf("*                    JAMES WILBUR WRIGHT                     *\n");
	printf("*                        1928 - 1984                         *\n");
	printf("**************************************************************\n");
	printf("\n");

	return;
}

/*
void EgoVehicleToSensor(double& oSensorX, double& oSensorY, double iEgoX, double iEgoY, double icosSyaw, double isinSyaw, double iSx, double iSy)
{
	/
	Converts a point in ego vehicle coordinates to sensor coordinates

	INPUTS:
		oSensorX, oSensorY - will contain the transformed points on output
		iEgoX, iEgoY - the input point, in ego-vehicle coordinates
		icosSyaw, isinSyaw - cosine and sine of the sensor's yaw wrt ego-vehicle coordinates
		iSx, iSy - location of the sensor, in ego-vehicle coordinates

	OUTPUTS:
		oSensorX, oSensorY - will contain the transformed points on output
	/

	//shift to sensor position
	double evx = iEgoX - iSx;
	double evy = iEgoY - iSy;

	//rotate the coordinate frame to sensor coordinates from ego vehicle coordinates
	oSensorX = icosSyaw*evx + isinSyaw*evy;
	oSensorY = -isinSyaw*evx + icosSyaw*evy;

	return;
}
*/

/*
void SensorToEgoVehicle(double& oEgoX, double& oEgoY, double iSensorX, double iSensorY, double icosSyaw, double isinSyaw, double iSx, double iSy)
{
	/
	Converts a point in sensor coordinates to ego vehicle coordinates

	INPUTS:
		oEgoX, oEgoY - will contain the transformed points on output
		iSensorX, iSensorY - the input point, in sensor coordinates
		icosSyaw, isinSyaw - cosine and sine of the sensor's yaw wrt ego-vehicle coordinates
		iSx, iSy - location of the sensor, in ego-vehicle coordinates

	OUTPUTS:
		oEgoX, oEgoY - will contain the transformed points on output
	/

	//rotate the coordinate frame to ego vehicle coordinates from sensor coordinates
	//NOTE: since Syaw rotates ego coordinates to sensor, the reverse rotation is performed here
	oEgoX = icosSyaw*iSensorX - isinSyaw*iSensorY;
	oEgoY = isinSyaw*iSensorX + icosSyaw*iSensorY;

	//shift from sensor position to ego-vehicle origin
	oEgoX += iSx;
	oEgoY += iSy;

	return;
}
*/

/*
void ObjectToEgoVehicle(double& oEgoX, double& oEgoY, double iObjectX, double iObjectY, double icosOrient, double isinOrient, double iAx, double iAy)
{
	/
	Converts a point in the object storage frame to ego vehicle coordinates

	INPUTS:
		oEgoX, oEgoY - will contain the transformed points on output
		iObjectX, iObjectY - the input point, in object storage frame
		icosOrient, isinOrient - cosine and sine of the orientation angle 
			(angle from ego vehicle to the object storage frame)
		iAx, iAy - origin of the object storage frame, in ego-vehicle coordinates

	OUTPUTS:
		oEgoX, oEgoY - will contain the transformed points on output
	/

	//rotate the coordinate frame from object storage to ego vehicle
	//NOTE: since orientation is the angle from ego vehicle to object storage, perform the reverse rotation here
	oEgoX = icosOrient*iObjectX - isinOrient*iObjectY;
	oEgoY = isinOrient*iObjectX + icosOrient*iObjectY;

	//shift from object storage origin to ego-vehicle origin
	oEgoX += iAx;
	oEgoY += iAy;

	return;
}
*/

/*
void EgoVehicleToObject(double& oObjectX, double& oObjectY, double iEgoX, double iEgoY, double icosOrient, double isinOrient, double iAx, double iAy)
{
	/
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
	/

	//shift from ego-vehicle origin to object storage origin
	double osx = iEgoX - iAx;
	double osy = iEgoY - iAy;

	//rotate the coordinate frame from ego vehicle to object storage
	oObjectX = icosOrient*osx + isinOrient*osy;
	oObjectY = -isinOrient*osx + icosOrient*osy;

	return;
}
*/

void XYVarFromRB(double* oXYCov, double iPx, double iPy, double iVarR, double iVarB, double icosSyaw, double isinSyaw)
{
	/*
	Calculates the XY covariance from an x-y point with variance in range and bearing.

	INPUTS:
		oXYCov - will be populated with the XY covariance on output
		iPx - the x-coordinate of the point, in sensor coordinates
		iPy - the y-coordinate of the point, in sensor coordinates
		iVarR - the range variance
		iVarB - the bearing variance
		icosSyaw - cosine of the sensor's yaw from vehicle coordinates (an optional extra rotation from sensor to ego-vehicle coordinates)
		isinSyaw - sine of the sensor's yaw from vehicle coordinates (optional extra rotation from sensor to ego-vehicle coordinates)

	OUTPUTS:
		oXYCov - will be populated with the XY covariance (in vehicle coordinates) on output
	*/

	int i;
	int j;
	int k;

	//calculate the XY variance of each point and aggregate them
	double r = sqrt(iPx*iPx + iPy*iPy);
	double cosb = iPx / r;
	double sinb = iPy / r;
	double rcosb = iPx;
	double rsinb = iPy;

	double J[4];
	J[midx(0, 0, 2)] = cosb;
	J[midx(0, 1, 2)] = -rsinb;
	J[midx(1, 0, 2)] = sinb;
	J[midx(1, 1, 2)] = rcosb;

	double JR[4];
	JR[midx(0, 0, 2)] = cosb*iVarR;
	JR[midx(0, 1, 2)] = -rsinb*iVarB;
	JR[midx(1, 0, 2)] = sinb*iVarR;
	JR[midx(1, 1, 2)] = rcosb*iVarB;

	//calculate JRJ'
	for (i = 0; i < 2; i++)
	{
		for (j = 0; j < 2; j++)
		{
			oXYCov[midx(i, j, 2)] = 0.0;
			for (k = 0; k < 2; k++)
			{
				oXYCov[midx(i, j, 2)] += JR[midx(i, k, 2)]*J[midx(j, k, 2)];
			}
		}
	}

	if (icosSyaw != 1.0 && isinSyaw != 0.0)
	{
		//calculate the rotation matrix
		double R[4];
		//note: rotating the coordinate frame from sensor to vehicle coordinates...
		//same as rotating the vector by positive Syaw
		R[midx(0, 0, 2)] = icosSyaw;
		R[midx(0, 1, 2)] = -isinSyaw;
		R[midx(1, 0, 2)] = isinSyaw;
		R[midx(1, 1, 2)] = icosSyaw;

		//calculate RP
		double RP[4];
		for (i = 0; i < 2; i++)
		{
			for (j = 0; j < 2; j++)
			{
				RP[midx(i, j, 2)] = 0.0;
				for (k = 0; k < 2; k++)
				{
					RP[midx(i, j, 2)] += R[midx(i, k, 2)] * oXYCov[midx(k, j, 2)];
				}
			}
		}

		//calculate RPR'
		for (i = 0; i < 2; i++)
		{
			for (j = 0; j < 2; j++)
			{
				oXYCov[midx(i, j, 2)] = 0.0;
				for (k = 0; k < 2; k++)
				{
					oXYCov[midx(i, j, 2)] += RP[midx(i, k, 2)] * R[midx(j, k, 2)];
				}
			}
		}
	}

	return;
}

void EgoVarFromSensorXY(double* oEgoCovXY, double* oSensorCovXY, double icosSyaw, double isinSyaw)
{
	/*
	A helper function to transform an XY covariance matrix (2x2) in sensor 
	coordinates to a 2x2 covariance matrix in vehicle coordinates, by 
	performing a coordinate transformation.

	INPUTS:
		oEgoCovXY - will be populated with the new covariance matrix on output
		oSensorCovXY - contains the original covariance matrix, in sensor
			coordinates
		icosSyaw - cosine of the angle from ego-vehicle coordinates to sensor
		isinSyaw - sine of the angle from ego-vehicle coordinates to sensor

	OUTPUTS:
		oEgoCovXY - will be populated with the new covariance matrix on output
	*/

	int i;
	int j;
	int k;

	//calculate the rotation matrix
	double R[4];
	//note: rotating the coordinate frame from sensor to vehicle coordinates...
	//same as rotating the vector by positive Syaw
	R[midx(0, 0, 2)] = icosSyaw;
	R[midx(0, 1, 2)] = -isinSyaw;
	R[midx(1, 0, 2)] = isinSyaw;
	R[midx(1, 1, 2)] = icosSyaw;

	//calculate RP
	double RP[4];
	for (i = 0; i < 2; i++)
	{
		for (j = 0; j < 2; j++)
		{
			RP[midx(i, j, 2)] = 0.0;
			for (k = 0; k < 2; k++)
			{
				RP[midx(i, j, 2)] += R[midx(i, k, 2)] * oSensorCovXY[midx(k, j, 2)];
			}
		}
	}

	//calculate RPR'
	for (i = 0; i < 2; i++)
	{
		for (j = 0; j < 2; j++)
		{
			oEgoCovXY[midx(i, j, 2)] = 0.0;
			for (k = 0; k < 2; k++)
			{
				oEgoCovXY[midx(i, j, 2)] += RP[midx(i, k, 2)] * R[midx(j, k, 2)];
			}
		}
	}

	return;
}

void RoadModelDynamics(double* xdot, double* Jx, double* Jv, double* x, VehicleOdometry* iVehicleOdometry)
{
	/*
	Calculates the derivative of the road model with respect to time with the given state and
	vehicle motion.

	INPUTS:
		xdot - 3-element vector that will be populated with the state derivative on output
		Jx - 3x3 matrix that will be populated with the state jacobian on output
		Jv - 3x6 matrix that will be populated with the input jacobian on output
		x - current state = [road offset, road heading, road curvature]
		iVehicleOdometry - vehicle odometry structure containing the ego-vehicle's current odometry

	OUTPUTS:
		xdot - 3-element vector that will be populated with the state derivative on output
		Jx - 3x3 matrix that will be populated with the state jacobian on output
		Jv - 3x6 matrix that will be populated with the input jacobian on output
	*/

	int i;
	int j;

	int nx = 3;
	int nv = 6;

	//extract the state
	double ro = x[0];
	double rh = x[1];
	double rc = x[2];

	//extract the relevant vehicle odometry
	double vx = iVehicleOdometry->vx;
	double vy = iVehicleOdometry->vy;
	double wz = iVehicleOdometry->wz;

	//precalculate some values
	double cosrh = cos(rh);
	double sinrh = sin(rh);
	double oooprcro = 1.0 / (1.0 + rc*ro);
	double oooprcro2 = oooprcro*oooprcro;
	double crhvxpsrhvy = cosrh*vx + sinrh*vy;

	//initialize the state derivative
	for (i = 0; i < nx; i++)
	{
		xdot[i] = 0.0;
	}

	//derivative of road offset
	xdot[0] = sinrh*vx - cosrh*vy;
	//derivative of heading wrt road
	xdot[1] = rc * crhvxpsrhvy * oooprcro - wz;
	//derivative of curvature is zero

	//initialize the state jacobian
	for (i = 0; i < nx; i++)
	{
		for (j = 0; j < nx; j++)
		{
			Jx[midx(i, j, nx)] = 0.0;
		}
	}
	//derivatives for road offset
	Jx[midx(0, 1, nx)] = cosrh*vx + sinrh*vy;
	//derivatives for road heading
	Jx[midx(1, 0, nx)] = -rc * rc * crhvxpsrhvy * oooprcro2;
	Jx[midx(1, 1, nx)] = rc * (cosrh*vy - sinrh*vx) * oooprcro;
	Jx[midx(1, 2, nx)] = crhvxpsrhvy * oooprcro2;
	//derivatives for road curvature are all zero.

	//initialize the input jacobian
	for (i = 0; i < nx; i++)
	{
		for (j = 0; j < nv; j++)
		{
			Jv[midx(i, j, nx)] = 0.0;
		}
	}
	//input derivatives for road offset
	Jv[midx(0, 0, nx)] = 1.0;
	Jv[midx(0, 3, nx)] = sinrh;
	Jv[midx(0, 4, nx)] = -cosrh;
	//input derivatives for road heading
	Jv[midx(1, 1, nx)] = 1.0;
	Jv[midx(1, 3, nx)] = rc*cosrh * oooprcro;
	Jv[midx(1, 4, nx)] = rc*sinrh * oooprcro;
	Jv[midx(1, 5, nx)] = -1.0;
	//input derivatives for road curvature
	Jv[midx(2, 2, nx)] = 1.0;

	return;
}

void StaticTargetWithPointsDynamics(double* xdot, double* Jx, double* Jv, double* x, VehicleOdometry* iVehicleOdometry)
{
	/*
	Evaluates the dynamics function for a static target with a stored set of points

	INPUTS:
		xdot - will be populated with the state derivative
		Jx - will be populated with the state jacobian
		Jv - will be populated with the noise jacobian
		x - current state for evaluating the derivative
		iVehicleOdometry - vehicle odometry structure

	OUTPUTS:
		xdot, Jx, Jv are populated on output.
	*/

	int nx = 3;
	int nv = 6;

	int i;
	int j;

	//extract the state
	double px = x[0];
	double py = x[1];

	//extract the odometry
	double vx = iVehicleOdometry->vx;
	double vy = iVehicleOdometry->vy;
	double wz = iVehicleOdometry->wz;

	double ex = 0.0;
	double ey = 0.0;
	double eo = 0.0;
	double evx = 0.0;
	double evy = 0.0;
	double ewz = 0.0;

	//compute the state derivative
	for (i = 0; i < nx; i++)
	{
		xdot[i] = 0.0;
	}
	xdot[0] = -(vx + evx) + py*(wz + ewz) + ex;
	xdot[1] = -(vy + evy) - px*(wz + ewz) + ey;
	xdot[2] = -(wz + ewz) + eo;

	//define the state jacobian
	for (i = 0; i < nx; i++)
	{
		for (j = 0; j < nx; j++)
		{
			Jx[midx(i, j, nx)] = 0.0;
		}
	}
	Jx[midx(0, 1, nx)] = wz + ewz;
	Jx[midx(1, 0, nx)] = -(wz + ewz);

	//define the input jacobian
	for (i = 0; i < nx; i++)
	{
		for (j = 0; j < nv; j++)
		{
			Jv[midx(i, j, nx)] = 0.0;
		}
	}
	Jv[midx(0, 0, nx)] = 1.0;
	Jv[midx(0, 3, nx)] = -1.0;
	Jv[midx(0, 5, nx)] = py;
	Jv[midx(1, 1, nx)] = 1.0;
	Jv[midx(1, 4, nx)] = -1.0;
	Jv[midx(1, 5, nx)] = -px;
	Jv[midx(2, 2, nx)] = 1.0;
	Jv[midx(2, 5, nx)] = -1.0;

	return;
}

void DynamicTargetWithPointsDynamics(double* xdot, double* Jx, double* Jv, double* x, VehicleOdometry* iVehicleOdometry)
{
	/*
	Evaluates the dynamics function for a dynamic target with a stored set of points

	INPUTS:
		xdot - will be populated with the state derivative
		Jx - will be populated with the state jacobian
		Jv - will be populated with the noise jacobian
		x - current state for evaluating the derivative
		iVehicleOdometry - vehicle odometry structure

	OUTPUTS:
		xdot, Jx, Jv are populated on output.
	*/

	int nx = 5;
	int nv = 8;

	int i;
	int j;

	//extract the state
	double px = x[0];
	double py = x[1];
	double s = x[3];
	double h = x[4];

	//extract the odometry
	double vx = iVehicleOdometry->vx;
	double vy = iVehicleOdometry->vy;
	double wz = iVehicleOdometry->wz;

	double cos_h = cos(h);
	double sin_h = sin(h);

	double ex = 0.0;
	double ey = 0.0;
	double eo = 0.0;
	double es = 0.0;
	double eh = 0.0;
	double evx = 0.0;
	double evy = 0.0;
	double ewz = 0.0;

	//compute the state derivative
	for (i = 0; i < nx; i++)
	{
		xdot[i] = 0.0;
	}
	xdot[0] = s*cos_h - (vx + evx) + py*(wz + ewz) + ex;
	xdot[1] = s*sin_h - (vy + evy) - px*(wz + ewz) + ey;
	xdot[2] = -(wz + ewz) + eo;
	xdot[3] = es;
	xdot[4] = -(wz + ewz) + eh;

	//define the state jacobian
	for (i = 0; i < nx; i++)
	{
		for (j = 0; j < nx; j++)
		{
			Jx[midx(i, j, nx)] = 0.0;
		}
	}
	Jx[midx(0, 1, nx)] = wz + ewz;
	Jx[midx(0, 3, nx)] = cos_h;
	Jx[midx(0, 4, nx)] = -s*sin_h;
	Jx[midx(1, 0, nx)] = -(wz + ewz);
	Jx[midx(1, 3, nx)] = sin_h;
	Jx[midx(1, 4, nx)] = s*cos_h;

	//define the input jacobian
	for (i = 0; i < nx; i++)
	{
		for (j = 0; j < nv; j++)
		{
			Jv[midx(i, j, nx)] = 0.0;
		}
	}
	Jv[midx(0, 0, nx)] = 1.0;
	Jv[midx(0, 5, nx)] = -1.0;
	Jv[midx(0, 7, nx)] = py;
	Jv[midx(1, 1, nx)] = 1.0;
	Jv[midx(1, 6, nx)] = -1.0;
	Jv[midx(1, 7, nx)] = -px;
	Jv[midx(2, 2, nx)] = 1.0;
	Jv[midx(2, 7, nx)] = -1.0;
	Jv[midx(3, 3, nx)] = 1.0;
	Jv[midx(4, 4, nx)] = 1.0;
	Jv[midx(4, 7, nx)] = -1.0;

	return;
}

void DynamicTargetWithWidthDynamics(double* xdot, double* Jx, double* Jv, double* x, VehicleOdometry* iVehicleOdometry)
{
	/*
	Evaluates the dynamics function for a dynamic target with a width

	INPUTS:
		xdot - will be populated with the state derivative
		Jx - will be populated with the state jacobian
		Jv - will be populated with the noise jacobian
		x - current state for evaluating the derivative
		iVehicleOdometry - vehicle odometry structure

	OUTPUTS:
		xdot, Jx, Jv are populated on output.
	*/

	int nx = 5;
	int nv = 8;

	int i;
	int j;

	//extract the state
	double px = x[0];
	double py = x[1];
	double s = x[2];
	double h = x[3];

	//extract the odometry
	double vx = iVehicleOdometry->vx;
	double vy = iVehicleOdometry->vy;
	double wz = iVehicleOdometry->wz;

	double cos_h = cos(h);
	double sin_h = sin(h);

	double ex = 0.0;
	double ey = 0.0;
	double es = 0.0;
	double eh = 0.0;
	double ew = 0.0;
	double evx = 0.0;
	double evy = 0.0;
	double ewz = 0.0;

	//compute the state derivative
	for (i = 0; i < nx; i++)
	{
		xdot[i] = 0.0;
	}
	xdot[0] = s*cos_h - (vx + evx) + py*(wz + ewz) + ex;
	xdot[1] = s*sin_h - (vy + evy) - px*(wz + ewz) + ey;
	xdot[2] = es;
	xdot[3] = -(wz + ewz) + eh;
	xdot[4] = ew;

	//define the state jacobian
	for (i = 0; i < nx; i++)
	{
		for (j = 0; j < nx; j++)
		{
			Jx[midx(i, j, nx)] = 0.0;
		}
	}
	Jx[midx(0, 1, nx)] = wz + ewz;
	Jx[midx(0, 2, nx)] = cos_h;
	Jx[midx(0, 3, nx)] = -s*sin_h;
	Jx[midx(1, 0, nx)] = -(wz + ewz);
	Jx[midx(1, 2, nx)] = sin_h;
	Jx[midx(1, 3, nx)] = s*cos_h;

	//define the input jacobian
	for (i = 0; i < nx; i++)
	{
		for (j = 0; j < nv; j++)
		{
			Jv[midx(i, j, nx)] = 0.0;
		}
	}
	Jv[midx(0, 0, nx)] = 1.0;
	Jv[midx(0, 5, nx)] = -1.0;
	Jv[midx(0, 7, nx)] = py;
	Jv[midx(1, 1, nx)] = 1.0;
	Jv[midx(1, 6, nx)] = -1.0;
	Jv[midx(1, 7, nx)] = -px;
	Jv[midx(2, 2, nx)] = 1.0;
	Jv[midx(3, 3, nx)] = 1.0;
	Jv[midx(3, 7, nx)] = -1.0;
	Jv[midx(4, 4, nx)] = 1.0;

	return;
}

void DynamicTargetDynamics(double* xdot, double* Jx, double* Jv, double* x, VehicleOdometry* iVehicleOdometry)
{
	/*
	Evaluates the dynamics function for a dynamic target with no size

	INPUTS:
		xdot - will be populated with the state derivative
		Jx - will be populated with the state jacobian
		Jv - will be populated with the noise jacobian
		x - current state for evaluating the derivative
		iVehicleOdometry - vehicle odometry structure

	OUTPUTS:
		xdot, Jx, Jv are populated on output.
	*/

	int nx = 4;
	int nv = 7;

	int i;
	int j;

	//extract the state
	double px = x[0];
	double py = x[1];
	double s = x[2];
	double h = x[3];

	//extract the odometry
	double vx = iVehicleOdometry->vx;
	double vy = iVehicleOdometry->vy;
	double wz = iVehicleOdometry->wz;

	double cos_h = cos(h);
	double sin_h = sin(h);

	double ex = 0.0;
	double ey = 0.0;
	double es = 0.0;
	double eh = 0.0;
	double evx = 0.0;
	double evy = 0.0;
	double ewz = 0.0;

	//compute the state derivative
	for (i = 0; i < nx; i++)
	{
		xdot[i] = 0.0;
	}
	xdot[0] = s*cos_h - (vx + evx) + py*(wz + ewz) + ex;
	xdot[1] = s*sin_h - (vy + evy) - px*(wz + ewz) + ey;
	xdot[2] = es;
	xdot[3] = -(wz + ewz) + eh;

	//define the state jacobian
	for (i = 0; i < nx; i++)
	{
		for (j = 0; j < nx; j++)
		{
			Jx[midx(i, j, nx)] = 0.0;
		}
	}
	Jx[midx(0, 1, nx)] = wz + ewz;
	Jx[midx(0, 2, nx)] = cos_h;
	Jx[midx(0, 3, nx)] = -s*sin_h;
	Jx[midx(1, 0, nx)] = -(wz + ewz);
	Jx[midx(1, 2, nx)] = sin_h;
	Jx[midx(1, 3, nx)] = s*cos_h;

	//define the input jacobian
	for (i = 0; i < nx; i++)
	{
		for (j = 0; j < nv; j++)
		{
			Jv[midx(i, j, nx)] = 0.0;
		}
	}
	Jv[midx(0, 0, nx)] = 1.0;
	Jv[midx(0, 4, nx)] = -1.0;
	Jv[midx(0, 6, nx)] = py;
	Jv[midx(1, 1, nx)] = 1.0;
	Jv[midx(1, 5, nx)] = -1.0;
	Jv[midx(1, 6, nx)] = -px;
	Jv[midx(2, 2, nx)] = 1.0;
	Jv[midx(3, 3, nx)] = 1.0;
	Jv[midx(3, 6, nx)] = -1.0;

	return;
}

void KalmanPredict(double* xbar, double* Pbar, int nx, int nv, double dt, double* x, double* P, double* Q, VehicleOdometry* iVehicleOdometry, void (*DynamicsFunction) (double* xdot, double* Jx, double* Jv, double* x, VehicleOdometry* iVehicleOdometry))
{
	/*
	Performs one Kalman prediction step given a state and dynamics function.

	INPUTS:
		xbar, Pbar - will be populated with the predicted state and covariance on output
		nx, nv - number of states and process noises, respectively
		dt - length of prediction step
		x, P - the input state and state covariance
		Q - the process noise covariance matrix
		iVehicleOdometry - the vehicle odometry, valid over this time interval
		DynamicsFunction - function pointer to a dynamics function with the calling signature
			DynamicsFunction(xdot, Jx, Jv, x, iVehicleOdometry);

	OUTPUTS:
		xbar, Pbar - will be populated with the predcited state and covariance on output
	*/

	int i;
	int j;
	int k;
	int idx;

	int nx2 = nx*nx;
	int nxnv = nx*nv;

	//declare system matrices
	double* F = (double*) _malloca(nx2*sizeof(double));
	double* G = (double*) _malloca(nxnv*sizeof(double));

	double* xdot = (double*) _malloca(nx*sizeof(double));
	double* xcur = (double*) _malloca(nx*sizeof(double));
	double* Jx = (double*) _malloca(nx2*sizeof(double));
	double* Jv = (double*) _malloca(nxnv*sizeof(double));

	double* k1 = (double*) _malloca(nx*sizeof(double));
	double* k2 = (double*) _malloca(nx*sizeof(double));
	double* k3 = (double*) _malloca(nx*sizeof(double));
	double* k4 = (double*) _malloca(nx*sizeof(double));

	double* dF1 = (double*) _malloca(nx2*sizeof(double));
	double* dF2 = (double*) _malloca(nx2*sizeof(double));
	double* dF3 = (double*) _malloca(nx2*sizeof(double));
	double* dF4 = (double*) _malloca(nx2*sizeof(double));
	double* dG1 = (double*) _malloca(nxnv*sizeof(double));
	double* dG2 = (double*) _malloca(nxnv*sizeof(double));
	double* dG3 = (double*) _malloca(nxnv*sizeof(double));
	double* dG4 = (double*) _malloca(nxnv*sizeof(double));

	double* FP = (double*) _malloca(nx2*sizeof(double));
	double* FPFt = (double*) _malloca(nx2*sizeof(double));
	double* GQ = (double*) _malloca(nxnv*sizeof(double));
	double* GQGt = (double*) _malloca(nx2*sizeof(double));
	//NOTE: using new is too slow...

	//initialize F to identity
	memset(F, 0x00, nx2*sizeof(double));
	for (i = 0; i < nx; i++)
	{
		F[midx(i, i, nx)] = 1.0;
	}

	//initialize G to zero
	memset(G, 0x00, nxnv*sizeof(double));

	//initialize runge-kutta placeholder variables
	memset(xcur, 0x00, nx*sizeof(double));

	memset(k1, 0x00, nx*sizeof(double));
	memset(k2, 0x00, nx*sizeof(double));
	memset(k3, 0x00, nx*sizeof(double));
	memset(k4, 0x00, nx*sizeof(double));

	memset(dF1, 0x00, nx2*sizeof(double));
	memset(dF2, 0x00, nx2*sizeof(double));
	memset(dF3, 0x00, nx2*sizeof(double));
	memset(dF4, 0x00, nx2*sizeof(double));

	memset(dG1, 0x00, nxnv*sizeof(double));
	memset(dG2, 0x00, nxnv*sizeof(double));
	memset(dG3, 0x00, nxnv*sizeof(double));
	memset(dG4, 0x00, nxnv*sizeof(double));

	//initialize covariance propagation variables
	memset(FP, 0x00, nx2*sizeof(double));
	memset(FPFt, 0x00, nx2*sizeof(double));
	memset(GQ, 0x00, nxnv*sizeof(double));
	memset(GQGt, 0x00, nx2*sizeof(double));

	//begin 4th order Runge-Kutta integration
	
	//RK1:
	for (i = 0; i < nx; i++)
	{
		xcur[i] = x[i];
	}
	(*DynamicsFunction)(xdot, Jx, Jv, xcur, iVehicleOdometry);
	for (i = 0; i < nx; i++)
	{
		//k1 = dt*xdot
		k1[i] = dt * xdot[i];

		//dF1 = dt*(Jx * F)
		for (j = 0; j < nx; j++)
		{
			for (k = 0; k < nx; k++)
			{
				dF1[midx(i, j, nx)] += Jx[midx(i, k, nx)] * F[midx(k, j, nx)];
			}
			dF1[midx(i, j, nx)] *= dt;
		}

		//dG1 = dt*(Jx*G + Jv)
		for (j = 0; j < nv; j++)
		{
			for (k = 0; k < nx; k++)
			{
				dG1[midx(i, j, nx)] += Jx[midx(i, k, nx)] * G[midx(k, j, nx)];
			}
			dG1[midx(i, j, nx)] += Jv[midx(i, j, nx)];
			dG1[midx(i, j, nx)] *= dt;
		}
	}

	//RK2:
	for (i = 0; i < nx; i++)
	{
		xcur[i] = x[i] + 0.5*k1[i];
	}
	(*DynamicsFunction)(xdot, Jx, Jv, xcur, iVehicleOdometry);
	for (i = 0; i < nx; i++)
	{
		//k2 = dt*xdot
		k2[i] = dt * xdot[i];

		//dF2 = dt*Jx*(F + 0.5*dF1)
		for (j = 0; j < nx; j++)
		{
			for (k = 0; k < nx; k++)
			{
				dF2[midx(i, j, nx)] += Jx[midx(i, k, nx)] * (F[midx(k, j, nx)] + 0.5*dF1[midx(k, j, nx)]);
			}
			dF2[midx(i, j, nx)] *= dt;
		}

		//dG2 = dt*(Jx*(G + 0.5*dG1) + Jv)
		for (j = 0; j < nv; j++)
		{
			for (k = 0; k < nx; k++)
			{
				dG2[midx(i, j, nx)] += Jx[midx(i, k, nx)] * (G[midx(k, j, nx)] + 0.5*dG1[midx(k, j, nx)]);
			}
			dG2[midx(i, j, nx)] += Jv[midx(i, j, nx)];
			dG2[midx(i, j, nx)] *= dt;
		}
	}

	//RK3:
	for (i = 0; i < nx; i++)
	{
		xcur[i] = x[i] + 0.5*k2[i];
	}
	(*DynamicsFunction)(xdot, Jx, Jv, xcur, iVehicleOdometry);
	for (i = 0; i < nx; i++)
	{
		//k3 = dt*xdot
		k3[i] = dt * xdot[i];

		//dF3 = dt*Jx*(F + 0.5*dF2)
		for (j = 0; j < nx; j++)
		{
			for (k = 0; k < nx; k++)
			{
				dF3[midx(i, j, nx)] += Jx[midx(i, k, nx)] * (F[midx(k, j, nx)] + 0.5*dF2[midx(k, j, nx)]);
			}
			dF3[midx(i, j, nx)] *= dt;
		}

		//dG3 = dt*(Jx*(G + 0.5*dG2) + Jv)
		for (j = 0; j < nv; j++)
		{
			for (k = 0; k < nx; k++)
			{
				dG3[midx(i, j, nx)] += Jx[midx(i, k, nx)] * (G[midx(k, j, nx)] + 0.5*dG2[midx(k, j, nx)]);
			}
			dG3[midx(i, j, nx)] += Jv[midx(i, j, nx)];
			dG3[midx(i, j, nx)] *= dt;
		}
	}

	//RK4:
	for (i = 0; i < nx; i++)
	{
		xcur[i] = x[i] + k3[i];
	}
	(*DynamicsFunction)(xdot, Jx, Jv, xcur, iVehicleOdometry);
	for (i = 0; i < nx; i++)
	{
		//k4 = dt*xdot
		k4[i] = dt * xdot[i];

		//dF4 = dt*Jx*(F + dF3)
		for (j = 0; j < nx; j++)
		{
			for (k = 0; k < nx; k++)
			{
				dF4[midx(i, j, nx)] += Jx[midx(i, k, nx)] * (F[midx(k, j, nx)] + dF3[midx(k, j, nx)]);
			}
			dF4[midx(i, j, nx)] *= dt;
		}

		//dG4 = dt*(Jx*(G + dG3) + Jv)
		for (j = 0; j < nv; j++)
		{
			for (k = 0; k < nx; k++)
			{
				dG4[midx(i, j, nx)] += Jx[midx(i, k, nx)] * (G[midx(k, j, nx)] + dG3[midx(k, j, nx)]);
			}
			dG4[midx(i, j, nx)] += Jv[midx(i, j, nx)];
			dG4[midx(i, j, nx)] *= dt;
		}
	}

	//combine RK steps into prediction
	double oos = 1.0/6.0;
	for (i = 0; i < nx; i++)
	{
		xbar[i] = x[i] + (k1[i] + 2.0*k2[i] + 2.0*k3[i] + k4[i])*oos;

		for (j = 0; j < nx; j++)
		{
			idx = midx(i, j, nx);
			F[idx] += (dF1[idx] + 2.0*dF2[idx] + 2.0*dF3[idx] + dF4[idx])*oos;
		}

		for (j = 0; j < nv; j++)
		{
			idx = midx(i, j, nx);
			G[idx] += (dG1[idx] + 2.0*dG2[idx] + 2.0*dG3[idx] + dG4[idx])*oos;
		}
	}

	//calculate Pbar = F*P*F' + G*Q*G'
	for (i = 0; i < nx; i++)
	{
		for (j = 0; j < nx; j++)
		{
			idx = midx(i, j, nx);
			for (k = 0; k < nx; k++)
			{
				FP[idx] += F[midx(i, k, nx)] * P[midx(k, j, nx)];
			}
		}
	}

	for (i = 0; i < nx; i++)
	{
		for (j = 0; j < nx; j++)
		{
			idx = midx(i, j, nx);
			for (k = 0; k < nx; k++)
			{
				FPFt[idx] += FP[midx(i, k, nx)] * F[midx(j, k, nx)];
			}
		}
	}

	for (i = 0; i < nx; i++)
	{
		for (j = 0; j < nv; j++)
		{
			idx = midx(i, j, nx);
			for (k = 0; k < nv; k++)
			{
				GQ[idx] += G[midx(i, k, nx)] * Q[midx(k, j, nv)];
			}
		}
	}

	for (i = 0; i < nx; i++)
	{
		for (j = 0; j < nx; j++)
		{
			idx = midx(i, j, nx);
			for (k = 0; k < nv; k++)
			{
				GQGt[idx] += GQ[midx(i, k, nx)] * G[midx(j, k, nx)];
			}
		}
	}

	for (i = 0; i < nx; i++)
	{
		for (j = 0; j < nx; j++)
		{
			idx = midx(i, j, nx);
			Pbar[idx] = FPFt[idx] + GQGt[idx];
		}
	}

	//NOTE: BLAS is slow!

	/*
	//RK1:
	//xcur = x
	dcopy(nx, x, 1, xcur, 1);
	(*DynamicsFunction)(xdot, Jx, Jv, xcur, iVehicleOdometry);
	//k1 = dt*xdot
	daxpy(nx, dt, xdot, 1, k1, 1);
	//dF1 = dt*(Jx * F)
	dgemm('N', 'N', nx, nx, nx, dt, Jx, nx, F, nx, 0.0, dF1, nx);
	//dG1 = dt*(Jx*G + Jv)
	dcopy(nxnv, Jv, 1, dG1, 1);
	dgemm('N', 'N', nx, nv, nx, dt, Jx, nx, G, nx, dt, dG1, nx);

	//RK2:
	//xcur = x + 0.5*k1
	dcopy(nx, x, 1, xcur, 1);
	daxpy(nx, 0.5, k1, 1, xcur, 1);
	(*DynamicsFunction)(xdot, Jx, Jv, xcur, iVehicleOdometry);
	//k2 = dt*xdot
	daxpy(nx, dt, xdot, 1, k2, 1);
	//dF2 = dt*Jx*(F + 0.5*dF1)
	dgemm('N', 'N', nx, nx, nx, 1.0, Jx, nx, dF1, nx, 0.0, dF2, nx);
	dgemm('N', 'N', nx, nx, nx, dt, Jx, nx, F, nx, 0.5*dt, dF2, nx);
	//dG2 = dt*(Jx*(G + 0.5*dG1) + Jv)
	dcopy(nxnv, Jv, 1, dG2, 1);
	dgemm('N', 'N', nx, nv, nx, 0.5, Jx, nx, dG1, nx, 1.0, dG2, nx);
	dgemm('N', 'N', nx, nv, nx, dt, Jx, nx, G, nx, dt, dG2, nx);

	//RK3:
	//xcur = x + 0.5*k2
	dcopy(nx, x, 1, xcur, 1);
	daxpy(nx, 0.5, k2, 1, xcur, 1);
	(*DynamicsFunction)(xdot, Jx, Jv, xcur, iVehicleOdometry);
	//k3 = dt*xdot
	daxpy(nx, dt, xdot, 1, k3, 1);
	//dF3 = dt*Jx*(F + 0.5*dF2)
	dgemm('N', 'N', nx, nx, nx, 1.0, Jx, nx, dF2, nx, 0.0, dF3, nx);
	dgemm('N', 'N', nx, nx, nx, dt, Jx, nx, F, nx, 0.5*dt, dF3, nx);
	//dG3 = dt*(Jx*(G + 0.5*dG2) + Jv)
	dcopy(nxnv, Jv, 1, dG3, 1);
	dgemm('N', 'N', nx, nv, nx, 0.5, Jx, nx, dG2, nx, 1.0, dG3, nx);
	dgemm('N', 'N', nx, nv, nx, dt, Jx, nx, G, nx, dt, dG3, nx);

	//RK4:
	//xcur = x + k3
	dcopy(nx, x, 1, xcur, 1);
	daxpy(nx, 1.0, k3, 1, xcur, 1);
	(*DynamicsFunction)(xdot, Jx, Jv, xcur, iVehicleOdometry);
	//k4 = dt*xdot
	daxpy(nx, dt, xdot, 1, k4, 1);
	//dF4 = dt*Jx*(F + dF3)
	dgemm('N', 'N', nx, nx, nx, 1.0, Jx, nx, dF3, nx, 0.0, dF4, nx);
	dgemm('N', 'N', nx, nx, nx, dt, Jx, nx, F, nx, dt, dF4, nx);
	//dG4 = dt*(Jx*(G + dG3) + Jv)
	dcopy(nxnv, Jv, 1, dG4, 1);
	dgemm('N', 'N', nx, nv, nx, 1.0, Jx, nx, dG3, nx, 1.0, dG4, nx);
	dgemm('N', 'N', nx, nv, nx, dt, Jx, nx, G, nx, dt, dG4, nx);

	//combine RK steps into prediction
	double oos = 1.0/6.0;
	//xbar = x + (k1 + 2*k2 + 2*k3 + k4)/6
	dcopy(nx, x, 1, xbar, 1);
	daxpy(nx, oos, k1, 1, xbar, 1);
	daxpy(nx, 2.0*oos, k2, 1, xbar, 1);
	daxpy(nx, 2.0*oos, k3, 1, xbar, 1);
	daxpy(nx, oos, k4, 1, xbar, 1);
	//F = F + (dF1 + 2*dF2 + 2*dF3 + dF4)/6
	daxpy(nx2, oos, dF1, 1, F, 1);
	daxpy(nx2, 2.0*oos, dF2, 1, F, 1);
	daxpy(nx2, 2.0*oos, dF3, 1, F, 1);
	daxpy(nx2, oos, dF4, 1, F, 1);
	//G = G + (dG1 + 2*dG2 + 2*dG3 + dG4)/6
	daxpy(nxnv, oos, dG1, 1, G, 1);
	daxpy(nxnv, 2.0*oos, dG2, 1, G, 1);
	daxpy(nxnv, 2.0*oos, dG3, 1, G, 1);
	daxpy(nxnv, oos, dG4, 1, G, 1);

	//calculate Pbar = F*P*F' + G*Q*G'
	dgemm('N', 'N', nx, nx, nx, 1.0, F, nx, P, nx, 0.0, FP, nx);
	dgemm('N', 'T', nx, nx, nx, 1.0, FP, nx, F, nx, 0.0, FPFt, nx);
	dgemm('N', 'N', nx, nv, nv, 1.0, G, nx, Q, nv, 0.0, GQ, nx);
	dgemm('N', 'T', nx, nx, nv, 1.0, GQ, nx, G, nx, 0.0, GQGt, nx);
	dcopy(nx2, FPFt, 1, Pbar, 1);
	daxpy(nx2, 1.0, GQGt, 1, Pbar, 1);
	*/

	//free system matrices
	_freea(F);
	_freea(G);

	_freea(xdot);
	_freea(xcur);
	_freea(Jx);
	_freea(Jv);

	_freea(k1);
	_freea(k2);
	_freea(k3);
	_freea(k4);

	_freea(dF1);
	_freea(dF2);
	_freea(dF3);
	_freea(dF4);
	_freea(dG1);
	_freea(dG2);
	_freea(dG3);
	_freea(dG4);

	_freea(FP);
	_freea(FPFt);
	_freea(GQ);
	_freea(GQGt);

	return;
}

double KalmanLikelihood(double* nu, double* S, double* W, int nx, int nz, double* z, double* R, double* H, double* xbar, double* Pbar, double iKalmanGate)
{
	/*
	Computes the Kalman likelihood function for the measurement z as well as the 
	measurement covariance and the Kalman gain matrix for a particular measurement,
	assuming a linear measurement model.

	INPUTS:
		nu - nz x 1 element preallocated vector that will be set with the innovation on output
		S - nz x nz preallocated matrix that will be set with the measurement covariance on output
		W - nx x nz preallocated matrix that will be set with the Kalman gain on output
		nz, nx - length of the measurement and state vector, respectively
		z - nz x 1 element vector containing the measurement
		R - nz x nz measurement covariance matrix 
		H - nx x nz measurement mapping matrix
		xbar - nx x 1 vector containing the current predicted state
		Pbar - nx x nx matrix containing the predicted state covariance
		iKalmanGate - an optional gating threshold to be applied to the innovation statistic.
			If the statistic fails the gate, rLambda = 0 is returned.

	OUTPUTS:
		rLambda - returns the measurement likelihood evaluated at N(nu, 0, S) if all
			matrix math is successful.  Returns 0 if unsuccessful.  If successful,
			nu, S, and W are populated.
	*/

	double rLambda = 0.0;

	if (nx <= 0 || nz <= 0)
	{
		//invalid state dimensions, so return
		printf("Warning: KalmanLikelihood called with nz = %d and nx = %d.\n", nz, nx);
		return rLambda;
	}

	int i;
	int j;
	int k;

	//preallocate space
	double* PbarHt = (double*) _malloca(nx*nz*sizeof(double));
	double* invS = (double*) _malloca(nz*nz*sizeof(double));
	int* ipiv = (int*) _malloca(nz*sizeof(int));
	int info;

	//initialize output arguments
	for (i = 0; i < nz; i++)
	{
		nu[i] = 0.0;

		for (j = 0; j < nz; j++)
		{
			S[midx(i, j, nz)] = 0.0;
		}
	}
	for (i = 0; i < nx; i++)
	{
		for (j = 0; j < nz; j++)
		{
			W[midx(i, j, nx)] = 0.0;
		}
	}

	//compute nu = z - H*xbar
	for (i = 0; i < nz; i++)
	{
		nu[i] = z[i];
		for (j = 0; j < nx; j++)
		{
			nu[i] -= H[midx(i, j, nz)] * xbar[j];
		}
	}

	//compute S = HPbarHt + R
	for (i = 0; i < nx; i++)
	{
		for (j = 0; j < nz; j++)
		{
			//this loop computes PbarHt, which is also a partial computation of W
			PbarHt[midx(i, j, nx)] = 0.0;
			for (k = 0; k < nx; k++)
			{
				PbarHt[midx(i, j, nx)] += Pbar[midx(i, k, nx)] * H[midx(j, k, nz)];
			}
		}
	}
	for (i = 0; i < nz; i++)
	{
		for (j = 0; j < nz; j++)
		{
			S[midx(i, j, nz)] = R[midx(i, j, nz)];
			for (k = 0; k < nx; k++)
			{
				S[midx(i, j, nz)] += H[midx(i, k, nz)] * PbarHt[midx(k, j, nx)];
			}
		}
	}
	//copy S into invS
	for (i = 0; i < nz; i++)
	{
		for (j = 0; j < nz; j++)
		{
			invS[midx(i, j, nz)] = S[midx(i, j, nz)];
		}
	}

	//invert S
	//LU decomposition for matrix inversion
	dgetrf(nz, nz, invS, nz, ipiv, &info);
	if (info != 0)
	{
		_freea(PbarHt);
		_freea(invS);
		_freea(ipiv);

		printf("Warning: dgetrf error in KalmanLikelihood.\n");
		rLambda = 0.0;
		return rLambda;
	}
	//calculate the determinant of S before destroying the LU decomposition
	double detS = 1.0;
	for (i = 0; i < nz; i++)
	{
		if (ipiv[i] > i+1)
		{
			//negate the determinant because a row pivot took place
			detS *= -invS[midx(i, i, nz)];
		}
		else
		{
			//don't negate the determinant because the ith row either wasn't pivoted
			//or it was pivoted (but we counted it already)
			detS *= invS[midx(i, i, nz)];
		}
	}
	//invert S and store in invS
	dgetri(nz, invS, nz, ipiv, &info);
	if (info != 0)
	{
		_freea(PbarHt);
		_freea(invS);
		_freea(ipiv);

		printf("Warning: dgetri error in KalmanLikelihood.\n");
		rLambda = 0.0;
		return rLambda;
	}

	//calculate W = PbarHt*invS
	for (i = 0; i < nx; i++)
	{
		for (j = 0; j < nz; j++)
		{
			W[midx(i, j, nx)] = 0.0;
			for (k = 0; k < nz; k++)
			{
				W[midx(i, j, nx)] += PbarHt[midx(i, k, nx)] * invS[midx(k, j, nz)];
			}
		}
	}

	//calculate likelihood: 
	//rLambda = exp(-0.5*nu'*inv(S)*nu) / sqrt((2*pi)^(length(nu))*det(S))
	for (i = 0; i < nz; i++)
	{
		for (j = 0; j < nz; j++)
		{
			rLambda += nu[i] * invS[midx(i, j, nz)] * nu[j];
		}
	}
	if (rLambda > iKalmanGate)
	{
		_freea(PbarHt);
		_freea(invS);
		_freea(ipiv);

		rLambda = 0.0;
		return rLambda;
	}
	//rLambda = exp(-0.5*rLambda) / sqrt(pow(TWOPI, (double) nz) * detS);
	//do this instead for a little bit more numerical robustness
	rLambda = exp(-0.5*rLambda - 0.5*((double) nz)*LNTWOPI - 0.5*log(detS));

	_freea(PbarHt);
	_freea(invS);
	_freea(ipiv);

	return rLambda;
}

void KalmanUpdate(double* xhat, double* Phat, int nx, int nz, double* xbar, double* Pbar, double* nu, double* S, double* W)
{
	/*
	Performs a standard Kalman update, assuming the innovation has already been computed.

	INPUTS:
		xhat, Phat - will be populated with the updated state and covariance
		nx, nz - number of states and measurements, respectively
		xbar, Pbar - the predicted state and covariance
		nu - the measurement innovation
		S - the innovation covariance matrix
		W - the Kalman gain matrix

	OUTPUTS:
		xhat, Phat - populated with updated state and covariance matrix
	*/

	int i;
	int j;
	int k;

	//form the updated state: xhat = xbar + W*nu
	for (i = 0; i < nx; i++)
	{
		xhat[i] = xbar[i];
		for (j = 0; j < nz; j++)
		{
			xhat[i] += W[midx(i, j, nx)] * nu[j];
		}
	}

	//form the updated covariance matrix Phat = Pbar - W*S*W'
	double* WS = (double*) _malloca(nx*nz*sizeof(double));

	for (i = 0; i < nx; i++)
	{
		for (j = 0; j < nz; j++)
		{
			WS[midx(i, j, nx)] = 0.0;
			for (k = 0; k < nz; k++)
			{
				WS[midx(i, j, nx)] += W[midx(i, k, nx)] * S[midx(k, j, nz)];
			}
		}
	}

	for (i = 0; i < nx; i++)
	{
		for (j = 0; j < nx; j++)
		{
			Phat[midx(i, j, nx)] = Pbar[midx(i, j, nx)];
			for (k = 0; k < nz; k++)
			{
				Phat[midx(i, j, nx)] -= WS[midx(i, k, nx)] * W[midx(j, k, nx)];
			}
		}
	}

	_freea(WS);

	return;
}

void BcwBccwRminClusterMeasurement(double* z, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument)
{
	/*
	Computes the expected measurement for a cluster of points.

	INPUTS:
		z - will be populated with the cluster measurement, [CWOB, CCWOB, RCP]
		nx - length of the state vector x
		x - the object's state [xref, yref, oref, sref, href, ...]
		iSensor - the sensor used to generate the measurements
		iVehicleOdometry - the vehicle odometry structure
		iArgument - pointer to a cluster structure containing the cluster information

	OUTPUTS:
		z - populated with the cluster measurement.
	*/

	//initialize the measurements
	z[0] = DBL_MAX;
	z[1] = -DBL_MAX;
	z[2] = DBL_MAX;

	//extract the useful portions of the state
	double xref = x[0];
	double yref = x[1];
	double oref = x[2];

	//cast the optional argument as a cluster
	Cluster* iCluster = (Cluster*) (iArgument);
	double* iClusterPoints = iCluster->Points;

	double cosOrient = cos(oref);
	double sinOrient = sin(oref);
	double cosSyaw = cos(iSensor->SensorYaw);
	double sinSyaw = sin(iSensor->SensorYaw);
	double sx = iSensor->SensorX;
	double sy = iSensor->SensorY;

	int i;
	int np = iCluster->NumPoints;
	//wrap all bearings to the bearing of the anchor point in sensor coordinates
	double scxref;
	double scyref;
	EgoVehicleToSensor(scxref, scyref, xref, yref, cosSyaw, sinSyaw, sx, sy);
	double wraptarget = atan2(scyref, scxref);

	//calculate components of x-target direction (toward anchor point) in sensor coordinates
	double xtx = scxref;
	double xty = scyref;
	double ntx = sqrt(xtx*xtx + xty*xty);
	if (fabs(ntx) == 0.0)
	{
		xtx = 1.0;
		xty = 0.0;
	}
	else
	{
		double oontx = 1.0/ntx;
		xtx *= oontx;
		xty *= oontx;
	}
	//calculate components of y-target direction
	double ytx = -xty;
	double yty = xtx;

	//these will store the locations of the extreme bearing points
	double maxs = -DBL_MAX;
	double maxscx;
	double maxscy;
	double mins = DBL_MAX;
	double minscx;
	double minscy;

	for (i = 0; i < np; i++)
	{
		//transform the cluster point from object storage to ego-vehicle coordinates
		double px = iClusterPoints[midx(i, 0, np)];
		double py = iClusterPoints[midx(i, 1, np)];
		//note when converting to sensor coordinates, rotate the coordinate frame
		double evx;
		double evy;
		ObjectToEgoVehicle(evx, evy, px, py, cosOrient, sinOrient, xref, yref);
		//transform from ego-vehicle to sensor coordinates
		double scx;
		double scy;
		EgoVehicleToSensor(scx, scy, evx, evy, cosSyaw, sinSyaw, sx, sy);

		//compute the point's range
		double rng = sqrt(scx*scx + scy*scy);

		//project each target point into the target's coordinates to see if it is
		//an extreme point (arctan will be computed later only on these points)
		double pxp = xtx*scx + xty*scy;
		double pyp = ytx*scx + yty*scy;

		if (pxp > 0.0)
		{
			//if the slope can be calculated legitimately, use max and min slope to find extreme points
			double curs = pyp/pxp;
			if (curs > maxs)
			{
				maxs = curs;
				maxscx = scx;
				maxscy = scy;
			}
			if (curs < mins)
			{
				mins = curs;
				minscx = scx;
				minscy = scy;
			}
		}
		else
		{
			//a weird target that wraps over more than +-90o; have to calculate bearings brute force
			//NOTE: this is an odd case that doesn't happen too much

			//wrap the angle to the same 2pi branch
			double ang = WrapAngle(atan2(scy, scx), wraptarget);
			if (ang < z[0])
			{
				z[0] = ang;
			}
			if (ang > z[1])
			{
				z[1] = ang;
			}
		}

		//compare range to the measurements found so far
		if (rng < z[2])
		{
			z[2] = rng;
		}
	}

	//calculate max and min bearing from all the normal points
	if (fabs(maxs) != DBL_MAX)
	{
		double ang = atan2(maxscy, maxscx);
		ang = WrapAngle(ang, wraptarget);
		if (ang < z[0])
		{
			z[0] = ang;
		}
		if (ang > z[1])
		{
			z[1] = ang;
		}
	}
	if (fabs(mins) != DBL_MAX)
	{
		double ang = atan2(minscy, minscx);
		ang = WrapAngle(ang, wraptarget);
		if (ang < z[0])
		{
			z[0] = ang;
		}
		if (ang > z[1])
		{
			z[1] = ang;
		}
	}

	return;
}

void BcwBccwRminMeasurement(double* z, double* H, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument)
{
	/*
	Computes the expected bounding points measurement for an object with no cluster data.

	INPUTS:
		z - will be populated with the measurement, [cwob, ccwob, rcp]
		H - will be populated with the jacobian of the measurement
		nx - length of the state vector x
		x - the object's state [x, y, s, h, w, ...]
		iSensor - the sensor used to generate the measurements
		iVehicleOdometry - the vehicle odometry structure
		iArgument - not used

	OUTPUTS:
		z - populated with the cluster measurement.
	*/

	int i;
	int j;
	int nz = 3;

	//initialize the output arguments
	for (i = 0; i < nz; i++)
	{
		z[i] = 0.0;
		for (j = 0; j < nx; j++)
		{
			H[midx(i, j, nz)] = 0.0;
		}
	}

	//extract the state
	double tx = x[0];
	double ty = x[1];
	double ts = x[2];
	double th = x[3];
	double tw = x[4];

	double cosTh = cos(th);
	double sinTh = sin(th);

	//calculate obstacle position in sensor coordinates
	double cosSyaw = cos(iSensor->SensorYaw);
	double sinSyaw = sin(iSensor->SensorYaw);
	double sx = iSensor->SensorX;
	double sy = iSensor->SensorY;

	double tsx;
	double tsy;
	EgoVehicleToSensor(tsx, tsy, tx, ty, cosSyaw, sinSyaw, sx, sy);
	//calculate the target's range and bearing in sensor coordinates
	double tsr = sqrt(tsx*tsx + tsy*tsy);
	double tsb = atan2(tsy, tsx);
	//calculate the target's half-angular spread
	double tsda = fabs(atan(0.5*tw / tsr));
	//set the occlusion boundary measurements
	z[0] = tsb - tsda;
	z[1] = tsb + tsda;
	//set the target range as the range to the closest point
	z[2] = tsr;

	//populate the jacobian matrix
	if (fabs(tsr) > 0.0)
	{
		double twotsr = tw / tsr;
		double ootsr = 1.0/tsr;
		double tsr2 = tsr*tsr;
		double ootsr2 = 1.0 / tsr2;
		double ootsr2ptw2 = 1.0 / (tsr2 + tw*tw);

		H[midx(0, 0, nz)] = -ootsr2*(tsx*sinSyaw + tsy*cosSyaw) + ootsr2ptw2*(twotsr*(tsx*cosSyaw - tsy*sinSyaw));
		H[midx(0, 1, nz)] = ootsr2*(tsx*cosSyaw - tsy*sinSyaw) + ootsr2ptw2*(twotsr*(tsx*sinSyaw + tsy*cosSyaw));
		H[midx(0, 4, nz)] = -ootsr2ptw2*tsr;
		H[midx(1, 0, nz)] = -ootsr2*(tsx*sinSyaw + tsy*cosSyaw) - ootsr2ptw2*(twotsr*(tsx*cosSyaw - tsy*sinSyaw));
		H[midx(1, 1, nz)] = ootsr2*(tsx*cosSyaw - tsy*sinSyaw) - ootsr2ptw2*(twotsr*(tsx*sinSyaw + tsy*cosSyaw));
		H[midx(1, 4, nz)] = ootsr2ptw2*tsr;
		H[midx(2, 0, nz)] = ootsr*(tsx*cosSyaw - tsy*sinSyaw);
		H[midx(2, 1, nz)] = ootsr*(tsx*sinSyaw + tsy*cosSyaw);
	}
	//NOTE: if tsr == 0, the entire jacobian is 0.

	return;
}

void BcwBccwRminNoWidthMeasurement(double* z, double* H, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument)
{
	/*
	Computes the expected bounding points measurement for an object with no cluster data
	and no width... where the bounding points are converted to an average range and bearing.

	INPUTS:
		z - will be populated with the measurement, [bavg, rcp]
		H - will be populated with the jacobian of the measurement
		nx - length of the state vector x
		x - the object's state [x, y, s, h, ...]
		iSensor - the sensor used to generate the measurements
		iVehicleOdometry - the vehicle odometry structure
		iArgument - not used

	OUTPUTS:
		z - populated with the cluster measurement.
	*/

	int i;
	int j;
	int nz = 2;

	//initialize the output arguments
	for (i = 0; i < nz; i++)
	{
		z[i] = 0.0;
		for (j = 0; j < nx; j++)
		{
			H[midx(i, j, nz)] = 0.0;
		}
	}

	//extract the state
	double tx = x[0];
	double ty = x[1];
	double ts = x[2];
	double th = x[3];

	double cosTh = cos(th);
	double sinTh = sin(th);

	//calculate target position in sensor coordinates
	double cosSyaw = cos(iSensor->SensorYaw);
	double sinSyaw = sin(iSensor->SensorYaw);
	double sx = iSensor->SensorX;
	double sy = iSensor->SensorY;

	double tsx;
	double tsy;
	EgoVehicleToSensor(tsx, tsy, tx, ty, cosSyaw, sinSyaw, sx, sy);
	//calculate the target's range and bearing in sensor coordinates
	double tsr = sqrt(tsx*tsx + tsy*tsy);
	double tsb = atan2(tsy, tsx);
	//store these as the measurements: tsb represents the average
	//of the occlusion boundaries, and tsr the range.
	z[0] = tsb;
	z[1] = tsr;

	//populate the jacobian matrix
	if (fabs(tsr) > 0.0)
	{
		double ootsr = 1.0/tsr;
		double tsr2 = tsr*tsr;
		double ootsr2 = ootsr*ootsr;

		H[midx(0, 0, nz)] = -ootsr2*(tsx*sinSyaw + tsy*cosSyaw);
		H[midx(0, 1, nz)] = ootsr2*(tsx*cosSyaw - tsy*sinSyaw);
		H[midx(1, 0, nz)] = ootsr*(tsx*cosSyaw - tsy*sinSyaw);
		H[midx(1, 1, nz)] = ootsr*(tsx*sinSyaw + tsy*cosSyaw);
	}
	//NOTE: if tsr == 0, the entire jacobian is 0.

	return;
}

void BcwMeasurement(double* z, double* H, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument)
{
	/*
	Computes the expected clockwise bounding point measurement for an object with no cluster data.

	INPUTS:
		z - will be populated with the measurement, [cwob]
		H - will be populated with the jacobian of the measurement
		nx - length of the state vector x
		x - the object's state [x, y, s, h, w, ...]
		iSensor - the sensor used to generate the measurements
		iVehicleOdometry - the vehicle odometry structure
		iArgument - not used.

	OUTPUTS:
		z - populated with the cluster measurement.
	*/

	int i;
	int j;
	int nz = 1;

	//initialize the output arguments
	for (i = 0; i < nz; i++)
	{
		z[i] = 0.0;
		for (j = 0; j < nx; j++)
		{
			H[midx(i, j, nz)] = 0.0;
		}
	}

	//extract the state
	double tx = x[0];
	double ty = x[1];
	double ts = x[2];
	double th = x[3];
	double tw = x[4];

	double cosTh = cos(th);
	double sinTh = sin(th);

	//calculate obstacle position in sensor coordinates
	double cosSyaw = cos(iSensor->SensorYaw);
	double sinSyaw = sin(iSensor->SensorYaw);
	double sx = iSensor->SensorX;
	double sy = iSensor->SensorY;

	double tsx;
	double tsy;
	EgoVehicleToSensor(tsx, tsy, tx, ty, cosSyaw, sinSyaw, sx, sy);
	//calculate the target's range and bearing in sensor coordinates
	double tsr = sqrt(tsx*tsx + tsy*tsy);
	double tsb = atan2(tsy, tsx);
	//calculate the target's half-angular spread
	double tsda = fabs(atan(0.5*tw / tsr));
	//set the occlusion boundary measurements
	z[0] = tsb - tsda;

	//populate the jacobian matrix
	if (fabs(tsr) > 0.0)
	{
		double twotsr = tw / tsr;
		double ootsr = 1.0/tsr;
		double tsr2 = tsr*tsr;
		double ootsr2 = 1.0 / tsr2;
		double ootsr2ptw2 = 1.0 / (tsr2 + tw*tw);

		H[midx(0, 0, nz)] = -ootsr2*(tsx*sinSyaw + tsy*cosSyaw) + ootsr2ptw2*(twotsr*(tsx*cosSyaw - tsy*sinSyaw));
		H[midx(0, 1, nz)] = ootsr2*(tsx*cosSyaw - tsy*sinSyaw) + ootsr2ptw2*(twotsr*(tsx*sinSyaw + tsy*cosSyaw));
		H[midx(0, 4, nz)] = -ootsr2ptw2*tsr;
	}
	//NOTE: if tsr == 0, the entire jacobian is 0.

	return;
}

void BccwMeasurement(double* z, double* H, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument)
{
	/*
	Computes the expected bounding points measurement for an object with no cluster data.

	INPUTS:
		z - will be populated with the measurement, [cwob, ccwob, rcp]
		H - will be populated with the jacobian of the measurement
		nx - length of the state vector x
		x - the object's state [x, y, s, h, w, ...]
		iSensor - the sensor used to generate the measurements
		iVehicleOdometry - the vehicle odometry structure
		iArgument - not used

	OUTPUTS:
		z - populated with the cluster measurement.
	*/

	int i;
	int j;
	int nz = 1;

	//initialize the output arguments
	for (i = 0; i < nz; i++)
	{
		z[i] = 0.0;
		for (j = 0; j < nx; j++)
		{
			H[midx(i, j, nz)] = 0.0;
		}
	}

	//extract the state
	double tx = x[0];
	double ty = x[1];
	double ts = x[2];
	double th = x[3];
	double tw = x[4];

	double cosTh = cos(th);
	double sinTh = sin(th);

	//calculate obstacle position in sensor coordinates
	double cosSyaw = cos(iSensor->SensorYaw);
	double sinSyaw = sin(iSensor->SensorYaw);
	double sx = iSensor->SensorX;
	double sy = iSensor->SensorY;

	double tsx;
	double tsy;
	EgoVehicleToSensor(tsx, tsy, tx, ty, cosSyaw, sinSyaw, sx, sy);
	//calculate the target's range and bearing in sensor coordinates
	double tsr = sqrt(tsx*tsx + tsy*tsy);
	double tsb = atan2(tsy, tsx);
	//calculate the target's half-angular spread
	double tsda = fabs(atan(0.5*tw / tsr));
	//set the occlusion boundary measurements
	z[0] = tsb + tsda;

	//populate the jacobian matrix
	if (fabs(tsr) > 0.0)
	{
		double twotsr = tw / tsr;
		double ootsr = 1.0/tsr;
		double tsr2 = tsr*tsr;
		double ootsr2 = 1.0 / tsr2;
		double ootsr2ptw2 = 1.0 / (tsr2 + tw*tw);

		H[midx(0, 0, nz)] = -ootsr2*(tsx*sinSyaw + tsy*cosSyaw) - ootsr2ptw2*(twotsr*(tsx*cosSyaw - tsy*sinSyaw));
		H[midx(0, 1, nz)] = ootsr2*(tsx*cosSyaw - tsy*sinSyaw) - ootsr2ptw2*(twotsr*(tsx*sinSyaw + tsy*cosSyaw));
		H[midx(0, 4, nz)] = ootsr2ptw2*tsr;
	}
	//NOTE: if tsr == 0, the entire jacobian is 0.

	return;
}

void BcwClusterMeasurement(double* z, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument)
{
	/*
	Computes the expected measurement for a cluster of points with only the cw side visible

	INPUTS:
		z - will be populated with the cluster measurement, [CWOB]
		nx - length of the state vector x
		x - the object's state [xref, yref, oref, sref, href, ...]
		iSensor - the sensor used to generate the measurements
		iVehicleOdometry - the vehicle odometry structure
		iArgument - pointer to a cluster structure containing the cluster information

	OUTPUTS:
		z - populated with the cluster measurement.
	*/

	//initialize the measurements
	z[0] = DBL_MAX;

	//extract the useful portions of the state
	double xref = x[0];
	double yref = x[1];
	double oref = x[2];

	//cast the optional argument as a cluster
	Cluster* iCluster = (Cluster*) (iArgument);
	double* iClusterPoints = iCluster->Points;

	double cosOrient = cos(oref);
	double sinOrient = sin(oref);
	double cosSyaw = cos(iSensor->SensorYaw);
	double sinSyaw = sin(iSensor->SensorYaw);
	double sx = iSensor->SensorX;
	double sy = iSensor->SensorY;

	int i;
	int np = iCluster->NumPoints;
	//wrap all bearings to the bearing of the anchor point in sensor coordinates
	double scxref;
	double scyref;
	EgoVehicleToSensor(scxref, scyref, xref, yref, cosSyaw, sinSyaw, sx, sy);
	double wraptarget = atan2(scyref, scxref);

	//calculate components of x-target direction (toward anchor point) in sensor coordinates
	double xtx = scxref;
	double xty = scyref;
	double ntx = sqrt(xtx*xtx + xty*xty);
	if (fabs(ntx) == 0.0)
	{
		xtx = 1.0;
		xty = 0.0;
	}
	else
	{
		double oontx = 1.0/ntx;
		xtx *= oontx;
		xty *= oontx;
	}
	//calculate components of y-target direction
	double ytx = -xty;
	double yty = xtx;

	//these will store the locations of the extreme bearing points
	double maxs = -DBL_MAX;
	double maxscx;
	double maxscy;
	double mins = DBL_MAX;
	double minscx;
	double minscy;

	for (i = 0; i < np; i++)
	{
		//transform the cluster point from object storage to ego-vehicle coordinates
		double px = iClusterPoints[midx(i, 0, np)];
		double py = iClusterPoints[midx(i, 1, np)];
		//note when converting to sensor coordinates, rotate the coordinate frame
		double evx;
		double evy;
		ObjectToEgoVehicle(evx, evy, px, py, cosOrient, sinOrient, xref, yref);
		//transform from ego-vehicle to sensor coordinates
		double scx;
		double scy;
		EgoVehicleToSensor(scx, scy, evx, evy, cosSyaw, sinSyaw, sx, sy);

		//project each target point into the target's coordinates to see if it is
		//an extreme point (arctan will be computed later only on these points)
		double pxp = xtx*scx + xty*scy;
		double pyp = ytx*scx + yty*scy;

		if (pxp > 0.0)
		{
			//if the slope can be calculated legitimately, use max and min slope to find extreme points
			double curs = pyp/pxp;
			if (curs > maxs)
			{
				maxs = curs;
				maxscx = scx;
				maxscy = scy;
			}
			if (curs < mins)
			{
				mins = curs;
				minscx = scx;
				minscy = scy;
			}
		}
		else
		{
			//a weird target that wraps over more than +-90o; have to calculate bearings brute force
			//NOTE: this is an odd case that doesn't happen too much

			//wrap the angle to the same 2pi branch
			double ang = WrapAngle(atan2(scy, scx), wraptarget);
			if (ang < z[0])
			{
				z[0] = ang;
			}
		}
	}

	//calculate min bearing from all the normal points
	if (fabs(maxs) != DBL_MAX)
	{
		double ang = atan2(maxscy, maxscx);
		ang = WrapAngle(ang, wraptarget);
		if (ang < z[0])
		{
			z[0] = ang;
		}
	}
	if (fabs(mins) != DBL_MAX)
	{
		double ang = atan2(minscy, minscx);
		ang = WrapAngle(ang, wraptarget);
		if (ang < z[0])
		{
			z[0] = ang;
		}
	}

	return;
}

void BccwClusterMeasurement(double* z, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument)
{
	/*
	Computes the expected measurement for a cluster of points where only the ccw side is visible.

	INPUTS:
		z - will be populated with the cluster measurement, [CCWOB]
		nx - length of the state vector x
		x - the object's state [xref, yref, oref, sref, href, ...]
		iSensor - the sensor used to generate the measurements
		iVehicleOdometry - the vehicle odometry structure
		iArgument - pointer to a cluster structure containing the cluster information

	OUTPUTS:
		z - populated with the cluster measurement.
	*/

	//initialize the measurements
	z[0] = -DBL_MAX;

	//extract the useful portions of the state
	double xref = x[0];
	double yref = x[1];
	double oref = x[2];

	//cast the optional argument as a cluster
	Cluster* iCluster = (Cluster*) (iArgument);
	double* iClusterPoints = iCluster->Points;

	double cosOrient = cos(oref);
	double sinOrient = sin(oref);
	double cosSyaw = cos(iSensor->SensorYaw);
	double sinSyaw = sin(iSensor->SensorYaw);
	double sx = iSensor->SensorX;
	double sy = iSensor->SensorY;

	int i;
	int np = iCluster->NumPoints;
	//wrap all bearings to the bearing of the anchor point in sensor coordinates
	double scxref;
	double scyref;
	EgoVehicleToSensor(scxref, scyref, xref, yref, cosSyaw, sinSyaw, sx, sy);
	double wraptarget = atan2(scyref, scxref);

	//calculate components of x-target direction (toward anchor point) in sensor coordinates
	double xtx = scxref;
	double xty = scyref;
	double ntx = sqrt(xtx*xtx + xty*xty);
	if (fabs(ntx) == 0.0)
	{
		xtx = 1.0;
		xty = 0.0;
	}
	else
	{
		double oontx = 1.0/ntx;
		xtx *= oontx;
		xty *= oontx;
	}
	//calculate components of y-target direction
	double ytx = -xty;
	double yty = xtx;

	//these will store the locations of the extreme bearing points
	double maxs = -DBL_MAX;
	double maxscx;
	double maxscy;
	double mins = DBL_MAX;
	double minscx;
	double minscy;

	for (i = 0; i < np; i++)
	{
		//transform the cluster point from object storage to ego-vehicle coordinates
		double px = iClusterPoints[midx(i, 0, np)];
		double py = iClusterPoints[midx(i, 1, np)];
		//note when converting to sensor coordinates, rotate the coordinate frame
		double evx;
		double evy;
		ObjectToEgoVehicle(evx, evy, px, py, cosOrient, sinOrient, xref, yref);
		//transform from ego-vehicle to sensor coordinates
		double scx;
		double scy;
		EgoVehicleToSensor(scx, scy, evx, evy, cosSyaw, sinSyaw, sx, sy);

		//project each target point into the target's coordinates to see if it is
		//an extreme point (arctan will be computed later only on these points)
		double pxp = xtx*scx + xty*scy;
		double pyp = ytx*scx + yty*scy;

		if (pxp > 0.0)
		{
			//if the slope can be calculated legitimately, use max and min slope to find extreme points
			double curs = pyp/pxp;
			if (curs > maxs)
			{
				maxs = curs;
				maxscx = scx;
				maxscy = scy;
			}
			if (curs < mins)
			{
				mins = curs;
				minscx = scx;
				minscy = scy;
			}
		}
		else
		{
			//a weird target that wraps over more than +-90o; have to calculate bearings brute force
			//NOTE: this is an odd case that doesn't happen too much

			//wrap the angle to the same 2pi branch
			double ang = WrapAngle(atan2(scy, scx), wraptarget);
			if (ang > z[0])
			{
				z[0] = ang;
			}
		}
	}

	//calculate max bearing from all the normal points
	if (fabs(maxs) != DBL_MAX)
	{
		double ang = atan2(maxscy, maxscx);
		ang = WrapAngle(ang, wraptarget);
		if (ang > z[0])
		{
			z[0] = ang;
		}
	}
	if (fabs(mins) != DBL_MAX)
	{
		double ang = atan2(minscy, minscx);
		ang = WrapAngle(ang, wraptarget);
		if (ang > z[0])
		{
			z[0] = ang;
		}
	}

	return;
}

void MobileyeMeasurement(double* z, double* H, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument)
{
	/*
	Computes the expected mobileye measurement for a target.

	INPUTS:
		z - will be populated with the mobileye measurement, [x, y, s, w]
		H - will be populated with the mobileye sensor Jacobian
		nx - length of the state vector x
		x - the object's state [x, y, s, h, w]
		iSensor - the sensor used to generate the measurements
		iVehicleOdometry - the vehicle odometry structure
		iArgument - not used (NULL)

	OUTPUTS:
		z - populated with the mobileye measurement.
		H - populated with the mobileye Jacobian.
	*/

	int i;
	int j;
	int nz = 4;

	//initialize the output arguments
	for (i = 0; i < nz; i++)
	{
		z[i] = 0.0;
		for (j = 0; j < nx; j++)
		{
			H[midx(i, j, nz)] = 0.0;
		}
	}

	//extract the state
	double tx = x[0];
	double ty = x[1];
	double ts = x[2];
	double th = x[3];
	double tw = x[4];

	double cosTh = cos(th);
	double sinTh = sin(th);

	//calculate exected obstacle position in sensor coordinates
	double cosSyaw = cos(iSensor->SensorYaw);
	double sinSyaw = sin(iSensor->SensorYaw);
	double sx = iSensor->SensorX;
	double sy = iSensor->SensorY;

	double osx;
	double osy;
	EgoVehicleToSensor(osx, osy, tx, ty, cosSyaw, sinSyaw, sx, sy);
	//set these as the first two measurements
	z[0] = osx;
	z[1] = osy;

	//calculate expected relative range rate in sensor coordinates
	//calculate velocity of the sensor in ego-vehicle coordinates
	double vx = iVehicleOdometry->vx;
	double vy = iVehicleOdometry->vy;
	double wz = iVehicleOdometry->wz;
	double vsx = vx - sy*wz;
	double vsy = vy + sx*wz;
	//compute target motion in ego-vehicle coordinates
	double tvx = ts*cosTh;
	double tvy = ts*sinTh;
	//calculate target velocity relative to sensor
	double trvx = tvx - vsx;
	double trvy = tvy - vsy;
	//dot this velocity vector with the sensor's forward direction to get projected speed
	z[2] = cosSyaw*trvx + sinSyaw*trvy;

	//calculate width
	z[3] = tw;

	//populate the jacobian matrix
	H[midx(0, 0, nz)] = cosSyaw;
	H[midx(0, 1, nz)] = sinSyaw;
	H[midx(1, 0, nz)] = -sinSyaw;
	H[midx(1, 1, nz)] = cosSyaw;
	H[midx(2, 2, nz)] = cosSyaw*cosTh + sinSyaw*sinTh;
	H[midx(2, 3, nz)] = -cosSyaw*ts*sinTh + sinSyaw*ts*cosTh;
	H[midx(3, 4, nz)] = 1.0;

	return;
}

void MobileyeNoWidthMeasurement(double* z, double* H, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument)
{
	/*
	Computes the expected mobileye measurement for a target with no width

	INPUTS:
		z - will be populated with the mobileye measurement, [x, y, s]
		H - will be populated with the mobileye sensor Jacobian
		nx - length of the state vector x
		x - the object's state [x, y, s, h, ...]
		iSensor - the sensor used to generate the measurements
		iVehicleOdometry - the vehicle odometry structure
		iArgument - not used (NULL)

	OUTPUTS:
		z - populated with the mobileye measurement.
		H - populated with the mobileye Jacobian.
	*/

	int i;
	int j;
	int nz = 3;

	//initialize the output arguments
	for (i = 0; i < nz; i++)
	{
		z[i] = 0.0;
		for (j = 0; j < nx; j++)
		{
			H[midx(i, j, nz)] = 0.0;
		}
	}

	//extract the state
	double tx = x[0];
	double ty = x[1];
	double ts = x[2];
	double th = x[3];

	double cosTh = cos(th);
	double sinTh = sin(th);

	//calculate exected obstacle position in sensor coordinates
	double cosSyaw = cos(iSensor->SensorYaw);
	double sinSyaw = sin(iSensor->SensorYaw);
	double sx = iSensor->SensorX;
	double sy = iSensor->SensorY;

	double osx;
	double osy;
	EgoVehicleToSensor(osx, osy, tx, ty, cosSyaw, sinSyaw, sx, sy);
	//set these as the first two measurements
	z[0] = osx;
	z[1] = osy;

	//calculate expected relative range rate in sensor coordinates
	//calculate velocity of the sensor in ego-vehicle coordinates
	double vx = iVehicleOdometry->vx;
	double vy = iVehicleOdometry->vy;
	double wz = iVehicleOdometry->wz;
	double vsx = vx - sy*wz;
	double vsy = vy + sx*wz;
	//compute target motion in ego-vehicle coordinates
	double tvx = ts*cosTh;
	double tvy = ts*sinTh;
	//calculate target velocity relative to sensor
	double trvx = tvx - vsx;
	double trvy = tvy - vsy;
	//dot this velocity vector with the sensor's forward direction to get projected speed
	z[2] = trvx*cosSyaw + trvy*sinSyaw;

	//populate the jacobian matrix
	H[midx(0, 0, nz)] = cosSyaw;
	H[midx(0, 1, nz)] = sinSyaw;
	H[midx(1, 0, nz)] = -sinSyaw;
	H[midx(1, 1, nz)] = cosSyaw;
	H[midx(2, 2, nz)] = cosTh*cosSyaw + sinTh*sinSyaw;
	H[midx(2, 3, nz)] = -ts*sinTh*cosSyaw + ts*cosTh*sinSyaw;

	return;
}

void MobileyeNoSpeedClusterMeasurement(double* z, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument)
{
	/*
	Computes a mobileye measurement without speed for a cluster of points

	INPUTS:
		z - will be populated with the cluster measurement, [x, y, w]
		nx - length of the state vector x
		x - the object's state [xref, yref, oref, ...]
		iSensor - the sensor used to generate the measurements
		iVehicleOdometry - the vehicle odometry structure
		iArgument - pointer to a cluster structure containing the cluster information

	OUTPUTS:
		z - populated with the cluster measurement.
	*/

	//initialize the measurements
	z[0] = 0.0;
	z[1] = 0.0;
	z[2] = 0.0;

	//calculate the max and min bearings, and the minimum range
	double zt[3];
	BcwBccwRminClusterMeasurement(zt, nx, x, iSensor, iVehicleOdometry, iArgument);
	//extract the extreme points from the measurement
	double minb = zt[0];
	double maxb = zt[1];
	double minr = zt[2];

	//compute x and y position using average bearing and minimum range
	maxb = WrapAngle(maxb, minb);
	double avgb = UnwrapAngle(0.5*(maxb + minb));
	z[0] = minr*cos(avgb);
	z[1] = minr*sin(avgb);

	//compute width measurement using the angular span and the minimum range
	z[2] = 2.0*minr*tan(0.5*fabs(UnwrapAngle(maxb - minb)));

	return;
}

void MobileyeClusterMeasurement(double* z, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument)
{
	/*
	Computes a full mobileye measurement for a cluster of points

	INPUTS:
		z - will be populated with the cluster measurement, [x, y, s, w]
		nx - length of the state vector x
		x - the object's state [xref, yref, oref, sref, href, ...]
		iSensor - the sensor used to generate the measurements
		iVehicleOdometry - the vehicle odometry structure
		iArgument - pointer to a cluster structure containing the cluster information

	OUTPUTS:
		z - populated with the cluster measurement.
	*/

	//initialize the measurements
	z[0] = 0.0;
	z[1] = 0.0;
	z[2] = 0.0;
	z[3] = 0.0;

	//extract the useful portions of the state
	double xref = x[0];
	double yref = x[1];
	double oref = x[2];
	double sref = x[3];
	double href = x[4];

	double cosOrient = cos(oref);
	double sinOrient = sin(oref);
	double cosSyaw = cos(iSensor->SensorYaw);
	double sinSyaw = sin(iSensor->SensorYaw);
	double sx = iSensor->SensorX;
	double sy = iSensor->SensorY;

	//calculate the max and min bearings, and the minimum range
	double zt[3];
	BcwBccwRminClusterMeasurement(zt, nx, x, iSensor, iVehicleOdometry, iArgument);
	//extract the extreme points from the measurement
	double minb = zt[0];
	double maxb = zt[1];
	double minr = zt[2];

	//compute x and y position using average bearing and minimum range
	maxb = WrapAngle(maxb, minb);
	double avgb = UnwrapAngle(0.5*(maxb + minb));
	z[0] = minr*cos(avgb);
	z[1] = minr*sin(avgb);

	//calculate expected relative range rate in sensor coordinates
	//calculate velocity of the sensor in ego-vehicle coordinates
	double vx = iVehicleOdometry->vx;
	double vy = iVehicleOdometry->vy;
	double wz = iVehicleOdometry->wz;
	double vsx = vx - sy*wz;
	double vsy = vy + sx*wz;
	//compute target motion in ego-vehicle coordinates
	double ts = sref;
	double cosTh = cos(href);
	double sinTh = sin(href);
	double tvx = ts*cosTh;
	double tvy = ts*sinTh;
	//calculate target velocity relative to sensor
	double trvx = tvx - vsx;
	double trvy = tvy - vsy;
	//dot this velocity vector with the sensor's forward direction to get projected speed
	z[2] = trvx*cosSyaw + trvy*sinSyaw;

	//compute width measurement using the angular span and the minimum range
	z[3] = 2.0*minr*tan(0.5*fabs(maxb - minb));

	return;
}

void RadarMeasurement(double* z, double* H, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument)
{
	/*
	Computes the expected radar measurement for a target.

	INPUTS:
		z - will be populated with the radar measurement, [r, b, rr]
		H - will be populated with the radar sensor Jacobian
		nx - length of the state vector x
		x - the object's state [x, y, s, h]
		iSensor - the sensor used to generate the measurements
		iVehicleOdometry - the vehicle odometry structure
		iArgument - not used (NULL)

	OUTPUTS:
		z - populated with the radar measurement.
		H - populated with the radar Jacobian.
	*/

	int i;
	int j;
	int nz = 3;

	//initialize the output arguments
	for (i = 0; i < nz; i++)
	{
		z[i] = 0.0;
		for (j = 0; j < nx; j++)
		{
			H[midx(i, j, nz)] = 0.0;
		}
	}

	//extract the state
	double tx = x[0];
	double ty = x[1];
	double ts = x[2];
	double th = x[3];

	double cosTh = cos(th);
	double sinTh = sin(th);

	//calculate exected obstacle position in sensor coordinates
	double cosSyaw = cos(iSensor->SensorYaw);
	double sinSyaw = sin(iSensor->SensorYaw);
	double sx = iSensor->SensorX;
	double sy = iSensor->SensorY;

	double osx;
	double osy;
	EgoVehicleToSensor(osx, osy, tx, ty, cosSyaw, sinSyaw, sx, sy);
	//convert these to range and bearing, and store as measurements
	double zr = sqrt(osx*osx + osy*osy);
	z[0] = zr;
	double zb = atan2(osy, osx);
	z[1] = zb;

	//NOTE: range rate calculations will use zb, the un-rolled bearing, since it is easier to visualize.
	//The result should be identical to using the rolled bearing.
	//compute the unit vector in the radial direction from sensor to target, in ego-vehicle coordinates
	double rdx = cos(iSensor->SensorYaw + zb);
	double rdy = sin(iSensor->SensorYaw + zb);

	//calculate velocity of the sensor in ego-vehicle coordinates
	double vx = iVehicleOdometry->vx;
	double vy = iVehicleOdometry->vy;
	double wz = iVehicleOdometry->wz;
	double vsx = vx - sy*wz;
	double vsy = vy + sx*wz;
	//compute target motion in ego-vehicle coordinates
	double tvx = ts*cosTh;
	double tvy = ts*sinTh;
	//calculate target velocity relative to sensor, in ego-vehicle coordinates
	double trvx = tvx - vsx;
	double trvy = tvy - vsy;
	//dot this velocity vector with the sensor's radial direction to get range rate
	z[2] = trvx*rdx + trvy*rdy;

	if (fabs(zr) > 0.0)
	{
		double oor = 1.0/zr;
		double oor2 = oor*oor;

		//populate the jacobian matrix
		H[midx(0, 0, nz)] = oor*(osx*cosSyaw - osy*sinSyaw);
		H[midx(0, 1, nz)] = oor*(osx*sinSyaw + osy*cosSyaw);
		H[midx(1, 0, nz)] = -oor2*(osy*cosSyaw + osx*sinSyaw);
		H[midx(1, 1, nz)] = oor2*(-osy*sinSyaw + osx*cosSyaw);
		H[midx(2, 0, nz)] = (-trvx*rdy + trvy*rdx)*H[midx(1, 0, nz)];
		H[midx(2, 1, nz)] = (-trvx*rdy + trvy*rdx)*H[midx(1, 1, nz)];
		H[midx(2, 2, nz)] = cosTh*rdx + sinTh*rdy;
		H[midx(2, 3, nz)] = -ts*sinTh*rdx + ts*cosTh*rdy;
	}
	//NOTE: if zr == 0, the entire Jacobian is 0.

	return;
}

void RangeBearingClusterMeasurement(double* z, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument)
{
	/*
	Computes a range and bearing measurement from a cluster type object

	INPUTS:
		z - will be populated with the cluster measurement, [CR, CB]
		nx - length of the state vector x
		x - the object's state [xref, yref, oref, sref, href, ...]
		iSensor - the sensor used to generate the measurements
		iVehicleOdometry - the vehicle odometry structure
		iArgument - pointer to a cluster structure containing the cluster information

	OUTPUTS:
		z - populated with the cluster measurement.
	*/

	//initialize the measurements
	z[0] = 0.0;
	z[1] = 0.0;

	//calculate the max and min bearings, and the minimum range
	double zt[3];
	BcwBccwRminClusterMeasurement(zt, nx, x, iSensor, iVehicleOdometry, iArgument);
	//extract the extreme points from the measurement
	double minb = zt[0];
	double maxb = zt[1];
	double minr = zt[2];

	//compute range and bearing using average bearing and minimum range
	maxb = WrapAngle(maxb, minb);
	double avgb = UnwrapAngle(0.5*(maxb + minb));
	z[0] = minr;
	z[1] = avgb;

	return;
}

void RangeBearingRateClusterMeasurement(double* z, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument)
{
	/*
	Computes a range-bearing-rate measurement from a cluster type object
	with a speed and heading

	INPUTS:
		z - will be populated with the cluster measurement, [CR, CB, CRR]
		nx - length of the state vector x
		x - the object's state [xref, yref, oref, sref, href, ...]
		iSensor - the sensor used to generate the measurements
		iVehicleOdometry - the vehicle odometry structure
		iArgument - pointer to a cluster structure containing the cluster information

	OUTPUTS:
		z - populated with the cluster measurement.
	*/

	//initialize the measurements
	z[0] = 0.0;
	z[1] = 0.0;
	z[2] = 0.0;

	//extract the useful portions of the state
	double xref = x[0];
	double yref = x[1];
	double oref = x[2];
	double sref = x[3];
	double href = x[4];

	//extract the sensor parameters used by this function
	double syaw = iSensor->SensorYaw;
	double sx = iSensor->SensorX;
	double sy = iSensor->SensorY;

	//calculate the max and min bearings, and the minimum range
	double zt[3];
	BcwBccwRminClusterMeasurement(zt, nx, x, iSensor, iVehicleOdometry, iArgument);
	//extract the extreme points from the measurement
	double minb = zt[0];
	double maxb = zt[1];
	double minr = zt[2];

	//compute range and bearing using average bearing and minimum range
	maxb = WrapAngle(maxb, minb);
	double avgb = UnwrapAngle(0.5*(maxb + minb));
	z[0] = minr;
	z[1] = avgb;

	//extract the target's bearing
	double tb = z[1];

	double vx = iVehicleOdometry->vx;
	double vy = iVehicleOdometry->vy;
	double wz = iVehicleOdometry->wz;

	//calculate range rate:
	//calculate target ground speed in ego-vehicle coordinates
	double tvx = sref*cos(href);
	double tvy = sref*sin(href);
	//calculate sensor speed in ego-vehicle coordinates
	double svx = vx - sy*wz;
	double svy = vy + sx*wz;
	//calculate the radial unit vector in ego vehicle coordinates
	double ruvx = cos(syaw + tb);
	double ruvy = sin(syaw + tb);
	//dot the relative speed with the radial unit vector for range rate
	z[2] = (tvx - svx)*ruvx + (tvy - svy)*ruvy;

	return;
}

void SensorDirectionalDistanceCluster(double* z, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument)
{
	/*
	Computes a sensor directional distance measurement for a target with a cluster of
	points.  The measurement projects the vector to the target onto the sensor forward
	direction and computes a distance from that.

	INPUTS:
		z - will be populated with the cluster measurement, [d]
		nx - length of the state vector x
		x - the object's state [xref, yref, oref, ...]
		iSensor - the sensor used to generate the measurements
		iVehicleOdometry - the vehicle odometry structure
		iArgument - pointer to a cluster structure containing the cluster information

	OUTPUTS:
		z - populated with the distance measurement.
	*/

	//initialize the measurement
	z[0] = 0.0;

	//extract the useful portions of the state
	double xref = x[0];
	double yref = x[1];
	double oref = x[2];

	//cast the optional argument as a cluster
	Cluster* iCluster = (Cluster*) (iArgument);
	double* iClusterPoints = iCluster->Points;

	int i;
	int np = iCluster->NumPoints;

	double cosOrient = cos(oref);
	double sinOrient = sin(oref);
	double syaw = iSensor->SensorYaw;
	double cosSyaw = cos(syaw);
	double sinSyaw = sin(syaw);
	double sx = iSensor->SensorX;
	double sy = iSensor->SensorY;

	double minp = DBL_MAX;
	double mind = DBL_MAX;

	for (i = 0; i < np; i++)
	{
		//transform the cluster point from object storage to ego-vehicle coordinates
		double px = iClusterPoints[midx(i, 0, np)];
		double py = iClusterPoints[midx(i, 1, np)];
		//note when converting to sensor coordinates, rotate the coordinate frame
		double evx;
		double evy;
		ObjectToEgoVehicle(evx, evy, px, py, cosOrient, sinOrient, xref, yref);
		//transform from ego-vehicle to sensor coordinates
		double scx;
		double scy;
		EgoVehicleToSensor(scx, scy, evx, evy, cosSyaw, sinSyaw, sx, sy);

		//record the closest range inside the sensor's field of view
		if (fabs(scy) <= minp)
		{
			minp = fabs(scy);
			mind = scx;
		}
	}

	//the measurement is just the closest projected distance
	z[0] = mind;

	return;
}

void SensorDirectionalDistance(double* z, double* H, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument)
{
	/*
	Computes a sensor directional distance measurement for a target with no points.
	The measurement projects the vector to the target onto the sensor forward
	direction and computes a distance from that.

	INPUTS:
		z - will be populated with the cluster measurement, [d]
		nx - length of the state vector x
		x - the object's state [xref, yref, sref, href, wref, ...]
		iSensor - the sensor used to generate the measurements
		iVehicleOdometry - the vehicle odometry structure
		iArgument - pointer to a cluster structure containing the cluster information

	OUTPUTS:
		z - populated with the distance measurement.
		H - populated with the Jacobian of the measurement
	*/

	int i;
	int nz = 1;

	//initialize the output arguments
	z[0] = DBL_MAX;
	for (i = 0; i < nx; i++)
	{
		H[midx(0, i, nz)] = 0.0;
	}

	//extract the state
	double tx = x[0];
	double ty = x[1];
	double tw = x[4];

	//calculate expected target position in sensor coordinates
	double cosSyaw = cos(iSensor->SensorYaw);
	double sinSyaw = sin(iSensor->SensorYaw);
	double sx = iSensor->SensorX;
	double sy = iSensor->SensorY;

	double scx;
	double scy;
	EgoVehicleToSensor(scx, scy, tx, ty, cosSyaw, sinSyaw, sx, sy);
	//use the projection toward the sensor as the distance measurement
	z[0] = scx - tw;

	//set the Jacobian
	H[midx(0, 0, nz)] = cosSyaw;
	H[midx(0, 1, nz)] = sinSyaw;
	H[midx(0, 4, nz)] = -1.0;

	return;
}

void SensorDirectionalDistanceNoWidth(double* z, double* H, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument)
{
	/*
	Computes a sensor directional distance measurement for a target with no points
	and no width.  The measurement projects the vector to the target onto the 
	sensor forward direction and computes a distance from that.

	INPUTS:
		z - will be populated with the cluster measurement, [d]
		nx - length of the state vector x
		x - the object's state [xref, yref, ...]
		iSensor - the sensor used to generate the measurements
		iVehicleOdometry - the vehicle odometry structure
		iArgument - pointer to a cluster structure containing the cluster information

	OUTPUTS:
		z - populated with the distance measurement.
		H - populated with the Jacobian of the measurement
	*/

	int i;
	int nz = 1;

	//initialize the output arguments
	z[0] = DBL_MAX;
	for (i = 0; i < nx; i++)
	{
		H[midx(0, i, nz)] = 0.0;
	}

	//extract the state
	double tx = x[0];
	double ty = x[1];

	//calculate expected target position in sensor coordinates
	double cosSyaw = cos(iSensor->SensorYaw);
	double sinSyaw = sin(iSensor->SensorYaw);
	double sx = iSensor->SensorX;
	double sy = iSensor->SensorY;

	double scx;
	double scy;
	EgoVehicleToSensor(scx, scy, tx, ty, cosSyaw, sinSyaw, sx, sy);
	//use the projection toward the sensor as the distance measurement
	z[0] = scx;

	//set the Jacobian
	H[midx(0, 0, nz)] = cosSyaw;
	H[midx(0, 1, nz)] = sinSyaw;

	return;
}

double ExtendedKalmanLikelihood(double* nu, double* S, double* W, int nz, double* z, bool* zwrap, double* R, int nx, double* xbar, double* Pbar, void (*MeasurementFunction) (double* z, double* H, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument), Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument, double iExtendedKalmanGate)
{
	/*
	Computes the likelihood of a measurement using the exteneded Kalman method

	INPUTS:
		nu, S, W - will contain the innovation, innovation covariance, and Kalman
			update matrix computed for the target.
		nz - number of measurements (length of z)
		z, zwrap, R - measurement, which elements are to be wrapped angles, and measurement covariance
		nx - number of states (length of x)
		xbar, Pbar - predicted state and state covariance
		MeasurementFunction - a function pointer defining a function that is used to
			evaluate measurements.  Function must follow the signature:
			MeasurementFunction(double* z, double* H, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument);
		iSensor - the sensor structure used to generate the measurement
		iVehicleOdometry - the vehicle odometry structure for the present time
		iArgument - an optional pointer to extra data
		iExtendedKalmanGate - a chi2 gate to be applied to the measurement

	OUTPUTS:
		rLambda - the measurement likelihood.  Also populates nu, S, and W.
			If an error is encountered, the matrices are not populated.  In that case, rLambda = 0.0
	*/

	double rLambda = 0.0;

	if (nx <= 0 || nz <= 0)
	{
		//invalid state dimensions, so return
		printf("Warning: ExtendedKalmanLikelihood called with nz = %d and nx = %d.\n", nz, nx);
		return rLambda;
	}

	int i;
	int j;
	int k;

	//initialize output arguments
	for (i = 0; i < nz; i++)
	{
		nu[i] = 0.0;

		for (j = 0; j < nz; j++)
		{
			S[midx(i, j, nz)] = 0.0;
		}
	}
	for (i = 0; i < nx; i++)
	{
		for (j = 0; j < nz; j++)
		{
			W[midx(i, j, nx)] = 0.0;
		}
	}

	//preallocate space

	double* zbar = (double*) _malloca(nz*sizeof(double));
	double* H = (double*) _malloca(nz*nx*sizeof(double));
	double* PbarHt = (double*) _malloca(nx*nz*sizeof(double));
	double* invS = (double*) _malloca(nz*nz*sizeof(double));
	int* ipiv = (int*) _malloca(nz*sizeof(int));
	int info;

	//compute the expected measurement and the input jacobian
	MeasurementFunction(zbar, H, nx, xbar, iSensor, iVehicleOdometry, iArgument);
	//compute nu = z - zbar
	for (i = 0; i < nz; i++)
	{
		if (zwrap[i] == false)
		{
			nu[i] = z[i] - zbar[i];
		}
		else
		{
			nu[i] = UnwrapAngle(z[i] - zbar[i]);
		}
	}

	//compute S = HPbarHt + R
	for (i = 0; i < nx; i++)
	{
		for (j = 0; j < nz; j++)
		{
			//this loop computes PbarHt, which is also a partial computation of W
			PbarHt[midx(i, j, nx)] = 0.0;
			for (k = 0; k < nx; k++)
			{
				PbarHt[midx(i, j, nx)] += Pbar[midx(i, k, nx)] * H[midx(j, k, nz)];
			}
		}
	}
	for (i = 0; i < nz; i++)
	{
		for (j = 0; j < nz; j++)
		{
			S[midx(i, j, nz)] = R[midx(i, j, nz)];
			for (k = 0; k < nx; k++)
			{
				S[midx(i, j, nz)] += H[midx(i, k, nz)] * PbarHt[midx(k, j, nx)];
			}
		}
	}
	//copy S into invS
	for (i = 0; i < nz; i++)
	{
		for (j = 0; j < nz; j++)
		{
			invS[midx(i, j, nz)] = S[midx(i, j, nz)];
		}
	}

	//invert S
	//LU decomposition for matrix inversion
	dgetrf(nz, nz, invS, nz, ipiv, &info);
	if (info != 0)
	{
		_freea(zbar);
		_freea(H);
		_freea(PbarHt);
		_freea(invS);
		_freea(ipiv);

		printf("Warning: dgetrf error in ExtendedKalmanLikelihood.\n");
		rLambda = 0.0;
		return rLambda;
	}
	//calculate the determinant of S before destroying the LU decomposition
	double detS = 1.0;
	for (i = 0; i < nz; i++)
	{
		if (ipiv[i] > i+1)
		{
			//negate the determinant because a row pivot took place
			detS *= -invS[midx(i, i, nz)];
		}
		else
		{
			//don't negate the determinant because the ith row either wasn't pivoted
			//or it was pivoted (but we counted it already)
			detS *= invS[midx(i, i, nz)];
		}
	}
	//invert S and store in invS
	dgetri(nz, invS, nz, ipiv, &info);
	if (info != 0)
	{
		_freea(zbar);
		_freea(H);
		_freea(PbarHt);
		_freea(invS);
		_freea(ipiv);

		printf("Warning: dgetri error in ExtendedKalmanLikelihood.\n");
		rLambda = 0.0;
		return rLambda;
	}

	//calculate W = PbarHt*invS
	for (i = 0; i < nx; i++)
	{
		for (j = 0; j < nz; j++)
		{
			W[midx(i, j, nx)] = 0.0;
			for (k = 0; k < nz; k++)
			{
				W[midx(i, j, nx)] += PbarHt[midx(i, k, nx)] * invS[midx(k, j, nz)];
			}
		}
	}

	//calculate likelihood: 
	//rLambda = exp(-0.5*nu'*inv(S)*nu) / sqrt((2*pi)^(length(nu))*det(S))
	for (i = 0; i < nz; i++)
	{
		for (j = 0; j < nz; j++)
		{
			rLambda += nu[i] * invS[midx(i, j, nz)] * nu[j];
		}
	}
	if (rLambda > iExtendedKalmanGate)
	{
		_freea(zbar);
		_freea(H);
		_freea(PbarHt);
		_freea(invS);
		_freea(ipiv);

		rLambda = 0.0;
		return rLambda;
	}
	//rLambda = exp(-0.5*rLambda) / sqrt(pow(TWOPI, (double) nz) * detS);
	//do this for a little bit more stability numerically
	rLambda = exp(-0.5*rLambda - 0.5*((double) nz)*LNTWOPI - 0.5*log(detS));

	_freea(zbar);
	_freea(H);
	_freea(PbarHt);
	_freea(invS);
	_freea(ipiv);

	return rLambda;
}

double SigmaPointLikelihood(double* nu, double* S, double* W, int nz, double* z, bool* zwrap, double* R, int nx, double* xbar, double* Pbar, void (*MeasurementFunction) (double* z, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument), Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument, double iSigmaPointGate)
{
	/*
	Computes the likelihood of a measurement using the sigma point method

	INPUTS:
		nu, S, W - will contain the innovation, innovation covariance, and Kalman
			gain matrix computed for the target.
		nz - number of measurements (length of z)
		z, zwrap, R - measurement, which elements are to be wrapped angles, and measurement covariance
		nx - number of states (length of x)
		xbar, Pbar - predicted state and state covariance
		MeasurementFunction - a function pointer defining a function that is used to
			evaluate measurements.  Function must follow the signature:
			MeasurementFunction(double* z, int nx, double* x, Sensor* iSensor, VehicleOdometry* iVehicleOdometry, void* iArgument);
		iSensor - the sensor structure used to generate the measurement
		iVehicleOdometry - the vehicle odometry structure for the present time
		iArgument - an optional pointer to extra data
		iSigmaPointGate - a chi2 gate to be applied to the measurement

	OUTPUTS:
		rLambda - the measurement likelihood.  Also populates nu, S, and W.
			If an error is encountered, the matrices are not populated.  In that case, rLambda = 0.0
	*/

	double rLambda = 0.0;

	if (nz <= 0 || nx <= 0)
	{
		printf("Warning: SigmaPointLikelihood called with nz = %d and nx = %d.\n", nz, nx);
		return rLambda;
	}

	int i;
	int j;
	int k;

	//sigma point tuning parameters
	double nxd = (double) (nx);
	int nsp = 2*nx + 1;
	double spf_alpha = SPF_ALPHA;
	double spf_beta = SPF_BETA;
	double spf_kappa = SPF_KAPPA;
	double spf_lambda = spf_alpha*spf_alpha*(nxd + spf_kappa) - nxd;

	//preallocate space
	double* Sx = (double*) _malloca(nx*nx*sizeof(double));
	double* xsp_bar = (double*) _malloca(nx*nsp*sizeof(double));
	double* zsp_bar = (double*) _malloca(nz*nsp*sizeof(double));
	double* ztemp = (double*) _malloca(nz*sizeof(double));
	double* xtemp = (double*) _malloca(nx*sizeof(double));
	double* zbar = (double*) _malloca(nz*sizeof(double));
	double* Pzz = (double*) _malloca(nz*nz*sizeof(double));
	double* Pxz = (double*) _malloca(nx*nz*sizeof(double));
	int* ipiv = (int*) _malloca(nz*sizeof(int));

	//calculate the sigma-point likelihood for the given measurement function

	//create the sigma points for the a priori state
	//Cholesky factorize Pbar: Sx = chol(Pbar)'
	int info;
	for (i = 0; i < nx; i++)
	{
		for (j = 0; j < nx; j++)
		{
			Sx[midx(i, j, nx)] = Pbar[midx(i, j, nx)];
		}
	}
	dpotrf('U', nx, Sx, nx, &info);
	if (info != 0)
	{
		//exit on ACML error
		printf("Warning: dpotrf error in SigmaPointLikelihood.\n");

		_freea(Sx);
		_freea(xsp_bar);
		_freea(zsp_bar);
		_freea(ztemp);
		_freea(xtemp);
		_freea(zbar);
		_freea(Pzz);
		_freea(Pxz);
		_freea(ipiv);

		return rLambda;
	}
	//transpose so Sx = chol(P)'
	//also clear out the upper triangle, which should be zeros
	for (i = 0; i < nx; i++)
	{
		for (j = 0; j < i; j++)
		{
			Sx[midx(i, j, nx)] = Sx[midx(j, i, nx)];
			Sx[midx(j, i, nx)] = 0.0;
		}
	}

	//number of sigma points = 2*nx + 1
	double spfact = sqrt(nxd + spf_lambda);
	for (i = 0; i < nx; i++)
	{
		for (j = 0; j < nsp; j++)
		{
			xsp_bar[midx(i, j, nx)] = xbar[i];
			if (j > 0 && j <= nx)
			{
				xsp_bar[midx(i, j, nx)] += spfact * Sx[midx(i, j - 1, nx)];
			}
			else if (j > nx)
			{
				xsp_bar[midx(i, j, nx)] -= spfact * Sx[midx(i, j - nx - 1, nx)];
			}
		}
	}

	//measurement sigma points for 2 ob's + range
	for (i = 0; i < nsp; i++)
	{
		//create a measurement for each x-sigma point

		for (j = 0; j < nx; j++)
		{
			xtemp[j] = xsp_bar[midx(j, i, nx)];
		}

		MeasurementFunction(ztemp, nx, xtemp, iSensor, iVehicleOdometry, iArgument);

		//copy the measurement into zsp_bar
		for (j = 0; j < nz; j++)
		{
			zsp_bar[midx(j, i, nz)] = ztemp[j];
			if (zwrap[j] == true)
			{
				//wrap all angle measurements toward the main sigma point
				zsp_bar[midx(j, i, nz)] = WrapAngle(zsp_bar[midx(j, i, nz)], zsp_bar[midx(j, 0, nz)]);
			}
		}
	}

	double Wmspf_1 = spf_lambda / (nxd + spf_lambda);
	double Wmspf_n = 0.5 / (nxd + spf_lambda);
	for (i = 0; i < nz; i++)
	{
		zbar[i] = Wmspf_1 * zsp_bar[midx(i, 0, nz)];
		for (j = 1; j < nsp; j++)
		{
			zbar[i] += Wmspf_n*zsp_bar[midx(i, j, nz)];
		}
	}

	//calculate innovation nu from z and zbar
	for (i = 0; i < nz; i++)
	{
		if (zwrap[i] == false)
		{
			nu[i] = z[i] - zbar[i];
		}
		else
		{
			nu[i] = UnwrapAngle(z[i] - zbar[i]);
		}
	}

	//calculate measurement covariance Pzz ~ HPH' + R
	double Wcspf_1 = Wmspf_1 + 1.0 - spf_alpha*spf_alpha + spf_beta;
	double Wcspf_n = Wmspf_n;
	for (i = 0; i < nz; i++)
	{
		for (j = 0; j < nz; j++)
		{
			Pzz[midx(i, j, nz)] = Wcspf_1*(zsp_bar[midx(i, 0, nz)] - zbar[i])*(zsp_bar[midx(j, 0, nz)] - zbar[j]);
			Pzz[midx(i, j, nz)] += R[midx(i, j, nz)];
		}
	}
	for (i = 1; i < nsp; i++)
	{
		for (j = 0; j < nz; j++)
		{
			for (k = 0; k < nz; k++)
			{
				Pzz[midx(j, k, nz)] += Wcspf_n*(zsp_bar[midx(j, i, nz)] - zbar[j])*(zsp_bar[midx(k, i, nz)] - zbar[k]);
			}
		}
	}

	//calculate Pxz ~PH', nx x nz matrix
	for (i = 0; i < nx; i++)
	{
		for (j = 0; j < nz; j++)
		{
			Pxz[midx(i, j, nx)] = Wcspf_1*(xsp_bar[midx(i, 0, nx)] - xbar[i])*(zsp_bar[midx(j, 0, nz)] - zbar[j]);
		}
	}
	for (i = 1; i < nsp; i++)
	{
		for (j = 0; j < nx; j++)
		{
			for (k = 0; k < nz; k++)
			{
				Pxz[midx(j, k, nx)] += Wcspf_n*(xsp_bar[midx(j, i, nx)] - xbar[j])*(zsp_bar[midx(k, i, nz)] - zbar[k]);
			}
		}
	}

	//calculate S = Pzz and W = Pxx*inv(Pzz)
	for (i = 0; i < nz; i++)
	{
		for (j = 0; j < nz; j++)
		{
			S[midx(i, j, nz)] = Pzz[midx(i, j, nz)];
		}
	}

	//invert the S matrix
	//LU decomposition for matrix inversion
	dgetrf(nz, nz, Pzz, nz, ipiv, &info);
	if (info != 0)
	{
		printf("Warning: dgetrf error in SigmaPointLikelihood.\n");

		_freea(Sx);
		_freea(xsp_bar);
		_freea(zsp_bar);
		_freea(ztemp);
		_freea(xtemp);
		_freea(zbar);
		_freea(Pzz);
		_freea(Pxz);
		_freea(ipiv);

		return rLambda;
	}
	//calculate the determinant of S before destroying the LU decomposition
	double detS = 1.0;
	for (i = 0; i < nz; i++)
	{
		if (ipiv[i] > i+1)
		{
			//negate the determinant because a row pivot took place
			detS *= -Pzz[midx(i, i, nz)];
		}
		else
		{
			//don't negate the determinant because the ith row either wasn't pivoted
			//or it was pivoted (but we counted it already)
			detS *= Pzz[midx(i, i, nz)];
		}
	}
	//invert S and store in Pzz
	dgetri(nz, Pzz, nz, ipiv, &info);
	if (info != 0)
	{
		printf("Warning: dgetri error in SigmaPointLikelihood.\n");

		_freea(Sx);
		_freea(xsp_bar);
		_freea(zsp_bar);
		_freea(ztemp);
		_freea(xtemp);
		_freea(zbar);
		_freea(Pzz);
		_freea(Pxz);
		_freea(ipiv);

		return rLambda;
	}

	//create the W matrix: W = Pxz*inv(S)
	for (i = 0; i < nx; i++)
	{
		for (j = 0; j < nz; j++)
		{
			W[midx(i, j, nx)] = 0.0;
			for (k = 0; k < nz; k++)
			{
				W[midx(i, j, nx)] += Pxz[midx(i, k, nx)] * Pzz[midx(k, j, nz)];
			}
		}
	}

	//calculate likelihood: 
	//rLambda = exp(-0.5*nu'*inv(S)*nu) / sqrt((2*pi)^(length(nu))*det(S))
	for (i = 0; i < nz; i++)
	{
		for (j = 0; j < nz; j++)
		{
			rLambda += nu[i] * Pzz[midx(i, j, nz)] * nu[j];
		}
	}
	if (rLambda > iSigmaPointGate)
	{
		//measurement failed the gating hypothesis test
		rLambda = 0.0;

		_freea(Sx);
		_freea(xsp_bar);
		_freea(zsp_bar);
		_freea(ztemp);
		_freea(xtemp);
		_freea(zbar);
		_freea(Pzz);
		_freea(Pxz);
		_freea(ipiv);

		return rLambda;
	}
	//rLambda = exp(-0.5*rLambda) / sqrt(pow(TWOPI, (double) (nz))*detS);
	//do this for a little bit more stability numerically
	rLambda = exp(-0.5*rLambda - 0.5*((double) nz)*LNTWOPI - 0.5*log(detS));

	//deallocate memory
	_freea(Sx);
	_freea(xsp_bar);
	_freea(zsp_bar);
	_freea(ztemp);
	_freea(xtemp);
	_freea(zbar);
	_freea(Pzz);
	_freea(Pxz);
	_freea(ipiv);

	return rLambda;
}

void PredictPoints(double iDt, int iNumPoints, double* iPoints, VehicleOdometry* iVehicleOdometry)
{
	/*
	Integrates a set of static points forward in ego-vehicle coordinates using
	4th order runge-kutta integration

	INPUTS:
		iDt - length of time of the prediction
		iNumPoints - number of points to integrate
		iPoints - iNumPoints x 2 vector of points in ego-vehicle coordinates
			at the current time
		iVehicleOdometry - vehicle odometry structure valid over the prediction

	OUTPUTS:
		iPoints - will contain the predicted set of points on exit.  Order of
			the points is not changed
	*/

	int i;
	int np = iNumPoints;
	double dt = iDt;

	//declare memory
	double xcur[2];
	double xdot[2];
	double k1[2];
	double k2[2];
	double k3[2];
	double k4[2];

	double ifact = 1.0 / 6.0;

	for (i = 0; i < np; i++)
	{
		//extract the ith point
		double px = iPoints[midx(i, 0, np)];
		double py = iPoints[midx(i, 1, np)];

		//RK1:
		xcur[0] = px;
		xcur[1] = py;
		PointDynamics(xdot, xcur, iVehicleOdometry);
		k1[0] = dt * xdot[0];
		k1[1] = dt * xdot[1];

		//RK2:
		xcur[0] = px + 0.5*k1[0];
		xcur[1] = py + 0.5*k1[1];
		PointDynamics(xdot, xcur, iVehicleOdometry);
		k2[0] = dt * xdot[0];
		k2[1] = dt * xdot[1];

		//RK3:
		xcur[0] = px + 0.5*k2[0];
		xcur[1] = py + 0.5*k2[1];
		PointDynamics(xdot, xcur, iVehicleOdometry);
		k3[0] = dt * xdot[0];
		k3[1] = dt * xdot[1];

		//RK4:
		xcur[0] = px + k3[0];
		xcur[1] = py + k3[1];
		PointDynamics(xdot, xcur, iVehicleOdometry);
		k4[0] = dt * xdot[0];
		k4[1] = dt * xdot[1];

		//combine RK steps into prediction
		iPoints[midx(i, 0, np)] += (k1[0] + 2.0*k2[0] + 2.0*k3[0] + k4[0])*ifact;
		iPoints[midx(i, 1, np)] += (k1[1] + 2.0*k2[1] + 2.0*k3[1] + k4[1])*ifact;
	}

	return;
}

void PointDynamics(double* xdot, double* x, VehicleOdometry* iVehicleOdometry)
{
	/*
	Dynamics function describing the dynamics of a single 2d point in 
	ego vehicle coordinates.

	INPUTS:
		xdot - 2 x 1 preallocated vector that will contain the spatial
			derivatives of the point in ego-vehicle coordinates
		x - 2 x 1 vector containing the point's {x, y} location in ego-
			vehicle coordinates
		iVehicleOdometry - ego motion at the desired time

	OUTPUTS:
		xdot - populated with the spatial derivatives on output
	*/

	//extract the state
	double px = x[0];
	double py = x[1];

	//extract the vehicle odometry
	double vx = iVehicleOdometry->vx;
	double vy = iVehicleOdometry->vy;
	double wz = iVehicleOdometry->wz;

	//compute the deriviative
	xdot[0] = -vx + py*wz;
	xdot[1] = -vy - px*wz;

	return;
}
