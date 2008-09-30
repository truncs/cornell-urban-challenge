#include "matrix.h"
#include <string.h>

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

matrix_exception::matrix_exception() {
	msg = NULL;
}

matrix_exception::matrix_exception(const char *m) {
	len = (int)strlen(m) + 1;
	msg = (char*)malloc(len);
	strcpy_s(msg, len, m);
}

matrix_exception::matrix_exception(const matrix_exception &s) {
	len = s.len;
	msg = (char*)malloc(len);
	strcpy_s(msg, len, s.msg);
}

matrix_exception& matrix_exception::operator =(const matrix_exception &s) {
	len = s.len;
	msg = (char*)malloc(len);
	strcpy_s(msg, len, s.msg);
	return *this;
}

matrix_exception::~matrix_exception() {
	free(msg);
}

const char* matrix_exception::what() const {
	return msg;
}