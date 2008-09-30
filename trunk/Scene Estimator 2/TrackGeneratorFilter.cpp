#include "TrackGeneratorFilter.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

TrackGeneratorFilter::TrackGeneratorFilter(RandomNumberGenerator* iRandomNumberGenerator, RoadGraph* iRoadGraph)
{
	/*
	Default constructor for the track generator.  Creates the track 
	generator with placeholder values in the variables.

	INPUTS:
		iRandomNumberGenerator - pointer to the random number generator used to fill the cache
		iRoadGraph - pointer to the road graph used to generate partition ID memberships

	OUTPUTS:
		none.
	*/

	//store the input random number generator
	mTGGenerator = iRandomNumberGenerator;
	while (mGaussianCache.AddNumber(mTGGenerator->RandGaussian()) == false)
	{
		//add numbers to the cache until it is full
	}

	//store the input road graph
	mRoadGraph = iRoadGraph;

	mTrackGeneratorTime = -DBL_MAX;
	mIsInitialized = false;

	mNumLoosePoints = 0;
	mLooseClusterIDs = NULL;
	mLoosePointHeights = NULL;
	mLoosePoints = NULL;

	mNumTracks = 0;
	mFirstTrack = NULL;

	mAIFirstTrack = NULL;
	mAINumLoosePoints = 0;
	mAILooseClusterIDs = NULL;
	mAILoosePointHeights = NULL;
	mAILoosePoints = NULL;

	return;
}

TrackGeneratorFilter::~TrackGeneratorFilter()
{
	/*
	Destructor for the track generator.  Frees memory stored in the
	track generator and gets it ready for deletion.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	//free the memory stored in loose obstacle points
	mNumLoosePoints = 0;
	delete [] mLooseClusterIDs;
	delete [] mLoosePointHeights;
	delete [] mLoosePoints;

	//free the memory stored in the tracks and release their IDs
	RemoveAllTracks();

	//free the memory stored in the transmit variables
	Track* CurrentTrack = mAIFirstTrack;
	while (CurrentTrack != NULL)
	{
		Track* OldTrack = CurrentTrack;
		CurrentTrack = CurrentTrack->NextTrack;
		delete OldTrack;
	}

	mAINumLoosePoints = 0;
	delete [] mAILooseClusterIDs;
	delete [] mAILoosePointHeights;
	delete [] mAILoosePoints;

	return;
}

void TrackGeneratorFilter::AddTrack(Track* iNewTrack)
{
	/*
	Adds a new track to the list of tracked targets.  The new
	track must be valid and already initialized with an ID.

	INPUTS:
		iNewTrack - the new track to add to the list

	OUTPUTS:
		none.
	*/

	if (iNewTrack == NULL)
	{
		//don't add an empty track to the list
		return;
	}
	if (iNewTrack->IsInitialized() == false)
	{
		//don't add a track that hasn't been initialized

		//NOTE: iNewTrack is usually freed when the generator is deleted,
		//but since it's not entering the generator, delete it here
		delete iNewTrack;
		return;
	}

	//increment the number of tracks stored
	mNumTracks++;

	//store the existing first track
	Track* OldFirstTrack = mFirstTrack;

	//put the new track at the head of the list
	mFirstTrack = iNewTrack;
	//set the new target's pointers
	iNewTrack->NextTrack = OldFirstTrack;
	iNewTrack->PrevTrack = NULL;

	if (OldFirstTrack != NULL)
	{
		//set the old first track's pointer too
		OldFirstTrack->PrevTrack = mFirstTrack;
	}

	return;
}

void TrackGeneratorFilter::RemoveTrack(Track* iOldTrack)
{
	/*
	Removes a particular track from the list of tracks.  Frees
	memory allocated in the deleted track, and releases the
	track's ID number for reuse.

	INPUTS:
		iOldTrack - pointer to the track to be deleted.

	OUTPUTS:
		none.
	*/

	if (iOldTrack == NULL)
	{
		//don't remove an empty track
		return;
	}

	//decrement the number of tracks
	mNumTracks--;

	//fix up the links in the linked list
	Track* PrevTrack = iOldTrack->PrevTrack;
	Track* NextTrack = iOldTrack->NextTrack;

	if (PrevTrack != NULL)
	{
		PrevTrack->NextTrack = NextTrack;
	}
	if (NextTrack != NULL)
	{
		NextTrack->PrevTrack = PrevTrack;
	}

	//check to see if we're deleting the first track in the generator
	if (iOldTrack == mFirstTrack)
	{
		//if the first track is to be deleted, update the list pointer
		mFirstTrack = NextTrack;
	}

	//release the track's ID
	mIDGenerator.ReleaseID(iOldTrack->ID());

	//finally, free memory allocated to the track
	delete iOldTrack;
	//and invalidate the pointer so data isn't corrupted
	iOldTrack = NULL;

	return;
}

void TrackGeneratorFilter::RemoveAllTracks()
{
	/*
	Removes all tracks from the track generator, freeing memory
	and releasing track IDs.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	//delete all the tracks in the list
	while (mFirstTrack != NULL)
	{
		Track* CurrentTrack = mFirstTrack;
		RemoveTrack(CurrentTrack);
	}

	return;
}

void TrackGeneratorFilter::GenerateUntrackedClusterMessage(SceneEstimatorUntrackedClusterMsg* oUntrackedClusterMsg)
{
	/*
	Generates an untracked cluster message for transmit to the obstacle avoidance block.

	INPUTS:
		oUntrackedClusterMsg - the untracked cluster message, which will be populated on output.

	OUTPUTS:
		none.  Fills iUntrackedClusterMsg with the untracked cluster data.
	*/

	int i;
	int idx = 0;
	int nc = 0;
	int cbeg = 0;
	int cend = 0;

	oUntrackedClusterMsg->timestamp = mAITime;

	//count the number of clusters
	while (cbeg < mAINumLoosePoints)
	{
		cend = cbeg;
		while (mAILooseClusterIDs[cbeg] == mAILooseClusterIDs[cend])
		{
			cend++;
			if (cend == mAINumLoosePoints)
			{
				break;
			}
		}
		cbeg = cend;
		nc++;
	}
	oUntrackedClusterMsg->untrackedClusters.resize(nc);

	cbeg = 0;
	cend = 0;
	while (cbeg < mAINumLoosePoints)
	{
		//find the beginning and the end of this cluster
		cend = cbeg;
		while (mAILooseClusterIDs[cbeg] == mAILooseClusterIDs[cend])
		{
			cend++;
			if (cend == mAINumLoosePoints)
			{
				break;
			}
		}

		//count the number of points in this cluster
		int np = cend - cbeg;
		oUntrackedClusterMsg->untrackedClusters[idx].points.resize(np);
		//determine this cluster's class (high obstacle / low obstacle)
		switch (mAILoosePointHeights[cbeg])
		{
		case CLUSTER_LOWOBSTACLE:
			oUntrackedClusterMsg->untrackedClusters[idx].clusterClass = SCENE_EST_LowObstacle;
			break;
		case CLUSTER_HIGHOBSTACLE:
			oUntrackedClusterMsg->untrackedClusters[idx].clusterClass = SCENE_EST_HighObstacle;
			break;
		}

		if (np == 1)
		{
			#ifdef SE_COMMONDEBUGMSGS
				printf("Warning: single point loose cluster encountered.\n");
			#endif
		}
		for (i = cbeg; i < cend; i++)
		{
			//extract the point and store it in the proper cluster
			double px = mAILoosePoints[midx(i, 0, mAINumLoosePoints)];
			double py = mAILoosePoints[midx(i, 1, mAINumLoosePoints)];
			oUntrackedClusterMsg->untrackedClusters[idx].points[i-cbeg] = SceneEstimatorClusterPoint(px, py);
		}

		//when done copying the cluster, move to the next
		cbeg = cend;
		idx++;
	}

	return;
}

