#include "RandomNumberGenerator.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

RandomNumberGenerator::RandomNumberGenerator(void)
{
	/*
	Default constructor for random number generator.  Does NOT initialize the generator.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	//initialize the blocking mutex
	InitializeCriticalSection(&mRNGLock);

	EnterCriticalSection(&mRNGLock);

	//generator hasn't been seeded yet
	mIsSeeded = false;
	mSeed = NULL;
	mState = NULL;

	//initialize and seed the C random number generator
	srand((unsigned)time(NULL));

	LeaveCriticalSection(&mRNGLock);

	return;
}

RandomNumberGenerator::~RandomNumberGenerator(void)
{
	/*
	Destructor for random number generator.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	EnterCriticalSection(&mRNGLock);
	DeleteCriticalSection(&mRNGLock);

	//free memory associated with random number generator
	delete [] mSeed;
	delete [] mState;

	return;
}

int RandomNumberGenerator::GeneratorType(void)
{
	/*
	Accessor function for the type of generator used.

	INPUTS:
		none.

	OUTPUTS:
		returns mGeneratorType
	*/

	int rGeneratorType;

	EnterCriticalSection(&mRNGLock);
	rGeneratorType = mGeneratorType;
	LeaveCriticalSection(&mRNGLock);

	return rGeneratorType;
}

bool RandomNumberGenerator::IsSeeded(void)
{
	/*
	Accessor function for whether the generator is seeded or not

	INPUTS:
		none.

	OUTPUTS:
		returns true if the generator is seeded, false otherwise.
	*/

	bool rIsSeeded;

	EnterCriticalSection(&mRNGLock);
	rIsSeeded = mIsSeeded;
	LeaveCriticalSection(&mRNGLock);

	return rIsSeeded;
}

bool RandomNumberGenerator::SeedGenerator(int iGeneratorType)
{
	/*
	Initializes and seeds the random number generator with a random seed.

	INPUTS:
		iGeneratorType - type of random number generator to use.  Valid values are:
			RAND_NAG - NAG generator, good basic-use generator
			RAND_WICHMANNHILL - Wichmann-Hill generator, good for generating multiple streams of random numbers
			RAND_MERSENNETWISTER - Mersenne Twister generator, good basic-use generator with longer period than NAG
			RAND_LECUYERCOMBINEDRECURSIVE - L'Ecuyer Combined Recursive generator, good for large numbers of multiple streams of random numbes
	
	OUTPUTS:
		Returns true if the seeding was successful, false otherwise.
	*/

	int i;

	EnterCriticalSection(&mRNGLock);

	mIsSeeded = false;
	printf("Seeding random number generator randomly...\n");

	switch (iGeneratorType)
	{
	case RAND_NAG:
		{
			mSubId = 0;
			//NAG only needs one seed value
			mLSeed = 1;
			//NAG has 16 states
			mLState = 16;
		}
		break;

	case RAND_WICHMANNHILL:
		{
			//choose a random Wichmann-Hill generator ID from 1 to 273
			mSubId = (int) (((double) rand()) / ((double) RAND_MAX) * 272.0 + 1.0);
			//Wichmann-Hill needs 4 seed values
			mLSeed = 4;
			//Wichmann-Hill has 20 states
			mLState = 20;
		}
		break;

	case RAND_MERSENNETWISTER:
		{
			mSubId = 0;
			//Mersenne Twister needs 624 seed values
			mLSeed = 624;
			//Mersenne Twister has 633 states
			mLState = 633;
		}
		break;

	case RAND_LECUYERCOMBINEDRECURSIVE:
		{
			mSubId = 0;
			//L'Ecuyer needs 6 seed values
			mLSeed = 6;
			//Mersenne Twister has 61 states
			mLState = 61;
		}
		break;

	default:
		//default to no seed
		printf("Warning: random number generator not seeded successfully.\n");
		LeaveCriticalSection(&mRNGLock);
		return false;
	}

	mGeneratorType = iGeneratorType;

	//draw seed values randomly
	mSeed = new int[mLSeed];
	for (i = 0; i < mLSeed; i++)
	{
		//note: ACML expects positive integer seeds
		mSeed[i] = rand() + 1;
	}
	mState = new int[mLState];

	//write the seed to a seed file
	errno_t err;
	FILE* SeedFile;
	const char* seedname = "seed.txt";
	err = fopen_s(&SeedFile, seedname, "w");
	if (err == 0)
	{
		//success in opening the seed output file
		//write the seed to the file

		//write the name of the generator used
		fprintf(SeedFile, "%d\n", mGeneratorType);
		fprintf(SeedFile, "%d\n", mSubId);
		fprintf(SeedFile, "%d\n", mLSeed);
		fprintf(SeedFile, "%d\n", mLState);
		for (i = 0; i < mLSeed; i++)
		{
			fprintf(SeedFile, "%d\n", mSeed[i]);
		}
		printf("Random number seed saved to file \"%s\".\n", seedname);
	}
	fclose(SeedFile);

	int info;
	drandinitialize(mGeneratorType, mSubId, mSeed, &mLSeed, mState, &mLState, &info);
	if (info != 0)
	{
		printf("Warning: random number generator not seeded successfully.\n");
		LeaveCriticalSection(&mRNGLock);
		return false;
	}

	printf("Random number generator seeded successfully.\n");
	mIsSeeded = true;

	LeaveCriticalSection(&mRNGLock);

	return true;
}

