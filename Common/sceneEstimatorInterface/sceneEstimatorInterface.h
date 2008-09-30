#pragma once

#include "..\network\udp_connection.h"
#include "..\utility/fastdelegate.h"
#include "..\localroadmodel\localroadmodel.h"
#include <vector>

#include <map>
#include <string>

#define UDP_SCENE_EST_PORT 30035
#define UDP_SCENE_EST_MULTICAST_ADDR "239.132.1.35"

#define MAX_PART_LENGTH 40

#define CHANNEL_VERSION 2
#define SERIALIZER_TYPE_SE 3

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

//***************************************************************************
//***************************************************************************
//***************************************************************************
//WARNING: YOU MUST UPDATE sceneEstimatorInterface.cs when editing this file!!!!
//***************************************************************************
//***************************************************************************
//***************************************************************************

//EXMAPLE-----------------------------------------
	/*SceneEstimatorUntrackedClusterMsg ucmsg;
		ucmsg.timestamp = 1000;
		for (int i=0; i<10; i++)
		{
			SceneEstimatorUntrackedCluster clust;
			for (int j=0; j<500; j++)			
				clust.points.push_back (SceneEstimatorClusterPoint(10.0 * ((double)rand()/(double)RAND_MAX),10.0 * ((double)rand()/(double)RAND_MAX)));
			ucmsg.untrackedClusters.push_back(clust);
		}
		SceneSender->SendSceneEstimatorUntrackedClusters (&ucmsg);
------------------------------------------------

*/
#define MAX_SE_CLUSTER_POINTS 1000
#define MAX_SE_CLOSEST_PARTITIONS 100
#define MAX_SE_RENDER_POINTS 2000

using namespace std;

typedef map<string, unsigned short> PartitionMap;
enum SceneEstimatorMessageID : int
{
	SCENE_EST_Info = 0,
	SCENE_EST_PositionEstimate= 1,
	SCENE_EST_Stopline = 2,
	SCENE_EST_ParticleRender=3,
	SCENE_EST_TrackedClusters=4,
	SCENE_EST_UntrackedClusters=5,
	SCENE_EST_Bad = 99
};

enum SceneEstimatorTargetStatusFlag : int
{
	TARGET_STATUS_ACTIVE = 1,
	TARGET_STATUS_DELETED = 2,
	TARGET_STATUS_OCCLUDED_FULL = 3,
	TARGET_STATUS_OCCLUDED_PARTIAL = 4
};

enum SceneEstimatorTargetClass : int
{
	TARGET_CLASS_UNKNOWN = -1,
	TARGET_CLASS_CARLIKE = 1,
	TARGET_CLASS_NOTCARLIKE = 2
};

enum SceneEstimatorClusterClass : int
{	
	SCENE_EST_LowObstacle= 0,				
	SCENE_EST_HighObstacle = 1
};


#pragma pack(1)
struct SceneEstimatorClusterPoint
{ 
	private:
		short x; 	//these are in CM!!!!!!!
		short y; 
	public:
		SceneEstimatorClusterPoint ()
		{x=0; y=0;}
		SceneEstimatorClusterPoint (double x, double y)
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

#pragma pack(1)
struct SceneEstimatorPartitionStringIntPair
{
	unsigned char* dotnetstring;
	unsigned short num;	
	unsigned int strLen;
	SceneEstimatorPartitionStringIntPair (string str,unsigned short num)
	{
		this->num = num; 
		dotnetstring = (unsigned char*)malloc(str.length() + 1);
		if (str.length() > 127) 		
			printf("SE WARNING: too long partition id! cannot be > 127 chars\n");
		int len = (int)str.length ();
		if (len > 127) len = 127;
		dotnetstring[0] = (unsigned char)len;
		memcpy(dotnetstring+1,str.c_str(),str.length());
		strLen = (int)str.length() + 1;
	}
	void CopySparse(void* memory, int& byteOffset)
	{
		memcpy (((unsigned char*)(memory))+byteOffset,(unsigned char*)dotnetstring,strLen);	
		byteOffset+=strLen;
		memcpy (((unsigned char*)(memory))+byteOffset,&num,sizeof(unsigned short));	
		byteOffset+=sizeof(unsigned short);
	}
	~SceneEstimatorPartitionStringIntPair()
	{
		free(dotnetstring);
	}
};
#pragma pack()

#pragma pack(1)
struct SceneEstimatorTrackedCluster 
{
	friend struct SceneEstimatorClusterPartition; 
private:
	int numPoints;
	int numClosestPartitions;  
public:
	float xClosestPoint;
	float yClosestPoint;
	float speed; 
	bool speedValid;
	float relHeading;
	float absHeading;
	bool  headingValid;
	SceneEstimatorTargetClass targetClass;
	int id;	
	SceneEstimatorTargetStatusFlag statusFlag;	
	bool isStopped;
	//--------------------------------
	//stuff below here is serialized manually
	vector <SceneEstimatorClusterPartition> closestPartitions;
	vector <SceneEstimatorClusterPoint> points;
	float range; //THIS IS NOT SENT< it is for internal prioritization only
	int GetFixedSize();
	int GetTotalSize();
	void CopySparse(void* memory, int& byteOffset);	
};
#pragma pack()

#pragma pack(1)
struct SceneEstimatorTrackedClusterMsgHeader
{
	unsigned char channelVersion;
	unsigned char serializerType;
	int sequenceNumber;
	SceneEstimatorMessageID id;
	unsigned char chunkNum;
	unsigned char numTotChunks;
	SceneEstimatorTrackedClusterMsgHeader ()
	{
			this->id = SCENE_EST_TrackedClusters;
			this->channelVersion = CHANNEL_VERSION;
			this->serializerType = SERIALIZER_TYPE_SE;		
			this->sequenceNumber = 0;
	}

};
#pragma pack()

#pragma pack(1)
struct SceneEstimatorTrackedClusterMsg
{
	friend class SceneEstimatorInterfaceSender;
	friend struct SceneEstimatorClusterPartition;
		int numTrackedClusters;
public:
		double timestamp;
		vector<SceneEstimatorTrackedCluster> trackedClusters;
private:
		int mapsize;
		PartitionMap partitionMap;
public:
		SceneEstimatorTrackedClusterMsg()
		{		
		}
		
		int GetFixedSize()
		{
			return 12; //this doesnt include mapsize on purpose!
		}
		bool CopySparse(void* memory, int& byteOffset, int maxSize)
		{
			numTrackedClusters = (int)trackedClusters.size ();		
			mapsize=(int)partitionMap.size(); 
			//copy the beginning....
			memcpy (((unsigned char*)(memory))+byteOffset,this,GetFixedSize());	
			byteOffset+=GetFixedSize();
			//and the inner  messages
			for (int i=0; i<numTrackedClusters; i++)		
			{								
					trackedClusters[i].CopySparse(memory,byteOffset);	
					if (byteOffset > maxSize) 
					{
						printf("WARNING: ARTIFICIAL TRUNCATION OF PACKET IN TRACKED CLUSTERS!!! (TRACKS) num =  %d\n",numTrackedClusters);			
						return false;
					}				
			}
			//and now the partition table
			memcpy (((unsigned char*)(memory))+byteOffset,&mapsize,sizeof(int));	
			byteOffset+=sizeof(int);			
			//map
			for (PartitionMap::iterator i = partitionMap.begin(); i!=partitionMap.end(); ++i) 
			{
				SceneEstimatorPartitionStringIntPair pair(i->first, i->second);
				pair.CopySparse (memory,byteOffset);
				if (byteOffset > maxSize) 
				{
					printf("WARNING: ARTIFICIAL TRUNCATION OF PACKET IN TRACKED CLUSTERS!!! (MAP) size = %d\n",mapsize);			
					return false;
				}
			}	
			return true;
		}
};

#pragma pack()
#pragma pack(1)
struct SceneEstimatorClusterPartition
{	
private:	
	unsigned short partitionHashID;
	unsigned short probabilityFP; //probability encoded in fixed point
public:
	SceneEstimatorClusterPartition() {}
	//for isaac
	float Probability() {return (float) (probabilityFP/65535.0f);}
	SceneEstimatorClusterPartition(char* name, float probability, SceneEstimatorTrackedClusterMsg* parentClusterMsg);
};
#pragma pack()

#pragma pack(1)
struct SceneEstimatorUntrackedCluster
{
	int numPoints;
	SceneEstimatorClusterClass clusterClass;
	vector<SceneEstimatorClusterPoint> points;
	int GetFixedSize()
	{
		return 8;
	}
	void CopySparse(void* memory, int& byteOffset)
	{
		numPoints = (int)points.size ();		
		//copy the beginning....
		memcpy (((unsigned char*)(memory))+byteOffset,this,GetFixedSize());	
		byteOffset+=GetFixedSize();
		//and the vectors....
		if (points.size() > 0)
		{
			memcpy (((unsigned char*)(memory))+byteOffset,&points.front(),sizeof(SceneEstimatorClusterPoint)*points.size());	
			byteOffset+=sizeof(SceneEstimatorClusterPoint)*((int)points.size());
		}
	}
};
#pragma pack()


#pragma pack(1)
struct SceneEstimatorUntrackedClusterMsg
{
		friend class SceneEstimatorInterfaceSender;
private:
		unsigned char channelVersion;
		unsigned char serializerType;
		int sequenceNumber;
		SceneEstimatorMessageID id;
		int numUntrackedClusters;
public:
		double timestamp;
		vector<SceneEstimatorUntrackedCluster> untrackedClusters;

		SceneEstimatorUntrackedClusterMsg()
		{
			this->id = SCENE_EST_UntrackedClusters;
			this->channelVersion = CHANNEL_VERSION;
			this->serializerType = SERIALIZER_TYPE_SE;		
			this->sequenceNumber = 0;
		}

		int GetFixedSize()
		{
			return 22;
		}
		void CopySparse(void* memory, int& byteOffset)
		{
			numUntrackedClusters = (int)untrackedClusters.size ();		
			//copy the beginning....
			memcpy (((unsigned char*)(memory))+byteOffset,this,GetFixedSize());	
			byteOffset+=GetFixedSize();
			//and the inner messages
			for (int i=0; i<numUntrackedClusters; i++)		
				untrackedClusters[i].CopySparse(memory,byteOffset);	
		}
};
#pragma pack()

#pragma region ParticlePoints
#pragma pack(1)
struct SceneEstimatorParticlePoints
{
	double x; double y; double weight;
	SceneEstimatorParticlePoints(){}
	SceneEstimatorParticlePoints(double x, double y, double weight) {this->x = x; this->y = y; this->weight = weight;}
};
#pragma pack()

#pragma pack(1)
struct SceneEstimatorParticlePointsMsg
{
	SceneEstimatorMessageID msgType;
	double time;
	int		 numPoints;
	SceneEstimatorParticlePoints points[MAX_SE_RENDER_POINTS];
};
#pragma pack()
#pragma endregion



class SceneEstimatorInterfaceSender;
class SceneEstimatorInterfaceReceiver;

typedef FastDelegate3<SceneEstimatorParticlePointsMsg, SceneEstimatorInterfaceReceiver*, void*> sceneEstimatorPoints_Msg_handler;
typedef FastDelegate3<LocalRoadModelEstimateMsg, SceneEstimatorInterfaceReceiver*, void*> sceneEstimatorLocalRoadEstimate_Msg_handler;

/*
class SceneEstimatorInterfaceReceiver
{
private:
	udp_connection *conn;		
	void UDPCallback(udp_message& msg, udp_connection* conn, void* arg);
	sceneEstimatorPoints_Msg_handler sceneEstimatorParticlePoints_cbk;	
	void* sceneEstimatorParticlePoints_cbk_arg;	

	sceneEstimatorLocalRoadEstimate_Msg_handler sceneEstimatorLocalRoadEstimates_cbk;	
	void* sceneEstimatorLocalRoadEstimates_cbk_arg;	


public:
	SceneEstimatorInterfaceReceiver(void);
	~SceneEstimatorInterfaceReceiver(void);
	void SetSceneEstimatorParticlePointsCallback(sceneEstimatorPoints_Msg_handler handler, void* arg);			
};
*/

class SceneEstimatorInterfaceSender
{
private:
	udp_connection *conn;
	void UDPCallback(udp_message msg, udp_connection* conn, void* arg);
	void* sparseMsgTracked;
public:
	int sequenceNumber;
	SceneEstimatorInterfaceSender(void);
	~SceneEstimatorInterfaceSender(void);
	//void SendSceneEstimatorParticlePoints(SceneEstimatorParticlePointsMsg* output);
	void SendSceneEstimatorUntrackedClusters(SceneEstimatorUntrackedClusterMsg* output);
	void SendSceneEstimatorTrackedClusters(SceneEstimatorTrackedClusterMsg* output);
	void SendSceneEstimatorLocalRoadModel(LocalRoadModelEstimateMsg* output);
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

