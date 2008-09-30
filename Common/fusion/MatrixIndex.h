#ifndef MATRIXINDEX_H
#define MATRIXINDEX_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

//used to access a particular row, colum of a zero-indexed matrix
#define midx(row, col, nr) ((col)*(nr) + (row))

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //MATRIXINDEX_H
