#include "RandomNumberCache.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

RandomNumberCache::RandomNumberCache()
{
	/*
	Default constructor for the random number cache.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	int i;

	//initialize the cache elements with placeholder values
	mIsInitialized = false;
	mCacheSize = RNC_CACHESIZE;
	mNumValues = 0;
	mCache = new double[RNC_CACHESIZE];

	for (i = 0; i < mCacheSize; i++)
	{
		mCache[i] = 0.0;
	}

	return;
}

RandomNumberCache::~RandomNumberCache()
{
	/*
	Destructor for the random number cache.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	//free memory allocated in the cache
	mIsInitialized = false;
	delete [] mCache;
	mCache = NULL;

	return;
}

bool RandomNumberCache::AddNumber(double iNewRandomNumber)
{
	/*
	Adds one random number to the cache at the end of the list.

	INPUTS:
		iNewRandomNumber - the new random number to add

	OUTPUTS:
		rIsFull - true if the cache is filled, false otherwise
	*/

	bool rIsFull = false;

	if (mNumValues < mCacheSize)
	{
		//add the new random number to the cache
		mCache[mNumValues] = iNewRandomNumber;
		mNumValues++;

		//once one value is added, the cache is initialized
		mIsInitialized = true;
	}

	if (mNumValues == mCacheSize)
	{
		//the cache is full
		rIsFull = true;
	}

	return rIsFull;
}

void RandomNumberCache::ReplaceRandomNumber(double iNewRandomNumber)
{
	/*
	Replaces one number from the cache at random with the given value.

	INPUTS:
		iNewRandomNumber - the new random number to add.

	OUTPUTS:
		none.
	*/

	if (mIsInitialized == false)
	{
		//use this number to initialize the cache
		mCache[0] = iNewRandomNumber;
		mIsInitialized = true;
		mNumValues = 1;
		return;
	}

	//mNumValues is guaranteed to be > 0
	int ridx = (rand()) % mNumValues;
	mCache[ridx] = iNewRandomNumber;

	return;
}

void RandomNumberCache::EmptyCache()
{
	/*
	Empties the cache of all random numbers.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	int i;

	//erasing all the values makes the cache invalid
	mIsInitialized = false;
	mNumValues = 0;

	for (i = 0; i < mCacheSize; i++)
	{
		mCache[i] = 0.0;
	}

	return;
}

double RandomNumberCache::RandomNumber()
{
	/*
	Retrieves a random number from the cache

	INPUTS:
		none.

	OUTPUTS:
		rRand - a random number pulled from the cache.
	*/

	double rRand = 0.0;

	if (mIsInitialized == false)
	{
		printf("Warning: RandomNumber() called in uninitialized RandomNumberCache.\n");
		return rRand;
	}

	//mNumValues is guaranteed to be > 0
	int ridx = (rand()) % mNumValues;

	rRand = mCache[ridx];
	return rRand;
}
