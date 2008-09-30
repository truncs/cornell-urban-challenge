#pragma once

#include <string>

//this file provides the methods that will go from the unmanaged C++ code of the scene estimator to the managed publisher
//specifically for the Arbiter communication

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

#define ARBITER_MAX_PARTITIONS 20
using namespace std;

enum PartitionType
{
	PARTITIONTYPE_INVALID = -1,
	PARTITIONTYPE_LANE = 0,
	PARTITIONTYPE_INTERCONNECT = 1,
	PARTITIONTYPE_ZONE = 2

};


struct UnmanagedArbiterPartition
{
	string id;
	PartitionType partitionType;
	double confidence;
	UnmanagedArbiterPartition () {id = "NOID"; partitionType = PARTITIONTYPE_INVALID; confidence = 0;}
	UnmanagedArbiterPartition(char* id, PartitionType partitionType, double confidence)
	{this->id = string(id),this->partitionType = partitionType; this->confidence = confidence;}
};

struct UnmanagedArbiterPositionMessage
{	
	UnmanagedArbiterPositionMessage()
	{
		eastMMSE = 0; northMMSE = 0; headingMMSE = 0; isSparseWaypoints = 0; timestamp = -1; numberPartitions = 0; 
	}

	UnmanagedArbiterPositionMessage(double e, double n, double h, double* ENCovariance, UnmanagedArbiterPartition* partitions, int numberPartitions, double isSparseWaypoints, double timestamp)
	{
		eastMMSE = e; northMMSE = n; headingMMSE = h; 
		for (int i=0; i<4; i++)	this->ENCovariance[i] = ENCovariance[i];
		if (numberPartitions > ARBITER_MAX_PARTITIONS) numberPartitions = ARBITER_MAX_PARTITIONS;
		for (int i=0; i< numberPartitions; i++)
		{
			this->partitions[i] = partitions[i];
		}
		for (int i=numberPartitions; i<ARBITER_MAX_PARTITIONS; i++)
		{
			this->partitions[i] = UnmanagedArbiterPartition();
		}
		this->isSparseWaypoints = isSparseWaypoints;
		this->timestamp = timestamp;
		this->numberPartitions = numberPartitions;
	}
	double timestamp;
	int numberPartitions;
	double isSparseWaypoints;
	double eastMMSE;
	double northMMSE;
	double headingMMSE;	
	double ENCovariance[4]; 
	UnmanagedArbiterPartition partitions[ARBITER_MAX_PARTITIONS];
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif
