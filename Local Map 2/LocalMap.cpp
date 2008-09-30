#include "LocalMap.h"

LocalMap::LocalMap(int iNumParticles, RandomNumberGenerator* iRandomNumberGenerator, int iAssociationType)
{
	/*
	LocalMap default constructor.  Creates an empty LocalMap and allocates
	memory for its particles.

	INPUTS:
		iNumParticles - the number of particles to use in this LocalMap
		iRandomNumberGenerator - the random number generator that will be used
			to make random decisions in the particle filter
		iAssociationType - the type of association to perform in the LocalMap

	OUTPUTS:
		none.  Declares memory for an empty set of particles.
	*/

	int i;

	mLocalMapTime = -DBL_MAX;
	mIsInitialized = false;
	mAssociationType = iAssociationType;

	if (iNumParticles > 0)
	{
		mNumParticles = iNumParticles;
	}
	else
	{
		printf("Warning: LocalMap created with %d particle(s), defaulting to 1.\n", iNumParticles);
		mNumParticles = 1;
	}

	//declare memory for the particles
	mParticles = new Particle[mNumParticles];
	mMostLikelyParticle = &(mParticles[0]);

	//initialize the set of loose (unused) obstacle points
	mNumLooseIbeoClusters = 0;
	for (i = 0; i < LM_MAXCLUSTERS; i++)
	{
		mLooseIbeoClusters[i].IsHighObstacle = false;
		mLooseIbeoClusters[i].NumPoints = 0;
		mLooseIbeoClusters[i].Points = NULL;
	}

	mLooseUnclusterableIbeoPoints.IsHighObstacle = false;
	mLooseUnclusterableIbeoPoints.NumPoints = 0;
	mLooseUnclusterableIbeoPoints.Points = NULL;

	mNumLooseDriverSideSickClusters = 0;
	mNumLoosePassengerSideSickClusters = 0;
	for (i = 0; i < LM_MAXCLUSTERS; i++)
	{
		mLooseDriverSideSickClusters[i].IsHighObstacle = false;
		mLooseDriverSideSickClusters[i].NumPoints = 0;
		mLooseDriverSideSickClusters[i].Points = NULL;
		mLoosePassengerSideSickClusters[i].IsHighObstacle = false;
		mLoosePassengerSideSickClusters[i].NumPoints = 0;
		mLoosePassengerSideSickClusters[i].Points = NULL;
	}

	mNumLooseClusteredSickClusters = 0;
	for (i = 0; i < LM_MAXCLUSTERS; i++)
	{
		mLooseClusteredSickClusters[i].IsHighObstacle = false;
		mLooseClusteredSickClusters[i].NumPoints = 0;
		mLooseClusteredSickClusters[i].Points = NULL;
	}

	mLooseUnclusterableClusteredSickPoints.IsHighObstacle = false;
	mLooseUnclusterableClusteredSickPoints.NumPoints = 0;
	mLooseUnclusterableClusteredSickPoints.Points = NULL;

	//initialize transmit objects to nulls
	mTransmitParticle = NULL;
	mTransmitNumLoosePoints = 0;
	mTransmitLooseIDs = NULL;
	mTransmitLoosePointHeights = NULL;
	mTransmitLoosePoints = NULL;

	//store the random number generator and cache necessary values
	mLMGenerator = iRandomNumberGenerator;
	while (mUniformCache.AddNumber(mLMGenerator->RandUniform()) == false)
	{
		//cache values until the cache is full
	}

	return;
}

LocalMap::~LocalMap()
{
	/*
	LocalMap destructor.  Frees memory in the LocalMap.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	//free memory allocated in LocalMap
	mNumParticles = 0;
	mIsInitialized = false;
	delete [] mParticles;

	//delete loose cluster data
	DeleteLooseIbeoClusters();
	DeleteLooseDriverSideSickClusters();
	DeleteLoosePassengerSideSickClusters();
	DeleteLooseClusteredSickClusters();

	//free memory allocated in transmit variables
	delete mTransmitParticle;
	delete [] mTransmitLooseIDs;
	delete [] mTransmitLoosePointHeights;
	delete [] mTransmitLoosePoints;

	return;
}

void LocalMap::Predict(double iPredictTime, VehicleOdometry* iVehicleOdometry)
{
	/*
	LocalMap prediction step.  Predicts each particle forward to the desired
	timestamp.

	INPUTS:
		iPredictTime - time to which the LocalMap should be predicted.
		iVehicleOdometry - vehicle odometry structure describing how the
			vehicle has moved during the prediction

	OUTPUTS:
		none.  Predicts all targets in all particles forward to the specified
			time.
	*/

	if (mIsInitialized == false || iVehicleOdometry->IsValid == false)
	{
		//do not predict if the filter hasn't been initialized or odometry is invalid
		return;
	}

	double dt = iPredictTime - mLocalMapTime;
	if (dt <= 0.0)
	{
		//do not predict backwards in time
		return;
	}

	//PREDICT PARTICLES

	int i;
	int j;
	for (i = 0; i < mNumParticles; i++)
	{
		//predict each target forward in each particle

		int nt = mParticles[i].NumTargets();
		if (nt == 0)
		{
			//no predicting to do if the particle is empty
			continue;
		}

		Target* CurrentTarget = mParticles[i].FirstTarget();
		for (j = 0; j < nt; j++)
		{
			//predict each target forward
			CurrentTarget->Predict(dt, iVehicleOdometry);
			CurrentTarget = CurrentTarget->NextTarget;
		}
	}

	//PREDICT LOOSE OBSTACLE POINTS

	for (i = 0; i < mNumLooseIbeoClusters; i++)
	{
		//predict the points in each cluster forward in ego-vehicle coordinates
		PredictPoints(dt, mLooseIbeoClusters[i].NumPoints, mLooseIbeoClusters[i].Points, iVehicleOdometry);
	}
	PredictPoints(dt, mLooseUnclusterableIbeoPoints.NumPoints, mLooseUnclusterableIbeoPoints.Points, iVehicleOdometry);
	for (i = 0; i < mNumLooseDriverSideSickClusters; i++)
	{
		//predict the points in each cluster forward in ego-vehicle coordinates
		PredictPoints(dt, mLooseDriverSideSickClusters[i].NumPoints, mLooseDriverSideSickClusters[i].Points, iVehicleOdometry);
	}
	for (i = 0; i < mNumLoosePassengerSideSickClusters; i++)
	{
		//predict the points in each cluster forward in ego-vehicle coordinates
		PredictPoints(dt, mLoosePassengerSideSickClusters[i].NumPoints, mLoosePassengerSideSickClusters[i].Points, iVehicleOdometry);
	}
	for (i = 0; i < mNumLooseClusteredSickClusters; i++)
	{
		//predict the points in each cluster forward in ego-vehicle coordinates
		PredictPoints(dt, mLooseClusteredSickClusters[i].NumPoints, mLooseClusteredSickClusters[i].Points, iVehicleOdometry);
	}

	//set the LocalMap time to the prediction time after the targets are advanced
	mLocalMapTime = iPredictTime;

	return;
}

bool LocalMap::PredictForTransmit(double iPredictTime, VehicleOdometry* iVehicleOdometry)
{
	/*
	Predicts the most likely particle forward to the current time using vehicle odometry
	and stores it in mTransmitParticle.

	INPUTS:
		iPredictTime - time to predict the particle to
		iVehicleOdometry - vehicle odometry valid over the desired time interval

	OUTPUTS:
		rSuccess - true if prediction was successful, false otherwise.  If successful, 
			it copies mMostLikelyParticle into mTransmitParticle and predicts
			mTransmitParticle forward to the transmit time
	*/

	bool rSuccess = false;

	//check whether the prediction can be done
	if (mIsInitialized == false)
	{
		return rSuccess;
	}
	if (iVehicleOdometry->IsValid == false)
	{
		return rSuccess;
	}

	//PREDICT THE TRANSMIT PARTICLE

	double dt = iPredictTime - mLocalMapTime;
	if (dt < 0.0)
	{
		return rSuccess;
	}

	//delete the transmit particle
	delete mTransmitParticle;
	//copy over the most likely particle
	mTransmitParticle = new Particle();
	mTransmitParticle->CopyParticle(mMostLikelyParticle);

	int i;
	int j;

	int nt = mTransmitParticle->NumTargets();
	Target* CurrentTarget = mTransmitParticle->FirstTarget();
	for (i = 0; i < nt; i++)
	{
		//predict each target forward
		CurrentTarget->Predict(dt, iVehicleOdometry);
		//maintain the target
		CurrentTarget->MaintainTarget();
		//and calculate a set of points for the target (possibly fake points)

		CurrentTarget->PrepareForTransmit();

		CurrentTarget = CurrentTarget->NextTarget;
	}

	//CHECK FOR TARGET DUPLICATION

	#ifdef LM_NODUPLICATION
		int k;

		//first reset each target's occupancy grid
		j = 0;
		Target* jtarget = mTransmitParticle->FirstTarget();
		while (j < nt)
		{
			jtarget->ResetOccupancyGrid();
			jtarget = jtarget->NextTarget;
			j++;
		}

		//next check each target for possible duplication
		j = 0;
		jtarget = mTransmitParticle->FirstTarget();
		while (j < nt && jtarget != NULL)
		{
			//check whether each target is a duplicate of another, 
			//and delete the newer duplicates

			bool deletej = false;
			bool deletek = false;

			double jx = jtarget->X();
			double jy = jtarget->Y();

			k = j + 1;
			Target* ktarget = jtarget->NextTarget;
			while (k < nt && ktarget != NULL)
			{
				//check if the jth and kth targets are similar

				double kx = ktarget->X();
				double ky = ktarget->Y();

				double dst = sqrt(pow(jx - kx, 2.0) + pow(jy - ky, 2.0));
				if (dst <= TARGET_MAXDUPERANGE)
				{
					//jth and kth targets were close enough to check for duplication

					if (jtarget->NumPoints() > 0 && ktarget->NumPoints() > 0)
					{
						//both jth and kth targets have clusters

						//set jth and kth targets' occupancy grids if not already computed
						//also dilate the occupancy grids
						if (jtarget->OccupancyGridIsValid() == false)
						{
							jtarget->SetOccupancyGrid();
							jtarget->DilateOccupancyGrid();
						}
						if (ktarget->OccupancyGridIsValid() == false)
						{
							ktarget->SetOccupancyGrid();
							ktarget->DilateOccupancyGrid();
						}

						//compute overlap percentages of each target
						double jok = jtarget->OverlapPercentage(ktarget->TargetGrid());
						double koj = ktarget->OverlapPercentage(jtarget->TargetGrid());
						if (jok >= TARGET_MINDUPEPCT && koj >= TARGET_MINDUPEPCT)
						{
							//each target overlaps the other with high degree: they're duplicates

							if (jtarget->NumMeasurements() >= ktarget->NumMeasurements())
							{
								//jtarget is the older target: delete ktarget
								deletek = true;
							}
							else
							{
								//ktarget is the older target: delete jtarget
								//NOTE: once jtarget is to be deleted, there's no need to continue
								//testing for duplicates
								deletej = true;
								break;
							}
						}

						//printf("jok=%lg, koj=%lg\n", jok, koj);
					}
				}

				if (deletek == true)
				{
					//iterate and delete the kth target
					Target* oldtarget = ktarget;
					ktarget = ktarget->NextTarget;
					mTransmitParticle->RemoveTarget(oldtarget);
				}
				else
				{
					//iterate without deleting anything
					ktarget = ktarget->NextTarget;
				}

				k++;
			}

			if (deletej == true)
			{
				//iterate and delete the kth target
				Target* oldtarget = jtarget;
				jtarget = jtarget->NextTarget;
				mTransmitParticle->RemoveTarget(oldtarget);
			}
			else
			{
				//iterate without deleting anything
				jtarget = jtarget->NextTarget;
			}

			j++;
		}
	#endif

	//PREDICT LOOSE OBSTACLE POINTS

	//first delete the old transmit objects
	mTransmitNumLoosePoints = 0;
	delete [] mTransmitLoosePoints;
	mTransmitLoosePoints = NULL;
	delete [] mTransmitLooseIDs;
	mTransmitLooseIDs = NULL;
	delete [] mTransmitLoosePointHeights;
	mTransmitLoosePointHeights = NULL;

	int np = 0;
	int npc;

	//first count the total number of loose obstacle points
	for (i = 0; i < mNumLooseIbeoClusters; i++)
	{
		npc = mLooseIbeoClusters[i].NumPoints;
		if (npc >= CLUSTER_MINNUMPOINTS)
		{
			np += npc;
		}
		else
		{
			//do not pass on clusters that are too small
			if (npc > 0)
			{
				printf("Warning: ibeo cluster with %d point(s).\n", npc);
			}
		}
	}

	npc = mLooseUnclusterableIbeoPoints.NumPoints;
	if (npc >= CLUSTER_MINNUMPOINTS)
	{
		np += npc;
	}
	else
	{
		//do not pass on clusters that are too small
		if (npc > 0)
		{
			printf("Warning: unclustered ibeo cluster with %d point(s).\n", npc);
		}
	}

	//NOTE: do not send loose clusters for side SICKs
	/*
	for (i = 0; i < mNumLooseDriverSideSickClusters; i++)
	{
		npc = mLooseDriverSideSickClusters[i].NumPoints;
		if (npc >= CLUSTER_MINNUMPOINTS)
		{
			np += npc;
		}
		else
		{
			//do not pass on clusters that are too small
			if (npc > 0)
			{
				printf("Warning: driver SICK cluster with %d point(s).\n", npc);
			}
		}
	}
	for (i = 0; i < mNumLoosePassengerSideSickClusters; i++)
	{
		npc = mLoosePassengerSideSickClusters[i].NumPoints;
		if (npc >= CLUSTER_MINNUMPOINTS)
		{
			np += npc;
		}
		else
		{
			//do not pass on clusters that are too small
			if (npc > 0)
			{
				printf("Warning: passenger SICK cluster with %d point(s).\n", npc);
			}
		}
	}
	*/

	for (i = 0; i < mNumLooseClusteredSickClusters; i++)
	{
		npc = mLooseClusteredSickClusters[i].NumPoints;
		if (npc >= CLUSTER_MINNUMPOINTS)
		{
			np += npc;
		}
		else
		{
			//do not pass on clusters that are too small
			if (npc > 0)
			{
				printf("Warning: clustered SICK cluster with %d point(s).\n", npc);
			}
		}
	}

	npc = mLooseUnclusterableClusteredSickPoints.NumPoints;
	if (npc >= CLUSTER_MINNUMPOINTS)
	{
		np += npc;
	}
	else
	{
		//do not pass on clusters that are too small
		if (npc > 0)
		{
			printf("Warning: unclustered SICK cluster with %d point(s).\n", npc);
		}
	}

	//declare memory for the new loose points
	mTransmitNumLoosePoints = np;
	if (np > 0)
	{
		mTransmitLoosePoints = new double[2*np];
		mTransmitLooseIDs = new int[np];
		mTransmitLoosePointHeights = new int[np];
	}

	int idx = 0;
	int cid = 0;
	for (i = 0; i < mNumLooseIbeoClusters; i++)
	{
		//copy over the existing loose points
		npc = mLooseIbeoClusters[i].NumPoints;
		if (npc < CLUSTER_MINNUMPOINTS)
		{
			//don't send on any clusters with small numbers of points
			continue;
		}
		for (j = 0; j < npc; j++)
		{
			mTransmitLoosePoints[midx(idx, 0, np)] = mLooseIbeoClusters[i].Points[midx(j, 0, npc)];
			mTransmitLoosePoints[midx(idx, 1, np)] = mLooseIbeoClusters[i].Points[midx(j, 1, npc)];
			mTransmitLooseIDs[idx] = cid;
			if (mLooseIbeoClusters[i].IsHighObstacle == true)
			{
				mTransmitLoosePointHeights[idx] = CLUSTER_HIGHOBSTACLE;
			}
			else
			{
				mTransmitLoosePointHeights[idx] = CLUSTER_LOWOBSTACLE;
			}
			idx++;
		}
		//increment the cluster counter
		cid++;
	}

	npc = mLooseUnclusterableIbeoPoints.NumPoints;
	if (npc >= CLUSTER_MINNUMPOINTS)
	{
		//don't send on any clusters with small numbers of points
		for (j = 0; j < npc; j++)
		{
			mTransmitLoosePoints[midx(idx, 0, np)] = mLooseUnclusterableIbeoPoints.Points[midx(j, 0, npc)];
			mTransmitLoosePoints[midx(idx, 1, np)] = mLooseUnclusterableIbeoPoints.Points[midx(j, 1, npc)];
			mTransmitLooseIDs[idx] = LM_UNCLUSTERED;
			if (mLooseUnclusterableIbeoPoints.IsHighObstacle == true)
			{
				mTransmitLoosePointHeights[idx] = CLUSTER_HIGHOBSTACLE;
			}
			else
			{
				mTransmitLoosePointHeights[idx] = CLUSTER_LOWOBSTACLE;
			}
			idx++;
		}
	}

	//NOTE: do not send side SICK clusters
	/*
	for (i = 0; i < mNumLooseDriverSideSickClusters; i++)
	{
		//copy over the existing loose points
		npc = mLooseDriverSideSickClusters[i].NumPoints;
		if (npc < CLUSTER_MINNUMPOINTS)
		{
			//don't send on any clusters with small numbers of points
			continue;
		}
		for (j = 0; j < npc; j++)
		{
			mTransmitLoosePoints[midx(idx, 0, np)] = mLooseDriverSideSickClusters[i].Points[midx(j, 0, npc)];
			mTransmitLoosePoints[midx(idx, 1, np)] = mLooseDriverSideSickClusters[i].Points[midx(j, 1, npc)];
			mTransmitLooseIDs[idx] = cid;
			if (mLooseDriverSideSickClusters[i].IsHighObstacle == true)
			{
				mTransmitLoosePointHeights[idx] = CLUSTER_HIGHOBSTACLE;
			}
			else
			{
				mTransmitLoosePointHeights[idx] = CLUSTER_LOWOBSTACLE;
			}
			idx++;
		}
		//increment the cluster counter
		cid++;
	}

	for (i = 0; i < mNumLoosePassengerSideSickClusters; i++)
	{
		//copy over the existing loose points
		npc = mLoosePassengerSideSickClusters[i].NumPoints;
		if (npc < CLUSTER_MINNUMPOINTS)
		{
			//don't send on any clusters with small numbers of points
			continue;
		}
		for (j = 0; j < npc; j++)
		{
			mTransmitLoosePoints[midx(idx, 0, np)] = mLoosePassengerSideSickClusters[i].Points[midx(j, 0, npc)];
			mTransmitLoosePoints[midx(idx, 1, np)] = mLoosePassengerSideSickClusters[i].Points[midx(j, 1, npc)];
			mTransmitLooseIDs[idx] = cid;
			if (mLoosePassengerSideSickClusters[i].IsHighObstacle == true)
			{
				mTransmitLoosePointHeights[idx] = CLUSTER_HIGHOBSTACLE;
			}
			else
			{
				mTransmitLoosePointHeights[idx] = CLUSTER_LOWOBSTACLE;
			}
			idx++;
		}
		//increment the cluster counter
		cid++;
	}
	*/

	for (i = 0; i < mNumLooseClusteredSickClusters; i++)
	{
		//copy over the existing loose points
		npc = mLooseClusteredSickClusters[i].NumPoints;
		if (npc < CLUSTER_MINNUMPOINTS)
		{
			//don't send on any clusters with small numbers of points
			continue;
		}
		for (j = 0; j < npc; j++)
		{
			mTransmitLoosePoints[midx(idx, 0, np)] = mLooseClusteredSickClusters[i].Points[midx(j, 0, npc)];
			mTransmitLoosePoints[midx(idx, 1, np)] = mLooseClusteredSickClusters[i].Points[midx(j, 1, npc)];
			mTransmitLooseIDs[idx] = cid;
			if (mLooseClusteredSickClusters[i].IsHighObstacle == true)
			{
				mTransmitLoosePointHeights[idx] = CLUSTER_HIGHOBSTACLE;
			}
			else
			{
				mTransmitLoosePointHeights[idx] = CLUSTER_LOWOBSTACLE;
			}
			idx++;
		}
		//increment the cluster counter
		cid++;
	}

	npc = mLooseUnclusterableClusteredSickPoints.NumPoints;
	if (npc >= CLUSTER_MINNUMPOINTS)
	{
		//don't send on any clusters with small numbers of points
		for (j = 0; j < npc; j++)
		{
			mTransmitLoosePoints[midx(idx, 0, np)] = mLooseUnclusterableClusteredSickPoints.Points[midx(j, 0, npc)];
			mTransmitLoosePoints[midx(idx, 1, np)] = mLooseUnclusterableClusteredSickPoints.Points[midx(j, 1, npc)];
			mTransmitLooseIDs[idx] = LM_UNCLUSTERED;
			if (mLooseUnclusterableClusteredSickPoints.IsHighObstacle == true)
			{
				mTransmitLoosePointHeights[idx] = CLUSTER_HIGHOBSTACLE;
			}
			else
			{
				mTransmitLoosePointHeights[idx] = CLUSTER_LOWOBSTACLE;
			}
			idx++;
		}
	}

	//now predict all the points forward to the transmit time
	PredictPoints(dt, mTransmitNumLoosePoints, mTransmitLoosePoints, iVehicleOdometry);

	//set the timestamp of the transmit
	mTransmitTime = iPredictTime;

	//prediction is successful if code gets here
	rSuccess = true;
	return rSuccess;
}

void LocalMap::Update(double iMeasurementTime, MetaMeasurement* iMeasurement, Sensor* iSensor, VehicleOdometry* iVehicleOdometry)
{
	/*
	Updates the LocalMap with a single MetaMeasurement.  NOTE: assumes that the LocalMap
	is already predicted to the time of the MetaMeasurement.

	INPUTS:	
		iMeasurementTime - the timestamp of the measurement
		iMeasurement - the metameasurement that is to be applied.
		iSensor - the sensor making the measurement
		iVehicleOdometry - the vehicle odometry structure at the time of the measurement

	OUTPUTS:
		none.  Updates the LocalMap with the given MetaMeasurement.
	*/

	if (iVehicleOdometry->IsValid == false || iMeasurement == NULL)
	{
		return;
	}

	int i;
	int j;

	double MaxLogWeight = -DBL_MAX;

	for (i = 0; i < mNumParticles; i++)
	{
		//apply the measurement to each particle

		Particle* CurrentParticle = &(mParticles[i]);
		int nt = CurrentParticle->NumTargets();
		Target* CurrentTarget;

		//1. calculate likelihoods for each target in the particle		
		CurrentTarget = CurrentParticle->FirstTarget();
		double LambdaSum = 0.0;
		for (j = 0; j < nt; j++)
		{
			//compute the likelihood of applying the measurement to this target
			CurrentTarget->Likelihood(iMeasurement, iSensor, iVehicleOdometry);
			//and sum up the total measurement likelihoods
			LambdaSum += CurrentTarget->Lambda();

			//and iterate to the next target
			CurrentTarget = CurrentTarget->NextTarget;
		}

		//2. calculate birth and clutter likelihoods
		double LambdaBirth = BirthLikelihood(iMeasurement, CurrentParticle, iSensor, iVehicleOdometry);
		double LambdaClutter = ClutterLikelihood(iMeasurement, CurrentParticle, iSensor, iVehicleOdometry);
		LambdaSum += LambdaBirth + LambdaClutter;

		//3. update this particle's weight with the mean target likelihood (including the birth likelihood and clutter)
		//temporarily overwrite this particle's weight with its log-likelihood weight
		CurrentParticle->SetWeight(log(CurrentParticle->Weight()) + log(LambdaSum / (((double) CurrentParticle->NumTargets()) + 1.0 + 1.0)));
		//also keep track of the maximum log-likelihood
		if (CurrentParticle->Weight() > MaxLogWeight)
		{
			MaxLogWeight = CurrentParticle->Weight();
			mMostLikelyParticle = CurrentParticle;
		}

		//4. choose an association from the likelihoods

		//first check to see if one of the targets was chosen
		Target* ChosenTarget = NULL;
		int ChosenIdx = LM_CLUTTER;

		switch (mAssociationType)
		{
		case LM_MAXLIKELIHOOD:
			{
				//max likelihood association

				double LambdaMax = -DBL_MAX;
				CurrentTarget = CurrentParticle->FirstTarget();
				for (j = 0; j < nt; j++)
				{
					//check if this target is more likely than the most likely found so far
					if (CurrentTarget->Lambda() > LambdaMax)
					{
						LambdaMax = CurrentTarget->Lambda();
						ChosenIdx = j;
						ChosenTarget = CurrentTarget;
					}

					CurrentTarget = CurrentTarget->NextTarget;
				}

				//check if the birth model is more likely
				if (LambdaMax < LambdaBirth)
				{
					LambdaMax = LambdaBirth;
					ChosenIdx = LM_BIRTH;
					ChosenTarget = NULL;
				}
				//check if the clutter model is more likely
				if (LambdaMax < LambdaClutter)
				{
					LambdaMax = LambdaClutter;
					ChosenIdx = LM_CLUTTER;
					ChosenTarget = NULL;
				}
			}

			break;

		case LM_NEWTARGET:
			{
				//always create a new target
				ChosenIdx = LM_BIRTH;
			}

			break;

		case LM_RANDOM:
			{
				//random association as per the particle filter

				double rval = RandUniform() * LambdaSum;
				double csum = 0.0;

				CurrentTarget = CurrentParticle->FirstTarget();
				for (j = 0; j < nt; j++)
				{
					//calculate the cumulative sum over likelihoods
					csum += CurrentTarget->Lambda();
					if (csum > rval)
					{
						//this target is chosen
						ChosenIdx = j;
						ChosenTarget = CurrentTarget;

						break;
					}

					CurrentTarget = CurrentTarget->NextTarget;
				}

				if (ChosenIdx < 0)
				{
					//none of the targets were chosen

					//evaluate whether the birth model is chosen
					csum += LambdaBirth;

					if (csum > rval)
					{
						ChosenIdx = LM_BIRTH;
					}
					else
					{
						//if no target is chosen and birth model isn't either, this is clutter
						ChosenIdx = LM_CLUTTER;
					}
				}
			}

			break;

		case LM_IGNORE:
			{
				//ignores all measurements as clutter
				ChosenIdx = LM_CLUTTER;
			}

			break;
		}

		//5. now update the chosen target
		if (ChosenIdx == LM_BIRTH)
		{
			//create a new target from the measurement
			Target* NewTarget;
			NewTarget = new Target();
			NewTarget->Initialize(iMeasurement, iSensor, iVehicleOdometry);

			//add the new target to the list of targets in this particle
			CurrentParticle->AddTarget(NewTarget);
			//NOTE: memory for NewTarget is deallocated in the particle
		}
		else if (ChosenIdx >= 0)
		{
			//update the chosen target with the measurement
			ChosenTarget->Update(iMeasurement, iSensor, iVehicleOdometry);
		}
		//NOTE: LM_CLUTTER MEASUREMENTS ARE DROPPED
	}

	//6. now reweight and renormalize particles from log likelihood
	//NOTE: this is better-conditioned numerically than a multiplicative reweighting
	double sumw = 0.0;
	for (i = 0; i < mNumParticles; i++)
	{
		mParticles[i].SetWeight(exp(mParticles[i].Weight() - MaxLogWeight));
		sumw += mParticles[i].Weight();
	}
	//renormalize weights to sum to unity
	double oosumw = 1.0 / sumw;
	for (i = 0; i < mNumParticles; i++)
	{
		mParticles[i].SetWeight(mParticles[i].Weight() * oosumw);
	}

	//7. replenish one uniform random number
	ReplaceUniform();

	return;
}

double LocalMap::BirthLikelihood(MetaMeasurement* iMeasurement, Particle* iParticle, Sensor* iSensor, VehicleOdometry* iVehicleOdometry)
{
	/*
	Calculates the likelihood of a particular measurement being a new target.

	INPUTS:
		iMeasurement - the measurement for which the likelihood will be computed
		iParticle - the particle for which the likelihood is requested
		iSensor - the sensor that generated the measurement
		iVehicleOdometry - the vehicle odometry structure at the time the measurement was taken

	OUTPUTS:
		rLambda - the birth likelihood.
	*/

	double rLambda = 0.0;

	int i;
	int nt;
	double* z = iMeasurement->MeasurementData();

	switch (iMeasurement->MeasurementType())
	{
	case MM_IBEOCLUSTER_00:
		{
			//stable, fully visible ibeo cluster

			//1. model the ccw boundary as uniform over -105 - 105o bearing
			//2. model the cw boundary as uniform over -105 - 105o bearing
			//3. model the closest point as uniform over range 0 - 100m

			rLambda = (1.0/IBEO_RSPAN) * (1.0/IBEO_BSPAN) * (1.0/IBEO_BSPAN);

			double bmin = z[0];
			double bmax = z[1];
			double rmin = z[2];
			bool isoccupied = false;

			nt = iParticle->NumTargets();
			Target* CurrentTarget = iParticle->FirstTarget();
			for (i = 0; i < nt; i++)
			{
				//make sure birth likelihood is low near existing targets
				if (isoccupied == true)
				{
					break;
				}

				//compute the expected measurement for this target and see if it overlaps

				switch (CurrentTarget->TargetType())
				{
				case T_IBEO:
				case T_IBEOMATURE:
				case T_MATURE:
					{
						double zt[3];
						int nx = 3;
						double x[3] = {CurrentTarget->X(), CurrentTarget->Y(), CurrentTarget->Orientation()};
						Cluster iCluster;
						iCluster = CurrentTarget->TargetPointsCluster();
						BcwBccwRminClusterMeasurement(zt, nx, x, iSensor, iVehicleOdometry, &iCluster);

						double bmint = UnwrapAngle(zt[0]);
						double bmaxt = UnwrapAngle(zt[1]);
						double rmint = zt[2];

						if ((bmint >= bmin && bmint <= bmax) || (bmaxt >= bmin && bmaxt <= bmax))
						{
							if (fabs(rmint - rmin) < B_RANGETOL)
							{
								//this target seems to occupy the same space as the metameasurement
								rLambda = 0.0;
								isoccupied = true;
							}
						}
					}

					break;

				case T_MOBILEYE:
				case T_QUASIMATURE:
					{
						double zt[3];
						int nx = 5;
						double x[5] = {CurrentTarget->X(), CurrentTarget->Y(), CurrentTarget->Speed(), CurrentTarget->Heading(), CurrentTarget->Width()};
						double Ht[3*5];
						BcwBccwRminMeasurement(zt, Ht, nx, x, iSensor, iVehicleOdometry, NULL);

						double bmint = UnwrapAngle(zt[0]);
						double bmaxt = UnwrapAngle(zt[1]);
						double rmint = zt[2];

						if ((bmint >= bmin && bmint <= bmax) || (bmaxt >= bmin && bmaxt <= bmax))
						{
							if (fabs(rmint - rmin) < B_RANGETOL)
							{
								//this target seems to occupy the same space as the metameasurement
								rLambda = 0.0;
								isoccupied = true;
							}
						}
					}

					break;

				case T_RADAR:
					{
						double zt[2];
						double Ht[2*4];
						int nx = 4;
						double x[4] = {CurrentTarget->X(), CurrentTarget->Y(), CurrentTarget->Speed(), CurrentTarget->Heading()};

						BcwBccwRminNoWidthMeasurement(zt, Ht, nx, x, iSensor, iVehicleOdometry, NULL);

						double bavgt = UnwrapAngle(zt[0]);
						double rmint = zt[2];

						if (bavgt <= bmax && bavgt >= bmin && fabs(rmint - rmin) < B_RANGETOL)
						{
							//this target seems to occupy the same space as the metameasurement
							rLambda = 0.0;
							isoccupied = true;
						}
					}

					break;
				}

				CurrentTarget = CurrentTarget->NextTarget;
			}
		}

		break;

	//NOTE: MM_IBEOCLUSTER_01 & MM_IBEOCLUSTER_02 can't be used to create new targets.

	case MM_MOBILEYEOBSTACLE:
		{
			//mobileye obstacle measurement

			//model x as uniform over 100m
			//model y as uniform over +-25m
			//model s as uniform over +-26.8224m/s
			//model w as uniform over 0-6m

			//model as uniform in x, y, s, w
			rLambda = (1.0/MOBILEYE_XSPAN) * (1.0/MOBILEYE_YSPAN) * (1.0/MOBILEYE_SSPAN) * (1.0/MOBILEYE_WSPAN);

			double zx = z[0];
			double zy = z[1];
			bool isoccupied = false;

			if (fabs(iVehicleOdometry->vx) < MOBILEYE_MIN_EGOSPEED)
			{
				//do not allow the mobileye to create new targets when vehicle is stopped
				rLambda = 0.0;
				break;
			}
			if (sqrt(zx*zx + zy*zy) < MOBILEYE_MIN_OBSDIST)
			{
				//do not allow the mobileye to create new targets when they are close to the vehicle
				rLambda = 0.0;
				break;
			}

			//do not allow Mobileye to create targets
			rLambda = 0.0;
			break;

			nt = iParticle->NumTargets();
			Target* CurrentTarget = iParticle->FirstTarget();
			for (i = 0; i < nt; i++)
			{
				//make sure birth likelihood is low near existing targets
				if (isoccupied == true)
				{
					break;
				}

				switch (CurrentTarget->TargetType())
				{
				case T_IBEO:
					{
						//NOTE: immature ibeo targets don't use the mobileye's speed
						//measurement, so the likelihood matrices need redefined

						double zt[3];
						int nx = 3;
						double x[3] = {CurrentTarget->X(), CurrentTarget->Y(), CurrentTarget->Orientation()};
						Cluster iCluster;
						iCluster = CurrentTarget->TargetPointsCluster();

						MobileyeNoSpeedClusterMeasurement(zt, nx, x, iSensor, iVehicleOdometry, &iCluster);

						//extract the position from the measurement
						double zxt = zt[0];
						double zyt = zt[1];

						if (fabs(zxt - zx) < B_DISTANCETOL || fabs(zyt - zy) < B_DISTANCETOL)
						{
							//this target seems to occupy the same space as the metameasurement
							rLambda = 0.0;
							isoccupied = true;
						}
					}

					break;

				case T_IBEOMATURE:
				case T_MATURE:
					{
						double zt[4];
						int nx = 5;
						double x[5] = {CurrentTarget->X(), CurrentTarget->Y(), CurrentTarget->Orientation(), CurrentTarget->Speed(), CurrentTarget->Heading()};
						Cluster iCluster;
						iCluster = CurrentTarget->TargetPointsCluster();
						MobileyeClusterMeasurement(zt, nx, x, iSensor, iVehicleOdometry, &iCluster);

						//extract the position from the measurement
						double zxt = zt[0];
						double zyt = zt[1];

						if (fabs(zxt - zx) < B_DISTANCETOL || fabs(zyt - zy) < B_DISTANCETOL)
						{
							//this target seems to occupy the same space as the metameasurement
							rLambda = 0.0;
							isoccupied = true;
						}
					}

					break;

				case T_MOBILEYE:
				case T_QUASIMATURE:
					{
						//compute the expected measurement for this target and see if it overlaps

						double zt[4];
						double Ht[20];
						int nx = 5;
						double xt[5] = {CurrentTarget->X(), CurrentTarget->Y(), CurrentTarget->Speed(),
							CurrentTarget->Heading(), CurrentTarget->Width()};
						MobileyeMeasurement(zt, Ht, nx, xt, iSensor, iVehicleOdometry, NULL);

						//extract the position from the measurement
						double zxt = zt[0];
						double zyt = zt[1];

						if (fabs(zxt - zx) < B_DISTANCETOL || fabs(zyt - zy) < B_DISTANCETOL)
						{
							//this target seems to occupy the same space as the metameasurement
							rLambda = 0.0;
							isoccupied = true;
						}
					}

					break;

				case T_RADAR:
					{
						double zt[3];
						double Ht[3*4];
						int nx = 4;
						double x[4] = {CurrentTarget->X(), CurrentTarget->Y(), CurrentTarget->Speed(), CurrentTarget->Heading()};
						MobileyeNoWidthMeasurement(zt, Ht, nx, x, iSensor, iVehicleOdometry, NULL);

						//extract the position from the measurement
						double zxt = zt[0];
						double zyt = zt[1];

						if (fabs(zxt - zx) < B_DISTANCETOL || fabs(zyt - zy) < B_DISTANCETOL)
						{
							//this target seems to occupy the same space as the metameasurement
							rLambda = 0.0;
							isoccupied = true;
						}
					}

					break;
				}

				CurrentTarget = CurrentTarget->NextTarget;
			}
		}

		break;

	case MM_RADAROBSTACLE:
		{
			//radar obstacle measurement

			//model r as uniform over 254m
			//model b as uniform over +-7.5m
			//model rr as uniform over +-26.8224m/s

			//model as uniform in r, b, rr
			rLambda = (1.0/RADAR_RSPAN) * (1.0/RADAR_BSPAN) * (1.0/RADAR_RRSPAN);

			double zr = z[0];
			double zb = z[1];
			double zx = zr*cos(zb);
			double zy = zr*sin(zb);
			bool isoccupied = false;

			nt = iParticle->NumTargets();
			Target* CurrentTarget = iParticle->FirstTarget();
			for (i = 0; i < nt; i++)
			{
				//make sure birth likelihood is low near existing targets
				if (isoccupied == true)
				{
					break;
				}

				switch (CurrentTarget->TargetType())
				{
				case T_IBEO:
					{
						double zt[2];
						int nx = 3;
						double x[3] = {CurrentTarget->X(), CurrentTarget->Y(), CurrentTarget->Orientation()};
						Cluster iCluster;
						iCluster = CurrentTarget->TargetPointsCluster();
						RangeBearingClusterMeasurement(zt, nx, x, iSensor, iVehicleOdometry, &iCluster);

						//extract the position from the measurement
						double zrt = zt[0];
						double zbt = UnwrapAngle(zt[1]);
						double zxt = zrt*cos(zbt);
						double zyt = zrt*sin(zbt);

						if (fabs(zxt - zx) < B_DISTANCETOL || fabs(zyt - zy) < B_DISTANCETOL)
						{
							//this target seems to occupy the same space as the metameasurement
							rLambda = 0.0;
							isoccupied = true;
						}
					}

					break;

				case T_IBEOMATURE:
				case T_MATURE:
					{
						double zt[3];
						int nx = 5;
						double x[5] = {CurrentTarget->X(), CurrentTarget->Y(), CurrentTarget->Orientation(), CurrentTarget->Speed(), CurrentTarget->Heading()};
						Cluster iCluster;
						iCluster = CurrentTarget->TargetPointsCluster();
						RangeBearingRateClusterMeasurement(zt, nx, x, iSensor, iVehicleOdometry, &iCluster);

						//extract the position from the measurement
						double zrt = zt[0];
						double zbt = UnwrapAngle(zt[1]);
						double zxt = zrt*cos(zbt);
						double zyt = zrt*sin(zbt);

						if (fabs(zxt - zx) < B_DISTANCETOL || fabs(zyt - zy) < B_DISTANCETOL)
						{
							//this target seems to occupy the same space as the metameasurement
							rLambda = 0.0;
							isoccupied = true;
						}
					}

					break;

				case T_MOBILEYE:
				case T_QUASIMATURE:
					{
						double zt[3];
						double Ht[3*5];
						int nx = 5;
						double x[5] = {CurrentTarget->X(), CurrentTarget->Y(), CurrentTarget->Speed(), CurrentTarget->Heading(), CurrentTarget->Width()};
						RadarMeasurement(zt, Ht, nx, x, iSensor, iVehicleOdometry, NULL);

						//extract the position from the measurement
						double zrt = zt[0];
						double zbt = UnwrapAngle(zt[1]);
						double zxt = zrt*cos(zbt);
						double zyt = zrt*sin(zbt);

						if (fabs(zxt - zx) < B_DISTANCETOL || fabs(zyt - zy) < B_DISTANCETOL)
						{
							//this target seems to occupy the same space as the metameasurement
							rLambda = 0.0;
							isoccupied = true;
						}
					}

					break;

				case T_RADAR:
					{
						//compute the expected measurement for this target and see if it overlaps

						double zt[3];
						double Ht[12];
						int nx = 4;
						double xt[4] = {CurrentTarget->X(), CurrentTarget->Y(), CurrentTarget->Speed(), CurrentTarget->Heading()};
						RadarMeasurement(zt, Ht, nx, xt, iSensor, iVehicleOdometry, NULL);

						//extract the position from the measurement
						double zrt = zt[0];
						double zbt = UnwrapAngle(zt[1]);
						double zxt = zrt*cos(zbt);
						double zyt = zrt*sin(zbt);

						if (fabs(zxt - zx) < B_DISTANCETOL || fabs(zyt - zy) < B_DISTANCETOL)
						{
							//this target seems to occupy the same space as the metameasurement
							rLambda = 0.0;
							isoccupied = true;
						}
					}

					break;
				}

				CurrentTarget = CurrentTarget->NextTarget;
			}
		}

		break;

	case MM_SIDESICKOBSTACLE:
		{
			//stable, fully visible ibeo cluster

			//NOTE: do not initialize objects with side SICK
			//NOTE: need to check birth likelihood
			rLambda = 0.0;

			/*
			//1. model the distance as uniform over range 0 - 30m

			rLambda = (1.0/SIDESICK_BRSPAN);

			double mind = z[0];
			bool isoccupied = false;

			nt = iParticle->NumTargets();
			Target* CurrentTarget = iParticle->FirstTarget();
			for (i = 0; i < nt; i++)
			{
				//make sure birth likelihood is low near existing targets
				if (isoccupied == true)
				{
					break;
				}

				//compute the expected measurement for this target and see if it overlaps

				switch (CurrentTarget->TargetType())
				{
				case T_IBEO:
				case T_IBEOMATURE:
				case T_MATURE:
					{
						double zt[1];
						int nx = 3;
						double x[3] = {CurrentTarget->X(), CurrentTarget->Y(), CurrentTarget->Orientation()};
						Cluster iCluster;
						iCluster = CurrentTarget->TargetPointsCluster();
						SensorDirectionalDistanceCluster(zt, nx, x, iSensor, iVehicleOdometry, &iCluster);

						double mindt = zt[0];

						if (CurrentTarget->IsInLineOfSight(iSensor) == true && fabs(mindt - mind) < B_RANGETOL)
						{
							//this target seems to occupy the same space as the metameasurement
							rLambda = 0.0;
							isoccupied = true;
						}
					}

					break;

				case T_MOBILEYE:
				case T_QUASIMATURE:
					{
						double zt[1];
						int nx = 5;
						double x[5] = {CurrentTarget->X(), CurrentTarget->Y(), CurrentTarget->Speed(), CurrentTarget->Heading(), CurrentTarget->Width()};
						double Ht[1*5];
						SensorDirectionalDistance(zt, Ht, nx, x, iSensor, iVehicleOdometry, NULL);

						double mindt = zt[0];

						if (CurrentTarget->IsInLineOfSight(iSensor) == true && fabs(mindt - mind) < B_RANGETOL)
						{
							//this target seems to occupy the same space as the metameasurement
							rLambda = 0.0;
							isoccupied = true;
						}
					}

					break;

				case T_RADAR:
					{
						double zt[1];
						int nx = 4;
						double x[4] = {CurrentTarget->X(), CurrentTarget->Y(), CurrentTarget->Speed(), CurrentTarget->Heading()};
						double Ht[1*4];

						SensorDirectionalDistanceNoWidth(zt, Ht, nx, x, iSensor, iVehicleOdometry, NULL);

						double mindt = zt[0];

						if (CurrentTarget->IsInLineOfSight(iSensor) == true && fabs(mindt - mind) < B_RANGETOL)
						{
							//this target seems to occupy the same space as the metameasurement
							rLambda = 0.0;
							isoccupied = true;
						}
					}

					break;
				}

				CurrentTarget = CurrentTarget->NextTarget;
			}
			*/
		}

		break;
	}

	return rLambda;
}

