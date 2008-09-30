#ifndef GLOBALS_H
#define GLOBALS_H

//general includes
#include "CarTime.h"
#include "EventQueue.h"
#include "PosteriorPoseQueue.h"
#include "RandomNumberGenerator.h"
#include "RelativePoseQueue.h"
#include "RoadGraph.h"
#include "Sensor.h"
#include "SceneEstimatorConstants.h"
#include "SynchronizedEventQueue.h"
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

//includes for scene estimator interface to AI
#include "sceneEstimatorInterface/sceneEstimatorInterface.h"
#include "SceneEstimatorPublisher.h"

#include <WINDOWS.H>

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

//A set of global variables and allocations for the scene estimator

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //GLOBALS_H