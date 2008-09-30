// Main.cpp : Defines the entry point for the console application.
//

//general includes
#include "CarTime.h"
#include "Event.h"
#include "EventCodes.h"
#include "Globals.h"
#include "LocalMapConstants.h"
#include "LocalMapMain.h"
#include "LocalRoadMain.h"
#include "RelativePoseQueue.h"
#include "Sensor.h"
#include "SynchronizedEventQueue.h"
#include "time/timing.h"
#include "TransmitSignaler.h"

//includes for interface with log sensors
#include "LocalMapLogSensor.h"
#include "LocalMapLogWriter.h"

//includes for interface with real sensors
#include "clusteredSickInterface/clusteredSickInterface.h"
#include "delphiInterface/DelphiInterface/delphiInterface.h"
#include "LidarClusterInterface/LidarClusterClient.h"
#include "LN200/ln200.h"
#include "mobileyeInterface/mobileyeInterface.h"
#include "OccupancyGrid/OccupancyGridInterface.h"
#include "poseinterface/poseclient.h"
#include "roadFitterInterface/roadFitterInterface.h"
#include "sideSickInterface/sideSickInterface.h"
#include "SensorCallbacks.h"
//#include "Velodyne/VelodyneTahoeCalibration.h"

#include "localMapInterface/localMapInterface.h"

#include <STDIO.H>
#include <WINDOWS.H>

//the shutting down function
void ShutDownLocalMap();
//the callback to handle the application close event
BOOL WINAPI ConsoleEventHandlerCallback(DWORD dwCtrlType);

int main(int argc, char** argv)
{
	//initialize the LocalMap
	LocalMapScreenInit();

	//set LocalMap to receive application close events
	SetConsoleCtrlHandler(ConsoleEventHandlerCallback, TRUE);

	#ifdef LM_PRINTLOGS
		//***CREATE THE LOG WRITER***
		extern LocalMapLogWriter* TheLogWriter;
		if (argc > 1)
		{
			//create the log writer with an input directory
			TheLogWriter = new LocalMapLogWriter(argv[1]);
		}
		else
		{
			TheLogWriter = new LocalMapLogWriter();
		}
		if (TheLogWriter->IsRunning() == false)
		{
			printf("LocalMap will now terminate because log writer failed to initialize.\n");
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

	printf("Initializing event queues...\n");
	//***DECLARE THE RELATIVE POSE QUEUE***
	extern RelativePoseQueue* TheRelativePoseQueue;
	TheRelativePoseQueue = new RelativePoseQueue(LM_RPPQUEUESIZE, 
		ODOM_QWX, ODOM_QWY, ODOM_QWZ, ODOM_QVX, ODOM_QVY, ODOM_QVZ);

	//***DECLARE THE LOCAL MAP EVENT QUEUE***
	//TheLocalMapEventQueue is the (global) event queue for LocalMap
	extern SynchronizedEventQueue* TheLocalMapEventQueue;
	TheLocalMapEventQueue = new SynchronizedEventQueue(LM_QUEUEDELAY);

	//***DECLARE THE LOCAL ROAD EVENT QUEUE***
	//TheLocalRoadEventQueue is the (global) event queue for LocalRoad
	extern SynchronizedEventQueue* TheLocalRoadEventQueue;
	TheLocalRoadEventQueue = new SynchronizedEventQueue(LR_QUEUEDELAY);

	//***DEFINE THE SENSOR POSITIONS ON THE VEHICLE***
	printf("Loading sensor configuration...\n");

	//clustered Ibeo (Tahoe) NOTE: sergei puts all ibeo points in ego vehicle coordinates
	extern Sensor ClusteredIbeoSensor;
	ClusteredIbeoSensor.SensorID = IBEO_EVENT;
	ClusteredIbeoSensor.SensorX = 0.0;
	ClusteredIbeoSensor.SensorY = 0.0;
	ClusteredIbeoSensor.SensorZ = 0.0;
	ClusteredIbeoSensor.SensorYaw =  0.0;
	ClusteredIbeoSensor.SensorPitch = 0.0;
	ClusteredIbeoSensor.SensorRoll = 0.0;

	//center IBEO (Tahoe)
	extern Sensor CenterIbeoSensor;
	CenterIbeoSensor.SensorID = IBEOCENTERRAW_EVENT;
	CenterIbeoSensor.SensorX = 3.6449;
	CenterIbeoSensor.SensorY = 0.0;
	CenterIbeoSensor.SensorZ = -0.0508;
	CenterIbeoSensor.SensorYaw =  0.0;
	CenterIbeoSensor.SensorPitch = 0.0;
	CenterIbeoSensor.SensorRoll = 0.0;
	//drivIBEO (Tahoe)
	extern Sensor DrivIbeoSensor;
	DrivIbeoSensor.SensorID = IBEODRIVRAW_EVENT;
	DrivIbeoSensor.SensorX = 3.5306;
	DrivIbeoSensor.SensorY = 0.8;
	DrivIbeoSensor.SensorZ =  -0.5118;
	DrivIbeoSensor.SensorYaw =  0.646644487863899;
	DrivIbeoSensor.SensorPitch = -0.0191986217719376;
	DrivIbeoSensor.SensorRoll = -0.015707963267949;
	//passIBEO (Tahoe)
	extern Sensor PassIbeoSensor;
	PassIbeoSensor.SensorID = IBEOPASSRAW_EVENT;
	PassIbeoSensor.SensorX = 3.5306;
	PassIbeoSensor.SensorY = -0.8;
	PassIbeoSensor.SensorZ =  -0.5118;
	PassIbeoSensor.SensorYaw =  -0.724311639577647;
	PassIbeoSensor.SensorPitch = -0.0122173047639603;
	PassIbeoSensor.SensorRoll = 0.0174532925199433;

	//back0 radar (Tahoe)
	extern Sensor Back0RadarSensor;
	Back0RadarSensor.SensorID = BACK0RADAR_EVENT;
	Back0RadarSensor.SensorX = -1.0541;
	Back0RadarSensor.SensorY = 0.2286;
	Back0RadarSensor.SensorZ = -0.4318;
	Back0RadarSensor.SensorYaw = PI;
	Back0RadarSensor.SensorPitch = 0.0;
	Back0RadarSensor.SensorRoll = PI;
	//front0 radar (Tahoe)
	extern Sensor Front0RadarSensor;
	Front0RadarSensor.SensorID = FRONT0RADAR_EVENT;
	Front0RadarSensor.SensorX = 3.5687;
	Front0RadarSensor.SensorY = 0.4572;
	Front0RadarSensor.SensorZ = -0.1016;
	Front0RadarSensor.SensorYaw = 1.0/180.0*PI;
	Front0RadarSensor.SensorPitch = 0.0;
	Front0RadarSensor.SensorRoll = 0.0;

	//driv0 radar (Tahoe)
	extern Sensor Driv0RadarSensor;
	Driv0RadarSensor.SensorID = DRIV0RADAR_EVENT;
	Driv0RadarSensor.SensorX = 3.6449;
	Driv0RadarSensor.SensorY = 0.6477;
	Driv0RadarSensor.SensorZ =  -0.1651;
	Driv0RadarSensor.SensorYaw = 80.5/180.0*PI;
	Driv0RadarSensor.SensorPitch = 0.0;
	Driv0RadarSensor.SensorRoll = 3.14159265358979;
	//driv1 radar (Tahoe)
	extern Sensor Driv1RadarSensor;
	Driv1RadarSensor.SensorID = DRIV1RADAR_EVENT;
	Driv1RadarSensor.SensorX = 3.4417;
	Driv1RadarSensor.SensorY = 0.9398;
	Driv1RadarSensor.SensorZ =  -0.200025;
	Driv1RadarSensor.SensorYaw =  107.5/180.0*PI;
	Driv1RadarSensor.SensorPitch = 0.0;
	Driv1RadarSensor.SensorRoll = 3.14159265358979;
	//driv2 radar (Tahoe)
	extern Sensor Driv2RadarSensor;
	Driv2RadarSensor.SensorID = DRIV2RADAR_EVENT;
	Driv2RadarSensor.SensorX = 0.0;
	Driv2RadarSensor.SensorY = 0.0;
	Driv2RadarSensor.SensorZ =  0.0;
	Driv2RadarSensor.SensorYaw =  0.0;
	Driv2RadarSensor.SensorPitch = 0.0;
	Driv2RadarSensor.SensorRoll = 0.0;
	//driv3 radar (Tahoe)
	extern Sensor Driv3RadarSensor;
	Driv3RadarSensor.SensorID = DRIV3RADAR_EVENT;
	Driv3RadarSensor.SensorX = -1.2700;
	Driv3RadarSensor.SensorY = 0.7366;
	Driv3RadarSensor.SensorZ =  -0.2794;
	Driv3RadarSensor.SensorYaw = 169.0/180.0*PI;
	Driv3RadarSensor.SensorPitch = 0.0;
	Driv3RadarSensor.SensorRoll = PI;

	//pass0 radar (Tahoe)
	extern Sensor Pass0RadarSensor;
	Pass0RadarSensor.SensorID = PASS0RADAR_EVENT;
	Pass0RadarSensor.SensorX = 3.6449;
	Pass0RadarSensor.SensorY = -0.6477;
	Pass0RadarSensor.SensorZ =  -0.1651;
	Pass0RadarSensor.SensorYaw =  -84.5/180.0*PI;
	Pass0RadarSensor.SensorPitch = 0.0;
	Pass0RadarSensor.SensorRoll = 0.0;
	//pass1 radar (Tahoe)
	extern Sensor Pass1RadarSensor;
	Pass1RadarSensor.SensorID = PASS1RADAR_EVENT;
	Pass1RadarSensor.SensorX = 3.4417;
	Pass1RadarSensor.SensorY = -0.9398;
	Pass1RadarSensor.SensorZ =  -0.200025;
	Pass1RadarSensor.SensorYaw =  -107.5/180.0*PI;
	Pass1RadarSensor.SensorPitch = 0.0;
	Pass1RadarSensor.SensorRoll = 0.0;
	//pass2 radar (Tahoe)
	extern Sensor Pass2RadarSensor;
	Pass2RadarSensor.SensorID = PASS2RADAR_EVENT;
	Pass2RadarSensor.SensorX = 0.0;
	Pass2RadarSensor.SensorY = 0.0;
	Pass2RadarSensor.SensorZ =  0.0;
	Pass2RadarSensor.SensorYaw =  0.0;
	Pass2RadarSensor.SensorPitch = 0.0;
	Pass2RadarSensor.SensorRoll = 0.0;
	//pass3 radar (Tahoe)
	extern Sensor Pass3RadarSensor;
	Pass3RadarSensor.SensorID = PASS3RADAR_EVENT;
	Pass3RadarSensor.SensorX = -1.2700;
	Pass3RadarSensor.SensorY = -0.7366;
	Pass3RadarSensor.SensorZ = -0.2794;
	Pass3RadarSensor.SensorYaw = -169.0/180.0*PI;
	Pass3RadarSensor.SensorPitch = 0.0;
	Pass3RadarSensor.SensorRoll = 0.0;

	//front Jason camera (Tahoe)
	extern Sensor FrontJasonSensor;
	FrontJasonSensor.SensorID = FRONTJASONROAD_EVENT;
	FrontJasonSensor.SensorX = 1.7272;
	FrontJasonSensor.SensorY = 0.0;
	FrontJasonSensor.SensorZ = 0.9017;
	FrontJasonSensor.SensorYaw = 0.0;
	FrontJasonSensor.SensorPitch = 8.25/180.0*PI;
	FrontJasonSensor.SensorRoll = 0.0;
	//back Jason camera (Tahoe)
	extern Sensor BackJasonSensor;
	BackJasonSensor.SensorID = BACKJASONROAD_EVENT;
	BackJasonSensor.SensorX = -0.7049;
	BackJasonSensor.SensorY = 0.2203;
	BackJasonSensor.SensorZ = 1.1256;
	BackJasonSensor.SensorYaw = PI;
	BackJasonSensor.SensorPitch = 0.0519532065609139;
	BackJasonSensor.SensorRoll = 0.0;

	//front Mobileye road (Tahoe)
	extern Sensor FrontMobileyeRoadSensor;
	FrontMobileyeRoadSensor.SensorID = FRONTMOBILEYEROAD_EVENT;
	FrontMobileyeRoadSensor.SensorX = 1.7272;
	FrontMobileyeRoadSensor.SensorY = 0.0;
	FrontMobileyeRoadSensor.SensorZ = 0.9017;
	FrontMobileyeRoadSensor.SensorYaw = 0.0;
	//FrontMobileyeRoadSensor.SensorYaw = 0.23/180.0*PI;
	FrontMobileyeRoadSensor.SensorPitch = 8.25/180.0*PI;
	FrontMobileyeRoadSensor.SensorRoll = 0.0;
	//front Mobileye obstacle (Tahoe)
	extern Sensor FrontMobileyeObstacleSensor;
	FrontMobileyeObstacleSensor.SensorID = FRONTMOBILEYEOBSTACLE_EVENT;
	FrontMobileyeObstacleSensor.SensorX = 1.7272;
	FrontMobileyeObstacleSensor.SensorY = 0.0;
	FrontMobileyeObstacleSensor.SensorZ = 0.9017;
	//FrontMobileyeObstacleSensor.SensorYaw = 0.23/180.0*PI;
	FrontMobileyeObstacleSensor.SensorYaw = 0.0;
	FrontMobileyeObstacleSensor.SensorPitch = 8.25/180.0*PI;
	FrontMobileyeObstacleSensor.SensorRoll = 0.0;
	//back Mobileye road (Tahoe)
	extern Sensor BackMobileyeRoadSensor;
	BackMobileyeRoadSensor.SensorID = BACKMOBILEYEROAD_EVENT;
	BackMobileyeRoadSensor.SensorX = -0.7049;
	BackMobileyeRoadSensor.SensorY = 0.2203;
	BackMobileyeRoadSensor.SensorZ = 1.1256;
	BackMobileyeRoadSensor.SensorYaw = PI;
	BackMobileyeRoadSensor.SensorPitch = 0.0519532065609139;
	BackMobileyeRoadSensor.SensorRoll = 0.0;
	//back Mobileye obstacle (Tahoe)
	extern Sensor BackMobileyeObstacleSensor;
	BackMobileyeObstacleSensor.SensorID = BACKMOBILEYEOBSTACLE_EVENT;
	BackMobileyeObstacleSensor.SensorX = -0.7049;
	BackMobileyeObstacleSensor.SensorY = 0.2203;
	BackMobileyeObstacleSensor.SensorZ = 1.1256;
	BackMobileyeObstacleSensor.SensorYaw = UnwrapAngle(PI + 0.0151988295595802);
	BackMobileyeObstacleSensor.SensorPitch = 0.0519532065609139;
	BackMobileyeObstacleSensor.SensorRoll = 0.0;

	//back and side SICKs (Tahoe)
	extern Sensor DrivSideSickSensor;
	DrivSideSickSensor.SensorID = SIDESICKDRIV_EVENT;
	DrivSideSickSensor.SensorX = 0.6604;
	DrivSideSickSensor.SensorY = 0.6096;
	DrivSideSickSensor.SensorZ = 0.8618;
	DrivSideSickSensor.SensorYaw = 1.5707963267949;
	DrivSideSickSensor.SensorPitch = 0.0;
	DrivSideSickSensor.SensorRoll = 0.0;
	extern Sensor PassSideSickSensor;
	PassSideSickSensor.SensorID = SIDESICKPASS_EVENT;
	PassSideSickSensor.SensorX = 0.6604;
	PassSideSickSensor.SensorY = -0.6096;
	PassSideSickSensor.SensorZ = 0.8618;
	PassSideSickSensor.SensorYaw = -1.5707963267949;
	PassSideSickSensor.SensorPitch = 0.0;
	PassSideSickSensor.SensorRoll = 0.0;
	extern Sensor BackClusteredSickSensor;
	BackClusteredSickSensor.SensorID = BACKCLUSTEREDSICK_EVENT;
	BackClusteredSickSensor.SensorX = -1.1049;
	BackClusteredSickSensor.SensorY = 0.0;
	BackClusteredSickSensor.SensorZ = -0.4699;
	BackClusteredSickSensor.SensorYaw = 3.14159265358979;
	BackClusteredSickSensor.SensorPitch = 0.0;
	BackClusteredSickSensor.SensorRoll = 0.0;

	//Velodyne occupancy grid sensor
	//NOTE: this sensor operates in ego-vehicle coordinates
	extern Sensor VelodyneOccupancySensor;
	VelodyneOccupancySensor.SensorID = VELODYNE_EVENT;
	VelodyneOccupancySensor.SensorX = 0.0;
	VelodyneOccupancySensor.SensorY = 0.0;
	VelodyneOccupancySensor.SensorZ = 0.0;
	VelodyneOccupancySensor.SensorYaw = 0.0;
	VelodyneOccupancySensor.SensorPitch = 0.0;
	VelodyneOccupancySensor.SensorRoll = 0.0;
	//Raw velodyne sensor
	extern Sensor VelodyneSensor;
	//VelodyneSensor = VELODYNE_CALIBRATION::CURRENT::SensorCalibration();
	VelodyneSensor.SensorID = VELODYNE_EVENT;
	VelodyneSensor.SensorX = 1.27;
	VelodyneSensor.SensorY = 0.0;
	VelodyneSensor.SensorZ = 1.1176;
	VelodyneSensor.SensorYaw = 0.0;
	VelodyneSensor.SensorPitch = -2.5/180.0*PI;
	VelodyneSensor.SensorRoll = 0.35/180.0*PI;

	//***CREATE SYNCHRONIZATION OBJECTS***
	printf("Creating synchronization objects...\n");
	//the car timestamping object
	extern CarTime* TheCarTime;
	TheCarTime = new CarTime();
	TheCarTime->SetCurrentCarTime(Q_MINTIMESTAMP);
	//the LocalMap quit signaling event
	extern HANDLE TheLocalMapQuitEvent;
	TheLocalMapQuitEvent = CreateEventA(NULL, false, false, "TheLocalMapQuitEvent");
	//the LocalRoad quit signaling event
	extern HANDLE TheLocalRoadQuitEvent;
	TheLocalRoadQuitEvent = CreateEventA(NULL, false, false, "TheLocalRoadQuitEvent");
	//the LocalMap watchdog event
	extern HANDLE TheLocalMapWatchdogEvent;
	TheLocalMapWatchdogEvent = CreateEventA(NULL, false, false, "TheLocalMapWatchdogEvent");
	//the LocalRoad watchdog event
	extern HANDLE TheLocalRoadWatchdogEvent;
	TheLocalRoadWatchdogEvent = CreateEventA(NULL, false, false, "TheLocalRoadWatchdogEvent");

	//***CREATE SENSOR INTERFACES***
	#ifdef LM_LOGSENSORS
		//***LOG FILES AND FAKE SENSORS***

		//full set of logs
		printf("Initializing log sensors...\n");
		extern LocalMapLogSensor* TheLogSensor;
		TheLogSensor = new LocalMapLogSensor(10, "odom.txt", "frontmobileyeobstacle.txt", "frontmobileyeroad.txt", 
			"backmobileyeobstacle.txt", "backmobileyeroad.txt", "frontjasonroad.txt", "backjasonroad.txt", 
			"ibeo.txt", "backclusteredsick.txt", "drivsidesick.txt", "passsidesick.txt",
			"front0radar.txt", "driv0radar.txt", "driv1radar.txt", "driv2radar.txt", "driv3radar.txt", 
			"pass0radar.txt", "pass1radar.txt", "pass2radar.txt", "pass3radar.txt");

		//transmit signaler for displaying messages
		printf("Initializing transmit signaler...\n");
		extern TransmitSignaler* TheTransmitSignaler;
		TheTransmitSignaler = new TransmitSignaler(100);
	#endif

	#ifdef LM_REALSENSORS
		//***REAL SENSORS***

		//the interface to the clustered horizontal SICK
		extern ClusteredSickReceiver* ClusteredSickClient;
		ClusteredSickClient = NULL;
		//the interface to the Delphis
		extern DelphiInterfaceReceiver* DelphiClient;
		DelphiClient = NULL;
		//the interface to the LN200 (for timing)
		extern LN200InterfaceReceiver* LN200Client;
		LN200Client = NULL;
		//the interface to the front ibeo
		extern LidarClusterClient* IbeoClient;
		IbeoClient = NULL;
		//the interface to the mobileyes
		extern MobilEyeInterfaceReceiver* MobileyeClient;
		MobileyeClient = NULL;
		//the velodyne occupancy grid client
		extern OccupancyGridInterface* VelodyneGridClient;
		//the interface to pose and odometry
		extern pose_client* PoseClient;
		PoseClient = NULL;
		//the interface to Jason's road fitter
		extern RoadFitterInterfaceReceiver* RoadFitterClient;
		RoadFitterClient = NULL;
		//the interface to the side SICKs
		extern SideSickReceiver* SideSickClient;
		SideSickClient = NULL;
		//the signaler that will say when it's time to transmit
		extern TransmitSignaler* TheTransmitSignaler;
		TheTransmitSignaler = NULL;

		//initialize the LN200 timing
		printf("Initializing LN200...\n");
		LN200Client = new LN200InterfaceReceiver();
		LN200Client->SetDTCallback(&LN200Callback, NULL, LM_TIMINGPERIOD);

		//initialize odometry
		printf("Initializing odometry...\n");
		PoseClient = new pose_client();
		PoseClient->register_rel_callback(OdometryCallback, NULL);

		//initialize ibeo
		printf("Initializing clustered ibeos...\n");
		IbeoClient = new LidarClusterClient();
		IbeoClient->SetCallback(&ClusteredIbeoCallback, NULL);

		//initialize delphis
		printf("Initializing delphis...\n");
		DelphiClient = new DelphiInterfaceReceiver();
		DelphiClient->SetDelphiCallback(&DelphiCallback, NULL);

		//initialize front mobileye obstacle
		printf("Initializing mobileye obstacles and road environment...\n");
		MobileyeClient = new MobilEyeInterfaceReceiver();
		MobileyeClient->SetObstacleCallback(&MobileyeObstacleCallback, NULL);
		MobileyeClient->SetRoadEnvInfoCallback(&MobileyeRoadCallback, NULL);

		//initialize rear horizontal SICK
		printf("Initializing clustered horizontal sick...\n");
		ClusteredSickClient = new ClusteredSickReceiver();
		ClusteredSickClient->SetCallback(&ClusteredSickCallback, NULL);

		//initialize front jason road finder
		printf("Initializing front jason roadfinder...\n");
		RoadFitterClient = new RoadFitterInterfaceReceiver();
		RoadFitterClient->SetRoadFitterOutputCallback(&JasonRoadCallback, NULL);

		//initialize side sicks
		printf("Initializing side sicks...\n");
		SideSickClient = new SideSickReceiver();
		SideSickClient->SetCallback(&SideSickCallback, NULL);

		//initialize velodyne occupancy grid
		printf("Initializing velodyne...\n");
		VelodyneGridClient = new OccupancyGridInterface();

		//create and initialize the communications signaler
		printf("Initializing transmit signaler...\n");
		TheTransmitSignaler = new TransmitSignaler(100);
	#endif

	//***DECLARE THE LOCALMAP MESSAGE SENDER***
	#ifdef LM_TRANSMITMSGS
		extern LocalMapInterfaceSender* TheLocalMapSender;
		TheLocalMapSender = new LocalMapInterfaceSender();
	#endif

	//start up the LocalMap thread
	printf("Starting LocalMap thread...\n");
	extern HANDLE LocalMapThread;
	LocalMapThread = NULL;
	LocalMapThread = CreateThread(NULL, 0, LocalMapMain, NULL, 0, NULL);
	SetThreadIdealProcessor(LocalMapThread, 0);
	//start up the LocalRoad thread
	printf("Starting LocalRoad thread...\n");
	extern HANDLE LocalRoadThread;
	LocalRoadThread = NULL;
	LocalRoadThread = CreateThread(NULL, 0, LocalRoadMain, NULL, 0, NULL);
	SetThreadIdealProcessor(LocalRoadThread, 1);

	//******************************************************************************

	printf("\nStarting LocalMap and LocalRoad...\n\n");

	//***BEGIN EXECUTIVE MAIN LOOP***

	while (true)
	{
		//1. CHECK FOR KEYBOARD SIGNAL TO QUIT
		char LocalMapKeyboard[1024];
		
		if (gets_s(LocalMapKeyboard, 1024) != NULL)
		{
			if (strcmp(LocalMapKeyboard, "quit") == 0)
			{
				//signal to quit
				break;
			}
			if (strcmp(LocalMapKeyboard, "time") == 0)
			{
				//display the current timestamp
				printf("Current car time is %.12lg.\n", TheCarTime->CurrentCarTime());
			}
		}
	}

	//free all memory allocated in LocalMap and LocalRoad
	ShutDownLocalMap();

	printf("\nShutting down LocalMap...\n\n");

	return 0;
}

void ShutDownLocalMap()
{
	/*
	Shuts down LocalMap and LocalRoad and frees memory and objects correctly.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	//DELETE MEMORY ALLOCATED IN LOCALMAP

	printf("\nFreeing memory in LocalMap and LocalRoad...\n");

	//shut down localmap and localroad threads by sending quit events to their queues
	//Event* TheQuitEvent;
	//double iQuitTime = Q_MAXTIMESTAMP;

	//shut down LocalMap
	//TheQuitEvent = new Event();
	//TheQuitEvent->SetEventType(QUIT_EVENT, iQuitTime);
	extern SynchronizedEventQueue* TheLocalMapEventQueue;
	//TheLocalMapEventQueue->PushEvent(TheQuitEvent);
	printf("Waiting for LocalMap thread to terminate...\n");
	extern HANDLE LocalMapThread;
	extern HANDLE TheLocalMapQuitEvent;
	SetEvent(TheLocalMapQuitEvent);
	if (WaitForSingleObjectEx(LocalMapThread, LM_THREADTIMEOUT, false) != WAIT_OBJECT_0)
	{
		printf("Warning: LocalMap thread not responding, terminating forcefully...\n");
		TerminateThread(LocalMapThread, 0);
	}
	CloseHandle(LocalMapThread);
	printf("LocalMap thread terminated successfully.\n");

	//shut down LocalRoad
	//TheQuitEvent = new Event();
	//TheQuitEvent->SetEventType(QUIT_EVENT, iQuitTime);
	extern SynchronizedEventQueue* TheLocalRoadEventQueue;
	//TheLocalRoadEventQueue->PushEvent(TheQuitEvent);
	printf("Waiting for LocalRoad thread to terminate...\n");
	extern HANDLE LocalRoadThread;
	extern HANDLE TheLocalRoadQuitEvent;
	SetEvent(TheLocalRoadQuitEvent);
	if (WaitForSingleObjectEx(LocalRoadThread, LM_THREADTIMEOUT, false) != WAIT_OBJECT_0)
	{
		printf("Warning: LocalRoad thread not responding, terminating forcefully...\n");
		TerminateThread(LocalRoadThread, 0);
	}
	CloseHandle(LocalRoadThread);
	printf("LocalRoad thread terminated successfully.\n");

	#ifdef LM_TRANSMITMSGS
		extern LocalMapInterfaceSender* TheLocalMapSender;
		delete TheLocalMapSender;
	#endif

	printf("Shutting down sensors...\n");
	#ifdef LM_LOGSENSORS
		//clear memory allocated for log sensors
		extern LocalMapLogSensor* TheLogSensor;
		delete TheLogSensor;
		extern TransmitSignaler* TheTransmitSignaler;
		delete TheTransmitSignaler;
	#endif

	#ifdef LM_REALSENSORS
		//clear memory allocated for real sensors

		extern DelphiInterfaceReceiver* DelphiClient;
		delete DelphiClient;

		extern LidarClusterClient* IbeoClient;
		delete IbeoClient;

		extern LN200InterfaceReceiver* LN200Client;
		delete LN200Client;

		extern MobilEyeInterfaceReceiver* MobileyeClient;
		delete MobileyeClient;

		extern pose_client* PoseClient;
		PoseClient->unregister_rel_callback();
		delete PoseClient;

		extern ClusteredSickReceiver* ClusteredSickClient;
		delete ClusteredSickClient;

		extern RoadFitterInterfaceReceiver* RoadFitterClient;
		delete RoadFitterClient;

		extern SideSickReceiver* SideSickClient;
		delete SideSickClient;

		extern OccupancyGridInterface* VelodyneGridClient;
		delete VelodyneGridClient;

		extern TransmitSignaler* TheTransmitSignaler;
		delete TheTransmitSignaler;
	#endif

	printf("Deleting event queues...\n");
	#ifdef LM_PRINTLOGS
		extern LocalMapLogWriter* TheLogWriter;
		delete TheLogWriter;
	#endif
	delete TheLocalRoadEventQueue;
	delete TheLocalMapEventQueue;
	extern RelativePoseQueue* TheRelativePoseQueue;
	delete TheRelativePoseQueue;

	printf("Deleting synchronization objects...\n");
	//delete major items in reverse order of creation
	extern HANDLE TheLocalMapWatchdogEvent;
	CloseHandle(TheLocalMapWatchdogEvent);
	extern HANDLE TheLocalRoadWatchdogEvent;
	CloseHandle(TheLocalRoadWatchdogEvent);
	CloseHandle(TheLocalRoadQuitEvent);
	CloseHandle(TheLocalMapQuitEvent);
	extern CarTime* TheCarTime;
	delete TheCarTime;

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
	ShutDownLocalMap();

	//exit the process
	ExitProcess(2);

	return TRUE;
}
