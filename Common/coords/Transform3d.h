#ifndef TRANSFORM_3D_H_SEPT_9_2007_SVL5
#define TRANSFORM_3D_H_SEPT_9_2007_SVL5

#include "../math/Mat4x4d.h"

class Transform3d{
public:
  Transform3d(){
    eye4x4(xform);
  }

  Transform3d(const double mat[16]){
    memcpy(xform,mat,sizeof(double)*16);
  }

  void Reset(){
    eye4x4(xform);
  }

  void Translate(double x, double y, double z){
    xform[3] += x;
    xform[7] += y;
    xform[11] += z;
  }

  Transform3d Transpose(){
    Transform3d ret;
    for(int x=0;x<4;x++){
      for(int y=0;y<4;y++){
        ret.xform[y*4+x] = xform[x*4+y];
      }
    }
    return ret;
  }

  // rotation applied in this order: R, then P, then Y
  void Rotate(double yaw, double pitch, double roll){
    double R[16];
   
    // Roll
    if(roll!=0){
      eye4x4(R);
	    R[5] = cos(roll);
	    R[6] = sin(roll);
	    R[9] = -sin(roll);
	    R[10] = cos(roll);
      matmul4x4(xform,R,xform);
    }

    // Pitch
    if(pitch!=0){
      eye4x4(R);
	    R[0] = cos(pitch);
	    R[2] = -sin(pitch);
	    R[8] = sin(pitch);
	    R[10] = cos(pitch);
      matmul4x4(xform,R,xform);
    }

    // Yaw
    if(yaw!=0){
      eye4x4(R);
	    R[0] = cos(yaw);
	    R[1] = sin(yaw);
	    R[4] = -sin(yaw);
	    R[5] = cos(yaw);
      matmul4x4(xform,R,xform);
    }
  }

  Transform3d Inverse() const{
    Transform3d ret(*this);
    ret.Invert();
    return ret;
  }

  void Invert(){
    double tmp[16];
    unsigned char r,c;

    memcpy(tmp,xform,sizeof(double)*16);

    // figure out the inverse
    for (r = 0; r < 3; r++){
      for (c = 0; c < 3; c++){
			  //upper left 3x3 block is just transposed
        xform[c*4+r] = tmp[r*4+c];
		  }
	  }
	  //last row is [0, 0, 0, 1]
	  xform[12] = xform[13] = xform[14] = 0.0;
	  xform[15] = 1.0;

	  //last col is -R'*d
	  for (r = 0; r < 3; r++)
	  {
		  xform[r*4+3] = 0.0; // init last column to 0
		  for (c = 0; c < 3; c++)
			  xform[r*4+3] -= tmp[c*4+r] * tmp[c*4+3];
	  }
  }

  const double* GetXFormMatrix_RowMajor() const{
    return xform;
  }

  void GetXFormMatrix_RowMajor(double dest[16]) const{
    memcpy(dest,xform,sizeof(double)*16);
  }

  void GetXFormMatrix_ColMajor(double dest[16]) const{
    for(unsigned char r=0;r<4;r++){
      for(unsigned char c=0;c<4;c++){
        dest[c*4+r] = xform[r*4+c];
      }
    }
  }

  // combines this transform with "b"
  Transform3d operator*(const Transform3d& b){
    Transform3d ret(*this);
    matmul4x4(xform,b.xform,ret.xform);
    return ret;
  }

  // combines this transform with "b"
  Transform3d operator*(const double b[16]){
    Transform3d ret(*this);
    matmul4x4(xform,b,ret.xform);
    return ret;
  }

  // v==1 --> use this transform; v==0 --> use 'b' transform
  Transform3d Interpolate(const Transform3d b, double v){
    Transform3d ret(*this);
    for(unsigned char i=0;i<16;i++){
      ret.xform[i] = ret.xform[i]*v + b.xform[i]*(1.0-v);
    }
    return ret;
  }
  
//private:
  double xform[16];
  bool valid;

};

#endif //TRANSFORM_3D_H_SEPT_9_2007_SVL5
