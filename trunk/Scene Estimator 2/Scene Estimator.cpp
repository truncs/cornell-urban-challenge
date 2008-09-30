// Scene Estimator II.cpp : Defines the entry point for the console application.
//

#include "Event.h"
#include "Globals.h"
#include "PosteriorPoseParticleFilter.h"
#include "RoadGraph.h"
#include "SceneEstimatorConstants.h"
#include "SceneEstimatorFunctions.h"
#include "SynchronizedEventQueue.h"

#include <STDIO.H>

int main(int argc, char** argv)
{
	//initialize the SceneEstimator
	SceneEstimatorScreenInit();

	printf("Starting Scene Estimator.\n");

	//***LOAD THE ROAD GRAPH***
	extern RoadGraph* TheRoadGraph;
	TheRoadGraph = new RoadGraph("MyRoadGraph.rgp");
	/*
	if (TheRoadGraph->IsValid() == false)
	{
		printf("SceneEstimator will now terminate because road graph is not valid.\n");
		delete TheRoadGraph;
		return 0;
	}
	*/

	//***CREATE THE POSTERIOR POSE PARTICLE FILTER***
	PosteriorPoseParticleFilter* ThePosteriorPose;
	ThePosteriorPose = new PosteriorPoseParticleFilter(PP_NUMPARTICLES, "seed.txt");

	//initialize the current ppose time
	double PosteriorPoseTime;
	PosteriorPoseTime = -DBL_MAX;

	//***DECLARE THE EVENT QUEUE***
	//PosteriorPoseEventQueue is the (global) event queue
	extern SynchronizedEventQueue PosteriorPoseEventQueue;
	//holder for the current event
	Event* TheCurrentEvent = NULL;

	//******************************************************************************

	//***BEGIN POSTERIOR POSE MAIN LOOP***

	while (true)
	{
		//1. GET NEXT EVENT
		if (PosteriorPoseEventQueue.QueueHasEventsReady() == true)
		{
			//queue has data

			//delete the event that has just been processed
			delete TheCurrentEvent;
			TheCurrentEvent = NULL;

			//and grab the next event in its place
			TheCurrentEvent = PosteriorPoseEventQueue.PullEvent();
		}
		else
		{
			//go to sleep until queue has data
			DWORD PosteriorPoseSignalStatus;
			PosteriorPoseSignalStatus = WaitForSingleObjectEx(PosteriorPoseEventQueue.mQueueDataEventHandle, PP_EVENTTIMEOUT, false);

			if (PosteriorPoseSignalStatus != WAIT_OBJECT_0)
			{
				printf("PosteriorPose event queue timed out.\n");
			}

			continue;
		}

		//when code gets here, something is on the event queue

		//check for invalid or malformed events
		if (TheCurrentEvent == NULL)
		{
			//no event present
			continue;
		}
		if (TheCurrentEvent->EventType == INVALID_EVENT)
		{
			//invalid event present
			continue;
		}
		if (TheCurrentEvent->EventType == QUIT_EVENT)
		{
			//signal for scene estimator to quit
			break;
		}

		//set ppose time equal to the most recent event processed
		PosteriorPoseTime = TheCurrentEvent->EventTime;
	}

	//DELETE MEMORY ALLOCATED IN SCENE ESTIMATOR
	delete TheCurrentEvent;
	delete ThePosteriorPose;
	delete TheRoadGraph;

	return 0;
}
