#ifndef _DPVECTOR2_H
#define _DPVECTOR2_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

//#include <intrin.h>
#include <cmath>
#include "sse.h"
#include <cassert>

class dpmatrix2;

class _MM_ALIGN16 dpvector2 {
private:
	union {
		__m128d l;
		struct {
			double vx, vy;
		};
		double v[2];
	};

public:
	inline dpvector2(const double x = 0.0, const double y = 0.0) : l(_mm_set_pd(y, x)) {}
	inline dpvector2(const __m128d c) : l(c) {}
	inline dpvector2(const double* c) : l(_mm_loadu_pd(c)) {};

	inline double x() const { return vx; }
	inline double& x() { return vx; }
	inline double y() const { return vy; }
	inline double& y() { return vy; }

	inline bool iszero() { return vx == 0.0 && vy == 0.0; }

	inline double operator [] (const int i) const { assert(i >= 0 && i < 2); return v[i]; }
	inline double& operator [] (const int i) { assert(i >= 0 && i < 2); return v[i]; }

	inline void get(double* c) { /*if (((void*)c & 0xf) == 0) _mm_store_pd(c, l) else*/ _mm_storeu_pd(c, l); }
	
	inline dpvector2 operator + (const dpvector2& b) const { return dpvector2(_mm_add_pd(l, b.l)); }
	inline dpvector2 operator - (const dpvector2& b) const { return dpvector2(_mm_sub_pd(l, b.l)); }
	inline dpvector2 operator * (const dpvector2& b) const { return dpvector2(_mm_mul_pd(l, b.l)); }
	inline dpvector2 operator / (const dpvector2& b) const { return dpvector2(_mm_div_pd(l, b.l)); }
	
	inline dpvector2 operator * (const double b) const { return dpvector2(_mm_mul_pd(l, _mm_set1_pd(b))); }
	inline dpvector2 operator / (const double b) const { return dpvector2(_mm_div_pd(l, _mm_set1_pd(b))); }

	inline dpvector2& operator += (const dpvector2& b) { l = _mm_add_pd(l, b.l); return *this; }
	inline dpvector2& operator -= (const dpvector2& b) { l = _mm_sub_pd(l, b.l); return *this; }
	inline dpvector2& operator *= (const dpvector2& b) { l = _mm_mul_pd(l, b.l); return *this; }
	inline dpvector2& operator /= (const dpvector2& b) { l = _mm_div_pd(l, b.l); return *this; }

	inline dpvector2& operator *= (const double b) { l = _mm_mul_pd(l, _mm_set1_pd(b)); return *this; }
	inline dpvector2& operator /= (const double b) { l = _mm_div_pd(l, _mm_set1_pd(b)); return *this; }

	inline dpvector2 operator - () const { return dpvector2(_mm_neg_pd(l)); }

	inline operator __m128d () { return l; }

	inline bool operator == (const dpvector2& b) { return vx == b.vx && vy == b.vy; }
	inline bool operator != (const dpvector2& b) { return vx != b.vx || vy != b.vy; }

	inline double dot(const dpvector2& b) const { 
		__m128d p = _mm_mul_pd(l, b.l); 
#ifdef USE_SSE3
		return _mm_hadd_pd(p, p).m128d_f64[0];
#else
		return p.m128d_f64[0] + p.m128d_f64[1];
#endif;
	}

	inline double cross(const dpvector2& b) const {
		return vx*b.vy - vy*b.vx;
	}

	inline double magsq() const { return dot(*this); }
	inline double mag() const { return sqrt(magsq()); }
	inline dpvector2 norm(double k = 1.0) const { k /= mag(); return dpvector2(_mm_mul_pd(l, _mm_set1_pd(k))); }

	dpmatrix2 outerproduct(const dpvector2& b) const;

	inline double maxelem() const {
		_mm_max_sd(l, _mm_shuffle_pd(l, l, 1)).m128d_f64[0];
	}

	inline double minelem() const {
		_mm_min_sd(l, _mm_shuffle_pd(l, l, 1)).m128d_f64[0];
	}

	// rotates the vector by 90 degrees
	inline dpvector2 rot90() const { return dpvector2(-vy,vx); }
	// rotates the vector by -90 degrees
	inline dpvector2 rotM90() const { return dpvector2(vy,-vx); }

	friend class dpmatrix3;
	friend class dpmatrix2;
};

#include "dpmatrix2.h"

inline dpmatrix2 dpvector2::outerproduct(const dpvector2& b) const {
	return dpmatrix2(_mm_mul_pd(l, _mm_set1_pd(b.vx)), _mm_mul_pd(l, _mm_set1_pd(b.vy)));
}

inline dpvector2 operator * (const double c, const dpvector2& v) {
	return v * c;
}

inline dpvector2 operator / (const double c, const dpvector2& v) {
	return v / c;
}

inline double cross(const dpvector2& a, const dpvector2& b) {
	return a.cross(b);
}

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif