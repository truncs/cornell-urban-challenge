using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.MessagingService;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common.Sensors;

namespace UrbanChallenge.Arbiter.Core.Communications
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
		private string remotingSuffix;
		private double? vehicleSpeed = null;
		private SideObstacles sideSickObstaclesDriver = null;
		private SideObstacles sideSickObstaclesPass = null;

		#endregion

		#region Constructor

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="remotingSuffix"></param>
		public MessagingListener(string remotingSuffix)
		{
			this.remotingSuffix = remotingSuffix;

			// check if sim mode HACK
			bool simMode = global::UrbanChallenge.Arbiter.Core.ArbiterSettings.Default.SimMode;

			if (!simMode)
			{
				SceneEstimatorTrackedClusterCollection setc = new SceneEstimatorTrackedClusterCollection();
				setc.clusters = new SceneEstimatorTrackedCluster[] { };
				this.observedVehicles = setc;

				SceneEstimatorUntrackedClusterCollection seutc = new SceneEstimatorUntrackedClusterCollection();
				seutc.clusters = new SceneEstimatorUntrackedCluster[] { };
				this.observedObstacles = seutc;
			}
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
			try
			{
				if (channelName == "ArbiterSceneEstimatorPositionChannel" + this.remotingSuffix
					&& message is VehicleState)
				{
					// cast and set
					vehicleState = (VehicleState)message;
				}
				else if (channelName == "ObservedObstacleChannel" + this.remotingSuffix
					&& message is SceneEstimatorUntrackedClusterCollection)
				{
					// cast and set
					observedObstacles = (SceneEstimatorUntrackedClusterCollection)message;
				}
				else if (channelName == "ObservedVehicleChannel" + this.remotingSuffix
					&& message is SceneEstimatorTrackedClusterCollection)
				{
					// check if not ignoring vehicles
					if (!Arbiter.Core.ArbiterSettings.Default.IgnoreVehicles)
					{
						// cast and set
						observedVehicles = (SceneEstimatorTrackedClusterCollection)message;
					}
				}
				else if (channelName == "VehicleSpeedChannel" + this.remotingSuffix
					&& message is double)
				{
					// cast and set
					vehicleSpeed = (double)message;
				}
				else if (channelName == "SideObstacleChannel" + this.remotingSuffix
					&& message is SideObstacles)
				{
					SideObstacles sideSickObstacles = (SideObstacles)message;
					if (sideSickObstacles.side == SideObstacleSide.Driver)
						this.sideSickObstaclesDriver = sideSickObstacles;
					else
						this.sideSickObstaclesPass = sideSickObstacles;
				}
			}
			catch (Exception ex)
			{
				ArbiterOutput.Output("Error receiving message: " + ex.ToString());
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
