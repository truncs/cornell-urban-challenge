#include "sceneEstimatorInterface.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif


int SceneEstimatorTrackedCluster::GetFixedSize()
{
	return 43;
}

int SceneEstimatorTrackedCluster::GetTotalSize()
{
	return 
			GetFixedSize() + 
			sizeof(SceneEstimatorClusterPartition)*(int)closestPartitions.size() + 
			sizeof(SceneEstimatorClusterPoint)*(int)points.size();
}

void SceneEstimatorTrackedCluster::CopySparse(void* memory, int& byteOffset)
{
	numPoints = (int)points.size ();
	numClosestPartitions = (int)closestPartitions.size();
	//copy the beginning....
	memcpy (((unsigned char*)(memory))+byteOffset,this,GetFixedSize());	
	byteOffset+=GetFixedSize();
	//and the vectors....			
	//partitions
	if (closestPartitions.size() > 0)
	{
		memcpy (((unsigned char*)(memory))+byteOffset,&closestPartitions.front(),sizeof(SceneEstimatorClusterPartition)*closestPartitions.size());	
		byteOffset+=sizeof(SceneEstimatorClusterPartition)*((int)closestPartitions.size());
	}
	//points
	if (points.size() > 0)
	{
		memcpy (((unsigned char*)(memory))+byteOffset,&points.front(),sizeof(SceneEstimatorClusterPoint)*points.size());	
		byteOffset+=sizeof(SceneEstimatorClusterPoint)*((int)points.size());
	}
}	

SceneEstimatorClusterPartition::SceneEstimatorClusterPartition(char* name, float probability, SceneEstimatorTrackedClusterMsg* parentCluster)
{
	//check bounds
	if (probability > 1.0f) probability = 1.0f;
	if (probability < 0.0f) probability = 0.0f; 
	float tempf = probability * 65535;
	int temp = (int) tempf;
	if (temp>65535) temp = 65535;
	if (temp<0) temp = 0;
	probabilityFP = (unsigned short)(temp);
	//enter this into the hashtable...
	string sname = string(name); //convert char* to stl string
	//check to see if the entry exists
	PartitionMap::iterator i = parentCluster->partitionMap.find(sname);
	if (i == parentCluster->partitionMap.end()) //it does not exist so add entry
	{	
		int id =(int)parentCluster->partitionMap.size ();
		partitionHashID = id;
		parentCluster->partitionMap.insert(make_pair(sname,id));
	}
	else //use our existing entry
	{		
		partitionHashID = i->second;
	}
}

//RECIEVER-----------------------------------------------------------------------------------------------------------
/*
void SceneEstimatorInterfaceReceiver::SetSceneEstimatorParticlePointsCallback(sceneEstimatorPoints_Msg_handler handler, void* arg)
{
	sceneEstimatorParticlePoints_cbk = handler;
	sceneEstimatorParticlePoints_cbk_arg = arg;
}

SceneEstimatorInterfaceReceiver::SceneEstimatorInterfaceReceiver() 
{
	udp_params params = udp_params();
	params.remote_ip = inet_addr(UDP_SCENE_EST_MULTICAST_ADDR);
	params.local_port = UDP_SCENE_EST_PORT;
	conn = new udp_connection (params);	
	conn->set_callback (MakeDelegate(this,&SceneEstimatorInterfaceReceiver::UDPCallback),conn);
	printf("Scene Estimator RX Interface Initialized. %s:%d\r\n",UDP_SCENE_EST_MULTICAST_ADDR,UDP_SCENE_EST_PORT);
}

SceneEstimatorInterfaceReceiver::~SceneEstimatorInterfaceReceiver ()
{
	delete conn;
	printf("Scene Estimator Shutdown...\r\n");
}


void SceneEstimatorInterfaceReceiver::UDPCallback(udp_message& msg, udp_connection* conn, void* arg)
{ 
	//messages come in like this:
	// ID + memory mapped message, ID is 1 byte
	SceneEstimatorMessageID m = (SceneEstimatorMessageID)msg.data[0];
	
	switch(m)
	{
		case SCENE_EST_Info:
			printf("SE: INFO: %s \r\n", msg.data);
		break;		
		case SCENE_EST_Bad:
			printf("SE: WARNING BAD MESSAGE!\r\n");
			break;
		case SCENE_EST_ParticleRender:
			//dont do anytihng here...
			break;
		case SCENE_EST_TrackedClusters:
			//dont do anytihng here...
			break;
		case SCENE_EST_UntrackedClusters:
			//dont do anytihng here...
			break;
			
		default:
			printf("SE: WARNING UNKNOWN MESSAGE TYPE! Type is %d \r\n", (int)msg.data[0]);
			break;
	}
}
*/
//TRANSMITTER----------------------------------------------------------------------------------------------------

SceneEstimatorInterfaceSender::SceneEstimatorInterfaceSender()
{
	udp_params params = udp_params();
	params.remote_ip = inet_addr(UDP_SCENE_EST_MULTICAST_ADDR);	
	params.local_port = 0;
	params.multicast_loopback = true;
	params.multicast_ttl=10;
	conn = new udp_connection (params);		
	sequenceNumber=0;
	sparseMsgTracked = malloc(MAX_PACKET_SIZE*10);
	//TrackedClusterMsgSeqCount=0;
	//LocalRoadModelMsgSeqCount=0;
	printf("Scene Estimator TX Interface Initialized. %s:%d\r\n",UDP_SCENE_EST_MULTICAST_ADDR,UDP_SCENE_EST_PORT);
}


