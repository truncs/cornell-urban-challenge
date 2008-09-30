#include "SensorCallbacks.h"

void ClusteredIbeoCallback(const vector<LidarCluster> &IbeoMessage, double IbeoTime, void* Arg)
{
	/*
	The callback for received Ibeo data.  Creates an Ibeo event and adds it to the
	event queue.

	INPUTS:
		IbeoMessage - the cluster data packet
		IbeoTime - the vehicle time stamp, in seconds
		Arg - a void pointer for extra information

	OUTPUTS:
		none.  (Adds an Ibeo event to the event queue)
	*/

	int i;
	int j;

	//count the number of clusters
	int nc;
	nc = (int) IbeoMessage.size();

	//extract the event time
	double iEventTime;
	iEventTime = IbeoTime;

	//check the event time for valid timestamp
	extern CarTime* TheCarTime;
	double cct = TheCarTime->CurrentCarTime();
	if (fabs(iEventTime - cct) > CT_TIMEJUMPREJECT)
	{
		//do not allow events with inconsistent timestamps into the event queues
		printf("Warning: got clustered ibeo timestamp %.12lg; expected something near %.12lg\n", iEventTime, cct);
		return;
	}

	//count the total number of points in all clusters
	LidarCluster cc;
	int np = 0;
	for (i = 0; i < nc; i++)
	{
		//count the total number of ibeo points
		cc = IbeoMessage[i];
		np += (int) cc.pts.size();
	}

	//declare memory for the whole Ibeo packet
	double* iEventData = NULL;
	if (np > 0)
	{
		iEventData = new double[np*LM_CLUSTEREDIBEOPACKETSIZE];
	}

	int idx = 0;
	for (i = 0; i < nc; i++)
	{
		//pull all the points from the ith cluster

		cc = IbeoMessage[i];
		int npc = (int) cc.pts.size();

		for (j = 0; j < npc; j++)
		{
			//timestamp
			iEventData[midx(idx, 0, np)] = iEventTime;
			//point index
			iEventData[midx(idx, 1, np)] = (double) idx;
			//total number of points
			iEventData[midx(idx, 2, np)] = (double) np;

			//cluster ID (0 - nc-1, the cluster ID)
			iEventData[midx(idx, 3, np)] = (double) i;
			//cluster stability flag (0 = stable, 1 = unstable)
			if (cc.stable == true)
			{
				iEventData[midx(idx, 4, np)] = 0.0;
			}
			else
			{
				iEventData[midx(idx, 4, np)] = 1.0;
			}
			//cluster occlusion flag (0 = not occluded, 1 = CCW occluded, 2 = CW occluded, 3 = both occluded)
			iEventData[midx(idx, 5, np)] = 0.0;
			if (cc.leftOccluded == true)
			{
				iEventData[midx(idx, 5, np)] += 1.0;
			}
			if (cc.rightOccluded == true)
			{
				iEventData[midx(idx, 5, np)] += 2.0;
			}
			//cluster height flag (0 = low obstacle, 1 = high obstacle)
			if (cc.highObstacle == false)
			{
				iEventData[midx(idx, 6, np)] = 0.0;
			}
			else
			{
				iEventData[midx(idx, 6, np)] = 1.0;
			}

			//the point itself, xyz coordinates off the center ibeo
			iEventData[midx(idx, 7, np)] = cc.pts[j].x;
			iEventData[midx(idx, 8, np)] = cc.pts[j].y;
			iEventData[midx(idx, 9, np)] = cc.pts[j].z;

			idx++;
		}
	}

	//when the code gets here, iEventData is populated with a complete sensor packet
	//this will be used to create each new event as it arrives
	Event *NewEvent;
	NewEvent = new Event();
	NewEvent->SetEventType(IBEO_EVENT, iEventTime);
	NewEvent->SetEventData(np, LM_CLUSTEREDIBEOPACKETSIZE, iEventData);
	#ifdef LM_PRINTLOGS
		//create a separate logging event with the same data
		Event* LogEvent = new Event(NewEvent);
	#endif

	//access to the local map event queue
	extern SynchronizedEventQueue* TheLocalMapEventQueue;
	//drop the event onto the event queue
	bool spush = TheLocalMapEventQueue->PushEvent(NewEvent);
	#ifdef LM_PRINTLOGS
		//only log the packet if it successfully entered the event queue
		extern EventQueue* TheLoggingQueue;
		if (spush == true)
		{
			TheLoggingQueue->PushEvent(LogEvent);
		}
		else
		{
			delete LogEvent;
		}
	#endif

	return;
}

void ClusteredSickCallback(const vector<LidarCluster> &CSMessage, double SickTime, void* Arg)
{
	/*
	The callback for received clustered horizontal SICK data.  Creates an event 
	and adds it to the event queue.

	INPUTS:
		CSMessage - the cluster data packet
		SickTime - the vehicle time stamp, in seconds
		Arg - a void pointer for extra information

	OUTPUTS:
		none.  (Adds an Ibeo event to the event queue)
	*/

	int i;
	int j;

	//count the number of clusters
	int nc;
	nc = (int) CSMessage.size();

	//extract the event time
	double iEventTime;
	iEventTime = SickTime;

	//check the event time for valid timestamp
	extern CarTime* TheCarTime;
	double cct = TheCarTime->CurrentCarTime();
	if (fabs(iEventTime - cct) > CT_TIMEJUMPREJECT)
	{
		//do not allow events with inconsistent timestamps into the event queues
		printf("Warning: got clustered sick timestamp %.12lg; expected something near %.12lg\n", iEventTime, cct);
		return;
	}

	//count the total number of points in all clusters
	LidarCluster cc;
	int np = 0;
	for (i = 0; i < nc; i++)
	{
		//count the total number of ibeo points
		cc = CSMessage[i];
		np += (int) cc.pts.size();
	}

	//declare memory for the whole Ibeo packet
	double* iEventData = NULL;
	if (np > 0)
	{
		iEventData = new double[np*LM_CLUSTEREDSICKPACKETSIZE];
	}

	int idx = 0;
	for (i = 0; i < nc; i++)
	{
		//pull all the points from the ith cluster

		cc = CSMessage[i];
		int npc = (int) cc.pts.size();

		for (j = 0; j < npc; j++)
		{
			//timestamp
			iEventData[midx(idx, 0, np)] = iEventTime;
			//point index
			iEventData[midx(idx, 1, np)] = (double) idx;
			//total number of points
			iEventData[midx(idx, 2, np)] = (double) np;

			//cluster ID (0 - nc-1, the cluster ID)
			iEventData[midx(idx, 3, np)] = (double) i;
			//cluster stability flag (0 = stable, 1 = unstable)
			if (cc.stable == true)
			{
				iEventData[midx(idx, 4, np)] = 0.0;
			}
			else
			{
				iEventData[midx(idx, 4, np)] = 1.0;
			}
			//cluster occlusion flag (0 = not occluded, 1 = CCW occluded, 2 = CW occluded, 3 = both occluded)
			iEventData[midx(idx, 5, np)] = 0.0;
			if (cc.leftOccluded == true)
			{
				iEventData[midx(idx, 5, np)] += 1.0;
			}
			if (cc.rightOccluded == true)
			{
				iEventData[midx(idx, 5, np)] += 2.0;
			}
			/*
			//cluster height flag (0 = low obstacle, 1 = high obstacle)
			if (cc.highObstacle == false)
			{
				iEventData[midx(idx, 6, np)] = 0.0;
			}
			else
			{
				iEventData[midx(idx, 6, np)] = 1.0;
			}
			*/
			//call all clustered SICK points high obstacles for now
			iEventData[midx(idx, 6, np)] = 1.0;

			//the point itself, xyz coordinates off the SICK
			iEventData[midx(idx, 7, np)] = cc.pts[j].x;
			iEventData[midx(idx, 8, np)] = cc.pts[j].y;
			iEventData[midx(idx, 9, np)] = cc.pts[j].z;

			idx++;
		}
	}

	//when the code gets here, iEventData is populated with a complete sensor packet
	//this will be used to create each new event as it arrives
	Event *NewEvent;
	NewEvent = new Event();
	NewEvent->SetEventType(BACKCLUSTEREDSICK_EVENT, iEventTime);
	NewEvent->SetEventData(np, LM_CLUSTEREDSICKPACKETSIZE, iEventData);
	#ifdef LM_PRINTLOGS
		//create a separate logging event with the same data
		Event* LogEvent = new Event(NewEvent);
	#endif

	//access to the local map event queue
	extern SynchronizedEventQueue* TheLocalMapEventQueue;
	//drop the event onto the event queue
	bool spush = TheLocalMapEventQueue->PushEvent(NewEvent);
	#ifdef LM_PRINTLOGS
		//only log the packet if it successfully entered the event queue
		extern EventQueue* TheLoggingQueue;
		if (spush == true)
		{
			TheLoggingQueue->PushEvent(LogEvent);
		}
		else
		{
			delete LogEvent;
		}
	#endif

	return;
}

void DelphiCallback(DelphiRadarScan DelphiMessage, DelphiInterfaceReceiver* DRX, int ID, void* Arg)
{
	/*
	Called when a delphi radar packet is received

	INPUTS:
		DelphiMessage - the actual radar message
		DRX - pointer to the interface receiver (not used)
		ID - enumerated ID indicating which radar generated the measurement
		Arg - a void pointer (not used here)

	OUTPUTS:
		none.  Formats delphi information and drops into the appropriate event queue
	*/

	//gain access to the event queue
	extern SynchronizedEventQueue* TheLocalMapEventQueue;

	int i;
	//number of targets detected by radar
	int nt = 0;

	double iEventTime = DelphiMessage.timestamp;

	//check the event time for valid timestamp
	extern CarTime* TheCarTime;
	double cct = TheCarTime->CurrentCarTime();
	if (fabs(iEventTime - cct) > CT_TIMEJUMPREJECT)
	{
		//do not allow events with inconsistent timestamps into the event queues
		printf("Warning: got radar timestamp %.12lg; expected something near %.12lg\n", iEventTime, cct);
		return;
	}

	for (i = 0; i < DELPHI_NUM_TRACKS; i++)
	{
		//count the number of valid tracks
		if (DelphiMessage.tracks[i].isValid == true)
		{
			nt++;
		}
	}

	//declare space for the event data
	//NOTE: this memory is cleared automatically when the event is pulled from the queue
	Event* NewEvent = new Event();
	int iEventType;
	double* iEventData = NULL;
	if (nt > 0)
	{
		iEventData = new double[nt*LM_DELPHIPACKETSIZE];
	}
	int inr = nt;
	int inc = LM_DELPHIPACKETSIZE;

	int idx = 0;
	for (i = 0; i < DELPHI_NUM_TRACKS; i++)
	{
		//loop through and keep all valid tracks
		if (DelphiMessage.tracks[i].isValid == false)
		{
			//invalid track, ignore it
			continue;
		}

		//timestamp
		iEventData[midx(idx, 0, nt)] = iEventTime;
		//obstacle index in the packet
		iEventData[midx(idx, 1, nt)] = (double) idx;
		//total number of obstacles
		iEventData[midx(idx, 2, nt)] = (double) nt;
		//obstacle track ID number
		iEventData[midx(idx, 3, nt)] = (double) DelphiMessage.tracks[i].id;

		//measurement invalid flag
		if (DelphiMessage.status.errorCommunication == true ||
			DelphiMessage.status.errorInternal == true ||
			DelphiMessage.status.errorOverheat == true ||
			DelphiMessage.status.errorRangePerformance == true ||
			DelphiMessage.status.errorVoltage == true ||
			DelphiMessage.status.isBlockageDetection == true ||
			DelphiMessage.status.scanOperational == false ||
			DelphiMessage.status.xvrOperational == false)
		{
			//the measurement was invalid
			iEventData[midx(idx, 4, nt)] = 1.0;
		}
		else
		{
			iEventData[midx(idx, 4, nt)] = 0.0;
		}

		//target scan count age
		iEventData[midx(idx, 5, nt)] = (double) DelphiMessage.tracks[i].counter;

		//combined object ID
		iEventData[midx(idx, 6, nt)] = (double) DelphiMessage.tracks[i].combinedObjectID;

		//target class ID
		iEventData[midx(idx, 7, nt)] = 0.0;
		if (DelphiMessage.tracks[i].isSidelobe == true)
		{
			iEventData[midx(idx, 7, nt)] = 1.0;
		}
		else if (DelphiMessage.tracks[i].isForwardTruckReflector == true)
		{
			iEventData[midx(idx, 7, nt)] = 2.0;
		}
		else if (DelphiMessage.tracks[i].isBridge == true)
		{
			iEventData[midx(idx, 7, nt)] = 3.0;
		}
		else if (DelphiMessage.tracks[i].isMatureObject == true)
		{
			iEventData[midx(idx, 7, nt)] = 4.0;
		}

		//target power
		iEventData[midx(idx, 8, nt)] = (double) DelphiMessage.tracks[i].power;

		//range to target (filtered)
		iEventData[midx(idx, 9, nt)] = (double) DelphiMessage.tracks[i].range;
		//range to target (unfiltered)
		iEventData[midx(idx, 10, nt)] = (double) DelphiMessage.tracks[i].rangeUnfiltered;

		//range rate (filtered)
		iEventData[midx(idx, 11, nt)] = (double) DelphiMessage.tracks[i].rangeRate;
		//range rate (unfiltered)
		iEventData[midx(idx, 12, nt)] = (double) DelphiMessage.tracks[i].rangeRateUnfiltered;

		//NOTE: delphi uses a left-handed coordinate frame, so negate all the angles

		//target bearing angle (filtered)
		iEventData[midx(idx, 13, nt)] = -(double) DelphiMessage.tracks[i].trackAngle;
		//target bearing angle (unfiltered)
		iEventData[midx(idx, 14, nt)] = -(double) DelphiMessage.tracks[i].trackAngleUnfiltered;

		//left and right unfiltered edge angles (rad.)
		iEventData[midx(idx, 15, nt)] = -(double) DelphiMessage.tracks[i].edgeAngleLeftUnfiltered;
		iEventData[midx(idx, 16, nt)] = -(double) DelphiMessage.tracks[i].edgeAngleRightUnfiltered;

		idx++;
	}

	switch (ID)
	{
	case DELPHI_Z_REAR0:
		iEventType = BACK0RADAR_EVENT;
		break;

	case DELPHI_X_DRIV3:
		iEventType = DRIV3RADAR_EVENT;
		break;

	case DELPHI_G_DRIV2:
		iEventType = DRIV2RADAR_EVENT;
		break;

	case DELPHI_F_DRIV1:
		iEventType = DRIV1RADAR_EVENT;
		break;

	case DELPHI_E_DRIV0:
		iEventType = DRIV0RADAR_EVENT;
		break;

	case DELPHI_D_FRONT0:
		iEventType = FRONT0RADAR_EVENT;
		break;

	case DELPHI_C_PASS0:
		iEventType = PASS0RADAR_EVENT;
		break;

	case DELPHI_B_PASS1:
		iEventType = PASS1RADAR_EVENT;
		break;

	case DELPHI_A_PASS2:
		iEventType = PASS2RADAR_EVENT;
		break;

	case DELPHI_Y_PASS3:
		iEventType = PASS3RADAR_EVENT;
		break;

	default:
		iEventType = INVALID_EVENT;
		break;
	}

	//store the event data in the event container
	NewEvent->SetEventType(iEventType, iEventTime);
	NewEvent->SetEventData(inr, inc, iEventData);
	#ifdef LM_PRINTLOGS
		//create a separate logging event with the same data
		Event* LogEvent = new Event(NewEvent);
	#endif

	//drop the event onto the event queue
	bool spush = TheLocalMapEventQueue->PushEvent(NewEvent);
	#ifdef LM_PRINTLOGS
		//only log the packet if it successfully entered the event queue
		extern EventQueue* TheLoggingQueue;
		if (spush == true)
		{
			TheLoggingQueue->PushEvent(LogEvent);
		}
		else
		{
			delete LogEvent;
		}
	#endif

	return;
}

void OdometryCallback(const pose_rel_msg& OdomMessage, void* Arg)
{
	/*
	Called when an odometry packet is received

	INPUTS:
		OdomMessage - contains the odometry packet information
		Arg - a void pointer (not used here)

	OUTPUTS:
		none.  Reformats the odometry inforamtion into an event and
		deposits the event into TheRelativePoseQueue.
	*/

	//gain access to the relative pose queue
	extern RelativePoseQueue* TheRelativePoseQueue;

	//timestamp
	double iEventTime = (double)(OdomMessage.car_ts_secs) + (double)(OdomMessage.car_ts_ticks)/10000.0;

	//check the event time for valid timestamp
	extern CarTime* TheCarTime;
	double cct = TheCarTime->CurrentCarTime();
	if (fabs(iEventTime - cct) > CT_TIMEJUMPREJECT)
	{
		//do not allow events with inconsistent timestamps into the event queues
		printf("Warning: got odometry timestamp %.12lg; expected something near %.12lg\n", iEventTime, cct);
		return;
	}

	double iEventData[LM_ODOMPACKETSIZE];
	iEventData[0] = iEventTime;
	//integration time dt
	iEventData[1] = OdomMessage.dt;
	//elements of the transformation matrix
	iEventData[2] = OdomMessage.Rinit2veh[0][0];
	iEventData[3] = OdomMessage.Rinit2veh[1][0];
	iEventData[4] = OdomMessage.Rinit2veh[2][0];
	iEventData[5] = OdomMessage.Rinit2veh[0][1];
	iEventData[6] = OdomMessage.Rinit2veh[1][1];
	iEventData[7] = OdomMessage.Rinit2veh[2][1];
	iEventData[8] = OdomMessage.Rinit2veh[0][2];
	iEventData[9] = OdomMessage.Rinit2veh[1][2];
	iEventData[10] = OdomMessage.Rinit2veh[2][2];
	iEventData[11] = OdomMessage.Rinit2veh[0][3];
	iEventData[12] = OdomMessage.Rinit2veh[1][3];
	iEventData[13] = OdomMessage.Rinit2veh[2][3];

	#ifdef LM_PRINTLOGS
		//create a separate logging event with the same data
		Event* LogEvent = new Event();
		double* LogEventData = new double[LM_ODOMPACKETSIZE];
		memcpy(LogEventData, iEventData, sizeof(double) * LM_ODOMPACKETSIZE);
		LogEvent->SetEventType(ODOM_EVENT, iEventTime);
		LogEvent->SetEventData(1, LM_ODOMPACKETSIZE, LogEventData);
	#endif

	//push the packet to the relative pose queue
	bool spush = TheRelativePoseQueue->PushPacket(iEventData);
	#ifdef LM_PRINTLOGS
		//only log the packet if it successfully entered the odometry queue
		extern EventQueue* TheLoggingQueue;
		if (spush == true)
		{
			TheLoggingQueue->PushEvent(LogEvent);
		}
		else
		{
			delete LogEvent;
		}
	#endif

	return;
}

