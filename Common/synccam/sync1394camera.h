#ifndef _SYNCCAM1394_H
#define _SYNCCAM1394_H

#define WIN32_LEAN_AND_MEAN 
#include <windows.h>
#include <iostream>
#include <memory.h>
#include <queue>
#include <math.h>
#pragma comment(lib,"1394camera.lib")
#include <1394Camera.h>
#include <vector>
#include "..\network\udp_connection.h"


#define CAMERA_MSG_REGISTER 0x00 //Set register listener
#define CAMERA_MSG_SETFPS   0x01 //Set frame rate
#define CAMERA_MSG_SETDUTY  0x02 
#define CAMERA_START        0x04 //resets the counter, initializes stuff
#define CAMERA_STOP         0x05 //resets the counter, initializes stuff

#define UDP_CONTROL_PORT 20
#define UDP_CONTROL_IP "192.168.1.8"

#define UDP_BROADCAST_IP "232.132.1.8"
#define UDP_BROADCAST_PORT 30008

#define AUTOGAIN_USE_MEDIAN	1
#define AUTOGAIN_MEDIAN_IDEAL 100
#define AUTOGAIN_MAX_IDEAL		100
#define AUTOGAIN_KP						1.0
#define AUTOGAIN_MAX8BIT			250
#define AUTOGAIN_MAX16BIT			1000
#define MAX_FPS_LIMITED_SHUTTER 900
#define USESHUTTERGAIN
using namespace std;
//Encapsulates the UDP connection class for syncronized images delivered
//from a 1394 camera, using the CMU cam driver


struct SyncCamParams
{
	int		width; 
	int		height;
	bool	isColor;
	int		videoMode;		//3 for 640x480 YUV 422
	int		videoFormat; // 0 for 640x480 YUV 422  
	int  videoFrameRate; //4 for normal stuff ,3 for unibrain
	bool  isSlave;

  bool  usePartialScan;	  
	unsigned short bytesPerPacket; //needed on the a622f, set to < 4000, > 3500
	bool	BitDepth16;							
	bool  AGC;									 //enabled auto gain control (software)
	int	  AGCtop, AGCbot;				 //the top and bottom of the frame used for agc control
	bool	syncEnabled;  				 //set to -1 for no use!
	int		syncID;								 //this is the ID on the MCU that provides the sync signal, NOT the firewire ID
	int   syncFPS;							 //desired FPS from the sync mcu
  bool  syncKillOnClose;       //turns off timing when this app closes...
	bool  ghettoSync;							//dont ever use this please
	unsigned short syncCamInput; //the physical port on the camera used for sync
	int partialHeight;
	int partialWidth;
	int partialTop;
	int partialLeft;
	
  SyncCamParams()
  {
	  width=640; 
	  height=480;
	  isColor=true;
	  videoMode=3;		//3 for 640x480 YUV 422
	  videoFormat=0; // 0 for 640x480 YUV 422 
		videoFrameRate=4; //4 for baslers, 3 for unibrain
	  usePartialScan=false;					 
	  bytesPerPacket=0; //needed on the a622f, set to < 4000, > 3500
	  BitDepth16=false;							
	  AGC=true;									 //enabled auto gain control (software)
	  AGCtop=640;
    AGCbot=480;				 //the top and bottom of the frame used for agc control
	  syncEnabled=false;							 //set to -1 for no use!
	  syncID=0;								 //this is the ID on the MCU that provides the sync signal, NOT the firewire ID
	  syncFPS=30;							 //desired FPS from the sync mcu
    syncKillOnClose=false;       //turns off timing when this app closes...
		ghettoSync = false;
	  syncCamInput=0; //the physical port on the camera used for sync
		partialHeight = height;
		partialWidth = width;
		partialLeft = 0;
		partialTop = 0;
  }
};

#pragma pack (1)
struct SyncCamPacket
{
	unsigned short seconds;
	unsigned long	 ticks;
	unsigned long	 seqNum;
  unsigned char  id;
};
#pragma pack ()

DWORD WINAPI CamThreadWrap(LPVOID t);

class Sync1394Camera
{
public:
	Sync1394Camera();
	~Sync1394Camera();
	bool InitCamera (int cameraID, SyncCamParams config);
	int GetNumberOfCameras ();
	int SetGain(int val);
	int SetShutter (int val);
	unsigned short GetGain();
	unsigned short GetShutter ();
	void GetShutterMinMax(unsigned short*, unsigned short*);
	void GetGainMinMax(unsigned short*, unsigned short*);
	void DoAutoShutter(unsigned short targetGain);

	DWORD CamThread();
	HANDLE cameraEvent;
	unsigned char* buf;
	double curtimestamp;
	int lastMedian;
	int lastMaxAcc;
	float AGCerror;
	float AGCeffort;
	float kp;
	int idealMedian;
	CRITICAL_SECTION camgrab_cs;	
private:
	static int shortComp (const void* a, const void* b);
	static int charComp (const void* a, const void* b);
	static C1394Camera camera;
	bool isRunning;	
	HANDLE cameraHandle;
	SyncCamParams config;
	int GetNumMaxedPixelsInBuf (int top, int bottom);
	int GetMedianPixelValue(int top, int bottom);
	int size;
	udp_connection* udpRX;
	udp_connection* udpTX;
	void UDPCallback(udp_message& msg, udp_connection* conn, void* arg);
	unsigned short maxGain;
	unsigned short minGain;
	unsigned short maxShutter;
	unsigned short minShutter;
	int curSeqNumber;
	int expSeqNumber;

};

#endif
