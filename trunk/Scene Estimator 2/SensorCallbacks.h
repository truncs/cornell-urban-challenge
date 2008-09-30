#ifndef SENSORCALLBACKS_H
#define SENSORCALLBACKS_H

#include "CarTime.h"
#include "Event.h"
#include "EventCodes.h"
#include "EventQueue.h"
#include "Globals.h"
#include "MatrixIndex.h"
#include "RelativePoseQueue.h"
#include "SceneEstimatorConstants.h"
#include "SynchronizedEventQueue.h"

#include "LN200/ln200.h"
#include "localMapInterface/localMapInterface.h"
#include "mobileyeInterface/mobileyeInterface.h"
#include "poseinterface/poseclient.h"
#include "roadFitterInterface/roadFitterInterface.h"
#include "roadFitterInterface/RoadFitterOutput.h"
#include "stoplineInterface/stoplineInterface.h"

#include <MATH.H>
#include <MEMORY.H>

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

//Jason roadfinder callback function
void JasonCallback(RoadFitterOutput JasonMessage, RoadFitterInterfaceReceiver* JRX, RoadFitterID ID, void* Arg);
//LN200 callback function
void LN200Callback(LN200_data_packet_struct LNMessage, LN200InterfaceReceiver* LRX, void* Arg);
//LocalMap loose clusters callback function
void LocalMapLooseClustersCallback(LocalMapLooseClustersMsg LMMessage, LocalMapInterfaceReceiver* LMRX, void* Arg);
//LocalMap targets callback function
void LocalMapTargetsCallback(LocalMapTargetsMsg LMMessage, LocalMapInterfaceReceiver* LMRX, void* Arg);
//LocalRoad local road model callback function
void LocalRoadCallback(LocalRoadModelEstimateMsg LRMessage, LocalMapInterfaceReceiver* LMRX, void* Arg);
//Mobileye callback function
void MobileyeCallback(MobilEyeRoadEnv MobileyeMessage, MobilEyeInterfaceReceiver* MRX, MobilEyeID ID, void* Arg);
//Relative pose (odometry) callback function
void OdometryCallback(const pose_rel_msg& OdomMessage, void* Arg);
//Absolute pose (regular pose) callback function
void PoseCallback(const pose_abs_msg& PoseMessage, void* Arg);
//Stopline callback function
void StoplineCallback(StopLineMessage SLMessage, StopLineInterfaceReceiver* SRX, void* Arg);

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //SENSORCALLBACKS_H
