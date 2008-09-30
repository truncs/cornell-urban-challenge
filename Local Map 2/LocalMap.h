#ifndef LOCALMAP_H
#define LOCALMAP_H

#include "LocalMapConstants.h"
#include "LocalMapFunctions.h"
#include "LooseCluster.h"
#include "MetaMeasurement.h"
#include "Particle.h"
#include "RadialExistenceGrid.h"
#include "RandomNumberCache.h"
#include "RandomNumberGenerator.h"
#include "Sensor.h"
#include "VehicleOdometry.h"

#include "..\Common\localMapInterface\localMapInterface.h"
#include "OccupancyGrid/OccupancyGridInterface.h"

#include <FLOAT.H>
#include <MATH.H>
#include <STDIO.H>
#include <STRING.H>

//types of associations (max likelihood, create new target, random, ignore all)
#define LM_MAXLIKELIHOOD 1
#define LM_NEWTARGET 2
#define LM_RANDOM 3
#define LM_IGNORE 4

//indicators for birth and clutter associations
#define LM_BIRTH -1
#define LM_CLUTTER -99

//maximum number of clusters
#define LM_MAXCLUSTERS 500
//the unclusterable cluster tag
#define LM_UNCLUSTERED -1

//lengths of character arrays for printing
#define LM_LINESIZE 1024
#define LM_FIELDSIZE 128

class LocalMap
{
private:

	//the current LocalMap timestamp
	double mLocalMapTime;
	//whether the LocalMap has been initialized
	bool mIsInitialized;
	//what type of association to perform in this target
	int mAssociationType;

	//LOOSE (UNUSED OBSTACLE POINTS)
	//number of loose (unused) ibeo clusters
	int mNumLooseIbeoClusters;
	//the loose ibeo clusters (stored as an array of clusters)
	LooseCluster mLooseIbeoClusters[LM_MAXCLUSTERS];
	//the unclusterable cluster
	LooseCluster mLooseUnclusterableIbeoPoints;

	//number of loose (unused) left side SICK clusters
	int mNumLooseDriverSideSickClusters;
	//number of loose (unused) right side SICK clusters
	int mNumLoosePassengerSideSickClusters;
	//number of loose (unused) clustered SICK clusters
	//the loose left side SICK clusters (stored as an array of clusters)
	LooseCluster mLooseDriverSideSickClusters[LM_MAXCLUSTERS];
	//the loose right side SICK clusters (stored as an array of clusters)
	LooseCluster mLoosePassengerSideSickClusters[LM_MAXCLUSTERS];

	int mNumLooseClusteredSickClusters;
	//number of loose (unused) back horizontal SICK clusters
	LooseCluster mLooseClusteredSickClusters[LM_MAXCLUSTERS];
	//the unclusterable cluster
	LooseCluster mLooseUnclusterableClusteredSickPoints;

	//the number of particles in the LocalMap
	int mNumParticles;
	//the array of particles in the LocalMap
	Particle* mParticles;
	//the current most likely particle
	Particle* mMostLikelyParticle;

	//the LocalMap random number generator
	RandomNumberGenerator* mLMGenerator;
	//the cache of uniform random numbers
	RandomNumberCache mUniformCache;

	//TRANSMIT OBJECTS
	//the timestamp of the forward predicted data for transmit
	double mTransmitTime;
	//the particle to transmit
	Particle* mTransmitParticle;
	//number of unused obstacle points to transmit
	int mTransmitNumLoosePoints;
	//the cluster IDs for the unused obstacle points
	int* mTransmitLooseIDs;
	//the heights of each loose point (0 = low obstacle, 1 = high obstacle)
	int* mTransmitLoosePointHeights;
	//the loose obstacle points, as {x, y} pairs
	double* mTransmitLoosePoints;

	double BirthLikelihood(MetaMeasurement* iMeasurement, Particle* iParticle, Sensor* iSensor, VehicleOdometry* iVehicleOdometry);
	double ClutterLikelihood(MetaMeasurement* iMeasurement, Particle* iParticle, Sensor* iSensor, VehicleOdometry* iVehicleOdometry);
	void DeleteLooseIbeoClusters();
	void DeleteLooseDriverSideSickClusters();
	void DeleteLoosePassengerSideSickClusters();
	void DeleteLooseClusteredSickClusters();
	double RandUniform() {return mUniformCache.RandomNumber();}
	void ReplaceUniform() {mUniformCache.ReplaceRandomNumber(mLMGenerator->RandUniform()); return;}
	void Update(double iMeasurementTime, MetaMeasurement* iMeasurement, Sensor* iSensor, VehicleOdometry* iVehicleOdometry);

public:

	LocalMap(int iNumParticles, RandomNumberGenerator* iRandomNumberGenerator, int iAssociationType = LM_RANDOM);
	~LocalMap();

	bool IsInitialized() {return mIsInitialized;}
	double LocalMapTime() {return mLocalMapTime;}
	Particle* MostLikelyParticle() {return mMostLikelyParticle;}
	
	void GenerateTargetsMessage(LocalMapTargetsMsg* oTargetsMessage);
	void GenerateLooseClustersMessage(LocalMapLooseClustersMsg* oLooseClustersMessage);
	void Initialize(double iInitialTime);
	void MaintainTargets();
	void Predict(double iPredictTime, VehicleOdometry* iVehicleOdometry);
	bool PredictForTransmit(double iPredictTime, VehicleOdometry* iVehicleOdometry);
	void PrintLoosePoints(FILE* iLoosePointsFile);
	void PrintTargets(FILE* iTargetFile);
	void Resample();
	void UpdateWithClusteredIbeo(double iMeasurementTime, int iNumIbeoPoints, double* iClusteredIbeoBuff, Sensor* iSensor, VehicleOdometry* iVehicleOdometry);
	void UpdateExistenceWithClusteredIbeo(double iMeasurementTime, int iNumIbeoPoints, double* iClusteredIbeoBuff, Sensor* iClusterSensor, Sensor* iIbeoSensor, VehicleOdometry* iVehicleOdometry);
	void UpdateExistenceWithVelodyneGrid(double iMeasurementTime, OccupancyGridInterface* iVelodyneGrid, Sensor* iSensor, VehicleOdometry* iVehicleOdometry);
	void UpdateWithMobileyeObstacles(double iMeasurementTime, int iNumObstacles, double* iMobileyeBuff, Sensor* iSensor, VehicleOdometry* iVehicleOdometry);
	void UpdateWithRadar(double iMeasurementTime, int iNumObstacles, double* iRadarBuff, Sensor* iSensor, VehicleOdometry* iVehicleOdometry);
	void UpdateWithSideSick(double iMeasurementTime, int iNumDataRows, double* iSickBuff, bool iIsDriverSide, Sensor* iSensor, VehicleOdometry* iVehicleOdometry);
	void UpdateWithClusteredSick(double iMeasurementTime, int iNumSickPoints, double* iClusteredSickBuff, Sensor* iSensor, VehicleOdometry* iVehicleOdometry);
	void UpdateExistenceWithClusteredSick(double iMeasurementTime, int iNumSickPoints, double* iClusteredSickBuff, Sensor* iSensor, VehicleOdometry* iVehicleOdometry);
};

#endif //LOCALMAP_H
