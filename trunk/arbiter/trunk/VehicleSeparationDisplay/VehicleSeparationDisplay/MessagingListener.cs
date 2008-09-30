using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.Core.Common.Arbiter;
using UrbanChallenge.MessagingService;

namespace VehicleSeparationDisplay
{
	/// <summary>
	/// Provides pooint of entry for messages from messaging service
	/// </summary>
	[Serializable]
	public class MessagingListener : MarshalByRefObject, IChannelListener
	{
		#region Private Members

		[NonSerialized]
		private VehicleSeparationDisplay VehicleDisplay;
		private Communicator c;

		#endregion

		#region Constructor

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="remotingSuffix"></param>
		public MessagingListener(VehicleSeparationDisplay vsd, Communicator c)
		{
			this.VehicleDisplay = vsd;
			this.c = c;
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
			if (channelName == "ArbiterInformationChannel" + this.c.RemotingSuffix
				&& message is ArbiterInformation)
			{
				// set info
				this.VehicleDisplay.Information = (ArbiterInformation)message;
			}
		}

		#endregion
	}
}
