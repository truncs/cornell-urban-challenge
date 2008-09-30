#pragma once

#include <iostream>
#include <fstream>
#include <string>
#include <vector>
#include <sstream>
#include <algorithm>
#include "posecommon.h"
#include "../coords/dpmatrix4.h"
using namespace std;

class LoggedPoseInterface
{
public:
	
	LoggedPoseInterface(const char* logpath);
	~LoggedPoseInterface(void);
	bool LoggedPoseInterface::ReadUntilTimestamp (double timestamp, simpleVehicleState*);

private:

	//k current
	//i initial
	dpmatrix4 Rki;				//double Rki [4][4];		//rk,i
	dpmatrix4 Rkmoi;			//double Rkmoi [4][4];	//r k-1,i
	dpmatrix4 RkmoiInv;		//double RkmoInv [4][4];//inverse r k-1,i
	dpmatrix4 RkRkmo;			//double RkRkmo [4][4];	//r k * (r k-1)^-1
	ifstream log;
	bool firstRead;
	void CalcOptimalInvRkmoi();
	void PopulateRki(vector<string>& posePacket);
	void Tokenize(const string& str,vector<string>& tokens, const string&);
	double lastT;
};
