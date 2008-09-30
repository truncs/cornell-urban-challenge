// Main.cpp : Defines the entry point for the console application.
//

//general includes
#include "CarTime.h"
#include "Event.h"
#include "EventCodes.h"
#include "Globals.h"
#include "PosteriorPoseMain.h"
#include "PosteriorPoseQueue.h"
#include "RelativePoseQueue.h"
#include "RoadGraph.h"
#include "SceneEstimatorConstants.h"
#include "Sensor.h"
#include "SynchronizedEventQueue.h"
#include "time/timing.h"
#include "TrackGeneratorMain.h"
#include "TransmitSignaler.h"

//includes for interface with log sensors
#include "SceneEstimatorLogSensor.h"
#include "SceneEstimatorLogWriter.h"

//includes for interface with real sensors
#include "LN200/ln200.h"
#include "localMapInterface/localMapInterface.h"
#include "mobileyeInterface/mobileyeInterface.h"
#include "poseinterface/poseclient.h"
#include "roadFitterInterface/roadFitterInterface.h"
#include "sceneEstimatorInterface/sceneEstimatorInterface.h"
#include "SensorCallbacks.h"
#include "stoplineInterface/stoplineInterface.h"

//includes for interfaces to AI
#include "sceneEstimatorInterface/sceneEstimatorInterface.h"
#include "SceneEstimatorPublisher.h"

#include <STDIO.H>
#include <WINDOWS.H>

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

//the shutting down function
void ShutDownSceneEstimator();
//the function to handle console interrupts
BOOL WINAPI ConsoleEventHandlerCallback(DWORD dwCtrlType);

