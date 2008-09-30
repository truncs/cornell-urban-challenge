#ifndef WRITERLOGFILE_H
#define WRITERLOGFILE_H

#include <FLOAT.H>
#include <STDIO.H>

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

class WriterLogFile
{
	//A writer log file class.  Opens and permits writes to a single log file.

private:

	//file pointer to the log file to be written to
	FILE* mLogFile;
	//whether the log file is valid (opened successfully)
	bool mIsValid;

public:

	WriterLogFile();
	~WriterLogFile();

	bool IsValid() {return mIsValid;}

	void CloseLog();
	void OpenLog(char* iLogName);
	void WriteToLog(char* iLogBuffer);
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif //WRITERLOGFILE_H
