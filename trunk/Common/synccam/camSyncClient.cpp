#include "camsyncclient.h"
//RECIEVER-----------------------------------------------------------------------------------------------------------
void CamSyncReceiver::SetCallback(CamSync_Msg_handler handler, void* arg)
{
	cbk = handler;
	cbk_arg = arg;
}

CamSyncReceiver::CamSyncReceiver() 
{
	udp_params params = udp_params();
	params.remote_ip = inet_addr(CAMSYNC_BROADCAST_IP);
	params.local_port = CAMSYNC_BROADCAST_PORT;
	params.reuse_addr = 1;
	conn = new udp_connection (params);	
	conn->set_callback (MakeDelegate(this,&CamSyncReceiver::UDPCallback),conn);	
	sequenceNumber=0;
	packetCount=0;
	dropCount=0;
	printf("Camera Sync RX Interface Initialized. %s:%d\r\n",CAMSYNC_BROADCAST_IP,CAMSYNC_BROADCAST_PORT);
}

CamSyncReceiver::~CamSyncReceiver ()
{
	delete conn;
	printf("Camera Sync Shutdown...\r\n");
}

void CamSyncReceiver::UDPCallback(udp_message& msg, udp_connection* conn, void* arg)
{ 
	//messages come in like this:
	// ID + memory mapped message, ID is 1 byte	
	SyncCamPacket smsg = *((SyncCamPacket*)msg.data);
	
	int rxSequenceNumber= ntohl(smsg.seqNum);
	dropCount += (rxSequenceNumber-(sequenceNumber+1));
	sequenceNumber = rxSequenceNumber;
	packetCount++;
	
	//raise event				
	if (!(cbk.empty()))	cbk(smsg, this, cbk_arg);


	if (packetCount%100==0)	
	{
#ifdef PRINT_PACKET_COUNT_DEBUG
		printf("SC: Packets: %d Seq: %d Dropped: %d Drop Rate: %f \r\n",packetCount,sequenceNumber,dropCount,((float)dropCount/(float)packetCount)*100.0f);	
#endif
		packetCount=0; dropCount=0;
	}
}