double LocalMap::ClutterLikelihood(MetaMeasurement* iMeasurement, Particle* iParticle, Sensor* iSensor, VehicleOdometry* iVehicleOdometry)
{
	/*
	Calculates the likelihood of a particular measurement being clutter.

	INPUTS:
		iMeasurement - the measurement for which the likelihood will be computed
		iParticle - the particle for which the likelihood is requested
		iSensor - the sensor that generated the measurement
		iVehicleOdometry - the vehicle odometry structure at the time the measurement was taken

	OUTPUTS:
		rLambda - the clutter likelihood.
	*/

	double rLambda = 0.0;
	double* z = iMeasurement->MeasurementData();
	double* R = iMeasurement->MeasurementCovariance();

	switch (iMeasurement->MeasurementType())
	{
	case MM_IBEOCLUSTER_00:
		{
			//stable, fully visible ibeo cluster

			//1. model the ccw boundary as uniform over -105 - 105o bearing
			//2. model the cw boundary as uniform over -105 - 105o bearing
			//3. model the closest point as gaussian with mean 100m and 20m sigma

			double dR = z[2] - 100.0;
			double varR = 20.0*20.0;

			rLambda = exp(-0.5 * dR*dR/varR) / sqrt(TWOPI * varR) * (1.0/IBEO_BSPAN) * (1.0/IBEO_BSPAN);
		}

		break;

	case MM_IBEOCLUSTER_01:
	case MM_IBEOCLUSTER_02:
		{
			//an ibeo cluster with only one corner visible

			//1. model the visible corner as uniform over -105 - 105o bearing

			rLambda = (1.0/IBEO_BSPAN);
		}

		break;

	case MM_MOBILEYEOBSTACLE:
		{
			//mobileye obstacle measurement

			//model x as multimodal gaussian centered at each visible obstacle's ground x
			//model y as multimodal gaussian centered at each visible obstacle's y
			//model s as multimodal gaussian with center at each visible obstacle's speed
			//model w as multimodal gaussian at each visible obstacle's width

			double zx = z[0];
			double zy = z[1];
			double zs = z[2];
			double zw = z[3];

			int i;
			int nt = iParticle->NumTargets();
			int ntv = 1;
			double xspan = MOBILEYE_XSPAN;
			double yspan = 0.5*MOBILEYE_YSPAN;
			Target* CurrentTarget = iParticle->FirstTarget();

			//initialize clutter with a uniform component
			rLambda = (1.0/MOBILEYE_XSPAN) * (1.0/MOBILEYE_YSPAN) * (1.0/MOBILEYE_SSPAN) * (1.0/MOBILEYE_WSPAN);

			//initialize likelihood gaussian parameters
			double var_x = R[midx(0, 0, 4)];
			double xfact = 1.0 / sqrt(TWOPI * var_x);

			double var_y = R[midx(1, 1, 4)];
			double yfact = 1.0 / sqrt(TWOPI * var_y);

			double var_s = R[midx(2, 2, 4)];
			double sfact = 1.0 / sqrt(TWOPI * var_s);

			double var_w = R[midx(3, 3, 4)];
			double wfact = 1.0 / sqrt(TWOPI * var_w);

			//the scale factor that will scale forward range
			double scfact = (fabs(iSensor->SensorZ) + MOBILEYE_BUMPERHEIGHT) / (fabs(iSensor->SensorZ));

			for (i = 0; i < nt; i++)
			{
				//calculate the mobileye measurement for the target given that mobileye has locked onto
				//the ground instead of the target's bumper

				switch (CurrentTarget->TargetType())
				{
				case T_IBEO:
					{
						double zt[3];
						int nx = 3;
						Cluster iCluster;
						iCluster = CurrentTarget->TargetPointsCluster();
						double xt[3] = {CurrentTarget->X(), CurrentTarget->Y(), CurrentTarget->Orientation()};
						MobileyeNoSpeedClusterMeasurement(zt, nx, xt, iSensor, iVehicleOdometry, &iCluster);

						//extract the measurement
						double zxt = zt[0];
						double zyt = zt[1];
						double zwt = zt[2];

						//calculate the measurement assuming the mobileye locked onto the ground
						zxt *= scfact;

						//construct the mm gaussian component for this target
						double dx = zxt - zx;
						double dy = zyt - zy;
						double dw = zwt - zw;

						double xL = exp(-0.5*dx*dx/var_x) * xfact;
						double yL = exp(-0.5*dy*dy/var_y) * yfact;
						double sL = (1.0/MOBILEYE_SSPAN);
						double wL = exp(-0.5*dw*dw/var_w) * yfact;

						if (zxt >= 0.0 && zxt <= xspan && fabs(zyt) <= yspan)
						{
							//this target should be visible
							rLambda += xL*yL*sL*wL;
							ntv++;
						}
					}

					break;

				case T_IBEOMATURE:
				case T_MATURE:
					{
						double zt[4];
						int nx = 5;
						Cluster iCluster;
						iCluster = CurrentTarget->TargetPointsCluster();
						double xt[5] = {CurrentTarget->X(), CurrentTarget->Y(), CurrentTarget->Orientation(), CurrentTarget->Speed(), CurrentTarget->Heading()};
						MobileyeClusterMeasurement(zt, nx, xt, iSensor, iVehicleOdometry, &iCluster);

						//extract the measurement
						double zxt = zt[0];
						double zyt = zt[1];
						double zst = zt[2];
						double zwt = zt[3];

						//calculate the measurement assuming the mobileye locked onto the ground
						zxt *= scfact;

						//construct the mm gaussian component for this target
						double dx = zxt - zx;
						double dy = zyt - zy;
						double ds = zst - zs;
						double dw = zwt - zw;

						double xL = exp(-0.5*dx*dx/var_x) * xfact;
						double yL = exp(-0.5*dy*dy/var_y) * yfact;
						double sL = exp(-0.5*ds*ds/var_s) * sfact;
						double wL = exp(-0.5*dw*dw/var_w) * wfact;

						if (zxt >= 0.0 && zxt <= xspan && fabs(zyt) <= yspan)
						{
							//this target should be visible
							rLambda += xL*yL*sL*wL;
							ntv++;
						}
					}

					break;

				case T_MOBILEYE:
				case T_QUASIMATURE:
					{
						double zt[4];
						double Ht[20];
						int nx = 5;
						double xt[5] = {CurrentTarget->X(), CurrentTarget->Y(), CurrentTarget->Speed(), CurrentTarget->Heading(), CurrentTarget->Width()};
						MobileyeMeasurement(zt, Ht, nx, xt, iSensor, iVehicleOdometry, NULL);

						//extract the measurement
						double zxt = zt[0];
						double zyt = zt[1];
						double zst = zt[2];
						double zwt = zt[3];

						//calculate the measurement assuming the mobileye locked onto the ground
						zxt *= scfact;

						//construct the mm gaussian component for this target
						double dx = zxt - zx;
						double dy = zyt - zy;
						double ds = zst - zs;
						double dw = zwt - zw;

						double xL = exp(-0.5*dx*dx/var_x) * xfact;
						double yL = exp(-0.5*dy*dy/var_y) * yfact;
						double sL = exp(-0.5*ds*ds/var_s) * sfact;
						double wL = exp(-0.5*dw*dw/var_w) * wfact;

						if (zxt >= 0.0 && zxt <= xspan && fabs(zyt) <= yspan)
						{
							//this target should be visible
							rLambda += xL*yL*sL*wL;
							ntv++;
						}
					}

					break;

				case T_RADAR:
					{
						double zt[3];
						int nx = 4;
						double Ht[12];
						double xt[4] = {CurrentTarget->X(), CurrentTarget->Y(), CurrentTarget->Speed(), CurrentTarget->Heading()};
						MobileyeNoWidthMeasurement(zt, Ht, nx, xt, iSensor, iVehicleOdometry, NULL);

						//extract the measurement
						double zxt = zt[0];
						double zyt = zt[1];
						double zst = zt[2];

						//calculate the measurement assuming the mobileye locked onto the ground
						zxt *= scfact;

						//construct the mm gaussian component for this target
						double dx = zxt - zx;
						double dy = zyt - zy;
						double ds = zst - zs;

						double xL = exp(-0.5*dx*dx/var_x) * xfact;
						double yL = exp(-0.5*dy*dy/var_y) * yfact;
						double sL = exp(-0.5*ds*ds/var_s) * sfact;
						double wL = (1.0/MOBILEYE_WSPAN);

						if (zxt >= 0.0 && zxt <= xspan && fabs(zyt) <= yspan)
						{
							//this target should be visible
							rLambda += xL*yL*sL*wL;
							ntv++;
						}
					}

					break;
				}

				CurrentTarget = CurrentTarget->NextTarget;
			}

			//scale the sum of gaussians by the number of visible targets
			double mmgwt = 1.0 / ((double) ntv);
			rLambda *= mmgwt;
		}

		break;

	case MM_RADAROBSTACLE:
		{
			//radar obstacle measurement

			//model r as multimodal gaussian centered at twice each visible obstacle's range with sigma of 1m
			//model b as multimodal gaussian centered at each visible obstacle's bearing with sigma of 1o
			//model rr as multimodal gaussian centered at each visible obstacle's rr with sigma of 1m/s

			double zr = z[0];
			double zb = z[1];
			double zrr = z[2];

			int i;
			int nt = iParticle->NumTargets();
			int ntv = 1;
			double bspan = 0.5*RADAR_BSPAN;
			Target* CurrentTarget = iParticle->FirstTarget();

			//initialize clutter with a uniform component
			rLambda = (1.0/RADAR_RSPAN) * (1.0/RADAR_BSPAN) * (1.0/RADAR_RRSPAN);

			//initialize likelihood gaussian parameters
			double var_r = R[midx(0, 0, 3)];
			double rfact = 1.0 / sqrt(TWOPI * var_r);

			double var_b = R[midx(1, 1, 3)];
			double bfact = 1.0 / sqrt(TWOPI * var_b);

			double var_rr = R[midx(2, 2, 3)];
			double rrfact = 1.0 / sqrt(TWOPI * var_rr);

			for (i = 0; i < nt; i++)
			{
				//make sure clutter likelihood is high near multipath places from existing targets

				switch (CurrentTarget->TargetType())
				{
				case T_IBEO:
					{
						double zt[2];
						int nx = 3;
						Cluster iCluster;
						iCluster = CurrentTarget->TargetPointsCluster();
						double xt[3] = {CurrentTarget->X(), CurrentTarget->Y(), CurrentTarget->Orientation()};
						RangeBearingClusterMeasurement(zt, nx, xt, iSensor, iVehicleOdometry, &iCluster);

						//extract the measurement
						double zrt = zt[0];
						double zbt = zt[1];

						//construct the mm gaussian component for this target
						double dr = 2.0*zrt - zr;
						double db = zbt - zb;

						double rL = exp(-0.5*dr*dr/var_r) * rfact;
						double bL = exp(-0.5*db*db/var_b) * bfact;
						double rrL = (1.0/RADAR_RRSPAN);

						if (fabs(UnwrapAngle(zbt - iSensor->SensorYaw)) <= bspan)
						{
							//this target should be visible by the radar
							rLambda += rL*bL*rrL;
							ntv++;
						}
					}

				case T_IBEOMATURE:
				case T_MATURE:
					{
						double zt[3];
						int nx = 5;
						Cluster iCluster;
						iCluster = CurrentTarget->TargetPointsCluster();
						double xt[5] = {CurrentTarget->X(), CurrentTarget->Y(), CurrentTarget->Orientation(), CurrentTarget->Speed(), CurrentTarget->Heading()};
						RangeBearingRateClusterMeasurement(zt, nx, xt, iSensor, iVehicleOdometry, &iCluster);

						//extract the measurement
						double zrt = zt[0];
						double zbt = zt[1];
						double zrrt = zt[2];

						//construct the mm gaussian component for this target
						double dr = 2.0*zrt - zr;
						double db = zbt - zb;
						double drr = zrrt - zrr;

						double rL = exp(-0.5*dr*dr/var_r) * rfact;
						double bL = exp(-0.5*db*db/var_b) * bfact;
						double rrL = exp(-0.5*drr*drr/var_rr) * rrfact;

						if (fabs(UnwrapAngle(zbt - iSensor->SensorYaw)) <= bspan)
						{
							//this target should be visible by the radar
							rLambda += rL*bL*rrL;
							ntv++;
						}
					}

					break;

				case T_MOBILEYE:
				case T_QUASIMATURE:
					{
						double zt[3];
						double Ht[15];
						int nx = 5;
						double xt[5] = {CurrentTarget->X(), CurrentTarget->Y(), CurrentTarget->Speed(), CurrentTarget->Heading(), CurrentTarget->Width()};
						RadarMeasurement(zt, Ht, nx, xt, iSensor, iVehicleOdometry, NULL);

						//extract the measurement
						double zrt = zt[0];
						double zbt = zt[1];
						double zrrt = zt[2];

						//construct the mm gaussian component for this target
						double dr = 2.0*zrt - zr;
						double db = zbt - zb;
						double drr = zrrt - zrr;

						double rL = exp(-0.5*dr*dr/var_r) * rfact;
						double bL = exp(-0.5*db*db/var_b) * bfact;
						double rrL = exp(-0.5*drr*drr/var_rr) * rrfact;

						if (fabs(UnwrapAngle(zbt - iSensor->SensorYaw)) <= bspan)
						{
							//this target should be visible by the radar
							rLambda += rL*bL*rrL;
							ntv++;
						}
					}

					break;

				case T_RADAR:
					{
						//compute the measurement for this target and see if it overlaps

						double zt[3];
						double Ht[12];
						int nx = 4;
						double xt[4] = {CurrentTarget->X(), CurrentTarget->Y(), CurrentTarget->Speed(), CurrentTarget->Heading()};
						RadarMeasurement(zt, Ht, nx, xt, iSensor, iVehicleOdometry, NULL);

						//extract the measurement
						double zrt = zt[0];
						double zbt = zt[1];
						double zrrt = zt[2];

						//construct the mm gaussian component for this target
						double dr = 2.0*zrt - zr;
						double db = zbt - zb;
						double drr = zrrt - zrr;

						double rL = exp(-0.5*dr*dr/var_r) * rfact;
						double bL = exp(-0.5*db*db/var_b) * bfact;
						double rrL = exp(-0.5*drr*drr/var_rr) * rrfact;

						if (fabs(UnwrapAngle(zbt - iSensor->SensorYaw)) <= bspan)
						{
							//this target should be visible by the radar
							rLambda += rL*bL*rrL;
							ntv++;
						}
					}

					break;
				}

				CurrentTarget = CurrentTarget->NextTarget;
			}

			//scale the sum of gaussians by the number of visible targets
			double mmgwt = 1.0 / ((double) ntv);
			rLambda *= mmgwt;
		}

		break;

	case MM_SIDESICKOBSTACLE:
		{
			//side SICK obstacle measurement

			//1. model range as uniform over 0 - 30m
			//rLambda = (1.0/SIDESICK_CRSPAN);

			//NOTE: check the birth model to make sure it's more likely than clutter
			rLambda = (1.0/SIDESICK_BRSPAN);
		}

		break;
	}

	return rLambda;
}

void LocalMap::Resample()
{
	/*
	Resamples the particles in the LocalMap, if necessary

	INPUTS:
		none.

	OUTPUTS:
		none.  Resamples the particles if necessary.  If not, it returns.
	*/

	if (mIsInitialized == false)
	{
		//don't resample if the filter isn't initialized
		return;
	}

	int i;
	int j;

	//calculate the effective number of particles to decide if resampling is necessary
	double npeff = 0.0;
	double w;
	for (i = 0; i < mNumParticles; i++)
	{
		//compute sum of squares of weights
		w = mParticles[i].Weight();
		npeff += (w * w);
	}
	//invert to find effective number of particles
	npeff = 1.0 / npeff;

	if (npeff < LM_NPEFFRESAMPLE)
	{
		//resampling is necessary

		#ifdef LM_COMMONDEBUGMSGS
			printf("Resampling LocalMap at %.12lg.\n", mLocalMapTime);
		#endif

		//store a pointer to the old particle array
		Particle* OldParticles = mParticles;
		//create space for the new particles
		mParticles = new Particle[mNumParticles];
		//calculate the new weight for all resampled particles
		w = 1.0 / ((double) mNumParticles);
		
		//sample from the existing particles
		for (i = 0; i < mNumParticles; i++)
		{
			double csum = 0.0;
			double rval = RandUniform();

			for (j = 0; j < mNumParticles; j++)
			{
				csum += OldParticles[j].Weight();
				if (rval < csum || j == mNumParticles - 1)
				{
					//the jth particle is chosen to copy as the new ith particle
					mParticles[i].CopyParticle(&(OldParticles[j]));
					//reset the particle's weight to uniform
					mParticles[i].SetWeight(w);
					break;
				}
			}
		}

		//when particles are resampled, all weights are reset to even
		mMostLikelyParticle = &(mParticles[0]);

		//replenish one uniform value in the cache
		ReplaceUniform();

		//when the code gets here, all the particles have been resampled.
		//free the old memory associated with OldParticles
		delete [] OldParticles;
	}

	return;
}

