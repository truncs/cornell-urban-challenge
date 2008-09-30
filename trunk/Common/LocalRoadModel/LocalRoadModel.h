#pragma once

#include <math.h>
#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

#define MAX_LANE_POINTS 100

//----------------------------------------
enum LocalRoadModelMessageID : int
{
	LRM_LocalRoadModel=10
};

#pragma pack(1)
struct LocalRoadModelLanePoint
{ 
	private:
		short x; 	//these are in CM!!!!!!!
		short y; 
		unsigned short variance;
	public:
		LocalRoadModelLanePoint ()
		{x=0; y=0;}
		LocalRoadModelLanePoint (double x, double y, double variance)
		{
			this->x = (short)(x*100.0);
			this->y = (short)(y*100.0);
			//cap the variance and sqrt the variance
			double val = 0;
			if (variance>=0)
				val=sqrt(variance);
			if (val > 655)
				val = 655;
			this->variance = (unsigned short)(val*100.0);
		}
		double GetX()
		{
			return ((double)x /100.0);
		}
		double GetY()
		{
			return ((double)y /100.0);
		}
		double GetVariance()
		{
			return (((double)variance /100.0) * ((double)variance /100.0));
		}
};
#pragma pack()

#pragma pack(1)
struct LocalRoadModelEstimateMsg
{
	friend class SceneEstimatorInterfaceSender;
	friend class LocalMapInterfaceSender;
private:
	unsigned char channelVersion;
	unsigned char serializerType;
	int sequenceNumber;
	LocalRoadModelMessageID id;
public:
	double timestamp;
	float probabilityRoadModelValid;
	float probabilityCenterLaneExists;
	float probabilityLeftLaneExists;
	float probabilityRightLaneExists;
	
	float laneWidthCenter;
	float laneWidthCenterVariance;
	float laneWidthLeft;
	float laneWidthLeftVariance;
	float laneWidthRight;
	float laneWidthRightVariance;

	int numPointsCenter;	
	int numPointsLeft;
	int numPointsRight;

	LocalRoadModelLanePoint LanePointsCenter[MAX_LANE_POINTS];	
	LocalRoadModelLanePoint LanePointsLeft[MAX_LANE_POINTS];	
	LocalRoadModelLanePoint LanePointsRight[MAX_LANE_POINTS];
};
#pragma pack()

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif