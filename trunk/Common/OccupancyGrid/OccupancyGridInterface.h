#ifndef OCCUPANCY_GRID_INTERFACE_H_OCT_03_2007_SVL5
#define OCCUPANCY_GRID_INTERFACE_H_OCT_03_2007_SVL5

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

#include "../Network/UDP_Connection.h"
#include "../Utility/FastDelegate.h"
#include "../Utility/FixedQueue.h"
#include "../Coords/Transform3d.h"
#include "../Math/Angle.h"
#include <vector>
using namespace std;

#define OCCUPANCY_GRID_PORT 30061
#define OCCUPANCY_GRID_MC_IP "239.132.1.61"

const double OCC_GRID_MAX_ALLOWABLE_TIME_DIFF = .02;

namespace OCCUPANCY_STATUS{
  const unsigned char UNKNOWN = 0;
  const unsigned char FREE = 1;
  const unsigned char OCCUPIED = 2;
};

#pragma pack(1)
struct OGridHeader{
  double vts; // in seconds
  unsigned short seqNum;  // sequence number
  float angularResolution; //in rads
  float rightAngle; // in rads. the rightmost edge 
  float closestDist;  // in m
  float distResolution; // in m
  unsigned int dimRad, dimDist; // number of "columns" (angular buckets) and "rows" (dist buckets)
};
#pragma pack()

struct OGridCell{
  unsigned char status;
};

struct OGrid{
  OGridHeader info;
  vector<OGridCell> cells;
  
  inline OGridCell* GetCell(float x, float y){
    // Amanda @ parkers: so where are you from?
    // Me: ... (Iraq).. umm Russia!
    float angle = atan2f(y,x);
    float dist = sqrtf(x*x+y*y);
    if(dist < info.closestDist || Angle_A_Right_Of_B(angle,info.rightAngle)) return NULL;
    int dI = (int)((dist-info.closestDist)/info.distResolution);
    if(dI<0 || dI>=(int)info.dimDist) return NULL;
    if(angle<-10 || angle>10) return NULL;
    angle -= info.rightAngle;
    while(angle<0) angle += F_PI*2.f;
    while(angle>F_PI*2.f) angle -= F_PI*2.f;
    int rI = (int)(angle/info.angularResolution);
    if(rI<0||rI>=(int)info.dimRad) return NULL;
    int index = dI*info.dimRad + rI;
    if(index < 0 || index >= (int)cells.size()) return NULL;
    return &cells[index];
  }

  inline unsigned char GetCellStatus(float x, float y){
    OGridCell* c = GetCell(x,y);
    if(c==NULL) return OCCUPANCY_STATUS::UNKNOWN;
    return c->status; 
  }
  inline void SetCellStatus(float x, float y, unsigned char status){
    OGridCell* c = GetCell(x,y);
    if(c==NULL) return;
    c->status = status;
  }
};

class OccupancyGridInterface{

public:
  OccupancyGridInterface();
  virtual ~OccupancyGridInterface();

  // returns the vts of the grid loaded. if no grid found close to requested timestamp, returns a negative timestamp.
  double LoadGridAtVTS(double vts);

  // returns a vts of less than 0 if no grids are available. otherwise returns the loaded grids vts.
  double LoadNewestAvailableGrid();

  unsigned char GetOccupancy_VehCoords(float x, float y);
  unsigned char GetOccupancy_VeloCoords(float x, float y);

protected:
  udp_connection *conn;		
	void UDPCallback(udp_message& msg, udp_connection* conn, void* arg);
  CRITICAL_SECTION cs;
  FixedQueue<OGrid> gridQueue;
  OGrid curGrid;
  Transform3d veh2velo;

};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //OCCUPANCY_GRID_INTERFACE_H_OCT_03_2007_SVL5

