#include "SceneEstimatorLogWriter.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

SceneEstimatorLogWriter::SceneEstimatorLogWriter(char* iLogDirectory)
{
	/*
	Default constructor for log writer.  Opens all log files for writing and
	creates the callback thread for writing.

	INPUTS:
		iLogDirectory - the directory in which the logs are to be created.  If
			not supplied, the working directory is used.

	OUTPUTS:
		none.  Opens desired files
	*/

	extern EventQueue* TheLoggingQueue;
	TheLoggingQueue = NULL;

	//***OPEN LOGS***

	char fname[SE_LOGWRITER_FIELDSIZE];
	errno_t err;

	//prepend all file names with the directory
	char dirbuff[SE_LOGWRITER_FIELDSIZE];
	if (DirectoryExists(iLogDirectory) == true)
	{
		strcpy_s(dirbuff, SE_LOGWRITER_FIELDSIZE, iLogDirectory);
		//the directory exists, but check for a terminating backslash
		int dlen = (int) strlen(iLogDirectory);
		if (iLogDirectory[dlen-1] != '\\' && iLogDirectory[dlen-1] != '/')
		{
			//need to add a terminating backslash
			strcat_s(dirbuff, SE_LOGWRITER_FIELDSIZE, "\\");
		}
	}
	else
	{
		dirbuff[0] = '\0';
	}

	//get the system time for timing all the log files
	char timebuff[SE_LOGWRITER_FIELDSIZE];
	struct tm today;

	time_t ltime;
	time(&ltime);
	err = _localtime64_s(&today, &ltime);
	if (err != 0)
	{
		printf("Warning: log writer could not successfully open log files.\n");
		return;
	}
	strftime(timebuff, SE_LOGWRITER_FIELDSIZE, "%m-%d-%y %I.%M.%S %p", &today);

	//odometry file
	strcpy_s(fname, SE_LOGWRITER_FIELDSIZE, dirbuff);
	strcat_s(fname, SE_LOGWRITER_FIELDSIZE, "odom ");
	strcat_s(fname, SE_LOGWRITER_FIELDSIZE, timebuff);
	strcat_s(fname, SE_LOGWRITER_FIELDSIZE, ".txt");
	mOdometryLog.OpenLog(fname);

	//pose file
	strcpy_s(fname, SE_LOGWRITER_FIELDSIZE, dirbuff);
	strcat_s(fname, SE_LOGWRITER_FIELDSIZE, "pose ");
	strcat_s(fname, SE_LOGWRITER_FIELDSIZE, timebuff);
	strcat_s(fname, SE_LOGWRITER_FIELDSIZE, ".txt");
	mPoseLog.OpenLog(fname);

	//stopline file
	strcpy_s(fname, SE_LOGWRITER_FIELDSIZE, dirbuff);
	strcat_s(fname, SE_LOGWRITER_FIELDSIZE, "stopline ");
	strcat_s(fname, SE_LOGWRITER_FIELDSIZE, timebuff);
	strcat_s(fname, SE_LOGWRITER_FIELDSIZE, ".txt");
	mStoplineLog.OpenLog(fname);

	//front mobileye file
	strcpy_s(fname, SE_LOGWRITER_FIELDSIZE, dirbuff);
	strcat_s(fname, SE_LOGWRITER_FIELDSIZE, "frontmobileyeroad ");
	strcat_s(fname, SE_LOGWRITER_FIELDSIZE, timebuff);
	strcat_s(fname, SE_LOGWRITER_FIELDSIZE, ".txt");
	mFrontMobileyeRoadLog.OpenLog(fname);

	//back mobileye file
	strcpy_s(fname, SE_LOGWRITER_FIELDSIZE, dirbuff);
	strcat_s(fname, SE_LOGWRITER_FIELDSIZE, "backmobileyeroad ");
	strcat_s(fname, SE_LOGWRITER_FIELDSIZE, timebuff);
	strcat_s(fname, SE_LOGWRITER_FIELDSIZE, ".txt");
	mBackMobileyeRoadLog.OpenLog(fname);

	//front jason file
	strcpy_s(fname, SE_LOGWRITER_FIELDSIZE, dirbuff);
	strcat_s(fname, SE_LOGWRITER_FIELDSIZE, "frontjasonroad ");
	strcat_s(fname, SE_LOGWRITER_FIELDSIZE, timebuff);
	strcat_s(fname, SE_LOGWRITER_FIELDSIZE, ".txt");
	mFrontJasonRoadLog.OpenLog(fname);

	//back jason file
	strcpy_s(fname, SE_LOGWRITER_FIELDSIZE, dirbuff);
	strcat_s(fname, SE_LOGWRITER_FIELDSIZE, "backjasonroad ");
	strcat_s(fname, SE_LOGWRITER_FIELDSIZE, timebuff);
	strcat_s(fname, SE_LOGWRITER_FIELDSIZE, ".txt");
	mBackJasonRoadLog.OpenLog(fname);

	//***CREATE LOGGING EVENT QUEUE***

	//create the log writer event queue
	TheLoggingQueue = new EventQueue();

	//***START THE LOGGING THREAD RUNNING***

	//begin the thread running
	mIsRunning = true;

	//create the reader thread that will provide the sensor data
	mLoggingThread = CreateThread(NULL, 0, SceneEstimatorLogWriterCallback, this, 0, NULL);

	printf("Log writer running.\n");

	return;
}

