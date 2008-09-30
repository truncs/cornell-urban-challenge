#include "LoggedPoseInterface.h"

LoggedPoseInterface::LoggedPoseInterface(const char* logpath)
{	
	firstRead=true;
	lastT=0;
	log.open (logpath);  
}
LoggedPoseInterface::~LoggedPoseInterface(void)
{
	firstRead=true;
	if (log.is_open ()) 
		log.close ();
}

bool LoggedPoseInterface::ReadUntilTimestamp (double timestamp,simpleVehicleState* state)
{		
	//double accdt =0;
	if (log.is_open())
  {
    while (! log.eof())
    {				
			string line;
			vector<string> posePacket;
			getline (log,line);			
			Tokenize(line,posePacket,",");					
			double packet_timestamp = atof(posePacket[0].c_str());	

			if (firstRead) //to intialize, we want rkmoi = rki
			{					
				Rkmoi = Rki; 
				firstRead=false;					
			} 
			
			while ((packet_timestamp < timestamp) && (! log.eof()))
			{
				getline (log,line);		
				posePacket.clear ();
				Tokenize(line,posePacket,",");						
				packet_timestamp = atof(posePacket[0].c_str());	
			//	printf("POSE TS:%f TOOLATE\n",packet_timestamp);				
			}
			if (log.eof()) return false;

			PopulateRki(posePacket);	
			double dt = packet_timestamp  - lastT;

			CalcOptimalInvRkmoi();
			RkRkmo = Rki * RkmoiInv;
			
			double sinDY = RkRkmo(2,0);
			double cosDY = sqrt((RkRkmo(2,1)*RkRkmo(2,1)) + (RkRkmo(2,2)*RkRkmo(2,2)));
			double sinDX = ((-1.0f * RkRkmo(2,1)) / cosDY); 
			double cosDX = RkRkmo(2,2) / cosDY;				  
			double sinDZ = ((-1.0f * RkRkmo(1,0)) / cosDY);	
			double cosDZ = RkRkmo(0,0) / cosDY;					  

			//vehicle coordinates, delta angle (yaw rate = drZ / dt)
			state->dRY = atan2(sinDY,cosDY);
			state->dRX = atan2(sinDX,cosDX);
			state->dRZ = atan2(sinDZ,cosDZ);

			//vehicle coordinates, delta pos (forward speed = dX / dt)
			state->dX = (RkRkmo(0,0) * RkRkmo(0,3) * -1.0f) + (RkRkmo(1,0) * RkRkmo(1,3) * -1.0f) + (RkRkmo(2,0) * RkRkmo(2,3) * -1.0f);
			state->dY = (RkRkmo(0,1) * RkRkmo(0,3) * -1.0f) + (RkRkmo(1,1) * RkRkmo(1,3) * -1.0f) + (RkRkmo(2,1) * RkRkmo(2,3) * -1.0f);
			state->dZ = (RkRkmo(0,2) * RkRkmo(0,3) * -1.0f) + (RkRkmo(1,2) * RkRkmo(1,3) * -1.0f) + (RkRkmo(2,2) * RkRkmo(2,3) * -1.0f);
			state->dt = dt;
			state->timestamp = packet_timestamp;
			
			printf("POSE TS: %f DT:%f\n", packet_timestamp,dt);
			Rkmoi = Rki;	
			lastT = packet_timestamp;
			return true; //we are done				
    }		
  }
	return false;
}

void LoggedPoseInterface::PopulateRki(vector<string>& posePacket)
{
	Rki(0,0) = atof(posePacket[2].c_str());			
	Rki(1,0) = atof(posePacket[3].c_str());			
	Rki(2,0) = atof(posePacket[4].c_str());			
	Rki(3,0) = 0.0f;
	Rki(0,1) = atof(posePacket[5].c_str());			
	Rki(1,1) = atof(posePacket[6].c_str());			
	Rki(2,1) = atof(posePacket[7].c_str());			
	Rki(3,1) = 0.0f;
	Rki(0,2) = atof(posePacket[8].c_str());			
	Rki(1,2) = atof(posePacket[9].c_str());			
	Rki(2,2) = atof(posePacket[10].c_str());			
	Rki(3,2) = 0.0f;
	Rki(0,3) = atof(posePacket[11].c_str());			
	Rki(1,3) = atof(posePacket[12].c_str());			
	Rki(2,3) = atof(posePacket[13].c_str());			
	Rki(3,3) = 1.0f;
}



void LoggedPoseInterface::CalcOptimalInvRkmoi()
{
	RkmoiInv(0,0) = Rkmoi(0,0);
	RkmoiInv(1,0) = Rkmoi(0,1);
	RkmoiInv(2,0) = Rkmoi(0,2);
	RkmoiInv(3,0) = 0.0f;
	
	RkmoiInv(0,1) = Rkmoi(1,0);
	RkmoiInv(1,1) = Rkmoi(1,1);
	RkmoiInv(2,1) = Rkmoi(1,2);
	RkmoiInv(3,1) = 0.0f;
	
	RkmoiInv(0,2) = Rkmoi(2,0);
	RkmoiInv(1,2) = Rkmoi(2,1);
	RkmoiInv(2,2) = Rkmoi(2,2);
	RkmoiInv(3,2) = 0.0f;
	
	RkmoiInv(0,3) = (Rkmoi(0,0) * Rkmoi(0,3) * -1.0f) + (Rkmoi(1,0) * Rkmoi(1,3) * -1.0f) + (Rkmoi(2,0) * Rkmoi(2,3) * -1.0f);
	RkmoiInv(1,3) = (Rkmoi(0,1) * Rkmoi(0,3) * -1.0f) + (Rkmoi(1,1) * Rkmoi(1,3) * -1.0f) + (Rkmoi(2,1) * Rkmoi(2,3) * -1.0f);
	RkmoiInv(2,3) = (Rkmoi(0,2) * Rkmoi(0,3) * -1.0f) + (Rkmoi(1,2) * Rkmoi(1,3) * -1.0f) + (Rkmoi(2,2) * Rkmoi(2,3) * -1.0f);
	RkmoiInv(3,3) = 1.0f;
}

void LoggedPoseInterface::Tokenize(const string& str,
                      vector<string>& tokens,
                      const string& delimiters)
{
    // Skip delimiters at beginning.
    string::size_type lastPos = str.find_first_not_of(delimiters, 0);
    // Find first "non-delimiter".
    string::size_type pos     = str.find_first_of(delimiters, lastPos);

    while (string::npos != pos || string::npos != lastPos)
    {
        // Found a token, add it to the vector.
        tokens.push_back(str.substr(lastPos, pos - lastPos));
        // Skip delimiters.  Note the "not_of"
        lastPos = str.find_first_not_of(delimiters, pos);
        // Find next "non-delimiter"
        pos = str.find_first_of(delimiters, lastPos);
    }
}