void LocalMap::UpdateWithClusteredIbeo(double iMeasurementTime, int iNumIbeoPoints, double* iClusteredIbeoBuff, Sensor* iSensor, VehicleOdometry* iVehicleOdometry)
{
	/*
	Updates the LocalMap with one clustered ibeo packet.

	INPUTS:
		iMeasurementTime - time of the measurement
		iNumPoints - total number of points in the ibeo buffer
		iClusteredIbeoBuff - the actual measurement data
		iSensor - the sensor generating the measurements
		iVehicleOdometry - the vehicle odometry at the measurement time

	OUTPUTS:
		none.  Updates the particles with the measurement
	*/

	//first delete the existing loose ibeo cluster points, which will
	//be replaced by the unused points from this function
	DeleteLooseIbeoClusters();

	int np = iNumIbeoPoints;

	int i;
	int j;
	int idx = 0;

	while (idx < np)
	{
		//pull each cluster out one at a time and update the LocalMap

		//pull the new cluster id
		double cid = iClusteredIbeoBuff[midx(idx, 3, np)];
		
		int cbeg = idx;
		int cend = -1;
		//determine the start and end indices in this cluster
		while (iClusteredIbeoBuff[midx(idx, 3, np)] == cid)
		{
			idx++;
			if (idx == np)
			{
				break;
			}
		}
		cend = idx;

		//when code gets here, the cluster is all points from (i = cbeg; i < cend; i++)
		int npc = cend - cbeg;

		//pull the cluster flags
		int isunstable = (int) iClusteredIbeoBuff[midx(cbeg, 4, np)];
		int isoccluded = (int) iClusteredIbeoBuff[midx(cbeg, 5, np)];
		int ishighobstacle = (int) iClusteredIbeoBuff[midx(cbeg, 6, np)];

		/*
		//TEMPORARY!!!
		int foo = 1.34;
		for (i = cbeg; i < cend; i++)
		{
			if (iClusteredIbeoBuff[midx(i, 7, np)] > 7.5 && iClusteredIbeoBuff[midx(i, 7, np)] < 12.5 && iClusteredIbeoBuff[midx(i, 8, np)] >= 10.0 && iClusteredIbeoBuff[midx(i, 8, np)] < 15.0)
			{
				if (isoccluded < 3 && isunstable == 0)
				{
					printf("Spirit at %.12lg.\n", iClusteredIbeoBuff[midx(i, 0, np)]);
				}
				//isunstable = 1;
				break;
			}
		}
		*/

	#ifndef LM_NOTRACKING
		//if (isoccluded < 3 && isunstable == 0 && ishighobstacle == 1 && cid >= 0 && npc > 0)
		if (isoccluded == 0 && isunstable == 0 && ishighobstacle == 1 && cid >= 0 && npc > 0)
	#else
		if (false)
	#endif
		{
			//only process stable clusters with at least one corner visible

			//pull the cluster's metameasurement
			double bmin = DBL_MAX;
			double bmax = -DBL_MAX;
			double rmin = DBL_MAX;

			double* ClusterData = new double[2*npc];

			for (i = cbeg; i < cend; i++)
			{
				//extract the cluster measurement
				double px = iClusteredIbeoBuff[midx(i, 7, np)];
				double py = iClusteredIbeoBuff[midx(i, 8, np)];

				//store the cluster point for later
				ClusterData[midx(i-cbeg, 0, npc)] = px;
				ClusterData[midx(i-cbeg, 1, npc)] = py;

				double pb = atan2(py, px);
				double pr = sqrt(px*px + py*py);

				if (pb < bmin)
				{
					bmin = pb;
				}
				if (pb > bmax)
				{
					bmax = pb;
				}
				if (pr < rmin)
				{
					rmin = pr;
				}
			}

			MetaMeasurement ClusterMeasurement;
			switch (isoccluded)
			{
			case 0:
				{
					//both sides of the cluster are visible

					//create the measurement: [bccw, bcw, rmin]
					double* z = new double[3];
					z[0] = bmin;
					z[1] = bmax;
					z[2] = rmin;
					//create the cluster covariance
					double* R = new double[9];
					for (i = 0; i < 3; i++)
					{
						for (j = 0; j < 3; j++)
						{
							R[midx(i, j, 3)] = 0.0;
						}
					}
					R[midx(0, 0, 3)] = IBEO_CLUSTERBEARINGVAR;
					R[midx(1, 1, 3)] = IBEO_CLUSTERBEARINGVAR;
					R[midx(2, 2, 3)] = IBEO_CLUSTERRANGEVAR;

					//set the measurement data
					ClusterMeasurement.SetMeasurementData(MM_IBEOCLUSTER_00, iMeasurementTime, 3, z, R, npc, ClusterData);
				}

				break;

			case 1:
				{
					//cw side of cluster is visible

					//create the measurement: [bcw]
					double* z = new double[1];
					z[0] = bmin;
					//create the cluster covariance
					double* R = new double[1];
					R[0] = IBEO_CLUSTERBEARINGVAR;

					//set the measurement data
					ClusterMeasurement.SetMeasurementData(MM_IBEOCLUSTER_01, iMeasurementTime, 1, z, R, npc, ClusterData);
				}

				break;

			case 2:
				{
					//ccw side of cluster is visible

					//create the measurement: [bcw]
					double* z = new double[1];
					z[0] = bmax;
					//create the cluster covariance
					double* R = new double[1];
					R[0] = IBEO_CLUSTERBEARINGVAR;

					//set the measurement data
					ClusterMeasurement.SetMeasurementData(MM_IBEOCLUSTER_02, iMeasurementTime, 1, z, R, npc, ClusterData);
				}

				break;
			}

			//update the LocalMap with the data
			Update(iMeasurementTime, &ClusterMeasurement, iSensor, iVehicleOdometry);

			//NOTE: windows automatically cleans up memory for z, R, ClusterData when deleting ClusterMeasurement.
		}
		else
		{
			//this cluster was not used: it becomes one of the new loose ibeo clusters

			if (npc > 0)
			{
				if (mNumLooseIbeoClusters == LM_MAXCLUSTERS)
				{
					printf("Warning: ignoring a loose Ibeo cluster.\n");
					continue;
				}

				//extract sensor parameters
				double sx = iSensor->SensorX;
				double sy = iSensor->SensorY;
				double cosSyaw = cos(iSensor->SensorYaw);
				double sinSyaw = sin(iSensor->SensorYaw);

				LooseCluster* Cluster2Update;

				if (cid >= 0)
				{
					//this was a valid cluster; store at the end of mLooseIbeoClusters
					Cluster2Update = &(mLooseIbeoClusters[mNumLooseIbeoClusters]);
					//increment the count of unused ibeo clusters
					mNumLooseIbeoClusters++;
				}
				else
				{
					//this was the unclusterable cluster
					Cluster2Update = &mLooseUnclusterableIbeoPoints;
				}

				if (ishighobstacle == 1)
				{
					Cluster2Update->IsHighObstacle = true;
				}
				else
				{
					Cluster2Update->IsHighObstacle = false;
				}
				Cluster2Update->NumPoints = npc;
				Cluster2Update->Points = new double[2*npc];

				for (i = 0; i < npc; i++)
				{
					//extract the ibeo point
					double px = iClusteredIbeoBuff[midx(cbeg + i, 7, np)];
					double py = iClusteredIbeoBuff[midx(cbeg + i, 8, np)];
					//transform it to ego-vehicle coordinates
					double evx;
					double evy;
					SensorToEgoVehicle(evx, evy, px, py, cosSyaw, sinSyaw, sx, sy);

					//store the point
					Cluster2Update->Points[midx(i, 0, npc)] = evx;
					Cluster2Update->Points[midx(i, 1, npc)] = evy;
				}
			}
		}
	}

	//set the LocalMap current at the time of the update
	mLocalMapTime = iMeasurementTime;

	return;
}

void LocalMap::UpdateExistenceWithClusteredIbeo(double iMeasurementTime, int iNumIbeoPoints, double* iClusteredIbeoBuff, Sensor* iClusterSensor, Sensor* iIbeoSensor, VehicleOdometry* iVehicleOdometry)
{
	/*
	Updates target existence probabilities with clustered Ibeo data.  Uses ibeo data
	to deny existence to phantom targets.


	INPUTS:
		iMeasurementTime - time of the measurement
		iNumPoints - total number of points in the ibeo buffer
		iClusteredIbeoBuff - the actual measurement data
		iClusterSensor - the sensor that defines the cluster coordinate frame
		iIbeoSensor - the sensor that defines the frame for testing
		iVehicleOdometry - the vehicle odometry at the measurement time

	OUTPUTS:
		none.  Updates particle targets with existence probabilities
	*/

	int i;
	int j;
	int np = iNumIbeoPoints;

	//extract cluster sensor parameters
	double csx = iClusterSensor->SensorX;
	double csy = iClusterSensor->SensorY;
	double csyaw = iClusterSensor->SensorYaw;
	double ccosSyaw = cos(csyaw);
	double csinSyaw = sin(csyaw);

	//extract ibeo sensor parameters
	double isx = iIbeoSensor->SensorX;
	double isy = iIbeoSensor->SensorY;
	double isyaw = iIbeoSensor->SensorYaw;
	double icosSyaw = cos(isyaw);
	double isinSyaw = sin(isyaw);

	//create the radial grid
	RadialExistenceGrid TheGrid(IBEO_NUMGRIDBINS, 0.0, IBEO_GRIDSPAN);
	//populate the radial grid
	for (i = 0; i < np; i++)
	{
		//extract the cluster measurement (NOTE: these are in cluster coordinates)
		double px = iClusteredIbeoBuff[midx(i, 7, np)];
		double py = iClusteredIbeoBuff[midx(i, 8, np)];
		//convert to ego vehicle coordinates
		double evx;
		double evy;
		SensorToEgoVehicle(evx, evy, px, py, ccosSyaw, csinSyaw, csx, csy);
		//convert to ibeo coordinates
		double scx;
		double scy;
		EgoVehicleToSensor(scx, scy, evx, evy, icosSyaw, isinSyaw, isx, isy);
		//add to the radial grid in ibeo coordinates
		TheGrid.AddPointToGrid(scx, scy);
	}

	for (i = 0; i < mNumParticles; i++)
	{
		int nt = mParticles[i].NumTargets();
		Target* CurrentTarget = mParticles[i].FirstTarget();

		for (j = 0; j < nt; j++)
		{
			//update each target's existence probability

			//pull the target's anchor point
			double tx = CurrentTarget->X();
			double ty = CurrentTarget->Y();
			//convert it to ibeo coordinates
			double scx;
			double scy;
			EgoVehicleToSensor(scx, scy, tx, ty, icosSyaw, isinSyaw, isx, isy);
			double scr = sqrt(scx*scx + scy*scy);
			double scb = atan2(scy, scx);
			if (TheGrid.PointTest(scr, scb, TARGET_ANCHOREXISTENCERANGE) == REG_FAIL)
			{
				//ibeo provides evidence against the target existing
				CurrentTarget->UpdateExistenceProbability(1.0 - IBEO_ACCURACY, 1.0 - IBEO_FPRATE);
			}
			else if (CurrentTarget->NumPoints() > 0)
			{
				//calculate a more expensive test if the initial one doesn't remove the target

				//pull the target's extreme points, in ego-vehicle coordinates
				double cwx;
				double cwy;
				double ccwx;
				double ccwy;
				double cpx;
				double cpy;
				CurrentTarget->ExtremePoints(cwx, cwy, ccwx, ccwy, cpx, cpy);

				//test the cw extreme point in ibeo coordinates
				EgoVehicleToSensor(scx, scy, cwx, cwy, icosSyaw, isinSyaw, isx, isy);
				scr = sqrt(scx*scx + scy*scy);
				scb = atan2(scy, scx);
				//test the point against the radial grid
				switch (TheGrid.PointTest(scr, scb, TARGET_POINTEXISTENCERANGE))
				{
				case REG_FAIL:
					{
						//ibeo provides evidence against the target existing
						CurrentTarget->UpdateExistenceProbability(1.0 - IBEO_ACCURACY, 1.0 - IBEO_FPRATE);
						break;
					}
				case REG_PASS:
					{
						//ibeo provides evidence for the target existing
						int foo = 1.3;
						#ifdef LM_CONFIRMTARGETS
							if (fabs(iVehicleOdometry->vx) <= TARGET_MAXCONFIRMSPD)
							{
								CurrentTarget->UpdateExistenceProbability(IBEO_ACCURACY, IBEO_FPRATE);
							}
						#endif
						break;
					}
				}

				//test the ccw extreme point in ibeo coordinates
				EgoVehicleToSensor(scx, scy, ccwx, ccwy, icosSyaw, isinSyaw, isx, isy);
				scr = sqrt(scx*scx + scy*scy);
				scb = atan2(scy, scx);
				//test the point against the radial grid
				switch (TheGrid.PointTest(scr, scb, TARGET_POINTEXISTENCERANGE))
				{
				case REG_FAIL:
					//ibeo provides evidence against the target existing
					CurrentTarget->UpdateExistenceProbability(1.0 - IBEO_ACCURACY, 1.0 - IBEO_FPRATE);
					break;
				case REG_PASS:
					//ibeo provides evidence for the target existing
					CurrentTarget->UpdateExistenceProbability(IBEO_ACCURACY, IBEO_FPRATE);
					break;
				}

				//test the closest range extreme point in ibeo coordinates
				EgoVehicleToSensor(scx, scy, cpx, cpy, icosSyaw, isinSyaw, isx, isy);
				scr = sqrt(scx*scx + scy*scy);
				scb = atan2(scy, scx);
				//test the point against the radial grid
				switch (TheGrid.PointTest(scr, scb, TARGET_POINTEXISTENCERANGE))
				{
				case REG_FAIL:
					//ibeo provides evidence against the target existing
					CurrentTarget->UpdateExistenceProbability(1.0 - IBEO_ACCURACY, 1.0 - IBEO_FPRATE);
					break;
				case REG_PASS:
					//ibeo provides evidence for the target existing
					CurrentTarget->UpdateExistenceProbability(IBEO_ACCURACY, IBEO_FPRATE);
					break;
				}
			}

			/*
			//TEST
			int foo = 1.3;
			double dlast = iMeasurementTime - CurrentTarget->LastUpdateTime();
			double dthresh = 0.75*TARGET_EXISTENCERANGE*exp(-3.0*dlast/1.0) + 0.25*TARGET_EXISTENCERANGE;
			//pull the target's anchor point
			tx = CurrentTarget->X();
			ty = CurrentTarget->Y();
			//convert it to ibeo coordinates
			EgoVehicleToSensor(scx, scy, tx, ty, icosSyaw, isinSyaw, isx, isy);
			double scr = sqrt(scx*scx + scy*scy);
			double scb = atan2(scy, scx);
			if (TheGrid.PointTest(scr, scb, dthresh) == REG_FAIL)
			{
				//ibeo provides evidence against the target existing
				CurrentTarget->UpdateExistenceProbability(1.0 - IBEO_ACCURACY, 1.0 - IBEO_FPRATE);
			}
			//END TEST
			*/

			CurrentTarget = CurrentTarget->NextTarget;
		}
	}

	//set the LocalMap current at the time of the update
	mLocalMapTime = iMeasurementTime;

	return;
}

void LocalMap::UpdateExistenceWithVelodyneGrid(double iMeasurementTime, OccupancyGridInterface* iVelodyneGrid, 
											   Sensor* iSensor, VehicleOdometry* iVehicleOdometry)
{
	/*
	Updates the existence probability of LocalMap targets with a Velodyne occupancy grid.

	INPUTS:
		iMeasurementTime - time at which the velodyne grid is valid
		iVelodyneGrid - the actual velodyne obstacle occupancy grid, which can accept
			queries of position in sensor coordinates and return whether there
			is an obstacle there or not
		iSensor - the Velodyne sensor structure that defines its orientation on the car
		iVehicleOdometry - vehicle odometry structure valid at the current velodyne grid

	OUTPUTS:
		none.  Updates existence probabilities for all particles using the Velodyne grid
	*/

	int i;
	int j;

	//load the velodyne occupancy grid nearest the measurement time
	double gridtime = iVelodyneGrid->LoadGridAtVTS(iMeasurementTime);
	if (gridtime < 0.0 || fabs(gridtime - iMeasurementTime) > VELODYNE_MAXGRIDAGE)
	{
		//velodyne grid could not be found at the requested timestamp
		#ifdef LM_COMMONDEBUGMSGS
			printf("Warning: velodyne occupancy grid could not be loaded at time %.12lg.\n", iMeasurementTime);
		#endif
		return;
	}

	Particle* CurrentParticle;
	for (i = 0; i < mNumParticles; i++)
	{
		//extract each particle
		CurrentParticle = &(mParticles[i]);

		//and test the existence of all its targets
		int nt = CurrentParticle->NumTargets();
		Target* CurrentTarget = CurrentParticle->FirstTarget();
		for (j = 0; j < nt; j++)
		{
			//check and update the existence of this target

			if (CurrentTarget->NumPoints() == 0)
			{
				//pull the target's anchor point in ego-vehicle coordinates
				double tx = CurrentTarget->X();
				double ty = CurrentTarget->Y();

				//test the target's anchor point
				switch (iVelodyneGrid->GetOccupancy_VehCoords((float) tx, (float) ty))
				{
				case OCCUPANCY_STATUS::FREE:
					//velodyne provides evidence against the target existing
					CurrentTarget->UpdateExistenceProbability(1.0 - VELODYNE_ACCURACY, 1.0 - VELODYNE_FPRATE);
					break;

				case OCCUPANCY_STATUS::OCCUPIED:
					//velodyne provides evidence for the target existing
					int foo = 1.3;
					#ifdef LM_CONFIRMTARGETS
						if (fabs(iVehicleOdometry->vx) <= TARGET_MAXCONFIRMSPD)
						{
							CurrentTarget->UpdateExistenceProbability(VELODYNE_ACCURACY, VELODYNE_FPRATE);
						}
					#endif
					break;
				}
			}
			else
			{
				//pull the target's extreme points to test

				//pull the target's extreme points, in ego-vehicle coordinates
				double cwx;
				double cwy;
				double ccwx;
				double ccwy;
				double cpx;
				double cpy;
				CurrentTarget->ExtremePoints(cwx, cwy, ccwx, ccwy, cpx, cpy);

				//test the cw extreme point in ibeo coordinates
				if (iVelodyneGrid->GetOccupancy_VehCoords((float) cwx, (float) cwy) == OCCUPANCY_STATUS::FREE)
				{
					//ibeo provides evidence against the target existing
					CurrentTarget->UpdateExistenceProbability(1.0 - VELODYNE_ACCURACY, 1.0 - VELODYNE_FPRATE);
				}

				//test the ccw extreme point in ibeo coordinates
				if (iVelodyneGrid->GetOccupancy_VehCoords((float) ccwx, (float) ccwy) == OCCUPANCY_STATUS::FREE)
				{
					//ibeo provides evidence against the target existing
					CurrentTarget->UpdateExistenceProbability(1.0 - VELODYNE_ACCURACY, 1.0 - VELODYNE_FPRATE);
				}

				//test the closest range extreme point in ibeo coordinates
				if (iVelodyneGrid->GetOccupancy_VehCoords((float) cpx, (float) cpy) == OCCUPANCY_STATUS::FREE)
				{
					//ibeo provides evidence against the target existing
					CurrentTarget->UpdateExistenceProbability(1.0 - VELODYNE_ACCURACY, 1.0 - VELODYNE_FPRATE);
				}
			}

			CurrentTarget = CurrentTarget->NextTarget;
		}
	}

	//set the LocalMap current at the time of the update
	mLocalMapTime = iMeasurementTime;

	return;
}

void LocalMap::UpdateWithClusteredSick(double iMeasurementTime, int iNumSickPoints, double* iClusteredSickBuff, Sensor* iSensor, VehicleOdometry* iVehicleOdometry)
{
	/*
	Updates the LocalMap with one clustered SICK packet.

	INPUTS:
		iMeasurementTime - time of the measurement
		iNumSickPoints - total number of points in the SICK buffer
		iClusteredSickBuff - the actual measurement data
		iSensor - the sensor generating the measurements
		iVehicleOdometry - the vehicle odometry at the measurement time

	OUTPUTS:
		none.  Updates the particles with the measurement
	*/

	//first delete the existing loose cluster points, which will
	//be replaced by the unused points from this function
	DeleteLooseClusteredSickClusters();

	int np = iNumSickPoints;

	int i;
	int j;
	int idx = 0;

	while (idx < np)
	{
		//pull each cluster out one at a time and update the LocalMap

		//pull the new cluster id
		double cid = iClusteredSickBuff[midx(idx, 3, np)];
		
		int cbeg = idx;
		int cend = -1;
		//determine the start and end indices in this cluster
		while (iClusteredSickBuff[midx(idx, 3, np)] == cid)
		{
			idx++;
			if (idx == np)
			{
				break;
			}
		}
		cend = idx;

		//when code gets here, the cluster is all points from (i = cbeg; i < cend; i++)
		int npc = cend - cbeg;

		//pull the cluster flags
		int isunstable = (int) iClusteredSickBuff[midx(cbeg, 4, np)];
		int isoccluded = (int) iClusteredSickBuff[midx(cbeg, 5, np)];
		int ishighobstacle = (int) iClusteredSickBuff[midx(cbeg, 6, np)];

	#ifndef LM_NOTRACKING
		//if (isoccluded < 3 && isunstable == 0 && ishighobstacle == 1 && cid >= 0 && npc > 0)
		if (isoccluded == 0 && isunstable == 0 && ishighobstacle == 1 && cid >= 0 && npc > 0)
	#else
		if (false)
	#endif
		{
			//only process stable clusters with at least one corner visible

			//pull the cluster's metameasurement
			double bmin = DBL_MAX;
			double bmax = -DBL_MAX;
			double rmin = DBL_MAX;

			double* ClusterData = new double[2*npc];

			for (i = cbeg; i < cend; i++)
			{
				//extract the cluster measurement
				double px = iClusteredSickBuff[midx(i, 7, np)];
				double py = iClusteredSickBuff[midx(i, 8, np)];

				//store the cluster point for later
				ClusterData[midx(i-cbeg, 0, npc)] = px;
				ClusterData[midx(i-cbeg, 1, npc)] = py;

				double pb = atan2(py, px);
				double pr = sqrt(px*px + py*py);

				if (pb < bmin)
				{
					bmin = pb;
				}
				if (pb > bmax)
				{
					bmax = pb;
				}
				if (pr < rmin)
				{
					rmin = pr;
				}
			}

			MetaMeasurement ClusterMeasurement;
			switch (isoccluded)
			{
			case 0:
				{
					//both sides of the cluster are visible

					//create the measurement: [bccw, bcw, rmin]
					double* z = new double[3];
					z[0] = bmin;
					z[1] = bmax;
					z[2] = rmin;
					//create the cluster covariance
					double* R = new double[9];
					for (i = 0; i < 3; i++)
					{
						for (j = 0; j < 3; j++)
						{
							R[midx(i, j, 3)] = 0.0;
						}
					}
					R[midx(0, 0, 3)] = CLUSTEREDSICK_CLUSTERBEARINGVAR;
					R[midx(1, 1, 3)] = CLUSTEREDSICK_CLUSTERBEARINGVAR;
					R[midx(2, 2, 3)] = CLUSTEREDSICK_CLUSTERRANGEVAR;

					//set the measurement data
					ClusterMeasurement.SetMeasurementData(MM_IBEOCLUSTER_00, iMeasurementTime, 3, z, R, npc, ClusterData);
				}

				break;

			case 1:
				{
					//cw side of cluster is visible

					//create the measurement: [bcw]
					double* z = new double[1];
					z[0] = bmin;
					//create the cluster covariance
					double* R = new double[1];
					R[0] = CLUSTEREDSICK_CLUSTERBEARINGVAR;

					//set the measurement data
					ClusterMeasurement.SetMeasurementData(MM_IBEOCLUSTER_01, iMeasurementTime, 1, z, R, npc, ClusterData);
				}

				break;

			case 2:
				{
					//ccw side of cluster is visible

					//create the measurement: [bcw]
					double* z = new double[1];
					z[0] = bmax;
					//create the cluster covariance
					double* R = new double[1];
					R[0] = CLUSTEREDSICK_CLUSTERBEARINGVAR;

					//set the measurement data
					ClusterMeasurement.SetMeasurementData(MM_IBEOCLUSTER_02, iMeasurementTime, 1, z, R, npc, ClusterData);
				}

				break;
			}

			//update the LocalMap with the data
			Update(iMeasurementTime, &ClusterMeasurement, iSensor, iVehicleOdometry);

			//NOTE: windows automatically cleans up memory for z, R, ClusterData when deleting ClusterMeasurement.
		}
		else
		{
			//this cluster was not used: it becomes one of the new loose ibeo clusters

			if (npc > 0)
			{
				if (mNumLooseIbeoClusters == LM_MAXCLUSTERS)
				{
					printf("Warning: ignoring a loose clustered sick cluster.\n");
					continue;
				}

				//extract sensor parameters
				double sx = iSensor->SensorX;
				double sy = iSensor->SensorY;
				double cosSyaw = cos(iSensor->SensorYaw);
				double sinSyaw = sin(iSensor->SensorYaw);

				LooseCluster* Cluster2Update;

				if (cid >= 0)
				{
					//this was a valid cluster; store at the end of mLooseIbeoClusters
					Cluster2Update = &(mLooseClusteredSickClusters[mNumLooseClusteredSickClusters]);
					//increment the count of unused sick clusters
					mNumLooseClusteredSickClusters++;
				}
				else
				{
					//this was the unclusterable cluster
					Cluster2Update = &mLooseUnclusterableClusteredSickPoints;
				}

				Cluster2Update->NumPoints = npc;
				Cluster2Update->Points = new double[2*npc];
				if (ishighobstacle == 1)
				{
					Cluster2Update->IsHighObstacle = true;
				}
				else
				{
					Cluster2Update->IsHighObstacle = false;
				}

				for (i = 0; i < npc; i++)
				{
					//extract the ibeo point
					double px = iClusteredSickBuff[midx(cbeg + i, 7, np)];
					double py = iClusteredSickBuff[midx(cbeg + i, 8, np)];
					//transform it to ego-vehicle coordinates
					double evx;
					double evy;
					SensorToEgoVehicle(evx, evy, px, py, cosSyaw, sinSyaw, sx, sy);

					//store the point
					Cluster2Update->Points[midx(i, 0, npc)] = evx;
					Cluster2Update->Points[midx(i, 1, npc)] = evy;
				}
			}
		}
	}

	//set the LocalMap current at the time of the update
	mLocalMapTime = iMeasurementTime;

	return;
}

void LocalMap::UpdateExistenceWithClusteredSick(double iMeasurementTime, int iNumSickPoints, double* iClusteredSickBuff, Sensor* iSensor, VehicleOdometry* iVehicleOdometry)
{
	/*
	Updates target existence probabilities using clustered SICK data.  Uses data to deny existence
	to any phantom targets that are obviously false.

	INPUTS:
		iMeasurementTime - time of the measurement
		iNumSickPoints - total number of points in the SICK buffer
		iClusteredSickBuff - the actual measurement data
		iSensor - the sensor generating the measurements
		iVehicleOdometry - the vehicle odometry at the measurement time

	OUTPUTS:
		none.  Updates targets' existence with the measurement
	*/

	int i;
	int j;
	int np = iNumSickPoints;

	//extract sensor parameters
	double sx = iSensor->SensorX;
	double sy = iSensor->SensorY;
	double syaw = iSensor->SensorYaw;
	double cosSyaw = cos(syaw);
	double sinSyaw = sin(syaw);

	//create the radial grid
	RadialExistenceGrid TheGrid(CLUSTEREDSICK_NUMGRIDBINS, 0.0, CLUSTEREDSICK_GRIDSPAN);
	//populate the radial grid
	for (i = 0; i < np; i++)
	{
		//extract the cluster point (NOTE: these are already in sensor coordinates
		double px = iClusteredSickBuff[midx(i, 7, np)];
		double py = iClusteredSickBuff[midx(i, 8, np)];
		//add to the radial grid in sensor coordinates
		TheGrid.AddPointToGrid(px, py);
	}

	for (i = 0; i < mNumParticles; i++)
	{
		int nt = mParticles[i].NumTargets();
		Target* CurrentTarget = mParticles[i].FirstTarget();

		for (j = 0; j < nt; j++)
		{
			//update each target's existence probability

			//pull the target's anchor point
			double tx = CurrentTarget->X();
			double ty = CurrentTarget->Y();
			//convert it to sensor coordinates
			double scx;
			double scy;
			EgoVehicleToSensor(scx, scy, tx, ty, cosSyaw, sinSyaw, sx, sy);
			double scr = sqrt(scx*scx + scy*scy);
			double scb = atan2(scy, scx);
			if (TheGrid.PointTest(scr, scb, TARGET_ANCHOREXISTENCERANGE) == REG_FAIL)
			{
				//ibeo provides evidence against the target existing
				CurrentTarget->UpdateExistenceProbability(1.0 - CLUSTEREDSICK_ACCURACY, 1.0 - CLUSTEREDSICK_FPRATE);
			}
			else if (CurrentTarget->NumPoints() > 0)
			{
				//calculate a more expensive test if the initial one doesn't remove the target

				//pull the target's extreme points, in ego-vehicle coordinates
				double cwx;
				double cwy;
				double ccwx;
				double ccwy;
				double cpx;
				double cpy;
				CurrentTarget->ExtremePoints(cwx, cwy, ccwx, ccwy, cpx, cpy);

				//test the cw extreme point in ibeo coordinates
				EgoVehicleToSensor(scx, scy, cwx, cwy, cosSyaw, sinSyaw, sx, sy);
				scr = sqrt(scx*scx + scy*scy);
				scb = atan2(scy, scx);
				//test the point against the radial grid
				switch (TheGrid.PointTest(scr, scb, TARGET_POINTEXISTENCERANGE))
				{
				case REG_FAIL:
					//sick provides evidence against the target existing
					CurrentTarget->UpdateExistenceProbability(1.0 - CLUSTEREDSICK_ACCURACY, 1.0 - CLUSTEREDSICK_FPRATE);
					break;
				case REG_PASS:
					//sick provides evidence for the target existing
					int foo = 1.3;
					#ifdef LM_CONFIRMTARGETS
						if (fabs(iVehicleOdometry->vx) <= TARGET_MAXCONFIRMSPD)
						{
							CurrentTarget->UpdateExistenceProbability(CLUSTEREDSICK_ACCURACY, CLUSTEREDSICK_FPRATE);
						}
					#endif
					break;
				}

				//test the ccw extreme point in ibeo coordinates
				EgoVehicleToSensor(scx, scy, ccwx, ccwy, cosSyaw, sinSyaw, sx, sy);
				scr = sqrt(scx*scx + scy*scy);
				scb = atan2(scy, scx);
				//test the point against the radial grid
				switch (TheGrid.PointTest(scr, scb, TARGET_POINTEXISTENCERANGE))
				{
				case REG_FAIL:
					//sick provides evidence against the target existing
					CurrentTarget->UpdateExistenceProbability(1.0 - CLUSTEREDSICK_ACCURACY, 1.0 - CLUSTEREDSICK_FPRATE);
					break;
				case REG_PASS:
					//sick provides evidence for the target existing
					CurrentTarget->UpdateExistenceProbability(CLUSTEREDSICK_ACCURACY, CLUSTEREDSICK_FPRATE);
					break;
				}

				//test the closest range extreme point in ibeo coordinates
				EgoVehicleToSensor(scx, scy, cpx, cpy, cosSyaw, sinSyaw, sx, sy);
				scr = sqrt(scx*scx + scy*scy);
				scb = atan2(scy, scx);
				//test the point against the radial grid
				switch (TheGrid.PointTest(scr, scb, TARGET_POINTEXISTENCERANGE))
				{
				case REG_FAIL:
					//sick provides evidence against the target existing
					CurrentTarget->UpdateExistenceProbability(1.0 - CLUSTEREDSICK_ACCURACY, 1.0 - CLUSTEREDSICK_FPRATE);
					break;
				case REG_PASS:
					//sick provides evidence for the target existing
					CurrentTarget->UpdateExistenceProbability(CLUSTEREDSICK_ACCURACY, CLUSTEREDSICK_FPRATE);
					break;
				}
			}


			CurrentTarget = CurrentTarget->NextTarget;
		}
	}

	//set the LocalMap current at the time of the update
	mLocalMapTime = iMeasurementTime;

	return;
}

void LocalMap::UpdateWithMobileyeObstacles(double iMeasurementTime, int iNumObstacles, double* iMobileyeBuff, Sensor* iSensor, VehicleOdometry* iVehicleOdometry)
{
	/*
	Updates the LocalMap with a Mobileye obstacle detection packet

	INPUTS:
		iMeasurementTime - timestamp of the measurement
		iNumObstacles - number of obstacles in the packet
		iMobileyeBuff - the actual data packet
		iSensor - sensor structure giving the mobileye's location on the car
		iVehicleOdometry - vehicle odometry structure valid for the update time

	OUTPUTS:
		none.  Updates the LocalMap targets with the mobileye information
	*/

	#ifdef LM_NOTRACKING
		return;
	#endif

	int no = iNumObstacles;

	int i;
	int j;
	int k;
	for (i = 0; i < no; i++)
	{
		//pull each obstacle one at a time and update the LocalMap

		if (iMobileyeBuff[midx(i, 10, no)] < MOBILEYE_MIN_OBSCONF)
		{
			//mobileye obstacle wasn't confident enough to consider

			#ifdef LM_COMMONDEBUGMSGS
				printf("Mobileye obstacle rejected due to lack of confidence.\n");
			#endif
			continue;
		}

		//extract the rest of the obstacle data
		double zxcam = iMobileyeBuff[midx(i, 4, no)];
		double zycam = iMobileyeBuff[midx(i, 5, no)];
		double zscam = iMobileyeBuff[midx(i, 6, no)];
		double zwcam = iMobileyeBuff[midx(i, 7, no)];

		MetaMeasurement ObstacleMeasurement;
		//create the obstacle measurement: [x, y, s, w]
		int nz = 4;
		double* z = new double[nz];
		z[0] = zxcam;
		z[1] = zycam;
		z[2] = zscam;
		z[3] = zwcam;

		//create the measurement covariance
		double* R = new double[nz*nz];
		for (j = 0; j < nz; j++)
		{
			for (k = 0; k < nz; k++)
			{
				R[midx(j, k, nz)] = 0.0;
			}
		}
		//scale variances appropriately with the camera
		double py = MOBILEYE_FOCALLEN * tan(iSensor->SensorPitch - atan(iSensor->SensorZ / zxcam));
		double pxang = iSensor->SensorPitch - atan(py/MOBILEYE_FOCALLEN);
		double dxdp = iSensor->SensorZ / pow(sin(pxang), 2.0) / (1.0 + pow(py/MOBILEYE_FOCALLEN, 2.0)) / MOBILEYE_FOCALLEN;
		double varx = dxdp*dxdp*MOBILEYE_BUMPERVAR;
		R[midx(0, 0, nz)] = varx;
		R[midx(1, 1, nz)] = pow(zycam/zxcam, 2.0)*varx + pow(zxcam/MOBILEYE_FOCALLEN, 2.0)*MOBILEYE_LATERALVAR;
		R[midx(2, 2, nz)] = MOBILEYE_SPEEDVAR;
		R[midx(3, 3, nz)] = pow(zwcam/zxcam, 2.0)*varx + pow(zxcam/MOBILEYE_FOCALLEN, 2.0)*MOBILEYE_WIDTHVAR;

		//set the measurement data
		ObstacleMeasurement.SetMeasurementData(MM_MOBILEYEOBSTACLE, iMeasurementTime, nz, z, R);

		//update the LocalMap with the data
		Update(iMeasurementTime, &ObstacleMeasurement, iSensor, iVehicleOdometry);
		//NOTE: windows automatically cleans up memory for z and R when deleting ObstacleMeasurement.
	}

	//NOTE: not currently using Mobileye to deny the existence of obstacles

	//set the LocalMap current at the time of the update
	mLocalMapTime = iMeasurementTime;

	return;
}

