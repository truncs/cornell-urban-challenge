#include "SynchronizedEventQueue.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

SynchronizedEventQueue::SynchronizedEventQueue(double iQueueDelayTime)
{
	/*
	Default constructor.  Initializes the queue to empty and initializes the 
	queue locking critical function properly.

	INPUTS:
		iQueueDelayTime - delay time (sec.) to wait before signaling the queue

	OUTPUTS:
		none.
	*/

	//initialize the queue locking section
	InitializeCriticalSection(&mQueueLock);

	//initialize the queue data event as an auto-reset event
	mQueueDataEventHandle = CreateEvent(NULL, false, false, NULL);

	//initialize the queue to empty with no bad events
	mNumEvents = 0;
	mQueueHead = NULL;
	mQueueTail = NULL;
	mNumBadEvents = 0;

	//start out the event queue as not initialized
	mIsInitialized = false;
	//and not shutting down
	mIsShuttingDown = false;
	//initialize the oldest and newest elements
	mQueueOldestTime = -DBL_MAX;
	mQueueNewestTime = -DBL_MAX;

	mQueueDelayTime = 0.0;
	if (iQueueDelayTime >= mQueueDelayTime)
	{
		mQueueDelayTime = iQueueDelayTime;
	}

	return;
}

SynchronizedEventQueue::~SynchronizedEventQueue()
{
	/*
	Default destructor.  Deletes all elements in the queue and then deletes the critical
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

void SynchronizedEventQueue::ResetQueue()
{
	/*
	Completely resets the event queue to a state where it never received
	any events.  NOTE: does not lock the queue, so this must be kept private.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	printf("Warning: resetting synchronized event queue...\n");

	//remove all events on the queue (fast remove, not elegant)
	Event* OldEvent;
	while (mQueueHead != NULL)
	{
		OldEvent = mQueueHead;
		mQueueHead = mQueueHead->NextEvent;
		delete OldEvent;
	}

	//initialize the queue to empty with no bad events
	mNumEvents = 0;
	mQueueHead = NULL;
	mQueueTail = NULL;
	mNumBadEvents = 0;

	//start out the event queue as not initialized
	mIsInitialized = false;
	//and not shutting down
	mIsShuttingDown = false;
	//initialize the oldest and newest elements
	mQueueOldestTime = -DBL_MAX;
	mQueueNewestTime = -DBL_MAX;

	return;
}

int SynchronizedEventQueue::NumEvents()
{
	/*
	Returns the number of events in the queue

	INPUTS:
		none.

	OUTPUTS:
		mNumEvents - returns the number of events in the queue.
	*/

	int rNumEvents;

	//lock the queue while counting the elements
	EnterCriticalSection(&mQueueLock);

	rNumEvents = mNumEvents;

	LeaveCriticalSection(&mQueueLock);

	return rNumEvents;
}

bool SynchronizedEventQueue::IsInitialized()
{
	/*
	Returns true if the queue has received a valid timestamp, false otherwise

	INPUTS:
		none.

	OUTPUTS:
		rIsInitialized - returns whether the queue has received a valid timestamp
	*/

	bool rIsInitialized;

	//lock the queue while counting the elements
	EnterCriticalSection(&mQueueLock);

	rIsInitialized = mIsInitialized;

	LeaveCriticalSection(&mQueueLock);

	return rIsInitialized;
}

bool SynchronizedEventQueue::IsShuttingDown()
{
	/*
	Returns true if the queue is shutting down (has received a QUIT_EVENT), 
	false otherwise.

	INPUTS:
		none.

	OUTPUTS:
		rIsShuttingDown - returns whether the queue is shutting down
	*/

	bool rIsShuttingDown;

	//lock the queue while counting the elements
	EnterCriticalSection(&mQueueLock);

	rIsShuttingDown = mIsShuttingDown;

	LeaveCriticalSection(&mQueueLock);

	return rIsShuttingDown;
}

bool SynchronizedEventQueue::QueueHasEventsReady()
{
	/*
	Returns true if the queue has events ready for processing,
	false otherwise.

	INPUTS:
		none.

	OUTPUTS:
		returns true if the queue is has events ready for processing, false otherwise
	*/

	bool rHasEvents = true;

	EnterCriticalSection(&mQueueLock);

	if (mNumEvents == 0)
	{
		//no events in the queue
		rHasEvents = false;
	}

	if (mIsInitialized == true && mIsShuttingDown == false)
	{
		//if the queue is initialized and not shutting down, delay packets appropriately

		if (mQueueNewestTime - mQueueOldestTime < mQueueDelayTime)
		{
			//queue hasn't waited long enough for data to be ready
			rHasEvents = false;
		}
	}

	LeaveCriticalSection(&mQueueLock);

	//if code gets here, there's an event ready for processing
	return rHasEvents;
}

void SynchronizedEventQueue::EmptyQueue()
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

	LeaveCriticalSection(&mQueueLock);

	return;
}

void SynchronizedEventQueue::SetQueueTimes()
{
	/*
	Sets the queue's oldest and newest event times, to evaluate when some
	events are ready to be processed.  NOTE: this is not thread safe, so it
	must be called from a function that locks the queue.

	INPUTS:
		none.

	OUTPUTS:
		none.  Sets mQueueOldestTime and mQueueNewestTime
	*/

	if (mQueueHead != NULL)
	{
		mQueueOldestTime = mQueueHead->EventTime();
	}

	if (mQueueTail != NULL)
	{
		mQueueNewestTime = mQueueTail->EventTime();
	}

	return;
}

Event* SynchronizedEventQueue::PullEvent()
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
	if (mIsInitialized == false || mNumEvents == 0)
	{
		//nothing in the queue
		LeaveCriticalSection(&mQueueLock);
		return NULL;
	}
	if (mIsShuttingDown == false)
	{
		//NOTE: this allows all queue events to be flushed when the queue shuts down
		//otherwise, the queue delays events before pulling them

		if (mQueueNewestTime - mQueueOldestTime < mQueueDelayTime)
		{
			//queue hasn't delayed long enough
			LeaveCriticalSection(&mQueueLock);
			return NULL;
		}
	}
	//if code gets here, the queue has delayed long enough to return an event

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

	//reset the queue delay times
	SetQueueTimes();

	//release the queue to other threads
	LeaveCriticalSection(&mQueueLock);

	return NewEvent;
}

bool SynchronizedEventQueue::PushEvent(Event *NewEvent)
{
	/*
	Pushes a new event onto the end of the queue.

	INPUTS:
		NewEvent - a pointer to the event to be added to the queue.

	OUTPUTS:
		rSuccess - returns true if successful, false otherwise.
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

		//check whether the new event is not too old
		if (mIsInitialized == true && mIsShuttingDown == false)
		{
			//only bother checking if the queue is initialized (has valid times)
			//and is not shutting down
			//NOTE: the ShuttingDown check forces quit events to be accepted.
			if (mQueueNewestTime - NewEvent->EventTime() > mQueueDelayTime)
			{
				//this event is too old: do not add it.
				//NOTE: mQueueNewestTime is initialized to -DBL_MAX, so the first
				//event is automatically accepted.

				printf("Warning: ignoring old type %d event of age %.12lg ms.\n", NewEvent->EventType(), 1000.0*(mQueueNewestTime - NewEvent->EventTime()));
				//increment the number of bad events received
				mNumBadEvents++;
				if (mNumBadEvents > SEQ_NUMEVENTSB4RESET)
				{
					//the queue received a lot of bad timestamps, so reset it
					//NOTE: the next valid event will set the queue timestamp
					ResetQueue();
				}

				LeaveCriticalSection(&mQueueLock);

				//NOTE: normally NewEvent is freed when pulled from the queue, so here it needs to be done automatically
				delete NewEvent;
				return rSuccess;
			}
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

			//insert in sorted order (oldest first)
			if (mQueueTail->EventTime() <= NewEvent->EventTime())
			{
				//new event is the most recent event; add it at the tail
				mQueueTail->NextEvent = NewEvent;
				NewEvent->PrevEvent = mQueueTail;
				mQueueTail = NewEvent;
			}
			else if (mQueueHead->EventTime() >= NewEvent->EventTime())
			{
				//new event is the oldest event; add it at the head
				mQueueHead->PrevEvent = NewEvent;
				NewEvent->NextEvent = mQueueHead;
				mQueueHead = NewEvent;
			}
			else
			{
				//new event must be inserted in the middle of the queue
				Event* EventBefore;
				EventBefore = mQueueTail;
				while (EventBefore->EventTime() > NewEvent->EventTime())
				{
					EventBefore = EventBefore->PrevEvent;
				}
				//the new event is to be inserted just after EventBefore
				Event* EventAfter = EventBefore->NextEvent;
				//and just before EventAfter
				EventBefore->NextEvent = NewEvent;
				EventAfter->PrevEvent = NewEvent;
				NewEvent->PrevEvent = EventBefore;
				NewEvent->NextEvent = EventAfter;
			}
		}

		//if code gets here, the event has been added successfully
		rSuccess = true;
		//mark the queue as being successfully initialized
		mIsInitialized = true;
		//increment the number of events stored
		mNumEvents++;
		//when an event is successfully pushed, reset the number of bad events seen.
		mNumBadEvents = 0;

		//reset the queue times
		SetQueueTimes();

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

double SynchronizedEventQueue::MostRecentQueueTime()
{
	/*
	Returns the most recent (latest) time event currently in the queue.  Used to
	determine what the current time is.

	INPUTS:
		none.

	OUTPUTS:
		rLatestTime - returns the timestamp of the latest time in the queue.  If no
			events are in the queue, then the latest time that has ever been in the
			queue is returned.
	*/

	double rLatestTime = 0.0;

	EnterCriticalSection(&mQueueLock);

	rLatestTime = mQueueNewestTime;

	LeaveCriticalSection(&mQueueLock);

	return rLatestTime;
}

double SynchronizedEventQueue::LeastRecentQueueTime()
{
	/*
	Returns the least recent (oldest) time event currently in the queue.  Used to
	determine what the current time is.

	INPUTS:
		none.

	OUTPUTS:
		rOldestTime - returns the timestamp of the oldest time in the queue.  If no
			events are in the queue, then the latest time that has ever been in the
			queue is returned.
	*/

	double rOldestTime = 0.0;

	EnterCriticalSection(&mQueueLock);

	rOldestTime = mQueueOldestTime;

	LeaveCriticalSection(&mQueueLock);

	return rOldestTime;
}
