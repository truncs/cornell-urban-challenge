#pragma once

#include "..\..\Common\network\udp_connection.h"

#define IBEO_SYNC_MC_IP "239.132.1.9"
#define IBEO_SYNC_PORT 30009

struct IbeoSyncData{
  double vehicleTS;       // in seconds
  double localTS;         // in seconds
};

class IbeoSyncClient;

typedef void(*IbeoSyncCallback)(double vehicleTS, double localTS, bool delayedTrigger, unsigned int seqNum);

class IbeoSyncClient
{
private:
	udp_connection *conn;		
	static void UDPCallback(udp_message& msg, udp_connection* conn, void* arg);
	
  static IbeoSyncCallback callback;

public:
	IbeoSyncClient();
	~IbeoSyncClient();
	
	void SetCallback(IbeoSyncCallback callback);
};
