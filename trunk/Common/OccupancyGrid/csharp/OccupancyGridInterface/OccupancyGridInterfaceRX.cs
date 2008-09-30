using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Net.NetworkInformation;
	
namespace OccupancyGridInterface{
	public delegate void GotOccupanctGridDel(object sender, OccupancyGridEventArgs e);
 
  public enum OccupancyStatus 
	{
    Unknown=0,
    Free=1,
    Occupied=2
	}
  
  public struct OccupancyGridCell
  {    
    public byte status;
  }
	
	public class OccupancyGridMsg
	{
		//this is your grid
    public double vts; // in seconds
    public UInt16 seqNum;  // sequence number
    public float angularResolution; //in rads
    public float rightAngle; // in rads. the rightmost edge 
    public float closestDist;  // in m
    public float distResolution; // in m
    public UInt32 dimRad, dimDist; // number of "columns" (angular buckets) and "rows" (dist buckets)
    public OccupancyGridCell[] cells;
	}

	public class OccupancyGridEventArgs : EventArgs
	{
		public OccupancyGridMsg msg;
		public OccupancyGridEventArgs(OccupancyGridMsg msg)
		{
			this.msg = msg;
		}
	}

	public class OccupancyGridInterfaceRX
	{
		public event GotOccupanctGridDel GotPacket;

		//change these to be whatever you use
		private IPAddress multIP = IPAddress.Parse("239.132.1.61");
		private int multPort = 30061;


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
					this.sock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(this.multIP));
					this.sock.BeginReceive(this.buf, 0, this.buf.Length, SocketFlags.None, ReceiveCallback, null);
				}
			}
		}

		public OccupancyGridInterfaceRX()
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

		void Processstuff(byte[] message)
		{
      if (message.Length < 34) return;
			OccupancyGridMsg msg = new OccupancyGridMsg();

      BinaryReader br = new BinaryReader(new MemoryStream(message, false));
      msg.vts = br.ReadDouble();
      msg.seqNum = br.ReadUInt16();
      msg.angularResolution = br.ReadSingle();
      msg.rightAngle = br.ReadSingle();
      msg.closestDist = br.ReadSingle();
      msg.distResolution = br.ReadSingle();
      msg.dimRad = br.ReadUInt32();
      msg.dimDist = br.ReadUInt16();

      if (message.Length < 34 + msg.dimRad * msg.dimDist)
        return;

      msg.cells = new OccupancyGridCell[msg.dimRad * msg.dimDist];

      for (int i = 0; i < msg.cells.Length; i++)
        msg.cells[i].status = br.ReadByte();

			if (GotPacket != null)
				GotPacket(this, new OccupancyGridEventArgs(msg));
		}
	}
}
