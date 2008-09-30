#ifndef _MATRIX_H
#define _MATRIX_H

#include <cassert>
#include "acml.h"

#include "../coords/dpvector3.h"
#include "../coords/dpmatrix3.h"

class matrix_exception {
private:
	size_t len;
	char* msg;

public:
	matrix_exception();
	matrix_exception(const char* msg);
	matrix_exception(const matrix_exception&);
	
	virtual matrix_exception& operator = (const matrix_exception&);

	virtual ~matrix_exception();

	virtual const char* what() const;
};

template<int m, int n>
class dpmatrix {
protected:
	double d[n][m];	// store in column major order

public:
	dpmatrix() {};
	dpmatrix(const double* c) {
		for (int j = 0; j < n; j++) { // column index
			for (int i = 0; i < m; i++) { // row index
				d[j][i] = c[j * m + i];
			}
		}
	}

	dpmatrix(const dpmatrix<m, n>& c) {
		for (int j = 0; j < n; j++) {
			for (int i = 0; i < m; i++) {
				d[j][i] = c.d[j][i];
			}
		}
	}

	inline dpmatrix<m, n>& operator = (const dpmatrix<m, n>& c) {
		for (int j = 0; j < n; j++) for (int i = 0; i < m; i++) d[j][i] = c(i, j);
		return *this;
	}

	inline operator double * () { return (double*)d; }

	inline double operator () (const int i, const int j) const { assert(i >= 0 && i < m && j >= 0 && j < n); return d[j][i]; }
	inline double& operator() (const int i, const int j) { assert(i >= 0 && i < m && j >= 0 && j < n); return d[j][i]; }

	virtual dpmatrix<n, m> trans() const {
		dpmatrix<n, m> r;
		for (int j = 0; j < n; j++) {
			for (int i = 0; i < m; i++) {
				r(j, i) = d[j][i];
			}
		}
		return r;
	}

	template<int k, int l>
	void set_submatrix(const int i_start, const int j_start, const dpmatrix<k, l>& mat) {
		assert(i_start >= 0 && i_start + k <= m && j_start >= 0 && j_start + l <= n);
		for (int j = j_start; j < j_start + l; j++) {
			for (int i = i_start; i < i_start + k; i++) {
				operator()(i, j) = mat(i - i_start, j - j_start);
			}
		}
	}

	template<int k, int l>
	void get_submatrix(const int i_start, const int j_start, dpmatrix<k, l>& dest) {
		assert(i_start >= 0 && i_start + k <= m && j_start >= 0 && j_start + l <= n);
		for (int j = j_start; j < j_start + l; j++) {
			for (int i = i_start; i < i_start + k; i++) {
				dest(i - i_start, j - j_start) = operator()(i, j);
			}
		}
	}

	template<int k, int l>
	dpmatrix<k, l> get_submatrix(const int i_start, const int j_start) const {
		dpmatrix<k, l> r;
		get_submatrix(i_start, j_start, r);
		return r;
	}

	inline int lda() const { return m; }
};

template<int m, bool upper>
class tri_dpmatrix : public dpmatrix<m, m> {
public:
	tri_dpmatrix() {}
	tri_dpmatrix(const double* c) : dpmatrix<m, m>(c) {}
	tri_dpmatrix(const dpmatrix<m, m>& c) : dpmatrix<m, m>(c) {}
	tri_dpmatrix(const tri_dpmatrix<m, upper> &c) : dpmatrix<m, m>((double*)c.d) {}

	virtual dpmatrix<m, m> trans() const {
		return tri_dpmatrix<m, !upper>(dpmatrix<m, m>::trans());
	}
};

template<>
class dpmatrix<3, 1> {
private:
	dpvector3 d;

public:
	inline dpmatrix() {}
	inline dpmatrix(const double* c) : d(c) {}
	inline dpmatrix(const dpmatrix<3, 1>& c) : d(c.d) {}
	inline dpmatrix(const dpvector3 &c) : d(c) {}

	inline dpmatrix<3, 1>& operator = (const dpmatrix<3, 1>& c) {
		d = c.d;
		return *this;
	}

	inline operator double* () { return (double*)&d; }

	inline double operator () (const int i, const int j) const { return d[i]; }
	inline double& operator() (const int i, const int j) { return d[i]; }

	virtual dpmatrix<1, 3> trans() const {	return dpmatrix<1, 3>(d); }

	inline int lda() const { return 4; }
};

template<>
class dpmatrix<1, 3> {
private:
	dpvector3 d;

public:
	inline dpmatrix() {}
	inline dpmatrix(const double* c) : d(c) {}
	inline dpmatrix(const dpmatrix<1, 3> &c) : d(c.d) {}
	inline dpmatrix(const dpvector3 &c) : d(c) {}

	inline dpmatrix<1, 3>& operator = (const dpmatrix<1, 3>& c) {
		d = c.d;
		return *this;
	}

	inline operator double* () { return (double*)&d; }

	inline double operator () (const int i, const int j) const { return d[i]; }
	inline double& operator() (const int i, const int j) { return d[i]; }

	virtual dpmatrix<3, 1> trans() const { return dpmatrix<3, 1>(d); }

	inline int lda() const { return 4; }
};

template<>
class dpmatrix<3, 3> {
private:
	dpmatrix3 d;

public:
	inline dpmatrix() {}
	inline dpmatrix(const double* c) : d(c) {}
	inline dpmatrix(const dpmatrix<3, 3> &c) : d(c.d) {}
	inline dpmatrix(const dpmatrix3 &c) : d(c) {}

	inline dpmatrix<3, 3>& operator = (const dpmatrix<3, 3>& c) {
		d = c.d;
		return *this;
	}

	inline operator double* () { return (double*)&d; }
	
	inline double operator () (const int i, const int j) const { return d(i, j); }
	inline double& operator() (const int i, const int j) { return d(i, j); }

	virtual dpmatrix<3, 3> trans() const {
		return dpmatrix<3, 3>(d.transpose());
	}

	inline int lda() const { return 4; }

	template<int m, int k, int n> friend dpmatrix<m, n> operator * (const dpmatrix<m, k>&, const dpmatrix<k, n>&);
};

// utility operator for matrix multiplication
// this doesn't allow most the flexibility that you get with the BLAS multiplication, but it's decent
template <int m, int k, int n>
inline dpmatrix<m, n> operator * (const dpmatrix<m, k> &A, const dpmatrix<k, n> &B) {
	dpmatrix<m, n> C;
	dgemm('N', 'N', m, n, k, 1.0, ((dpmatrix<m,k>&)A), A.lda(), (dpmatrix<m,k>&)B, B.lda(), 0.0, C, C.lda());
	return C;
}

template<>
inline dpmatrix<3, 3> operator * (const dpmatrix<3, 3> &A, const dpmatrix<3, 3> &B) {
	return dpmatrix<3, 3>(A.d * B.d);
}

template<int m, bool upper>
inline tri_dpmatrix<m, upper> operator * (const tri_dpmatrix<m, upper> &A, const tri_dpmatrix<m, upper> &B) {
	tri_dpmatrix<m, upper> temp(B);
	dtrmm('L', upper ? 'U' : 'L', 'N', 'N', m, m, 1.0, (tri_dpmatrix<m, upper>&)A, A.lda(), temp, temp.lda());
	return temp;
}

template<int m, bool upper>
inline void mat_mult(const tri_dpmatrix<m, upper> &A, tri_dpmatrix<m, upper> &B, const double ab_coeff = 1.0) {
	dtrmm('L', upper ? 'U' : 'L', 'N', 'N', m, m, ab_coeff, (tri_dpmatrix<m, upper>&)A, A.lda(), B, B.lda());
}

// TODO: could specialize multiplication for vector3, matrix3

template<int m, int k>
inline dpmatrix<m, k> operator - (const dpmatrix<m, k>& A, const dpmatrix<m, k>& B) {
	dpmatrix<m, k> r;
	for (int j = 0; j < k; j++) for (int i = 0; i < m; i++) r(i, j) = A(i, j) - B(i, j);
	return r;
}

template<int m, int k>
inline dpmatrix<m, k> operator + (const dpmatrix<m, k>& A, const dpmatrix<m, k>& B) {
	dpmatrix<m, k> r;
	for (int j = 0; j < k; j++) for (int i = 0; i < m; i++) r(i, j) = A(i, j) + B(i, j);
	return r;
}

template<int m, int k>
inline dpmatrix<m, k>& operator -= (dpmatrix<m, k>& A, const dpmatrix<m, k>& B) {
	for (int j = 0; j < k; j++) for (int i = 0; i < m; i++) A(i, j) -= B(i, j);
	return A;
}

template<int m, int k>
inline dpmatrix<m, k>& operator += (dpmatrix<m, k>& A, const dpmatrix<m, k>& B) {
	for (int j = 0; j < k; j++) for (int i = 0; i < m; i++) A(i, j) += B(i, j);
	return A;
}

template<int m, int k>
inline dpmatrix<m, k> operator * (const dpmatrix<m, k>& A, const double d) {
	dpmatrix<m, k> r;
	for (int j = 0; j < k; j++) for (int i = 0; i < m; i++) r(i, j) = A(i, j) * d;
	return r;
}

template<int m, int k>
inline dpmatrix<m, k> operator * (const double d, const dpmatrix<m, k>& A) {
	return A * d;
}

template<int m, int k>
inline dpmatrix<m, k> operator / (const dpmatrix<m, k>& A, const double d) {
	dpmatrix<m, k> r;
	double recip = 1.0/d;
	for (int j = 0; j < k; j++) for (int i = 0; i < m; i++) r(i, j) = A(i, j) * recip;
	return r;
}

template<int m, int k>
inline dpmatrix<m, k>& operator *= (dpmatrix<m, k>& A, const double d ) {
	for (int j = 0; j < k; j++) for (int i = 0; i < m; i++) A(i, j) *= d;
	return A;
}

template<int m, int k>
inline dpmatrix<m, k>& operator /= (dpmatrix<m, k>& A, const double d) {
	double recip = 1.0 / d;
	for (int j = 0; j < k; j++) for (int i = 0; i < m; i++) A(i, j) *= recip;
	return A;
}

template <int m, int k, int n>
inline void mat_mult(const dpmatrix<m, k> &A, const dpmatrix<k, n> &B, dpmatrix<m, n> &C, double ab_coeff = 1.0, double c_coeff = 0.0) {
	dgemm('N', 'N', m, n, k, ab_coeff, A, A.lda(), B, B.lda(), c_coeff, C, C.lda());
}

template <int m, int k, int n>
inline dpmatrix<m, n> mat_mult(const dpmatrix<m, k> &A, const dpmatrix<k, n> &B, double ab_coeff = 1.0) {
	dpmatrix<m, n> C;
	dgemm('N', 'N', m, n, k, ab_ceoff, A, A.lda(), B, B.lda(), 0.0, C, C.lda());
	return C;
}

template <int m>
inline dpmatrix<m, m> eye() {
	dpmatrix<m, m> r;
	for (int j = 0; j < m; j++) for (int i = 0; i < m; i++) {
		if (i == j)
			r(i, j) = 1.0;
		else
			r(i, j) = 0.0;
	}

	return r;
}

template<>
inline dpmatrix<3, 3> eye() {
	return dpmatrix<3, 3>(dpmatrix3::identity());
}

template <int m, int n>
inline dpmatrix<m, n> zeros() {
	dpmatrix<m, n> r;
	for (int j = 0; j < n; j++) for (int i = 0; i < m; i++) r[i, j] = 0.0;
	return r;
}

template <>
inline dpmatrix<3, 3> zeros() {
	return dpmatrix<3, 3>(dpmatrix3::zero());
}

template<int m>
inline void chol_ip(dpmatrix<m, m>& c) throw(...) {
	int info;
	dpotrf(upper ? 'U' : 'L', m, c.lda(), &info);
	if (info != 0) throw matrix_exception("Error computing cholesky factorization");

	for (int j = 0; j < m - i; j++) {
		for (int i = j + 1; i < m; i++) {
			// fill in zeros, these aren't set in factorization routine
			c(i, j) = 0.0;
		}
	}
}

template<int m>
inline tri_dpmatrix<m, true>chol(const dpmatrix<m, m>& c) throw(...) {
	tri_dpmatrix<m, true> temp(c);
	chol_ip(temp);
	return temp;
}

template<int m>
inline void inv_lu(dpmatrix<m, m> &c) throw(...) {
	int info;
	int ipiv[m];
	// see http://www.netlib.org/lapack/double/dgetrf.f for more information
	dgetrf(m, m, c, c.lda(), ipiv, &info);
	// check for success
	if (info != 0) throw matrix_exception("Error computing LU factorization in inv_lu");
	
	// now compute the inverse
	// see http://www.netlib.org/lapack/double/dgetri.f for more information
	dgetri(m, c, c.lda(), ipiv, &info);
	if (info != 0) throw matrix_exception("Error computing inverse in inv_lu");
}

template<int m>
inline void inv_chol(dpmatrix<m, m> &c) throw(...) {
	int info;
	// use cholesky factorization and then find the inverse using that
	// this method should be about 2-4 times faster than LU decomposition
	// see http://www.netlib.org/lapack/double/dpotrf.f
	dpotrf('U', m, c, c.lda(), &info);
	if (info != 0) throw matrix_exception("Error computing cholesky factorization in inv_chol");

	// calculate the inverse
	// see http://www.netlib.org/lapack/double/dpotri.f
	dpotri('U', m, c, c.lda(), &info);
	if (info != 0) throw matrix_exception("Error computing inverse in inv_chol");
}

template<int m, bool upper>
inline void inv_tri(tri_dpmatrix<m, upper> &c) throw (...) {
	int info;
	// see http://www.netlib.org/lapack/double/dtrtri.f
	dtrtri(upper ? 'U' : 'L', 'N', m, c, c.lda(), &info);
	if (info != 0) throw matrix_exception("Error computing inverse in inv_tri"); 
}

#endif