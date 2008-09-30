using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UrbanChallenge.Arbiter.Core.Communications;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Behavioral;
using UrbanChallenge.Arbiter.Core.Common.State;
using UrbanChallenge.Common.Utility;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Arbiter.ArbiterMission;
using UrbanChallenge.Arbiter.Core.Common;
using UrbanChallenge.Common;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.State;
using System.Diagnostics;
using UrbanChallenge.Arbiter.Core.Common.Arbiter;
using UrbanChallenge.Behaviors;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence
{
	/// <summary>
	/// Core of the intelligence of the ai
	/// </summary>
	[Serializable]
	public class Core
	{
		#region Private Members

		/// <summary>
		/// The behavioral layer
		/// </summary>
		public BehavioralDirector Behavioral;

		/// <summary>
		/// Mode of the ai
		/// </summary>
		public ArbiterMode arbiterMode = ArbiterMode.Stop;

		/// <summary>
		/// Lane agent for the core
		/// </summary>
		public LaneAgent coreLaneAgent;

		/// <summary>
		/// Stopwatch determining time since last execution
		/// </summary>
		public Stopwatch executionStopwatch;

		#endregion

		#region Public Members

		/// <summary>
		/// Thread of the core intelligence
		/// </summary>
		public Thread CoreIntelligenceThread;

		#endregion

		/// <summary>
		/// Constructor
		/// </summary>
		public Core()
		{
			// initialize turn signals
			TurnDecorators.Initialize();

			// set initial state
			CoreCommon.CorePlanningState = new StartUpState();

			// start a new behavioral layer
			this.Behavioral = new BehavioralDirector();

			// start new lane agent
			this.coreLaneAgent = new LaneAgent();

			// create execution stopwatch
			this.executionStopwatch = new Stopwatch();
		}

		/// <summary>
		/// Jumpstarts the intelligence core
		/// </summary>
		public void Jumpstart()
		{
			// make sure to remove old
			this.DestroyIntelligence();

			// start new
			ArbiterOutput.Output("Starting New Core Intelligence Thread");
			this.arbiterMode = ArbiterMode.Run;

			// start intelligence thread
			CoreIntelligenceThread = new Thread(CoreIntelligence);
			CoreIntelligenceThread.Priority = ThreadPriority.AboveNormal;
			CoreIntelligenceThread.IsBackground = true;
			CoreIntelligenceThread.Start();

			// notify
			ArbiterOutput.Output("Core Intelligence Thread Jumpstarted");
		}

		/// <summary>
		/// Shuts down the old ai thread
		/// </summary>
		public void DestroyIntelligence()
		{
			// notify
			ArbiterOutput.Output("Attemptimg to Destroy Old Core Intelligence, Please Wait");

			int count = 0;

			// destroy old arbiter intelligence
			while (this.CoreIntelligenceThread != null && this.CoreIntelligenceThread.IsAlive)
			{				
				this.arbiterMode = ArbiterMode.Stop;				
				Thread.Sleep(100);
				count++;

				if (count > 50)
				{
					try
					{
						ArbiterOutput.Output("Cold not normally shutdown old intelligence, Aborting the core intelligence thread");
						this.CoreIntelligenceThread.Abort();
					}
					catch (Exception)
					{						
					}

					// chill and wait for intelligence to exit
					Thread.Sleep(1000);
				}
			}

			// notify
			ArbiterOutput.Output("No Intelligence Running, Destruction Successful");
		}

		/// <summary>
		/// Pause intelligence
		/// </summary>
		public void PauseIntelligence()
		{
			ArbiterOutput.Output("Paused Intelligence");
			this.arbiterMode = ArbiterMode.Pause;
		}

		/// <summary>
		/// Restart the core intelligence
		/// </summary>
		public void Restart()
		{
			// jumpstart takes care of this info
			this.Jumpstart();
		}

		/// <summary>
		/// Executes an emergency stop and kills core intelligence
		/// </summary>
		public void ExecuteEmergencyStop()
		{
			// execute stop
			CoreCommon.Communications.Execute(new EmergencyStop(false, false, false));
			this.DestroyIntelligence();
			CoreCommon.Communications.Execute(new EmergencyStop(false, false, false));
			ArbiterOutput.Output("Executed Emergency Stop, Destroyed Core Intelligence");
		}

		/// <summary>
		/// Pause the vehicle using an ai defined pause
		/// </summary>
		public void ExecuteAiDefinedPause()
		{
			this.PauseIntelligence();
			Thread.Sleep(500);
			CoreCommon.Communications.Execute(new EmergencyStop(true, true, true));
			ArbiterOutput.Output("Executed Ai Defined Pause, Core Intelligence Paused");
		}

		/// <summary>
		/// Reset ai's position estimate
		/// </summary>
		public void ResetPostionEstimate()
		{			
			this.DestroyIntelligence();
			this.Jumpstart();
			ArbiterOutput.Output("Reset to Startup");
		}

		/// <summary>
		/// Runs intelligence
		/// </summary>
		public void RunIntelligence()
		{
			ArbiterOutput.Output("Attempting to switch Core Intelligence to run mode");

			if (this.CoreIntelligenceThread != null)
			{
				if (this.CoreIntelligenceThread.ThreadState != System.Threading.ThreadState.Running)
				{
					ArbiterOutput.Output("Core Intelligence set to run mode");
					this.arbiterMode = ArbiterMode.Run;
				}
				else
				{
					ArbiterOutput.Output("Intelligence already running");
				}
			}
			else
			{
				ArbiterOutput.Output("Intelligence Not Initialized");
			}
		}

		/// <summary>
		/// Intelligence thread
		/// </summary>
		public void CoreIntelligence()
		{
			// wait for entry data
			this.WaitForEntryData();

			// jumpstart behavioral
			this.Behavioral.Jumpstart();

			// timer, run at 10Hz
			MMWaitableTimer cycleTimer = new MMWaitableTimer(100);

			// stopwatch
			Stopwatch stopwatch = new Stopwatch();
			Stopwatch stopwatch2 = new Stopwatch();

			// set initial state
			CoreCommon.CorePlanningState = new StartUpState();

			// send the projection to the operational components
			CoreCommon.Communications.TrySendProjection();
			CoreCommon.Communications.TryOperationalTestFacadeConnect();

			// always run
			while (this.arbiterMode == ArbiterMode.Run || this.arbiterMode == ArbiterMode.Pause)
			{
				try
				{
					// make sure we run at 10Hz
					cycleTimer.WaitEvent.WaitOne();

					if (this.arbiterMode == ArbiterMode.Run)
					{
						// start stopwatch
						stopwatch.Reset();
						stopwatch.Start();

						// reset ai information
						CoreCommon.CurrentInformation = new ArbiterInformation();

						// check for null current state
						if (CoreCommon.CorePlanningState == null)
						{
							CoreCommon.CorePlanningState = new StartUpState();
							throw new Exception("CoreCommon.CorePlanningState == null, returning to startup state");
						}

						// get goal
						INavigableNode goal = StateReasoning.FilterGoal(CoreCommon.Communications.GetVehicleState());

						// filter the state for needed changes
						CoreCommon.CorePlanningState = StateReasoning.FilterStates(CoreCommon.Communications.GetCarMode(), Behavioral);

						// set current state
						CoreCommon.CurrentInformation.CurrentState = CoreCommon.CorePlanningState.ShortDescription();
						CoreCommon.CurrentInformation.CurrentStateInfo = CoreCommon.CorePlanningState.StateInformation();

						// plan the maneuver
						Maneuver m = Behavioral.Plan(
							CoreCommon.Communications.GetVehicleState(),
							CoreCommon.Communications.GetVehicleSpeed().Value,
							CoreCommon.Communications.GetObservedVehicles(),
							CoreCommon.Communications.GetObservedObstacles(),
							CoreCommon.Communications.GetCarMode(),
							goal);

						// set next state
						CoreCommon.CorePlanningState = m.PrimaryState;
						CoreCommon.CurrentInformation.NextState = CoreCommon.CorePlanningState.ShortDescription();
						CoreCommon.CurrentInformation.NextStateInfo = CoreCommon.CorePlanningState.StateInformation();
						
						// get ignorable
						List<int> toIgnore = new List<int>();
						if (m.PrimaryBehavior is StayInLaneBehavior)
						{
							StayInLaneBehavior silb = (StayInLaneBehavior)m.PrimaryBehavior;

							if(silb.IgnorableObstacles != null)
								toIgnore.AddRange(silb.IgnorableObstacles);
							else
								ArbiterOutput.Output("stay in lane ignorable obstacles null");
						}
						else if (m.PrimaryBehavior is SupraLaneBehavior)
						{
							SupraLaneBehavior slb = (SupraLaneBehavior)m.PrimaryBehavior;

							if (slb.IgnorableObstacles != null)
								toIgnore.AddRange(slb.IgnorableObstacles);
							else
								ArbiterOutput.Output("Supra lane ignorable obstacles null");
						}
						CoreCommon.CurrentInformation.FVTIgnorable = toIgnore.ToArray();

						// reset the execution stopwatch
						this.executionStopwatch.Stop();
						this.executionStopwatch.Reset();
						this.executionStopwatch.Start();

						// send behavior to communications
						CoreCommon.Communications.Execute(m.PrimaryBehavior);
						CoreCommon.CurrentInformation.NextBehavior = m.PrimaryBehavior.ToShortString();
						CoreCommon.CurrentInformation.NextBehaviorInfo = m.PrimaryBehavior.ShortBehaviorInformation();
						CoreCommon.CurrentInformation.NextSpeedCommand = m.PrimaryBehavior.SpeedCommandString();
						CoreCommon.CurrentInformation.NextBehaviorTimestamp = m.PrimaryBehavior.TimeStamp.ToString("F6");
						#region Turn Decorators
						// set turn signal decorators
						if (m.PrimaryBehavior.Decorators != null)
						{
							bool foundDec = false;

							foreach (BehaviorDecorator bd in m.PrimaryBehavior.Decorators)
							{
								if (bd is TurnSignalDecorator)
								{
									if (!foundDec)
									{
										TurnSignalDecorator tsd = (TurnSignalDecorator)bd;
										foundDec = true;
										CoreCommon.CurrentInformation.NextBehaviorTurnSignals = tsd.Signal.ToString();
									}
									else
									{
										CoreCommon.CurrentInformation.NextBehaviorTurnSignals = "Multiple!";
									}
								}
							}
						}
						#endregion

						// filter the lane state
						if(CoreCommon.CorePlanningState.UseLaneAgent)
						{
							this.coreLaneAgent.UpdateInternal(CoreCommon.CorePlanningState.InternalLaneState, CoreCommon.CorePlanningState.ResetLaneAgent);
							this.coreLaneAgent.UpdateEvidence(CoreCommon.Communications.GetVehicleState().Area);
							CoreCommon.CorePlanningState = this.coreLaneAgent.UpdateFilter();
						}

						// log and send information to remote listeners
						CoreCommon.Communications.UpdateInformation(CoreCommon.CurrentInformation);

						// check cycle time
						stopwatch.Stop();
						if (stopwatch.ElapsedMilliseconds > 100 || global::UrbanChallenge.Arbiter.Core.ArbiterSettings.Default.PrintCycleTimesAlways)
							ArbiterOutput.Output("Cycle t: " + stopwatch.ElapsedMilliseconds.ToString());
					}
				}
				catch (Exception e)
				{
					// notify exception made its way up to the core thread
					ArbiterOutput.Output("\n\n");
					ArbiterOutput.Output("Core Intelligence Thread caught exception!! \n");
					ArbiterOutput.Output(" Exception type: " + e.GetType().ToString());
					ArbiterOutput.Output(" Exception thrown by: " + e.TargetSite + "\n");
					ArbiterOutput.Output(" Stack Trace: " + e.StackTrace + "\n");					

					if (e is NullReferenceException)
					{
						NullReferenceException nre = (NullReferenceException)e;
						ArbiterOutput.Output("Null reference exception from: " + nre.Source);						
					}

					ArbiterOutput.Output("\n");

					if (this.executionStopwatch.ElapsedMilliseconds / 1000.0 > 3.0)
					{
						ArbiterOutput.Output(" Time since last execution more then 3 seconds");
						ArbiterOutput.Output(" Resetting and Restarting Intelligence");
						try
						{
							Thread tmp = new Thread(ResetThread);
							tmp.IsBackground = true;
							this.arbiterMode = ArbiterMode.Stop;
							tmp.Start();
						}
						catch (Exception ex)
						{
							ArbiterOutput.Output("\n\n");
							ArbiterOutput.Output("Core Intelligence Thread caught exception attempting to restart itself1!!!!HOLYCRAP!! \n");
							ArbiterOutput.Output(" Exception thrown by: " + ex.TargetSite + "\n");
							ArbiterOutput.Output(" Stack Trace: " + ex.StackTrace + "\n");
							ArbiterOutput.Output(" Resetting planning state to startup");
							ArbiterOutput.Output("\n");
							CoreCommon.CorePlanningState = new StartUpState();
						}
					}
				}
			}
		}

		/// <summary>
		/// Resets the ai
		/// </summary>
		private void ResetThread()
		{
			this.Jumpstart();
		}
		
		/// <summary>
		/// Makes the thread wait for the entry data
		/// </summary>
		private void WaitForEntryData()
		{
			// wait for entry data
			ArbiterOutput.Output("Waiting for entry data");

			// flag to wait
			bool canStart = false;

			// chack while we can't start
			while (!canStart && (this.arbiterMode == ArbiterMode.Run || this.arbiterMode == ArbiterMode.Pause))
			{
				// check data ready
				canStart =
					(CoreCommon.RoadNetwork != null &&
					CoreCommon.Mission != null &&
					CoreCommon.Communications.GetVehicleState() != null &&
					CoreCommon.Communications.GetVehicleSpeed() != null &&
					CoreCommon.Communications.GetObservedVehicles() != null &&
					CoreCommon.Communications.GetObservedObstacles() != null);

				if (canStart)
					ArbiterOutput.Output("Entry data nominal, starting");
				else
				{
					bool roads = CoreCommon.RoadNetwork != null;
					bool mission = CoreCommon.Mission != null;
					bool vs = CoreCommon.Communications.GetVehicleState() != null;
					bool vspeed = CoreCommon.Communications.GetVehicleSpeed() != null;
					bool obsV = CoreCommon.Communications.GetObservedVehicles() != null;
					bool obsO = CoreCommon.Communications.GetObservedObstacles() != null;

					ArbiterOutput.Output("");
					ArbiterOutput.Output("Waiting for data, currently received:");
					ArbiterOutput.Output("     Roads: " + roads.ToString() + ", Mission: " + mission.ToString() + ", Vehicle State " + vs.ToString());
					ArbiterOutput.Output("     Speed: " + vspeed.ToString() + ", Vehicles: " + obsV.ToString() + ", Obstacles " + obsO.ToString());
				}
				
				Thread.Sleep(1000);
			}

			// nominal
			ArbiterOutput.Output("All inputs nominal, car in Run, starting");
		}
	}
}
