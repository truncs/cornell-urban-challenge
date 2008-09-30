#include "IbeoClient.h"

IbeoClient::IbeoClient(const char* multicast_ip,const USHORT port){
	udp_params params = udp_params();
	params.remote_ip = inet_addr(multicast_ip);
	params.local_port = port;
  params.reuse_addr = true;
	conn = new udp_connection (params);	
	conn->set_callback (MakeDelegate(this,&IbeoClient::UDPCallback),conn);

	printf("Ibeo RX Interface Initialized. %s:%d\r\n",multicast_ip,port);
}

IbeoClient::~IbeoClient (){
	delete conn;
}

void IbeoClient::SetCallback(IbeoCallbackType callback, void* arg)
{
	this->callback = callback;
	this->callback_arg = arg;
	//printf("Set callback OK\n");
}

void IbeoClient::UDPCallback(udp_message& msg, udp_connection* conn, void* arg){
  if(msg.len < sizeof(IbeoScanDataPacketHdr))
	{
		//printf("Bad header!\n");
		return;   // packet too small!
	}
  if(msg.data[0] != IBEO_SCAN_DATA_PACKET_TYPE)
	{
		//printf("Bad Type!\n");	
		return;   // don't know how to handle anything but scan data packets
  }

  IbeoScanDataPacketHdr * hdr = (IbeoScanDataPacketHdr*)msg.data;
    
  size_t calced_size = sizeof(IbeoScanDataPacketHdr) + hdr->numPts*sizeof(IbeoScanDataPacketPoint);
  if(msg.len!=calced_size)
	{
		//printf("Bad size!\n");   
		return; // ERRROR! size mismatch
	}

  IbeoScanData data;
  data.vehicleTS = hdr->tsSeconds + (double)hdr->tsTicks*.0001;
  data.scannerID = hdr->scannerID;
  data.pts.resize(hdr->numPts);
  data.packetNum = hdr->packetNum;

  for(USHORT p=0;p<hdr->numPts;p++){
    IbeoScanDataPacketPoint* pt = (IbeoScanDataPacketPoint*)(msg.data + sizeof(IbeoScanDataPacketHdr) + p*sizeof(IbeoScanDataPacketPoint));
    
    if(pt->X == IBEO_INVALID_MEAS)  data.pts[p].x = IBEO_INVALID;
    else                            data.pts[p].x = .01f * (float)(pt->X);

    if(pt->Y == IBEO_INVALID_MEAS)  data.pts[p].y = IBEO_INVALID;
    else                            data.pts[p].y = .01f * (float)(pt->Y);

    if(pt->Z == IBEO_INVALID_MEAS)  data.pts[p].z = IBEO_INVALID;
    else                            data.pts[p].z = .01f * (float)(pt->Z);

    data.pts[p].finalHit = (pt->chanInfo & 0x4)!=0;
    data.pts[p].chan = (pt->chanInfo>>4)&0x3;
    data.pts[p].subchan = (pt->chanInfo)&0x3;
    data.pts[p].status = pt->status & 0x7F;
  }
	//printf("Got data!\n");
  if (!(callback.empty()))	callback(&data, this, callback_arg);
}
