using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.NameService;
using System.Runtime.Remoting;
using UrbanChallenge.MessagingService;
using Simulator;
using UrbanChallenge.Common.Utility;
using System.Threading;
using Simulator.Engine;
using UrbanChallenge.Arbiter.Core.Remote;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Arbiter.ArbiterMission;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common.Sensors.Vehicle;
using UrbanChallenge.Common.Sensors.Obstacle;
using UrbanChallenge.Common.Sensors.LocalRoadEstimate;
using SimOperationalService;
using UrbanChallenge.Common.Sensors;

namespace UrbanChallenge.Simulator.Client.Communications
{
	/// <summary>
	/// Communicates with outside world
	/// </summary>
	public class Communicator
	{
		#region Private members

		/// <summary>
		/// Remoting object directory
		/// </summary>
		private ObjectDirectory objectDirectory;

		/// <summary>
		/// Services
		/// </summary>
		private WellKnownServiceTypeEntry[] wkst;

		/// <summary>
		/// Client
		/// </summary>
		private SimulatorClient client;

		/// <summary>
		/// Listens for messages
		/// </summary>
		private MessagingListener messagingListener;

		#region Channels

		// simulation broadcast alive
		private IChannel simulationMessageChannel;
		private uint simulationMessageChannelToken;

		// vehicle state
		private IChannel vehicleStateChannel;

		// observed obstacles
		private IChannel observedObstacleChannel;

		// observed vehicles
		private IChannel observedVehicleChannel;

		// vehicle speed channel
		private IChannel vehicleSpeedChannel;

		// local road estimate channel
		private IChannel localRoadChannel;

		#endregion

		#endregion

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="simulation"></param>
		public Communicator(SimulatorClient client)
		{
			this.client = client;
			this.messagingListener = new MessagingListener(this);			
		}

		/// <summary>
		/// Configures remoting
		/// </summary>
		public void Configure()
		{
			try
			{
				// configure
				RemotingConfiguration.Configure("SimulatorClient.exe.config", false);
				wkst = RemotingConfiguration.GetRegisteredWellKnownServiceTypes();
			}
			catch (Exception e)
			{
				Console.WriteLine(DateTime.Now.ToString() + ": Error Configuring Remoting: " + e.ToString());
			}
		}

		/// <summary>
		/// Registers with the correct services
		/// </summary>
		public void Register()
		{
			try
			{
				// "Activate" the NameService singleton.
				objectDirectory = (ObjectDirectory)Activator.GetObject(typeof(ObjectDirectory), wkst[0].ObjectUri);

				// Retreive the directory of messaging channels
				IChannelFactory channelFactory = (IChannelFactory)objectDirectory.Resolve("ChannelFactory");

				// Retreive the Messaging Service channels we want to interact with
				if (simulationMessageChannel != null)
					simulationMessageChannel.Dispose();
				simulationMessageChannel = channelFactory.GetChannel("SimulationMessageChannel", ChannelMode.UdpMulticast);

				// Listen to channels we wish to listen to
				simulationMessageChannelToken = simulationMessageChannel.Subscribe(this.messagingListener);

				// Rebind the Simulator as the simulator remote facade server
				objectDirectory.Rebind(this.client, this.client.Name());

				// get vehicle state channel
				if (vehicleStateChannel != null)
					vehicleStateChannel.Dispose();
				vehicleStateChannel = channelFactory.GetChannel("ArbiterSceneEstimatorPositionChannel_" + SimulatorClient.MachineName, ChannelMode.Bytestream);

				// get observed obstacle channel
				if (observedObstacleChannel != null)
					observedObstacleChannel.Dispose();
				observedObstacleChannel = channelFactory.GetChannel("ObservedObstacleChannel_" + SimulatorClient.MachineName, ChannelMode.Bytestream);

				// get observed vehicle channel
				if (observedVehicleChannel != null)
					observedVehicleChannel.Dispose();
				observedVehicleChannel = channelFactory.GetChannel("ObservedVehicleChannel_" + SimulatorClient.MachineName, ChannelMode.Bytestream);

				// get vehicle speed channel
				if (vehicleSpeedChannel != null)
					vehicleSpeedChannel.Dispose();
				vehicleSpeedChannel = channelFactory.GetChannel("VehicleSpeedChannel_" + SimulatorClient.MachineName, ChannelMode.Bytestream);

				// get the local road estimate channel
				if (localRoadChannel != null)
					localRoadChannel.Dispose();
				localRoadChannel = channelFactory.GetChannel(LocalRoadEstimate.ChannelName + "_" + SimulatorClient.MachineName, ChannelMode.Bytestream);

				// Notify user of success
				Console.WriteLine(DateTime.Now.ToString() + ": Connection to Name Service and Registration of Simulation Client Successful");
			}
			catch (Exception e)
			{
				// notify
				Console.WriteLine(DateTime.Now.ToString() + ": Error Registering With Remoting Services: " + e.ToString());
			}
		}

		/// <summary>
		/// Attempts to register with the simulation
		/// </summary>
		public void AttemptSimulationConnection()
		{
			// notify
			Console.WriteLine(DateTime.Now.ToString() + ": Attempting Connection To Simulation Server");

			try
			{
				// "Activate" the NameService singleton.
				objectDirectory = (ObjectDirectory)Activator.GetObject(typeof(ObjectDirectory), wkst[0].ObjectUri);

				// Resolve the SimulationServer as the simulator remote facade server
				this.client.SimulationServer = (SimulatorFacade)objectDirectory.Resolve("SimulationServer");

				// create the dynamics sim facade
				this.client.DynamicsVehicle = new DynamicsSimVehicle(new SimVehicleState(), this.objectDirectory);

				// register ourselves
				bool success = this.client.SimulationServer.Register(this.client.Name());

				// notify
				if(success)
					Console.WriteLine(DateTime.Now.ToString() + ": Attempt Succeeded");
				else
					Console.WriteLine(DateTime.Now.ToString() + ": Registration Failed in Simulation");
			}
			catch (Exception e)
			{
				// notify
				Console.WriteLine(DateTime.Now.ToString() + ": Attempt Failed" + "\n" + e.ToString());
			}			
		}

		/// <summary>
		/// Pings the simulation for response
		/// </summary>
		public bool PingSimulation()
		{
			try
			{
				// ping
				bool c = this.client.SimulationServer.Ping();

				// notify
				Console.WriteLine(DateTime.Now.ToString() + ": Ping Successful");

				// return 
				return true;
			}
			catch (Exception)
			{
				// notify
				Console.WriteLine(DateTime.Now.ToString() + ": Ping Failed... Attempting To Reconnect");

				// return 
				return false;
			}
		}

		/// <summary>
		/// Whaqt to do when lose simulator
		/// </summary>
		public void SimulatorLost()
		{
			this.client.SimulationServer = null;
			this.client.ClientVehicle = null;

			Console.WriteLine("");
			Console.WriteLine(DateTime.Now + " Simulation lost");
			Console.WriteLine("");
			Console.Write("SimClient > ");
		}

		/// <summary>
		/// Runs to maintain the simulation and clients
		/// </summary>
		private void Maintenance()
		{
			while (true)
			{
				// ping sim
				if (this.client.SimulationServer != null)
				{
					try
					{
						this.client.SimulationServer.Ping();							
					}
					catch (Exception)
					{
						Console.WriteLine(DateTime.Now.ToString() + ": Ping of Simulation Server Unsuccsessful");
						this.SimulatorLost();
					}
				}

				// rest
				Thread.Sleep(5000);
			}
		}

		/// <summary>
		/// run maintanance over the clients
		/// </summary>
		public void RunMaintenance()
		{
			Thread t = new Thread(Maintenance);
			t.IsBackground = true;
			t.Priority = ThreadPriority.Lowest;
			t.Start();
		}

		/// <summary>
		/// Starts the vehicle
		/// </summary>
		/// <param name="arbiterRoadNetwork"></param>
		/// <param name="arbiterMissionDescription"></param>
		/// <returns></returns>
		public bool StartupVehicle(ArbiterRoadNetwork arbiterRoadNetwork, ArbiterMissionDescription arbiterMissionDescription)
		{
			try
			{
				// get arbiter
				this.client.ClientArbiter = (ArbiterAdvancedRemote)this.objectDirectory.Resolve("ArbiterAdvancedRemote_" + SimulatorClient.MachineName);

				// set road and mission and spool up ai to wait for initial data stream
				this.client.ClientArbiter.JumpstartArbiter(arbiterRoadNetwork, arbiterMissionDescription);

				// success!
				return true;
			}
			catch (Exception e)
			{
				Console.WriteLine("Error spooling up ai: \n" + e.ToString());
				return false;
			}
		}

		public bool CreateSimpleVehicle() {
			try {
				this.client.DynamicsVehicle = new DynamicsSimVehicle(new SimVehicleState(), objectDirectory);
				return true;
			}
			catch (Exception e) {
				Console.WriteLine("Error creating sim: \n" + e.ToString());
				return false;
			}
		}

		/// <summary>
		/// Update sensor states
		/// </summary>
		/// <param name="vehicleState"></param>
		/// <param name="vehicles"></param>
		/// <param name="obstacles"></param>
		public void Update(VehicleState vehicleState, SceneEstimatorTrackedClusterCollection vehicles, SceneEstimatorUntrackedClusterCollection obstacles, double speed, LocalRoadEstimate lre, PathRoadModel prm)
		{
			this.vehicleStateChannel.PublishUnreliably(vehicleState);
			this.observedVehicleChannel.PublishUnreliably(vehicles);
			this.observedObstacleChannel.PublishUnreliably(obstacles);
			this.vehicleSpeedChannel.PublishUnreliably(speed);
			this.localRoadChannel.PublishUnreliably(lre);
			if (prm != null) {
				this.localRoadChannel.PublishUnreliably(prm);
			}
		}
	}
}
