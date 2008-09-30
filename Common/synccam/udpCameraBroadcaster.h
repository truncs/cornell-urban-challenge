#pragma once

#include "..\network\udp_connection.h"
#include "..\utility/fastdelegate.h"
#include "..\jpeg\simplejpeg.h"
#include <fstream>

//gui bullstuff
//#pragma	comment(lib,"cxcore.lib")
//#pragma	comment(lib,"cv.lib")
//#pragma	comment(lib,"highgui.lib")
//#pragma	comment(lib,"cvaux.lib")

#ifdef WIDE
	#define UDP_CAMERA_PORT 30098
	#define UDP_CAMERA_ADDR "239.132.1.98"
#else
	#define UDP_CAMERA_PORT 30099
	#define UDP_CAMERA_ADDR "239.132.1.99"
#endif

#pragma pack(1)
struct UDPCameraMsg
{
	int index;
	double timestamp;
	int width;
	int height;
	int size;
};

#pragma pack()

class UDPCameraBroadcaster
{
public:

	UDPCameraBroadcaster();
	~UDPCameraBroadcaster();
	void SendImage (const uchar* img, int height, int width, double ts, int index);			

private:
	udp_connection *conn;			
};

class UDPCameraReceiver;

typedef FastDelegate3<UDPCameraMsg, UDPCameraReceiver*, void*> UDPCamera_Msg_handler;

using namespace std;
class UDPCameraReceiver
{
private:
	udp_connection *conn;		
	void UDPCallback(udp_message& msg, udp_connection* conn, void* arg);
	UDPCamera_Msg_handler cbk;		
	void* cbk_arg;		

public:
	UDPCameraReceiver(void);
	~UDPCameraReceiver(void);
	void SetCallback(UDPCamera_Msg_handler handler, void* arg);		
	int sequenceNumber;
	int packetCount;
	int dropCount;
};
