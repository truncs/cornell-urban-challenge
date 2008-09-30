#include "Globals.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

//***RANDOM NUMBER GENERATOR***
RandomNumberGenerator TheRandomNumberGenerator;

//***EVENT QUEUES***
//the relative pose queue for storing odometry information
RelativePoseQueue* TheRelativePoseQueue;
//the PosteriorPose event queue
SynchronizedEventQueue* ThePosteriorPoseEventQueue;
//the TrackGenerator event queue
SynchronizedEventQueue* TheTrackGeneratorEventQueue;
//the logging event queue
EventQueue* TheLoggingQueue;

//***HANDLES***
//PosteriorPose thread handle
HANDLE PosteriorPoseThread;
//TrackGenerator thread handle
HANDLE TrackGeneratorThread;

//***LOG WRITER***
#ifdef SE_PRINTLOGS
	//the log writer, for taking logs
	SceneEstimatorLogWriter* TheLogWriter;
#endif

//***RNDF ROAD GRAPH STRUCTURE***
//the road graph used throughout the whole scene estimator
RoadGraph* TheRoadGraph;

//***SCENE ESTIMATOR SYNCHRONIZATION OBJECTS***
//the posterior pose solution queue
PosteriorPoseQueue* ThePosteriorPoseQueue;
//the current car time
CarTime* TheCarTime;
//the signaling event for quitting PosteriorPose
HANDLE ThePosteriorPoseQuitEvent;
//the signaling event for quitting TrackGenerator
HANDLE TheTrackGeneratorQuitEvent;
//the watchdog event for PosteriorPose
HANDLE ThePosteriorPoseWatchdogEvent;
//the watchdog event for TrackGenerator
HANDLE TheTrackGeneratorWatchdogEvent;

//***SENSORS***
#ifdef SE_LOGSENSORS
	//the log sensor for reading logs
	SceneEstimatorLogSensor* TheLogSensor;
#endif
#ifdef SE_REALSENSORS
	//the interface to the LN200 (for timing)
	LN200InterfaceReceiver* LN200Client;
	//the interface to the LocalMap
	LocalMapInterfaceReceiver* LocalMapClient;
	//the interface to the mobileyes
	MobilEyeInterfaceReceiver* MobileyeClient;
	//the interface to pose and odometry
	pose_client* PoseClient;
	//the interface to Jason's road fitter
	RoadFitterInterfaceReceiver* RoadFitterClient;
	//the interface to the stopline camera
	StopLineInterfaceReceiver* StoplineClient;
#endif
//the transmit signaler
TransmitSignaler* TheTransmitSignaler;

//***SENSOR STRUCTURES***
Sensor BackSickSensor;
Sensor CenterIbeoSensor;
Sensor FrontJasonSensor;
Sensor FrontMobileyeSensor;
Sensor NullSensor;
Sensor PoseSensor;
Sensor StoplineSensor;

//***TRANSMIT INTERFACES***
#ifdef SE_TRANSMITMSGS
	//the AI publisher (managed code)
	SceneEstimatorPublisher* TheSceneEstimatorPublisher;
	//the operational/AI interface
	SceneEstimatorInterfaceSender* TheSceneEstimatorSender;
#endif
