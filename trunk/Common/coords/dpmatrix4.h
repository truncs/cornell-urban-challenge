#ifndef _DPMATRIX4_H
#define _DPMATRIX4_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

#include <cassert>
#include <emmintrin.h>
#include "dpmatrix2.h"

#pragma pack(push,16) // 16-byte alignment for all XMM stuff

class dpvector4;

_MM_ALIGN16 class dpmatrix4 {
private:
	// note that data must be stored in column-major order, which makes things annoying
	union {
		struct {
			__m128d L1a, L1b,						// L1a = column 1, row set a (1, 2), L1b = column 1 , row set b(3, 4)
							L2a, L2b,
							L3a, L3b,
							L4a, L4b;
		};
		struct {
			double m11, m21, m31, m41,	// this is the transpose of what you would think, but column-major style
						 m12, m22, m32, m42,
						 m13, m23, m33, m43,
						 m14, m24, m34, m44;
		};
		double m[16];
	};

public:
	inline dpmatrix4() {}
	inline dpmatrix4(const dpmatrix4& c) : L1a(c.L1a), L1b(c.L1b), L2a(c.L2a), L2b(c.L2b), 
																			   L3a(c.L3a), L3b(c.L3b), L4a(c.L4a), L4b(c.L4b) {}

	dpmatrix4(double m11, double m12, double m13, double m14, 
						double m21, double m22, double m23, double m24,
						double m31, double m32, double m33, double m34,
						double m41, double m42, double m43, double m44);

	dpmatrix4(const double* mat);

	inline dpmatrix4(const __m128d& l1a, const __m128d& l1b, const __m128d& l2a, const __m128d& l2b,
		const __m128d& l3a, const __m128d& l3b, const __m128d& l4a, const __m128d& l4b) 
		: L1a(l1a), L1b(l1b), L2a(l2a), L2b(l2b), L3a(l3a), L3b(l3b), L4a(l4a), L4b(l4b) {}

	inline dpmatrix4& operator = (const dpmatrix4& c) {
		L1a = c.L1a; L1b = c.L1b;
		L2a = c.L2a; L2b = c.L2b;
		L3a = c.L3a; L3b = c.L3b;
		L4a = c.L4a; L4b = c.L4b;
		return *this;
	}

	// element access
	// row, col
	inline double  operator() (const int i, const int j) const {
		assert (i >= 0 && i < 4);
		assert (j >= 0 && j < 4);
		return m[j * 4 + i]; 
	}

	inline double& operator() (const int i, const int j) { 
		assert (i >= 0 && i < 4);
		assert (j >= 0 && j < 4);
		return m[j * 4 + i]; 
	}

	friend dpmatrix4 operator * (const dpmatrix4&, const dpmatrix4&);
	friend dpmatrix4 operator + (const dpmatrix4&, const dpmatrix4&);
	friend dpmatrix4 operator - (const dpmatrix4&, const dpmatrix4&);
	friend dpmatrix4 operator + (const dpmatrix4&);
	friend dpmatrix4 operator - (const dpmatrix4&);
	friend dpmatrix4 operator * (const dpmatrix4&, const double);
	friend dpmatrix4 operator * (const double, const dpmatrix4&);
	friend dpvector4 operator * (const dpmatrix4&, const dpvector4&);
	friend dpvector4 operator * (const dpvector4&, const dpmatrix4&);

	dpmatrix4& operator *= (const dpmatrix4&);
	dpmatrix4& operator *= (const double);
	dpmatrix4& operator += (const dpmatrix4&);
	dpmatrix4& operator -= (const dpmatrix4&);

	dpmatrix4 transpose() const;

	double inv();

	double det() const;
	
	static dpmatrix4 YPR(double yaw, double pitch, double roll);
	static dpmatrix4 translate (double dx, double dy, double dz);
	static dpmatrix4 zero();
	static dpmatrix4 identity();
	/*static dpmatrix4 TranslateMatrix(const double dx, const double dy, const double dz);
	static dpmatrix4 ScaleMatrix(const double sx, const double sy, const double sz);
	static dpmatrix4 ScaleMatrix(const double x);
	static dpmatrix4 RotateXMatrix(const double rads);
	static dpmatrix4 RotateYMatrix(const double rads);
	static dpmatrix4 RotateZMatrix(const double rads);*/

	void get(double* mat);

	dpmatrix2 submatrixA() const { return dpmatrix2(L1a, L2a); }
	dpmatrix2 submatrixB() const { return dpmatrix2(L3a, L4a); }
	dpmatrix2 submatrixC() const { return dpmatrix2(L1b, L2b); }
	dpmatrix2 submatrixD() const { return dpmatrix2(L3b, L4b); }
};

#pragma pack(pop) 

#include "dpvector4.h"

inline dpmatrix4::dpmatrix4(
		double m11, double m12, double m13, double m14, 
		double m21, double m22, double m23, double m24,
		double m31, double m32, double m33, double m34,
		double m41, double m42, double m43, double m44) {

	// store everything in column-major order, which is stuff, but that's what BLAS wants
	L1a = _mm_set_pd(m21, m11); L1b = _mm_set_pd(m41, m31);
	L2a = _mm_set_pd(m22, m12); L2b = _mm_set_pd(m42, m32);
	L3a = _mm_set_pd(m23, m13); L3b = _mm_set_pd(m43, m33);
	L4a = _mm_set_pd(m24, m14); L4b = _mm_set_pd(m44, m34);
}

// c must be in column-major order
inline dpmatrix4::dpmatrix4(const double* c) {
	/*if (((void*)c & 0xF) == 0) {
		// this is 16-byte aligned, take advantage because the speedup is huge
		L1a = _mm_load_pd(c);			L1b = _mm_load_pd(c+2);
		L2a = _mm_load_pd(c+4);		L2b = _mm_load_pd(c+6);
		L3a = _mm_load_pd(c+8);		L3b = _mm_load_pd(c+10);
		L4a = _mm_load_pd(c+12);	L4b = _mm_load_pd(c+14);
	}
	else {*/
		// this is unaligned, revert back to unaligned load
		L1a = _mm_loadu_pd(c);		L1b = _mm_loadu_pd(c+2);
		L2a = _mm_loadu_pd(c+4);	L2b = _mm_loadu_pd(c+6);
		L3a = _mm_loadu_pd(c+8);	L3b = _mm_loadu_pd(c+10);
		L4a = _mm_loadu_pd(c+12); L4b = _mm_loadu_pd(c+14);
	//}
}

inline dpmatrix4 dpmatrix4::zero() {
	dpmatrix4 r;
	r.L1a = r.L1b = r.L2a = r.L2b = r.L3a = r.L3b = r.L4a = r.L4b = _mm_setzero_pd();
	return r;
}

inline dpmatrix4 dpmatrix4::identity() {
	dpmatrix4 r;
	__m128d onezero = _mm_set_sd(1.0);   //_ZERONE_;
	r.L1b = r.L2b = r.L3a = r.L4a = _mm_setzero_pd();
  r.L1a = r.L3b = onezero;
  onezero = _mm_shuffle_pd(onezero, onezero, 1);
  r.L2a = r.L4b = onezero;
	return r;
}

inline dpmatrix4 operator * (const dpmatrix4& A, const dpmatrix4& B) {
	dpmatrix4 r;
	dpvector4 r1, r2;
	// columns of A
	dpvector4 A1(A.L1a, A.L1b), A2(A.L2a, A.L2b), A3(A.L3a, A.L3b), A4(A.L4a, A.L4b);
	
	r1 = A1 * B.m11;
	r2 = A1 * B.m12;
	r1 += A2 * B.m21;
	r2 += A2 * B.m22;
	r1 += A3 * B.m31;
	r2 += A3 * B.m32;
	r1 += A4 * B.m41;
	r2 += A4 * B.m42;

	r.L1a = r1.a; r.L1b = r1.b;
	r.L2a = r2.a; r.L2b = r2.b;

	r1 = A1 * B.m13;
	r2 = A1 * B.m14;
	r1 += A2 * B.m23;
	r2 += A2 * B.m24;
	r1 += A3 * B.m33;
	r2 += A3 * B.m34;
	r1 += A4 * B.m43;
	r2 += A4 * B.m44;
	
	r.L3a = r1.a; r.L3b = r1.b;
	r.L4a = r2.a; r.L4b = r2.b;

	return r;
}

inline dpmatrix4 operator + (const dpmatrix4& A, const dpmatrix4& B) {
	dpmatrix4 r;
	r.L1a = _mm_add_pd(A.L1a, B.L1a); r.L1b = _mm_add_pd(A.L1b, B.L1b);
	r.L2a = _mm_add_pd(A.L2a, B.L2a); r.L2b = _mm_add_pd(A.L2b, B.L2b);
	r.L3a = _mm_add_pd(A.L3a, B.L3a); r.L3b = _mm_add_pd(A.L3b, B.L3b);
	r.L4a = _mm_add_pd(A.L4a, B.L4a); r.L4b = _mm_add_pd(A.L4b, B.L4b);
	return r;
}

inline dpmatrix4 operator - (const dpmatrix4& A, const dpmatrix4& B) {
	dpmatrix4 r;
	r.L1a = _mm_sub_pd(A.L1a, B.L1a); r.L1b = _mm_sub_pd(A.L1b, B.L1b);
	r.L2a = _mm_sub_pd(A.L2a, B.L2a); r.L2b = _mm_sub_pd(A.L2b, B.L2b);
	r.L3a = _mm_sub_pd(A.L3a, B.L3a); r.L3b = _mm_sub_pd(A.L3b, B.L3b);
	r.L4a = _mm_sub_pd(A.L4a, B.L4a); r.L4b = _mm_sub_pd(A.L4b, B.L4b);
	return r;
}

inline dpmatrix4 operator + (const dpmatrix4& A) {
	return A;
}

inline dpmatrix4 operator - (const dpmatrix4& A) {
	dpmatrix4 r;
	r.L1a = _mm_neg_pd(A.L1a); r.L1b = _mm_neg_pd(A.L1b);
	r.L2a = _mm_neg_pd(A.L2a); r.L2b = _mm_neg_pd(A.L2b);
	r.L3a = _mm_neg_pd(A.L3a); r.L3b = _mm_neg_pd(A.L3b);
	r.L4a = _mm_neg_pd(A.L4a); r.L4b = _mm_neg_pd(A.L4b);
	return r;
}

inline dpmatrix4 operator * (const dpmatrix4& A, const double s) {
	dpmatrix4 r;
	__m128d S = _mm_set1_pd(s);

	r.L1a = _mm_mul_pd(A.L1a, S); r.L1b = _mm_mul_pd(A.L1b, S);
	r.L2a = _mm_mul_pd(A.L2a, S); r.L2b = _mm_mul_pd(A.L2b, S);
	r.L3a = _mm_mul_pd(A.L3a, S); r.L3b = _mm_mul_pd(A.L3b, S);
	r.L4a = _mm_mul_pd(A.L4a, S); r.L4b = _mm_mul_pd(A.L4b, S);
	return r;
}

inline dpmatrix4 operator * (const double s, const dpmatrix4& A) {
	return A * s;
}

inline dpvector4 operator * (const dpmatrix4& A, const dpvector4& c) {
	dpvector4 r;

	// use the sum of column vector times scalar form
	r = dpvector4(A.L1a, A.L1b) * c[0];
	r += dpvector4(A.L2a, A.L2b) * c[1];
	r += dpvector4(A.L3a, A.L3b) * c[2];
	r += dpvector4(A.L4a, A.L4b) * c[3];

	return r;
}

inline dpvector4 operator * (const dpvector4& c, const dpmatrix4& A) {
	// use the row dot col form
	return dpvector4(c.dot(dpvector4(A.L1a, A.L1b)), c.dot(dpvector4(A.L2a, A.L2b)), 
		c.dot(dpvector4(A.L3a, A.L3b)), c.dot(dpvector4(A.L4a, A.L4b)));
}

inline dpmatrix4& dpmatrix4::operator *= (const dpmatrix4& A) {
	(*this) = (*this) * A;
	return *this;
}

inline dpmatrix4& dpmatrix4::operator *= (const double s) {
	(*this) = (*this) * s;
	return *this;
}

inline dpmatrix4& dpmatrix4::operator += (const dpmatrix4& A) {
	(*this) = (*this) + A;
	return *this;
}

inline dpmatrix4& dpmatrix4::operator -= (const dpmatrix4& A) {
	(*this) = (*this) - A;
	return *this;
}

inline dpmatrix4 dpmatrix4::transpose() const {
	dpmatrix4 r;
	r.L1a = _mm_unpacklo_pd(L1a, L2a);
	r.L1b = _mm_unpacklo_pd(L3a, L4a);
	r.L2a = _mm_unpackhi_pd(L1a, L2a);
	r.L2b = _mm_unpackhi_pd(L3a, L4a);
	r.L3a = _mm_unpacklo_pd(L1b, L2b);
	r.L3b = _mm_unpacklo_pd(L3b, L4b);
	r.L4a = _mm_unpackhi_pd(L1b, L2b);
	r.L4b = _mm_unpackhi_pd(L3b, L4b);
	return r;
}

inline double dpmatrix4::inv() {
	// The inverse is calculated using "Divide and Conquer" technique. 
	// The original matrix is divided into four 2x2 sub-matrices. 
	// See "Numerical Recipies in C," 2.7 (pg 77)

	dpmatrix2 iA = submatrixA();
	dpmatrix2 B = submatrixB();
	dpmatrix2 C = submatrixC();
	dpmatrix2 Dinv = submatrixD();
	double det = Dinv.det();
	Dinv.inv();

	iA = (iA - B * Dinv * C);
	det *= iA.det();
	iA.inv();
	dpmatrix2 iB = B * Dinv;
	dpmatrix2 iC = Dinv * C;
	dpmatrix2 iD = Dinv + iC * iA * iB;
	iB = -iA * iB;
	iC = -iC * iA;

	L1a = iA.L1; L2a = iA.L2; L3a = iB.L1; L4a = iB.L2;
	L1b = iC.L1; L2b = iC.L2; L3b = iD.L1; L4b = iD.L2;

	return det;
}

inline double dpmatrix4::det() const {
	dpmatrix2 Dinv = submatrixD();
	double det = Dinv.det();
	Dinv.inv();

	dpmatrix2 iA = submatrixA();
	dpmatrix2 B = submatrixB();
	dpmatrix2 C = submatrixC();
	iA = (iA - B * Dinv * C);
	return det * iA.det();
}

//added by amn32 for doing homogenous coordinate crap
//they are identical to the Matrix4.cs file used in the managed code
inline dpmatrix4 dpmatrix4::YPR(double yaw, double pitch, double roll) {

	double cy = cos(yaw), sy = sin(yaw);
	double cp = cos(pitch), sp = sin(pitch);
	double cr = cos(roll), sr = sin(roll);

	dpmatrix4 rz(
				 cy, sy, 0, 0,
				 -sy,  cy, 0, 0,
				  0,   0, 1, 0,
				  0,   0, 0, 1
					);
	
	dpmatrix4 ry (
				 cp, 0, -sp, 0,
				  0, 1,  0, 0,
				 sp, 0, cp, 0,
				  0, 0,  0, 1
				);

	dpmatrix4 rx (
				1,   0,   0, 0,
				0,  cr, sr, 0,
				0,  -sr,  cr, 0,
				0,   0,   0, 1
				);
	
	return rx*ry*rz;
}

inline dpmatrix4 dpmatrix4::translate(double dx, double dy, double dz) {
	return dpmatrix4(
				0, 0, 0, dx,
				0, 0, 0, dy,
				0, 0, 0, dz,
				0, 0, 0,  0
				);	

}

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif
