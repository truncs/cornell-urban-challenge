using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace ClusteredSickInterface
{

	public delegate void GotClusteredSickPacketDel(object sender, ClusteredSickEventArgs e);

	enum ClusteredSickMessageID : int
	{
		RS_Info = 0,
		RS_ClusterMsg = 1,
		RS_BAD = 99
	};

	public struct SickXYPoint
	{
		public double x; public double y;
		public SickXYPoint(double x, double y)
		{ this.x = x; this.y = y; }
	}
	public struct SickCluster
	{
		public bool stable;
		public bool leftOccluded;
		public bool rightOccluded;
		public List<SickXYPoint> points;
	}


	public class ClusteredSickEventArgs : EventArgs
	{
		public double time;
		public List<SickCluster> cluster;
		public ClusteredSickEventArgs(double time, List<SickCluster> cluster)
		{
			this.time = time;
			this.cluster = cluster;
		}
	}

	public class ClusteredSickInterfaceRX
	{
	

		public event GotClusteredSickPacketDel GotPacket;

		private IPAddress multIP = IPAddress.Parse("239.132.1.39");
		private int multPort = 30039;


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

		public ClusteredSickInterfaceRX()
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
			BinaryReader br = new BinaryReader(new MemoryStream(message));
			ClusteredSickMessageID msgID = (ClusteredSickMessageID)br.ReadInt32();
			int seq = (br.ReadInt32());
			double time = br.ReadDouble();
			List<SickCluster> clusters = new List<SickCluster>();
			int numclusters = br.ReadInt32();

			for (int i = 0; i < numclusters; i++)
			{
				SickCluster cluster = new SickCluster();
				cluster.stable = br.ReadBoolean();
				cluster.leftOccluded = br.ReadBoolean();
				cluster.rightOccluded = br.ReadBoolean();
				int numpoints = br.ReadInt32();
				if (numpoints !=0)
				cluster.points = new List<SickXYPoint>(numpoints);
				for (int j = 0; j < numpoints; j++)
				{
					double x = br.ReadDouble();
					double y = br.ReadDouble();
					SickXYPoint pt = new SickXYPoint(x,y);
					cluster.points.Add(pt);
				}
				clusters.Add(cluster);
			}
			if (br.BaseStream.Position != br.BaseStream.Length)
				Console.WriteLine("Incomplete read of rear sick clusters.");
			if (GotPacket != null)
				GotPacket(this, new ClusteredSickEventArgs(time,clusters));
		}
		
	}

}
