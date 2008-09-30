#ifndef MESS_SENDER_H_SEPT_03_2007_SVL5
#define MESS_SENDER_H_SEPT_03_2007_SVL5

#include "../network/udp_connection.h"
#include "mess.h"

class MessSender{
  friend DWORD WINAPI MessSenderThread(LPVOID);

public:
  MessSender(unsigned char tag, string name){
    this->tag = tag;
    this->name = name;
    InitializeCriticalSection(&cs);

    udp_params params = udp_params();
	  params.remote_ip = inet_addr(MESS_IP_STR);	
	  params.local_port = 0;
	  params.multicast_loopback = true;
	  params.multicast_ttl=10;
	  conn = new udp_connection (params);		

    messThreadHandle = CreateThread(NULL,NULL,MessSenderThread,this,NULL,NULL);
    running = true;
  }
  ~MessSender(){
    running = false;
    if(WaitForSingleObject(messThreadHandle,1000)!=WAIT_OBJECT_0)
      TerminateThread(messThreadHandle,17);
    DeleteCriticalSection(&cs);
    delete conn;
  }

  void SendError(string err, BYTE subclass=0){
    EnterCriticalSection(&cs);
    buf[0] = MESS_ERROR | subclass;
    buf[1] = tag;
    memcpy(buf+2,err.c_str(),err.length());
    conn->send_message(buf,err.length()+2,MESS_IP_STR,MESS_PORT);
    LeaveCriticalSection(&cs);
  }

  void SendStartup(){
    EnterCriticalSection(&cs);
    buf[0] = 0;
    buf[1] = tag;
    memcpy(buf+2,name.c_str(),name.length());
    conn->send_message(buf,name.length()+2,MESS_IP_STR,MESS_PORT);
    LeaveCriticalSection(&cs);
  }

  void SendBootup(int seq){
    EnterCriticalSection(&cs);
    buf[0] = seq;
    buf[1] = tag;
    memcpy(buf+2,name.c_str(),name.length());
    conn->send_message(buf,name.length()+2,MESS_IP_STR,MESS_PORT);
    LeaveCriticalSection(&cs);
  }

  void SendNameDecl(){
    EnterCriticalSection(&cs);
    buf[0] = MESS_NAME_DECL;
    buf[1] = tag;
    memcpy(buf+2,name.c_str(),name.length());
    conn->send_message(buf,name.length()+2,MESS_IP_STR,MESS_PORT);
    LeaveCriticalSection(&cs);
  }

private:
  string name;
  udp_connection *conn;
  CRITICAL_SECTION cs;
  unsigned char tag;
  HANDLE messThreadHandle;
  BYTE buf[60000];
  volatile bool running;
};

static DWORD WINAPI MessSenderThread(LPVOID a){
  MessSender* ms = (MessSender*)a;

  ms->SendStartup();
  int seq=0;
  int cnt=0;

  while(ms->running){
    Sleep(1000);
    
    if(seq<8){
      ms->SendBootup(seq);
      seq++;
    }

    cnt++;
    if(cnt>60){
      ms->SendNameDecl();
      cnt=0;
    }
  }

  return 0;
};


#endif //MESS_SENDER_H_SEPT_03_2007_SVL5