void MobileyeObstacleCallback(MobilEyeObstacles MobileyeMessage, MobilEyeInterfaceReceiver* MRX, MobilEyeID ID, void* Arg)
{
	/*
	Sensor callback for receiving obstacle packets from the Mobileyes

	INPUTS:
		MobileyeMessage - class containing the mobileye obstacle
			packet information
		MRX - Mobileye interface receiver (not used here)
		ID - enumerated value telling which mobileye camera generated the message
		Arg - void pointer (not used here)

	OUTPUTS:
		none.  Creates an event for the mobileye information and pushes the 
			event onto the appropriate event queue
	*/

	//gain access to the event queue
	extern SynchronizedEventQueue* TheLocalMapEventQueue;

	int i;
	int no = MobileyeMessage.numObstacles;

	double iEventTime;
	iEventTime = MobileyeMessage.carTime;

	//check the event time for valid timestamp
	extern CarTime* TheCarTime;
	double cct = TheCarTime->CurrentCarTime();
	if (fabs(iEventTime - cct) > CT_TIMEJUMPREJECT)
	{
		//do not allow events with inconsistent timestamps into the event queues
		printf("Warning: got mobileye obstacle timestamp %.12lg; expected something near %.12lg\n", iEventTime, cct);
		return;
	}

	//declare space for the event data
	//NOTE: this memory is cleared automatically when the event is pulled from the queue
	Event* NewEvent = new Event();
	int iEventType;
	double* iEventData = NULL;
	if (no > 0)
	{
		iEventData = new double[no*LM_MOBILEYEOBSTACLEPACKETSIZE];
	}
	int inr = no;
	int inc = LM_MOBILEYEOBSTACLEPACKETSIZE;

	for (i = 0; i < no; i++)
	{
		//timestamp (should be the same for both road and environment message
		iEventData[midx(i, 0, no)] = iEventTime;
		//obstacle index in the packet
		iEventData[midx(i, 1, no)] = (double) i;
		//total number of obstacles
		iEventData[midx(i, 2, no)] = (double) no;
		//obstacle ID number
		iEventData[midx(i, 3, no)] = (double) (MobileyeMessage.obstacles[i].obstacleID);

		//obstacle forward distance from the camera
		iEventData[midx(i, 4, no)] = (double) (MobileyeMessage.obstacles[i].obstacleDistZ);
		//obstacle lateral distance from the camera
		if (MobileyeMessage.obstacles[i].obstacleDistXDirection == false)
		{
			//obstacle to the left of center
			iEventData[midx(i, 5, no)] = (double) (MobileyeMessage.obstacles[i].obstacleDistX);
		}
		else
		{
			//obstacle to the right of center
			iEventData[midx(i, 5, no)] = -(double) (MobileyeMessage.obstacles[i].obstacleDistX);
		}

		//obstacle forward relative speed
		double speed = (double) (MobileyeMessage.obstacles[i].velocity);
		if (_isnan(speed) != 0 || _finite(speed) == 0)
		{
			//if mobileye speed calculation is NaN or infinite, reject the entire frame
			printf("Warning: NaN or Inf encountered in Mobileye obstacle speed.\n");
			delete NewEvent;
			delete [] iEventData;
			return;
		}
		iEventData[midx(i, 6, no)] = speed;
		//obstacle width
		iEventData[midx(i, 7, no)] = (double) (MobileyeMessage.obstacles[i].obstacleWidth);
		//obstacle path
		iEventData[midx(i, 8, no)] = (double) (MobileyeMessage.obstacles[i].path);
		//current in-path vehicle
		if (MobileyeMessage.obstacles[i].currentInPathVehicle == true)
		{
			iEventData[midx(i, 9, no)] = 1.0;
		}
		else
		{
			iEventData[midx(i, 9, no)] = 0.0;
		}
		//obstacle confidence
		iEventData[midx(i, 10, no)] = (double) (MobileyeMessage.obstacles[i].confidence);
	}

	switch (ID)
	{
	case MOBILEYE_CTR:
		iEventType = FRONTMOBILEYEOBSTACLE_EVENT;
		break;

	case MOBILEYE_REAR:
		iEventType = BACKMOBILEYEOBSTACLE_EVENT;
		break;

	default:
		iEventType = INVALID_EVENT;
		break;
	}

	//store the event data in the event container
	NewEvent->SetEventType(iEventType, iEventTime);
	NewEvent->SetEventData(inr, inc, iEventData);
	#ifdef LM_PRINTLOGS
		//create a separate logging event with the same data
		Event* LogEvent = new Event(NewEvent);
	#endif

	//drop the event onto the event queue
	bool spush = TheLocalMapEventQueue->PushEvent(NewEvent);
	#ifdef LM_PRINTLOGS
		//only log the packet if it successfully entered the event queue
		extern EventQueue* TheLoggingQueue;
		if (spush == true)
		{
			TheLoggingQueue->PushEvent(LogEvent);
		}
		else
		{
			delete LogEvent;
		}
	#endif

	return;
}

void MobileyeRoadCallback(MobilEyeRoadEnv MobileyeMessage, MobilEyeInterfaceReceiver* MRX, MobilEyeID ID, void* Arg)
{
	/*
	Sensor callback for receiving road and environment packets from the Mobileyes

	INPUTS:
		MobileyeMessage - class containing the mobileye road / environment
			packet information
		MRX - Mobileye interface receiver (not used here)
		ID - enumerated value telling which mobileye camera generated the message
		Arg - void pointer (not used here)

	OUTPUTS:
		none.  Creates an event for the mobileye information and pushes the 
			event onto the event queue
	*/

	//gain access to the event queue
	extern SynchronizedEventQueue* TheLocalRoadEventQueue;

	double iEventTime = MobileyeMessage.road.carTime;

	//check the event time for valid timestamp
	extern CarTime* TheCarTime;
	double cct = TheCarTime->CurrentCarTime();
	if (fabs(iEventTime - cct) > CT_TIMEJUMPREJECT)
	{
		//do not allow events with inconsistent timestamps into the event queues
		printf("Warning: got mobileye road timestamp %.12lg; expected something near %.12lg\n", iEventTime, cct);
		return;
	}

	//declare space for the event data
	//NOTE: this memory is cleared automatically when the event is pulled from the queue
	Event* NewEvent = new Event();
	int iEventType;
	double* iEventData = new double[LM_MOBILEYEROADPACKETSIZE];
	int inr = 1;
	int inc = LM_MOBILEYEROADPACKETSIZE;

	//timestamp (should be the same for both road and environment message)
	iEventData[0] = iEventTime;
	//left and right road edge distances (not danger zone)
	iEventData[1] = (double) MobileyeMessage.road.distToLeftEdge;
	iEventData[2] = (double) MobileyeMessage.road.distToRightEdge;
	//left and right lane distances for the car's current lane
	iEventData[3] = (double) MobileyeMessage.road.distToLeftMark;
	iEventData[4] = (double) MobileyeMessage.road.distToRightMark;
	//far left and far right lane line distances
	iEventData[5] = (double) MobileyeMessage.road.distToLeftNeighborMark;
	iEventData[6] = (double) MobileyeMessage.road.distToRightNeighborMark;
	//heading of the vehicle wrt the road
	iEventData[7] = (double) MobileyeMessage.road.roadModelSlope;
	//curvature of the road
	iEventData[8] = (double) MobileyeMessage.road.roadModelCurvature;
	//left and right road edge line mark types
	iEventData[9] = (double) MobileyeMessage.road.leftEdgeMarkType;
	iEventData[10] = (double) MobileyeMessage.road.rightEdgeMarkType;
	//left and right lane mark types
	iEventData[11] = (double) MobileyeMessage.road.leftMarkType;
	iEventData[12] = (double) MobileyeMessage.road.rightMarkType;
	//far left and far right lane mark types
	iEventData[13] = (double) MobileyeMessage.road.leftNeighborMarkType;
	iEventData[14] = (double) MobileyeMessage.road.rightNeighborMarkType;
	//left and right road edge confidences
	iEventData[15] = (double) MobileyeMessage.road.leftEdgeConfidence;
	iEventData[16] = (double) MobileyeMessage.road.rightEdgeConfidence;
	//left and right lane confidences
	iEventData[17] = (double) MobileyeMessage.road.leftLaneConfidence;
	iEventData[18] = (double) MobileyeMessage.road.rightLaneConfidence;
	//far left and far right lane confidences
	iEventData[19] = (double) MobileyeMessage.road.leftNeighborConfidence;
	iEventData[20] = (double) MobileyeMessage.road.rightNeighborConfidence;
	//model validity distance
	iEventData[21] = (double) MobileyeMessage.road.roadModelValidRange;
	//whether a stop line is present
	if (MobileyeMessage.road.stopLineDistance == 0xFFF)
	{
		//no stopline detected
		iEventData[22] = 0.0;
	}
	else
	{
		//stopline detected
		iEventData[22] = 1.0;
	}
	iEventData[23] = (double) MobileyeMessage.road.stopLineDistance;
	iEventData[24] = (double) MobileyeMessage.road.stopLineConf;

	switch (ID)
	{
	case MOBILEYE_CTR:
		iEventType = FRONTMOBILEYEROAD_EVENT;
		break;

	case MOBILEYE_REAR:
		iEventType = BACKMOBILEYEROAD_EVENT;
		break;

	default:
		iEventType = INVALID_EVENT;
		break;
	}

	//store the event data in the event container
	NewEvent->SetEventType(iEventType, iEventTime);
	NewEvent->SetEventData(inr, inc, iEventData);
	#ifdef LM_PRINTLOGS
		//create a separate logging event with the same data
		Event* LogEvent = new Event(NewEvent);
	#endif

	//drop the event onto the event queue
	bool spush = TheLocalRoadEventQueue->PushEvent(NewEvent);
	#ifdef LM_PRINTLOGS
		//only log the packet if it successfully entered the event queue
		extern EventQueue* TheLoggingQueue;
		if (spush == true)
		{
			TheLoggingQueue->PushEvent(LogEvent);
		}
		else
		{
			delete LogEvent;
		}
	#endif

	return;
}

void JasonRoadCallback(RoadFitterOutput JasonMessage, RoadFitterInterfaceReceiver* JRX, RoadFitterID ID, void* Arg)
{
	/*
	Called when jason's algorithm returns a road fix

	INPUTS:
		JasonMessage - the results stored in Jason's data packet
		JRX - pointer to the interface receiver (not used here)
		ID - enumerated ID indicating which camera was used to create the message
		Arg - null pointer (not used here)

	OUTPUTS:
		none.  Reformats the packet into an event and drops it onto ThePosteriorPoseEventQueue
	*/

	int i;
	int j;
	//extract the number of jason's road segments (always RF_MAX_FITS)
	int nj = RF_VALID_FITS;

	//set timestamp from car time
	double iEventTime = JasonMessage.carTime;

	//check the event time for valid timestamp
	extern CarTime* TheCarTime;
	double cct = TheCarTime->CurrentCarTime();
	if (fabs(iEventTime - cct) > CT_TIMEJUMPREJECT)
	{
		//do not allow events with inconsistent timestamps into the event queues
		printf("Warning: got roadfinder timestamp %.12lg; expected something near %.12lg\n", iEventTime, cct);
		return;
	}

	//gain access to the event queue
	extern SynchronizedEventQueue* TheLocalRoadEventQueue;

	//declare space for the event data
	//NOTE: this memory is cleared automatically when the event is pulled from the queue
	Event* NewEvent = new Event();
	int iEventType;
	double* iEventData = new double[LM_JASONROADPACKETSIZE*nj];

	for (i = 0; i < nj; i++)
	{
		for (j = 0; j < LM_JASONROADPACKETSIZE; j++)
		{
			iEventData[midx(i, j, nj)] = 0.0;
		}
	}

	for (i = 0; i < nj; i++)
	{
		//timestamp (synchronized vehicle time)
		iEventData[midx(i, 0, nj)] = iEventTime;
		//index of the segmentation and number of segmentations
		iEventData[midx(i, 1, nj)] = (double) (i);
		iEventData[midx(i, 2, nj)] = (double) (nj);
		//number of lines / boundaries found (0 - 4)
		int nb = JasonMessage.roadFits[i].borders_observed;
		iEventData[midx(i, 3, nj)] = (double) nb;
		for (j = 0; j < nb; j++)
		{
			//distance to boundaries found (m from camera, positive for lines to the left of center)
			iEventData[midx(i, j+4, nj)] = JasonMessage.roadFits[i].border_offsets[j];
			//boundary types
			switch (JasonMessage.roadFits[i].observed_border_types[j])
			{
			case RoadFitterOutputData::BT_SingleLaneLine:
				iEventData[midx(i, j+8, nj)] = 1.0;
				break;
			case RoadFitterOutputData::BT_Edge:
				iEventData[midx(i, j+8, nj)] = 0.0;
				break;
			default:
				iEventData[midx(i, j+8, nj)] = 0.0;
				break;
			}
		}
		//heading of road wrt camera (m/m, positive for roads heading right)
		iEventData[midx(i, 12, nj)] = -JasonMessage.roadFits[i].road_heading;
		//curvature of road wrt camera (m/m^2, positive for roads curving right)
		iEventData[midx(i, 13, nj)] = -JasonMessage.roadFits[i].system_curvature;
		//confidence in segmentation fit
		iEventData[midx(i, 14, nj)] = JasonMessage.roadFits[i].confidence;
	}

	switch (ID)
	{
	case RF_CTR:
		iEventType = FRONTJASONROAD_EVENT;
		break;

	case RF_REAR:
		iEventType = BACKJASONROAD_EVENT;
		break;

	default:
		iEventType = INVALID_EVENT;
		break;
	}

	//store the event data in the event container
	NewEvent->SetEventType(iEventType, iEventTime);
	NewEvent->SetEventData(nj, LM_JASONROADPACKETSIZE, iEventData);
	#ifdef LM_PRINTLOGS
		//create a separate logging event with the same data
		Event* LogEvent = new Event(NewEvent);
	#endif

	//drop the event onto the event queue
	bool spush = TheLocalRoadEventQueue->PushEvent(NewEvent);
	#ifdef LM_PRINTLOGS
		//only log the packet if it successfully entered the event queue
		extern EventQueue* TheLoggingQueue;
		if (spush == true)
		{
			TheLoggingQueue->PushEvent(LogEvent);
		}
		else
		{
			delete LogEvent;
		}
	#endif

	return;
}

void LN200Callback(LN200_data_packet_struct LNMessage, LN200InterfaceReceiver* LRX, void* Arg)
{
	/*
	Callback for the LN200 timing message

	INPUTS:
		LNMessage - the LN200 message packet
		LRX - pointer to the interface receiver
		Arg - a void pointer (not used)

	OUTPUTS:
		none.
	*/

	//extract the current car time from the LN200
	double CurrentTime = ((double) ntohs(LNMessage.ts_s)) + ((double) ntohl(LNMessage.ts_t)) / 10000.0;

	extern CarTime* TheCarTime;
	TheCarTime->SetCurrentCarTime(CurrentTime);

	return;
}

void SideSickCallback(SideSickMsg SSMessage, SideSickReceiver* SSRX, SIDESICKID ID, void* Arg)
{
	/*
	Callback used for side SICK distance / height type measurments

	INPUTS:
		SSMessage - the side SICK message data
		SSRX - the side SICK message receiver
		ID - the enumerated ID of the scanner
		Arg - a void pointer (not used here)

	OUTPUTS:
		none.  Creates an event and drops it onto the event queue
	*/

	int i;
	int nss = SSMessage.numObstacles;

	double iEventTime = SSMessage.carTime;

	//check the event time for valid timestamp
	extern CarTime* TheCarTime;
	double cct = TheCarTime->CurrentCarTime();
	if (fabs(iEventTime - cct) > CT_TIMEJUMPREJECT)
	{
		//do not allow events with inconsistent timestamps into the event queues
		printf("Warning: got side sick timestamp %.12lg; expected something near %.12lg\n", iEventTime, cct);
		return;
	}

	//declare space for the event data
	//NOTE: this memory is cleared automatically when the event is pulled from the queue
	Event* NewEvent = new Event();
	int iEventType = INVALID_EVENT;
	double* iEventData = NULL;
	if (nss > 0)
	{
		iEventData = new double[nss*LM_SIDESICKPACKETSIZE];
	}

	for (i = 0; i < nss; i++)
	{
		//set all the data for this side sick detection
		//timestamp
		iEventData[midx(i, 0, nss)] = iEventTime;
		//index
		iEventData[midx(i, 1, nss)] = (double) i;
		//number of events
		iEventData[midx(i, 2, nss)] = (double) nss;
		//distance to object of interest
		iEventData[midx(i, 3, nss)] = SSMessage.obstacles[i].distance;
		//height of object of interest
		iEventData[midx(i, 4, nss)] = SSMessage.obstacles[i].height;
	}

	switch (ID)
	{
	case SS_DRIVER:
		iEventType = SIDESICKDRIV_EVENT;
		break;
	case SS_PASSENGER:
		iEventType = SIDESICKPASS_EVENT;
		break;
	}

	//gain access to the event queue
	extern SynchronizedEventQueue* TheLocalMapEventQueue;
	//store the event data in the event container
	NewEvent->SetEventType(iEventType, iEventTime);
	NewEvent->SetEventData(nss, LM_SIDESICKPACKETSIZE, iEventData);
	#ifdef LM_PRINTLOGS
		//create a separate logging event with the same data
		Event* LogEvent = new Event(NewEvent);
	#endif

	//drop the event onto the event queue
	bool spush = TheLocalMapEventQueue->PushEvent(NewEvent);
	#ifdef LM_PRINTLOGS
		//only log the packet if it successfully entered the event queue
		extern EventQueue* TheLoggingQueue;
		if (spush == true)
		{
			TheLoggingQueue->PushEvent(LogEvent);
		}
		else
		{
			delete LogEvent;
		}
	#endif

	return;
}
