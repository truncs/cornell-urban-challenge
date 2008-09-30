#include "sidesickinterface.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif


//RECIEVER-----------------------------------------------------------------------------------------------------------
void SideSickReceiver::SetCallback(SideSick_Msg_Handler handler, void* arg)
{
	cbk = handler;
	cbk_arg = arg;
}


SideSickReceiver::SideSickReceiver() 
{
	udp_params params = udp_params();
	params.remote_ip = inet_addr(UDP_SIDESICK_ADDR);
	params.local_port = UDP_SIDESICK_PORT;
	params.reuse_addr = 1;
	conn = new udp_connection (params);	
	conn->set_callback (MakeDelegate(this,&SideSickReceiver::UDPCallback),conn);
	printf("SideSick RX Interface Initialized. %s:%d\r\n",UDP_SIDESICK_ADDR,UDP_SIDESICK_PORT);
}

SideSickReceiver::~SideSickReceiver ()
{
	delete conn;
	printf("SideSick Shutdown...\r\n");
}


void SideSickReceiver::UDPCallback(udp_message& msg, udp_connection* conn, void* arg)
{ 		
	if (msg.len < 12)
	{
		printf("SS: Packet to short to process. (<12 bytes) \r\n", msg.data);
		return;
	}
	int rxSequenceNumber=	((int*)(msg.data+2))[0];
	SideSickMessageID m = (SideSickMessageID)((int*)(msg.data+2))[1];
	SIDESICKID id = (SIDESICKID)((int*)(msg.data+2))[2];	

	dropCount += (rxSequenceNumber-(sequenceNumber+1));
	sequenceNumber = rxSequenceNumber;
	packetCount++;

	switch(m)
	{
		case SS_Info:
			printf("SS: INFO: %s \r\n", msg.data);
		break;
		case SS_ScanMsg:
			//cast this stuff		
			if (msg.len != sizeof (SideSickMsg))
				printf("SS: WARNING MESSAGE IS WRONG SIZE: is: %d should be %d\r\n",msg.len,sizeof(SideSickMsg));
			else
				if (!(cbk.empty()))	cbk(*((SideSickMsg*)(msg.data)), this, id, cbk_arg);
		break;		
		case SS_BAD:
			printf("SS: WARNING BAD MESSAGE!\r\n");
			break;
		default:
			printf("SS: WARNING UNKNOWN MESSAGE TYPE! Type is %d \r\n", (int)msg.data[0]);
			break;
	}
	if (packetCount%100==0)	
	{
		#ifdef PRINT_PACKET_COUNT_DEBUG
		printf("SS: Packets: %d Dropped %d Drop Rate: %f",packetCount,dropCount,((float)dropCount/(float)packetCount)*100.0f);	
		#endif
		packetCount=0; dropCount=0;
	}
}


//TRANSMITTER----------------------------------------------------------------------------------------------------

SideSickSender::SideSickSender()
{
	udp_params params = udp_params();
	params.remote_ip = inet_addr(UDP_SIDESICK_ADDR);	
	params.local_port = 0;
	params.multicast_loopback = true;
	params.multicast_ttl=10;
	conn = new udp_connection (params);		
	sequenceNumber=0;
	printf("SideSick TX Interface Initialized. %s:%d\r\n",UDP_SIDESICK_ADDR,UDP_SIDESICK_PORT);
}


SideSickSender::~SideSickSender()
{
	printf("SideSick Shutdown...\r\n");
}

void SideSickSender::Send(SideSickMsg *msg, SIDESICKID id) 
{	
	msg->channelVersion = CHANNEL_VERSION;
	msg->serializerType = SERIALIZER_TYPE_SS;		
	msg->sequenceNumber = sequenceNumber++;
	msg->msgType = SS_ScanMsg;
	msg->scannerID = id;
	msg->sequenceNumber = sequenceNumber++;
	conn->send_message(msg,sizeof(SideSickMsg),inet_addr(UDP_SIDESICK_ADDR),UDP_SIDESICK_PORT);
}

