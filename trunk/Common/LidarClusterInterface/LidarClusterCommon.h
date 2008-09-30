#pragma once

#define F_PI 3.14159265358979323846f

#define CLUSTER_DEFAULT_PORT 30041
#define CLUSTER_DEFAULT_MC_IP "239.132.1.41"

#define CLUSTER_PACKET_TYPE 0x75

#include "../coords/v3f.h"

#pragma pack(push,1)
struct ClusterPacketHdr{
  unsigned char type;         // for now should always be CLUSTER_PACKET_TYPE
  unsigned short packetNum;   // packet counter

  unsigned short tsSeconds;   // vehicle timestamp
  unsigned long tsTicks;      
  
  unsigned short numPts;      // number of points in this scan
  unsigned short numClusters; // number of clusters in this scan
};

struct ClusterPacketPoint{
  short X,Y,Z;      // in cm
};

struct ClusterPacketCluster{
  unsigned short firstPtIndex, lastPtIndex; // indexes of the first and last points belonging to this cluster
  unsigned char flags;    // bit  0: unstable; 
                          //      1: left-occluded; 
                          //      2: right-occluded; 
                          //      3: if set, this cluster is actually a set of unclustered points (should always be 0)
};

#include <vector>

struct LidarCluster{
  std::vector<v3f> pts;
  bool stable;
  bool leftOccluded, rightOccluded;
  bool highObstacle;
};

#pragma pack(pop)
