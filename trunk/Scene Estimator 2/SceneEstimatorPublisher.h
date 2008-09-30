#ifndef SCENEESTIMATORPUBLISHER_H
#define SCENEESTIMATORPUBLISHER_H


#include <vcclr.h>
#include "OperationalMessage.h"
#include "ArbiterMessage.h"
#include "poseinterface/poseclient.h"
#include "../common/sceneEstimatorInterface/sceneEstimatorInterface.h"

class SceneEstimatorPublisher {

public:

	SceneEstimatorPublisher(pose_client* poseClient);

	void PublishOperationalMessage(UnmanagedOperationalMessage* msg);
	void PublishArbiterPositionMessage(UnmanagedArbiterPositionMessage *msg);
	void PublishArbiterVelocityMessage(double vel);

private:

	static bool initialized;	
	bool initializedChannels;
	gcroot<UrbanChallenge::MessagingService::IChannel^> arbiterPosChan;
	gcroot<UrbanChallenge::MessagingService::IChannel^> arbiterSpeedChan;
	gcroot<UrbanChallenge::MessagingService::IChannel^> operRoadChan;
	
	
};

#endif
