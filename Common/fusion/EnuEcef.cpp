#include "EnuEcef.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

void EnuEcefTransform(double pNEW[3], double pOLD[3], bool ToECEF, double LatOrigin, double LonOrigin, double AltOrigin, bool UseShift)
{
	/*
	Transforms pOLD from ECEF to ENU or vice versa.

	INPUTS:
		pNEW - will hold the transformed location
		pOLD - holds the location to be transformed
		ToECEF - true if transforming to ECEF, false otherwise
		LatOrigin - latitude origin of the transformation
		LonOrigin - longitude origin of the transformation
		AltOrigin - altitude origin of the transformation
		UseShift - true if the transform should use the origin as an offset (i.e. position transform), false otherwise

	OUTPUTS:
		pNEW is set to the transformed coordinates.
	*/

	int i;
	int j;

	//extract ECEF of origin
	double LLAOrigin[3];
	double ECEFOrigin[3];

	LLAOrigin[0] = LatOrigin;
	LLAOrigin[1] = LonOrigin;
	LLAOrigin[2] = AltOrigin;

	EcefLlaTransform(ECEFOrigin, LLAOrigin, true);

	//multiply an enu vector by this matrix to transform it to ecef
	double Renu2ecef[9];
	double slat = sin(LatOrigin);
	double slon = sin(LonOrigin);
	double clat = cos(LatOrigin);
	double clon = cos(LonOrigin);
	Renu2ecef[midx(0, 0, 3)] = -slon;
	Renu2ecef[midx(0, 1, 3)] = -clon*slat;
	Renu2ecef[midx(0, 2, 3)] = clon*clat;
	Renu2ecef[midx(1, 0, 3)] = clon;
	Renu2ecef[midx(1, 1, 3)] = -slon*slat;
	Renu2ecef[midx(1, 2, 3)] = slon*clat;
	Renu2ecef[midx(2, 0, 3)] = 0.0;
	Renu2ecef[midx(2, 1, 3)] = clat;
	Renu2ecef[midx(2, 2, 3)] = slat;

	if (ToECEF == false)
	{
		//transforming from ecef to enu

		//copy memory so the inputs aren't corrupted
		double pECEF[3];
		pECEF[0] = pOLD[0];
		pECEF[1] = pOLD[1];
		pECEF[2] = pOLD[2];

		if (UseShift == true)
		{
			//express relative to the origin

			pECEF[0] -= ECEFOrigin[0];
			pECEF[1] -= ECEFOrigin[1];
			pECEF[2] -= ECEFOrigin[2];
		}

		for (i = 0; i < 3; i++)
		{
			pNEW[i] = 0.0;
			for (j = 0; j < 3; j++)
			{
				//Renu2ecef' * pECEF
				pNEW[i] += Renu2ecef[midx(j, i, 3)] * pECEF[j];
			}
		}
	}
	else
	{
		//transforming from enu to ecef

		//pNEW = Renu2ecef * pOLD
		for (i = 0; i < 3; i++)
		{
			pNEW[i] = 0.0;
			for (j = 0; j < 3; j++)
			{
				pNEW[i] += Renu2ecef[midx(i, j, 3)] * pOLD[j];
			}
		}

		if (UseShift == true)
		{
			//express as absolute position
			pNEW[0] += ECEFOrigin[0];
			pNEW[1] += ECEFOrigin[1];
			pNEW[2] += ECEFOrigin[2];
		}
	}

	return;
}
