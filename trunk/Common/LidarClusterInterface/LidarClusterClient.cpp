#include "LidarClusterClient.h"

typedef unsigned int uint;

LidarClusterClient::LidarClusterClient(const char* multicast_ip,const USHORT port){
	udp_params params = udp_params();
	params.remote_ip = inet_addr(multicast_ip);
	params.local_port = port;
  params.reuse_addr = true;
	conn = new udp_connection (params);	
	conn->set_callback (MakeDelegate(this,&LidarClusterClient::UDPCallback),conn);
	sequenceNumber=0;
	dropCount=0;
	packetCount=0;
	printf("LidarCluster RX Interface Initialized. %s:%d\r\n",multicast_ip,port);
}

LidarClusterClient::~LidarClusterClient (){
	delete conn;
}

void LidarClusterClient::SetCallback(LidarClusterCallbackType callback, void* arg)
{
	this->callback = callback;
	this->callback_arg = arg;
}

void LidarClusterClient::UDPCallback(udp_message& msg, udp_connection* conn, void* arg){
  packetCount++;
	if(msg.len < sizeof(ClusterPacketHdr))
		return;   // packet too small!
  if(msg.data[0] != CLUSTER_PACKET_TYPE)
		return;   // don't know how to handle anything but scan data packets

  ClusterPacketHdr * hdr = (ClusterPacketHdr*)msg.data;
    
  size_t calced_size = sizeof(ClusterPacketHdr) + hdr->numPts*sizeof(ClusterPacketPoint) + hdr->numClusters*sizeof(ClusterPacketCluster);
  if(msg.len!=calced_size)
		return; // ERRROR! size mismatch

	int rxSequenceNumber=	(int)hdr->packetNum;
	dropCount += (rxSequenceNumber-(sequenceNumber+1));
	sequenceNumber = rxSequenceNumber;	

  vector<v3f> pts;
  uint i;
  for(i=0;i<hdr->numPts;i++){
    ClusterPacketPoint* p = (ClusterPacketPoint*)(msg.data+sizeof(ClusterPacketHdr)+i*sizeof(ClusterPacketPoint));
    v3f pt = v3f((float)p->X/100.f,(float)p->Y/100.f,(float)p->Z/100.f);
    pts.push_back(pt);
  }
  vector<LidarCluster> ret;
  for(i=0;i<hdr->numClusters;i++){
    ClusterPacketCluster* c = (ClusterPacketCluster*)(msg.data+sizeof(ClusterPacketHdr)+hdr->numPts*sizeof(ClusterPacketPoint)+i*sizeof(ClusterPacketCluster));
    LidarCluster lc;
    lc.stable = (c->flags&0x01)?false:true;
    lc.leftOccluded = (c->flags&0x02)?true:false;
    lc.rightOccluded = (c->flags&0x04)?true:false;
    lc.highObstacle = (c->flags&0x08)?true:false;
    for(uint j=c->firstPtIndex;j<=c->lastPtIndex;j++)
      lc.pts.push_back(pts[j]);
    ret.push_back(lc);
  }

  double vts = (double)hdr->tsSeconds + (double)hdr->tsTicks/10000.0;

  if (!(callback.empty()))	callback(ret, vts, callback_arg);
	if (packetCount%100==0)	
	{
		#ifdef PRINT_PACKET_COUNT_DEBUG
		printf("LC: Packets: %d Seq: %d Dropped: %d Drop Rate: %f \r\n",packetCount,sequenceNumber,dropCount,((float)dropCount/(float)packetCount)*100.0f);	
		#endif
		packetCount=0; dropCount=0;
	}
}

