#ifndef TRANSMITSIGNALER_H
#define TRANSMITSIGNALER_H

#include "CarTime.h"
#include "Event.h"
#include "EventCodes.h"
#include "Globals.h"
#include "SceneEstimatorConstants.h"
#include "SynchronizedEventQueue.h"

#include <FLOAT.H>
#include <STDIO.H>
#include <WINDOWS.H>

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

//time to wait for the signaler to quit (ms)
#define TS_SHUTDOWNTIMEOUT 500

class TransmitSignaler
{
	//periodically drops a transmit event into the event queue

private:

	//the callback thread that acts as the interface to the sensor
	HANDLE mInterfaceThread;
	//whether the thread is running or should be terminated
	bool mIsRunning;
	//the sample period, in milliseconds (how long to wait between transmit events)
	DWORD mSamplePeriod;

public:

	TransmitSignaler(DWORD iSamplePeriod);
	~TransmitSignaler();

	bool IsRunning() {return mIsRunning;}
	DWORD SamplePeriod() {return mSamplePeriod;}
};

//the sensor callback function
static DWORD WINAPI TransmitCallback(LPVOID lpparam);

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //TRANSMITSIGNALER_H
