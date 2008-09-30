#include "ReaderLogFile.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

ReaderLogFile::ReaderLogFile()
{
	/*
	Default constructor for the log file reader.  Initializes variables.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	//initialize member variables to invalid values
	mLogFile = NULL;
	mIsValid = false;
	mLogTime = DBL_MAX;
	mLogBuffer[0] = '\0';

	return;
}

ReaderLogFile::~ReaderLogFile()
{
	/*
	Default destructor for the log file reader.  Closes the open log file gracefully.

	INPUTS:
		none.

	OUTPUTS:
		none.  Closes the log file and sets mIsValid to false
	*/

	mIsValid = false;
	CloseLog();

	return;
}

bool ReaderLogFile::GetNextLogLine()
{
	/*
	Reads the next line from the log file and stores it in the buffer.

	INPUTS:
		none.

	OUTPUTS:
		rSuccess - true if a line was read successfully, false otherwise.  
			If read is successful, the line is stored in mLogBuffer and 
			mLogTime is set properly.  If unsuccessful, mLogTime is set 
			to DBL_MAX and the buffer is set to empty.
	*/

	bool rSuccess = false;
	errno_t err = -1;

	mLogTime = DBL_MAX;
	mLogBuffer[0] = '\0';

	if (mIsValid == false)
	{
		//do not populate the event for an invalid log file
		return rSuccess;
	}

	while (err == -1)
	{
		//continue reading in until a line with a valid timestamp is found or eof is found
		if (fgets(mLogBuffer, RLF_BUFFERSIZE, mLogFile) != NULL)
		{
			//successfully read a line: try to determine event time
			err = sscanf_s(mLogBuffer, "%lg", &mLogTime);
		}
		else
		{
			//end of file: return a bad read and give up

			mLogTime = DBL_MAX;
			mLogBuffer[0] = '\0';
			rSuccess = false;
			return rSuccess;
		}
	}

	rSuccess = true;

	return rSuccess;
}

void ReaderLogFile::OpenLog(char* iLogName)
{
	/*
	Attempts to open the log file and prepare the first buffer from it.

	INPUTS:
		iLogName - name of the log file to be opened (absolute or relative path)

	OUTPUTS:
		none.  mLogFile is opened if successful and mIsValid is set accordingly.
	*/

	//close any existing log file that is opened
	CloseLog();

	//try to open the log file
	errno_t err;

	//initialize member variables to invalid values
	mLogTime = DBL_MAX;
	mLogBuffer[0] = '\0';

	err = fopen_s(&mLogFile, iLogName, "r");
	if (err != 0)
	{
		//could not open log file
		mIsValid = false;
		mLogFile = NULL;
	}
	else
	{
		mIsValid = true;
		printf("Opened connection to log file %s.\n", iLogName);
	}

	//read in the first line of the log file
	GetNextLogLine();

	return;
}

void ReaderLogFile::CloseLog()
{
	/*
	Closes the log file if it is open.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	mIsValid = false;
	mLogTime = DBL_MAX;
	mLogBuffer[0] = '\0';

	if (mLogFile != NULL)
	{
		fclose(mLogFile);
	}

	return;
}
