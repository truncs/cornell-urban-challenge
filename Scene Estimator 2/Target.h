#ifndef TARGET_H
#define TARGET_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

struct Target
{
	//a target structure for holding LocalMap targets

	double X;
	double Y;
	double Orientation;
	double Speed;
	double Heading;

	double* Covariance;
	int NumPoints;
	double* TargetPoints;

	//position measurement data (cached due to expense of calculating)
	double zp[3];
	double Pzzp[3*3];
	double Pxzp[5*3];
	//NOTE: speed-heading measurement data is free to calculate
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //TARGET_H
