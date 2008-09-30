#include "delphiInterface.h"

//RECIEVER-----------------------------------------------------------------------------------------------------------
void DelphiInterfaceReceiver::SetDelphiCallback(Delphi_Msg_handler handler, void* arg)
{
	delphi_cbk = handler;
	delphi_cbk_arg = arg;
}

DelphiInterfaceReceiver::DelphiInterfaceReceiver() 
{
	packetNum=0;
	udp_params params = udp_params();
	params.remote_ip = inet_addr(UDP_DELPHI_ADDR);
	params.local_port = UDP_DELPHI_PORT;
	params.reuse_addr = 1;
	conn = new udp_connection (params);
	conn->set_callback (MakeDelegate(this,&DelphiInterfaceReceiver::UDPCallback),conn);
	printf("Delphi RX Interface Initialized. %s:%d\r\n",UDP_DELPHI_ADDR,UDP_DELPHI_PORT);
}

DelphiInterfaceReceiver::~DelphiInterfaceReceiver ()
{
	delete conn;
	printf("Delphi Interface Shutdown...\r\n");
}


void DelphiInterfaceReceiver::UDPCallback(udp_message& msg, udp_connection* conn, void* arg)
{ 	
		DelphiRadarScan scan;
			
		//format is TIMESTAMP(6), SEQ(4), ID (1), DATA (496)
		if (msg.len != sizeof (rawRadarPacket))
		{
			printf("ERROR: Delphi Wrong Packet Length! (%d)\n",msg.len);
			return;
		}
		
		//now the sketchballs part. we have raw structs, we'll cast the message to that, and then try this out.
		rawRadarPacket rawPacket = *((rawRadarPacket*)msg.data);
		
		
		//keep in mind this whole packet is big endian, so we'll need to convert everything
		unsigned short seconds = ntohs(rawPacket.timestamp.seconds);
		unsigned int ticks = ntohl(rawPacket.timestamp.ticks);
		unsigned int seq = ntohl(rawPacket.timestamp.seq);
		scan.timestamp = (double)seconds + (double)ticks / 10000.0;
		scan.sequence = seq;
		scan.scannerID = (DELPHIID)rawPacket.id;
		
		scan.status.scanIndex = ntohs(rawPacket.status.scanIndex);
		scan.status.softwareVersion = ntohs (rawPacket.status.swVer1 + (rawPacket.status.swVer2 << 16));
		scan.status.scanOperational				= ((rawPacket.status.flags1 & 0x01)!=0);
		scan.status.xvrOperational				= ((rawPacket.status.flags1 & 0x02)!=0);
		scan.status.errorCommunication		= ((rawPacket.status.flags1 & 0x04)!=0);
		scan.status.errorOverheat					= ((rawPacket.status.flags1 & 0x08)!=0);
		scan.status.errorVoltage					= ((rawPacket.status.flags1 & 0x10)!=0);
		scan.status.errorInternal					= ((rawPacket.status.flags1 & 0x20)!=0);
		scan.status.errorRangePerformance	= ((rawPacket.status.flags1 & 0x40)!=0);
		scan.status.isleftToRightScanMode	= ((rawPacket.status.flags1 & 0x80)!=0);

		scan.status.isBlockageDetection		= ((rawPacket.status.flags2 & 0x01)!=0);;
		scan.status.isShortRangeMode			= ((rawPacket.status.flags2 & 0x02)!=0);;
		scan.status.FOVAdjustment					= rawPacket.status.fovAdjustment;

		////--------------------------------------------------------------------------------------------------------																																				
		
		scan.echo.scanIndexLSB = rawPacket.echo.scanIndex;
		scan.echo.vehicleSpeed = ntohs(rawPacket.echo.vehicleSpeed) * 0.0078125f;
		scan.echo.vehicleYawRate							=	ntohs(rawPacket.echo.vehicleYawRate) * 0.0078125f;
		scan.echo.vehicleRadiusCurvature			= ntohs(rawPacket.echo.vehicleRadCurv);
		scan.echo.radarLevel									= rawPacket.echo.flags & 0x0f;
		scan.echo.isInternalYawSensorMissing	= ((rawPacket.echo.flags & 0x10)!=0);
		scan.echo.isRadarMergingTargets				= ((rawPacket.echo.flags & 0x20)!=0);

		////--------------------------------------------------------------------------------------------------------																																				
		for (int i = 0; i < DELPHI_NUM_TRACKS; i++)
		{			
			if (!(rawPacket.tracksAB[i].idA == rawPacket.tracksAB[i].idB) && (rawPacket.tracksAB[i].idB == rawPacket.tracksC[i].idC) && (rawPacket.tracksC[i].idC == (i+1)))
			{
				printf("WARNING: FATAL: ID Mismatch in radar message. dropping packet. A:%d,B:%d,C:%d,id:%d\n",
					rawPacket.tracksAB[i].idA,
					rawPacket.tracksAB[i].idB,
					rawPacket.tracksC[i].idC,
					(i+1)); 
			}
			

			scan.tracks[i].id = rawPacket.tracksAB[i].idA;
			scan.tracks[i].range									= (float)(ntohs(rawPacket.tracksAB[i].range)) * 0.0078125f;
			scan.tracks[i].rangeRate							= ((float)((short)ntohs(rawPacket.tracksAB[i].rangeRate))) * 0.0078125f;
			scan.tracks[i].trackAngle							= (float)(rawPacket.tracksAB[i].angle)  * -0.1f * (M_PI / 180.0f);
			scan.tracks[i].trackAngleUnfiltered		= (float)(rawPacket.tracksAB[i].angleUnfiltered)  * -0.1f * (M_PI / 180.0f);
			////--------------------------------------------------------------------------------------------------------																																				
			scan.tracks[i].rangeUnfiltered					=	(float)(ntohs(rawPacket.tracksAB[i].rangeUnfiltered))* 0.0078125f;
			scan.tracks[i].power										= (((float)(ntohs(rawPacket.tracksAB[i].power))) * 0.2f) - 60.0f;
			scan.tracks[i].counter									= rawPacket.tracksAB[i].flags & 0x0F;
			scan.tracks[i].isBridge									= ((rawPacket.tracksAB[i].flags & 0x10)!=0);
			scan.tracks[i].isSidelobe								= ((rawPacket.tracksAB[i].flags & 0x20)!=0);
			scan.tracks[i].isForwardTruckReflector	= ((rawPacket.tracksAB[i].flags & 0x40)!=0);
			scan.tracks[i].isMatureObject						= ((rawPacket.tracksAB[i].flags & 0x80)!=0);
			scan.tracks[i].combinedObjectID					= rawPacket.tracksAB[i].combinedObjectID;				
			////--------------------------------------------------------------------------------------------------------																																							
			scan.tracks[i].rangeRateUnfiltered					= ((float)((short)ntohs(rawPacket.tracksC[i].rangeRateUnfiltered))) * 0.0078125f;
			scan.tracks[i].edgeAngleLeftUnfiltered			= (float)(rawPacket.tracksC[i].edgeAngleLeftUnfiltered) * -0.1f * (M_PI / 180.0f);
			scan.tracks[i].edgeAngleRightUnfiltered			= (float)(rawPacket.tracksC[i].edgeAngleRightUnfiltered) * -0.1f * (M_PI / 180.0f);
			scan.tracks[i].trackAngleUnfilteredNoOffset = (float)(rawPacket.tracksC[i].trackAngleUnfilteredNoOffset) * -0.1f * (M_PI / 180.0f);		


			
			scan.tracks[i].isValid								= ((scan.tracks[i].range >= 0) && (scan.tracks[i].range <= 255.0f));			

		}
		if (delphi_cbk.empty() == false)
		{
			delphi_cbk(scan,this,scan.scannerID,delphi_cbk_arg);
		}
		packetNum++;

}
