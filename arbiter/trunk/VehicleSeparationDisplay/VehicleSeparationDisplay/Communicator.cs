using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.NameService;
using System.Runtime.Remoting;
using UrbanChallenge.MessagingService;
using System.Threading;

namespace VehicleSeparationDisplay
{
	/// <summary>
	/// Handles communications to the ai, sim, etc
	/// </summary>
	public class Communicator
	{
		#region Private Members

		/// <summary>
		/// Remoting object directory
		/// </summary>
		private ObjectDirectory objectDirectory;

		/// <summary>
		/// Services
		/// </summary>
		private WellKnownServiceTypeEntry[] wkst;

		/// <summary>
		/// listener for messaging
		/// </summary>
		private MessagingListener messagingListener;

		/// <summary>
		/// Channel factory holding all channels
		/// </summary>
		private IChannelFactory channelFactory;

		public VehicleSeparationDisplay VehicleDisplay;

		#endregion

		#region Channels

		// arebiter information channel
		private IChannel arbiterInformationChannel;
		private uint arbiterInformationChannelToken;

		#endregion

		#region Constructor

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="arbiterCore"></param>
		public Communicator(VehicleSeparationDisplay vsd)
		{
			// messaging
			this.messagingListener = new MessagingListener(vsd, this);
			this.VehicleDisplay = vsd;
			this.BeginCommunications();
		}

		#endregion

		#region Functions

		/// <summary>
		/// start communications
		/// </summary>
		public void BeginCommunications()
		{
			this.Configure();
		}

		/// <summary>
		/// Configures remoting
		/// </summary>
		public void Configure()
		{
			// configure
			RemotingConfiguration.Configure("VehicleSeparationDisplay.exe.config", false);
			wkst = RemotingConfiguration.GetRegisteredWellKnownServiceTypes();

			// "Activate" the NameService singleton.
			objectDirectory = (ObjectDirectory)Activator.GetObject(typeof(ObjectDirectory), wkst[0].ObjectUri);

			// Retreive the directory of messaging channels
			channelFactory = (IChannelFactory)objectDirectory.Resolve("ChannelFactory");

			// Notify user of success
			Console.WriteLine("Connection to Name Service Successful");

			// get information channel
			arbiterInformationChannel = channelFactory.GetChannel("ArbiterInformationChannel" + this.RemotingSuffix, ChannelMode.UdpMulticast);
			arbiterInformationChannelToken = arbiterInformationChannel.Subscribe(messagingListener);
		}

		/// <summary>
		/// Provides suffix to remoting name if in sim
		/// </summary>
		public string RemotingSuffix
		{
			get
			{
				return global::VehicleSeparationDisplay.Properties.Settings.Default.SimMode ?
					"_" + global::VehicleSeparationDisplay.Properties.Settings.Default.ComputerName : "";
			}
		}

		#endregion
	}
}
