#pragma once

#include "..\network\udp_connection.h"
#include "..\utility\fastdelegate.h"

#define UDP_SIDEROADEDGE_PORT 30042
#define UDP_SIDEROADEDGE_ADDR "239.132.1.42"

#define CHANNEL_VERSION 2
#define SERIALIZER_TYPE_SRE 7


#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

	enum SIDEROADEDGEID:int
	{
		SRE_DRIVER=0,
		SRE_PASSENGER=1
	};

	enum SideRoadEdgeMsgID : int
	{
		SRE_Info = 0,
		SRE_RoadEdgeMsg= 1,
		SRE_BAD = 99
	};

	#pragma pack(1)
	struct SideRoadEdgeMsg
	{
		unsigned char channelVersion;
		unsigned char serializerType;
		int sequenceNumber;
		SideRoadEdgeMsgID msgType;		
		SIDEROADEDGEID scannerID;
		double carTime;
		double curbHeading;
		double curbDistance;
		bool isValid;
		double probabilityValid;
	};
	#pragma pack()
	
	class SideRoadEdgeSender;
	class SideRoadEdgeReceiver;

typedef FastDelegate4<SideRoadEdgeMsg, SideRoadEdgeReceiver*, SIDEROADEDGEID, void*> SideRoadEdge_Msg_Handler;

using namespace std;
class SideRoadEdgeReceiver
{
private:
	udp_connection *conn;		
	void UDPCallback(udp_message& msg, udp_connection* conn, void* arg);
	SideRoadEdge_Msg_Handler cbk;
	void* cbk_arg;
	
public:
	SideRoadEdgeReceiver(void);
	~SideRoadEdgeReceiver(void);	
	void SetCallback(SideRoadEdge_Msg_Handler handler, void* arg);	
	int packetCount; int dropCount; int sequenceNumber;
};

class SideRoadEdgeSender
{
private:
	udp_connection *conn;
	void UDPCallback(udp_message msg, udp_connection* conn, void* arg);
	int sequenceNumber;
public:
	SideRoadEdgeSender(void);
	~SideRoadEdgeSender(void);

	void Send(SideRoadEdgeMsg* msg, SIDEROADEDGEID id);	
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif