using System;
using System.Collections.Generic;
using System.Text;

using UrbanChallenge.Common.RndfNetwork;

using System.Runtime.Remoting;
using UrbanChallenge.MessagingService;
using UrbanChallenge.NameService;
using UrbanChallenge.Common.Vehicle;

namespace UrbanChallenge.Arbiter.TestDataServer
{
	[Serializable]
	public class MessagingListener : IChannelListener
	{
		private RndfNetwork rndfNetwork;
		private Mdf mdf;
		private VehicleState vehicleState;

		public void MessageArrived(string channelName, object message)
		{
			if (channelName == "RndfNetworkChannel")
			{
				this.rndfNetwork = (Common.RndfNetwork.RndfNetwork)message;
				Console.WriteLine("   > Rndf Received");
			}
			else if (channelName == "MdfChannel")
			{	
				this.mdf = (Common.RndfNetwork.Mdf)message;
				Console.WriteLine("   > Mdf Received");
			}
			else if (channelName == "PositionChannel")
			{
				this.vehicleState = (VehicleState) message;
				Console.WriteLine("   > Position Received: " + this.vehicleState.ToString());
			}
			else if (channelName == "TestStringChannel")
			{
				Console.WriteLine("Received String On TestStringChannel: " + ((string)message));
			}
			else
			{
				throw new ArgumentException("Unknown Channel", channelName);

			}
		}

		/// <summary>
		/// Mdf
		/// </summary>
		public Mdf Mdf
		{
			get { return mdf; }
		}

		/// <summary>
		/// RndfNetwork
		/// </summary>
		public RndfNetwork RndfNetwork
		{
			get { return rndfNetwork; }
		}

		/// <summary>
		/// Vehicle
		/// </summary>
		public VehicleState Vehicle
		{
			get { return vehicleState; }
		}
	}
}
