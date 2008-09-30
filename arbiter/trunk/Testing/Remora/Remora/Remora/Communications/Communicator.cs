using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using System.Runtime.Remoting;
using UrbanChallenge.NameService;
using UrbanChallenge.MessagingService;

using UrbanChallenge.Common.RndfNetwork;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common.Route;

using UrbanChallenge.Arbiter.Communication;
using UrbanChallenge.OperationalService;
using UrbanChallenge.Arbiter.ArbiterCommon;
using UrbanChallenge.Common;
using System.Diagnostics;

namespace Remora.Communications
{
	/// <summary>
	/// Manages connections from the Remora to the Arbiter
	/// </summary>
	public class Communicator
	{
		// ********** Remoting Communications *********** //
		private ObjectDirectory objectDirectory;
		public MessagingListener channelListener;		
		private ArbiterRemote arbiterRemote;
		private RemoraDisplay remora;
        public bool RemotingInitialized = false;

		// ********** Messaging Channels **************** //
		private IChannel vehicleStateChannel;	// Listen for vehicle state updates (comes faster than ai updates, can plot difference)
		private uint vehicleStateChannelToken;	// Token representing the vehicle state channel

		/// <summary>
		/// static obstacle channel
		/// </summary>
		private IChannel observedObstacleChannel;

		/// <summary>
		/// dynamic obstacles channel
		/// </summary>
		private IChannel observedVehicleChannel;

		/// <summary>
		/// arbiter information channel
		/// </summary>
		private IChannel arbiterInformationChannel;
		private uint arbiterInformationChannelToken;

		/// <summary>
		/// arbiter information channel
		/// </summary>
		private IChannel carModeChannel;
		private uint carModeChannelToken;

		/// <summary>
		/// test channel for fake vehicles
		/// </summary>
		private IChannel fakeVehicleChannel;
		private uint fakeVehicleChannelToken;

		/// <summary>
		/// Constructor
		/// </summary>		
		public Communicator(RemoraDisplay remoraDisplay)
		{
			// set the reference to the display
			this.remora = remoraDisplay;	
		
			// initialize the remoting zommunications
			// this.InitializeRemotingCommunications();
		}

		#region Initialization