int main(int argc, char** argv)
{
	//initialize the SceneEstimator
	SceneEstimatorScreenInit();

	//set SceneEstimator to receive application close events
	SetConsoleCtrlHandler(ConsoleEventHandlerCallback, TRUE);

	#ifdef SE_PRINTLOGS
		//***CREATE THE LOG WRITER***
		extern SceneEstimatorLogWriter* TheLogWriter;
		if (argc > 2)
		{
			//create the log writer with an input directory
			TheLogWriter = new SceneEstimatorLogWriter(argv[2]);
		}
		else
		{
			TheLogWriter = new SceneEstimatorLogWriter();
		}
		if (TheLogWriter->IsRunning() == false)
		{
			printf("SceneEstimator will now terminate because log writer failed to initialize.\n");
			delete TheLogWriter;
			return 1;
		}
	#endif

	//***INITIALIZE THE RANDOM NUMBER GENERATOR
	extern RandomNumberGenerator TheRandomNumberGenerator;
	//attempt to seed the RNG
	FILE* TheSeedFile;
	if (fopen_s(&TheSeedFile, "seed.txt", "r") != 0)
	{
		//error opening the seed file: seed the generator randomly
		TheRandomNumberGenerator.SeedGenerator(RAND_MERSENNETWISTER);
	}
	else
	{
		//successfully opened seed file, so use it to seed the generator
		fclose(TheSeedFile);
		TheRandomNumberGenerator.SeedGeneratorFromFile("seed.txt");
	}

	//***LOAD THE ROAD GRAPH***
	extern RoadGraph* TheRoadGraph;
	if (argc > 1)
	{
		//attempt to use the road graph from the command line
		TheRoadGraph = new RoadGraph(argv[1], PP_NUMCACHEROWS, PP_NUMCACHECOLS);
	}
	else
	{
		//no command line arguments specifying road graph: quit.
		printf("SceneEstimator will now terminate because road graph has not been supplied in the command line.\n");

		delete TheRoadGraph;
		#ifdef SE_PRINTLOGS
			delete TheLogWriter;
		#endif

		return 1;
	}

	if (TheRoadGraph->IsValid() == false)
	{
		printf("SceneEstimator will now terminate because road graph is not valid.\n");

		delete TheRoadGraph;
		#ifdef SE_PRINTLOGS
			delete TheLogWriter;
		#endif

		return 1;
	}

	#ifdef SE_ROADGRAPHTEST
		//test the road graph and then quit
		TheRoadGraph->TestRoadGraph();

		delete TheRoadGraph;
		#ifdef SE_PRINTLOGS
			delete TheLogWriter;
		#endif

		return 0;
	#endif
	
	printf("Initializing event queues...\n");
	//***DECLARE THE RELATIVE POSE QUEUE***
	extern RelativePoseQueue* TheRelativePoseQueue;
	TheRelativePoseQueue = new RelativePoseQueue(PP_RPPQUEUESIZE, ODOM_QWX, ODOM_QWY, ODOM_QWZ, ODOM_QVX, ODOM_QVY, ODOM_QVZ);

	//***DECLARE THE POSTERIOR POSE EVENT QUEUE
	extern SynchronizedEventQueue* ThePosteriorPoseEventQueue;
	ThePosteriorPoseEventQueue = new SynchronizedEventQueue(PP_QUEUEDELAY);

	//***DECLARE THE TRACK GENERATOR EVENT QUEUE
	extern SynchronizedEventQueue* TheTrackGeneratorEventQueue;
	TheTrackGeneratorEventQueue = new SynchronizedEventQueue(TG_QUEUEDELAY);

	//***DEFINE THE SENSOR POSITIONS ON THE VEHICLE***
	printf("Loading sensor configuration...\n");
	//back SICK (Tahoe)
	extern Sensor BackSickSensor;
	BackSickSensor.SensorID = BACKCLUSTEREDSICK_EVENT;
	BackSickSensor.SensorX = -1.1049;
	BackSickSensor.SensorY = 0.0;
	BackSickSensor.SensorZ = -0.4699;
	BackSickSensor.SensorYaw = 3.14159265358979;
	BackSickSensor.SensorPitch = 0.0;
	BackSickSensor.SensorRoll = 0.0;
	//center ibeo (Tahoe)
	extern Sensor CenterIbeoSensor;
	CenterIbeoSensor.SensorID = IBEOCENTERRAW_EVENT;
	CenterIbeoSensor.SensorX = 3.6449;
	CenterIbeoSensor.SensorY = 0.0;
	CenterIbeoSensor.SensorZ = -0.0508;
	CenterIbeoSensor.SensorYaw =  0.0;
	CenterIbeoSensor.SensorPitch = 0.0;
	CenterIbeoSensor.SensorRoll = 0.0;
	//front Jason camera (Tahoe)
	extern Sensor FrontJasonSensor;
	FrontJasonSensor.SensorID = FRONTJASONROAD_EVENT;
	FrontJasonSensor.SensorX = 1.7272;
	FrontJasonSensor.SensorY = 0.0;
	FrontJasonSensor.SensorZ = 0.9017;
	FrontJasonSensor.SensorYaw = 0.0;
	//FrontJasonSensor.SensorYaw = 0.23/180.0*PI;
	FrontJasonSensor.SensorPitch = 8.25/180.0*PI;
	FrontJasonSensor.SensorRoll = 0.0;
	//front Mobileye (Tahoe)
	extern Sensor FrontMobileyeSensor;
	FrontMobileyeSensor.SensorID = FRONTMOBILEYEROAD_EVENT;
	FrontMobileyeSensor.SensorX = 1.7272;
	FrontMobileyeSensor.SensorY = 0.0;
	FrontMobileyeSensor.SensorZ = 0.9017;
	//FrontMobileyeSensor.SensorYaw = 0.23/180.0*PI;
	FrontMobileyeSensor.SensorYaw = 0.0;
	FrontMobileyeSensor.SensorPitch = 8.25/180.0*PI;
	FrontMobileyeSensor.SensorRoll = 0.0;
	//pose (Tahoe)
	extern Sensor PoseSensor;
	PoseSensor.SensorID = POSE_EVENT;
	PoseSensor.SensorX = 0.0;
	PoseSensor.SensorY = 0.0;
	PoseSensor.SensorZ = 0.0;
	PoseSensor.SensorYaw = 0.0;
	PoseSensor.SensorPitch = 0.0;
	PoseSensor.SensorRoll = 0.0;
	//stopline camera (Tahoe)
	extern Sensor StoplineSensor;
	StoplineSensor.SensorID = STOPLINE_EVENT;
	StoplineSensor.SensorX = 3.6949;
	StoplineSensor.SensorY = -0.3429;
	StoplineSensor.SensorZ = -0.03175;
	StoplineSensor.SensorYaw = 0.0;
	StoplineSensor.SensorPitch = 0.0;
	StoplineSensor.SensorRoll = 0.0;

	//***CREATE SYNCHRONIZATION OBJECTS***
	printf("Creating synchronization objects...\n");
	//the queue of posterior poses
	extern PosteriorPoseQueue* ThePosteriorPoseQueue;
	ThePosteriorPoseQueue = new PosteriorPoseQueue(PP_PPQUEUESIZE);
	//the current car timestamp
	extern CarTime* TheCarTime;
	TheCarTime = new CarTime();
	TheCarTime->SetCurrentCarTime(Q_MINTIMESTAMP);
	//the event that signals posterior pose to quit
	extern HANDLE ThePosteriorPoseQuitEvent;
	ThePosteriorPoseQuitEvent = CreateEventA(NULL, false, false, "ThePosteriorPoseQuitEvent");
	//the event that signals track generator to quit
	extern HANDLE TheTrackGeneratorQuitEvent;
	TheTrackGeneratorQuitEvent = CreateEventA(NULL, false, false, "TheTrackGeneratorQuitEvent");
	//the PosteriorPose watchdog event
	extern HANDLE ThePosteriorPoseWatchdogEvent;
	ThePosteriorPoseWatchdogEvent = CreateEventA(NULL, false, false, "ThePosteriorPoseWatchdogEvent");
	//the TrackGenerator watchdog event
	extern HANDLE TheTrackGeneratorWatchdogEvent;
	TheTrackGeneratorWatchdogEvent = CreateEventA(NULL, false, false, "TheTrackGeneratorWatchdogEvent");

	//***CREATE SENSOR INTERFACES***

	#ifdef SE_LOGSENSORS
		//***LOG FILES AND FAKE SENSORS***

		//full set of logs
		printf("Initializing log sensors...\n");
		extern SceneEstimatorLogSensor* TheLogSensor;
		TheLogSensor = new SceneEstimatorLogSensor(5, "odom.txt", "pose.txt", "frontmobileyeroad.txt", "backmobileyeroad.txt", 
			"frontjasonroad.txt", "backjasonroad.txt", "stopline.txt", "localmap.txt", "localpoints.txt");

		//transmit signaler for displaying messages
		printf("Initializing transmit signaler...\n");
		extern TransmitSignaler* TheTransmitSignaler;
		TheTransmitSignaler = new TransmitSignaler(100);
	#endif

	#ifdef SE_REALSENSORS
		//***REAL SENSORS***

		//the interface to the LN200 (for timing)
		extern LN200InterfaceReceiver* LN200Client;
		LN200Client = NULL;
		//the interface to the LocalMap
		extern LocalMapInterfaceReceiver* LocalMapClient;
		LocalMapClient = NULL;
		//the interface to the mobileyes
		extern MobilEyeInterfaceReceiver* MobileyeClient;
		MobileyeClient = NULL;
		//the interface to pose and odometry
		extern pose_client* PoseClient;
		PoseClient = NULL;
		//the interface to Jason's road fitter
		extern RoadFitterInterfaceReceiver* RoadFitterClient;
		RoadFitterClient = NULL;
		//the interface to the stopline camera
		extern StopLineInterfaceReceiver* StoplineClient;
		StoplineClient = NULL;

		//initialize the LN200 timing
		printf("Initializing LN200...\n");
		LN200Client = new LN200InterfaceReceiver();
		LN200Client->SetDTCallback(&LN200Callback, NULL, SE_TIMINGPERIOD);

		//initialize the localmap interface
		printf("Initializing local map interface...\n");
		LocalMapClient = new LocalMapInterfaceReceiver();
		LocalMapClient->SetLocalMapLooseClustersCallback(&LocalMapLooseClustersCallback, NULL);
		LocalMapClient->SetLocalMapTargetsCallback(&LocalMapTargetsCallback, NULL);
		LocalMapClient->SetLocalMapLocalRoadModelCallback(&LocalRoadCallback, NULL);

		//initialize front mobileye
		printf("Initializing front mobileye...\n");
		MobileyeClient = new MobilEyeInterfaceReceiver();
		MobileyeClient->SetRoadEnvInfoCallback(&MobileyeCallback, NULL);

		//initialize pose and odometry
		printf("Initializing pose and odometry...\n");
		PoseClient = new pose_client();
		PoseClient->register_rel_callback(OdometryCallback, NULL);
		PoseClient->register_abs_callback(PoseCallback, NULL);

		//initialize front jason road finder
		printf("Initializing roadfinder...\n");
		RoadFitterClient = new RoadFitterInterfaceReceiver();
		RoadFitterClient->SetRoadFitterOutputCallback(&JasonCallback, NULL);

		//initialize stopline
		printf("Initializing stopline...\n");
		StoplineClient = new StopLineInterfaceReceiver();
		StoplineClient->SetStopLineCallback(&StoplineCallback, NULL);

		//create and initialize the communications signaler
		printf("Initializing transmit signaler...\n");
		extern TransmitSignaler* TheTransmitSignaler;
		TheTransmitSignaler = new TransmitSignaler(100);
	#endif

	#ifdef SE_TRANSMITMSGS
		//the interface to the AI (note: this has to be created AFTER the pose interface)
		printf("Initializing the scene estimator publisher...\n");
		extern SceneEstimatorPublisher* TheSceneEstimatorPublisher;
		//initialize the scene estimator publisher (NB: this must happen after poseclient is initialized)
		TheSceneEstimatorPublisher = new SceneEstimatorPublisher(PoseClient);
		extern SceneEstimatorInterfaceSender* TheSceneEstimatorSender;
		TheSceneEstimatorSender = new SceneEstimatorInterfaceSender();
	#endif

	//go to sleep to let the sensors start up
	Sleep(PP_EVENTTIMEOUT);
	//start up the PosteriorPose thread
	printf("Starting PosteriorPose thread...\n");
	extern HANDLE PosteriorPoseThread;
	PosteriorPoseThread = NULL;
	PosteriorPoseThread = CreateThread(NULL, 0, PosteriorPoseMain, NULL, 0, NULL);
	SetThreadIdealProcessor(PosteriorPoseThread, 0);
	//start up the TrackGenerator thread
	printf("Starting TrackGenerator thread...\n");
	extern HANDLE TrackGeneratorThread;
	TrackGeneratorThread = NULL;
	TrackGeneratorThread = CreateThread(NULL, 0, TrackGeneratorMain, NULL, 0, NULL);
	SetThreadIdealProcessor(TrackGeneratorThread, 1);

	//******************************************************************************

	printf("\nStarting SceneEstimator...\n\n");

	//***BEGIN EXECUTIVE MAIN LOOP***

	while (true)
	{
		//1. CHECK FOR KEYBOARD SIGNAL TO QUIT
		char SceneEstimatorKeyboard[1024];
		if (gets_s(SceneEstimatorKeyboard, 1024) != NULL)
		{
			if (strcmp(SceneEstimatorKeyboard, "quit") == 0)
			{
				//signal to quit
				break;
			}
			if (strcmp(SceneEstimatorKeyboard, "time") == 0)
			{
				//display the current timestamp
				printf("Current car time is %.12lg.\n", TheCarTime->CurrentCarTime());
			}
		}
	}

	//free memory allocated in PosteriorPose and TrackGenerator
	ShutDownSceneEstimator();

	printf("\nShutting down SceneEstimator...\n\n");

	return 0;
}

