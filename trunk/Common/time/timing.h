// precision timing functions
// Cornell University Robocup2003 - 2004
// sergei lupashin (svl5@cornell.edu)
// 11/09/2002
// revised 02/19/2005 - linux support!
#ifndef UTIL_TIMING_H_11_12_2002
#define UTIL_TIMING_H_11_12_2002
#ifdef _WIN32

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif
typedef __int64 TIMER;

// frequency of the hi-res counter
extern __int64 __highResCounterFreq;

// one global static instance of this transfers the annoyance
// of initializing and de-initializing high-res timing and sleep
class CHighResTimingScope{
public:
  CHighResTimingScope();
  ~CHighResTimingScope();
};

extern CHighResTimingScope __highResTimingScope;

#else

#include <sys/time.h>
typedef struct timeval TIMER;

#endif

// functions to time things...
TIMER getTime();
// returns the time elapsed since the last getTime assignment to [timer] (in seconds!)
double timeElapsed(TIMER timer);

extern TIMER ref_time;

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

inline TIMER GetTime(){
  return getTime();
}

inline double TimeElapsed(TIMER t){
  return timeElapsed(t);
}

#endif
