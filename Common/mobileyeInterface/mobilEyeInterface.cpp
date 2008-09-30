#include "mobileyeInterface.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif


//RECIEVER-----------------------------------------------------------------------------------------------------------
void MobilEyeInterfaceReceiver::SetRoadEnvInfoCallback(mobilEye_RoadEnv_Msg_handler handler, void* arg)
{
	roadenv_cbk = handler;
	roadenv_cbk_arg = arg;
}

void MobilEyeInterfaceReceiver::SetObstacleCallback(mobilEye_Obstacle_Msg_handler handler, void* arg)
{
	obs_cbk = handler;
	obs_cbk_arg = arg;
}

MobilEyeInterfaceReceiver::MobilEyeInterfaceReceiver() 
{
	udp_params params = udp_params();
	params.remote_ip = inet_addr(UDP_MOBILEYE_MULTICAST_ADDR);
	params.local_port = UDP_MOBILEYE_MULTICAST_PORT;
	params.reuse_addr = 1;
	conn = new udp_connection (params);	
	conn->set_callback (MakeDelegate(this,&MobilEyeInterfaceReceiver::UDPCallback),conn);
	sequenceNumber=0;
	packetCount=0;
	dropCount=0;
	printf("MobilEye RX Interface Initialized. %s:%d\r\n",UDP_MOBILEYE_MULTICAST_ADDR,UDP_MOBILEYE_MULTICAST_PORT);
}

MobilEyeInterfaceReceiver::~MobilEyeInterfaceReceiver ()
{
	delete conn;
	printf("MobilEye Shutdown...\r\n");
}


void MobilEyeInterfaceReceiver::UDPCallback(udp_message& msg, udp_connection* conn, void* arg)
{ 
	//messages come in like this:
	// ID + msgID + memory mapped message, ID is 1 byte	
	MobilEyeID id = ((MobilEyeID*)msg.data)[0];
	MobilEyeMessageID m = ((MobilEyeMessageID*)msg.data)[1];
	int rxSequenceNumber=	((int*)msg.data)[2];
	dropCount += (rxSequenceNumber-(sequenceNumber+1));
	sequenceNumber = rxSequenceNumber;
	packetCount++;
	switch(m)
	{
		case ME_Info:
			printf("ME: INFO: %s \r\n", msg.data);
		break;
		case ME_RoadEnv:
			//cast this stuff		
			if (msg.len != sizeof (MobilEyeRoadEnv))
				printf("ME: WARNING MESSAGE IS WRONG SIZE: is: %d should be %d\r\n",msg.len,sizeof(MobilEyeRoadEnv));
			else
				if (!(roadenv_cbk.empty()))	roadenv_cbk(*((MobilEyeRoadEnv*)(msg.data)), this, (MobilEyeID)id, roadenv_cbk_arg);
		break;		
		case ME_Obs:
			//cast this stuff
			if (msg.len != sizeof (MobilEyeObstacles))
				printf("ME: WARNING MESSAGE IS WRONG SIZE: is: %d should be %d\r\n",msg.len,sizeof(MobilEyeObstacles));
			else
				if (!(obs_cbk.empty()))	obs_cbk(*((MobilEyeObstacles*)(msg.data)), this, (MobilEyeID)id,obs_cbk_arg);
		break;
		case ME_BAD:
			printf("ME: WARNING BAD MESSAGE!\r\n");
			break;
		default:
			printf("ME: WARNING UNKNOWN MESSAGE TYPE! Type is %d \r\n", (int)msg.data[0]);
			break;
	}
	if (packetCount%100==0)	
	{
		#ifdef PRINT_PACKET_COUNT_DEBUG
		printf("ME: Packets: %d Seq: %d Dropped: %d Drop Rate: %f \r\n",packetCount,sequenceNumber,dropCount,((float)dropCount/(float)packetCount)*100.0f);	
		#endif
		packetCount=0; dropCount=0;
	}
}

//TRANSMITTER----------------------------------------------------------------------------------------------------

MobilEyeInterfaceSender::MobilEyeInterfaceSender()
{
	udp_params params = udp_params();
	params.remote_ip = inet_addr(UDP_MOBILEYE_MULTICAST_ADDR);	
	params.local_port = 0;
	params.multicast_loopback = true;
	params.multicast_ttl=10;
	sequenceNumber=0;
	conn = new udp_connection (params);		
	printf("MobilEye TX Interface Initialized. %s:%d\r\n",UDP_MOBILEYE_MULTICAST_ADDR,UDP_MOBILEYE_MULTICAST_PORT);
}


MobilEyeInterfaceSender::~MobilEyeInterfaceSender()
{
	printf("MobilEye Shutdown...\r\n");
}


void MobilEyeInterfaceSender::SendObstacles(MobilEyeObstacles *obs, MobilEyeID id) 
{
	obs->msgType = ME_Obs;
	obs->id = id;
	obs->sequenceNumber = sequenceNumber++;
	conn->send_message(obs,sizeof(MobilEyeObstacles),inet_addr(UDP_MOBILEYE_MULTICAST_ADDR),UDP_MOBILEYE_MULTICAST_PORT);	
}

void MobilEyeInterfaceSender::SendRoadEnvInfo(MobilEyeRoadInformation *road, MobilEyeEnvironment *env, MobilEyeID id) 
{
	MobilEyeRoadEnv mre;	
	mre.sequenceNumber = sequenceNumber++;
	mre.msgType = ME_RoadEnv;
	mre.env = *env;
	mre.road = *road;
	mre.id = id;	
	conn->send_message(&mre,sizeof(MobilEyeRoadEnv),inet_addr(UDP_MOBILEYE_MULTICAST_ADDR),UDP_MOBILEYE_MULTICAST_PORT);
	
}
