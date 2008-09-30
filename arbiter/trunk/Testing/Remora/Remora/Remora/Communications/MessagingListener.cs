using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common.RndfNetwork;
using UrbanChallenge.MessagingService;
using UrbanChallenge.Common.Route;
using UrbanChallenge.Common;
using UrbanChallenge.Arbiter.Communication;
using UrbanChallenge.Common.Sensors;
using UrbanChallenge.Arbiter.ArbiterCommon;
using UrbanChallenge.OperationalService;
using UrbanChallenge.Common.Sensors.Vehicle;
using UrbanChallenge.Common.Sensors.Obstacle;

namespace Remora.Communications
{
	/// <summary>
	/// Listens for messages
	/// </summary>
	public class MessagingListener : MarshalByRefObject, IChannelListener
	{
        private RemoraDisplay remora;
		private VehicleState vehicleState;
		public FullRoute route;
		public IState state;
		public ArbiterInformation ArbiterInformation;
		public CarMode CarMode;
		private ObservedVehicles? observedVehicles;
		private ObservedObstacles? observedObstacles;
		private ObservedVehicles? fakeVehicles;

		public MessagingListener()
		{
		}

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="remora">Sets reference to the gui</param>
        public MessagingListener(RemoraDisplay remora)
        {
            // for callbacks
            this.remora = remora;
        }

		public void MessageArrived(string channelName, object message)
		{
			if (channelName == "PositionChannel")
			{
				this.vehicleState = (VehicleState)message;
			}
			else if (channelName == "ArbiterInformationChannel")
			{
				this.ArbiterInformation = (ArbiterInformation)message;
			}
			else if (channelName == "CarMode")
			{
				this.CarMode = (CarMode)message;
			}
			else if (channelName == "ObservedVehicleChannel")
			{
				this.observedVehicles = (ObservedVehicles)message;
			}
			else if (channelName == "ObservedObstacleChannel")
			{
				this.observedObstacles = (ObservedObstacles)message;
			}
			else if (channelName == "FakeVehicleChannel")
			{
				this.fakeVehicles = (ObservedVehicles)message;
			}
			else
			{
				throw new ArgumentException("Unknown Channel", channelName);
			}
		}

		public ObservedVehicles? ObservedVehicles
		{
			get { return observedVehicles; }
			set { observedVehicles = value; }
		}

		public ObservedObstacles? ObservedObstacles
		{
			get { return observedObstacles; }
			set { observedObstacles = value; }
		}

		public ObservedVehicles? FakeVehicles
		{
			get { return fakeVehicles; }
			set { fakeVehicles = value; }
		}

		/// <summary>
		/// Vehicle's Current State
		/// </summary>
		public VehicleState Vehicle
		{
			get { return vehicleState; }
		}
	}
}
