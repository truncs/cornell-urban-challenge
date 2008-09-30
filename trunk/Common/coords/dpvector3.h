#ifndef _DPVECTOR3_H
#define _DPVECTOR3_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

#include <cassert>
#include <cmath>
#include "sse.h"

using namespace std;

class dpmatrix3;

__declspec(align(16)) class dpvector3 {
	friend class matrix;
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
	inline dpvector3(double x, double y, double z) : a(_mm_set_pd(y, x)), b(_mm_set_pd(0.0, z)) {}
	inline dpvector3(const dpvector3& c) : a(c.a), b(c.b) {};
	inline dpvector3(const double* c) {	for (int i = 0; i < 3; i++) v[i] = c[i]; vw = 0.0; }
	inline dpvector3(__m128d v0, __m128d v1) : a(v0), b(v1) {}
	inline dpvector3(const double c = 0.0) : a(_mm_set1_pd(c)), b(_mm_set_pd(0.0, c)) {}

	friend class dpmatrix3;

	inline double& x() { return vx; };
	inline double  x() const {	return vx; };
	inline double& y() { return vy; };
	inline double  y() const { return vy; };
	inline double& z() { return vz; };
	inline double  z() const { return vz; };

	inline bool iszero() { return vx == 0 && vy == 0 && vz == 0; }
	
	inline double operator[] (const int i) const { assert(i < 3 && i >= 0); return v[i]; }
	inline double& operator[] (const int i) { assert(i < 3 && i >= 0); return v[i]; }

	inline void get(double* c) const { for (int i = 0; i < 3; i++) c[i] = v[i]; }

	inline dpvector3 operator + (const dpvector3& c) const { return dpvector3(_mm_add_pd(a, c.a), _mm_add_sd(b, c.b)); }
	inline dpvector3 operator * (const dpvector3& c) const { return dpvector3(_mm_mul_pd(a, c.a), _mm_mul_sd(b, c.b)); }
  inline dpvector3 operator / (const dpvector3& c) const { return dpvector3(_mm_div_pd(a, c.a), _mm_div_sd(b, c.b)); }
	inline dpvector3 operator - (const dpvector3& c) const { return dpvector3(_mm_sub_pd(a, c.a), _mm_sub_sd(b, c.b)); }
  
  inline dpvector3 operator + (const double c) const {
		__m128d t0 = _mm_set1_pd(c);
		return dpvector3(_mm_add_pd(a, t0), _mm_add_sd(b, t0));
	}

  inline dpvector3 operator * (const double c) const {
		__m128d t0 = _mm_set1_pd(c);
		return dpvector3(_mm_mul_pd(a, t0), _mm_mul_sd(b, t0));
	}

  inline dpvector3 operator / (const double c) const {
		__m128d t0 = _mm_set1_pd(c);
		return dpvector3(_mm_div_pd(a, t0), _mm_div_sd(b, t0));
	}

  inline dpvector3 operator - (const double c) const {
		__m128d t0 = _mm_set1_pd(c);
		return dpvector3(_mm_sub_pd(a, t0), _mm_sub_sd(b, t0));
	}

  inline bool operator == (const dpvector3& c) const { return (vx == c.vx && vy == c.vy && vz == c.vz); }
  inline bool operator != (const dpvector3& c) const { return (vx != c.vx || vy != c.vy || vz != c.vz); }

  inline dpvector3 operator /= (const dpvector3& c) { 
		a = _mm_div_pd(a, c.a); b = _mm_div_sd(b, c.b);
		return *this;
	}
  inline dpvector3 operator *= (const dpvector3& c) { 
		a = _mm_mul_pd(a, c.a); b = _mm_mul_sd(b, c.b);
		return *this;
	}
  inline dpvector3 operator += (const dpvector3& c) { 
		a = _mm_add_pd(a, c.a); b = _mm_add_sd(b, c.b); 
		return *this;
	}
  inline dpvector3 operator -= (const dpvector3& c) { 
		a = _mm_sub_pd(a, c.a); b = _mm_sub_sd(b, c.b); 
		return *this;
	}

	inline dpvector3 operator += (const double c) { 
		__m128d t = _mm_set1_pd(c);
		a = _mm_add_pd(a, t); b = _mm_add_sd(b, t);
		return *this; 
	}
	inline dpvector3 operator -= (const double c) { 
		__m128d t = _mm_set1_pd(c);
		a = _mm_sub_pd(a, t); b = _mm_sub_sd(b, t);
		return *this; 
	}
	inline dpvector3 operator /= (const double c) { 
		__m128d t = _mm_set1_pd(c);
		a = _mm_div_pd(a, t); b = _mm_div_sd(b, t);
		return *this; 
	}
	inline dpvector3 operator *= (const double c) { 
		__m128d t = _mm_set1_pd(c);
		a = _mm_mul_pd(a, t); b = _mm_mul_sd(b, t);
		return *this; 
	}
	
	inline dpvector3 operator - () const { return dpvector3(_mm_neg_pd(a), _mm_neg_pd(b)); }

	inline dpvector3 cross(const dpvector3& c) const {
		__m128d tv0 = a, tv1 = b;
		__m128d cv0 = c.a, cv1 = c.b;

		__m128d t0, c0;
		__m128d t1, c1;
		// t0 needs to contain v.y, v.z
		// c0 needs to contain c.v.z, c.v.y
		t0 = _mm_shuffle_pd(tv0, tv1, 1); // select upper 64-bits of tv0, lower 64-bits of tv1
		c0 = _mm_shuffle_pd(cv1, cv0, 2); // select lower 64-bits of cv1, upper 64-bits of cv0

		// t1 needs to contain v.z, v.x
		// c1 needs to contain c.v.x, c.v.z
		t1 = _mm_shuffle_pd(tv1, tv0, 0); // select lower 64-bits of tv1, lower 64-bits of tv0
		c1 = _mm_shuffle_pd(cv0, cv1, 0); // select lower 64-bits of cv0, lower 64-bits of cv1

		t0 = _mm_mul_pd(t0, c0); // multiply v.y * c.v.z, v.z * c.v.y
		t1 = _mm_mul_pd(t1, c1); // multiple v.z * c.v.x, v.x * c.v.z

#ifdef USE_SSE3
		__m128d res0 = _mm_hsub_pd(t0, t1);
#else
		__m128d res0 = t1;
		t1 = _mm_shuffle_pd(t0, res0, 3);
		t0 = _mm_shuffle_pd(t0, res0, 0);
		res0 = _mm_sub_pd(t0, t1);
		/*res0.m128d_f64[0] = t0.m128d_f64[0] - t0.m128d_f64[1];
		res0.m128d_f64[1] = t1.m128d_f64[0] - t1.m128d_f64[1];*/
#endif

		// compute z term
		// t0 needs to contain v.x, v.y
		// c0 needs to contain c.v.y, c.v.x
		t0 = tv0;
		c0 = _mm_shuffle_pd(cv0, cv0, 1); // select upper 64-bits of cv0 into lower 64-bits of c0, lower 64-bits of cv0 into upper 64-bits of c0

		t0 = _mm_mul_pd(t0, c0); // multiply v.x * c.v.y, v.y * c.v.x

#ifdef USE_SSE3
		t1 = _mm_setzero_pd();
		__m128d res1 = _mm_hsub_pd(t0, t1); // w will contain 0
#else
		// put the lower qword into res1, 0 in the upper qword
		__m128d res1 = _mm_shuffle_pd(t0, _mm_setzero_pd(), 0);
		t0 = _mm_shuffle_pd(t0, t0, 1); // swap the order
		res1 = _mm_sub_sd(res1, t0); // scalar subtract
		/*res1.m128d_f64[0] = t0.m128d_f64[0] - t0.m128d_f64[1];
		res1.m128d_f64[1] = 0.0;*/
#endif

		return dpvector3(res0, res1);
	}

	inline double dot(const dpvector3& c) const {
		assert(vw == 0.0 && c.vw == 0.0);
		__m128d t0 = _mm_mul_pd(a, c.a);
		__m128d t1 = _mm_mul_pd(b, c.b);

#ifdef USE_SSE3
		t0 = _mm_hadd_pd(t0, t1);
		t0 = _mm_hadd_pd(t0, t1);
		return t0.m128d_f64[0];
#else
		t0 = _mm_add_pd(t0, t1);
		t1 = _mm_shuffle_pd(t0, t0, 1); // swap the order
		return _mm_add_sd(t0, t1).m128d_f64[0];
#endif
	}

	inline double magsq() const { return dot(*this); }

	inline double mag() const { return sqrt(magsq()); }

	inline dpvector3 normalize(double l = 1.0) const {
		l /= mag();
		return operator * (l);
	}

	inline dpmatrix3 outerproduct(const dpvector3& v) const; 

	inline dpvector3 squared() const {
		__m128d t0 = _mm_mul_pd(a, a);
		__m128d t1 = _mm_mul_pd(b, b);
		return dpvector3(t0, t1);
	}

	inline double maxelem() const {
		__m128d m = _mm_max_sd(a, b);
		m = _mm_max_sd(m, _mm_shuffle_pd(m, m, 1));
		return m.m128d_f64[0];
	}

	inline double minelem() const {
		__m128d m = _mm_min_sd(a, b);
		m = _mm_min_sd(m, _mm_shuffle_pd(m, m, 1));
		return m.m128d_f64[0];
	}

	friend dpvector3 abs(const dpvector3&);
	friend dpvector3 sqrt(const dpvector3&);
};

#include "dpmatrix3.h"

inline dpmatrix3 dpvector3::outerproduct(const dpvector3& v) const {
	dpmatrix3 r;
	r.L1a = _mm_mul_pd(a, _mm_set1_pd(v.vx)); r.L1b = _mm_mul_sd(b, _mm_set1_pd(v.vx));
	r.L2a = _mm_mul_pd(a, _mm_set1_pd(v.vy)); r.L2b = _mm_mul_sd(b, _mm_set1_pd(v.vy));
	r.L3a = _mm_mul_pd(a, _mm_set1_pd(v.vz)); r.L3b = _mm_mul_sd(b, _mm_set1_pd(v.vz));
	return r;
}

inline dpvector3 operator * (const double c, const dpvector3& v) {
	return v * c;
}

inline dpvector3 operator / (const double c, const dpvector3& v) {
	return v / c;
}

inline dpvector3 cross(const dpvector3& l, const dpvector3& r) {
	return l.cross(r);
}

inline dpvector3 abs(const dpvector3& l) {
	return dpvector3(abs(l.vx), abs(l.vy), abs(l.vz));
}

inline double norm(const dpvector3& l) {
	return l.mag();
}

inline dpvector3 sqrt(const dpvector3& l) {
	return dpvector3(_mm_sqrt_pd(l.a), _mm_sqrt_pd(l.b));
}

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif