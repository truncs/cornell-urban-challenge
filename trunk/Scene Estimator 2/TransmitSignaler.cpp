#include "TransmitSignaler.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

TransmitSignaler::TransmitSignaler(DWORD iSamplePeriod)
{
	/*
	Constructor for the transmit type signaler.

	INPUTS:
		iSamplePeriod - (integer) number of milliseconds between each transmit event.  Used
			to signal a data transmit to subscribed listeners

	OUTPUTS:
		none.
	*/

	//store the sensor sampling period
	mSamplePeriod = iSamplePeriod;

	//begin the thread running
	mIsRunning = true;

	//create the reader thread that will provide the sensor data
	mInterfaceThread = CreateThread(NULL, 0, TransmitCallback, this, 0, NULL);

	printf("Transmit signaler running.\n");

	return;
}

TransmitSignaler::~TransmitSignaler()
{
	/*
	Destructor for the transmit sensor.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	//stop the thread from running
	mIsRunning = false;
	//wait for the thread to terminate itself
	printf("Waiting for transmit signaler thread to terminate...\n");
	if (WaitForSingleObjectEx(mInterfaceThread, TS_SHUTDOWNTIMEOUT, false) != WAIT_OBJECT_0)
	{
		printf("Warning: transmit signaler thread timed out, terminating forcefully...\n");
		TerminateThread(mInterfaceThread, 2);
	}
	//destroy the thread associated with the sensor
	CloseHandle(mInterfaceThread);
	printf("Transmit signaler thread terminated successfuly.\n");

	return;
}

DWORD WINAPI TransmitCallback(LPVOID lpparam)
{
	/*
	Callback function for signaling a data transmit.

	INPUTS:
		lpparam - a pointer to the TransmitSensor that owns this reader thread.

	OUTPUTS:
		none.  (always returns 0)
	*/

	//cast the input argument to an instance of the sensor container class
	TransmitSignaler* TheSignaler = (TransmitSignaler*)lpparam;

	//this will store the event data (i.e. the entire packet)
	int iEventType = TRANSMIT_EVENT;
	//the event's time stamp
	double iEventTime = DBL_MAX;
	//the number of elements in the event
	int iNumElements = 0;
	//the number of columns in the event
	int iNumColumns = 0;
	double* iEventData = NULL;
	//this will be used to create each new event as it arrives
	Event* NewEvent = NULL;
	//access to the posterior pose event queue
	extern SynchronizedEventQueue* ThePosteriorPoseEventQueue;
	extern SynchronizedEventQueue* TheTrackGeneratorEventQueue;
	extern CarTime* TheCarTime;
	double SceneEstimatorTime;

	while (TheSignaler->IsRunning() == true)
	{
		//create an event for transmitting

		if (TheSignaler->IsRunning() == false)
		{
			//for terminating thread, just go to sleep
			Sleep(TheSignaler->SamplePeriod());
			continue;
		}

		//when the code gets here, create a transmit event
		iEventType = TRANSMIT_EVENT;
		SceneEstimatorTime = TheCarTime->CurrentCarTime();
		iEventTime = SceneEstimatorTime;
		iNumElements = 0;
		iNumColumns = 0;
		iEventData = NULL;

		//create the event data
		NewEvent = new Event();
		NewEvent->SetEventType(iEventType, iEventTime);
		NewEvent->SetEventData(iNumElements, iNumColumns, iEventData);
		ThePosteriorPoseEventQueue->PushEvent(NewEvent);

		NewEvent = new Event();
		NewEvent->SetEventType(iEventType, iEventTime);
		NewEvent->SetEventData(iNumElements, iNumColumns, iEventData);
		TheTrackGeneratorEventQueue->PushEvent(NewEvent);

		//sleep for the delay time of the sensor
		Sleep(TheSignaler->SamplePeriod());
	}

	return 0;
}