SceneEstimatorLogWriter::~SceneEstimatorLogWriter()
{
	/*
	Destructor for the log writer.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	//stop the thread from running
	mIsRunning = false;
	//wait for the thread to terminate itself
	printf("Waiting for log writing thread to terminate...\n");
	if (WaitForSingleObjectEx(mLoggingThread, SE_LOGWRITER_SHUTDOWNTIMEOUT, false) != WAIT_OBJECT_0)
	{
		printf("Warning: log writing thread timed out, terminating forcefully...\n");
		TerminateThread(mLoggingThread, 2);
	}
	//destroy the thread associated with the log writer
	CloseHandle(mLoggingThread);
	printf("Log writing thread terminated successfully.\n");

	//delete the log writer event queue
	extern EventQueue* TheLoggingQueue;
	delete TheLoggingQueue;

	return;
}

void SceneEstimatorLogWriter::LogEvent(Event* iEvent)
{
	/*
	Logs an event to the appropriate log file based on its type

	INPUTS:
		iEvent - an event object containing the relevant event data
	*/

	int i;
	int j;
	int ne = iEvent->NumDataRows();
	char buff[SE_LOGWRITER_FIELDSIZE];
	char packet[SE_LOGWRITER_FIELDSIZE];
	WriterLogFile* TheLogFile = NULL;
	double* ed = iEvent->EventData;

	switch (iEvent->EventType())
	{
	case ODOM_EVENT:

		sprintf_s(packet, SE_LOGWRITER_FIELDSIZE, "%.12lg", ed[0]);
		for (j = 1; j < SE_ODOMPACKETSIZE; j++)
		{
			sprintf_s(buff, SE_LOGWRITER_FIELDSIZE, ",%.12lg", ed[j]);
			strcat_s(packet, SE_LOGWRITER_FIELDSIZE, buff);
		}
		mOdometryLog.WriteToLog(packet);

		break;

	case POSE_EVENT:

		sprintf_s(packet, SE_LOGWRITER_FIELDSIZE, "%.12lg", ed[0]);
		for (j = 1; j < SE_POSEPACKETSIZE; j++)
		{
			sprintf_s(buff, SE_LOGWRITER_FIELDSIZE, ",%.12lg", ed[j]);
			strcat_s(packet, SE_LOGWRITER_FIELDSIZE, buff);
		}
		mPoseLog.WriteToLog(packet);

		break;

	case FRONTMOBILEYEROAD_EVENT:

		sprintf_s(packet, SE_LOGWRITER_FIELDSIZE, "%.12lg", ed[0]);
		for (j = 1; j < SE_MOBILEYEROADPACKETSIZE; j++)
		{
			sprintf_s(buff, SE_LOGWRITER_FIELDSIZE, ",%.12lg", ed[j]);
			strcat_s(packet, SE_LOGWRITER_FIELDSIZE, buff);
		}
		mFrontMobileyeRoadLog.WriteToLog(packet);

		break;

	case BACKMOBILEYEROAD_EVENT:

		sprintf_s(packet, SE_LOGWRITER_FIELDSIZE, "%.12lg", ed[0]);
		for (j = 1; j < SE_MOBILEYEROADPACKETSIZE; j++)
		{
			sprintf_s(buff, SE_LOGWRITER_FIELDSIZE, ",%.12lg", ed[j]);
			strcat_s(packet, SE_LOGWRITER_FIELDSIZE, buff);
		}
		mBackMobileyeRoadLog.WriteToLog(packet);

		break;

	case FRONTJASONROAD_EVENT:

		for (i = 0; i < ne; i++)
		{
			sprintf_s(packet, SE_LOGWRITER_FIELDSIZE, "%.12lg", ed[midx(i, 0, ne)]);
			for (j = 1; j < SE_JASONROADPACKETSIZE; j++)
			{
				sprintf_s(buff, SE_LOGWRITER_FIELDSIZE, ",%.12lg", ed[midx(i, j, ne)]);
				strcat_s(packet, SE_LOGWRITER_FIELDSIZE, buff);
			}
			mFrontJasonRoadLog.WriteToLog(packet);
		}

		break;

	case BACKJASONROAD_EVENT:

		for (i = 0; i < ne; i++)
		{
			sprintf_s(packet, SE_LOGWRITER_FIELDSIZE, "%.12lg", ed[midx(i, 0, ne)]);
			for (j = 1; j < SE_JASONROADPACKETSIZE; j++)
			{
				sprintf_s(buff, SE_LOGWRITER_FIELDSIZE, ",%.12lg", ed[midx(i, j, ne)]);
				strcat_s(packet, SE_LOGWRITER_FIELDSIZE, buff);
			}
			mBackJasonRoadLog.WriteToLog(packet);
		}

		break;

	case STOPLINE_EVENT:

		for (i = 0; i < ne; i++)
		{
			sprintf_s(packet, SE_LOGWRITER_FIELDSIZE, "%.12lg", ed[midx(i, 0, ne)]);
			for (j = 1; j < SE_STOPLINEPACKETSIZE; j++)
			{
				sprintf_s(buff, SE_LOGWRITER_FIELDSIZE, ",%.12lg", ed[midx(i, j, ne)]);
				strcat_s(packet, SE_LOGWRITER_FIELDSIZE, buff);
			}
			mStoplineLog.WriteToLog(packet);
		}

		break;
	}

	return;
}

