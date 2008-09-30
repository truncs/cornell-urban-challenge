#ifndef RANDOMNUMBERGENERATOR_H
#define RANDOMNUMBERGENERATOR_H

#include "acml.h"

#include <MATH.H>
#include <STDIO.H>
#include <STDLIB.H>
#include <TIME.H>
#include <WINDOWS.H>

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

//constants associated with random number generator

//type of random number generator
#define RAND_NAG 1
#define RAND_WICHMANNHILL 2
#define RAND_MERSENNETWISTER 3
#define RAND_LECUYERCOMBINEDRECURSIVE 4

class RandomNumberGenerator
{
	//A class used to generate random numbers.  Interfaces with the AMD ACML.
	//NOTE: this is thread-safe.

private:

	int mGeneratorType;
	int mSubId;
	int* mSeed;
	int mLSeed;
	int* mState;
	int mLState;

	bool mIsSeeded;

	CRITICAL_SECTION mRNGLock;

	double CRandUniform();
	double CRandGaussian();

public:

	RandomNumberGenerator();
	~RandomNumberGenerator();

	int GeneratorType();
	bool IsSeeded();
	bool SeedGenerator(int iGeneratorType);
	bool SeedGeneratorFromFile(char* iSeedFile);
	double RandUniform();
	double RandGaussian();
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //RANDOMNUMBERGENERATOR_H
