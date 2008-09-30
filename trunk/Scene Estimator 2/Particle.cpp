#include "Particle.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

Particle::Particle(void)
{
	/*
	Default particle constructor.  Initializes values.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	mWeight = 1.0;

	mEast = 0.0;
	mNorth = 0.0;
	mHeading = 0.0;
	mGPSBiasEast = 0.0;
	mGPSBiasNorth = 0.0;

	return;
}

void Particle::CopyParticle(Particle* Particle2Copy)
{
	/*
	Copies all values of Particle2Copy.

	INPUTS:
		Particle2Copy - the particle to be copied into this particle.

	OUTPUTS:
		none.
	*/

	mWeight = Particle2Copy->Weight();

	mEast = Particle2Copy->East();
	mNorth = Particle2Copy->North();
	mHeading = Particle2Copy->Heading();
	mGPSBiasEast = Particle2Copy->GPSBiasEast();
	mGPSBiasNorth = Particle2Copy->GPSBiasNorth();

	return;
}

void Particle::SetState(double iEast, double iNorth, double iHeading, double iGPSBiasEast, double iGPSBiasNorth)
{
	/*
	Sets the state of the particle.

	INPUTS:
		iEast, iNorth, iHeading, iGPSBiasEast, iGPSBiasNorth - new state for the particle

	OUTPUTS:
		none.
	*/

	mEast = iEast;
	mNorth = iNorth;
	mHeading = UnwrapAngle(iHeading);
	mGPSBiasEast = iGPSBiasEast;
	mGPSBiasNorth = iGPSBiasNorth;

	return;
}
