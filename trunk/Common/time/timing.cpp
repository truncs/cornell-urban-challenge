
#include "timing.h"
#include <stdio.h>

#ifdef _WIN32

#ifndef USING_MFC
#include <windows.h>
#else
#include <afxwin.h>
#endif

#include <mmsystem.h>

#ifdef _WIN32_WCE
#pragma comment(lib,"mmtimer.lib")
#else
#pragma comment(lib,"winmm.lib")
#endif

#ifdef __cplusplus_cli
#pragma unmanaged
#endif
static CHighResTimingScope __highResTimingScope;
static __int64 __highResCounterFreq;

#endif

/*void getTime(TIMER& timer){
#ifdef _WIN32
  QueryPerformanceCounter((LARGE_INTEGER*)&timer);      
#else
  gettimeofday(&timer,NULL);
#endif
}*/

//TIMER getTime(){
//  TIMER ret;
//  getTime(ret);
//  return ret;
//}

TIMER getTime(){
  TIMER ret;
#ifdef _WIN32
  QueryPerformanceCounter((LARGE_INTEGER*)&ret);      
#else
  gettimeofday(&ret,NULL);
#endif
  return ret;
}

double timeElapsed(TIMER timer){
#ifdef _WIN32
  __int64 current;
  QueryPerformanceCounter((LARGE_INTEGER*)&current);
  return ((double)(current-timer)) / (double)__highResCounterFreq;
#else
  struct timeval end;
//  double t1, t2;
  unsigned long int t1a, t2a;
  gettimeofday(&end,NULL);
  // note to self: replacing the doubles below with floats breaks everything (always results in 0). whaa?
  t1a = timer.tv_sec*1000000 + timer.tv_usec;
  t2a = end.tv_sec*1000000 + end.tv_usec;
//  t1 = (double)timer.tv_sec + (double)timer.tv_usec/(1000.0*1000.0);
//  t2 = (double)end.tv_sec + (double)end.tv_usec/(1000.0*1000.0);
  return ((double)(t2a-t1a)/(1000000.0));
#endif
}

#ifdef _WIN32

//#error "lost in translation"
//the stuff here had to be removed due to switching to C.

// the one and only ref_time
TIMER ref_time;

CHighResTimingScope::CHighResTimingScope(){
  timeBeginPeriod(1); // inits hi-res sleep
  QueryPerformanceFrequency((LARGE_INTEGER*)&__highResCounterFreq);
  if(__highResCounterFreq==0){
    printf("ERROR: no performance counter found\n");
    __highResCounterFreq = 1;
  }
  ref_time = getTime();
};
CHighResTimingScope::~CHighResTimingScope(){
  timeEndPeriod(1); // de-inits hi-res sleep
};




#endif
