// nov 15 2007 svl5

#include <windows.h>
#include <io.h>
#include <fcntl.h>
#include <string>
#include <map>
#include <set>
#include <fstream>
#include <share.h>
#include <sys/types.h>
#include <stdio.h>
#include <sys/stat.h>
#include <float.h>
#include <limits>
using namespace std;

map<string,unsigned short> ReadPortMap(const char* fname){
  map<string,unsigned short> ret;
  ifstream f;
  f.open(fname);
  
  if(!f.is_open()) return ret;

  char buf[100];
  int port;

  while(!f.eof()){
    f >> buf >> port;
    if(strlen(buf)==0 || port<=0) continue;
    _strupr_s(buf,100);
    ret.insert(pair<string,unsigned short>(buf,port));
  }

  return ret;
};

void PrintUsage(){
  printf("USAGE: logsaw.exe <log name> [<REMOVE|ONLY> item1 [item2...etc]...]\n");
  printf("                             [LOCALTIME <min> <max>] [-o <output filename>]\n");
  printf("  REMOVE: keep all packets except for the ones specified in the item list\n");
  printf("  ONLY: keep ONLY the packets specified in the item list\n");
  printf("  Each item can be either a port number a named port specified in portmap.txt\n");
  printf("  LOCALTIME allows for specifying a local timestamp range.\n");
  printf("     (use '*' instead of either min or max to specify lack of respective bound)\n");
  printf("EXAMPLES:\n");
  printf(" logsaw somelog.dat REMOVE velodyne     -- all packets minus the velodyne\n");
  printf(" logsaw somelog.dat ONLY 30099          -- only port 30099 packets\n");
  printf(" logsaw somelog.dat LOCALTIME * 100    -- only packets w/ local timestamp < 100\n");
  printf(" logsaw somelog.dat ONLY velodyne ibeos LOCALTIME 120 140\n");
}

bool FileExists(const char* fName){
  FILE* tmp;
  fopen_s(&tmp,fName,"r");
  if(tmp==NULL) return false;
  fclose(tmp);
  return true;
}

const int MODE_REMOVE = 0;
const int MODE_ONLY = 1;

bool is_numeric(const char* str){
  for(int i=0;i<(int)strlen(str);i++){
    if(!isdigit(str[i])) return false;
  }
  return true;
}

void main(int argc, char** argv)
{
  int mode = MODE_REMOVE;
  double lts_low = -numeric_limits<float>::infinity();
  double lts_high = numeric_limits<float>::infinity();

  if(argc<4){
    PrintUsage();
    return;
  }

  map<string,unsigned short> portmap = ReadPortMap("portmap.txt");
  set<unsigned short> filterSet;

  string outputFileName = "";

  int timeArgState=0;
  for(int i=2;i<argc;i++){
    _strupr_s(argv[i],strlen(argv[i])+1);
    string arg = argv[i];

    if(timeArgState!=0){
      timeArgState++;
      if(timeArgState==2){
        if(arg!="*"){
          lts_low = atof(argv[i]);
        }
      }else if(timeArgState==3){
        if(arg!="*"){
          lts_high = atof(argv[i]);
        }
        timeArgState = 0;
      }      
    }else{
      if(arg=="REMOVE"){
        mode = MODE_REMOVE;
      }else if(arg=="ONLY"){
        mode = MODE_ONLY;
      }else if(arg=="LOCALTIME"){
        timeArgState = 1;
      
	    }else if(arg=="-O"){
		    if(i+1 >= argc){
          printf("ERROR: Must specify output file name after -O option\n");
          return;
        }
        outputFileName = argv[i+1];
        i++;
      }else{
        if(is_numeric(argv[i])){
          filterSet.insert(atoi(argv[i]));
        }else{
          map<string,unsigned short>::iterator itr = portmap.find(arg);
          if(itr==portmap.end()){
            printf("ERROR: Couldn't find port with name [%s] (misspelled argument?)\n",argv[i]);
            return;
          }
          filterSet.insert(itr->second);
        }
      }
    }
  }

  if(timeArgState!=0){
    printf("ERROR: incomplete LOCALTIME specification. Must speficy both a lower and upper bound.\n");
    return;
  }

  int iHandle,oHandle;
  
  _sopen_s(&iHandle,argv[1],_O_RDONLY|_O_BINARY,_SH_DENYNO,_S_IREAD|_S_IWRITE);
  if(iHandle<0){
    printf("ERROR: couldn't open log file [%s]\n",argv[1]);
    return;
  }

  if(outputFileName==""){
    char oFileName[2000];
    sprintf_s(oFileName,2000,"%s.sawed.dat",argv[1]);
    outputFileName = oFileName;
  }
  
  if(FileExists(outputFileName.c_str())){
    printf("File [%s] already exists.\n",outputFileName.c_str());
    printf("Type in YES to overwrite\n");
    char buf[100];
    gets_s(buf,100);
    if(strcmp(buf,"YES")) return;
  }

  _sopen_s(&oHandle,outputFileName.c_str(),_O_BINARY|_O_WRONLY|_O_CREAT|_O_TRUNC|_O_SEQUENTIAL,_SH_DENYWR,_S_IREAD|_S_IWRITE);
  if(oHandle<0){
    printf("Couldn't open output file (%s)\n",outputFileName.c_str());
    _close(iHandle);
    return;
  }

  unsigned char *buf = new unsigned char[70000];
  int cnt=0;

  __int64 bytesWritten=0,bytesRead=0,packetCntIn=0,packetCntOut=0;
  bool ltsStop=false;

  while(!_eof(iHandle)){  
    unsigned long ticks;
    unsigned char hdr[10];
    unsigned short port;
    unsigned long packetLen;
    double lts;
   
    if(_read(iHandle,&ticks,sizeof(ticks))!=sizeof(ticks)) break;
    bytesRead+=sizeof(ticks);
    if(_read(iHandle,hdr,10)!=10) break;;
    bytesRead+=10;
    if(_read(iHandle,&packetLen,sizeof(packetLen))!=sizeof(packetLen)) break;
    bytesRead+=sizeof(packetLen);

    port = *(unsigned short*)(hdr+8);
    lts = (double)ticks / 10000.0;

    if(packetLen >= 65536){ 
      printf("\nERROR: CORRUPT LOG FILE (packetLen = %lu)\n",packetLen);
      printf("\rWr: %d MB; Rd: %d MB; Ratio: %.2lf; Pckts In: %d; Pckts Out: %d\n",
		  (int)(bytesWritten/1024/1024),(int)(bytesRead/1024/1024),(float)bytesWritten/(float)bytesRead,(int)packetCntIn,(int)packetCntOut);
      break;
    }

    if((mode==MODE_REMOVE && filterSet.find(port)!=filterSet.end()) || 
       (mode==MODE_ONLY && filterSet.find(port)==filterSet.end()) || 
       (lts < lts_low || lts > lts_high)){
      _lseeki64(iHandle,packetLen,SEEK_CUR);
      packetCntIn++;
      bytesRead+=packetLen;
    }else{
      if(_read(iHandle,buf,packetLen)!=packetLen) break;
      packetCntIn++;
      bytesRead+=packetLen;
      _write(oHandle,&ticks,sizeof(ticks));
      _write(oHandle,hdr,10);
      _write(oHandle,&packetLen,sizeof(packetLen));
      _write(oHandle,buf,packetLen);
      bytesWritten+=packetLen+10+sizeof(ticks)+sizeof(packetLen);
      packetCntOut++;
    }

    cnt++;
    if(cnt>=5000){
	  cnt = 0;
      printf("\rWr: %d MB; Rd: %d MB; Ratio: %.2lf; Pckts In: %d; Pckts Out: %d",
		  (int)(bytesWritten/1024/1024),(int)(bytesRead/1024/1024),(double)bytesWritten/(double)bytesRead,(int)packetCntIn,(int)packetCntOut);
    }

    if(lts>lts_high){
      ltsStop = true;
      break;
    }
  }

  if(!_eof(iHandle) && !ltsStop){
    printf("WARNING: LogSaw was unable to process the entire input log file\n");
    printf("This info may help:\n");
    printf("\rWr: %d MB; Rd: %d MB; Ratio: %.2lf; Pckts In: %d; Pckts Out: %d\n",
		  (int)(bytesWritten/1024/1024),(int)(bytesRead/1024/1024),(float)bytesWritten/(float)bytesRead,(int)packetCntIn,(int)packetCntOut);
    printf("Good luck.\n");
  }else{
    printf("\nDONE!\n");
  }

  delete [] buf;
  _close(iHandle);
  _commit(oHandle);
  _close(oHandle);

  printf("New log written to [%s]\n",outputFileName.c_str());
}