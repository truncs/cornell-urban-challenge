#define WIN32_LEAN_AND_MEAN 
#include <windows.h>
#include <iostream>
#include <string>

using namespace std;

HANDLE wdEvent0;
HANDLE wdEvent1;

void main()
{
	char wdname0[] = "watchdog0";
	char wdname1[] = "watchdog1";
		string f;
		cin>>f;
	wdEvent0 = CreateEvent (NULL , false , false , wdname0);		
	wdEvent1 = CreateEvent (NULL , false , false , wdname1);		
	while(1)

	{
	
		printf("Set \n");
		SetEvent (wdEvent0);
		SetEvent (wdEvent1);
		Sleep(500);
		fflush (stdout);
	}
}