bool RandomNumberGenerator::SeedGeneratorFromFile(char* iSeedFile)
{
	/*
	Initializes and seeds the random number generator with a random seed.

	INPUTS:
		iSeedFile - name of the file to open as the random number generator seed.
	
	OUTPUTS:
		Returns true if the seeding was successful, false otherwise.
	*/

	int i;

	EnterCriticalSection(&mRNGLock);

	mIsSeeded = false;
	printf("Seeding random number generator from file \"%s\"...\n", iSeedFile);

	//attempt to open the seed file
	FILE* SeedFile;
	errno_t err;
	err = fopen_s(&SeedFile, iSeedFile, "r");

	if (err != 0)
	{
		//could not open the seed file correctly
		printf("Warning: random number generator not seeded successfully.\n");
		LeaveCriticalSection(&mRNGLock);
		return false;
	}

	//opened seed file: read generator data
	int nr = 0;
	nr += fscanf_s(SeedFile, "%d\n", &mGeneratorType);
	nr += fscanf_s(SeedFile, "%d\n", &mSubId);
	nr += fscanf_s(SeedFile, "%d\n", &mLSeed);
	nr += fscanf_s(SeedFile, "%d\n", &mLState);

	if (nr != 4)
	{
		//couldn't read the initialization correctly
		printf("Warning: random number generator not seeded successfully.\n");
		fclose(SeedFile);
		LeaveCriticalSection(&mRNGLock);
		return false;
	}

	//read seed values from the file
	mSeed = new int[mLSeed];
	for (i = 0; i < mLSeed; i++)
	{
		//note: ACML expects positive integer seeds
		nr = fscanf_s(SeedFile, "%d\n", &(mSeed[i]));
		if (nr != 1)
		{
			printf("Warning: random number generator not seeded successfully.\n");
			fclose(SeedFile);
			LeaveCriticalSection(&mRNGLock);
			return false;
		}
	}
	mState = new int[mLState];
	fclose(SeedFile);

	int info;
	drandinitialize(mGeneratorType, mSubId, mSeed, &mLSeed, mState, &mLState, &info);
	if (info != 0)
	{
		printf("Warning: random number generator not seeded successfully.\n");
		LeaveCriticalSection(&mRNGLock);
		return false;
	}

	printf("Random number generator seeded successfully.\n");
	mIsSeeded = true;
	LeaveCriticalSection(&mRNGLock);

	return true;
}

double RandomNumberGenerator::RandUniform(void)
{
	/*
	Draws a uniform random number using the initialized random number generators.
	Random number drawn on the open interval (0, 1)

	INPUTS:
		none.

	OUTPUTS:
		A quasi-random number on uniform (0, 1).
	*/

	double rnd;

	EnterCriticalSection(&mRNGLock);

	if (mIsSeeded == false)
	{
		//called without being seeded: use the C rng

		printf("Warning: random number generator used without being initialized.\n");
		rnd = ((double) rand()) / ((double) RAND_MAX);

		LeaveCriticalSection(&mRNGLock);

		return rnd;
	}

	//try to generate the random number
	int info;
	dranduniform(1, 0.0, 1.0, mState, &rnd, &info);

	if (info != 0)
	{
		printf("Warning: random number generator error.  Defaulting to rand()...\n");
		rnd = ((double) rand()) / ((double) RAND_MAX);
	}

	LeaveCriticalSection(&mRNGLock);

	return rnd;
}

double RandomNumberGenerator::RandGaussian(void)
{
	/*
	Draws a Gaussian pseudorandom number from the unit normal ~N(0, 1).

	INPUTS:
		none.

	OUTPUTS:
		A pseudorandom number drawn from the unit normal.
	*/

	double rnd;

	EnterCriticalSection(&mRNGLock);

	if (mIsSeeded == false)
	{
		//called without being seeded: return a unit gaussian from rand()

		printf("Warning: random number generator used without being initialized.\n");
		rnd = CRandGaussian();

		LeaveCriticalSection(&mRNGLock);
		return rnd;
	}

	int info;
	drandgaussian(1, 0.0, 1.0, mState, &rnd, &info);

	if (info != 0)
	{
		printf("Warning: random number generator error.  Defaulting to rand()...\n");

		//if the generator fails, do a box-muller transform
		rnd = CRandGaussian();
	}

	LeaveCriticalSection(&mRNGLock);

	return rnd;
}

double RandomNumberGenerator::CRandUniform()
{
	/*
	Generates a uniform pseudo-random number using the C rand() function.
	Should only be used if ACML barfs.

	INPUTS:
		none.

	OUTPUTS:
		rRand - a uniform pseudo-random number.
	*/

	double rfact = 1.0 / ((double) RAND_MAX);
	double rRand = ((double) rand()) * rfact;

	return rRand;
}

double RandomNumberGenerator::CRandGaussian()
{
	/*
	Generates a unit-gaussian pseudo-random number using the C rand() function.
	Should only be used if ACML barfs.

	INPUTS:
		none.

	OUTPUTS:
		rRand - a unit-gaussian pseudo-random number.
	*/

	double rRand = 0.0;

	double x1;
	double x2;
	double w = 2.0;
	double y1;
	double y2;
	double rfact = 1.0 / ((double) RAND_MAX);

	while (w >= 1.0)
	{
		//polar implementation of box-muller transform on uniform random numbers
		x1 = 2.0*((double) rand())*rfact - 1.0;
		x2 = 2.0*((double) rand())*rfact - 1.0;
		w = x1*x1 + x2*x2;
	}

	w = sqrt(-2.0*log(w) / w);
	y1 = x1*w;
	y2 = x2*w;

	rRand = y1;

	return rRand;
}
