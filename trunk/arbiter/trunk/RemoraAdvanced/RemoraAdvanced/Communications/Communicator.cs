using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using UrbanChallenge.MessagingService;
using UrbanChallenge.OperationalService;
using UrbanChallenge.NameService;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common.Sensors.Vehicle;
using UrbanChallenge.Common.Sensors.Obstacle;
using RemoraAdvanced.Common;
using System.Threading;
using Simulator;
using System.Runtime.Remoting;
using UrbanChallenge.Arbiter.Core.Remote;
using UrbanChallenge.Common.Sensors;

namespace RemoraAdvanced.Communications
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
		/// Operational layer
		/// </summary>
		private OperationalFacade operationalFacade;

		/// <summary>
		/// Facade of the simulator
		/// </summary>
		private SimulatorFacade simulatorFacade;

		/// <summary>
		/// Remote ai
		/// </summary>
		private ArbiterAdvancedRemote arbiterRemote;

		/// <summary>
		/// listener for messaging
		/// </summary>
		private MessagingListener messagingListener;

		/// <summary>
		/// Channel factory holding all channels
		/// </summary>
		private IChannelFactory channelFactory;

		/// <summary>
		/// Watchdog for comms
		/// </summary>
		private Thread commWatchdog;
		private bool runCommsWatchdog = true;

		/// <summary>
		/// car mode from operational (set by watchdog)
		/// </summary>
		private CarMode carMode;

		/// <summary>
		/// Link to main
		/// </summary>
		private Remora remora;

		#endregion

		#region Channels

		// vehicle state
		private IChannel vehicleStateChannel;
		private uint vehicleStateChannelToken;

		// message channel
		private IChannel arbiterOutputChannel;
		private uint arbiterOutputChannelToken;

		// arebiter information channel
		private IChannel arbiterInformationChannel;
		private uint arbiterInformationChannelToken;

		// observed obstacles
		private IChannel observedObstacleChannel;
		private uint observedObstacleChannelToken;

		// observed vehicles
		private IChannel observedVehicleChannel;
		private uint observedVehicleChannelToken;

		// speed
		private IChannel vehicleSpeedChannel;
		private uint vehicleSpeedChannelToken;

		// side sicks
		private IChannel sideObstacleChannel;
		private uint sideObstacleChannelToken;

		#endregion

		#region Public Members

		/// <summary>
		/// Notifies whether the communications are ready
		/// </summary>
		public bool CommunicationsReady = false;

		/// <summary>
		/// Machine name updates
		/// </summary>
		public event MachineEventHandler ManchineNameUpdate;
		//declaring the event handler delegate
		public delegate void MachineEventHandler(object source, Dictionary<string, int> machineNames);

		#endregion

		#region Constructor

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="arbiterCore"></param>
		public Communicator(Remora remora)
		{
			// messaging
			this.messagingListener = new MessagingListener(remora);
			this.remora = remora;
		}

		#endregion

		#region Fields

		/// <summary>
		/// Provides suffix to remoting name if in sim
		/// </summary>
		public string RemotingSuffix
		{
			get
			{
				return global::RemoraAdvanced.Properties.Settings.Default.SimMode ? "_" + this.remora.clientHandler.Current : "";
			}
		}

		/// <summary>
		/// Remota ai
		/// </summary>
		public ArbiterAdvancedRemote ArbiterRemote
		{
			get { return arbiterRemote; }
		}

		#endregion

		#region Functions

		/// <summary>
		/// start communications
		/// </summary>
		public void BeginCommunications()
		{
			// start comms
			commWatchdog = new Thread(this.Watchdog);
			commWatchdog.IsBackground = true;
			commWatchdog.Priority = ThreadPriority.Normal;
			commWatchdog.Start();
		}

		/// <summary>
		/// Configures remoting
		/// </summary>
		public void Configure()
		{
			// configure
			RemotingConfiguration.Configure("RemoraAdvanced.exe.config", false);
			wkst = RemotingConfiguration.GetRegisteredWellKnownServiceTypes();
		}

		/// <summary>
		/// Registers with the correct services
		/// </summary>
		public void Register()
		{
			// "Activate" the NameService singleton.
			objectDirectory = (ObjectDirectory)Activator.GetObject(typeof(ObjectDirectory), wkst[0].ObjectUri);

			// Retreive the directory of messaging channels
			channelFactory = (IChannelFactory)objectDirectory.Resolve("ChannelFactory");

			// Notify user of success
			RemoraOutput.WriteLine("Connection to Name Service Successful", OutputType.Remora);

			// get simulation if supposed to
			if (global::RemoraAdvanced.Properties.Settings.Default.SimMode)
			{
				this.simulatorFacade = (SimulatorFacade)objectDirectory.Resolve("SimulationServer");

				// Notify user of success
				RemoraOutput.WriteLine("Connection to Simulation Service Successful", OutputType.Remora);
			}
		}

		public void ResgisterWithClient()
		{
			try
			{
				// get vehicle state channel
				vehicleStateChannel = channelFactory.GetChannel("ArbiterSceneEstimatorPositionChannel" + this.RemotingSuffix, ChannelMode.UdpMulticast);
				vehicleStateChannelToken = vehicleStateChannel.Subscribe(messagingListener);

				// get observed obstacle channel
				observedObstacleChannel = channelFactory.GetChannel("ObservedObstacleChannel" + this.RemotingSuffix, ChannelMode.UdpMulticast);
				observedObstacleChannelToken = observedObstacleChannel.Subscribe(messagingListener);

				// get observed vehicle channel
				observedVehicleChannel = channelFactory.GetChannel("ObservedVehicleChannel" + this.RemotingSuffix, ChannelMode.UdpMulticast);
				observedVehicleChannelToken = observedVehicleChannel.Subscribe(messagingListener);

				// get vehicle speed channel
				vehicleSpeedChannel = channelFactory.GetChannel("VehicleSpeedChannel" + this.RemotingSuffix, ChannelMode.UdpMulticast);
				vehicleSpeedChannelToken = vehicleSpeedChannel.Subscribe(messagingListener);

				// get output channel
				arbiterOutputChannel = channelFactory.GetChannel("ArbiterOutputChannel" + this.RemotingSuffix, ChannelMode.UdpMulticast);
				arbiterOutputChannelToken = arbiterOutputChannel.Subscribe(messagingListener);

				// get information channel
				arbiterInformationChannel = channelFactory.GetChannel("ArbiterInformationChannel" + this.RemotingSuffix, ChannelMode.UdpMulticast);
				arbiterInformationChannelToken = arbiterInformationChannel.Subscribe(messagingListener);

				// get side obstacle channel
				sideObstacleChannel = channelFactory.GetChannel("SideObstacleChannel" + this.RemotingSuffix, ChannelMode.UdpMulticast);
				sideObstacleChannelToken = sideObstacleChannel.Subscribe(messagingListener);

				// get ai
				this.arbiterRemote = (ArbiterAdvancedRemote)objectDirectory.Resolve("ArbiterAdvancedRemote" + this.RemotingSuffix);

				// get the operational layer
				this.operationalFacade = (OperationalFacade)objectDirectory.Resolve("OperationalService" + this.RemotingSuffix);

				// register
				RemoraOutput.WriteLine("Resgistered To Client with suffix: " + this.RemotingSuffix, OutputType.Remora);
			}
			catch (Exception e)
			{
				RemoraOutput.WriteLine("Error registering with client: " + e.ToString(), OutputType.Remora);
			}
		}

		/// <summary>
		/// Keeps watch over communications components and determines if any need to be restarted
		/// </summary>
		public void Watchdog()
		{
			// always loop
			while (runCommsWatchdog)
			{
				try
				{
					#region Check for communications readiness

					if (!this.CommunicationsReady)
					{
						try
						{
							// configure
							this.Configure();
						}
						catch (Exception e)
						{
							// notify
							RemoraOutput.WriteLine("Error in communications watchdog, configuration", OutputType.Remora);
							Console.WriteLine(e.ToString());
						}
					}

					if (!this.CommunicationsReady)
					{
						try
						{
							// make sure nothing else registered
							this.Shutdown();

							// register services
							this.Register();
						}
						catch (Exception e)
						{
							// notify
							RemoraOutput.WriteLine("Error in communications watchdog, registration", OutputType.Remora);
							Console.WriteLine(e.ToString());
						}
					}

					#endregion

					#region Get Available Machines if in sim

					if (global::RemoraAdvanced.Properties.Settings.Default.SimMode)
					{
						try
						{
							// update the machine names
							Dictionary<string, int> machineNames = this.simulatorFacade.GetClientMachines();
							this.ManchineNameUpdate(this, machineNames);

							// set comms ready as true given success
							if (!this.CommunicationsReady)
								this.CommunicationsReady = true;
						}
						catch (Exception e)
						{
							// notify
							RemoraOutput.WriteLine("Error retreiving client machines from simulation in watchdog, attempting to reconnect", OutputType.Remora);
							Console.WriteLine(e.ToString());
							this.ManchineNameUpdate(this, new Dictionary<string, int>());

							// set comms ready as false
							this.CommunicationsReady = false;
						}
					}
					else
					{
						if (!this.CommunicationsReady)
						{
							try
							{
								this.ResgisterWithClient();

								// notify
								if (RemoraAdvanced.Properties.Settings.Default.LogMode || arbiterRemote.Ping())
								{
									RemoraOutput.WriteLine("Registered with default non-sim client", OutputType.Remora);
									this.CommunicationsReady = true;
								}
							}
							catch (Exception ex)
							{
								RemoraOutput.WriteLine("Error connecting to default non-sim client in watchdog, attempting to reconnect", OutputType.Remora);
								Console.WriteLine(ex.ToString());
								this.CommunicationsReady = false;
							}
						}
						else
						{
							// try and ping the ai
							try
							{
								if(!RemoraAdvanced.Properties.Settings.Default.LogMode)
									this.arbiterRemote.Ping();
							}
							catch (Exception e)
							{
								RemoraOutput.WriteLine("Error pinging default ai", OutputType.Remora);
								this.CommunicationsReady = false;
								Console.WriteLine(e.ToString());
							}
						}
					}

					#endregion

					#region Get Car Mode (Acts as Operational Ping and know then Comms Ready)

					try
					{
						// update the car mode
						if(this.operationalFacade != null)
							this.carMode = this.operationalFacade.GetCarMode();
					}
					catch (Exception e)
					{
						// notify						
						Console.WriteLine(e.ToString());
					}

					#endregion
				}
				catch (Exception e)
				{
					// notify
					RemoraOutput.WriteLine("Error in comms watchdog: " + e.ToString(), OutputType.Remora);
					Console.WriteLine(e.ToString());
				}

				// wait for cycle time
				Thread.Sleep(5000);
			}
		}

		/// <summary>
		/// Gets vehicle state
		/// </summary>
		/// <returns></returns>
		public VehicleState GetVehicleState()
		{
			// vehicle state from messaging listener
			return messagingListener.VehicleState;
		}

		/// <summary>
		/// Gets observed vehicles
		/// </summary>
		/// <returns></returns>
		public SceneEstimatorTrackedClusterCollection GetObservedVehicles()
		{
			// vehicles from messaging listener
			return messagingListener.ObservedVehicles;
		}

		public SideObstacles GetSideObstacles(SideObstacleSide side)
		{
			return this.messagingListener.SideSickObstacles(side);
		}

		/// <summary>
		/// Gets observed obstacles
		/// </summary>
		/// <returns></returns>
		public SceneEstimatorUntrackedClusterCollection GetObservedObstacles()
		{
			// obstacles from messaging listener
			return messagingListener.ObservedObstacles;
		}

		/// <summary>
		/// Gets speed of the vehicle
		/// </summary>
		/// <returns></returns>
		public double? GetVehicleSpeed()
		{
			// returnr peed from messaging
			return this.messagingListener.VehicleSpeed;
		}

		/// <summary>
		/// Gets car mode
		/// </summary>
		/// <returns></returns>
		public CarMode GetCarMode()
		{
			// return car mode
			return this.carMode;
		}

		/// <summary>
		/// Shuts down the communicator and unsubscribes from channels, whatnot
		/// </summary>
		public void Shutdown()
		{
			try
			{
				if (vehicleStateChannel != null)
				{
					// unsubscribe from channels
					vehicleStateChannel.Unsubscribe(vehicleStateChannelToken);
					observedObstacleChannel.Unsubscribe(observedObstacleChannelToken);
					observedVehicleChannel.Unsubscribe(observedVehicleChannelToken);
					vehicleSpeedChannel.Unsubscribe(vehicleSpeedChannelToken);
					arbiterOutputChannel.Unsubscribe(arbiterOutputChannelToken);
					arbiterInformationChannel.Unsubscribe(arbiterInformationChannelToken);
					sideObstacleChannel.Unsubscribe(sideObstacleChannelToken);
					this.arbiterRemote = null;
					this.operationalFacade = null;

					// notify
					RemoraOutput.WriteLine("Unsubscribed from channels", OutputType.Remora);
				}
			}
			catch (Exception e)
			{
				// notify
				RemoraOutput.WriteLine("Error in shutting down registered channels", OutputType.Remora);
				Console.WriteLine(e.ToString());
			}
		}

		/// <summary>
		/// Restarts standard comms
		/// </summary>
		public void Restart()
		{
			try
			{
				this.Register();
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}

			// start watchdog
			this.runCommsWatchdog = true;

			// start comms watchdog
			commWatchdog = new Thread(this.Watchdog);
			commWatchdog.IsBackground = true;
			commWatchdog.Priority = ThreadPriority.Normal;
			commWatchdog.Start();
		}

		/// <summary>
		/// Get operational's current behavior
		/// </summary>
		public Type GetCurrentOperationalBehavior()
		{
			return operationalFacade.GetCurrentBehaviorType();
		}

		#endregion
	}
}
