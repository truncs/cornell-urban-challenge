#ifndef STRINGINTLUT_H
#define STRINGINTLUT_H

#include <map>
#include <string>

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

using namespace std;

class StringIntLUT
{
	//a string-to-int lookup table class.

private:

	//the lookup table itself
	map<string, int> mLUT;

public:

	void AddEntry(char* iID, int iIdx);
	void ClearLUT(void);
	bool FindIndex(int* oIdx, char* iID);
	int NumEntries();
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //STRINGINTLUT_H
