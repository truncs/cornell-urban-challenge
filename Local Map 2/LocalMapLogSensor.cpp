#include "LocalMapLogSensor.h"

LocalMapLogSensor::LocalMapLogSensor(DWORD iSamplePeriod, char* iOdomLogName, 
	char* iFrontMobileyeObstacleLogName, char* iFrontMobileyeRoadLogName, 
	char* iBackMobileyeObstacleLogName, char* iBackMobileyeRoadLogName, 
	char* iFrontJasonRoadLogName, char* iBackJasonRoadLogName, 
	char* iIbeoLogName, char* iBackClusteredSickLogName, 
	char* iDrivSideSickLogName, char* iPassSideSickLogName, 
	char* iFront0RadarLogName, 
	char* iDriv0RadarLogName, char* iDriv1RadarLogName, char* iDriv2RadarLogName, char* iDriv3RadarLogName, 
	char* iPass0RadarLogName, char* iPass1RadarLogName, char* iPass2RadarLogName, char* iPass3RadarLogName)
{
	/*
	Constructor for the log file reader type sensor.

	INPUTS:
		iSamplePeriod - (integer) number of milliseconds between each sensor packet.  Used
			to fake actual sensor data rates when reading log files.
		iOdomLogName - name of the log file to use for the odometry sensor.
		iFrontMobileyeObstacleLogName - name of the log file to use for the front mobileye obstacle sensor.
		iFrontMobileyeRoadLogName - name of the log file to use for the front mobileye road sensor.
		iBackMobileyeObstacleLogName - name of the log file to use for the back mobileye obstacle sensor.
		iBackMobileyeRoadLogName - name of the log file to use for the back mobileye road sensor.
		iFrontJasonRoadLogName - name of the log file to use for the front jason road sensor.
		iBackJasonRoadLogName - name of the log file to use for the front jason road sensor.
		iDrivSideSickLogName - name of the log file to use for the driver side sick sensor
		iPassSideSickLogName - name of the log file to use for the passenger side sick sensor
		iIbeoLogName - name of the log file to use for the ibeo sensors
		iBackClusteredSickLogName - name of the log file to use for the clustered SICK
		iFront0RadarLogName - name of the log file to use for the front0 radar
		iDriv0,1,2,3RadarLogName - names of the log files for the driv radars
		iPass0,1,2,3RadarLogName - names of the log files for the pass radars

	OUTPUTS:
		none.
	*/

	//try to open the log files
	OdomLog.OpenLog(iOdomLogName);
	FrontMobileyeObstacleLog.OpenLog(iFrontMobileyeObstacleLogName);
	FrontMobileyeRoadLog.OpenLog(iFrontMobileyeRoadLogName);
	BackMobileyeObstacleLog.OpenLog(iBackMobileyeObstacleLogName);
	BackMobileyeRoadLog.OpenLog(iBackMobileyeRoadLogName);
	FrontJasonRoadLog.OpenLog(iFrontJasonRoadLogName);
	BackJasonRoadLog.OpenLog(iBackJasonRoadLogName);
	IbeoLog.OpenLog(iIbeoLogName);
	BackClusteredSickLog.OpenLog(iBackClusteredSickLogName);
	Front0RadarLog.OpenLog(iFront0RadarLogName);
	Driv0RadarLog.OpenLog(iDriv0RadarLogName);
	Driv1RadarLog.OpenLog(iDriv1RadarLogName);
	Driv2RadarLog.OpenLog(iDriv2RadarLogName);
	Driv3RadarLog.OpenLog(iDriv3RadarLogName);
	Pass0RadarLog.OpenLog(iPass0RadarLogName);
	Pass1RadarLog.OpenLog(iPass1RadarLogName);
	Pass2RadarLog.OpenLog(iPass2RadarLogName);
	Pass3RadarLog.OpenLog(iPass3RadarLogName);
	DrivSideSickLog.OpenLog(iDrivSideSickLogName);
	PassSideSickLog.OpenLog(iPassSideSickLogName);

	//begin the thread running
	mIsRunning = true;
	//store the sensor sampling period
	mSamplePeriod = iSamplePeriod;

	//create the reader thread that will provide the sensor data
	mInterfaceThread = CreateThread(NULL, 0, LogCallback, this, 0, NULL);

	return;
}

LocalMapLogSensor::~LocalMapLogSensor()
{
	/*
	Destructor for the log file reader.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	//stop the thread from running
	mIsRunning = false;
	//wait for the thread to terminate itself
	printf("Waiting for log sensor thread to terminate...\n");
	WaitForSingleObjectEx(mInterfaceThread, INFINITE, false);
	//destroy the thread associated with the sensor
	CloseHandle(mInterfaceThread);
	printf("Log sensor thread terminated successfully.\n");

	return;
}

DWORD WINAPI LogCallback(LPVOID lpparam)
{
	/*
	Callback function for reading data from log file.

	INPUTS:
		lpparam - a pointer to the LogSensor that owns this reader thread.

	OUTPUTS:
		none.  (always returns 0)
	*/

	//cast the input argument to an instance of the sensor container class
	LocalMapLogSensor *SensorLogs = (LocalMapLogSensor *)lpparam;

	errno_t retcode;
	//this will store the event data (i.e. the entire packet)
	int iEventType = INVALID_EVENT;
	//the event's time stamp
	double iEventTime = DBL_MAX;
	//the number of elements in the event
	int ine = 0;
	//the number of columns per row of event data
	int inc = 0;
	double *iEventData = NULL;
	//this will be used to create each new event as it arrives
	Event* NewEvent = NULL;
	Event* NewEvent2 = NULL;
	//access to the event queues
	extern SynchronizedEventQueue* TheLocalMapEventQueue;
	extern SynchronizedEventQueue* TheLocalRoadEventQueue;
	//access to the odometry queue
	extern RelativePoseQueue* TheRelativePoseQueue;
	//access to the car time
	extern CarTime* TheCarTime;
	//access to the logging queue
	#ifdef LM_PRINTLOGS
		extern EventQueue* TheLoggingQueue;
		Event* LogEvent;
	#endif

	int i;
	int j;
	//for determining which event happens next
	double eventtimes[20];
	int enext;
	double etime = DBL_MAX;
	ReaderLogFile* TheReaderLogFile;

	while (SensorLogs->IsRunning() == true)
	{
		//create an event each time sensor data is present

		//find which event happens next
		eventtimes[0] = SensorLogs->OdomLog.LogTime();
		eventtimes[1] = SensorLogs->FrontMobileyeObstacleLog.LogTime();
		eventtimes[2] = SensorLogs->FrontMobileyeRoadLog.LogTime();
		eventtimes[3] = SensorLogs->BackMobileyeObstacleLog.LogTime();
		eventtimes[4] = SensorLogs->BackMobileyeRoadLog.LogTime();
		eventtimes[5] = SensorLogs->FrontJasonRoadLog.LogTime();
		eventtimes[6] = SensorLogs->BackJasonRoadLog.LogTime();
		eventtimes[7] = SensorLogs->IbeoLog.LogTime();
		eventtimes[8] = SensorLogs->BackClusteredSickLog.LogTime();
		eventtimes[9] = SensorLogs->Front0RadarLog.LogTime();
		eventtimes[10] = SensorLogs->Driv0RadarLog.LogTime();
		eventtimes[11] = SensorLogs->Driv1RadarLog.LogTime();
		eventtimes[12] = SensorLogs->Driv2RadarLog.LogTime();
		eventtimes[13] = SensorLogs->Driv3RadarLog.LogTime();
		eventtimes[14] = SensorLogs->Pass0RadarLog.LogTime();
		eventtimes[15] = SensorLogs->Pass1RadarLog.LogTime();
		eventtimes[16] = SensorLogs->Pass2RadarLog.LogTime();
		eventtimes[17] = SensorLogs->Pass3RadarLog.LogTime();
		eventtimes[18] = SensorLogs->DrivSideSickLog.LogTime();
		eventtimes[19] = SensorLogs->PassSideSickLog.LogTime();
		enext = -1;
		etime = DBL_MAX;

		for (i = 0; i < 20; i++)
		{
			if (eventtimes[i] < etime)
			{
				enext = i;
				etime = eventtimes[i];
			}
		}

		switch (enext)
		{
			case 0:
				{
					//odometry packet comes next

					//parse out the odometry event information
					iEventData = new double[LM_ODOMPACKETSIZE];
					retcode = StringToArrayOfDoubles(iEventData, LM_ODOMPACKETSIZE, SensorLogs->OdomLog.LogBuffer());

					if (retcode != LM_ODOMPACKETSIZE)
					{
						//improper packet read: throw out this packet
						printf("Malformed odometry log packet\n");
						iEventTime = DBL_MAX;
						iEventType = INVALID_EVENT;
						ine = 0;
						inc = 0;
						delete [] iEventData;
						iEventData = NULL;
					}
					else
					{
						//if code gets here, iEventData was created successfully

						//for log files, use the odometry to drive the car time
						TheCarTime->SetCurrentCarTime(iEventData[0]);

						#ifdef LM_PRINTLOGS
							//extract the event time

							iEventTime = iEventData[0];
							iEventType = ODOM_EVENT;
							ine = 1;
							inc = LM_ODOMPACKETSIZE;

							//create a separate logging event with the same data
							LogEvent = new Event();
							double* iLogData = new double[LM_ODOMPACKETSIZE];
							memcpy(iLogData, iEventData, sizeof(double) * inc);
							LogEvent->SetEventType(iEventType, iEventTime);
							LogEvent->SetEventData(ine, inc, iLogData);
						#endif

						bool spush = TheRelativePoseQueue->PushPacket(iEventData);
						#ifdef LM_PRINTLOGS
							//only log the packet if it successfully entered the event queue
							if (spush == true)
							{
								TheLoggingQueue->PushEvent(LogEvent);
							}
							else
							{
								delete LogEvent;
							}
						#endif

						//note: now the event gets pushed directly to the relative pose queue
						iEventTime = DBL_MAX;
						iEventType = INVALID_EVENT;
						ine = 0;
						inc = 0;
						delete [] iEventData;
						iEventData = NULL;
					}

					//done reading the odometry event; read the next line
					SensorLogs->OdomLog.GetNextLogLine();
				}

				break;

			case 1:
			case 3:
				{
					//front / back mobileye obstacle packet comes next

					switch(enext)
					{
						case 1:
							iEventType = FRONTMOBILEYEOBSTACLE_EVENT;
							TheReaderLogFile = &(SensorLogs->FrontMobileyeObstacleLog);
							break;
						case 3:
							iEventType = BACKMOBILEYEOBSTACLE_EVENT;
							TheReaderLogFile = &(SensorLogs->BackMobileyeObstacleLog);
							break;
					}

					//parse out the mobileye event information
					double tEventData[LM_MOBILEYEOBSTACLEPACKETSIZE];
					retcode = StringToArrayOfDoubles(tEventData, LM_MOBILEYEOBSTACLEPACKETSIZE, TheReaderLogFile->LogBuffer());

					if (retcode != LM_MOBILEYEOBSTACLEPACKETSIZE)
					{
						//improper packet read: throw out this packet
						printf("Malformed mobileye obstacle log packet\n");
						iEventTime = DBL_MAX;
						iEventType = INVALID_EVENT;
						ine = 0;
						inc = 0;
						iEventData = NULL;
					}
					else
					{
						//if code gets here, iEventData was created successfully

						//extract the event time
						iEventTime = tEventData[0];
						ine = (int) (tEventData[2]);
						inc = LM_MOBILEYEOBSTACLEPACKETSIZE;

						iEventData = new double[inc*ine];
						for (j = 0; j < inc; j++)
						{
							//copy the first line of the packet into iEventData
							iEventData[midx(0, j, ine)] = tEventData[j];
						}

						for (i = 1; i < ine; i++)
						{
							//read the rest of the packet line by line
							TheReaderLogFile->GetNextLogLine();
							retcode = StringToArrayOfDoubles(tEventData, LM_MOBILEYEOBSTACLEPACKETSIZE, TheReaderLogFile->LogBuffer());

							if (retcode != LM_MOBILEYEOBSTACLEPACKETSIZE)
							{
								//improper packet read: throw out this packet
								printf("Malformed mobileye obstacle log packet\n");
								iEventTime = DBL_MAX;
								iEventType = INVALID_EVENT;
								ine = 0;
								inc = 0;
								delete [] iEventData;
								iEventData = NULL;

								break;
							}

							for (j = 0; j < LM_MOBILEYEOBSTACLEPACKETSIZE; j++)
							{
								//copy the next line of the packet into iEventData
								iEventData[midx(i, j, ine)] = tEventData[j];
							}
						}
					}

					//done reading the event; read the next line
					TheReaderLogFile->GetNextLogLine();
				}

				break;

			case 2:
			case 4:
				{
					//front / back mobileye road packet comes next

					switch(enext)
					{
						case 2:
							iEventType = FRONTMOBILEYEROAD_EVENT;
							TheReaderLogFile = &(SensorLogs->FrontMobileyeRoadLog);
							break;
						case 4:
							iEventType = BACKMOBILEYEROAD_EVENT;
							TheReaderLogFile = &(SensorLogs->BackMobileyeRoadLog);
							break;
					}

					//parse out the mobileye event information
					iEventData = new double[LM_MOBILEYEROADPACKETSIZE];
					retcode = StringToArrayOfDoubles(iEventData, LM_MOBILEYEROADPACKETSIZE, TheReaderLogFile->LogBuffer());

					if (retcode != LM_MOBILEYEROADPACKETSIZE)
					{
						//improper packet read: throw out this packet
						printf("Malformed mobileye road log packet\n");
						iEventTime = DBL_MAX;
						iEventType = INVALID_EVENT;
						ine = 0;
						inc = 0;
						delete [] iEventData;
						iEventData = NULL;
					}
					else
					{
						//if code gets here, iEventData was created successfully
						//extract the event time
						iEventTime = iEventData[0];
						ine = 1;
						inc = LM_MOBILEYEROADPACKETSIZE;
					}

					//done reading the event; read the next line
					TheReaderLogFile->GetNextLogLine();
				}

				break;

			case 5:
			case 6:
				{
					//front / back jason packet comes next

					switch(enext)
					{
						case 5:
							iEventType = FRONTJASONROAD_EVENT;
							TheReaderLogFile = &(SensorLogs->FrontJasonRoadLog);
							break;
						case 6:
							iEventType = BACKJASONROAD_EVENT;
							TheReaderLogFile = &(SensorLogs->BackJasonRoadLog);
							break;
					}

					//parse out the jason event information
					double tEventData[LM_JASONROADPACKETSIZE];
					retcode = StringToArrayOfDoubles(tEventData, LM_JASONROADPACKETSIZE, TheReaderLogFile->LogBuffer());

					if (retcode != LM_JASONROADPACKETSIZE)
					{
						//improper packet read: throw out this packet
						printf("Malformed jason road log packet\n");
						iEventTime = DBL_MAX;
						iEventType = INVALID_EVENT;
						ine = 0;
						inc = 0;
						iEventData = NULL;
					}
					else
					{
						//if code gets here, iEventData was created successfully

						//extract the event time
						iEventTime = tEventData[0];
						ine = (int) (tEventData[2]);
						inc = LM_JASONROADPACKETSIZE;

						iEventData = new double[inc*ine];
						for (j = 0; j < inc; j++)
						{
							//copy the first line of the packet into iEventData
							iEventData[midx(0, j, ine)] = tEventData[j];
						}

						for (i = 1; i < ine; i++)
						{
							//read the rest of the packet line by line
							SensorLogs->FrontJasonRoadLog.GetNextLogLine();
							retcode = StringToArrayOfDoubles(tEventData, LM_JASONROADPACKETSIZE, TheReaderLogFile->LogBuffer());

							if (retcode != LM_JASONROADPACKETSIZE)
							{
								//improper packet read: throw out this packet
								printf("Malformed jason road log packet\n");
								iEventTime = DBL_MAX;
								iEventType = INVALID_EVENT;
								ine = 0;
								inc = 0;
								delete [] iEventData;
								iEventData = NULL;

								break;
							}

							for (j = 0; j < LM_JASONROADPACKETSIZE; j++)
							{
								//copy the next line of the packet into iEventData
								iEventData[midx(i, j, ine)] = tEventData[j];
							}
						}
					}

					//done reading the event; read the next line
					TheReaderLogFile->GetNextLogLine();
				}

				break;

			case 7:
				{
					//front ibeo packet comes next

					//parse out the front ibeo event information
					double tEventData[LM_CLUSTEREDIBEOPACKETSIZE];
					retcode = StringToArrayOfDoubles(tEventData, LM_CLUSTEREDIBEOPACKETSIZE, SensorLogs->IbeoLog.LogBuffer());

					if (retcode != LM_CLUSTEREDIBEOPACKETSIZE)
					{
						//improper packet read: throw out this packet
						printf("Malformed front ibeo log packet\n");
						iEventTime = DBL_MAX;
						iEventType = INVALID_EVENT;
						ine = 0;
						inc = 0;
						iEventData = NULL;
					}
					else
					{
						//if code gets here, iEventData was created successfully

						//extract the event time
						iEventTime = tEventData[0];
						iEventType = IBEO_EVENT;
						ine = (int) (tEventData[2]);
						inc = LM_CLUSTEREDIBEOPACKETSIZE;

						iEventData = new double[inc*ine];
						for (j = 0; j < LM_CLUSTEREDIBEOPACKETSIZE; j++)
						{
							//copy the first line of the packet into iEventData
							iEventData[midx(0, j, ine)] = tEventData[j];
						}

						for (i = 1; i < ine; i++)
						{
							//read the rest of the packet line by line
							SensorLogs->IbeoLog.GetNextLogLine();
							retcode = StringToArrayOfDoubles(tEventData, LM_CLUSTEREDIBEOPACKETSIZE, SensorLogs->IbeoLog.LogBuffer());

							if (retcode != LM_CLUSTEREDIBEOPACKETSIZE)
							{
								//improper packet read: throw out this packet
								printf("Malformed front ibeo log packet\n");
								iEventTime = DBL_MAX;
								iEventType = INVALID_EVENT;
								ine = 0;
								inc = 0;
								delete [] iEventData;
								iEventData = NULL;

								break;
							}

							for (j = 0; j < inc; j++)
							{
								//copy the next line of the packet into iEventData
								iEventData[midx(i, j, ine)] = tEventData[j];
							}
						}
					}

					//done reading the event; read the next line
					SensorLogs->IbeoLog.GetNextLogLine();
				}

				break;

			case 8:
				{
					//back clustered sick packet comes next

					//parse out the front ibeo event information
					double tEventData[LM_CLUSTEREDSICKPACKETSIZE];
					retcode = StringToArrayOfDoubles(tEventData, LM_CLUSTEREDSICKPACKETSIZE, SensorLogs->BackClusteredSickLog.LogBuffer());

					if (retcode != LM_CLUSTEREDSICKPACKETSIZE)
					{
						//improper packet read: throw out this packet
						printf("Malformed back clustered sick log packet\n");
						iEventTime = DBL_MAX;
						iEventType = INVALID_EVENT;
						ine = 0;
						inc = 0;
						iEventData = NULL;
					}
					else
					{
						//if code gets here, iEventData was created successfully

						//extract the event time
						iEventTime = tEventData[0];
						iEventType = BACKCLUSTEREDSICK_EVENT;
						ine = (int) (tEventData[2]);
						inc = LM_CLUSTEREDSICKPACKETSIZE;

						iEventData = new double[inc*ine];
						for (j = 0; j < LM_CLUSTEREDSICKPACKETSIZE; j++)
						{
							//copy the first line of the packet into iEventData
							iEventData[midx(0, j, ine)] = tEventData[j];
						}

						for (i = 1; i < ine; i++)
						{
							//read the rest of the packet line by line
							SensorLogs->BackClusteredSickLog.GetNextLogLine();
							retcode = StringToArrayOfDoubles(tEventData, LM_CLUSTEREDSICKPACKETSIZE, SensorLogs->BackClusteredSickLog.LogBuffer());

							if (retcode != LM_CLUSTEREDSICKPACKETSIZE)
							{
								//improper packet read: throw out this packet
								printf("Malformed back clustered sick log packet\n");
								iEventTime = DBL_MAX;
								iEventType = INVALID_EVENT;
								ine = 0;
								inc = 0;
								delete [] iEventData;
								iEventData = NULL;

								break;
							}

							for (j = 0; j < inc; j++)
							{
								//copy the next line of the packet into iEventData
								iEventData[midx(i, j, ine)] = tEventData[j];
							}
						}
					}

					//done reading the event; read the next line
					SensorLogs->BackClusteredSickLog.GetNextLogLine();
				}

				break;

			case 9:
			case 10:
			case 11:
			case 12:
			case 13:
			case 14:
			case 15:
			case 16:
			case 17:

				switch(enext)
				{
					case 9:
						iEventType = FRONT0RADAR_EVENT;
						TheReaderLogFile = &(SensorLogs->Front0RadarLog);
						break;
					case 10:
						iEventType = DRIV0RADAR_EVENT;
						TheReaderLogFile = &(SensorLogs->Driv0RadarLog);
						break;
					case 11:
						iEventType = DRIV1RADAR_EVENT;
						TheReaderLogFile = &(SensorLogs->Driv1RadarLog);
						break;
					case 12:
						iEventType = DRIV2RADAR_EVENT;
						TheReaderLogFile = &(SensorLogs->Driv2RadarLog);
						break;
					case 13:
						iEventType = DRIV3RADAR_EVENT;
						TheReaderLogFile = &(SensorLogs->Driv3RadarLog);
						break;
					case 14:
						iEventType = PASS0RADAR_EVENT;
						TheReaderLogFile = &(SensorLogs->Pass0RadarLog);
						break;
					case 15:
						iEventType = PASS1RADAR_EVENT;
						TheReaderLogFile = &(SensorLogs->Pass1RadarLog);
						break;
					case 16:
						iEventType = PASS2RADAR_EVENT;
						TheReaderLogFile = &(SensorLogs->Pass2RadarLog);
						break;
					case 17:
						iEventType = PASS3RADAR_EVENT;
						TheReaderLogFile = &(SensorLogs->Pass3RadarLog);
						break;
				}

				{
					//radar packet comes next

					//parse out the radar event information
					double tEventData[LM_DELPHIPACKETSIZE];
					retcode = StringToArrayOfDoubles(tEventData, LM_DELPHIPACKETSIZE, TheReaderLogFile->LogBuffer());

					if (retcode != LM_DELPHIPACKETSIZE)
					{
						//improper packet read: throw out this packet
						printf("Malformed radar log packet\n");
						iEventTime = DBL_MAX;
						iEventType = INVALID_EVENT;
						ine = 0;
						inc = 0;
						iEventData = NULL;
					}
					else
					{
						//if code gets here, iEventData was created successfully

						//extract the event time
						iEventTime = tEventData[0];
						//NOTE: iEventType is already set in the cascading cases
						ine = (int) (tEventData[2]);
						inc = LM_DELPHIPACKETSIZE;

						iEventData = new double[inc*ine];
						for (j = 0; j < LM_DELPHIPACKETSIZE; j++)
						{
							//copy the first line of the packet into iEventData
							iEventData[midx(0, j, ine)] = tEventData[j];
						}

						for (i = 1; i < ine; i++)
						{
							//read the rest of the packet line by line
							TheReaderLogFile->GetNextLogLine();
							retcode = StringToArrayOfDoubles(tEventData, LM_DELPHIPACKETSIZE, TheReaderLogFile->LogBuffer());

							if (retcode != LM_DELPHIPACKETSIZE)
							{
								//improper packet read: throw out this packet
								printf("Malformed radar log packet\n");
								iEventTime = DBL_MAX;
								iEventType = INVALID_EVENT;
								ine = 0;
								inc = 0;
								delete [] iEventData;
								iEventData = NULL;

								break;
							}

							for (j = 0; j < inc; j++)
							{
								//copy the next line of the packet into iEventData
								iEventData[midx(i, j, ine)] = tEventData[j];
							}
						}
					}

					//done reading the event; read the next line
					TheReaderLogFile->GetNextLogLine();
				}

				break;

			case 18:
			case 19:
				{
					//driv / pass side sick event came next

					switch(enext)
					{
						case 18:
							iEventType = SIDESICKDRIV_EVENT;
							TheReaderLogFile = &(SensorLogs->DrivSideSickLog);
							break;
						case 19:
							iEventType = SIDESICKPASS_EVENT;
							TheReaderLogFile = &(SensorLogs->PassSideSickLog);
							break;
					}

					//parse out the side sick event information
					double tEventData[LM_SIDESICKPACKETSIZE];
					retcode = StringToArrayOfDoubles(tEventData, LM_SIDESICKPACKETSIZE, TheReaderLogFile->LogBuffer());

					if (retcode != LM_SIDESICKPACKETSIZE)
					{
						//improper packet read: throw out this packet
						printf("Malformed side sick log packet\n");
						iEventTime = DBL_MAX;
						iEventType = INVALID_EVENT;
						ine = 0;
						inc = 0;
						iEventData = NULL;
					}
					else
					{
						//if code gets here, iEventData was created successfully

						//extract the event time
						iEventTime = tEventData[0];
						//NOTE: iEventType is already set in the cascading cases
						ine = (int) (tEventData[2]);
						inc = LM_SIDESICKPACKETSIZE;

						iEventData = new double[inc*ine];
						for (j = 0; j < LM_SIDESICKPACKETSIZE; j++)
						{
							//copy the first line of the packet into iEventData
							iEventData[midx(0, j, ine)] = tEventData[j];
						}

						for (i = 1; i < ine; i++)
						{
							//read the rest of the packet line by line
							TheReaderLogFile->GetNextLogLine();
							retcode = StringToArrayOfDoubles(tEventData, LM_SIDESICKPACKETSIZE, TheReaderLogFile->LogBuffer());

							if (retcode != LM_SIDESICKPACKETSIZE)
							{
								//improper packet read: throw out this packet
								printf("Malformed side sick packet\n");
								iEventTime = DBL_MAX;
								iEventType = INVALID_EVENT;
								ine = 0;
								inc = 0;
								delete [] iEventData;
								iEventData = NULL;

								break;
							}

							for (j = 0; j < inc; j++)
							{
								//copy the next line of the packet into iEventData
								iEventData[midx(i, j, ine)] = tEventData[j];
							}
						}
					}

					//done reading the event; read the next line
					TheReaderLogFile->GetNextLogLine();
				}

				break;

			default:
				{
					//invalid event
					iEventType = INVALID_EVENT;
					iEventTime = DBL_MAX;
					ine = 0;
					inc = 0;
					iEventData = NULL;
				}

				break;
		}

		//when no data is left, "disconnect" the sensor
		if (etime == DBL_MAX)
		{
			printf("Log sensors terminating...\n");
			iEventType = INVALID_EVENT;
			iEventTime = DBL_MAX;
			ine = 0;
			inc = 0;
			iEventData = NULL;
			SensorLogs->StopRunning();
		}

		if (iEventType == INVALID_EVENT || SensorLogs->IsRunning() == false)
		{
			//for invalid reads or terminating logfiles, go to sleep
			//note: memory has already been deallocated for bad reads

			//sleep for the delay time of the sensor
			Sleep(SensorLogs->SamplePeriod());
			continue;
		}

		//when the code gets here, iEventData is populated with a complete sensor packet
		//create the event data
		NewEvent = new Event();
		NewEvent->SetEventType(iEventType, iEventTime);
		NewEvent->SetEventData(ine, inc, iEventData);
		#ifdef LM_PRINTLOGS
			//create a separate logging event with the same data
			LogEvent = new Event(NewEvent);
		#endif

		bool spush = false;

		switch (NewEvent->EventType())
		{
		case IBEO_EVENT:
		case BACKCLUSTEREDSICK_EVENT:
		case FRONTMOBILEYEOBSTACLE_EVENT:
		case BACKMOBILEYEOBSTACLE_EVENT:
		case DRIV3RADAR_EVENT:
		case DRIV2RADAR_EVENT:
		case DRIV1RADAR_EVENT:
		case DRIV0RADAR_EVENT:
		case FRONT0RADAR_EVENT:
		case PASS0RADAR_EVENT:
		case PASS1RADAR_EVENT:
		case PASS2RADAR_EVENT:
		case PASS3RADAR_EVENT:
		case SIDESICKDRIV_EVENT:
		case SIDESICKPASS_EVENT:
			//obstacle packets go to LocalMap

			spush = TheLocalMapEventQueue->PushEvent(NewEvent);

			break;

		case FRONTJASONROAD_EVENT:
		case BACKJASONROAD_EVENT:
		case FRONTMOBILEYEROAD_EVENT:
		case BACKMOBILEYEROAD_EVENT:
			//local road packets go to LocalRoad
			spush = TheLocalRoadEventQueue->PushEvent(NewEvent);

			break;

		case ODOM_EVENT:
		case QUIT_EVENT:
		case TRANSMIT_EVENT:
			//message events go to both queues

			NewEvent2 = new Event(NewEvent);

			spush = TheLocalMapEventQueue->PushEvent(NewEvent);
			TheLocalRoadEventQueue->PushEvent(NewEvent2);

			break;
		}

		#ifdef LM_PRINTLOGS
			//only log the packet if it successfully entered the event queue
			if (spush == true)
			{
				TheLoggingQueue->PushEvent(LogEvent);
			}
			else
			{
				delete LogEvent;
			}
		#endif

		//sleep for the delay time of the sensor
		Sleep(SensorLogs->SamplePeriod());
	}

	return 0;
}
