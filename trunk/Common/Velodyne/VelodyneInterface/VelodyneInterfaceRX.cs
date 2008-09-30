using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;

namespace VelodyneInterface
{

		public delegate void GotVelodynePacketDel(object sender, VelodyneEventArgs e);

		public enum BlockHeaderID : int
		{
			TOP = 0xEEFF,
			BOTTOM = 0xDDFF
		};

		public struct VelodyneBlock
		{
			/// <summary>
			/// 100 bytes
			/// </summary>
			public BlockHeaderID block;
			public double rotation;
			public List<double> ranges;
			public List<int> intensity;			
		}

		public struct VelodynePacket
		{
			//should be 12 of these
			public List<VelodyneBlock> blocks;
			//then 6 bytes that either the thermistor or version
			public string info;
			public int spincount;
		}
		
		public class VelodyneEventArgs : EventArgs
		{
			public VelodyneXYZScan scan;
			public VelodyneEventArgs(VelodyneXYZScan scan)
			{
				this.scan = scan;
			}
		}

		public struct VelodyneXYZPoint
		{
			public double x; public double y; public double z;
      public double intensity;
      public int laserNum;
			public VelodyneXYZPoint(int laserNumber, BlockHeaderID block, double range, double sensorTheta, int intensity)
			{
				RCFVCFHCF angles;
				if (block == BlockHeaderID.TOP)
					angles = VelodyneAngles.topblockDegrees[laserNumber];
				else
					angles = VelodyneAngles.bottomblockDegrees[laserNumber];
				double xv = range * angles.RCFCOS * angles.VCFCOS;
				double yv = range * angles.RCFSIN * angles.VCFCOS;
				//yv += angles.HCF;
				double zv = range * angles.VCFSIN;

				//now do sensor rotation

				sensorTheta *= -(Math.PI / 180.0);
				x = (xv * Math.Cos(sensorTheta)) - (yv * Math.Sin(sensorTheta));
				y = (xv * Math.Sin(sensorTheta)) + (yv * Math.Cos(sensorTheta));
				z = zv;

        this.intensity = (double)intensity/255.0;
        this.laserNum = laserNumber + ((block==BlockHeaderID.TOP)?32:0);
			}
		}
		
		public class	VelodyneXYZScan
		{
			public List<VelodyneXYZPoint> points;
		}

		public class VelodyneInterfaceRX
		{

			public event GotVelodynePacketDel GotPacket;

			private IPAddress multIP = IPAddress.Parse("192.168.3.255");
			private int multPort = 2368;
			VelodyneXYZScan scan = new VelodyneXYZScan();
			int scancount = 0;
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
						this.sock.Bind(new IPEndPoint(IPAddress.Any, this.multPort));
						//this.sock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(this.multIP));
						this.sock.BeginReceive(this.buf, 0, this.buf.Length, SocketFlags.None, ReceiveCallback, null);
					}
				}
			}

			public VelodyneInterfaceRX()
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
				catch (Exception ex)
				{
					Console.WriteLine("Socket exception! " + ex.Message);
					return;
				}
				this.sock.BeginReceive(this.buf, 0, this.buf.Length, SocketFlags.None, ReceiveCallback, null);
			}
			
			double lastMaxR=0;
			int lastSpinCount = 0;
			void Processstuff(byte[] message)
			{
			
				BinaryReader br = new BinaryReader(new MemoryStream(message));
				double maxr = 0;
				VelodynePacket vpacket = new VelodynePacket();
				vpacket.blocks = new List<VelodyneBlock> (12);
				//first read 12 100 byte blocks
				for (int i = 0; i < 12; i++)
				{
					VelodyneBlock block = new VelodyneBlock();
					block.block = (BlockHeaderID)br.ReadUInt16();
					block.rotation = (((double)(br.ReadUInt16())) / 100.0);
					if (block.rotation > maxr) maxr = block.rotation;
					block.ranges = new List<double>(32);
					block.intensity = new List<int>(32);
					//now read 32 lasers
					for (int j = 0; j < 32; j++)
					{
						double range = ((double)(br.ReadUInt16())) * .002;						
						int intensity = (int)br.ReadByte();						
						block.ranges.Add(range);
						block.intensity.Add(intensity);
					}
					vpacket.blocks.Add(block);					
				}
				//now read the status
				vpacket.spincount = (br.ReadUInt16());
				int val1 = br.ReadByte();
				int val2 = br.ReadByte();
				char[] status = br.ReadChars(4);

				foreach (char c in status)
					vpacket.info += c + "";
				//Console.WriteLine("Got Scan! Length: " + message.Length + " Status: " + val1.ToString() + val2.ToString() + vpacket.info);

				if (br.BaseStream.Position != br.BaseStream.Length)
				{
					Console.WriteLine ("Did not reach end of velodyne packet: Pos: " + br.BaseStream.Position + "Len: " + br.BaseStream.Length);
				}



				//now make it xyz
				if (scan.points == null)
				scan.points = new List<VelodyneXYZPoint>();
				for (int i=0; i<vpacket.blocks.Count; i++) //each block
				{
					for (int j=0; j<vpacket.blocks[i].ranges.Count; j++) //each laser
					{
						double range = vpacket.blocks[i].ranges[j];
						//j is laser number
						if (range < 1.0) continue;
						VelodyneXYZPoint pt = new VelodyneXYZPoint (j,vpacket.blocks[i].block,range,vpacket.blocks[i].rotation,vpacket.blocks[i].intensity[j]);
						scan.points.Add (pt);
					}
				}
				scancount++;
				Console.WriteLine("spincount: " + vpacket.spincount);
				if (scancount >= 400)
				//if (lastMaxR>maxr) // we've cycled
				//if (vpacket.spincount != lastSpinCount)
				{                   					
					if (GotPacket != null)
						GotPacket(this, new VelodyneEventArgs(scan));
					scan = new VelodyneXYZScan();
					scancount = 0;
				}            
				lastMaxR = maxr;
				lastSpinCount = vpacket.spincount;
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
	}

