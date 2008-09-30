#include "IDGenerator.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

IDGenerator::IDGenerator()
{
	/*
	Default constructor for the ID generator class.  Initializes
	member variables with the first available track ID (1)

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	//the first ID assigned will be this one
	mLargestID = 1;
	//initially mark "1" as the first free ID
	mFreeIDs.push_back(1);

	return;
}

int IDGenerator::GenerateID()
{
	/*
	Returns a new unique ID that is not currently in use.

	INPUTS:
		none.

	OUTPUTS:
		rID - the new ID that will be assigned to a track.  NOTE:
			this ID is not recycled until ReleaseID is called on it.
	*/

	int rID;

	int nf = (int) mFreeIDs.size();
	if (nf > 0)
	{
		//there was at least one free ID in the list, so return that one
		rID = mFreeIDs[nf-1];
		//and remove it from the list, as it is no longer free
		mFreeIDs.pop_back();
	}
	else
	{
		//all IDs from 1 to mLargestID are assigned, so increment
		mLargestID++;
		rID = mLargestID;
	}

	//mark the ID as having been assigned
	mAssignedIDs.insert(rID);

	return rID;
}

void IDGenerator::ReleaseID(int iIDToRelease)
{
	/*
	Releases an ID from a track, so it may be reused for another track.

	INPUTS:
		iIDToRelease - the integer ID to release.

	OUTPUTS:
		none.
	*/

	//first check to see if the ID is actually assigned
	set<int>::iterator idx = mAssignedIDs.find(iIDToRelease);

    if (idx == mAssignedIDs.end())
	{
		//the track id didn't appear to be assigned in the first place
        return;
    }

	//if code gets here, the ID was assigned, so free it
	mAssignedIDs.erase(idx);
	//mark the ID as being free
	mFreeIDs.push_back(iIDToRelease);

	return;
}
