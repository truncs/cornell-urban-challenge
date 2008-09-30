#ifndef _CRT_SECURE_NO_DEPRECATE
#define _CRT_SECURE_NO_DEPRECATE
#endif
#pragma once

#include "..\network\udp_connection.h"
#include "..\utility/fastdelegate.h"
#include "..\localroadmodel\localroadmodel.h"
#include <vector>
#include <string>

#define UDP_LOCALMAP_PORT 30034
#define UDP_LOCALMAP_MULTICAST_ADDR "239.132.1.34"

#define CHANNEL_VERSION 2
#define SERIALIZER_TYPE 3

#define SERIALIZER_IGNORE_TYPE 100
//***************************************************************************
//***************************************************************************
//***************************************************************************
//WARNING: YOU MUST UPDATE localMapInterface.cs when editing this file!!!!
//***************************************************************************
//***************************************************************************
//***************************************************************************

#define MAX_LMTX_TARGETS 200
#define MAX_LMTX_POINTS 5000
#define MAX_LMTX_CLUSTERS 1000

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

enum LocalMapClusterClass : int
{	
	LOCALMAP_LowObstacle= 0,				
	LOCALMAP_HighObstacle = 1
};


enum LocalMapMessageID : int
{
	LOCALMAP_Info = 0,
	LOCALMAP_Targets= 1,				
	LOCALMAP_LooseClusters= 2,				
	LOCALMAP_Bad = 99
};

enum LocalMapTargetType : int
{
	LM_TT_INVALID=-1,
	LM_TT_IBEO= 101,
	LM_TT_IBEOMATURE = 102,
	LM_TT_MOBILEYE = 103,
	LM_TT_RADAR= 104,
	LM_TT_QUASIMATURE = 105,
	LM_TT_MATURE = 106
};

#pragma pack(1)
struct LocalMapMsgHdr
{
	unsigned char	serializerVersion;
	unsigned char	serializerType;
	int sequenceNumber;
	LocalMapMessageID msgType;
};
#pragma pack()

#pragma pack(1)
struct LocalMapPoint
{ 
private:
	short x; 	//these are in CM!!!!!!!
	short y; 
public:
	LocalMapPoint ()
	{x=0; y=0;}
	LocalMapPoint (double x, double y)
	{
		this->x = (short)(x*100.0);
		this->y = (short)(y*100.0);
	}
	double GetX()
	{
		return ((double)x /100.0);
	}
	double GetY()
	{
		return ((double)y /100.0);
	}
};
#pragma pack()

#pragma region Targets
#pragma pack(1)
struct LocalMapTarget
{ 		
private: 
	int numPoints;
public:
	LocalMapTargetType type;
	float x;
	float y;
	float speed;
	float heading;
	float width;
	float orientation;

	float covariance[36];
	vector <LocalMapPoint> points;

	int getFixedSize()
	{
		return 176;
	}

	int getTotalSize()
	{
		return getFixedSize() + sizeof(LocalMapPoint)*numPoints;
	}

	void copySparse(void* memory, int& byteOffset)
	{
    numPoints = (int)points.size();			
		memcpy (((unsigned char*)(memory))+byteOffset,this,getFixedSize()); //get the header
		byteOffset += getFixedSize();				  
    //and the vector....
		if (numPoints > 0)
		{
			memcpy (((unsigned char*)(memory))+byteOffset,&points.front(),sizeof(LocalMapPoint)*numPoints);	
			byteOffset+=sizeof(LocalMapPoint)*(numPoints);  
		}		 
	} 
};
#pragma pack()

#pragma pack(1)
struct LocalMapTargetsMsg
{ 			
	friend class LocalMapInterfaceSender;
private:
	LocalMapMsgHdr header;	
	int numTargets;
public:
	double timestamp;
	vector <LocalMapTarget> targets;
	int getFixedSize()
	{		
		return 12 + sizeof(LocalMapMsgHdr);
	}
	
	void copySparse(void* memory, int& byteOffset)
	{
    numTargets = (int)targets.size ();
		memcpy (((unsigned char*)(memory))+byteOffset,this,getFixedSize()); //get the header
		byteOffset += getFixedSize();		
		for (int i=0; i<numTargets; i++)		
			targets[i].copySparse(memory,byteOffset);			
	}
};
#pragma pack()
#pragma endregion

#pragma region LooseClusters
#pragma pack(1)
struct LocalMapLooseCluster
{ 		
private:
	int	numPoints;	
public:
	LocalMapClusterClass clusterClass;
	vector<LocalMapPoint> points;
	LocalMapLooseCluster()
	{}	
	int getFixedSize()
	{
		return 8;
	}
	int getTotalSize()
	{
		return getFixedSize() + sizeof(LocalMapPoint)*numPoints;
	}

	void copySparse(void* memory, int& byteOffset)
	{
		numPoints = (int)points.size ();
		//copy the beginning....
		memcpy (((unsigned char*)(memory))+byteOffset,this,getFixedSize());	
		byteOffset+=getFixedSize();
		//and the vector....
		if (numPoints > 0)
		{
			memcpy (((unsigned char*)(memory))+byteOffset,&points.front(),sizeof(LocalMapPoint)*numPoints);	
			byteOffset+=sizeof(LocalMapPoint)*(numPoints);
		}
		
	}
};
#pragma pack()

#pragma pack(1)
struct LocalMapLooseClustersMsg
{ 
	friend class LocalMapInterfaceSender;
private:
	LocalMapMsgHdr header;
	int numClusters;
public:
	double timestamp;
	vector<LocalMapLooseCluster> clusters;
		
	int getFixedSize()
	{
		return 12 + sizeof(LocalMapMsgHdr);		
	}
	void copySparse(void* memory, int& byteOffset)
	{
		numClusters = (int)clusters.size ();
		memcpy (((unsigned char*)(memory))+byteOffset,this,getFixedSize()); //get the header
		byteOffset += getFixedSize();		
		for (int i=0; i<numClusters; i++)		
			clusters[i].copySparse(memory,byteOffset);			
	}
};
#pragma pack()
#pragma endregion

class LocalMapInterfaceSender;
class LocalMapInterfaceReceiver;

using namespace std;

typedef FastDelegate3<LocalMapTargetsMsg, LocalMapInterfaceReceiver*, void*> LocalMapTargets_Msg_handler;
typedef FastDelegate3<LocalMapLooseClustersMsg, LocalMapInterfaceReceiver*, void*> LocalMapLooseClusters_Msg_handler;
typedef FastDelegate3<LocalRoadModelEstimateMsg, LocalMapInterfaceReceiver*, void*> LocalMapLocalRoadEstimate_Msg_handler;
class LocalMapInterfaceReceiver
{
private:
	udp_connection *conn;		
	void UDPCallback(udp_message& msg, udp_connection* conn, void* arg);		
	LocalMapTargets_Msg_handler localMapTargets_cbk;		
	void* localMapTargets_cbk_arg;		
	LocalMapLooseClusters_Msg_handler localMapLooseClusters_cbk;		
	void* localMapLooseClusters_cbk_arg;		
	LocalMapLocalRoadEstimate_Msg_handler localMapLocalRoadEstimates_cbk;		
	void* localMapLocalRoadEstimates_cbk_arg;		

public:
	LocalMapInterfaceReceiver(void);
	~LocalMapInterfaceReceiver(void);		
	int sequenceNumber;
	int dropCount;
	int packetCount;
	void SetLocalMapTargetsCallback(LocalMapTargets_Msg_handler handler, void* arg);
	void SetLocalMapLooseClustersCallback(LocalMapLooseClusters_Msg_handler handler, void* arg);
	void SetLocalMapLocalRoadModelCallback(LocalMapLocalRoadEstimate_Msg_handler handler, void* arg);		
};


class LocalMapInterfaceSender
{
private:
	udp_connection *conn;
	void UDPCallback(udp_message msg, udp_connection* conn, void* arg);
	int sequenceNumber;
public:
	LocalMapInterfaceSender(void);
	~LocalMapInterfaceSender(void);

	void SendLocalMapTargets (LocalMapTargetsMsg* msg);		
	void SendLocalMapLooseClusters (LocalMapLooseClustersMsg* msg);
	void SendLocalRoadModel (LocalRoadModelEstimateMsg* msg);
};


#ifdef __cplusplus_cli
#pragma managed(pop)
#endif
