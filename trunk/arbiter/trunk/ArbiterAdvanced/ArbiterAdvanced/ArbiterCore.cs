using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.Core.Remote;
using UrbanChallenge.Arbiter.Core.Common;
using UrbanChallenge.Arbiter.ArbiterMission;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Arbiter.Core.Communications;
using System.Threading;
using System.Runtime.Remoting.Messaging;
using UrbanChallenge.Arbiter.Core.CoreIntelligence;
using UrbanChallenge.Arbiter.Core.Common.State;
using UrbanChallenge.Common;

namespace UrbanChallenge.Arbiter.Core
{
	/// <summary>
	/// Main control point for arbiter functions
	/// </summary>
	[Serializable]
	public class ArbiterCore : ArbiterAdvancedRemote
	{
		#region Private Members

		/// <summary>
		/// the road network the arbiter is operating upon
		/// </summary>
		private ArbiterRoadNetwork arbiterRoadNetwork;

		/// <summary>
		/// the current mission
		/// </summary>
		private ArbiterMissionDescription arbiterMissionDescription;

		/// <summary>
		/// Thread containing watchdog over the intelligence components
		/// </summary>
		private Thread watchdogThread;

		/// <summary>
		/// Contains the intelligence components
		/// </summary>
		private CoreIntelligence.Core intelligenceCore;

		/// <summary>
		/// Communications handler
		/// </summary>
		private Communicator communicator;

		#endregion

		#region Public Members

		#endregion

		#region Events

		// Road network changed
		public delegate void RoadNetworkChangedDelegate();
		public event RoadNetworkChangedDelegate OnRoadNetworkChanged;

		// Mission description changed
		public delegate void MissionDescriptionChangedDelegate();
		public event MissionDescriptionChangedDelegate OnMissionDescriptionChanged;

		#endregion

		#region Constructor

		/// <summary>
		/// Constructor
		/// </summary>
		public ArbiterCore()
		{
			// begin the log
			ArbiterOutput.BeginLog();

			// constructor
			this.communicator = new Communicator(this);

			// set intelligence core
			this.intelligenceCore = new CoreIntelligence.Core();

			#region Handle Events

			this.OnRoadNetworkChanged += new RoadNetworkChangedDelegate(ArbiterCore_OnRoadNetworkChanged);
			this.OnMissionDescriptionChanged += new MissionDescriptionChangedDelegate(ArbiterCore_OnMissionDescriptionChanged);

			#endregion
		}

		#endregion

		#region Fields

		#endregion

		#region Functions

		/// <summary>
		/// Begins all functions for maintaining the arbiter
		/// </summary>
		public void BeginArbiterCore()
		{
			// Console begin
			Console.WriteLine("");
			Console.WriteLine("Cornell University Darpa Urban Challenge, 2006-2007");
			Art.WriteImpossible();

			// begin communications
			ArbiterOutput.Output("Creating Communications Watchdog");
			this.communicator.BeginCommunications();
			CoreCommon.Communications = this.communicator;			

			// begin watchdog
			ArbiterOutput.Output("Creating Intelligence Watchdog");
			watchdogThread = new Thread(this.Watchdog);
			watchdogThread.IsBackground = true;
			watchdogThread.Priority = ThreadPriority.BelowNormal;
			watchdogThread.Start();

			// Notify ready
			Console.WriteLine("");
			ArbiterOutput.Output("Ready To Begin, Good Luck!");
			Console.WriteLine("");

			// begin command line interface
			this.Command();
		}

		/// <summary>
		/// Keeps watch over the ai and determines if any components need to be restarted
		/// </summary>
		private void Watchdog()
		{
			while (true)
			{
				try
				{
					// wait for a second or so at a time
					Thread.Sleep(1000);

					// check state of intelligence
					if ((intelligenceCore.CoreIntelligenceThread == null || (intelligenceCore.CoreIntelligenceThread != null && !intelligenceCore.CoreIntelligenceThread.IsAlive)) && intelligenceCore.arbiterMode == ArbiterMode.Run)
					{
						// critical error
						ArbiterOutput.Output("Critical error found by intelligence watchdog! intelligence thread not running");

						// restart
						ArbiterOutput.Output("Restarting ai by watchdog");

						// begin intelligence thread
						ArbiterOutput.Output("Spooling Up Arbiter Core Intelligence");						
						intelligenceCore.Jumpstart();
					}
				}
				catch (Exception e)
				{
					ArbiterOutput.Output("Error in intelligence watchdog: \n" + e.ToString());
				}
			}
		}

		/// <summary>
		/// The command line for simple information adn control functions
		/// </summary>
		private void Command()
		{
			// command string
			string command = "";

			// run while not supposed to quit
			while (command != "exit")
			{
				// command prompt
				Console.Write("Arbiter > ");

				// wait for entry
				command = Console.ReadLine();

				// exit
				if (command == "exit")
				{
					// shutdown
					this.ShutDown();
				}
				else if (command == "resetArbiter")
				{
					try
					{
						this.intelligenceCore.Restart();						
					}
					catch (Exception e)
					{
						ArbiterOutput.Output("Attempted to reset arbiter, error: " + e.ToString());
					}
				}
				else if (command == "disableOutputLogging")
				{
					ArbiterOutput.Output("Disabled Output Logging");
					ArbiterOutput.DefaultOutputLoggingEnabled = false;					
				}
				else if (command == "disableOutputMessaging")
				{
					ArbiterOutput.Output("Disabled Output Messaging");
					ArbiterOutput.DefaultOutputMessagingEnabled = false;
				}
				else if (command == "enableOutputLogging")
				{
					ArbiterOutput.DefaultOutputLoggingEnabled = true;
					ArbiterOutput.Output("Enabled Output Logging");
				}
				else if (command == "enableOutputMessaging")
				{
					ArbiterOutput.DefaultOutputMessagingEnabled = true;
					ArbiterOutput.Output("Enabled Output Logging");
				}
				else if (command == "removeCheckpoint")
				{
					if (CoreCommon.Mission != null)
					{
						if (CoreCommon.Mission.MissionCheckpoints.Count > 0)
						{
							this.intelligenceCore.DestroyIntelligence();
							Thread.Sleep(300);
							ArbiterOutput.Output("Removing checkpoint: " + CoreCommon.Mission.MissionCheckpoints.Peek().WaypointId.ToString());
							CoreCommon.Mission.MissionCheckpoints.Dequeue();
							this.Reset();
						}
						else
						{
							ArbiterOutput.Output("No checkpoint to remove");
						}
					}
					else
					{
						ArbiterOutput.Output("Mission cannot be null to remove checkpoints");
					}
				}
				else if (command == "")
				{
				}
				else
				{
					Console.WriteLine(
						"\n" +
						"exit\n" +
						"resetArbiter\n" +
						"disableOutputMessaging\n" + 
						"enableOutputMessaging\n" + 
						"disableOutputLogging\n" + 
						"enableOutputLogging\n" + 
						"removeCheckpoint\n");
				}
			}
		}

		/// <summary>
		/// Quit
		/// </summary>
		private void ShutDown()
		{
			// notify shut down nominally
			ArbiterOutput.WriteToLog("Shutting down nominally");

			// close comms
			this.communicator.Shutdown();

			// close output
			ArbiterOutput.ShutDownOutput();
		}

		#endregion

		#region Arbiter Advanced Remote Members

		/// <summary>
		/// Pings the arbiter to see if there is a response
		/// </summary>
		public override bool Ping()
		{
			return true;
		}

		/// <summary>
		/// Jumpstarts the ai with a new road network and mission if we can
		/// </summary>
		/// <param name="roadNetwork"></param>
		/// <param name="mission"></param>
		public override void JumpstartArbiter(ArbiterRoadNetwork roadNetwork, ArbiterMissionDescription mission)
		{
			try
			{
				ArbiterOutput.Output("Jumpstarting Arbiter");
				
				// warn if carmode not correct
				CarMode carMode = CoreCommon.Communications.GetCarMode();
				if (carMode != CarMode.Pause && carMode != CarMode.Human)
					ArbiterOutput.Output("Warning: Vehicle is in CarMode: " + carMode.ToString());

				if (roadNetwork != null && mission != null)
				{
					// destroy intelligence if exists
					this.intelligenceCore.DestroyIntelligence();

					// create roads and mission
					roadNetwork.SetSpeedLimits(mission.SpeedLimits);
					roadNetwork.GenerateVehicleAreas();
					this.arbiterRoadNetwork = roadNetwork;
					this.arbiterMissionDescription = mission;
					CoreCommon.RoadNetwork = roadNetwork;
					CoreCommon.Mission = mission;

					// startup ai
					this.intelligenceCore.Jumpstart();
				}
				else
				{
					ArbiterOutput.Output("RoadNetwork and Mission must both have value");
				}
			}
			catch (Exception ex)
			{
				ArbiterOutput.Output("JumpstartArbiter(ArbiterRoadNetwork roadNetwork, ArbiterMissionDescription mission) Failed", ex);
			}
		}

		/// <summary>
		/// Updates the ai with a new mission if can
		/// </summary>
		/// <param name="mission"></param>
		/// <returns></returns>
		public override bool UpdateMission(ArbiterMissionDescription mission)
		{
			try
			{
				ArbiterOutput.Output("Setting new mission");
				CarMode carMode = CoreCommon.Communications.GetCarMode();

				if (carMode == CarMode.Pause || carMode == CarMode.Human)
				{
					if (mission != null)
					{
						// create roads and mission
						CoreCommon.RoadNetwork.SetSpeedLimits(mission.SpeedLimits);
						this.arbiterMissionDescription = mission;
						CoreCommon.Mission = mission;
						return true;
					}
					else
					{
						ArbiterOutput.Output("Mission must have value");
					}
				}
				else
				{
					ArbiterOutput.Output("Cannot set mission when car is in CarMode: " + carMode.ToString());
				}
			}
			catch (Exception ex)
			{
				ArbiterOutput.Output("UpdateMission(ArbiterMissionDescription mission) Failed", ex);
			}

			return false;
		}

		/// <summary>
		/// Sets the ai mode
		/// </summary>
		/// <param name="mode"></param>
		public override void SetAiMode(ArbiterMode mode)
		{
			try
			{
				ArbiterOutput.Output("Setting ai mode to ArbiterMode: " + mode.ToString());

				switch (mode)
				{
					case ArbiterMode.Run:
						this.intelligenceCore.RunIntelligence();
						break;
					case ArbiterMode.Pause:
						this.intelligenceCore.PauseIntelligence();
						break;
					case ArbiterMode.Stop:
						this.intelligenceCore.DestroyIntelligence();
						break;
				}
			}
			catch (Exception ex)
			{
				ArbiterOutput.Output("SetAiMode(ArbiterMode mode) Failed", ex);
			}
		}

		/// <summary>
		/// Resets the intelligence
		/// </summary>
		public override void Reset()
		{
			try
			{
				ArbiterOutput.Output("Resetting Arbiter Intelligence");
				this.intelligenceCore.Restart();
			}
			catch (Exception ex)
			{
				ArbiterOutput.Output("Reset() Failed", ex);
			}
		}

		/// <summary>
		/// Starts a new log
		/// </summary>
		public override void BeginNewLog()
		{
			try
			{
				ArbiterOutput.Output("Starting new log");
				ArbiterOutput.ShutDownOutput();
				ArbiterOutput.BeginLog();
			}
			catch (Exception ex)
			{
				ArbiterOutput.Output("BeginNewLog Failed", ex);
			}
		}

		/// <summary>
		/// Pauses the ai, direting it to forece the vehicle to pause
		/// </summary>
		public override void PauseFromAi()
		{
			try
			{
				ArbiterOutput.Output("Directing ai to pause vehicle");
				this.intelligenceCore.PauseIntelligence();
			}
			catch (Exception ex)
			{
				ArbiterOutput.Output("PauseFromAi() Failed", ex);
			}
		}

		/// <summary>
		/// Emergency stop the vehicle
		/// </summary>
		public override void EmergencyStop()
		{
			try
			{
				ArbiterOutput.Output("Emergency Stop");
				this.intelligenceCore.ExecuteEmergencyStop();
			}
			catch (Exception ex)
			{
				ArbiterOutput.Output("EmergencyStop() Failed", ex);
			}
		}

		/// <summary>
		/// Gets road network
		/// </summary>
		/// <returns></returns>
		public override ArbiterRoadNetwork GetRoadNetwork()
		{
			return CoreCommon.RoadNetwork;
		}

		/// <summary>
		/// Gets mission description
		/// </summary>
		/// <returns></returns>
		public override ArbiterMissionDescription GetMissionDescription()
		{
			return CoreCommon.Mission;
		}

		/// <summary>
		/// Don't deregister
		/// </summary>
		/// <returns></returns>
		public override object InitializeLifetimeService()
		{
			return null;
		}

		/// <summary>
		/// Reconnect to stuff
		/// </summary>
		public override void Reconnect()
		{
			ArbiterOutput.Output("Attempting to reconnect to stuff");
			CoreCommon.Communications.TryReconnect();
		}

		#endregion

		#region Event Handlers

		/// <summary>
		/// Handles event of road network changing
		/// </summary>
		protected void ArbiterCore_OnRoadNetworkChanged()
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// <summary>
		/// Handles event of mission description changed
		/// </summary>
		protected void ArbiterCore_OnMissionDescriptionChanged()
		{
			throw new Exception("The method or operation is not implemented.");
		}

		#endregion

		public override void RemoveNextCheckpoint()
		{
			try
			{
				if (CoreCommon.Mission != null)
				{
					if (CoreCommon.Mission.MissionCheckpoints.Count > 0)
					{
						this.intelligenceCore.DestroyIntelligence();
						Thread.Sleep(300);
						ArbiterOutput.Output("Removing checkpoint: " + CoreCommon.Mission.MissionCheckpoints.Peek().WaypointId.ToString());
						CoreCommon.Mission.MissionCheckpoints.Dequeue();
						this.Reset();
					}
					else
					{
						ArbiterOutput.Output("No checkpoint to remove");
					}
				}
			}
			catch (Exception e)
			{
				ArbiterOutput.Output(e.ToString());
			}
		}
	}
}
