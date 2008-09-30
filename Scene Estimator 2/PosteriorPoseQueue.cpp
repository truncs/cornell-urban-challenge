#include "PosteriorPoseQueue.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

PosteriorPoseQueue::PosteriorPoseQueue(int iQueueSize)
{
	/*
	Default constructor.  Puts zeros in the queue.

	INPUTS:
		iQueueSize - number of posterior pose solutions to keep

	OUTPUTS:
		none.
	*/

	InitializeCriticalSection(&mQueueLock);

	EnterCriticalSection(&mQueueLock);

	mQueueSize = iQueueSize;
	if (mQueueSize <= 0)
	{
		//check for invalid initial values
		mQueueSize = 100;
	}

	mPPQueue = new double[PPQ_PACKETSIZE*mQueueSize];

	//initialize the queue
	int i;
	int j;
	for (i = 0; i < mQueueSize; i++)
	{
		for (j = 0; j < PPQ_PACKETSIZE; j++)
		{
			mPPQueue[midx(i, j, mQueueSize)] = 0.0;
		}
	}

	mQueueTail = -1;
	mNumPacketsInQueue = 0;
	mNumBadEvents = 0;

	LeaveCriticalSection(&mQueueLock);

	return;
}

PosteriorPoseQueue::~PosteriorPoseQueue(void)
{
	/*
	Posterior pose queue destructor.  Frees memory allocated.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	EnterCriticalSection(&mQueueLock);
	DeleteCriticalSection(&mQueueLock);

	mNumPacketsInQueue = 0;

	//free memory in the posterior pose queue
	delete [] mPPQueue;

	return;
}

void PosteriorPoseQueue::ResetQueue()
{
	/*
	Resets the posterior pose queue to its original state.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	int i;
	int j;

	EnterCriticalSection(&mQueueLock);

	printf("Warning: resetting posterior pose queue...\n");

	//initialize the queue
	for (i = 0; i < mQueueSize; i++)
	{
		for (j = 0; j < PPQ_PACKETSIZE; j++)
		{
			mPPQueue[midx(i, j, mQueueSize)] = 0.0;
		}
	}

	mQueueTail = -1;
	mNumPacketsInQueue = 0;
	//and reset the number of bad packets ignored
	mNumBadEvents = 0;

	LeaveCriticalSection(&mQueueLock);

	return;
}

bool PosteriorPoseQueue::PushPacket(PosteriorPosePosition* iPosteriorPose)
{
	/*
	Pushes a packet onto the queue.  Since the queue is circular,
	this may delete the oldest element on the queue.

	INPUTS:
		iPosteriorPose - the posterior pose solution to add to the queue

	OUTPUTS:
		rSuccess - returns true if push was successful, false otherwise.
	*/

	bool rSuccess = false;

	int i;
	int j;
	double mrqt = MostRecentQueueTime();

	if (iPosteriorPose->IsValid == false)
	{
		//don't add an invalid packet to the queue
		return rSuccess;
	}

	EnterCriticalSection(&mQueueLock);

	if (iPosteriorPose->PosteriorPoseTime < mrqt)
	{
		//don't push an out of order posterior pose packet to the queue
		printf("Warning: ignoring old posterior pose solution of age %.12lg ms.\n", (mrqt - iPosteriorPose->PosteriorPoseTime)*1000.0);
		//increment the number of bad posterior pose solutions
		mNumBadEvents++;
		LeaveCriticalSection(&mQueueLock);

		if (mNumBadEvents > PPQ_NUMEVENTSB4RESET)
		{
			//reset the queue if too many bad events show up
			ResetQueue();
		}

		return rSuccess;
	}

	//if this event has an OK timestamp then reset the bad events counter
	mNumBadEvents = 0;

	//copy the posterior pose data into the queue
	double iPacket[PPQ_PACKETSIZE];
	iPacket[0] = iPosteriorPose->PosteriorPoseTime;
	iPacket[1] = iPosteriorPose->EastMMSE;
	iPacket[2] = iPosteriorPose->NorthMMSE;
	iPacket[3] = iPosteriorPose->HeadingMMSE;
	int idx = 4;
	for (i = 0; i < PPP_NUMSTATES; i++)
	{
		for (j = 0; j <= i; j++)
		{
			iPacket[idx] = iPosteriorPose->CovarianceMMSE[midx(i, j, PPP_NUMSTATES)];
			idx++;
		}
	}

	//increment the queue tail, and drop the packet at the tail
	mQueueTail = (mQueueTail + 1) % mQueueSize;
	for (j = 0; j < PPQ_PACKETSIZE; j++)
	{
		mPPQueue[midx(mQueueTail, j, mQueueSize)] = iPacket[j];
	}
	//increment the number of elements in the queue
	mNumPacketsInQueue++;
	if (mNumPacketsInQueue > mQueueSize)
	{
		//but make sure never to have more than the queue's worth
		mNumPacketsInQueue = mQueueSize;
	}

	//if code gets here, push was successful
	rSuccess = true;

	LeaveCriticalSection(&mQueueLock);

	return rSuccess;
}

