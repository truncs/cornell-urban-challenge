#include "sickInterface.h"

//RECIEVER-----------------------------------------------------------------------------------------------------------
void SickInterfaceReceiver::SetSickScanCallback(SickScan_Msg_handler handler, void* arg)
{
	sickScan_cbk = handler;
	sickScan_cbk_arg = arg;
}

SickInterfaceReceiver::SickInterfaceReceiver(int id) 
{
	this->is180=false; this->angularSeperationDegrees=0.5; this->id=id; this->ismmMode = false;
	packetNum=0;
	udp_params params = udp_params();
	params.remote_ip = inet_addr(UDP_SICK_ADDR);
	params.local_port = UDP_SICK_PORT;
	params.reuse_addr = 1;

	
	conn = new udp_connection (params);	
	conn->set_callback (MakeDelegate(this,&SickInterfaceReceiver::UDPCallback),conn);
	printf("Sick RX Interface Initialized. %s:%d\r\n",UDP_SICK_ADDR,UDP_SICK_PORT);
}


SickInterfaceReceiver::SickInterfaceReceiver(bool is180, bool ismmMode, double angularSeperationDegrees, int id) 
{
	this->id=id;
	this->is180=is180; 
	this->angularSeperationDegrees=angularSeperationDegrees;
	this->ismmMode = ismmMode;
	packetNum=0;
	udp_params params = udp_params();
	params.remote_ip = inet_addr(UDP_SICK_ADDR);
	params.local_port = UDP_SICK_PORT;
	params.reuse_addr = 1;
	conn = new udp_connection (params);	
	conn->set_callback (MakeDelegate(this,&SickInterfaceReceiver::UDPCallback),conn);
	printf("Sick RX Interface Initialized. %s:%d\r\n",UDP_SICK_ADDR,UDP_SICK_PORT);
}

SickInterfaceReceiver::~SickInterfaceReceiver ()
{
	delete conn;
	printf("Sick Interface Shutdown...\r\n");
}


void SickInterfaceReceiver::UDPCallback(udp_message& msg, udp_connection* conn, void* arg)
{ 	
	unsigned char* data = (unsigned char*)msg.data;
	SickScan s;
	int pos=0;	
	
	int p_id = data[pos++];
	//printf("got id %d\n",p_id);
	if (p_id!=id)
			return;	

	//180 len is 740 //360 more bytes than 90....hmmmm
	if (is180)
	{
		if (740>msg.len) {printf("SICK: Unexpected end of message in LIDAR!\n"); return;}
	}
	else
	{
		if (380>msg.len) {printf("SICK: Unexpected end of message in LIDAR!\n"); return;}
	}
	int mode = data[pos++];
	int secs = (data[pos] << 8) + data[pos+1];
	pos+=2;
	int ticks = (data[pos] << 24) + (data[pos+1] << 16) +(data[pos+2] << 8) + data[pos+3];
	pos+=4;
	if (((data[pos++])&0xFF) != LIDAR_STX)
	{printf("SICK: BAD STX HEADER in LIDAR!\n"); return;}
	if (((data[pos++])&0xFF) != LIDAR_ADDR)
	{printf("SICK:  BAD ADDR HEADER in LIDAR!\n is: %02x should be: %02x",data[pos-1],LIDAR_ADDR); return;}
	int length = (data[pos++]);
			length += (data[pos++]<<8);
	int cmd = data [pos++];
	pos++; //two bullstuff bytes
	pos++;
	s.timestamp = (double)secs + (double)ticks/10000.0f;
	s.packetNum = packetNum;
	s.scannerID = id;
//pos is 15 here....
	if(is180)
	{
		//724 bytes...
		for (double i=-90; i<=90; i+=angularSeperationDegrees)
		{				
			float rTemp = (data [pos++]);
			rTemp += (data[pos++]*256);		
			if (ismmMode)
				s.points.push_back (SickPoint (RThetaCoordinate(rTemp * .001f, (i * (PI / 180.0)))));
			else
				s.points.push_back (SickPoint (RThetaCoordinate(rTemp * .01f, (i * (PI / 180.0)))));
		}		
	}
	else
	{
		for (double i=-45; i<=45; i+=angularSeperationDegrees)
		{	
			float rTemp = (data [pos++]);
			rTemp += (data[pos++]*256);
			if (ismmMode)
				s.points.push_back (SickPoint (RThetaCoordinate(rTemp * .001f, (i * (PI / 180.0)))));
			else
				s.points.push_back (SickPoint (RThetaCoordinate(rTemp * .01f, (i * (PI / 180.0)))));
		}
	}
	if (is180)
	{
		if (pos!=737)
			printf("Did not get to end of packet: got to %d\n",pos);
	}
	if (sickScan_cbk.empty() == false)
	{
		sickScan_cbk(s,this,sickScan_cbk_arg);
	}
	packetNum++;
}