void TrackGeneratorFilter::GenerateTrackedClusterMessage(SceneEstimatorTrackedClusterMsg* oTrackedClusterMsg, PosteriorPosePosition* iPosteriorPose)
{
	/*
	Populates a tracked clusters message for transmission to the arbiter.  The message
	is populated using the mAI* variables, so make sure these are predicted for transmit
	prior to calling this function.

	INPUTS:
		oTrackedClusterMsg - the tracked cluster message, which will be populated on output.
		iPosteriorPose - current posterior pose solution, valid at the time of the transmit.

	OUTPUTS:
		oTrackedClusterMsg - populated with tracks on output.
	*/

	int h;
	int i;
	int j;
	int k;
	int nt = mNumTracks;

	//extract parameters from posterior pose
	double ave = iPosteriorPose->EastMMSE;
	double avn = iPosteriorPose->NorthMMSE;
	double avh = iPosteriorPose->HeadingMMSE;

	double Penh[9];
	for (i = 0; i < 3; i++)
	{
		for (j = 0; j < 3; j++)
		{
			Penh[midx(i, j, 3)] = iPosteriorPose->CovarianceMMSE[midx(i, j, PPP_NUMSTATES)];
		}
	}
	double Vavh = iPosteriorPose->CovarianceMMSE[midx(2, 2, PPP_NUMSTATES)];

	double cosavh = cos(avh);
	double sinavh = sin(avh);

	//these will store transformed points in absolute coordinates
	double ae;
	double an;

	//this will be used for the uncertainty transformation in obstacle tracks
	double Pbig[8*8];
	//define the big covariance matrix
	for (i = 0; i < 8; i++)
	{
		for (j = 0; j < 8; j++)
		{
			Pbig[midx(i, j, 8)] = 0.0;
		}
	}
	for (i = 5; i < 8; i++)
	{
		for (j = 5; j < 8; j++)
		{
			Pbig[midx(i, j, 8)] = Penh[midx(i-5, j-5, 3)];
		}
	}

	//these will be used to create the monte carlo sample for each track
	int npe = TG_NUMTRACKPARTICLES;
	double track_sample[2*TG_NUMTRACKPARTICLES];
	double oonpe = 1.0 / ((double) TG_NUMTRACKPARTICLES);

	//BEGIN SETTING MESSAGE

	//set the message time
	oTrackedClusterMsg->timestamp = mAITime;

	//set message size and data
	oTrackedClusterMsg->trackedClusters.resize(nt);
	Track* CurrentTrack = mAIFirstTrack;
	for (i = 0; i < nt; i++)
	{
		//perform maintenance on the track before sending
		CurrentTrack->MaintainTrack();

		//extract parameters from the track
		double tx = CurrentTrack->X();
		double ty = CurrentTrack->Y();
		double to = CurrentTrack->Orientation();
		double ts = CurrentTrack->Speed();
		double th = CurrentTrack->Heading();

		double Pxyo[9];
		for (j = 0; j < 3; j++)
		{
			for (k = 0; k < 3; k++)
			{
				Pxyo[midx(j, k, 3)] = CurrentTrack->Covariance(j, k);
			}
		}

		/*
		if (tx > 10.0 && tx < 20.0 && ty < 0.0 && ty > -25.0)
		{
			int foo = 1.3;
			for (j = 0; j < 3; j++)
			{
				printf("%.12lg\t%.12lg\t%.12lg\n", CurrentTrack->Covariance(j, 0), CurrentTrack->Covariance(j, 1), CurrentTrack->Covariance(j, 2));
			}
			printf("\n");
		}
		*/

		//continue to populate Pbig...
		for (j = 2; j < 5; j++)
		{
			for (k = 2; k < 5; k++)
			{
				Pbig[midx(j, k, 8)] = Pxyo[midx(j-2, k-2, 3)];
			}
		}

		double costo = cos(to);
		double sinto = sin(to);

		double tcp = CurrentTrack->CarProbability();
		int tid = CurrentTrack->ID();
		int tsf = CurrentTrack->StatusFlag();
		double tsp = CurrentTrack->StoppedProbability();

		double evx;
		double evy;

		int np = CurrentTrack->NumTrackPoints();
		//size the vector to the appropriate number of points
		oTrackedClusterMsg->trackedClusters[i].points.resize(np);

		//find the closest point to the ego-vehicle, in relative coordinates
		double rBest = DBL_MAX;
		double xBest = DBL_MAX;
		double yBest = DBL_MAX;
		for (j = 0; j < np; j++)
		{
			//extract each vehicle point, in object storage frame
			double osx = CurrentTrack->TrackPoints(j, 0);
			double osy = CurrentTrack->TrackPoints(j, 1);
			//convert point to ego vehicle coordinates
			ObjectToEgoVehicle(evx, evy, osx, osy, costo, sinto, tx, ty);

			//check if this point is closer than the current closest
			double rCur = sqrt(evx*evx + evy*evy);
			if (rCur <= rBest)
			{
				//found a closer point
				rBest = rCur;
				xBest = evx;
				yBest = evy;
			}

			//copy the point to the track message, in ego vehicle coordinates
			oTrackedClusterMsg->trackedClusters[i].points[j] = SceneEstimatorClusterPoint(evx, evy);
		}

		//set the closest range to the track
		oTrackedClusterMsg->trackedClusters[i].range = (float) (sqrt(xBest*xBest + yBest*yBest));
		//convert the closest point to absolute coordinates and store it in the message
		EgoVehicleToAbsolute(ae, an, xBest, yBest, cosavh, sinavh, ave, avn);
		oTrackedClusterMsg->trackedClusters[i].xClosestPoint = (float) ae;
		oTrackedClusterMsg->trackedClusters[i].yClosestPoint = (float) an;
		
		//set the track speed
		oTrackedClusterMsg->trackedClusters[i].speed = (float) ts;

		if (CurrentTrack->SpeedIsValid() == true)
		{
			//set whether the speed is valid or not
			oTrackedClusterMsg->trackedClusters[i].speedValid = true;
		}
		else
		{
			//speed is invalid
			oTrackedClusterMsg->trackedClusters[i].speedValid = false;
			//if speed is invalid, substitute in the sketchy speed estimate
			oTrackedClusterMsg->trackedClusters[i].speed = (float) CurrentTrack->aveSpeed();
		}

		//set the track heading (relative and absolute)
		oTrackedClusterMsg->trackedClusters[i].relHeading = (float) th;
		oTrackedClusterMsg->trackedClusters[i].absHeading = (float) UnwrapAngle(th + avh);

		if (CurrentTrack->HeadingIsValid() == true)
		{
			//set whether the heading is valid or not
			oTrackedClusterMsg->trackedClusters[i].headingValid = true;
		}
		else
		{
			oTrackedClusterMsg->trackedClusters[i].headingValid = false;
			//if heading is invalid, substitute in the sketchy heading estimate
			oTrackedClusterMsg->trackedClusters[i].relHeading = (float) CurrentTrack->aveHeading();
			oTrackedClusterMsg->trackedClusters[i].absHeading = (float) UnwrapAngle(CurrentTrack->aveHeading() + avh);
		}

		/*
		int foo = 1.3;
		if (tx > 10.0 && tx < 20.0 && ty < 0.0 && ty > -25.0)
		{
			if (CurrentTrack->SpeedIsValid() == true)
			{
				//printf("Speed: %.12lg, Std: %.12lg\n", CurrentTrack->Speed(), sqrt(CurrentTrack->Covariance(3, 3)));
				printf("Speed: %.12lg, %.12lg\n", CurrentTrack->Speed(), CurrentTrack->aveSpeed());
				//printf("Hdg: %.12lg, Std: %.12lg\n", CurrentTrack->Heading(), sqrt(CurrentTrack->Covariance(4, 4)));
			}
		}
		*/

		//printf("Target car probability: %.12lg.\n", tcp);

		//set the track class
		if (tcp >= 0.5)
		{
			oTrackedClusterMsg->trackedClusters[i].targetClass = TARGET_CLASS_CARLIKE;
		}
		else
		{
			oTrackedClusterMsg->trackedClusters[i].targetClass = TARGET_CLASS_NOTCARLIKE;
		}

		//set the ID
		oTrackedClusterMsg->trackedClusters[i].id = tid;

		//set the status flag (active or deleted)
		switch (tsf)
		{
		case T_STATUSACTIVE:
			oTrackedClusterMsg->trackedClusters[i].statusFlag = TARGET_STATUS_ACTIVE;
			break;

		case T_STATUSFULLOCCLUDED:
			oTrackedClusterMsg->trackedClusters[i].statusFlag = TARGET_STATUS_OCCLUDED_FULL;
			break;

		case T_STATUSPARTOCCLUDED:
			oTrackedClusterMsg->trackedClusters[i].statusFlag = TARGET_STATUS_OCCLUDED_PARTIAL;
			break;

		case T_STATUSDELETED:
			oTrackedClusterMsg->trackedClusters[i].statusFlag = TARGET_STATUS_DELETED;
			break;

		default:
			oTrackedClusterMsg->trackedClusters[i].statusFlag = TARGET_STATUS_DELETED;
			break;
		}

		//set whether the track is stopped or not
		if (tsp >= 0.5)
		{
			oTrackedClusterMsg->trackedClusters[i].isStopped = true;
		}
		else
		{
			oTrackedClusterMsg->trackedClusters[i].isStopped = false;
		}

		//PARTITION OCCUPANCY

		//calculate what partitions the track is on...
		RoadPartition* rpcenter = CurrentTrack->ClosestPartition();
		int nn = 0;
		if (rpcenter != NULL)
		{
			nn = rpcenter->NumNearbyPartitions();
		}

		//temporary storage for track closest partitions and their probabilities
		vector <SceneEstimatorClusterPartition> cpTrack;
		//resize the set of closest partitions appropriately
		cpTrack.resize(nn);

		//calculate an object ellipse (not quite a bounding ellipse) in the object storage frame
		//this ellipse mimics "uncertainty" in the offset from the object's center of mass
		double wt = 1.0 / ((double) np);
		double xcm = 0.0;
		double ycm = 0.0;
		for (j = 0; j < np; j++)
		{
			xcm += wt * CurrentTrack->TrackPoints(j, 0);
			ycm += wt * CurrentTrack->TrackPoints(j, 1);
		}
		double Pobj[4] = {0.0, 0.0, 0.0, 0.0};
		for (j = 0; j < np; j++)
		{
			double dx = CurrentTrack->TrackPoints(j, 0) - xcm;
			double dy = CurrentTrack->TrackPoints(j, 1) - ycm;
			Pobj[midx(0, 0, 2)] += wt*dx*dx;
			Pobj[midx(0, 1, 2)] += wt*dx*dy;
			Pobj[midx(1, 1, 2)] += wt*dy*dy;
		}
		Pobj[midx(1, 0, 2)] = Pobj[midx(0, 1, 2)];

		//finish populating Pbig...
		for (j = 0; j < 2; j++)
		{
			for (k = 0; k < 2; k++)
			{
				Pbig[midx(j, k, 8)] = Pobj[midx(j, k, 2)];
			}
		}

		ObjectToEgoVehicle(evx, evy, xcm, ycm, costo, sinto, tx, ty);
		//transform the object ellipse into absolute coordinates
		//using jacobian transformation on the uncertainty state:
		//[xcm, ycm, tx, ty, to, ave, avn, avh]

		//define the jacobian matrix
		double J[2*8];
		J[midx(0, 0, 2)] = cosavh*costo - sinavh*sinto;
		J[midx(0, 1, 2)] = -cosavh*sinto - sinavh*costo;
		J[midx(0, 2, 2)] = cosavh;
		J[midx(0, 3, 2)] = -sinavh;
		J[midx(0, 4, 2)] = -cosavh*(sinto*xcm + costo*ycm) - sinavh*(costo*xcm - sinto*ycm);
		J[midx(0, 5, 2)] = 1.0;
		J[midx(0, 6, 2)] = 0.0;
		J[midx(0, 7, 2)] = -sinavh*evx - cosavh*evy;
		J[midx(1, 0, 2)] = sinavh*costo + cosavh*sinto;
		J[midx(1, 1, 2)] = -sinavh*sinto + cosavh*costo;
		J[midx(1, 2, 2)] = sinavh;
		J[midx(1, 3, 2)] = cosavh;
		J[midx(1, 4, 2)] = -sinavh*(sinto*xcm + costo*ycm) + cosavh*(costo*xcm - sinto*ycm);
		J[midx(1, 5, 2)] = 0.0;
		J[midx(1, 6, 2)] = 1.0;
		J[midx(1, 7, 2)] = cosavh*evx - sinavh*evy;

		double JP[2*8];
		//compute the final object covariance, J*Pbig*J'
		for (j = 0; j < 2; j++)
		{
			for (k = 0; k < 8; k++)
			{
				JP[midx(j, k, 2)] = 0.0;
				for (h = 0; h < 8; h++)
				{
					JP[midx(j, k, 2)] += J[midx(j, h, 2)] * Pbig[midx(h, k, 8)];
				}
			}
		}
		double Pxy[4];
		for (j = 0; j < 2; j++)
		{
			for (k = 0; k < 2; k++)
			{
				Pxy[midx(j, k, 2)] = 0.0;
				for (h = 0; h < 8; h++)
				{
					Pxy[midx(j, k, 2)] += JP[midx(j, h, 2)] * J[midx(k, h, 2)];
				}
			}
		}

		//calculate the cholesky of Pxy for drawing data according to the object's density
		double CholP[4];
		double a = Pxy[midx(0, 0, 2)];
		double b = Pxy[midx(0, 1, 2)];
		double c = Pxy[midx(1, 1, 2)];
		double a11 = sqrt(a);
		double a12;
		if (fabs(a11) > 0.0)
		{
			a12 = b/a11;
		}
		else
		{
			a12 = 0.0;
		}
		double a22;
		if (c > a12*a12)
		{
			a22 = sqrt(c - a12*a12);
		}
		else
		{
			a22 = sqrt(c);
		}
		CholP[midx(0, 0, 2)] = a11;
		CholP[midx(0, 1, 2)] = a12;
		CholP[midx(1, 0, 2)] = 0.0;
		CholP[midx(1, 1, 2)] = a22;

		//use the cholesky factorization to skew the samples to the track's distribution in configuration space
		//NOTE: centered at the anchor point in absolute coordinates
		//EgoVehicleToAbsolute(ae, an, tx, ty, cosavh, sinavh, ave, avn);
		//NOTE: centered at the center of mass point in absolute coordinates
		EgoVehicleToAbsolute(ae, an, evx, evy, cosavh, sinavh, ave, avn);
		for (j = 0; j < npe; j++)
		{
			//skew a unit normal to the track's distribution
			double rval[2];
			rval[0] = RandGaussian();
			rval[1] = RandGaussian();
			//this affine transform is chol(P)'*randn() + [ae; an]
			track_sample[midx(0, j, 2)] = (CholP[midx(0, 0, 2)]*rval[0] + CholP[midx(1, 0, 2)]*rval[1]) + ae;
			track_sample[midx(1, j, 2)] = (CholP[midx(0, 1, 2)]*rval[0] + CholP[midx(1, 1, 2)]*rval[1]) + an;
		}

		//use the ellipse Pxy as the measure of the object; test against intersection with the partition
		for (j = 0; j < nn; j++)
		{
			//pull each nearby partition
			RoadPartition* rpcur = rpcenter->NearbyPartition(j);

			//calculate partition probability as the fraction of points that lie in the partition
			int inpartcount = 0;
			for (k = 0; k < npe; k++)
			{
				double etest = track_sample[midx(0, k, 2)];
				double ntest = track_sample[midx(1, k, 2)];

				if (rpcur->IsOnPartition(etest, ntest) == true)
				{
					//this track sample point is on the partition
					inpartcount++;
				}
			}
			double oprob = ((double) inpartcount) * oonpe;
			cpTrack[j] = SceneEstimatorClusterPartition(rpcur->PartitionID(), (float) oprob, oTrackedClusterMsg);
			//done processing this partition
		}

		//threshold and remove partitions with low probabilities
		for (j = 0; j < nn; j++)
		{
			if (cpTrack[j].Probability() >= TG_MINPARTITIONPROBABILITY)
			{
				//this partition has a high enough probability to be sent
				oTrackedClusterMsg->trackedClusters[i].closestPartitions.push_back(cpTrack[j]);
			}
		}

		/*
		int foo = 1.3;
		if (tx > 10.0 && tx < 30.0 && fabs(ty) < 3.0)
		{
			//printf("%.12lg\t%.12lg\n%.12lg\t%.12lg\n\n", Pxy[0], Pxy[1], Pxy[2], Pxy[3]);
			//printf("Tahoe: %.12lg, %.12lg\n", ae, an);
			printf("Tahoe %d with %d partitions.\n", CurrentTrack->ID(), (int) oTrackedClusterMsg->trackedClusters[i].closestPartitions.size());
		}
		*/

		//done processing this track
		CurrentTrack = CurrentTrack->NextTrack;
		//when finished with the track, replace one gaussian number in the cache
		ReplaceGaussian();
	}

	return;
}

void TrackGeneratorFilter::Predict(double iPredictTime, VehicleOdometry* iVehicleOdometry, PosteriorPosePosition* iPosteriorPosePosition)
{
	/*
	Predicts all the tracks and loose obstacle points in the track generator forward to
	iPredictTime using the tracks' motion and the vehicle's odometry.

	INPUTS:
		iPredictTime - time to which the track generator will be predicted.
		iVehicleOdometry - vehicle odometry structure valid during the
			prediction times.
		iPosteriorPosePosition - the current posterior pose position, valid at the end
			of the prediction

	OUTPUTS:
		none.  Updates the tracks and the loose obstacle points to the time of the prediction
	*/

	if (iVehicleOdometry->IsValid == false)
	{
		//do not predict if odometry is invalid
		printf("Predit Failed: Odom Invalid\n");
		return;
	}

	if (mIsInitialized == false)
	{
		#ifdef SE_COMMONDEBUGMSGS
			printf("Warning: TrackGenerator::Predict failed because TrackGenerator wasn't initialized.\n");
		#endif

		//can't update until ppose is initialized
		return;
	}

	//extract relevant odometry information
	double dt = iPredictTime - mTrackGeneratorTime;
	if (dt < 0.0)
	{
		printf("Warning: TrackGenerator::Predict failed because dt = %.12lg ms.\n", dt*1000.0);
		//do not predict tracks backwards
		return;
	}

	//***PREDICT TRACKS FORWARD
	int i;
	int nt = mNumTracks;
	Track* CurrentTrack = mFirstTrack;

	for (i = 0; i < nt; i++)
	{
		//predict each track forward
		CurrentTrack->Predict(dt, iVehicleOdometry, iPosteriorPosePosition, mRoadGraph);
		CurrentTrack = CurrentTrack->NextTrack;
	}

	//***PREDICT LOOSE OBSTACLE POINTS FORWARD

	if (mNumLoosePoints > 0)
	{
		PredictPoints(dt, mNumLoosePoints, mLoosePoints, iVehicleOdometry);
	}

	//***END LOOSE OBSTACLE PREDICTION

	//store the new timestamp
	mTrackGeneratorTime = iPredictTime;

	return;
}

bool TrackGeneratorFilter::PredictForTransmit(double iPredictTime, VehicleOdometry* iVehicleOdometry, 
	PosteriorPosePosition* iPosteriorPosePosition)
{
	/*
	Prepares the track generator for transmit by copying all salient variables
	into temporary transmit objects and predicting those objects forward to 
	iPredictTime, the time for transmit (the current time).

	INPUTS:
		iPredictTime - time to which the track generator will be predicted.
		iVehicleOdometry - vehicle odometry structure valid during the
			prediction times.
		iPosteriorPosePosition - current posterior pose solution, valid at the
			end of the prediction time

	OUTPUTS:
		rSuccess - true if the transmit variables are populated successfully, 
			false otherwise.  If successful, updates the tracks and the loose 
			obstacle points to the desired time of transmit.
	*/

	bool rSuccess = false;

	if (iVehicleOdometry->IsValid == false)
	{
		//do not predict if odometry is invalid
		return rSuccess;
	}

	if (mIsInitialized == false)
	{
		//can't update until ppose is initialized
		return rSuccess;
	}

	//extract relevant odometry information
	double dt = iPredictTime - mTrackGeneratorTime;
	if (dt < 0.0)
	{
		//do not predict tracks backwards
		return rSuccess;
	}

	int i;

	//***PREDICT TRACKS FORWARD

	//first delete the existing AI tracks
	Track* CurrentTrack = mAIFirstTrack;
	while (CurrentTrack != NULL)
	{
		Track* OldTrack = CurrentTrack;
		CurrentTrack = CurrentTrack->NextTrack;
		delete OldTrack;
	}
	mAIFirstTrack = NULL;

	//next copy the existing tracks into the AI variables
	CurrentTrack = mFirstTrack;
	Track* LastAITrack = NULL;
	for (i = 0; i < mNumTracks; i++)
	{
		//create a new track that mirrors an existing track
		Track* NewTrack = new Track(CurrentTrack);

		//store the new track in a linked list of AI Tracks
		if (LastAITrack == NULL)
		{
			mAIFirstTrack = NewTrack;
			NewTrack->PrevTrack = NULL;
		}
		else
		{
			NewTrack->PrevTrack = LastAITrack;
			LastAITrack->NextTrack = NewTrack;
		}

		NewTrack->NextTrack = NULL;
		LastAITrack = NewTrack;

		CurrentTrack = CurrentTrack->NextTrack;
	}

	//and finally predict each AI track to the current time
	CurrentTrack = mAIFirstTrack;
	for (i = 0; i < mNumTracks; i++)
	{
		CurrentTrack->Predict(dt, iVehicleOdometry, iPosteriorPosePosition, mRoadGraph);
		CurrentTrack = CurrentTrack->NextTrack;
	}

	//***PREDICT LOOSE OBSTACLE POINTS FORWARD

	//first delete the existing loose objects
	mAINumLoosePoints = mNumLoosePoints;
	delete [] mAILooseClusterIDs;
	mAILooseClusterIDs = NULL;
	delete [] mAILoosePointHeights;
	mAILoosePointHeights = NULL;
	delete [] mAILoosePoints;
	mAILoosePoints = NULL;

	int np = mAINumLoosePoints;

	if (np > 0)
	{
		//declare memory for the unused points to transmit

		mAILooseClusterIDs = new int[np];
		mAILoosePointHeights = new int[np];
		mAILoosePoints = new double[2*np];

		for (i = 0; i < np; i++)
		{
			//copy the existing loose obstacle points
			mAILooseClusterIDs[i] = mLooseClusterIDs[i];
			mAILoosePointHeights[i] = mLoosePointHeights[i];
			mAILoosePoints[midx(i, 0, np)] = mLoosePoints[midx(i, 0, np)];
			mAILoosePoints[midx(i, 1, np)] = mLoosePoints[midx(i, 1, np)];
		}

		//predict the loose points forward to the time of transmit
		PredictPoints(dt, mAINumLoosePoints, mAILoosePoints, iVehicleOdometry);
	}

	//***END LOOSE OBSTACLE PREDICTION

	//store the new timestamp
	mAITime = iPredictTime;

	rSuccess = true;
	return rSuccess;
}

void TrackGeneratorFilter::Initialize(double iInitialTime)
{
	/*
	Initializes the TrackGenerator with a valid timestamp

	INPUTS:
		iInitialTime - the initial timestamp used for initialization
		iRoadGraph - road graph structure used in scene estimator

	OUTPUTS:
		none.  Initializes the TrackGenerator (or reinitializes)
	*/

	if (mIsInitialized == false)
	{
		//initialize the TrackGenerator on a valid timestamp
		printf("Initializing TrackGenerator...\n");
	}

	mTrackGeneratorTime = iInitialTime;

	//delete any existing tracks
	RemoveAllTracks();

	//delete any existing obstacle points
	mNumLoosePoints = 0;
	delete [] mLooseClusterIDs;
	mLooseClusterIDs = NULL;
	delete [] mLoosePointHeights;
	mLoosePointHeights = NULL;
	delete [] mLoosePoints;
	mLoosePoints = NULL;

	//delete any memory stored in the transmit variables
	Track* CurrentTrack = mAIFirstTrack;
	while (CurrentTrack != NULL)
	{
		Track* OldTrack = CurrentTrack;
		CurrentTrack = CurrentTrack->NextTrack;
		delete OldTrack;
	}
	mAIFirstTrack = NULL;

	mAINumLoosePoints = 0;
	delete [] mAILooseClusterIDs;
	mAILooseClusterIDs = NULL;
	delete [] mAILoosePointHeights;
	mAILoosePointHeights = NULL;
	delete [] mAILoosePoints;
	mAILoosePoints = NULL;

	if (mIsInitialized == false)
	{
		printf("TrackGenerator initialized.\n");
	}
	
	mIsInitialized = true;

	return;
}

void TrackGeneratorFilter::UpdateWithLocalMapPoints(double iUpdateTime, int iNumPoints, double* iLocalMapPacket)
{
	/*
	Updates the track generator with a new set of LocalMap unused obstacle points
	(loose points).

	INPUTS:
		iUpdateTime - the timestamp on the points
		iNumPoints - number of points in the update
		iLocalMapPacket - the actual localmap data

	OUTPUTS:
		none.  Stores the given loose obstacle points and deletes the old ones
	*/

	if (mIsInitialized == false)
	{
		//can't update before filter is initialized
		return;
	}

	//delete the old loose obstacle points
	mNumLoosePoints = 0;
	delete [] mLooseClusterIDs;
	mLooseClusterIDs = NULL;
	delete [] mLoosePointHeights;
	mLoosePointHeights = NULL;
	delete [] mLoosePoints;
	mLoosePoints = NULL;

	//copy over the new points
	mNumLoosePoints = iNumPoints;

	if (mNumLoosePoints == 0)
	{
		//no need to store anything if there are no points
		return;
	}

	//declare space for new points
	int i;
	int np = mNumLoosePoints;
	mLooseClusterIDs = new int[np];
	mLoosePointHeights = new int[np];
	mLoosePoints = new double[2*np];

	for (i = 0; i < mNumLoosePoints; i++)
	{
		//copy over the cluster ID
		mLooseClusterIDs[i] = (int) iLocalMapPacket[midx(i, 3, np)];
		//copy over the point height
		if (iLocalMapPacket[midx(i, 4, np)] == 1.0)
		{
			mLoosePointHeights[i] = CLUSTER_HIGHOBSTACLE;
		}
		else
		{
			mLoosePointHeights[i] = CLUSTER_LOWOBSTACLE;
		}
		//copy over the cluster point
		mLoosePoints[midx(i, 0, np)] = iLocalMapPacket[midx(i, 5, np)];
		mLoosePoints[midx(i, 1, np)] = iLocalMapPacket[midx(i, 6, np)];
	}

	//store the most recent update time
	mTrackGeneratorTime = iUpdateTime;

	return;
}

void TrackGeneratorFilter::UpdateWithLocalMapTargets(double iUpdateTime, int iNumTargets, double* iLocalMapTargets, 
	double* iLocalMapCovariances, int iNumPoints, double* iLocalMapPoints, PosteriorPosePosition* iPosteriorPosePosition)
{
	/*
	Updates the track generator with targets from the LocalMap.

	INPUTS:
		iUpdateTime - the timestamp on the local map targets.
		iNumTargets - number of targets at this update
		iLocalMapTargets - the actual target packet data from the localmap
		iLocalMapCovariances - the local map target covariance matrices
		iNumPoints - total number of points in the packet
		iLocalMapPoints - the target points for the targets
		iPosteriorPosePosition - pointer to a posterior pose position 
			structure giving the posterior pose solution at the time 
			of the update

	OUTPUTS:
		none.  Updates the tracks with the LocalMap targets.
	*/

	if (mIsInitialized == false)
	{
		//can't update before filter is initialized
		return;
	}

	//first extract all LocalMap targets into temporary target data structures
	int nr = iNumPoints;
	int nt = mNumTracks;
	int nz = iNumTargets;

	Target* LocalMapTargets = NULL;
	if (nz > 0)
	{
		LocalMapTargets = new Target[nz];
	}

	int h = 0;
	int i;
	int j;
	int k;
	int idx;

	for (i = 0; i < nz; i++)
	{
		//first pull the target's state
		LocalMapTargets[i].X = iLocalMapTargets[midx(i, 4, nz)];
		LocalMapTargets[i].Y = iLocalMapTargets[midx(i, 5, nz)];
		LocalMapTargets[i].Orientation = iLocalMapTargets[midx(i, 6, nz)];
		LocalMapTargets[i].Speed = iLocalMapTargets[midx(i, 7, nz)];
		LocalMapTargets[i].Heading = iLocalMapTargets[midx(i, 8, nz)];
		int np = (int) iLocalMapTargets[midx(i, 10, nz)];

		//now pull the target's full covariance
		LocalMapTargets[i].Covariance = new double[6*6];
		double* lmc = LocalMapTargets[i].Covariance;
		idx = 3;
		for (j = 0; j < 6; j++)
		{
			for (k = 0; k <= j; k++)
			{
				lmc[midx(j, k, 6)] = iLocalMapCovariances[midx(i, idx, nz)];
				idx++;
			}
		}
		//fill in the upper triangle for a symmetric covariance
		for (j = 0; j < 6; j++)
		{
			for (k = j+1; k < 6; k++)
			{
				lmc[midx(j, k, 6)] = lmc[midx(k, j, 6)];
			}
		}

		//now pull the target's points
		LocalMapTargets[i].NumPoints = np;
		LocalMapTargets[i].TargetPoints = NULL;
		if (np > 0)
		{
			LocalMapTargets[i].TargetPoints = new double[2*np];
		}
		double tx = LocalMapTargets[i].X;
		double ty = LocalMapTargets[i].Y;
		double cosOt = cos(LocalMapTargets[i].Orientation);
		double sinOt = sin(LocalMapTargets[i].Orientation);

		for (j = 0; j < np; j++)
		{
			//extract the target points, which are given in ego vehicle coordinates
			double evx = iLocalMapPoints[midx(h, 5, nr)];
			double evy = iLocalMapPoints[midx(h, 6, nr)];
			//and convert the points to object storage frame for updating
			double osx;
			double osy;
			EgoVehicleToObject(osx, osy, evx, evy, cosOt, sinOt, tx, ty);

			LocalMapTargets[i].TargetPoints[midx(j, 0, np)] = osx;
			LocalMapTargets[i].TargetPoints[midx(j, 1, np)] = osy;

			//done pulling this point
			h++;
		}

		//now compute the target's position measurement (so it isn't recomputed for each track)
		double* zp = LocalMapTargets[i].zp;
		double* Pzzp = LocalMapTargets[i].Pzzp;
		double* Pxzp = LocalMapTargets[i].Pxzp;
		double x[5] = {LocalMapTargets[i].X, LocalMapTargets[i].Y, LocalMapTargets[i].Orientation, 
			LocalMapTargets[i].Speed, LocalMapTargets[i].Heading};
		double P[5*5];
		int idxp[5] = {0, 1, 2, 3, 4};
		Cluster iCluster;
		iCluster.NumPoints = np;
		iCluster.Points = LocalMapTargets[i].TargetPoints;
		for (j = 0; j < 5; j++)
		{
			for (k = 0; k < 5; k++)
			{
				P[midx(j, k, 5)] = LocalMapTargets[i].Covariance[midx(idxp[j], idxp[k], 6)];
			}
		}
		ClusterPositionMeasurement(zp, Pzzp, Pxzp, x, P, &iCluster);
	}

	//next calculate likelihoods of matching up each target with existing tracks
	//and with the birth model

	//***BEGIN ASSOCIATION***

	//store these as correspondence scores = log(likelihood)
	double* cScores = NULL;
	double* cpmDists = NULL;
	double* cshmDists = NULL;
	if (nz > 0)
	{
		cScores = new double[(nt+1)*nz];
		cpmDists = new double[(nt+1)*nz];
		cshmDists = new double[(nt+1)*nz];
		double pmDist;
		double shmDist;

		Track* CurrentTrack = mFirstTrack;
		for (i = 0; i < nt; i++)
		{
			for (j = 0; j < nz; j++)
			{
				//populate the (i, j) entry with the score for assigning target
				//j to track i
				double lambda = CurrentTrack->Likelihood(pmDist, shmDist, &(LocalMapTargets[j]));
				if (fabs(lambda) != 0.0)
				{
					cScores[midx(i, j, nt+1)] = log(lambda);
				}
				else
				{
					cScores[midx(i, j, nt+1)] = -DBL_MAX;
				}
				//also save the mahalanobis distances
				cpmDists[midx(i, j, nt+1)] = pmDist;
				cshmDists[midx(i, j, nt+1)] = shmDist;
			}
			CurrentTrack = CurrentTrack->NextTrack;
		}
		for (j = 0; j < nz; j++)
		{
			//populate the last row with the birth scores
			double lambda = BirthLikelihood(&(LocalMapTargets[j]));
			if (fabs(lambda) != 0.0)
			{
				cScores[midx(nt, j, nt+1)] = log(lambda);
			}
			else
			{
				cScores[midx(nt, j, nt+1)] = -DBL_MAX;
			}
			
		}

		//next, compute maximum correspondence by dynamic programming
		if (nt > 0)
		{
			//if there are existing tracks, compute a dynamic programming correspondence

			//extract an array of the tracks for simplicity
			Track** dpTracks = new Track*[nt];
			Track* CurrentTrack = mFirstTrack;
			for (i = 0; i < nt; i++)
			{
				dpTracks[i] = CurrentTrack;
				CurrentTrack = CurrentTrack->NextTrack;
			}

			//the jth column in this table will store the maximum likelihood score
			//for assigning targets 0 thru j, with j assigned to track i in row i
			double* dpScores = new double[(nt+1)*nz];
			//this table stores back-pointers... the jth column stores the best assignment
			//for the j-1st target, as an index into dpTracks.  
			int* dpBackPointers = new int[(nt+1)*nz];
			//this table stores which tracks have already been assigned measurements- 
			//the (i, j)th entry tells whether track j has already been assigned a
			//measurement in the ith solution
			bool* dpAssigned = new bool[(nt+1)*nt];

			/*
			//MAX LIKELIHOOD ASSIGNMENT WITH REPLACEMENT
			bool* dpIsAssigned = new bool[nt];
			for (j = 0; j < nz; j++)
			{
				//compute max likelihood assignment for each target

				double sBest = -DBL_MAX;
				double sCur;
				int ibest = -1;

				for (i = 0; i < nt + 1; i++)
				{
					sCur = cScores[midx(i, j, nt+1)];
					if (sCur != -DBL_MAX && sCur >= sBest)
					{
						sBest = sCur;
						ibest = i;
					}
				}
				if (ibest != -1)
				{
					if (ibest < nt)
					{
						dpTracks[ibest]->Update(&(LocalMapTargets[j]), iPosteriorPosePosition, mRoadGraph);
					}
					else
					{
						//create a new track from each target measurement
						int NewID = mIDGenerator.GenerateID();
						//give the new track a new id
						Track* NewTrack = new Track(NewID);
						//update the new track with the local map target
						NewTrack->Update(&(LocalMapTargets[j]), iPosteriorPosePosition, mRoadGraph);
						//add the new track to the generator
						AddTrack(NewTrack);
					}
				}
			}
			//END MAX LIKELIHOOD
			*/

			//initialize the dynamic programming solution by assigning the first target
			//(target j=0) to each track i (including the birth track)
			for (i = 0; i < nt + 1; i++)
			{
				idx = midx(i, 0, nt+1);
				dpScores[idx] = cScores[idx];
				dpBackPointers[idx] = -1;
			}

			int bp;
			int kbest;
			double sBest;
			double sCur;
			double sFixed;

			for (j = 1; j < nz; j++)
			{
				//now continue to assign the rest of the targets j

				//first memoize the current nt+1 best partial assignment paths...
				//this creates a lookup table of which tracks have already received measurements for each path
				for (i = 0; i < nt+1; i++)
				{
					//initialize the ith partial assignment path
					for (k = 0; k < nt; k++)
					{
						dpAssigned[midx(i, k, nt+1)] = false;
					}

					//record which assignments are made in the ith path
					bp = i;
					for (k = j-1; k >= 0; k--)
					{
						if (bp < nt)
						{
							dpAssigned[midx(i, bp, nt+1)] = true;
						}
						bp = dpBackPointers[midx(bp, k, nt+1)];
						if (bp == -1)
						{
							//ith partial path is infeasible, or we've reached the beginning of it
							break;
						}
					}
				}

				//next continue by expanding each partial path by one assignment
				for (i = 0; i < nt; i++)
				{
					//compute the best partial assignment path that assigns target j to track i

					idx = midx(i, j, nt+1);
					//extract the fixed score of assigning target j to track i
					sFixed = cScores[idx];

					kbest = -1;
					sBest = -DBL_MAX;
					if (sFixed != -DBL_MAX)
					{
						//if assigning target j to track i makes sense (nonzero likelihood), compute
						//the best partial path that makes that assignment

						for (k = 0; k < nt + 1; k++)
						{
							//use the bellman equation to find the best way to make the jth assignment,
							//which is just the best j-1st assignment path that hasn't assigned anything
							//to track i yet.

							//extract the cost of the kth assignment path up through j-1
							sCur = dpScores[midx(k, j-1, nt+1)];
							if (sCur == -DBL_MAX)
							{
								//this partial path is infeasible even before assigning target j
								continue;
							}
							if (sCur >= sBest)
							{
								//this path can potentially be the best one so far, so check to see if 
								//it is feasible (i.e. check that it hasn't yet assigned anything to track i)

								if (dpAssigned[midx(k, i, nt+1)] == false)
								{
									//this assignment is valid, so it becomes the current best choice
									kbest = k;
									sBest = sCur;
								}
							}
						}
					}
					//kbest is the best partial path for assigning measurement j to target i
					//mark the backpointers and score appropriately
					if (kbest == -1)
					{
						//could not find a valid partial path assigning target j to track i
						dpBackPointers[idx] = -1;
						dpScores[idx] = -DBL_MAX;
					}
					else
					{
						//found a feasible partial path
						dpBackPointers[idx] = kbest;
						dpScores[idx] = sFixed + sBest;
					}
				}

				//also compute the best partial assignment path that declares target j
				//as a new track
				idx = midx(nt, j, nt+1);
				kbest = -1;
				sBest = -DBL_MAX;
				sFixed = cScores[idx];
				for (k = 0; k < nt + 1; k++)
				{
					sCur = dpScores[midx(k, j-1, nt+1)];
					if (sCur == -DBL_MAX)
					{
						//this partial path was infeasible
						continue;
					}
					if (sCur >= sBest)
					{
						kbest = k;
						sBest = sCur;
					}
				}
				if (kbest == -1)
				{
					//could not find a valid partial path assigning meas j as a new track
					dpBackPointers[idx] = -1;
					dpScores[idx] = -DBL_MAX;
				}
				else
				{
					//found a feasible partial path
					dpBackPointers[idx] = kbest;
					dpScores[idx] = sFixed + sBest;
				}
			}

			//when the code gets here, the best solution is just the best score
			//from the last column
			int ibest = -1;
			sBest = -DBL_MAX;
			for (i = 0; i < nt+1; i++)
			{
				sCur = dpScores[midx(i, nz-1, nt+1)];
				if (sCur >= sBest)
				{
					ibest = i;
					sBest = sCur;
				}
			}

			//walk the backpointers to update the targets as per assignment
			bool* dpIsAssigned = new bool[nt];
			for (i = 0; i < nt; i++)
			{
				//while walking, mark which tracks have been updated
				dpIsAssigned[i] = false;
			}
			bp = ibest;
			for (j = nz-1; j >= 0; j--)
			{

				/*
				//TEMPORARY!!!  DISPLAY ASSIGNMENT VS. MAX LIKELIHOOD
				int foo = 1.3;
				sBest = -DBL_MAX;
				for (k = 0; k < nt+1; k++)
				{
					sCur = cScores[midx(k, j, nt+1)];
					if (sCur >= sBest)
					{
						sBest = sCur;
					}
				}
				if (cScores[midx(bp, j, nt+1)] != sBest)
				{
					printf("Assignment likelihood for target %d: %.12lg vs. %.12lg\n", j+1, exp(cScores[midx(bp, j, nt+1)]), exp(sBest));
				}
				//END TEMPORARY
				*/

				if (bp < nt)
				{
					//update track bp with target h
					dpIsAssigned[bp] = true;
					if (cpmDists[midx(bp, j, nt+1)] <= TG_CHI2POSTHRESH) // && cshmDists[midx(bp, j, nt+1)] <= TG_CHI2SPDTHRESH)
					{
						dpTracks[bp]->Update(&(LocalMapTargets[j]), iPosteriorPosePosition, mRoadGraph);
					}
					else
					{
						//don't update the track if the assignment looks sketchy

						#ifdef SE_COMMONDEBUGMSGS
							printf("Track update rejected due to Chi2 test.\n");
						#endif
					}
				}
				else
				{
					//target h was assigned to a new track
					int NewID = mIDGenerator.GenerateID();
					Track* NewTrack = new Track(NewID);
					NewTrack->Update(&(LocalMapTargets[j]), iPosteriorPosePosition, mRoadGraph);
					AddTrack(NewTrack);
				}
				bp = dpBackPointers[midx(bp, j, nt+1)];
			}

			//NOTE: tracks will be deleted after integrating for a short while
			#ifdef SE_FASTDELETE
				//and mark for deletion the tracks that weren't updated
				for (i = 0; i < nt; i++)
				{
					if (dpIsAssigned[i] == false && dpTracks[i]->IsOccluded() == false)
					{
						//NOTE: if not marked for deletion or occluded, these tracks just weren't updated

						if (dpTracks[i]->IsNearStopline(iPosteriorPosePosition) == false)
						{
							//only fast delete tracks if they're not near a stopline
							dpTracks[i]->MarkForDeletion();
						}
					}
				}
			#endif

			//free memory allocated in dynamic programming
			delete [] dpAssigned;
			delete [] dpBackPointers;
			delete [] dpIsAssigned;
			delete [] dpScores;
			delete [] dpTracks;
		}
		else
		{
			//if there are currently no tracks, then each LocalMap target is automatically
			//used to create a new track

			for (i = 0; i < nz; i++)
			{
				//create a new track from each target measurement
				int NewID = mIDGenerator.GenerateID();
				//give the new track a new id
				Track* NewTrack = new Track(NewID);
				//update the new track with the local map target
				NewTrack->Update(&(LocalMapTargets[i]), iPosteriorPosePosition, mRoadGraph);
				//add the new track to the generator
				AddTrack(NewTrack);
			}
		}

		//free memory allocated in correspondence scores
		delete [] cScores;
		delete [] cpmDists;
		delete [] cshmDists;
	}
	else
	{
		//LocalMap reported no targets: mark all tracks for deletion

		//NOTE: tracks will be deleted after integrating for a short while
		#ifdef SE_FASTDELETE
			Track* CurrentTrack = mFirstTrack;
			for (i = 0; i < nt; i++)
			{
				if (CurrentTrack->IsNearStopline(iPosteriorPosePosition) == false)
				{
					//only fast delete tracks if they're not near a stopline
					CurrentTrack->MarkForDeletion();
				}
				CurrentTrack = CurrentTrack->NextTrack;
			}
		#endif
	}

	//***END ASSOCIATION***

	//delete the temporary memory allocated for storing targets
	for (i = 0; i < nz; i++)
	{
		delete [] LocalMapTargets[i].Covariance;
		delete [] LocalMapTargets[i].TargetPoints;
	}
	delete [] LocalMapTargets;

	//store the most recent update time
	mTrackGeneratorTime = iUpdateTime;

	return;
}

double TrackGeneratorFilter::BirthLikelihood(Target* iTarget)
{
	/*
	Calculates the birth likelihood for a new track given a particular
	LocalMap target measurement.

	INPUTS:
		iTarget - the LocalMap target measurement that is to be
			evaluated for creating new tracks.
		iSensor - the sensor structure that describes the 
			LocalMap's orientation with respect to the track generator
		iVehicleOdometry - vehicle odometry structure valid during the
			evaluation.

	OUTPUTS:
		rLambda - likelihood of the measurement being caused by a new
			track just discovered.
	*/

	double rLambda = 0.0;

	//model birth likelihood as uniform:
	//anchor X uniform over +-100m
	//anchor Y uniform over +-100m
	//orientation uniform over +-Pi
	//speed uniform over +-26.8224
	//heading uniform over +-Pi
	rLambda = (1.0/200.0) * (1.0/200.0) * (1.0/TWOPI) * (1.0/53.6448) * (1.0/TWOPI);

	return rLambda;
}

void TrackGeneratorFilter::MaintainTracks(Sensor* iFrontSensor, Sensor* iBackSensor)
{
	/*
	Performs maintenance on the tracks in track generator.

	INPUTS:
		iFrontSensor - sensor structure to use for calculating occlusion angles
			from tracks in front of the car.
		iBackSensor - sensor structure to use for calculating occlusion angles
			from tracks in back of the car.

	OUTPUTS:
		none.
	*/

	int i;
	int j;

	//PROCESS TRACK OCCLUSION

	#ifdef SE_TRACKOCCLUSION
		Track* itrack;
		Track* jtrack;

		itrack = mFirstTrack;
		for (i = 0; i < mNumTracks; i++)
		{
			//reset each track's occlusion status to unoccluded
			itrack->MarkForOcclusion(T_OCCLUSIONNONE);

			itrack = itrack->NextTrack;
		}

		//precompute each track's extreme angles and closest range
		double* trackcache = NULL;
		//also precompute the xy locations of each track's extreme angles
		double* trachcachexy = NULL;
		if (mNumTracks > 0)
		{
			//trackcache stores each track's left and right ranges and bearings and also the shortest ranges
			trackcache = new double[10*mNumTracks];

			itrack = mFirstTrack;
			for (i = 0; i < mNumTracks; i++)
			{
				double zi[3];
				double zixy[6];
				double xi[5] = {itrack->X(), itrack->Y(), itrack->Orientation(), itrack->Speed(), itrack->Heading()};
				Cluster iCluster = itrack->TrackPointsCluster();

				//FRONT SENSORS

				//calculate track bounds from the front vantage point
				ClusterBcwBccwRmin(zi, xi, &iCluster, iFrontSensor);

				//save the precomputed track extreme angles and shortest range
				trackcache[midx(i, 1, mNumTracks)] = zi[0];
				trackcache[midx(i, 3, mNumTracks)] = zi[1];
				trackcache[midx(i, 4, mNumTracks)] = zi[2];

				//calculate x-y coordinates of track bounds from the front vantage point
				ClusterExtremePoints(zixy, xi, &iCluster, iFrontSensor);

				//save ranges to the extreme angles
				trackcache[midx(i, 0, mNumTracks)] = sqrt(zixy[0]*zixy[0] + zixy[1]*zixy[1]);
				trackcache[midx(i, 2, mNumTracks)] = sqrt(zixy[2]*zixy[2] + zixy[3]*zixy[3]);

				//REAR SENSORS

				//calculate track bounds from the rear vantage point
				ClusterBcwBccwRmin(zi, xi, &iCluster, iBackSensor);

				//save the precomputed track extreme angles and shortest range
				trackcache[midx(i, 6, mNumTracks)] = zi[0];
				trackcache[midx(i, 8, mNumTracks)] = zi[1];
				trackcache[midx(i, 9, mNumTracks)] = zi[2];

				//calculate x-y coordinates of track bounds from the back vantage point
				ClusterExtremePoints(zixy, xi, &iCluster, iBackSensor);

				//save ranges to the extreme angles
				trackcache[midx(i, 5, mNumTracks)] = sqrt(zixy[0]*zixy[0] + zixy[1]*zixy[1]);
				trackcache[midx(i, 7, mNumTracks)] = sqrt(zixy[2]*zixy[2] + zixy[3]*zixy[3]);

				itrack = itrack->NextTrack;
			}
		}
		
		itrack = mFirstTrack;
		for (i = 0; i < mNumTracks; i++)
		{
			//check to see if itrack can occlude other tracks

			if (itrack->StatusFlag() == T_STATUSDELETED)
			{
				//deleted tracks can't occlude other tracks
				itrack = itrack->NextTrack;
				continue;
			}

			//pull the occluder track's clockwise and counterclockwise angles and range to the extreme angles
			double ocwb;
			double occwb;
			double ocwr;
			double occwr;
			double ocr;
			//the sensor vantage point (f = front, b = back)
			char svp = 'f';

			//determine which set of angles to use based on where the track is
			if (itrack->X() > iBackSensor->SensorX)
			{
				//track was seen from the front vantage point
				ocwr = trackcache[midx(i, 0, mNumTracks)];
				ocwb = trackcache[midx(i, 1, mNumTracks)];
				occwr = trackcache[midx(i, 2, mNumTracks)];
				occwb = trackcache[midx(i, 3, mNumTracks)];
				ocr = trackcache[midx(i, 4, mNumTracks)];
				svp = 'f';
			}
			else
			{
				//track was seen from the back vantage point
				ocwr = trackcache[midx(i, 5, mNumTracks)];
				ocwb = trackcache[midx(i, 6, mNumTracks)];
				occwr = trackcache[midx(i, 7, mNumTracks)];
				occwb = trackcache[midx(i, 8, mNumTracks)];
				ocr = trackcache[midx(i, 9, mNumTracks)];
				svp = 'b';
			}

			//wrap all angles to the center of the occluder track for testing
			double wraptarget = 0.5*(ocwb + occwb);

			if (ocr <= TG_MAXOCCLUDERDIST)
			{
				//this track can occlude other tracks

				jtrack = mFirstTrack;
				for (j = 0; j < mNumTracks; j++)
				{
					//test whether any of the other tracks are occluded by this track

					if (jtrack->StatusFlag() == T_STATUSDELETED)
					{
						//deleted tracks can't be occluded
						jtrack = jtrack->NextTrack;
						continue;
					}
					if (jtrack == itrack)
					{
						//tracks can't occlude themselves
						jtrack = jtrack->NextTrack;
						continue;
					}

					//pull the candidate track's clockwise and counterclockwise angles and range
					double ccwr;
					double ccwb;
					double cccwr;
					double cccwb;
					double ccr;

					switch (svp)
					{
					case 'f':
						//pull the candidate's angles from the front vantage point
						ccwr = trackcache[midx(j, 0, mNumTracks)];
						ccwb = trackcache[midx(j, 1, mNumTracks)];
						cccwr = trackcache[midx(j, 2, mNumTracks)];
						cccwb = trackcache[midx(j, 3, mNumTracks)];
						ccr = trackcache[midx(j, 4, mNumTracks)];
						break;
					case 'b':
						//pull the candidate's angles from the back vantage point
						ccwr = trackcache[midx(j, 5, mNumTracks)];
						ccwb = trackcache[midx(j, 6, mNumTracks)];
						cccwr = trackcache[midx(j, 7, mNumTracks)];
						cccwb = trackcache[midx(j, 8, mNumTracks)];
						ccr = trackcache[midx(j, 9, mNumTracks)];
						break;
					}

					//test whether itrack and jtrack are nearly identical copies... don't let these occlude each other
					if (fabs(ccwb - ocwb) <= TG_BEARINGMATCH && fabs(cccwb - occwb) <= TG_BEARINGMATCH
						&& fabs(ocr - ccr) <= TG_RANGEMATCH)
					{
						//itrack and jtrack are nearly identical... don't let them occlude each other
						jtrack = jtrack->NextTrack;
						continue;
					}

					//test whether itrack occludes jtrack
					ccwb = WrapAngle(ccwb, wraptarget);
					cccwb = WrapAngle(cccwb, wraptarget);
					if (ccwb > cccwb)
					{
						//the candidate track sits on the opposite side of the Tahoe as the occluder,
						//so it can't be occluded by this occluder.
						//NOTE: this check needs to be done to prevent tracks in front of the Tahoe from
						//occluding tracks behind the Tahoe and vice versa (due to angle branch point)
						jtrack = jtrack->NextTrack;
						continue;
					}

					if ((occwb >= cccwb && ocr + TG_MINOCCLUSIONDIST <= cccwr) 
						&& (ocwb <= ccwb && ocr + TG_MINOCCLUSIONDIST <= ccwr))
					{
						//NOTE: this conditional requires BOTH corners to be occluded for a track to be occluded

						//the occluder track (itrack) occludes the candidate track (jtrack)
						//this is a FULL occlusion
						jtrack->MarkForOcclusion(T_OCCLUSIONFULL);
					}
					else if (occwb + TG_OCCLUDERANGLEBUFFER >= cccwb 
						&& ocwb - TG_OCCLUDERANGLEBUFFER <= cccwb
						&& ocr + TG_MINOCCLUSIONDIST <= cccwr)
					{
						//NOTE: this conditional requires ONLY the ccw corner to be occluded for a track to be occluded

						//the occluder track (itrack) occludes the candidate track (jtrack)
						//this is a PARTIAL occlusion
						jtrack->MarkForOcclusion(T_OCCLUSIONPART);
					}
					else if (occwb + TG_OCCLUDERANGLEBUFFER >= ccwb 
						&& ocwb - TG_OCCLUDERANGLEBUFFER <= ccwb
						&& ocr + TG_MINOCCLUSIONDIST <= ccwr)
					{
						//NOTE: this conditional requires ONLY the cw corner to be occluded for a track to be occluded

						//the occluder track (itrack) occludes the candidate track (jtrack)
						//this is a PARTIAL occlusion
						jtrack->MarkForOcclusion(T_OCCLUSIONPART);
					}

					jtrack = jtrack->NextTrack;
				}
			}

			itrack = itrack->NextTrack;
		}

		//free memory for the cached extreme angles
		delete [] trackcache;
	#endif

	//PROCESS TRACK DELETION

	Track* CurrentTrack = mFirstTrack;
	for (i = 0; i < mNumTracks; i++)
	{
		//perform maintenance on each track
		CurrentTrack->MaintainTrack();

		//check if this track can be marked for deletion
		if (CurrentTrack->TimeSinceLastUpdate() >= TG_PREDICTTIME)
		{
			//this track has not been updated for too long: mark for deletion
			CurrentTrack->MarkForDeletion();
		}

		//check tracks for exceeding maximum range
		double ctx = CurrentTrack->X();
		double cty = CurrentTrack->Y();
		double ctr = sqrt(ctx*ctx + cty*cty);
		if (ctr > TG_MAXTRACKRANGE)
		{
			//delete tracks that are too far away, regardless of occlusion status
			CurrentTrack->MarkForDeletion();
		}

		//check tracks for going too long without being updated
		if (CurrentTrack->IsOccluded() == true && CurrentTrack->AbsoluteTimeSinceLastUpdate() > TG_MAXTIMESINCEUPDATE)
		{
			//delete tracks that have gone too long without a measurement if they're occluded
			CurrentTrack->MarkForDeletion();
			#ifdef SE_COMMONDEBUGMSGS
				printf("Warning: deleting stale track for age\n");
			#endif
		}

		//check tracks for having too low an update rate
		if (CurrentTrack->TimeSinceCreation() >= TG_MINTIMEFORRATECHECK)
		{
			//calculate the update rate (measurements / sec.) for the track
			double ctu = ((double) CurrentTrack->NumMeasurements()) / CurrentTrack->TimeSinceCreation();
			/*
			if (ctu < 3.0)
			{
				printf("ctu: %lg\n", ctu);
			}
			*/
			//check whether the track update rate is too low
			if (CurrentTrack->IsOccluded() == true && ctu < TG_MINUPDATERATE)
			{
				//delete tracks that have too low an update rate if they are occluded
				CurrentTrack->MarkForDeletion();
				#ifdef SE_COMMONDEBUGMSGS
					printf("Warning: deleting stale track for update rate\n");
				#endif
			}
		}

		//check if this track can be deleted
		if (CurrentTrack->TimeSinceLastUpdate() >= TG_PREDICTTIME + TG_DELETETIME)
		{
			//time to delete this track
			Track* OldTrack = CurrentTrack;
			CurrentTrack = CurrentTrack->NextTrack;

			//remove the old track from the list
			RemoveTrack(OldTrack);
		}
		else
		{
			CurrentTrack = CurrentTrack->NextTrack;
		}
	}

	//PROCESS TRACK OVERFLOW DELETION

	//first count the number of occluded tracks
	int noc = 0;
	CurrentTrack = mFirstTrack;
	for (i = 0; i < mNumTracks; i++)
	{
		if (CurrentTrack->IsOccluded() == true)
		{
			noc++;
		}
		CurrentTrack = CurrentTrack->NextTrack;
	}
	if (noc > TG_MAXOCCLUDEDTRACKS)
	{
		//if number of occluded tracks is too big, start removing occluded tracks

		//make a list of all the numbers of measurements of each occluded track
		int idx = 0;
		int* nmocc = new int[noc];
		memset(nmocc, 0x00, noc*sizeof(int));

		CurrentTrack = mFirstTrack;
		for (i = 0; i < mNumTracks; i++)
		{
			if (CurrentTrack->IsOccluded() == true)
			{
				nmocc[idx] = CurrentTrack->NumMeasurements();
				idx++;
			}
			CurrentTrack = CurrentTrack->NextTrack;
		}

		int nrem = noc - TG_MAXOCCLUDEDTRACKS;
		//determine the nrem_th fewest number of measurements and remove all tracks with fewer or equal measurements
		qsort_s(nmocc, noc, sizeof(int), &IntegerCompare, NULL);
		if (noc > nrem && nrem > 0)
		{
			//determine the maximum number of measurements a track can have and still be deleted
			int mmd = nmocc[nrem-1];
			//and delete all tracks that have this or fewer measurements
			printf("Warning: maintaining %d occluded tracks; culling at %d measurement(s)...\n", noc, mmd);
			CurrentTrack = mFirstTrack;
			for (i = 0; i < mNumTracks; i++)
			{
				if (CurrentTrack->IsOccluded() == true && CurrentTrack->NumMeasurements() <= mmd)
				{
					//if the current track is occluded and has too few measurements, mark it for deletion
					CurrentTrack->MarkForDeletion();
				}
				CurrentTrack = CurrentTrack->NextTrack;
			}
		}

		//free memory allocated
		delete [] nmocc;
	}

	return;
}

void TrackGeneratorFilter::PrintTracks(FILE* iTrackFile, PosteriorPosePosition* iPosteriorPosePosition)
{
	/*
	Prints the TrackGenerator AI tracks to the given file.  Should be used
	after PredictForTransmit() has been called, so the AI variables are
	populated correctly.

	INPUTS:
		iTrackFile - the open file to print to.
		iPosteriorPosePosition - the current posterior pose position, for
			finding tracks in absolute coordinates

	OUTPUTS:
		none.  Writes a track packet to iTrackFile
	*/

	if (mIsInitialized == false)
	{
		//don't print anything if the filter isn't initialized
		return;
	}

	int i;
	int j;
	int k;
	Track* CurrentTrack = mAIFirstTrack;

	//extract the posterior pose position
	double ppe = iPosteriorPosePosition->EastMMSE;
	double ppn = iPosteriorPosePosition->NorthMMSE;
	double pph = iPosteriorPosePosition->HeadingMMSE;
	double cospph = cos(pph);
	double sinpph = sin(pph);

	for (i = 0; i < mNumTracks; i++)
	{
		//print each track

		int np = CurrentTrack->NumTrackPoints();

		//calculate the location of the track in absolute coordinates
		double abe;
		double abn;
		EgoVehicleToAbsolute(abe, abn, CurrentTrack->X(), CurrentTrack->Y(), cospph, sinpph, ppe, ppn);
		double abh = UnwrapAngle(CurrentTrack->Heading() - pph);

		RoadPartition* cp = CurrentTrack->ClosestPartition();

		//print the track header
		if (cp != NULL)
		{
			fprintf(iTrackFile, "%.12lg,%d,%d,%d,%.12lg,%.12lg,%.12lg,%.12lg,%.12lg,%d,%s\n", 
				mAITime, i, mNumTracks, CurrentTrack->ID(), abe, abn, CurrentTrack->Orientation(), 
				CurrentTrack->Speed(), abh, np, cp->PartitionID());
		}
		else
		{
			fprintf(iTrackFile, "%.12lg,%d,%d,%d,%.12lg,%.12lg,%.12lg,%.12lg,%.12lg,%d\n", 
				mAITime, i, mNumTracks, CurrentTrack->ID(), abe, abn, CurrentTrack->Orientation(), 
				CurrentTrack->Speed(), abh, np);
		}

		//calculate the track covariance in absolute coordinates
		double tCov[T_NUMSTATES*T_NUMSTATES];
		double iCov[T_NUMSTATES*T_NUMSTATES];
		for (j = 0; j < T_NUMSTATES; j++)
		{
			for (k = 0; k < T_NUMSTATES; k++)
			{
				iCov[midx(j, k, T_NUMSTATES)] = CurrentTrack->Covariance(j, k);
			}
		}
		TrackCovarianceToAbsolute(tCov, T_NUMSTATES, CurrentTrack->X(), CurrentTrack->Y(), CurrentTrack->Heading(), iCov, iPosteriorPosePosition);

		//print the track covariance in absolute coordinates
		char cbuff[TG_LINESIZE];
		cbuff[0] = '\0';
		char buff[TG_FIELDSIZE];
		sprintf_s(cbuff, TG_LINESIZE, "%.12lg,%d,%d,%.12lg", mAITime, i, mNumTracks, tCov[midx(0, 0, T_NUMSTATES)]);
		for (j = 1; j < T_NUMSTATES; j++)
		{
			for (k = 0; k <= j; k++)
			{
				sprintf_s(buff, TG_FIELDSIZE, ",%.12lg", tCov[midx(j, k, T_NUMSTATES)]);
				strcat_s(cbuff, TG_LINESIZE, buff);
			}
		}
		fprintf(iTrackFile, "%s\n", cbuff);

		//print each track point in absolute coordinates
		double aX = CurrentTrack->X();
		double aY = CurrentTrack->Y();
		double cosOrient = cos(CurrentTrack->Orientation());
		double sinOrient = sin(CurrentTrack->Orientation());
		for (j = 0; j < np; j++)
		{
			//extract each track point in the object storage frame
			double osx = CurrentTrack->TrackPoints(j, 0);
			double osy = CurrentTrack->TrackPoints(j, 1);
			//convert to ego vehicle coordinates
			double evx;
			double evy;
			ObjectToEgoVehicle(evx, evy, osx, osy, cosOrient, sinOrient, aX, aY);
			//convert to absolute coordinates
			EgoVehicleToAbsolute(abe, abn, evx, evy, cospph, sinpph, ppe, ppn);
			//then print
			fprintf(iTrackFile, "%.12lg,%d,%d,%d,%d,%.12lg,%.12lg\n", mAITime, i, mNumTracks, j, np, abe, abn);
		}

		CurrentTrack = CurrentTrack->NextTrack;
	}

	return;
}

