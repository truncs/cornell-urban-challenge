#ifndef CLUSTER_H
#define CLUSTER_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

struct Cluster
{
	//A cluster structure used to store a cluster of points.  Only requirement
	//is that Points is NumPoints x ???

	int NumPoints;
	double* Points;
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //CLUSTER_H
