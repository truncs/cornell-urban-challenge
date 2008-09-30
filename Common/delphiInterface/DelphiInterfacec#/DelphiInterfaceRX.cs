using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;
using System.IO;
using System.Net;
using System.Net.Sockets;


namespace DelphiInterface
{
	public delegate void GotDelphiRadarPacketDel(object sender, DelphiRadarPacketEventArgs e);

	public class DelphiInterfaceRX
	{
		public event GotDelphiRadarPacketDel GotRadarPacket;					
		
		const byte FLS_STATUS = 0x01;
		const byte FLS_ECHO = 0x02;
		const byte FLS_TRACKA = 0x03;
		const byte FLS_TRACKB = 0x04;
		const byte FLS_TRACKC = 0x12;
		
		private IPAddress radarMultIP = IPAddress.Parse("239.132.1.12");	
		private int radarRXPort = 30012;

		public bool doRawLog = false;
		public string logfile = "C:\\RADAR.log";
		public BinaryWriter br;
					
		private byte[] radarBuf;
		private Socket radarSock;

		private void BuildSocket()
		{
			lock (this)
			{				
				if (this.radarSock == null)
				{
					if (this.radarBuf == null)
						this.radarBuf = new byte[65536];
					this.radarSock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
					this.radarSock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
					this.radarSock.Bind(new IPEndPoint(IPAddress.Any, this.radarRXPort));
					this.radarSock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(this.radarMultIP));
					this.radarSock.BeginReceive(this.radarBuf, 0, this.radarBuf.Length, SocketFlags.None, RadarReceiveCallback, null);
				}
			}
		}

		public DelphiInterfaceRX()
		{			
			BuildSocket();											
		}		

		private void RadarReceiveCallback(IAsyncResult ar)
		{
			try
			{
				int bytesReceived = this.radarSock.EndReceive(ar);
				if (bytesReceived > 0)
				{
					MemoryStream stream = new MemoryStream(this.radarBuf, 0, bytesReceived, false, true);
					Processstuff(stream.ToArray ());					
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Socket exception! " + ex.Message);
				return;
			}
			this.radarSock.BeginReceive(this.radarBuf, 0, this.radarBuf.Length, SocketFlags.None, RadarReceiveCallback, null);
		}

		void Logstuff (byte[] message)
		{
			if (br == null) br = new BinaryWriter (new FileStream (logfile,FileMode.Create));
			br.Write (message);
		}

		void Processstuff(byte[] message)
		{
			DelphiRadarData data;
			if (doRawLog) Logstuff (message);
			int id = 0;
			
				//format is TIMESTAMP(6), SEQ(4), ID (1), DATA (496)
				if (message.Length != 507)
				{
					Console.WriteLine("stuff! UDPRADAR Wrong Length!");
					return;
				}
				BinaryReader br = new BinaryReader(new MemoryStream(message));
				UInt16 secs = BigToLittle(br.ReadUInt16());
				UInt32 ticks = BigToLittle(br.ReadUInt32());
				UInt32 seq = BigToLittle(br.ReadUInt32());
				id = br.ReadByte();
				//Console.WriteLine("Got msg: id " + id.ToString());
				
				
				data.status.scanIndex = BigToLittle(br.ReadUInt16());																//2
				data.status.softwareVersion = BigToLittle(br.ReadUInt16()) + (br.ReadByte() << 16);	//3
				byte errs = br.ReadByte();																																				//1
				data.status.scanOperational = ((errs & 0x01) != 0);
				data.status.xvrOperational = ((errs & 0x02) != 0);
				data.status.errorCommunication = ((errs & 0x04) != 0);
				data.status.errorOverheat = ((errs & 0x08) != 0);
				data.status.errorVoltage = ((errs & 0x10) != 0);
				data.status.errorInternal = ((errs & 0x20) != 0);
				data.status.errorRangePerformance = ((errs & 0x40) != 0);
				data.status.isleftToRightScanMode = ((errs & 0x80) != 0);
				byte status7 = br.ReadByte();																																			//1
				data.status.isBlockageDetection = ((status7 & 0x01) != 0);
				data.status.isShortRangeMode = ((status7 & 0x02) != 0);
				data.status.FOVAdjustment = br.ReadSByte() * 0.1f;																	//1	
				//--------------------------------------------------------------------------------------------------------																																				
				data.echo.scanIndexLSB = br.ReadByte();																							//1
				data.echo.vehicleSpeed = BigToLittle(br.ReadUInt16()) * 0.0078125f;									//2
				data.echo.vehicleYawRate = BigToLittle(br.ReadUInt16()) * 0.0078125f;								//2
				data.echo.vehicleRadiusCurvature = BigToLittle(br.ReadInt16());											//2
				byte echo8 = br.ReadByte();																																				//1
				data.echo.radarLevel = (byte)(echo8 & 0x0F);
				data.echo.isInternalYawSensorMissing = ((echo8 & 0x10) != 0);
				data.echo.isRadarMergingTargets = ((echo8 & 0x20) != 0);
				//--------------------------------------------------------------------------------------------------------																																				

				data.tracks = new DelphiRadarTrack[20];

				for (int i = 0; i < 20; i++)
				{
					data.tracks[i].id = 0xFF;
				}

				for (int i = 0; i < 20; i++)
				{
					byte idA = br.ReadByte();																																							//1				
					if (idA < 1 || idA > 20)
					{
						Console.WriteLine("got bad stuff!");
						return;
					}

					if (data.tracks[idA - 1].id != 0xFF)
					{
						Console.WriteLine("you're stuff");
						return;						
					}

					data.tracks[idA - 1].ts = (float)((float)secs + (float)ticks / 10000.0f);
					data.tracks[idA - 1].id = idA;

					if (data.tracks[idA - 1].id != (i + 1))
					{
						Console.WriteLine("you're REALLY messING stuff");
						return;
					}

					data.tracks[idA - 1].range = BigToLittle(br.ReadUInt16()) * 0.0078125f;									//2
					data.tracks[idA - 1].rangeRate = BigToLittle(br.ReadInt16()) * 0.0078125f;							//2
					data.tracks[idA - 1].trackAngle = br.ReadSByte() * -0.1f * (Math.PI / 180.0f);					//1
					data.tracks[idA - 1].trackAngleUnfiltered = br.ReadSByte() * -0.1f * (Math.PI / 180.0f);//1
					byte scanLSBA = br.ReadByte();																																				//1
					//--------------------------------------------------------------------------------------------------------																																				
					byte idB = br.ReadByte();																																							//1
					if (idB < 1 || idB > 20)
					{
						Console.WriteLine("got bad stuff!");
						return;
					}
					data.tracks[idB - 1].rangeUnfiltered = BigToLittle(br.ReadUInt16()) * 0.0078125f;				//2
					data.tracks[idB - 1].power = (BigToLittle(br.ReadUInt16()) * 0.2f) - 60.0f;							//2
					byte trackb6 = br.ReadByte();																																					//1
					data.tracks[idB - 1].counter = (byte)(trackb6 & 0x0F);
					data.tracks[idB - 1].isBridge = ((trackb6 & 0x10) != 0);
					data.tracks[idB - 1].isSidelobe = ((trackb6 & 0x20) != 0);
					data.tracks[idB - 1].isForwardTruckReflector = ((trackb6 & 0x40) != 0);
					data.tracks[idB - 1].isMatureObject = ((trackb6 & 0x80) != 0);
					data.tracks[idB - 1].combinedObjectID = br.ReadByte();																	//1
					byte scanLSBB = br.ReadByte();																																				//1
					//--------------------------------------------------------------------------------------------------------																																				
				}
				for (int i = 0; i < 20; i++)
				{
					byte idC = br.ReadByte();
					if (idC < 1 || idC > 20)
					{
						Console.WriteLine("got bad stuff!");
						return;
					}
					data.tracks[idC - 1].rangeRateUnfiltered = BigToLittle(br.ReadInt16()) * 0.0078125f;
					data.tracks[idC - 1].edgeAngleLeftUnfiltered = br.ReadSByte() * -0.1f * (Math.PI / 180.0f);
					data.tracks[idC - 1].edgeAngleRightUnfiltered = br.ReadSByte() * -0.1f * (Math.PI / 180.0f);
					br.ReadByte(); //unused
					data.tracks[idC - 1].trackAngleUnfilteredNoOffset = br.ReadSByte() * -0.1f * (Math.PI / 180.0f);
					byte scanLSBC = br.ReadByte();
				}			
			if (GotRadarPacket != null)
			GotRadarPacket(this, new DelphiRadarPacketEventArgs(data,id));
		}


		#region bigtolittle
		public static UInt32 BigToLittle(UInt32 stuff)
		{
			Byte[] stuffness = BitConverter.GetBytes(stuff);
			Array.Reverse(stuffness);
			return BitConverter.ToUInt32(stuffness, 0);
		}
		public static UInt16 BigToLittle(UInt16 stuff)
		{
			Byte[] stuffness = BitConverter.GetBytes(stuff);
			Array.Reverse(stuffness);
			return BitConverter.ToUInt16(stuffness, 0);
		}
		public static Int16 BigToLittle(Int16 stuff)
		{
			Byte[] stuffness = BitConverter.GetBytes(stuff);
			Array.Reverse(stuffness);
			return BitConverter.ToInt16(stuffness, 0);
		}
		#endregion
	}
	public struct DelphiRadarData
	{
		public DelphiRadarStatus status;
		public DelphiRadarEcho echo;
		public DelphiRadarTrack[] tracks;
	}
	/// <summary>
	/// CAN FLS Message 602H ECHO
	/// </summary>
	public struct DelphiRadarEcho
	{
		public byte scanIndexLSB;
		public float vehicleSpeed;           //meters/sec
		public float vehicleYawRate;         //degrees/sec
		public short vehicleRadiusCurvature; //meters
		public byte radarLevel;
		public bool isInternalYawSensorMissing;
		public bool isRadarMergingTargets;
	}
	/// <summary>
	/// CAN FLS Message 601h STATUS
	/// </summary>
	public struct DelphiRadarStatus
	{
		public ushort scanIndex;
		public int softwareVersion;
		public bool scanOperational;
		public bool xvrOperational;
		public bool errorCommunication;
		public bool errorOverheat;
		public bool errorVoltage;
		public bool errorInternal;
		public bool errorRangePerformance;
		public bool isleftToRightScanMode;
		public bool isBlockageDetection;
		public bool isShortRangeMode;
		public float FOVAdjustment;
	}
	public struct DelphiRadarTrack
	{
		public byte id;                            //1 to 20 indicates the ID of the current track
		public float range;                        //meters
		public float rangeUnfiltered;              //meters

		public float rangeRate;                    //meters/sec
		public float rangeRateUnfiltered;          //meters/sec

		public double trackAngle;                   //degreees
		public double trackAngleUnfiltered;         //degreees
		public double trackAngleUnfilteredNoOffset; //degreees 

		public double edgeAngleLeftUnfiltered;      //degrees
		public double edgeAngleRightUnfiltered;     //degrees

		public float power;													//dbV
		public float counter;												//counts (scans)
		public byte combinedObjectID;								//it is unclear what this means, possibly a "candidate" to merge tracks with.

		public bool isBridge;
		public bool isSidelobe;
		public bool isForwardTruckReflector;
		public bool isMatureObject;
		public float ts;
	}

	public class DelphiRadarPacketEventArgs
	{
		public DelphiRadarData rd;
		public int id;
		public DelphiRadarPacketEventArgs(DelphiRadarData rd, int id)
		{
			this.rd = rd;
			this.id = id;
		}
	}
	public class DelphiRadarPoseCommand //600
	{
		public float vehicleSpeed;
		public float vehicleYawRate;
		public float vehicleRadiusCurvature;

		public byte[] toMsg()
		{
			//0, 2x message len, id, 2xmesagelen_int, message
			MemoryStream ms = new MemoryStream();
			ms.WriteByte(0x15);
			ms.Write(BitConverter.GetBytes(DelphiInterfaceRX.BigToLittle((ushort)(vehicleSpeed * 128.0))), 0, 2);
			ms.Write(BitConverter.GetBytes(DelphiInterfaceRX.BigToLittle((short)(vehicleYawRate * 128.0))), 0, 2);
			ms.Write(BitConverter.GetBytes(DelphiInterfaceRX.BigToLittle((short)(vehicleRadiusCurvature))), 0, 2);			
			return ms.ToArray();
		}
	}
}

