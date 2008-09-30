#include "LocalMapLogWriter.h"

LocalMapLogWriter::LocalMapLogWriter(char* iLogDirectory)
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

	char fname[LM_LOGWRITER_FIELDSIZE];
	errno_t err;

	//prepend all file names with the directory
	char dirbuff[LM_LOGWRITER_FIELDSIZE];
	if (DirectoryExists(iLogDirectory) == true)
	{
		strcpy_s(dirbuff, LM_LOGWRITER_FIELDSIZE, iLogDirectory);
		//the directory exists, but check for a terminating backslash
		int dlen = (int) strlen(iLogDirectory);
		if (iLogDirectory[dlen-1] != '\\' && iLogDirectory[dlen-1] != '/')
		{
			//need to add a terminating backslash
			strcat_s(dirbuff, LM_LOGWRITER_FIELDSIZE, "\\");
		}
	}
	else
	{
		dirbuff[0] = '\0';
	}

	//get the system time for timing all the log files
	char timebuff[LM_LOGWRITER_FIELDSIZE];
	struct tm today;

	time_t ltime;
	time(&ltime);
	err = _localtime64_s(&today, &ltime);
	if (err != 0)
	{
		printf("Warning: log writer could not successfully open log files.\n");
		return;
	}
	strftime(timebuff, LM_LOGWRITER_FIELDSIZE, "%m-%d-%y %I.%M.%S %p", &today);

	//odometry file
	strcpy_s(fname, LM_LOGWRITER_FIELDSIZE, dirbuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, "odom ");
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, timebuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, ".txt");
	mOdometryLog.OpenLog(fname);

	//front ibeo file
	strcpy_s(fname, LM_LOGWRITER_FIELDSIZE, dirbuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, "ibeo ");
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, timebuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, ".txt");
	mClusteredIbeoLog.OpenLog(fname);
	//back clustered sick file
	strcpy_s(fname, LM_LOGWRITER_FIELDSIZE, dirbuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, "backclusteredsick ");
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, timebuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, ".txt");
	mClusteredSickLog.OpenLog(fname);

	//driver side sick file
	strcpy_s(fname, LM_LOGWRITER_FIELDSIZE, dirbuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, "drivsidesick ");
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, timebuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, ".txt");
	mDrivSickLog.OpenLog(fname);

	//passenger side sick file
	strcpy_s(fname, LM_LOGWRITER_FIELDSIZE, dirbuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, "passsidesick ");
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, timebuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, ".txt");
	mPassSickLog.OpenLog(fname);

	//front mobileye obstacle file
	strcpy_s(fname, LM_LOGWRITER_FIELDSIZE, dirbuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, "frontmobileyeobstacle ");
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, timebuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, ".txt");
	mFrontMobileyeObstacleLog.OpenLog(fname);

	//front mobileye road file
	strcpy_s(fname, LM_LOGWRITER_FIELDSIZE, dirbuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, "frontmobileyeroad ");
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, timebuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, ".txt");
	mFrontMobileyeRoadLog.OpenLog(fname);

	//back mobileye obstacle file
	strcpy_s(fname, LM_LOGWRITER_FIELDSIZE, dirbuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, "backmobileyeobstacle ");
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, timebuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, ".txt");
	mBackMobileyeObstacleLog.OpenLog(fname);

	//back mobileye road file
	strcpy_s(fname, LM_LOGWRITER_FIELDSIZE, dirbuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, "backmobileyeroad ");
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, timebuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, ".txt");
	mBackMobileyeRoadLog.OpenLog(fname);

	//center radar file
	strcpy_s(fname, LM_LOGWRITER_FIELDSIZE, dirbuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, "front0radar ");
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, timebuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, ".txt");
	mFront0RadarLog.OpenLog(fname);

	//left0 radar file
	strcpy_s(fname, LM_LOGWRITER_FIELDSIZE, dirbuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, "driv0radar ");
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, timebuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, ".txt");
	mDriv0RadarLog.OpenLog(fname);

	//left1 radar file
	strcpy_s(fname, LM_LOGWRITER_FIELDSIZE, dirbuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, "driv1radar ");
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, timebuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, ".txt");
	mDriv1RadarLog.OpenLog(fname);

	//left2 radar file
	strcpy_s(fname, LM_LOGWRITER_FIELDSIZE, dirbuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, "driv2radar ");
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, timebuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, ".txt");
	mDriv2RadarLog.OpenLog(fname);

	//left3 radar file
	strcpy_s(fname, LM_LOGWRITER_FIELDSIZE, dirbuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, "driv3radar ");
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, timebuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, ".txt");
	mDriv3RadarLog.OpenLog(fname);

	//right0 radar file
	strcpy_s(fname, LM_LOGWRITER_FIELDSIZE, dirbuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, "pass0radar ");
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, timebuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, ".txt");
	mPass0RadarLog.OpenLog(fname);

	//right1 radar file
	strcpy_s(fname, LM_LOGWRITER_FIELDSIZE, dirbuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, "pass1radar ");
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, timebuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, ".txt");
	mPass1RadarLog.OpenLog(fname);

	//right2 radar file
	strcpy_s(fname, LM_LOGWRITER_FIELDSIZE, dirbuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, "pass2radar ");
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, timebuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, ".txt");
	mPass2RadarLog.OpenLog(fname);

	//right3 radar file
	strcpy_s(fname, LM_LOGWRITER_FIELDSIZE, dirbuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, "pass3radar ");
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, timebuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, ".txt");
	mPass3RadarLog.OpenLog(fname);

	//front jason roadfinder file
	strcpy_s(fname, LM_LOGWRITER_FIELDSIZE, dirbuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, "frontjasonroad ");
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, timebuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, ".txt");
	mFrontRoadFinderLog.OpenLog(fname);

	//rear jason roadfinder file
	strcpy_s(fname, LM_LOGWRITER_FIELDSIZE, dirbuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, "backjasonroad ");
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, timebuff);
	strcat_s(fname, LM_LOGWRITER_FIELDSIZE, ".txt");
	mBackRoadFinderLog.OpenLog(fname);

	//***CREATE LOGGING EVENT QUEUE***

	//create the log writer event queue
	TheLoggingQueue = new EventQueue();

	//***START THE LOGGING THREAD RUNNING***

	//begin the thread running
	mIsRunning = true;

	//create the reader thread that will provide the sensor data
	mLoggingThread = CreateThread(NULL, 0, LocalMapLogWriterCallback, this, 0, NULL);

	printf("Log writer running.\n");

	return;
}

