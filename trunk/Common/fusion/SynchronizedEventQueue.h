#ifndef SYNCHRONIZEDEVENTQUEUE_H
#define SYNCHRONIZEDEVENTQUEUE_H

#include "Event.h"
#include "EventCodes.h"

#include <FLOAT.H>
#include <WINDOWS.H>

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

//maximum number of bad events tolerated before a reset
#define SEQ_NUMEVENTSB4RESET 100

class SynchronizedEventQueue
{
	//The synchronized event queue class.  Stores synchronized data to incorporate
	//into an estimator in the proper order.

private:

	//the critical section used to lock the queue for pushing / pulling events
	CRITICAL_SECTION mQueueLock;

	//the head of the queue (oldest element added)
	Event *mQueueHead;
	//the tail of the queue (most recent element added)
	Event *mQueueTail;

	//the oldest and newest event times (sec.)
	double mQueueOldestTime;
	double mQueueNewestTime;
	//the time to wait before signaling the queue (sec.)
	double mQueueDelayTime;

	//whether the event queue has been initialized with a valid time
	bool mIsInitialized;
	//whether the event queue is shutting down or not
	bool mIsShuttingDown;
	//number of elements in the queue
	int mNumEvents;
	//number of bad events received in the queue
	int mNumBadEvents;

	//resets the queue
	void ResetQueue();
	//sets the oldest and newest queue times
	void SetQueueTimes();

public:

	//handle to the event that triggers when the queue has data
	HANDLE mQueueDataEventHandle;

	SynchronizedEventQueue(double iQueueDelayTime);
	~SynchronizedEventQueue();

	//empties the entire queue of all events
	void EmptyQueue();
	//whether the queue has ever received a valid timestamp or not
	bool IsInitialized();
	//whether the queue is shutting down or not
	bool IsShuttingDown();
	//returns the time of the oldest event in the queue
	double LeastRecentQueueTime();
	//returns the time of the newest event in the queue
	double MostRecentQueueTime();
	//return the number of events in the queue
	int NumEvents();
	//returns whether the queue has events ready to be processed
	bool QueueHasEventsReady();

	//pushes a new event to the queue
	bool PushEvent(Event *NewEvent);
	//pulls the next event from the queue
	Event* PullEvent();
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif


#endif //SYNCHRONIZEDEVENTQUEUE_H