void TrackGeneratorFilter::PrintLoosePoints(FILE* iLoosePointsFile, PosteriorPosePosition* iPosteriorPosePosition)
{
	/*
	Prints the TrackGenerator AI loose obstacle points to the given file.
	Should be used after PredictForTransmit() has been called, so the AI 
	variables are populated correctly.

	INPUTS:
		iTrackFile - the open file to print to.
		iPosteriorPosePosition - the current posterior pose position, for
			finding loose obstacle points in absolute coordinates

	OUTPUTS:
		none.  Writes a loose points packet to iLoosePointsFile
	*/

	int i;
	int np = mNumLoosePoints;

	//extract the posterior pose position
	double ppe = iPosteriorPosePosition->EastMMSE;
	double ppn = iPosteriorPosePosition->NorthMMSE;
	double pph = iPosteriorPosePosition->HeadingMMSE;
	double cospph = cos(pph);
	double sinpph = sin(pph);

	for (i = 0; i < np; i++)
	{
		//calculate the location of each point in absolute coordinates
		double abe;
		double abn;
		double evx = mLoosePoints[midx(i, 0, np)];
		double evy = mLoosePoints[midx(i, 1, np)];
		EgoVehicleToAbsolute(abe, abn, evx, evy, cospph, sinpph, ppe, ppn);

		//and print the point
		fprintf(iLoosePointsFile, "%.12lg,%d,%d,%d,%d,%.12lg,%.12lg\n", mAITime, i, np, 
			mLooseClusterIDs[i], mLoosePointHeights[i], abe, abn);
	}

	return;
}
