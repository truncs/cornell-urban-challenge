#include "VelodyneReceiver.h"
#define USE_MATH_DEFINES
#include <math.h>
#include <utility>
#include "../Math/Angle.h"
#include "../Time/Timing.h"





extern HANDLE hStdout;


VelodyneReceiver::VelodyneReceiver(car_timestamp_sync* vtsProvider, bool filter){
  udp_params params = udp_params();
	params.remote_ip = inet_addr(VELODYNE_REMOTE_IP);
	params.local_port = VELODYNE_PORT;
  params.reuse_addr = true;
	conn = new udp_connection (params);	
	conn->set_callback (MakeDelegate(this,&VelodyneReceiver::UDPCallback),conn);
  scanSegmentCallback = NULL;
  this->vtsProvider = vtsProvider;
	filterPoints = filter;
  for(int i=0;i<64;i++){
    VelodyneFactors f;
    const T_corr_factors* table = (i<32)?velodyne_bottom_block_laser_correction_factors:velodyne_top_block_laser_correction_factors;
    f.RCF = (float)asin(table[i%32][0]);
    f.VCF = (float)asin(table[i%32][1]);
    f.RCF_SIN = (float)table[i%32][0];
    f.VCF_SIN = (float)table[i%32][1];
    f.RCF_COS = (float)table[i%32][2];
    f.VCF_COS = (float)table[i%32][3];
    f.HCF = (float)table[i%32][4]/100.f;
    factors[i] = f;
  }

  timeSpentInUDPCallback=0;
  numUDPCallbacks=0;
  timeSpentInUserCallback=0;
}

VelodyneReceiver::~VelodyneReceiver(){
  delete conn;
}

void VelodyneReceiver::UDPCallback(udp_message& msg, udp_connection* conn, void* arg){
  if(msg.len != 1206){
    printf("VELODYNE: incorrect packet size (%d)\n",msg.len);
    return; // incorrect packet size!
  }
  TIMER tmr=GetTime();

  unsigned int nRead=0;
  unsigned short tmpUShort;
  unsigned char tmpUChar;

  VelodyneScan scan;
  scan.pts.reserve(12*32);

  if(vtsProvider!=NULL){
    car_timestamp ct = vtsProvider->current_time();
    if(ct.is_valid())
      scan.vts = ct.total_secs();
    else
      scan.vts = -1;
  }

  float maxRotation=-1000;

#define RD(X) memcpy(&X,msg.data+nRead,sizeof(X)); nRead+=sizeof(X);

  for(unsigned int firing=0;firing<12;firing++){
    RD(tmpUShort);
    unsigned short blockHeaderID = tmpUShort;
    RD(tmpUShort);
    float rotation = (float)tmpUShort / 100.f;
    rotation *= -(F_PI / 180.f);

    maxRotation = MAX(maxRotation,rotation);

    const T_corr_factors * corr_factors;
    unsigned char lno=0;
    if(blockHeaderID==0xEEFF){ // this is from 32 "upper" lasers
      corr_factors = velodyne_top_block_laser_correction_factors;
      lno=32;
    }
    else{                      // should be 0xDDFF - lower 32 lasers.
      corr_factors = velodyne_bottom_block_laser_correction_factors;
      lno=0;
    }

    float rot_cos = cosf(rotation);
    float rot_sin = sinf(rotation);

    for(unsigned char laser=0;laser<32;laser++){
      RD(tmpUShort);
      RD(tmpUChar);

      if(lno==0){
        if(laser==17) continue; // for now, ignore laser 17... (#18, low block)

        if(laser==1 || 
           laser==4 || 
           laser==22||
           laser==15||
           laser==8) continue; // for now, ignore laser 17... (#18, low block) //DEBUG
      }

      VelodynePt vpt;
      vpt.range = (float)tmpUShort*.002f;

	  // aho - I don't know why this is here
      if(vpt.range < 1.0) continue;
      vpt.range += .15f;
      vpt.intensity = (float)tmpUChar;

/*      float& cosVertAngle = factors[lno+laser].VCF_COS;
      float& sinVertAngle = factors[lno+laser].VCF_SIN;
      float& cosRotCorrection = factors[lno+laser].RCF_COS;
      float& sinRotCorrection = factors[lno+laser].RCF_SIN;

      float cosRotAngle = rot_cos*cosRotCorrection + rot_sin*sinRotCorrection;
      float sinRotAngle = rot_sin*cosRotCorrection - rot_cos*sinRotCorrection;

      float& distance = vpt.range;
      
      float hOffsetCorr = factors[lno+laser].HCF;
      float vOffsetCorr = 0;  // top vs bottom block (everything is centered center of unit at bottom block height)

      float xyDistance = distance*cosVertAngle - vOffsetCorr*sinVertAngle;
      
      vpt.x = xyDistance * sinRotAngle - hOffsetCorr * cosRotAngle;
      vpt.y = xyDistance * cosRotAngle + hOffsetCorr * sinRotAngle;
      vpt.z = distance * sinVertAngle + vOffsetCorr * cosVertAngle;
*/
      float x = vpt.range * factors[lno+laser].RCF_COS * factors[lno+laser].VCF_COS;
      float y = vpt.range * factors[lno+laser].RCF_SIN * factors[lno+laser].VCF_COS;
      float z = vpt.range * factors[lno+laser].VCF_SIN;
      
      vpt.x = (x * rot_cos) - (y * rot_sin);
      vpt.y = (x * rot_sin) + (y * rot_cos);
      vpt.z = z;

      if(lno!=0) vpt.z += VELODYNE_BOTTOM_TO_TOP_BLOCK_DIST;

      vpt.sensorTheta = factors[lno+laser].RCF + rotation;
      if(vpt.sensorTheta<0.f) vpt.sensorTheta += F_PI*2.f;
      if(vpt.sensorTheta>2.f*F_PI) vpt.sensorTheta -= F_PI*2.f;
		if (filterPoints)
		{
			if(vpt.sensorTheta > 5.f*F_PI/8.f && vpt.sensorTheta < 11.f*F_PI/8.f) continue;
			if(fabsf(vpt.y)>30.f) continue;
			if(vpt.x > 60) continue;
			if(vpt.x < -10) continue;
		}
      vpt.laserNum = lno+laser;      
      vpt.blockTheta = rotation;
      
      scan.pts.push_back(vpt);
    }    
  }

  char buf[5] = {0,0,0,0,0};
  memcpy(buf,msg.data+nRead+2,4);
  //printf("%s\n",buf);

  // figure out the timestamps...
  for(unsigned int i=0;i<scan.pts.size();i++){
    if(maxRotation==scan.pts[i].blockTheta){
      scan.pts[i].vts = scan.vts;
    }else{
      double dt = AngleDiff((float)maxRotation,(float)scan.pts[i].blockTheta)/(F_PI*2.f) * 1.f/15.f;
      scan.pts[i].vts = scan.vts + dt;
    }
  }

#undef RD

  TIMER usrCallbackTmr = GetTime();

  if(scanSegmentCallback!=NULL)
    scanSegmentCallback(scan,scanSegmentCallbackParam);

  timeSpentInUDPCallback+=TimeElapsed(tmr);
  timeSpentInUserCallback+=TimeElapsed(usrCallbackTmr);
  numUDPCallbacks++;
}