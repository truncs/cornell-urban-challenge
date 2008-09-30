#include "RelativePoseResolver.h"
#include <stdio.h>
#include "../Math/Mat4x4d.h"

RelativePoseResolver::RelativePoseResolver(int queueSize) : queue(queueSize,RelativePoseState()){
  InitializeCriticalSection(&cs);
}

RelativePoseResolver::~RelativePoseResolver(){
  DeleteCriticalSection(&cs);
}

void RelativePoseResolver::PushRelPose(const pose_rel_msg& msg){
  int r,c;

  RelativePoseState rps;
  rps.vts = (double)msg.car_ts_secs + (double)msg.car_ts_ticks/10000.0;
  memcpy(rps.Rinit2veh,msg.Rinit2veh,sizeof(double)*16);

  // figure out the inverse
  for (r = 0; r < 3; r++){
    for (c = 0; c < 3; c++){
			//upper left 3x3 block is just transposed
      rps.Rveh2init[c*4+r] = msg.Rinit2veh[r][c];
		}
	}
	//last row is [0, 0, 0, 1]
	rps.Rveh2init[12] = rps.Rveh2init[13] = rps.Rveh2init[14] = 0.0;
	rps.Rveh2init[15] = 1.0;

	//last col is -R'*d
	for (r = 0; r < 3; r++)
	{
		rps.Rveh2init[r*4+3] = 0.0; // init last column to 0
		for (c = 0; c < 3; c++)
			rps.Rveh2init[r*4+3] -= msg.Rinit2veh[c][r] * msg.Rinit2veh[c][3];
	}
  
  EnterCriticalSection(&cs);
  if(queue.n_meas()>0 && queue.newest().vts >= rps.vts){
    printf("RelativePoseResolver WARNING: new relative pose is older than the previous newest state... flushing resolver (new: %lf, already have: %lf)\n",rps.vts,queue.newest().vts);
    queue.reset();
  }
  queue.push(rps);
  LeaveCriticalSection(&cs);

  double test[16];
  matmul4x4(rps.Rinit2veh,rps.Rveh2init,test);
}

bool RelativePoseResolver::GetTransform(double to, double from, double* xform){
  EnterCriticalSection(&cs);
  if(to < queue.oldest().vts || to > queue.newest().vts){
    printf("RelativePoseResolver WARNING: 'to' time (%lf) is outside of known xform bounds (%lf to %lf)\n",to,queue.oldest().vts,queue.newest().vts);
    LeaveCriticalSection(&cs);
    return false;
  }

  if(from < queue.oldest().vts || from > queue.newest().vts){
    printf("RelativePoseResolver WARNING: 'from' time (%lf) is outside of known xform bounds (%lf to %lf)\n",from,queue.oldest().vts,queue.newest().vts);
    LeaveCriticalSection(&cs);
    return false;
  }

  // we've verified that both 'to' and 'from' timestamps are inside the known relative pose xform timespan

  // find the rp states right before "to" and "from"
  int toIndex=0, fromIndex=0;
  for(unsigned int i=0;i<queue.n_meas();i++){
    if(queue[i].vts < to) toIndex = i;
    if(queue[i].vts < from) fromIndex = i;
    
    if(queue[i].vts > to && queue[i].vts > from) break;
  }

  // for now, directly use the closest known relative pose
  // in the future, might want to interpolate

  if(toIndex==fromIndex)
    eye4x4(xform);
  else
    matmul4x4(queue[toIndex].Rinit2veh,queue[fromIndex].Rveh2init,xform);

  LeaveCriticalSection(&cs);

  return true;
}

Transform3d RelativePoseResolver::GetTransform(double to, double from){
  Transform3d ret;

  EnterCriticalSection(&cs);
  if(to < queue.oldest().vts || to > queue.newest().vts){
    printf("RelativePoseResolver WARNING: 'to' time (%lf) is outside of known xform bounds (%lf to %lf)\n",to,queue.oldest().vts,queue.newest().vts);
    LeaveCriticalSection(&cs);
    return ret;
  }

  if(from < queue.oldest().vts || from > queue.newest().vts){
    printf("RelativePoseResolver WARNING: 'from' time (%lf) is outside of known xform bounds (%lf to %lf)\n",from,queue.oldest().vts,queue.newest().vts);
    LeaveCriticalSection(&cs);
    return ret;
  }

  // we've verified that both 'to' and 'from' timestamps are inside the known relative pose xform timespan

  // find the rp states right before "to" and "from"
  int toIndex=0, fromIndex=0;
  for(unsigned int i=0;i<queue.n_meas();i++){
    if(queue[i].vts < to) toIndex = i;
    if(queue[i].vts < from) fromIndex = i;
    
    if(queue[i].vts > to && queue[i].vts > from) break;
  }

  // interpolate to find the "to" Rinit2veh matrix
  double to_Rinit2veh[16];
  double q = (to - queue[toIndex].vts)/(queue[toIndex+1].vts-queue[toIndex].vts);
  for(unsigned int i=0;i<16;i++){
    to_Rinit2veh[i] = queue[toIndex].Rinit2veh[i] * (1.0-q) + queue[toIndex+1].Rinit2veh[i] * q;
  }

  // interpolate to find the "from" Rinit2veh matrix
  double from_Rveh2init[16];
  q = (from - queue[fromIndex].vts)/(queue[fromIndex+1].vts-queue[fromIndex].vts);
  for(unsigned int i=0;i<16;i++){
    from_Rveh2init[i] = queue[fromIndex].Rveh2init[i] * (1.0-q) + queue[fromIndex+1].Rveh2init[i] * q;
  }

  ret = to_Rinit2veh;
  ret = ret * from_Rveh2init;
  
  LeaveCriticalSection(&cs);

  return ret;
}

Transform3d RelativePoseResolver::GetInitToVeh(double vts,bool suppressWarning){
  Transform3d ret;
  ret.valid = false;

  EnterCriticalSection(&cs);
  if(vts < queue.oldest().vts || vts > queue.newest().vts){
    if(!suppressWarning) printf("RelativePoseResolver::GetInitToVeh WARNING: 'vts' time (%lf) is outside of known xform bounds (%lf to %lf)\n",vts,queue.oldest().vts,queue.newest().vts);
    LeaveCriticalSection(&cs);
    return ret;
  }

  // find the rp states right before "to" and "from"
  int vtsIndex=0;
  for(unsigned int i=0;i<queue.n_meas();i++){
    if(queue[i].vts < vts) vtsIndex = i;
    else break;
  }

  // interpolate to find the "to" Rinit2veh matrix
  double q = (vts - queue[vtsIndex].vts)/(queue[vtsIndex+1].vts-queue[vtsIndex].vts);
  for(unsigned int i=0;i<16;i++){
    ret.xform[i] = queue[vtsIndex].Rinit2veh[i] * (1.0-q) + queue[vtsIndex+1].Rinit2veh[i] * q;
  }

  LeaveCriticalSection(&cs);

  ret.valid = true;
  return ret;
}

Transform3d RelativePoseResolver::GetVehToInit(double vts,bool suppressWarning){
  Transform3d ret;
  ret.valid = false;

  EnterCriticalSection(&cs);
  if(vts < queue.oldest().vts || vts > queue.newest().vts){
    if(!suppressWarning) printf("RelativePoseResolver::GetVehToInit WARNING: 'vts' time (%lf) is outside of known xform bounds (%lf to %lf)\n",vts,queue.oldest().vts,queue.newest().vts);
    LeaveCriticalSection(&cs);
    return ret;
  }

  // find the rp states right before "to" and "from"
  int vtsIndex=0;
  for(unsigned int i=0;i<queue.n_meas();i++){
    if(queue[i].vts < vts) vtsIndex = i;
    else break;
  }

  // interpolate to find the "to" Rinit2veh matrix
  double q = (vts - queue[vtsIndex].vts)/(queue[vtsIndex+1].vts-queue[vtsIndex].vts);
  for(unsigned int i=0;i<16;i++){
    ret.xform[i] = queue[vtsIndex].Rveh2init[i] * (1.0-q) + queue[vtsIndex+1].Rveh2init[i] * q;
  }

  LeaveCriticalSection(&cs);

  ret.valid = true;
  return ret;
}