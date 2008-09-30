using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Simulator.Client;
using System.Runtime.Remoting;
using UrbanChallenge.NameService;
using UrbanChallenge.MessagingService;
using System.Threading;
using UrbanChallenge.Common.Utility;
using UrbanChallenge.OperationalService;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common.Sensors.Vehicle;
using UrbanChallenge.Common.Sensors.Obstacle;
using UrbanChallenge.Common;
using System.Runtime.Remoting.Messaging;
using UrbanChallenge.Arbiter.Core.Common.Arbiter;
using UrbanChallenge.Behaviors.CompletionReport;
using UrbanChallenge.Arbiter.Core.CoreIntelligence;
using System.Diagnostics;
using UrbanChallenge.Common.Sensors;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Behavioral;

namespace UrbanChallenge.Arbiter.Core.Communications
{
	/// <summary>
	/// Handles most communications functions for the ai
	/// </summary>
	[Serializable]
	public class Communicator : OperationalListener
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
		/// Core of the arbiter intelligence
		/// </summary>
		private ArbiterCore arbiterCore;

		/// <summary>
		/// Operational layer
		/// </summary>
		private OperationalFacade operationalFacade;

		/// <summary>
		/// Test facade for operational
		/// </summary>
		private OperationalTestComponentFacade operationalTestFacade;

		/// <summary>
		/// listener for messaging
		/// </summary>
		private MessagingListener messagingListener;

		/// <summary>
		/// Channel factory holding all channels
		/// </summary>
		private IChannelFactory channelFactory;

		/// <summary>
		/// Recent completion reports
		/// </summary>
		private List<KeyValuePair<CompletionReport, DateTime>> recentReports;
		
		/// <summary>
		/// Current ai information
		/// </summary>
		private ArbiterInformation currentInformation;

		#endregion

		#region Channels

		// vehicle state
		private IChannel vehicleStateChannel;
		private uint vehicleStateChannelToken;

		// car mode from operational (set by watchdog)
		private CarMode carMode;

		// message channel
		private IChannel arbiterOutputChannel;

		// information channel
		private IChannel arbiterInformationChannel;		

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
		/// Handler of arbiter blockages
		/// </summary>
		public BlockageHandler ArbiterBlockageHandler;

		/// <summary>
		/// Notifies whether the communications are ready
		/// </summary>
		public bool CommunicationsReady = false;

		#endregion

