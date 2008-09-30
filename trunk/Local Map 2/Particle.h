#ifndef PARTICLE_H
#define PARTICLE_H

#include "Target.h"
#include "VehicleOdometry.h"

#include <STDLIB.H>

//declare the particle class to permit a copy constructor
class Particle;

class Particle
{
	//The particle class.  Stores one complete list of targets for the LocalMap

private:

	//the particle's weight in the particle filter
	double mWeight;

	//the number of targets stored in this particle
	int mNumTargets;
	//pointer to the head of the list of targets
	Target* mFirstTarget;

public:

	Particle();
	~Particle();

	Target* FirstTarget() {return mFirstTarget;}
	int NumTargets() {return mNumTargets;}
	double Weight() {return mWeight;}

	void AddTarget(Target* iNewTarget);
	void CopyParticle(Particle* iParticle2Copy);
	void RemoveTarget(Target* iOldTarget);
	void RemoveAllTargets();
	void SetWeight(double iWeight) {mWeight = iWeight; return;}
};

#endif //PARTICLE_H