void LocalMap::UpdateWithRadar(double iMeasurementTime, int iNumObstacles, double* iRadarBuff, Sensor* iSensor, VehicleOdometry* iVehicleOdometry)
{
	/*
	Updates the LocalMap with a radar measurement packet (from a single radar)

	INPUTS:
		iMeasurementTime - timestamp of the measurement
		iNumObstacles - number of obstacles detected by the radar
		iRadarBuff - the actual radar measurement buffer
		iSensor - the sensor structure describing the radar's location on the vehicle
		iVehicleOdometry - the ego-vehicle's odometry, valid during the update

	OUTPUTS:
		none.  Updates the targets in LocalMap with the radar measurement.
	*/

	#ifdef LM_NOTRACKING
		return;
	#endif

	int no = iNumObstacles;

	int i;
	int j;
	int k;
	for (i = 0; i < no; i++)
	{
		//pull each obstacle one at a time and update the LocalMap

		if (iRadarBuff[midx(i, 4, no)] == 1.0)
		{
			//radar obstacle was flagged with a detection error

			#ifdef LM_COMMONDEBUGMSGS
				printf("Radar obstacle rejected due to scan error.\n");
			#endif
			continue;
		}
		if (iRadarBuff[midx(i, 7, no)] != 4.0)
		{
			//radar obstacle wasn't a mature target
			#ifdef LM_COMMONDEBUGMSGS
				printf("Radar obstacle rejected because it is not mature.\n");
			#endif
			continue;
		}
		if (iRadarBuff[midx(i, 8, no)] < RADAR_MINPOWER)
		{
			//radar obstacle wasn't a strong enough return to consider
			#ifdef LM_COMMONDEBUGMSGS
				printf("Radar obstacle rejected due to signal power.\n");
			#endif
			continue;
		}

		//extract the rest of the obstacle data
		//(NOTE: flip the sign of bearing for DUC convention)
		double zr = iRadarBuff[midx(i, 10, no)];
		double zb = -iRadarBuff[midx(i, 14, no)];
		if (fabs(iSensor->SensorRoll) > PIOTWO)
		{
			//the sensor is upside-down, so reverse bearing
			zb = -zb;
		}
		double zrr = iRadarBuff[midx(i, 12, no)];

		MetaMeasurement ObstacleMeasurement;
		//create the obstacle measurement: [r, b, rr]
		int nz = 3;
		double* z = new double[nz];
		z[0] = zr;
		z[1] = zb;
		z[2] = zrr;

		//create the measurement covariance
		double* R = new double[nz*nz];
		for (j = 0; j < nz; j++)
		{
			for (k = 0; k < nz; k++)
			{
				R[midx(j, k, nz)] = 0.0;
			}
		}
		R[midx(0, 0, nz)] = RADAR_RANGEVAR;
		R[midx(1, 1, nz)] = RADAR_BEARINGVAR;
		R[midx(2, 2, nz)] = RADAR_RANGERATEVAR;

		//set the measurement data
		ObstacleMeasurement.SetMeasurementData(MM_RADAROBSTACLE, iMeasurementTime, nz, z, R);

		//update the LocalMap with the data
		Update(iMeasurementTime, &ObstacleMeasurement, iSensor, iVehicleOdometry);
		//NOTE: windows automatically cleans up memory for z and R when deleting ObstacleMeasurement.
	}

	//NOTE: not currently using Radar to deny the existence of obstacles

	//set the LocalMap current at the time of the update
	mLocalMapTime = iMeasurementTime;

	return;
}

void LocalMap::UpdateWithSideSick(double iMeasurementTime, int iNumDataRows, double* iSickBuff, bool iIsDriverSide, Sensor* iSensor, VehicleOdometry* iVehicleOdometry)
{
	/*
	Updates the LocalMap with measurements made by a side-pointing LIDAR.

	INPUTS:
		iMeasurementTime - timestamp at which the measurement was made
		iNumDataRows - number of interesting measurements made (objects found)
		iSickBuff - buffer containing the processed LIDAR data (not raw data)
		iIsDriverSide - true if the lidar is the driver's side lidar, false otherwise
		iSensor - a sensor structure describing the LIDAR's orientation
		iVehicleOdometry - vehicle odometry structure valid at the time of the update.

	OUTPUTS:
		none.  Updates LocalMap with LIDAR data.
	*/

	#ifdef LM_NOTRACKING
		return;
	#endif

	int i;
	int j;
	int no = iNumDataRows;

	if (iIsDriverSide == true)
	{
		//delete the left side LIDAR clusters
		DeleteLooseDriverSideSickClusters();
	}
	else
	{
		//delete the right side LIDAR clusters
		DeleteLoosePassengerSideSickClusters();
	}

	for (i = 0; i < no; i++)
	{
		if (iSickBuff[midx(i, 3, no)] > SIDESICK_MAXRANGE)
		{
			//this measurement was too far away from the vehicle to consider for anything
			#ifdef LM_COMMONDEBUGMSGS
				printf("Side SICK obstacle rejected because it is too far away.\n");
			#endif

			continue;
		}

		if (iSickBuff[midx(i, 4, no)] < SIDESICK_MINLOOSECLUSTERHEIGHT)
		{
			//this measurement was too low to the ground to consider for anything
			#ifdef LM_COMMONDEBUGMSGS
				printf("Side SICK obstacle rejected because it is not tall enough.\n");
			#endif

			continue;
		}

		if (iSickBuff[midx(i, 4, no)] >= SIDESICK_MINTARGETHEIGHT)
		{
			//this measurement is big enough to be used to update targets (or to create as a new target)

			//extract the rest of the obstacle data
			double zr = iSickBuff[midx(i, 3, no)];

			MetaMeasurement ObstacleMeasurement;
			//create the obstacle measurement: [r]
			int nz = 1;
			double* z = new double[nz];
			z[0] = zr;

			//create the measurement covariance
			double* R = new double[nz*nz];
			R[midx(0, 0, nz)] = SIDESICK_RANGEVAR;

			//set the measurement data
			ObstacleMeasurement.SetMeasurementData(MM_SIDESICKOBSTACLE, iMeasurementTime, nz, z, R);

			//update the LocalMap with the data
			Update(iMeasurementTime, &ObstacleMeasurement, iSensor, iVehicleOdometry);
			//NOTE: windows automatically cleans up memory for z and R when deleting ObstacleMeasurement.
		}
		else
		{
			//this measurement was not used: it becomes one of the new loose side-lidar clusters

			if (iIsDriverSide == true && mNumLooseDriverSideSickClusters == LM_MAXCLUSTERS)
			{
				printf("Warning: ignoring a loose left side SICK cluster.\n");
				continue;
			}
			else if (iIsDriverSide == false && mNumLoosePassengerSideSickClusters == LM_MAXCLUSTERS)
			{
				printf("Warning: ignoring a loose right side SICK cluster.\n");
				continue;
			}

			//extract sensor parameters
			double sx = iSensor->SensorX;
			double sy = iSensor->SensorY;
			double cosSyaw = cos(iSensor->SensorYaw);
			double sinSyaw = sin(iSensor->SensorYaw);

			LooseCluster* Cluster2Update;
			//this was a valid cluster; store at the end of mLoose(Left/Right)SideSickClusters
			if (iIsDriverSide == true)
			{
				Cluster2Update = &(mLooseDriverSideSickClusters[mNumLooseDriverSideSickClusters]);
				//increment the count of unused side lidar clusters
				mNumLooseDriverSideSickClusters++;
			}
			else
			{
				Cluster2Update = &(mLoosePassengerSideSickClusters[mNumLoosePassengerSideSickClusters]);
				//increment the count of unused side lidar clusters
				mNumLoosePassengerSideSickClusters++;
			}

			//create a fake set of 4 points for the cluster
			int npc = 4;
			//note: if the obstacle was a high obstacle, it would be tracked
			Cluster2Update->IsHighObstacle = false;
			Cluster2Update->NumPoints = npc;
			Cluster2Update->Points = new double[2*npc];

			//convert the nominal measurement to vehicle coordinates
			double px = iSickBuff[midx(i, 3, no)];
			double py = 0.0;
			double evx;
			double evy;
			SensorToEgoVehicle(evx, evy, px, py, cosSyaw, sinSyaw, sx, sy);

			//create a fake cluster of points around this point
			Cluster2Update->Points[midx(0, 0, npc)] = evx + 0.5*SIDESICK_DEFAULTWIDTH;
			Cluster2Update->Points[midx(0, 1, npc)] = evy;
			Cluster2Update->Points[midx(1, 0, npc)] = evx + 0.5*SIDESICK_DEFAULTWIDTH;
			Cluster2Update->Points[midx(1, 1, npc)] = evy + SIDESICK_DEFAULTWIDTH;
			Cluster2Update->Points[midx(2, 0, npc)] = evx - 0.5*SIDESICK_DEFAULTWIDTH;
			Cluster2Update->Points[midx(2, 1, npc)] = evy;
			Cluster2Update->Points[midx(3, 0, npc)] = evx - 0.5*SIDESICK_DEFAULTWIDTH;
			Cluster2Update->Points[midx(3, 1, npc)] = evy + SIDESICK_DEFAULTWIDTH;
		}
	}

	//***

	//also use side lidar data to update obstacle existence probabilities

	//extract the distance to the closest obstacle
	double mind = DBL_MAX;
	for (i = 0; i < no; i++)
	{
		if (iSickBuff[midx(i, 4, no)] >= SIDESICK_MINLOOSECLUSTERHEIGHT)
		{
			//this measurement is high enough to be an obstacle

			double dcur = iSickBuff[midx(i, 3, no)];
			if (dcur <= mind)
			{
				//this measurement is the closest obstacle seen so far
				mind = dcur;
			}
		}
	}
	if (mind == DBL_MAX)
	{
		//no obstacle seen
		mind = 0.0;
	}

	double sx = iSensor->SensorX;
	double sy = iSensor->SensorY;
	double syaw = iSensor->SensorYaw;
	double cosSyaw = cos(syaw);
	double sinSyaw = sin(syaw);

	for (i = 0; i < mNumParticles; i++)
	{
		int nt = mParticles[i].NumTargets();
		Target* CurrentTarget = mParticles[i].FirstTarget();

		for (j = 0; j < nt; j++)
		{
			//update each target's existence probability

			//pull the target's anchor point
			double tx = CurrentTarget->X();
			double ty = CurrentTarget->Y();

			//convert to sensor coordinates
			double scx;
			double scy;
			EgoVehicleToSensor(scx, scy, tx, ty, cosSyaw, sinSyaw, sx, sy);

			//check if the target is reasonably visible
			if (CurrentTarget->IsInLineOfSight(iSensor) == true)
			{
				//if the target should be visible, check whether the lidar offers
				//evidence against its existence
				if (mind - sqrt(scx*scx + scy*scy) > TARGET_ANCHOREXISTENCERANGE)
				{
					//side lidar provides evidence against the target existing
					CurrentTarget->UpdateExistenceProbability(1.0 - SIDESICK_ACCURACY, 1.0 - SIDESICK_FPRATE);
				}
			}

			CurrentTarget = CurrentTarget->NextTarget;
		}
	}

	//set the LocalMap current at the time of the update
	mLocalMapTime = iMeasurementTime;

	return;
}

void LocalMap::Initialize(double iInitialTime)
{
	/*
	Initializes the LocalMap with a valid timestamp

	INPUTS:
		iInitialTime - the initial timestamp used for initialization

	OUTPUTS:
		none.  Initializes the LocalMap (or reinitializes)
	*/

	if (mIsInitialized == false)
	{
		//initialize the LocalMap on a valid timestamp
		printf("Initializing LocalMap...\n");
	}

	mLocalMapTime = iInitialTime;

	int i;
	double w = 1.0 / ((double) mNumParticles);
	for (i = 0; i < mNumParticles; i++)
	{
		//delete all targets in each particle when the LocalMap is initialized
		mParticles[i].RemoveAllTargets();
		//and initialize the particle's weight
		mParticles[i].SetWeight(w);
	}

	//remove any loose clusters when reinitializing
	DeleteLooseIbeoClusters();
	DeleteLooseDriverSideSickClusters();
	DeleteLoosePassengerSideSickClusters();
	DeleteLooseClusteredSickClusters();

	if (mIsInitialized == false)
	{
		printf("LocalMap initialized.\n");
	}
	
	mIsInitialized = true;

	return;
}

void LocalMap::PrintTargets(FILE* iTargetFile)
{
	/*
	Prints all the targets from the most likely particle to a file.

	INPUTS:
		iTargetFile - the file that will contain the output targets.

	OUTPUTS:
		none.  Writes the targets in the most likely particle to iTargetfile
	*/

	if (mIsInitialized == false)
	{
		//don't print anything if the filter isn't initialized
		return;
	}

	int i;
	int j;
	int k;

	int nt = mTransmitParticle->NumTargets();
	Target* CurrentTarget = mTransmitParticle->FirstTarget();
	for (i = 0; i < nt; i++)
	{
		//print each target separately

		//extract the points for this target
		int np = CurrentTarget->NumPoints();
		double xt = CurrentTarget->X();
		double yt = CurrentTarget->Y();
		double ot = CurrentTarget->Orientation();
		double st = CurrentTarget->Speed();
		double ht = CurrentTarget->Heading();
		double wt = CurrentTarget->Width();
		double cosOrient = cos(CurrentTarget->Orientation());
		double sinOrient = sin(CurrentTarget->Orientation());

		//print the target state header
		fprintf(iTargetFile, "%.12lg,%d,%d,%d,%.12lg,%.12lg,%.12lg,%.12lg,%.12lg,%.12lg,%d\n", mTransmitTime, i, nt, 
			CurrentTarget->TargetType(), xt, yt, ot, st, ht, wt, np);

		//print the target covariance
		char cbuff[LM_LINESIZE];
		sprintf_s(cbuff, LM_LINESIZE, "%.12lg,%d,%d,%.12lg", mTransmitTime, i, nt, CurrentTarget->Covariance(0, 0));
		char buff[LM_FIELDSIZE];
		for (j = 1; j < T_NUMSTATES; j++)
		{
			for (k = 0; k <= j; k++)
			{
				sprintf_s(buff, LM_FIELDSIZE, ",%.12lg", CurrentTarget->Covariance(j, k));
				strcat_s(cbuff, LM_LINESIZE, buff);
			}
		}
		fprintf(iTargetFile, "%s\n", cbuff);

		for (j = 0; j < np; j++)
		{
			//print the target points

			//transform each point into vehicle coordinates before printing
			double px = CurrentTarget->TargetPoints(j, 0);
			double py = CurrentTarget->TargetPoints(j, 1);
			double evx;
			double evy;
			ObjectToEgoVehicle(evx, evy, px, py, cosOrient, sinOrient, xt, yt);

			fprintf(iTargetFile, "%.12lg,%d,%d,%d,%d,%.12lg,%.12lg\n", mTransmitTime, i, nt,
				j, np, evx, evy);
		}

		CurrentTarget = CurrentTarget->NextTarget;
	}

	return;
}

