#pragma once

#include "..\network\udp_connection.h"
#include "..\utility/fastdelegate.h"

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

#define UDP_MOBILEYE_MULTICAST_PORT 30036
#define UDP_MOBILEYE_MULTICAST_ADDR "239.132.1.36"

#define MOBILEYE_MAX_OBS 20

	enum MobilEyeID:int
	{
		MOBILEYE_FL=0,
		MOBILEYE_FR=1,
		MOBILEYE_CTR=2,
		MOBILEYE_REAR=3
	};

	enum MobilEyeMessageID:int
	{
		ME_Info = 0,
		ME_RoadEnv= 1,
		ME_Obs = 3,
		ME_BAD = 99
	};
	enum PoseAvailable:int
	{
		POSE_None = 0x00,
		POSE_Vel  = 0x04,
		POSE_Pitch= 0x02,
		POSE_Yaw  = 0x01
	};

	enum LaneMarkType:int
	{
		LM_None, LM_Solid, LM_Dashed, LM_Virtual, LM_BottsDots, LM_RoadEdge, LM_DoubleSolid, LM_Reserved
	};
	enum LatDistDir:int
	{
		LD_Left, LD_Right
	};

	enum LaneCrossingFlag:int
	{
		LC_Unknown, LC_NoCrossing, LC_CrossingLeft, LC_CrossingRight
	};

	enum StopLinePath:int
	{
		SL_Unknown, SL_InPath, SL_LeftPath, SL_RightPath
	};

	enum VehiclePath:int
	{
		VP_NoSide, VP_Center, VP_Left, VP_Right
	};
	
#pragma pack(1)
	struct MobilEyeEnvironment
	{		
		double carTime;
		int	meTimestamp;
		int	frameIndex;
		int	FOEY;
		int	FOEX;
		float	CamHeight; 
		float	leftDangerZone;
		float	rightDangerZone;
	};
#pragma pack()
	
#pragma pack(1)
	struct MobilEyeRoadInformation
	{
		double				carTime;
		LaneMarkType		rightMarkType;
		LaneMarkType		leftMarkType;
		float					distToLeftMark; 
		float					distToRightMark;
		float					roadModelSlope;
		float					roadModelCurvature;		
		float					roadModelConst;
		int							rightLaneConfidence;
		int							leftLaneConfidence;
		float						stopLineDistance;
		StopLinePath		stopLinePath;
		int							stopLineConf;
		float					roadModelValidRange;
		LaneCrossingFlag   laneCrossingFlag;
		float					leftTimeToLaneCrossing;
		float					rightTimeToLaneCrossing;
		bool						isRoadModelValid;

		//new----------
		//lanes---------------
		LaneMarkType rightNeighborMarkType;
		LaneMarkType leftNeighborMarkType;
		int rightNeighborConfidence;
		int leftNeighborConfidence;
		float distToRightNeighborMark;
		float distToLeftNeighborMark;
		//edges--------------
		LaneMarkType rightEdgeMarkType;
		LaneMarkType leftEdgeMarkType;
		int rightEdgeConfidence;
		int leftEdgeConfidence;
		float distToRightEdge;
		float distToLeftEdge;
	};
#pragma pack()
#pragma pack(1)	
	struct MobilEyeRoadEnv
	{
		MobilEyeID id;
		MobilEyeMessageID msgType;	
		int sequenceNumber;
		MobilEyeRoadInformation road;
		MobilEyeEnvironment env;
	};
#pragma pack()

#pragma pack(1)	
	struct MobilEyeWorldObstacle
	{				
		int	obstacleID;
		float	obstacleDistZ;
		int confidence;
		VehiclePath	path; //0 no side, 1 center, 2 left, 3 right
		bool currentInPathVehicle;
		bool obstacleDistXDirection; // 0 left, 1 right		
		float obstacleDistX; 
		float obstacleWidth;
		float	scaleChange; // (.5 to -.5)
		float velocity;
		int	bottomRect;	//pixels?
		int	leftRect;
		int	topRect;
		int	rightRect;
	};

#pragma pack()

#pragma pack(1)
	struct MobilEyeObstacles
	{
		MobilEyeID id;
		MobilEyeMessageID msgType;
		int sequenceNumber;
		double carTime;		
		int numObstacles;
		MobilEyeWorldObstacle obstacles[MOBILEYE_MAX_OBS];
	};
#pragma pack()


	class MobilEyeInterfaceSender;
	class MobilEyeInterfaceReceiver;

typedef FastDelegate4<MobilEyeRoadEnv, MobilEyeInterfaceReceiver*, MobilEyeID, void*> mobilEye_RoadEnv_Msg_handler;
typedef FastDelegate4<MobilEyeObstacles, MobilEyeInterfaceReceiver*, MobilEyeID, void*> mobilEye_Obstacle_Msg_handler;

using namespace std;
class MobilEyeInterfaceReceiver
{
private:
	udp_connection *conn;		
	void UDPCallback(udp_message& msg, udp_connection* conn, void* arg);
	mobilEye_RoadEnv_Msg_handler roadenv_cbk;	
	mobilEye_Obstacle_Msg_handler obs_cbk;
	void* roadenv_cbk_arg;	
	void* obs_cbk_arg;	

public:
	MobilEyeInterfaceReceiver();
	~MobilEyeInterfaceReceiver(void);
	void SetRoadEnvInfoCallback(mobilEye_RoadEnv_Msg_handler handler, void* arg);	
	void SetObstacleCallback(mobilEye_Obstacle_Msg_handler handler, void* arg);
	int sequenceNumber;
	int dropCount;
	int packetCount;
};

class MobilEyeInterfaceSender
{
private:
	udp_connection *conn;
	void UDPCallback(udp_message msg, udp_connection* conn, void* arg);
	int sequenceNumber;
public:
	MobilEyeInterfaceSender();
	~MobilEyeInterfaceSender(void);

	void SendRoadEnvInfo(MobilEyeRoadInformation* road, MobilEyeEnvironment* env, MobilEyeID id);	
	void SendObstacles(MobilEyeObstacles* obs, MobilEyeID id);
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif
