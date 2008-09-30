#include "SceneEstimatorPublisher.h"

using namespace System;
using namespace System::Runtime::Remoting;
using namespace System::Collections;
using namespace System::Collections::Generic;

using namespace UrbanChallenge::NameService;
using namespace UrbanChallenge::MessagingService;
using namespace UrbanChallenge::Common;
using namespace UrbanChallenge::Common::Vehicle;
using namespace UrbanChallenge::Common::Sensors::LocalRoadEstimate;

#define CHANNEL_FACTORY_NAME				"ChannelFactory"
#define ARBITER_POS_CHANNEL_NAME		"ArbiterSceneEstimatorPositionChannel"
#define ARBITER_SPEED_CHANNEL_NAME	"VehicleSpeedChannel"
#define OPER_ROAD_CHANNEL_NAME			"OperationalSceneEstimatorRoadInfoChannel"

#define REMOTING_CONFIG							"Remoting.config"

#define PI 3.1415926535897932384626433832795

bool SceneEstimatorPublisher::initialized = false;

//BEGIN RIDICULOUSLY GHETTO HACK--------------------
#pragma unmanaged
static void StaticPoseCallback(const pose_abs_msg& poseMessage, void* args)
{
	SceneEstimatorPublisher* pub = (SceneEstimatorPublisher*)args;
	if (pub!=NULL)
		pub->PublishArbiterVelocityMessage(poseMessage.veh_vx);
}

static void StaticPoseRegister(pose_client* poseClient, SceneEstimatorPublisher* pub)
{
	poseClient ->register_abs_callback (StaticPoseCallback,pub);
}

#pragma managed
//END RIDICULOUSLY GHETTO HACK--------------------



//TODO: consider making this static?

SceneEstimatorPublisher::SceneEstimatorPublisher(pose_client* poseClient) {
	initializedChannels = false;	
	printf("Initializing Scene Estimator Publisher...\n");
	if (poseClient == NULL) 
	{
		printf("\n\nWARNING: FATAL ERROR. POSE CLIENT IS NULL. CANNOT INITIALIZE SCENE ESTIMATOR PUBLISHER.\n\n"); 		
	}
	else
	{
		StaticPoseRegister(poseClient,this);
	}
	

	try
	{
		if (!initialized) {
			RemotingConfiguration::Configure(REMOTING_CONFIG, false);
			initialized = true;
		}
		
		try
		{
			array<WellKnownServiceTypeEntry^> ^ const wkst = RemotingConfiguration::GetRegisteredWellKnownServiceTypes();
			printf("Connecting to Object Directory...\n");
			ObjectDirectory ^ const od = dynamic_cast<ObjectDirectory^>(Activator::GetObject(ObjectDirectory::typeid, wkst[0]->ObjectUri));
			printf("Obtaining Channel Factory...\n");
			IChannelFactory ^ const factory = dynamic_cast<IChannelFactory^>(od->Resolve(CHANNEL_FACTORY_NAME));
			
			this->arbiterPosChan = factory->GetChannel(ARBITER_POS_CHANNEL_NAME, ChannelMode::UdpMulticast);
			this->arbiterSpeedChan = factory->GetChannel(ARBITER_SPEED_CHANNEL_NAME, ChannelMode::UdpMulticast);
			this->operRoadChan = factory->GetChannel(OPER_ROAD_CHANNEL_NAME, ChannelMode::UdpMulticast);
			initializedChannels = true;
			
			printf("Succesfully Initialized Scene Estimator Publisher!\n");
		}
		catch(Exception^ e)
		{
			printf("\n\nWARNING: COULD NOT INITIALIZE REMOTING INTERFACE. CHECK UDP MESSAGING SERVICE IS RUNNING.\n\n");
			Console::WriteLine ("EXCEPTION in managed code: " + e->Message);
		}	
	}
	catch (Exception^ e)
	{
		printf("\n\nERROR INITIALIZING REMOTING CONFIGURATION. Make sure remoting.config is in the application path.\n\n");
		Console::WriteLine ("EXCEPTION in managed code: " + e->Message);
	}
	
}

void SceneEstimatorPublisher::PublishOperationalMessage(UnmanagedOperationalMessage* msg)
{		
	LocalRoadEstimate^ lre = gcnew LocalRoadEstimate();

	lre->timestamp = msg->timestamp;
			/*
	lre->isModelValid = msg->isModelValid;

	lre->roadHeading = msg->roadHeading;
	lre->roadCurvature = msg->roadCurvature;
	lre->roadHeadingVar = msg->roadHeadingVar;
	lre->roadCurvatureVar = msg->roadCurvatureVar;
	
	lre->rightLaneEstimate.id = gcnew System::String(msg->rightLaneID.c_str ());
	lre->rightLaneEstimate.center = msg->rightLaneCenter;
	lre->rightLaneEstimate.width = msg->rightLaneWidth;
	lre->rightLaneEstimate.exists = msg->rightLaneExists;
	lre->rightLaneEstimate.centerVar = msg->rightLaneCenterVar;
	lre->rightLaneEstimate.widthVar = msg->rightLaneWidthVar;

	lre->leftLaneEstimate.id = gcnew System::String(msg->leftLaneID.c_str ());
	lre->leftLaneEstimate.center = msg->leftLaneCenter;
	lre->leftLaneEstimate.width = msg->leftLaneWidth;
	lre->leftLaneEstimate.exists = msg->leftLaneExists;
	lre->leftLaneEstimate.centerVar = msg->leftLaneCenterVar;
	lre->leftLaneEstimate.widthVar = msg->leftLaneWidthVar;

	lre->centerLaneEstimate.id = gcnew System::String(msg->centerLaneID.c_str ());
	lre->centerLaneEstimate.center = msg->centerLaneCenter;
	lre->centerLaneEstimate.width = msg->centerLaneWidth;
	lre->centerLaneEstimate.exists = msg->centerLaneExists;
	lre->centerLaneEstimate.centerVar = msg->centerLaneCenterVar;
	lre->centerLaneEstimate.widthVar = msg->centerLaneWidthVar;
	*/

	lre->stopLineEstimate.distToStopline = msg->distToStopline;
	lre->stopLineEstimate.distToStoplineVar = msg->distToStoplineVar;
	lre->stopLineEstimate.stopLineExists = msg->stopLineExists;
	if (initializedChannels)
	this->operRoadChan->PublishUnreliably (lre,ChannelSerializerInfo::BinarySerializer);
	
}
void SceneEstimatorPublisher::PublishArbiterVelocityMessage(double vel)
{
	if (initializedChannels)
	{
		try
		{
			
			arbiterSpeedChan->PublishUnreliably(vel);	
		}
		catch(...)
		{
			printf("Warning: Attempting to send on ArbiterSpeedChannel without creating the channel first.\n");
		}
	}
}

void SceneEstimatorPublisher::PublishArbiterPositionMessage(UnmanagedArbiterPositionMessage *msg)
{	
	VehicleState^ vs = gcnew VehicleState ();			

	vs->Area  = gcnew List<AreaEstimate>();
	vs->Position = Coordinates(	msg->eastMMSE, msg->northMMSE);	

	Coordinates t = Coordinates(1, 0);
	vs->Heading = t.Rotate(msg->headingMMSE);
	vs->Timestamp = msg->timestamp;
	vs->IsSparseWaypoints = msg->isSparseWaypoints;
	vs->ENCovariance = gcnew array<Double>(4);
	for (int i=0; i<4; i++) vs->ENCovariance [i] = msg->ENCovariance [i];

	for (int i=0; i<msg->numberPartitions; i++)
	{
		double conf = msg->partitions[i].confidence;
		string id = msg->partitions[i].id;
		PartitionType partitionType = msg->partitions[i].partitionType;
		AreaEstimate areaEstimate;
		areaEstimate.AreaId = gcnew System::String(id.c_str());
		areaEstimate.AreaType = (StateAreaType)(partitionType);
		areaEstimate.Probability = conf;		
		vs->Area->Add (areaEstimate);
	}
	
	if (initializedChannels)
	{		
		this->arbiterPosChan->PublishUnreliably (vs,ChannelSerializerInfo::BinarySerializer);
	}
}
