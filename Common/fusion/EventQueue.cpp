#include "EventQueue.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

EventQueue::EventQueue()
{
	/*
	EventQueue constructor.  Initializes the queue to empty and initializes the 
	queue locking critical function properly.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	//initialize the queue locking section
	InitializeCriticalSection(&mQueueLock);

	//initialize the queue data event as an auto-reset event
	mQueueDataEventHandle = CreateEvent(NULL, false, false, NULL);

	//initialize the queue to empty
	mNumEvents = 0;
	mIsShuttingDown = false;
	mQueueHead = NULL;
	mQueueTail = NULL;

	return;
}

EventQueue::~EventQueue()
{
	/*
	EventQueue destructor.  Deletes all elements in the queue and then deletes the critical
	section association with the queue locking functionality.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	//empty the queue
	EmptyQueue();

	//acquire the critical section so nothing else can modify the queue
	EnterCriticalSection(&mQueueLock);

	//release the queue data flag event
	CloseHandle(mQueueDataEventHandle);

	//release the queue locking section
	DeleteCriticalSection(&mQueueLock);

	return;
}

int EventQueue::NumEvents()
{
	/*
	Returns the number of events in the queue

	INPUTS:
		none.

	OUTPUTS:
		rNumEvents - returns the number of events in the queue.
	*/

	int rNumEvents;

	//lock the queue
	EnterCriticalSection(&mQueueLock);

	rNumEvents = mNumEvents;

	LeaveCriticalSection(&mQueueLock);

	return rNumEvents;
}

bool EventQueue::IsShuttingDown()
{
	/*
	Returns whether the queue is shutting down (has received 
	a QUIT_EVENT) or not

	INPUTS:
		none.

	OUTPUTS:
		rIsShuttingDown - true if the queue is shutting down,
			false otherwise.
	*/

	bool rIsShuttingDown = false;

	//lock the queue
	EnterCriticalSection(&mQueueLock);

	rIsShuttingDown = mIsShuttingDown;

	LeaveCriticalSection(&mQueueLock);

	return rIsShuttingDown;
}

bool EventQueue::QueueHasEventsReady()
{
	/*
	Returns true if the queue has events ready for processing,
	false otherwise.

	INPUTS:
		none.

	OUTPUTS:
		rHasEvents - true if the queue is has events ready for processing, false otherwise
	*/

	bool rHasEvents = true;

	EnterCriticalSection(&mQueueLock);

	if (mNumEvents == 0)
	{
		//no events in the queue
		rHasEvents = false;
	}

	LeaveCriticalSection(&mQueueLock);

	//if code gets here, there's an event ready for processing
	return rHasEvents;
}

Event* EventQueue::PullEvent()
{
	/*
	Pulls the first element from the queue.

	INPUTS:
		none.

	OUTPUTS:
		NewEvent - a pointer to the event that happens next
	*/

	//return the first event on the queue
	Event* NewEvent;

	//lock the queue so no other events can be added while tampering
	EnterCriticalSection(&mQueueLock);

	//verify that the data can actually be removed
	if (mNumEvents == 0)
	{
		//nothing in the queue
		LeaveCriticalSection(&mQueueLock);
		return NULL;
	}

	//store the event to return
	NewEvent = mQueueHead;

	if (mQueueHead != NULL)
	{
		//advance the queue head to the next unpulled event
		mQueueHead = mQueueHead->NextEvent;
		//the pull was successful, so drop one element from the queue count
		mNumEvents--;
	}

	if (mQueueHead != NULL)
	{
		//if the new queue is not empty, isolate it from the pulled event
		mQueueHead->PrevEvent = NULL;
	}
	else
	{
		//we just pulled the last event... erase the pointer to the queue tail
		mQueueTail = NULL;
	}

	if (NewEvent != NULL)
	{
		//isolate the event that will be returned
		NewEvent->NextEvent = NULL;
		NewEvent->PrevEvent = NULL;
	}

	//release the queue to other threads
	LeaveCriticalSection(&mQueueLock);

	return NewEvent;
}

bool EventQueue::PushEvent(Event *NewEvent)
{
	/*
	Pushes a new event onto the end of the queue

	INPUTS:
		NewEvent - a pointer to the event to be added to the queue.

	OUTPUTS:
		rSuccess - true if the packet is successfully pushed, false otherwise.
	*/

	bool rSuccess = false;

	if (NewEvent != NULL)
	{
		//lock the queue so no other events can be added while tampering
		EnterCriticalSection(&mQueueLock);

		//check the event for a valid timestamp
		if (NewEvent->EventTime() < Q_MINTIMESTAMP || NewEvent->EventTime() > Q_MAXTIMESTAMP)
		{
			printf("Warning: ignoring type %d event with timestamp %.12lg.\n", NewEvent->EventType(), NewEvent->EventTime());
			LeaveCriticalSection(&mQueueLock);

			//NOTE: normally NewEvent is freed when pulled from the queue, so here it needs to be done automatically
			delete NewEvent;
			return rSuccess;
		}

		if (mIsShuttingDown == true)
		{
			//do not accept packets when the queue is shutting down
			LeaveCriticalSection(&mQueueLock);

			//NOTE: normally NewEvent is freed when pulled from the queue, so here it needs to be done automatically
			delete NewEvent;
			return rSuccess;
		}

		//add the new event to the queue
		NewEvent->PrevEvent = NULL;
		NewEvent->NextEvent = NULL;
		if (NewEvent->EventType() == QUIT_EVENT)
		{
			//when a quit event comes in, mark the queue as shutting down
			mIsShuttingDown = true;
		}

		if (mQueueHead == NULL)
		{
			//the new event becomes the only event in the queue
			mQueueHead = NewEvent;
			mQueueTail = NewEvent;
		}
		else
		{
			//some events already exist on the queue
			mQueueTail->NextEvent = NewEvent;
			NewEvent->PrevEvent = mQueueTail;
			mQueueTail = NewEvent;
		}

		//if code gets here, event has been added successfully
		rSuccess = true;
		//increment the number of events stored
		mNumEvents++;

		//release the queue to other threads
		LeaveCriticalSection(&mQueueLock);

		if (QueueHasEventsReady() == true)
		{
			//signal that there is data present in the queue if the queue is ready
			SetEvent(mQueueDataEventHandle);
		}
	}

	return rSuccess;
}

double EventQueue::MostRecentQueueTime()
{
	/*
	Returns the most recent time of all the times in the event queue.

	INPUTS:
		none.

	OUTPUTS:
		rEvenTime - the timestamp on the most recent event in the queue
	*/

	double rEventTime = -DBL_MAX;
	int i;
	Event* CurrentEvent;

	EnterCriticalSection(&mQueueLock);

	CurrentEvent = mQueueHead;
	for (i = 0; i < mNumEvents; i++)
	{
		if (rEventTime < CurrentEvent->EventTime())
		{
			rEventTime = CurrentEvent->EventTime();
		}
		CurrentEvent = CurrentEvent->NextEvent;
	}

	LeaveCriticalSection(&mQueueLock);

	return rEventTime;
}

double EventQueue::LeastRecentQueueTime()
{
	/*
	Returns the least recent time of all the times in the event queue.

	INPUTS:
		none.

	OUTPUTS:
		rEventTime - time timestamp on the least recent event in the queue.
	*/

	double rEventTime = DBL_MAX;
	int i;
	Event* CurrentEvent;

	EnterCriticalSection(&mQueueLock);

	CurrentEvent = mQueueHead;
	for (i = 0; i < mNumEvents; i++)
	{
		if (rEventTime > CurrentEvent->EventTime())
		{
			rEventTime = CurrentEvent->EventTime();
		}
		CurrentEvent = CurrentEvent->NextEvent;
	}

	LeaveCriticalSection(&mQueueLock);

	if (rEventTime == DBL_MAX)
	{
		//return an invalid time if no events are present
		rEventTime = -DBL_MAX;
	}

	return rEventTime;
}

void EventQueue::EmptyQueue()
{
	/*
	Removes all events from the queue.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	Event* CurrentEvent;

	//lock the queue so no other events can be added while tampering
	EnterCriticalSection(&mQueueLock);

	while (mQueueHead != NULL)
	{
		//store the event to delete
		CurrentEvent = mQueueHead;

		if (mQueueHead != NULL)
		{
			//advance the queue head to the next unpulled event
			mQueueHead = mQueueHead->NextEvent;
			//the pull was successful, so drop one element from the queue count
			mNumEvents--;
		}

		if (mQueueHead != NULL)
		{
			//if the new queue is not empty, isolate it from the pulled event
			mQueueHead->PrevEvent = NULL;
		}
		else
		{
			//we just pulled the last event... erase the pointer to the queue tail
			mQueueTail = NULL;
		}

		if (CurrentEvent != NULL)
		{
			//isolate the event that will be deleted
			CurrentEvent->NextEvent = NULL;
			CurrentEvent->PrevEvent = NULL;
		}

		//delete the event that was just removed from the queue
		delete CurrentEvent;
	}

	//release the queue to other threads
	LeaveCriticalSection(&mQueueLock);

	return;
}