		/// <summary>
		/// Initializes communications with the outside world by means of remoting
		/// </summary>
		public void InitializeRemotingCommunications()
		{
			// try to shut down in case we have already been active
			this.ShutDown();

			try
			{
				// Read the configuration file.
				RemotingConfiguration.Configure("Remora.exe.config", false);
				WellKnownServiceTypeEntry[] wkst = RemotingConfiguration.GetRegisteredWellKnownServiceTypes();

				// "Activate" the NameService singleton.
				objectDirectory = (ObjectDirectory)Activator.GetObject(typeof(ObjectDirectory), wkst[0].ObjectUri);

				// Retreive the directory of messaging channels
				IChannelFactory channelFactory = (IChannelFactory)objectDirectory.Resolve("ChannelFactory");

				// Retreive the Messaging Service channels we want to listen to
				vehicleStateChannel = channelFactory.GetChannel("PositionChannel", ChannelMode.UdpMulticast);
				observedObstacleChannel = channelFactory.GetChannel("ObservedObstacleChannel", ChannelMode.UdpMulticast);
				observedVehicleChannel = channelFactory.GetChannel("ObservedVehicleChannel", ChannelMode.UdpMulticast);
				arbiterInformationChannel = channelFactory.GetChannel("ArbiterInformationChannel", ChannelMode.UdpMulticast);
				carModeChannel = channelFactory.GetChannel("CarMode", ChannelMode.UdpMulticast);
				fakeVehicleChannel = channelFactory.GetChannel("FakeVehicleChannel", ChannelMode.UdpMulticast);

				// Create a channel listeners and listen on wanted channels
				channelListener = new MessagingListener();
				vehicleStateChannelToken = vehicleStateChannel.Subscribe(channelListener);
				observedObstacleChannel.Subscribe(channelListener);
				observedVehicleChannel.Subscribe(channelListener);
				arbiterInformationChannelToken = arbiterInformationChannel.Subscribe(channelListener);
				carModeChannelToken = carModeChannel.Subscribe(channelListener);
				fakeVehicleChannelToken = fakeVehicleChannel.Subscribe(channelListener);

        // set that remoting has been successfully initialize
        this.RemotingInitialized = true;
        RemoraOutput.WriteLine("Successfully initialized communications");
			}
			catch (Exception e)
			{
				RemoraOutput.WriteLine(e.ToString() + "\n\n" + "Could not initialize communications, attempting to reconnect");

				try
				{
					WellKnownServiceTypeEntry[] wkst = RemotingConfiguration.GetRegisteredWellKnownServiceTypes();

					// "Activate" the NameService singleton.
					objectDirectory = (ObjectDirectory)Activator.GetObject(typeof(ObjectDirectory), wkst[0].ObjectUri);

					// Retreive the directory of messaging channels
					IChannelFactory channelFactory = (IChannelFactory)objectDirectory.Resolve("ChannelFactory");

					// Retreive the Messaging Service channels we want to listen to
					vehicleStateChannel = channelFactory.GetChannel("PositionChannel", ChannelMode.UdpMulticast);
					observedObstacleChannel = channelFactory.GetChannel("ObservedObstacleChannel", ChannelMode.UdpMulticast);
					observedVehicleChannel = channelFactory.GetChannel("ObservedVehicleChannel", ChannelMode.UdpMulticast);
					arbiterInformationChannel = channelFactory.GetChannel("ArbiterInformationChannel", ChannelMode.UdpMulticast);
					carModeChannel = channelFactory.GetChannel("CarMode", ChannelMode.UdpMulticast);
					fakeVehicleChannel = channelFactory.GetChannel("FakeVehicleChannel", ChannelMode.UdpMulticast);

					// Create a channel listeners and listen on wanted channels
					channelListener = new MessagingListener();
					vehicleStateChannelToken = vehicleStateChannel.Subscribe(channelListener);
					observedObstacleChannel.Subscribe(channelListener);
					observedVehicleChannel.Subscribe(channelListener);
					arbiterInformationChannelToken = arbiterInformationChannel.Subscribe(channelListener);
					carModeChannelToken = carModeChannel.Subscribe(channelListener);
					fakeVehicleChannelToken = fakeVehicleChannel.Subscribe(channelListener);

					// set that remoting has been successfully initialize
					this.RemotingInitialized = true;
					RemoraOutput.WriteLine("Successfully initialized communications");
				}
				catch (Exception e1)
				{
					RemoraOutput.WriteLine(e1.ToString() + "\n\n" + "Could not reinitialize nameservice communications");

					try
					{
						// Retreive the directory of messaging channels
						IChannelFactory channelFactory = (IChannelFactory)objectDirectory.Resolve("ChannelFactory");

						// Retreive the Messaging Service channels we want to listen to
						vehicleStateChannel = channelFactory.GetChannel("PositionChannel", ChannelMode.UdpMulticast);
						observedObstacleChannel = channelFactory.GetChannel("ObservedObstacleChannel", ChannelMode.UdpMulticast);
						observedVehicleChannel = channelFactory.GetChannel("ObservedVehicleChannel", ChannelMode.UdpMulticast);
						arbiterInformationChannel = channelFactory.GetChannel("ArbiterInformationChannel", ChannelMode.UdpMulticast);
						carModeChannel = channelFactory.GetChannel("CarMode", ChannelMode.UdpMulticast);
						fakeVehicleChannel = channelFactory.GetChannel("FakeVehicleChannel", ChannelMode.UdpMulticast);

						// Create a channel listeners and listen on wanted channels
						channelListener = new MessagingListener();
						vehicleStateChannelToken = vehicleStateChannel.Subscribe(channelListener);
						observedObstacleChannel.Subscribe(channelListener);
						observedVehicleChannel.Subscribe(channelListener);
						arbiterInformationChannelToken = arbiterInformationChannel.Subscribe(channelListener);
						carModeChannelToken = carModeChannel.Subscribe(channelListener);
						fakeVehicleChannelToken = fakeVehicleChannel.Subscribe(channelListener);

						// set that remoting has been successfully initialize
						this.RemotingInitialized = true;
						RemoraOutput.WriteLine("Successfully initialized communications");
					}
					catch (Exception e2)
					{
						RemoraOutput.WriteLine(e2.ToString() + "\n\n" + "Could not reinitialize messaging communications");
					}
				}
			}
		}

		#endregion

		#region Connection to Arbiter

