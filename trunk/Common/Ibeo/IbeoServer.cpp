#include "IbeoServer.h"

IbeoServer::IbeoServer(const char* multicast_ip,const USHORT port){
	udp_params params = udp_params();
	params.remote_ip = inet_addr(multicast_ip);	
	params.local_port = 0;
	params.multicast_loopback = true;
	params.multicast_ttl=10;
	conn = new udp_connection (params);		

  dest_ip = inet_addr(multicast_ip);
  dest_port = port;

  printf("Ibeo Server running. Destination IP: %s ; port: %d\n",multicast_ip,port);
}

IbeoServer::~IbeoServer(){
  delete conn;
	printf("Ibeo Server closed\n");
}

void IbeoServer::Send(void* data, UINT len){	  
  conn->send_message(data,len,dest_ip,dest_port);
}

