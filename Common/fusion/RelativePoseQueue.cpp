#include "RelativePoseQueue.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

RelativePoseQueue::RelativePoseQueue(int iQueueSize, double iWXVar, double iWYVar, double iWZVar, double iVXVar, double iVYVar, double iVZVar)
{
	/*
	Default constructor.  Puts zeros in the queue.

	INPUTS:
		iQueueSize - number of odometry packets to keep
		iWXVar, iWYVar, iWZVar - continuous white noise variance on rates of rotation
		iVXVar, iVYVar, iVZVar - continuous white noise variance on velocities

	OUTPUTS:
		none.
	*/

	InitializeCriticalSection(&mQueueLock);

	EnterCriticalSection(&mQueueLock);

	//copy over the instantaneous white noise variances
	mWXVar = iWXVar;
	mWYVar = iWYVar;
	mWZVar = iWZVar;
	mVXVar = iVXVar;
	mVYVar = iVYVar;
	mVZVar = iVZVar;

	mQueueSize = iQueueSize;
	if (mQueueSize <= 0)
	{
		//check for invalid initial values
		mQueueSize = 100;
	}

	mRPPQueue = new double[RPQ_PACKETSIZE*mQueueSize];

	//initialize the queue
	int i;
	int j;
	for (i = 0; i < mQueueSize; i++)
	{
		for (j = 0; j < RPQ_PACKETSIZE; j++)
		{
			mRPPQueue[midx(i, j, mQueueSize)] = 0.0;
		}
	}

	mQueueTail = -1;
	mNumPacketsInQueue = 0;
	mNumOldEventsIgnored = 0;

	LeaveCriticalSection(&mQueueLock);

	return;
}

RelativePoseQueue::~RelativePoseQueue(void)
{
	/*
	Relative pose queue destructor.  Frees memory allocated.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	EnterCriticalSection(&mQueueLock);
	DeleteCriticalSection(&mQueueLock);

	mNumPacketsInQueue = 0;

	//free memory in the odometry queue
	delete [] mRPPQueue;

	return;
}

void RelativePoseQueue::ResetQueue()
{
	/*
	Resets the queue to its initial state.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	int i;
	int j;

	EnterCriticalSection(&mQueueLock);

	printf("Warning: resetting relative pose queue...\n");
	
	//delete all relative pose packets
	for (i = 0; i < mQueueSize; i++)
	{
		for (j = 0; j < RPQ_PACKETSIZE; j++)
		{
			mRPPQueue[midx(i, j, mQueueSize)] = 0.0;
		}
	}

	mQueueTail = -1;
	mNumPacketsInQueue = 0;
	//and reset the number of old events ignored
	mNumOldEventsIgnored = 0;

	LeaveCriticalSection(&mQueueLock);

	return;
}

bool RelativePoseQueue::PushPacket(const pose_rel_msg& iOdomMessage)
{
	/*
	Overloaded push method for pushing the actual odometry data structure.

	INPUTS:
		iOdomMessage - the input odometry message to copy

	OUTPUTS:
		rSuccess - true if packet push was successful, false otherwise.
	*/

	double iEventData[RPQ_PACKETSIZE];

	//timestamp
	iEventData[0] = (double)(iOdomMessage.car_ts_secs) + (double)(iOdomMessage.car_ts_ticks)/10000.0;
	//integration time dt
	iEventData[1] = iOdomMessage.dt;
	//elements of the transformation matrix
	iEventData[2] = iOdomMessage.Rinit2veh[0][0];
	iEventData[3] = iOdomMessage.Rinit2veh[1][0];
	iEventData[4] = iOdomMessage.Rinit2veh[2][0];
	iEventData[5] = iOdomMessage.Rinit2veh[0][1];
	iEventData[6] = iOdomMessage.Rinit2veh[1][1];
	iEventData[7] = iOdomMessage.Rinit2veh[2][1];
	iEventData[8] = iOdomMessage.Rinit2veh[0][2];
	iEventData[9] = iOdomMessage.Rinit2veh[1][2];
	iEventData[10] = iOdomMessage.Rinit2veh[2][2];
	iEventData[11] = iOdomMessage.Rinit2veh[0][3];
	iEventData[12] = iOdomMessage.Rinit2veh[1][3];
	iEventData[13] = iOdomMessage.Rinit2veh[2][3];

	return PushPacket(iEventData);
}