DWORD WINAPI SceneEstimatorLogWriterCallback(LPVOID lpparam)
{
	/*
	Callback function for writing to log files.

	INPUTS:
		lpparam - a pointer to the SceneEstimatorLogWriter that owns this thread.

	OUTPUTS:
		none.  (always returns 0)
	*/

	//set the log writer thread to lower priority
	HANDLE LoggingThread = GetCurrentThread();
	SetThreadPriority(LoggingThread, THREAD_PRIORITY_BELOW_NORMAL);

	//cast the input argument to an instance of the sensor container class
	SceneEstimatorLogWriter* TheWriter = (SceneEstimatorLogWriter*)lpparam;

	//access to the logging event queue
	extern EventQueue* TheLoggingQueue;
	Event* TheCurrentEvent = NULL;

	while (TheWriter->IsRunning() == true)
	{
		//1. GET NEXT EVENT
		if (TheLoggingQueue->QueueHasEventsReady() == true)
		{
			//queue has data

			//delete the event that has just been processed
			delete TheCurrentEvent;
			TheCurrentEvent = NULL;

			//and grab the next event in its place
			TheCurrentEvent = TheLoggingQueue->PullEvent();
		}
		else
		{
			//go to sleep until queue has data
			DWORD LogWriterSignalStatus;
			LogWriterSignalStatus = WaitForSingleObjectEx(TheLoggingQueue->mQueueDataEventHandle, PP_EVENTTIMEOUT, false);

			continue;
		}

		//when code gets here, something is on the event queue

		//check for invalid or malformed events
		if (TheCurrentEvent == NULL)
		{
			//no event present
			continue;
		}
		if (TheCurrentEvent->EventType() == INVALID_EVENT)
		{
			//invalid event present
			continue;
		}
		if (TheCurrentEvent->EventType() == QUIT_EVENT)
		{
			//signal to quit
			TheWriter->StopRunning();
		}

		//2. PROCESS THE EVENT ON THE EVENT QUEUE
		TheWriter->LogEvent(TheCurrentEvent);
	}

	//free memory allocated in the logging queue
	delete TheCurrentEvent;
	printf("Log writer terminating.\n");

	return 0;
}
