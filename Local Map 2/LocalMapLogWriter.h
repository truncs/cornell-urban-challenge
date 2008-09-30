#ifndef LOCALMAPLOGWRITER_H
#define LOCALMAPLOGWRITER_H

#include "Event.h"
#include "EventCodes.h"
#include "EventQueue.h"
#include "Globals.h"
#include "LocalMapConstants.h"
#include "LocalMapFunctions.h"
#include "MatrixIndex.h"
#include "WriterLogFile.h"

#include <STDIO.H>
#include <TIME.H>
#include <WINDOWS.H>

//size of filenames
#define LM_LOGWRITER_FIELDSIZE 1024
//time to wait for the writer to quit (ms)
#define LM_LOGWRITER_SHUTDOWNTIMEOUT 500

class LocalMapLogWriter
{
	//The local map log writer class.  Writes local map logs to files.

private:

	//boolean telling the logging thread to continue
	bool mIsRunning;
	//the logging thread
	HANDLE mLoggingThread;

	//relative pose log
	WriterLogFile mOdometryLog;
	//clustered ibeo log
	WriterLogFile mClusteredIbeoLog;
	//side SICK logs
	WriterLogFile mDrivSickLog;
	WriterLogFile mPassSickLog;
	//back horizontal SICK log
	WriterLogFile mClusteredSickLog;
	//mobileye obstacle logs
	WriterLogFile mFrontMobileyeObstacleLog;
	WriterLogFile mBackMobileyeObstacleLog;
	//mobileye road logs
	WriterLogFile mFrontMobileyeRoadLog;
	WriterLogFile mBackMobileyeRoadLog;
	//jason roadfinder logs
	WriterLogFile mFrontRoadFinderLog;
	WriterLogFile mBackRoadFinderLog;
	//radars
	WriterLogFile mFront0RadarLog;
	WriterLogFile mDriv0RadarLog;
	WriterLogFile mDriv1RadarLog;
	WriterLogFile mDriv2RadarLog;
	WriterLogFile mDriv3RadarLog;
	WriterLogFile mPass0RadarLog;
	WriterLogFile mPass1RadarLog;
	WriterLogFile mPass2RadarLog;
	WriterLogFile mPass3RadarLog;

public:

	LocalMapLogWriter(char* iLogDirectory = NULL);
	~LocalMapLogWriter();

	bool IsRunning() {return mIsRunning;}
	void StopRunning() {mIsRunning = false; return;}

	void LogEvent(Event* iEvent);
};

//the log writer callback function
static DWORD WINAPI LocalMapLogWriterCallback(LPVOID lpparam);

#endif //LOCALMAPLOGWRITER_H
