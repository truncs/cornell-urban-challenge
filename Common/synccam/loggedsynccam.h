#pragma once

#include <windows.h>
#include <stdio.h>
#include <malloc.h>
#include <string>
#include <vector>

using namespace std;


#define BUFSIZE MAX_PATH


class LoggedSyncCam
{
public:

	LoggedSyncCam(string dir, string ext = "bmp");	
	void GetFilesInDir(string dir, vector<string>& filenames, string ext = "bmp");	
	string GetNextImage(double &timestamp);
	bool   CanGetNextImage();
	
private:

	vector<string> files;
	string dir;
	unsigned long filenum;
};