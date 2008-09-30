#ifndef ANGLE_H_SEPT_7_2007_SVL5
#define ANGLE_H_SEPT_7_2007_SVL5

#define _USE_MATH_DEFINES
#include <math.h>
#include <limits>
using namespace std;
#include <stdio.h>
#include <windows.h>

#ifndef D_PI
#define D_PI M_PI
#endif

#ifndef F_PI
#define F_PI ((float)M_PI)
#endif

#ifndef MAX
#define MAX(A,B) (((A)>(B))?(A):(B))
#endif

#ifndef MIN
#define MIN(A,B) (((A)<(B))?(A):(B))
#endif


inline float Deg2Rad(float a){
  return (a/180.f*F_PI);
}
inline float Rad2Deg(float a){
  return (a/F_PI*180.f);
}
inline double Deg2Rad(double a){
  return (a/180.0*D_PI);
}
inline double Rad2Deg(double a){
  return (a/D_PI*180.0);
}

// 1 = greater than (a is ccw of b)
// 0 = equal
// -1 = less than (a is cw of b)
inline char AngleComparison(float a, float b){
  if(a==b) return 0;
  if(a==numeric_limits<float>::infinity()) return 1;
  if(a==-numeric_limits<float>::infinity()) return -1;
  if(b==numeric_limits<float>::infinity()) return -1;
  if(b==-numeric_limits<float>::infinity()) return 1;
 
  while(a>F_PI*2.f) a-=(F_PI*2.f);
  while(b>F_PI*2.f) b-=(F_PI*2.f);
  while(a<0) a+=(F_PI*2.f);
  while(b<0) b+=(F_PI*2.f);
  if(a==b) return 0;

  float A = MIN(a,b);
  float B = MAX(a,b)-A;

  if(B<F_PI) return (a<b)?-1:1;
  return  (a>b)?-1:1;
}

inline bool Angle_A_Left_Of_B(float a, float b){
  return AngleComparison(a,b)>0;
}

inline bool Angle_A_Right_Of_B(float a, float b){
  return AngleComparison(a,b)<0;
}

inline float AngleMin(float a, float b){
  if(a==numeric_limits<float>::infinity() || a==-numeric_limits<float>::infinity()) return b;
  if(b==numeric_limits<float>::infinity() || b==-numeric_limits<float>::infinity()) return a;

  if(a>1000.f || b>1000.f){
    printf("ANGLEMIN ERROR: %lf %lf\n",a,b);
    return 0.f;
  }

  while(a>F_PI*2.f) a-=(F_PI*2.f);
  while(b>F_PI*2.f) b-=(F_PI*2.f);
  while(a<0) a+=(F_PI*2.f);
  while(b<0) b+=(F_PI*2.f);
  
  float A = MIN(a,b);
  float B = MAX(a,b)-A;

  if(B<F_PI) return MIN(a,b);
  return MAX(a,b);
}

inline float AngleMax(float a, float b){
  if(a==numeric_limits<float>::infinity() || a==-numeric_limits<float>::infinity()) return b;
  if(b==numeric_limits<float>::infinity() || b==-numeric_limits<float>::infinity()) return a;

  //DEBUG
  if(a>1000.f || b>1000.f){
    printf("ANGLEMAX ERROR: %lf %lf\n",a,b);
    return 0.f;
  }

  while(a>F_PI*2.f) a-=(F_PI*2.f);
  while(b>F_PI*2.f) b-=(F_PI*2.f);
  while(a<0) a+=(F_PI*2.f);
  while(b<0) b+=(F_PI*2.f);
  
  float A = MIN(a,b);
  float B = MAX(a,b)-A;

  if(B<F_PI) return MAX(a,b);
  return MIN(a,b);
}

inline float AngleDiff(float a, float b){
  if(a==b) return 0.f;
  if(a<-1000.f || a>1000.f || b<-1000.f || b>1000.f){
    DebugBreak();
    return 0.f;
  }
  while(a>F_PI*2.f) a-=(F_PI*2.f);
  while(b>F_PI*2.f) b-=(F_PI*2.f);
  while(a<0) a+=(F_PI*2.f);
  while(b<0) b+=(F_PI*2.f);
  
  float A = MAX(a,b)-MIN(a,b);
  float B = MIN(a,b)-MAX(a,b);
  if(B<0) B+=2.f*F_PI;
  
  return MIN(A,B);

  //float dot = cosf(a)*cosf(b)+sinf(a)*sinf(b);
  //return acosf(dot);
}

#endif //ANGLE_H_SEPT_7_2007_SVL5
