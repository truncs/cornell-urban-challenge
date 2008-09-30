#include "RoadCache.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

RoadCache::RoadCache()
{
	/*
	Default constructor for road cache.  Fills in garbage values.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	mIsValid = false;

	mRoadCacheName[0] = '\0';
	mNumRows = 0;
	mNumCols = 0;
	mCenterRow = 0;
	mCenterCol = 0;
	mRoadCacheGrid.clear();

	mEastCenter = 0.0;
	mNorthCenter = 0.0;
	mEastSpan = 0.0;
	mNorthSpan = 0.0;
	mCellLengthEast = 0.0;
	mCellLengthNorth = 0.0;

	return;
}

void RoadCache::ResetRoadCache()
{
	/*
	Resets the road cache to an empty state.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	mIsValid = false;

	mRoadCacheName[0] = '\0';
	mNumRows = 0;
	mNumCols = 0;
	mCenterRow = 0;
	mCenterCol = 0;
	mCRC = 0;
	mRoadCacheGrid.clear();

	mEastCenter = 0.0;
	mNorthCenter = 0.0;
	mEastSpan = 0.0;
	mNorthSpan = 0.0;
	mCellLengthEast = 0.0;
	mCellLengthNorth = 0.0;

	return;
}

bool RoadCache::LoadRoadCacheFromScratch(int iNumRows, int iNumCols, char* iRoadGraphName, unsigned short iCRC, int iNumPoints, RoadPoint* iRoadPoints, int iNumPartitions, RoadPartition* iRoadPartitions)
{
	/*
	Generates the road cache from scratch (VERY slow).

	INPUTS:
		iNumRows, iNumCols - number of rows and columns in the road cache.
		iRoadGraphName - name of the road graph file that will be processed.
		iCRC - the CRC for the road graph file that will be processed.
		iNumPoints, iRoadPoints - number and array of points on the RNDF (for sizing the grid)
		iNumPartitions, iRoadPartitions - number and array of partitions on the RNDF
			(will be cached)

	OUTPUTS:
		rSuccess - true if the road cache is computed correctly, false otherwise.
			If true, generates the road cache from scratch, loads it, and also saves it to disk.
	*/

	bool rSuccess = false;

	int i;
	int idx;
	int j;
	int k;
	int npt = iNumPoints;
	int np = iNumPartitions;
	FILE* oRoadCache = NULL;

	printf("Calculating road cache from scratch...\n");
	ResetRoadCache();

	printf("Allocating memory for rows and columns...\n");
	//copy the number of rows and columns
	mNumRows = iNumRows;
	mNumCols = iNumCols;
	if (mNumRows < 1)
	{
		printf("Warning: road cache created with %d rows, defaulting to 1...\n", mNumRows);
		mNumRows = 1;
	}
	if (mNumCols < 1)
	{
		printf("Warning: road cache created with %d columns, defaulting to 1...\n", mNumCols);
		mNumCols = 1;
	}
	//set the center cell of the grid
	mCenterRow = mNumRows/2;
	mCenterCol = mNumCols/2;

	//store the CRC for the road graph file
	mCRC = iCRC;
	//preallocate the total number of grid cells
	mRoadCacheGrid.resize(mNumRows*mNumCols);

	double maxE = -DBL_MAX;
	double minE = DBL_MAX;
	double maxN = -DBL_MAX;
	double minN = DBL_MAX;

	//calculate the east and north spans from the road graph
	printf("Calculating east and north spans...\n");
	for (i = 0; i < npt; i++)
	{
		double ecur = iRoadPoints[i].East();
		double ncur = iRoadPoints[i].North();

		if (ecur < minE)
		{
			minE = ecur;
		}
		if (ecur > maxE)
		{
			maxE = ecur;
		}
		if (ncur < minN)
		{
			minN = ncur;
		}
		if (ncur > maxN)
		{
			maxN = ncur;
		}
	}

	//set the road cache centers
	mEastCenter = 0.5*(maxE + minE);
	mNorthCenter = 0.5*(maxN + minN);
	//set the road cache spans
	mEastSpan = fabs(maxE - minE);
	mNorthSpan = fabs(maxN - minN);

	//expand the spans by the road cache pad size
	mEastSpan += 2.0*RC_PADSIZE;
	mNorthSpan += 2.0*RC_PADSIZE;

	printf("Calculating grid cell size...\n");
	mCellLengthEast = mEastSpan / ((double) mNumCols);
	mCellLengthNorth = mNorthSpan / ((double) mNumRows);

	printf("Calculating closest partitions...\n");
	double pct = 0.0;
	double lastdisp = -0.1;
	double* CPDists;
	CPDists = new double[np];
	//calculate the maximum distance to any point in the cell from the cell center
	double ddist = 0.5*sqrt(mCellLengthEast*mCellLengthEast + mCellLengthNorth*mCellLengthNorth);
	for (i = 0; i < mNumRows; i++)
	{
		for (j = 0; j < mNumCols; j++)
		{
			//calculate the closest partitions to the (i, j)th grid cell

			idx = RoadCacheCellGridIndex(i, j);
			double ecell;
			double ncell;
			RoadCacheCellEastNorth(ecell, ncell, i, j);

			double dmin = DBL_MAX;
			for (k = 0; k < np; k++)
			{
				//compute the distance from this grid cell to each partition
				double dcur = iRoadPartitions[k].DistanceToPartition(ecell, ncell);
				//store the distance from this grid cell to each partition
				CPDists[k] = dcur;
				//also keep track of the smallest distance
				if (dcur < dmin)
				{
					dmin = dcur;
				}
			}

			//calculate the threshold for partitions to be added to the list
			double dthresh = dmin + 2.0*ddist;
			//the distance to the closest partition to any point in the cell
			//is now bounded by dthresh (triangle inequality)

			for (k = 0; k < np; k++)
			{
				//go back through the partitions to pick out those that could be the
				//closest partition for any point in each grid cell

				if (CPDists[k] <= dthresh)
				{
					//partition k is a candidate for closest partition, add it to the cache
					mRoadCacheGrid[idx].push_back(&(iRoadPartitions[k]));
				}
			}

			pct = ((double) (i*mNumCols + j + 1)) / ((double) (mNumRows*mNumCols));
			if (pct - lastdisp >= 0.1)
			{
				lastdisp += 0.1;
				printf("%d%% complete.\n", (int) (Round(lastdisp*100.0)));
			}
		}
	}
	delete [] CPDists;

	printf("Saving road cache...\n");

	//determine the name of the file that will hold the road cache
	int slen = (int) strnlen(iRoadGraphName, RC_NAMELENGTH);
	if (strcmp(iRoadGraphName + slen - 4, ".rgp") != 0)
	{
		printf("Warning: incompatible road graph file name.\n");
		return rSuccess;
	}
	mRoadCacheName[0] = '\0';
	strncpy_s(mRoadCacheName, RC_NAMELENGTH, iRoadGraphName, slen-4);
	strncat_s(mRoadCacheName, RC_NAMELENGTH, ".rcf", 4);

	//open the file that will hold the road cache
	errno_t err = fopen_s(&oRoadCache, mRoadCacheName, "w");
	if (err != 0)
	{
		printf("Warning: failed to save road cache.\n");
		return rSuccess;
	}

	//print the basic road cache information
	fprintf(oRoadCache, "CACHENAME=%s\n", mRoadCacheName);
	fprintf(oRoadCache, "CRC=%d\n", (int) mCRC);
	fprintf(oRoadCache, "NUMROWS=%d\n", mNumRows);
	fprintf(oRoadCache, "NUMCOLS=%d\n", mNumCols);
	fprintf(oRoadCache, "CENTERROW=%d\n", mCenterRow);
	fprintf(oRoadCache, "CENTERCOL=%d\n", mCenterCol);

	fprintf(oRoadCache, "EASTCENTER=%.12lg\n", mEastCenter);
	fprintf(oRoadCache, "NORTHCENTER=%.12lg\n", mNorthCenter);
	fprintf(oRoadCache, "EASTSPAN=%.12lg\n", mEastSpan);
	fprintf(oRoadCache, "NORTHSPAN=%.12lg\n", mNorthSpan);
	fprintf(oRoadCache, "CELLLENGTHEAST=%.12lg\n", mCellLengthEast);
	fprintf(oRoadCache, "CELLLENGTHNORTH=%.12lg\n", mCellLengthNorth);

	//print the partition id's
	fprintf(oRoadCache, "BEGIN_ROADCACHE\n");
	for (i = 0; i < mNumRows; i++)
	{
		for (j = 0; j < mNumCols; j++)
		{
			//print the list of partition ids for each grid cell
			fprintf(oRoadCache, "CELL ROW=%d COL=%d\n", i, j);
			idx = RoadCacheCellGridIndex(i, j);

			np = (int) mRoadCacheGrid[idx].size();
			for (k = 0; k < np; k++)
			{
				//print each ID in the grid cell
				RoadPartition* rpcur = mRoadCacheGrid[idx][k];
				fprintf(oRoadCache, "%s\n", rpcur->PartitionID());
			}
		}
	}
	fprintf(oRoadCache, "END_ROADCACHE\n");
	fclose(oRoadCache);

	printf("Road cache calculated successfully.\n");

	//mark the road cache as valid on a successful calculation

	mIsValid = true;
	rSuccess = true;

	return rSuccess;
}