SceneEstimatorInterfaceSender::~SceneEstimatorInterfaceSender()
{
	free(sparseMsgTracked);
	delete conn;
	printf("Scene Estimator TX Shutdown...\r\n");
}


void SceneEstimatorInterfaceSender::SendSceneEstimatorLocalRoadModel(LocalRoadModelEstimateMsg* output)
{
	output->id = LRM_LocalRoadModel;
	output->channelVersion = CHANNEL_VERSION;
	output->serializerType = SERIALIZER_TYPE_SE;
	output->sequenceNumber = sequenceNumber++;
	conn->send_message(output,sizeof(LocalRoadModelEstimateMsg),inet_addr(UDP_SCENE_EST_MULTICAST_ADDR),UDP_SCENE_EST_PORT);
}
/*
void SceneEstimatorInterfaceSender::SendSceneEstimatorParticlePoints(SceneEstimatorParticlePointsMsg* output)
{
	output->msgType = SCENE_EST_ParticleRender;
	conn->send_message(output,sizeof(SceneEstimatorParticlePointsMsg),inet_addr(UDP_SCENE_EST_MULTICAST_ADDR),UDP_SCENE_EST_PORT);
}
*/
void SceneEstimatorInterfaceSender::SendSceneEstimatorUntrackedClusters(SceneEstimatorUntrackedClusterMsg* output)
{	
	void* sparseMsg =	malloc(MAX_PACKET_SIZE*2);
	int byteoffset=0;	
	output->CopySparse (sparseMsg,byteoffset);
	output->sequenceNumber = sequenceNumber++;

	if(byteoffset < MAX_PACKET_SIZE)	
		conn->send_message(sparseMsg,byteoffset,inet_addr(UDP_SCENE_EST_MULTICAST_ADDR),UDP_SCENE_EST_PORT);		
	else
		printf("CRITICAL: Message too large to be sent in SendSceneEstimatorUntrackedClusters. Size is %d\n",byteoffset);
	
	free(sparseMsg);
}

void SceneEstimatorInterfaceSender::SendSceneEstimatorTrackedClusters(SceneEstimatorTrackedClusterMsg* output)
{			
	const int chunksize = 60000;
	memset(sparseMsgTracked,0x00,MAX_PACKET_SIZE * 10);
	int byteoffset=0;	
			
	bool msgOKsize = false;	
	msgOKsize = output->CopySparse (sparseMsgTracked,byteoffset,MAX_PACKET_SIZE*10);
	
	void* sparseMsgChunk =	malloc(MAX_PACKET_SIZE);

	if (msgOKsize)
	{
		int numChunks=byteoffset/chunksize + 1;		

		//so keep in mind here that there is no header on these chunks, so we need to append one on each iteration	
		SceneEstimatorTrackedClusterMsgHeader header;
				
		header.numTotChunks = numChunks;

		//send it in chunks	of chunksize bytes				
		if (numChunks > 1)
			printf("Sending big msg (%d)\n",numChunks);
	//	else
		//	printf("Sending small msg (%d)\n",numChunks);

		for (int chunkNum=0; chunkNum<numChunks; chunkNum++)
		{
			header.chunkNum = chunkNum;
			header.sequenceNumber = sequenceNumber++;
			if (chunkNum==numChunks-1) 
			{
				//send the last chunk
				int remaining = byteoffset%chunksize;//byteoffset - (numChunks * chunksize);
				memcpy(sparseMsgChunk,&header,sizeof(SceneEstimatorTrackedClusterMsgHeader));
				memcpy((unsigned char*)sparseMsgChunk + sizeof(SceneEstimatorTrackedClusterMsgHeader),(unsigned char*)sparseMsgTracked + (chunkNum * chunksize),remaining);
			//	printf("Sending bytes from %d to %d \n",(int)((chunkNum * chunksize)),(int)((chunkNum * chunksize)+remaining));
				conn->send_message(sparseMsgChunk,remaining + sizeof(SceneEstimatorTrackedClusterMsgHeader),inet_addr(UDP_SCENE_EST_MULTICAST_ADDR),UDP_SCENE_EST_PORT);					
			}
			else
			{
				//send big chunks				
				memcpy(sparseMsgChunk,&header,sizeof(SceneEstimatorTrackedClusterMsgHeader));
				memcpy((unsigned char*)sparseMsgChunk + sizeof(SceneEstimatorTrackedClusterMsgHeader),(unsigned char*)sparseMsgTracked + (chunkNum * chunksize),chunksize);
			//	printf("Sending bytes from %d to %d \n",(int)((chunkNum * chunksize)),(int)((chunkNum * chunksize)+chunksize));
				conn->send_message(sparseMsgChunk,chunksize+ sizeof(SceneEstimatorTrackedClusterMsgHeader),inet_addr(UDP_SCENE_EST_MULTICAST_ADDR),UDP_SCENE_EST_PORT);					
			}
		}
	}
	else
	{
		printf("HOLY stuff. The message was wayyyyyy too big (tracked cluster). Size: %d\n",byteoffset);
	}
	
	free(sparseMsgChunk);
}
