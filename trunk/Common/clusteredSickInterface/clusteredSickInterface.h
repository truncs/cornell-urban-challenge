#pragma once

#include "..\network\udp_connection.h"
#include "..\utility\fastdelegate.h"
#include "..\LidarClusterInterface\LidarClusterCommon.h"
#include "..\sickinterface\sickcommon.h"

#define UDP_REARSICK_PORT 30039
#define UDP_REARSICK_ADDR "239.132.1.39"

#define TX_MAX_REAR_CLUSTERS 100

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif


	enum ClusteredSickMessageID : int
	{
		RS_Info = 0,
		RS_ClusterMsg= 1,
		RS_BAD = 99
	};
	
	#pragma pack(1)
	struct SickCluster
	{		  
		bool stable;
		bool leftOccluded;
		bool rightOccluded;
	private:
		int numpts;
	public:
		vector<SickXYPoint> pts;
		int getFixedSize()
		{		
			return 7;
		}
		void copySparse(void* memory, int& byteOffset)
		{
			numpts = (int)pts.size ();
			memcpy (((unsigned char*)(memory))+byteOffset,this,getFixedSize()); //get the header
			byteOffset += getFixedSize();		
			if (numpts > 0)
			{
				memcpy (((unsigned char*)(memory))+byteOffset,&pts.front(),sizeof(SickXYPoint)*numpts);	
				byteOffset+=sizeof(SickXYPoint)*(numpts);  
			}		
		}
	};
	#pragma pack()

	#pragma pack(1)
	struct ClusteredSickMsgHdr
	{
		ClusteredSickMessageID msgType;				
		int sequenceNumber;
		double carTime;
	};
	#pragma pack()
	#pragma pack(1)
	struct ClusteredSickMsg
	{		
		ClusteredSickMsgHdr hdr;
	private:
		int numClusters;
	public:
		vector<SickCluster> clusters;
		int getFixedSize()
		{		
			return sizeof(ClusteredSickMsgHdr) + 4;
		}
		void copySparse(void* memory, int& byteOffset)
		{
			numClusters = (int)clusters.size ();
			memcpy (((unsigned char*)(memory))+byteOffset,this,getFixedSize()); //get the header
			byteOffset += getFixedSize();		
			for (int i=0; i<numClusters; i++)		
			{
				clusters[i].copySparse (memory,byteOffset);
			}
		}
	};
	#pragma pack()
	class ClusteredSickSender;
	class ClusteredSickReceiver;

typedef FastDelegate3<const vector<LidarCluster>&, double, void*> LidarClusterCallbackType;

class ClusteredSickReceiver
{
private:
	udp_connection *conn;		
	void UDPCallback(udp_message& msg, udp_connection* conn, void* arg);
	LidarClusterCallbackType cbk;
	void* cbk_arg;
	
public:
	ClusteredSickReceiver(void);
	~ClusteredSickReceiver(void);	
	void SetCallback(LidarClusterCallbackType handler, void* arg);	
	int packetCount; int dropCount; int sequenceNumber;
};

class ClusteredSickSender
{
private:
	udp_connection *conn;
	void UDPCallback(udp_message msg, udp_connection* conn, void* arg);
	int sequenceNumber;
public:
	ClusteredSickSender(void);
	~ClusteredSickSender(void);

	void Send(ClusteredSickMsg* msg);	
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif