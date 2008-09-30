#include "SceneEstimatorLogSensor.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

SceneEstimatorLogSensor::	SceneEstimatorLogSensor(DWORD iSamplePeriod, char* iOdomLogName, char* iPoseLogName, 
		char* iFrontMobileyeRoadLogName, char* iBackMobileyeRoadLogName, 
		char* iFrontJasonRoadLogName, char* iBackJasonRoadLogName, 
		char* iStoplineLogName, char* iLocalMapLogName, char* iLocalPointsLogName)
{
	/*
	Constructor for the log file reader type sensor.

	INPUTS:
		iSamplePeriod - (integer) number of milliseconds between each sensor packet.  Used
			to fake actual sensor data rates when reading log files.
		iOdomLogName - name of the log file to use for the odometry sensor.
		iPoseLogName - name of the log file to use for the pose sensor.
		iFrontMobileyeRoadLogName - name of the log file to use for the front mobileye sensor.
		iBackMobileyeRoadLogName - name of the log file to use for the back mobileye sensor.
		iFrontJasonRoadLogName - name of the log file to use for the front jason sensor.
		iBackJasonRoadLogName - name of the log file to use for the back jason sensor.
		iStoplineLogName - name of the log file to use for the stopline sensor.
		iLocalMapLogName - name of the log file to use for the local map.
		iLocalPointsLogName - name of the log file to use for the local points file.

	OUTPUTS:
		none.
	*/

	//begin the thread running
	mIsRunning = true;
	//store the sensor sampling period
	mSamplePeriod = iSamplePeriod;

	//try to open the log files
	OdomLog.OpenLog(iOdomLogName);
	PoseLog.OpenLog(iPoseLogName);
	FrontMobileyeRoadLog.OpenLog(iFrontMobileyeRoadLogName);
	BackMobileyeRoadLog.OpenLog(iBackMobileyeRoadLogName);
	FrontJasonRoadLog.OpenLog(iFrontJasonRoadLogName);
	BackJasonRoadLog.OpenLog(iBackJasonRoadLogName);
	StoplineLog.OpenLog(iStoplineLogName);
	LocalMapLog.OpenLog(iLocalMapLogName);
	LocalPointsLog.OpenLog(iLocalPointsLogName);

	//create the reader thread that will provide the sensor data
	mInterfaceThread = CreateThread(NULL, 0, LogCallback, this, 0, NULL);

	return;
}

