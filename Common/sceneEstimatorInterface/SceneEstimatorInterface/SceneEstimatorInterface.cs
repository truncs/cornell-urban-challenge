using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Net.NetworkInformation;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Sensors;
using UrbanChallenge.MessagingService;
namespace SceneEstimatorInterface
{

		#region Render Points
		public struct SceneEstimatorParticlePoint
		{
			public double x; public double y; public double conf;
			public SceneEstimatorParticlePoint(double x, double y, double conf) { this.x = x; this.y = y; this.conf = conf; }
		};

		public struct SceneEstimatorPointsMsg
		{
			public SceneEstimatorMessageID msgType;
			public double time;
			public int numPoints;
			public List<SceneEstimatorParticlePoint> points;
		};


		public class SceneEstimatorPointListRXEventArgs : EventArgs
		{
			private SceneEstimatorPointsMsg p;

			public SceneEstimatorPointListRXEventArgs(SceneEstimatorPointsMsg p)
			{
				this.p = p;
			}

			public SceneEstimatorPointsMsg CurrentSceneEstimatorPointList
			{
				get { return p; }
			}
		}
		#endregion

	

		public class SceneEstimatorUCC_RXEventArgs : EventArgs
		{
			public SceneEstimatorUntrackedClusterCollection ucc;
		
			public SceneEstimatorUCC_RXEventArgs(SceneEstimatorUntrackedClusterCollection ucc)
			{
				this.ucc = ucc;
			}		
		}

		public class SceneEstimatorTCC_RXEventArgs : EventArgs
		{
			public SceneEstimatorTrackedClusterCollection tcc;
		
			public SceneEstimatorTCC_RXEventArgs(SceneEstimatorTrackedClusterCollection tcc)
			{
				this.tcc= tcc;
			}		
		}

		public class SceneEstimatorLRM_LMRXEventArgs : EventArgs
		{
			public LocalRoadModel lrmLM;
		
			public SceneEstimatorLRM_LMRXEventArgs(LocalRoadModel lrmLM)
			{
				this.lrmLM = lrmLM;
			}		
		}

		public class SceneEstimatorLRM_SERXEventArgs : EventArgs 
		{
			public LocalRoadModel lrmSE;
		
			public SceneEstimatorLRM_SERXEventArgs(LocalRoadModel lrmSE)
			{
				this.lrmSE = lrmSE;
			}		
		}

		public class SceneEstimatorInterfaceListener
		{
			//const int MAX_SE_RENDER_POINTS = 1000;
			//private IPAddress ip;
			//private Int32 port;
			//private byte[] buf;
			//private Socket sock;
			UDPChannel clusterChannel;
			UDPChannel localRoadChannel;
			UDPChannel lmLocalRoadChannel;
			uint clusterToken;
			uint localRoadToken;
			uint lmLocalRoadChannelToken;
			
			//public event EventHandler<SceneEstimatorPointListRXEventArgs> ParticlePointListReceived;
			public event EventHandler<SceneEstimatorUCC_RXEventArgs> RX_UCCEvent;
			public event EventHandler<SceneEstimatorTCC_RXEventArgs> RX_TCCEvent;
			public event EventHandler<SceneEstimatorLRM_LMRXEventArgs> RX_LRM_LMEvent;
			public event EventHandler<SceneEstimatorLRM_SERXEventArgs> RX_LRM_SEEvent;

			public SceneEstimatorChannelRX seChannel = null;// seChannel = new SceneEstimatorChannelRX(this);
			public SceneEstimatorInterfaceListener()
			{
				seChannel = new SceneEstimatorChannelRX(this);
				clusterChannel = new UDPChannel(SceneEstimatorObstacleChannelNames.AnyClusterChannelName, IPAddress.Parse("239.132.1.35"), 30035);
				localRoadChannel = new UDPChannel(LocalRoadModelChannelNames.LocalRoadModelChannelName, IPAddress.Parse("239.132.1.35"), 30035);
				lmLocalRoadChannel = new UDPChannel(LocalRoadModelChannelNames.LMLocalRoadModelChannelName, IPAddress.Parse("239.132.1.34"), 30034);				
			}

			public void Start(IPAddress ip, Int32 port)
			{
				clusterToken = clusterChannel.Subscribe(seChannel);
				localRoadToken = localRoadChannel.Subscribe(seChannel);
				lmLocalRoadChannelToken = lmLocalRoadChannel.Subscribe(seChannel);
			}

			public void Stop()
			{
				clusterChannel.Unsubscribe(clusterToken);
				localRoadChannel.Unsubscribe(localRoadToken);
				lmLocalRoadChannel.Unsubscribe(lmLocalRoadChannelToken);
			}


			//private void ParsePointListPacket(BinaryReader br)
			//{
			//  //bear in mind the first int32 is picked off for us.
			//  SceneEstimatorPointsMsg p = new SceneEstimatorPointsMsg();
			//  p.time = br.ReadDouble();
			//  p.numPoints = br.ReadInt32();
			//  Console.WriteLine("Got Point List: " + p.numPoints.ToString());
			//  //now read the points...
			//  if (p.numPoints > MAX_SE_RENDER_POINTS) p.numPoints = MAX_SE_RENDER_POINTS;
			//  p.points = new List<SceneEstimatorParticlePoint>(p.numPoints);
			//  for (int i = 0; i < p.numPoints; i++)
			//  {
			//    SceneEstimatorParticlePoint pt = new SceneEstimatorParticlePoint(br.ReadDouble(), br.ReadDouble(), br.ReadDouble());
			//    p.points.Add(pt);
			//  }
			//  if (ParticlePointListReceived != null) ParticlePointListReceived(this, new SceneEstimatorPointListRXEventArgs(p));

			//}

			public class SceneEstimatorChannelRX : IChannelListener 
			{
				SceneEstimatorInterfaceListener listener = null;
				public SceneEstimatorChannelRX(SceneEstimatorInterfaceListener listener)
				{
					this.listener = listener;
				}

				#region IChannelListener Members
				public void MessageArrived(string channelName, object message)
				{
					if (message is SceneEstimatorUntrackedClusterCollection)
					{
                        if (listener.RX_UCCEvent!= null)  
						listener.RX_UCCEvent (listener,new SceneEstimatorUCC_RXEventArgs ((SceneEstimatorUntrackedClusterCollection)message));						
					}
					else if (message is SceneEstimatorTrackedClusterCollection)
					{
                        if (listener.RX_TCCEvent != null)
						listener.RX_TCCEvent (listener, new SceneEstimatorTCC_RXEventArgs ((SceneEstimatorTrackedClusterCollection)message));
					}
					else if ((message is LocalRoadModel) && (channelName == LocalRoadModelChannelNames.LocalRoadModelChannelName))
					{
                        if (listener.RX_LRM_SEEvent != null)
						listener.RX_LRM_SEEvent(listener, new SceneEstimatorLRM_SERXEventArgs(((LocalRoadModel)message)));
					}
					else if ((message is LocalRoadModel) && (channelName == LocalRoadModelChannelNames.LMLocalRoadModelChannelName ))
					{
                        if (listener.RX_LRM_LMEvent != null)
						listener.RX_LRM_LMEvent(listener, new SceneEstimatorLRM_LMRXEventArgs(((LocalRoadModel)message)));					
					}
				}
				#endregion
			}
		}	
}
