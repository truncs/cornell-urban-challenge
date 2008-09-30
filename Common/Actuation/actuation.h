#pragma once

#include "../network/udp_connection.h"
#include "../utility/fastdelegate.h"

#define UDP_ACTUATION_PORT 30006
#define UDP_ACTUATION_ADDR "239.132.1.6"

#define COUNTS_PER_REV	54.0f
#define WHEELRADIUS		0.4013962f							
#define PI				3.1415926535897932384626433832795f
#define MIN_DT			.29f

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

class ActuationReceiver;

using namespace std;

#pragma pack(1)
struct ActuationHeader
{
	unsigned short seconds;
	unsigned long	 ticks;
	unsigned char	 id;
	unsigned short length;
};
#pragma pack()

#pragma pack(1)
// data packet structure. This is in BIGENDIAN. use ntohl ntohs to convert stuff.
struct ActuationFeedback {
	ActuationHeader header;
	unsigned short act_lfws;
	unsigned short act_rfws;
	unsigned short act_rrws;
	unsigned short act_lrws;
	unsigned short engineRPM;
	short steering;
	unsigned char	 gear;
	unsigned char	 gearDirection;
	unsigned short engineTorqueAct;
	unsigned short engineTorqueReq;
	unsigned char	 cylDeact;
	unsigned char	 pedalPos;
	unsigned char	 brakePressure;	
};
#pragma pack()

#pragma pack(1)
struct ActuationWheelspeed
{
	ActuationHeader header;
	unsigned short lfws_raw;
	unsigned short rfws_raw;
	unsigned short rrws_raw;
	unsigned short lrws_raw;
	
	unsigned short front_rawTSsecs;
	unsigned short front_rawTSticks;
	unsigned short rear_rawTSsecs;
	unsigned short rear_rawTSticks;

	unsigned char flags;
	
};
#pragma pack()

typedef FastDelegate3<ActuationFeedback, ActuationReceiver*, void*> Actuation_Feedback_Msg_handler;
typedef FastDelegate3<ActuationWheelspeed, ActuationReceiver*, void*> Actuation_Wheelspeed_Msg_handler;

class ActuationReceiver
{
private:
	udp_connection *conn;		
	void UDPCallback(udp_message& msg, udp_connection* conn, void* arg);	
	Actuation_Feedback_Msg_handler actuation_feedback_cbk;		
	void* actuation_feedback_cbk_arg;				
	Actuation_Wheelspeed_Msg_handler actuation_wheelspeed_cbk;		
	void* actuation_wheelspeed_cbk_arg;				
	int packetNum;

public:
	ActuationReceiver(void);
	~ActuationReceiver(void);	
	void SetFeedbackCallback(Actuation_Feedback_Msg_handler handler, void* arg);				
	void SetWheelspeedCallback(Actuation_Wheelspeed_Msg_handler handler, void* arg);				
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif
