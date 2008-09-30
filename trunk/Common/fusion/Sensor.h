#ifndef SENSOR_H
#define SENSOR_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

struct Sensor
{
	//The sensor structure.  Contains information about the orientation of the sensor
	//on the vehicle.

	//the sensor's ID on the vehicle (what type of events it generates)
	int SensorID;

	//the sensor's location wrt vehicle coordinates
	double SensorX;
	double SensorY;
	double SensorZ;

	//the sensor's orientation wrt vehicle coordinates (yaw, pitch, roll)
	double SensorYaw;
	double SensorPitch;
	double SensorRoll;
	Sensor () {}
	Sensor (double X, double Y, double Z, double yaw, double pitch, double roll, int ID)
	{
		SensorX=X; SensorY=Y; SensorZ=Z; SensorYaw = yaw; SensorPitch = pitch; SensorRoll = roll; SensorID = ID;
	}
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //SENSOR_H
