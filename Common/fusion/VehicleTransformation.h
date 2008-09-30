#ifndef VEHICLETRANSFORMATION_H
#define VEHICLETRANSFORMATION_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

struct VehicleTransformation
{
	//The vehicle transformation structure - stores the vehicle transformation 
	//from one frame to another.

	//whether the odometry is valid
	bool IsValid;
	//the start time of the transformation
	double StartTime;
	//the end time of the transformation
	double EndTime;
	//the transformation interval (the length of time this odometry is averaged)
	double dt;
	//the vehicle transformation matrix
	double T[16];
	//the covariance matrix for each element in T
	double PTT[16*16];
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //VEHICLETRANSFORMATION_H
