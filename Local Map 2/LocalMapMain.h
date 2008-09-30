#ifndef LOCALMAPMAIN_H
#define LOCALMAPMAIN_H

//general includes
#include "CarTime.h"
#include "Event.h"
#include "EventCodes.h"
#include "Globals.h"
#include "LocalMap.h"
#include "LocalMapConstants.h"
#include "QueueMonitor.h"
#include "RandomNumberGenerator.h"
#include "RelativePoseQueue.h"
#include "Sensor.h"
#include "SynchronizedEventQueue.h"
#include "time/timing.h"
#include "VehicleOdometry.h"

#include "localMapInterface/localMapInterface.h"
#include "OccupancyGrid/OccupancyGridInterface.h"

#include <MATH.H>
#include <STDIO.H>
#include <WINDOWS.H>

//the local map main function
DWORD WINAPI LocalMapMain(LPVOID lpparam);

#endif //LOCALMAPMAIN_H