bool RoadCache::LoadRoadCacheFromFile(char* iRoadGraphName, unsigned short iCRC, int iNumPartitions, RoadPartition* iRoadPartitions)
{
	/*
	Loads a road cache from a road cache file (*.rcf).

	INPUTS:
		iRoadGraphName - name of the road graph file that is associated with the road cache.
		iCRC - the crc value of the loaded road graph file (for comparison)
		iNumPartitions, iRoadPartitions - number and array of partitions on the RNDF
			(will be cached)

	OUTPUTS:
		rSuccess - true if the road cache is loaded correctly, false otherwise.
			If true, road cache is loaded on exit.
	*/

	bool rSuccess = false;

	int i;
	FILE* iRoadCacheFile = NULL;

	ResetRoadCache();

	//determine the name of the file that will hold the road cache
	int slen = (int) strnlen(iRoadGraphName, RC_NAMELENGTH);
	if (strcmp(iRoadGraphName + slen - 4, ".rgp") != 0)
	{
		printf("Error: invalid road graph file name.\n");
		return rSuccess;
	}
	char fname[RC_NAMELENGTH];
	fname[0] = '\0';
	strncpy_s(fname, RC_NAMELENGTH, iRoadGraphName, slen-4);
	strncat_s(fname, RC_NAMELENGTH, ".rcf", 4);

	//attempt to open the road cache file
	errno_t err = fopen_s(&iRoadCacheFile, fname, "r");
	if (err != 0)
	{
		//road cache file didn't exist
		printf("Error: could not find road cache file for road graph \"%s.\"\n", iRoadGraphName);
		return rSuccess;
	}
	//road cache file opened successfully

	printf("Loading road cache from \"%s.\"\n", fname);

	//read road cache name
	printf("Loading road cache name...\n");
	fscanf_s(iRoadCacheFile, "CACHENAME=%s\n", mRoadCacheName, RC_NAMELENGTH);
	if (strcmp(mRoadCacheName, fname) != 0)
	{
		printf("Error: road cache name does not match road graph.\n");
		fclose(iRoadCacheFile);
		return rSuccess;
	}

	//read the road graph crc
	printf("Testing road graph CRC...\n");
	int tCRC = -1;
	fscanf_s(iRoadCacheFile, "CRC=%d\n", &tCRC);
	mCRC = (unsigned short) tCRC;
	if (mCRC != iCRC)
	{
		printf("Error: road cache CRC does not match road graph CRC.\n");
		fclose(iRoadCacheFile);
		return rSuccess;
	}

	//read number of rows and columns
	printf("Loading grid cell stats...\n");
	fscanf_s(iRoadCacheFile, "NUMROWS=%d\n", &mNumRows);
	fscanf_s(iRoadCacheFile, "NUMCOLS=%d\n", &mNumCols);
	if (mNumRows <= 0 || mNumCols <= 0)
	{
		printf("Error: attempted to load %d rows and %d columns.\n", mNumRows, mNumCols);
		fclose(iRoadCacheFile);
		return rSuccess;
	}
	//read center row and column
	fscanf_s(iRoadCacheFile, "CENTERROW=%d\n", &mCenterRow);
	fscanf_s(iRoadCacheFile, "CENTERCOL=%d\n", &mCenterCol);
	if (mCenterRow <= 0 || mCenterRow >= mNumRows || mCenterCol <= 0 || mCenterCol >= mNumCols)
	{
		printf("Error: attempted to set center row and column to (%d, %d).\n", mCenterRow, mCenterCol);
		fclose(iRoadCacheFile);
		return rSuccess;
	}

	//read east-north center and span
	fscanf_s(iRoadCacheFile, "EASTCENTER=%lg\n", &mEastCenter);
	fscanf_s(iRoadCacheFile, "NORTHCENTER=%lg\n", &mNorthCenter);
	fscanf_s(iRoadCacheFile, "EASTSPAN=%lg\n", &mEastSpan);
	fscanf_s(iRoadCacheFile, "NORTHSPAN=%lg\n", &mNorthSpan);
	//read cell length
	fscanf_s(iRoadCacheFile, "CELLLENGTHEAST=%lg\n", &mCellLengthEast);
	fscanf_s(iRoadCacheFile, "CELLLENGTHNORTH=%lg\n", &mCellLengthNorth);
	if (mCellLengthEast <= 0.0 || mCellLengthNorth <= 0.0)
	{
		printf("Error: attempted to set cell dimensions to %lg x %lg.\n", mCellLengthEast, mCellLengthNorth);
		fclose(iRoadCacheFile);
		return rSuccess;
	}

	//allocate memory for road cache
	printf("Allocating memory for road cache grid cells...\n");
	mRoadCacheGrid.resize(mNumRows*mNumCols);

	printf("Building partition table...\n");
	//build a partition table to map partition id's to indices into iRoadPartitions
	StringIntLUT PartitionTable;
	int np = iNumPartitions;
	for (i = 0; i < np; i++)
	{
		RoadPartition* rpcur = &(iRoadPartitions[i]);
		PartitionTable.AddEntry(rpcur->PartitionID(), i);
	}

	printf("Reading partition data...\n");
	char rcbuff[RC_NAMELENGTH];
	if (GetNextLine(iRoadCacheFile, RC_NAMELENGTH, rcbuff) == false)
	{
		printf("Error: could not find partition data.\n");
		fclose(iRoadCacheFile);
		return rSuccess;
	}
	if (strcmp(rcbuff, "BEGIN_ROADCACHE") != 0)
	{
		printf("Error: could not find partition data.\n");
		fclose(iRoadCacheFile);
		return rSuccess;
	}

	//read in all the partitions
	int r;
	int c;
	int cacheidx = -1;
	int idx;
	int ncc = 0;
	double pct = 0.0;
	double lastdisp = -0.1;
	while (GetNextLine(iRoadCacheFile, RC_NAMELENGTH, rcbuff) == true)
	{
		if (strcmp(rcbuff, "END_ROADCACHE") == 0)
		{
			//found the end of the road cache
			break;
		}
		else if (sscanf_s(rcbuff, "CELL ROW=%d COL=%d\n", &r, &c) == 2)
		{
			//found the beginning of a new index
			cacheidx = RoadCacheCellGridIndex(r, c);
			//keep track of the number of cache cells hit
			ncc++;

			pct = ((double) ncc) / ((double) (mNumRows*mNumCols));
			if (pct - lastdisp >= 0.1)
			{
				lastdisp += 0.1;
				printf("%d%% complete.\n", (int) (Round(lastdisp*100.0)));
			}
		}
		else
		{
			//found a partition to be added to this cell

			if (cacheidx == -1)
			{
				printf("Error: could not find partition data.\n");
				fclose(iRoadCacheFile);
				return rSuccess;
			}

			//try to look up the partition id of rcbuff
			if (PartitionTable.FindIndex(&idx, rcbuff) == false)
			{
				printf("Error: could not find partition %s.\n", rcbuff);
				fclose(iRoadCacheFile);
				return rSuccess;
			}

			mRoadCacheGrid[cacheidx].push_back(&(iRoadPartitions[idx]));
		}
	}

	fclose(iRoadCacheFile);

	if (ncc != mNumRows*mNumCols)
	{
		printf("Error: found partition data for only %d cells.\n", ncc);
		return rSuccess;
	}

	//return a valid road cache when read successfully
	printf("Road cache loaded successfully.\n");
	mIsValid = true;
	rSuccess = true;

	return rSuccess;
}

bool RoadCache::IsInRoadCache(double iEast, double iNorth)
{
	/*
	Quickly determines whether the test point is inside the road cache, i.e.
	whether the road cache can be used to find the closest partition to the
	test point.

	INPUTS:
		iEast, iNorth - point to test

	OUTPUTS:
		rIsInCache - true if the test point is covered by the cache, false
			otherwise.
	*/

	bool rIsInCache = false;

	if (mIsValid == false)
	{
		//can't do anything if road cache is invalid
		return rIsInCache;
	}

	//extract the corners of the cache
	double ule;
	double uln;
	//upper left corner
	RoadCacheCellEastNorth(ule, uln, 0, 0);
	double lre;
	double lrn;
	//lower right corner
	RoadCacheCellEastNorth(lre, lrn, mNumRows-1, mNumCols-1);

	//extract the minimum and maximum bounds of the cache
	double maxE = lre + 0.5*mCellLengthEast;
	double minE = ule - 0.5*mCellLengthEast;
	double maxN = uln + 0.5*mCellLengthNorth;
	double minN = lrn - 0.5*mCellLengthNorth;

	//test whether the point is inside the cache
	if (iEast >= minE && iEast <= maxE && iNorth >= minN && iNorth <= maxN)
	{
		rIsInCache = true;
	}

	return rIsInCache;
}

