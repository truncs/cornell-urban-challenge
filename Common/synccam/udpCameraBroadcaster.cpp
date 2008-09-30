#include "udpCameraBroadcaster.h"

//TRANSMITTER----------------------------------------------------------------------------------------------------

UDPCameraBroadcaster::UDPCameraBroadcaster()
{
	udp_params params = udp_params();
	params.remote_ip = inet_addr(UDP_CAMERA_ADDR);	
	params.local_port = 0;
	params.multicast_loopback = true;
	params.multicast_ttl=10;
	conn = new udp_connection (params);		
	printf("UDP Camera Broadcast TX Interface Initialized. %s:%d\r\n",UDP_CAMERA_ADDR,UDP_CAMERA_PORT);
}


UDPCameraBroadcaster::~UDPCameraBroadcaster()
{
	printf("UDP Camera Broadcast Shutdown...\r\n");
}


void UDPCameraBroadcaster::SendImage (const uchar* img, int width, int height, double ts, int index)
{
	WorstStreamEver s = WorstStreamEver (65000);
	SimpleJPEG jpeg;
	jpeg.WriteImage (img,width,height,s);
	int jpegSize = s.GetLength ();
	
	UDPCameraMsg hdr;
	hdr.height = height; hdr.width = width; hdr.index = index; hdr.timestamp = ts; hdr.size =jpegSize; 

	int rawmsgSize= jpegSize + sizeof(UDPCameraMsg);
	void* rawmsg = malloc (rawmsgSize);
	memcpy(rawmsg,&hdr,sizeof(UDPCameraMsg));
	memcpy(((unsigned char*)rawmsg)+sizeof(UDPCameraMsg),s.GetBuffer(),s.GetLength());
	conn->send_message(rawmsg,rawmsgSize,inet_addr(UDP_CAMERA_ADDR),UDP_CAMERA_PORT);
	free (rawmsg);		
}



//RECIEVER-----------------------------------------------------------------------------------------------------------
void UDPCameraReceiver::SetCallback(UDPCamera_Msg_handler handler, void* arg)
{
	cbk = handler;
	cbk_arg = arg;
}

UDPCameraReceiver::UDPCameraReceiver() 
{
	udp_params params = udp_params();
	params.remote_ip = inet_addr(UDP_CAMERA_ADDR);
	params.local_port = UDP_CAMERA_PORT;
	params.reuse_addr = 1;
	conn = new udp_connection (params);	
	conn->set_callback (MakeDelegate(this,&UDPCameraReceiver::UDPCallback),conn);	
	sequenceNumber=0;
	packetCount=0;
	dropCount=0;
	printf("UDP Camera RX Interface Initialized. %s:%d\r\n",UDP_CAMERA_ADDR,UDP_CAMERA_PORT);
}

UDPCameraReceiver::~UDPCameraReceiver ()
{
	delete conn;
	printf("UDP Camera Shutdown...\r\n");
}

void UDPCameraReceiver::UDPCallback(udp_message& msg, udp_connection* conn, void* arg)
{ 
	//messages come in like this:
	// ID + memory mapped message, ID is 1 byte	
	UDPCameraMsg umsg = *((UDPCameraMsg*)msg.data);
	
	int rxSequenceNumber=	umsg.index;
	dropCount += (rxSequenceNumber-(sequenceNumber+1));
	sequenceNumber = rxSequenceNumber;
	packetCount++;
	
	//raise event				
	if (!(cbk.empty()))	cbk(umsg, this, cbk_arg);


	if (packetCount%100==0)	
	{
		#ifdef PRINT_PACKET_COUNT_DEBUG
		printf("UC: Packets: %d Seq: %d Dropped: %d Drop Rate: %f \r\n",packetCount,sequenceNumber,dropCount,((float)dropCount/(float)packetCount)*100.0f);	
		#endif
		packetCount=0; dropCount=0;
	}
}