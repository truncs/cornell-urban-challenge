#ifndef PACKET_LOGGER_H_JULY_25_2007_SVL5
#define PACKET_LOGGER_H_JULY_25_2007_SVL5

#include <windows.h>
#include "../network/udp_message.h"
#include <io.h>
#include <fcntl.h>
#include "../time/timing.h"

typedef unsigned long ulong;
typedef unsigned short ushort;
typedef unsigned char byte;

class PacketLogger{
public:
  PacketLogger(const char* fname, __int64 time_origin, bool append=false){
    fHandle = -1;
    this->time_origin = time_origin;
   
		
    InitializeCriticalSection(&cs);

    this->fname = fname;
    this->append = append;
    triedOpen=false;

		if(fHandle<0) {
			int oflag = _O_BINARY|_O_WRONLY|_O_CREAT;
      if(append) oflag |= _O_APPEND;
      fHandle = _open(fname,oflag);		
      triedOpen = true;
    }
  }

  ~PacketLogger(){
    if(fHandle!=-1) _close(fHandle);
    DeleteCriticalSection(&cs);
  }

  bool IsOpen() const {
    if(!triedOpen) return true; 
    return fHandle!=-1;
  };

  bool Log(udp_message& msg){
    byte entry_id[10];
    memcpy(entry_id,&msg.source_addr,sizeof(msg.source_addr));
    memcpy(entry_id+4,&msg.dest_addr,sizeof(msg.dest_addr));
    memcpy(entry_id+8,&msg.port,sizeof(msg.port));
    return Log(entry_id,msg.data,(ulong)msg.len);
  }

  bool Log(byte entry_id[10], void* packet, ulong packet_len){
    EnterCriticalSection(&cs);
		if(fHandle<0) {
      
			int oflag = _O_BINARY|_O_WRONLY|_O_CREAT;
      if(append) oflag |= _O_APPEND;
      fHandle = _open(fname.c_str(),oflag);		
      triedOpen = true;
    }

    double t = timeElapsed(time_origin);
    ulong ticks = (ulong)((t*10000.0)+.5);
    
    if(fHandle<0) 
		{
			LeaveCriticalSection(&cs);			
			return false;
		}
    _write(fHandle,&ticks,sizeof(ticks));
    _write(fHandle,entry_id,10);
    _write(fHandle,&packet_len,sizeof(packet_len));
    _write(fHandle,packet,packet_len);
    LeaveCriticalSection(&cs);
    return true;
  }


private:
  CRITICAL_SECTION cs;
  volatile int fHandle;
  string fname;
  bool append;
  __int64 time_origin;
  bool triedOpen;
};


#endif //PACKET_LOGGER_H_JULY_25_2007_SVL5
