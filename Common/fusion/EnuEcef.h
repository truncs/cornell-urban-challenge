#ifndef ENUECEF_H
#define ENUECEF_H

#include "EcefLla.h"
#include "MatrixIndex.h"

#include <MATH.H>

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

//transform from ENU to ECEF or from ECEF to ENU
void EnuEcefTransform(double pNEW[3], double pOLD[3], bool ToECEF, double LatOrigin, double LonOrigin, double AltOrigin, bool UseOrigin);

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //ENUECEF_H