SceneEstimatorLogSensor::~SceneEstimatorLogSensor()
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
	SceneEstimatorLogSensor *SensorLogs = (SceneEstimatorLogSensor *)lpparam;

	errno_t retcode;
	//this will be used to create each new event as it arrives
	Event* NewEvent = NULL;
	Event* NewEvent2 = NULL;
	//access to the posterior pose event queue
	extern SynchronizedEventQueue* ThePosteriorPoseEventQueue;
	extern SynchronizedEventQueue* TheTrackGeneratorEventQueue;
	//access to the posterior pose odometry queue
	extern RelativePoseQueue* TheRelativePoseQueue;
	//access to the car time
	extern CarTime* TheCarTime;
	//access to the logging queue
	#ifdef SE_PRINTLOGS
		extern EventQueue* TheLoggingQueue;
		Event* LogEvent;
	#endif

	int i;
	int j;
	int k;
	//for determining which event happens next
	double eventtimes[9];
	int enext;
	double etime = DBL_MAX;

	while (SensorLogs->IsRunning() == true)
	{
		//create an event each time sensor data is present

		//this will store the event data (i.e. the entire packet)
		int iEventType = INVALID_EVENT;
		//the event's time stamp
		double iEventTime = DBL_MAX;
		//the number of elements in the event
		int ine = 0;
		//the number of columns per row of event data
		int inc = 0;
		int ine2 = 0;
		int inc2 = 0;
		int ine3 = 0;
		int inc3 = 0;
		double* iEventData = NULL;
		double* iEventData2 = NULL;
		double* iEventData3 = NULL;

		//find which event happens next
		eventtimes[0] = SensorLogs->OdomLog.LogTime();
		eventtimes[1] = SensorLogs->PoseLog.LogTime();
		eventtimes[2] = SensorLogs->FrontMobileyeRoadLog.LogTime();
		eventtimes[3] = SensorLogs->BackMobileyeRoadLog.LogTime();
		eventtimes[4] = SensorLogs->FrontJasonRoadLog.LogTime();
		eventtimes[5] = SensorLogs->BackJasonRoadLog.LogTime();
		eventtimes[6] = SensorLogs->StoplineLog.LogTime();
		eventtimes[7] = SensorLogs->LocalMapLog.LogTime();
		eventtimes[8] = SensorLogs->LocalPointsLog.LogTime();
		enext = -1;
		etime = DBL_MAX;

		for (i = 0; i < 9; i++)
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
					iEventData = new double[SE_ODOMPACKETSIZE];
					retcode = StringToArrayOfDoubles(iEventData, SE_ODOMPACKETSIZE, SensorLogs->OdomLog.LogBuffer());

					if (retcode != SE_ODOMPACKETSIZE)
					{
						//improper packet read: throw out this packet
						printf("Malformed odometry log packet.\n");
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

						#ifdef SE_PRINTLOGS
							//extract the event time

							iEventTime = iEventData[0];
							iEventType = ODOM_EVENT;
							ine = 1;
							inc = SE_ODOMPACKETSIZE;

							//create a separate logging event with the same data
							LogEvent = new Event();
							double* iLogData = new double[SE_ODOMPACKETSIZE];
							memcpy(iLogData, iEventData, sizeof(double) * inc);
							LogEvent->SetEventType(iEventType, iEventTime);
							LogEvent->SetEventData(ine, inc, iLogData);
						#endif

						bool spush = TheRelativePoseQueue->PushPacket(iEventData);
						#ifdef SE_PRINTLOGS
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
				{
					//pose packet comes next

					//parse out the odometry event information
					iEventData = new double[SE_POSEPACKETSIZE];
					retcode = StringToArrayOfDoubles(iEventData, SE_POSEPACKETSIZE, SensorLogs->PoseLog.LogBuffer());

					if (retcode != SE_POSEPACKETSIZE)
					{
						//improper packet read: throw out this packet
						printf("Malformed pose log packet.\n");
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
						iEventType = POSE_EVENT;
						ine = 1;
						inc = SE_POSEPACKETSIZE;
					}

					//done reading the event; read the next line
					SensorLogs->PoseLog.GetNextLogLine();
				}

				break;

			case 2:
				{
					//front mobileye packet comes next

					//parse out the mobileye event information
					iEventData = new double[SE_MOBILEYEROADPACKETSIZE];
					retcode = StringToArrayOfDoubles(iEventData, SE_MOBILEYEROADPACKETSIZE, SensorLogs->FrontMobileyeRoadLog.LogBuffer());

					if (retcode != SE_MOBILEYEROADPACKETSIZE)
					{
						//improper packet read: throw out this packet
						printf("Malformed front mobileye log packet.\n");
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
						iEventType = FRONTMOBILEYEROAD_EVENT;
						ine = 1;
						inc = SE_MOBILEYEROADPACKETSIZE;
					}

					//done reading the event; read the next line
					SensorLogs->FrontMobileyeRoadLog.GetNextLogLine();
				}

				break;

			case 3:
				{
					//back mobileye packet comes next

					//parse out the mobileye event information
					iEventData = new double[SE_MOBILEYEROADPACKETSIZE];
					retcode = StringToArrayOfDoubles(iEventData, SE_MOBILEYEROADPACKETSIZE, SensorLogs->BackMobileyeRoadLog.LogBuffer());

					if (retcode != SE_MOBILEYEROADPACKETSIZE)
					{
						//improper packet read: throw out this packet
						printf("Malformed back mobileye log packet.\n");
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
						iEventType = BACKMOBILEYEROAD_EVENT;
						ine = 1;
						inc = SE_MOBILEYEROADPACKETSIZE;
					}

					//done reading the event; read the next line
					SensorLogs->BackMobileyeRoadLog.GetNextLogLine();
				}

				break;

			case 4:
				{
					//front jason packet comes next

					//parse out the jason event information
					double tEventData[SE_JASONROADPACKETSIZE];
					retcode = StringToArrayOfDoubles(tEventData, SE_JASONROADPACKETSIZE, SensorLogs->FrontJasonRoadLog.LogBuffer());

					if (retcode != SE_JASONROADPACKETSIZE)
					{
						//improper packet read: throw out this packet
						printf("Malformed front jason log packet.\n");
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
						iEventType = FRONTJASONROAD_EVENT;
						ine = (int) (tEventData[2]);
						inc = SE_JASONROADPACKETSIZE;

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
							retcode = StringToArrayOfDoubles(tEventData, SE_JASONROADPACKETSIZE, SensorLogs->FrontJasonRoadLog.LogBuffer());

							if (retcode != SE_JASONROADPACKETSIZE)
							{
								//improper packet read: throw out this packet
								printf("Malformed front jason log packet.\n");
								iEventTime = DBL_MAX;
								iEventType = INVALID_EVENT;
								ine = 0;
								inc = 0;
								delete [] iEventData;
								iEventData = NULL;

								break;
							}

							for (j = 0; j < SE_JASONROADPACKETSIZE; j++)
							{
								//copy the next line of the packet into iEventData
								iEventData[midx(i, j, ine)] = tEventData[j];
							}
						}
					}

					//done reading the event; read the next line
					SensorLogs->FrontJasonRoadLog.GetNextLogLine();
				}

				break;

			case 5:
				{
					//back jason packet comes next

					//parse out the jason event information
					double tEventData[SE_JASONROADPACKETSIZE];
					retcode = StringToArrayOfDoubles(tEventData, SE_JASONROADPACKETSIZE, SensorLogs->BackJasonRoadLog.LogBuffer());

					if (retcode != SE_JASONROADPACKETSIZE)
					{
						//improper packet read: throw out this packet
						printf("Malformed back jason log packet.\n");
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
						iEventType = BACKJASONROAD_EVENT;
						ine = (int) (tEventData[2]);
						inc = SE_JASONROADPACKETSIZE;

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
							retcode = StringToArrayOfDoubles(tEventData, SE_JASONROADPACKETSIZE, SensorLogs->BackJasonRoadLog.LogBuffer());

							if (retcode != SE_JASONROADPACKETSIZE)
							{
								//improper packet read: throw out this packet
								printf("Malformed back jason log packet.\n");
								iEventTime = DBL_MAX;
								iEventType = INVALID_EVENT;
								ine = 0;
								inc = 0;
								delete [] iEventData;
								iEventData = NULL;

								break;
							}

							for (j = 0; j < SE_JASONROADPACKETSIZE; j++)
							{
								//copy the next line of the packet into iEventData
								iEventData[midx(i, j, ine)] = tEventData[j];
							}
						}
					}

					//done reading the event; read the next line
					SensorLogs->BackJasonRoadLog.GetNextLogLine();
				}

				break;

			case 6:
				{
					//stopline packet comes next

					//parse out the stopline event information
					double tEventData[SE_STOPLINEPACKETSIZE];
					retcode = StringToArrayOfDoubles(tEventData, SE_STOPLINEPACKETSIZE, SensorLogs->StoplineLog.LogBuffer());

					if (retcode != SE_STOPLINEPACKETSIZE)
					{
						//improper packet read: throw out this packet
						printf("Malformed stopline log packet.\n");
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
						iEventType = STOPLINE_EVENT;
						ine = (int) (tEventData[2]);
						inc = SE_STOPLINEPACKETSIZE;

						iEventData = new double[inc*ine];
						for (j = 0; j < SE_STOPLINEPACKETSIZE; j++)
						{
							//copy the first line of the packet into iEventData
							iEventData[midx(0, j, ine)] = tEventData[j];
						}

						for (i = 1; i < ine; i++)
						{
							//read the rest of the packet line by line
							SensorLogs->StoplineLog.GetNextLogLine();
							retcode = StringToArrayOfDoubles(tEventData, SE_STOPLINEPACKETSIZE, SensorLogs->StoplineLog.LogBuffer());

							if (retcode != SE_STOPLINEPACKETSIZE)
							{
								//improper packet read: throw out this packet
								printf("Malformed stopline log packet.\n");
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
					SensorLogs->StoplineLog.GetNextLogLine();
				}

				break;

			case 7:
				{
					//local map packet comes next

					//parse out the localmap event information
					double tEventData[SE_LMTHEADERPACKETSIZE];
					double tEventData2[SE_LMTCOVARIANCEPACKETSIZE];
					double tEventData3[SE_LMTPOINTSPACKETSIZE];

					//parse the first target header
					retcode = StringToArrayOfDoubles(tEventData, SE_LMTHEADERPACKETSIZE, SensorLogs->LocalMapLog.LogBuffer());

					if (retcode != SE_LMTHEADERPACKETSIZE)
					{
						//improper packet read: throw out this packet
						printf("Malformed localmap target log packet.\n");
						//try to advance the log
						SensorLogs->LocalMapLog.GetNextLogLine();
					}
					else
					{
						//if code gets here, tEventData was created successfully

						//extract the event time
						iEventTime = tEventData[0];
						iEventType = LOCALMAPTARGETS_EVENT;

						//extract the number of targets and store target headers in iEventData
						ine = (int) (tEventData[2]);
						inc = SE_LMTHEADERPACKETSIZE;
						iEventData = new double[inc*ine];

						ine2 = ine;
						inc2 = SE_LMTCOVARIANCEPACKETSIZE;
						iEventData2 = new double[ine2*inc2];

						ine3 = 0;
						inc3 = SE_LMTPOINTSPACKETSIZE;
						//create temporary space to hold all the points
						int tidx = 0;
						double tSpace[LS_MAXTARGETPOINTS*SE_LMTPOINTSPACKETSIZE];

						for (i = 0; i < ine; i++)
						{
							//read in each target

							//1. copy the target header into iEventData
							if (retcode != SE_LMTHEADERPACKETSIZE)
							{
								//improper packet read: throw out this packet
								printf("Malformed localmap target log packet.\n");
								iEventTime = DBL_MAX;
								iEventType = INVALID_EVENT;

								ine = 0;
								inc = 0;
								delete [] iEventData;
								iEventData = NULL;

								ine2 = 0;
								inc2 = 0;
								delete [] iEventData2;
								iEventData2 = NULL;

								ine3 = 0;
								inc3 = 0;
								delete [] iEventData3;
								iEventData3 = NULL;

								break;
							}

							for (j = 0; j < SE_LMTHEADERPACKETSIZE; j++)
							{
								iEventData[midx(i, j, ine)] = tEventData[j];
							}
							//pull the number of points for this target
							int np = (int) tEventData[10];

							//2. read the covariance packet for this target
							SensorLogs->LocalMapLog.GetNextLogLine();
							retcode = StringToArrayOfDoubles(tEventData2, SE_LMTCOVARIANCEPACKETSIZE, SensorLogs->LocalMapLog.LogBuffer());

							if (retcode != SE_LMTCOVARIANCEPACKETSIZE)
							{
								//improper packet read: throw out this packet
								printf("Malformed localmap target log packet.\n");
								iEventTime = DBL_MAX;
								iEventType = INVALID_EVENT;

								ine = 0;
								inc = 0;
								delete [] iEventData;
								iEventData = NULL;

								ine2 = 0;
								inc2 = 0;
								delete [] iEventData2;
								iEventData2 = NULL;

								ine3 = 0;
								inc3 = 0;
								delete [] iEventData3;
								iEventData3 = NULL;

								break;
							}

							//copy the target covariance into iEventData2
							for (j = 0; j < inc2; j++)
							{
								iEventData2[midx(i, j, ine)] = tEventData2[j];
							}

							//3. read each points packet for this target
							bool pSuccess = true;
							if (np > 0)
							{
								for (j = 0; j < np; j++)
								{
									SensorLogs->LocalMapLog.GetNextLogLine();
									retcode = StringToArrayOfDoubles(tEventData3, SE_LMTPOINTSPACKETSIZE, SensorLogs->LocalMapLog.LogBuffer());

									if (retcode != SE_LMTPOINTSPACKETSIZE)
									{
										//improper packet read: throw out this packet
										pSuccess = false;
										break;
									}

									//copy the point into tSpace
									for (k = 0; k < inc3; k++)
									{
										tSpace[midx(tidx, k, LS_MAXTARGETPOINTS)] = tEventData3[k];
									}
									tidx++;
								}
							}

							if (pSuccess == false)
							{
								//improper packet read: throw out this packet
								printf("Malformed localmap target log packet.\n");
								iEventTime = DBL_MAX;
								iEventType = INVALID_EVENT;

								ine = 0;
								inc = 0;
								delete [] iEventData;
								iEventData = NULL;

								ine2 = 0;
								inc2 = 0;
								delete [] iEventData2;
								iEventData2 = NULL;

								ine3 = 0;
								inc3 = 0;
								delete [] iEventData3;
								iEventData3 = NULL;

								//try to advance the log
								SensorLogs->LocalMapLog.GetNextLogLine();

								break;
							}

							//done parsing one target, move to the next
							SensorLogs->LocalMapLog.GetNextLogLine();
							retcode = StringToArrayOfDoubles(tEventData, SE_LMTHEADERPACKETSIZE, SensorLogs->LocalMapLog.LogBuffer());
						}

						//done parsing all targets, now set the event data
						ine3 = tidx;
						if (ine3 > 0)
						{
							//if there are any points, allocate memory and copy them into iEventData3
							iEventData3 = new double[ine3*inc3];

							for (i = 0; i < ine3; i++)
							{
								for (j = 0; j < inc3; j++)
								{
									iEventData3[midx(i, j, ine3)] = tSpace[midx(i, j, LS_MAXTARGETPOINTS)];
								}
							}
						}

						//all the targets have been processed
						//NOTE: the next target header is already loaded
					}
				}

				break;

			case 8:
				{
					//local points packet comes next

					//parse out the localmap event information
					double tEventData[SE_LOCALPOINTSPACKETSIZE];
					retcode = StringToArrayOfDoubles(tEventData, SE_LOCALPOINTSPACKETSIZE, SensorLogs->LocalPointsLog.LogBuffer());

					if (retcode != SE_LOCALPOINTSPACKETSIZE)
					{
						//improper packet read: throw out this packet
						printf("Malformed local points log packet\n");
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
						iEventType = LOCALMAPPOINTS_EVENT;
						ine = (int) (tEventData[2]);
						inc = SE_LOCALPOINTSPACKETSIZE;

						iEventData = new double[inc*ine];
						for (j = 0; j < inc; j++)
						{
							//copy the first line of the packet into iEventData
							iEventData[midx(0, j, ine)] = tEventData[j];
						}

						for (i = 1; i < ine; i++)
						{
							//read the rest of the packet line by line
							SensorLogs->LocalPointsLog.GetNextLogLine();
							retcode = StringToArrayOfDoubles(tEventData, SE_LOCALPOINTSPACKETSIZE, SensorLogs->LocalPointsLog.LogBuffer());

							if (retcode != SE_LOCALPOINTSPACKETSIZE)
							{
								//improper packet read: throw out this packet
								printf("Malformed local points log packet\n");
								iEventTime = DBL_MAX;
								iEventType = INVALID_EVENT;
								ine = 0;
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
					SensorLogs->LocalPointsLog.GetNextLogLine();
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
		NewEvent->SetEventData2(ine2, inc2, iEventData2);
		NewEvent->SetEventData3(ine3, inc3, iEventData3);
		#ifdef SE_PRINTLOGS
			//create a separate logging event with the same data
			LogEvent = new Event(NewEvent);
		#endif

		bool spush = false;

		switch (NewEvent->EventType())
		{
		case ODOM_EVENT:
		case POSE_EVENT:
		case FRONTMOBILEYEROAD_EVENT:
		case BACKMOBILEYEROAD_EVENT:
		case FRONTJASONROAD_EVENT:
		case BACKJASONROAD_EVENT:
			//positioning packets go to PosteriorPose

			spush = ThePosteriorPoseEventQueue->PushEvent(NewEvent);

			break;

		case LOCALMAPTARGETS_EVENT:
		case LOCALMAPPOINTS_EVENT:
			//localmap packets go to track generator

			spush = TheTrackGeneratorEventQueue->PushEvent(NewEvent);

			break;

		case QUIT_EVENT:
		case TRANSMIT_EVENT:
			//message events go to both queues

			NewEvent2 = new Event(NewEvent);

			spush = ThePosteriorPoseEventQueue->PushEvent(NewEvent);
			TheTrackGeneratorEventQueue->PushEvent(NewEvent2);

			break;
		}

		#ifdef SE_PRINTLOGS
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
