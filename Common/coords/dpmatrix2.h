#ifndef _DPMATRIX2_H
#define _DPMATRIX2_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

#include "sse.h"
#include <cassert>

class dpvector2;

class _MM_ALIGN16 dpmatrix2 {
private:
	union {
		struct {
			__m128d L1, L2; // column-major order, so each is a column
		};
		struct {
			double m11, m21,
			m12, m22;
		};
		double m[4]; // column-major order
	};

public:
	inline dpmatrix2() {}
	inline dpmatrix2(const __m128d c1, const __m128d c2) : L1(c1), L2(c2) {}
	inline dpmatrix2(const double m11, const double m12,
		const double m21, const double m22) : 
		L1(_mm_set_pd(m21, m11)), 
		L2(_mm_set_pd(m22, m12)) {}
	inline dpmatrix2(const double* c) {
		for (int j = 0; j < 2; j++) for (int i = 0; i < 2; i++) m[j * 2 + i] = c[j * 2 + i];
	}

	inline dpmatrix2& operator = (const dpmatrix2& c) {
		L1 = c.L1; L2 = c.L2;
		return *this;
	}

	// element access
	inline double operator () (const int i, const int j) const { 
		assert(i >= 0 && i < 2 && j >= 0 && j < 2); 
		return m[j * 2 + i]; 
	}
	inline double& operator () (const int i, const int j) { 
		assert(i >= 0 && i < 2 && j >= 0 && j < 2); 
		return m[j * 2 + i]; 
	}

	inline dpvector2 col(const int j) const;
	inline dpvector2 row(const int i) const;

	friend dpmatrix2 operator * (const dpmatrix2&, const dpmatrix2&);
	friend dpmatrix2 operator + (const dpmatrix2&, const dpmatrix2&);
	friend dpmatrix2 operator - (const dpmatrix2&, const dpmatrix2&);
	friend dpmatrix2 operator - (const dpmatrix2&);
	friend dpmatrix2 operator * (const dpmatrix2&, const double);
	friend dpmatrix2 operator * (const double, const dpmatrix2&);
	friend dpvector2 operator * (const dpmatrix2&, const dpvector2&);
	friend dpvector2 operator * (const dpvector2&, const dpmatrix2&);

	dpmatrix2& operator *= (const dpmatrix2&);
	dpmatrix2& operator += (const dpmatrix2&);
	dpmatrix2& operator -= (const dpmatrix2&);
	dpmatrix2& operator *= (const double);

	dpmatrix2 transpose() const;
	//void transpose();

	double inv();
	double det() const;

	double trace() const { return m11 + m22; }

	static dpmatrix2 zero() { __m128d zero = _mm_setzero_pd(); return dpmatrix2(zero, zero); }
	static dpmatrix2 identity() {
		__m128d onezero = _mm_set_pd(1.0, 0.0);
		__m128d zeroone = _mm_shuffle_pd(onezero, onezero, 1);
		return dpmatrix2(onezero, zeroone);
	}

	inline void get(double* mat) {
		for (int j = 0; j < 2; j++) for (int i = 0; i < 2; i++) mat[j * 2 + i] = m[j * 2 + i];
	}
};

#include "dpvector2.h"

inline dpmatrix2 operator * (const dpmatrix2& A, const dpmatrix2& B) {
	__m128d A1 = A.L1, A2 = A.L2;

	__m128d r1, r2;
	r1 = _mm_mul_pd(A1, _mm_set1_pd(B.m11));
	r2 = _mm_mul_pd(A1, _mm_set1_pd(B.m12));
	r1 = _mm_add_pd(r1, _mm_mul_pd(A2, _mm_set1_pd(B.m21)));
	r2 = _mm_add_pd(r2, _mm_mul_pd(A2, _mm_set1_pd(B.m22)));

	return dpmatrix2(r1, r2);
}

inline dpmatrix2 operator + (const dpmatrix2& A, const dpmatrix2& B) {
	return dpmatrix2(_mm_add_pd(A.L1, B.L1), _mm_add_pd(A.L2, B.L2));
}

inline dpmatrix2 operator - (const dpmatrix2& A, const dpmatrix2& B) {
	return dpmatrix2(_mm_sub_pd(A.L1, B.L1), _mm_sub_pd(A.L2, B.L2));
}

inline dpmatrix2 operator - (const dpmatrix2& A) {
	return dpmatrix2(_mm_neg_pd(A.L1), _mm_neg_pd(A.L2));
}

inline dpmatrix2 operator * (const dpmatrix2& A, const double c) {
	__m128d s = _mm_set1_pd(c);
	return dpmatrix2(_mm_mul_pd(A.L1, s), _mm_mul_pd(A.L2, s));
}

inline dpmatrix2 operator * (const double c, const dpmatrix2& A) {
	return A * c;
}

inline dpvector2 operator * (const dpmatrix2& A, const dpvector2& v) {
	// use the sum of column vectors time scalar form
	return dpvector2(_mm_add_pd(_mm_mul_pd(A.L1, _mm_set1_pd(v.vx)), _mm_mul_pd(A.L2, _mm_set1_pd(v.vy))));
}

inline dpvector2 operator * (const dpvector2& v, const dpmatrix2& A) {
	// use the dot product of vector times column form
#ifdef USE_SSE3
	return dpvector2(_mm_hadd_pd(_mm_mul_pd(A.L1, v.l), _mm_mul_pd(A.L2, v.l)));
#else
	return dpvector2(_mm_add_pd(_mm_mul_pd(_mm_unpacklo_pd(A.L1, A.L2), _mm_set1_pd(v.vx)), _mm_mul_pd(_mm_unpackhi_pd(A.L1, A.L2), _mm_set1_pd(v.vy))));
#endif
}

inline dpmatrix2& dpmatrix2::operator *=(const dpmatrix2 &A) {
	(*this) = (*this) * A;
	return *this;
}

inline dpmatrix2& dpmatrix2::operator *=(const double c) {
	__m128d s = _mm_set1_pd(c);
	L1 = _mm_mul_pd(L1, s); L2 = _mm_mul_pd(L2, s);
	return *this;
}

inline dpmatrix2& dpmatrix2::operator +=(const dpmatrix2 &A) {
	L1 = _mm_add_pd(L1, A.L1); L2 = _mm_add_pd(L2, A.L2);
	return *this;
}

inline dpmatrix2& dpmatrix2::operator -=(const dpmatrix2 &A) {
	L1 = _mm_sub_pd(L1, A.L1); L2 = _mm_sub_pd(L2, A.L2);
	return *this;
}

inline dpmatrix2 dpmatrix2::transpose() const {
	return dpmatrix2(_mm_unpacklo_pd(L1, L2), _mm_unpackhi_pd(L1, L2));
}

/*inline void dpmatrix2::transpose() {
	__m128d t = L1;
	L1 = _mm_unpacklo_pd(L1, L2);
	L2 = _mm_unpackhi_pd(t, L2);
}*/

inline double dpmatrix2::inv() {
	double d = det();
	if (d == 0.0) return 0.0;
	
	__m128d t = L1;
	L1 = _mm_unpackhi_pd(L2, t);
	L2 = _mm_unpacklo_pd(L2, t);
	
	__m128d scale = _mm_set_pd(-1.0 / d, 1.0 / d);
	L1 = _mm_mul_pd(L1, scale);
	scale = _mm_shuffle_pd(scale, scale, 1);
	L2 = _mm_mul_pd(L2, scale);

	return d;
}

inline double dpmatrix2::det() const {
	/*_MM_ALIGN16 double r;
	_asm {
		movapd	xmm0, [ecx]dpmatrix2.L2
		shufpd  xmm0, xmm0, 1
		movapd	xmm1, [ecx]dpmatrix2.L1
		mulpd   xmm0, xmm1
#ifdef USE_SSE3
		hsubpd  xmm0, xmm0
#else
		movapd  xmm1, xmm0
		shufpd  xmm1, xmm1, 1
		subsd   xmm0, xmm1
		movq    r, xmm0
#endif
	}
	return r;*/
	// result is in xmm0
	__m128d t = _mm_shuffle_pd(L2, L2, 1); // reverse the order of the pair
	t = _mm_mul_pd(t, L1);
#ifdef USE_SSE3
	t = _mm_hsub_pd(t, t);
	return t.m128d_f64[0];
#else
	return t.m128d_f64[0] - t.m128d_f64[1];
#endif
}

inline dpvector2 dpmatrix2::col(const int j) const {
	assert(j >= 0 && j < 2);

	switch (j) {
		case 0:
			return dpvector2(L1);

		case 1:
			return dpvector2(L2);
	}

	return dpvector2();
}

inline dpvector2 dpmatrix2::row(const int i) const {
	assert(i >= 0 && i < 2);

	switch (i) {
		case 0:
			return dpvector2(m11, m12);

		case 1:
			return dpvector2(m21, m22);
	}

	return dpvector2();
}

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif