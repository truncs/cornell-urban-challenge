#pragma once

#include "..\..\network\udp_connection.h"
#include "..\..\utility/fastdelegate.h"
#include <vector>
#define _USE_MATH_DEFINES
#include <math.h>
#define UDP_DELPHI_PORT 30012
#define UDP_DELPHI_ADDR "239.132.1.12"

#define DELPHI_NUM_TRACKS 20
#define DELPHI_PACKET_LEN 507

//603,604,612
using namespace std;

#define FLS_STATUS  0x01
#define FLS_ECHO		0x02
#define FLS_TRACKA  0x03
#define FLS_TRACKB  0x04
#define FLS_TRACKC  0x12


enum DELPHIID:int
	{
		DELPHI_X_DRIV3=0, //used to be DELPHI_Z_DRIV3
		DELPHI_Y_PASS3=1,		
		DELPHI_F_DRIV1=2,
		DELPHI_B_PASS1=3,		
		DELPHI_E_DRIV0=4,
		DELPHI_C_PASS0=5,				
		DELPHI_D_FRONT0=6,		
		DELPHI_Z_REAR0=7,
		//these dont actually exist!
		DELPHI_G_DRIV2=99,
		DELPHI_A_PASS2=100	
	};

struct DelphiRadarEcho
{
	unsigned char scanIndexLSB;
	float vehicleSpeed;           //meters/sec
	float vehicleYawRate;         //radians/sec
	short vehicleRadiusCurvature; //meters
	unsigned char radarLevel;
	bool isInternalYawSensorMissing;
	bool isRadarMergingTargets;
};

struct DelphiRadarStatus
{
	 unsigned short scanIndex;
	 int softwareVersion;
	 bool scanOperational;
	 bool xvrOperational;
	 bool errorCommunication;
	 bool errorOverheat;
	 bool errorVoltage;
	 bool errorInternal;
	 bool errorRangePerformance;
	 bool isleftToRightScanMode;
	 bool isBlockageDetection;
	 bool isShortRangeMode;
	 float FOVAdjustment;
};

struct DelphiRadarTrack
{
	 unsigned char id;                            //1 to 20 indicates the ID of the current track
	 float range;                        //meters
	 float rangeUnfiltered;              //meters


	 float rangeRate;                    //meters/sec
	 float rangeRateUnfiltered;          //meters/sec

	 double trackAngle;                   //radians
	 double trackAngleUnfiltered;         //radians
	 double trackAngleUnfilteredNoOffset; //radians 

	 double edgeAngleLeftUnfiltered;      //radians
	 double edgeAngleRightUnfiltered;     //radians

	 float power;													//dbV
	 int counter;													//counts (scans)
	 unsigned char combinedObjectID;			//it is unclear what this means, possibly a "candidate" to merge tracks with.

	 bool isBridge;
	 bool isSidelobe;
	 bool isForwardTruckReflector;
	 bool isMatureObject;	 	 
	 bool isValid;
};


struct DelphiRadarScan
{
	DELPHIID scannerID;
	double timestamp;
	int sequence;
	DelphiRadarStatus status;
	DelphiRadarEcho echo;
	DelphiRadarTrack tracks[DELPHI_NUM_TRACKS];
};


//---------------------------------------------------------------------------------------
class DelphiInterfaceReceiver;

typedef FastDelegate4<DelphiRadarScan, DelphiInterfaceReceiver*, int , void*> Delphi_Msg_handler;

class DelphiInterfaceReceiver
{
private:
	udp_connection *conn;		
	void UDPCallback(udp_message& msg, udp_connection* conn, void* arg);	
	Delphi_Msg_handler delphi_cbk;		
	void* delphi_cbk_arg;		
	int packetNum;

	#pragma region memorymappedstructs

#pragma pack(1)
	struct rawTsPacket
	{
		unsigned short seconds;
		unsigned int	 ticks;
		unsigned int	 seq;
	};
#pragma pack()

#pragma pack(1)
struct rawStatusPacket
{
	unsigned short scanIndex;
	unsigned short swVer1;
	unsigned char  swVer2;
	unsigned char  flags1;
	unsigned char  flags2;
	signed char  fovAdjustment;
};
#pragma pack()

#pragma pack(1)
struct rawEchoPacket
{
	unsigned char scanIndex;
	unsigned short vehicleSpeed;
	signed short vehicleYawRate;
	signed short vehicleRadCurv;
	unsigned char flags;
};
#pragma pack()

#pragma pack(1)
struct rawTrackAB
{
	unsigned char idA;
	unsigned short range;
	short rangeRate;
	char angle;
	char angleUnfiltered;
	unsigned char unusedA;
	//---------------------
	unsigned char idB;
	unsigned short rangeUnfiltered;
	unsigned short power;
	unsigned char flags;
	unsigned char combinedObjectID;
	unsigned char unusedB;
};
#pragma pack()

#pragma pack(1)
struct rawTrackC
{
	unsigned char idC;
	short rangeRateUnfiltered;
	char edgeAngleLeftUnfiltered;
	char edgeAngleRightUnfiltered;
	char unused1;
	char trackAngleUnfilteredNoOffset;
	char unused2;
};
#pragma pack()

#pragma pack(1)
	struct rawRadarPacket
	{
		rawTsPacket timestamp;
		unsigned char id;
		rawStatusPacket status;
		rawEchoPacket	echo;
		rawTrackAB tracksAB[DELPHI_NUM_TRACKS];
		rawTrackC tracksC[DELPHI_NUM_TRACKS];
	};
#pragma pack()

#pragma endregion

public:
	DelphiInterfaceReceiver(void);
	~DelphiInterfaceReceiver(void);	
	void SetDelphiCallback(Delphi_Msg_handler handler, void* arg);			

};

