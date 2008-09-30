#include "..\..\common\Ibeo\IbeoClient.h"
#include "..\..\common\poseinterface\poseclient.h"
#include "..\..\common\Ibeo\IbeoSyncClient.h"
#include "..\..\common\LidarClusterInterface\LidarClusterClient.h"
#include "GenericUDPLoggingClient.h"

#include "time.h"

__int64 prog_begin_ts;

string GenerateLogFileName(){
  // figure out how to name a log file if it needs to be used...
  char buf[200], buf2[200];
  struct tm   *newTime;
  time_t      szClock;
  time( &szClock );
  newTime = localtime( &szClock );
  strftime(buf,150,"%Y-%m-%d %I %M %S%p",newTime);


#ifdef LOG_VELODYNE
  char tag[] = "-velo";
#else
  char tag[] = "";
#endif

  if(GetDiskFreeSpaceExA("Z:\\",NULL,NULL,NULL)!=FALSE)
    sprintf(buf2,"Z:\\logs\\udp_log %s%s.dat",buf,tag);
  else if(GetDiskFreeSpaceExA("E:\\",NULL,NULL,NULL)!=FALSE)
    sprintf(buf2,"E:\\logs\\udp_log %s%s.dat",buf,tag);
  else
    sprintf(buf2,"C:\\logs\\udp_log %s%s.dat",buf,tag);


  return string(buf2);
}

vector<GenericUDPLoggingClient*> loggers;
PacketLogger *logger = NULL;

void AddLogger(int port, string multicastIP,string name,int subnet=-1){
  GenericUDPLoggingClient * c = new GenericUDPLoggingClient(port,multicastIP.c_str(),logger,name.c_str(),subnet);
  loggers.push_back(c);
}

void main(){
  HANDLE hStdout = GetStdHandle(STD_OUTPUT_HANDLE); 
  prog_begin_ts = getTime();

  string lfn = GenerateLogFileName();  
  
#ifndef UDP_LOGGER_MONITOR_MODE
  logger = new PacketLogger(lfn.c_str(),prog_begin_ts);

  if(!logger->IsOpen()){
    printf("Logger couldn't open log file [%s]\nExiting.\n",lfn.c_str());
    delete logger;
		system("pause");
    return;
  }
#endif
	
  AddLogger(92,"239.132.1.4","LN200");  
  AddLogger(30006,"239.132.1.6", "Actuation");
  AddLogger(30008,"232.132.1.8", "CamSync");
  AddLogger(30009,"239.132.1.9", "IbeoSync");
  AddLogger(30010,"239.132.1.10", "PwrSupply");
  AddLogger(3679,"239.132.1.11",  "StopLine");
  AddLogger(30012,"239.132.1.12", "Delphi");
  AddLogger(30016,"239.132.1.16", "SICK");
  //AddLogger(30020,"239.132.1.20", "Gimbal Pos");
  AddLogger(4839,"239.132.1.33",  "Pose");
  AddLogger(30034,"239.132.1.34", "LocalMap");
  AddLogger(30035,"239.132.1.35", "SceneEst");
  AddLogger(30036,"239.132.1.36", "MobilEye");
  AddLogger(30037,"239.132.1.37", "RoadFit");
  AddLogger(30038,"239.132.1.38", "SickEdge");
  AddLogger(30039,"239.132.1.39", "SickClstrs");
  AddLogger(30040,"239.132.1.40", "MESS");
  AddLogger(30041,"239.132.1.41", "Clusters");  
  AddLogger(30060,"239.132.1.60", "Ibeo");   
	AddLogger(30061,"239.132.1.61", "OccupGrid");   
  AddLogger(30065,"239.132.1.65", "VeloRoad");   
  AddLogger(30098,"239.132.1.98", "CameraWide"); 
  AddLogger(30099,"239.132.1.99", "Camera"); 
  AddLogger(30100,"239.132.1.100","HealthMon");	
	AddLogger(30101,"239.132.1.101","ArbCmdChan");	
  //AddLogger(30105,"239.132.1.105","GimbalCmds");
	AddLogger(30237,"239.132.1.237","ArbOutChan");
	AddLogger(30238,"239.132.1.238","ArbInfChan");	
  AddLogger(30123,"224.0.2.7",    "ArbPosChan");
	AddLogger(30123,"224.0.2.8",    "OpRoadChan");		
	AddLogger(30123,"224.0.2.10",   "ArbVehSpd");
  
#ifdef LOG_VELODYNE
  AddLogger(2368,"","Velodyne",3);
	AddLogger(30,"255.255.255.255","Timeserver");
#endif

	
  printf("Logging to [%s]...\n",lfn.c_str());
  printf("Press [space]+[e] to quit\n");

  unsigned int cnt=0;
  while(!(GetKeyState('E')&0x80) || !(GetKeyState(' ')&0x80)){
    Sleep(100);
    cnt++;
    if(cnt==20){
      cnt = 0;
      system("cls");

      if(logger==NULL){
        SetConsoleTextAttribute(hStdout, FOREGROUND_RED|FOREGROUND_GREEN|FOREGROUND_INTENSITY|BACKGROUND_BLUE);
      }
      printf("%10s   pckts/sec     %kB/sec        pckt len                       \n","Name");
      printf("========================================================================\n");
      if(logger==NULL){
        SetConsoleTextAttribute(hStdout,0x7);
      }
      
      for(unsigned int i=0;i<loggers.size();i++)
        loggers[i]->PrintStats(hStdout);
      
      if(logger!=NULL){
        SetConsoleTextAttribute(hStdout, FOREGROUND_GREEN|FOREGROUND_INTENSITY);
        printf("Logging to [%s]...\n",lfn.c_str());
        SetConsoleTextAttribute(hStdout,0x7);
      }
      printf("Press [space]+[e] to quit\n");

      if(logger==NULL){
        SetConsoleTextAttribute(hStdout, FOREGROUND_RED|FOREGROUND_GREEN|FOREGROUND_INTENSITY|BACKGROUND_BLUE);
        printf("------------------ MONITOR MODE (NO LOGGING TO HD) ---------------------",27);
        SetConsoleTextAttribute(hStdout,0x7);
      }
    }
  }

  for(unsigned int i=0;i<loggers.size();i++){
    loggers[i]->StopLogging();
    delete loggers[i];
  }
  
  Sleep(100);

  if(logger!=NULL)
    delete logger;
}