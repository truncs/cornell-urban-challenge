#ifndef CARTIME_H
#define CARTIME_H

#include <FLOAT.H>
#include <MATH.H>
#include <STDIO.H>
#include <WINDOWS.H>

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

//maximum time difference between existing and incoming car time before a warning is issued (sec.)
#define CT_TIMEJUMPWARNING 1.0
//maximum time jump an event can have to be considered valid (sec.)
#define CT_TIMEJUMPREJECT 5.0
//maximum number of bad timestamps tolerated before a reset
#define CT_NUMEVENTSB4RESET 100
//minimum timestamp
#define CT_MINTIMESTAMP 0.0
//maximum timestamp
#define CT_MAXTIMESTAMP 65535.0

class CarTime
{
	//The car time class.  Stores the current car time in a thread-safe manner.

private:

	//the current car time
	double mCurrentCarTime;
	//total number of weird timestamps received so far
	int mNumBadTimestamps;
	//the time locking critical section
	CRITICAL_SECTION mTimeLock;

	//resets car time
	void Reset();

public:

	CarTime();
	~CarTime();

	double CurrentCarTime();
	void SetCurrentCarTime(double iCurrentCarTime);
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //CARTIME_H
