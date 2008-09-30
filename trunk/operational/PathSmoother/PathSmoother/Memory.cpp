#include "memory_helper.h"
#include <malloc.h>

public ref class Memory {
private:
	// private constructor so this can't get instantiated
	Memory(){}

public:
	static void* Allocate(unsigned int len) {
		return _aligned_malloc(len, CACHE_LINE);
	}

	static void Free(void* ptr) {
		_aligned_free(ptr);
	}

#pragma push_macro("ZeroMemory")
#undef ZeroMemory
	static void ZeroMemory(void* ptr, unsigned int len) {
		zero_memory_sse(ptr, len);
	}
#pragma pop_macro("ZeroMemory")
};