		/// <summary>
		/// Connects to the Arbiter and Sets it up for Remote Control
		/// </summary>
		public void ConnectToArbiter()
		{
			RemoraOutput.WriteLine("Connecting to Arbiter");
			try
			{
				this.arbiterRemote = (ArbiterRemote)objectDirectory.Resolve("ArbiterRemote");
				RemoraOutput.WriteLine("Bind Successful...");
			}
			catch (Exception e)
			{
				RemoraOutput.WriteLine(e.ToString() + "\n\n" + "Error Binding Arbiter");
			}			
		}

		/// <summary>
		/// Sets the rndf and mdf network data
		/// </summary>
		public void RetrieveNetworkData()
		{
			RemoraOutput.WriteLine("Retrieving Rndf, Mdf");

			try
			{
				this.remora.SetRndf(this.arbiterRemote.Rndf());
				this.remora.SetMdf(this.arbiterRemote.Mdf());
				RemoraOutput.WriteLine("Set Rndf, Mdf");
			}
			catch (Exception e)
			{
				RemoraOutput.WriteLine(e.ToString() + "\n\n" + "Error Retrieving Rndf, Mdf");
			}
		}

		/// <summary>
		/// Stop the Arbiter dead and remove the Rndf, Mdf
		/// </summary>
		public void StopArbiter()
		{
			if (arbiterRemote != null)
			{
				try
				{
					this.arbiterRemote.Stop();
				}
				catch (Exception e)
				{
					RemoraOutput.WriteLine(e.ToString());
				}
			}
			else
			{
				RemoraOutput.WriteLine("Arbter Remote does not exist");
			}
		}

		public void RestartArbiter(RndfNetwork rndfNetwork, Mdf mdf)
		{
			if (arbiterRemote != null)
			{
				try
				{
					this.arbiterRemote.Restart(rndfNetwork, mdf, UrbanChallenge.Arbiter.ArbiterCommon.ArbiterMode.Debug);
				}
				catch (Exception e)
				{
					RemoraOutput.WriteLine(e.ToString());
				}
			}
			else
			{
				RemoraOutput.WriteLine("Arbter Remote does not exist");
			}
		}

		public RndfNetwork RetrieveRndf()
		{
			if (arbiterRemote != null)
			{
				try
				{
					return this.arbiterRemote.Rndf();
				}
				catch (Exception e)
				{
					RemoraOutput.WriteLine(e.ToString());
				}
			}
			else
			{
				RemoraOutput.WriteLine("Arbter Remote does not exist");
			}

			return null;
		}

		public Mdf RetrieveMdf()
		{
			if (arbiterRemote != null)
			{
				try
				{
					return this.arbiterRemote.Mdf();
				}
				catch (Exception e)
				{
					RemoraOutput.WriteLine(e.ToString());					
				}
			}
			else
			{
				RemoraOutput.WriteLine("Arbter Remote does not exist");
			}

			return null;
		}

		public bool PingArbiter()
		{
			if (arbiterRemote != null)
			{
				try
				{
					return this.arbiterRemote.Ping();
				}
				catch (Exception e)
				{
					RemoraOutput.WriteLine(e.ToString());
					return false;
				}
			}
			else
			{
				RemoraOutput.WriteLine("Arbter Remote does not exist");
				return false;
			}
		}

		public bool UpdateMdf(Mdf mdf)
		{
			if (arbiterRemote != null)
			{
				try
				{
					return this.arbiterRemote.SetMdf(mdf);
				}
				catch (Exception e)
				{
					RemoraOutput.WriteLine(e.ToString());
				}
			}
			else
			{
				RemoraOutput.WriteLine("Arbter Remote does not exist");
			}

			return false;
		}


		/// <summary>
		/// gets updated arbiter information
		/// </summary>
		public ArbiterInformation ArbiterInformation
		{
			get
			{
				return channelListener.ArbiterInformation;
			}
		}

		/// <summary>
		/// gets the listened operational car mode
		/// </summary>
		public CarMode CarMode
		{
			get
			{
				return channelListener.CarMode;
			}
		}

		#endregion

		public void ShutDown()
		{
			try
			{
				this.arbiterInformationChannel.Unsubscribe(this.arbiterInformationChannelToken);
				this.carModeChannel.Unsubscribe(this.carModeChannelToken);
				this.vehicleStateChannel.Unsubscribe(this.vehicleStateChannelToken);
			}
			catch (Exception e)
			{
				Debug.WriteLine(e.ToString());
			}
		}
	}
}
