#include "SensorCallbacks.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

void PoseCallback(const pose_abs_msg& PoseMessage, void* Arg)
{
	/*
	Called when an absolute pose packet is received

	INPUTS:
		PoseMessage - contains the pose packet information
		Arg - a void pointer (not used here)

	OUTPUTS:
		none.  Reformats the pose packet information into an event and
		deposits the event into ThePosteriorPoseEventQueue.
	*/

	//gain access to the event queue
	extern SynchronizedEventQueue* ThePosteriorPoseEventQueue;

	//timestamp
	double iEventTime = (double)(PoseMessage.car_ts_secs) + (double)(PoseMessage.car_ts_ticks)/10000.0;

	//check the event time for valid timestamp
	extern CarTime* TheCarTime;
	double cct = TheCarTime->CurrentCarTime();
	if (fabs(iEventTime - cct) > CT_TIMEJUMPREJECT)
	{
		//do not allow events with inconsistent timestamps into the event queues
		printf("Warning: got pose timestamp %.12lg; expected something near %.12lg\n", iEventTime, cct);
		return;
	}

	//declare space for the pose event data
	//NOTE: this memory is cleared automatically when the event is pulled from the queue
	Event* NewEvent = new Event();
	double* iEventData = new double[SE_POSEPACKETSIZE];
	int inr = 1;
	int inc = SE_POSEPACKETSIZE;

	iEventData[0] = iEventTime;
	switch (PoseMessage.flags & POSE_ABS_CORR_MASK)
	{
	case POSE_ABS_CORR_NONE:
		iEventData[1] = 0.0;
		break;
	case POSE_ABS_CORR_WAAS:
		iEventData[1] = 2.0;
		break;
	case POSE_ABS_CORR_VBS:
		iEventData[1] = 3.0;
		break;
	case POSE_ABS_CORR_HP:
		iEventData[1] = 4.0;
		break;
	default:
		printf("Warning: received unknown pose quality of service flag: %d.\n", PoseMessage.flags);
		iEventData[1] = 0.0;
		break;
	}
	//yaw, pitch, roll (vehicle coordinates)
	iEventData[2] = PoseMessage.yaw;
	iEventData[3] = PoseMessage.pitch;
	iEventData[4] = PoseMessage.roll;
	//ECEF xyz
	iEventData[5] = PoseMessage.px;
	iEventData[6] = PoseMessage.py;
	iEventData[7] = PoseMessage.pz;
	//ECEF vx, vy, vz
	iEventData[8] = PoseMessage.ecef_vx;
	iEventData[9] = PoseMessage.ecef_vy;
	iEventData[10] = PoseMessage.ecef_vz;
	//ypr covariance
	iEventData[11] = PoseMessage.cov_ypr[0][0];
	iEventData[12] = PoseMessage.cov_ypr[1][0];
	iEventData[13] = PoseMessage.cov_ypr[1][1];
	iEventData[14] = PoseMessage.cov_ypr[2][0];
	iEventData[15] = PoseMessage.cov_ypr[2][1];
	iEventData[16] = PoseMessage.cov_ypr[2][2];
	//ECEF xyz covariance
	iEventData[17] = PoseMessage.cov_pos[0][0];
	iEventData[18] = PoseMessage.cov_pos[1][0];
	iEventData[19] = PoseMessage.cov_pos[1][1];
	iEventData[20] = PoseMessage.cov_pos[2][0];
	iEventData[21] = PoseMessage.cov_pos[2][1];
	iEventData[22] = PoseMessage.cov_pos[2][2];
	//ECEF vx, vy, vz covariance
	iEventData[23] = PoseMessage.cov_vel[0][0];
	iEventData[24] = PoseMessage.cov_vel[1][0];
	iEventData[25] = PoseMessage.cov_vel[1][1];
	iEventData[26] = PoseMessage.cov_vel[2][0];
	iEventData[27] = PoseMessage.cov_vel[2][1];
	iEventData[28] = PoseMessage.cov_vel[2][2];

	//store the event data in the event container
	NewEvent->SetEventType(POSE_EVENT, iEventTime);
	NewEvent->SetEventData(inr, inc, iEventData);
	#ifdef SE_PRINTLOGS
		//create a separate logging event with the same data
		Event* LogEvent = new Event(NewEvent);
	#endif

	//drop the event onto the event queue
	bool spush = ThePosteriorPoseEventQueue->PushEvent(NewEvent);
	#ifdef SE_PRINTLOGS
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

	double iEventData[SE_ODOMPACKETSIZE];
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

	#ifdef SE_PRINTLOGS
		//create a separate logging event with the same data
		Event* LogEvent = new Event();
		double* LogEventData = new double[SE_ODOMPACKETSIZE];
		memcpy(LogEventData, iEventData, sizeof(double) * SE_ODOMPACKETSIZE);
		LogEvent->SetEventType(ODOM_EVENT, iEventTime);
		LogEvent->SetEventData(1, SE_ODOMPACKETSIZE, LogEventData);
	#endif

	//push the packet to the relative pose queue
	bool spush = TheRelativePoseQueue->PushPacket(iEventData);
	#ifdef SE_PRINTLOGS
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

void MobileyeCallback(MobilEyeRoadEnv MobileyeMessage, MobilEyeInterfaceReceiver* MRX, MobilEyeID ID, void* Arg)
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
		event onto ThePosteriorPoseEventQueue
	*/

	//gain access to the event queue
	extern SynchronizedEventQueue* ThePosteriorPoseEventQueue;

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
	double* iEventData = new double[SE_MOBILEYEROADPACKETSIZE];
	int inr = 1;
	int inc = SE_MOBILEYEROADPACKETSIZE;

	//timestamp (should be the same for both road and environment message
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

	default:
		iEventType = INVALID_EVENT;
		break;
	}

	//store the event data in the event container
	NewEvent->SetEventType(iEventType, iEventTime);
	NewEvent->SetEventData(inr, inc, iEventData);
	#ifdef SE_PRINTLOGS
		//create a separate logging event with the same data
		Event* LogEvent = new Event(NewEvent);
	#endif

	//drop the event onto the event queue
	bool spush = ThePosteriorPoseEventQueue->PushEvent(NewEvent);
	#ifdef SE_PRINTLOGS
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

void StoplineCallback(StopLineMessage SLMessage, StopLineInterfaceReceiver* SRX, void* Arg)
{
	/*
	Sensor callback for receiving stopline information from Aaron's stopline algorithm

	INPUTS:
		SLMessage - class containing the stopline packet data
		SRX - stopline interface receiver (not used here)
		Arg - void pointer (not used here)

	OUTPUTS:
		none.  Creates an event for the stopline information and pushes the 
		event onto ThePosteriorPoseEventQueue
	*/

	int i;

	//gain access to the event queue
	extern SynchronizedEventQueue* ThePosteriorPoseEventQueue;

	//set timestamp from car time
	double iEventTime = SLMessage.carTime;

	//check the event time for valid timestamp
	extern CarTime* TheCarTime;
	double cct = TheCarTime->CurrentCarTime();
	if (fabs(iEventTime - cct) > CT_TIMEJUMPREJECT)
	{
		//do not allow events with inconsistent timestamps into the event queues
		printf("Warning: got stopline timestamp %.12lg; expected something near %.12lg\n", iEventTime, cct);
		return;
	}

	//extract the number of stoplines
	int ns = SLMessage.numStopLines;

	//declare space for the event data
	//NOTE: this memory is cleared automatically when the event is pulled from the queue
	Event* NewEvent = new Event();
	double* iEventData = NULL;
	if (ns > 0)
	{
		iEventData = new double[ns*SE_STOPLINEPACKETSIZE];
	}

	for (i = 0; i < ns; i++)
	{
		//timestamp (synchronized vehicle time)
		iEventData[midx(i, 0, ns)] = iEventTime;
		//index of the stopline and number of stoplines
		iEventData[midx(i, 1, ns)] = (double) (i);
		iEventData[midx(i, 2, ns)] = (double) (ns);
		//distance to stopline
		iEventData[midx(i, 3, ns)] = SLMessage.stopLines[i].distance;
		//confidence in stopline
		iEventData[midx(i, 4, ns)] = SLMessage.stopLines[i].confidence;
	}

	//store the event data in the event container
	NewEvent->SetEventType(STOPLINE_EVENT, iEventTime);
	NewEvent->SetEventData(ns, SE_STOPLINEPACKETSIZE, iEventData);
	#ifdef SE_PRINTLOGS
		//create a separate logging event with the same data
		Event* LogEvent = new Event(NewEvent);
	#endif

	//drop the event onto the event queue
	bool spush = ThePosteriorPoseEventQueue->PushEvent(NewEvent);
	#ifdef SE_PRINTLOGS
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

void JasonCallback(RoadFitterOutput JasonMessage, RoadFitterInterfaceReceiver* JRX, RoadFitterID ID, void* Arg)
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

	//gain access to the event queue
	extern SynchronizedEventQueue* ThePosteriorPoseEventQueue;

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

	//declare space for the event data
	//NOTE: this memory is cleared automatically when the event is pulled from the queue
	Event* NewEvent = new Event();
	int iEventType;
	double* iEventData = new double[nj*SE_JASONROADPACKETSIZE];

	for (i = 0; i < nj; i++)
	{
		for (j = 0; j < SE_JASONROADPACKETSIZE; j++)
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

	default:
		iEventType = INVALID_EVENT;
		break;
	}

	//store the event data in the event container
	NewEvent->SetEventType(iEventType, iEventTime);
	NewEvent->SetEventData(nj, SE_JASONROADPACKETSIZE, iEventData);
	#ifdef SE_PRINTLOGS
		//create a separate logging event with the same data
		Event* LogEvent = new Event(NewEvent);
	#endif

	//drop the event onto the event queue
	bool spush = ThePosteriorPoseEventQueue->PushEvent(NewEvent);
	#ifdef SE_PRINTLOGS
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

void LocalMapLooseClustersCallback(LocalMapLooseClustersMsg LMMessage, LocalMapInterfaceReceiver* LMRX, void* Arg)
{
	/*
	Callback for the localmap loose clusters message

	INPUTS:
		LMMessage - the LocalMap message packet
		LMRX - pointer to the localmap receiver
		Arg - a void pointer (not used)

	OUTPUTS:
		none.  Drops an event onto the event queue.
	*/

	//gain access to the event queue
	extern SynchronizedEventQueue* TheTrackGeneratorEventQueue;

	double iEventTime = LMMessage.timestamp;

	//check the event time for valid timestamp
	extern CarTime* TheCarTime;
	double cct = TheCarTime->CurrentCarTime();
	if (fabs(iEventTime - cct) > CT_TIMEJUMPREJECT)
	{
		//do not allow events with inconsistent timestamps into the event queues
		printf("Warning: got localmap loose clusters timestamp %.12lg; expected something near %.12lg\n", iEventTime, cct);
		return;
	}

	int nc = (int) LMMessage.clusters.size();

	int i;
	int j;
	int np = 0;
	for (i = 0; i < nc; i++)
	{
		//count the total number of obstacle points across all clusters
		LocalMapLooseCluster cc = LMMessage.clusters[i];
		np += (int) cc.points.size();
	}

	//declare memory for the packet and assign values
	int iEventType = LOCALMAPPOINTS_EVENT;
	int iNumDataRows = np;
	int iNumDataCols = SE_LOCALPOINTSPACKETSIZE;
	double* iEventData = NULL;
	if (np > 0)
	{
		iEventData = new double[np*SE_LOCALPOINTSPACKETSIZE];
	}

	int idx = 0;
	for (i = 0; i < nc; i++)
	{
		//pull all clusters and copy into an event packet
		LocalMapLooseCluster cc = LMMessage.clusters[i];
		int npc = (int) cc.points.size();

		for (j = 0; j < npc; j++)
		{
			//pull each point from the cluster
			LocalMapPoint cp = cc.points[j];

			//set the event data
			iEventData[midx(idx, 0, np)] = iEventTime;
			iEventData[midx(idx, 1, np)] = (double) idx;
			iEventData[midx(idx, 2, np)] = (double) np;
			iEventData[midx(idx, 3, np)] = (double) i;
			switch (cc.clusterClass)
			{
			case LOCALMAP_HighObstacle:
				iEventData[midx(idx, 4, np)] = 1.0;
				break;
			case LOCALMAP_LowObstacle:
				iEventData[midx(idx, 4, np)] = 0.0;
				break;
			}
			iEventData[midx(idx, 5, np)] = cp.GetX();
			iEventData[midx(idx, 6, np)] = cp.GetY();

			//done setting this point
			idx++;
		}
	}

	//create the event and push it to the track generator
	Event* NewEvent = new Event();
	NewEvent->SetEventType(iEventType, iEventTime);
	NewEvent->SetEventData(iNumDataRows, iNumDataCols, iEventData);
	#ifdef SE_PRINTLOGS
		//create a separate logging event with the same data
		Event* LogEvent = new Event(NewEvent);
	#endif

	bool spush = TheTrackGeneratorEventQueue->PushEvent(NewEvent);

	#ifdef SE_PRINTLOGS
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

void LocalMapTargetsCallback(LocalMapTargetsMsg LMMessage, LocalMapInterfaceReceiver* LMRX, void* Arg)
{
	/*
	Callback for handling the targets tracked by LocalMap

	INPUTS:
		LMMessage - the LocalMap message data containing the targets
		LMRX - the local map interface receiver
		Arg - a void pointer, not used here

	OUTPUTS:
		none.  Creates an event and places it on the event queue
	*/

	//gain access to the event queue
	extern SynchronizedEventQueue* TheTrackGeneratorEventQueue;

	int nt = (int) LMMessage.targets.size();

	double iEventTime = LMMessage.timestamp;

	//check the event time for valid timestamp
	extern CarTime* TheCarTime;
	double cct = TheCarTime->CurrentCarTime();
	if (fabs(iEventTime - cct) > CT_TIMEJUMPREJECT)
	{
		//do not allow events with inconsistent timestamps into the event queues
		printf("Warning: got localmap targets timestamp %.12lg; expected something near %.12lg\n", iEventTime, cct);
		return;
	}

	int i;
	int j;
	int k;
	int idx;

	//declare memory for the header packet and the covariance packet
	int iEventType = LOCALMAPTARGETS_EVENT;
	int iNumDataRows = nt;
	int iNumDataCols = SE_LMTHEADERPACKETSIZE;
	double* iEventData = NULL;
	if (nt > 0)
	{
		iEventData = new double[nt*SE_LMTHEADERPACKETSIZE];
	}
	int iNumDataRows2 = nt;
	int iNumDataCols2 = SE_LMTCOVARIANCEPACKETSIZE;
	double* iEventData2 = NULL;
	if (nt > 0)
	{
		iEventData2 = new double[nt*SE_LMTCOVARIANCEPACKETSIZE];
	}
	int np = 0;

	for (i = 0; i < nt; i++)
	{
		//set the packet headers and count the total number of target points
		LocalMapTarget CurrentTarget = LMMessage.targets[i];
		np += (int) CurrentTarget.points.size();

		//HEADER PACKET
		//event time
		iEventData[midx(i, 0, nt)] = iEventTime;
		//target index
		iEventData[midx(i, 1, nt)] = (double) i;
		//number of targets
		iEventData[midx(i, 2, nt)] = (double) nt;
		//target type
		switch (CurrentTarget.type)
		{
		case LM_TT_INVALID:
			iEventData[midx(i, 3, nt)] = -1.0;
			break;
		case LM_TT_IBEO:
			iEventData[midx(i, 3, nt)] = 101.0;
			break;
		case LM_TT_IBEOMATURE:
			iEventData[midx(i, 3, nt)] = 102.0;
			break;
		case LM_TT_MOBILEYE:
			iEventData[midx(i, 3, nt)] = 103.0;
			break;
		case LM_TT_RADAR:
			iEventData[midx(i, 3, nt)] = 104.0;
			break;
		case LM_TT_QUASIMATURE:
			iEventData[midx(i, 3, nt)] = 105.0;
			break;
		case LM_TT_MATURE:
			iEventData[midx(i, 3, nt)] = 106.0;
			break;
		default:
			iEventData[midx(i, 3, nt)] = -1.0;
		}
		//target X, Y, Orientation, Speed, Heading, Width
		iEventData[midx(i, 4, nt)] = CurrentTarget.x;
		iEventData[midx(i, 5, nt)] = CurrentTarget.y;
		iEventData[midx(i, 6, nt)] = CurrentTarget.orientation;
		iEventData[midx(i, 7, nt)] = CurrentTarget.speed;
		iEventData[midx(i, 8, nt)] = CurrentTarget.heading;
		iEventData[midx(i, 9, nt)] = CurrentTarget.width;

		//number of points
		iEventData[midx(i, 10, nt)] = (double) CurrentTarget.points.size();

		//COVARIANCE PACKET
		//event time
		iEventData2[midx(i, 0, nt)] = iEventTime;
		//target index
		iEventData2[midx(i, 1, nt)] = (double) i;
		//number of targets
		iEventData2[midx(i, 2, nt)] = (double) nt;
		//lower triangle of covariance matrix, in column major order
		idx = 3;
		for (j = 0; j < 6; j++)
		{
			for (k = 0; k <= j; k++)
			{
				iEventData2[midx(i, idx, nt)] = CurrentTarget.covariance[midx(j, k, 6)];
				idx++;
			}
		}
	}

	//declare memory for target points data
	int iNumDataRows3 = np;
	int iNumDataCols3 = SE_LMTPOINTSPACKETSIZE;
	double* iEventData3 = NULL;
	if (np > 0)
	{
		iEventData3 = new double[np*SE_LMTPOINTSPACKETSIZE];
	}
	idx = 0;
	int npt;

	for (i = 0; i < nt; i++)
	{
		//set the points packet for each target

		LocalMapTarget CurrentTarget = LMMessage.targets[i];
		npt = (int) CurrentTarget.points.size();
		for (j = 0; j < npt; j++)
		{
			//event time
			iEventData3[midx(idx, 0, np)] = iEventTime;
			iEventData3[midx(idx, 1, np)] = (double) i;
			iEventData3[midx(idx, 2, np)] = (double) nt;
			iEventData3[midx(idx, 3, np)] = (double) j;
			iEventData3[midx(idx, 4, np)] = (double) npt;
			LocalMapPoint CurrentPoint = CurrentTarget.points[j];
			iEventData3[midx(idx, 5, np)] = CurrentPoint.GetX();
			iEventData3[midx(idx, 6, np)] = CurrentPoint.GetY();
			idx++;
		}
	}

	//create the event and push it to the track generator
	Event* NewEvent = new Event();
	NewEvent->SetEventType(iEventType, iEventTime);
	NewEvent->SetEventData(iNumDataRows, iNumDataCols, iEventData);
	NewEvent->SetEventData2(iNumDataRows2, iNumDataCols2, iEventData2);
	NewEvent->SetEventData3(iNumDataRows3, iNumDataCols3, iEventData3);
	#ifdef SE_PRINTLOGS
		//create a separate logging event with the same data
		Event* LogEvent = new Event(NewEvent);
	#endif

	bool spush = TheTrackGeneratorEventQueue->PushEvent(NewEvent);

	#ifdef SE_PRINTLOGS
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

void LocalRoadCallback(LocalRoadModelEstimateMsg LRMessage, LocalMapInterfaceReceiver* LMRX, void* Arg)
{
	/*
	Callback for handling the local road model coming from LocalMap

	INPUTS:
		LRMessage - the actual local road message data
		LMRX - the local map interface receiver
		Arg - a void pointer (not used)

	OUTPUTS:
		none.  Creates an event and places it on the event queue
	*/

	//gain access to the event queue
	extern SynchronizedEventQueue* ThePosteriorPoseEventQueue;

	double iEventTime = LRMessage.timestamp;

	//check the event time for valid timestamp
	extern CarTime* TheCarTime;
	double cct = TheCarTime->CurrentCarTime();
	if (fabs(iEventTime - cct) > CT_TIMEJUMPREJECT)
	{
		//do not allow events with inconsistent timestamps into the event queues
		printf("Warning: got localroad timestamp %.12lg; expected something near %.12lg\n", iEventTime, cct);
		return;
	}

	int iNumDataRows = 1;
	int iNumDataCols = SE_LRHEADERPACKETSIZE;

	//construct the header packet
	double* iEventData = new double[SE_LRHEADERPACKETSIZE];
	iEventData[0] = LRMessage.timestamp;
	iEventData[1] = LRMessage.probabilityRoadModelValid;
	iEventData[2] = LRMessage.probabilityLeftLaneExists;
	iEventData[3] = LRMessage.laneWidthLeft;
	iEventData[4] = LRMessage.laneWidthLeftVariance;
	iEventData[5] = LRMessage.probabilityCenterLaneExists;
	iEventData[6] = LRMessage.laneWidthCenter;
	iEventData[7] = LRMessage.laneWidthCenterVariance;
	iEventData[8] = LRMessage.probabilityRightLaneExists;
	iEventData[9] = LRMessage.laneWidthRight;
	iEventData[10] = LRMessage.laneWidthRightVariance;
	iEventData[11] = LRMessage.numPointsLeft;
	iEventData[12] = LRMessage.numPointsCenter;
	iEventData[13] = LRMessage.numPointsRight;

	//construct the points packet
	int npL = LRMessage.numPointsLeft;
	int npC = LRMessage.numPointsCenter;
	int npR = LRMessage.numPointsRight;
	int np = npL + npC + npR;
	int iNumDataRows2 = np;
	int iNumDataCols2 = SE_LRPOINTSPACKETSIZE;
	double* iEventData2 = new double[np*SE_LRPOINTSPACKETSIZE];

	int i;
	int idx = 0;

	for (i = 0; i < npL; i++)
	{
		//set each left lane point in the points packet
		iEventData2[midx(idx, 0, np)] = iEventTime;
		iEventData2[midx(idx, 1, np)] = (double) i;
		iEventData2[midx(idx, 2, np)] = (double) npL;
		iEventData2[midx(idx, 3, np)] = 0.0;
		iEventData2[midx(idx, 4, np)] = LRMessage.LanePointsLeft[i].GetX();
		iEventData2[midx(idx, 5, np)] = LRMessage.LanePointsLeft[i].GetY();
		iEventData2[midx(idx, 6, np)] = LRMessage.LanePointsLeft[i].GetVariance();
		idx++;
	}
	for (i = 0; i < npC; i++)
	{
		//set each center lane point in the points packet
		iEventData2[midx(idx, 0, np)] = iEventTime;
		iEventData2[midx(idx, 1, np)] = (double) i;
		iEventData2[midx(idx, 2, np)] = (double) npC;
		iEventData2[midx(idx, 3, np)] = 1.0;
		iEventData2[midx(idx, 4, np)] = LRMessage.LanePointsCenter[i].GetX();
		iEventData2[midx(idx, 5, np)] = LRMessage.LanePointsCenter[i].GetY();
		iEventData2[midx(idx, 6, np)] = LRMessage.LanePointsCenter[i].GetVariance();
		idx++;
	}
	for (i = 0; i < npR; i++)
	{
		//set each center lane point in the points packet
		iEventData2[midx(idx, 0, np)] = iEventTime;
		iEventData2[midx(idx, 1, np)] = (double) i;
		iEventData2[midx(idx, 2, np)] = (double) npC;
		iEventData2[midx(idx, 3, np)] = 2.0;
		iEventData2[midx(idx, 4, np)] = LRMessage.LanePointsRight[i].GetX();
		iEventData2[midx(idx, 5, np)] = LRMessage.LanePointsRight[i].GetY();
		iEventData2[midx(idx, 6, np)] = LRMessage.LanePointsRight[i].GetVariance();
		idx++;
	}

	//set the event data
	Event* NewEvent = new Event();
	NewEvent->SetEventType(LOCALMAPROAD_EVENT, iEventTime);
	NewEvent->SetEventData(iNumDataRows, iNumDataCols, iEventData);
	NewEvent->SetEventData2(iNumDataRows2, iNumDataCols2, iEventData2);

	//and push to the event queue
	#ifdef SE_PRINTLOGS
		//create a separate logging event with the same data
		Event* LogEvent = new Event(NewEvent);
	#endif

	bool spush = ThePosteriorPoseEventQueue->PushEvent(NewEvent);

	#ifdef SE_PRINTLOGS
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
