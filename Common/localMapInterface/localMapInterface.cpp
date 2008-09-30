#include "localMapInterface.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

//RECIEVER-----------------------------------------------------------------------------------------------------------
void LocalMapInterfaceReceiver::SetLocalMapTargetsCallback(LocalMapTargets_Msg_handler handler, void* arg)
{
	localMapTargets_cbk = handler;
	localMapTargets_cbk_arg = arg;
}

void LocalMapInterfaceReceiver::SetLocalMapLooseClustersCallback(LocalMapLooseClusters_Msg_handler handler, void* arg)
{
	localMapLooseClusters_cbk = handler;
	localMapLooseClusters_cbk_arg = arg;
}

void LocalMapInterfaceReceiver::SetLocalMapLocalRoadModelCallback(LocalMapLocalRoadEstimate_Msg_handler handler, void* arg)
{
	localMapLocalRoadEstimates_cbk = handler;
	localMapLocalRoadEstimates_cbk_arg = arg;
}

LocalMapInterfaceReceiver::LocalMapInterfaceReceiver() 
{
	udp_params params = udp_params();
	params.remote_ip = inet_addr(UDP_LOCALMAP_MULTICAST_ADDR);
	params.local_port = UDP_LOCALMAP_PORT;
  params.reuse_addr = 1;
	conn = new udp_connection (params);	
	conn->set_callback (MakeDelegate(this,&LocalMapInterfaceReceiver::UDPCallback),conn);
	sequenceNumber=0;
	dropCount=0;
	packetCount=0;
	printf("Local Map RX Interface Initialized. %s:%d\r\n",UDP_LOCALMAP_MULTICAST_ADDR,UDP_LOCALMAP_PORT);
}

LocalMapInterfaceReceiver::~LocalMapInterfaceReceiver ()
{
	delete conn;
	printf("Local Map Shutdown...\r\n");
}


void LocalMapInterfaceReceiver::UDPCallback(udp_message& msg, udp_connection* conn, void* arg)
{ 
	//messages come in like this:
	//BYTE, BYTE, ID + memory mapped message, ID is 1 byte
		
	unsigned char* data =(unsigned char*) msg.data;
	LocalMapMsgHdr hdr = *((LocalMapMsgHdr*)(data));
	
	int rxSequenceNumber=	hdr.sequenceNumber;
	dropCount += (rxSequenceNumber-(sequenceNumber+1));
	sequenceNumber = rxSequenceNumber;
	packetCount++;

	switch(hdr.msgType)
	{
		case LOCALMAP_Info:
			printf("LM INFO: %s \r\n", msg.data);
		break;			
		case LOCALMAP_Targets:
		{
			LocalMapTargetsMsg tmsg;
			int numtargets=0;
			//ok now recreate this object			
			//tmsg.header = *((LocalMapMsgHdr*)(data));
			data+=sizeof(LocalMapMsgHdr);
			numtargets = *((int*)(data));
			data+=sizeof(int);
			tmsg.timestamp = *((double*)(data));
			data+=sizeof(double);
			tmsg.targets.resize (numtargets);
			for (int i=0; i<numtargets; i++)
			{					
				//now get each target
				LocalMapTarget tgt;
				int numPts = *((int*)(data));				
        data+=sizeof(int);
        tgt.type = *((LocalMapTargetType*)(data));
				data+=sizeof(LocalMapTargetType);
        tgt.x = *((float*)(data));		
				data+=sizeof(float);
        tgt.y = *((float*)(data));
				data+=sizeof(float);
        tgt.speed = *((float*)(data));
				data+=sizeof(float);
        tgt.heading = *((float*)(data));
				data+=sizeof(float);
        tgt.width = *((float*)(data));
				data+=sizeof(float);
        tgt.orientation = *((float*)(data));
				data+=sizeof(float);
				for (int j=0; j<36; j++)
				{
					tgt.covariance[j] = *((float*)(data));
					data+=sizeof(float);
				}

				tgt.points.resize (numPts);
        for (int j=0; j<numPts; j++)
				{          
					tgt.points[j] = *((LocalMapPoint*)(data));
					data+=sizeof(LocalMapPoint);          
				}
        tmsg.targets[i] = tgt;
			}
			if (data != (unsigned char*)(msg.data + msg.len))			
				printf("WARNING: LM did not reach end of target packet!\n");
			
			if (!(localMapTargets_cbk.empty()))	localMapTargets_cbk(tmsg, this, localMapTargets_cbk_arg);
			
			break;
		}
		case LOCALMAP_LooseClusters:
		{				
			LocalMapLooseClustersMsg lcmsg;
			//ok now recreate this object			
			//lcmsg.header = *((LocalMapMsgHdr*)(data));
			
			data+=sizeof(LocalMapMsgHdr);
			int numClusters = *((int*)(data));
			lcmsg.clusters.resize (numClusters);
			data+=sizeof(int);
			lcmsg.timestamp = *((double*)(data));
			data+=sizeof(double);
			for (int i=0; i<numClusters; i++)
			{	
				//now get each target
				int numPoints = *((int*)data);
				data+=sizeof(int);		
				lcmsg.clusters[i].clusterClass = *((LocalMapClusterClass*)data);
				data+=sizeof(LocalMapClusterClass);		


				lcmsg.clusters[i].points.resize (numPoints);	
				for (int j=0; j<numPoints; j++)
				{
					lcmsg.clusters[i].points[j] = *((LocalMapPoint*)data);
					data+=sizeof (LocalMapPoint);
				}
			}
			if (data != (unsigned char*)(msg.data + msg.len))			
				printf("WARNING: LM did not reach end of loose cluster packet!\n");
			
			if (!(localMapLooseClusters_cbk.empty()))	localMapLooseClusters_cbk(lcmsg, this, localMapLooseClusters_cbk_arg);

			break;
		}
		case LRM_LocalRoadModel:
			//cast this stuff		
			if (msg.len != sizeof (LocalRoadModelEstimateMsg))
				printf("LM: WARNING MESSAGE IS WRONG SIZE: is: %d should be %d\r\n",msg.len,sizeof(LocalRoadModelEstimateMsg));
			else
				if (!(localMapLocalRoadEstimates_cbk.empty()))	localMapLocalRoadEstimates_cbk(*((LocalRoadModelEstimateMsg*)(msg.data)), this, localMapLocalRoadEstimates_cbk_arg);
			break;			
		case LOCALMAP_Bad:
			printf("WARNING: LM BAD MESSAGE!\r\n");
			break;
		default:
			printf("WARNING: LM UNKNOWN MESSAGE TYPE! Type is %d \r\n", hdr.msgType);
			break;
	}
	if (packetCount%100==0)	
	{
		#ifdef PRINT_PACKET_COUNT_DEBUG
		printf("LM: Packets: %d Seq: %d Dropped: %d Drop Rate: %f \r\n",packetCount,sequenceNumber,dropCount,((float)dropCount/(float)packetCount)*100.0f);	
		#endif
		packetCount=0; dropCount=0;
	}
}


//TRANSMITTER----------------------------------------------------------------------------------------------------

LocalMapInterfaceSender::LocalMapInterfaceSender()
{
	udp_params params = udp_params();
	params.remote_ip = inet_addr(UDP_LOCALMAP_MULTICAST_ADDR);	
	params.local_port = 0;
	params.multicast_loopback = true;
	params.multicast_ttl=10;
	conn = new udp_connection (params);		
	sequenceNumber=0;
	printf("Local Map TX Interface Initialized. %s:%d\r\n",UDP_LOCALMAP_MULTICAST_ADDR,UDP_LOCALMAP_PORT);
}


LocalMapInterfaceSender::~LocalMapInterfaceSender()
{
	delete conn;
	printf("Local Map Shutdown...\r\n");
}

void LocalMapInterfaceSender::SendLocalMapTargets(LocalMapTargetsMsg* msg)
{	
	msg->header.msgType = LOCALMAP_Targets;	
	msg->header.serializerVersion = CHANNEL_VERSION;
	msg->header.serializerType = SERIALIZER_IGNORE_TYPE;
	msg->header.sequenceNumber= sequenceNumber++;
	void* sparseMsg =	malloc(65535*2);
	int byteoffset= 0;
	msg->copySparse (sparseMsg,byteoffset);

	if(byteoffset < MAX_PACKET_SIZE)
	{			
		conn->send_message(sparseMsg,byteoffset,inet_addr(UDP_LOCALMAP_MULTICAST_ADDR),UDP_LOCALMAP_PORT);	
	}
	else
		printf("CRITICAL: Message too large to be sent in SendLocalMapTargets. Size is %d\n",byteoffset);
		
	free(sparseMsg);
}

void LocalMapInterfaceSender::SendLocalRoadModel (LocalRoadModelEstimateMsg* msg)
{
	msg->id = LRM_LocalRoadModel;
	msg->channelVersion = CHANNEL_VERSION;
	msg->serializerType = SERIALIZER_TYPE;
	msg->sequenceNumber = sequenceNumber++;
	conn->send_message(msg,sizeof(LocalRoadModelEstimateMsg),inet_addr(UDP_LOCALMAP_MULTICAST_ADDR),UDP_LOCALMAP_PORT);
}

void LocalMapInterfaceSender::SendLocalMapLooseClusters(LocalMapLooseClustersMsg* msg) 
{	
	msg->header.msgType = LOCALMAP_LooseClusters;	
	msg->header.serializerVersion = CHANNEL_VERSION;
	msg->header.serializerType = SERIALIZER_IGNORE_TYPE;
	msg->header.sequenceNumber= sequenceNumber++;
	void* sparseMsg =	malloc(65535*2);
	int byteoffset= 0;
	msg->copySparse (sparseMsg,byteoffset);
	
	if(byteoffset < MAX_PACKET_SIZE)
	{			
		conn->send_message(sparseMsg,byteoffset,inet_addr(UDP_LOCALMAP_MULTICAST_ADDR),UDP_LOCALMAP_PORT);	
	}
	else
		printf("CRITICAL: Message too large to be sent in SendLocalMapLooseClusters. Size is %d\n",byteoffset);
	
	free(sparseMsg);
}