void LocalMap::PrintLoosePoints(FILE* iLoosePointsFile)
{
	/*
	Prints the current set of loose (unused) obstacle points to a file.

	INPUTS:
		iLoosePointsFile - the opened file that will be printed to.

	OUTPUTS:
		none.  Prints the set of loose points to the given file
	*/

	int i;
	int np = mTransmitNumLoosePoints;
	int cid = 0;
	int ish = 0;
	double px;
	double py;

	for (i = 0; i < np; i++)
	{
		//extract predicted points and print them
		cid = mTransmitLooseIDs[i];
		ish = mTransmitLoosePointHeights[i];
		px = mTransmitLoosePoints[midx(i, 0, np)];
		py = mTransmitLoosePoints[midx(i, 1, np)];

		fprintf(iLoosePointsFile, "%.12lg,%d,%d,%d,%d,%.12lg,%.12lg\n", 
			mLocalMapTime, i, np, cid, ish, px, py);
	}

	return;
}

void LocalMap::MaintainTargets()
{
	/*
	Performs maintenance on the list of available targets.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	int i;
	int j;

	//PERFORM TARGET MAINTENANCE

	for (i = 0; i < mNumParticles; i++)
	{
		int nt = mParticles[i].NumTargets();

		j = 0;
		Target* CurrentTarget = mParticles[i].FirstTarget();
		while (j < nt)
		{
			//maintain each target, and check if it can be deleted
			CurrentTarget->MaintainTarget();

			double tx = CurrentTarget->X();
			double ty = CurrentTarget->Y();
			double tr = sqrt(tx*tx + ty*ty);
			double tb = atan2(ty, tx);

			if (CurrentTarget->ExistenceProbability() < TARGET_MINEXISTENCEPROB ||
				tr > TARGET_MAXRANGE || CurrentTarget->Width() > TARGET_MAXWIDTH)
			{
				//this target was too old, so remove it
				Target* OldTarget = CurrentTarget;
				CurrentTarget = CurrentTarget->NextTarget;
				mParticles[i].RemoveTarget(OldTarget);
			}
			else
			{
				//this target was ok, so go to the next one
				CurrentTarget = CurrentTarget->NextTarget;
			}

			j++;
		}
	}

	//CHECK FOR TARGET DUPLICATION

	/*
	#ifdef LM_NODUPLICATION
		int k;

		//first reset each target's occupancy grid
		for (i = 0; i < mNumParticles; i++)
		{
			int nt = mParticles[i].NumTargets();

			j = 0;
			Target* jtarget = mParticles[i].FirstTarget();
			while (j < nt)
			{
				jtarget->ResetOccupancyGrid();
				jtarget = jtarget->NextTarget;
				j++;
			}
		}

		//next check each target for possible duplication
		for (i = 0; i < mNumParticles; i++)
		{
			Particle* CurrentParticle = &(mParticles[i]);
			int nt = CurrentParticle->NumTargets();

			j = 0;
			Target* jtarget = CurrentParticle->FirstTarget();
			while (j < nt && jtarget != NULL)
			{
				//check whether each target is a duplicate of another, 
				//and delete the newer duplicates

				bool deletej = false;
				bool deletek = false;

				double jx = jtarget->X();
				double jy = jtarget->Y();

				k = j + 1;
				Target* ktarget = jtarget->NextTarget;
				while (k < nt && ktarget != NULL)
				{
					//check if the jth and kth targets are similar

					double kx = ktarget->X();
					double ky = ktarget->Y();

					double dst = sqrt(pow(jx - kx, 2.0) + pow(jy - ky, 2.0));
					if (dst <= TARGET_MAXDUPERANGE)
					{
						//jth and kth targets were close enough to check for duplication

						if (jtarget->NumPoints() > 0 && ktarget->NumPoints() > 0)
						{
							//both jth and kth targets have clusters

							//set jth and kth targets' occupancy grids if not already computed
							//also dilate the occupancy grids
							if (jtarget->OccupancyGridIsValid() == false)
							{
								jtarget->SetOccupancyGrid();
								jtarget->DilateOccupancyGrid();
							}
							if (ktarget->OccupancyGridIsValid() == false)
							{
								ktarget->SetOccupancyGrid();
								ktarget->DilateOccupancyGrid();
							}

							//compute overlap percentages of each target
							double jok = jtarget->OverlapPercentage(ktarget->TargetGrid());
							double koj = ktarget->OverlapPercentage(jtarget->TargetGrid());
							if (jok >= TARGET_MINDUPEPCT && koj >= TARGET_MINDUPEPCT)
							{
								//each target overlaps the other with high degree: they're duplicates

								if (jtarget->NumMeasurements() >= ktarget->NumMeasurements())
								{
									//jtarget is the older target: delete ktarget
									deletek = true;
								}
								else
								{
									//ktarget is the older target: delete jtarget
									//NOTE: once jtarget is to be deleted, there's no need to continue
									//testing for duplicates
									deletej = true;
									break;
								}
							}

							//printf("jok=%lg, koj=%lg\n", jok, koj);
						}
					}

					if (deletek == true)
					{
						//iterate and delete the kth target
						Target* oldtarget = ktarget;
						ktarget = ktarget->NextTarget;
						CurrentParticle->RemoveTarget(oldtarget);
					}
					else
					{
						//iterate without deleting anything
						ktarget = ktarget->NextTarget;
					}

					k++;
				}

				if (deletej == true)
				{
					//iterate and delete the kth target
					Target* oldtarget = jtarget;
					jtarget = jtarget->NextTarget;
					CurrentParticle->RemoveTarget(oldtarget);
				}
				else
				{
					//iterate without deleting anything
					jtarget = jtarget->NextTarget;
				}

				j++;
			}
		}
	#endif
	*/

	return;
}

void LocalMap::GenerateTargetsMessage(LocalMapTargetsMsg* oTargetsMessage)
{
	/*
	Populates the local map's target message for transmission.

	INPUTS:	
		oTargetsMessage - the message to populate

	OUTPUTS:
		none.  oTargetsMessage is populated on exit.
	*/

	//set the timestamp for the transmit
	oTargetsMessage->timestamp = mTransmitTime;

	int i;
	int j;
	int k;
	int nt;
	Particle* CurrentParticle = mTransmitParticle;
	nt = CurrentParticle->NumTargets();
	if (nt > MAX_LMTX_TARGETS)
	{
		printf("Warning: dropping %d targets for transmit.\n", nt - MAX_LMTX_TARGETS);
		nt = MAX_LMTX_TARGETS;
	}

	//scale the targets list to the appropriate size
	oTargetsMessage->targets.resize(nt);

	Target* CurrentTarget = CurrentParticle->FirstTarget();
	for (i = 0; i < nt; i++)
	{
		//set all the targets

		int np = CurrentTarget->NumPoints();
		if (np > MAX_LMTX_POINTS)
		{
			printf("Warning: dropping %d points for transmit.\n", np - MAX_LMTX_POINTS);
			np = MAX_LMTX_POINTS;
		}

		//pull the target points and send them over the network
		double xt = CurrentTarget->X();
		double yt = CurrentTarget->Y();
		double ot = CurrentTarget->Orientation();
		double st = CurrentTarget->Speed();
		double ht = CurrentTarget->Heading();
		double wt = CurrentTarget->Width();
		double cosOrient = cos(ot);
		double sinOrient = sin(ot);

		/*
		if (fabs(st) > 5.0)
		{
			printf("Speed: %.12lg\n", st);
		}
		*/
		
		oTargetsMessage->targets[i].points.resize(np);
		switch (CurrentTarget->TargetType())
		{
		case T_IBEO:
			oTargetsMessage->targets[i].type = LM_TT_IBEO;
			break;
		case T_IBEOMATURE:
			oTargetsMessage->targets[i].type = LM_TT_IBEOMATURE;
			break;
		case T_MOBILEYE:
			oTargetsMessage->targets[i].type = LM_TT_MOBILEYE;
			break;
		case T_RADAR:
			oTargetsMessage->targets[i].type = LM_TT_RADAR;
			break;
		case T_QUASIMATURE:
			oTargetsMessage->targets[i].type = LM_TT_QUASIMATURE;
			break;
		case T_MATURE:
			oTargetsMessage->targets[i].type = LM_TT_MATURE;
			break;
		default:
			oTargetsMessage->targets[i].type = LM_TT_INVALID;
			break;
		}
		oTargetsMessage->targets[i].x = (float) xt;
		oTargetsMessage->targets[i].y = (float) yt;
		oTargetsMessage->targets[i].orientation = (float) ot;
		oTargetsMessage->targets[i].speed = (float) st;
		oTargetsMessage->targets[i].heading = (float) ht;
		oTargetsMessage->targets[i].width = (float) wt;

		//set the covariance matrix
		for (j = 0; j < T_NUMSTATES; j++)
		{
			for (k = 0; k < T_NUMSTATES; k++)
			{
				oTargetsMessage->targets[i].covariance[midx(j, k, T_NUMSTATES)] = (float) CurrentTarget->Covariance(j, k);
			}
		}

		for (j = 0; j < np; j++)
		{
			//pull the jth target point from the target
			double px = CurrentTarget->TargetPoints(j, 0);
			double py = CurrentTarget->TargetPoints(j, 1);
			//convert to ego vehicle coordinates
			double evx;
			double evy;
			ObjectToEgoVehicle(evx, evy, px, py, cosOrient, sinOrient, xt, yt);

			oTargetsMessage->targets[i].points[j] = LocalMapPoint(evx, evy);
		}

		CurrentTarget = CurrentTarget->NextTarget;
	}

	return;
}

void LocalMap::GenerateLooseClustersMessage(LocalMapLooseClustersMsg* oLooseClustersMessage)
{
	/*
	Populates the local map's loose clusters message for transmission.

	INPUTS:	
		oLooseClustersMessage - the message to populate

	OUTPUTS:
		none.  oLooseClustersMessage is populated on successful exit.
	*/

	int i;
	int ish = 0;
	int idx = 0;
	int nc = 0;
	int cbeg = 0;
	int cend = 0;

	oLooseClustersMessage->timestamp = mTransmitTime;

	//count the number of clusters
	while (cbeg < mTransmitNumLoosePoints)
	{
		cend = cbeg;
		while (mTransmitLooseIDs[cbeg] == mTransmitLooseIDs[cend])
		{
			cend++;
			if (cend == mTransmitNumLoosePoints)
			{
				break;
			}
		}
		cbeg = cend;
		nc++;
	}
	if (nc > MAX_LMTX_CLUSTERS)
	{
		nc = MAX_LMTX_CLUSTERS;
	}
	oLooseClustersMessage->clusters.resize(nc);

	cbeg = 0;
	cend = 0;
	while (cbeg < mTransmitNumLoosePoints)
	{
		//find the beginning and the end of this cluster
		cend = cbeg;
		while (mTransmitLooseIDs[cbeg] == mTransmitLooseIDs[cend])
		{
			cend++;
			if (cend == mTransmitNumLoosePoints)
			{
				break;
			}
		}

		//count the number of points in this cluster
		int np = cend - cbeg;
		if (np > MAX_LMTX_POINTS)
		{
			np = MAX_LMTX_POINTS;
		}
		oLooseClustersMessage->clusters[idx].points.resize(np);
		//determine this cluster's class (high obstacle / low obstacle)
		switch (mTransmitLoosePointHeights[cbeg])
		{
		case CLUSTER_LOWOBSTACLE:
			oLooseClustersMessage->clusters[idx].clusterClass = LOCALMAP_LowObstacle;
			break;
		case CLUSTER_HIGHOBSTACLE:
			oLooseClustersMessage->clusters[idx].clusterClass = LOCALMAP_HighObstacle;
			break;
		}
		if (np == 1)
		{
			#ifdef LM_COMMONDEBUGMSGS
				printf("Warning: single point loose cluster encountered.\n");
			#endif
		}
		for (i = cbeg; i < cend; i++)
		{
			//keep adding points to the cluster as long as there's still room
			if (i - cbeg == MAX_LMTX_POINTS)
			{
				printf("Warning: more points for a loose cluster than MAX_LMTX_POINTS.\n");
				break;
			}

			//extract the point and store it in the proper cluster
			double px = mTransmitLoosePoints[midx(i, 0, mTransmitNumLoosePoints)];
			double py = mTransmitLoosePoints[midx(i, 1, mTransmitNumLoosePoints)];
			oLooseClustersMessage->clusters[idx].points[i-cbeg] = LocalMapPoint(px, py);
		}

		//when done copying the cluster, move to the next
		cbeg = cend;
		idx++;
		if (idx == MAX_LMTX_CLUSTERS)
		{
			//too many clusters to handle
			printf("Warning: more loose clusters than MAX_LMTX_CLUSTERS.\n");
			break;
		}
	}

	return;
}

void LocalMap::DeleteLooseIbeoClusters()
{
	/*
	Frees memory stored as the loose ibeo clusters.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	int i;

	mNumLooseIbeoClusters = 0;

	for (i = 0; i < LM_MAXCLUSTERS; i++)
	{
		//delete the cluster data stored in 
		mLooseIbeoClusters[i].IsHighObstacle = false;
		mLooseIbeoClusters[i].NumPoints = 0;
		delete [] mLooseIbeoClusters[i].Points;
		mLooseIbeoClusters[i].Points = NULL;
	}

	mLooseUnclusterableIbeoPoints.IsHighObstacle = false;
	mLooseUnclusterableIbeoPoints.NumPoints = 0;
	delete [] mLooseUnclusterableIbeoPoints.Points;
	mLooseUnclusterableIbeoPoints.Points = NULL;

	return;
}

void LocalMap::DeleteLooseDriverSideSickClusters()
{
	/*
	Frees memory stored as the loose left side SICK clusters.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	int i;

	mNumLooseDriverSideSickClusters = 0;

	for (i = 0; i < LM_MAXCLUSTERS; i++)
	{
		//delete the cluster data stored in side sick clusters
		mLooseDriverSideSickClusters[i].IsHighObstacle = false;
		mLooseDriverSideSickClusters[i].NumPoints = 0;
		delete [] mLooseDriverSideSickClusters[i].Points;
		mLooseDriverSideSickClusters[i].Points = NULL;
	}

	return;
}

void LocalMap::DeleteLoosePassengerSideSickClusters()
{
	/*
	Frees memory stored as the loose right side SICK clusters.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	int i;

	mNumLoosePassengerSideSickClusters = 0;

	for (i = 0; i < LM_MAXCLUSTERS; i++)
	{
		//delete the cluster data stored in side sick clusters
		mLoosePassengerSideSickClusters[i].IsHighObstacle = false;
		mLoosePassengerSideSickClusters[i].NumPoints = 0;
		delete [] mLoosePassengerSideSickClusters[i].Points;
		mLoosePassengerSideSickClusters[i].Points = NULL;
	}

	return;
}

void LocalMap::DeleteLooseClusteredSickClusters()
{
	/*
	Frees memory stored as the loose clustered SICK clusters.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	int i;

	mNumLooseClusteredSickClusters = 0;

	for (i = 0; i < LM_MAXCLUSTERS; i++)
	{
		//delete the cluster data stored in the clustered sick clusters
		mLooseClusteredSickClusters[i].IsHighObstacle = false;
		mLooseClusteredSickClusters[i].NumPoints = 0;
		delete [] mLooseClusteredSickClusters[i].Points;
		mLooseClusteredSickClusters[i].Points = NULL;
	}
	mLooseUnclusterableClusteredSickPoints.IsHighObstacle = false;
	mLooseUnclusterableClusteredSickPoints.NumPoints = 0;
	delete [] mLooseUnclusterableClusteredSickPoints.Points;
	mLooseUnclusterableClusteredSickPoints.Points = NULL;

	return;
}
