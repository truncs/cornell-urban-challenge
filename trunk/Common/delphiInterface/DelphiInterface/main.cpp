
#include <iostream>

#include <string>
#include "delphiInterface.h"

using namespace std;

DelphiInterfaceReceiver* delphi;

LARGE_INTEGER freq, t1, t2;
		
void DelphiCallback(DelphiRadarScan scan, DelphiInterfaceReceiver* radar, int id , void* args)
{
	for (int i=0; i<20; i++)
	{
		if (scan.tracks[i].isValid == true)
		{
			float rr = scan.tracks[i].rangeRate;
			float rru = scan.tracks[i].rangeRateUnfiltered;
			cout<<"Speed! "<<rr<< " un: "<<rru<<endl; 
		}
	}
	/*
	cout<<"Got Scan! "<<id<<endl;
	QueryPerformanceCounter (&t2);
	cout<<"Took: " << (double)(t2.QuadPart - t1.QuadPart)/(double)freq.QuadPart <<endl;
	t1= t2;
	//scan.
	*/
}

void main()
{
	QueryPerformanceFrequency (&freq);
	delphi = new DelphiInterfaceReceiver ();
	delphi->SetDelphiCallback (&DelphiCallback, NULL);	
	while(1)
	{
		Sleep(10);
	}
}