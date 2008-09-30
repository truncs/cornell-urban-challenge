#include "LocalRoadMain.h"

DWORD WINAPI LocalRoadMain(LPVOID lpparam)
{
	/*
	LocalRoad main loop thread.  Executed from the executive thread.  Runs
	LocalRoad in isolation on a single core.

	INPUTS:
		lpparam - not used.

	OUTPUTS:
		rStatus - 0 if successful exit, 1 if error.
	*/

	DWORD rStatus;

	//***DECLARE THE RELATIVE POSE QUEUE***
	VehicleOdometry* TheVehicleOdometry;
	TheVehicleOdometry = new VehicleOdometry;
	extern RelativePoseQueue* TheRelativePoseQueue;
	//the queue monitoring structure
	QueueMonitor LocalRoadQueueMonitor;
	LocalRoadQueueMonitor.QueueChecks = 0;
	LocalRoadQueueMonitor.QueuePackets = 0;
	LocalRoadQueueMonitor.QueuePackets2 = 0;

	//***DECLARE THE RANDOM NUMBER GENERATOR***
	extern RandomNumberGenerator TheRandomNumberGenerator;

	//***CREATE THE LOCAL ROAD MODEL***
	printf("Creating local road model...\n");
	LocalRoad* TheLocalRoad;
	TheLocalRoad = new LocalRoad();

	//***DECLARE THE EVENT QUEUE***
	//TheLocalRoadEventQueue is the (global) LocalRoad event queue
	extern SynchronizedEventQueue* TheLocalRoadEventQueue;
	//holder for the current event
	Event* TheCurrentEvent = NULL;

	//***GAIN ACCESS TO SYNCHRONIZATION OBJECTS
	extern CarTime* TheCarTime;
	extern LocalMapInterfaceSender* TheLocalMapSender;
	extern HANDLE TheLocalRoadQuitEvent;
	extern HANDLE TheLocalRoadWatchdogEvent;

	//***DEFINE THE SENSOR POSITIONS ON THE VEHICLE***
	//Jason cameras (Tahoe)
	extern Sensor FrontJasonSensor;
	extern Sensor BackJasonSensor;
	//Mobileye cameras (Tahoe)
	extern Sensor FrontMobileyeRoadSensor;
	extern Sensor BackMobileyeRoadSensor;

	#ifdef LM_PRINTROAD
		printf("Opening local road output file...\n");
		FILE* TheRoadFile;
		if (fopen_s(&TheRoadFile, "localroad.txt", "w") != 0)
		{
			//close the program if the output file won't open
			printf("LocalRoad will now terminate because output file could not be opened.\n");

			delete TheCurrentEvent;
			delete TheLocalRoad;
			delete TheVehicleOdometry;

			rStatus = 1;
			return rStatus;
		}
	#endif

	//******************************************************************************

	//drain the event queue before starting up
	TheLocalRoadEventQueue->EmptyQueue();

	//***BEGIN LOCALROAD MAIN LOOP***

	while (true)
	{
		//1. GET NEXT EVENT
		if (TheLocalRoadEventQueue->QueueHasEventsReady() == true)
		{
			//queue has data

			//delete the event that has just been processed
			delete TheCurrentEvent;
			TheCurrentEvent = NULL;

			//and grab the next event in its place
			TheCurrentEvent = TheLocalRoadEventQueue->PullEvent();
		}
		else
		{
			//go to sleep until queue has data
			DWORD LocalRoadSignalStatus;
			HANDLE TheLocalRoadEvents[2] = {TheLocalRoadEventQueue->mQueueDataEventHandle, TheLocalRoadQuitEvent};
			LocalRoadSignalStatus = WaitForMultipleObjectsEx(2, TheLocalRoadEvents, false, LM_EVENTTIMEOUT, false);

			if (LocalRoadSignalStatus == WAIT_OBJECT_0 + 1)
			{
				//signal to quit
				break;
			}
			if (LocalRoadSignalStatus != WAIT_OBJECT_0)
			{
				printf("LocalRoad event queue timed out.\n");
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
		if (TheLocalRoad->LocalRoadTime() > TheCurrentEvent->EventTime())
		{
			//NOTE: the event queue is guaranteed to process events in order, so a reverse timestamp
			//means that time jumped backwards consistently and should be reinitialized.
			printf("Warning: got timestamp %.12lg, resetting LocalRoad...\n", TheCurrentEvent->EventTime());
			TheLocalRoad->Initialize(TheCurrentEvent->EventTime());
			continue;
		}

		//2. PROCESS THE EVENT ON THE EVENT QUEUE
		switch (TheCurrentEvent->EventType())
		{
		case ODOM_EVENT:
			//odometry measurement came in next

			//NOTE: this shouldn't happen ever...  relative pose information should
			//be pushed to TheRelativePoseQueue directly in the sensor callback.
			printf("Warning: received relative pose packet in TheLocalRoadEventQueue.\n");

			//push the measurement to the relative pose queue
			TheRelativePoseQueue->PushPacket(TheCurrentEvent->EventData);

			break;

		case FRONTJASONROAD_EVENT:
		case BACKJASONROAD_EVENT:
			{
				//front / back jason measurement came in next

				//select the camera that is the updating sensor
				Sensor* iJasonSensor;
				switch (TheCurrentEvent->EventType())
				{
				case FRONTJASONROAD_EVENT:
					iJasonSensor = &FrontJasonSensor;
					break;
				case BACKJASONROAD_EVENT:
					iJasonSensor = &BackJasonSensor;
					break;
				}

				if (TheLocalRoad->IsInitialized() == true)
				{
					//extract the odometry between the local road's time and the measurement time
					TheRelativePoseQueue->GetVehicleOdometry(TheVehicleOdometry, TheLocalRoad->LocalRoadTime(), TheCurrentEvent->EventTime());
					if (TheVehicleOdometry->IsValid == true)
					{
						//predict the local road to the time of the update
						TheLocalRoad->Predict(TheCurrentEvent->EventTime(), TheVehicleOdometry);
						//and second do the update
						TheLocalRoad->UpdateWithJason(TheCurrentEvent->EventTime(), TheCurrentEvent->NumDataRows(), TheCurrentEvent->EventData, iJasonSensor);
					}
					else
					{
						double rpold = TheRelativePoseQueue->LeastRecentQueueTime();
						double rpnew = TheRelativePoseQueue->MostRecentQueueTime();
						double lrold = TheLocalRoad->LocalRoadTime();
						double lrnew = TheCurrentEvent->EventTime();

						if (lrold < rpold)
						{
							printf("Warning: LocalRoad lagging the relative pose queue by %.12g ms.\n", (rpold - lrold)*1000.0);
							TheLocalRoad->Initialize(TheCurrentEvent->EventTime());
						}
						if (lrnew > rpnew)
						{
							printf("Warning: roadfinder leading the relative pose queue by %.12g ms.\n", (lrnew - rpnew)*1000.0);
							TheLocalRoad->Initialize(TheCurrentEvent->EventTime());
						}

						#ifdef LM_COMMONDEBUGMSGS
							printf("Warning: invalid relative pose packet at %.12lg.\n", TheCurrentEvent->EventTime());
						#endif
					}
				}
				else
				{
					//initialize the local road model
					TheLocalRoad->Initialize(TheCurrentEvent->EventTime());
				}
			}

			break;

		case FRONTMOBILEYEROAD_EVENT:
		case BACKMOBILEYEROAD_EVENT:
			{
				//front / back mobileye measurement came in next

				//select the camera that is the updating sensor
				Sensor* iMobileyeSensor;
				switch (TheCurrentEvent->EventType())
				{
				case FRONTMOBILEYEROAD_EVENT:
					iMobileyeSensor = &FrontMobileyeRoadSensor;
					break;
				case BACKMOBILEYEROAD_EVENT:
					iMobileyeSensor = &BackMobileyeRoadSensor;
					break;
				}

				if (TheLocalRoad->IsInitialized() == true)
				{
					//extract the odometry between the local road's time and the measurement time
					TheRelativePoseQueue->GetVehicleOdometry(TheVehicleOdometry, TheLocalRoad->LocalRoadTime(), TheCurrentEvent->EventTime());
					if (TheVehicleOdometry->IsValid == true)
					{
						//predict the local road to the time of the update
						TheLocalRoad->Predict(TheCurrentEvent->EventTime(), TheVehicleOdometry);
						//and second do the update
						TheLocalRoad->UpdateWithMobileye(TheCurrentEvent->EventTime(), TheCurrentEvent->EventData, iMobileyeSensor);
					}
					else
					{
						double rpold = TheRelativePoseQueue->LeastRecentQueueTime();
						double rpnew = TheRelativePoseQueue->MostRecentQueueTime();
						double lrold = TheLocalRoad->LocalRoadTime();
						double lrnew = TheCurrentEvent->EventTime();

						if (lrold < rpold)
						{
							printf("Warning: LocalRoad lagging the relative pose queue by %.12g ms.\n", (rpold - lrold)*1000.0);
							TheLocalRoad->Initialize(TheCurrentEvent->EventTime());
						}
						if (lrnew > rpnew)
						{
							printf("Warning: mobileye road leading the relative pose queue by %.12g ms.\n", (lrnew - rpnew)*1000.0);
							TheLocalRoad->Initialize(TheCurrentEvent->EventTime());
						}

						#ifdef LM_COMMONDEBUGMSGS
							printf("Warning: invalid relative pose packet at %.12lg.\n", TheCurrentEvent->EventTime());
						#endif
					}
				}
				else
				{
					//initialize the local road model
					TheLocalRoad->Initialize(TheCurrentEvent->EventTime());
				}
			}

			break;

		case TRANSMIT_EVENT:
			{
				//a transmit event came in next

				//determine the current system time (most recent odometry packet received, NOT car time)
				double CurrentTime;
				CurrentTime = TheRelativePoseQueue->MostRecentQueueTime();

				#ifdef LM_COMMONDEBUGMSGS
					printf("%d items in LocalRoad event queue at %lg...\n", TheLocalRoadEventQueue->NumEvents(), CurrentTime);
				#endif
				//keep track of the average number of events in the event queue
				LocalRoadQueueMonitor.QueueChecks++;
				int QueueNumEvents = TheLocalRoadEventQueue->NumEvents();
				LocalRoadQueueMonitor.QueuePackets += QueueNumEvents;
				LocalRoadQueueMonitor.QueuePackets += QueueNumEvents*QueueNumEvents;

				if (TheLocalRoad->IsInitialized() == false)
				{
					//don't transmit anything if not initialized
					break;
				}

				//predict the local road forward to the signal event time
				TheRelativePoseQueue->GetVehicleOdometry(TheVehicleOdometry, TheLocalRoad->LocalRoadTime(), TheCurrentEvent->EventTime());
				if (TheVehicleOdometry->IsValid == true)
				{
					TheLocalRoad->Predict(TheCurrentEvent->EventTime(), TheVehicleOdometry);
				}
				else
				{
					double rpold = TheRelativePoseQueue->LeastRecentQueueTime();
					double rpnew = TheRelativePoseQueue->MostRecentQueueTime();
					double lrold = TheLocalRoad->LocalRoadTime();
					double lrnew = TheCurrentEvent->EventTime();

					if (lrold < rpold)
					{
						printf("Warning: LocalRoad lagging the relative pose queue by %.12g ms.\n", (rpold - lrold)*1000.0);
						TheLocalRoad->Initialize(TheCurrentEvent->EventTime());
					}
					if (lrnew > rpnew)
					{
						printf("Warning: transmit signal leading the relative pose queue by %.12g ms.\n", (lrnew - rpnew)*1000.0);
						TheLocalRoad->Initialize(TheCurrentEvent->EventTime());
					}

					#ifdef LM_COMMONDEBUGMSGS
						printf("Warning: invalid relative pose packet at %.12lg.\n", TheCurrentEvent->EventTime());
					#endif
				}

				//temporarily predict the local road forward for transmit
				TheRelativePoseQueue->GetVehicleOdometry(TheVehicleOdometry, TheLocalRoad->LocalRoadTime(), CurrentTime);
				if (TheVehicleOdometry->IsValid == false)
				{
					//don't transmit anything if the odometry is invalid
					break;
				}
				if (TheLocalRoad->PredictForTransmit(CurrentTime, TheVehicleOdometry) == false)
				{
					//don't transmit anything if the prediction fails
					break;
				}

				#ifdef LM_TRANSMITMSGS
					//send the localroad to the scene estimator and the viewer
					LocalRoadModelEstimateMsg AaronRoadMsg;
					TheLocalRoad->GenerateLocalRoadMessage(&AaronRoadMsg);
					TheLocalMapSender->SendLocalRoadModel(&AaronRoadMsg);
				#endif

				#ifdef LM_PRINTROAD
					//print the local road model to an output file at each transmit event
					TheLocalRoad->PrintLocalRoad(TheRoadFile);
				#endif

				//set the watchdog event on a successful transmit
				SetEvent(TheLocalRoadWatchdogEvent);
			}

			break;

		default:
			{
				//some weird event came in

				printf("Warning: type %d event found in LocalRoadEventQueue.\n", TheCurrentEvent->EventType());
			}

			break;
		}

		//3. DRAIN THE QUEUE IF IT IS TOO BACKED UP
		if (TheLocalRoadEventQueue->NumEvents() > LR_QUEUEMAXEVENTS)
		{
			//check whether the queue is too big, and drain it if it is
			if (TheLocalRoadEventQueue->IsShuttingDown() == false)
			{
				printf("Warning: emptying LocalRoad event queue at %.12lg.\n", TheLocalRoadEventQueue->MostRecentQueueTime());
				/*
				printf("Current LocalRoad queue size: %d.\n", TheLocalRoadEventQueue->NumEvents());
				double QueueAverageSize = ((double) LocalRoadQueueMonitor.QueuePackets) / ((double) LocalRoadQueueMonitor.QueueChecks);
				printf("Average LocalRoad queue size: %.12lg.\n", QueueAverageSize);
				printf("StdDev of LocalRoad queue size: %.12lg.\n", sqrt(((double) LocalRoadQueueMonitor.QueuePackets2) 
					/ ((double) LocalRoadQueueMonitor.QueueChecks) - QueueAverageSize*QueueAverageSize));
				*/
				TheLocalRoadEventQueue->EmptyQueue();
			}
		}
	}

	//DELETE MEMORY ALLOCATED IN LOCALROAD

	#ifdef LM_PRINTROAD
		//close the output file when the code terminates
		fclose(TheRoadFile);
	#endif

	delete TheCurrentEvent;
	delete TheLocalRoad;
	delete TheVehicleOdometry;

	rStatus = 0;
	return rStatus;
}
