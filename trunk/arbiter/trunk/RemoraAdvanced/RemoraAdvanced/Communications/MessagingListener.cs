using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common.Sensors.Obstacle;
using UrbanChallenge.Common.Sensors.Vehicle;
using RemoraAdvanced.Common;
using UrbanChallenge.MessagingService;
using System.Windows.Forms;
using UrbanChallenge.Arbiter.Core.Common.Arbiter;
using UrbanChallenge.Common.Sensors;

namespace RemoraAdvanced.Communications
{
	/// <summary>
	/// Provides pooint of entry for messages from messaging service
	/// </summary>
	[Serializable]
	public class MessagingListener : MarshalByRefObject, IChannelListener
	{
		#region Private Members

		private VehicleState vehicleState;
		private SceneEstimatorUntrackedClusterCollection observedObstacles = null;
		private SceneEstimatorTrackedClusterCollection observedVehicles = null;
		private double? vehicleSpeed = null;
		private Remora remora;
		private SideObstacles sideSickObstaclesDriver = null;
		private SideObstacles sideSickObstaclesPass = null;

		#endregion

		#region Constructor

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="remotingSuffix"></param>
		public MessagingListener(Remora remora)
		{
			this.remora = remora;
		}

		#endregion

		#region IChannelListener Members

		/// <summary>
		/// Called when message sent to us
		/// </summary>
		/// <param name="channelName"></param>
		/// <param name="message"></param>
		public void MessageArrived(string channelName, object message)
		{
			if (channelName == "ArbiterSceneEstimatorPositionChannel" + RemoraCommon.Communicator.RemotingSuffix
				&& message is VehicleState)
			{
				// cast and set
				vehicleState = (VehicleState)message;
			}
			else if (channelName == "ObservedObstacleChannel" + RemoraCommon.Communicator.RemotingSuffix
				&& message is SceneEstimatorUntrackedClusterCollection)
			{
				// cast and set
				observedObstacles = (SceneEstimatorUntrackedClusterCollection)message;
			}
			else if (channelName == "ObservedVehicleChannel" + RemoraCommon.Communicator.RemotingSuffix
				&& message is SceneEstimatorTrackedClusterCollection)
			{
				// cast and set
				observedVehicles = (SceneEstimatorTrackedClusterCollection)message;
			}
			else if (channelName == "VehicleSpeedChannel" + RemoraCommon.Communicator.RemotingSuffix
				&& message is double)
			{
				// cast and set
				vehicleSpeed = (double)message;
			}
			else if (channelName == "ArbiterOutputChannel" + RemoraCommon.Communicator.RemotingSuffix
				&& message is string)
			{
				// output
				RemoraOutput.WriteLine((string)message, OutputType.Arbiter);
			}
			else if (channelName == "ArbiterInformationChannel" + RemoraCommon.Communicator.RemotingSuffix
				&& message is ArbiterInformation)
			{
				// set info
				RemoraCommon.aiInformation.information = (ArbiterInformation)message;
			}
			else if (channelName == "SideObstacleChannel" + RemoraCommon.Communicator.RemotingSuffix
			 && message is SideObstacles)
			{
				SideObstacles sideSickObstacles = (SideObstacles)message;
				if (sideSickObstacles.side == SideObstacleSide.Driver)
					this.sideSickObstaclesDriver = sideSickObstacles;
				else
					this.sideSickObstaclesPass = sideSickObstacles;
			}
		}

		#endregion

		#region Accessors

		/// <summary>
		/// obstacles to the side
		/// </summary>
		public SideObstacles SideSickObstacles(SideObstacleSide sos)
		{
			return sos == SideObstacleSide.Driver ? this.sideSickObstaclesDriver : this.sideSickObstaclesPass;
		}

		/// <summary>
		/// Vehicles
		/// </summary>
		public SceneEstimatorTrackedClusterCollection ObservedVehicles
		{
			get { return observedVehicles; }
		}

		/// <summary>
		/// Obstacles
		/// </summary>
		public SceneEstimatorUntrackedClusterCollection ObservedObstacles
		{
			get { return observedObstacles; }
		}

		/// <summary>
		/// Vehicle State
		/// </summary>
		public VehicleState VehicleState
		{
			get { return vehicleState; }
			set { vehicleState = value; }
		}

		/// <summary>
		/// Vehicle speed
		/// </summary>
		public double? VehicleSpeed
		{
			get { return vehicleSpeed; }
			set { vehicleSpeed = value; }
		}

		#endregion
	}
}
