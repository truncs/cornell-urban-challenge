#ifndef SCENEESTIMATORLOGWRITER_H
#define SCENEESTIMATORLOGWRITER_H

#include "Event.h"
#include "EventCodes.h"
#include "EventQueue.h"
#include "Globals.h"
#include "MatrixIndex.h"
#include "SceneEstimatorConstants.h"
#include "WriterLogFile.h"

#include <STDIO.H>
#include <TIME.H>
#include <WINDOWS.H>

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

//size of filenames
#define SE_LOGWRITER_FIELDSIZE 1024
//time to wait for the writer to quit (ms)
#define SE_LOGWRITER_SHUTDOWNTIMEOUT 500

class SceneEstimatorLogWriter
{
	//The scene estimator log writer class.  Writes scene estimator logs to files.

private:

	//boolean telling the logging thread to continue
	bool mIsRunning;
	//the logging thread
	HANDLE mLoggingThread;

	//relative pose log
	WriterLogFile mOdometryLog;
	//absolute pose log
	WriterLogFile mPoseLog;
	//stopline camera log
	WriterLogFile mStoplineLog;
	//front mobileye road log
	WriterLogFile mFrontMobileyeRoadLog;
	//back mobileye road log
	WriterLogFile mBackMobileyeRoadLog;
	//front roadfinder log
	WriterLogFile mFrontJasonRoadLog;
	//back roadfinder log
	WriterLogFile mBackJasonRoadLog;

public:

	SceneEstimatorLogWriter(char* iLogDirectory = NULL);
	~SceneEstimatorLogWriter();

	bool IsRunning(void) {return mIsRunning;}
	void StopRunning(void) {mIsRunning = false; return;}

	void LogEvent(Event* iEvent);
};

//the log writer callback function
static DWORD WINAPI SceneEstimatorLogWriterCallback(LPVOID lpparam);

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //SCENEESTIMATORLOGWRITER_H