bool PosteriorPoseQueue::PullPacket(double oPacket[PPQ_PACKETSIZE], double iEventTime)
{
	/*
	Pulls the packet with the timestamp closest to iEventTime.  NOTE: this
	function is not thread-safe, so it must remain private, and must be called
	from a thread-safe public function.

	INPUTS:
		oPacket - will be filled with the returned packet
		iEventTime - time at which the packet is requested

	OUTPUTS:
		oPacket - filled with the packet.  Function returns true if a packet
			was successfully pulled, false otherwise.
	*/

	if (mNumPacketsInQueue == 0)
	{
		//no packets in the queue
		return false;
	}

	//if code gets here, a packet was in the queue
	//search the queue for the best matching time and return it
	int imax;
	imax = mQueueSize;
	if (mNumPacketsInQueue < mQueueSize)
	{
		imax = mNumPacketsInQueue;
	}

	//try to find a packet that comes close to the desired time
	int i;
	double dtbest = DBL_MAX;
	int ibest = -1;
	for (i = 0; i < imax; i++)
	{
		//extract the timestamp for this element of the queue
		double ttst = mPPQueue[midx(i, 0, mQueueSize)];
		double dt = iEventTime - ttst;
		if (fabs(dt) < fabs(dtbest) && dt >= 0.0)
		{
			//found a new closer timestamp without going over
			dtbest = fabs(dt);
			ibest = i;
		}
	}

	if (ibest == -1 || dtbest > PPQ_MAXPACKETOFFSET)
	{
		//couldn't find a valid packet
		return false;
	}

	int j;
	for (j = 0; j < PPQ_PACKETSIZE; j++)
	{
		oPacket[j] = mPPQueue[midx(ibest, j, mQueueSize)];
	}

	return true;
}

void PosteriorPoseQueue::GetPosteriorPose(PosteriorPosePosition* oPosteriorPose, double iPosteriorPoseTime)
{
	/*
	Returns the posterior pose solution at the desired time.  The posterior pose 
	solution is returned prior to the desired time, so that it may be predicted forward.

	INPUTS:
		oPosteriorPose - will be populated with the posterior pose solution at output
		iPosteriorPoseTime - starting time of relative pose transformation

	OUTPUTS:
		oPosteriorPose - will be populated with the posterior pose solution at output.
			Check the IsValid flag to see whether the values are valid / the retrieval
			was successful.
	*/

	int i;
	int j;

	EnterCriticalSection(&mQueueLock);

	//find the closest posterior pose packet
	double pp_packet[PPQ_PACKETSIZE];
	if (PullPacket(pp_packet, iPosteriorPoseTime) == false)
	{
		//couldn't find a packet
		oPosteriorPose->IsValid = false;
		LeaveCriticalSection(&mQueueLock);
		return;
	}

	//store the posterior pose solution in the output argument
	oPosteriorPose->IsValid = true;
	oPosteriorPose->PosteriorPoseTime = pp_packet[0];
	oPosteriorPose->EastMMSE = pp_packet[1];
	oPosteriorPose->NorthMMSE = pp_packet[2];
	oPosteriorPose->HeadingMMSE = pp_packet[3];
	int idx = 4;
	for (i = 0; i < PPP_NUMSTATES; i++)
	{
		for (j = 0; j <= i; j++)
		{
			oPosteriorPose->CovarianceMMSE[midx(i, j, PPP_NUMSTATES)] = pp_packet[idx];
			idx++;
		}
	}
	for (i = 0; i < PPP_NUMSTATES; i++)
	{
		for (j = i+1; j < PPP_NUMSTATES; j++)
		{
			oPosteriorPose->CovarianceMMSE[midx(i, j, PPP_NUMSTATES)] = 
				oPosteriorPose->CovarianceMMSE[midx(j, i, PPP_NUMSTATES)];
		}
	}

	LeaveCriticalSection(&mQueueLock);

	return;
}

double PosteriorPoseQueue::LeastRecentQueueTime()
{
	/*
	Returns the oldest time in the queue

	INPUTS:
		none.

	OUTPUTS:
		rOldestTime - the oldest time in the queue
	*/

	int idx;
	double rOldestTime = DBL_MAX;

	EnterCriticalSection(&mQueueLock);

	if (mNumPacketsInQueue > 0)
	{
		if (mNumPacketsInQueue < mQueueSize)
		{
			//queue hasn't filled up yet and rolled over
			idx = 0;
		}
		else
		{
			//queue has filled up
			idx = (mQueueTail + 1) % mQueueSize;
		}

		rOldestTime = mPPQueue[midx(idx, 0, mQueueSize)];
	}

	LeaveCriticalSection(&mQueueLock);

	return rOldestTime;
}

double PosteriorPoseQueue::MostRecentQueueTime()
{
	/*
	Returns the newest time in the queue

	INPUTS:
		none.

	OUTPUTS:
		rNewestTime - the newest time in the queue
	*/

	double rNewestTime = -DBL_MAX;

	EnterCriticalSection(&mQueueLock);

	if (mNumPacketsInQueue > 0)
	{
		//if queue has packets, then mQueueTail is the most recent packet
		rNewestTime = mPPQueue[midx(mQueueTail, 0, mQueueSize)];
	}

	LeaveCriticalSection(&mQueueLock);

	return rNewestTime;
}
