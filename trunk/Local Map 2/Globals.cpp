#include "Globals.h"

//***EVENT QUEUES***
//the relative pose queue for storing odometry information
RelativePoseQueue* TheRelativePoseQueue;
//the local map event queue
SynchronizedEventQueue* TheLocalMapEventQueue;
//the local road event queue
SynchronizedEventQueue* TheLocalRoadEventQueue;
//the logging event queue
EventQueue* TheLoggingQueue;

//***HANDLES***
//the LocalMap thread handle
HANDLE LocalMapThread;
//the LocalRoad thread handle
HANDLE LocalRoadThread;

//***LOG WRITER***
//the log writer for LocalMap / LocalRoad
LocalMapLogWriter* TheLogWriter;

//***RANDOM NUMBER GENERATOR***
//the global random number generator used to initialize caches
RandomNumberGenerator TheRandomNumberGenerator;

//***SENSORS***
#ifdef LM_LOGSENSORS
	//the log sensor
	LocalMapLogSensor* TheLogSensor;
#endif
#ifdef LM_REALSENSORS
	//the interface to the clustered horizontal SICK
	ClusteredSickReceiver* ClusteredSickClient;
	//the interface to the Delphis
	DelphiInterfaceReceiver* DelphiClient;
	//the interface to the LN200 (for timing)
	LN200InterfaceReceiver* LN200Client;
	//the interface to the front ibeo
	LidarClusterClient* IbeoClient;
	//the interface to the mobileyes
	MobilEyeInterfaceReceiver* MobileyeClient;
	//the interface to the velodyne occupancy grid
	OccupancyGridInterface* VelodyneGridClient;
	//the interface to pose and odometry
	pose_client* PoseClient;
	//the interface to Jason's road fitter
	RoadFitterInterfaceReceiver* RoadFitterClient;
	//the interface to the side SICKs
	SideSickReceiver* SideSickClient;
#endif
//the transmit signaler
TransmitSignaler* TheTransmitSignaler;

//***SENSOR STRUCTURES***
//the sensor structures used on the car
Sensor ClusteredIbeoSensor;
Sensor CenterIbeoSensor;
Sensor DrivIbeoSensor;
Sensor PassIbeoSensor;

Sensor Back0RadarSensor;
Sensor Front0RadarSensor;
Sensor Driv0RadarSensor;
Sensor Driv1RadarSensor;
Sensor Driv2RadarSensor;
Sensor Driv3RadarSensor;
Sensor Pass0RadarSensor;
Sensor Pass1RadarSensor;
Sensor Pass2RadarSensor;
Sensor Pass3RadarSensor;

Sensor FrontMobileyeObstacleSensor;
Sensor BackMobileyeObstacleSensor;
Sensor FrontMobileyeRoadSensor;
Sensor BackMobileyeRoadSensor;

Sensor FrontJasonSensor;
Sensor BackJasonSensor;

Sensor DrivSideSickSensor;
Sensor PassSideSickSensor;
Sensor BackClusteredSickSensor;

Sensor VelodyneOccupancySensor;
Sensor VelodyneSensor;

//***SYNCHRONIZATION OBJECTS***
//the current car time
CarTime* TheCarTime;
//the signaling event for quitting LocalMap
HANDLE TheLocalMapQuitEvent;
//the signaling event for quitting LocalRoad
HANDLE TheLocalRoadQuitEvent;
//the watchdog event set by LocalMap
HANDLE TheLocalMapWatchdogEvent;
//the watchdog event set bo LocalRoad
HANDLE TheLocalRoadWatchdogEvent;

//***TRANSMIT OBJECTS***
#ifdef LM_TRANSMITMSGS
	//the localmap interface sender
	LocalMapInterfaceSender* TheLocalMapSender;
#endif
