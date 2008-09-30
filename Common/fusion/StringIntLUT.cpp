#include "StringIntLUT.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

void StringIntLUT::AddEntry(char* iID, int iIdx)
{
	/*
	Adds a string / int pair as an entry in the lookup table.

	INPUTS:
		iID - the unique string ID that will be used.
		iIdx - the index that should be returned when querying the string

	OUTPUTS:
		none.
	*/

	mLUT.insert(make_pair(string(iID), iIdx));

	return;
}

bool StringIntLUT::FindIndex(int* oIdx, char* iID)
{
	/*
	Finds the integer index corresponding to the given string ID

	INPUTS:
		oIdx - will be populated with the integer index on output
		iID - the unique string identifier

	OUTPUTS:
		returns true if the index was found, false otherwise.  If true,
			oIdx contains the found index.
	*/

    map<string, int>::iterator i = mLUT.find(string(iID));

    if (i != mLUT.end())
	{
		//found the entry
        *oIdx = i->second;
        return true;
    }
    else
	{
		//didn't find the entry
        return false;
    }

	return false;
}

void StringIntLUT::ClearLUT(void)
{
	/*
	Removes all entries from the lookup table, leaving it empty.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	mLUT.clear();

	return;
}

int StringIntLUT::NumEntries()
{
	/*
	Returns the number of entries in the lookup table

	INPUTS:
		none.

	OUTPUTS:
		rNumEntries - the number of entries in the lookup table
	*/

	int rNumEntries = (int) mLUT.size();

	return rNumEntries;
}
