#include "loggedSyncCam.h"

#include <malloc.h>
#include <tchar.h> 
#include <wchar.h> 
#include <strsafe.h>


LoggedSyncCam::LoggedSyncCam(string dir, string ext)
{		
	filenum=0;
	 char stuff = dir.at(dir.length() - 1);
	 if (stuff == '\\')
		this->dir = dir.substr (0,dir.length () -1);
	 else
		 this->dir = dir;	
	GetFilesInDir (dir,files, ext);
}

bool LoggedSyncCam::CanGetNextImage()
{
	if (filenum >= files.size()) return 0; return 1;
}

string LoggedSyncCam::GetNextImage(double &timestamp)
{
	if (filenum >= files.size()) return 0;
	string filename = files[filenum];
	string time = filename.substr (0,time.length() - 4); //remove extension
	timestamp = atof(time.c_str());
	filenum++;
	string fullfilename(dir.c_str ());
	fullfilename.append ("\\" + filename);
	return fullfilename;
}


//jacked from msdn
void LoggedSyncCam::GetFilesInDir(string dir, vector<string>& filenames, string ext)
{
	 WIN32_FIND_DATAA FindFileData;
   HANDLE hFind = INVALID_HANDLE_VALUE;
   DWORD dwError;
   LPTSTR DirSpec;

   DirSpec = (LPTSTR) malloc (BUFSIZE);
	 char stuff = dir.at(dir.length() - 1);
	 if (stuff == '\\')
		dir = dir.append ("*." + ext);
	 else
		 dir = dir.append ("\\*."  + ext);
   // Find the first file in the directory.
   hFind = FindFirstFileA(dir.c_str(), &FindFileData);

   if (hFind == INVALID_HANDLE_VALUE) 
   {
      printf("Invalid file handle. Error is %u.\n",  GetLastError()); return;      
   } 
   else 
   {			
			
			filenames.push_back((char*)FindFileData.cFileName);					   
      // List all the other files in the directory.
      while (FindNextFileA(hFind, &FindFileData) != 0) 
				filenames.push_back((char*)FindFileData.cFileName);					
    
      dwError = GetLastError();
      FindClose(hFind);
      if (dwError != ERROR_NO_MORE_FILES) 
      {
         printf (("FindNextFile error. Error is %u.\n"), dwError);  return;
      }
   }
	 printf("Read %d files...",filenames.size());

   free(DirSpec);
}
