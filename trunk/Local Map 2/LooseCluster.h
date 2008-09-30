#ifndef LOOSECLUSTER_H
#define LOOSECLUSTER_H

struct LooseCluster
{
	//A cluster structure used to store a cluster of points.
	//Points are stored as an array NumPoints x ???

	bool IsHighObstacle;
	int NumPoints;
	double* Points;
};

#endif //LOOSECLUSTER_H
