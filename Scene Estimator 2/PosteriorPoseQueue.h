#ifndef POSTERIORPOSEQUEUE_H
#define POSTERIORPOSEQUEUE_H

#include "MatrixIndex.h"
#include "PosteriorPosePosition.h"

#include <FLOAT.H>
#include <MATH.H>
#include <STDIO.H>
#include <WINDOWS.H>

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

//number of doubles in a posterior pose position packet
#define PPQ_PACKETSIZE 10
//maximum allowed offset (sec.) between posterior pose solution and desired timestamp
#define PPQ_MAXPACKETOFFSET 0.2
//maximum number of bad events tolerated before the posterior pose queue resets
#define PPQ_NUMEVENTSB4RESET 10

class PosteriorPoseQueue
{
	//The posterior pose queue class.  Stores a rolling queue of posterior pose
	//measurements to supply to the track generator at arbitrary timestamps

private:

	//the size of the queue
	int mQueueSize;
	//number of packets currently stored
	int mNumPacketsInQueue;
	//the tail (last element added) of the queue
	int mQueueTail;
	//the queue storing posterior pose solutions
	double* mPPQueue;

	//a counter for events that are iqnored due to their timestamp being older than the most recent timestamp
	int mNumBadEvents;

	CRITICAL_SECTION mQueueLock;

	bool PullPacket(double oPacket[PPQ_PACKETSIZE], double iEventTime);

public:

	PosteriorPoseQueue(int iQueueSize);
	~PosteriorPoseQueue(void);	

	void GetPosteriorPose(PosteriorPosePosition* oPosteriorPose, double iPosteriorPoseTime);
	double LeastRecentQueueTime();
	double MostRecentQueueTime();
	bool PushPacket(PosteriorPosePosition* iPosteriorPose);
	void ResetQueue();
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //POSTERIORPOSEQUEUE_H
