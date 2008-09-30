#pragma once

#include "..\network\udp_connection.h"
#include "..\utility/fastdelegate.h"
#include "RoadFitterOutput.h"

#define UDP_RF_PORT 30037
#define UDP_RF_MULTICAST_ADDR "239.132.1.37"

#define RF_MAX_FITS 10

#define RF_VALID_FITS 1

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

enum RoadFitterID : int
{
	RF_FL,
	RF_FR,
	RF_CTR,
	RF_REAR
};

enum RoadFitterMessageID : int
{
	RF_Info = 0,		
	RF_RoadOutputMsg= 2,
	RF_BAD = 99
};

struct RoadFitterOutput
{
	RoadFitterID id;
	RoadFitterMessageID msgType;
	int sequenceNumber;
	double carTime;
	RoadFitterOutputData roadFits[RF_MAX_FITS];
};


class RoadFitterInterfaceSender;
class RoadFitterInterfaceReceiver;

typedef FastDelegate4<RoadFitterOutput, RoadFitterInterfaceReceiver*, RoadFitterID, void*> roadFitterOutput_Msg_handler;

using namespace std;
class RoadFitterInterfaceReceiver
{
private:
	udp_connection *conn;		
	void UDPCallback(udp_message& msg, udp_connection* conn, void* arg);
	roadFitterOutput_Msg_handler roadFitterOutput_cbk;		
	void* roadFitterOutput_cbk_arg;		

public:
	RoadFitterInterfaceReceiver(void);
	~RoadFitterInterfaceReceiver(void);
	void SetRoadFitterOutputCallback(roadFitterOutput_Msg_handler handler, void* arg);		
	int sequenceNumber;
	int packetCount;
	int dropCount;
};

class RoadFitterInterfaceSender
{
private:
	udp_connection *conn;
	int sequenceNumber;
	void UDPCallback(udp_message msg, udp_connection* conn, void* arg);
public:
	RoadFitterInterfaceSender(void);
	~RoadFitterInterfaceSender(void);
	void SendRoadFitterOutput(RoadFitterOutput* output, RoadFitterID id) ;	
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif
