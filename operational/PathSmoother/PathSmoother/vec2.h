#ifndef _VEC2_H
#define _VEC2_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

struct vec2 {
	double x, y;

	vec2() {}
	vec2(double x_, double y_) : x(x_), y(y_) {}
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif