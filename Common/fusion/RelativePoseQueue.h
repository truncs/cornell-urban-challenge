#ifndef RELATIVEPOSEQUEUE_H
#define RELATIVEPOSEQUEUE_H

#include "EventCodes.h"
#include "MatrixIndex.h"
#include "VehicleOdometry.h"
#include "VehicleTransformation.h"
#include "../PoseInterface/Pose_Message.h"

#include <FLOAT.H>
#include <MATH.H>
#include <STDIO.H>
#include <WINDOWS.H>

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

//number of doubles in the relative pose packet
#define RPQ_PACKETSIZE 14
//maximum allowed offset (sec.) between relative pose packets and desired timestamp
#define RPQ_MAXPACKETOFFSET 0.2
//maximum number of bad events tolerated before the relative pose queue resets
#define RPQ_NUMEVENTSB4RESET 100

//relative pose queue packet pulling behavior
#define RPQ_PULLPACKETBEFORETS 60
#define RPQ_PULLPACKETAFTERTS 61
#define RPQ_PULLPACKETCLOSESTTS 62

class RelativePoseQueue
{
	//The relative pose queue class.  Stores a rolling queue of relative pose
	//packets to supply transformations between two arbitrary timestamps.

private:

	//the size of the queue
	int mQueueSize;
	//number of packets currently stored
	int mNumPacketsInQueue;
	//the tail (last element added) of the queue
	int mQueueTail;
	//the queue storing relative pose packets
	double* mRPPQueue;

	//a counter for events that are iqnored due to their timestamp being older than the most recent timestamp
	int mNumOldEventsIgnored;

	//white noise variances on relative pose rates
	double mWXVar;
	double mWYVar;
	double mWZVar;
	double mVXVar;
	double mVYVar;
	double mVZVar;

	CRITICAL_SECTION mQueueLock;

	bool PullPacket(double oPacket[RPQ_PACKETSIZE], double iEventTime, int iQueryMode);

public:

	RelativePoseQueue(int iQueueSize, double iWXVar, double iWYVar, double iWZVar, double iVXVar, double iVYVar, double iVZVar);
	~RelativePoseQueue(void);	

	void GetVehicleOdometry(VehicleOdometry* oVehicleOdometry, double iStartTime, double iEndTime);
	bool GetVehicleTransformation(VehicleTransformation& oVehicleTransformation, double iStartTime, double iEndTime);
	bool GetVehicleTransformation(VehicleTransformation* oVehicleTransformation, double iStartTime, double iEndTime);
	double LeastRecentQueueTime();
	double MostRecentQueueTime();
	bool PushPacket(double iPacket[RPQ_PACKETSIZE]);
	bool PushPacket(const pose_rel_msg& iOdomMessage);
	void ResetQueue();
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //RELATIVEPOSEQUEUE_H
