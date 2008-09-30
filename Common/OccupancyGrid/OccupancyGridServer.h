#ifndef OCCUPANCY_GRID_SERVER_H_OCT_03_2007_SVL5
#define OCCUPANCY_GRID_SERVER_H_OCT_03_2007_SVL5

#include "OccupancyGridInterface.h"
#include "../MESS/MessSender.h"

class OccupancyGridServer{
public:
  OccupancyGridServer(MessSender* mess){
    udp_params params = udp_params();
	  params.remote_ip = inet_addr(OCCUPANCY_GRID_MC_IP);	
	  params.local_port = 0;
	  params.multicast_loopback = true;
	  params.multicast_ttl=10;
	  conn = new udp_connection (params);		

    dest_ip = inet_addr(OCCUPANCY_GRID_MC_IP);

    sendBuf = new BYTE[65000];
    this->mess = mess;
  }

  ~OccupancyGridServer(){
    delete conn;
    delete [] sendBuf;
  }
  
  int Send(OGrid& grid){
    size_t len = sizeof(OGridHeader) + grid.cells.size()*sizeof(OGridCell);
    if(len>60000){
      char buf[100];
      sprintf(buf,"OccupancyGridServer: output packet len (%d)",len);
      printf(buf);
      printf("\n");
      if(mess!=NULL)  mess->SendError(buf);
      return 0;
    }

    *((OGridHeader*)sendBuf) = grid.info;
    for(unsigned int i=0;i<grid.cells.size();i++){
      OGridCell* c = (OGridCell*)(sendBuf+sizeof(OGridHeader)+sizeof(OGridCell)*i);
      *c = grid.cells[i];
    }
    
    conn->send_message(sendBuf,len,dest_ip,OCCUPANCY_GRID_PORT);

    printf("ogrid len = %d\n",len);
    
    return (int)len;
  }


protected:
  udp_connection *conn;
  unsigned short seq;
  BYTE* sendBuf;
  unsigned long dest_ip;
  MessSender* mess;
};

#endif //OCCUPANCY_GRID_SERVER_H_OCT_03_2007_SVL5
