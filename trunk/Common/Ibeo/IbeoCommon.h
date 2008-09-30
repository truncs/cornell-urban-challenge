#pragma once

#ifndef F_PI
#define F_PI 3.14159265358979323846f
#endif

//#define IBEO_DEFAULT_PORT 30060
//DEBUG
#define IBEO_DEFAULT_PORT 30060
#define IBEO_DEFAULT_MC_IP "239.132.1.60"

#define NUM_CHANNELS 4
#define NUM_SUBCHANNELS 4

// internal structures for packet encoding/decoding

// a packet constists of a IbeoScanDataPacketHdr immediately followed by a number of IbeoScanDataPacketPulse.

#define IBEO_SCAN_DATA_PACKET_TYPE 0x17

#pragma pack(push,1)
struct IbeoScanDataPacketHdr{
  unsigned char type;         // for now should always be IBEO_SCAN_DATA_PACKET_TYPE
  unsigned short packetNum;   // packet counter
  
  unsigned short tsSeconds;   // vehicle timestamp for this scan
  unsigned long tsTicks;      
  
  unsigned int scannerID;     // ARCNet ID of the source scanner
  unsigned short numPts;      // number of points in thie scan
};

// measurements that were reported as invalid by the Ibeo are set to the following value
#define IBEO_INVALID_MEAS 0xFFFF

#define IBEO_PT_STATUS_OK		        0x00 // Specified that the scan point is to be used (or not yet processed).
#define IBEO_PT_STATUS_INVALID	    0x01 // Specifies that the scan point is invalid (will be removed by the scan data processing.
#define IBEO_PT_STATUS_RAIN			    0x02 // Specifies that the scan point was detected to be rain.
#define IBEO_PT_STATUS_GROUND		    0x03 // Specifies that the scan point was detected to be ground.
#define IBEO_PT_STATUS_DIRT			    0x04 // Specifies that the scan point was detected to be dirt on the mounting surface.
#define IBEO_PT_STATUS_OFF_ROI		  0x05 // Specifies that the scan point was detected to be not in the region of interest.
#define IBEO_PT_STATUS_LANDMARK	    0x06 //This scan point is a valid scan point detected as a land mark for special applications.
#define IBEO_PT_STATUS_RIGHTMIRROR	0x07 //This scan point was received over the right mirror. Only used if the sensor is equipped with mirrors for LDW.
#define IBEO_PT_STATUS_LEFTMIRROR	  0x08 //This scan point was received over the left mirror. Only used if the sensor is equipped with mirrors for LDW.
#define IBEO_PT_STATUS_OBST_FILTERED_OUT 0x09
#define IBEO_PT_STATUS_SHORT_FACE 0x0A
#define IBEO_PT_STATUS_OBST_CLOSE 0x0B
#define IBEO_PT_STATUS_LONG_FACE 0x0C

namespace IbeoPtType{
  const unsigned char OBSTACLE = 0;
  const unsigned char INVALID = 1;
  const unsigned char RAIN = 2;
  const unsigned char GROUND = 3;
  const unsigned char DIRT = 4;
};

#define IBEO_FINAL_HIT 0x80


struct IbeoScanDataPacketPoint{
  short X,Y,Z;      // in cm
  unsigned char chanInfo;    // high nibble is chan number, low nibble is subchannel
  unsigned char status;      // as defined above
};

typedef unsigned char BYTE;

#include <vector>

struct IbeoPoint{
  float x,y,z;    // in meters
  unsigned char chan, subchan;
  bool finalHit;
  BYTE status;
};

struct IbeoScanData{
  double vehicleTS;       // in seconds
  unsigned int scannerID;         // should always be [0-255]       
  std::vector<IbeoPoint> pts;
  unsigned int packetNum;
};


#pragma pack(pop)
