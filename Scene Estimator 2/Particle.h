#ifndef PARTICLE_H
#define PARTICLE_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

#include "SceneEstimatorFunctions.h"

class Particle;

class Particle
{
	//The particle class.  Stores one PosteriorPose particle

private:

	//the particle's weight in the filter
	double mWeight;

	//the vehicle's state: [E, N, H, EB, NB].  H is defined CCW E in radians.
	double mEast;
	double mNorth;
	double mHeading;
	double mGPSBiasEast;
	double mGPSBiasNorth;

public:

	Particle(void);

	//accessor functions
	double Weight(void) {return mWeight;}
	double East(void) {return mEast;}
	double North(void) {return mNorth;}
	double Heading(void) {return mHeading;}
	double GPSBiasEast(void) {return mGPSBiasEast;}
	double GPSBiasNorth(void) {return mGPSBiasNorth;}

	void CopyParticle(Particle* Particle2Copy);
	void SetState(double iEast, double iNorth, double iHeading, double iGPSBiasEast = 0.0, double iGPSBiasNorth = 0.0);
	void SetWeight(double iWeight) {mWeight = iWeight; return;}
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //PARTICLE_H
