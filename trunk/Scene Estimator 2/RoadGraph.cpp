#include "RoadGraph.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

RoadGraph::RoadGraph(char *iRoadGraphFileName, int iNumCacheRows, int iNumCacheCols)
{
	/*
	Default constructor for a road graph.  Reads in a road graph file (*.rgp)
	and stores all its road edges and waypoints for easy access.

	INPUTS:
		iRoadGraphFileName - name of the road graph file to load.
		iNumCacheRows - number of rows to use for the road cache
		iNumCacheCols - number of columns to use for the road cache

	OUTPUTS:
		none.
	*/

	//initialize the variables to default values
	mLatOrigin = 0.0;
	mLonOrigin = 0.0;
	mNumPoints = 0;
	mNumPartitions = 0;
	mRoadPoints = NULL;
	mRoadPartitions = NULL;
	mIsValid = false;

	int i;
	int j;
	StringIntLUT PointLUT;
	StringIntLUT PartitionLUT;
	StringIntLUT PartitionTypeLUT;
	StringIntLUT FitTypeLUT;
	StringIntLUT LineTypeLUT;

	//set up the partitiontype LUT
	PartitionTypeLUT.AddEntry("Lane", RP_LANE);
	PartitionTypeLUT.AddEntry("Zone", RP_ZONE);
	PartitionTypeLUT.AddEntry("Interconnect", RP_INTERCONNECT);

	//set up the fit type LUT
	FitTypeLUT.AddEntry("Line", RP_LINE);
	FitTypeLUT.AddEntry("Polygon", RP_POLYGON);

	//set up the line type LUT
	LineTypeLUT.AddEntry("None", RP_NOBOUNDARY);
	LineTypeLUT.AddEntry("DoubleYellow", RP_DOUBLEYELLOW);
	LineTypeLUT.AddEntry("SolidYellow", RP_SOLIDYELLOW);
	LineTypeLUT.AddEntry("SolidWhite", RP_SOLIDWHITE);
	LineTypeLUT.AddEntry("BrokenWhite", RP_BROKENWHITE);

	//open and read the road graph file
	FILE* RGFile;
	errno_t err;
	err = fopen_s(&RGFile, iRoadGraphFileName, "r");
	if (err != 0)
	{
		printf("Error: unable to open road graph file.\n");
		return;
	}

	//copy the name of the road graph file over
	strncpy_s(mRoadGraphFileName, ROADGRAPH_FIELDSIZE, iRoadGraphFileName, ROADGRAPH_FIELDSIZE);

	//if code gets here, road graph was opened successfully

	//declare a temporary text buffer
	int buffsize = 4096;
	char* buff = new char[buffsize];

	//***1. read the road graph header information
	//RNDF name
	GetNextLine(RGFile, buffsize, buff);
	sscanf_s(buff, "RndfName\t%s", mRNDFName, ROADGRAPH_FIELDSIZE);
	printf("Loading road graph for \"%s\" RNDF...\n", mRNDFName);
	//RNDF creation date
	GetNextLine(RGFile, buffsize, buff);
	sscanf_s(buff, "RndfCreationDate\t%s", mRNDFCreationDate, ROADGRAPH_FIELDSIZE);
	printf("RNDF creation date: %s.\n", mRNDFCreationDate);
	//Road graph creation date (not used)
	GetNextLine(RGFile, buffsize, buff);
	//RNDF projection origin
	GetNextLine(RGFile, buffsize, buff);
	sscanf_s(buff, "ProjectionOrigin\t%lg\t%lg", &mLatOrigin, &mLonOrigin);
	mLatOrigin = mLatOrigin*PI/180.0;
	mLonOrigin = mLonOrigin*PI/180.0;

	//***2. read in all the waypoints
	printf("Reading waypoints...\n");
	GetNextLine(RGFile, buffsize, buff);
	sscanf_s(buff, "NumberOfWaypoints\t%d\n", &mNumPoints);
	mRoadPoints = new RoadPoint[mNumPoints];
	//read through the waypoints directive
	GetNextLine(RGFile, buffsize, buff);

	char wpid[ROADGRAPH_FIELDSIZE];
	char isstop[ROADGRAPH_FIELDSIZE];
	double east;
	double north;

	//read the first waypoint
	GetNextLine(RGFile, buffsize, buff);
	for (i = 0; i < mNumPoints; i++)
	{
		//process each waypoint
		if (strcmp(buff, "End_Waypoints") == 0)
		{
			//found an end waypoints directive
			break;
		}

		//scan in the waypoint data
		sscanf_s(buff, "%s\t%s\t%lg\t%lg", wpid, ROADGRAPH_FIELDSIZE, isstop, ROADGRAPH_FIELDSIZE, &east, &north);
		if (strcmp(isstop, "IsStop") == 0)
		{
			mRoadPoints[i].SetPointData(wpid, east, north, true);
		}
		else
		{
			mRoadPoints[i].SetPointData(wpid, east, north, false);
		}
		//store the waypoint in the point lookup table as the ith point
		PointLUT.AddEntry(wpid, i);

		//read through all the waypoint's member partitions for now
		while (GetNextLine(RGFile, buffsize, buff) == true)
		{
			if (strcmp(buff, "EndMemberPartitions") == 0)
			{
				break;
			}
		}

		GetNextLine(RGFile, buffsize, buff);
	}
	if (i != mNumPoints)
	{
		//number of waypoints didn't match the file
		fclose(RGFile);
		delete [] buff;

		printf("Error: found %d waypoints, but RNDF was supposed to have %d.\n", i, mNumPoints);
		return;
	}
	if (strcmp(buff, "End_Waypoints") != 0)
	{
		//number of waypoints didn't match the file
		fclose(RGFile);
		delete [] buff;

		printf("Error: found more waypoints than RNDF specified.\n");
		return;
	}
	printf("Successfully parsed %d waypoints.\n", mNumPoints);

	//***3. read in all the partitions, ignoring connections for now
	printf("Reading road partitions...\n");
	GetNextLine(RGFile, buffsize, buff);
	if (strstr(buff, "NumberOfPartitions") != NULL)
	{
		//found the number of partitions directive
		sscanf_s(buff, "NumberOfPartitions\t%d", &mNumPartitions);
	}
	else
	{
		//couldn't find number of partitions
		fclose(RGFile);
		delete [] buff;

		printf("Error: could not find number of partitions.\n");
		return;
	}
	if (mNumPartitions <= 0)
	{
		//check to make sure there's a valid number of partitions
		fclose(RGFile);
		delete [] buff;

		printf("Error: number of partitions is invalid.\n");
		return;
	}
	//allocate memory for the partitions
	mRoadPartitions = new RoadPartition[mNumPartitions];

	char ptid[ROADGRAPH_FIELDSIZE];
	char smallbuff[ROADGRAPH_FIELDSIZE];
	int ptype;
	bool issparse;
	int ftype;
	double* fparms;
	double lw;
	int lltype;
	int rltype;
	int np;
	RoadPoint** ppts;
	int idx;

	GetNextLine(RGFile, buffsize, buff);
	for (i = 0; i < mNumPartitions; i++)
	{
		//read in each partition

		if (strcmp(buff, "Partition") != 0)
		{
			fclose(RGFile);
			delete [] buff;

			printf("Error: partition is invalid.\n");
			return;
		}

		//read in the partition ID
		GetNextLine(RGFile, buffsize, buff);
		sscanf_s(buff, "PartitionId\t%s", ptid, ROADGRAPH_FIELDSIZE);

		//read in the partition type
		GetNextLine(RGFile, buffsize, buff);
		sscanf_s(buff, "PartitionType\t%s", smallbuff, ROADGRAPH_FIELDSIZE);
		if (PartitionTypeLUT.FindIndex(&ptype, smallbuff) == false)
		{
			fclose(RGFile);
			delete [] buff;

			printf("Error: partition %s is invalid.\n", ptid);
			return;
		}

		//read in whether the partition is sparse or not
		GetNextLine(RGFile, buffsize, buff);
		sscanf_s(buff, "Sparse\t%s", smallbuff, ROADGRAPH_FIELDSIZE);
		if (strcmp(smallbuff, "True") == 0)
		{
			//partition is sparse
			issparse = true;
		}
		else
		{
			//partition is not sparse
			issparse = false;
		}

		//read in fit type
		GetNextLine(RGFile, buffsize, buff);
		sscanf_s(buff, "FitType\t%s", smallbuff, ROADGRAPH_FIELDSIZE);
		if (FitTypeLUT.FindIndex(&ftype, smallbuff) == false)
		{
			fclose(RGFile);
			delete [] buff;

			printf("Error: partition %s is invalid.\n", ptid);
			return;
		}

		//read in fit parameters
		switch (ftype)
		{
		case RP_LINE:

			//for a line, the single fit parameter is the angle of the line
			GetNextLine(RGFile, buffsize, buff);
			fparms = new double[1];
			sscanf_s(buff, "FitParameters\t%lg", fparms);
			break;

		case RP_POLYGON:
			{
				//for a polygon, the fit parameters are the x-y coordinates of the vertices
				GetNextLine(RGFile, buffsize, buff);
				int nparms;
				sscanf_s(buff, "FitParameters\t%d", &nparms);
				if (nparms < 1 || nparms > 250)
				{
					printf("Error: partition %s has %d parameters.\n", ptid, nparms);
					return;
				}
				fparms = new double[2*nparms + 1];
				fparms[0] = (double) nparms;

				//this will hold the pointer to the current place in the string
				char* fploc = strchr(buff, '\t') + 1;;
				for (j = 1; j < 2*nparms + 1; j++)
				{
					//scan in each coordinate component one at a time

					//find the tab that begins the next parameter
					fploc = strchr(fploc, '\t') + 1;
					sscanf_s(fploc, "\t%lg", &(fparms[j]));
				}
			}
			break;
		}

		lw = RP_INVALIDLANEWIDTH;
		if (ptype == RP_LANE)
		{
			//read in lane width
			GetNextLine(RGFile, buffsize, buff);
			sscanf_s(buff, "LaneWidth\t%lg", &lw);
		}
		switch (ptype)
		{
		case RP_LANE:
		case RP_INTERCONNECT:
			if (lw == RP_INVALIDLANEWIDTH)
			{
				lw = PP_DEFAULTLANEWIDTH;
			}
			break;
		}

		//read in left and right boundaries
		GetNextLine(RGFile, buffsize, buff);
		sscanf_s(buff, "LeftBoundary\t%s", smallbuff, ROADGRAPH_FIELDSIZE);
		if (LineTypeLUT.FindIndex(&lltype, smallbuff) == false)
		{
			fclose(RGFile);
			delete [] buff;
			delete [] fparms;

			printf("Error: partition %s has an invalid left boundary.\n", ptid);
			return;
		}
		GetNextLine(RGFile, buffsize, buff);
		sscanf_s(buff, "RightBoundary\t%s", smallbuff, ROADGRAPH_FIELDSIZE);
		if (LineTypeLUT.FindIndex(&rltype, smallbuff) == false)
		{
			fclose(RGFile);
			delete [] buff;
			delete [] fparms;

			printf("Error: partition %s has an invalid right boundary.\n", ptid);
			return;
		}

		//read in the number of points in the partition
		GetNextLine(RGFile, buffsize, buff);
		sscanf_s(buff, "NumberOfPoints\t%d", &np);
		if (np <= 0)
		{
			fclose(RGFile);
			delete [] buff;
			delete [] fparms;

			printf("Error: partition %s has an invalid number of waypoints.\n", ptid);
			return;
		}
		//declare space for the number of points
		ppts = new RoadPoint*[np];
		//read in the points directive
		GetNextLine(RGFile, buffsize, buff);
		//read in the first point
		GetNextLine(RGFile, buffsize, buff);
		for (j = 0; j < np; j++)
		{
			//read in each point

			if (strcmp(buff, "End_Points") == 0)
			{
				break;
			}

			if (PointLUT.FindIndex(&idx, buff) == false)
			{
				fclose(RGFile);
				delete [] buff;
				delete [] fparms;
				delete [] ppts;

				printf("Error: invalid waypoint id in partition %s.\n", ptid);
				return;
			}
			//store a link to this roadpoint
			ppts[j] = &(mRoadPoints[idx]);

			GetNextLine(RGFile, buffsize, buff);
		}
		if (j != np)
		{
			fclose(RGFile);
			delete [] buff;
			delete [] fparms;
			delete [] ppts;

			printf("Error: found %d waypoints in partition %s.\n", j, ptid);
			return;
		}

		//initialize the partition
		mRoadPartitions[i].SetPartitionData(ptid, ptype, issparse, ftype, fparms, lw, lltype, rltype, np, ppts);
		//and store its id in the lookup table
		PartitionLUT.AddEntry(ptid, i);

		//read to the end of the partition
		while (GetNextLine(RGFile, buffsize, buff) == true)
		{
			if (strcmp(buff, "End_Partition") == 0)
			{
				break;
			}
		}

		//read the beginning of the next partition
		if (GetNextLine(RGFile, buffsize, buff) == false)
		{
			break;
		}
	}
	if (i != mNumPartitions)
	{
		//number of partitions didn't match the file
		fclose(RGFile);
		delete [] buff;

		printf("Error: found %d partitions, but RNDF was supposed to have %d.\n", i, mNumPartitions);
		return;
	}
	if (strcmp(buff, "End_Rndf") != 0)
	{
		//number of partitions didn't match the file
		fclose(RGFile);
		delete [] buff;

		printf("Error: found more partitions than RNDF specified.\n");
		return;
	}
	printf("Successfully parsed %d partitions.\n", mNumPartitions);

	//rewind to the beginning of the file
	err = fseek(RGFile, 0L, SEEK_SET);
	if (err != 0)
	{
		fclose(RGFile);
		delete [] buff;

		printf("Error: unable to rewind road graph file.\n");
		return;
	}

	//***4. read in all partitions for their connections

	printf("Parsing connections for partitions...\n");

	int ns;
	RoadPoint** spts;
	int nlap;
	RoadPartition** lapts;
	int nllap;
	RoadPartition** llapts;
	int nrlap;
	RoadPartition** rlapts;
	int nnp;
	RoadPartition** npts;

	while (GetNextLine(RGFile, buffsize, buff) == true)
	{
		if (strcmp(buff, "Partition") == 0)
		{
			//found the first partition
			break;
		}
	}
	for (i = 0; i < mNumPartitions; i++)
	{
		//read in each partition for connections

		if (strcmp(buff, "Partition") != 0)
		{
			fclose(RGFile);
			delete [] buff;

			printf("Error: partition is invalid.\n");
			return;
		}

		//read in the partition ID
		GetNextLine(RGFile, buffsize, buff);
		sscanf_s(buff, "PartitionId\t%s", ptid, ROADGRAPH_FIELDSIZE);

		while (GetNextLine(RGFile, buffsize, buff) == true)
		{
			if (strstr(buff, "NumberOfNearbyStoplines") != NULL)
			{
				break;
			}
		}

		//read in the number of nearby stoplines
		sscanf_s(buff, "NumberOfNearbyStoplines\t%d", &ns);
		if (ns < 0)
		{
			fclose(RGFile);
			delete [] buff;

			printf("Error: partition %s has an invalid number of nearby stoplines.\n", ptid);
			return;
		}
		if (ns > 0)
		{
			//declare space for the number of stoplines
			spts = new RoadPoint*[ns];
			//read past the stoplines directive
			GetNextLine(RGFile, buffsize, buff);
		}
		else
		{
			//for no nearby stoplines, ignore
			spts = NULL;
		}
		for (j = 0; j < ns; j++)
		{
			//read in each stopline

			GetNextLine(RGFile, buffsize, buff);

			if (strcmp(buff, "End_NearbyStoplines") == 0)
			{
				break;
			}

			if (PointLUT.FindIndex(&idx, buff) == false)
			{
				fclose(RGFile);
				delete [] buff;
				delete [] spts;

				printf("Error: invalid stopline id in partition %s.\n", ptid);
				return;
			}
			//store a link to this stopline
			spts[j] = &(mRoadPoints[idx]);
		}
		if (j != ns)
		{
			fclose(RGFile);
			delete [] buff;
			delete [] spts;

			printf("Error: found %d stoplines in partition %s.\n", j, ptid);
			return;
		}
		if (ns > 0)
		{
			//read past the end_stoplines directive
			GetNextLine(RGFile, buffsize, buff);
		}

		//read in the number of lane adjacent partitions
		GetNextLine(RGFile, buffsize, buff);
		sscanf_s(buff, "NumberOfLaneAdjacentPartitions\t%d", &nlap);
		if (nlap < 0)
		{
			fclose(RGFile);
			delete [] buff;
			delete [] spts;

			printf("Error: partition %s has an invalid number of lane adjacent partitions.\n", ptid);
			return;
		}
		if (nlap > 0)
		{
			//declare space for the number of lane adjacent partitions
			lapts = new RoadPartition*[nlap];
			//read through the laneadjacentpartitions directive
			GetNextLine(RGFile, buffsize, buff);
		}
		else
		{
			//for no lane adjacent partitions, ignore
			lapts = NULL;
		}
		for (j = 0; j < nlap; j++)
		{
			//read in each lane adjacent partition
			GetNextLine(RGFile, buffsize, buff);

			if (strcmp(buff, "End_LaneAdjacentPartitions") == 0)
			{
				break;
			}

			if (PartitionLUT.FindIndex(&idx, buff) == false)
			{
				fclose(RGFile);
				delete [] buff;
				delete [] spts;
				delete [] lapts;

				printf("Error: invalid lane adjacent partition id in partition %s.\n", ptid);
				return;
			}
			//store a link to this partition
			lapts[j] = &(mRoadPartitions[idx]);
		}
		if (j != nlap)
		{
			fclose(RGFile);
			delete [] buff;
			delete [] spts;
			delete [] lapts;

			printf("Error: found %d lane adjacent partitions in partition %s.\n", j, ptid);
			return;
		}
		if (nlap > 0)
		{
			//read past the end_adjacentlanepartitions directive
			GetNextLine(RGFile, buffsize, buff);
		}

		//read in the number of left lane adjacent partitions
		GetNextLine(RGFile, buffsize, buff);
		sscanf_s(buff, "NumberOfLeftLaneAdjacentPartitions\t%d", &nllap);
		if (nllap < 0)
		{
			fclose(RGFile);
			delete [] buff;
			delete [] spts;
			delete [] lapts;

			printf("Error: partition %s has an invalid number of left lane adjacent partitions.\n", ptid);
			return;
		}
		if (nllap > 0)
		{
			//declare space for the number of left lane adjacent partitions
			llapts = new RoadPartition*[nllap];
			//read through the leftlaneadjacentpartitions directive
			GetNextLine(RGFile, buffsize, buff);
		}
		else
		{
			//for no left lane adjacent parttions, ignore
			llapts = NULL;
		}
		for (j = 0; j < nllap; j++)
		{
			//read in each left lane adjacent partition
			GetNextLine(RGFile, buffsize, buff);

			if (strcmp(buff, "End_LeftLaneAdjacentPartitions") == 0)
			{
				break;
			}

			if (PartitionLUT.FindIndex(&idx, buff) == false)
			{
				fclose(RGFile);
				delete [] buff;
				delete [] spts;
				delete [] lapts;
				delete [] llapts;

				printf("Error: invalid left lane adjacent partition id in partition %s.\n", ptid);
				return;
			}
			//store a link to this partition
			llapts[j] = &(mRoadPartitions[idx]);
		}
		if (j != nllap)
		{
			fclose(RGFile);
			delete [] buff;
			delete [] spts;
			delete [] lapts;
			delete [] llapts;

			printf("Error: found %d left lane adjacent partitions in partition %s.\n", j, ptid);
			return;
		}
		if (nllap > 0)
		{
			//read past the end_leftlaneadjacentpartitions directive
			GetNextLine(RGFile, buffsize, buff);
		}

		//read in the number of right lane adjacent partitions
		GetNextLine(RGFile, buffsize, buff);
		sscanf_s(buff, "NumberOfRightLaneAdjacentPartitions\t%d", &nrlap);
		if (nrlap < 0)
		{
			fclose(RGFile);
			delete [] buff;
			delete [] spts;
			delete [] lapts;
			delete [] llapts;

			printf("Error: partition %s has an invalid number of right lane adjacent partitions.\n", ptid);
			return;
		}
		if (nrlap > 0)
		{
			//declare space for the number of right lane adjacent partitions
			rlapts = new RoadPartition*[nrlap];
			//read through the rightlaneadjacentpartitions directive
			GetNextLine(RGFile, buffsize, buff);
		}
		else
		{
			//for no rightlane adjacent partitions, ignore
			rlapts = NULL;
		}
		for (j = 0; j < nrlap; j++)
		{
			//read in each right lane adjacent partition
			GetNextLine(RGFile, buffsize, buff);

			if (strcmp(buff, "End_RightLaneAdjacentPartitions") == 0)
			{
				break;
			}

			if (PartitionLUT.FindIndex(&idx, buff) == false)
			{
				fclose(RGFile);
				delete [] buff;
				delete [] spts;
				delete [] lapts;
				delete [] llapts;
				delete [] rlapts;

				printf("Error: invalid right lane adjacent partition id in partition %s.\n", ptid);
				return;
			}
			//store a link to this partition
			rlapts[j] = &(mRoadPartitions[idx]);
		}
		if (j != nrlap)
		{
			fclose(RGFile);
			delete [] buff;
			delete [] spts;
			delete [] lapts;
			delete [] llapts;
			delete [] rlapts;

			printf("Error: found %d right lane adjacent partitions in partition %s.\n", j, ptid);
			return;
		}
		if (nrlap > 0)
		{
			//read past the end_rightlaneadjacentpartitions directive
			GetNextLine(RGFile, buffsize, buff);
		}

		//read in the number of nearby partitions
		GetNextLine(RGFile, buffsize, buff);
		sscanf_s(buff, "NumberOfNearbyPartitions\t%d", &nnp);
		if (nnp < 0)
		{
			fclose(RGFile);
			delete [] buff;
			delete [] spts;
			delete [] lapts;
			delete [] llapts;
			delete [] rlapts;

			printf("Error: partition %s has an invalid number of nearby partitions.\n", ptid);
			return;
		}
		if (nnp > 0)
		{
			//declare space for the number of nearby partitions
			npts = new RoadPartition*[nnp];
			//read through the nearbypartitions directive
			GetNextLine(RGFile, buffsize, buff);
		}
		else
		{
			//for no nearby partitions, ignore
			npts = NULL;
		}
		for (j = 0; j < nnp; j++)
		{
			//read in each nearby partition
			GetNextLine(RGFile, buffsize, buff);

			if (strcmp(buff, "End_NearbyPartitions") == 0)
			{
				break;
			}

			if (PartitionLUT.FindIndex(&idx, buff) == false)
			{
				fclose(RGFile);
				delete [] buff;
				delete [] spts;
				delete [] lapts;
				delete [] llapts;
				delete [] rlapts;
				delete [] npts;

				printf("Error: invalid nearby partition id in partition %s.\n", ptid);
				return;
			}
			//store a link to this partition
			npts[j] = &(mRoadPartitions[idx]);
		}
		if (j != nnp)
		{
			fclose(RGFile);
			delete [] buff;
			delete [] spts;
			delete [] lapts;
			delete [] llapts;
			delete [] rlapts;
			delete [] npts;

			printf("Error: found %d nearby partitions in partition %s.\n", j, ptid);
			return;
		}
		if (nnp > 0)
		{
			//read past the end_nearbypartitions directive
			GetNextLine(RGFile, buffsize, buff);
		}

		//set this partition's connection data
		if (PartitionLUT.FindIndex(&idx, ptid) == false)
		{
			fclose(RGFile);
			delete [] buff;
			delete [] spts;
			delete [] lapts;
			delete [] llapts;
			delete [] rlapts;
			delete [] npts;

			printf("Error: invalid partition id in partition %s.\n", j, ptid);
			return;
		}

		mRoadPartitions[idx].SetPartitionConnections(ns, spts, nlap, lapts, nllap, llapts, nrlap, rlapts, nnp, npts);

		//read to the end of the partition
		while (GetNextLine(RGFile, buffsize, buff) == true)
		{
			if (strcmp(buff, "End_Partition") == 0)
			{
				break;
			}
		}

		//read the beginning of the next partition
		if (GetNextLine(RGFile, buffsize, buff) == false)
		{
			break;
		}
	}

	if (strcmp(buff, "End_Rndf") != 0)
	{
		//number of partitions didn't match the file
		fclose(RGFile);
		delete [] buff;

		printf("Error: found more partitions than RNDF specified.\n");
		return;
	}

	printf("Successfully parsed connections for partitions.\n", mNumPartitions);

	//***5. assign point membership to each partition

	printf("Parsing partition memberships for waypoints...\n");

	//rewind to the beginning of the file
	err = fseek(RGFile, 0L, SEEK_SET);
	if (err != 0)
	{
		fclose(RGFile);
		delete [] buff;

		printf("Error: unable to rewind road graph file.\n");
		return;
	}

	//read waypoints for member partitions
	int nmp;
	RoadPartition** mp = NULL;

	while (GetNextLine(RGFile, buffsize, buff) == true)
	{
		if (strcmp(buff, "Waypoints") == 0)
		{
			//found the waypoints
			break;
		}
	}
	GetNextLine(RGFile, buffsize, buff);
	for (i = 0; i < mNumPoints; i++)
	{
		//read the waypoint id for identification

		//scan in the waypoint data
		sscanf_s(buff, "%s\t%s\t%lg\t%lg", wpid, ROADGRAPH_FIELDSIZE, isstop, ROADGRAPH_FIELDSIZE, &east, &north);

		GetNextLine(RGFile, buffsize, buff);
		//read the number of member partitions for this waypoint
		sscanf_s(buff, "NumberOfMemberPartitions\t%d", &nmp);

		if (nmp <= 0)
		{
			fclose(RGFile);
			delete [] buff;

			printf("Error: invalid number of member partitions in waypoint %s.\n", wpid);
			return;
		}

		//declare memory for member partitions
		mp = new RoadPartition*[nmp];
		//read through the MemberPartitions directive
		GetNextLine(RGFile, buffsize, buff);
		for (j = 0; j < nmp; j++)
		{
			//read the next member partition
			GetNextLine(RGFile, buffsize, buff);
			//look up the member partition
			if (PartitionLUT.FindIndex(&idx, buff) == false)
			{
				fclose(RGFile);
				delete [] buff;
				delete [] mp;

				printf("Error: invalid member partition in waypoint %s.\n", wpid);
				return;
			}
			//store the member partition
			mp[j] = &(mRoadPartitions[idx]);
		}
		//read the EndMemberPartitions directive
		GetNextLine(RGFile, buffsize, buff);
		if (strcmp(buff, "EndMemberPartitions") != 0)
		{
			fclose(RGFile);
			delete [] buff;
			delete [] mp;

			printf("Error: incorrect number of member partitions found for waypoint %s.\n", wpid);
			return;
		}
		if (j != nmp)
		{
			fclose(RGFile);
			delete [] buff;
			delete [] mp;

			printf("Error: incorrect number of member partitions found for waypoint %s.\n", wpid);
			return;
		}

		//find the waypoint that's getting member partitions
		if (PointLUT.FindIndex(&idx, wpid) == false)
		{
			fclose(RGFile);
			delete [] buff;
			delete [] mp;

			printf("Error: invalid waypoint id %s.\n", ptid);
			return;
		}

		//store the waypoint connections
		mRoadPoints[idx].SetMemberPartitions(nmp, mp);

		//read the beginning of the next waypoint
		GetNextLine(RGFile, buffsize, buff);
	}
	if (strcmp(buff, "End_Waypoints") != 0)
	{
		fclose(RGFile);
		delete [] buff;

		printf("Error: encountered incorrect number of member partitions for waypoints.\n");
		return;
	}

	printf("Successfully parsed partition memberships for waypoints.\n");

	fclose(RGFile);

	printf("Computing road graph CRC...\n");
	if (ComputeCRC(mCRC, iRoadGraphFileName) == false)
	{
		printf("Error: could not compute road graph CRC...\n");
		return;
	}
	printf("Successfully computed road graph CRC.\n");

	printf("Loading road cache...\n");
	if (mRoadCache.LoadRoadCacheFromFile(mRoadGraphFileName, mCRC, mNumPartitions, mRoadPartitions) == false)
	{
		printf("Could not find road cache for \"%s,\" calculating from scratch...\n", mRoadGraphFileName);

		if (mRoadCache.LoadRoadCacheFromScratch(iNumCacheRows, iNumCacheCols, mRoadGraphFileName, mCRC, mNumPoints, 
			mRoadPoints, mNumPartitions, mRoadPartitions) == false)
		{
			printf("Warning: could not load road cache.\n");
		}
		else
		{
			printf("Road cache loaded.\n");
		}
	}
	else
	{
		printf("Road cache loaded.\n");
	}

	printf("Road graph \"%s\" loaded.\n\n", iRoadGraphFileName);
	mIsValid = true;

	//free memory from the file buffer
	delete [] buff;

	return;
}

RoadGraph::~RoadGraph(void)
{
	/*
	Default destructor for the road graph.  Frees any memory allocated
	for the road graph.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	//free memory associated with the road points and the road edges
	delete [] mRoadPoints;
	delete [] mRoadPartitions;

	return;
}

RoadPartition* RoadGraph::ClosestPartition(double iEast, double iNorth, RoadPartition* iBasePartition, double iOffGraphDistance)
{
	/*
	Given a particular EN, returns the closest partition among a set of neighboring partitions

	INPUTS:
		iEast, iNorth - EN position of interest
		iBasePartition - the base road partition.  The neighborhood of this partition is searched
			to find the closest partition.  If no base partition is supplied, the entire road
			graph is searched for the closest partition.
		iOffGraphDistance - distance threshold for searching the entire road graph.  If the desired
			position is farther than this threshold from the nearest partition, the entire graph
			is searched.

	OUTPUTS:
		rCPartition - returns a pointer to the closest partition.
	*/

	int i;
	int nrp;
	double dbest;
	double dcur;
	RoadPartition* rpbest;
	RoadPartition* rpcur;
	RoadPartition* rCPartition;

	//compute best location on the RNDF
	if (iBasePartition != NULL)
	{
		//only need to search a neighborhood of the road graph

		nrp = iBasePartition->NumNearbyPartitions();
		dbest = DBL_MAX;
		rpbest = NULL;
		dcur;
		RoadPartition* rpcur;

		for (i = 0; i < nrp; i++)
		{
			//compute distance to each partition in the neighborhood from MMSE EN position
			rpcur = iBasePartition->NearbyPartition(i);
			dcur = rpcur->DistanceToPartition(iEast, iNorth);

			if (dcur < dbest)
			{
				//keep track of the best road location
				dbest = dcur;
				rpbest = rpcur;
			}
		}

		if (dbest > iOffGraphDistance)
		{
			#ifdef SE_COMMONDEBUGMSGS
				printf("Warning: closest partition is %.12lg m away.  Searching entire RNDF...\n", dbest);
			#endif
			//search the entire RNDF for a new closest position if we're too far away
			rpbest = ClosestPartition(iEast, iNorth);
		}

		//update the road location
		rCPartition = rpbest;
	}
	else
	{
		//need to search the entire road graph for the closest partition

		//attempt to get the closest partition from the road cache
		rCPartition = mRoadCache.ClosestPartition(iEast, iNorth);

		if (rCPartition == NULL)
		{
			//off the road cache, have to brute force it

			nrp = mNumPartitions;
			dbest = DBL_MAX;
			rpbest = NULL;
			dcur;
			rpcur;

			for (i = 0; i < nrp; i++)
			{
				//compute distance to each partition from MMSE EN position
				rpcur = &(mRoadPartitions[i]);
				dcur = rpcur->DistanceToPartition(iEast, iNorth);

				if (dcur < dbest)
				{
					//keep track of the best road location
					dbest = dcur;
					rpbest = rpcur;
				}
			}

			rCPartition = rpbest;
		}
	}

	return rCPartition;
}

RoadPoint* RoadGraph::ClosestUpcomingStopline(double iEast, double iNorth, double iHeading, double iMaxAngle, bool iUseDirectionality, RoadPartition* iBasePartition)
{
	/*
	Given a particular ENH viewpoint, returns the closest upcoming stopline to that viewpoint.
	Upcoming stoplines are those that are visible to that viewpoint, i.e. in front of it.

	INPUTS:
		iEast, iNorth, iHeading - defines the viewpoint of interest
		iMaxAngle - maximum angle to consider a stopline waypoint being "in front of" the viewpoint
		iUseDirectionality - true if the closest stopline is to be returned only from lanes 
			with the same sense of direction as the viewpoint, false if any stoplines are to 
			be returned.
		iBasePartition - the partition that defines the neighborhood to search for stoplines

	OUTPUTS:
		rRPbest - returns a pointer to the road point that is the best matching stopline.  If no
			stopline matches in the supplied neighborhood, NULL is returned
	*/

	if (iBasePartition == NULL)
	{
		//can't do anything with no base neighborhood to search from
		return NULL;
	}

	int i;
	int j;
	int ns = iBasePartition->NumNearbyStoplines();
	RoadPoint* rptcur;

	double dbest = DBL_MAX;
	RoadPoint* rRPbest = NULL;
	double dcur;

	for (i = 0; i < ns; i++)
	{
		//search over all nearby stoplines for the closest

		rptcur = iBasePartition->NearbyStopline(i);
		if (rptcur->IsStop())
		{
			//this point is a stopline

			if (iUseDirectionality == true)
			{
				//test whether the stopline is a member of any partitions in the same direction

				int np;
				bool samedir = false;
				RoadPartition* rpcur;

				np = rptcur->NumMemberPartitions();
				for (j = 0; j < np; j++)
				{
					rpcur = rptcur->GetMemberPartition(j);

					if (rpcur->IsInSameDirection(iEast, iNorth, iHeading) == true)
					{
						//found a partition in the same direction
						samedir = true;
						break;
					}
				}

				if (samedir == false)
				{
					//couldn't find a partition with the same direction
					continue;
				}
			}

			//check to see if this point is ahead of the viewpoint
			double de = rptcur->East() - iEast;
			double dn = rptcur->North() - iNorth;
			double ptangl = atan2(dn, de);
			//compute the viewing angle relative to the input view
			double viewangl = UnwrapAngle(ptangl - iHeading);
			if (fabs(viewangl) < iMaxAngle)
			{
				//the stopline is at least in front of the viewpoint

				//compute distance to the stopline
				dcur = sqrt(de*de + dn*dn);

				if (dcur < dbest)
				{
					//found a better stopline candidate
					rRPbest = rptcur;
					dbest = dcur;
				}
			}
		}
	}

	return rRPbest;
}

bool RoadGraph::LocalRoadRepresentation(double* oCurvature, double* oCurvatureVar, 
		double iEast, double iNorth, double iHeading, RoadPartition* iClosestPartition)
{
	/*
	Computes and returns the local road representation with respect to the given viewpoint

	INPUTS:
		oCurvature - the returned road parameterization
		oCurvatureVar - the variances on the road parameters
		iEast, iNorth, iHeading - the viewpoint of interest.  The road representation will
			be given with respect to this viewpoint
		iClosestPartition - the closest partition to the viewpoint

	OUTPUTS:
		oHeading, oCurvature - will be filled with the road parameterization
		oHeadingVar, oCurvatureVar - will be filled with the variances on the parameters
		rIsValid - return value is true if the model is valid, false otherwise
	*/

	//initialize to dummy values
	*oCurvature = 0.0;
	*oCurvatureVar = 0.0;
	bool rIsValid = false;

	/*
	//compute heading and heading variance
	switch (iClosestPartition->PartitionType())
	{
	case RP_LANE:
	case RP_INTERCONNECT:
		{
			//for lanes and interconnects, the road heading is just the instantaneous heading

			if (iClosestPartition->IsInSameDirection(iEast, iNorth, iHeading) == true)
			{
				//lane goes in same direction as vehicle
				*oHeading = WrapAngle(iClosestPartition->RoadHeading(iEast, iNorth) - iHeading);
			}
			else
			{
				//lane goes in opposite direction of vehicle
				*oHeading = WrapAngle(PI + iClosestPartition->RoadHeading(iEast, iNorth) - iHeading);
			}

			*oHeadingVar = 0.0;
		}

		break;

	case RP_ZONE:

		//zones don't have a valid road model
		return rIsValid;
		break;
	}
	*/

	//compute curvature and curvature variance
	switch (iClosestPartition->PartitionType())
	{
	case RP_LANE:
	case RP_INTERCONNECT:
		{
			//for lanes and interconnects, do a 3-point least squares fit for curvature

			switch (iClosestPartition->FitType())
			{
			case RP_LINE:
				RoadPoint* pt1;
				RoadPoint* pt2;
				RoadPoint* pt3;
				RoadPartition* rpcur;

				if (iClosestPartition->IsInSameDirection(iEast, iNorth, iHeading) == true)
				{
					//road is in the same direction as the viewpoint
					pt1 = iClosestPartition->GetPoint(0);
					pt2 = iClosestPartition->GetPoint(1);

					//get the next road partition
					rpcur = iClosestPartition->NextPartitionInLane();
					if (rpcur == NULL)
					{
						//can't compute a curvature
						*oCurvature = 0.0;
						*oCurvatureVar = DBL_MAX;

						rIsValid = true;
						break;
					}
					//pull the last fit point as the end of the next partition
					pt3 = rpcur->GetPoint(1);
				}
				else
				{
					pt1 = iClosestPartition->GetPoint(1);
					pt2 = iClosestPartition->GetPoint(0);

					//get the next road partition
					rpcur = iClosestPartition->PreviousPartitionInLane();
					if (rpcur == NULL)
					{
						//can't compute a curvature
						*oCurvature = 0.0;
						*oCurvatureVar = DBL_MAX;
						rIsValid = true;

						break;
					}
					//pull the last fit point as the beginning of the previous partition
					pt3 = rpcur->GetPoint(0);
				}

				//if code gets here, all the points are populated and ready to be fit
				//project all points onto the pt1 - pt2 axis
				double de = pt2->East() - pt1->East();
				double dn = pt2->North() - pt1->North();
				double dlen = sqrt(de*de + dn*dn);

				if (fabs(dlen) == 0.0)
				{
					//can't compute a curvature for the degenerate point-on-point case
					break;
				}

				double x1 = 0.0;
				double y1 = 0.0;
				double x2 = dlen;
				double y2 = 0.0;
				double x3 = ((pt3->East() - pt1->East())*de + (pt3->North() - pt1->North())*dn) / dlen;
				double y3 = -((pt3->East() - pt1->East())*dn + (pt3->North() - pt1->North())*de) / dlen;

				//NOTE: need to scale this by the RNDF variance somehow...
				*oCurvatureVar = 1.0 / (pow(x1, 4.0) + pow(x2, 4.0) + pow(x3, 4.0));
				*oCurvature = (x1*x1*y1 + x2*x2*y2 + x3*x3*y3) * (*oCurvatureVar);

				break;
			}
		}

		rIsValid = true;

		break;
	}

	return rIsValid;
}

void RoadGraph::TestRoadGraph()
{
	/*
	Performs tests on the road graph

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	int i;
	int j;

	printf("\n***\n");
	printf("Starting road graph test...\n");

	//declare the timer for testing
	TIMER RGtimer;

	//1. statistics
	printf("Road Graph for RNDF: %s.\n", mRNDFName);
	printf("Lat. origin: %lgo, Lon. origin: %lgo.\n", mLatOrigin*180.0/PI, mLonOrigin*180.0/PI);
	printf("%d waypoints, %d partitions.\n", mNumPoints, mNumPartitions);

	//determine the range of the RNDF in E-N
	int npt = mNumPoints;
	double maxE = -DBL_MAX;
	double minE = DBL_MAX;
	double maxN = -DBL_MAX;
	double minN = DBL_MAX;
	for (i = 0; i < npt; i++)
	{
		double ecur = mRoadPoints[i].East();
		double ncur = mRoadPoints[i].North();

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
	double spanE = fabs(maxE - minE);
	double spanN = fabs(maxN - minN);

	//create sample east and north points to test
	int n = 1000000;
	double* teste = new double[n];
	double* testn = new double[n];
	double* testh = new double[n];
	for (i = 0; i < n; i++)
	{
		teste[i] = spanE * (((double) rand()) / ((double) RAND_MAX)) + minE;
		testn[i] = spanN * (((double) rand()) / ((double) RAND_MAX)) + minN;
		testh[i] = TWOPI * (((double) rand()) / ((double) RAND_MAX) - 0.5);
	}
	printf("East span: %lgm, North span: %lgm.\n", spanE, spanN);

	//2. partition check
	printf("->Testing partitions...\n");
	int np = mNumPartitions;
	for (i = 0; i < np; i++)
	{
		int nnby = mRoadPartitions[i].NumNearbyPartitions();
		StringIntLUT NeighborPartitions;
		if (nnby <= 1)
		{
			printf("Warning: partition %s has %d nearby partition(s).\n", mRoadPartitions[i].PartitionID(), nnby);
		}
		for (j = 0; j < nnby; j++)
		{
			NeighborPartitions.AddEntry(mRoadPartitions[i].NearbyPartition(j)->PartitionID(), j);
		}
		nnby = NeighborPartitions.NumEntries();
		if (nnby <= 1)
		{
			printf("Warning: partition %s has %d unique nearby partition(s).\n", mRoadPartitions[i].PartitionID(), nnby);
		}
		if (mRoadPartitions[i].NumPoints() <= 1)
		{
			printf("Warning: partition %s has %d point(s).\n", mRoadPartitions[i].PartitionID(), mRoadPartitions[i].NumPoints());
		}
	}
	printf("Partition test completed.\n");

	/*
	int foo = 1.3;
	for (j = 0; j < mNumPartitions; j++)
	{
		RoadPartition* rpcur = &(mRoadPartitions[j]);
		for (i = 0; i < n; i++)
		{
			if (rpcur->FitType() == RP_POLYGON)
			{
				rpcur->IsOnPartition(teste[i], testn[i]);

				//int npp;
				//double* opp;
				//rpcur->PartitionPolygon(npp, opp);
				//delete [] opp;
			}
		}
	}
	*/

	//3. closest partition algorithm
	printf("->Testing closest partition algorithm...\n");
	RGtimer = getTime();
	try
	{
		for (i = 0; i < n; i++)
		{
			ClosestPartition(teste[i], testn[i]);
		}
	}
	catch (...)
	{
		printf("Failed closest partition test at E:%.12lg, N:%.12lg.\n", teste[i], testn[i]);
	}
	printf("Average time to find closest partition: %.12lg ms.\n", timeElapsed(RGtimer)*1000.0 / ((double) n));
	printf("Closest partition algorithm test completed.\n");

	//4. closest stopline
	printf("->Testing closest stopline algorithm...\n");
	RGtimer = getTime();
	try
	{
		for (i = 0; i < n; i++)
		{
			RoadPartition* rpcur = ClosestPartition(teste[i], testn[i]);
			ClosestUpcomingStopline(teste[i], testn[i], testh[i], PIOTWO, true, rpcur);
		}
	}
	catch (...)
	{
		printf("Failed closest partition test at E:%.12lg, N:%.12lg, H:%.12lgo.\n", teste[i], testn[i], testh[i]*180.0/PI);
	}
	printf("Average time to find closest stopline: %.12lg ms.\n", timeElapsed(RGtimer)*1000.0 / ((double) n));
	printf("Closest stopline algorithm test completed.\n");

	//5. local road representation
	printf("->Testing local road representation algorithm...\n");
	RGtimer = getTime();
	try
	{
		for (i = 0; i < n; i++)
		{
			double oc;
			double ocv;
			RoadPartition* rpcur = ClosestPartition(teste[i], testn[i]);
			LocalRoadRepresentation(&oc, &ocv, teste[i], testn[i], testh[i], rpcur);
		}
	}
	catch (...)
	{
		printf("Failed local road test at E:%.12lg, N:%.12lg, H:%.12lgo.\n", teste[i], testn[i], testh[i]*180.0/PI);
	}
	printf("Average time to find local road representation: %.12lg ms.\n", timeElapsed(RGtimer)*1000.0 / ((double) n));
	printf("Local road representation algorithm test completed.\n");

	//free memory at the end of the test

	delete [] teste;
	delete [] testn;
	delete [] testh;

	printf("Concluding road graph test...\n");
	printf("***\n");

	return;
}
