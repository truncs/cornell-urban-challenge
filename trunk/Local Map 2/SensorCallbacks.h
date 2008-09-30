#ifndef SENSORCALLBACKS_H
#define SENSORCALLBACKS_H

#include "CarTime.h"
#include "Event.h"
#include "EventCodes.h"
#include "EventQueue.h"
#include "Globals.h"
#include "MatrixIndex.h"
#include "RelativePoseQueue.h"
#include "LocalMapConstants.h"
#include "SynchronizedEventQueue.h"

#include "clusteredSickInterface/clusteredSickInterface.h"
#include "delphiInterface/DelphiInterface/delphiInterface.h"
#include "LidarClusterInterface/LidarClusterClient.h"
#include "LN200/ln200.h"
#include "mobileyeInterface/mobileyeInterface.h"
#include "poseinterface/poseclient.h"
#include "roadFitterInterface/roadFitterInterface.h"
#include "roadFitterInterface/RoadFitterOutput.h"
#include "sideSickInterface/sideSickInterface.h"

#include <FLOAT.H>
#include <MATH.H>
#include <MEMORY.H>

//clustered ibeo callback function
void ClusteredIbeoCallback(const vector<LidarCluster> &IbeoMessage, double IbeoTime, void* Arg);
//Clustered SICK callback function
void ClusteredSickCallback(const vector<LidarCluster> &CSMessage, double SickTime, void* Arg);
//delphi radar callback message
void DelphiCallback(DelphiRadarScan DelphiMessage, DelphiInterfaceReceiver* DRX, int ID, void* Arg);
//Jason roadfinder callback function
void JasonRoadCallback(RoadFitterOutput JasonMessage, RoadFitterInterfaceReceiver* JRX, RoadFitterID ID, void* Arg);
//LN200 callback function
void LN200Callback(LN200_data_packet_struct LNMessage, LN200InterfaceReceiver* LRX, void* Arg);
//Mobileye obstacle callback function
void MobileyeObstacleCallback(MobilEyeObstacles MobileyeMessage, MobilEyeInterfaceReceiver* MRX, MobilEyeID ID, void* Arg);
//Mobileye roadfinder callback function
void MobileyeRoadCallback(MobilEyeRoadEnv MobileyeMessage, MobilEyeInterfaceReceiver* MRX, MobilEyeID ID, void* Arg);
//Relative pose (odometry) callback function
void OdometryCallback(const pose_rel_msg& OdomMessage, void* Arg);
//Side SICK callback function
void SideSickCallback(SideSickMsg SSMessage, SideSickReceiver* SSRX, SIDESICKID ID, void* Arg);

#endif //SENSORCALLBACKS_H
