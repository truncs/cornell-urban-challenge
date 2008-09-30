#include "IbeoSyncClient.h"
#include "..\time\timing.h"

IbeoSyncCallback IbeoSyncClient::callback;

IbeoSyncClient::IbeoSyncClient(){
	udp_params params = udp_params();
	params.remote_ip = inet_addr(IBEO_SYNC_MC_IP);
	params.local_port = IBEO_SYNC_PORT;
  params.reuse_addr = 1;
	conn = new udp_connection (params);	
	conn->set_callback (&UDPCallback, NULL);

  callback = NULL;
	printf("IbeoSync RX Interface Initialized. %s:%d\r\n",IBEO_SYNC_MC_IP,IBEO_SYNC_PORT);
}

IbeoSyncClient::~IbeoSyncClient (){
	delete conn;
}

void IbeoSyncClient::SetCallback(IbeoSyncCallback callback)
{
	this->callback = callback;
}

//extern TIMER MOO;
void IbeoSyncClient::UDPCallback(udp_message& msg, udp_connection* conn, void* arg){
  if(msg.len != 10 && msg.len != 11) return;
  
  bool delayed = false;
  if(msg.len==11 && msg.data[10]!=0) delayed = true; // non-0 ending packets signify a 50-ms delayed "trigger" packet

  USHORT secs = *(USHORT*)(msg.data);
  secs = ntohs(secs);
  ULONG ticks =  *(ULONG*)(msg.data+2);
  ticks = ntohl(ticks);

  double local_ts = timeElapsed(ref_time);
  double veh_ts = (double)secs + (double)ticks*.0001;

  UINT seq = *(UINT*)(msg.data+6);

  if (callback!=NULL) callback(veh_ts,local_ts,delayed,seq);
}