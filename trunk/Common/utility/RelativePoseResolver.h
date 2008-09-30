#pragma once

#include "../PoseInterface/pose_message.h"
#include "../Utility/FixedQueue.h"
#include "../Coords/Transform3d.h"
#include <windows.h>

class RelativePoseResolver{
public:
  RelativePoseResolver(int queueSize=200);
  ~RelativePoseResolver();

  void PushRelPose(const pose_rel_msg& msg);
  bool GetTransform(double to, double from, double* xform);
  Transform3d GetTransform(double to, double from); 

  bool IsKnownTime(double vts){
    EnterCriticalSection(&cs);
    if(vts < queue.oldest().vts || vts > queue.newest().vts){
      LeaveCriticalSection(&cs);
      return false;
    }
    LeaveCriticalSection(&cs);
    return true;
  }

  double OldestVTS(){
    if(queue.empty()) return 0;
    return queue.oldest().vts;
  }
  double NewestVTS(){
    if(queue.empty()) return 0;
    return queue.newest().vts;
  }

  Transform3d GetInitToVeh(double vts,bool suppressWarning=false);
  Transform3d GetVehToInit(double vts,bool suppressWarning=false);

private:
  struct RelativePoseState{
    double vts;
    double Rinit2veh[16]; // row-major order
    double Rveh2init[16]; // the inverse of the above matrix
  };
  FixedQueue<RelativePoseState> queue;
  CRITICAL_SECTION cs;
};