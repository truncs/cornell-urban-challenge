using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace IbeoInterface
{
	public enum PointType 
	{
		Obstacle=0,
    Invalid=1, 
    Rain=2,
    Ground=3,
    Dirt=4,
    Redundant=51
	}
	
	public struct IbeoScan
	{
		public int packetNum;   // packet counter	  
		public float timestamp;
		public int scannerID;     // ARCNet ID of the source scanner
		public List<IbeoPoint> points;
	}
	
	public struct IbeoPoint
	{
		public Coordinate3 location;
		public int ray;	 //ray  0 bottom
		public int echo; //echo 0 first
		public PointType type;
    public bool finalHit;
	}

	public struct Coordinate3
	{
		public float x;public float y;public float z;
	}

	public class IbeoRXEventArgs : EventArgs 
	{
		private IbeoScan s;
		
		public IbeoRXEventArgs(IbeoScan s)
		{
			this.s = s;
		}

		public IbeoScan CurrentScan {
			get { return s; }
		}
	}


	public class IbeoInterfaceReciever
	{
	private const int IBEO_SCAN_DATA_PACKET_TYPE = 0x17	;
		
		private IPAddress groupAddr;
		private ushort groupPort;
		private Socket s;
		private byte[] buf = new byte[65000];
		private Thread rxThread;
		public event EventHandler<IbeoRXEventArgs> ScanReceived;
    volatile bool running = true;

		public IbeoInterfaceReciever(IPAddress groupAddr, ushort groupPort)
		{
			this.groupAddr = groupAddr;
			this.groupPort = groupPort;
			rxThread = new Thread(new ThreadStart(this.rxThreadFunction));
			rxThread.IsBackground = true;
		}

		private void rxThreadFunction()
		{
			while (running)
			{
				int bytesRead = 0;
				try
				{
				 bytesRead = s.Receive(buf, 0, buf.Length, SocketFlags.None);
				}
				catch (SocketException sex)
				{
					Console.WriteLine("Socket Exception: " + sex.Message);
				}
				if (bytesRead > 0)
				{
					MemoryStream ms = new MemoryStream(buf, 0, bytesRead, false);
					BinaryReader br = new BinaryReader(ms);
					byte packetType = br.ReadByte();
					if (packetType == IBEO_SCAN_DATA_PACKET_TYPE)
					{
						ParseIbeoPacket(br);
						Console.Write("i");
					}
					else
						Console.WriteLine("unknown packet type");
				}
				else
					Console.WriteLine("no bytes read");
				}
		}

    public void Stop()
    {
      running = false;
      if(!rxThread.Join(100))
        rxThread.Abort();
      if (s != null)
      {
        s.Close();
        s = null;
      }
    }

		public void Start() {
			//if (s!=null)s.Close();
      
			s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
      s.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);

			IPAddress localAddr = IPAddress.Any;
			try
			{
				s.Bind(new IPEndPoint(localAddr, groupPort));
			}
			catch
			{
				Console.WriteLine("Already listening jackass!");
			}
			s.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(groupAddr));			

			Console.WriteLine("starting ibeo listener on multicast addr: " + groupAddr.ToString ());
			//rxThread.Abort();
			rxThread.Start();
		}


		private void ParseIbeoPacket(BinaryReader br) 
		{
			//bear in mind the first byte is picked off for us.
			IbeoScan s = new IbeoScan();
			s.packetNum = br.ReadUInt16();
			uint secs = br.ReadUInt16();
			uint ticks = br.ReadUInt32();
			s.timestamp = (float)secs + ((float)ticks / 10000.0f);
			s.scannerID = br.ReadInt32();
			int numPoints = br.ReadUInt16();
					
			s.points = new List<IbeoPoint> (numPoints);
			for (int i = 0; i < numPoints; i++)
			{
				IbeoPoint p = new IbeoPoint();
				p.location.x = br.ReadInt16() / 100.0f;
				p.location.y = br.ReadInt16() / 100.0f;
				p.location.z = br.ReadInt16() / 100.0f;
				byte chan = br.ReadByte ();
				p.ray = (chan >> 4) & 0x3;
				p.echo = chan & 0x3;
        p.finalHit = (chan & 0x4)!=0;
				p.type = (PointType)br.ReadByte();
				s.points.Add(p);
			}
			if (ScanReceived != null) ScanReceived(this, new IbeoRXEventArgs(s));
		}		
	}
}
