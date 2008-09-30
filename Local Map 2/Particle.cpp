#include "Particle.h"

Particle::Particle()
{
	/*
	Default particle constructor.  Initializes an empty particle with
	no targets and default values.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	//initialize the particle with no weight
	mWeight = 0.0;

	//initialize an empty set of targets
	mNumTargets = 0;
	mFirstTarget = NULL;

	return;
}

Particle::~Particle()
{
	/*
	Particle destructor.  Frees memory allocated in the particle
	and clears out its list of targets.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	mWeight = 0.0;

	//delete the list of targets
	RemoveAllTargets();

	return;
}

void Particle::RemoveAllTargets()
{
	/*
	Removes all the targets from this particle, freeing their memory

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	//delete all the targets in the list
	while (mFirstTarget != NULL)
	{
		Target* CurrentTarget = mFirstTarget;
		RemoveTarget(CurrentTarget);
	}

	return;
}

void Particle::CopyParticle(Particle* iParticle2Copy)
{
	/*
	Makes an exact copy of the passed in particle, storing the results
	in this particle

	INPUTS:
		iParticle2Copy - the particle that will be copied here

	OUTPUTS:
		none.  NOTE: memory is freed in this particle before reallocated.
			NOTE: ordering of targets is not preserved.
	*/

	//first clear out memory allocated in this particle
	RemoveAllTargets();

	//now copy the particle over
	mWeight = iParticle2Copy->Weight();

	int i;
	int nt = iParticle2Copy->NumTargets();

	Target* CurrentTarget = iParticle2Copy->FirstTarget();
	Target* NewTarget;

	for (i = 0; i < nt; i++)
	{
		//create a copy of the ith target in this particle
		NewTarget = new Target(CurrentTarget);
		AddTarget(NewTarget);
		CurrentTarget = CurrentTarget->NextTarget;
	}

	return;
}

void Particle::AddTarget(Target* iNewTarget)
{
	/*
	Adds a new target to the list stored in this particle.

	INPUTS:
		iNewTarget - the new target that will be added.  NOTE: memory for
			this target is not copied, so iNewTarget must be preallocated.

	OUTPUTS:
		none.
	*/

	if (iNewTarget == NULL)
	{
		//don't add an empty target to the list
		return;
	}
	if (iNewTarget->IsInitialized() == false)
	{
		//don't add a target that hasn't been initialized

		//NOTE: iNewTarget is usually freed when the particle is deleted,
		//but since it's not entering the particle, delete it here
		delete iNewTarget;
		return;
	}

	//increment the number of targets stored
	mNumTargets++;

	//store the existing first target
	Target* OldFirstTarget = mFirstTarget;

	//put the new target at the head of the list
	mFirstTarget = iNewTarget;
	//set the new target's pointers
	mFirstTarget->NextTarget = OldFirstTarget;
	mFirstTarget->PrevTarget = NULL;

	if (OldFirstTarget != NULL)
	{
		//set the old first target's pointer too
		OldFirstTarget->PrevTarget = mFirstTarget;
	}

	return;
}

void Particle::RemoveTarget(Target* iOldTarget)
{
	/*
	Removes a particular target from this particle's linked list.

	INPUTS:
		iOldTarget - pointer to the target to be removed

	OUTPUTS:
		none.
	*/

	if (iOldTarget == NULL)
	{
		//don't remove an empty target
		return;
	}

	//decrement the number of targets
	mNumTargets--;

	//fix up the links in the linked list
	Target* PrevTarget = iOldTarget->PrevTarget;
	Target* NextTarget = iOldTarget->NextTarget;

	if (PrevTarget != NULL)
	{
		PrevTarget->NextTarget = NextTarget;
	}
	if (NextTarget != NULL)
	{
		NextTarget->PrevTarget = PrevTarget;
	}

	//check to see if we're deleting the first target in the particle
	if (iOldTarget == mFirstTarget)
	{
		//if the first target is to be deleted, update the list pointer
		mFirstTarget = NextTarget;
	}

	//finally, free memory allocated to the target
	delete iOldTarget;
	//and invalidate the pointer so data isn't corrupted
	iOldTarget = NULL;

	return;
}
