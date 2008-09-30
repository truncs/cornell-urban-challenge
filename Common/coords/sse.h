#ifndef _SSE3_H
#define _SSE3_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

#include <emmintrin.h>

//#define USE_SSE3

// Some useful macros:

extern const _MM_ALIGN16 __int64 __MASKSIGNd_[2];
#define _MASKSIGNd_ (*(__m128d*)&__MASKSIGNd_)

extern const _MM_ALIGN16 __int64 __MASKSIGNd1_[2];
#define _MASKSIGNd1_ (*(__m128d*)&__MASKSIGNd1_)

extern const _MM_ALIGN16 __int64 __MASKSIGNd0_[2];
#define _MASKSIGNd0_ (*(__m128d*)&__MASKSIGNd0_)

#define _mm_abs_pd(vec)     _mm_andnot_pd(_MASKSIGNd_,vec)
#define _mm_neg_pd(vec)     _mm_xor_pd(_MASKSIGNd_,vec)
#define _mm_neg0_pd(vec)    _mm_xor_pd(_MASKSIGNd0_,vec)
#define _mm_neg1_pd(vec)    _mm_xor_pd(_MASKSIGNd1_,vec)

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif