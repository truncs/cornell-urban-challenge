#include "ln200.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

//RECIEVER-----------------------------------------------------------------------------------------------------------
void LN200InterfaceReceiver::SetCallback(LN200_Msg_handler handler, void* arg)
{
	LN200_cbk = handler;
	LN200_cbk_arg = arg;
}

void LN200InterfaceReceiver::SetDTCallback(LN200_Msg_handler handler, void* arg, double ms)
{
	LN200_DT_cbk = handler;
	LN200_DT_cbk_arg = arg;	
	//so if we want this thing every desiredms, and each time this is called is 2.5ms...
	modDT = (int)(ms/2.5);
	if (modDT == 0) modDT = 1; //make sure that we at least go as fast as we can	
}

LN200InterfaceReceiver::LN200InterfaceReceiver() 
{
	packetNum=0;
	udp_params params = udp_params();
	params.remote_ip = inet_addr(UDP_LN200_ADDR);
	params.local_port = UDP_LN200_PORT;
	params.reuse_addr = 1;
	conn = new udp_connection (params);	

	modDT=1;
	conn->set_callback (MakeDelegate(this,&LN200InterfaceReceiver::UDPCallback),conn);
	printf("LN200 RX Interface Initialized. %s:%d\r\n",UDP_LN200_ADDR,UDP_LN200_PORT);
}

LN200InterfaceReceiver::~LN200InterfaceReceiver ()
{
	delete conn;
	printf("LN200 Interface Shutdown...\r\n");
}


void LN200InterfaceReceiver::UDPCallback(udp_message& msg, udp_connection* conn, void* arg)
{ 	
	LN200_data_packet_struct packet;
	
	packet = *((LN200_data_packet_struct*)msg.data);

	if (packet.flag != 0)
	{
		return;
	}

	if (LN200_cbk.empty() == false)
	{
		LN200_cbk(packet,this,LN200_cbk_arg);
	}
	
	if ((packetNum%modDT == 0) && (LN200_DT_cbk.empty ()==false))
	{
		LN200_DT_cbk (packet,this,LN200_cbk_arg);
	}

	packetNum++;
}
