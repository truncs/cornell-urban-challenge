#ifndef ECEFLLA_H
#define ECEFLLA_H

#include "MatrixIndex.h"

#include <MATH.H>

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

//transform from ECEF to LLA or from LLA to ECEF
void EcefLlaTransform(double pNEW[3], double pOLD[3], bool ToECEF);
//retrieve the Recef2enu matrix
void GetRecef2enu(double Recef2enu[9], double lat, double lon);

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //ECEFLLA_H
