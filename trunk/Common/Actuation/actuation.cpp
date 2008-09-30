#include "actuation.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

//RECIEVER-----------------------------------------------------------------------------------------------------------
void ActuationReceiver::SetFeedbackCallback(Actuation_Feedback_Msg_handler handler, void* arg)
{
	actuation_feedback_cbk = handler;
	actuation_feedback_cbk_arg = arg;
}

void ActuationReceiver::SetWheelspeedCallback(Actuation_Wheelspeed_Msg_handler handler, void* arg)
{
	actuation_wheelspeed_cbk = handler;
	actuation_wheelspeed_cbk_arg = arg;
}

ActuationReceiver::ActuationReceiver() 
{
	packetNum=0;
	udp_params params = udp_params();
	params.remote_ip = inet_addr(UDP_ACTUATION_ADDR);
	params.local_port = UDP_ACTUATION_PORT;
	params.reuse_addr = 1;
	conn = new udp_connection (params);		
	conn->set_callback (MakeDelegate(this,&ActuationReceiver::UDPCallback),conn);
	printf("Actuation RX Interface Initialized. %s:%d\r\n",UDP_ACTUATION_ADDR,UDP_ACTUATION_PORT);
}

ActuationReceiver::~ActuationReceiver ()
{
	delete conn;
	printf("Actuation Interface Shutdown...\r\n");
}

void ActuationReceiver::UDPCallback(udp_message& msg, udp_connection* conn, void* arg)
{ 	
	ActuationHeader header;
	if (msg.len < sizeof(ActuationHeader)) return;
	header = *((ActuationHeader*)msg.data);

	if (header.id == 20)
	{	
		ActuationFeedback feedback = *((ActuationFeedback*)msg.data);
		if (msg.len != sizeof (ActuationFeedback))
			printf("Warning: size mistatch. Should be: %d Is: %d\n",sizeof (ActuationFeedback),msg.len);
		if (actuation_feedback_cbk.empty ()==false)
		{
			actuation_feedback_cbk (feedback,this,actuation_feedback_cbk_arg);
		}
	}
	if (header.id == 23)
	{	
		ActuationWheelspeed ws = *((ActuationWheelspeed*)msg.data);		
		if (msg.len != sizeof (ActuationWheelspeed))
			printf("Warning: size mistatch. Should be: %d Is: %d\n",sizeof (ActuationWheelspeed),msg.len);
		if (actuation_wheelspeed_cbk.empty ()==false)
		{
			actuation_wheelspeed_cbk (ws,this,actuation_wheelspeed_cbk_arg);
		}
	}
	packetNum++;
}
