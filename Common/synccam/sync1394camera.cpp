#include "sync1394camera.h"
C1394Camera Sync1394Camera::camera;

DWORD WINAPI CamThreadWrap(LPVOID t)
{
	return ((Sync1394Camera*)t)->CamThread();
}

int bullstuff= 0;
DWORD Sync1394Camera::CamThread ()
{
	printf("Cam Thread Started\n");
	while(isRunning) 
	{	
		unsigned long dlength=0;		
		//float pgain = .01f;
		int dFrames=0;
		if (CAM_SUCCESS != camera.AcquireImageEx(TRUE,&dFrames))
		{
			printf("FATAL: COULD NOT AQUIRE AN IMAGE FROM THE CAMERA.\n");			
			this->buf = NULL;			
			while(1);
		}
		if (dFrames>0) printf ("DROPPED %d FRAMES!\n",dFrames);
		EnterCriticalSection(&camgrab_cs);		
		if (config.isColor)
		{
			camera.getRGB(buf,config.height * config.width * 3); dlength=1;
		}
		else
			buf = camera.GetRawData (&dlength);
		LeaveCriticalSection(&camgrab_cs);
		if (0 == dlength) 
		{			
			continue;
		}

		if ((config.AGC) && (config.isSlave == false))
		{
#if AUTOGAIN_USE_MEDIAN
		int median = GetMedianPixelValue(config.AGCtop,config.AGCbot);
		
		//too bright, positive error, 
		//so pos error means decrease gain
		float error = ((float)median - (float)idealMedian); 
		float effort = 1;
				
		effort = (abs(error * kp));
		if (error<0) effort*=-1;
		AGCerror = error;
		AGCeffort = effort;		

#ifdef USESHUTTERGAIN
		float seffort = .1f;
		int newShutter = (int) GetShutter ()  - (int)(error*seffort);
		SetShutter (newShutter);		
		
	
#endif

		int newgain = (int)GetGain() - (int)effort;
		SetGain (newgain);

	bullstuff++;
		if (bullstuff %100==0)
		{
			printf("AGC: Gain: %d Shutter: %d Error: %f\n",newgain,newShutter,error);
		}
#else
		int maxedOutPixels = GetNumMaxedPixelsInBuf(config.AGCtop,config.AGCbot);
		if (maxedOutPixels != -1)
		{
			//too many pixels, + error
			int error = (maxedOutPixels - AUTOGAIN_MAX_IDEAL); 
			if ((error>0) && (GetGain()>0))	 //too bright
				SetGain (GetGain() - 1);
			else if (error<0)
				SetGain (GetGain() + 1);
		}
#endif
		}
		
		if (config.syncEnabled) //are we using timing?
		{
			if ((expSeqNumber != 0) && (curSeqNumber != expSeqNumber))
			{
				//interpolate the correct timestamp
				float expInterval = 1.0f/(float)config.syncFPS;				
				//printf("WARNING: Sequence number mismatch: Got: %d Expected %d \nCurTime: %f Interval: %f ProjTime: %f \n",curSeqNumber, expSeqNumber,curtimestamp,expInterval,expInterval + curtimestamp);
				curtimestamp += expInterval;		
			}
			expSeqNumber = curSeqNumber + 1;
			
		}
		SetEvent (cameraEvent);		
	}
	printf("Cam Thread Ended\n");
	return 0;
}


///This is a little ghetto but will be useful. Call this function at the beginning of your app.
// It will block until the auto gain setlles to the target gain value by adjusting shutter. It can
// take several seconds so be careful how you use it. Its important the scene is relatively constant
// while this is executing
void Sync1394Camera::DoAutoShutter(unsigned short targetGain)
{
	
	printf("Attempting to adjust shutter....\n");
	bool gotOKFrame = false;
	while (gotOKFrame==false)
	{
		WaitForSingleObject (cameraEvent,5000);
		if ((buf) == NULL)				
			printf("waiting for first image...\n");
		else
			gotOKFrame = true;
	}
	unsigned short minShutter =0;
	unsigned short maxShutter =0;
	GetShutterMinMax (&minShutter,&maxShutter);
	printf("max shutter: %d min shutter: %d",(int)maxShutter,(int)minShutter);
	bool targetOK = false;	
	bool oldAGCVal = this->config.AGC;
	this->config.AGC = false;
	SetGain (targetGain);
	while (targetOK == false)	
	{
		int median = GetMedianPixelValue(config.AGCtop,config.AGCbot);		
		//too bright, positive error, 
		//so pos error means decrease gain
		float error = ((float)median - (float)idealMedian); 
		float effort = 1;
				
		effort = (abs(error * kp));
		if (error<0) effort*=-1;
		AGCerror = error;
		AGCeffort = effort;		

		int newShutter = (int) GetShutter ()  - (int) effort;
		if ((newShutter < 900) && (newShutter > 0))
			SetShutter (newShutter);
		else if (newShutter > MAX_FPS_LIMITED_SHUTTER)
		{	
			SetShutter (MAX_FPS_LIMITED_SHUTTER);
			targetOK = true;
			printf("WARNING: Max shutter!\n");
		}
		else 
		{
			SetShutter (1);
			targetOK = true;
			printf("WARNING: Min shutter!\n");
		}

		if (error < 5) targetOK = true;
	}
	this->config.AGC = oldAGCVal;
	printf("Settled on shutter of: %d\n",(int)GetShutter ());
}

unsigned short Sync1394Camera::GetShutter()
{

	C1394CameraControl* ctrl = camera.GetCameraControl (FEATURE_SHUTTER);
	unsigned short val = 0;
	ctrl->GetValue(&val,NULL);
	return (int)val;
}

void Sync1394Camera::GetShutterMinMax (unsigned short* min, unsigned short* max)
{
  if (config.isSlave) return;
	C1394CameraControl* ctrl = camera.GetCameraControl (FEATURE_SHUTTER);
	ctrl->GetRange(min,max);
}


int Sync1394Camera::SetShutter (int val)
{
  if (config.isSlave) return 0 ;
	if (val>maxShutter) val = maxShutter;
	if (val<minShutter) val = minShutter;
	C1394CameraControl* ctrl = camera.GetCameraControl (FEATURE_SHUTTER);
	if (ctrl->HasAutoMode() == true)
	{	
		printf("I HAVE AUTOSHUTTER ASSHOLE");
		return (-1);
	}
	else
		return(ctrl->SetValue(val));
}

void Sync1394Camera::GetGainMinMax (unsigned short* min, unsigned short* max)
{
	C1394CameraControl* ctrl = camera.GetCameraControl (FEATURE_GAIN);
	ctrl->GetRange(min,max);
}

unsigned short Sync1394Camera::GetGain()
{
	C1394CameraControl* ctrl = camera.GetCameraControl (FEATURE_GAIN);
	unsigned short val = 0;
	ctrl->GetValue(&val,NULL);
	return val;
}

int Sync1394Camera::SetGain (int val)
{
  if (config.isSlave) return 0;
	if (val>maxGain) val = maxGain;
	if (val<minGain) val = minGain;
	//printf("G : %d\n",val);
	C1394CameraControl* ctrl = camera.GetCameraControl (FEATURE_GAIN);
	if (ctrl->HasAutoMode() == true)
	{	
		printf("I HAVE AUTOGAIN ASSHOLE");
		return -1;
	}
	else
	{
		//printf("gain set to %d\n",val);
		return(ctrl->SetValue(val));
	}
}


Sync1394Camera::Sync1394Camera()
{
	curSeqNumber = 0;
	expSeqNumber = 0;
	lastMedian=0;
	lastMaxAcc=0;
	kp = AUTOGAIN_KP;
	idealMedian = AUTOGAIN_MEDIAN_IDEAL;
	curtimestamp  = 0;
	cameraEvent = CreateEvent ( NULL , false , false , NULL);	
}

