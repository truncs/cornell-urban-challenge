#include "SceneEstimatorFunctions.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

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

bool ComputeCRC(unsigned short& oCRC, char* iFileName)
{
	/*
	Reads an entire file and computes a 16-bit CRC for the file.
	Uses the crc divisor CRC-16-IBM.

	INPUTS:
		iFileName - name of the file to be opened for reading.

	OUTPUTS:
		rSuccess - true if the CRC is computed successfully, false otherwise.
			if true, oCRC contains the CRC value.
	*/

	bool rSuccess = false;

	//attempt to open the file for computing the crc value
	FILE* iFile = NULL;
	errno_t err = fopen_s(&iFile, iFileName, "rb");
	if (err != 0)
	{
		//could not open the file to compute a CRC
		return rSuccess;
	}

	//define the crc divisor and its length
	unsigned short crc = 0x8005;
	//this buffer will hold the reserve bits
	unsigned char reservebits;
	//number of reserve bits remaining
	int numrbits;
	//this buffer will hold the file as the crc is computed
	unsigned short dividend;
	//this masks out the most significant bit for a 2-byte number
	unsigned short leadbitmask2 = 0x8000;
	unsigned char leadbitmask = 0x80;

	int ct;
	//read the first 2 bytes of the file into the dividend to start the crc
	ct = (int) fread(&dividend, 2, 1, iFile);
	if (ct == 0)
	{
		//don't compute a crc for a small file
		fclose(iFile);
		return rSuccess;
	}
	//read 1 byte into the reserve bits
	ct = (int) fread(&reservebits, 1, 1, iFile);
	numrbits = 8;
	if (ct == 0)
	{
		//don't compute a crc for a small file
		fclose(iFile);
		return rSuccess;
	}

	while (numrbits > 0 || feof(iFile) == 0)
	{
		//continue building up the crc code until the end of the file

		//check whether the highest order bit is 1 by masking against 0b1000000000000000
		if ((dividend & leadbitmask2) != 0)
		{
			//highest order bit is 1: xor the buffer with the divisor
			dividend = dividend ^ crc;
		}

		//left shift by one bit to multiply by 2 (gets rid of the leading zero)
		dividend = dividend << 1;

		//pull in one more bit from the reserve buffer
		if ((reservebits & leadbitmask) != 0)
		{
			//check whether the highest order bit is 1 by masking against 0b10000000
			dividend += 1;
		}
		//left shift reservebits to push the next reserve bit to the top of the cache
		reservebits = reservebits << 1;
		numrbits--;

		if (numrbits == 0)
		{
			//ran out of reserve bits; read the next byte in the file
			ct = (int) fread(&reservebits, 1, 1, iFile);
			if (ct == 0)
			{
				//either the end of the file was reached, or there was a read error
				if (ferror(iFile) != 0)
				{
					//a read error; exit the crc calculation
					fclose(iFile);
					return rSuccess;
				}
			}
			else
			{
				numrbits = 8;
			}
		}
	}

	if (feof(iFile) != 0)
	{
		//if the end of the file is successfully reached, compute the crc
		oCRC = dividend ^ crc;
		rSuccess = true;
	}

	//done reading the file, close it
	fclose(iFile);

	return rSuccess;
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

void SceneEstimatorScreenInit()
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

	if (fabs(dt) == 0.0)
	{
		//don't need to predict anything if dt is 0
		return;
	}

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

double PolygonArea(int iNumPoints, double* iPolygonPoints)
{
	/*
	Calculates the area of a given polygon, passed in as a set of vertices.
	The vertices should be sorted either clockwise or counterclockwise, and
	the polygon should be simple (non-intersecting sides).

	INPUTS:
		iNumPoints - number of points in the polygon (number of columns
			of iPolygonPoints)
		iPolygonPoints - 2 x iNumPoints array of vertices, where each column
			is an (x, y) pair.  The first column should equal the last column
			so the polygon is closed.
	*/

	double rArea = 0.0;

	if (iNumPoints == 0)
	{
		return rArea;
	}

	int i;

	double xcm = 0.0;
	double ycm = 0.0;
	double wt = 1.0 / ((double) iNumPoints);
	for (i = 0; i < iNumPoints; i++)
	{
		//compute the polygon center of mass
		xcm += wt*iPolygonPoints[midx(0, i, 2)];
		ycm += wt*iPolygonPoints[midx(1, i, 2)];
	}

	for (i = 0; i < iNumPoints-1; i++)
	{
		//compute the area as the sum of signed areas of triangles formed
		//from the center of mass to points i and i+1
		double x1 = iPolygonPoints[midx(0, i, 2)];
		double y1 = iPolygonPoints[midx(1, i, 2)];
		double x2 = iPolygonPoints[midx(0, i+1, 2)];
		double y2 = iPolygonPoints[midx(1, i+1, 2)];

		//compute the signed area of the triangle as a cross product
		double atri = 0.5*((x1 - xcm)*(y2 - ycm) - (x2 - xcm)*(y1 - ycm));
		rArea += atri;
	}

	//take the absolute value of area, which will be negative if the polygon
	//points were sorted in clockwise order.
	rArea = fabs(rArea);

	return rArea;
}

bool PointInPolygon(double iXtest, double iYtest, int iNumPoints, double* iPolygonPoints)
{
	/*
	Tests whether the point (iXtest, iYtest) is in the given polygon

	INPUTS:
		iXtest, iYtest - the (x, y) pair to test
		iNumPoints - number of points (number of columns in iPolygonPoints)
		iPolygonPoints - 2 x iNumPoints array of points, where each column
			is an (x, y) pair.  Assume the last point is the same as the 
			first point.

	OUTPUTS:
		rIsInPolygon - true if the point is in the polygon, false otherwise
	*/

	bool rIsInPolygon = false;

	int i;
	int np = iNumPoints;
	double dx1;
	double dy1;
	double dx2;
	double dy2;
	//count the number of crossings
	int numcrossings = 0;

	if (np == 0)
	{
		return rIsInPolygon;
	}

	//check each successive pair of points (assume points are in order)
	dy2 = iPolygonPoints[midx(1, 0, 2)] - iYtest;
	for (i = 1; i < np; i++)
	{
		//check successive pairs of points for y-axis crossings (DARPA gives CW ordering of zone points)

		dy1 = dy2;
		dy2 = iPolygonPoints[midx(1, i, 2)] - iYtest;
		if (dy1*dy2 <= 0.0)
		{
			//the points differ in sign (or are on the ray)

			if (fabs(dy1) > 0.0 || fabs(dy2) > 0.0)
			{
				//one point is above the ray and one point is below (or on) the ray: test for explicit intersection
				double w = -dy1 / (dy2 - dy1);
				dx1 = iPolygonPoints[midx(0, i-1, 2)] - iXtest;
				dx2 = iPolygonPoints[midx(0, i, 2)] - iXtest;

				if (dx1 + w*(dx2 - dx1) > 0.0)
				{
					//line intersection occurs on the ray to the right of the test point
					numcrossings++;
				}
			}
			//NOTE: if both points are on the ray, that edge is ignored as a "length 0" edge
		}
	}

	//check if the number of crossings is odd (point in polygon) or even (point outside polygon)
	if (numcrossings % 2 == 1)
	{
		//query point is in the polygon
		rIsInPolygon = true;
	}

	return rIsInPolygon;
}

bool LineIntersect(double& oIntersectX, double& oIntersectY, double x0, double y0, double x1, double y1, double u0, double v0, double u1, double v1)
{
	/*
	Calculates whether two lines (x0, y0)->(x1, y1) and (u0, v0)->(u1, v1) intersect.
	If they do intersect, this function also calculates the point of intersection.

	INPUTS:
		oIntersectX, oIntersectY - the intersection point between the two lines, if it
			exists.
		x0, y0 - the first point of the first line
		x1, y1 - the second point of the first line
		u0, v0 - the first point of the second line
		u1, v1 - the second point of the second line

	OUTPUTS:
		rIntersect - true if the line segments intersect, false otherwise.  If true, 
			oIntersectX and oIntersectY are also populated.  If false, they are not
			changed from their input values.

		NOTE: if the lines are on top of each other (multiple intersections), this
			function returns false.
	*/

	bool rIntersect = false;

	//first test if the segments intersect by expressing the second segment's points
	//in the coordinate frame of the first segment
	double dx = x1 - x0;
	double dy = y1 - y0;
	double dxo = -dy;
	double dyo = dx;

	//project the second line onto the y-axis of the first
	double py0 = (u0 - x0)*dxo + (v0 - y0)*dyo;
	double py1 = (u1 - x0)*dxo + (v1 - y0)*dyo;
	if (py0*py1 <= 0.0)
	{
		//the second segment's points straddle the first segment in one axis
		//so test for an explicit intersection
		double det = (v1 - v0)*(x1 - x0) - (u1 - u0)*(y1 - y0);
		if (fabs(det) == 0.0)
		{
			//the lines are parallel... but since we've already tested for straddling,
			//it means that the lines may overlap
			return rIntersect;
		}

		//calculate an explicit intersection
		double w1 = ((u1 - u0)*(y0 - v0) - (x0 - u0)*(v1 - v0)) / det;
		double w2 = ((x1 - x0)*(y0 - v0) - (x0 - u0)*(y1 - y0)) / det;

		if (w1 >= 0.0 && w1 <= 1.0 && w2 >= 0.0 && w2 <= 1.0)
		{
			//the intersection is valid and on the segments
			oIntersectX = u0 + w2*(u1 - u0);
			oIntersectY = v0 + w2*(v1 - v0);
			rIntersect = true;
		}
		//otherwise the intersection occurs beyond the segments
	}
	//NOTE: if py0 and py1 have the same sign, then no intersection is possible

	return rIntersect;
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

/*
void EgoVehicleToAbsolute(double& oEast, double& oNorth, double iEgoX, double iEgoY, double icosVehHeading, double isinVehHeading, double iVehEast, double iVehNorth)
{
	/
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
	/

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
*/

void TrackCovarianceToAbsolute(double* oAbsCov, int nxt, double iTrackX, double iTrackY, double iTrackH, double* iTrackCov, PosteriorPosePosition* iPosteriorPose)
{
	/*
	Converts a track's covariance matrix to a covariance in absolute coordinates,
	accounting for the current posterior pose solution and its accuracy.

	INPUTS:
		oAbsCov - will be populated by the transformed covariance on output.
		iTrack - the input track, whose covariance will be transformed
		iPosteriorPose - the current posterior pose solution

	OUTPUTS:
		oAbsCov - will be populated by the transformed covariance on output.
	*/

	int i;
	int j;
	int k;
	int nr;

	//extract the current track position
	double tx = iTrackX;
	double ty = iTrackY;
	double th = iTrackH;

	//extract the current posterior pose solution
	double pph = iPosteriorPose->HeadingMMSE;

	double cospph = cos(pph);
	double sinpph = sin(pph);

	//construct the full covariance matrix for all uncertain terms
	nr = nxt + PPP_NUMSTATES;
	double* Pfull = new double[nr*nr];
	for (i = 0; i < nr; i++)
	{
		for (j = 0; j < nr; j++)
		{
			Pfull[midx(i, j, nr)] = 0.0;
		}
	}
	for (i = 0; i < nxt; i++)
	{
		for (j = 0; j < nxt; j++)
		{
			Pfull[midx(i, j, nr)] = iTrackCov[midx(i, j, nxt)];
		}
	}
	for (i = nxt; i < nr; i++)
	{
		for (j = nxt; j < nr; j++)
		{
			Pfull[midx(i, j, nr)] = iPosteriorPose->CovarianceMMSE[midx(i-nxt, j-nxt, PPP_NUMSTATES)];
		}
	}

	//construct the jacobian for the transformation
	double* Jfull = new double[nxt*nr];
	for (i = 0; i < nxt; i++)
	{
		for (j = 0; j < nr; j++)
		{
			Jfull[midx(i, j, nxt)] = 0.0;
		}
	}
	Jfull[midx(0, 0, nxt)] = -sinpph;
	Jfull[midx(0, 1, nxt)] = -cospph;
	Jfull[midx(0, 5, nxt)] = 1.0;
	Jfull[midx(0, 7, nxt)] = -sinpph*tx - cospph*ty;
	Jfull[midx(1, 0, nxt)] = cospph;
	Jfull[midx(1, 1, nxt)] = -sinpph;
	Jfull[midx(1, 6, nxt)] = 1.0;
	Jfull[midx(1, 7, nxt)] = cospph*tx - sinpph*ty;
	Jfull[midx(2, 2, nxt)] = 1.0;
	Jfull[midx(3, 3, nxt)] = 1.0;
	Jfull[midx(4, 4, nxt)] = 1.0;
	Jfull[midx(4, 7, nxt)] = 1.0;

	//compute oAbsCov = Jfull*Pfull*Jfull'
	double* JP = new double[nxt*nr];
	for (i = 0; i < nxt; i++)
	{
		for (j = 0; j < nr; j++)
		{
			JP[midx(i, j, nxt)] = 0.0;
			for (k = 0; k < nr; k++)
			{
				JP[midx(i, j, nxt)] += Jfull[midx(i, j, nxt)] * Pfull[midx(k, j, nr)];
			}
		}
	}

	for (i = 0; i < nxt; i++)
	{
		for (j = 0; j < nxt; j++)
		{
			oAbsCov[midx(i, j, nxt)] = 0.0;
			for (k = 0; k < nr; k++)
			{
				oAbsCov[midx(i, j, nxt)] += JP[midx(i, k, nxt)] * Jfull[midx(j, k, nxt)];
			}
		}
	}

	delete [] Jfull;
	delete [] JP;
	delete [] Pfull;

	return;
}

void ClusterBcwBccwRmin(double z[3], double x[5], Cluster* iCluster, Sensor* iSensor)
{
	/*
	Computes the position correspondence measurement for a cluster of points.
	The measurement is calculated in sensor coordinates, for the given sensor.

	INPUTS:
		z - will be populated with the cluster measurement, [cwob, ccwob, rcp]
		x - the cluster's state [xref, yref, oref, ...]
		iCluster - pointer to a cluster structure containing the cluster
			information.  Each cluster point should be in an object storage
			frame.
		iSensor - (optional) the sensor structure to use to generate the cluster 
			measurement.  If not supplied, a null sensor is used.

	OUTPUTS:
		z - populated with the cluster measurement.
			NOTE: the clockwise boundary [0] is always the smallest angle, the 
				counterclockwise boundary [1] is always the largest angle.
	*/

	//initialize the measurements
	z[0] = DBL_MAX;
	z[1] = -DBL_MAX;
	z[2] = DBL_MAX;

	//check whether a sensor structure is given
	Sensor NullSensor;
	NullSensor.SensorID = INVALID_EVENT;
	NullSensor.SensorX = 0.0;
	NullSensor.SensorY = 0.0;
	NullSensor.SensorZ = 0.0;
	NullSensor.SensorYaw = 0.0;
	NullSensor.SensorPitch = 0.0;
	NullSensor.SensorRoll = 0.0;
	if (iSensor == NULL)
	{
		iSensor = &NullSensor;
	}

	//extract the state
	double xref = x[0];
	double yref = x[1];
	double oref = x[2];

	//declare variables for the coordinate transformations
	double osx;
	double osy;
	double evx;
	double evy;
	double scx;
	double scy;
	double sx = iSensor->SensorX;
	double sy = iSensor->SensorY;
	double cosSyaw = cos(iSensor->SensorYaw);
	double sinSyaw = sin(iSensor->SensorYaw);
	double cosOrient = cos(oref);
	double sinOrient = sin(oref);

	//pull the cluster points
	double* iClusterPoints = iCluster->Points;

	int i;
	int np = iCluster->NumPoints;
	//wrap all bearings to the bearing of the anchor point, in sensor coordinates
	evx = xref;
	evy = yref;
	EgoVehicleToSensor(scx, scy, evx, evy, cosSyaw, sinSyaw, sx, sy);
	double wraptarget = atan2(scy, scx);

	//calculate components of x-target direction (toward anchor point)
	//in sensor coordinates
	double xtx = scx;
	double xty = scy;
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
		osx = iClusterPoints[midx(i, 0, np)];
		osy = iClusterPoints[midx(i, 1, np)];
		ObjectToEgoVehicle(evx, evy, osx, osy, cosOrient, sinOrient, xref, yref);

		//transform the cluster point to sensor coordinates
		EgoVehicleToSensor(scx, scy, evx, evy, cosSyaw, sinSyaw, sx, sy);

		//compute the point's range in sensor coordinates
		double rng = sqrt(scx*scx + scy*scy);

		//project each obstacle point into the target coordinates to see if it is
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

			//wrap the angle to the same 2pi branch as the anchor point
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

void ClusterExtremePoints(double z[6], double x[5], Cluster* iCluster, Sensor* iSensor)
{
	/*
	Computes the x-y locations of the extreme points in a cluster.
	The measurement is calculated in sensor coordinates, for the given sensor.

	INPUTS:
		z - will be populated with the cluster measurement, [cwx, cwy, ccwx, ccwy, cpx, cpy]
		x - the cluster's state [xref, yref, oref, ...]
		iCluster - pointer to a cluster structure containing the cluster
			information.  Each cluster point should be in an object storage
			frame.
		iSensor - (optional) the sensor structure to use to generate the cluster 
			measurement.  If not supplied, a null sensor is used.

	OUTPUTS:
		z - populated with the cluster measurement.
			NOTE: the clockwise boundary [0] is always the smallest angle, the 
				counterclockwise boundary [1] is always the largest angle.
	*/

	//initialize the measurements
	double bmin = DBL_MAX;
	double bmax = -DBL_MAX;
	double rmin = DBL_MAX;
	z[0] = 0.0;
	z[1] = 0.0;
	z[2] = 0.0;
	z[3] = 0.0;
	z[4] = 0.0;
	z[5] = 0.0;

	//check whether a sensor structure is given
	Sensor NullSensor;
	NullSensor.SensorID = INVALID_EVENT;
	NullSensor.SensorX = 0.0;
	NullSensor.SensorY = 0.0;
	NullSensor.SensorZ = 0.0;
	NullSensor.SensorYaw = 0.0;
	NullSensor.SensorPitch = 0.0;
	NullSensor.SensorRoll = 0.0;
	if (iSensor == NULL)
	{
		iSensor = &NullSensor;
	}

	//extract the state
	double xref = x[0];
	double yref = x[1];
	double oref = x[2];

	//declare variables for the coordinate transformations
	double osx;
	double osy;
	double evx;
	double evy;
	double scx;
	double scy;
	double sx = iSensor->SensorX;
	double sy = iSensor->SensorY;
	double cosSyaw = cos(iSensor->SensorYaw);
	double sinSyaw = sin(iSensor->SensorYaw);
	double cosOrient = cos(oref);
	double sinOrient = sin(oref);

	//pull the cluster points
	double* iClusterPoints = iCluster->Points;

	int i;
	int np = iCluster->NumPoints;
	//wrap all bearings to the bearing of the anchor point, in sensor coordinates
	evx = xref;
	evy = yref;
	EgoVehicleToSensor(scx, scy, evx, evy, cosSyaw, sinSyaw, sx, sy);
	double wraptarget = atan2(scy, scx);

	//calculate components of x-target direction (toward anchor point)
	//in sensor coordinates
	double xtx = scx;
	double xty = scy;
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
		osx = iClusterPoints[midx(i, 0, np)];
		osy = iClusterPoints[midx(i, 1, np)];
		ObjectToEgoVehicle(evx, evy, osx, osy, cosOrient, sinOrient, xref, yref);

		//transform the cluster point to sensor coordinates
		EgoVehicleToSensor(scx, scy, evx, evy, cosSyaw, sinSyaw, sx, sy);

		//compute the point's range in sensor coordinates
		double rng = sqrt(scx*scx + scy*scy);

		//project each obstacle point into the target coordinates to see if it is
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

			//wrap the angle to the same 2pi branch as the anchor point
			double ang = WrapAngle(atan2(scy, scx), wraptarget);
			if (ang < bmin)
			{
				bmin = ang;
				//store the x-y location of the minimum bearing
				z[0] = scx;
				z[1] = scy;
			}
			if (ang > bmax)
			{
				bmax = ang;
				//store the x-y location of the maximum bearing
				z[2] = scx;
				z[3] = scy;
			}
		}

		//compare range to the measurements found so far
		if (rng < rmin)
		{
			rmin = rng;

			//store the x-y location of the closest point
			z[4] = scx;
			z[5] = scy;
		}
	}

	//calculate max and min bearing from all the normal points
	if (fabs(maxs) != DBL_MAX)
	{
		double ang = atan2(maxscy, maxscx);
		ang = WrapAngle(ang, wraptarget);
		if (ang < bmin)
		{
			bmin = ang;
			//store the x-y location of the minimum bearing
			z[0] = maxscx;
			z[1] = maxscy;
		}
		if (ang > bmax)
		{
			bmax = ang;
			//store the x-y location of the maximum bearing
			z[2] = maxscx;
			z[3] = maxscy;
		}
	}
	if (fabs(mins) != DBL_MAX)
	{
		double ang = atan2(minscy, minscx);
		ang = WrapAngle(ang, wraptarget);
		if (ang < bmin)
		{
			bmin = ang;
			//store the x-y location of the minimum bearing
			z[0] = minscx;
			z[1] = minscy;
		}
		if (ang > bmax)
		{
			bmax = ang;
			//store the x-y location of the maximum bearing
			z[2] = minscx;
			z[3] = minscy;
		}
	}

	return;
}

void ClusterPositionMeasurement(double z[3], double Pzz[3*3], double Pxz[5*3], double x[5], double P[5*5], Cluster* iCluster)
{
	/*
	Computes the expected position correspondence measurement and covariance matrix for
	a cluster.

	INPUTS:
		z, Pzz - [bmin, bmax, rmin] measurement for the tester target and its covariance
		Pxz - covariance of the state with the measurement
		x, P - state [xref, yref, oref, ...] and state covariance
		iCluster - pointer to the cluster data, which should be in object storage frame.

	OUTPUTS:
		z, R - [bmin, bmax, rmin] measurement and its covariance, approximately calculated
			via the sigma point method.
	*/

	int i;
	int j;
	int k;

	int nz = 3;
	int nx = 5;
	bool zwrap[3] = {true, true, false};
	//sigma point tuning parameters
	double nxd = (double) (nx);
	int nsp = 2*nx + 1;
	double spf_alpha = SPF_ALPHA;
	double spf_beta = SPF_BETA;
	double spf_kappa = SPF_KAPPA;
	double spf_lambda = spf_alpha*spf_alpha*(nxd + spf_kappa) - nxd;

	//calculate the sigma-point likelihood for the given measurement function

	//create the sigma points for the state
	//Cholesky factorize Pbar: Sx = chol(P)'
	int info;
	double* Sx = new double[nx*nx];
	for (i = 0; i < nx; i++)
	{
		for (j = 0; j < nx; j++)
		{
			Sx[midx(i, j, nx)] = P[midx(i, j, nx)];
		}
	}
	dpotrf('U', nx, Sx, nx, &info);
	if (info != 0)
	{
		//exit on ACML error
		printf("Warning: dpotrf error in ClusterPositionMeasurement.\n");
		delete [] Sx;

		return;
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
	double* xsp = new double[nx*nsp];
	double spfact = sqrt(nxd + spf_lambda);
	for (i = 0; i < nx; i++)
	{
		for (j = 0; j < nsp; j++)
		{
			xsp[midx(i, j, nx)] = x[i];
			if (j > 0 && j <= nx)
			{
				xsp[midx(i, j, nx)] += spfact * Sx[midx(i, j - 1, nx)];
			}
			else if (j > nx)
			{
				xsp[midx(i, j, nx)] -= spfact * Sx[midx(i, j - nx - 1, nx)];
			}
		}
	}

	//measurement sigma points for 2 ob's + range
	double* zsp = new double[nsp*nz];
	double* ztemp = new double[nz];
	double* xtemp = new double[nx];
	for (i = 0; i < nsp; i++)
	{
		//create a measurement for each x-sigma point

		for (j = 0; j < nx; j++)
		{
			xtemp[j] = xsp[midx(j, i, nx)];
		}

		ClusterBcwBccwRmin(ztemp, xtemp, iCluster);

		//copy the measurement into zsp_bar
		for (j = 0; j < nz; j++)
		{
			zsp[midx(j, i, nz)] = ztemp[j];
			if (zwrap[j] == true)
			{
				//wrap all angle measurements toward the main sigma point
				zsp[midx(j, i, nz)] = WrapAngle(zsp[midx(j, i, nz)], zsp[midx(j, 0, nz)]);
			}
		}
	}

	double Wmspf_1 = spf_lambda / (nxd + spf_lambda);
	double Wmspf_n = 0.5 / (nxd + spf_lambda);
	for (i = 0; i < nz; i++)
	{
		z[i] = Wmspf_1 * zsp[midx(i, 0, nz)];
		for (j = 1; j < nsp; j++)
		{
			z[i] += Wmspf_n*zsp[midx(i, j, nz)];
		}
	}

	//calculate measurement covariance Pzz (R) ~ HPH'
	double Wcspf_1 = Wmspf_1 + 1.0 - spf_alpha*spf_alpha + spf_beta;
	double Wcspf_n = Wmspf_n;
	for (i = 0; i < nz; i++)
	{
		for (j = 0; j < nz; j++)
		{
			Pzz[midx(i, j, nz)] = Wcspf_1*(zsp[midx(i, 0, nz)] - z[i])*(zsp[midx(j, 0, nz)] - z[j]);
		}
	}
	for (i = 1; i < nsp; i++)
	{
		for (j = 0; j < nz; j++)
		{
			for (k = 0; k < nz; k++)
			{
				Pzz[midx(j, k, nz)] += Wcspf_n*(zsp[midx(j, i, nz)] - z[j])*(zsp[midx(k, i, nz)] - z[k]);
			}
		}
	}

	//calculate measurement-state cross correlation Pxz ~ PH'
	//calculate Pxz ~PH', nx x nz matrix
	for (i = 0; i < nx; i++)
	{
		for (j = 0; j < nz; j++)
		{
			Pxz[midx(i, j, nx)] = Wcspf_1*(xsp[midx(i, 0, nx)] - x[i])*(zsp[midx(j, 0, nz)] - z[j]);
		}
	}
	for (i = 1; i < nsp; i++)
	{
		for (j = 0; j < nx; j++)
		{
			for (k = 0; k < nz; k++)
			{
				Pxz[midx(j, k, nx)] += Wcspf_n*(xsp[midx(j, i, nx)] - x[j])*(zsp[midx(k, i, nz)] - z[k]);
			}
		}
	}

	//deallocate memory
	delete [] Sx;
	delete [] xsp;
	delete [] zsp;
	delete [] ztemp;
	delete [] xtemp;

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

void TrackDynamics(double* xdot, double* Jx, double* Jv, double* x, VehicleOdometry* iVehicleOdometry)
{
	/*
	Evaluates the dynamics function for a track generator track.

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

void PredictPosteriorPoseSolution(double iPredictTime, PosteriorPosePosition* iPosteriorPose, VehicleOdometry* iVehicleOdometry)
{
	/*
	Predicts a posterior pose position solution to a desired time.

	INPUTS:
		iPredictTime - the time to predict the solution to.
		iPosteriorPose - the posterior pose position valid at the beginning
			of the prediction interval
		iVehicleOdometry - the vehicle odometry information valid over
			the prediction interval

	OUTPUTS:
		iPosteriorPose - will contain the new posterior pose solution with
			updated timestamp and variables.  Check IsValid to see whether
			the prediction was successful.
	*/

	if (iVehicleOdometry->IsValid == false)
	{
		//can't predict without vehicle odometry
		iPosteriorPose->IsValid = false;
		return;
	}

	if (iPosteriorPose->IsValid == false)
	{
		//can't predict without a valid input condition
		return;
	}

	double dt = iPredictTime - iPosteriorPose->PosteriorPoseTime;
	if (fabs(dt) == 0.0)
	{
		//solution is already valid; don't do any prediction
		return;
	}
	//NOTE: backwards prediction is ok.

	int i;
	int j;

	int nx = PPP_NUMSTATES;
	int nv = 3;

	//declare space for the output of the prediction
	double xbar[PPP_NUMSTATES];
	double Pbar[PPP_NUMSTATES*PPP_NUMSTATES];
	//extract state information
	double x[PPP_NUMSTATES] = {iPosteriorPose->EastMMSE, iPosteriorPose->NorthMMSE, iPosteriorPose->HeadingMMSE};
	double P[PPP_NUMSTATES*PPP_NUMSTATES];
	for (i = 0; i < PPP_NUMSTATES; i++)
	{
		for (j = 0; j < PPP_NUMSTATES; j++)
		{
			P[midx(i, j, PPP_NUMSTATES)] = iPosteriorPose->CovarianceMMSE[midx(i, j, PPP_NUMSTATES)];
		}
	}
	//extract noise matrix in discrete time
	double Q[3*3];
	for (i = 0; i < nv; i++)
	{
		for (j = 0; j < nv; j++)
		{
			Q[midx(i, j, nv)] = 0.0;
		}
	}
	Q[midx(0, 0, nv)] = iVehicleOdometry->qvx / fabs(dt);
	Q[midx(1, 1, nv)] = iVehicleOdometry->qvy / fabs(dt);
	Q[midx(2, 2, nv)] = iVehicleOdometry->qwz / fabs(dt);

	//predict the posterior pose state to the desired time
	KalmanPredict(xbar, Pbar, nx, nv, dt, x, P, Q, iVehicleOdometry, &PosteriorPoseDynamics);

	//extract the predicted state and covariance back into the posterior pose position
	iPosteriorPose->PosteriorPoseTime = iPredictTime;
	iPosteriorPose->EastMMSE = xbar[0];
	iPosteriorPose->NorthMMSE = xbar[1];
	iPosteriorPose->HeadingMMSE = xbar[2];
	for (i = 0; i < PPP_NUMSTATES; i++)
	{
		for (j = 0; j < PPP_NUMSTATES; j++)
		{
			iPosteriorPose->CovarianceMMSE[midx(i, j, PPP_NUMSTATES)] = Pbar[midx(i, j, PPP_NUMSTATES)];
		}
	}

	return;
}

void PosteriorPoseDynamics(double* xdot, double* Jx, double* Jv, double* x, VehicleOdometry* iVehicleOdometry)
{
	/*
	Dynamics function for a posterior pose solution.

	INPUTS:
		xdot, Jx, Jv - will contain the state derivative, state jacobian, and noise jacobian
		x - current state
		iVehicleOdometry - current vehicle odometry information

	OUTPUS:
		xdot, Jx, Jv - will contain the state, state jacobian, and noise jacobian
	*/

	int nx = 3;
	int nv = 3;

	int i;
	int j;

	//extract the state
	double pe = x[0];
	double pn = x[1];
	double ph = x[2];

	//extract the odometry
	double vx = iVehicleOdometry->vx;
	double vy = iVehicleOdometry->vy;
	double wz = iVehicleOdometry->wz;

	double cos_h = cos(ph);
	double sin_h = sin(ph);

	double evy = 0.0;
	double evx = 0.0;
	double ewz = 0.0;

	//compute the state derivative
	for (i = 0; i < nx; i++)
	{
		xdot[i] = 0.0;
	}
	xdot[0] = cos_h*(vx + evx) - sin_h*(vy + evy);
	xdot[1] = sin_h*(vx + evx) + cos_h*(vy + evy);
	xdot[2] = wz + ewz;

	//define the state jacobian
	for (i = 0; i < nx; i++)
	{
		for (j = 0; j < nx; j++)
		{
			Jx[midx(i, j, nx)] = 0.0;
		}
	}
	Jx[midx(0, 2, nx)] = -sin_h*(vx + evx) - cos_h*(vy + evy);
	Jx[midx(1, 2, nx)] = cos_h*(vx + evx) - sin_h*(vy + evy);

	//define the input jacobian
	for (i = 0; i < nx; i++)
	{
		for (j = 0; j < nv; j++)
		{
			Jv[midx(i, j, nx)] = 0.0;
		}
	}
	Jv[midx(0, 0, nx)] = cos_h;
	Jv[midx(0, 1, nx)] = -sin_h;
	Jv[midx(1, 0, nx)] = sin_h;
	Jv[midx(1, 1, nx)] = cos_h;
	Jv[midx(2, 2, nx)] = 1.0;

	return;
}

void DynamicPointDynamics(double* xdot, double* Jx, double* Jv, double* x, VehicleOdometry* iVehicleOdometry)
{
	/*
	Evaluates the dynamics function for a dynamic point with a constant velocity

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
