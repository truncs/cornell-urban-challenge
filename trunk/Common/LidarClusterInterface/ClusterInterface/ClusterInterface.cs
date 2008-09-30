
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Net.NetworkInformation;
using UrbanChallenge.Common;

namespace ClusterInterface
{
  public struct LidarCluster
  {
    public bool stable, leftOccluded, rightOccluded;
    public v3f[] pts;
    public v3f bb_low, bb_high;
    public bool highObstacle;
  };

	public class ClusterMsg
	{
		public double timestamp;
    public List<LidarCluster> clusters;
	}

	public class ClusterRXEventArgs : EventArgs
	{
		private ClusterMsg c;

		public ClusterRXEventArgs(ClusterMsg c)
		{
			this.c = c;
		}

		public ClusterMsg CurrentClusterMsg
		{
			get { return c; }
		}
	}

	public class ClusterInterfaceListener
	{
		private IPAddress ip;
		private Int32 port;
		private byte[] buf;
		private Socket sock;

    const byte CLUSTER_PACKET_TYPE = 0x75;

		public event EventHandler<ClusterRXEventArgs> ClustersReceived;

		public ClusterInterfaceListener()
		{
		}

		public void Start(IPAddress ip, Int32 port)
		{
			this.ip = ip; this.port = port;
			BuildSocket();
		}

		public void Stop()
		{
			this.sock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DropMembership, new MulticastOption(this.ip));
			this.sock = null;
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

		private void ReceiveCallback(IAsyncResult ar)
		{
			try
			{
				int bytesRead = this.sock.EndReceive(ar);
				if (bytesRead > 0)
				{
					MemoryStream ms = new MemoryStream(buf, 0, bytesRead, false);
					BinaryReader br = new BinaryReader(ms);				
					ParseClustersPacket(br);				
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Socket exception! " + ex.Message);

			}
			finally
			{
				this.sock.BeginReceive(this.buf, 0, this.buf.Length, SocketFlags.None, ReceiveCallback, null);
			}
		}

		private void ParseClustersPacket(BinaryReader br)
		{
			ClusterMsg msg = new ClusterMsg ();

      // read the header

      byte packetType = br.ReadByte();
      if (packetType != CLUSTER_PACKET_TYPE)
      {
        Debug.WriteLine("ERROR: cluster parser got packet starting with " + packetType.ToString());
        return;
      }

      UInt16 packetNum = br.ReadUInt16();
      UInt16 tsSeconds = br.ReadUInt16();
      UInt32 tsTicks = br.ReadUInt32();

      UInt16 numPts = br.ReadUInt16();
      UInt16 numClusters = br.ReadUInt16();

			msg.timestamp = (double)tsSeconds + (double)tsTicks / 10000.0;
      // now for the red meat..

      v3f[] pts = new v3f[numPts];
      for (int i = 0; i < numPts; i++)
      {
        pts[i].x = (float)(br.ReadInt16())/100.0f;
        pts[i].y = (float)(br.ReadInt16())/100.0f;
        pts[i].z = (float)(br.ReadInt16())/100.0f;
      }

      msg.clusters = new List<LidarCluster>();

      for (int i = 0; i < numClusters; i++)
      {
        LidarCluster lc = new LidarCluster();
        UInt16 firstPtIndex, lastPtIndex;
        byte flags;

        firstPtIndex = br.ReadUInt16();
        lastPtIndex = br.ReadUInt16();
        flags = br.ReadByte();

        lc.pts = new v3f[lastPtIndex - firstPtIndex + 1];
        lc.stable = (flags & 0x01)==0;
        lc.leftOccluded = (flags & 0x02)!=0;
        lc.rightOccluded = (flags & 0x04)!=0;
        lc.highObstacle = (flags & 0x08)!=0;

        lc.bb_low.x = lc.bb_high.x = pts[firstPtIndex].x;
        lc.bb_low.y = lc.bb_high.y = pts[firstPtIndex].y;
        lc.bb_low.z = lc.bb_high.z = pts[firstPtIndex].z;

        for (int j = firstPtIndex; j <= lastPtIndex; j++)
        {
          lc.pts[j - firstPtIndex] = pts[j];
          lc.bb_low.x = Math.Min(lc.bb_low.x, pts[j].x);
          lc.bb_low.y = Math.Min(lc.bb_low.y, pts[j].y);
          lc.bb_low.z = Math.Min(lc.bb_low.z, pts[j].z);
          lc.bb_high.x = Math.Max(lc.bb_high.x, pts[j].x);
          lc.bb_high.y = Math.Max(lc.bb_high.y, pts[j].y);
          lc.bb_high.z = Math.Max(lc.bb_high.z, pts[j].z);
        }

        msg.clusters.Add(lc);
      }

			if (ClustersReceived != null) ClustersReceived(this, new ClusterRXEventArgs(msg));

		}

	}
}
