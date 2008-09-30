#ifndef _MEMORY_HELPER_H
#define _MEMORY_HELPER_H

#include <windows.h>
#include <emmintrin.h>

#define CACHE_LINE 64
#define SSE_ALIGN_SIZE 16

#pragma managed(push, off)

void zero_memory_sse(void* dst, size_t len){
	unsigned char* dst_byte = (unsigned char*) dst;
	// do it byte wise to get to a cache-aligned size
	while ((((uintptr_t)dst_byte)&(CACHE_LINE-1)) != 0 && len > 0) {
		*dst_byte++ = 0; len--;
	}
	
	__m128i zero = _mm_setzero_si128();
	// do a cache-line worth at a time
	while (len >= SSE_ALIGN_SIZE*4) {
		_mm_stream_si128((__m128i*)dst_byte, zero);
		_mm_stream_si128((__m128i*)(dst_byte+SSE_ALIGN_SIZE), zero);
		_mm_stream_si128((__m128i*)(dst_byte+2*SSE_ALIGN_SIZE), zero);
		_mm_stream_si128((__m128i*)(dst_byte+3*SSE_ALIGN_SIZE), zero);

		dst_byte += 4*SSE_ALIGN_SIZE; len -= (4*SSE_ALIGN_SIZE);
	}

	// do it byte-wise for the remainder
	while (len > 0) {
		*dst_byte++ = 0; len--;
	}
}

void zero_memory_windows(void* dst, size_t len) {
	ZeroMemory(dst,len);
}

#pragma managed(pop)

#endif