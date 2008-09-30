#pragma once

#include <iostream>
#include <fstream>
#include <string>
#include <vector>
#include <deque>
#include <sstream>
#include <algorithm>
#include "posecommon.h"
#include "../coords/dpmatrix4.h"
#include "poseclient.h"

using namespace std;



class RTPoseInterface
{
public:
	
	RTPoseInterface();
	~RTPoseInterface(void);
	void InsertRk (const pose_rel_msg& msg);
	bool GetPoseWaitUntilTimestamp (double timestamp);
	HANDLE poseEvent;
	simpleVehicleState vehState;
	bool isRunning;
private:

	pose_client* pose;	
	bool fired;
	dpmatrix4 GetRk(pose_rel_msg& msg);
	double desiredTime;
	double latestPacket;
	bool firstRun;
	deque<pose_rel_msg> posemsgs;
	pose_rel_msg lastMsg;	

	void PopulateRki(vector<string>& posePacket);	
	void CalcOptimalInvRkmni(dpmatrix4 &RkmniInv, dpmatrix4 &Rkmni);
	void RTPoseInterface::RemoveMessagesOlderThan (double time);
	pose_rel_msg RTPoseInterface::GetJustNewerThan (double time);
	void CheckIfReady();
	void GetVehicleState();	
	CRITICAL_SECTION msg_queue_cs;
};