LocalMapLogWriter::~LocalMapLogWriter()
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
	if (WaitForSingleObjectEx(mLoggingThread, LM_LOGWRITER_SHUTDOWNTIMEOUT, false) != WAIT_OBJECT_0)
	{
		printf("Warning: log writing thread timed out, terminating forcefully...\n");
		TerminateThread(mLoggingThread, 2);
	}
	//destroy the thread associated with the log writer
	CloseHandle(mLoggingThread);
	printf("Log writing thread terminated successfully.\n");

	//NOTE: all open log files are automatically flushed and closed when windows deletes them

	//delete the log writer event queue
	extern EventQueue* TheLoggingQueue;
	delete TheLoggingQueue;

	return;
}

void LocalMapLogWriter::LogEvent(Event* iEvent)
{
	/*
	Logs an event to the appropriate log file based on its type

	INPUTS:
		iEvent - an event object containing the relevant event data

	OUTPUTS:
		none.
	*/

	int i;
	int j;
	int ne = iEvent->NumDataRows();
	char buff[LM_LOGWRITER_FIELDSIZE];
	char packet[LM_LOGWRITER_FIELDSIZE];
	WriterLogFile* TheLogFile = NULL;
	double* ed = iEvent->EventData;

	switch (iEvent->EventType())
	{
	case ODOM_EVENT:

		sprintf_s(packet, LM_LOGWRITER_FIELDSIZE, "%.12lg", ed[0]);
		for (j = 1; j < LM_ODOMPACKETSIZE; j++)
		{
			sprintf_s(buff, LM_LOGWRITER_FIELDSIZE, ",%.12lg", ed[j]);
			strcat_s(packet, LM_LOGWRITER_FIELDSIZE, buff);
		}
		mOdometryLog.WriteToLog(packet);

		break;

	case IBEO_EVENT:

		for (i = 0; i < ne; i++)
		{
			sprintf_s(packet, LM_LOGWRITER_FIELDSIZE, "%.12lg", ed[midx(i, 0, ne)]);
			for (j = 1; j < LM_CLUSTEREDIBEOPACKETSIZE; j++)
			{
				sprintf_s(buff, LM_LOGWRITER_FIELDSIZE, ",%.12lg", ed[midx(i, j, ne)]);
				strcat_s(packet, LM_LOGWRITER_FIELDSIZE, buff);
			}
			mClusteredIbeoLog.WriteToLog(packet);
		}

		break;

	case BACKCLUSTEREDSICK_EVENT:

		for (i = 0; i < ne; i++)
		{
			sprintf_s(packet, LM_LOGWRITER_FIELDSIZE, "%.12lg", ed[midx(i, 0, ne)]);
			for (j = 1; j < LM_CLUSTEREDSICKPACKETSIZE; j++)
			{
				sprintf_s(buff, LM_LOGWRITER_FIELDSIZE, ",%.12lg", ed[midx(i, j, ne)]);
				strcat_s(packet, LM_LOGWRITER_FIELDSIZE, buff);
			}
			mClusteredSickLog.WriteToLog(packet);
		}

		break;

	case FRONTMOBILEYEOBSTACLE_EVENT:

		for (i = 0; i < ne; i++)
		{
			sprintf_s(packet, LM_LOGWRITER_FIELDSIZE, "%.12lg", ed[midx(i, 0, ne)]);
			for (j = 1; j < LM_MOBILEYEOBSTACLEPACKETSIZE; j++)
			{
				sprintf_s(buff, LM_LOGWRITER_FIELDSIZE, ",%.12lg", ed[midx(i, j, ne)]);
				strcat_s(packet, LM_LOGWRITER_FIELDSIZE, buff);
			}
			mFrontMobileyeObstacleLog.WriteToLog(packet);
		}

		break;

	case BACKMOBILEYEOBSTACLE_EVENT:

		for (i = 0; i < ne; i++)
		{
			sprintf_s(packet, LM_LOGWRITER_FIELDSIZE, "%.12lg", ed[midx(i, 0, ne)]);
			for (j = 1; j < LM_MOBILEYEOBSTACLEPACKETSIZE; j++)
			{
				sprintf_s(buff, LM_LOGWRITER_FIELDSIZE, ",%.12lg", ed[midx(i, j, ne)]);
				strcat_s(packet, LM_LOGWRITER_FIELDSIZE, buff);
			}
			mBackMobileyeObstacleLog.WriteToLog(packet);
		}

		break;

	case FRONTMOBILEYEROAD_EVENT:

		sprintf_s(packet, LM_LOGWRITER_FIELDSIZE, "%.12lg", ed[0]);
		for (j = 1; j < LM_MOBILEYEROADPACKETSIZE; j++)
		{
			sprintf_s(buff, LM_LOGWRITER_FIELDSIZE, ",%.12lg", ed[j]);
			strcat_s(packet, LM_LOGWRITER_FIELDSIZE, buff);
		}
		mFrontMobileyeRoadLog.WriteToLog(packet);

		break;

	case BACKMOBILEYEROAD_EVENT:

		sprintf_s(packet, LM_LOGWRITER_FIELDSIZE, "%.12lg", ed[0]);
		for (j = 1; j < LM_MOBILEYEROADPACKETSIZE; j++)
		{
			sprintf_s(buff, LM_LOGWRITER_FIELDSIZE, ",%.12lg", ed[j]);
			strcat_s(packet, LM_LOGWRITER_FIELDSIZE, buff);
		}
		mBackMobileyeRoadLog.WriteToLog(packet);

		break;

	case FRONTJASONROAD_EVENT:

		for (i = 0; i < ne; i++)
		{
			sprintf_s(packet, LM_LOGWRITER_FIELDSIZE, "%.12lg", ed[midx(i, 0, ne)]);
			for (j = 1; j < LM_JASONROADPACKETSIZE; j++)
			{
				sprintf_s(buff, LM_LOGWRITER_FIELDSIZE, ",%.12lg", ed[midx(i, j, ne)]);
				strcat_s(packet, LM_LOGWRITER_FIELDSIZE, buff);
			}
			mFrontRoadFinderLog.WriteToLog(packet);
		}

		break;

	case BACKJASONROAD_EVENT:

		for (i = 0; i < ne; i++)
		{
			sprintf_s(packet, LM_LOGWRITER_FIELDSIZE, "%.12lg", ed[midx(i, 0, ne)]);
			for (j = 1; j < LM_JASONROADPACKETSIZE; j++)
			{
				sprintf_s(buff, LM_LOGWRITER_FIELDSIZE, ",%.12lg", ed[midx(i, j, ne)]);
				strcat_s(packet, LM_LOGWRITER_FIELDSIZE, buff);
			}
			mBackRoadFinderLog.WriteToLog(packet);
		}

		break;

	case FRONT0RADAR_EVENT:
	case DRIV0RADAR_EVENT:
	case DRIV1RADAR_EVENT:
	case DRIV2RADAR_EVENT:
	case DRIV3RADAR_EVENT:
	case PASS0RADAR_EVENT:
	case PASS1RADAR_EVENT:
	case PASS2RADAR_EVENT:
	case PASS3RADAR_EVENT:

		//all the radars have the same logging signature, just a different log file
		switch (iEvent->EventType())
		{
			case FRONT0RADAR_EVENT:
				TheLogFile = &mFront0RadarLog;
				break;
			case DRIV0RADAR_EVENT:
				TheLogFile = &mDriv0RadarLog;
				break;
			case DRIV1RADAR_EVENT:
				TheLogFile = &mDriv1RadarLog;
				break;
			case DRIV2RADAR_EVENT:
				TheLogFile = &mDriv2RadarLog;
				break;
			case DRIV3RADAR_EVENT:
				TheLogFile = &mDriv3RadarLog;
				break;
			case PASS0RADAR_EVENT:
				TheLogFile = &mPass0RadarLog;
				break;
			case PASS1RADAR_EVENT:
				TheLogFile = &mPass1RadarLog;
				break;
			case PASS2RADAR_EVENT:
				TheLogFile = &mPass2RadarLog;
				break;
			case PASS3RADAR_EVENT:
				TheLogFile = &mPass3RadarLog;
				break;
		}

		for (i = 0; i < ne; i++)
		{
			sprintf_s(packet, LM_LOGWRITER_FIELDSIZE, "%.12lg", ed[midx(i, 0, ne)]);
			for (j = 1; j < LM_DELPHIPACKETSIZE; j++)
			{
				sprintf_s(buff, LM_LOGWRITER_FIELDSIZE, ",%.12lg", ed[midx(i, j, ne)]);
				strcat_s(packet, LM_LOGWRITER_FIELDSIZE, buff);
			}
			TheLogFile->WriteToLog(packet);
		}

		break;

	case SIDESICKDRIV_EVENT:
		for (i = 0; i < ne; i++)
		{
			sprintf_s(packet, LM_LOGWRITER_FIELDSIZE, "%.12lg", ed[midx(i, 0, ne)]);
			for (j = 1; j < LM_SIDESICKPACKETSIZE; j++)
			{
				sprintf_s(buff, LM_LOGWRITER_FIELDSIZE, ",%.12lg", ed[midx(i, j, ne)]);
				strcat_s(packet, LM_LOGWRITER_FIELDSIZE, buff);
			}
			mDrivSickLog.WriteToLog(packet);
		}

		break;

	case SIDESICKPASS_EVENT:
		for (i = 0; i < ne; i++)
		{
			sprintf_s(packet, LM_LOGWRITER_FIELDSIZE, "%.12lg", ed[midx(i, 0, ne)]);
			for (j = 1; j < LM_SIDESICKPACKETSIZE; j++)
			{
				sprintf_s(buff, LM_LOGWRITER_FIELDSIZE, ",%.12lg", ed[midx(i, j, ne)]);
				strcat_s(packet, LM_LOGWRITER_FIELDSIZE, buff);
			}
			mPassSickLog.WriteToLog(packet);
		}

		break;
	}

	return;
}

DWORD WINAPI LocalMapLogWriterCallback(LPVOID lpparam)
{
	/*
	Callback function for writing to log files.

	INPUTS:
		lpparam - a pointer to the LocalMapLogWriter that owns this thread.

	OUTPUTS:
		none.  (always returns 0)
	*/

	//set the log writer thread to lower priority
	HANDLE LoggingThread = GetCurrentThread();
	SetThreadPriority(LoggingThread, THREAD_PRIORITY_BELOW_NORMAL);

	//cast the input argument to an instance of the sensor container class
	LocalMapLogWriter* TheWriter = (LocalMapLogWriter*)lpparam;

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
			LogWriterSignalStatus = WaitForSingleObjectEx(TheLoggingQueue->mQueueDataEventHandle, LM_EVENTTIMEOUT, false);

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

	//delete memory allocated in the logging queue
	delete TheCurrentEvent;
	printf("Log writer terminating.\n");

	return 0;
}
