#include "clusteredsickinterface.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif


//RECIEVER-----------------------------------------------------------------------------------------------------------
void ClusteredSickReceiver::SetCallback(LidarClusterCallbackType handler, void* arg)
{
	cbk = handler;
	cbk_arg = arg;
}

ClusteredSickReceiver::ClusteredSickReceiver() 
{
	udp_params params = udp_params();
	params.remote_ip = inet_addr(UDP_REARSICK_ADDR);
	params.local_port = UDP_REARSICK_PORT;
	params.reuse_addr = 1;
	conn = new udp_connection (params);	
	conn->set_callback (MakeDelegate(this,&ClusteredSickReceiver::UDPCallback),conn);
	printf("ClusteredSick RX Interface Initialized. %s:%d\r\n",UDP_REARSICK_ADDR,UDP_REARSICK_PORT);
}

ClusteredSickReceiver::~ClusteredSickReceiver ()
{
	delete conn;
	printf("ClusteredSick Shutdown...\r\n");
}


void ClusteredSickReceiver::UDPCallback(udp_message& msg, udp_connection* conn, void* arg)
{ 		
	if (msg.len < 12)
	{
		printf("RS: Packet to short to process. (<12 bytes) \r\n", msg.data);
		return;
	}
	ClusteredSickMessageID m = (ClusteredSickMessageID)((int*)(msg.data))[0];	
	int rxSequenceNumber=	((int*)(msg.data))[1];
	dropCount += (rxSequenceNumber-(sequenceNumber+1));
	sequenceNumber = rxSequenceNumber;
	packetCount++;
	unsigned char* data =(unsigned char*) msg.data;

	switch(m)
	{
		case RS_Info:
			printf("RS: INFO: %s \r\n", msg.data);
		break;
		case RS_ClusterMsg:
		{
			ClusteredSickMsg rxmsg;
			rxmsg.hdr = *((ClusteredSickMsgHdr*)data);
			data+= sizeof(ClusteredSickMsgHdr);
			int numClusters = *((int*)data);
			data+=sizeof(int);
			if (numClusters > 0)
			{
				rxmsg.clusters.resize(numClusters);
				for (int i=0; i<numClusters; i++)
				{
					rxmsg.clusters[i].stable = *((bool*)data);
					data+=sizeof(bool);
					rxmsg.clusters[i].leftOccluded = *((bool*)data);
					data+=sizeof(bool);
					rxmsg.clusters[i].rightOccluded = *((bool*)data);
					data+=sizeof(bool);
					int numPoints = *((int*)data);
					data+=sizeof(int);
					if (numPoints > 0)
					{
						rxmsg.clusters[i].pts.resize (numPoints);
						for (int j=0; j<numPoints; j++)
						{
							rxmsg.clusters[i].pts[j] = *((SickXYPoint*)data);
							data+=sizeof(SickXYPoint);
						}
					}
				}
			}
			if (data != (unsigned char*)(msg.data + msg.len))			
				printf("WARNING: RS did not reach end of cluster packet!\n");
			
			//stuff now convert to isaac interface....
			vector<LidarCluster> isaacClusters;
			if (rxmsg.clusters.size() > 0)
			{
				isaacClusters.resize (rxmsg.clusters.size());
				for (int i=0; i<(int)rxmsg.clusters.size(); i++)
				{
					isaacClusters[i].leftOccluded = rxmsg.clusters[i].leftOccluded;
					isaacClusters[i].rightOccluded = rxmsg.clusters[i].rightOccluded;
					isaacClusters[i].stable = rxmsg.clusters[i].stable;
					if (rxmsg.clusters[i].pts.size()>0)
					{
						isaacClusters[i].pts.resize (rxmsg.clusters[i].pts.size());
						for (int j=0; j<(int)rxmsg.clusters[i].pts.size(); j++)
							isaacClusters[i].pts[j] = v3f((float)rxmsg.clusters[i].pts[j].x,(float)rxmsg.clusters[i].pts[j].y,0.0f);
					}
				}
			}
			if (!(cbk.empty()))	cbk(isaacClusters,rxmsg.hdr.carTime, cbk_arg);
		
			break;		
		}
		case RS_BAD:
			printf("RS: WARNING BAD MESSAGE!\r\n");
			break;
		default:
			printf("RS: WARNING UNKNOWN MESSAGE TYPE! Type is %d \r\n", (int)msg.data[0]);
			break;
	}
	if (packetCount%100==0)	
	{
		#ifdef PRINT_PACKET_COUNT_DEBUG
		printf("RS: Packets: %d Dropped %d Drop Rate: %f",packetCount,dropCount,((float)dropCount/(float)packetCount)*100.0f);	
		#endif
		packetCount=0; dropCount=0;
	}
}


//TRANSMITTER----------------------------------------------------------------------------------------------------

ClusteredSickSender::ClusteredSickSender()
{
	udp_params params = udp_params();
	params.remote_ip = inet_addr(UDP_REARSICK_ADDR);	
	params.local_port = 0;
	params.multicast_loopback = true;
	params.multicast_ttl=10;
	conn = new udp_connection (params);		
	sequenceNumber=0;
	printf("ClusteredSick TX Interface Initialized. %s:%d\r\n",UDP_REARSICK_ADDR,UDP_REARSICK_PORT);
}


ClusteredSickSender::~ClusteredSickSender()
{
	printf("ClusteredSick Shutdown...\r\n");
}

void ClusteredSickSender::Send(ClusteredSickMsg *msg) 
{	
	msg->hdr.msgType = RS_ClusterMsg;	
	msg->hdr.sequenceNumber = sequenceNumber++;
	void* sparseMsg =	malloc(65535);
	int byteoffset= 0;
	msg->copySparse (sparseMsg,byteoffset);	
	conn->send_message(sparseMsg,byteoffset,inet_addr(UDP_REARSICK_ADDR),UDP_REARSICK_PORT);
	free(sparseMsg);
}

