#ifdef __cplusplus_cli
#pragma managed(off)
#endif

#include "OccupancyGridInterface.h"
#include "../Velodyne/VelodyneTahoeCalibration.h"
#include "../Coords/Transform3dFusionUtils.h"
#include "../Coords/v3f.h"

OccupancyGridInterface::OccupancyGridInterface():gridQueue(20,OGrid()){
  InitializeCriticalSection(&cs);

	udp_params params = udp_params();
	params.remote_ip = inet_addr(OCCUPANCY_GRID_MC_IP);
	params.local_port = OCCUPANCY_GRID_PORT;
  params.reuse_addr = true;
	conn = new udp_connection (params);	
	conn->set_callback (MakeDelegate(this,&OccupancyGridInterface::UDPCallback),conn);
  
  curGrid.info.vts = -1;
  curGrid.info.dimRad = curGrid.info.dimDist = 0;

  veh2velo = veh2SensorXForm(VELODYNE_CALIBRATION::CURRENT::SensorCalibration());
}

OccupancyGridInterface::~OccupancyGridInterface(){
  delete conn;
  DeleteCriticalSection(&cs);
}

void OccupancyGridInterface::UDPCallback(udp_message &msg, udp_connection* conn, void* arg){
  if(msg.len < sizeof(OGridHeader)){
    printf("ERROR: OCC_GRID Size mismatch (1) (size=%d)\n",msg.len);
    return;
  }

  OGridHeader* hdr = (OGridHeader*)msg.data;
  if(msg.len != sizeof(OGridHeader)+sizeof(OGridCell)*hdr->dimRad*hdr->dimDist){
    printf("ERROR: OCC_GRID Size mismatch (2) (size=%d dimDist=%u dimRad=%u )\n",msg.len,hdr->dimDist,hdr->dimRad);
    return;
  }

  OGrid g;
  g.info = *hdr;
  g.cells.resize(g.info.dimDist*g.info.dimRad);
  for(unsigned int i=0;i<g.cells.size();i++){
    OGridCell* c = (OGridCell*)(msg.data + sizeof(OGridHeader) + sizeof(OGridCell)*i);
    g.cells[i] = *c;
  }

  EnterCriticalSection(&cs);
  if(gridQueue.n_meas()>0 && gridQueue.newest().info.vts > g.info.vts){
    printf("WARNING: OCC_GRID: newest grid is older than oldest known grid. Flushing queue. (got vts=%.4lfs already have:%.4lfs)\n",g.info.vts,gridQueue.oldest().info.vts);
    gridQueue.reset();
  }

  gridQueue.push(g);
  LeaveCriticalSection(&cs);
}

double OccupancyGridInterface::LoadGridAtVTS(double vts){
  int closest=-1;
  EnterCriticalSection(&cs);
  for(unsigned int i=0;i<gridQueue.n_meas();i++){
    if(closest<0 || fabs(gridQueue[i].info.vts-vts)<OCC_GRID_MAX_ALLOWABLE_TIME_DIFF)
      closest = i;
  }

  if(closest<0){
    curGrid.info.vts = -1;
    curGrid.info.dimRad = curGrid.info.dimDist = 0;
  }else{
    curGrid = gridQueue[closest];
  }

  LeaveCriticalSection(&cs);

  return curGrid.info.vts;
}

double OccupancyGridInterface::LoadNewestAvailableGrid(){
  EnterCriticalSection(&cs);
  if(gridQueue.n_meas()==0){
    curGrid.info.vts = -1;
    curGrid.info.dimRad = curGrid.info.dimDist = 0;
  }else{
    curGrid = gridQueue.newest();
  }

  LeaveCriticalSection(&cs);
  return curGrid.info.vts;
}

unsigned char OccupancyGridInterface::GetOccupancy_VehCoords(float x, float y){
  if(curGrid.info.vts<0) return OCCUPANCY_STATUS::UNKNOWN;
  v3f tmp(x,y,0);
  tmp.transform(veh2velo);
  return GetOccupancy_VeloCoords(tmp.x,tmp.y);
}

unsigned char OccupancyGridInterface::GetOccupancy_VeloCoords(float x, float y){
  if(curGrid.info.vts<0) return OCCUPANCY_STATUS::UNKNOWN;
  unsigned char ret = curGrid.GetCellStatus(x,y);
  return ret;  
}