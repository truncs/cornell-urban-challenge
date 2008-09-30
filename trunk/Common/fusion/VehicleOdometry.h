#ifndef VEHICLEODOMETRY_H
#define VEHICLEODOMETRY_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

struct VehicleOdometry
{
	//The vehicle odometry structure - stores average vehicle odometry over an interval.

	//whether the odometry is valid
	bool IsValid;
	//the start time of the transformation
	double StartTime;
	//the end time of the transformation
	double EndTime;
	//the transformation interval (the length of time this odometry is averaged)
	double dt;
	//vehicle rates of rotation (rad / sec., in vehicle axes)
	double wx;
	double wy;
	double wz;
	//vehicle velocities (m/sec., in vehicle axes)
	double vx;
	double vy;
	double vz;
	//variances on rates of rotation (assume zero mean white noise)
	double qwx;
	double qwy;
	double qwz;
	//variances on velocities (assume zero mean white noise)
	double qvx;
	double qvy;
	double qvz;
	//incremental transformation matrix between the old and the current frame
	double Rvkmo2vk[16];
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //VEHICLEODOMETRY_H
