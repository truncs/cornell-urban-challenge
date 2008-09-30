#ifndef _DPMATRIX3_H
#define _DPMATRIX3_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

#include "sse.h"
#include <cassert>

class dpvector3;

_MM_ALIGN16 class dpmatrix3 {
	friend class matrix;
public:
	union {
		struct {
			__m128d L1a, L1b, L2a, L2b, L3a, L3b; // the last upper qword of the b vectors is not used
		};
		struct {
			double m11, m21, m31, m_1, m12, m22, m32, m_2, m13, m23, m33, m_3;
		};
		double m[12];
	};

public:

	inline dpmatrix3() {}
	inline dpmatrix3(const dpmatrix3& c) : L1a(c.L1a), L1b(c.L1b), L2a(c.L2a), L2b(c.L2b), L3a(c.L3a), L3b(c.L3b) {}
	inline dpmatrix3(
		double m11, double m12, double m13, 
		double m21, double m22, double m23,
		double m31, double m32, double m33)
		: L1a(_mm_set_pd(m21, m11)), L1b(_mm_set_pd(0.0, m31)), L2a(_mm_set_pd(m22, m12)), 
		L2b(_mm_set_pd(0.0, m32)), L3a(_mm_set_pd(m23, m13)), L3b(_mm_set_pd(0.0, m33)) {};
	dpmatrix3(const double* c);
	inline dpmatrix3(
		const __m128d& l1a, const __m128d& l1b, 
		const __m128d& l2a, const __m128d& l2b, 
		const __m128d& l3a, const __m128d& l3b) : L1a(l1a), L1b(l1b), L2a(l2a), L2b(l2b), L3a(l3a), L3b(l3b) {}

	inline dpmatrix3& operator = (const dpmatrix3& c) {
		L1a = c.L1a; L1b = c.L1b; L2a = c.L2a; L2b = c.L2b;	L3a = c.L3a; L3b = c.L3b;
		return *this;
	}

	// element access
	inline double operator () (const int i, const int j) const {
		assert(i >= 0 && i < 3 && j >= 0 && j < 3);
		return m[j * 4 + i];
	}

	inline double& operator () (const int i, const int j) {
		assert(i >= 0 && i < 3 && j >= 0 && j < 3);
		return m[j * 4 + i];
	}

	inline dpvector3 col(const int j) const;
	inline dpvector3 row(const int i) const;

	friend dpmatrix3 operator * (const dpmatrix3&, const dpmatrix3&);
	friend dpmatrix3 operator + (const dpmatrix3&, const dpmatrix3&);
	friend dpmatrix3 operator - (const dpmatrix3&, const dpmatrix3&);
	friend dpmatrix3 operator - (const dpmatrix3&);
	friend dpmatrix3 operator * (const dpmatrix3&, const double);
	friend dpmatrix3 operator * (const double, const dpmatrix3&);
	friend dpvector3 operator * (const dpmatrix3&, const dpvector3&);
	friend dpvector3 operator * (const dpvector3&, const dpmatrix3&);

	dpmatrix3& operator *= (const dpmatrix3&);
	dpmatrix3& operator *= (const double);
	dpmatrix3& operator += (const dpmatrix3&);
	dpmatrix3& operator -= (const dpmatrix3&);

	dpmatrix3  transpose() const;

	double inv();

	double det() const;

	double maxelem() const;
	double minelem() const;

	dpvector3 extract_ypr() const;

	static dpmatrix3 zero();
	static dpmatrix3 identity();

	void get(double* c);
};

#include "dpvector3.h" 
#include "dpvector2.h"

inline dpmatrix3::dpmatrix3(const double* c) {
	for (int j = 0; j < 3; j++) for (int i = 0; i < 3; i++) m[j * 4 + i] = c[j * 3 + i];
}

inline dpmatrix3 operator * (const dpmatrix3& A, const dpmatrix3& B) {
	dpmatrix3 r;
	dpvector3 r1, r2;
	// columns of A
	dpvector3 A1(A.L1a, A.L1b), A2(A.L2a, A.L2b), A3(A.L3a, A.L3b);

	r1  = A1 * B.m11;
	r2  = A1 * B.m12;
	r1 += A2 * B.m21;
	r2 += A2 * B.m22;
	r1 += A3 * B.m31;
	r2 += A3 * B.m32;

	r.L1a = r1.a; r.L1b = r1.b;
	r.L2a = r2.a; r.L2b = r2.b;

	r1  = A1 * B.m13;
	r1 += A2 * B.m23;
	r1 += A3 * B.m33;

	r.L3a = r1.a; r.L3b = r1.b;

	return r;
}

inline dpmatrix3 operator + (const dpmatrix3& A, const dpmatrix3& B) {
	return dpmatrix3(
		_mm_add_pd(A.L1a, B.L1a), _mm_add_sd(A.L1b, B.L1b),
		_mm_add_pd(A.L2a, B.L2a), _mm_add_sd(A.L2b, B.L2b),
		_mm_add_pd(A.L3a, B.L3a), _mm_add_sd(A.L3b, B.L3b));
}

inline dpmatrix3 operator - (const dpmatrix3& A, const dpmatrix3& B) {
	return dpmatrix3(
		_mm_sub_pd(A.L1a, B.L1a), _mm_sub_sd(A.L1b, B.L1b),
		_mm_sub_pd(A.L2a, B.L2a), _mm_sub_sd(A.L2b, B.L2b),
		_mm_sub_pd(A.L3a, B.L3a), _mm_sub_sd(A.L3b, B.L3b));
}

inline dpmatrix3 operator - (const dpmatrix3& A) {
	return dpmatrix3(_mm_neg_pd(A.L1a), _mm_neg_pd(A.L1b), _mm_neg_pd(A.L2a), 
		_mm_neg_pd(A.L2b), _mm_neg_pd(A.L3a), _mm_neg_pd(A.L3b));
}

inline dpmatrix3 operator * (const dpmatrix3& A, const double c) {
	__m128d t = _mm_set1_pd(c);
	return dpmatrix3(
		_mm_mul_pd(A.L1a, t), _mm_mul_sd(A.L1b, t), _mm_mul_pd(A.L2a, t),
		_mm_mul_sd(A.L2b, t), _mm_mul_pd(A.L3a, t), _mm_mul_sd(A.L3b, t));
}

inline dpmatrix3 operator * (const double c, const dpmatrix3& A) {
	return A * c;
}

// verified
inline dpvector3 operator * (const dpmatrix3& A, const dpvector3& c) {
	// use the sum of column vector times scalar form
	dpvector3 r;

	r  = dpvector3(A.L1a, A.L1b) * c[0];
	r += dpvector3(A.L2a, A.L2b) * c[1];
	r += dpvector3(A.L3a, A.L3b) * c[2];

	return r;
}

// not verified
// this doesn't work!! bws mar-14-07
inline dpvector3 operator * (const dpvector3& c, const dpmatrix3& A) {
	dpvector3 r(
		dpvector3(A.L1a, A.L1b).dot(c),
		dpvector3(A.L2a, A.L2b).dot(c),
		dpvector3(A.L3a, A.L3b).dot(c));

	return r;
}

inline dpmatrix3& dpmatrix3::operator *= (const dpmatrix3& A) { return *this = *this * A; }
inline dpmatrix3& dpmatrix3::operator *= (const double c) { return *this = *this * c; }
inline dpmatrix3& dpmatrix3::operator += (const dpmatrix3& A) { return *this = *this + A; }
inline dpmatrix3& dpmatrix3::operator -= (const dpmatrix3& A) { return *this = *this - A; }

inline dpmatrix3 dpmatrix3::transpose() const {
	__m128d z = _mm_setzero_pd();
	return dpmatrix3(
		_mm_unpacklo_pd(L1a, L2a), _mm_shuffle_pd(L3a, z, 0), 
		_mm_unpackhi_pd(L1a, L2a), _mm_shuffle_pd(L3a, z, 1),
		_mm_shuffle_pd(L1b, L2b, 0), L3b);
}

inline double dpmatrix3::inv() {
	// could do the explicit formulation (see wikipedia - Invertible Matrix)
	// suspect that divide and conquer will be faster
	dpmatrix2 iA(L1a, L2a);
	dpvector2 iB(L3a), iC(_mm_shuffle_pd(L1b, L2b, 0));
	double det = m33;
	double Dinv = 1.0/det; // inverse of scalar is reciporcal

	iA = (iA - iB.outerproduct(iC * Dinv));
	det *= iA.inv(); // multiply by |iA|
	iB *= Dinv;
	iC *= Dinv;
	double iD = Dinv + iC.dot(iA * iB);
	iB = -(iA * iB);
	iC = -(iC * iA);

	L1a = iA.L1; L2a = iA.L2; L3a = iB.l;
	__m128d z = _mm_setzero_pd();
	L1b = _mm_unpacklo_pd(iC, z);
	L2b = _mm_unpackhi_pd(iC, z);
	L3b = _mm_set_pd(0.0, iD);

	return det;
}

inline double dpmatrix3::det() const {
	// do the explicit formulation (see wikipedia - Invertible Matrix)
	// |A| = a(ei - fh) - b(di - fg) + c(dh - eg)
	// | a b c |
	// | d e f |
	// | g h i |
	__m128d ed = _mm_unpackhi_pd(L2a, L1a); // select hi qwords from both
	__m128d ii = _mm_unpacklo_pd(L3b, L3b); // select lo qword 

	__m128d eidi = _mm_mul_pd(ed, ii);

	__m128d ff = _mm_unpackhi_pd(L3a, L3a); // select hi qword
	__m128d hg = _mm_unpacklo_pd(L2b, L1b); 

	__m128d fhfg = _mm_mul_pd(ff, hg);

	__m128d s = _mm_sub_pd(eidi, fhfg);
	__m128d ab = _mm_unpacklo_pd(L1a, L2a); 
	// negate the b term
	ab = _mm_neg1_pd(ab);
	s = _mm_mul_pd(s, ab);

	// now handle the c term
	__m128d de = _mm_shuffle_pd(ed, ed, 1); // swap the order
	__m128d dheg = _mm_mul_pd(de, hg); 
	// negate the eg term
	dheg = _mm_neg1_pd(dheg);
	// load the c term
	__m128d cc = _mm_unpacklo_pd(L3a, L3a);
	__m128d s2 = _mm_mul_pd(dheg, cc);
	// add cross stuff
	s2 = _mm_add_pd(s2, s);

#ifdef USE_SSE3
	return _mm_hadd_pd(s2, s2).m128d_f64[0];
#else
	s = _mm_shuffle_pd(s2, s2, 3); // get the upper qword
	return _mm_add_sd(s, s2).m128d_f64[0];
#endif
}	

inline double dpmatrix3::maxelem() const {
	// find the largest element
	__m128d m = _mm_max_pd(L1a, L2a);
	m = _mm_max_pd(m, L3a);
	m = _mm_max_pd(m, _mm_unpacklo_pd(L1b, L2b));
	m = _mm_max_sd(m, _mm_shuffle_pd(m, m, 1));
	m = _mm_max_sd(m, L3b);
	return m.m128d_f64[0];
}

inline double dpmatrix3::minelem() const {
	__m128d m = _mm_min_pd(L1a, L2a);
	m = _mm_min_pd(m, L3a);
	m = _mm_min_pd(m, _mm_unpacklo_pd(L1b, L2b));
	m = _mm_min_sd(m, _mm_shuffle_pd(m, m, 1));
	m = _mm_min_sd(m, L3b);
	return m.m128d_f64[0];
}

inline dpmatrix3 dpmatrix3::zero() {
	__m128d z(_mm_setzero_pd());
	return dpmatrix3(z, z, z, z, z, z);
}

inline dpmatrix3 dpmatrix3::identity() {
	__m128d zo = _mm_set_pd(1.0, 0.0); // zero-one
	__m128d oz = _mm_shuffle_pd(zo, zo, 1); // swap the order, one-zero
	__m128d z = _mm_setzero_pd(); // zero
	return dpmatrix3(oz, z, zo, z, z, oz);
}

inline void dpmatrix3::get(double* c) { for (int j = 0; j < 3; j++) for (int i = 0; i < 3; i++) c[j * 3 + i] = m[j * 4 + i]; }

inline dpvector3 dpmatrix3::col(const int j) const { 
	assert(j >= 0 && j < 3);
	switch (j) {
		case 0:
			return dpvector3(L1a, L1b);

		case 1:
			return dpvector3(L2a, L2b);

		case 2:
			return dpvector3(L3a, L3b);

		default:
			return dpvector3();
	}
}

inline dpvector3 dpmatrix3::row(const int i) const {
	assert(i >= 0 && i < 3);
	switch (i) {
		case 0:
			return dpvector3(m11, m12, m13);

		case 1:
			return dpvector3(m21, m22, m23);

		case 2:
			return dpvector3(m31, m32, m33);

		default:
			return dpvector3();
	}
}

inline dpvector3 dpmatrix3::extract_ypr() const {
	double cos_pitch = sqrt(m11*m11 + m12*m12);
	double sin_pitch = -m13;
	double cos_roll = m33/cos_pitch;
	double sin_roll = m23/cos_pitch;
	double cos_yaw = m11/cos_pitch;
	double sin_yaw = m12/cos_pitch;

	return dpvector3(atan2(sin_yaw,cos_yaw), atan2(sin_pitch,cos_pitch), atan2(sin_roll,cos_roll));
}

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif