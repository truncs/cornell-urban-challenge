#pragma once

#include "LidarClusterCommon.h"
#include "..\network\udp_connection.h"

#include "..\utility\fastdelegate.h"
#include <vector>
using namespace std;

class ClusterClient;

typedef FastDelegate3<const vector<LidarCluster>&, double, void*> LidarClusterCallbackType;

class LidarClusterClient
{
private:
	udp_connection *conn;		
	void UDPCallback(udp_message& msg, udp_connection* conn, void* arg);
	
  LidarClusterCallbackType callback;
	void* callback_arg;
	
public:
	LidarClusterClient(const char* multicast_ip=CLUSTER_DEFAULT_MC_IP,const USHORT port=CLUSTER_DEFAULT_PORT);
	~LidarClusterClient();

	void SetCallback(LidarClusterCallbackType callback, void* arg);
	int packetCount;
	int dropCount;
	int sequenceNumber;
};
