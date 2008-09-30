#include "stoplineinterface.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif


//RECIEVER-----------------------------------------------------------------------------------------------------------
void StopLineInterfaceReceiver::SetStopLineCallback(stopLinemsg_handler handler, void* arg)
{
	sl_cbk = handler;
	sl_cbk_arg = arg;
}


StopLineInterfaceReceiver::StopLineInterfaceReceiver() 
{
	udp_params params = udp_params();
	params.remote_ip = inet_addr(UDP_STOPLINE_MULTICAST_ADDR);
	params.local_port = UDP_STOPLINE_PORT;
	params.reuse_addr = 1;
	conn = new udp_connection (params);	
	conn->set_callback (MakeDelegate(this,&StopLineInterfaceReceiver::UDPCallback),conn);
	printf("Stopline RX Interface Initialized. %s:%d\r\n",UDP_STOPLINE_MULTICAST_ADDR,UDP_STOPLINE_PORT);
}

StopLineInterfaceReceiver::~StopLineInterfaceReceiver ()
{
	delete conn;
	printf("Stopline Shutdown...\r\n");
}


void StopLineInterfaceReceiver::UDPCallback(udp_message& msg, udp_connection* conn, void* arg)
{ 
	//messages come in like this:
	// ID + memory mapped message, ID is 1 byte
	StopLineMessageID m = *((StopLineMessageID*)msg.data);	
	int rxSequenceNumber=	((int*)(msg.data))[1];	
	dropCount += (rxSequenceNumber-(sequenceNumber+1));
	sequenceNumber = rxSequenceNumber;
	packetCount++;

	switch(m)
	{
		case SL_Info:
			printf("SL: INFO: %s \r\n", msg.data);
		break;
		case SL_StopLineMsg:
			//cast this stuff		
			if (msg.len != sizeof (StopLineMessage))
				printf("SL: WARNING MESSAGE IS WRONG SIZE: is: %d should be %d\r\n",msg.len,sizeof(StopLineMessage));
			else
				if (!(sl_cbk.empty()))	sl_cbk(*((StopLineMessage*)(msg.data)), this, sl_cbk_arg);
		break;		
		case SL_BAD:
			printf("SL: WARNING BAD MESSAGE!\r\n");
			break;
		default:
			printf("SL: WARNING UNKNOWN MESSAGE TYPE! Type is %d \r\n", (int)msg.data[0]);
			break;
	}
	if (packetCount%100==0)	
	{
		#ifdef PRINT_PACKET_COUNT_DEBUG
		printf("SL: Packets: %d Dropped %d Drop Rate: %f",packetCount,dropCount,((float)dropCount/(float)packetCount)*100.0f);	
		#endif
		packetCount=0; dropCount=0;
	}
}


//TRANSMITTER----------------------------------------------------------------------------------------------------

StopLineInterfaceSender::StopLineInterfaceSender()
{
	udp_params params = udp_params();
	params.remote_ip = inet_addr(UDP_STOPLINE_MULTICAST_ADDR);	
	params.local_port = 0;
	params.multicast_loopback = true;
	params.multicast_ttl=10;
	conn = new udp_connection (params);		
	sequenceNumber=0;
	printf("StopLine TX Interface Initialized. %s:%d\r\n",UDP_STOPLINE_MULTICAST_ADDR,UDP_STOPLINE_PORT);
}


StopLineInterfaceSender::~StopLineInterfaceSender()
{
	printf("StopLine Shutdown...\r\n");
}

void StopLineInterfaceSender::SendStopLines(StopLineMessage *sl) 
{	
	sl->msgType = SL_StopLineMsg;
	sl->sequenceNumber = sequenceNumber++;
	conn->send_message(sl,sizeof(StopLineMessage),inet_addr(UDP_STOPLINE_MULTICAST_ADDR),UDP_STOPLINE_PORT);
}

