#ifndef MAT_4x4_H_SEPT_7_2007_SVL5
#define MAT_4x4_H_SEPT_7_2007_SVL5

#include "matrix.h"

// now allows for in-place, (even double-in-place :P), multiplication!
inline void matmul4x4(const double* A, const double* B, double* dest){
  int i,j;
  double tmp[16], tmp2[16];

  if(A==dest){
    memcpy(tmp,A,sizeof(double)*16);
    A = tmp;
  }
  if(B==dest){
    memcpy(tmp2,B,sizeof(double)*16);
    B = tmp;
  }

  for (i = 0; i < 4; i++){
	  for (j = 0; j < 4; j++){
		  //calculate the (i, j) entry of A*B
		  dest[i*4+j] = A[i*4+0] * B[0*4+j] + A[i*4+1] * B[1*4+j] + A[i*4+2] * B[2*4+j] + A[i*4+3] * B[3*4+j];
	  }
  }
}

inline void eye4x4(double* A){
  A[0] = A[5] = A[10] = A[15] = 1.0;
  A[1]=A[2]=A[3]=A[4]=A[6]=A[7]=A[8]=A[9]=A[11]=A[12]=A[13]=A[14]=0.0;
} 


// Creates a rotation matrix that rotates the coordinate frame about the x axis by angle theta
inline void rotation_x(double R[16], double theta){
	eye4x4(R);
	R[5] = cos(theta);
	R[6] = sin(theta);
	R[9] = -sin(theta);
	R[10] = cos(theta);
}

// Creates a rotation matrix that rotates the coordinate frame about the y axis by angle theta
inline void rotation_y(double R[16], double theta){
	eye4x4(R);
	R[0] = cos(theta);
	R[2] = -sin(theta);
	R[8] = sin(theta);
	R[10] = cos(theta);
}

//Creates a rotation matrix that rotates the coordinate frame about the z axis by angle theta
inline void rotation_z(double R[16], double theta){
	eye4x4(R);
	R[0] = cos(theta);
	R[1] = sin(theta);
	R[4] = -sin(theta);
	R[5] = cos(theta);
}

inline void rotationYPR(double mat[16],double y,double p,double r){
  double T[16],M[16];
  
  // x is roll
  memcpy(M,mat,sizeof(mat[0])*16);
  rotation_x(T,r);
  matmul4x4(M,T,mat);

  // y is pitch
  memcpy(M,mat,sizeof(mat[0])*16);
  rotation_y(T,p);
  matmul4x4(M,T,mat);

  // z is yaw
  memcpy(M,mat,sizeof(mat[0])*16);
  rotation_z(T,y);
  matmul4x4(M,T,mat);
}

inline void Translate(double xform[16],double x, double y, double z){
  xform[3] += x;
  xform[7] += y;
  xform[11] += z;
}



#endif //MAT_4x4_H_SEPT_7_2007_SVL5
