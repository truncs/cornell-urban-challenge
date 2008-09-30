#ifndef IDGENERATOR_H
#define IDGENERATOR_H

#include <set>
#include <vector>

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

using namespace std;

class IDGenerator
{
	//Generates unique integer IDs for the TrackGenerator.

private:

	//the largest (integer) ID ever assigned so far
	int mLargestID;
	//the set of free IDs
	vector<int> mFreeIDs;
	//the set of all assigned IDs
	set<int> mAssignedIDs;

public:

	IDGenerator();

	int GenerateID();
	void ReleaseID(int iIDToRelease);
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //IDGENERATOR_H
