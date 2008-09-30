#ifndef _DPVECTOR4_H
#define _DPVECTOR4_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

#include <cassert>
#include <cmath>
//#include <intrin.h>
#include "sse.h"

#pragma pack(push,16)

_MM_ALIGN16 class dpmatrix4;

_MM_ALIGN16 class dpvector4 {
private:
	union {
		struct {
			__m128d a, b;
		};
		struct {
			double vx, vy, vz, vw;
		};
		double v[4];
	};

public:

	inline dpvector4(const double x, const double y, const double z, const double w) 
		: a(_mm_set_pd(y, x)), b(_mm_set_pd(w, z)) {}
	inline explicit dpvector4(const double c = 0.0) :	a(_mm_set1_pd(c)), b(_mm_set1_pd(c)) {}
	inline dpvector4(const dpvector4& c) : a(c.a), b(c.b) {}
	inline dpvector4(const __m128d _a, const __m128d _b) : a(_a), b(_b) {}
	inline dpvector4(const double* c) {
		for (int i = 0; i < 4; i++) v[i] = c[i];
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

	inline double& x() { return vx; }
	inline double  x() const { return vx; }
	inline double& y() { return vy; }
	inline double  y() const { return vy; }
	inline double& z() { return vz; }
	inline double  z() const { return vz; }
	inline double& w() { return vw; }
	inline double  w() const { return vw; }

	inline bool iszero() { return vx == 0 && vy == 0 && vz == 0 && vw == 0; }

	inline double operator [] (const int i) const { assert(i >= 0 && i < 4); return v[i]; }
	inline double& operator[] (const int i) { assert(i >= 0 && i < 4); return v[i]; }

	inline void get(double* c) { for (int i = 0; i < 4; i++) c[i] = v[i]; }

	inline dpvector4 operator + (const dpvector4& c) const { return dpvector4(_mm_add_pd(a, c.a), _mm_add_pd(b, c.b)); }
	inline dpvector4 operator - (const dpvector4& c) const { return dpvector4(_mm_sub_pd(a, c.a), _mm_sub_pd(b, c.b)); }
	inline dpvector4 operator * (const dpvector4& c) const { return dpvector4(_mm_mul_pd(a, c.a), _mm_mul_pd(b, c.b)); }
	inline dpvector4 operator / (const dpvector4& c) const { return dpvector4(_mm_div_pd(a, c.a), _mm_div_pd(b, c.b)); }

	inline dpvector4 operator + (const double c) const { 
		__m128d t = _mm_set1_pd(c);
		return dpvector4(_mm_add_pd(a, t), _mm_add_pd(b, t));
	}
	inline dpvector4 operator - (const double c) const {
		__m128d t = _mm_set1_pd(c);
		return dpvector4(_mm_sub_pd(a, t), _mm_sub_pd(b, t));
	}
	inline dpvector4 operator * (const double c) const {
		__m128d t = _mm_set1_pd(c);
		return dpvector4(_mm_mul_pd(a, t), _mm_mul_pd(b, t));
	}
	inline dpvector4 operator / (const double c) const {
		__m128d t = _mm_set1_pd(c);
		return dpvector4(_mm_div_pd(a, t), _mm_div_pd(b, t));
	}

	inline dpvector4 operator - () const {
		return dpvector4(_mm_neg_pd(a), _mm_neg_pd(b));
	}

	inline dpvector4& operator += (const dpvector4& c) { 
		a = _mm_add_pd(a, c.a); b = _mm_add_pd(b, c.b);
		return *(this);
	}
	inline dpvector4& operator -= (const dpvector4& c) {
		a = _mm_sub_pd(a, c.a); b = _mm_sub_pd(b, c.b);
		return *(this);
	}
	inline dpvector4& operator *= (const dpvector4& c) {
		a = _mm_mul_pd(a, c.a); b = _mm_mul_pd(b, c.b);
		return *(this);
	}
	inline dpvector4& operator /= (const dpvector4& c) {
		a = _mm_div_pd(a, c.a); b = _mm_div_pd(b, c.b);
		return *(this);
	}

	inline dpvector4& operator += (const double c) {
		__m128d t = _mm_set1_pd(c);
		a = _mm_add_pd(a, t); b = _mm_add_pd(b, t);
		return *this;
	}
	inline dpvector4& operator -= (const double c) {
		__m128d t = _mm_set1_pd(c);
		a = _mm_sub_pd(a, t); b = _mm_sub_pd(b, t);
		return *this;
	}
	inline dpvector4& operator *= (const double c) {
		__m128d t = _mm_set1_pd(c);
		a = _mm_mul_pd(a, t); b = _mm_mul_pd(b, t);
		return *this;
	}
	inline dpvector4& operator /= (const double c) {
		__m128d t = _mm_set1_pd(c);
		a = _mm_div_pd(a, t); b = _mm_div_pd(b, t);
		return *this;
	}

	inline double dot(const dpvector4& c) const {
		__m128d t0 = _mm_mul_pd(a, c.a);
		__m128d t1 = _mm_mul_pd(b, c.b);

		t0 = _mm_add_pd(t0, t1);
#ifdef USE_SSE3
		t0 = _mm_hadd_pd(t0, t1);
		return t0.m128d_f64[0];
#else
		t1 = _mm_shuffle_pd(t0, t0, 1); // shuffle in the high qword from t0
		return _mm_add_sd(t0, t1).m128d_f64[0];
#endif
	}

	inline double magsq() const {
		return dot(*this);
	}

	inline double mag() const {
		return sqrt(magsq());
	}

	inline dpvector4 normalize(double l = 1.0) const {
		l /= mag();
		return operator * (l);
	}

	//added by amn32 for homogenous coordinate crap
	inline dpvector4 divomega() const
	{
		return operator / (vw);
	}
	inline double maxelem() const {
		__m128d m = _mm_max_pd(a, b);
		m = _mm_max_sd(m, _mm_shuffle_pd(m, m, 1));
		return m.m128d_f64[0];
	}

	inline double minelem() const {
		__m128d m = _mm_min_pd(a, b);
		m = _mm_min_sd(m, _mm_shuffle_pd(m, m, 1));
		return m.m128d_f64[0];
	}

	dpmatrix4 outerproduct(const dpvector4& c) const;
};

#pragma pack(pop)

#include "dpmatrix4.h"

inline dpmatrix4 dpvector4::outerproduct(const dpvector4& c) const {
	dpmatrix4 r;
	r.L1a = _mm_mul_pd(a, _mm_set1_pd(c.vx)); r.L1b = _mm_mul_pd(b, _mm_set1_pd(c.vx));
	r.L2a = _mm_mul_pd(a, _mm_set1_pd(c.vy)); r.L2b = _mm_mul_pd(b, _mm_set1_pd(c.vy));
	r.L3a = _mm_mul_pd(a, _mm_set1_pd(c.vz)); r.L3b = _mm_mul_pd(b, _mm_set1_pd(c.vz));
	r.L4a = _mm_mul_pd(a, _mm_set1_pd(c.vw)); r.L4b = _mm_mul_pd(b, _mm_set1_pd(c.vw));
	return r;
	/*return dpmatrix4(
		_mm_mul_pd(a, _mm_set1_pd(c.vx)), _mm_mul_pd(b, _mm_set1_pd(c.vx)), 
		_mm_mul_pd(a, _mm_set1_pd(c.vy)), _mm_mul_pd(b, _mm_set1_pd(c.vy)), 
		_mm_mul_pd(a, _mm_set1_pd(c.vz)), _mm_mul_pd(b, _mm_set1_pd(c.vz)),
		_mm_mul_pd(a, _mm_set1_pd(c.vw)), _mm_mul_pd(b, _mm_set1_pd(c.vw)));*/
}

inline dpvector4 operator * (const double c, const dpvector4& v) {
	return v * c;
}

inline dpvector4 operator / (const double c, const dpvector4& v) {
	return v / c;
}

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif