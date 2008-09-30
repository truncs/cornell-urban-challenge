#pragma once

#include "..\network\udp_connection.h"
#include "..\utility/fastdelegate.h"
#include <vector>
#define UDP_SICK_PORT 30016
#define UDP_SICK_ADDR "239.132.1.16"

#define LIDAR_STX							0x02
#define LIDAR_ADDR						0x80
#define LIDAR_MAXVAL_ERROR		8183
#define LIDAR_DAZZLING_ERROR	8190

#define PI 3.1415926535897932384626433832795
class SickInterfaceReceiver;


using namespace std;

struct RThetaCoordinate
{
	double R; double Theta;
	RThetaCoordinate() {}
	RThetaCoordinate (double R, double Theta)
	{
		this->R = R; this->Theta = Theta;
	}
};

struct SickPoint
{
	RThetaCoordinate location;
	SickPoint() {}
	SickPoint (RThetaCoordinate location)
	{this->location = location;}
};

struct SickScan
{
	int packetNum;
	double timestamp;
	int scannerID;
	vector<SickPoint> points;
};

typedef FastDelegate3<SickScan, SickInterfaceReceiver*, void*> SickScan_Msg_handler;

class SickInterfaceReceiver
{
private:
	udp_connection *conn;		
	void UDPCallback(udp_message& msg, udp_connection* conn, void* arg);	
	SickScan_Msg_handler sickScan_cbk;		
	void* sickScan_cbk_arg;		
	int packetNum;
	int id;
	bool is180;
	bool ismmMode;
	double angularSeperationDegrees;
public:
	SickInterfaceReceiver(int id);
	SickInterfaceReceiver(bool is180, bool ismmMode, double angularSeperationDegrees, int id);
	~SickInterfaceReceiver(void);	
	void SetSickScanCallback(SickScan_Msg_handler handler, void* arg);			
};

