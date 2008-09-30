#include "cumemory.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

__declspec( thread ) HANDLE thread_heap = NULL;

#define USE_LFG

void* thread_alloc(size_t size) {
	if (thread_heap == NULL) {
		thread_heap = HeapCreate(HEAP_NO_SERIALIZE, 0x10000, 0);
		assert(thread_heap != NULL);

#ifdef USE_LFH
		ULONG HeapFragValue = 2;
		HeapSetInformation(thread_heap, HeapCompatibilityInformation, &HeapFragValue, sizeof(HeapFragValue));
#endif
	}

	return HeapAlloc(thread_heap, 0, size);
}

void thread_free(void* p) {
	assert(thread_heap != NULL);
	HeapFree(thread_heap, 0, p);
}

void thread_destroy_heap() {
	if (thread_heap != NULL) {
		HeapDestroy(thread_heap);
	}
}