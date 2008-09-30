#ifndef POSTERIORPOSEMAIN_H
#define POSTERIORPOSEMAIN_H

//general includes
#include "CarTime.h"
#include "Event.h"
#include "EventCodes.h"
#include "Globals.h"
#include "PosteriorPoseParticleFilter.h"
#include "PosteriorPoseQueue.h"
#include "QueueMonitor.h"
#include "RandomNumberGenerator.h"
#include "RelativePoseQueue.h"
#include "RoadGraph.h"
#include "SceneEstimatorConstants.h"
#include "SceneEstimatorFunctions.h"
#include "Sensor.h"
#include "SynchronizedEventQueue.h"
#include "TransmitSignaler.h"
#include "VehicleOdometry.h"

//include for interface to AI and Operational
#include "ArbiterMessage.h"
#include "OperationalMessage.h"
#include "sceneEstimatorInterface/sceneEstimatorInterface.h"
#include "SceneEstimatorPublisher.h"

#include <MATH.H>
#include <STDIO.H>
#include <WINDOWS.H>

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

//the ppose main function
DWORD WINAPI PosteriorPoseMain(LPVOID lpparam);

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //POSTERIORPOSEMAIN_H