void ShutDownSceneEstimator()
{
	/*
	Frees memory allocated in the scene estimator and closes all objects properly.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	//DELETE MEMORY ALLOCATED IN SCENE ESTIMATOR

	printf("\nFreeing memory in SceneEstimator...\n");

	//shut down ppose and tgen threads by sending quit events to their queues
	//Event* TheQuitEvent;
	//double iQuitTime = Q_MAXTIMESTAMP;

	//shut down TrackGenerator
	//TheQuitEvent = new Event();
	//TheQuitEvent->SetEventType(QUIT_EVENT, iQuitTime);
	extern SynchronizedEventQueue* TheTrackGeneratorEventQueue;
	//TheTrackGeneratorEventQueue->PushEvent(TheQuitEvent);
	extern HANDLE TrackGeneratorThread;
	extern HANDLE TheTrackGeneratorQuitEvent;
	printf("Waiting for TrackGenerator thread to terminate...\n");
	SetEvent(TheTrackGeneratorQuitEvent);
	if (WaitForSingleObjectEx(TrackGeneratorThread, PP_THREADTIMEOUT, false) != WAIT_OBJECT_0)
	{
		printf("Warning: TrackGenerator thread not responding, terminating forcefully...\n");
		TerminateThread(TrackGeneratorThread, 0);
	}
	CloseHandle(TrackGeneratorThread);
	printf("TrackGenerator thread terminated successfully.\n");

	//shut down PosteriorPose
	//TheQuitEvent = new Event();
	//TheQuitEvent->SetEventType(QUIT_EVENT, iQuitTime);
	extern SynchronizedEventQueue* ThePosteriorPoseEventQueue;
	//ThePosteriorPoseEventQueue->PushEvent(TheQuitEvent);
	extern HANDLE PosteriorPoseThread;
	extern HANDLE ThePosteriorPoseQuitEvent;
	printf("Waiting for PosteriorPose thread to terminate...\n");
	SetEvent(ThePosteriorPoseQuitEvent);
	if (WaitForSingleObjectEx(PosteriorPoseThread, PP_THREADTIMEOUT, false) != WAIT_OBJECT_0)
	{
		printf("Warning: PosteriorPose thread not responding, terminating forcefully...\n");
		TerminateThread(PosteriorPoseThread, 0);
	}
	CloseHandle(PosteriorPoseThread);
	printf("PosteriorPose thread terminated successfully.\n");

	#ifdef SE_LOGSENSORS
		//clear memory allocated for log sensors
		extern SceneEstimatorLogSensor* TheLogSensor;
		delete TheLogSensor;
		extern TransmitSignaler* TheTransmitSignaler;
		delete TheTransmitSignaler;
	#endif

	#ifdef SE_REALSENSORS
		//clear memory allocated for real sensors
		extern LN200InterfaceReceiver* LN200Client;
		delete LN200Client;

		extern LocalMapInterfaceReceiver* LocalMapClient;
		delete LocalMapClient;

		extern MobilEyeInterfaceReceiver* MobileyeClient;
		delete MobileyeClient;

		extern pose_client* PoseClient;
		PoseClient->unregister_abs_callback();
		PoseClient->unregister_rel_callback();
		delete PoseClient;

		extern RoadFitterInterfaceReceiver* RoadFitterClient;
		delete RoadFitterClient;

		extern StopLineInterfaceReceiver* StoplineClient;
		delete StoplineClient;

		extern TransmitSignaler* TheTransmitSignaler;
		delete TheTransmitSignaler;
	#endif

	#ifdef SE_TRANSMITMSGS
		extern SceneEstimatorInterfaceSender* TheSceneEstimatorSender;
		delete TheSceneEstimatorSender;
		extern SceneEstimatorPublisher* TheSceneEstimatorPublisher;
		delete TheSceneEstimatorPublisher;
	#endif

	printf("Deleting event queues...\n");
	#ifdef SE_PRINTLOGS
		extern SceneEstimatorLogWriter* TheLogWriter;
		delete TheLogWriter;
	#endif
	delete TheTrackGeneratorEventQueue;
	delete ThePosteriorPoseEventQueue;
	extern RelativePoseQueue* TheRelativePoseQueue;
	delete TheRelativePoseQueue;

	printf("Deleting synchronization objects...\n");
	//delete major items in reverse order of creation
	CloseHandle(TheTrackGeneratorQuitEvent);
	CloseHandle(ThePosteriorPoseQuitEvent);
	extern HANDLE ThePosteriorPoseWatchdogEvent;
	CloseHandle(ThePosteriorPoseWatchdogEvent);
	extern HANDLE TheTrackGeneratorWatchdogEvent;
	CloseHandle(TheTrackGeneratorWatchdogEvent);
	extern CarTime* TheCarTime;
	delete TheCarTime;
	extern PosteriorPoseQueue* ThePosteriorPoseQueue;
	delete ThePosteriorPoseQueue;
	extern RoadGraph* TheRoadGraph;
	delete TheRoadGraph;

	return;
}

BOOL WINAPI ConsoleEventHandlerCallback(DWORD dwCtrlType)
{
	/*
	A callback to handle console interrupts and correctly free memory

	INPUTS:
		dwCtrlType - the type of control signal received

	OUTPUTS:
		Always returns TRUE (1)
	*/

	//correctly free memory in LocalMap
	ShutDownSceneEstimator();

	//exit the process
	ExitProcess(2);

	return TRUE;
}