		#region Constructor

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="arbiterCore"></param>
		public Communicator(ArbiterCore arbiterCore)
		{
			// set core
			this.arbiterCore = arbiterCore;

			// messaging
			this.messagingListener = new MessagingListener(this.RemotingSuffix);

			// initialize completion list
			this.recentReports = new List<KeyValuePair<CompletionReport, DateTime>>();

			// start the test facade connection
			this.testAsyncManagerThread = new Thread(this.AsynchronousExecutionManager);
			this.testAsyncManagerThread.IsBackground = true;
			this.testAsyncManagerThread.Start();
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
        if (global::UrbanChallenge.Arbiter.Core.ArbiterSettings.Default.SimMode)
        {
          if (global::UrbanChallenge.Arbiter.Core.ArbiterSettings.Default.ShouldSpoofComputerName)
						return "_" + global::UrbanChallenge.Arbiter.Core.ArbiterSettings.Default.SpoofComputerName;
          else
						return "_" + Environment.MachineName;
        }
        else
					return "";
			}
		}

		#endregion

		#region Functions

		/// <summary>
		/// start communications
		/// </summary>
		public void BeginCommunications()
		{
			// start comms
			Thread commWatchdog = new Thread(this.Watchdog);
			commWatchdog.IsBackground = true;
			commWatchdog.Priority = ThreadPriority.Normal;
			commWatchdog.Start();

			Thread informationThread = new Thread(ArbiterInformationUpdateThread);
			informationThread.IsBackground = true;
			informationThread.Priority = ThreadPriority.Normal;
			informationThread.Start();
		}

		/// <summary>
		/// Configures remoting
		/// </summary>
		public void Configure()
		{
			// configure
			RemotingConfiguration.Configure("ArbiterAdvanced.exe.config", false);
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

			// register the core as implementing the arbiter advanced remote facade
			objectDirectory.Rebind(this.arbiterCore, "ArbiterAdvancedRemote" + this.RemotingSuffix);

			// shutdown old channels
			this.Shutdown();

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

			// get side obstacle channel
			sideObstacleChannel = channelFactory.GetChannel("SideObstacleChannel" + this.RemotingSuffix, ChannelMode.UdpMulticast);
			sideObstacleChannelToken = sideObstacleChannel.Subscribe(messagingListener);

			// get output channel
			if (arbiterOutputChannel != null)
				arbiterOutputChannel.Dispose();
			arbiterOutputChannel = channelFactory.GetChannel("ArbiterOutputChannel" + this.RemotingSuffix, ChannelMode.UdpMulticast);

			// get information channel
			if (arbiterInformationChannel != null)
				arbiterInformationChannel.Dispose();
			arbiterInformationChannel = channelFactory.GetChannel("ArbiterInformationChannel" + this.RemotingSuffix, ChannelMode.UdpMulticast);

			// get the operational layer
			this.operationalFacade = (OperationalFacade)objectDirectory.Resolve("OperationalService" + this.RemotingSuffix);
			this.operationalFacade.RegisterListener(this);
			this.SendProjection();

			// try connecting to the test facade
			this.TryOperationalTestFacadeConnect();
		}

		/// <summary>
		/// Keeps watch over communications components and determines if any need to be restarted
		/// </summary>
		public void Watchdog()
		{			
			// always loop
			while (true)
			{
				try
				{
					if (Arbiter.Core.ArbiterSettings.Default.IgnoreVehicles)
					{
						ArbiterOutput.Output("Ignoring Tracked Clusters");
					}

					#region Update Completion reports with 3 Sec Timeout

					lock (this.recentReports)
					{
						List<KeyValuePair<CompletionReport, DateTime>> crs = new List<KeyValuePair<CompletionReport, DateTime>>();
						foreach (KeyValuePair<CompletionReport, DateTime> cr in this.recentReports)
						{							
							TimeSpan diff = DateTime.Now.Subtract(cr.Value);
							ArbiterOutput.WriteToLog("Comm line 263 test diff.ToString: " + diff.ToString());

							if (diff.Seconds < 2 && cr.Value.Date.Equals(DateTime.Now.Date))
							{
								crs.Add(cr);
							}
							else
							{
								ArbiterOutput.WriteToLog("Removed Completion Report: " + cr.Key.BehaviorType.ToString() + ", " + cr.Key.Result.ToString());
							}
						}
						this.recentReports = crs;
					}

					#endregion

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
							ArbiterOutput.Output("Error in communications watchdog, configuration");
							ArbiterOutput.WriteToLog(e.ToString());
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
							ArbiterOutput.Output("Error in communications watchdog, registration");
							ArbiterOutput.WriteToLog(e.ToString());
						}
					}

					#endregion

					#region Get Car Mode (Acts as Operational Ping and know then Comms Ready)

					try
					{
						if (this.operationalFacade != null)
						{
							// update the car mode
							this.carMode = this.operationalFacade.GetCarMode();

							// set comms ready as true given success
							if (!this.CommunicationsReady)
								this.CommunicationsReady = true;
						}
						else
						{
							this.CommunicationsReady = false;
						}
					}
					catch (Exception e)
					{
						// notify
						ArbiterOutput.Output("Error retreiving car mode from operational in watchdog, attempting to reconnect");

						// log
						ArbiterOutput.WriteToLog(e.ToString());

						// set comms ready as false
						this.CommunicationsReady = false;						
					}

					#endregion					
				}
				catch (Exception e)
				{
					ArbiterOutput.Output("Error in communications watchdog", e);
				}

				// wait for cycle time
				Thread.Sleep(1000);
			}
		}

		/// <summary>
		/// Execute
		/// </summary>
		/// <param name="behavior"></param>
		/// <remarks>We can catch exceptions here as the comms watchdog will know also</remarks>
		[OneWay]
		public void Execute(Behavior behavior)
		{
			try
			{
				if (!(behavior is NullBehavior))
				{
					Stopwatch bsw = new Stopwatch();
					bsw.Reset();
					bsw.Start();

					// execute operational behavior
					operationalFacade.ExecuteBehavior(behavior);

					bsw.Stop();
					if (bsw.ElapsedMilliseconds > 50)
					{
						ArbiterOutput.Output("Delay: Operational Execution Time: " + bsw.ElapsedMilliseconds.ToString());
					}
				}
			}
			catch (Exception e)
			{
				// Notify
				ArbiterOutput.Output("Errror executing operational behavior: " + e.ToString());
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

		public SideObstacles GetSideObstacles(SideObstacleSide side)
		{
			return this.messagingListener.SideSickObstacles(side);
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
					sideObstacleChannel.Unsubscribe(sideObstacleChannelToken);
				}

				// notify
				ArbiterOutput.Output("Unsubscribed from channels");
			}
			catch (Exception e)
			{
				// notify
				ArbiterOutput.Output("Error in shutting down registered channels");
				ArbiterOutput.WriteToLog(e.ToString());
			}
		}

		/// <summary>
		/// Get operational's current behavior
		/// </summary>
		public Type GetCurrentOperationalBehavior()
		{
			return operationalFacade.GetCurrentBehaviorType();
		}

		/// <summary>
		/// Sends message to arebiter output listeners
		/// </summary>
		/// <param name="s"></param>
		[OneWay]
		public void SendOutput(string s)
		{
			if(this.arbiterOutputChannel != null)
				this.arbiterOutputChannel.PublishUnreliably(s);
		}

		/// <summary>
		/// Updates information in remote viewers
		/// </summary>
		/// <param name="arbiterInformation"></param>
		[OneWay]
		public void UpdateInformation(ArbiterInformation arbiterInformation)
		{
			if (arbiterInformation != null)
			{
				//Stopwatch stopwatch3 = new Stopwatch();

				//stopwatch3.Reset();
				//stopwatch3.Start();
				ArbiterOutput.WriteToLog(arbiterInformation.LogString());
				//stopwatch3.Stop();
				//Console.WriteLine("SW 3: " + stopwatch3.ElapsedMilliseconds.ToString());

				//stopwatch3.Reset();
				//stopwatch3.Start();
				this.currentInformation = arbiterInformation;
				//stopwatch3.Stop();
				//Console.WriteLine("SW 4: " + stopwatch3.ElapsedMilliseconds.ToString());
			}
		}

		#endregion

		/// <summary>
		/// Thread that updates the arbiter information
		/// </summary>
		public void ArbiterInformationUpdateThread()
		{
			while (true)
			{
				if (this.currentInformation != null)
				{
					try
					{
						this.arbiterInformationChannel.PublishUnreliably(this.currentInformation);
					}
					catch(Exception)
					{
					}
				}

				Thread.Sleep(100);
			}
		}
	
		/// <summary>
		/// Operational competes a behavior
		/// </summary>
		/// <param name="report"></param>
		public override void OnCompletionReport(CompletionReport report)
		{
			try
			{
				if (report.Result == CompletionResult.Success)
					ArbiterOutput.OutputNoLog("Received Completion Report: " + report.BehaviorType.ToString() + ", Result: " + report.Result.ToString());
				else if (this.ArbiterBlockageHandler != null && report is TrajectoryBlockedReport)
					this.ArbiterBlockageHandler.OnBlockageReport((TrajectoryBlockedReport)report);

				lock (this.recentReports)
				{
					this.recentReports = new List<KeyValuePair<CompletionReport, DateTime>>(this.recentReports.ToArray());
					this.recentReports.Add(new KeyValuePair<CompletionReport, DateTime>(report, DateTime.Now));
				}
			}
			catch (Exception e)
			{
				try
				{
					ArbiterOutput.OutputNoLog("Error in completion report");
					ArbiterOutput.WriteToLog(e.ToString());
				}
				catch (Exception)
				{
				}
			}
		}

		/// <summary>
		/// Checks if we have recently completed a behavior of a certain type
		/// </summary>
		/// <param name="t"></param>
		public bool HasCompleted(Type t)
		{
			try
			{
				foreach (KeyValuePair<CompletionReport, DateTime> cr in this.recentReports)
				{
					if (cr.Key.BehaviorType.Equals(t) && cr.Key.Result == CompletionResult.Success)
					{
						return true;
					}
				}
			}
			catch (Exception) { }

			return false;
		}

		/// <summary>
		/// Clear all old completion reports
		/// </summary>
		public void ClearCompletionReports()
		{
			try
			{
				this.recentReports = new List<KeyValuePair<CompletionReport, DateTime>>();
				ArbiterOutput.Output("Cleared Completion Reports");
			}
			catch (Exception) { }
		}

		/// <summary>
		/// make sure this doesn't get lost
		/// </summary>
		/// <returns></returns>
		public override object InitializeLifetimeService()
		{
			return null;
		}

		/// <summary>
		/// Sends road network projection to the operational layer
		/// </summary>
		public void TrySendProjection()
		{
			ArbiterOutput.Output("Attempting to set Operational Road Network");

			if (CoreCommon.RoadNetwork != null)
			{
				if (this.operationalFacade != null)
				{
					try
					{						
						this.operationalFacade.SetRoadNetwork(CoreCommon.RoadNetwork);
					}
					catch (Exception ex)
					{
						ArbiterOutput.Output("Error setting road network in operational, sending projection");
						Console.WriteLine(ex.ToString());
						try
						{
							this.operationalFacade.SetProjection(CoreCommon.RoadNetwork.PlanarProjection);
						}
						catch (Exception e)
						{
							Console.WriteLine("error setting projection in op");
						}
						
					}
				}
				else
				{
					ArbiterOutput.Output("Need to connect to operational before can send road network");
				}
			}
			else
			{
				ArbiterOutput.Output("Road network cannot be null to send to operational");
			}

			ArbiterOutput.Output("Leaving TrySendOperationalProjection");
		}

		/// <summary>
		/// Sends road network projection to the operational layer
		/// </summary>
		public void SendProjection()
		{
			try
			{
				if (CoreCommon.RoadNetwork != null)
				{
					if (this.operationalFacade != null)
					{
						this.testComponentNeedsRoadNetwork = true;
						this.operationalFacade.SetRoadNetwork(CoreCommon.RoadNetwork);
					}
					else
					{
						ArbiterOutput.Output("Need to connect to operational before can send road network");
					}
				}
				else
				{
					ArbiterOutput.Output("Road network cannot be null to send to operational");
				}
			}
			catch (Exception e)
			{
				ArbiterOutput.Output("Error setting road network in operational, sending projection");
				this.operationalFacade.SetProjection(CoreCommon.RoadNetwork.PlanarProjection);
				Console.WriteLine(e.ToString());
			}
		}

		/// <summary>
		/// Try to reconnect to stuff
		/// </summary>
		public void TryReconnect()
		{
			this.CommunicationsReady = false;
			ArbiterOutput.Output("Set comms ready flag to false");
		}

		#region Operational Test Component Interface

		private Queue<Behavior> testAsyncExecuteQueue;
		private Thread testAsyncManagerThread;
		private Thread testBehaviorExecutionThread;
		private Stopwatch testBehaviorExecutionStopwatch;
		private List<TestBehaviorCompletion> testModeRecentCompleted;
		private CompletionReport testCompletionReport;
		private Behavior testCurrentTest;
		private bool testComponentExists = false;
		private bool testComponentNeedsRoadNetwork = true;

		/// <summary>
		/// Execute the currently tested behavior
		/// </summary>
		private void TestBehaviorExecutionThread()
		{
			if(this.testCurrentTest != null)
				this.TestExecute(this.testCurrentTest, out testCompletionReport);
		}

		/// <summary>
		/// Test executue a behavior
		/// </summary>
		/// <param name="b"></param>
		/// <param name="completionReport"></param>
		public bool TestExecute(Behavior b, out CompletionReport completionReport)
		{
			if (this.operationalTestFacade != null)
			{
				try
				{
					b.TimeStamp = CoreCommon.Communications.GetVehicleState().Timestamp;
					completionReport = this.operationalTestFacade.TestExecuteBehavior(b);
					ArbiterOutput.OutputNoLog("test execute: " + b.ShortBehaviorInformation() + " completion report: " + completionReport.GetType().ToString());
					return completionReport.Result == CompletionResult.Success;
				}
				catch (Exception e)
				{
					completionReport = new SuccessCompletionReport(b.GetType());
					ArbiterOutput.OutputNoLog("test execute: " + b.ShortBehaviorInformation() + " encountered error: " + e.ToString());
					this.TryOperationalTestFacadeConnect();
					return true;
				}
			}
			else
			{
				completionReport = new SuccessCompletionReport(b.GetType());
				ArbiterOutput.OutputNoLog("test execute: " + b.ShortBehaviorInformation() + " encountered error: operational test facade does not exist");
				this.TryOperationalTestFacadeConnect();
				return true;
			}
		}

		/// <summary>
		/// Try to connect to the operational facade
		/// </summary>
		public void TryOperationalTestFacadeConnect()
		{
			if (global::UrbanChallenge.Arbiter.Core.ArbiterSettings.Default.UseTestOperational)
			{
				try
				{
					// check if test facade ok
					bool testFacadeGood = this.operationalTestFacade != null;
					if (this.operationalTestFacade != null)
					{
						try
						{
							this.operationalTestFacade.Ping();
							this.testComponentExists = true;
						}
						catch (Exception)
						{
							testFacadeGood = false;
						}
					}

					// set the operational test facade if not ok
					if (!testFacadeGood)
					{
						// get the operational layer
						this.operationalTestFacade = (OperationalTestComponentFacade)objectDirectory.Resolve("OperationalTestComponentService" + this.RemotingSuffix);
						this.testComponentExists = true;
					}

					// try to send road network
					if (CoreCommon.RoadNetwork != null)
					{
						this.operationalTestFacade.SetRoadNetwork(CoreCommon.RoadNetwork);
						this.testComponentNeedsRoadNetwork = false;
					}
				}
				catch (Exception ex)
				{
					ArbiterOutput.OutputNoLog("Error registering with operational test service: " + ex.ToString());
				}
			}
		}

		/// <summary>
		/// Manages excution of the test behaviros
		/// </summary>
		private void AsynchronousExecutionManager()
		{
			// execute ten times per second
			MMWaitableTimer mmwt = new MMWaitableTimer(100);

			// initialize
			this.testAsyncExecuteQueue = new Queue<Behavior>();
			this.testBehaviorExecutionStopwatch = new Stopwatch();
			this.testModeRecentCompleted = new List<TestBehaviorCompletion>();
			this.testCurrentTest = null;
			this.testCompletionReport = null;

			// loop constantly
			while (true)
			{
				// make sre we are timing properly
				mmwt.WaitEvent.WaitOne();

				// reset values
				this.testCurrentTest = null;
				this.testCompletionReport = null;

				// update the list of completed to only include those in last 10 seconds
				List<TestBehaviorCompletion> updatedRecent = new List<TestBehaviorCompletion>();
				foreach (TestBehaviorCompletion tbc in this.testModeRecentCompleted)
				{
					if (this.GetVehicleState().Timestamp - tbc.CompletionTimestamp < 10.0)
						updatedRecent.Add(tbc);
				}
				this.testModeRecentCompleted = updatedRecent;

				try
				{
					if (!this.testComponentNeedsRoadNetwork &&
						this.testComponentExists && 
						testAsyncExecuteQueue.Count > 0)
					{
						// reset timer
						this.testBehaviorExecutionStopwatch.Stop();
						this.testBehaviorExecutionStopwatch.Reset();
						
						// eavior
						this.testCurrentTest = testAsyncExecuteQueue.Dequeue();
						
						// thread the execution
						this.testBehaviorExecutionThread = new Thread(TestBehaviorExecutionThread);
						this.testBehaviorExecutionThread.IsBackground = true;
						this.testBehaviorExecutionStopwatch.Start();
						this.testBehaviorExecutionThread.Start();

						// test execution time
						while (this.testBehaviorExecutionThread.IsAlive)
						{
							// check time
							if (this.testBehaviorExecutionStopwatch.ElapsedMilliseconds / 1000.0 > 3.0)
							{
								try
								{
									ArbiterOutput.OutputNoLog("Test Behavior Execution took Longer than 3 seconds, aborting");
									this.testBehaviorExecutionThread.Abort();
								}
								catch (Exception)
								{
								}
							}
							// fine
							else
							{
								// take 10ms
								Thread.Sleep(100);
							}
						}

						//  check completion report status
						if (this.testCompletionReport != null)
						{
							List<TestBehaviorCompletion> updated = new List<TestBehaviorCompletion>(this.testModeRecentCompleted.ToArray());
							updated.Add(new TestBehaviorCompletion(this.GetVehicleState().Timestamp, this.testCompletionReport, this.testCurrentTest));
							this.testModeRecentCompleted = updated;
						}

						this.testCompletionReport = null;
						this.testCurrentTest = null;
					}
					else
					{
						try
						{
							this.operationalTestFacade.Ping();
							this.testComponentExists = true;
						}
						catch (Exception)
						{
							this.testComponentExists = false;
							this.testComponentNeedsRoadNetwork = true;
							this.TryOperationalTestFacadeConnect();
						}

						if(!this.testComponentExists || this.testComponentNeedsRoadNetwork)
						{
							this.TryOperationalTestFacadeConnect();
						}
					}
				}
				catch (Exception e)
				{
				}
			}
		}

		/*
		/// <summary>
		/// Enter a behavior in the test execute queue
		/// </summary>
		/// <param name="testTurnBehavior"></param>
		/// <param name="resetQueue"></param>
		public void AsynchronousTestExecute(TurnBehavior testTurnBehavior, bool resetQueue)
		{
			if (resetQueue || this.testAsyncExecuteQueue.Count > 10)
			{
				Queue<Behavior> tmp = new Queue<Behavior>();
				tmp.Enqueue(testTurnBehavior);
				this.testAsyncExecuteQueue = tmp;
			}
			else
			{
				Queue<Behavior> tmp = new Queue<Behavior>(this.testAsyncExecuteQueue);
				tmp.Enqueue(testTurnBehavior);
				this.testAsyncExecuteQueue = tmp;
			}
		}*/

		/// <summary>
		/// Check if we have completed testing a behavior
		/// </summary>
		/// <param name="testBehavior"></param>
		/// <param name="completionReport"></param>
		/// <param name="idMatch"></param>
		/// <returns></returns>
		public bool AsynchronousTestHasCompleted(Behavior testBehavior, out CompletionReport completionReport, bool idMatch)
		{
			completionReport = null;
			foreach (TestBehaviorCompletion tbc in this.testModeRecentCompleted)
			{
				// check type
				if (tbc.Behavior.GetType().Equals(testBehavior.GetType()))
				{
					if (idMatch)
					{
						if (testBehavior.UniqueId().Equals(tbc.Behavior.UniqueId()))
						{
							completionReport = tbc.Report;
							return true;
						}
					}
					else
					{
						return true;
					}
				}
			}

			return false;
		}

		#endregion
	}

	/// <summary>
	/// Represents completion of beahvior
	/// </summary>
	public struct TestBehaviorCompletion
	{
		public double CompletionTimestamp;
		public CompletionReport Report;
		public Behavior Behavior;

		public TestBehaviorCompletion(double completionTs, CompletionReport cr, Behavior b)
		{
			this.CompletionTimestamp = completionTs;
			this.Report = cr;
			this.Behavior = b;
		}
	}
}