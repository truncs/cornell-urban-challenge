#include "RTPoseInterface.h"


void PoseRelCallback(const pose_rel_msg& msg, void* args)
{
	((RTPoseInterface*)args)->InsertRk(msg);	
}


RTPoseInterface::RTPoseInterface()
{	
	pose = new pose_client();
	fired = true;
	firstRun= true;
	isRunning=true;
	desiredTime = 0.0f;
	InitializeCriticalSection(&msg_queue_cs);
	poseEvent = CreateEvent ( NULL , false , false , NULL);		
	pose->register_rel_callback(&PoseRelCallback,this);
	printf("RT Pose Relative Interface Initialized\r\n");
}

RTPoseInterface::~RTPoseInterface(void)
{
	DeleteCriticalSection (&msg_queue_cs);
}


void RTPoseInterface::InsertRk (const  pose_rel_msg& msg)
{
	EnterCriticalSection(&(msg_queue_cs));
	posemsgs.push_back(msg); 		
	latestPacket = (double)(msg.car_ts_secs) + (double)msg.car_ts_ticks /10000.0f;	
	LeaveCriticalSection(&(msg_queue_cs));	
	CheckIfReady();
}


void RTPoseInterface::CheckIfReady() //cehcks if the latest packet is new enough for us to process stuff
{
	if ((latestPacket >= desiredTime) && (fired==false)) //the latest packet is ready!
	{
		fired=true;
		GetVehicleState();
		//now set the event
		SetEvent(poseEvent);
	}
}

dpmatrix4 RTPoseInterface::GetRk(pose_rel_msg& msg)
{
	dpmatrix4 rk;
	for (int i=0; i<4; i++)
	{
		for (int j=0; j<4; j++)
		{
			rk(i,j) =	msg.Rinit2veh[i][j];
		}
	}
	return rk;
}

//Returns true when you do not need to wait for the single object!!!!
bool RTPoseInterface::GetPoseWaitUntilTimestamp (double timestamp)
{		
	
	desiredTime = timestamp;
	fired=false;
	
	if ((latestPacket >= desiredTime)) //we already have the packet
	{		
		EnterCriticalSection(&(msg_queue_cs));	
		GetVehicleState();
		LeaveCriticalSection(&(msg_queue_cs));	
		//now set the event
		return true;
	}
	else return false; //they will have to wait on the event...	
}

void RTPoseInterface::RemoveMessagesOlderThan (double time)
{
	double thistime;	
	while ( ((posemsgs.size() > 1) && (((double)posemsgs.front().car_ts_secs  + ((double)posemsgs.front().car_ts_ticks/10000.0f)) < time)))
	{
		thistime = (double)posemsgs.front().car_ts_secs  + ((double)posemsgs.front().car_ts_ticks/10000.0f);			
		posemsgs.pop_front();		
	}
	
}

pose_rel_msg RTPoseInterface::GetJustNewerThan (double time)
{	
	for (deque<pose_rel_msg>::iterator i (posemsgs.begin()),end(posemsgs.end()); i!=end; i++)
	{
		double ritime = ((double)i->car_ts_secs  + ((double)i->car_ts_ticks/10000.0f));		
		if (ritime>time)
		{			
			return (*i);		
		} 
	}
	return posemsgs.back();
}

void RTPoseInterface::GetVehicleState()
{
	
		//k current
		//i initial
		dpmatrix4 Rki;				//double Rki [4][4];		//rk,i
		dpmatrix4 Rkmni;			//double Rkmoi [4][4];	//r k-n,i
		dpmatrix4 RkmniInv;		//double RkmoInv [4][4];//inverse r k-n,i
		dpmatrix4 RkRkmn;			//double RkRkmo [4][4];	//r k * (r k-n)^-1
		
		pose_rel_msg Rk_msg;			//pose message at time k
		pose_rel_msg Rkmn_msg;		//pose message at time k -1
		EnterCriticalSection(&(msg_queue_cs));		
		Rk_msg = GetJustNewerThan(desiredTime);
		if(firstRun)
		{
			lastMsg = Rk_msg; 
			firstRun =false; 
		}
		Rkmn_msg = lastMsg;		
		RemoveMessagesOlderThan(desiredTime);
		//posemsgs.clear();//kill all old messaages
		//now add the newest back 
		//posemsgs.push_back (Rk_msg);
		LeaveCriticalSection(&(msg_queue_cs));
		Rki = GetRk(Rk_msg);
		Rkmni = GetRk(Rkmn_msg);
		double dt= ((double)Rk_msg.car_ts_secs+(double)Rk_msg.car_ts_ticks/10000.0f) - 
				((double)Rkmn_msg.car_ts_secs+(double)Rkmn_msg.car_ts_ticks/10000.0f);
		//printf ("dt is %f\r\n",dt);
		CalcOptimalInvRkmni(RkmniInv,Rkmni);
		
		RkRkmn = Rki * RkmniInv;
		
		double sinDY = RkRkmn(2,0);
		double cosDY = sqrt((RkRkmn(2,1)*RkRkmn(2,1)) + (RkRkmn(2,2)*RkRkmn(2,2)));
		double sinDX = ((-1.0f * RkRkmn(2,1)) / cosDY); 
		double cosDX = RkRkmn(2,2) / cosDY;				  
		double sinDZ = ((-1.0f * RkRkmn(1,0)) / cosDY);	
		double cosDZ = RkRkmn(0,0) / cosDY;					  

		//vehicle coordinates, delta angle (yaw rate = drZ / dt)
		vehState.dRY = atan2(sinDY,cosDY);
		vehState.dRX = atan2(sinDX,cosDX);
		vehState.dRZ = atan2(sinDZ,cosDZ);

		//vehicle coordinates, delta pos (forward speed = dX / dt)
		vehState.dX = (RkRkmn(0,0) * RkRkmn(0,3) * -1.0f) + (RkRkmn(1,0) * RkRkmn(1,3) * -1.0f) + (RkRkmn(2,0) * RkRkmn(2,3) * -1.0f);
		vehState.dY = (RkRkmn(0,1) * RkRkmn(0,3) * -1.0f) + (RkRkmn(1,1) * RkRkmn(1,3) * -1.0f) + (RkRkmn(2,1) * RkRkmn(2,3) * -1.0f);
		vehState.dZ = (RkRkmn(0,2) * RkRkmn(0,3) * -1.0f) + (RkRkmn(1,2) * RkRkmn(1,3) * -1.0f) + (RkRkmn(2,2) * RkRkmn(2,3) * -1.0f);
		vehState.dt = dt;
		vehState.timestamp = (double)(Rk_msg.car_ts_secs+(double)Rk_msg.car_ts_ticks/10000.0f);    
		//printf("Queue is: %d Desired Time: %f Actual Time: %f\r\n",posemsgs.size(),desiredTime,vehState.timestamp);
		lastMsg = Rk_msg;
}


void RTPoseInterface::CalcOptimalInvRkmni(dpmatrix4 &RkmniInv, dpmatrix4 &Rkmni)
{
	RkmniInv(0,0) = Rkmni(0,0);
	RkmniInv(1,0) = Rkmni(0,1);
	RkmniInv(2,0) = Rkmni(0,2);
	RkmniInv(3,0) = 0.0f;
	
	RkmniInv(0,1) = Rkmni(1,0);
	RkmniInv(1,1) = Rkmni(1,1);
	RkmniInv(2,1) = Rkmni(1,2);
	RkmniInv(3,1) = 0.0f;
	
	RkmniInv(0,2) = Rkmni(2,0);
	RkmniInv(1,2) = Rkmni(2,1);
	RkmniInv(2,2) = Rkmni(2,2);
	RkmniInv(3,2) = 0.0f;
	
	RkmniInv(0,3) = (Rkmni(0,0) * Rkmni(0,3) * -1.0f) + (Rkmni(1,0) * Rkmni(1,3) * -1.0f) + (Rkmni(2,0) * Rkmni(2,3) * -1.0f);
	RkmniInv(1,3) = (Rkmni(0,1) * Rkmni(0,3) * -1.0f) + (Rkmni(1,1) * Rkmni(1,3) * -1.0f) + (Rkmni(2,1) * Rkmni(2,3) * -1.0f);
	RkmniInv(2,3) = (Rkmni(0,2) * Rkmni(0,3) * -1.0f) + (Rkmni(1,2) * Rkmni(1,3) * -1.0f) + (Rkmni(2,2) * Rkmni(2,3) * -1.0f);
	RkmniInv(3,3) = 1.0f;
}

