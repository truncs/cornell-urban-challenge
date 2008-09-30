#pragma once

#include <string>

//this file provides the methods that will go from the unmanaged C++ code of the scene estimator to the managed publisher
//specifically for the Operational communication

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

using namespace std;

struct UnmanagedOperationalMessage
{		
	double  timestamp;				
	bool		isModelValid;			
		
	double	roadHeading;			
	double	roadCurvature;		
	double	roadHeadingVar;   
	double	roadCurvatureVar; 

	string  centerLaneID;			
	string  leftLaneID;				
	string  rightLaneID;

	double 	centerLaneCenter;
  double	leftLaneCenter;
	double	rightLaneCenter;

	double  centerLaneWidth;
	double  leftLaneWidth;
	double  rightLaneWidth;
	
	bool		leftLaneExists;
	bool    rightLaneExists;
	bool		centerLaneExists;

	double 	centerLaneCenterVar;
  double	leftLaneCenterVar;
	double	rightLaneCenterVar;

	double  centerLaneWidthVar;
	double  leftLaneWidthVar;
	double  rightLaneWidthVar;
	
	bool		stopLineExists;
	double	distToStopline;
	double	distToStoplineVar;

};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif
