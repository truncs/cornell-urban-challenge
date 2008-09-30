#ifndef _MATRIX_H
#define _MATRIX_H

#include <cassert>
#include <memory>
#include <limits>

#ifndef _USE_MATH_DEFINES
#define _USE_MATH_DEFINES
#endif
#include <math.h>


using namespace std;

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

#include "acml.h"

#include "../coords/dpvector3.h"
#include "../coords/dpvector4.h"

#define MAT_IND(i, j, m, n) ((j) * (m) + (i))

inline int mat_ind(int i, int j, int m, int n) { return j * m + i; }

class matrix_exception {
private:
	int len;
	char* msg;

public:
	matrix_exception();
	matrix_exception(const char* msg);
	matrix_exception(const matrix_exception&);
	
	virtual matrix_exception& operator = (const matrix_exception&);

	virtual ~matrix_exception();

	// returns a human readable message for the exception
	virtual const char* what() const;
};

// random utility function, not sure where to put it
inline double round(double l) {
	double fl = floor(l);
	if (l - fl < 0.5) return fl; else return fl + 1.0;
}

inline double round(double l, int prec) {
	double s = _Pow_int(10, prec);
	double si = 1 / s;
	l *= s;
	double fl = floor(l);
	if (l - fl < 0.5) return fl * si; else return fl * si + si;
}

// rounds denormalized numbers
inline double denorm_round(const double l) {
	int fpc = _fpclass(l);

	if (fpc == _FPCLASS_ND || fpc == _FPCLASS_PD) return 0; 
	else return l;
}

inline double double_round_up(const double l) {
	int fpc = _fpclass(l);

	if (fpc == _FPCLASS_ND) return -DBL_MIN;
	else if (fpc == _FPCLASS_PD) return DBL_MIN;
	else return fpc;
}

//  N.B. -- there is a little weirdness with how the copy constructor and assignment operator work:
//  copy constructor will not copy the data, but will copy the pointer, so it will use the original data
//  assignment operator will copy the data and dimensions of the two matrices must match
// 
//  Here are some cases where each are used
//  matrix source(...);
//  matrix crap = source;  // copy constructor
//  matrix crap2(...);		 // use same dimensions as source, supply valid data buffer
//  crap2 = source;				 // assignment operator, so data will be copied into crap2 buffer
// 
//  There is no way right now to change the buffer once the class is constructed. 
//  
//  The reason for designing the class as a BYOBB (bring your own buffer beotch) is so that the program
//  can control allocations more effectively. Basically, it revolves around efficient multi-threading:
//  want to avoid heap allocations if possible since a program-wide lock is taken to allocate memory.
//  This is particularly so that data can be stack allocated but the utility class can still be used. 
//  Also, memory can be allocated from a specific heap and supplied. The syntax will likely be more 
//  awkward as a result, but hopefully the extra control will make up for it.
// 
//  As a result of this design, memory cannot be allocated for operations, so there is no convenient
//  multiplication, addition or subtraction operators that return a new matrix. Everything must be 
//  done with an explicit target/in place.
//
//  Operations that can fails (inversion, cholesky decomposition, etc) will throw a matrix_exception if 
//  there is a problem. This will have a human readable description, but not much else.

class submatrix;

class matrix {
	friend class submatrix;
protected:
	double* d;
	int cm, cn;
	bool trans;

public:
	matrix() : cm(0), cn(0), d(NULL), trans(false) {}
	matrix(const int m, const int n, double* data, bool transpose = false) : cm(m), cn(n), d(data), trans(transpose) {}
	matrix(const matrix& c) : cm(c.cm), cn(c.cn), d(c.d), trans(c.trans) {}

	// Assignment operator, copies data into local buffer
	// See notes above
	matrix& operator = (const matrix& c) {
		assert(d != NULL && c.d != NULL);
		int tm = m(), tn = n();
		int om = c.m(), on = c.n();
		assert(m() == c.m() && n() == c.n());

		// copy data in
		int dm = m(), dn = n();
		for (int j = 0; j < dn; j++) for (int i = 0; i < dm; i++)
			operator() (i, j) = c(i, j);

		return *this;
	}

	matrix& operator = (const dpvector3& c) {
		return copy(c);
	}

	matrix& operator = (const dpvector4& c) {
		return copy(c);
	}

	matrix& operator = (const dpmatrix3& c) {
		return copy(c);
	}

	matrix& copy(const dpvector3& c) {
		assert((m() == 3 && n() == 1) || (m() == 1 && n() == 3));
		assert(d != NULL);

		// copy data in
		int dm = m(), dn = n();
		if (dm > dn) {
			for (int i = 0; i < 3; i++) {
				operator() (i, 0) = c[i];
			}
		}
		else {
			for (int j = 0; j < 3; j++) {
				operator() (0, j) = c[j];
			}
		}

		return *this;
	}

	matrix& copy(const dpvector4& c) {
		assert((m() == 4 && n() == 1) || (m() == 1 && n() == 4));
		assert(d != NULL);

		// copy data in
		int dm = m(), dn = n();
		if (dm > dn) {
			for (int i = 0; i < 4; i++) operator()(i, 0) = c[i];
		}
		else {
			for (int j = 0; j < 4; j++) operator()(0, j) = c[j];
		}
		return *this;
	}

	matrix& copy(const dpmatrix3& c) {
		assert(m() == 3 && n() == 3);
		assert(d != NULL);

		// copy data in
		for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++)
			operator()(i,j) = c(i,j);

		return *this;
	}

	matrix& copy(const matrix& A, const double c) {
		assert(d != NULL && A.d != NULL);
		assert(m() == A.m() && n() == A.n());

		int dm = m(), dn = n();
		for (int j = 0; j < dn; j++) for (int i = 0; i < dm; i++)
			operator() (i, j) = A(i, j) * c;

		return *this;
	}

	matrix& copy(const matrix& A, const double c, const matrix& B, const double k) {
		assert(d != NULL && A.d != NULL && B.d != NULL);
		assert(m() == A.m() && m() == B.m() && n() == A.n() && n() == B.n());

		int dm = m(), dn = n();
		for (int j = 0; j < dn; j++) for (int i = 0; i < dm; i++)
			operator() (i, j) = A(i, j) * c + B(i, j) * k;

		return *this;
	}

	matrix& copy(const matrix& A, const double a, const matrix& B, const double b, const matrix& C, const double c, const matrix& D, const double ds, const matrix& E, const double e) {
		// skip asserts for now
		int dm = m(), dn = n();
		for (int j = 0; j < dn; j++) for (int i = 0; i < dm; i++) {
			operator () (i, j) = A(i, j) * a + B(i, j) * b + C(i, j) * c + D(i, j) * ds + E(i, j) * e;
		}
		return *this;
	}

	void resize(int m, int n) {
		cm = m;
		cn = n;
	}

	void get(double* c) {
		// store in column major order
		int dm = m(), dn = n();
		for (int j = 0; j < dn; j++) for (int i = 0; i < dm; i++) {
			c[j*dm+i] = operator() (i, j);
		}
	}

	// Number of rows 
	virtual int m() const { return trans ? cn : cm; }
	// Number of columns
	virtual int n() const { return trans ? cm : cn; }
	// Leading dimension of the matrix. Used in BLAS operations, not really useful elsewhere
	int ld() const { return (int)cm; } // not sure what to do with this and transpose

	// Retrievies the data pointer of the (0, 0) element of the matrix
	// For sub-matrices, this is the pointer to the (0, 0) element of the submatrix, not 
	// the original matrix
	virtual operator const double* () const { return d; }
	virtual operator double* () { return d; }
	operator void* () { return (void*)(operator double *()); }

	operator dpvector3 () const {
		assert((m() == 3 && n() == 1) || (m() == 1 && n() == 3));
		if (m() == 3)
			return dpvector3(operator()(0, 0), operator()(1, 0), operator()(2, 0));
		else if (n() == 3) 
			return dpvector3(operator()(0, 0), operator()(0, 1), operator()(0, 2));
		else
			return dpvector3();
	}

	operator dpmatrix3 () const {
		assert(m() == 3 && n() == 3);
		return dpmatrix3(d);
	}

	// Accessor for the elements of the matrix
	virtual double operator () (const int i, const int j) const { 
		assert(i >= 0 && i < m() && j >= 0 && j < n()); 
		if (trans) 
			return d[i * cm + j];
		else
			return d[j * cm + i]; 
	}
	virtual double& operator() (const int i, const int j) { 
		assert(i >= 0 && i < m() && j >= 0 && j < n()); 
		if (trans)
			return d[i * cm + j];
		else
			return d[j * cm + i]; 
	}

	bool is_valid() const { return d != NULL; }

	// Returns a sub-matrix with the same data pointer as this matrix. This can be used for 
	// assigning sub-blocks of the matrix:
	//   matrix big(10, 10, ...);
	//   matrix small(4, 4, ...);
	//   big(2, 5, 3, 6) = small;
	// 
	// si = start row, ei = end col (and similarly for sj, ej)
	// This is designed to be as similar as possible to matlab syntax
	// 
	// Also, the resulting submatrix can be used with the other operators (+=, -=, *=, etc)
	// and in multiplication, cholesky decomposition, etc, although that is somewhat sketch.
	// The result should be valid (as if the submatrix was the only matrix under consideration)
	// 
	// The following code SHOULD NOT be used
	//   matrix big(10, 10, ...);
	//   matrix small = big(2, 5, 3, 6);
	// This would cause small to reference the upper-left 4x4 block of big, which is clearly
	// not intended. If the intent is to copy into small, the following code would work
	//	 matrix big(10, 10, ...);
	//   matrix small(4, 4, ...);
	//   small = big(2, 5, 3, 6);
	// Also, submatrices can be assigned to other submatrices:
	//   matrix big1(10, 10, ...);
	//   matrix big2(10, 10, ...);
	//   big1(2, 5, 3, 6) = big2(4, 7, 0, 3);
	const submatrix operator () (int si, int ei, int sj, int ej) const;
	submatrix operator () (int si, int ei, int sj, int ej);

	// Add this and C element-wise
	matrix& operator += (const matrix& c) {
		assert(m() == c.m() && n() == c.n());
		int dm = m(), dn = n();
		for (int j = 0; j < dn; j++) for (int i = 0; i < dm; i++) operator()(i, j) += c(i, j);
		return *this;
	}

	// Subtracts this and C element-wise
	matrix& operator -= (const matrix& c) {
		assert(m() == c.m() && n() == c.n());
		int dm = m(), dn = n();
		for (int j = 0; j < dn; j++) for (int i = 0; i < dm; i++) operator()(i, j) -= c(i, j);
		return *this;
	}

	// Multiplies all elements by c
	matrix& operator *= (const double c) {
		int dm = m(), dn = n();
		for (int j = 0; j < dn; j++) for (int i = 0; i < dm; i++) operator()(i, j) *= c;
		return *this;
	}

	// Divides all elements by c
	matrix& operator /= (const double c) {
		double r = 1.0/c;
		int dm = m(), dn = n();
		for (int j = 0; j < dn; j++) for (int i = 0; i < dm; i++) operator()(i, j) *= r;
		return *this;
	}

	// Multiplies A * B and stores the result into this matrix, where A -> (m x k), B -> (k x n) and this matrix is (m x n).
	// Respects the transpose property of A and B
	// General formula is 
	//		ab_coeff * A * B + c_coeff * this
	// For standard multiplication, ab_coeff = 1.0, c_coeff = 0.0
	matrix& mult(const matrix &A, const matrix &B, const double ab_coeff = 1.0, const double c_coeff = 0.0) {
		assert(m() == A.m() && n() == B.n() && A.n() == B.m());
		dgemm(A.trans ? 'T' : 'N', B.trans ? 'T' : 'N', (int)m(), (int)n(), (int)A.n(), ab_coeff, (matrix&)A, (int)A.ld(), (matrix&)B, (int)B.ld(), c_coeff, this->operator double *(), (int)ld());
		return *this;
	}

	matrix& mult(const dpmatrix3 &A, const matrix &B, const double ab_coeff = 1.0, const double c_coeff = 0.0) {
		assert(m() == 3 && n() == B.n() && B.m() == 3);
		dgemm('N', B.trans ? 'T' : 'N', 3, (int)n(), 3, ab_coeff, (double*)A.m, 4, (matrix&)B, (int)B.ld(), c_coeff, (double*)(*this), (int)ld());
		return *this;
	}

	matrix& mult(const matrix &B, const dpmatrix3 &A, const double ab_coeff = 1.0, const double c_coeff = 0.0) {
		assert(m() == B.m() && n() == 3 && B.n() == 3);
		dgemm(B.trans ? 'T' : 'N', 'N', (int)m(), 3, 3, ab_coeff, (matrix&)B, (int)B.ld(), (double*)A.m, 4, c_coeff, (double*)(*this), (int)ld());
		return *this;
	}

	matrix& mult(const matrix &A, const dpvector3 &b, const double ab_coeff = 1.0, const double c_coeff = 0.0) {
		assert(m() == A.m() && n() == 1 && A.n() == 3);
		dgemm(A.trans ? 'T' : 'N', 'N', (int)m(), 1, 3, ab_coeff, (matrix&)A, (int)A.ld(), (double*)b.v, 4, c_coeff, (double*)(this), (int)ld());
		return *this;
	}

	// Performs the transpose in place. At the moment, only works with square matrices
	virtual matrix& T() { 
		//trans = !trans; 
		// do the actual transpose
		// swap (i, j) with (j, i)

		// this is surprisingly hard when the matrix is not square
		if (cm == cn) {
			for (int i = 0; i < cm; i++) for (int j = i+1; j < cn; j++) {
				double t = d[i * cm + j]; // source spot
				d[i * cm + j] = d[j * cm + i];
				d[j * cm + i] = t;
			}
		}
		else {
			// very tricky...use algorithm 467
			// for now, just fail
			assert(false);
		}

		return *this; 
	}

	// Fills the matrix with zeros
	matrix& zeros() {
		int dm = m(), dn = n();
		for (int j = 0; j < dn; j++) for (int i = 0; i < dm; i++) operator() (i, j) = 0.0;
		return *this;
	}

	// Set the matrix to the identity matrix of the appropriate dimension
	// Must be a square matrix for this to work
	matrix& eye() {
		assert(m() == n());
		int dm = m(), dn = n();
		for (int j = 0; j < dn; j++) for (int i = 0; i < dm; i++) {
			if (i == j)
				operator() (i, j) = 1.0;
			else
				operator() (i, j) = 0.0;
		}

		return *this;
	}

	// Fills the matrix with ones
	matrix& ones() {
		int dm = m(), dn = n();
		for (int j = 0; j < dn; j++) for (int i = 0; i < dm; i++) operator() (i, j) = 1.0;
		return *this;
	}

	// Performs cholesky decomposition on a matrix in place. The matrix must be positive definite.
	// upper specifies the form of the cholesky decomposition:
	// upper = true => chol(A)'*chol(A) = A
	// lower = true => chol(A)*chol(A)' = A
	// 
	// The opposite triangular portion of the matrix will contain zeros
	// The transpose property of the matrix will be set to false after exit--if we can perform 
	// cholesky decomposition, then the matrix was symmetric in the first place, so it doesn't matter
	// Throws matrix_exception if decomposition fails
	matrix& chol(bool upper = true) throw(...) {
		assert(m() == n()); // square matrices only
		int info;
		// see http://www.netlib.org/lapack/double/dpotrf.f
		dpotrf(upper ? 'U' : 'L', (int)m(), (double*)(*this), (int)ld(), &info);
		if (info != 0) throw matrix_exception("Error computing cholesky factorization");

		// set transpose to false--if we can perform cholesky decomposition, the 
		//  matrix was symmetric, so trans didn't matter
		// want to respect upper or lower as appropriate
		trans = false;

		// zero out other portion
		if (upper) {
			int dm = m(), dn = n();
			for (int j = 0; j < dn - 1; j++) {
				for (int i = j + 1; i < dm; i++) {
					operator()(i, j) = 0.0;
				}
			}
		}
		else {
			int dm = m(), dn = n();
			for (int j = 1; j < dn; j++) {
				for (int i = 0; i < j; i++) {
					operator()(i, j) = 0.0;
				}
			}
		}

		return *this;
	}

	// inverts a general matrix using LU factorization
	// throws matrix_exception if inversion fails
	matrix& inv_lu() throw(...) {
		assert(m() == n()); // square matrices only

		// to do this, need to compute LU factorization
		int* ipiv = (int*)_alloca(m()*sizeof(int));
		int info;

		// see http://www.netlib.org/lapack/double/dgetrf.f for more information
		dgetrf((int)m(), (int)n(), (*this), (int)ld(), ipiv, &info);
		// check for success
		if (info != 0) throw matrix_exception("Error computing LU factorization in inv_lu");

		// now compute the inverse
		// see http://www.netlib.org/lapack/double/dgetri.f for more information
		dgetri((int)m(), (*this), (int)ld(), ipiv, &info);
		if (info != 0) throw matrix_exception("Error computing inverse in inv_lu"); 

		// note that this works appropriately even matrix is transposed:
		//   inv(A') = inv(A)';

		return *this;
	}

	// inverts a triangular matrix stored the upper or lower triangle.
	// throws matrix_exception if inversion fails (matrix is not full rank)
	matrix& inv_tri(bool upper = true) throw(...) {
		upper ^= trans; // swap if transposed
		int info;
		// see http://www.netlib.org/lapack/double/dtrtri.f
		dtrtri(upper ? 'U' : 'L', 'N', (int)m(), (double*)(*this), (int)ld(), &info);
		if (info != 0) throw matrix_exception("Error computing inverse in inv_tri"); 

		// note that transpose is still respected

		return *this;
	}

	// rounds all elements of the matrix
	matrix& round() {
		assert(d != NULL);
		int dm = m(), dn = n();
		for (int j = 0; j < dn; j++) for (int i = 0; i < dm; i++)
			operator () (i, j) = ::round(operator()(i, j));
		return *this;
	}

	bool is_empty() const { return this->operator const double *() == NULL; }
};

class submatrix : public matrix {
	friend class matrix;

private:
	int si, sj, sm, sn;

	submatrix(const matrix& par, int si, int sj, int sm, int sn) {
		this->cn = par.cn; this->cm = par.cm; this->trans = par.trans;
		this->d = par.d;
		this->si = si; this->sj = sj;
		this->sm = sm; this->sn = sn;
	}

public:
	submatrix(const submatrix& m) {
		this->cn = m.cn; this->cm = m.cm; this->trans = m.trans;
		this->d = m.d;
		this->si = m.si; this->sj = m.sj;
		this->sm = m.sm; this->sn = m.sn;
	}

	matrix& operator = (const matrix& c) {
		assert(d != NULL && c.d != NULL);
		assert(m() == c.m() && n() == c.n());

		// copy data in
		int dm = m(), dn = n();
		for (int j = 0; j < dn; j++) for (int i = 0; i < dm; i++) 
			operator() (i, j) = c(i, j);

		return *this;
	}

	matrix& operator = (const submatrix& c) {
		return operator = ((matrix&)c);
	}

	matrix& operator = (const dpvector3& c) {
		return matrix::copy(c);
	}

	matrix& operator = (const dpvector4& c) {
		return matrix::copy(c);
	}

	matrix& operator = (const dpmatrix3& c) {
		return matrix::copy(c);
	}

	virtual int m() const { return /*trans ? sn :*/ sm; }
	virtual int n() const { return /*trans ? sm :*/ sn; }
	
	virtual operator const double* () const { 
		if (trans)
			return d + si * cm + sj;
		else
			return d + sj * cm + si;
	}
	virtual operator double* () { 
		if (trans)
			return d + si * cm + sj;
		else
			return d + sj * cm + si; 
	}

	virtual double operator () (const int i, const int j) const {
		assert(i >= 0 && i < m() && j >= 0 && j < n());
		if (trans)
			return d[(i + si) * cm + (j + sj)];
		else
			return d[(j + sj) * cm + (i + si)];
	}

	virtual double& operator() (const int i, const int j) { 
		assert(i >= 0 && i < m() && j >= 0 && j < n());
		if (trans)
			return d[(i + si) * cm + (j + sj)];
		else
			return d[(j + sj) * cm + (i + si)];
	}
};

inline const submatrix matrix::operator () (int si, int ei, int sj, int ej) const {
	assert(si >= 0 && si <= ei && ei < m() && sj >= 0 && sj <= ej && ej < n());
	// if this is a transpose, swap the stuffters
	/*if (trans) {
		int temp = si;
		si = sj;
		sj = temp;
		temp = ei;
		ei = ej;
		ej = temp;
	}*/
	return submatrix(*this, si, sj, ei-si+1, ej-sj+1);
}

inline submatrix matrix::operator () (int si, int ei, int sj, int ej) {
	assert(si >= 0 && si <= ei && ei < m() && sj >= 0 && sj <= ej && ej < n());
	/*if (trans) {
		int temp = si;
		si = sj;
		sj = temp;
		temp = ei;
		ei = ej;
		ej = temp;
	}*/
	// the submatrix will be un-transposed (if that makes sense)
	// it will handle the transpose stuff itself
	return submatrix(*this, si, sj, ei-si+1, ej-sj+1);
}

inline double unwrap(double d) {
	double w = fmod(d,2*M_PI);
	if (w > M_PI)
		w -= 2*M_PI;
	return w;
}

inline void unwrap(matrix& m) {
	for (int i = 0; i < m.m(); i++) {
		for (int j = 0; j < m.n(); j++) {
			double w = fmod(m(i,j),2*M_PI);
			if (w > M_PI)
				w -= 2*M_PI;
			m(i,j) = w;
		}
	}
}

/*template <class Ty, class Ax>
class _submatrix;

template <class Ty = double, class Ax = allocator<Ty>>
class matrix {
	template <typename Ty2, typename Ax2>
	friend class matrix;
public:
	typedef typename Ax::template
		rebind<Ty>::other Alt;

protected:
	typedef typename Ax::template rebind<int>::other Intalt;
	typedef matrix<Ty, Ax> Myt;

	typename Alt::pointer d;
	typename Alt::size_type cm, cn;
	Alt alloc;
	Intalt intalloc;
	int* submat_count;

	virtual void free_data() {
		if (submat_count != NULL)	(*submat_count)--;
		if (d != NULL && (submat_count == NULL || *submat_count == 0)) {
			//for (int i = 0; i < cm * cn; i++) alloc.destroy(d + i);
			alloc.deallocate(d, cm * cn);
		}

		if (submat_count != NULL && *submat_count == 0)	intalloc.deallocate(submat_count, 1);

		d = NULL;
	}

	void check_alloc() {
		if (d == NULL) d = alloc.allocate(cm * cn);
	}

public:
	
	matrix() : cm(0), cn(0), d(NULL), submat_count(NULL) {}

	matrix(const typename Alt::size_type m, const typename Alt::size_type n, typename Alt::const_pointer d = NULL) {
		this->cm = m;
		this->cn = n;
		this->d = NULL;

		if (d != NULL) {
			// allocate space
			this->d = alloc.allocate(m * n);
			// copy data
			for (typename Alt::size_type j = 0; j < n; j++) for (typename Alt::size_type i = 0; i < m; i++) 
				this->d[j * m + i] = d[j * m + i];
		}

		submat_count = NULL;
	}

	//template <typename Ax2>
	matrix(const Myt& m) {
		this->cm = m.m();
		this->cn = m.n();
		this->d = NULL;

		// copy data
		if (m.d != NULL) {
			this->d = alloc.allocate(cm * cn);
			for (typename Alt::size_type j = 0; j < cn; j++) for (typename Alt::size_type i = 0; i < cm; i++) 
				this->operator()(i, j) = m(i, j);
		}

		submat_count = NULL;
	}

	virtual ~matrix() { free_data(); }

	Myt& operator = (const Myt& m) {
		free_data();
		this->cm = m.cm;
		this->cn = m.cn;
		this->d = NULL;

		if (m.d != NULL) {
			this->d = alloc.allocate(cm * cn);
			// copy data
			for (int j = 0; j < cn; j++) for (int i = 0; i < cm; i++) 
				operator()(i, j) = m(i, j);
		}
		submat_count = NULL;
		return *this;
	}
	
	//template <typename Ax2>
	//matrix<Ty, Ax>& operator = (const matrix<Ty, Ax2>& m) {
	//	free_data();
	//	this->cm = m.cm;
	//	this->cn = m.cn;
	//	this->d = NULL;

	//	if (m.d != NULL) {
	//		this->d = alloc.allocate(cm * cn);
	//		// copy data
	//		for (int j = 0; j < cm; j++) for (int i = 0; i < cm; i++) 
	//			operator()(i, j) = m(i, j);
	//	}
	//	submat_count = NULL;
	//	return *this;
	//}

	virtual typename Alt::size_type m() const { return cm; }
	virtual typename Alt::size_type n() const { return cn; }

#ifdef min
#pragma push_macro("min")
#undef min
#define _min_removed
#endif

	virtual typename Alt::value_type operator () (const typename Alt::size_type i, const typename Alt::size_type j) const {
		assert(i >= 0 && i < cm && j >= 0 && j < cn);
		if (d == NULL) return numeric_limits<Alt::value_type>::min();
		return d[j * cm + i];
	}

#ifdef _min_removed
#pragma pop_macro("min")
#endif

	virtual typename Alt::reference operator() (const typename Alt::size_type i, const typename Alt::size_type j) {
		assert(i >= 0 && i < cm && j >= 0 && j < cn);
		check_alloc();
		return d[j * cm + i];
	}

	virtual _submatrix<Ty, Ax> operator () (const typename Alt::size_type si, const typename Alt::size_type ei, const typename Alt::size_type sj, const typename Alt::size_type ej) {
		assert(si >= 0 && ei >= si && ei < cm && sj >= 0 && ej >= sj && ej < cn);
		check_alloc();
		if (submat_count == NULL) {
			submat_count = intalloc.allocate(1);
			*submat_count = 1;
		}
		return _submatrix<Ty, Ax>(*this, submat_count, si, sj, ei - si + 1, ej - sj + 1);
	}

	//matrix<Ty, Ax> get_submatrix(const typename Alt::size_type i_start, const typename Alt::size_type j_start, const typename Alt::size_type m, const typename Alt::size_type n) const {
	//	assert(i_start >= 0 && i_start + m <= cm && j_start >= 0 && j_start + n <= cn);
	//	matrix<Ty, Ax> r(m, n);
	//	for (int j = 0; j < n; j++) for (int i = 0; i < m; i++) 
	//		r(i, j) = operator()(i + i_start, j + i_start);
	//	return r;
	//}
	//
	//void set_submatrix(const typename Alt::size_type i_start, const typename Alt::size_type j_start, const matrix<Ty, Ax>& d) {
	//	assert(i_start >= 0 && i_start + m <= cm && j_start >= 0 && j_start + n <= cn);

	//}

	virtual operator typename Alt::const_pointer () const { const_cast<matrix<Ty, Ax>*>(this)->check_alloc(); return d; }
	virtual operator typename Alt::pointer () { check_alloc(); return d; }

	virtual int ld() const { return (int)cm; }
};

template <typename Ty, typename Ax>
class _submatrix : public matrix<Ty, Ax> {
	friend class matrix<Ty, Ax>;
private:
	typename Alt::size_type si, sj, sm, sn;

	_submatrix(matrix<Ty, Ax>& par, int* submat_count, typename Alt::size_type si, typename Alt::size_type sj, typename Alt::size_type cm, typename Alt::size_type cn) {
		this->si = si; this->sj = sj; this->sm = cm; this->sn = cn;
		this->cm = par.m(); this->cn = par.n();
		this->d = (double*)par;
		this->submat_count = submat_count;
		(*submat_count)++;
	}

public:
	_submatrix(const _submatrix<Ty, Ax>& c) {
		this->d = c.d;
		this->cm = c.cm; this->cn = c.cn;
		this->submat_count = c.submat_count;
		this->si = c.si; this->sj = c.sj; this->sm = c.sm; this->sn = c.sn;
		(*submat_count)++;
	}

	virtual ~_submatrix() {
		free_data(); 
	}

	template <typename Ax2>
	matrix<Ty, Ax>& operator = (const matrix<Ty, Ax2>& r) {
		// copy data in
		assert(sm == r.m() && sn == r.n());
		for (typename Alt::size_type j = 0; j < sn; j++) for (typename Alt::size_type i = 0; i < sm; i++)
			this->operator () (i, j) = r(i, j);
		return *this;
	}

	virtual typename Alt::size_type m() const { return sm; }
	virtual typename Alt::size_type n() const { return sn; }

	virtual typename Alt::value_type operator () (const typename Alt::size_type i, const typename Alt::size_type j) const {
		assert(i >= 0 && i < sm && j >= 0 && j < cn);
		return d[(j + sj) * cm + (i + si)];
	}

	virtual typename Alt::reference operator() (const typename Alt::size_type i, const typename Alt::size_type j) {
		assert(i >= 0 && i < sm && j >= 0 && j < sn);
		return d[(j + sj) * cm + (i + si)];
	}

	// offset pointer by si, so that incrementing by ld will result in repositioning at the next full column
	virtual operator typename Alt::const_pointer () const { return d + si; }
	virtual operator typename Alt::pointer () { return d + si; }
};

template <typename Ty, typename Ax, typename Ax2>
matrix<Ty, Ax> operator + (const matrix<Ty, Ax>& A, const matrix<Ty, Ax2>& B) {
	assert(A.m() == B.m() && A.n() == B.n());
	matrix<Ty, Ax> r(A.m(), A.n());
	for (int j = 0; j < A.n(); j++) for (int i = 0; i < A.m(); i++) r(i, j) = A(i, j) + B(i, j);
	return r;
}

template <typename Ty, typename Ax, typename Ax2>
matrix<Ty, Ax> operator - (const matrix<Ty, Ax>& A, const matrix<Ty, Ax2>& B) {
	assert(A.m() == B.m() && A.n() == B.n());
	matrix<Ty, Ax> r(A.m(), A.n());
	for (int j = 0; j < A.n(); j++) for (int i = 0; i < A.m(); i++) r(i, j) = A(i, j) - B(i, j);
	return r; 
}

template <typename Ty, typename Ax, typename Ax2>
matrix<Ty, Ax>& operator += (matrix<Ty, Ax>& A, const matrix<Ty, Ax2>& B) {
	assert(A.m() == B.m() && A.n() == B.n());
	for (int j = 0; j < A.n(); j++) for (int i = 0; i < A.m(); i++) A(i, j) += B(i, j);
	return A; 
}

template <typename Ty, typename Ax, typename Ax2>
matrix<Ty, Ax>& operator -= (matrix<Ty, Ax>& A, const matrix<Ty, Ax2>& B) {
	assert(A.m() == B.m() && A.n() == B.n());
	for (int j = 0; j < A.n(); j++) for (int i = 0; i < A.m(); i++) A(i, j) -= B(i, j);
	return A; 
}

template <typename Ty, typename Ax,  typename Ax2>
matrix<Ty, Ax> operator * (const matrix<Ty, Ax>& A, const matrix<Ty, Ax2>& B) {
	assert(A.n() == B.m());
	matrix<Ty, Ax> r(A.m(), B.n());
	dgemm('N', 'N', (int)A.m(), (int)B.n(), (int)A.n(), 1.0, (matrix<Ty, Ax>&)A, A.ld(), (matrix<Ty, Ax>&)B, B.ld(), 0.0, r, r.ld());
	return r;
}

template <typename Ty, typename Ax>
matrix<Ty, Ax> operator * (const matrix<Ty, Ax>& A, const double c) {
	matrix<Ty, Ax> r(A.m(), A.n());
	for (int j = 0; j < A.n(); j++) for (int i = 0; i < A.m(); i++) r(i, j) = A(i, j) * c;
	return r;
}

template <typename Ty, typename Ax>
matrix<Ty, Ax> operator * (const double c, const matrix<Ty, Ax>& A) {
	return A * c;
}

template <typename Ty, typename Ax>
matrix<Ty, Ax>& operator *= (matrix<Ty, Ax>& A, const double c) {
	for (int j = 0; j < A.n(); j++) for (int i = 0; i < A.m(); i++) A(i, j) *= c;
	return A;
}

template <typename Ty, typename Ax>
matrix<Ty, Ax> operator / (const matrix<Ty, Ax>& A, const double c) {
	matrix<Ty, Ax> r(A.m(), A.n());
	double recip = 1.0/c;
	for (int j = 0; j < A.n(); j++) for (int i = 0; i < A.m(); i++) r(i, j) = A(i, j) * recip;
	return r;
}

template <typename Ty, typename Ax>
matrix<Ty, Ax>& operator /= (matrix<Ty, Ax>& A, const double c) {
	double recip = 1.0 / c;
	for (int j = 0; j < A.n(); j++) for (int i = 0; i < A.m(); i++) A(i, j) *= recip;
	return A;
}

*/

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif
