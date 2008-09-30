#pragma once

#include "..\network\udp_connection.h"
#include "..\utility\fastdelegate.h"

#define UDP_STOPLINE_PORT 3679
#define UDP_STOPLINE_MULTICAST_ADDR "239.132.1.11"

#define TX_MAX_LINES 100

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif


	enum StopLineMessageID : int
	{
		SL_Info = 0,
		SL_StopLineMsg= 1,
		SL_BAD = 99
	};
	#pragma pack(1)
	struct StopLine
	{	
		bool foundLine;
		float distance;
		float confidence;
	};
	#pragma pack()
	#pragma pack(1)
	struct StopLineMessage
	{
		StopLineMessageID msgType;		
		int sequenceNumber;
		float carTime;
		int numStopLines;
		StopLine stopLines[TX_MAX_LINES];
	};
	#pragma pack()
	
	class StopStopLineLineInterfaceSender;
	class StopStopLineLineInterfaceReceiver;

	class StopLineInterfaceReceiver;
	class StopLineInterfaceSender;

typedef FastDelegate3<StopLineMessage, StopLineInterfaceReceiver*, void*> stopLinemsg_handler;

using namespace std;
class StopLineInterfaceReceiver
{
private:
	udp_connection *conn;		
	void UDPCallback(udp_message& msg, udp_connection* conn, void* arg);
	stopLinemsg_handler sl_cbk;
	void* sl_cbk_arg;
	
public:
	StopLineInterfaceReceiver(void);
	~StopLineInterfaceReceiver(void);
	//This function returns up to the TX_MAX_LINES stop line candidate pairs
	//It will always send TX_MAX_LINES objects, so you must check the foundLine boolean
	//before using the measurement! The numStopLines will also only reflect the number of VALID measures
	void SetStopLineCallback(stopLinemsg_handler handler, void* arg);	
	int packetCount; int dropCount; int sequenceNumber;
};

class StopLineInterfaceSender
{
private:
	udp_connection *conn;
	void UDPCallback(udp_message msg, udp_connection* conn, void* arg);
	int sequenceNumber;
public:
	StopLineInterfaceSender(void);
	~StopLineInterfaceSender(void);

	void SendStopLines(StopLineMessage* sl);	
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif