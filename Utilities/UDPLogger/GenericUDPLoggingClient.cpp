#include "GenericUDPLoggingClient.h"
#include "Common\time\timing.h"
#include "Common\Network\Net_Utility.h"

extern TIMER prog_begin_ts;

GenericUDPLoggingClient::GenericUDPLoggingClient(ushort port, const char* multicastIP, PacketLogger* logger, const char* name, int subnet){
	udp_params params = udp_params();
  if(strlen(multicastIP)>0)
	  params.remote_ip = inet_addr(multicastIP);
	params.local_port = port;
  params.reuse_addr = true;
  
  if(subnet>=0){
    unsigned int addr;
    if(find_subnet_adapter((unsigned int)subnet,addr))
      params.local_ip = addr;
  }
    


  this->logger = logger;
  this->name = name;

  last_stats_print = getTime();
  packetCnt = 0;
  byteCnt=0;	
	
	conn = new udp_connection (params);	
	conn->set_callback (&UDPCallback, this);
	printf("Generic UDP Listener Initialized. %s:%d\r\n",multicastIP,port);
}

GenericUDPLoggingClient::~GenericUDPLoggingClient (){
  StopLogging();
	delete conn;
}

void GenericUDPLoggingClient::UDPCallback(udp_message& msg, udp_connection* conn, void* arg){
  GenericUDPLoggingClient* gulc = (GenericUDPLoggingClient*)arg;
  if(gulc->logger!=NULL) gulc->logger->Log(msg);
  gulc->packetCnt++;
  gulc->byteCnt += (int)msg.len;
}

void GenericUDPLoggingClient::PrintStats(HANDLE hStdout){
  double dt = timeElapsed(last_stats_print);
  double kBps = (double)byteCnt / dt / 1024.0;
  double pps = (double)packetCnt / dt;
  last_stats_print = getTime();
  if(hStdout!=NULL && packetCnt==0){
    SetConsoleTextAttribute(hStdout, FOREGROUND_RED|FOREGROUND_INTENSITY);
    printf("%10s  %8.1lf\t%8.2lf\t%8.2lf\t%s\n",name.c_str(),pps,kBps,(packetCnt>0)?(double)byteCnt/(double)packetCnt:0,packetCnt>0?"":"SILENT");
    SetConsoleTextAttribute(hStdout,0x7);
  }
  else{
    printf("%10s  %8.1lf\t%8.2lf\t%8.2lf\t%s\n",name.c_str(),pps,kBps,(packetCnt>0)?(double)byteCnt/(double)packetCnt:0,packetCnt>0?"":"SILENT");
  }
  byteCnt=packetCnt=0;
}