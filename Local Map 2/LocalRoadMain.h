#ifndef LOCALROADMAIN_H
#define LOCALROADMAIN_H

//general includes
#include "CarTime.h"
#include "Event.h"
#include "EventCodes.h"
#include "Globals.h"
#include "LocalMapConstants.h"
#include "LocalRoad.h"
#include "QueueMonitor.h"
#include "RandomNumberGenerator.h"
#include "RelativePoseQueue.h"
#include "Sensor.h"
#include "SynchronizedEventQueue.h"
#include "VehicleOdometry.h"

#include "localMapInterface/localMapInterface.h"

#include <MATH.H>
#include <STDIO.H>
#include <WINDOWS.H>

//the header for the LocalRoad main loop
DWORD WINAPI LocalRoadMain(LPVOID lpparam);

#endif //LOCALROADMAIN_H