RoadPartition* RoadCache::ClosestPartition(double iEast, double iNorth)
{
	/*
	Returns a pointer to the closest partition to a particular east-north test location.

	INPUTS:
		iEast, iNorth - the location of the point of interest, in RNDF-centric coordinates

	OUTPUTS:
		rClosestPartition - pointer to the road partition closest to the test point.
			NOTE: if the test point is not inside the road cache, NULL is returned.
	*/

	RoadPartition* rClosestPartition = NULL;

	if (mIsValid == false)
	{
		//don't search if the road cache is not valid
		return rClosestPartition;
	}

	int i;
	int np;

	if (IsInRoadCache(iEast, iNorth) == true)
	{
		//if the test point is inside the road cache, determine which cell it is in

		int r;
		int c;
		RoadCacheCellRowColumn(r, c, iEast, iNorth);

		//additional check that the indices are legitimate
		if (r >= 0 && r < mNumRows && c >= 0 && c < mNumCols)
		{
			//the test point is in the road cache

			//pull the desired road cache grid cell
			int idx = RoadCacheCellGridIndex(r, c);
			np = (int) mRoadCacheGrid[idx].size();

			double dmin = DBL_MAX;
			for (i = 0; i < np; i++)
			{
				//compute the distance to each partition in the road cache
				RoadPartition* rpcur = mRoadCacheGrid[idx][i];
				double dcur = rpcur->DistanceToPartition(iEast, iNorth);

				if (dcur < dmin)
				{
					//found a new closer partition
					dmin = dcur;
					rClosestPartition = rpcur;
				}
			}
		}
	}

	return rClosestPartition;
}
