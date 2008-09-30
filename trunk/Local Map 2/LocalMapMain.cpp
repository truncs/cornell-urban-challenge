#include "LocalMapMain.h"

DWORD WINAPI LocalMapMain(LPVOID lpparam)
{
	/*
	LocalMap main loop thread.  Executed from the executive thread.  Runs
	LocalMap in isolation on a single core.

	INPUTS:
		lpparam - not used.

	OUTPUTS:
		rStatus - 0 if successful exit.
	*/

	DWORD rStatus;

	//***DECLARE THE RELATIVE POSE QUEUE***
	VehicleOdometry* TheVehicleOdometry;
	TheVehicleOdometry = new VehicleOdometry;
	extern RelativePoseQueue* TheRelativePoseQueue;
	/*
	//the queue monitoring structure
	QueueMonitor LocalMapQueueMonitor;
	LocalMapQueueMonitor.QueueChecks = 0;
	LocalMapQueueMonitor.QueuePackets = 0;
	LocalMapQueueMonitor.QueuePackets2 = 0;
	*/

	//***DECLARE THE RANDOM NUMBER GENERATOR***
	extern RandomNumberGenerator TheRandomNumberGenerator;

	//***CREATE THE LOCAL MAP***
	printf("Creating local map particle filter...\n");
	LocalMap* TheLocalMap;
	//TheLocalMap = new LocalMap(LM_NUMPARTICLES, &TheRandomNumberGenerator, LM_MAXLIKELIHOOD);
	TheLocalMap = new LocalMap(LM_NUMPARTICLES, &TheRandomNumberGenerator, LM_RANDOM);

	//***DECLARE THE EVENT QUEUE***
	//TheLocalMapEventQueue is the (global) event queue
	extern SynchronizedEventQueue* TheLocalMapEventQueue;
	//holder for the current event
	Event* TheCurrentEvent = NULL;

	//***GAIN ACCESS TO SYNCHRONIZATION OBJECTS
	extern CarTime* TheCarTime;
	extern LocalMapInterfaceSender* TheLocalMapSender;
	extern OccupancyGridInterface* VelodyneGridClient;
	extern HANDLE TheLocalMapQuitEvent;
	extern HANDLE TheLocalMapWatchdogEvent;

	//***DEFINE THE SENSOR POSITIONS ON THE VEHICLE***
	//center Ibeo
	extern Sensor ClusteredIbeoSensor;
	extern Sensor CenterIbeoSensor;
	//front Mobileye
	extern Sensor FrontMobileyeObstacleSensor;
	extern Sensor BackMobileyeObstacleSensor;
	//radars
	extern Sensor Back0RadarSensor;
	extern Sensor Driv3RadarSensor;
	extern Sensor Driv2RadarSensor;
	extern Sensor Driv1RadarSensor;
	extern Sensor Driv0RadarSensor;
	extern Sensor Front0RadarSensor;
	extern Sensor Pass0RadarSensor;
	extern Sensor Pass1RadarSensor;
	extern Sensor Pass2RadarSensor;
	extern Sensor Pass3RadarSensor;
	//side SICKs
	extern Sensor DrivSideSickSensor;
	extern Sensor PassSideSickSensor;
	//rear horizontal SICK
	extern Sensor BackClusteredSickSensor;
	//velodyne
	extern Sensor VelodyneOccupancySensor;

	#ifdef LM_PRINTTARGETS
		printf("Opening local map output file...\n");
		FILE* TheTargetFile;
		if (fopen_s(&TheTargetFile, "localmap.txt", "w") != 0)
		{
			//close the program if the output file won't open
			printf("LocalMap will now terminate because output file could not be opened.\n");

			delete TheCurrentEvent;
			delete TheLocalMap;
			delete TheVehicleOdometry;

			rStatus = 1;
			return rStatus;
		}
		FILE* TheLoosePointsFile;
		if (fopen_s(&TheLoosePointsFile, "localpoints.txt", "w") != 0)
		{
			//close the program if the output file won't open
			printf("LocalMap will now terminate because output file could not be opened.\n");

			fclose(TheTargetFile);
			delete TheCurrentEvent;
			delete TheLocalMap;
			delete TheVehicleOdometry;

			rStatus = 1;
			return rStatus;
		}
	#endif

	//******************************************************************************

	//drain the event queue before starting up
	TheLocalMapEventQueue->EmptyQueue();

	//***BEGIN LOCALMAP MAIN LOOP***

	while (true)
	{
		//1. GET NEXT EVENT
		if (TheLocalMapEventQueue->QueueHasEventsReady() == true)
		{
			//queue has data

			//delete the event that has just been processed
			delete TheCurrentEvent;
			TheCurrentEvent = NULL;

			//and grab the next event in its place
			TheCurrentEvent = TheLocalMapEventQueue->PullEvent();
		}
		else
		{
			//go to sleep until queue has data
			DWORD LocalMapSignalStatus;
			HANDLE TheLocalMapEvents[2] = {TheLocalMapEventQueue->mQueueDataEventHandle, TheLocalMapQuitEvent};
			LocalMapSignalStatus = WaitForMultipleObjectsEx(2, TheLocalMapEvents, false, LM_EVENTTIMEOUT, false);

			if (LocalMapSignalStatus == WAIT_OBJECT_0 + 1)
			{
				//signal to quit
				break;
			}
			if (LocalMapSignalStatus != WAIT_OBJECT_0)
			{
				printf("LocalMap event queue timed out.\n");
			}

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
			break;
		}

		//check for need to reset LocalMap
		if (TheLocalMap->LocalMapTime() > TheCurrentEvent->EventTime())
		{
			//NOTE: the event queue is guaranteed to process events in order, so a reverse timestamp
			//means that time jumped backwards consistently and should be reinitialized.
			printf("Warning: got timestamp %.12lg, resetting LocalMap...\n", TheCurrentEvent->EventTime());
			TheLocalMap->Initialize(TheCurrentEvent->EventTime());
			continue;
		}

		//2. PROCESS THE EVENT ON THE EVENT QUEUE
		switch (TheCurrentEvent->EventType())
		{
		case ODOM_EVENT:
			//odometry measurement came in next

			//NOTE: this shouldn't happen ever...  relative pose information should
			//be pushed to TheRelativePoseQueue directly in the sensor callback.
			printf("Warning: received relative pose packet in delayed event queue.\n");

			//push the measurement to the relative pose queue
			TheRelativePoseQueue->PushPacket(TheCurrentEvent->EventData);

			break;

		case IBEO_EVENT:
			//a clustered ibeo event came next

			if (TheLocalMap->IsInitialized() == true)
			{
				//first predict to the time of the update
				TheRelativePoseQueue->GetVehicleOdometry(TheVehicleOdometry, TheLocalMap->LocalMapTime(), TheCurrentEvent->EventTime());
				if (TheVehicleOdometry->IsValid == true)
				{
					TheLocalMap->Predict(TheCurrentEvent->EventTime(), TheVehicleOdometry);
					//and second do the update
					TheLocalMap->UpdateWithClusteredIbeo(TheCurrentEvent->EventTime(), TheCurrentEvent->NumDataRows(), 
						TheCurrentEvent->EventData, &ClusteredIbeoSensor, TheVehicleOdometry);
					//third, update existence probabilities with ibeo
					TheLocalMap->UpdateExistenceWithClusteredIbeo(TheCurrentEvent->EventTime(), TheCurrentEvent->NumDataRows(), 
						TheCurrentEvent->EventData, &ClusteredIbeoSensor, &CenterIbeoSensor, TheVehicleOdometry);
					//fourth, update existence probabilities with velodyne occupancy grid
					TheLocalMap->UpdateExistenceWithVelodyneGrid(TheCurrentEvent->EventTime(), VelodyneGridClient, 
						&VelodyneOccupancySensor, TheVehicleOdometry);
				}
				else
				{
					//LocalMap is out of synch, so reset it or drop the measurement
					double rpold = TheRelativePoseQueue->LeastRecentQueueTime();
					double rpnew = TheRelativePoseQueue->MostRecentQueueTime();
					double pfold = TheLocalMap->LocalMapTime();
					double pfnew = TheCurrentEvent->EventTime();

					if (pfold < rpold)
					{
						printf("Warning: LocalMap lagging the relative pose queue by %.12lg ms.\n", (rpold - pfold)*1000.0);
						TheLocalMap->Initialize(TheCurrentEvent->EventTime());
					}
					if (pfnew > rpnew)
					{
						printf("Warning: ibeo leading the relative pose queue by %.12lg ms.\n", (pfnew - rpnew)*1000.0);
						TheLocalMap->Initialize(TheCurrentEvent->EventTime());
					}

					#ifdef LM_COMMONDEBUGMSGS
						printf("Warning: invalid relative pose packet at %.12lg.\n", TheCurrentEvent->EventTime());
					#endif
				}
			}
			else
			{
				//initialize the LocalMap with the first measurement
				TheLocalMap->Initialize(TheCurrentEvent->EventTime());
			}

			break;

		case FRONTMOBILEYEOBSTACLE_EVENT:
		case BACKMOBILEYEOBSTACLE_EVENT:
			//a mobileye obstacle packet came in next

			//select the camera that is the updating sensor
			Sensor* iMobileyeSensor;
			switch (TheCurrentEvent->EventType())
			{
			case FRONTMOBILEYEOBSTACLE_EVENT:
				iMobileyeSensor = &FrontMobileyeObstacleSensor;
				break;
			case BACKMOBILEYEOBSTACLE_EVENT:
				iMobileyeSensor = &BackMobileyeObstacleSensor;
				break;
			}

			if (TheLocalMap->IsInitialized() == true)
			{
				//first predict to the time of the update
				TheRelativePoseQueue->GetVehicleOdometry(TheVehicleOdometry, TheLocalMap->LocalMapTime(), TheCurrentEvent->EventTime());
				if (TheVehicleOdometry->IsValid == true)
				{
					TheLocalMap->Predict(TheCurrentEvent->EventTime(), TheVehicleOdometry);
					//and second do the update
					TheLocalMap->UpdateWithMobileyeObstacles(TheCurrentEvent->EventTime(), TheCurrentEvent->NumDataRows(), 
						TheCurrentEvent->EventData, iMobileyeSensor, TheVehicleOdometry);
				}
				else
				{
					//LocalMap is out of synch, so reset it or drop the measurement
					double rpold = TheRelativePoseQueue->LeastRecentQueueTime();
					double rpnew = TheRelativePoseQueue->MostRecentQueueTime();
					double pfold = TheLocalMap->LocalMapTime();
					double pfnew = TheCurrentEvent->EventTime();

					if (pfold < rpold)
					{
						printf("Warning: LocalMap lagging the relative pose queue by %.12lg ms.\n", (rpold - pfold)*1000.0);
						TheLocalMap->Initialize(TheCurrentEvent->EventTime());
					}
					if (pfnew > rpnew)
					{
						printf("Warning: mobileye obstacles leading the relative pose queue by %.12lg ms.\n", (pfnew - rpnew)*1000.0);
						TheLocalMap->Initialize(TheCurrentEvent->EventTime());
					}

					#ifdef LM_COMMONDEBUGMSGS
						printf("Warning: invalid relative pose packet at %.12lg.\n", TheCurrentEvent->EventTime());
					#endif
				}
			}
			else
			{
				//initialize the LocalMap with the first measurement
				TheLocalMap->Initialize(TheCurrentEvent->EventTime());
			}

			break;

		case BACK0RADAR_EVENT:
		case DRIV3RADAR_EVENT:
		case DRIV2RADAR_EVENT:
		case DRIV1RADAR_EVENT:
		case DRIV0RADAR_EVENT:
		case FRONT0RADAR_EVENT:
		case PASS0RADAR_EVENT:
		case PASS1RADAR_EVENT:
		case PASS2RADAR_EVENT:
		case PASS3RADAR_EVENT:
			//some type of radar packet came in

			//select the radar that is the updating sensor
			Sensor* iRadarSensor;
			switch (TheCurrentEvent->EventType())
			{
			case BACK0RADAR_EVENT:
				iRadarSensor = &Back0RadarSensor;
				break;
			case DRIV3RADAR_EVENT:
				iRadarSensor = &Driv3RadarSensor;
				break;
			case DRIV2RADAR_EVENT:
				iRadarSensor = &Driv2RadarSensor;
				break;
			case DRIV1RADAR_EVENT:
				iRadarSensor = &Driv1RadarSensor;
				break;
			case DRIV0RADAR_EVENT:
				iRadarSensor = &Driv0RadarSensor;
				break;
			case FRONT0RADAR_EVENT:
				iRadarSensor = &Front0RadarSensor;
				break;
			case PASS0RADAR_EVENT:
				iRadarSensor = &Pass0RadarSensor;
				break;
			case PASS1RADAR_EVENT:
				iRadarSensor = &Pass1RadarSensor;
				break;
			case PASS2RADAR_EVENT:
				iRadarSensor = &Pass2RadarSensor;
				break;
			case PASS3RADAR_EVENT:
				iRadarSensor = &Pass3RadarSensor;
				break;
			}

			if (TheLocalMap->IsInitialized() == true)
			{
				//first predict to the time of the update
				TheRelativePoseQueue->GetVehicleOdometry(TheVehicleOdometry, TheLocalMap->LocalMapTime(), TheCurrentEvent->EventTime());
				if (TheVehicleOdometry->IsValid == true)
				{
					TheLocalMap->Predict(TheCurrentEvent->EventTime(), TheVehicleOdometry);

					bool CanUpdateWhenMoving = false;
					switch (TheCurrentEvent->EventType())
					{
					case BACK0RADAR_EVENT:
					case FRONT0RADAR_EVENT:
					case DRIV3RADAR_EVENT:
					case PASS3RADAR_EVENT:
						CanUpdateWhenMoving = true;
						break;
					}

					//and second do the update
					if (CanUpdateWhenMoving == true || fabs(TheVehicleOdometry->vx) <= RADAR_MAXSPEEDFORUPDATE)
					{
						//NOTE: radars can only update when the car is moving if they're pointed along the axis of travel
						TheLocalMap->UpdateWithRadar(TheCurrentEvent->EventTime(), TheCurrentEvent->NumDataRows(), 
							TheCurrentEvent->EventData, iRadarSensor, TheVehicleOdometry);
					}
				}
				else
				{
					//LocalMap is out of synch, so reset it or drop the measurement
					double rpold = TheRelativePoseQueue->LeastRecentQueueTime();
					double rpnew = TheRelativePoseQueue->MostRecentQueueTime();
					double pfold = TheLocalMap->LocalMapTime();
					double pfnew = TheCurrentEvent->EventTime();

					if (pfold < rpold)
					{
						printf("Warning: LocalMap lagging the relative pose queue by %.12lg ms.\n", (rpold - pfold)*1000.0);
						TheLocalMap->Initialize(TheCurrentEvent->EventTime());
					}
					if (pfnew > rpnew)
					{
						printf("Warning: radar obstacles leading the relative pose queue by %.12lg ms.\n", (pfnew - rpnew)*1000.0);
						TheLocalMap->Initialize(TheCurrentEvent->EventTime());
					}

					#ifdef LM_COMMONDEBUGMSGS
						printf("Warning: invalid relative pose packet at %.12lg.\n", TheCurrentEvent->EventTime());
					#endif
				}
			}
			else
			{
				//initialize the LocalMap with the first measurement
				TheLocalMap->Initialize(TheCurrentEvent->EventTime());
			}

			break;

		case BACKCLUSTEREDSICK_EVENT:
			//a rear horizontal sick event came in next

			if (TheLocalMap->IsInitialized() == true)
			{
				//first predict to the time of the update
				TheRelativePoseQueue->GetVehicleOdometry(TheVehicleOdometry, TheLocalMap->LocalMapTime(), TheCurrentEvent->EventTime());
				if (TheVehicleOdometry->IsValid == true)
				{
					TheLocalMap->Predict(TheCurrentEvent->EventTime(), TheVehicleOdometry);
					//and second do the update
					TheLocalMap->UpdateWithClusteredSick(TheCurrentEvent->EventTime(), TheCurrentEvent->NumDataRows(), 
						TheCurrentEvent->EventData, &BackClusteredSickSensor, TheVehicleOdometry);
					//third update existence probabilities
					TheLocalMap->UpdateExistenceWithClusteredSick(TheCurrentEvent->EventTime(), TheCurrentEvent->NumDataRows(), 
						TheCurrentEvent->EventData, &BackClusteredSickSensor, TheVehicleOdometry);
				}
				else
				{
					//LocalMap is out of synch, so reset it or drop the measurement
					double rpold = TheRelativePoseQueue->LeastRecentQueueTime();
					double rpnew = TheRelativePoseQueue->MostRecentQueueTime();
					double pfold = TheLocalMap->LocalMapTime();
					double pfnew = TheCurrentEvent->EventTime();

					if (pfold < rpold)
					{
						printf("Warning: LocalMap lagging the relative pose queue by %.12lg ms.\n", (rpold - pfold)*1000.0);
						TheLocalMap->Initialize(TheCurrentEvent->EventTime());
					}
					if (pfnew > rpnew)
					{
						printf("Warning: back clustered sick leading the relative pose queue by %.12lg ms.\n", (pfnew - rpnew)*1000.0);
						TheLocalMap->Initialize(TheCurrentEvent->EventTime());
					}

					#ifdef LM_COMMONDEBUGMSGS
						printf("Warning: invalid relative pose packet at %.12lg.\n", TheCurrentEvent->EventTime());
					#endif
				}
			}
			else
			{
				//initialize the LocalMap with the first measurement
				TheLocalMap->Initialize(TheCurrentEvent->EventTime());
			}

			break;

		case SIDESICKDRIV_EVENT:
		case SIDESICKPASS_EVENT:
			//a side sick event came in next

			if (TheLocalMap->IsInitialized() == true)
			{
				//first predict to the time of the update
				TheRelativePoseQueue->GetVehicleOdometry(TheVehicleOdometry, TheLocalMap->LocalMapTime(), TheCurrentEvent->EventTime());
				if (TheVehicleOdometry->IsValid == true)
				{
					TheLocalMap->Predict(TheCurrentEvent->EventTime(), TheVehicleOdometry);
					//and second do the update
					switch (TheCurrentEvent->EventType())
					{
					case SIDESICKDRIV_EVENT:
						TheLocalMap->UpdateWithSideSick(TheCurrentEvent->EventTime(), TheCurrentEvent->NumDataRows(), TheCurrentEvent->EventData, true, &DrivSideSickSensor, TheVehicleOdometry);
						break;
					case SIDESICKPASS_EVENT:
						TheLocalMap->UpdateWithSideSick(TheCurrentEvent->EventTime(), TheCurrentEvent->NumDataRows(), TheCurrentEvent->EventData, false, &PassSideSickSensor, TheVehicleOdometry);
						break;
					}
				}
				else
				{
					//LocalMap is out of synch, so reset it or drop the measurement
					double rpold = TheRelativePoseQueue->LeastRecentQueueTime();
					double rpnew = TheRelativePoseQueue->MostRecentQueueTime();
					double pfold = TheLocalMap->LocalMapTime();
					double pfnew = TheCurrentEvent->EventTime();

					if (pfold < rpold)
					{
						printf("Warning: LocalMap lagging the relative pose queue by %.12lg ms.\n", (rpold - pfold)*1000.0);
						TheLocalMap->Initialize(TheCurrentEvent->EventTime());
					}
					if (pfnew > rpnew)
					{
						printf("Warning: side lidar leading the relative pose queue by %.12lg ms.\n", (pfnew - rpnew)*1000.0);
						TheLocalMap->Initialize(TheCurrentEvent->EventTime());
					}

					#ifdef LM_COMMONDEBUGMSGS
						printf("Warning: invalid relative pose packet at %.12lg.\n", TheCurrentEvent->EventTime());
					#endif
				}
			}
			else
			{
				//initialize the LocalMap with the first measurement
				TheLocalMap->Initialize(TheCurrentEvent->EventTime());
			}

			break;

		case TRANSMIT_EVENT:
			{
				//a transmit event came in next

				//determine the current system time (most recent odometry packet received, NOT car time)
				double CurrentTime;
				CurrentTime = TheRelativePoseQueue->MostRecentQueueTime();

				#ifdef LM_COMMONDEBUGMSGS
					printf("%d items in the LocalMap event queue at %lg...\n", TheLocalMapEventQueue->NumEvents(), CurrentTime);
				#endif
				/*
				//keep track of the average number of events in the event queue
				LocalMapQueueMonitor.QueueChecks++;
				int QueueNumEvents = TheLocalMapEventQueue->NumEvents();
				LocalMapQueueMonitor.QueuePackets += QueueNumEvents;
				LocalMapQueueMonitor.QueuePackets2 += QueueNumEvents*QueueNumEvents;
				*/

				if (TheLocalMap->IsInitialized() == false)
				{
					//don't transmit anything if not initialized
					break;
				}

				//try to predict the local map forward to the event time
				TheRelativePoseQueue->GetVehicleOdometry(TheVehicleOdometry, TheLocalMap->LocalMapTime(), TheCurrentEvent->EventTime());
				if (TheVehicleOdometry->IsValid == true)
				{
					TheLocalMap->Predict(TheCurrentEvent->EventTime(), TheVehicleOdometry);
				}
				else
				{
					//LocalMap is out of synch, so reset it
					double rpold = TheRelativePoseQueue->LeastRecentQueueTime();
					double rpnew = TheRelativePoseQueue->MostRecentQueueTime();
					double pfold = TheLocalMap->LocalMapTime();
					double pfnew = TheCurrentEvent->EventTime();

					if (pfold < rpold)
					{
						printf("Warning: LocalMap lagging the relative pose queue by %.12lg ms.\n", (rpold - pfold)*1000.0);
						TheLocalMap->Initialize(TheCurrentEvent->EventTime());
					}
					if (pfnew > rpnew)
					{
						printf("Warning: transmit signals leading the relative pose queue by %.12lg ms.\n", (pfnew - rpnew)*1000.0);
						TheLocalMap->Initialize(TheCurrentEvent->EventTime());
					}

					#ifdef LM_COMMONDEBUGMSGS
						printf("Warning: invalid relative pose packet at %.12lg.\n", TheCurrentEvent->EventTime());
					#endif
				}

				//try to predict the local map forward temporarily for transmit
				TheRelativePoseQueue->GetVehicleOdometry(TheVehicleOdometry, TheLocalMap->LocalMapTime(), CurrentTime);
				if (TheVehicleOdometry->IsValid == false)
				{
					//don't transmit anything if the odometry is invalid
					break;
				}
				if (TheLocalMap->PredictForTransmit(CurrentTime, TheVehicleOdometry) == false)
				{
					//don't transmit anything if the prediction fails
					break;
				}

				#ifdef LM_TRANSMITMSGS
					//send the targets to the scene estimator and the viewer
					LocalMapTargetsMsg AaronTargetsMsg;
					TheLocalMap->GenerateTargetsMessage(&AaronTargetsMsg);
					TheLocalMapSender->SendLocalMapTargets(&AaronTargetsMsg);

					//send the loose points to the scene estimator and the viewer
					LocalMapLooseClustersMsg AaronLooseClustersMsg;
					TheLocalMap->GenerateLooseClustersMessage(&AaronLooseClustersMsg);
					TheLocalMapSender->SendLocalMapLooseClusters(&AaronLooseClustersMsg);
				#endif

				#ifdef LM_PRINTTARGETS
					//print the targets to an output file at each transmit event
					TheLocalMap->PrintTargets(TheTargetFile);
					TheLocalMap->PrintLoosePoints(TheLoosePointsFile);
				#endif

				//set the watchdog event on a successful transmit
				SetEvent(TheLocalMapWatchdogEvent);
			}

			break;

		default:
			{
				//some weird event came in

				printf("Warning: type %d event found in LocalMapEventQueue.\n", TheCurrentEvent->EventType());
			}

			break;
		}

		//3. RESAMPLE IF NECESSARY
		TheLocalMap->Resample();

		//4. MAINTAIN TARGETS
		TheLocalMap->MaintainTargets();

		//5. DRAIN THE QUEUE IF IT IS TOO BACKED UP
		if (TheLocalMapEventQueue->NumEvents() > LM_QUEUEMAXEVENTS)
		{
			//check whether the queue is too big, and drain it if it is
			if (TheLocalMapEventQueue->IsShuttingDown() == false)
			{
				printf("Warning: emptying LocalMap event queue at %.12lg.\n", TheLocalMapEventQueue->MostRecentQueueTime());
				/*
				printf("Current LocalMap queue size: %d.\n", TheLocalMapEventQueue->NumEvents());
				double QueueAverageSize = ((double) LocalMapQueueMonitor.QueuePackets) / ((double) LocalMapQueueMonitor.QueueChecks);
				printf("Average LocalMap queue size: %.12lg.\n", QueueAverageSize);
				printf("StdDev of LocalMap queue size: %.12lg.\n", sqrt(((double) LocalMapQueueMonitor.QueuePackets2) 
					/ ((double) LocalMapQueueMonitor.QueueChecks) - QueueAverageSize*QueueAverageSize));
				*/
				TheLocalMapEventQueue->EmptyQueue();
			}
		}
	}

	//DELETE MEMORY ALLOCATED IN LOCALMAP

	#ifdef LM_PRINTTARGETS
		//close the output file when the code terminates
		fclose(TheTargetFile);
		fclose(TheLoosePointsFile);
	#endif

	delete TheCurrentEvent;
	delete TheLocalMap;
	delete TheVehicleOdometry;

	rStatus = 0;
	return rStatus;
}
