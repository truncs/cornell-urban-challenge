	using System;
	using System.Collections.Generic;
	using System.Text;
	using System.Net.Sockets;
	using System.Net;
	using System.IO;
	using System.Diagnostics;
	using System.Net.NetworkInformation;
	
namespace SideSickInterface
{
	public delegate void GotSideSickPacketDel(object sender, SideSickEventArgs e);
	
	public enum SideSickMessageID : int
	{
		SS_Info = 0,
		SS_ScanMsg = 1,
		SS_BAD = 99
	}

	public enum SideSickID : int
	{
		SS_DRIVER = 0,
		SS_PASSENGER = 1
	}

	public struct SideSickObstacle
	{			
		public float distance;		
		public int numpoints;
		public float height;
	}

	public struct SideSickMsg
	{
		public SideSickMessageID msgType;
		public SideSickID scannerID;
		public int sequenceNumber;
		public double carTime;		
		public SideSickObstacle[] obstacles;
	}


	public class SideSickEventArgs : EventArgs
	{
		public SideSickMsg msg;
		public SideSickEventArgs(SideSickMsg msg)
		{
			this.msg = msg;
		}
	}

	public class SideSickInterfaceRX
	{
		public event GotSideSickPacketDel GotPacket;

		private IPAddress multIP = IPAddress.Parse("239.132.1.38");
		private int multPort = 30038;


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

		public SideSickInterfaceRX()
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
			SideSickMsg msg;			
			BinaryReader br = new BinaryReader(new MemoryStream(message));
			br.ReadChar();
			br.ReadChar();
			msg.sequenceNumber = br.ReadInt32();
			msg.msgType = (SideSickMessageID)br.ReadInt32();
			msg.scannerID = (SideSickID)br.ReadInt32();			
			msg.carTime = br.ReadDouble();
			int numobstacles = br.ReadInt32();
			msg.obstacles = new SideSickObstacle[numobstacles];
			for (int i = 0; i < numobstacles; i++)
			{
				msg.obstacles[i].distance = br.ReadSingle();
				msg.obstacles[i].numpoints = br.ReadInt32();
				msg.obstacles[i].height = br.ReadSingle();
			}
			for (int i = numobstacles; i < 10; i++)
			{
				br.ReadSingle();
				br.ReadInt32();
				br.ReadSingle();
			}
			if (br.BaseStream.Position != br.BaseStream.Length)
				Console.WriteLine("Incomplete read of side sick msg.");
			if (GotPacket != null)
				GotPacket(this, new SideSickEventArgs(msg));
		}

	}

}
