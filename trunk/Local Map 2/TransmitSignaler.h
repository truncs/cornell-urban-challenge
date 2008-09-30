#ifndef TRANSMITSIGNALER_H
#define TRANSMITSIGNALER_H

#include "CarTime.h"
#include "Event.h"
#include "EventCodes.h"
#include "Globals.h"
#include "LocalMapConstants.h"
#include "SynchronizedEventQueue.h"

#include <FLOAT.H>
#include <STDIO.H>
#include <WINDOWS.H>

//time to wait for the signaler to quit (ms)
#define TS_SHUTDOWNTIMEOUT 500

class TransmitSignaler
{
	//periodically drops a transmit event into the event queue

private:

	//the callback thread that acts as the interface to the sensor
	HANDLE mInterfaceThread;
	//the sample period, in milliseconds (how long to wait between transmit events)
	DWORD mSamplePeriod;
	//whether the thread is running or should be terminated
	bool mIsRunning;

public:

	TransmitSignaler(DWORD iSamplePeriod);
	~TransmitSignaler();

	bool IsRunning() {return mIsRunning;}
	DWORD SamplePeriod() {return mSamplePeriod;}
};

//the sensor callback function
static DWORD WINAPI TransmitCallback(LPVOID lpparam);

#endif //TRANSMITSIGNALER_H
