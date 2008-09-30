#pragma once

#include "..\network\udp_connection.h"
#include "..\utility/fastdelegate.h"
#include <vector>



#define UDP_LN200_PORT 92
#define UDP_LN200_ADDR "239.132.1.4"

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

class LN200InterfaceReceiver;

using namespace std;

typedef unsigned char byte;
typedef unsigned short seq_no_t;

#pragma pack(1)
// data packet structure
// This is in BIGENDIAN. use ntohl ntohs to convert stuff.
struct LN200_data_packet_struct {
	// sequence number
	seq_no_t seq_no;
	// car time, seconds
	unsigned short ts_s;
	// car time, ticks (0.1 ms/tick)
	unsigned int ts_t;
	// data flag
	byte flag;
	// data bytes
	byte data[28];
};
#pragma pack()

typedef FastDelegate3<LN200_data_packet_struct, LN200InterfaceReceiver*, void*> LN200_Msg_handler;

class LN200InterfaceReceiver
{
private:
	udp_connection *conn;		
	void UDPCallback(udp_message& msg, udp_connection* conn, void* arg);	
	LN200_Msg_handler LN200_cbk;		
	void* LN200_cbk_arg;		
	LN200_Msg_handler LN200_DT_cbk;		
	void* LN200_DT_cbk_arg;		
	int modDT;
	int packetNum;

public:
	LN200InterfaceReceiver(void);
	~LN200InterfaceReceiver(void);	
	void SetCallback(LN200_Msg_handler handler, void* arg);			
	void SetDTCallback(LN200_Msg_handler handler, void* arg, double ms);	
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif
