#include "roadFitterInterface.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

//RECIEVER-----------------------------------------------------------------------------------------------------------
void RoadFitterInterfaceReceiver::SetRoadFitterOutputCallback(roadFitterOutput_Msg_handler handler, void* arg)
{
	roadFitterOutput_cbk = handler;
	roadFitterOutput_cbk_arg = arg;
}

RoadFitterInterfaceReceiver::RoadFitterInterfaceReceiver() 
{
	udp_params params = udp_params();
	params.remote_ip = inet_addr(UDP_RF_MULTICAST_ADDR);
	params.local_port = UDP_RF_PORT;
	params.reuse_addr = 1;
	conn = new udp_connection (params);	
	conn->set_callback (MakeDelegate(this,&RoadFitterInterfaceReceiver::UDPCallback),conn);	
	sequenceNumber=0;
	packetCount=0;
	dropCount=0;
	printf("Road Fitter RX Interface Initialized. %s:%d\r\n",UDP_RF_MULTICAST_ADDR,UDP_RF_PORT);
}

RoadFitterInterfaceReceiver::~RoadFitterInterfaceReceiver ()
{
	delete conn;
	printf("RoadFitter Shutdown...\r\n");
}

void RoadFitterInterfaceReceiver::UDPCallback(udp_message& msg, udp_connection* conn, void* arg)
{ 
	//messages come in like this:
	// ID + memory mapped message, ID is 1 byte	
	RoadFitterID id = ((RoadFitterID*)msg.data)[0];
	RoadFitterMessageID m = ((RoadFitterMessageID*)msg.data)[1];	
	int rxSequenceNumber=	((int*)msg.data)[2];
	dropCount += (rxSequenceNumber-(sequenceNumber+1));
	sequenceNumber = rxSequenceNumber;
	packetCount++;
	switch(m)
	{
		case RF_Info:
			printf("RF: INFO: %s \r\n", msg.data); //RoadFitterID
		break;
		case RF_RoadOutputMsg:
			//cast this stuff		
			if (msg.len != sizeof (RoadFitterOutput))
				printf("RF: WARNING MESSAGE IS WRONG SIZE: is: %d should be %d\r\n",msg.len,sizeof(RoadFitterOutput));
			else
				if (!(roadFitterOutput_cbk.empty()))	roadFitterOutput_cbk(*((RoadFitterOutput*)(msg.data)), this, id, roadFitterOutput_cbk_arg);
		break;				
		case RF_BAD:
			printf("RF: WARNING BAD MESSAGE!\r\n");
			break;
		default:
			printf("RF: WARNING UNKNOWN MESSAGE TYPE! Type is %d \r\n", (int)msg.data[0]);
			break;
	}
	if (packetCount%100==0)	
	{
		#ifdef PRINT_PACKET_COUNT_DEBUG
		printf("RF: Packets: %d Seq: %d Dropped: %d Drop Rate: %f \r\n",packetCount,sequenceNumber,dropCount,((float)dropCount/(float)packetCount)*100.0f);	
		#endif
		packetCount=0; dropCount=0;
	}
}

//TRANSMITTER----------------------------------------------------------------------------------------------------

RoadFitterInterfaceSender::RoadFitterInterfaceSender()
{
	udp_params params = udp_params();
	params.remote_ip = inet_addr(UDP_RF_MULTICAST_ADDR);	
	params.local_port = 0;
	params.multicast_loopback = true;
	params.multicast_ttl=10;
	conn = new udp_connection (params);		
	sequenceNumber=0;
	printf("RF TX Interface Initialized. %s:%d\r\n",UDP_RF_MULTICAST_ADDR,UDP_RF_PORT);
}


RoadFitterInterfaceSender::~RoadFitterInterfaceSender()
{
	printf("RF Shutdown...\r\n");
}


void RoadFitterInterfaceSender::SendRoadFitterOutput(RoadFitterOutput* output, RoadFitterID id) 
{
	output->msgType = RF_RoadOutputMsg;
	output->id = id;
	output->sequenceNumber = sequenceNumber++;
	conn->send_message(output,sizeof(RoadFitterOutput),inet_addr(UDP_RF_MULTICAST_ADDR),UDP_RF_PORT);
}

