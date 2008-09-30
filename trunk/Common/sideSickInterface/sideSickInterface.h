#pragma once

#include "..\network\udp_connection.h"
#include "..\utility\fastdelegate.h"

#define UDP_SIDESICK_PORT 30038
#define UDP_SIDESICK_ADDR "239.132.1.38"

#define TX_MAX_SS_OBSTACLES 10

#define CHANNEL_VERSION 2
#define SERIALIZER_TYPE_SS 6


#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

	enum SIDESICKID:int
	{
		SS_DRIVER=0,
		SS_PASSENGER=1
	};

	enum SideSickMessageID : int
	{
		SS_Info = 0,
		SS_ScanMsg= 1,
		SS_BAD = 99
	};
	#pragma pack(1)
	struct SideSickObstacle
	{			
		float distance;
		int numpoints;
		float height;
	};
	#pragma pack()
	#pragma pack(1)
	struct SideSickMsg
	{
		unsigned char channelVersion;
		unsigned char serializerType;
		int sequenceNumber;
		SideSickMessageID msgType;		
		SIDESICKID scannerID;
		double carTime;
		int numObstacles;
		SideSickObstacle obstacles[TX_MAX_SS_OBSTACLES];
	};
	#pragma pack()
	class SideSickSender;
	class SideSickReceiver;

typedef FastDelegate4<SideSickMsg, SideSickReceiver*, SIDESICKID, void*> SideSick_Msg_Handler;

using namespace std;
class SideSickReceiver
{
private:
	udp_connection *conn;		
	void UDPCallback(udp_message& msg, udp_connection* conn, void* arg);
	SideSick_Msg_Handler cbk;
	void* cbk_arg;
	
public:
	SideSickReceiver(void);
	~SideSickReceiver(void);	
	void SetCallback(SideSick_Msg_Handler handler, void* arg);	
	int packetCount; int dropCount; int sequenceNumber;
};

class SideSickSender
{
private:
	udp_connection *conn;
	void UDPCallback(udp_message msg, udp_connection* conn, void* arg);
	int sequenceNumber;
public:
	SideSickSender(void);
	~SideSickSender(void);

	void Send(SideSickMsg* msg, SIDESICKID id);	
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif