#include "WriterLogFile.h"

#ifdef __cplusplus_cli
#pragma unmanaged
#endif

WriterLogFile::WriterLogFile()
{
	/*
	Default constructor for the writer log file class.  Prepares
	the file and initializes all variables to holder values.

	INPUTS:
		none.

	OUTPUTS:
		none.  Initializes variables to default values
	*/

	mIsValid = false;
	mLogFile = NULL;

	return;
}

WriterLogFile::~WriterLogFile()
{
	/*
	Destructor for writer log file class.  Closes the log file that
	was being written to and flushes its buffer.

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	CloseLog();

	return;
}

void WriterLogFile::CloseLog()
{
	/*
	Closes the log file (in preparation for being deleted).

	INPUTS:
		none.

	OUTPUTS:
		none.
	*/

	mIsValid = false;

	if (mLogFile != NULL)
	{
		fflush(mLogFile);
		fclose(mLogFile);
	}

	mLogFile = NULL;

	return;
}

void WriterLogFile::OpenLog(char* iLogName)
{
	/*
	Opens the desired log for writing to disk.

	INPUTS:
		iLogName - name of the log file to be opened.  Will accept either
			a valid full path or just a name.  If just a name is passed,
			the file will be created in the program's execution directory.

	OUTPUTS:
		none.  Sets the mIsValid flag to true if the file is opened successfully,
			false otherwise.
	*/

	errno_t err = fopen_s(&mLogFile, iLogName, "w");

	if (err != 0)
	{
		//could not open the file successfully
		mIsValid = false;
		CloseLog();
		printf_s("Warning: log writer could not open log file \"%s\".\n", iLogName);
		return;
	}

	//file opened successfully
	mIsValid = true;

	return;
}

void WriterLogFile::WriteToLog(char* iLogBuffer)
{
	/*
	Writes the desired buffer to the log file.

	INPUTS:
		iLogBuffer - null terminated string to write to the log file.
			NOTE: a newline character is automatically added to the file, 
			so iLogBuffer should not have one.

	OUTPUTS:
		none.
	*/

	if (mIsValid == false)
	{
		//can't log the line if the file isn't opened
		return;
	}

	fprintf_s(mLogFile, "%s\n", iLogBuffer);

	return;
}
