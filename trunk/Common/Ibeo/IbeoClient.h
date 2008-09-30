#pragma once

#include "IbeoCommon.h"
#include "..\network\udp_connection.h"

#include "..\utility\fastdelegate.h"
#include <vector>
using namespace std;

static const float IBEO_INVALID = 10000.f;

class PacketLogger;

class IbeoClient;

typedef FastDelegate3<IbeoScanData*, IbeoClient*, void*> IbeoCallbackType;

class IbeoClient
{
private:
	udp_connection *conn;		
	void UDPCallback(udp_message& msg, udp_connection* conn, void* arg);
	
  IbeoCallbackType callback;
	void* callback_arg;

public:
	IbeoClient(const char* multicast_ip=IBEO_DEFAULT_MC_IP,const USHORT port=IBEO_DEFAULT_PORT);
	~IbeoClient();
	
	void SetCallback(IbeoCallbackType callback, void* arg);
};
