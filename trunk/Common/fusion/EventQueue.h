#ifndef EVENTQUEUE_H
#define EVENTQUEUE_H

#include "Event.h"
#include "EventCodes.h"

#include <FLOAT.H>
#include <WINDOWS.H>

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

class EventQueue
{
	//The event queue class.  Stores events for processing in the local map
private:
	//the critical section used to lock the queue for pushing / pulling events
	CRITICAL_SECTION mQueueLock;
	//the head of the queue (oldest element added)
	Event *mQueueHead;
	//the tail of the queue (most recent element added)
	Event *mQueueTail;

	//whether the event queue is shutting down or not
	bool mIsShuttingDown;
	//number of elements in the queue
	int mNumEvents;

public:

	//handle to the event that triggers when the queue has data
	HANDLE mQueueDataEventHandle;

	EventQueue();
	~EventQueue();

	//empties the entire queue of all events
	void EmptyQueue();
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
	Event* PullEvent(void);
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //EVENTQUEUE_H
