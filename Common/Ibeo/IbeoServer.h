#pragma once

#include "IbeoCommon.h"
#include "..\network\udp_connection.h"

class IbeoServer
{
private:
	udp_connection *conn;
	void UDPCallback(udp_message msg, udp_connection* conn, void* arg);

  unsigned long dest_ip;
  USHORT dest_port;
public:
	IbeoServer(const char* multicast_ip=IBEO_DEFAULT_MC_IP,const USHORT port=IBEO_DEFAULT_PORT);
	~IbeoServer();

	void Send(void* data, UINT len);	
};
