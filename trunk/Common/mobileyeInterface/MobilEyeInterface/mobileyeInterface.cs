using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace MobilEyeInterface
{

		public delegate void GotMobilEyeObstaclePacketDel(object sender, MobilEyeObstaclePacketEventArgs e);
		public enum MobilEyeID
		{
			MOBILEYE_FL=0,
			MOBILEYE_FR=1,
			MOBILEYE_CTR=2,
			MOBILEYE_REAR=3
		}

		enum MobilEyeMessageID : int
		{
			ME_Info = 0,
			ME_RoadEnv = 1,
			ME_Obs = 3,
			ME_BAD = 99
		}
		public enum VehiclePath : int
		{
			VP_NoSide, VP_Center, VP_Left, VP_Right
		}

		public struct MobilEyeWorldObstacle
		{
			public int obstacleID;
			public float obstacleDistZ;
			public int confidence;
			public VehiclePath path; //0 no side, 1 center, 2 left, 3 right
			public bool currentInPathVehicle;
			public bool obstacleDistXDirection; // 0 left, 1 right		
			public float obstacleDistX; //10cm units
			public float obstacleWidth;
			public float scaleChange; // (.5 to -.5)
			public float velocity;
			public int bottomRect;	//pixels?
			public int leftRect;
			public int topRect;
			public int rightRect;
		}
		public struct MobilEyeObstaclePacket
		{
			public MobilEyeID id;
			public double carTime;
			public int numObstacles;
			public MobilEyeWorldObstacle[] obstacles;
		}
		public class MobilEyeObstaclePacketEventArgs
		{
			public MobilEyeObstaclePacket obs;
			public MobilEyeID id;
			public MobilEyeObstaclePacketEventArgs(MobilEyeObstaclePacket obs, MobilEyeID id)
			{
				this.obs = obs;
				this.id = id;
			}
		}

		public class MobilEyeInterfaceRX
		{
			public event GotMobilEyeObstaclePacketDel GotMobilEyeObstaclePacket;


			private IPAddress mobilEyeIP = IPAddress.Parse("239.132.1.36");
			private int mobilEyePort = 30036;

			private byte[] buf;
			private Socket sock;

			private void BuildSocket()
			{
				lock (this)
				{
					if (this.sock == null)
					{
						if (this.buf == null)
							this.buf = new byte[65536];
						this.sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
						this.sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
						this.sock.Bind(new IPEndPoint(IPAddress.Any, this.mobilEyePort));
						this.sock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(this.mobilEyeIP));
						this.sock.BeginReceive(this.buf, 0, this.buf.Length, SocketFlags.None, ReceiveCallback, null);
					}
				}
			}

			public MobilEyeInterfaceRX()
			{
				BuildSocket();
			}

			private void ReceiveCallback(IAsyncResult ar)
			{
				try
				{
					int bytesReceived = this.sock.EndReceive(ar);
					if (bytesReceived > 0)
					{
						MemoryStream stream = new MemoryStream(this.buf, 0, bytesReceived, false, true);
						Processstuff(stream.ToArray());
					}
				}
				catch (SocketException ex)
				{
					Console.WriteLine("Socket exception! " + ex.Message);
					return;
				}
				this.sock.BeginReceive(this.buf, 0, this.buf.Length, SocketFlags.None, ReceiveCallback, null);
			}

	
			void Processstuff(byte[] message)
			{
			
				MemoryStream stream = new MemoryStream (message);
				BinaryReader br = new BinaryReader (stream);
				MobilEyeID camID = (MobilEyeID)br.ReadInt32();
				MobilEyeMessageID msgID = (MobilEyeMessageID)br.ReadInt32();
				br.ReadInt32();
				switch (msgID)
				{
					case MobilEyeMessageID.ME_Obs:
						MobilEyeObstaclePacket obs = new MobilEyeObstaclePacket();
						obs.id = camID;
						obs.carTime = br.ReadDouble();
						obs.numObstacles = br.ReadInt32();
						obs.obstacles = new MobilEyeWorldObstacle[obs.numObstacles];
						for (int i = 0; i < obs.numObstacles; i++)
						{
							obs.obstacles[i].obstacleID = br.ReadInt32();
							obs.obstacles[i].obstacleDistZ = br.ReadSingle (); 
							obs.obstacles[i].confidence = br.ReadInt32 ();
							obs.obstacles[i].path =(VehiclePath) br.ReadInt32();
							obs.obstacles[i].currentInPathVehicle=br.ReadBoolean ();
							obs.obstacles[i].obstacleDistXDirection = br.ReadBoolean();

							obs.obstacles[i].obstacleDistX = br.ReadSingle ();
							if (!(obs.obstacles[i].obstacleDistXDirection))
								obs.obstacles[i].obstacleDistX *= -1;
							else
							{
								Console.Write(".");
							}
							obs.obstacles[i].obstacleWidth = br.ReadSingle ();
							obs.obstacles[i].scaleChange  = br.ReadSingle ();
							obs.obstacles[i].velocity =br.ReadSingle ();
							obs.obstacles[i].bottomRect = br.ReadInt32 ();
							obs.obstacles[i].leftRect = br.ReadInt32 ();
							obs.obstacles[i].topRect = br.ReadInt32 ();
							obs.obstacles[i].rightRect = br.ReadInt32();
						}
					if (GotMobilEyeObstaclePacket != null) GotMobilEyeObstaclePacket(this, new MobilEyeObstaclePacketEventArgs(obs, camID));
					break;

				}


			
			}
		}
	}



