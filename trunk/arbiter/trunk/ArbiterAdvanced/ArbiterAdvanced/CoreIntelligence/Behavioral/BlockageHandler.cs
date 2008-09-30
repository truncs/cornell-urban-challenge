using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road;
using UrbanChallenge.Arbiter.Core.Common.State;
using System.Diagnostics;
using UrbanChallenge.Behaviors.CompletionReport;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Arbiter.Core.Communications;
using UrbanChallenge.Arbiter.Core.Common;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Behavioral
{
	/// <summary>
	/// Handles blockages
	/// </summary>
	public class BlockageHandler
	{
		/// <summary>
		/// most current blockage report
		/// </summary>
		private TrajectoryBlockedReport recentReport;

		/// <summary>
		/// timeout for the recent report
		/// </summary>
		private Stopwatch timeoutTimer;

		/// <summary>
		/// TImer for no blockages sent
		/// </summary>
		public static Stopwatch cooldownTimer;

		/// <summary>
		/// Maximum value of the cooldown
		/// </summary>
		public static double cooldownMaxValue;

		/// <summary>
		/// Constructor
		/// </summary>
		public BlockageHandler()
		{
			this.recentReport = null;
			this.timeoutTimer = new Stopwatch();
			cooldownTimer = new Stopwatch();
		}

		/// <summary>
		/// Resets current report
		/// </summary>
		public void Reset()
		{
			// prevent overwrites
			lock (timeoutTimer)
			{
				this.recentReport = null;
				this.timeoutTimer.Stop();
				this.timeoutTimer.Reset();
			}
		}

		/// <summary>
		/// sets the current blockage report
		/// </summary>
		/// <param name="tbr"></param>
		public void OnBlockageReport(TrajectoryBlockedReport tbr)
		{
			// set the report
			this.recentReport = tbr;

			// output
			ArbiterOutput.Output("Blockage report: Behavior: " + tbr.BehaviorType.ToString() + ", Type: " + tbr.BlockageType.ToString() + ", Dist: " + tbr.DistanceToBlockage.ToString("f2") + ", Result: " + tbr.Result.ToString() + ", Reverse Rec: " + tbr.ReverseRecommended.ToString() + ", Saudi: " + tbr.SAUDILevel.ToString() + ", Track: " + tbr.TrackID.ToString() + "\n");

			// lock the timer
			lock (timeoutTimer)
			{
				this.timeoutTimer.Stop();
				this.timeoutTimer.Reset();
				this.timeoutTimer.Start();
			}
		}

		/// <summary>
		/// Updates the blockage component
		/// </summary>
		public List<ITacticalBlockage> DetermineBlockages(IState currentState)
		{
			// check for a timeout or cooldown
			if (timeoutTimer.ElapsedMilliseconds / 1000.0 > 0.5 || 
				(cooldownTimer.IsRunning && cooldownTimer.ElapsedMilliseconds < cooldownMaxValue))
			{
				// reset all if recent blockage timed out
				this.Reset();
			}
			// check cooldown timer expired
			else if (cooldownTimer.IsRunning && cooldownTimer.ElapsedMilliseconds > cooldownMaxValue)
			{
				cooldownTimer.Stop();
				cooldownTimer.Reset();
				cooldownMaxValue = 0.0;
			}

			// set the report to use for this
			TrajectoryBlockedReport tbr = this.recentReport;

			// check if we still have a blockage report
			if (tbr != null)
			{
				// check type of state is stay in lane or stay in supra lane
				if (currentState is StayInLaneState || currentState is StayInSupraLaneState)
				{
					// check type of completion report matches current state
					if (tbr.BehaviorType == (new StayInLaneBehavior(null, null, null)).GetType() ||
						tbr.BehaviorType == (new SupraLaneBehavior(null, null, 0, 0, 0, null, null, 0, 0, 0, null, null, null)).GetType())
					{
						// create the lane blockage
						LaneBlockage lb = new LaneBlockage(tbr);
						CoreCommon.CurrentInformation.Blockage = lb.ToString();

						// return the blockage
						return new List<ITacticalBlockage>(new ITacticalBlockage[] { lb });
					}
				}
				// check if we're in a turn state
				else if (currentState is TurnState)
				{
					// check type of completion report matches current state
					if (tbr.BehaviorType == (new TurnBehavior(null, null, null, null, null, null)).GetType())
					{
						// create a new turn blockage
						TurnBlockage tb = new TurnBlockage(tbr);
						CoreCommon.CurrentInformation.Blockage = tb.ToString();

						// return the blockage
						return new List<ITacticalBlockage>(new ITacticalBlockage[] { tb });
					}
				}
				// check if we're changing lanes
				else if (currentState is ChangeLanesState)
				{
					// check type of completion report matches current state
					if (tbr.BehaviorType == (new ChangeLaneBehavior(null, null, false, 0.0, null, null)).GetType())
					{
						// create a new turn blockage
						LaneChangeBlockage lcb = new LaneChangeBlockage(tbr);
						CoreCommon.CurrentInformation.Blockage = lcb.ToString();

						// return the blockage
						return new List<ITacticalBlockage>(new ITacticalBlockage[] { lcb });
					}
				}
				// check if we are recovering from a blockage
				else if (currentState is BlockageRecoveryState)
				{
					// check type
					if (((BlockageRecoveryState)currentState).RecoveryBehavior != null &&
						tbr.BehaviorType == ((BlockageRecoveryState)currentState).RecoveryBehavior.GetType())
					{
						// create a new blockage recovery blockage
						BlockageRecoveryBlockage brb = new BlockageRecoveryBlockage(tbr);
						CoreCommon.CurrentInformation.Blockage = brb.ToString();
						((BlockageRecoveryState)currentState).RecoveryStatus = BlockageRecoverySTATUS.BLOCKED;

						// return the blockage
						return new List<ITacticalBlockage>(new ITacticalBlockage[] { brb });
					}
				}
				// check if we are in an opposing lane
				else if (currentState is OpposingLanesState)
				{
					// create a new blockage recovery blockage
					OpposingLaneBlockage olb = new OpposingLaneBlockage(tbr);
					CoreCommon.CurrentInformation.Blockage = olb.ToString();

					// return the blockage
					return new List<ITacticalBlockage>(new ITacticalBlockage[] { olb });
				}
			}

			// fall out returns empty list of blockages
			return new List<ITacticalBlockage>();
		}

		/// <summary>
		/// Start wait timer before accepting new blockages
		/// </summary>
		public static void SetDefaultBlockageCooldown()
		{
			cooldownMaxValue = CoreCommon.BlockageCooldownMilliseconds;
			cooldownTimer.Stop();
			cooldownTimer.Reset();
			cooldownTimer.Start();
		}
	}
}
