#ifndef _CUMEMORY_H
#define _CUMEMORY_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

#include <windows.h>
#include <cassert>

extern __declspec ( thread ) HANDLE thread_heap;

void* thread_alloc(size_t size);
void thread_free(void* p);

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif