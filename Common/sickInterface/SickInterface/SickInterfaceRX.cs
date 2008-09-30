using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace SickInterface
{

	public struct RThetaCoordinate
	{		
		public RThetaCoordinate(float R, float theta)
		{ this.R = R; this.theta = theta; }
		public float R; public float theta; 
	}

	public class SickRXEventArgs : EventArgs
	{
		private SickScan s;

		public SickRXEventArgs(SickScan s)
		{
			this.s = s;
		}

		public SickScan Scan
		{
			get { return s; }
		}
	}

	public struct SickScan
	{
		public int packetNum;   // packet counter	  
		public double timestamp;	
		public int scannerID;   // Out internal ID of the source scanner
		public List<SickPoint> points;
	}

	public struct SickPoint
	{
		public RThetaCoordinate location;
		public SickPoint(RThetaCoordinate location)
		{ this.location = location; }
	}

	public class SickInterfaceRX
	{

		private const int STX = 0x02;
		private const int LIDAR_ADDR = 0x80;
		private const int LIDAR_MAXVAL_ERROR = 8183;
		private const int LIDAR_DAZZLING_ERROR = 8190;
				
		private IPAddress ip;
		private Int32 port;
		private byte[] buf;
		private Socket sock;

		public event EventHandler<SickRXEventArgs> ScanReceived;		

    int totalPackets = 0;
    int totalPacketsResetable = 0;

		public SickInterfaceRX()
    {
			
    }

		public void Start(IPAddress ip, Int32 port)
		{
			this.ip = ip; this.port = port;
			BuildSocket();
		}

		public void Stop()
		{
            if (this.sock == null) return; //already stopped
            this.sock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DropMembership, new MulticastOption(this.ip));
			this.sock = null;
		}

		private void ReceiveCallback(IAsyncResult ar)
		{
			try
			{
				int bytesReceived = this.sock.EndReceive(ar);
				if (bytesReceived > 0)
				{					
					int ret = ProcessLidarPacket(buf);
          if (ret == 0)
          {
              totalPackets++;
              totalPacketsResetable++;
          }
          else
						Console.WriteLine("BAD sick packet!!!");
				}
			}
			catch (SocketException ex)
			{
				Console.WriteLine("Socket exception! " + ex.Message);
				return;
			}
			
			this.sock.BeginReceive(this.buf, 0, this.buf.Length, SocketFlags.None, ReceiveCallback, null);
		}

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
					this.sock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 10);
					this.sock.Bind(new IPEndPoint(IPAddress.Any, this.port));
					this.sock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(this.ip));
					this.sock.BeginReceive(this.buf, 0, this.buf.Length, SocketFlags.None, ReceiveCallback, null);
				}
			}
		}


		
    private int ProcessLidarPacket(Byte[] rx)
    {
			SickScan scan = new SickScan();
			scan.points = new List<SickPoint>();						
      MemoryStream ms = new MemoryStream(rx);
			int id = ms.ReadByte(); 
			//ghetto:
			//check for 180s 
			bool is180 = false;
			if (id == 5) is180 = true;	
				
			int mode = ms.ReadByte();//Remove id/mode
			int secs = (ms.ReadByte() << 8) + ms.ReadByte();
			int ticks =  (ms.ReadByte() << 24) + (ms.ReadByte() <<16)+ (ms.ReadByte() <<8) + (ms.ReadByte());
      if (ms.ReadByte() != STX) //STX
          return -1;
      if (ms.ReadByte() != LIDAR_ADDR) //Address
          return -2;
      int length = ms.ReadByte() + (ms.ReadByte() * 256); //message len excluding CRC
      int cmd = ms.ReadByte(); //command
      ms.ReadByte(); //boolstuff 2 bytes
      ms.ReadByte();
			if (is180)
			{
				for (double i = -90; i <= 90; i += 0.5)
				{
					int r = (ms.ReadByte() + (ms.ReadByte() * 256));
					double theta = ((i) * (Math.PI / 180)); //radians
					double range = (double)r * .01; //range in meters
					
					double rawY = -1 * Math.Sin(theta) * range;
					double rawX = Math.Cos(theta) * range;
					scan.points.Add(new SickPoint(new RThetaCoordinate((float)range, (float)theta)));
				} 
			}
			else
			{
				for (double i = -45; i <= 45; i += 0.5)
				{
					int r = (ms.ReadByte() + (ms.ReadByte() * 256));
					double theta = ((i) *  (Math.PI / 180)); //radians
					double range = (double)r * .01; //range in meters
					double rawY = -1 * Math.Sin(theta) * range;
					double rawX = Math.Cos(theta) * range;
					scan.points.Add(new SickPoint(new RThetaCoordinate((float)range, (float)theta)));
				}
			}

			scan.packetNum = totalPackets;			
			scan.scannerID = id;
			scan.timestamp = (double)secs + (double)ticks / 10000.0;

	
			if (ScanReceived != null)
						ScanReceived(this,new SickRXEventArgs  (scan));
        return 0;            
    }
        
	}
}
