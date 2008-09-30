#ifndef VECTOR_3_D_MAR_23_2005
#define VECTOR_3_D_MAR_23_2005

#ifndef _USE_MATH_DEFINES
#define _USE_MATH_DEFINES
#endif
#include <math.h>

#define NULL_VECTOR_D v3d(0.0,0.0,0.0)

inline void normalize_3(double* v){
  double mag = sqrt(v[0]*v[0]+v[1]*v[1]+v[2]*v[2]);
  v[0] /= mag;
  v[1] /= mag;
  v[2] /= mag;
}

class v3d{
public:
  double x,y,z;
  v3d():x(0),y(0),z(0){};
	v3d(double v):x(v),y(v),z(v){};
  v3d(double X, double Y, double Z):x(X),y(Y),z(Z){};
  v3d(double* d):x(d[0]),y(d[1]),z(d[2]){};
  
  inline v3d operator+(const v3d b) const{    return v3d(x+b.x,y+b.y,z+b.z);  };
  inline v3d operator*(const v3d b) const{    return v3d(x*b.x,y*b.y,z*b.z);  };
  inline v3d operator/(const v3d b) const{    return v3d(x/b.x,y/b.y,z/b.z);  };
  inline v3d operator-(const v3d b) const{    return v3d(x-b.x,y-b.y,z-b.z);  };
  
  inline v3d operator+(const double b) const{    return v3d(x+b,y+b,z+b);  };
  inline v3d operator*(const double b) const{    return v3d(x*b,y*b,z*b);  };
  inline v3d operator/(const double b) const{    return v3d(x/b,y/b,z/b);  };
  inline v3d operator-(const double b) const{    return v3d(x-b,y-b,z-b);  };

  inline bool operator==(const v3d& b) const{ return (b.x==x && b.y==y && b.z==z);}
  inline bool operator!=(const v3d& b) const{ return (b.x!=x || b.y!=y || b.z!=z);}

  inline v3d operator/=(const v3d b){ *this = *this / b; return *this;}
  inline v3d operator*=(const v3d b){ *this = *this * b; return *this;}
  inline v3d operator+=(const v3d b){ *this = *this + b; return *this;}
  inline v3d operator-=(const v3d b){ *this = *this - b; return *this;}

  inline v3d sq() const{ return operator*(*this);}
	
	inline v3d operator - () const { return v3d(-x, -y, -z); }

  inline double mag() const{return sqrt(x*x+y*y+z*z);  }

  inline bool isZero() const{return (x==0.0 && y==0.0 && z==0.0);};

  inline void zero(){x=y=z=0.0;};

  inline double operator[](const int i) const {
    switch (i) {
      case 0: return x;
      case 1: return y;
      default: return z;
    }
  }
	
  inline double& operator[](const int i){
    switch(i){
      case 0: return x;
      case 1: return y;
      default: return z;
    }
  }

  inline void get(double* v) const {v[0] = x;    v[1] = y;    v[2] = z;};
  inline void set(double* v)       {x = v[0];    y = v[1];    z = v[2];};

  inline v3d cross(const v3d v) const{
	  v3d resVector;
	  resVector.x = y*v.z - z*v.y;
	  resVector.y = z*v.x - x*v.z;
	  resVector.z = x*v.y - y*v.x;
	  return resVector;
  }

  inline double dot(const v3d a) const{
    return x*a.x + y*a.y + z*a.z;
  }

  inline v3d norm() const{
    v3d res;
	  double l = mag();
	  if (l == 0.0) return NULL_VECTOR_D;
    return operator/(l);
  }

	inline double sum() const{ return x+y+z;};

  inline double dist(const v3d b) const{    return operator-(b).mag();  }
	inline double dist_sq(const v3d b) const{    return operator-(b).sq().sum();  }
};


#endif //VECTOR_3_MAR_08_2005