int Sync1394Camera::GetNumberOfCameras ()
{
	if( camera.RefreshCameraList() == 0 ) 
	{ printf( "RefreshCameraList failed. Check that any cameras are connected.\n"); return false; }
	return camera.GetNumberCameras();
}

int Sync1394Camera::GetMedianPixelValue(int top, int bottom)
{
	int start = config.width * top;
	int end = config.width * bottom;
			
	//ugh sort the pixels, find the middle
	if (config.isColor)
	{
			int tmp [256]={0};
			for (int i=start; i<end; i++)
			{	
				tmp[(((unsigned char*)buf )[i*3])]++;			
				tmp[(((unsigned char*)buf )[i*3+1])]++;			
				tmp[(((unsigned char*)buf )[i*3+2])]++;			
			}
			int dastuffs = 0;
			int i=0;
			int mid = ((end-start)*3) /2;						
						
			while (dastuffs < mid)
				dastuffs += tmp[i++];			
			i--;				
			lastMedian = i;
			return i;
	}

	else
	{
		if (config.BitDepth16)
		{
			int tmp [1024]={0};
			for (int i=start; i<end; i++)
				tmp[(((unsigned short*)buf )[i])]++;			
			int dastuffs = 0;
			int i=0;
			int mid = (end-start) /2;						
			i=0;
			while (dastuffs < mid)
				dastuffs += tmp[i++];			
			i--;					
			lastMedian = i;
			return i;
		}
		else
		{
			int tmp [256]={0};
			for (int i=start; i<end; i++)
				tmp[(((unsigned char*)buf )[i])]++;			
			int dastuffs = 0;
			int i=0;
			int mid = (end-start) /2;						
			i=0;
			while (dastuffs < mid)
				dastuffs += tmp[i++];			
			i--;					
			lastMedian = i;
			return i;
		}
	}
}


int Sync1394Camera::shortComp (const void* a, const void* b)
{
	unsigned short stuffa = *(unsigned short*)a;
	unsigned short stuffb = *(unsigned short*)b;
  return (stuffa-stuffb);
}

int Sync1394Camera::charComp (const void* a, const void* b)
{
	unsigned char stuffa = *(unsigned char*)a;
	unsigned char stuffb = *(unsigned char*)b;
  return (stuffa-stuffb);
}

int Sync1394Camera::GetNumMaxedPixelsInBuf (int top, int bottom)
{
	int thisMax=0; 
	int acc = 0;
	int MAX = AUTOGAIN_MAX8BIT;
	int start = config.width * top;
	int end = config.width * bottom;
	if (config.BitDepth16) MAX = AUTOGAIN_MAX16BIT;
	if (config.isColor)
	{
		for (int i=start; i<end; i++)
		{
			if (((unsigned char*)buf )[i*3+0] >= MAX) acc++;			
			if (((unsigned char*)buf )[i*3+1] >= MAX) acc++;
			if (((unsigned char*)buf )[i*3+2] >= MAX) acc++;
		}
	}
	else
	{
		for (int i=start; i<end; i++)
		{
			if (config.BitDepth16)
			{
				if (((unsigned short*)buf )[i] >= MAX) acc++;
			}
			else
			{
				if (((unsigned char*)buf )[i] >= MAX)	acc++;
			}
		}
	}
	lastMaxAcc = acc;
	return acc;
}


Sync1394Camera::~Sync1394Camera()
{	
	isRunning = false;
	
	WaitForSingleObject (cameraHandle,INFINITE);
	printf("Terminating SyncCam\n");
	if (config.syncEnabled )
	{
    if ((config.syncKillOnClose) && (config.isSlave==false))
    {
      char killmsg[] = {0x05, config.syncID & 0xFF}; //stop message
		  udpTX->send_message (killmsg,2,UDP_CONTROL_IP,UDP_CONTROL_PORT);
    }
		delete udpRX;
		delete udpTX;
	}
	DeleteCriticalSection (&camgrab_cs);
}
bool Sync1394Camera::InitCamera(int cameraID, SyncCamParams m_config) 
{
	isRunning = true;
	Sync1394Camera::config = m_config;	
	int effWidth = config.partialWidth;
	int effHeight = config.partialHeight;

	if (config.isColor) 
		size = effWidth * effHeight * 3;
	else	
		size = effWidth * effHeight;
	if (config.BitDepth16) size *= 2;
	buf = new unsigned char[size];

	if( camera.RefreshCameraList() == 0 ) 																		{ printf("RefreshCameraList failed.\n"); return false; } 
	if(camera.SelectCamera(cameraID)!=CAM_SUCCESS)														{ printf("Could not select camera\n" ); return false; }
	if(camera.InitCamera()!=CAM_SUCCESS)																			{	printf("Could not init camera\n" ); return false; }	
	if(camera.SetVideoFormat(config.videoFormat)!=CAM_SUCCESS)								{ printf("Could not SetVideoFormat on camera\n" ); }
	if(camera.SetVideoMode(config.videoMode)!=CAM_SUCCESS)										{ printf("Could not SetVideoMode on camera\n"); }
  	
	if (config.usePartialScan == false)
	{
		if(camera.SetVideoFrameRate (config.videoFrameRate)!=CAM_SUCCESS)														{ printf("Could not set video frame rate!");	return false;} //30 fps
	}

	if (config.isSlave)																												{ printf("Using SLAVE configuration.\n");}
  
	if (config.usePartialScan)
	{
		unsigned short w, h; 
		C1394CameraControlSize* p_size_control = camera.GetCameraControlSize();
		p_size_control->GetSizeLimits(&w, &h);
		if ((w>effWidth) || (h>effHeight))																			{ printf("FATAL: Bad Partial Scan Size Specified! Specified w:%d h:%d, Max: w:%d h:%d",effWidth,effHeight,w,h);	return false;} 
		if (config.isColor)
		{
			if( p_size_control->SetColorCode(COLOR_CODE_YUV422) != CAM_SUCCESS ) 	{ printf("SetColorCode failed.\n"); return false; } 
		}
		else
		{
			if( p_size_control->SetColorCode(config.BitDepth16?COLOR_CODE_Y16:COLOR_CODE_Y8) != CAM_SUCCESS ) { printf("SetColorCode failed (BW).\n"); return false; } 
		}

		if( p_size_control->SetSize(effWidth,effHeight) != CAM_SUCCESS )												{ printf("SetSize failed.\n"); return false; } 
		if( p_size_control->SetPos(config.partialLeft,config.partialTop) != CAM_SUCCESS )												{ printf("SetPos failed.\n"); return false; } 
		if (p_size_control->SetBytesPerPacket (config.bytesPerPacket) != CAM_SUCCESS)	{ printf("SetBytesPerPacket failed.\n"); return false; } 
		
		float interval=0.0; p_size_control->GetFrameInterval (&interval);
		if (((float)config.syncFPS) > (1.0f/interval))													{ printf("WARNING: SyncFPS (%f) is greater than the actual camera FPS (%f)\n",(float)config.syncFPS,(1.0f/interval));}
    printf("Frame Interval is: %f",interval);
	}
	minGain=0;
	maxGain=0;
	GetGainMinMax(&minGain, &maxGain);
	printf("Min gain: %d, Max gain: %d\n",minGain,maxGain);

	minShutter=0;
	maxShutter=0;
	GetShutterMinMax(&minShutter, &maxShutter);
	printf("Min shutter: %d, Max shutter: %d\n",minShutter,maxShutter);

	printf("Completed Init of Camera\n");

	if (config.syncEnabled)
	{
		udp_params paramsRX  = udp_params(); 
   	paramsRX.remote_ip = inet_addr(UDP_BROADCAST_IP);
		paramsRX.local_port = UDP_BROADCAST_PORT;
		paramsRX.reuse_addr = 1;
		try
		{		
			udpRX = new udp_connection(paramsRX);  
		}
		catch (exception)
		{
      printf("Couldn't init UDP RX on  %s:%d  \n",UDP_BROADCAST_IP,paramsRX.local_port);
			return false;
		}

		udpRX->set_callback(MakeDelegate(this,&Sync1394Camera::UDPCallback), udpRX);
		
		udp_params paramsTX  =  udp_params(); 
		
		try
		{		
			udpTX = new udp_connection(paramsTX);  
		}
			catch (exception)
		{
			printf("Couldn't init UDP TX on port %d\n",paramsTX.local_port);
			return false;
		}
		
    if (config.isSlave == false)
    {
		  char regmsg[] = {CAMERA_MSG_REGISTER, config.syncID & 0xFF, 0x00, 0x00, 0x00, 0x00,(paramsRX.local_port>>8)&0xff, paramsRX.local_port&0xff}; //register message
		  udpTX->send_message (regmsg,8,UDP_CONTROL_IP,UDP_CONTROL_PORT);
		  Sleep(100);
		  char fpsmsg[] = {CAMERA_MSG_SETFPS, config.syncID & 0xff, config.syncFPS &0xff};
		  udpTX->send_message (fpsmsg,3,UDP_CONTROL_IP,UDP_CONTROL_PORT);
		  Sleep(100);
		  char initmsg[] = {CAMERA_START, config.syncID & 0xFF}; //start message
		  udpTX->send_message (initmsg,2,UDP_CONTROL_IP,UDP_CONTROL_PORT);
			if (config.ghettoSync == false)
			{
				C1394CameraControlTrigger* trig = camera.GetCameraControlTrigger ();
					
				unsigned short modein=0; unsigned short modeparam =0; unsigned short trigsrc=0;
				if (trig->SetTriggerSource (config.syncCamInput)!=CAM_SUCCESS)
					printf("Could Not Set Trigger Source!\n");
				if (trig->SetMode (0))
					printf("Could Not Set Trigger Mode!\n");
				if (trig->SetOnOff (true))
					printf("Could Not Set Trigger ON!\n");
			}
    }
	  printf("Timing Initialized.\n");	
	}
  
	else
		printf("Timing has been disabled.\n");

  unsigned long ulFlags = 0;  
  ulFlags |= ACQ_START_VIDEO_STREAM;
  
  if(config.isSlave)
  {
    ulFlags |= ACQ_SUBSCRIBE_ONLY;
    printf("Using slave configuration... make sure to start a master or no images will come in.\n");
    if (camera.StartImageAcquisitionEx(6,1000,ulFlags))
    {
      printf("WARNING: Could not start image aquisition! Bailing....\n");
      return false;
    }
  }
  else
    camera.StartImageAcquisitionEx (6,1000,ACQ_START_VIDEO_STREAM);
  
	InitializeCriticalSection(&camgrab_cs);

	cameraHandle = CreateThread(NULL, 0, CamThreadWrap, this, 0, NULL);
	//Sleep(2000);//starting...
	//SetThreadPriority(cameraHandle, THREAD_PRIORITY_HIGHEST);
	return true;
}


void Sync1394Camera::UDPCallback(udp_message& msg, udp_connection* conn, void* arg)
{ 	
	if (msg.len != sizeof (SyncCamPacket))
	{
		printf("Warning: bad packet size. Packet is: %d bytes Expected %d bytes\n", msg.len,  sizeof (SyncCamPacket));
		return;
	}
	SyncCamPacket packet = *((SyncCamPacket*)(msg.data)); //very high sketchfactor
  if (config.syncID != packet.id) return;	
	packet.seconds = ntohs(packet.seconds);
	packet.seqNum = ntohl(packet.seqNum);
	packet.ticks = ntohl(packet.ticks);
  
	curtimestamp = (double)packet.seconds + (double)packet.ticks/10000.0;
	if (packet.seqNum > (unsigned int)curSeqNumber)
	curSeqNumber = packet.seqNum;
	
	//printf ("sec: %d ticks: %d sync: %d\n",packet.seconds,packet.ticks,packet.seqNum);
}