bool RelativePoseQueue::PushPacket(double iPacket[RPQ_PACKETSIZE])
{
	/*
	Pushes a packet onto the queue.  Since the queue is circular,
	this may delete the oldest element on the queue.

	INPUTS:
		iPacket - the packet to add to the queue

	OUTPUTS:
		rSuccess - returns true if push was successful, false otherwise.
	*/

	bool rSuccess = false;
	double mrqt = MostRecentQueueTime();

	EnterCriticalSection(&mQueueLock);

	//check the event for a valid timestamp
	if (iPacket[0] < Q_MINTIMESTAMP || iPacket[0] > Q_MAXTIMESTAMP)
	{
		printf("Warning: ignoring type %d event with timestamp %.12lg.\n", ODOM_EVENT, iPacket[0]);
		LeaveCriticalSection(&mQueueLock);

		return rSuccess;
	}

	if (iPacket[0] < mrqt)
	{
		//don't push an out of order odometry packet to the queue
		printf("Warning: ignoring old type %d event of age %.12lg ms.\n", ODOM_EVENT, (mrqt - iPacket[0])*1000.0);

		mNumOldEventsIgnored++;
		LeaveCriticalSection(&mQueueLock);

		if (mNumOldEventsIgnored > RPQ_NUMEVENTSB4RESET)
		{
			//reset the queue if too many bad packets show up
			ResetQueue();
		}

		return rSuccess;
	}

	//if this event has an OK timestamp then reset the "ignored old events" counter
	mNumOldEventsIgnored = 0;

	//increment the queue tail, and drop the packet at the tail
	mQueueTail = (mQueueTail + 1) % mQueueSize;
	int j;
	for (j = 0; j < RPQ_PACKETSIZE; j++)
	{
		mRPPQueue[midx(mQueueTail, j, mQueueSize)] = iPacket[j];
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

bool RelativePoseQueue::PullPacket(double oPacket[RPQ_PACKETSIZE], double iEventTime, int iQueryMode)
{
	/*
	Pulls the packet with the timestamp closest to iEventTime under certain criteria.
	NOTE: this function is not thread-safe, so it must remain private, and must be 
	called from a thread-safe public function.

	INPUTS:
		oPacket - will be filled with the returned packet
		iEventTime - time at which the packet is requested
		iQueryMode - determines the behavior of how packets are pulled.  Can be:
			RPQ_PULLPACKETBEFORETS - pulls the packet closest to but before the desired
				timestamp.
			RPQ_PULLPACKETAFTERTS - pulls the packet closest to but after the desired
				timestamp.
			RPQ_PULLPACKETCLOSESTTS - pulls the packet closest to the desired timestamp.

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

	//try to find a relative pose packet that comes just before the desired time
	int i;
	double dtbest = DBL_MAX;
	int ibest = -1;
	for (i = 0; i < imax; i++)
	{
		//extract the timestamp for this element of the queue
		double ttst = mRPPQueue[midx(i, 0, mQueueSize)];
		double dt = iEventTime - ttst;
		if (fabs(dt) < fabs(dtbest))
		{
			//found a new closer timestamp

			switch (iQueryMode)
			{
			case RPQ_PULLPACKETBEFORETS:
				//pull a packet before the desired timestamp
				if (dt > 0.0)
				{
					//found a new closer timestamp that happened before the desired time
					dtbest = fabs(dt);
					ibest = i;
				}
				break;

			case RPQ_PULLPACKETAFTERTS:
				//pull a packet after the desired timestamp
				if (dt <= 0.0)
				{
					//found a new closer timestamp that happened after the desired time
					dtbest = fabs(dt);
					ibest = i;
				}
				break;

			case RPQ_PULLPACKETCLOSESTTS:
				//pull a packet closest to the desired timestamp
				//found a new closer timestamp
				dtbest = fabs(dt);
				ibest = i;
				break;

			}
		}
	}

	if (ibest == -1 || dtbest > RPQ_MAXPACKETOFFSET)
	{
		//couldn't find a valid odometry packet
		return false;
	}

	int j;
	for (j = 0; j < RPQ_PACKETSIZE; j++)
	{
		oPacket[j] = mRPPQueue[midx(ibest, j, mQueueSize)];
	}

	return true;
}

void RelativePoseQueue::GetVehicleOdometry(VehicleOdometry* oVehicleOdometry, double iStartTime, double iEndTime)
{
	/*
	Returns average vehicle odometry over a specified time window.

	INPUTS:
		oVehicleOdometry - will be populated with the vehicle's odometry at output
		iStartTime - starting time of relative pose transformation
		iEndTime - ending time of relative pose transformation

	OUTPUTS:
		none.  Sets values in oVehicleOdometry appropriately.  Check the IsValid flag
			to see whether the values are valid
	*/

	int i;
	int j;
	int k;

	//declare variables that will store the output values
	double dt;
	double wx;
	double wy;
	double wz;
	double vx;
	double vy;
	double vz;
	double qwx;
	double qwy;
	double qwz;
	double qvx;
	double qvy;
	double qvz;

	EnterCriticalSection(&mQueueLock);

	//set vehicle odometry process noises
	qvx = mVXVar;
	qvy = mVYVar;
	qvz = mVZVar;
	qwx = mWXVar;
	qwy = mWYVar;
	qwz = mWZVar;

	//find the two pertinent odometry buffers
	double start_packet[RPQ_PACKETSIZE];
	if (PullPacket(start_packet, iStartTime, RPQ_PULLPACKETBEFORETS) == false)
	{
		//find an odometry packet before the start time
		oVehicleOdometry->IsValid = false;
		LeaveCriticalSection(&mQueueLock);
		return;
	}
	double end_packet[RPQ_PACKETSIZE];
	if (PullPacket(end_packet, iEndTime, RPQ_PULLPACKETAFTERTS) == false)
	{
		//find an odometry packet after the end time
		oVehicleOdometry->IsValid = false;
		LeaveCriticalSection(&mQueueLock);
		return;
	}

	//extract the current odometry time
	double tk = end_packet[0];

	double Ri2vk[16];
	//extract the current transformation matrix
	Ri2vk[midx(0, 0, 4)] = end_packet[2];
	Ri2vk[midx(1, 0, 4)] = end_packet[3];
	Ri2vk[midx(2, 0, 4)] = end_packet[4];
	Ri2vk[midx(3, 0, 4)] = 0.0;
	Ri2vk[midx(0, 1, 4)] = end_packet[5];
	Ri2vk[midx(1, 1, 4)] = end_packet[6];
	Ri2vk[midx(2, 1, 4)] = end_packet[7];
	Ri2vk[midx(3, 1, 4)] = 0.0;
	Ri2vk[midx(0, 2, 4)] = end_packet[8];
	Ri2vk[midx(1, 2, 4)] = end_packet[9];
	Ri2vk[midx(2, 2, 4)] = end_packet[10];
	Ri2vk[midx(3, 2, 4)] = 0.0;
	Ri2vk[midx(0, 3, 4)] = end_packet[11];
	Ri2vk[midx(1, 3, 4)] = end_packet[12];
	Ri2vk[midx(2, 3, 4)] = end_packet[13];
	Ri2vk[midx(3, 3, 4)] = 1.0;

	//extract the old odometry time
	double tkmo = start_packet[0];
	double Ri2vkmo[16];
	//extract the old transformation matrix
	Ri2vkmo[midx(0, 0, 4)] = start_packet[2];
	Ri2vkmo[midx(1, 0, 4)] = start_packet[3];
	Ri2vkmo[midx(2, 0, 4)] = start_packet[4];
	Ri2vkmo[midx(3, 0, 4)] = 0.0;
	Ri2vkmo[midx(0, 1, 4)] = start_packet[5];
	Ri2vkmo[midx(1, 1, 4)] = start_packet[6];
	Ri2vkmo[midx(2, 1, 4)] = start_packet[7];
	Ri2vkmo[midx(3, 1, 4)] = 0.0;
	Ri2vkmo[midx(0, 2, 4)] = start_packet[8];
	Ri2vkmo[midx(1, 2, 4)] = start_packet[9];
	Ri2vkmo[midx(2, 2, 4)] = start_packet[10];
	Ri2vkmo[midx(3, 2, 4)] = 0.0;
	Ri2vkmo[midx(0, 3, 4)] = start_packet[11];
	Ri2vkmo[midx(1, 3, 4)] = start_packet[12];
	Ri2vkmo[midx(2, 3, 4)] = start_packet[13];
	Ri2vkmo[midx(3, 3, 4)] = 1.0;

	//create Rvkmo2vk, the incremental transformation matrix
	double invRi2vkmo[16];
	for (i = 0; i < 3; i++)
	{
		for (j = 0; j < 3; j++)
		{
			//upper left 3x3 block is just transposed
			invRi2vkmo[midx(i, j, 4)] = Ri2vkmo[midx(j, i, 4)];
		}
	}
	//last row is [0, 0, 0, 1]
	invRi2vkmo[midx(3, 0, 4)] = 0.0;
	invRi2vkmo[midx(3, 1, 4)] = 0.0;
	invRi2vkmo[midx(3, 2, 4)] = 0.0;
	invRi2vkmo[midx(3, 3, 4)] = 1.0;
	//last col is -R'*d
	for (i = 0; i < 3; i++)
	{
		invRi2vkmo[midx(i, 3, 4)] = 0.0;
		for (k = 0; k < 3; k++)
		{
			invRi2vkmo[midx(i, 3, 4)] -= Ri2vkmo[midx(k, i, 4)] * Ri2vkmo[midx(k, 3, 4)];
		}
	}

	//Rvkmo2vk = Ri2vk*inv(Ri2vkmo);
	double Rvkmo2vk[16];
	for (i = 0; i < 4; i++)
	{
		for (j = 0; j < 4; j++)
		{
			Rvkmo2vk[midx(i, j, 4)] = 0.0;
			for (k = 0; k < 4; k++)
			{
				Rvkmo2vk[midx(i, j, 4)] += Ri2vk[midx(i, k, 4)] * invRi2vkmo[midx(k, j, 4)];
			}
		}
	}

	//extract inverse kinematics from sequential transformation matrix
	double sindy = Rvkmo2vk[midx(2, 0, 4)];
	double cosdy = sqrt(Rvkmo2vk[midx(2, 1, 4)]*Rvkmo2vk[midx(2, 1, 4)] 
		+ Rvkmo2vk[midx(2, 2, 4)]*Rvkmo2vk[midx(2, 2, 4)]);
	double sindx = -Rvkmo2vk[midx(2, 1, 4)] / cosdy;
	double cosdx = Rvkmo2vk[midx(2, 2, 4)] / cosdy;
	double sindz = -Rvkmo2vk[midx(1, 0, 4)] / cosdy;
	double cosdz = Rvkmo2vk[midx(0, 0, 4)] / cosdy;
	//calculate average vehicle odometry
	dt = tk - tkmo;

	if (dt > 0.0)
	{
		//set average vehicle odometry
		wx = atan2(sindx, cosdx) / dt;
		wy = atan2(sindy, cosdy) / dt;
		wz = atan2(sindz, cosdz) / dt;

		vx = -(Rvkmo2vk[midx(0, 0, 4)]*Rvkmo2vk[midx(0, 3, 4)] 
			+ Rvkmo2vk[midx(1, 0, 4)]*Rvkmo2vk[midx(1, 3, 4)] 
			+ Rvkmo2vk[midx(2, 0, 4)]*Rvkmo2vk[midx(2, 3, 4)]) / dt;
		vy = -(Rvkmo2vk[midx(0, 1, 4)]*Rvkmo2vk[midx(0, 3, 4)] 
			+ Rvkmo2vk[midx(1, 1, 4)]*Rvkmo2vk[midx(1, 3, 4)] 
			+ Rvkmo2vk[midx(2, 1, 4)]*Rvkmo2vk[midx(2, 3, 4)]) / dt;
		vz = -(Rvkmo2vk[midx(0, 2, 4)]*Rvkmo2vk[midx(0, 3, 4)] 
			+ Rvkmo2vk[midx(1, 2, 4)]*Rvkmo2vk[midx(1, 3, 4)] 
			+ Rvkmo2vk[midx(2, 2, 4)]*Rvkmo2vk[midx(2, 3, 4)]) / dt;
	}
	else
	{
		//set odometry to zero and return a false packet

		wx = 0.0;
		wy = 0.0;
		wz = 0.0;
		vx = 0.0;
		vy = 0.0;
		vz = 0.0;

		oVehicleOdometry->IsValid = false;
		LeaveCriticalSection(&mQueueLock);
		return;
	}

	//store the information into the output argument
	oVehicleOdometry->IsValid = true;
	oVehicleOdometry->EndTime = tk;
	oVehicleOdometry->StartTime = tkmo;
	oVehicleOdometry->dt = dt;
	oVehicleOdometry->wx = wx;
	oVehicleOdometry->wy = wy;
	oVehicleOdometry->wz = wz;
	oVehicleOdometry->vx = vx;
	oVehicleOdometry->vy = vy;
	oVehicleOdometry->vz = vz;
	oVehicleOdometry->qwx = qwx;
	oVehicleOdometry->qwy = qwy;
	oVehicleOdometry->qwz = qwz;
	oVehicleOdometry->qvx = qvx;
	oVehicleOdometry->qvy = qvy;
	oVehicleOdometry->qvz = qvz;

	for (i = 0; i < 4; i++)
	{
		for (j = 0; j < 4; j++)
		{
			oVehicleOdometry->Rvkmo2vk[midx(i, j, 4)] = Rvkmo2vk[midx(i, j, 4)];
		}
	}

	LeaveCriticalSection(&mQueueLock);

	return;
}

double RelativePoseQueue::LeastRecentQueueTime()
{
	/*
	Returns the oldest time in the odometry queue

	INPUTS:
		none.

	OUTPUTS:
		rOldestTime - the oldest time in the odometry queue
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

		rOldestTime = mRPPQueue[midx(idx, 0, mQueueSize)];
	}

	LeaveCriticalSection(&mQueueLock);

	return rOldestTime;
}

double RelativePoseQueue::MostRecentQueueTime()
{
	/*
	Returns the newest time in the odometry queue

	INPUTS:
		none.

	OUTPUTS:
		rNewestTime - the newest time in the odometry queue
	*/

	double rNewestTime = -DBL_MAX;

	EnterCriticalSection(&mQueueLock);

	if (mNumPacketsInQueue > 0)
	{
		//if queue has packets, then mQueueTail is the most recent packet
		rNewestTime = mRPPQueue[midx(mQueueTail, 0, mQueueSize)];
	}

	LeaveCriticalSection(&mQueueLock);

	return rNewestTime;
}

bool RelativePoseQueue::GetVehicleTransformation(VehicleTransformation& oVehicleTransformation, double iStartTime, double iEndTime)
{
	/*
	Overloaded function for retrieving the vehicle transformation using a structure
	passed by reference instead of a pointer.

	INPUTS:
		oVehicleTransformation - the vehicle transformation structure, passed by reference.
		iStartTime - starting time of the transformation.
		iEndTime - ending time of the transformation

	OUTPUTS:
		rSuccess - true if the vehicle transformation is obtained successfully, false otherwise.
			If true, oVehicleTransformation is populated with the vehicle transformation.
	*/

	return GetVehicleTransformation(&oVehicleTransformation, iStartTime, iEndTime);
}

bool RelativePoseQueue::GetVehicleTransformation(VehicleTransformation* oVehicleTransformation, double iStartTime, double iEndTime)
{
	/*
	Retrieves the vehicle transformation matrix to transform points from vehicle 
	coordinates at time iStartTime to vehicle coordinates at iEndTime.  Transformation
	is done by calculating:

		[new x; new y; new z; 1] = oRelativeMatrix * [old x; old y; old z; 1]

	INPUTS:
		oVehicleTransformation - vehicle transformation structure that describes
			the relative transformation between iStartTime and iEndTime on output.
		iStartTime - starting time of relative pose transformation
		iEndTime - ending time of relative pose transformation

	OUTPUTS:
		rSuccess - true if the transformation was successfully retrieved, false otherwise.
			If false, oVehicleTransformation is set to identity, otherwise it is set to 
			the desired transformation matrix.
	*/

	bool rSuccess = false;

	int i;
	int j;
	int k;

	oVehicleTransformation->IsValid = false;

	//initialize oRelativeMatrix to identity
	for (i = 0; i < 4; i++)
	{
		for (j = 0; j < 4; j++)
		{
			if (i == j)
			{
				oVehicleTransformation->T[midx(i, j, 4)] = 1.0;
			}
			else
			{
				oVehicleTransformation->T[midx(i, j, 4)] = 0.0;
			}
		}
	}

	EnterCriticalSection(&mQueueLock);

	//find the two pertinent odometry buffers
	double start_packet[RPQ_PACKETSIZE];
	if (PullPacket(start_packet, iStartTime, RPQ_PULLPACKETBEFORETS) == false)
	{
		//failed to find an odometry packet near the desired start time
		LeaveCriticalSection(&mQueueLock);
		return rSuccess;
	}
	double end_packet[RPQ_PACKETSIZE];
	if (PullPacket(end_packet, iEndTime, RPQ_PULLPACKETBEFORETS) == false)
	{
		//failed to find an odometry packet near the desired end time
		LeaveCriticalSection(&mQueueLock);
		return rSuccess;
	}

	//extract the current odometry time
	double tk = end_packet[0];

	double Ri2vk[16];
	//extract the current transformation matrix
	Ri2vk[midx(0, 0, 4)] = end_packet[2];
	Ri2vk[midx(1, 0, 4)] = end_packet[3];
	Ri2vk[midx(2, 0, 4)] = end_packet[4];
	Ri2vk[midx(3, 0, 4)] = 0.0;
	Ri2vk[midx(0, 1, 4)] = end_packet[5];
	Ri2vk[midx(1, 1, 4)] = end_packet[6];
	Ri2vk[midx(2, 1, 4)] = end_packet[7];
	Ri2vk[midx(3, 1, 4)] = 0.0;
	Ri2vk[midx(0, 2, 4)] = end_packet[8];
	Ri2vk[midx(1, 2, 4)] = end_packet[9];
	Ri2vk[midx(2, 2, 4)] = end_packet[10];
	Ri2vk[midx(3, 2, 4)] = 0.0;
	Ri2vk[midx(0, 3, 4)] = end_packet[11];
	Ri2vk[midx(1, 3, 4)] = end_packet[12];
	Ri2vk[midx(2, 3, 4)] = end_packet[13];
	Ri2vk[midx(3, 3, 4)] = 1.0;

	//extract the old odometry time
	double tkmo = start_packet[0];
	double Ri2vkmo[16];
	//extract the old transformation matrix
	Ri2vkmo[midx(0, 0, 4)] = start_packet[2];
	Ri2vkmo[midx(1, 0, 4)] = start_packet[3];
	Ri2vkmo[midx(2, 0, 4)] = start_packet[4];
	Ri2vkmo[midx(3, 0, 4)] = 0.0;
	Ri2vkmo[midx(0, 1, 4)] = start_packet[5];
	Ri2vkmo[midx(1, 1, 4)] = start_packet[6];
	Ri2vkmo[midx(2, 1, 4)] = start_packet[7];
	Ri2vkmo[midx(3, 1, 4)] = 0.0;
	Ri2vkmo[midx(0, 2, 4)] = start_packet[8];
	Ri2vkmo[midx(1, 2, 4)] = start_packet[9];
	Ri2vkmo[midx(2, 2, 4)] = start_packet[10];
	Ri2vkmo[midx(3, 2, 4)] = 0.0;
	Ri2vkmo[midx(0, 3, 4)] = start_packet[11];
	Ri2vkmo[midx(1, 3, 4)] = start_packet[12];
	Ri2vkmo[midx(2, 3, 4)] = start_packet[13];
	Ri2vkmo[midx(3, 3, 4)] = 1.0;

	//create Rvkmo2vk, the incremental transformation matrix
	double invRi2vkmo[16];
	for (i = 0; i < 3; i++)
	{
		for (j = 0; j < 3; j++)
		{
			//upper left 3x3 block is just transposed
			invRi2vkmo[midx(i, j, 4)] = Ri2vkmo[midx(j, i, 4)];
		}
	}
	//last row is [0, 0, 0, 1]
	invRi2vkmo[midx(3, 0, 4)] = 0.0;
	invRi2vkmo[midx(3, 1, 4)] = 0.0;
	invRi2vkmo[midx(3, 2, 4)] = 0.0;
	invRi2vkmo[midx(3, 3, 4)] = 1.0;
	//last col is -R'*d
	for (i = 0; i < 3; i++)
	{
		invRi2vkmo[midx(i, 3, 4)] = 0.0;
		for (k = 0; k < 3; k++)
		{
			invRi2vkmo[midx(i, 3, 4)] -= Ri2vkmo[midx(k, i, 4)] * Ri2vkmo[midx(k, 3, 4)];
		}
	}

	//Rvkmo2vk = Ri2vk*inv(Ri2vkmo);
	double Rvkmo2vk[4*4];
	for (i = 0; i < 4; i++)
	{
		for (j = 0; j < 4; j++)
		{
			Rvkmo2vk[midx(i, j, 4)] = 0.0;
			for (k = 0; k < 4; k++)
			{
				Rvkmo2vk[midx(i, j, 4)] += Ri2vk[midx(i, k, 4)] * invRi2vkmo[midx(k, j, 4)];
			}
			oVehicleTransformation->T[midx(i, j, 4)] = Rvkmo2vk[midx(i, j, 4)];
		}
	}

	//set the rest of the vehicle transformation values
	oVehicleTransformation->IsValid = true;
	oVehicleTransformation->StartTime = tkmo;
	oVehicleTransformation->EndTime = tk;
	double dt = tk - tkmo;
	oVehicleTransformation->dt = dt;

	//set the uncertainty in each element as it relates to uncertainty in vehicle odometry
	double fdt = fabs(dt);
	double var_wx = fdt*mWXVar;
	double var_wy = fdt*mWYVar;
	double var_wz = fdt*mWZVar;
	double var_vx = fdt*mVXVar;
	double var_vy = fdt*mVYVar;
	double var_vz = fdt*mVZVar;

	//extract trig computations of delta-angles
	double sindy = Rvkmo2vk[midx(2, 0, 4)];
	double cosdy = sqrt(Rvkmo2vk[midx(2, 1, 4)]*Rvkmo2vk[midx(2, 1, 4)] 
		+ Rvkmo2vk[midx(2, 2, 4)]*Rvkmo2vk[midx(2, 2, 4)]);
	double sindx = -Rvkmo2vk[midx(2, 1, 4)] / cosdy;
	double cosdx = Rvkmo2vk[midx(2, 2, 4)] / cosdy;
	double sindz = -Rvkmo2vk[midx(1, 0, 4)] / cosdy;
	double cosdz = Rvkmo2vk[midx(0, 0, 4)] / cosdy;

	//extract delta-positions
	double dx = -(Rvkmo2vk[midx(0, 0, 4)]*Rvkmo2vk[midx(0, 3, 4)] 
		+ Rvkmo2vk[midx(1, 0, 4)]*Rvkmo2vk[midx(1, 3, 4)] 
		+ Rvkmo2vk[midx(2, 0, 4)]*Rvkmo2vk[midx(2, 3, 4)]);
	double dy = -(Rvkmo2vk[midx(0, 1, 4)]*Rvkmo2vk[midx(0, 3, 4)] 
		+ Rvkmo2vk[midx(1, 1, 4)]*Rvkmo2vk[midx(1, 3, 4)] 
		+ Rvkmo2vk[midx(2, 1, 4)]*Rvkmo2vk[midx(2, 3, 4)]);
	double dz = -(Rvkmo2vk[midx(0, 2, 4)]*Rvkmo2vk[midx(0, 3, 4)] 
		+ Rvkmo2vk[midx(1, 2, 4)]*Rvkmo2vk[midx(1, 3, 4)] 
		+ Rvkmo2vk[midx(2, 2, 4)]*Rvkmo2vk[midx(2, 3, 4)]);

	//use delta-angles and delta-positions to calculate jacobian of transformation
	double J[16*6];
	for (i = 0; i < 16; i++)
	{
		for (j = 0; j < 6; j++)
		{
			J[midx(i, j, 16)] = 0.0;
		}
	}
	//element (0, 0)
	J[midx(0, 1, 16)] = -cosdz*sindy;
	J[midx(0, 2, 16)] = -sindz*cosdy;
	//element (1, 0)
	J[midx(1, 1, 16)] = sindz*sindy;
	J[midx(1, 2, 16)] = -cosdz*cosdy;
	//element (2, 0)
	J[midx(2, 1, 16)] = cosdy;
	//element (0, 1)
	J[midx(4, 0, 16)] = cosdz*sindy*cosdx - sindz*sindx;
	J[midx(4, 1, 16)] = cosdz*cosdy*sindx;
	J[midx(4, 2, 16)] = -sindz*sindy*sindx + cosdz*cosdx;
	//element (1, 1)
	J[midx(5, 0, 16)] = -sindz*sindy*cosdx - cosdz*sindx;
	J[midx(5, 1, 16)] = -sindz*cosdy*sindx;
	J[midx(5, 2, 16)] = -sindz*sindy*cosdx - sindz*cosdx;
	//element (2, 1)
	J[midx(6, 0, 16)] = -cosdy*cosdx;
	J[midx(6, 1, 16)] = sindy*sindx;
	//element (3, 0)
	J[midx(8, 0, 16)] = cosdz*sindy*sindx + sindz*cosdx;
	J[midx(8, 1, 16)] = -cosdz*cosdy*cosdx;
	J[midx(8, 2, 16)] = sindz*sindy*cosdx + cosdz*sindx;
	//element (3, 1)
	J[midx(9, 0, 16)] = -sindz*sindy*sindx + cosdz*cosdx;
	J[midx(9, 1, 16)] = sindz*cosdy*cosdx;
	J[midx(9, 2, 16)] = cosdz*sindy*cosdx - sindz*sindx;
	//elelment (3, 2)
	J[midx(10, 0, 16)] = -cosdy*sindx;
	J[midx(10, 1, 16)] = -sindy*cosdx;
	//element (4, 0)
	J[midx(12, 0, 16)] = -(J[midx(0, 0, 16)]*dx + J[midx(4, 0, 16)]*dy + J[midx(8, 0, 16)]*dz);
	J[midx(12, 1, 16)] = -(J[midx(0, 1, 16)]*dx + J[midx(4, 1, 16)]*dy + J[midx(8, 1, 16)]*dz);
	J[midx(12, 2, 16)] = -(J[midx(0, 2, 16)]*dx + J[midx(4, 2, 16)]*dy + J[midx(8, 2, 16)]*dz);
	J[midx(12, 3, 16)] = -(cosdz*cosdy);
	J[midx(12, 4, 16)] = -(cosdz*sindy*sindx + sindz*cosdx);
	J[midx(12, 5, 16)] = -(-cosdz*sindy*cosdx + sindz*sindx);
	//element (5, 0)
	J[midx(13, 0, 16)] = -(J[midx(1, 0, 16)]*dx + J[midx(5, 0, 16)]*dy + J[midx(9, 0, 16)]*dz);
	J[midx(13, 1, 16)] = -(J[midx(1, 1, 16)]*dx + J[midx(5, 1, 16)]*dy + J[midx(9, 1, 16)]*dz);
	J[midx(13, 2, 16)] = -(J[midx(1, 2, 16)]*dx + J[midx(5, 2, 16)]*dy + J[midx(9, 2, 16)]*dz);
	J[midx(13, 3, 16)] = -(sindz*cosdy);
	J[midx(13, 4, 16)] = -(sindz*sindy*sindx + cosdz*cosdx);
	J[midx(13, 5, 16)] = -(sindz*sindy*cosdx + cosdz*sindx);
	//element (6, 0)
	J[midx(14, 0, 16)] = -(J[midx(2, 0, 16)]*dx + J[midx(6, 0, 16)]*dy + J[midx(10, 0, 16)]*dz);
	J[midx(14, 1, 16)] = -(J[midx(2, 1, 16)]*dx + J[midx(6, 1, 16)]*dy + J[midx(10, 1, 16)]*dz);
	J[midx(14, 2, 16)] = -(J[midx(2, 2, 16)]*dx + J[midx(6, 2, 16)]*dy + J[midx(10, 2, 16)]*dz);
	J[midx(14, 3, 16)] = -(sindy);
	J[midx(14, 4, 16)] = -(-cosdy*sindx);
	J[midx(14, 5, 16)] = -(cosdy*cosdx);

	double Q[6*6];
	for (i = 0; i < 6; i++)
	{
		for (j = 0; j < 6; j++)
		{
			Q[midx(i, j, 6)] = 0.0;
		}
	}
	Q[midx(0, 0, 6)] = var_wx;
	Q[midx(1, 1, 6)] = var_wy;
	Q[midx(2, 2, 6)] = var_wz;
	Q[midx(3, 3, 6)] = var_vx;
	Q[midx(4, 4, 6)] = var_vy;
	Q[midx(5, 5, 6)] = var_vz;

	//calculate the covariance of each element of the transformation matrix as JQJ'
	double JQ[16*6];
	for (i = 0; i < 16; i++)
	{
		for (j = 0; j < 6; j++)
		{
			JQ[midx(i, j, 16)] = 0.0;
			for (k = 0; k < 6; k++)
			{
				JQ[midx(i, j, 16)] += J[midx(i, k, 16)]*Q[midx(k, j, 6)];
			}
		}
	}
	for (i = 0; i < 16; i++)
	{
		for (j = 0; j < 16; j++)
		{
			oVehicleTransformation->PTT[midx(i, j, 16)] = 0.0;
			for (k = 0; k < 6; k++)
			{
				oVehicleTransformation->PTT[midx(i, j, 16)] += JQ[midx(i, k, 16)]*J[midx(j, k, 16)];
			}
		}
	}

	//done using the relative pose queue
	LeaveCriticalSection(&mQueueLock);

	//if code gets here, oVehicleTransformation has been set correctly
	rSuccess = true;

	return rSuccess;
}
