#pragma once

#include "..\..\Common\network\udp_connection.h"
#include "..\..\Common\utility\PacketLogger.h"

#include <string>
using namespace std;

class GenericUDPLoggingClient
{
private:
	udp_connection *conn;		
	static void UDPCallback(udp_message& msg, udp_connection* conn, void* arg);

  PacketLogger* logger;
  string name;

  TIMER last_stats_print;
  int packetCnt;
  int byteCnt;

public:
	GenericUDPLoggingClient(ushort port, const char* multicastIP, PacketLogger* logger=NULL, const char* name=NULL, int subnet=-1);
	~GenericUDPLoggingClient();

  void StartLogging(PacketLogger* logger){this->logger = logger;}
  void StopLogging(){logger = NULL;}

  void PrintStats(HANDLE hStdout=NULL);
};
