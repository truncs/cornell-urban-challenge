#pragma once

#include "..\network\udp_connection.h"
#include "..\utility\fastdelegate.h"

#define CAMSYNC_BROADCAST_IP "232.132.1.8"
#define CAMSYNC_BROADCAST_PORT 30008


#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif


#pragma pack (1)
struct SyncCamPacket
{
	unsigned short seconds;
	unsigned long	 ticks;
	unsigned long	 seqNum;
  unsigned char  id;
};
#pragma pack ()

class CamSyncReceiver;

typedef FastDelegate3<SyncCamPacket, CamSyncReceiver*, void*> CamSync_Msg_handler;

using namespace std;
class CamSyncReceiver
{
private:
	udp_connection *conn;		
	void UDPCallback(udp_message& msg, udp_connection* conn, void* arg);
	CamSync_Msg_handler cbk;		
	void* cbk_arg;		

public:
	CamSyncReceiver(void);
	~CamSyncReceiver(void);
	void SetCallback(CamSync_Msg_handler handler, void* arg);		
	int sequenceNumber;
	int packetCount;
	int dropCount;
};
