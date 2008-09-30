#include "PosteriorPoseMain.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

DWORD WINAPI PosteriorPoseMain(LPVOID lpparam)
{
	/*
	Posterior Pose main loop.  Creates the posterior pose particle filter and
	handles all incoming events.

	INPUTS:
		lpparam - not used.

	OUTPUTS:
		rStatus - returns 0 on a successful exit, 1 otherwise.
	*/

	DWORD rStatus;

	//***DECLARE THE ROADGRAPH***
	extern RoadGraph* TheRoadGraph;

	//***DECLARE THE RANDOM NUMBER GENERATOR
	extern RandomNumberGenerator TheRandomNumberGenerator;

	//***CREATE THE POSTERIOR POSE PARTICLE FILTER***
	printf("Creating PosteriorPose particle filter...\n");
	PosteriorPoseParticleFilter* ThePosteriorPose;
	ThePosteriorPose = new PosteriorPoseParticleFilter(PP_NUMPARTICLES, &TheRandomNumberGenerator);
	
	//***DECLARE THE RELATIVE POSE QUEUE***
	VehicleOdometry* TheVehicleOdometry;
	TheVehicleOdometry = new VehicleOdometry;
	extern RelativePoseQueue* TheRelativePoseQueue;
	//the queue monitoring structure
	QueueMonitor PosteriorPoseQueueMonitor;
	PosteriorPoseQueueMonitor.QueueChecks = 0;
	PosteriorPoseQueueMonitor.QueuePackets = 0;
	PosteriorPoseQueueMonitor.QueuePackets2 = 0;

	//***DECLARE THE EVENT QUEUE***
	//ThePosteriorPoseEventQueue is the (global) event queue
	extern SynchronizedEventQueue* ThePosteriorPoseEventQueue;
	//holder for the current event
	Event* TheCurrentEvent = NULL;

	//***DECLARE THE SENSOR POSITIONS ON THE VEHICLE***
	//front Jason camera (Tahoe)
	extern Sensor FrontJasonSensor;
	//front Mobileye (Tahoe)
	extern Sensor FrontMobileyeSensor;
	//stopline camera (Tahoe)
	extern Sensor StoplineSensor;
	//pose (Tahoe)
	extern Sensor PoseSensor;

	#ifdef SE_PRINTPARTICLES
		printf("Opening ppose output file...\n");
		FILE* TheOutputFile;
		if (fopen_s(&TheOutputFile, "pposefile.txt", "w") != 0)
		{
			//close the program if the particle log file won't open
			printf("SceneEstimator will now terminate because output file could not be opened.\n");
			delete ThePosteriorPose;
			delete TheVehicleOdometry;

			rStatus = 1;
			return rStatus;
		}
	#endif

	//***DECLARE THE SYNCHRONIZATION OBJECTS***
	//access to the posterior pose solution
	extern PosteriorPoseQueue* ThePosteriorPoseQueue;
	//access to the global car timestamp
	extern CarTime* TheCarTime;
	//access to the quit event signaler
	extern HANDLE ThePosteriorPoseQuitEvent;
	//access to the watchdog event
	extern HANDLE ThePosteriorPoseWatchdogEvent;

	#ifdef SE_TRANSMITMSGS
		extern SceneEstimatorPublisher* TheSceneEstimatorPublisher;
		extern SceneEstimatorInterfaceSender* TheSceneEstimatorSender;
	#endif

	//******************************************************************************

	//drain the queue before starting
	ThePosteriorPoseEventQueue->EmptyQueue();

	//***BEGIN POSTERIOR POSE MAIN LOOP***

	while (true)
	{
		//1. GET NEXT EVENT
		if (ThePosteriorPoseEventQueue->QueueHasEventsReady() == true)
		{
			//queue has data

			//delete the event that has just been processed
			delete TheCurrentEvent;
			TheCurrentEvent = NULL;

			//and grab the next event in its place
			TheCurrentEvent = ThePosteriorPoseEventQueue->PullEvent();
		}
		else
		{
			//go to sleep until queue has data
			DWORD PosteriorPoseSignalStatus;
			HANDLE ThePosteriorPoseEvents[2] = {ThePosteriorPoseEventQueue->mQueueDataEventHandle, ThePosteriorPoseQuitEvent};
			PosteriorPoseSignalStatus = WaitForMultipleObjectsEx(2, ThePosteriorPoseEvents, false, PP_EVENTTIMEOUT, false);

			if (PosteriorPoseSignalStatus == WAIT_OBJECT_0 + 1)
			{
				//signal to quit
				break;
			}
			if (PosteriorPoseSignalStatus != WAIT_OBJECT_0)
			{
				printf("PosteriorPose event queue timed out.\n");
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
			//signal for scene estimator to quit
			break;
		}

		//check for need to reset
		if (ThePosteriorPose->PosteriorPoseTime() > TheCurrentEvent->EventTime())
		{
			//NOTE: the event queue is guaranteed to process events in order, so a reverse timestamp
			//means that time jumped backwards consistently and should be reinitialized.
			printf("Warning: got timestamp %.12lg, resetting PosteriorPose...\n", TheCurrentEvent->EventTime());
			ThePosteriorPose->ResetFilter();
			continue;
		}

		//2. PROCESS THE EVENT ON THE EVENT QUEUE
		switch (TheCurrentEvent->EventType())
		{
		case ODOM_EVENT:
			//odometry measurement came in next

			//NOTE: this shouldn't happen ever...  relative pose information should
			//be pushed to TheRelativePoseQueue directly in the sensor callback.
			printf("Warning: received relative pose packet in posterior pose queue.\n");

			//push the measurement to the relative pose queue
			TheRelativePoseQueue->PushPacket(TheCurrentEvent->EventData);

			break;

		case POSE_EVENT:
			//pose measurement came in next

			if (ThePosteriorPose->IsInitialized() == true)
			{
				//if initialized, just do a regular update

				//first predict to the time of the update
				TheRelativePoseQueue->GetVehicleOdometry(TheVehicleOdometry, ThePosteriorPose->PosteriorPoseTime(), TheCurrentEvent->EventTime());
				if (TheVehicleOdometry->IsValid == true)
				{
					ThePosteriorPose->Predict(TheCurrentEvent->EventTime(), TheVehicleOdometry);
					//and second do the update
					ThePosteriorPose->UpdateWithPose(TheCurrentEvent->EventTime(), TheCurrentEvent->EventData, &PoseSensor);
				}
				else
				{
					double rpold = TheRelativePoseQueue->LeastRecentQueueTime();
					double rpnew = TheRelativePoseQueue->MostRecentQueueTime();
					double pfold = ThePosteriorPose->PosteriorPoseTime();
					double pfnew = TheCurrentEvent->EventTime();

					if (pfold < rpold)
					{
						printf("Warning: PosteriorPose lagging the relative pose queue by %.12lg ms.\n", (rpold - pfold)*1000.0);
						ThePosteriorPose->Initialize(TheCurrentEvent->EventTime(), TheCurrentEvent->EventData, TheRoadGraph);
					}
					if (pfnew > rpnew)
					{
						printf("Warning: pose event leading the relative pose queue by %.12lg ms.\n", (pfnew - rpnew)*1000.0);
						ThePosteriorPose->Initialize(TheCurrentEvent->EventTime(), TheCurrentEvent->EventData, TheRoadGraph);
					}

					#ifdef SE_COMMONDEBUGMSGS
						printf("Warning: invalid relative pose packet at %.12lg.\n", TheCurrentEvent->EventTime());
					#endif
				}
			}
			else
			{
				//initialize ppose with the first pose measurement
				ThePosteriorPose->Initialize(TheCurrentEvent->EventTime(), TheCurrentEvent->EventData, TheRoadGraph);
			}

			break;

		case FRONTMOBILEYEROAD_EVENT:
			//front mobileye measurement came in next

			if (ThePosteriorPose->IsInitialized() == true)
			{
				//first predict to the time of the update
				TheRelativePoseQueue->GetVehicleOdometry(TheVehicleOdometry, ThePosteriorPose->PosteriorPoseTime(), TheCurrentEvent->EventTime());
				if (TheVehicleOdometry->IsValid == true)
				{
					ThePosteriorPose->Predict(TheCurrentEvent->EventTime(), TheVehicleOdometry);
					//and second do the lane update
					ThePosteriorPose->UpdateWithMobileyeLane(TheCurrentEvent->EventTime(), TheCurrentEvent->EventData, &FrontMobileyeSensor);
					//and third do the line update
					//ThePosteriorPose->UpdateWithMobileyeLines(TheCurrentEvent->EventTime(), TheCurrentEvent->EventData, &FrontMobileyeSensor);
				}
				else
				{
					double rpold = TheRelativePoseQueue->LeastRecentQueueTime();
					double rpnew = TheRelativePoseQueue->MostRecentQueueTime();
					double pfold = ThePosteriorPose->PosteriorPoseTime();
					double pfnew = TheCurrentEvent->EventTime();

					if (pfold < rpold)
					{
						printf("Warning: PosteriorPose lagging the relative pose queue by %.12g ms.\n", (rpold - pfold)*1000.0);
					}
					if (pfnew > rpnew)
					{
						printf("Warning: mobileye leading the relative pose queue by %.12g ms.\n", (pfnew - rpnew)*1000.0);
					}

					#ifdef SE_COMMONDEBUGMSGS
						printf("Warning: invalid relative pose packet at %.12lg.\n", TheCurrentEvent->EventTime());
					#endif
				}
			}

			break;

		case FRONTJASONROAD_EVENT:
			//front jason measurement came in next

			if (ThePosteriorPose->IsInitialized() == true)
			{
				//first predict to the time of the update
				TheRelativePoseQueue->GetVehicleOdometry(TheVehicleOdometry, ThePosteriorPose->PosteriorPoseTime(), TheCurrentEvent->EventTime());
				if (TheVehicleOdometry->IsValid == true)
				{
					ThePosteriorPose->Predict(TheCurrentEvent->EventTime(), TheVehicleOdometry);
					//and second do the update
					ThePosteriorPose->UpdateWithJasonLane(TheCurrentEvent->EventTime(), TheCurrentEvent->NumDataRows(), TheCurrentEvent->EventData, &FrontJasonSensor);
				}
				else
				{
					double rpold = TheRelativePoseQueue->LeastRecentQueueTime();
					double rpnew = TheRelativePoseQueue->MostRecentQueueTime();
					double pfold = ThePosteriorPose->PosteriorPoseTime();
					double pfnew = TheCurrentEvent->EventTime();

					if (pfold < rpold)
					{
						printf("Warning: PosteriorPose lagging the relative pose queue by %.12lg ms.\n", (rpold - pfold)*1000.0);
					}
					if (pfnew > rpnew)
					{
						printf("Warning: roadfinder leading the relative pose queue by %.12lg ms.\n", (pfnew - rpnew)*1000.0);
					}

					#ifdef SE_COMMONDEBUGMSGS
						printf("Warning: invalid relative pose packet at %.12lg.\n", TheCurrentEvent->EventTime());
					#endif
				}
			}

			break;

		case STOPLINE_EVENT:
			//stopline measurement came in next

			if (ThePosteriorPose->IsInitialized() == true)
			{
				//first predict to the time of the update
				TheRelativePoseQueue->GetVehicleOdometry(TheVehicleOdometry, ThePosteriorPose->PosteriorPoseTime(), TheCurrentEvent->EventTime());
				if (TheVehicleOdometry->IsValid == true)
				{
					ThePosteriorPose->Predict(TheCurrentEvent->EventTime(), TheVehicleOdometry);
					//and second do the update
					ThePosteriorPose->UpdateWithAaronStopline(TheCurrentEvent->EventTime(), TheCurrentEvent->NumDataRows(), TheCurrentEvent->EventData, &StoplineSensor);
				}
				else
				{
					double rpold = TheRelativePoseQueue->LeastRecentQueueTime();
					double rpnew = TheRelativePoseQueue->MostRecentQueueTime();
					double pfold = ThePosteriorPose->PosteriorPoseTime();
					double pfnew = TheCurrentEvent->EventTime();

					if (pfold < rpold)
					{
						printf("Warning: PosteriorPose lagging the relative pose queue by %.12lg ms.\n", (rpold - pfold)*1000.0);
					}
					if (pfnew > rpnew)
					{
						printf("Warning: stopline leading the relative pose queue by %.12lg ms.\n", (pfnew - rpnew)*1000.0);
					}

					#ifdef SE_COMMONDEBUGMSGS
						printf("Warning: invalid relative pose packet at %.12lg.\n", TheCurrentEvent->EventTime());
					#endif
				}
			}

			break;

		case LOCALMAPROAD_EVENT:
			{
				//a local road message came in

				if (ThePosteriorPose->IsInitialized() == true)
				{
					//first predict to the time of the local road packet
					TheRelativePoseQueue->GetVehicleOdometry(TheVehicleOdometry, ThePosteriorPose->PosteriorPoseTime(), TheCurrentEvent->EventTime());
					if (TheVehicleOdometry->IsValid == true)
					{
						ThePosteriorPose->Predict(TheCurrentEvent->EventTime(), TheVehicleOdometry);
						//and second store the local road model
						ThePosteriorPose->UpdateWithLocalRoad(TheCurrentEvent->EventTime(), TheCurrentEvent->NumDataRows(), TheCurrentEvent->EventData, 
							TheCurrentEvent->NumDataRows2(), TheCurrentEvent->EventData2);
					}
					else
					{
						double rpold = TheRelativePoseQueue->LeastRecentQueueTime();
						double rpnew = TheRelativePoseQueue->MostRecentQueueTime();
						double pfold = ThePosteriorPose->PosteriorPoseTime();
						double pfnew = TheCurrentEvent->EventTime();

						if (pfold < rpold)
						{
							printf("Warning: PosteriorPose lagging the relative pose queue by %.12lg ms.\n", (rpold - pfold)*1000.0);
						}
						if (pfnew > rpnew)
						{
							printf("Warning: LocalRoad leading the relative pose queue by %.12lg ms.\n", (pfnew - rpnew)*1000.0);
						}

						#ifdef SE_COMMONDEBUGMSGS
							printf("Warning: invalid relative pose packet at %.12lg.\n", TheCurrentEvent->EventTime());
						#endif
					}
				}
			}

			break;

		case TRANSMIT_EVENT:
			{
				//a transmit event came in next

				//determine the current system time (most recent odometry packet received, NOT car time)
				double CurrentTime;
				CurrentTime = TheRelativePoseQueue->MostRecentQueueTime();

				#ifdef SE_COMMONDEBUGMSGS
					printf("%d items in the posterior pose event queue at %.12lg...\n", ThePosteriorPoseEventQueue->NumEvents(), CurrentTime);
				#endif
				//keep track of the average number of events in the event queue
				PosteriorPoseQueueMonitor.QueueChecks++;
				int QueueNumEvents = ThePosteriorPoseEventQueue->NumEvents();
				PosteriorPoseQueueMonitor.QueuePackets += QueueNumEvents;
				PosteriorPoseQueueMonitor.QueuePackets2 += QueueNumEvents*QueueNumEvents;

				if (ThePosteriorPose->IsInitialized() == false)
				{
					//don't transmit anything if the filter isn't initialized
					break;
				}

				//predict ppose to the time of the signal event
				TheRelativePoseQueue->GetVehicleOdometry(TheVehicleOdometry, ThePosteriorPose->PosteriorPoseTime(), TheCurrentEvent->EventTime());
				if (TheVehicleOdometry->IsValid == true)
				{
					ThePosteriorPose->Predict(TheCurrentEvent->EventTime(), TheVehicleOdometry);
				}
				else
				{
					double rpold = TheRelativePoseQueue->LeastRecentQueueTime();
					double rpnew = TheRelativePoseQueue->MostRecentQueueTime();
					double pfold = ThePosteriorPose->PosteriorPoseTime();
					double pfnew = TheCurrentEvent->EventTime();

					if (pfold < rpold)
					{
						printf("Warning: PosteriorPose lagging the relative pose queue by %.12lg ms.\n", (rpold - pfold)*1000.0);
					}
					if (pfnew > rpnew)
					{
						printf("Warning: transmit signal leading the relative pose queue by %.12lg ms.\n", (pfnew - rpnew)*1000.0);
					}

					#ifdef SE_COMMONDEBUGMSGS
						printf("Warning: invalid relative pose packet at %.12lg.\n", TheCurrentEvent->EventTime());
					#endif

					break;
				}

				//predict the ppose solution to the current time temporarily
				TheRelativePoseQueue->GetVehicleOdometry(TheVehicleOdometry, ThePosteriorPose->PosteriorPoseTime(), CurrentTime);
				if (TheVehicleOdometry->IsValid == false)
				{
					//don't transmit anything if there's no valid odometry
					break;
				}
				//predict forward to the time of transmit
				if (ThePosteriorPose->PredictForTransmit(CurrentTime, TheVehicleOdometry) == false)
				{
					//don't transmit anything if prediction was unsuccessful
					break;
				}
				if (ThePosteriorPose->SetLocalRoadForTransmit() == false)
				{
					//don't transmit anything if the local road representation is invalid
					break;
				}

				//extract the current posterior pose position and send to track generator
				PosteriorPosePosition ThePosteriorPosePosition;
				ThePosteriorPose->GetPosteriorPosePositionForTransmit(ThePosteriorPosePosition);
				ThePosteriorPoseQueue->PushPacket(&ThePosteriorPosePosition);

				#ifdef SE_COMMONDEBUGMSGS
					ThePosteriorPose->DisplayArbiterMessage();
					ThePosteriorPose->DisplayOperationalMessage();
				#endif SE_COMMONDEBUGMSGS

				#ifdef SE_TRANSMITMSGS
					//send vehicle state and local road information to the arbiter and operational layers

					UnmanagedArbiterPositionMessage FrankMsg;
					ThePosteriorPose->SetArbiterMessage(&FrankMsg);
					TheSceneEstimatorPublisher->PublishArbiterPositionMessage(&FrankMsg);

					UnmanagedOperationalMessage BrianMsg;
					ThePosteriorPose->SetOperationalMessage(&BrianMsg);
					TheSceneEstimatorPublisher->PublishOperationalMessage(&BrianMsg);

					LocalRoadModelEstimateMsg BrianMsg2;
					ThePosteriorPose->SetLocalRoadMessage(&BrianMsg2);
					TheSceneEstimatorSender->SendSceneEstimatorLocalRoadModel(&BrianMsg2);

					/*
					//send particles to viewer program (NOTE: kills bandwidth)
					SceneEstimatorParticlePointsMsg AaronMsg;
					ThePosteriorPose->SetViewerMessage(&AaronMsg);
					TheSceneEstimatorSender->SendSceneEstimatorParticlePoints(&AaronMsg);
					*/
				#endif

				#ifdef SE_PRINTPARTICLES
					//print the particles to the output file at each transmit event
					ThePosteriorPose->PrintParticles(TheOutputFile);
				#endif

				//set the watchdog event after a successful transmit
				SetEvent(ThePosteriorPoseWatchdogEvent);
			}

			break;

		default:
			{
				//some weird event came in

				printf("Warning: type %d event found in PosteriorPoseEventQueue.\n", TheCurrentEvent->EventType());
			}

			break;
		}

		//3. RESAMPLE IF NECESSARY
		ThePosteriorPose->Resample();

		//4. DRAIN THE QUEUE IF IT IS TOO BACKED UP
		if (ThePosteriorPoseEventQueue->NumEvents() > PP_QUEUEMAXEVENTS)
		{
			//check whether the queue is too big, and drain it if it is
			if (ThePosteriorPoseEventQueue->IsShuttingDown() == false)
			{
				printf("Warning: emptying PosteriorPose event queue at %.12lg.\n", ThePosteriorPoseEventQueue->MostRecentQueueTime());
				/*
				printf("Current PosteriorPose queue size: %d.\n", ThePosteriorPoseEventQueue->NumEvents());
				double QueueAverageSize = ((double) PosteriorPoseQueueMonitor.QueuePackets) / ((double) PosteriorPoseQueueMonitor.QueueChecks);
				printf("Average PosteriorPose queue size: %.12lg.\n", QueueAverageSize);
				printf("StdDev of PosteriorPose queue size: %.12lg.\n", sqrt(((double) PosteriorPoseQueueMonitor.QueuePackets2) 
					/ ((double) PosteriorPoseQueueMonitor.QueueChecks) - QueueAverageSize*QueueAverageSize));
				*/
				ThePosteriorPoseEventQueue->EmptyQueue();
			}
		}
	}

	//DELETE MEMORY ALLOCATED IN SCENE ESTIMATOR

	#ifdef SE_PRINTPARTICLES
		//close the output file when the code terminates
		fclose(TheOutputFile);
	#endif

	//delete major items in reverse order of creation
	delete TheCurrentEvent;
	delete ThePosteriorPose;
	delete TheVehicleOdometry;

	printf("PosteriorPose thread closing...\n");

	rStatus = 0;
	return rStatus;
}
