#include "Event.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

Event::Event(void)
{
	/*
	Event constructor.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	//store the type of event
	mEventType = -1;
	mEventTime = 0.0;
	//store the event data
	mNumDataRows = 0;
	mNumDataCols = 0;

	EventData = NULL;

	mNumDataRows2 = 0;
	mNumDataCols2 = 0;
	mNumDataRows3 = 0;
	mNumDataCols3 = 0;

	EventData2 = NULL;
	EventData3 = NULL;

	//initialize with no pointer data
	NextEvent = NULL;
	PrevEvent = NULL;

	return;
}

Event::Event(Event* iEvent2Copy)
{
	/*
	Copy constructor for an event.  Copies all memory from the input event
	into the new event, allocating new memory in the process

	INPUTS:
		iEvent2Copy - the event to copy

	OUTPUTS:
		none.
	*/

	//copy variables by value first
	mEventType = iEvent2Copy->EventType();
	mEventTime = iEvent2Copy->EventTime();

	mNumDataRows = iEvent2Copy->NumDataRows();
	mNumDataCols = iEvent2Copy->NumDataCols();
	if (mNumDataRows > 0 && mNumDataCols > 0)
	{
		EventData = new double[mNumDataRows*mNumDataCols];
		memcpy(EventData, iEvent2Copy->EventData, sizeof(double) * mNumDataRows * mNumDataCols);
	}
	else
	{
		EventData = NULL;
	}

	mNumDataRows2 = iEvent2Copy->NumDataRows2();
	mNumDataCols2 = iEvent2Copy->NumDataCols2();
	if (mNumDataRows2 > 0 && mNumDataCols2 > 0)
	{
		EventData2 = new double[mNumDataRows2*mNumDataCols2];
		memcpy(EventData2, iEvent2Copy->EventData2, sizeof(double) * mNumDataRows2 * mNumDataCols2);
	}
	else
	{
		EventData2 = NULL;
	}

	mNumDataRows3 = iEvent2Copy->NumDataRows3();
	mNumDataCols3 = iEvent2Copy->NumDataCols3();
	if (mNumDataRows3 > 0 && mNumDataCols3 > 0)
	{
		EventData3 = new double[mNumDataRows3*mNumDataCols3];
		memcpy(EventData3, iEvent2Copy->EventData3, sizeof(double) * mNumDataRows3 * mNumDataCols3);
	}
	else
	{
		EventData3 = NULL;
	}

	NextEvent = iEvent2Copy->NextEvent;
	PrevEvent = iEvent2Copy->PrevEvent;

	return;
}

Event::~Event(void)
{
	/*
	Event destructor.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	//erase pointer data
	NextEvent = NULL;
	PrevEvent = NULL;

	//delete all event data
	delete [] EventData;
	delete [] EventData2;
	delete [] EventData3;

	return;
}

void Event::SetEventType(int iEventType, double iEventTime)
{
	/*
	Stores the event type and time.

	INPUTS:
		iEventType - type of event (valid values defined in Event.h)
		iEventTime - time of the event (system time, sec.)

	OUTPUTS:
		none.
	*/

	mEventType = iEventType;
	mEventTime = iEventTime;

	return;
}

void Event::SetEventData(int iNumDataRows, int iNumDataCols, double* iEventData)
{
	/*
	Stores event data in the primary event data matrix

	INPUTS:
		iNumDataRows - number of rows in the iEventData event data matrix
		iNumDataCols - number of columns in the iEventData event data matrix
		iEventData - array of doubles containing the information of the event

	OUTPUTS:
		none.
	*/

	//delete any existing event data to prevent event memory leak
	delete [] EventData;

	mNumDataRows = iNumDataRows;
	mNumDataCols = iNumDataCols;
	EventData = iEventData;

	return;
}

void Event::SetEventData2(int iNumDataRows2, int iNumDataCols2, double* iEventData2)
{
	/*
	Stores event data in the secondary event data matrix

	INPUTS:
		iNumDataRows2 - number of rows in the iEventData event data matrix
		iNumDataCols2 - number of columns in the iEventData event data matrix
		iEventData2 - array of doubles containing the information of the event

	OUTPUTS:
		none.
	*/

	//delete any existing event data to prevent event memory leak
	delete [] EventData2;

	mNumDataRows2 = iNumDataRows2;
	mNumDataCols2 = iNumDataCols2;
	EventData2 = iEventData2;

	return;
}

void Event::SetEventData3(int iNumDataRows3, int iNumDataCols3, double* iEventData3)
{
	/*
	Stores event data in the tertiary event data matrix

	INPUTS:
		iNumDataRows3 - number of rows in the iEventData event data matrix
		iNumDataCols3 - number of columns in the iEventData event data matrix
		iEventData3 - array of doubles containing the information of the event

	OUTPUTS:
		none.
	*/

	//delete any existing event data to prevent event memory leak
	delete [] EventData3;

	mNumDataRows3 = iNumDataRows3;
	mNumDataCols3 = iNumDataCols3;
	EventData3 = iEventData3;

	return;
}
