#ifndef RANDOMNUMBERCACHE_H
#define RANDOMNUMBERCACHE_H

#include <STDIO.H>
#include <STDLIB.H>

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

#define RNC_CACHESIZE RAND_MAX+1

class RandomNumberCache
{
	//A cache of RNC_CACHESIZE random numbers to be used for fast drawing.
	//NOTE: THIS CLASS IS NOT THREAD SAFE!  DO NOT MAKE IT GLOBAL!!!

private:

	//whether the cache has been initialized
	bool mIsInitialized;
	//the size of the cache
	int mCacheSize;
	//the number of valid values in the cache (0 - RNC_CACHESIZE)
	int mNumValues;
	//the cache of numbers
	double* mCache;

public:

	RandomNumberCache();
	~RandomNumberCache();

	bool IsInitialized() {return mIsInitialized;}
	int SizeOfCache() {return mCacheSize;}

	bool AddNumber(double iNewRandomNumber);
	void EmptyCache();
	double RandomNumber();
	void ReplaceRandomNumber(double iNewRandomNumber);
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //RANDOMNUMBERCACHE_H
