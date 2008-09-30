#include "sideroadedgeinterface.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif


//RECIEVER-----------------------------------------------------------------------------------------------------------
void SideRoadEdgeReceiver::SetCallback(SideRoadEdge_Msg_Handler handler, void* arg)
{
	cbk = handler;
	cbk_arg = arg;
}


SideRoadEdgeReceiver::SideRoadEdgeReceiver() 
{
	udp_params params = udp_params();
	params.remote_ip = inet_addr(UDP_SIDEROADEDGE_ADDR);
	params.local_port = UDP_SIDEROADEDGE_PORT;
	params.reuse_addr = 1;
	conn = new udp_connection (params);	
	conn->set_callback (MakeDelegate(this,&SideRoadEdgeReceiver::UDPCallback),conn);
	printf("SideRoadEdgeMsg RX Interface Initialized. %s:%d\r\n",UDP_SIDEROADEDGE_ADDR,UDP_SIDEROADEDGE_PORT);
}

SideRoadEdgeReceiver::~SideRoadEdgeReceiver ()
{
	delete conn;
	printf("SideRoadEdgeMsg Shutdown...\r\n");
}


void SideRoadEdgeReceiver::UDPCallback(udp_message& msg, udp_connection* conn, void* arg)
{ 		
	if (msg.len < 12)
	{
		printf("SRE: Packet to short to process. (<12 bytes) \r\n", msg.data);
		return;
	}
	SideRoadEdgeMsgID m = (SideRoadEdgeMsgID)((int*)(msg.data))[0];
	SIDEROADEDGEID id = (SIDEROADEDGEID)((int*)(msg.data))[1];	
	int rxSequenceNumber=	((int*)(msg.data))[2];
	dropCount += (rxSequenceNumber-(sequenceNumber+1));
	sequenceNumber = rxSequenceNumber;
	packetCount++;

	switch(m)
	{
		case SRE_Info:
			printf("SRE: INFO: %s \r\n", msg.data);
		break;
		case SRE_RoadEdgeMsg:
			//cast this stuff		
			if (msg.len != sizeof (SideRoadEdgeMsg))
				printf("SRE: WARNING MESSAGE IS WRONG SIZE: is: %d should be %d\r\n",msg.len,sizeof(SideRoadEdgeMsg));
			else
				if (!(cbk.empty()))	cbk(*((SideRoadEdgeMsg*)(msg.data)), this, id, cbk_arg);
		break;		
		case SRE_BAD:
			printf("SRE: WARNING BAD MESSAGE!\r\n");
			break;
		default:
			printf("SRE: WARNING UNKNOWN MESSAGE TYPE! Type is %d \r\n", (int)msg.data[0]);
			break;
	}
	if (packetCount%100==0)	
	{
		#ifdef PRINT_PACKET_COUNT_DEBUG
		printf("SRE: Packets: %d Dropped %d Drop Rate: %f",packetCount,dropCount,((float)dropCount/(float)packetCount)*100.0f);	
		#endif
		packetCount=0; dropCount=0;
	}

}


//TRANSMITTER----------------------------------------------------------------------------------------------------

SideRoadEdgeSender::SideRoadEdgeSender()
{
	udp_params params = udp_params();
	params.remote_ip = inet_addr(UDP_SIDEROADEDGE_ADDR);	
	params.local_port = 0;
	params.multicast_loopback = true;
	params.multicast_ttl=10;
	conn = new udp_connection (params);		
	sequenceNumber=0;
	printf("SideRoadEdgeMsg TX Interface Initialized. %s:%d\r\n",UDP_SIDEROADEDGE_ADDR,UDP_SIDEROADEDGE_PORT);
}


SideRoadEdgeSender::~SideRoadEdgeSender()
{
	printf("SideRoadEdgeMsg Shutdown...\r\n");
}

void SideRoadEdgeSender::Send(SideRoadEdgeMsg *msg, SIDEROADEDGEID id) 
{	
	msg->channelVersion = CHANNEL_VERSION;
	msg->serializerType = SERIALIZER_TYPE_SRE;		
	msg->sequenceNumber = sequenceNumber++;
	msg->msgType = SRE_RoadEdgeMsg;
	msg->scannerID = id;
	msg->sequenceNumber = sequenceNumber++;
	conn->send_message(msg,sizeof(SideRoadEdgeMsg),inet_addr(UDP_SIDEROADEDGE_ADDR),UDP_SIDEROADEDGE_PORT);
}

