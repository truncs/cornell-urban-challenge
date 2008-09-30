#ifndef EVENT_H
#define EVENT_H

#include <MEMORY.H>
#include <STDIO.H>

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

class Event;

class Event
{
	//The event class.  Stores a packet for later processing.

private:

	//the type of event (uses event type defines)
	int mEventType;
	//the time of the event
	double mEventTime;
	//number of rows in the event data information matrix
	int mNumDataRows;
	//number of columns in the event data information matrix
	int mNumDataCols;

	//extra row and column counts for the additional data matrices
	int mNumDataRows2;
	int mNumDataCols2;
	int mNumDataRows3;
	int mNumDataCols3;

public:

	//contains all the data for the event
	double* EventData;
	//contains extra data for the event
	double* EventData2;
	//contains extra data for the event
	double* EventData3;

	//pointer to next event (for queueing)
	Event* NextEvent;
	//pointer to previous event (for queueing)
	Event* PrevEvent;

	Event(void);
	Event(Event* iEvent2Copy);
	~Event(void);

	int EventType(void) {return mEventType;}
	double EventTime(void) {return mEventTime;}
	int NumDataRows(void) {return mNumDataRows;}
	int NumDataRows2(void) {return mNumDataRows2;}
	int NumDataRows3(void) {return mNumDataRows3;}
	int NumDataCols(void) {return mNumDataCols;}
	int NumDataCols2(void) {return mNumDataCols2;}
	int NumDataCols3(void) {return mNumDataCols3;}

	void SetEventType(int iEventType, double iEventTime);
	void SetEventData(int iNumDataRows, int iNumDataCols, double* iEventData);
	void SetEventData2(int iNumDataRows2, int iNumDataCols2, double* iEventData2);
	void SetEventData3(int iNumDataRows3, int iNumDataCols3, double* iEventData3);
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //EVENT_H
