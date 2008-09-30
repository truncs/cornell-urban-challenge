using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Net.NetworkInformation;

namespace LocalMapInterface
{
	public enum LocalMapMessageID : int
		{
			LOCALMAP_Info = 0,
			LOCALMAP_Targets= 1,
      LOCALMAP_LooseClusters = 2,
			LOCALMAP_Bad = 99
		}

	public enum LocalMapClusterClass : int
	{
		LOCALMAP_LowObstacle = 0,
		LOCALMAP_HighObstacle = 1
	}

	public struct LocalMapPoint
	{ 	
		public double x;
		public double y;
		public LocalMapPoint(double x, double y) { this.x = x; this.y = y; }
	};

	public struct LocalMapTarget
	{
		public List<LocalMapPoint> points;
		public int type;
		public float x;
		public float y;
		public float speed;
		public float heading;
		public float width;
		public float orientation;
	};

  public struct LocalMapLooseCluster
  {
    public List<LocalMapPoint> points;
		public LocalMapClusterClass targetClass;
  };

	public struct LocalMapTargetsMsg
	{
		public List<LocalMapTarget> targets;
    public double timestamp;
	}

  public struct LocalMapLooseClusterMsg
  {
    public List<LocalMapLooseCluster> clusters;
    public double timestamp;
  };

	public class LocalMapTargetsRXEventArgs : EventArgs
	{
		private LocalMapTargetsMsg p;

		public LocalMapTargetsRXEventArgs(LocalMapTargetsMsg p)
		{
			this.p = p;
		}

		public LocalMapTargetsMsg CurrentLocalMapTargetsMsg
		{
			get { return p; }
		}
	}

  public class LocalMapLooseClustersRXEventArgs : EventArgs
  {
    private LocalMapLooseClusterMsg p;

    public LocalMapLooseClustersRXEventArgs(LocalMapLooseClusterMsg p)
    {
      this.p = p;
    }

    public LocalMapLooseClusterMsg CurrentLocalMapLooseClustersMsg
    {
      get { return p; }
    }
  }

	public class LocalMapInterfaceListener
	{
		private IPAddress ip;
		private Int32 port;
		private byte[] buf;
		private Socket sock;

		public event EventHandler<LocalMapTargetsRXEventArgs> TargetsReceived;
    public event EventHandler<LocalMapLooseClustersRXEventArgs> LooseClustersReceived;		

		public LocalMapInterfaceListener()
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
					br.ReadByte(); //ver
					br.ReadByte(); //type
					br.ReadInt32(); //seq
					LocalMapMessageID packetType = (LocalMapMessageID)br.ReadInt32(); //type
					if (packetType == LocalMapMessageID.LOCALMAP_Info)
					{
						Console.WriteLine("Info message recived: " + (br.ReadString()));
					}
					else if (packetType == LocalMapMessageID.LOCALMAP_Targets)
					{						
						ParseTargetsPacket(br);						
					}
          else if (packetType == LocalMapMessageID.LOCALMAP_LooseClusters)
          {
            ParseLooseClustersPacket(br);
          }
          else if (packetType == LocalMapMessageID.LOCALMAP_Bad)
          {
            Console.WriteLine("BAD MESSAGE RECIEVED.");
          }
          else
            Debug.WriteLine("WARNING: unknown packet type: " + packetType);
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

    private void ParseLooseClustersPacket(BinaryReader br)
    {
      LocalMapLooseClusterMsg msg = new LocalMapLooseClusterMsg();
      int numClusters = br.ReadInt32();
      msg.clusters = new List<LocalMapLooseCluster>(numClusters);
      msg.timestamp = br.ReadDouble();
      for (int i = 0; i < numClusters; i++)
      {
        LocalMapLooseCluster cluster = new LocalMapLooseCluster();
        int numPts = br.ReadInt32();
				int clusterType = br.ReadInt32();
				cluster.targetClass = (LocalMapClusterClass)clusterType;
        cluster.points = new List<LocalMapPoint>(numPts);
        for (int j = 0; j < numPts; j++)
        {
          short x = br.ReadInt16();
          short y = br.ReadInt16();
          cluster.points.Add(new LocalMapPoint(x / 100.0, y / 100.0));
        }
        msg.clusters.Add(cluster);		
      }

      if (LooseClustersReceived != null) LooseClustersReceived(this, new LocalMapLooseClustersRXEventArgs(msg));
    }

    private void ParseTargetsPacket(BinaryReader br)
		{
			//bear in mind the first int32 is picked off for us.
			LocalMapTargetsMsg targets = new LocalMapTargetsMsg();
			int numTargets = br.ReadInt32();
			//Console.WriteLine("GOT " + numTargets + " targets!");
			targets.targets = new List<LocalMapTarget>(numTargets);
      targets.timestamp = br.ReadDouble();
			for (int i = 0; i < numTargets; i++)
			{
				//now get the targets
				LocalMapTarget target = new LocalMapTarget();
				int numPoints = br.ReadInt32();
				target.type = br.ReadInt32();
				target.x = br.ReadSingle();
				target.y = br.ReadSingle();
				target.speed = br.ReadSingle();
				target.heading = br.ReadSingle();
				target.width = br.ReadSingle();
				target.orientation = br.ReadSingle();
				for (int k = 0; k < 36; k++)
					br.ReadSingle(); //mess the covariance matrix
				target.points = new List<LocalMapPoint>(numPoints);
				for (int j = 0; j < numPoints; j++)
				{
					short x = br.ReadInt16();
					short y = br.ReadInt16();
					target.points.Add (new LocalMapPoint (x / 100.0, y / 100.0));
					//Console.WriteLine("got to " + i + " " + j);
				}
				targets.targets.Add(target);				
			}
			
			if (TargetsReceived != null) TargetsReceived(this, new LocalMapTargetsRXEventArgs(targets));
		
		}
		
	}
}



	
