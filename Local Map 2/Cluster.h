#ifndef CLUSTER_H
#define CLUSTER_H

struct Cluster
{
	//A cluster structure used to store a cluster of points.  Only requirement
	//is that Points is NumPoints x ???

	int NumPoints;
	double* Points;
};

#endif //CLUSTER_H
