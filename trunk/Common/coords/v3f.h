#ifndef VECTOR_3_MAR_08_2005
#define VECTOR_3_MAR_08_2005

#ifndef _USE_MATH_DEFINES
#define _USE_MATH_DEFINES
#endif
#include <math.h>

#ifndef UDP_MSVS6_COMPAT
#include "Transform3d.h"
#endif

#define NULL_VECTOR v3f(0.0f,0.0f,0.0f)

inline void normalize_3(float* v){
  float mag = sqrt(v[0]*v[0]+v[1]*v[1]+v[2]*v[2]);
  v[0] /= mag;
  v[1] /= mag;
  v[2] /= mag;
}

class v3f{
public:
  float x,y,z;
  v3f():x(0.f),y(0.f),z(0.f){};
	v3f(float v):x(v),y(v),z(v){};
  v3f(float X, float Y, float Z):x(X),y(Y),z(Z){};
  v3f(float* X):x(X[0]),y(X[1]),z(X[2]){};
  
  v3f operator+(const v3f b) const{    return v3f(x+b.x,y+b.y,z+b.z);  };
  v3f operator*(const v3f b) const{    return v3f(x*b.x,y*b.y,z*b.z);  };
  v3f operator/(const v3f b) const{    return v3f(x/b.x,y/b.y,z/b.z);  };
  v3f operator-(const v3f b) const{    return v3f(x-b.x,y-b.y,z-b.z);  };
  
  v3f operator+(const float b) const{    return v3f(x+b,y+b,z+b);  };
  v3f operator*(const float b) const{    return v3f(x*b,y*b,z*b);  };
  v3f operator/(const float b) const{    return v3f(x/b,y/b,z/b);  };
  v3f operator-(const float b) const{    return v3f(x-b,y-b,z-b);  };

  v3f operator+=(const v3f b){ x+=b.x; y+=b.y; z+=b.z; return *this;};
  v3f operator-=(const v3f b){ x-=b.x; y-=b.y; z-=b.z; return *this;};
  v3f operator*=(const v3f b){ x*=b.x; y*=b.y; z*=b.z; return *this;};
  v3f operator/=(const v3f b){ x/=b.x; y/=b.y; z/=b.z; return *this;};

  v3f operator+=(const float b){ x+=b; y+=b; z+=b; return *this;};
  v3f operator-=(const float b){ x-=b; y-=b; z-=b; return *this;};
  v3f operator*=(const float b){ x*=b; y*=b; z*=b; return *this;};
  v3f operator/=(const float b){ x/=b; y/=b; z/=b; return *this;};

  inline v3f sq() const{ return operator*(*this);}
  float mag() const{return sqrtf(x*x+y*y+z*z);  }

  inline bool isZero() const{return (x==0.0f && y==0.0f && z==0.0f);};

  inline void zero(){x=y=z=0.0f;};

  float operator[](const int i) const{
    switch(i){
      case 0: return x;
      case 1: return y;
      default: return z;
    }
  }

  float& operator[](const int i){
    switch(i){
      case 0: return x;
      case 1: return y;
      default: return z;
    }
  }

  void get(float* v) const {v[0] = x;    v[1] = y;    v[2] = z;};
  void set(float* v)       {x = v[0];    y = v[1];    z = v[2];};

  v3f cross(v3f v) const{
	  v3f resVector;
	  resVector.x = y*v.z - z*v.y;
	  resVector.y = z*v.x - x*v.z;
	  resVector.z = x*v.y - y*v.x;
	  return resVector;
  }

  inline float dot(const v3f a) const{
    return x*a.x + y*a.y + z*a.z;
  }

  inline v3f norm() const{
    v3f res;
	  float l = mag();
	  if (l == 0.0) return NULL_VECTOR;
    return operator/(l);
  }

	inline float sum() const{ return x+y+z;};

  inline float dist(const v3f b) const{    return operator-(b).mag();  }
	inline float dist_sq(const v3f b) const{    return operator-(b).sq().sum();  }

  inline float dist2d_sq(const v3f a) const{ return (a.x-x)*(a.x-x)+(a.y-y)*(a.y-y);}
  inline float dist2d(const v3f a) const{ return sqrtf((a.x-x)*(a.x-x)+(a.y-y)*(a.y-y));}
  inline float mag2d() const {return sqrtf(x*x+y*y);}

  inline float mag2d_sq() const {return (x*x+y*y);}

  inline void transform(const float xform[16]){
    float tmp[4];

	  tmp[0] = x*xform[0]+y*xform[1]+z*xform[2]+xform[3];
    tmp[1] = x*xform[4]+y*xform[5]+z*xform[6]+xform[7];
    tmp[2] = x*xform[8]+y*xform[9]+z*xform[10]+xform[11];
    tmp[3] = x*xform[12]+y*xform[13]+z*xform[14]+xform[15];

    x = tmp[0]/tmp[3];
    y = tmp[1]/tmp[3];
    z = tmp[2]/tmp[3];
  }

  inline void transform(const double xform[16]){
    double tmp[4];

	  tmp[0] = x*xform[0]+y*xform[1]+z*xform[2]+xform[3];
    tmp[1] = x*xform[4]+y*xform[5]+z*xform[6]+xform[7];
    tmp[2] = x*xform[8]+y*xform[9]+z*xform[10]+xform[11];
    tmp[3] = x*xform[12]+y*xform[13]+z*xform[14]+xform[15];

    x = (float)(tmp[0]/tmp[3]);
    y = (float)(tmp[1]/tmp[3]);
    z = (float)(tmp[2]/tmp[3]);
  }

#ifndef UDP_MSVS6_COMPAT
  inline void transform(const Transform3d& xform){
    transform(xform.GetXFormMatrix_RowMajor());
  }
#endif

  inline float  distToLine2D_sq(const v3f A, const v3f B){
    float xU=B.x-A.x,yU=B.y-A.y;
    float t = ((x-A.x)*xU+(y-A.y)*yU) / (xU*xU+yU*yU);
    float xC=A.x+t*xU, yC=A.y+t*yU;
    return (xC-x)*(xC-x)+(yC-y)*(yC-y);
  }

  inline float distToSegment2D_sq(const v3f A, const v3f B){
    float xU=B.x-A.x,yU=B.y-A.y;
    float t = ((x-A.x)*xU+(y-A.y)*yU) / (xU*xU+yU*yU);
    float xC,yC;
    if(t<=0.f){
      xC = A.x; yC = A.y;
    }else if(t>=1.f){
      xC = B.x; yC = B.y;
    }else{
      xC=A.x+t*xU, yC=A.y+t*yU;
    }
    return (xC-x)*(xC-x)+(yC-y)*(yC-y);
  }

  inline v3f ClosestPointOnSegment_2d(const v3f A, const v3f B){
    v3f U = B-A;
    float t = ((x-A.x)*U.x+(y-A.y)*U.y) / U.mag2d_sq();
    if(t<=0.f)  return A;
    if(t>=1.f)  return B;
    return A + U*t;
  }
};

inline float ptToSegmentDistSq(v3f pt, v3f A, v3f B){
  v3f U = B-A;
  float t = U.dot(pt-A)/U.dot(U);
  if(t>1) t=1;
  if(t<0) t=0;
  return (A+U*t).dist_sq(pt);
};

#endif //VECTOR_3_MAR_08_2005