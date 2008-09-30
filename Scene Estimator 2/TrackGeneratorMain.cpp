#include "TrackGeneratorMain.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

DWORD WINAPI TrackGeneratorMain(LPVOID lpparam)
{
	/*
	Track Generator main loop.  Creates the track generator filter and
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

	//***CREATE THE TRACK GENERATOR FILTER***
	printf("Creating TrackGenerator filter...\n");
	TrackGeneratorFilter* TheTrackGenerator = NULL;
	TheTrackGenerator = new TrackGeneratorFilter(&TheRandomNumberGenerator, TheRoadGraph);
	
	//***DECLARE THE RELATIVE POSE QUEUE***
	VehicleOdometry* TheVehicleOdometry;
	TheVehicleOdometry = new VehicleOdometry;
	extern RelativePoseQueue* TheRelativePoseQueue;
	/*
	//the queue monitoring structure
	QueueMonitor TrackGeneratorQueueMonitor;
	TrackGeneratorQueueMonitor.QueueChecks = 0;
	TrackGeneratorQueueMonitor.QueuePackets = 0;
	TrackGeneratorQueueMonitor.QueuePackets2 = 0;
	*/

	//***DECLARE THE EVENT QUEUE***
	//TheTrackGeneratorEventQueue is the (global) event queue
	extern SynchronizedEventQueue* TheTrackGeneratorEventQueue;
	//holder for the current event
	Event* TheCurrentEvent = NULL;

	//***DECLARE THE SENSOR POSITIONS ON THE VEHICLE***
	//the back sick sensor, used for occlusion reasoning in the back
	extern Sensor BackSickSensor;
	//the front center ibeo sensor, used for occlusion reasoning in the front
	extern Sensor CenterIbeoSensor;
	//NOTE: copy the occlusion sensors over to holders and set the yaws to align with vehicle coordinates
	Sensor iFrontOcclusionSensor = CenterIbeoSensor;
	iFrontOcclusionSensor.SensorYaw = 0.0;
	Sensor iBackOcclusionSensor = BackSickSensor;
	iBackOcclusionSensor.SensorYaw = 0.0;

	#ifdef SE_PRINTTRACKS
		printf("Opening tgen output file...\n");
		FILE* TheTrackFile;
		if (fopen_s(&TheTrackFile, "tgenfile.txt", "w") != 0)
		{
			//close the program if the particle log file won't open
			printf("SceneEstimator will now terminate because output file could not be opened.\n");
			delete TheTrackGenerator;
			delete TheVehicleOdometry;

			rStatus = 1;
			return rStatus;
		}
	#endif

	#ifdef SE_PRINTPOINTS
		printf("Opening points output file...\n");
		FILE* TheLoosePointsFile;
		if (fopen_s(&TheLoosePointsFile, "pointsfile.txt", "w") != 0)
		{
			//close the program if the particle log file won't open
			printf("SceneEstimator will now terminate because output file could not be opened.\n");
			#ifdef SE_PRINTTRACKS
				fclose(TheTrackFile);
			#endif
			delete TheTrackGenerator;
			delete TheVehicleOdometry;

			rStatus = 1;
			return rStatus;
		}
	#endif

	//***DECLARE THE SYNCHRONIZATION OBJECTS***
	PosteriorPosePosition* ThePosteriorPosePosition;
	ThePosteriorPosePosition = new PosteriorPosePosition;
	//access to the posterior pose solution
	extern PosteriorPoseQueue* ThePosteriorPoseQueue;
	//access to the global car timestamp
	extern CarTime* TheCarTime;
	//access to the track generator quit event
	extern HANDLE TheTrackGeneratorQuitEvent;
	//access to the track generator watchdog event
	extern HANDLE TheTrackGeneratorWatchdogEvent;

	#ifdef SE_TRANSMITMSGS
		extern SceneEstimatorInterfaceSender* TheSceneEstimatorSender;
		extern SceneEstimatorPublisher* TheSceneEstimatorPublisher;
	#endif

	//******************************************************************************

	//drain the queue before starting
	TheTrackGeneratorEventQueue->EmptyQueue();

	//***BEGIN TRACK GENERATOR MAIN LOOP***

	while (true)
	{
		//1. GET NEXT EVENT
		if (TheTrackGeneratorEventQueue->QueueHasEventsReady() == true)
		{
			//queue has data

			//delete the event that has just been processed
			delete TheCurrentEvent;
			TheCurrentEvent = NULL;

			//and grab the next event in its place
			TheCurrentEvent = TheTrackGeneratorEventQueue->PullEvent();
		}
		else
		{
			//go to sleep until queue has data
			DWORD TrackGeneratorSignalStatus;
			HANDLE TheTrackGeneratorEvents[2] = {TheTrackGeneratorEventQueue->mQueueDataEventHandle, TheTrackGeneratorQuitEvent};
			TrackGeneratorSignalStatus = WaitForMultipleObjectsEx(2, TheTrackGeneratorEvents, false, TG_EVENTTIMEOUT, false);

			if (TrackGeneratorSignalStatus  == WAIT_OBJECT_0 + 1)
			{
				//signal to quit
				break;
			}
			if (TrackGeneratorSignalStatus != WAIT_OBJECT_0)
			{
				printf("TrackGenerator event queue timed out.\n");
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
		if (TheTrackGenerator->TrackGeneratorTime() > TheCurrentEvent->EventTime())
		{
			//NOTE: the event queue is guaranteed to process events in order, so a reverse timestamp
			//means that time jumped backwards consistently and should be reinitialized.
			printf("Warning: got timestamp %.12lg, resetting TrackGenerator...\n", TheCurrentEvent->EventTime());
			TheTrackGenerator->Initialize(TheCurrentEvent->EventTime());
			continue;
		}

		//2. PROCESS THE EVENT ON THE EVENT QUEUE
		switch (TheCurrentEvent->EventType())
		{
		case ODOM_EVENT:
			//odometry measurement came in next

			//NOTE: this shouldn't happen ever...  relative pose information should
			//be pushed to TheRelativePoseQueue directly in the sensor callback.
			printf("Warning: received relative pose packet in TrackGenerator queue.\n");

			//push the measurement to the relative pose queue
			TheRelativePoseQueue->PushPacket(TheCurrentEvent->EventData);

			break;

		case LOCALMAPTARGETS_EVENT:
			//a localmap targets event came in next

			if (TheTrackGenerator->IsInitialized() == true)
			{
				//if initialized, just do a regular track update

				//1. extract the posterior pose solution at the time of the update
				ThePosteriorPoseQueue->GetPosteriorPose(ThePosteriorPosePosition, TheCurrentEvent->EventTime());
				if (ThePosteriorPosePosition->IsValid == false)
				{
					#ifdef SE_COMMONDEBUGMSGS
						printf("Warning: could not find an up to date PosteriorPose solution.\n");
					#endif

					break;
				}
				//predict the posterior pose solution to the exact time of the update
				TheRelativePoseQueue->GetVehicleOdometry(TheVehicleOdometry, ThePosteriorPosePosition->PosteriorPoseTime, TheCurrentEvent->EventTime());
				if (TheVehicleOdometry->IsValid == false)
				{
					#ifdef SE_COMMONDEBUGMSGS
						printf("Warning: could not find valid odometry for the PosteriorPose solution.\n");
					#endif

					break;
				}
				PredictPosteriorPoseSolution(TheCurrentEvent->EventTime(), ThePosteriorPosePosition, TheVehicleOdometry);

				//2. predict track generator to the time of the update
				TheRelativePoseQueue->GetVehicleOdometry(TheVehicleOdometry, TheTrackGenerator->TrackGeneratorTime(), TheCurrentEvent->EventTime());
				if (TheVehicleOdometry->IsValid == true)
				{
					TheTrackGenerator->Predict(TheCurrentEvent->EventTime(), TheVehicleOdometry, ThePosteriorPosePosition);
					//3. do the update
					TheTrackGenerator->UpdateWithLocalMapTargets(TheCurrentEvent->EventTime(), TheCurrentEvent->NumDataRows(), TheCurrentEvent->EventData, 
						TheCurrentEvent->EventData2, TheCurrentEvent->NumDataRows3(), TheCurrentEvent->EventData3, ThePosteriorPosePosition);
				}
				else
				{
					double rpold = TheRelativePoseQueue->LeastRecentQueueTime();
					double rpnew = TheRelativePoseQueue->MostRecentQueueTime();
					double tgold = TheTrackGenerator->TrackGeneratorTime();
					double tgnew = TheCurrentEvent->EventTime();

					if (tgold < rpold)
					{
						printf("Warning: TrackGenerator lagging the relative pose queue by %.12lg ms.\n", (rpold - tgold)*1000.0);
						TheTrackGenerator->Initialize(TheCurrentEvent->EventTime());
					}
					if (tgnew > rpnew)
					{
						printf("Warning: LocalMap targets event leading the relative pose queue by %.12lg ms.\n", (tgnew - rpnew)*1000.0);
						TheTrackGenerator->Initialize(TheCurrentEvent->EventTime());
					}

					#ifdef SE_COMMONDEBUGMSGS
						printf("Warning: invalid relative pose packet at %.12lg.\n", TheCurrentEvent->EventTime());
					#endif
				}
			}
			else
			{
				//initialize tgen with the first valid timestamp
				TheTrackGenerator->Initialize(TheCurrentEvent->EventTime());
			}

			break;

		case LOCALMAPPOINTS_EVENT:
			//a localmap points event came in next

			if (TheTrackGenerator->IsInitialized() == true)
			{
				//if initialized, just do a regular points update

				//1. extract the posterior pose solution at the time of the update
				ThePosteriorPoseQueue->GetPosteriorPose(ThePosteriorPosePosition, TheCurrentEvent->EventTime());
				if (ThePosteriorPosePosition->IsValid == false)
				{
					#ifdef SE_COMMONDEBUGMSGS
						printf("Warning: could not find an up to date PosteriorPose solution.\n");
					#endif

					break;
				}
				//predict the posterior pose solution to the exact time of the update
				TheRelativePoseQueue->GetVehicleOdometry(TheVehicleOdometry, ThePosteriorPosePosition->PosteriorPoseTime, TheCurrentEvent->EventTime());
				if (TheVehicleOdometry->IsValid == false)
				{
					#ifdef SE_COMMONDEBUGMSGS
						printf("Warning: could not find valid odometry for the PosteriorPose solution.\n");
					#endif

					break;
				}
				PredictPosteriorPoseSolution(TheCurrentEvent->EventTime(), ThePosteriorPosePosition, TheVehicleOdometry);

				//2. predict track generator to the time of the update
				TheRelativePoseQueue->GetVehicleOdometry(TheVehicleOdometry, TheTrackGenerator->TrackGeneratorTime(), TheCurrentEvent->EventTime());
				if (TheVehicleOdometry->IsValid == true)
				{
					TheTrackGenerator->Predict(TheCurrentEvent->EventTime(), TheVehicleOdometry, ThePosteriorPosePosition);
					//3. do the update
					TheTrackGenerator->UpdateWithLocalMapPoints(TheCurrentEvent->EventTime(), TheCurrentEvent->NumDataRows(), TheCurrentEvent->EventData);
				}
				else
				{
					double rpold = TheRelativePoseQueue->LeastRecentQueueTime();
					double rpnew = TheRelativePoseQueue->MostRecentQueueTime();
					double tgold = TheTrackGenerator->TrackGeneratorTime();
					double tgnew = TheCurrentEvent->EventTime();

					if (tgold < rpold)
					{
						printf("Warning: TrackGenerator lagging the relative pose queue by %.12lg ms.\n", (rpold - tgold)*1000.0);
						TheTrackGenerator->Initialize(TheCurrentEvent->EventTime());
					}
					if (tgnew > rpnew)
					{
						printf("Warning: LocalMap points event leading the relative pose queue by %.12lg ms.\n", (tgnew - rpnew)*1000.0);
						TheTrackGenerator->Initialize(TheCurrentEvent->EventTime());
					}

					#ifdef SE_COMMONDEBUGMSGS
						printf("Warning: invalid relative pose packet at %.12lg.\n", TheCurrentEvent->EventTime());
					#endif
				}
			}
			else
			{
				//initialize tgen with the first valid timestamp
				TheTrackGenerator->Initialize(TheCurrentEvent->EventTime());
			}

			break;

		case TRANSMIT_EVENT:
			{
				//a transmit event came in next

				//determine the current system time (most recent odometry packet received, NOT car time)
				double CurrentTime;
				CurrentTime = TheRelativePoseQueue->MostRecentQueueTime();

				#ifdef SE_COMMONDEBUGMSGS
					printf("%d items in the track generator event queue at %.12lg...\n", TheTrackGeneratorEventQueue->NumEvents(), CurrentTime);
				#endif
				/*
				//keep track of the average number of events in the event queue
				TrackGeneratorQueueMonitor.QueueChecks++;
				int QueueNumEvents = TheTrackGeneratorEventQueue->NumEvents();
				TrackGeneratorQueueMonitor.QueuePackets += QueueNumEvents;
				TrackGeneratorQueueMonitor.QueuePackets2 += QueueNumEvents*QueueNumEvents;
				*/

				if (TheTrackGenerator->IsInitialized() == false)
				{
					//don't transmit anything if not initialized
					break;
				}

				//1. extract the posterior pose solution at the time of the transmit event
				ThePosteriorPoseQueue->GetPosteriorPose(ThePosteriorPosePosition, CurrentTime);
				if (ThePosteriorPosePosition->IsValid == false)
				{
					#ifdef SE_COMMONDEBUGMSGS
						printf("Warning: could not find an up to date PosteriorPose solution.\n");
					#endif

					break;
				}
				//predict the posterior pose solution to the exact time of the transmit
				TheRelativePoseQueue->GetVehicleOdometry(TheVehicleOdometry, ThePosteriorPosePosition->PosteriorPoseTime, CurrentTime);
				if (TheVehicleOdometry->IsValid == false)
				{
					#ifdef SE_COMMONDEBUGMSGS
						printf("Warning: could not find valid odometry for the PosteriorPose solution.\n");
					#endif

					break;
				}
				PredictPosteriorPoseSolution(CurrentTime, ThePosteriorPosePosition, TheVehicleOdometry);

				//2. predict the tgen solution to the event time
				TheRelativePoseQueue->GetVehicleOdometry(TheVehicleOdometry, TheTrackGenerator->TrackGeneratorTime(), TheCurrentEvent->EventTime());
				if (TheVehicleOdometry->IsValid == false)
				{
					double rpold = TheRelativePoseQueue->LeastRecentQueueTime();
					double rpnew = TheRelativePoseQueue->MostRecentQueueTime();
					double tgold = TheTrackGenerator->TrackGeneratorTime();
					double tgnew = TheCurrentEvent->EventTime();

					if (tgold < rpold)
					{
						printf("Warning: TrackGenerator lagging the relative pose queue by %.12lg ms.\n", (rpold - tgold)*1000.0);
						TheTrackGenerator->Initialize(TheCurrentEvent->EventTime());
					}
					if (tgnew > rpnew)
					{
						printf("Warning: transmit event leading the relative pose queue by %.12lg ms.\n", (tgnew - rpnew)*1000.0);
						TheTrackGenerator->Initialize(TheCurrentEvent->EventTime());
					}

					#ifdef SE_COMMONDEBUGMSGS
						printf("Warning: invalid relative pose packet at %.12lg.\n", TheCurrentEvent->EventTime());
					#endif

					break;
				}

				//3. temporarily predict tgen forward for transmit
				TheRelativePoseQueue->GetVehicleOdometry(TheVehicleOdometry, TheTrackGenerator->TrackGeneratorTime(), CurrentTime);
				if (TheVehicleOdometry->IsValid == false)
				{
					//don't transmit anything if there's no valid odometry
					break;
				}
				//predict forward to the time of transmit
				if (TheTrackGenerator->PredictForTransmit(CurrentTime, TheVehicleOdometry, ThePosteriorPosePosition) == false)
				{
					//don't transmit anything if prediction was unsuccessful
					break;
				}

				//4. populate messages and send them
				#ifdef SE_TRANSMITMSGS
					//send track generator obstacles to the arbiter and operational layers
					SceneEstimatorUntrackedClusterMsg AaronUntrackedClusterMsg;
					TheTrackGenerator->GenerateUntrackedClusterMessage(&AaronUntrackedClusterMsg);
					TheSceneEstimatorSender->SendSceneEstimatorUntrackedClusters(&AaronUntrackedClusterMsg);

					SceneEstimatorTrackedClusterMsg AaronTrackedClusterMsg;
					TheTrackGenerator->GenerateTrackedClusterMessage(&AaronTrackedClusterMsg, ThePosteriorPosePosition);
					TheSceneEstimatorSender->SendSceneEstimatorTrackedClusters(&AaronTrackedClusterMsg);
				#endif

				#ifdef SE_PRINTTRACKS
					//print the tracks to the output file at each transmit event
					TheTrackGenerator->PrintTracks(TheTrackFile, ThePosteriorPosePosition);
				#endif

				#ifdef SE_PRINTPOINTS
					//print the loose points to the output file at each transmit event
					TheTrackGenerator->PrintLoosePoints(TheLoosePointsFile, ThePosteriorPosePosition);
				#endif

				//set the watchdog event after a successful transmit
				SetEvent(TheTrackGeneratorWatchdogEvent);
			}

			break;

		default:
			{
				//some weird event came in

				printf("Warning: type %d event found in TrackGeneratorEventQueue.\n", TheCurrentEvent->EventType());
			}

			break;
		}

		//3. CHECK FOR MAINTENANCE ON THE TRACKS
		TheTrackGenerator->MaintainTracks(&iFrontOcclusionSensor, &iBackOcclusionSensor);

		//4. DRAIN THE QUEUE IF IT IS TOO BACKED UP
		if (TheTrackGeneratorEventQueue->NumEvents() > TG_QUEUEMAXEVENTS)
		{
			//check whether the queue is too big, and drain it if it is
			if (TheTrackGeneratorEventQueue->IsShuttingDown() == false)
			{
				printf("Warning: emptying TrackGenerator event queue at %.12lg.\n", TheTrackGeneratorEventQueue->MostRecentQueueTime());
				/*
				printf("Current TrackGenerator queue size: %d.\n", TheTrackGeneratorEventQueue->NumEvents());
				double QueueAverageSize = ((double) TrackGeneratorQueueMonitor.QueuePackets) / ((double) TrackGeneratorQueueMonitor.QueueChecks);
				printf("Average TrackGenerator queue size: %.12lg.\n", QueueAverageSize);
				printf("StdDev of TrackGenerator queue size: %.12lg.\n", sqrt(((double) TrackGeneratorQueueMonitor.QueuePackets2) 
					/ ((double) TrackGeneratorQueueMonitor.QueueChecks) - QueueAverageSize*QueueAverageSize));
				*/
				TheTrackGeneratorEventQueue->EmptyQueue();
			}
		}
	}

	//DELETE MEMORY ALLOCATED IN SCENE ESTIMATOR

	#ifdef SE_PRINTTRACKS
		//close the track file when the code terminates
		fclose(TheTrackFile);
	#endif

	#ifdef SE_PRINTPOINTS
		//close the loose points file when the code terminates
		fclose(TheLoosePointsFile);
	#endif

	//delete major items in reverse order of creation
	delete ThePosteriorPosePosition;
	delete TheCurrentEvent;
	delete TheVehicleOdometry;
	delete TheTrackGenerator;

	printf("TrackGenerator thread closing...\n");

	rStatus = 0;
	return rStatus;
}
