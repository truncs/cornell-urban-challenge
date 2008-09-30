#ifndef SCENEESTIMATORLOGSENSOR_H
#define SCENEESTIMATORLOGSENSOR_H

#include "Event.h"
#include "EventCodes.h"
#include "Globals.h"
#include "ReaderLogFile.h"
#include "MatrixIndex.h"
#include "RelativePoseQueue.h"
#include "SceneEstimatorConstants.h"
#include "SceneEstimatorFunctions.h"
#include "SynchronizedEventQueue.h"

#include <FLOAT.H>
#include <MEMORY.H>
#include <STDIO.H>
#include <STRING.H>
#include <WINDOWS.H>

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

//a buffer constant for character arrays
#define LS_BUFFERSIZE 2048
//maximum number of target points per packet read
#define LS_MAXTARGETPOINTS 5000

class SceneEstimatorLogSensor
{
	//handles interfacing to a set of log files

private:

	//the callback thread that acts as the interface to the sensors
	HANDLE mInterfaceThread;
	//whether the thread is running or should be terminated
	bool mIsRunning;
	//the sample period, in milliseconds (how long to wait between log reads)
	DWORD mSamplePeriod;

public:

	//the log file readers
	ReaderLogFile OdomLog;
	ReaderLogFile PoseLog;
	ReaderLogFile FrontMobileyeRoadLog;
	ReaderLogFile BackMobileyeRoadLog;
	ReaderLogFile FrontJasonRoadLog;
	ReaderLogFile BackJasonRoadLog;
	ReaderLogFile StoplineLog;
	ReaderLogFile LocalMapLog;
	ReaderLogFile LocalPointsLog;

	SceneEstimatorLogSensor(DWORD iSamplePeriod, char* iOdomLogName, char* iPoseLogName, 
		char* iFrontMobileyeRoadLogName, char* iBackMobileyeRoadLogName, 
		char* iFrontJasonRoadLogName, char* iBackJasonRoadLogName, 
		char* iStoplineLogName, char* iLocalMapLogName, char* iLocalPointsLogName);
	~SceneEstimatorLogSensor();

	bool IsRunning() {return mIsRunning;}
	DWORD SamplePeriod() {return mSamplePeriod;}
	void StopRunning() {mIsRunning = false; return;}
};

//the sensor callback function
static DWORD WINAPI LogCallback(LPVOID lpparam);

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //SCENEESTIMATORLOGSENSOR_H
