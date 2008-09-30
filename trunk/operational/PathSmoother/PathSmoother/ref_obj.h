#ifndef _REF_OBJ_H
#define _REF_OBJ_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

#include <intrin.h>

class ref_obj {
private:
	mutable volatile long _ref_count;

public:
	ref_obj() : _ref_count(0) {}

	int add_ref() const { return _InterlockedIncrement(&_ref_count); }
	int release_ref() const { return _InterlockedDecrement(&_ref_count); }

	int ref_count() const { return _ref_count; }
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif