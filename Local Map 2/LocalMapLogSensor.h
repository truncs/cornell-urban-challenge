#ifndef LOCAMAPLOGSENSOR_H
#define LOCAMAPLOGSENSOR_H

#include "CarTime.h"
#include "Event.h"
#include "EventCodes.h"
#include "Globals.h"
#include "LocalMapConstants.h"
#include "LocalMapFunctions.h"
#include "MatrixIndex.h"
#include "RelativePoseQueue.h"
#include "ReaderLogFile.h"
#include "SynchronizedEventQueue.h"

#include <FLOAT.H>
#include <MEMORY.H>
#include <STDIO.H>
#include <WINDOWS.H>

#define LS_BUFFERSIZE 2048

class LocalMapLogSensor
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
	ReaderLogFile FrontMobileyeObstacleLog;
	ReaderLogFile FrontMobileyeRoadLog;
	ReaderLogFile BackMobileyeObstacleLog;
	ReaderLogFile BackMobileyeRoadLog;
	ReaderLogFile FrontJasonRoadLog;
	ReaderLogFile BackJasonRoadLog;
	ReaderLogFile IbeoLog;
	ReaderLogFile BackClusteredSickLog;
	ReaderLogFile Front0RadarLog;
	ReaderLogFile Driv0RadarLog;
	ReaderLogFile Driv1RadarLog;
	ReaderLogFile Driv2RadarLog;
	ReaderLogFile Driv3RadarLog;
	ReaderLogFile Pass0RadarLog;
	ReaderLogFile Pass1RadarLog;
	ReaderLogFile Pass2RadarLog;
	ReaderLogFile Pass3RadarLog;
	ReaderLogFile DrivSideSickLog;
	ReaderLogFile PassSideSickLog;

	LocalMapLogSensor(DWORD iSamplePeriod, char* iOdomLogName, 
		char* iFrontMobileyeObstacleLogName, char* iFrontMobileyeRoadLogName, 
		char* iBackMobileyeObstacleLogName, char* iBackMobileyeRoadLogName, 
		char* iFrontJasonRoadLogName, char* iBackJasonRoadLogName, 
		char* iIbeoLogName, char* iBackClusteredSickLogName, 
		char* iDrivSideSickLogName, char* iPassSideSickLogName, 
		char* iFront0RadarLogName, 
		char* iDriv0RadarLogName, char* iDriv1RadarLogName, char* iDriv2RadarLogName, char* iDriv3RadarLogName, 
		char* iPass0RadarLogName, char* iPass1RadarLogName, char* iPass2RadarLogName, char* iPass3RadarLogName);
	~LocalMapLogSensor();

	bool IsRunning() {return mIsRunning;}
	DWORD SamplePeriod() {return mSamplePeriod;}
	void StopRunning() {mIsRunning = false; return;}
};

//the sensor callback function
static DWORD WINAPI LogCallback(LPVOID lpparam);

#endif //LOCALMAPLOGSENSOR_H
