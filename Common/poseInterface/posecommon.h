
#pragma once
struct simpleVehicleState
{
	//vehicle coordinates, delta angle (yaw rate = drZ / dt)
	double dRY;
	double dRX;
	double dRZ;

	//vehicle coordinates, delta pos (forward speed = dX / dt)
	double dX;
	double dY;
	double dZ;

	double timestamp;
	double dt;
};