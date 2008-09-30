#ifndef READERLOGFILE_H
#define READERLOGFILE_H

#include <FLOAT.H>
#include <STDIO.H>

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

#define RLF_BUFFERSIZE 2048

class ReaderLogFile
{
	//A reader log file class.  Opens and permits reads to a single log file.
private:

	//file pointer to the log file to be read
	FILE* mLogFile;
	//whether the log file is valid (opened successfully)
	bool mIsValid;

	//the timestamp on the log file's next line
	double mLogTime;
	//a buffer containing the next line of the log file
	char mLogBuffer[RLF_BUFFERSIZE];

public:

	ReaderLogFile();
	~ReaderLogFile();

	bool IsValid() {return mIsValid;}
	char* LogBuffer() {return mLogBuffer;}
	double LogTime() {return mLogTime;}

	void CloseLog();
	bool GetNextLogLine();
	void OpenLog(char* iLogName);
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //READERLOGFILE_H
