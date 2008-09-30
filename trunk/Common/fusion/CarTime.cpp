#include "CarTime.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

CarTime::CarTime()
{
	/*
	CarTime default constructor.  Initializes the car time object.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	//initialize the car time to something invalid
	mCurrentCarTime = -DBL_MAX;
	//initialize to no bad timestamps
	mNumBadTimestamps = 0;
	//initialize the time lock
	InitializeCriticalSection(&mTimeLock);

	return;
}

CarTime::~CarTime()
{
	/*
	CarTime destructor.  Releases the car time object.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	//acquire the time lock and delete it
	EnterCriticalSection(&mTimeLock);
	DeleteCriticalSection(&mTimeLock);

	return;
}

void CarTime::Reset()
{
	/*
	Resets car time to no information.  NOTE: not threadsafe, so must be called from
	inside the timelock critical section.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	printf("Warning: resetting car time...\n");

	//initialize the car time to something invalid
	mCurrentCarTime = -DBL_MAX;
	//initialize to no bad timestamps
	mNumBadTimestamps = 0;

	return;
}

double CarTime::CurrentCarTime()
{
	/*
	Returns the current car time.

	INPUTS:
		none.

	OUTPUTS:
		rTime - the current car time.
	*/

	double rTime;

	EnterCriticalSection(&mTimeLock);
	rTime = mCurrentCarTime;
	LeaveCriticalSection(&mTimeLock);

	return rTime;
}

void CarTime::SetCurrentCarTime(double iCurrentCarTime)
{
	/*
	Sets the current car time to a given value.

	INPUTS:
		iCurrentCarTime - the time to be set.

	OUTPUTS:
		none.
	*/

	EnterCriticalSection(&mTimeLock);

	if (iCurrentCarTime < CT_MINTIMESTAMP || iCurrentCarTime > CT_MAXTIMESTAMP)
	{
		//don't update car time with known invalid timestamps
		printf("Warning: got unexpected car time: %.12lg.\n", iCurrentCarTime);
		LeaveCriticalSection(&mTimeLock);
		return;
	}

	if (iCurrentCarTime < mCurrentCarTime)
	{
		printf("Warning: got old car time of age %.12lg ms.\n", (mCurrentCarTime - iCurrentCarTime)*1000.0);
		//increment the number of bad timestamps received
		mNumBadTimestamps++;

		if (mNumBadTimestamps > CT_NUMEVENTSB4RESET)
		{
			//if too many bad events come in, reset car time (next valid timestamp will set the time)
			Reset();
		}

		LeaveCriticalSection(&mTimeLock);

		return;
	}
	else if (iCurrentCarTime - mCurrentCarTime > CT_TIMEJUMPWARNING)
	{
		//got an unexpected timestamp
		printf("Warning: got car time %.12lg; expected something near %.12lg.\n", iCurrentCarTime, mCurrentCarTime);
		//NOTE: time is allowed to move forward: this is not a bad packet, just a warning
	}

	//update the car time
	mCurrentCarTime = iCurrentCarTime;
	//each time a successful timestamp goes through, reset the number of bad timestamps to 0
	mNumBadTimestamps = 0;

	LeaveCriticalSection(&mTimeLock);

	return;
}
