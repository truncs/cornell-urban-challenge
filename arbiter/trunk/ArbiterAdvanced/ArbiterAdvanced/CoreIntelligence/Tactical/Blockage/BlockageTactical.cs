using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.Core.Common;
using UrbanChallenge.Arbiter.Core.Common.State;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road;
using UrbanChallenge.Common.Sensors;
using UrbanChallenge.Behaviors.CompletionReport;
using UrbanChallenge.Arbiter.Core.Communications;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Vehicles;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Behavioral;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Navigation;
using UrbanChallenge.Arbiter.Core.Common.Tools;
using UrbanChallenge.Common.Utility;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road.Opposing.Reasoning;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road.Reasoning;
using UrbanChallenge.Arbiter.Core.Common.Common;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road.Common;
using System.Diagnostics;
using System.Threading;
using UrbanChallenge.Arbiter.ArbiterMission;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Intersection.Reasoning;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Intersection.Monitors;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Blockage
{
	/// <summary>
	/// Tactical execution of blockage-execution and recovery
	/// </summary>
	public class BlockageTactical
	{
		/// <summary>
		/// Timer we set before doing the uturn
		/// </summary>
		public static Stopwatch uTurnTimer;

		/// <summary>
		/// Makees sure we're not in an execution state for too long
		/// </summary>
		public static Stopwatch ExecutionTimer;

		/// <summary>
		/// Umbrella over all the tacticals
		/// </summary>
		public TacticalDirector tacticalUmbrella;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="tacticalUmbrella"></param>
		public BlockageTactical(TacticalDirector tacticalUmbrella)
		{
			this.tacticalUmbrella = tacticalUmbrella;
			CoreCommon.BlockageDirector = this;
			uTurnTimer = new Stopwatch();
			ExecutionTimer = new Stopwatch();
		}

		/// <summary>
		/// Plan the maneuver
		/// </summary>
		/// <param name="planningState"></param>
		/// <param name="vehicleState"></param>
		/// <param name="vehicleSpeed"></param>
		/// <returns></returns>
		public Maneuver Plan(IState planningState, VehicleState vehicleState, double vehicleSpeed, List<ITacticalBlockage> blockages, INavigationalPlan plan)
		{
			#region Encountered a Blockage

			// something is blocked
			if (CoreCommon.CorePlanningState is EncounteredBlockageState)
			{
				// cast blockage state
				EncounteredBlockageState ebs = (EncounteredBlockageState)CoreCommon.CorePlanningState;
				
				// get the saudi level
				SAUDILevel saudi = ebs.Saudi;

				// get the defcon level
				BlockageRecoveryDEFCON defcon = ebs.Defcon;

				#region Stay In Lane Blockage

				if (ebs.PlanningState is StayInLaneState)
				{
					// get the recovery state
					BlockageRecoveryState brs = new BlockageRecoveryState(null, ebs.PlanningState, ebs.PlanningState, defcon, ebs, BlockageRecoverySTATUS.ENCOUNTERED);
 
					// set cooldown
					BlockageHandler.SetDefaultBlockageCooldown();

					// return
					return new Maneuver(new NullBehavior(), brs, TurnDecorators.NoDecorators, vehicleState.Timestamp);
				}

				#endregion

				#region Supra Lane Blockage

				else if (ebs.PlanningState is StayInSupraLaneState)
				{
					// get all supra lane info
					StayInSupraLaneState sisls = (StayInSupraLaneState)ebs.PlanningState;
					SupraLane sl = sisls.Lane;

					// the closest component of the supra lane to us is the initial lane
					if (sl.ClosestComponent(vehicleState.Front) == SLComponentType.Initial)
					{
						// get stay in lane state
						StayInLaneState sils = new StayInLaneState(sl.Initial, CoreCommon.CorePlanningState);
						ebs.PlanningState = sils;

						// get the recovery state
						BlockageRecoveryState brs = new BlockageRecoveryState(null, sils, sils, defcon, ebs, BlockageRecoverySTATUS.ENCOUNTERED);

						// set cooldown
						BlockageHandler.SetDefaultBlockageCooldown();

						// return
						return new Maneuver(new NullBehavior(), brs, TurnDecorators.NoDecorators, vehicleState.Timestamp);
					}
					// check if closest part of the supra lane is the final lane
					else if (sl.ClosestComponent(vehicleState.Front) == SLComponentType.Final)
					{
						// get stay in lane state
						StayInLaneState sils = new StayInLaneState(sl.Final, CoreCommon.CorePlanningState);
						ebs.PlanningState = sils;

						// get the recovery state
						BlockageRecoveryState brs = new BlockageRecoveryState(null, sils, sils, defcon, ebs, BlockageRecoverySTATUS.ENCOUNTERED);

						// set cooldown
						BlockageHandler.SetDefaultBlockageCooldown();

						// return
						return new Maneuver(new NullBehavior(), brs, TurnDecorators.NoDecorators, vehicleState.Timestamp);
					}
					// the closest component of the supra lane to us is the interconnect otherwise
					else
					{
						// get turn info
						LinePath fP;
						LineList lB;
						LineList rB;
						IntersectionToolkit.TurnInfo(((ArbiterWaypoint)sl.Interconnect.FinalGeneric), out fP, out lB, out rB);

						// make sure to update the turn reasoning
						this.tacticalUmbrella.intersectionTactical.TurnReasoning =
							new TurnReasoning(sl.Interconnect, null);

						// get turn state
						TurnState ts = new TurnState(sl.Interconnect, sl.Interconnect.TurnDirection, sl.Final, fP, lB, rB, new ScalarSpeedCommand(sl.Interconnect.MaximumDefaultSpeed));
						ebs.PlanningState = ts;

						// get final lane state
						StayInLaneState sils = new StayInLaneState(sl.Final, CoreCommon.CorePlanningState);

						// get the recovery state
						BlockageRecoveryState brs = new BlockageRecoveryState(null, sils, ts, defcon, ebs, BlockageRecoverySTATUS.ENCOUNTERED);

						// set cooldown
						BlockageHandler.SetDefaultBlockageCooldown();

						// return
						return new Maneuver(new NullBehavior(), brs, TurnDecorators.NoDecorators, vehicleState.Timestamp);
					}
				}

				#endregion

				#region Change Lanes Blockage

				else if (ebs.PlanningState is ChangeLanesState)
				{
					// get lane change state
					ChangeLanesState cls = (ChangeLanesState)ebs.PlanningState;

					// figure out which lane we are closest to being inside
					ArbiterLane goLane = cls.Parameters.Initial.LanePolygon.IsInside(vehicleState.Front) ?
						cls.Parameters.Initial : cls.Parameters.Target;

					// check if lane is oncoming
					bool goLaneOncoming = cls.Parameters.Initial.LanePolygon.IsInside(vehicleState.Front) ?
						cls.Parameters.InitialOncoming : cls.Parameters.TargetOncoming;

					// normal
					if (!goLaneOncoming)
					{
						// get lane state
						StayInLaneState sils = new StayInLaneState(goLane, CoreCommon.CorePlanningState);
						ebs.PlanningState = sils;

						// get the recovery state
						BlockageRecoveryState brs = new BlockageRecoveryState(null, sils, sils, defcon, ebs, BlockageRecoverySTATUS.ENCOUNTERED);

						// set cooldown
						BlockageHandler.SetDefaultBlockageCooldown();

						// return
						return new Maneuver(new NullBehavior(), brs, TurnDecorators.NoDecorators, vehicleState.Timestamp);
					}
					// opposing
					else
					{
						// get lane state
						OpposingLanesState ols = new OpposingLanesState(goLane, true, CoreCommon.CorePlanningState, vehicleState);
						ebs.PlanningState = ols;

						// get the recovery state
						BlockageRecoveryState brs = new BlockageRecoveryState(null, ols, ols, defcon, ebs, BlockageRecoverySTATUS.ENCOUNTERED);

						// set cooldown
						BlockageHandler.SetDefaultBlockageCooldown();

						// return
						return new Maneuver(new NullBehavior(), brs, TurnDecorators.NoDecorators, vehicleState.Timestamp);
					}
				}

				#endregion

				#region Turn Blockage

				else if (ebs.PlanningState is TurnState)
				{
					// get turn state
					TurnState ts = (TurnState)ebs.PlanningState;

					// notify
					ArbiterOutput.Output("Encountered turn blockage, entering blockage recovery state");

					// get the recovery state
					BlockageRecoveryState brs = new BlockageRecoveryState(null, ts, ts, defcon, ebs, BlockageRecoverySTATUS.ENCOUNTERED);

					// set cooldown
					BlockageHandler.SetDefaultBlockageCooldown();

					// return
					return new Maneuver(new HoldBrakeBehavior(), brs, TurnDecorators.NoDecorators, vehicleState.Timestamp);
				}

				#endregion

				#region Opposing Lanes Blockage

				else if (ebs.PlanningState is OpposingLanesState)
				{
					// get the recovery state
					BlockageRecoveryState brs = new BlockageRecoveryState(null, ebs.PlanningState, ebs.PlanningState, defcon, ebs, BlockageRecoverySTATUS.ENCOUNTERED);

					// set cooldown
					BlockageHandler.SetDefaultBlockageCooldown();

					// return
					return new Maneuver(new NullBehavior(), brs, TurnDecorators.NoDecorators, vehicleState.Timestamp);
				}

				#endregion

				#region Fallout

				// plan through the blockage state
				Maneuver final = new Maneuver(new NullBehavior(), CoreCommon.CorePlanningState, TurnDecorators.NoDecorators, vehicleState.Timestamp);

				// return the final maneuver
				return final;

				#endregion
			}

			#endregion

			#region Blockage Recovery State

			// recover from blockages
			else if (CoreCommon.CorePlanningState is BlockageRecoveryState)
			{
				// get the blockage recovery state
				BlockageRecoveryState brs = (BlockageRecoveryState)CoreCommon.CorePlanningState;

				#region Blockage Encountered

				if (brs.RecoveryStatus == BlockageRecoverySTATUS.ENCOUNTERED)
				{
					// check encoutnered state for a turn
					if (brs.EncounteredState.PlanningState is TurnState)
					{
						// reset timing
						this.Reset();

						// return the turn recovery maneuver
						TurnState ts = (TurnState)brs.EncounteredState.PlanningState;
						return this.TurnRecoveryManeuver(ts.Interconnect, vehicleState, vehicleSpeed, brs);
					}
					// check encountered state for a lane
					else if (brs.EncounteredState.PlanningState is StayInLaneState)
					{
						// reset execution timer
						ExecutionTimer.Stop();
						ExecutionTimer.Reset();

						// lane blockage recovery
						bool fakeWait;
						StayInLaneState sils = (StayInLaneState)brs.EncounteredState.PlanningState;
						return this.LaneRecoveryManeuver(sils.Lane, vehicleState, vehicleSpeed,
							plan, brs, false, out fakeWait);
					}
					// check encountered state for opposing lane
					else if (brs.EncounteredState.PlanningState is OpposingLanesState)
					{
						// reset timing
						this.Reset();

						// reset timing
						OpposingLanesState ols = (OpposingLanesState)brs.EncounteredState.PlanningState;
						return this.OpposingLaneRecoveryManeuver(ols.OpposingLane, vehicleState, vehicleSpeed, plan, brs, false);
					}
				}

				#endregion

				#region Blockage Recovery Executing

				else if (brs.RecoveryStatus == BlockageRecoverySTATUS.EXECUTING)
				{
					// reset uturn timer
					uTurnTimer.Stop();
					uTurnTimer.Reset();

					// check if lane change
					if (brs.RecoveryBehavior is ChangeLaneBehavior)
					{
						// reset timing
						this.Reset();

						// bypass
						ArbiterOutput.Output("Executing behavior of type: " + brs.RecoveryBehavior.GetType().ToString() + ", waiting for completion");
						return new Maneuver(brs.RecoveryBehavior, CoreCommon.CorePlanningState, TurnDecorators.NoDecorators, vehicleState.Timestamp);
					}
					else
					{
						// check execution timer
						if (!ExecutionTimer.IsRunning)
						{
							ExecutionTimer.Stop();
							ExecutionTimer.Reset();
							ExecutionTimer.Start();
						}
						
						// check too long
						if (ExecutionTimer.ElapsedMilliseconds / 1000.0 > 25)
						{
							// reset execution timer
							ExecutionTimer.Stop();
							ExecutionTimer.Reset();

							// bypass
							// notify
							ArbiterOutput.Output("Blockage recovery execution timeout, bypassing to send hold brake");
							try
							{
								CoreCommon.Communications.Execute(new HoldBrakeBehavior());
								Thread.Sleep(100);
							}
							catch (Exception) { }

							// notify
							ArbiterOutput.Output("Blockage recovery execution timeout, resending behavior");

							// resend recovery behavior
							return new Maneuver(brs.RecoveryBehavior, CoreCommon.CorePlanningState, TurnDecorators.NoDecorators, vehicleState.Timestamp);
						}

						// bypass
						ArbiterOutput.Output("Executing behavior of type: " + brs.RecoveryBehavior.GetType().ToString() + ", Currently Sending Null Behavior, waiting for completion");
						return new Maneuver(new NullBehavior(), CoreCommon.CorePlanningState, TurnDecorators.NoDecorators, vehicleState.Timestamp);
					}
				}

				#endregion

				#region Blockage Recovery Behavior Completed

				else if (brs.RecoveryStatus == BlockageRecoverySTATUS.COMPLETED)
				{
					// reset timing
					this.Reset();

					// check encoutnered state for a turn
					if (brs.EncounteredState.PlanningState is TurnState)
					{
						// return the turn recovery maneuver
						TurnState ts = (TurnState)brs.EncounteredState.PlanningState;
						return this.TurnRecoveryManeuver(ts.Interconnect, vehicleState, vehicleSpeed, brs);
					}
					// check lane state
					else if (brs.EncounteredState.PlanningState is StayInLaneState)
					{
						// return the completion state
						brs.RecoveryStatus = BlockageRecoverySTATUS.ENCOUNTERED;
						return new Maneuver(new HoldBrakeBehavior(), brs.CompletionState, TurnDecorators.NoDecorators, vehicleState.Timestamp);
					}
					// check opposing
					else if(brs.EncounteredState.PlanningState is OpposingLanesState)
					{
						// return the completion state
						brs.RecoveryStatus = BlockageRecoverySTATUS.ENCOUNTERED;
						return new Maneuver(new HoldBrakeBehavior(), brs.CompletionState, TurnDecorators.NoDecorators, vehicleState.Timestamp);
					}
				}

				#endregion

				#region Fallout Of Recovery

				// reset timing
				this.Reset();

				ArbiterOutput.Output("Fell out of blockage recovery state: recovery status: " + brs.RecoveryStatus.ToString());
				return new Maneuver(new HoldBrakeBehavior(), CoreCommon.CorePlanningState, brs.RecoveryBehavior != null ? brs.RecoveryBehavior.Decorators : TurnDecorators.NoDecorators, vehicleState.Timestamp);

				#endregion
			}

			#endregion

			#region Unknown

			else
			{
				throw new Exception("Unknown blockage planning state type");
			}

			#endregion
		}

		#region Lane Maneuvers

		/// <summary>
		/// Get the default recovery behavior
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="vehicleState"></param>
		/// <param name="vehicleSpeed"></param>
		/// <param name="brs"></param>
		/// <param name="ebs"></param>
		/// <returns></returns>
		private StayInLaneBehavior LaneRecoveryBehavior(ArbiterLane lane, VehicleState vehicleState, double vehicleSpeed, INavigationalPlan plan,
			BlockageRecoveryState brs, EncounteredBlockageState ebs)
		{
			#region Get the Recovery Behavior

			// set the default distance to go forwards
			double distanceForwards = TahoeParams.VL * 3.0;

			// check distance to next lane point
			this.tacticalUmbrella.roadTactical.forwardReasoning.ForwardMonitor.Primary(lane, vehicleState, (RoadPlan)plan, new List<ITacticalBlockage>() , new List<ArbiterWaypoint>(), false);

			// get navigation distance to go
			double navDistanceToGo = this.tacticalUmbrella.roadTactical.forwardReasoning.ForwardMonitor.NavigationParameters.DistanceToGo;
			distanceForwards = navDistanceToGo < distanceForwards ? navDistanceToGo : distanceForwards;

			// check if there is a forward vehicle we should follow normally
			if (this.tacticalUmbrella.roadTactical.forwardReasoning.ForwardMonitor.ForwardVehicle.ShouldUseForwardTracker)
			{
				// fv distance
				double fvDistance = this.tacticalUmbrella.roadTactical.forwardReasoning.ForwardMonitor.ForwardVehicle.ForwardControl.xSeparation;

				// check distance forwards to vehicle
				if (fvDistance < distanceForwards)
				{
					// distance modification
					distanceForwards = fvDistance > 2.0 ? fvDistance - 2.0 : fvDistance;
				}
			}

			// test behavior
			StayInLaneBehavior testForwards = new StayInLaneBehavior(
				lane.LaneId, new StopAtDistSpeedCommand(distanceForwards), new List<int>(), lane.LanePath(), lane.Width,
				lane.NumberOfLanesLeft(vehicleState.Front, true), lane.NumberOfLanesRight(vehicleState.Front, true));

			// set the specifiec saudi level
			testForwards.Decorators = new List<BehaviorDecorator>(new BehaviorDecorator[] { new ShutUpAndDoItDecorator(brs.EncounteredState.Saudi) });

			// return the test
			return testForwards;

			#endregion
		}

		#region Lane Reverse Maneuvers

		/// <summary>
		/// Reverse because of a blockage
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="vehicleState"></param>
		/// <param name="vehicleSpeed"></param>
		/// <param name="defcon"></param>
		/// <param name="saudi"></param>
		/// <param name="laneOpposing"></param>
		/// <param name="currentBlockage"></param>
		/// <param name="ebs"></param>
		/// <returns></returns>
		private Maneuver LaneReverseManeuver(ArbiterLane lane, VehicleState vehicleState, double vehicleSpeed,
			BlockageRecoveryDEFCON defcon, SAUDILevel saudi, bool laneOpposing, ITacticalBlockage currentBlockage, EncounteredBlockageState ebs)
		{
			// distance to reverse
			double distToReverse = TahoeParams.VL * 2.0;

			// just reverse and let brian catch it
			StayInLaneBehavior reverseBehavior = new StayInLaneBehavior(
				lane.LaneId, new StopAtDistSpeedCommand(distToReverse, true),
				new List<int>(), lane.LanePath(), lane.Width,
				lane.NumberOfLanesLeft(vehicleState.Front, true), lane.NumberOfLanesRight(vehicleState.Front, true));

			// get the saudi level
			List<BehaviorDecorator> decs = new List<BehaviorDecorator>(TurnDecorators.HazardDecorator.ToArray());
			decs.Add(new ShutUpAndDoItDecorator(saudi));
			reverseBehavior.Decorators = decs;

			// state
			IState laneState = new StayInLaneState(lane, CoreCommon.CorePlanningState);
			BlockageRecoveryState brs = new BlockageRecoveryState(
				reverseBehavior, laneState, laneState, defcon, ebs, BlockageRecoverySTATUS.EXECUTING);

			// check enough room in lane to reverse
			RearQuadrantMonitor rearMonitor = this.tacticalUmbrella.roadTactical.forwardReasoning.RearMonitor;
			if (rearMonitor == null || !rearMonitor.lane.Equals(lane))
				this.tacticalUmbrella.roadTactical.forwardReasoning.RearMonitor = new RearQuadrantMonitor(lane, SideObstacleSide.Driver);

			#region Start too close to start of lane

			// check distance to the start of the lane
			double laneStartDistanceFromFront = lane.DistanceBetween(lane.WaypointList[0].Position, vehicleState.Front);
			if (laneStartDistanceFromFront < TahoeParams.VL)
			{
				// make sure we're not at the very start of the lane
				if (laneStartDistanceFromFront < 0.5)
				{
					// output
					ArbiterOutput.Output("Too close to the start of the lane, raising defcon");

					// go up chain
					return new Maneuver(new NullBehavior(),
						new EncounteredBlockageState(currentBlockage, laneState, BlockageRecoveryDEFCON.WIDENBOUNDS, saudi),
						TurnDecorators.NoDecorators,
						vehicleState.Timestamp);
				}
				// otherwise back up to the start of the lane
				else
				{
					// output
					ArbiterOutput.Output("Too close to the start of the lane, setting reverse distance to TP.VL");

					// set proper distance
					distToReverse = TahoeParams.VL;

					// set behavior speed (no car behind us as too close to start of lane)
					LinePath bp = vehicleState.VehicleLinePath;
					reverseBehavior.SpeedCommand = new StopAtDistSpeedCommand(distToReverse, true);
					StayInLaneBehavior silb = new StayInLaneBehavior(null, reverseBehavior.SpeedCommand, new List<int>(), bp, lane.Width * 1.5, 0, 0);
					return new Maneuver(silb, brs, decs, vehicleState.Timestamp);
				}
			}

			#endregion

			#region Sparse

			// check distance to the start of the lane
			if (lane.GetClosestPartition(vehicleState.Front).Type == PartitionType.Sparse)
			{
				// set behavior speed (no car behind us as too close to start of lane)
				LinePath bp = vehicleState.VehicleLinePath;
				reverseBehavior.SpeedCommand = new StopAtDistSpeedCommand(distToReverse, true);
				StayInLaneBehavior silb = new StayInLaneBehavior(null, reverseBehavior.SpeedCommand, new List<int>(), bp, lane.Width * 1.5, 0, 0);
				return new Maneuver(silb, brs, decs, vehicleState.Timestamp);
			}

			#endregion

			#region Vehicle Behind us

			// update and check for vehicle
			rearMonitor.Update(vehicleState);
			if (rearMonitor.CurrentVehicle != null)
			{
				// check for not failed forward vehicle
				if (rearMonitor.CurrentVehicle.QueuingState.Queuing != QueuingState.Failed)
				{
					// check if enough room to rear vehicle
					double vehicleDistance = lane.DistanceBetween(rearMonitor.CurrentVehicle.ClosestPosition, vehicleState.Rear);
					if (vehicleDistance < distToReverse - 2.0)
					{
						// revised distance given vehicle
						double revisedDistance = vehicleDistance - 2.0;

						// check enough room
						if (revisedDistance > TahoeParams.VL)
						{
							// set the updated speed command
							reverseBehavior.SpeedCommand = new StopAtDistSpeedCommand(revisedDistance, true);
						}
						// not enough room
						else
						{
							// output not enough room because of vehicle
							ArbiterOutput.Output("Blockage recovery, not enough room in rear because of rear vehicle, waiting for it to clear");
							return new Maneuver(new NullBehavior(), CoreCommon.CorePlanningState, TurnDecorators.NoDecorators, vehicleState.Timestamp);
						}
					}
				}
				// check failed rear vehicle, up defcon
				else
				{
					// failed vehicle in rear, go up chain
					return new Maneuver(new NullBehavior(),
						new EncounteredBlockageState(currentBlockage, laneState, BlockageRecoveryDEFCON.WIDENBOUNDS, saudi),
						TurnDecorators.NoDecorators,
						vehicleState.Timestamp);
				}
			}

			#endregion

			// all clear, return reverse maneuver, set cooldown
			BlockageHandler.SetDefaultBlockageCooldown();
			return new Maneuver(reverseBehavior, brs, decs, vehicleState.Timestamp);
		}

		/// <summary>
		/// Maneuver if the reverse maneuver is blocked
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="vehicleState"></param>
		/// <param name="vehicleSpeed"></param>
		/// <param name="defcon"></param>
		/// <param name="saudi"></param>
		/// <param name="laneOpposing"></param>
		/// <param name="currentBlockage"></param>
		/// <param name="ebs"></param>
		/// <returns></returns>
		private Maneuver LaneReverseBlockedManeuver(ArbiterLane lane, VehicleState vehicleState, double vehicleSpeed,
			BlockageRecoveryState brs, EncounteredBlockageState ebs, INavigationalPlan plan)
		{
			// get closest partition
			ArbiterLanePartition alp = lane.GetClosestPartition(vehicleState.Front);

			// check type
			if (alp.Type == PartitionType.Sparse)
			{
				#region Get Recovery Behavior

				// get the recovery behavior
				StayInLaneBehavior testForwards = this.LaneRecoveryBehavior(lane, vehicleState, vehicleSpeed, plan, brs, ebs);

				// up the saudi level
				SAUDILevel sl = brs.EncounteredState.Saudi < SAUDILevel.L2 ? brs.EncounteredState.Saudi++ : SAUDILevel.L2;
				testForwards.Decorators = new List<BehaviorDecorator>(new BehaviorDecorator[] { new ShutUpAndDoItDecorator(sl) });

				// return the behavior
				brs.EncounteredState.Saudi = sl;
				brs.RecoveryStatus = BlockageRecoverySTATUS.EXECUTING;
				return new Maneuver(testForwards, brs, TurnDecorators.NoDecorators, vehicleState.Timestamp);

				#endregion
			}
			else
			{
				return new Maneuver(new NullBehavior(), brs.AbortState, TurnDecorators.NoDecorators, vehicleState.Timestamp);
			}
		}

		/// <summary>
		/// What to do when we complete a reverse maneuver
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="vehicleState"></param>
		/// <param name="vehicleSpeed"></param>
		/// <param name="defcon"></param>
		/// <param name="saudi"></param>
		/// <param name="laneOpposing"></param>
		/// <param name="currentBlockage"></param>
		/// <param name="ebs"></param>
		/// <returns></returns>
		private Maneuver LaneReverseCompleteManeuver(ArbiterLane lane, VehicleState vehicleState, double vehicleSpeed, 
			BlockageRecoveryState brs, EncounteredBlockageState ebs, INavigationalPlan plan)
		{
			// get the lane recovery behavior
			StayInLaneBehavior testForwards = this.LaneRecoveryBehavior(lane, vehicleState, vehicleSpeed, plan, brs, ebs);

			#region Test and Return

			// notify
			ArbiterOutput.Output("Attempting to test execute recovery complete behavior: " + testForwards.ToString());

			// test the test behavior
			CompletionReport testForwardsReport;
			bool canCompleteTestForwards = CoreCommon.Communications.TestExecute(testForwards, out testForwardsReport);

			// notify
			ArbiterOutput.Output("Received completion result ok: " + canCompleteTestForwards.ToString() + " with completion result: " + testForwardsReport.Result.ToString());

			// check completion ok of stop at distance
			if (canCompleteTestForwards)
			{
				// switch to the completion state
				brs.RecoveryStatus = BlockageRecoverySTATUS.EXECUTING;
				return new Maneuver(testForwards, brs, TurnDecorators.NoDecorators, vehicleState.Timestamp);
			}
			// not ok, report this blockage
			else
			{
				// return the blocked reversal maneuver
				return this.LaneReverseBlockedManeuver(lane, vehicleState, vehicleSpeed, brs, ebs, plan);
			}

			#endregion
		}

		#endregion

		#endregion

		#region Turn Recovery

		/// <summary>
		/// Maneuver for recovering from a turn blockage
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="vehicleState"></param>
		/// <param name="vehicleSpeed"></param>
		/// <param name="defcon"></param>
		/// <param name="saudi"></param>
		/// <returns></returns>
		private Maneuver TurnRecoveryManeuver(ArbiterInterconnect ai, VehicleState vehicleState,
			double vehicleSpeed, BlockageRecoveryState brs)
		{
			// get the blockage
			ITacticalBlockage turnBlockageReport = brs.EncounteredState.TacticalBlockage;

			// get the turn state
			TurnState turnState = (TurnState)brs.EncounteredState.PlanningState;

			// check the state of the recovery for being the initial state
			if (brs.Defcon == BlockageRecoveryDEFCON.INITIAL)
			{
				// determine if the reverse behavior is recommended
				if (turnBlockageReport.BlockageReport.ReverseRecommended)
				{
					// get the path of the interconnect
					LinePath interconnectPath = ai.InterconnectPath;

					// check how much room there is to the start of the interconnect
					double distanceToStart = interconnectPath.DistanceBetween(interconnectPath.StartPoint, interconnectPath.GetClosestPoint(vehicleState.Rear));

					// check distance to start from the rear bumper is large enough
					if (distanceToStart > 0)
					{
						// notify
						ArbiterOutput.Output("Initial encounter of turn blockage with reverse recommended, reversing");

						// get hte reverse path
						LinePath reversePath = vehicleState.VehicleLinePath;

						// generate the reverse recovery behavior
						StopAtDistSpeedCommand sadsc = new StopAtDistSpeedCommand(TahoeParams.VL, true);
						StayInLaneBehavior silb = new StayInLaneBehavior(null, sadsc, new List<int>(), reversePath, TahoeParams.T * 2.0, 0, 0);
						
						// update the state
						BlockageRecoveryState brsUpdated = new BlockageRecoveryState(
							silb, brs.CompletionState, brs.AbortState, BlockageRecoveryDEFCON.REVERSE,
							brs.EncounteredState, BlockageRecoverySTATUS.EXECUTING);

						// chill out
						BlockageHandler.SetDefaultBlockageCooldown();

						// send the maneuver
						Maneuver recoveryManeuver = new Maneuver(
							silb, brsUpdated, TurnDecorators.HazardDecorator, vehicleState.Timestamp);
						return recoveryManeuver;
					}
				}
			}
			
			// get the default turn behavior
			TurnBehavior defaultTurnBehavior = (TurnBehavior)turnState.Resume(vehicleState, vehicleSpeed);

			// check that we are not already ignoring small obstacles
			if (turnState.Saudi == SAUDILevel.None)
			{
				// check the turn ignoring small obstacles
				ShutUpAndDoItDecorator saudiDecorator = new ShutUpAndDoItDecorator(SAUDILevel.L1);
				defaultTurnBehavior.Decorators.Add(saudiDecorator);
				turnState.Saudi = SAUDILevel.L1;

				// notify
				ArbiterOutput.Output("Attempting test of turn behavior ignoring small obstacles");

				// test execute the turn
				CompletionReport saudiCompletionReport;
				bool completedSaudiTurn = CoreCommon.Communications.TestExecute(defaultTurnBehavior, out saudiCompletionReport);

				// if the turn worked with ignorimg small obstacles, execute that
				if (completedSaudiTurn)
				{
					// notify
					ArbiterOutput.Output("Test of turn behavior ignoring small obstacles successful");

					// chill out
					BlockageHandler.SetDefaultBlockageCooldown();

					// return the recovery maneuver
					return new Maneuver(defaultTurnBehavior, turnState, TurnDecorators.NoDecorators, vehicleState.Timestamp);
				}
			}

			// notify
			ArbiterOutput.Output("Turn behavior reached last level of using no turn boundaries");

			// when ignoring small obstacles does not work, send the turn without boundaries and ignore small obstacles
			turnState.UseTurnBounds = false;
			turnState.LeftBound = null;
			turnState.RightBound = null;
			defaultTurnBehavior.RightBound = null;
			defaultTurnBehavior.LeftBound = null;

			// ignoring small obstacles
			ShutUpAndDoItDecorator saudiNoBoundsDecorator = new ShutUpAndDoItDecorator(SAUDILevel.L1);
			defaultTurnBehavior.Decorators.Add(saudiNoBoundsDecorator);
			turnState.Saudi = SAUDILevel.L1;

			// chill out
			BlockageHandler.SetDefaultBlockageCooldown();

			// go to the turn state
			return new Maneuver(defaultTurnBehavior, turnState, TurnDecorators.NoDecorators, vehicleState.Timestamp);
		}

		#endregion

		#region Lane Recovery

		/// <summary>
		/// Maneuver for recovering from a lane blockage, used for lane changes as well
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="vehicleState"></param>
		/// <param name="vehicleSpeed"></param>
		/// <param name="defcon"></param>
		/// <param name="saudi"></param>
		/// <returns></returns>
		public Maneuver LaneRecoveryManeuver(ArbiterLane lane, VehicleState vehicleState, double vehicleSpeed, 
			INavigationalPlan plan, BlockageRecoveryState brs, bool failedVehiclesPersistentlyOnly, out bool waitForUTurn)
		{
			// get the blockage
			ITacticalBlockage laneBlockageReport = brs.EncounteredState.TacticalBlockage;

			// get the turn state
			StayInLaneState sils = new StayInLaneState(lane, CoreCommon.CorePlanningState);

			// set wait false
			waitForUTurn = false;

			#region Reverse

			// check the state of the recovery for being the initial state
			if (brs.Defcon == BlockageRecoveryDEFCON.INITIAL)
			{
				// determine if the reverse behavior is recommended
				if (laneBlockageReport.BlockageReport.ReverseRecommended)
				{
					// notify
					ArbiterOutput.Output("Initial encounter, reverse recommended, reversing");

					// get reverse behavior
					StayInLaneBehavior reverseRecovery = this.LaneReverseRecovery(lane, vehicleState, vehicleSpeed, brs.EncounteredState.TacticalBlockage.BlockageReport);
					reverseRecovery.TimeStamp = vehicleState.Timestamp;

					// get recovery state
					BlockageRecoveryState brsT = new BlockageRecoveryState(
						reverseRecovery, null, new StayInLaneState(lane, CoreCommon.CorePlanningState),
						brs.Defcon = BlockageRecoveryDEFCON.REVERSE, brs.EncounteredState, BlockageRecoverySTATUS.EXECUTING);
					brsT.CompletionState = brsT;

					// reset blockages
					BlockageHandler.SetDefaultBlockageCooldown();

					// maneuver
					return new Maneuver(reverseRecovery, brsT, TurnDecorators.HazardDecorator, vehicleState.Timestamp);
				}
			}

			#endregion

			#region uTurn

			// check if uturn is available
			ArbiterLane opp = this.tacticalUmbrella.roadTactical.forwardReasoning.GetClosestOpposing(lane, vehicleState);

			// resoning
			OpposingLateralReasoning olrTmp = new OpposingLateralReasoning(opp, SideObstacleSide.Driver);
			
			if (opp != null && olrTmp.ExistsExactlyHere(vehicleState) && opp.IsInside(vehicleState.Front) && opp.LanePath().GetClosestPoint(vehicleState.Front).Location.DistanceTo(vehicleState.Front) < TahoeParams.VL * 3.0)
			{
				// check possible to reach goal given block partition way
				RoadPlan uTurnNavTest = CoreCommon.Navigation.PlanRoadOppositeWithoutPartition(
					lane.GetClosestPartition(vehicleState.Front),
					opp.GetClosestPartition(vehicleState.Front),
					CoreCommon.RoadNetwork.ArbiterWaypoints[CoreCommon.Mission.MissionCheckpoints.Peek().WaypointId],
					true,
					vehicleState.Front,
					true);

				// flag to try the uturn
				bool uturnTry = false;

				// all polygons to include
				List<Polygon> allPolys = BlockageTactical.AllForwardPolygons(vehicleState, failedVehiclesPersistentlyOnly);// .AllObstacleAndStoppedVehiclePolygons(vehicleState);

				// test blockage takes up segment
				double numLanesLeft = lane.NumberOfLanesLeft(vehicleState.Front, true);
				double numLanesRight = lane.NumberOfLanesRight(vehicleState.Front, true);
				LinePath leftBound = lane.LanePath().ShiftLateral((lane.Width * numLanesLeft) + (lane.Width / 2.0));
				LinePath rightBound = lane.LanePath().ShiftLateral((lane.Width * numLanesRight) + (lane.Width / 2.0));
				bool segmentBlockage = BlockageTester.TestBlockage(allPolys, leftBound, rightBound);
				uturnTry = segmentBlockage;

				// check if we should try the uturn
				if(uturnTry)
				{
					// check uturn timer
					if(uTurnTimer.ElapsedMilliseconds / 1000.0 > 2.0)
					{
						#region Determine Checkpoint

						// check route feasible
						if (uTurnNavTest.BestPlan.laneWaypointOfInterest.RouteTime < double.MaxValue - 1
							&& uTurnNavTest.BestPlan.laneWaypointOfInterest.TimeCostToPoint < double.MaxValue - 1)
						{
							ArbiterOutput.Output("valid route to checkpoint by rerouting, uturning");
						}
						// otherwise remove the next checkpoint
						else
						{
							// notiify
							ArbiterOutput.Output("NO valid route to checkpoint by rerouting");

							// get the next cp
							ArbiterCheckpoint cp1 = CoreCommon.Mission.MissionCheckpoints.Peek();

							// get distance to blockage
							double blockageDistance = 15.0;

							// check cp is in our lane						
							if (cp1.WaypointId.AreaSubtypeId.Equals(lane.LaneId))
							{
								// check distance to cp1
								double distanceToCp1 = lane.DistanceBetween(vehicleState.Front, CoreCommon.RoadNetwork.ArbiterWaypoints[cp1.WaypointId].Position);

								// check that this is an already inserted waypoint
								if (cp1.Type == CheckpointType.Inserted)
								{
									// remove cp
									ArbiterCheckpoint ac = CoreCommon.Mission.MissionCheckpoints.Dequeue();
									ArbiterOutput.Output("removed checkpoint: " + ac.WaypointId.ToString() + " as inserted type of checkpoint");

									ArbiterCheckpoint ac2 = CoreCommon.Mission.MissionCheckpoints.Dequeue();
									ArbiterOutput.Output("removed checkpoint: " + ac2.WaypointId.ToString() + " as blocked type of checkpoint");
								}
								// closer to us than the blockage
								else if (distanceToCp1 < blockageDistance)
								{
									// remove cp
									ArbiterCheckpoint ac = CoreCommon.Mission.MissionCheckpoints.Dequeue();
									ArbiterOutput.Output("removed checkpoint: " + ac.WaypointId.ToString() + " as between us and the blockage ~ 15m away");
								}
								// very close to blockage on the other side
								else if (distanceToCp1 < blockageDistance + 5.0)
								{
									// remove cp
									ArbiterCheckpoint ac = CoreCommon.Mission.MissionCheckpoints.Dequeue();
									ArbiterOutput.Output("removed checkpoint: " + ac.WaypointId.ToString() + " as on the other side of blockage ~ 15m away");
								}
								// further away from the blockage
								else
								{
									// check over all checkpoints if there exists a checkpoint cp2 in the opposing lane within 5m along our lane
									ArbiterCheckpoint cp2 = null;
									foreach (ArbiterWaypoint oppAw in opp.WaypointList)
									{
										// check checkpoint
										if (oppAw.IsCheckpoint)
										{
											// distance between us and that waypoint
											double distanceToCp2 = lane.DistanceBetween(vehicleState.Front, oppAw.Position);

											// check along distance < 5.0m
											if (Math.Abs(distanceToCp1 - distanceToCp2) < 5.0)
											{
												// set cp
												cp2 = new ArbiterCheckpoint(oppAw.CheckpointId, oppAw.WaypointId, CheckpointType.Inserted);
											}
										}
									}

									// check close cp exists
									if (cp2 != null)
									{
										// remove cp1 and replace with cp2
										ArbiterOutput.Output("inserting checkpoint: " + cp2.WaypointId.ToString() + " before checkpoint: " + cp1.WaypointId.ToString() + " as can be replaced with adjacent opposing cp2");
										//CoreCommon.Mission.MissionCheckpoints.Dequeue();

										// get current checkpoints
										ArbiterCheckpoint[] updatedAcs = new ArbiterCheckpoint[CoreCommon.Mission.MissionCheckpoints.Count + 1];
										updatedAcs[0] = cp2;										
										CoreCommon.Mission.MissionCheckpoints.CopyTo(updatedAcs, 1);
										updatedAcs[1].Type = CheckpointType.Blocked;
										CoreCommon.Mission.MissionCheckpoints = new Queue<ArbiterCheckpoint>(new List<ArbiterCheckpoint>(updatedAcs));
									}
									// otherwise remove cp1
									else
									{
										// remove cp
										ArbiterCheckpoint ac = CoreCommon.Mission.MissionCheckpoints.Dequeue();
										ArbiterOutput.Output("removed checkpoint: " + ac.WaypointId.ToString() + " as cp not further down our lane");
									}
								}
							}
							// otherwise we remove the checkpoint
							else
							{
								// remove cp
								ArbiterCheckpoint ac = CoreCommon.Mission.MissionCheckpoints.Dequeue();
								ArbiterOutput.Output("removed checkpoint: " + ac.WaypointId.ToString() + " as cp not further down our lane");
							}
						}

						#endregion
					
						#region Plan Uturn
							
						// notify
						ArbiterOutput.Output("Segment blocked, uturn available");

						// nav penalties
						ArbiterLanePartition alpClose = lane.GetClosestPartition(vehicleState.Front);
						CoreCommon.Navigation.AddHarshBlockageAcrossSegment(alpClose, vehicleState.Front);

						// uturn
						LinePath lpTmp = new LinePath(new Coordinates[] { vehicleState.Front, opp.LanePath().GetClosestPoint(vehicleState.Front).Location });
						Coordinates vector = lpTmp[1] - lpTmp[0];
						lpTmp[1] = lpTmp[1] + vector.Normalize(opp.Width / 2.0);
						lpTmp[0] = lpTmp[0] - vector.Normalize(lane.Width / 2.0);
						LinePath lb = lpTmp.ShiftLateral(15.0);
						LinePath rb = lpTmp.ShiftLateral(-15.0);
						List<Coordinates> uturnPolyCOords = new List<Coordinates>();
						uturnPolyCOords.AddRange(lb);
						uturnPolyCOords.AddRange(rb);
						uTurnState uts = new uTurnState(opp, Polygon.GrahamScan(uturnPolyCOords));

						// reset blockages
						BlockageHandler.SetDefaultBlockageCooldown();

						// reset the timers
						this.Reset();

						// go to uturn
						return new Maneuver(uts.Resume(vehicleState, 2.0), uts, TurnDecorators.NoDecorators, vehicleState.Timestamp);

						#endregion
					}
					// uturn timer not enough
					else
					{
						// check timer running
						if(!uTurnTimer.IsRunning)
						{
							uTurnTimer.Stop();
							uTurnTimer.Reset();
							uTurnTimer.Start();
						}

						// if gets here, need to wait
						double time = uTurnTimer.ElapsedMilliseconds / 1000.0;
						ArbiterOutput.Output("uTurn behavior evaluated to success, cooling down for: " + time.ToString("f2") + " out of 2");
						waitForUTurn = true;
						return new Maneuver(new HoldBrakeBehavior(), CoreCommon.CorePlanningState, TurnDecorators.NoDecorators, vehicleState.Timestamp);
					}
				}
				else
				{
					waitForUTurn = false;
					this.Reset();
				}
			}
			else
			{
				waitForUTurn = false;
				this.Reset();
			}

			#endregion

			#region Recovery Escalation

			// plan forward reasoning
			this.tacticalUmbrella.roadTactical.forwardReasoning.ForwardManeuver(lane, vehicleState, (RoadPlan)plan, new List<ITacticalBlockage>(), new List<ArbiterWaypoint>());

			// check clear on right, or if it does nto exist here
			ILateralReasoning rightLateral = this.tacticalUmbrella.roadTactical.rightLateralReasoning;
			bool rightClear = !rightLateral.Exists || !rightLateral.ExistsExactlyHere(vehicleState) ||
				rightLateral.AdjacentAndRearClear(vehicleState);

			// check clear on left, or if it does nto exist here
			ILateralReasoning leftLateral = this.tacticalUmbrella.roadTactical.leftLateralReasoning;
			bool leftClear = !leftLateral.Exists || !leftLateral.ExistsExactlyHere(vehicleState) ||
				leftLateral.AdjacentAndRearClear(vehicleState);
			if(leftClear && leftLateral is OpposingLateralReasoning)
				leftClear = leftLateral.ForwardClear(vehicleState, TahoeParams.VL * 3.0, 2.24,
					new LaneChangeInformation(LaneChangeReason.FailedForwardVehicle, null), 
					lane.LanePath().AdvancePoint(lane.LanePath().GetClosestPoint(vehicleState.Front), TahoeParams.VL * 3.0).Location);

			// check both clear to widen
			bool widenOk = rightClear && leftClear;

			// Notify
			ArbiterOutput.Output("Blockage tactical recovery escalation: rightClear: " + rightClear.ToString() + ", leftCler: " + leftClear.ToString());

			// if we can't widen for some reason just go through with no widen
			StayInLaneBehavior silb = this.LaneRecoveryBehavior(lane, vehicleState, vehicleSpeed, plan, brs, brs.EncounteredState);
			silb.TimeStamp = vehicleState.Timestamp;
 
			// check widen
			if (widenOk)
			{
				// output
				ArbiterOutput.Output("Lane Blockage Recovery: Adjacent areas clear");

				// mini icrease width
				silb.LaneWidth = silb.LaneWidth + TahoeParams.T;

				// check execute none saudi
				CompletionReport l0Cr;
				bool l0TestOk = CoreCommon.Communications.TestExecute(silb, out l0Cr);

				// check mini ok
				if (l0TestOk)
				{
					// notify
					ArbiterOutput.Output("Lane Blockage Recovery: Test Tahoe.T lane widen ok");

					// update the current state
					BlockageRecoveryState brsL0 = new BlockageRecoveryState(
						silb, sils, sils, BlockageRecoveryDEFCON.WIDENBOUNDS, brs.EncounteredState, BlockageRecoverySTATUS.EXECUTING);
					return new Maneuver(silb, brsL0, TurnDecorators.NoDecorators, vehicleState.Timestamp);
				}
				else
				{
					// notify
					ArbiterOutput.Output("Lane Blockage Recovery: Test Tahoe.T lane widen failed, moving to large widen");

					#region Change Lanes

					// check not in change lanes
					if (brs.Defcon != BlockageRecoveryDEFCON.CHANGELANES_FORWARD && brs.Defcon != BlockageRecoveryDEFCON.WIDENBOUNDS)
					{
						// check normal change lanes reasoning
						bool shouldWait;
						IState laneChangeCompletionState;
						ChangeLaneBehavior changeLanesBehavior = this.ChangeLanesRecoveryBehavior(lane, vehicleState, out shouldWait, out laneChangeCompletionState);

						// check change lanes behaviore exists
						if (changeLanesBehavior != null)
						{
							ArbiterOutput.Output("Lane Blockage Recovery: Found adjacent lane available change lanes beahvior: " + changeLanesBehavior.ToShortString() + ", " + changeLanesBehavior.ShortBehaviorInformation());

							// update the current state
							BlockageRecoveryState brsCL = new BlockageRecoveryState(
								changeLanesBehavior, laneChangeCompletionState, sils, BlockageRecoveryDEFCON.CHANGELANES_FORWARD, brs.EncounteredState, BlockageRecoverySTATUS.EXECUTING);
							return new Maneuver(changeLanesBehavior, brsCL, changeLanesBehavior.ChangeLeft ? TurnDecorators.LeftTurnDecorator : TurnDecorators.RightTurnDecorator, vehicleState.Timestamp);
						}
						// check should wait
						else if (shouldWait)
						{
							ArbiterOutput.Output("Lane Blockage Recovery: Should wait for the forward lane, waiting");
							return new Maneuver(new HoldBrakeBehavior(), CoreCommon.CorePlanningState, TurnDecorators.NoDecorators, vehicleState.Timestamp);
						}
					}

					#endregion

					// notify
					ArbiterOutput.Output("Lane Blockage Recovery: Fell Through Forward Change Lanes");

					// increase width
					silb.LaneWidth = silb.LaneWidth * 3.0;

					// check execute l1
					CompletionReport l1Cr;
					bool l1TestOk = CoreCommon.Communications.TestExecute(silb, out l1Cr);

					// check ok
					if (l1TestOk)
					{
						// notify
						ArbiterOutput.Output("Lane Blockage Recovery: Test 3LW lane large widen ok");

						// update the current state
						BlockageRecoveryState brsL1 = new BlockageRecoveryState(
							silb, sils, sils, BlockageRecoveryDEFCON.WIDENBOUNDS, brs.EncounteredState, BlockageRecoverySTATUS.EXECUTING);
						return new Maneuver(silb, brsL1, TurnDecorators.NoDecorators, vehicleState.Timestamp);
					}
					// go to l2 for all the marbles
					else
					{
						// notify
						ArbiterOutput.Output("Lane Blockage Recovery: Test 3LW lane large widen failed, moving to 3LW, L1 Saudi");

						ShutUpAndDoItDecorator saudi2 = new ShutUpAndDoItDecorator(SAUDILevel.L1);
						List<BehaviorDecorator> saudi2bds = new List<BehaviorDecorator>(new BehaviorDecorator[] { saudi2 });
						silb.Decorators = saudi2bds;
						BlockageRecoveryState brsL2 = new BlockageRecoveryState(
							silb, sils, sils, BlockageRecoveryDEFCON.WIDENBOUNDS, brs.EncounteredState, BlockageRecoverySTATUS.EXECUTING);
						return new Maneuver(silb, brsL2, saudi2bds, vehicleState.Timestamp);
					}
				}
			}

			#endregion

			// fallout goes back to lane state
			return new Maneuver(new HoldBrakeBehavior(), CoreCommon.CorePlanningState, TurnDecorators.NoDecorators, vehicleState.Timestamp);
		}

		/// <summary>
		/// Reverse in the lane
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="vehicleState"></param>
		/// <param name="vehicleSpeed"></param>
		/// <returns></returns>
		private StayInLaneBehavior LaneReverseRecovery(ArbiterLane lane, VehicleState vehicleState, double vehicleSpeed, TrajectoryBlockedReport blockage)
		{
			// distance to reverse
			double distToReverse = double.IsNaN(blockage.DistanceToBlockage) ? TahoeParams.VL * 2.0 : TahoeParams.VL - blockage.DistanceToBlockage;
			if (distToReverse <= 3.0)
				distToReverse = TahoeParams.VL;

			// just reverse and let brian catch it
			StayInLaneBehavior reverseBehavior = new StayInLaneBehavior(
				lane.LaneId, new StopAtDistSpeedCommand(distToReverse, true),
				new List<int>(), lane.LanePath(), lane.Width,
				lane.NumberOfLanesLeft(vehicleState.Front, true), lane.NumberOfLanesRight(vehicleState.Front, true));

			#region Start too close to start of lane

			// check distance to the start of the lane
			double laneStartDistanceFromFront = lane.DistanceBetween(lane.WaypointList[0].Position, vehicleState.Front);
			if (laneStartDistanceFromFront < TahoeParams.VL)
			{
				// make sure we're not at the very start of the lane
				if (laneStartDistanceFromFront < 0.5)
				{
					// do nothing
					return null;
				}
				// otherwise back up to the start of the lane
				else
				{
					// output
					ArbiterOutput.Output("Too close to the start of the lane, setting reverse distance to TP.VL");

					// set proper distance
					distToReverse = TahoeParams.VL;

					// set behavior speed (no car behind us as too close to start of lane)
					LinePath bp = vehicleState.VehicleLinePath;
					reverseBehavior.SpeedCommand = new StopAtDistSpeedCommand(distToReverse, true);
					StayInLaneBehavior silb = new StayInLaneBehavior(null, reverseBehavior.SpeedCommand, new List<int>(), bp, lane.Width * 1.5, 0, 0);
					return silb;
				}
			}

			#endregion

			#region Sparse

			// check distance to the start of the lane
			if (lane.GetClosestPartition(vehicleState.Front).Type == PartitionType.Sparse)
			{
				// set behavior speed (no car behind us as too close to start of lane)
				LinePath bp = vehicleState.VehicleLinePath;
				reverseBehavior.SpeedCommand = new StopAtDistSpeedCommand(distToReverse, true);
				StayInLaneBehavior silb = new StayInLaneBehavior(null, reverseBehavior.SpeedCommand, new List<int>(), bp, lane.Width * 1.5, 0, 0);
				return silb;
			}

			#endregion

			#region Vehicle Behind us

			// rear monitor
			RearQuadrantMonitor rearMonitor = this.tacticalUmbrella.roadTactical.forwardReasoning.RearMonitor;

			// update and check for vehicle
			rearMonitor.Update(vehicleState);
			if (rearMonitor.CurrentVehicle != null)
			{
				// check for not failed forward vehicle
				if (rearMonitor.CurrentVehicle.QueuingState.Queuing != QueuingState.Failed)
				{
					// check if enough room to rear vehicle
					double vehicleDistance = lane.DistanceBetween(rearMonitor.CurrentVehicle.ClosestPosition, vehicleState.Rear);
					if (vehicleDistance < distToReverse - 2.0)
					{
						// revised distance given vehicle
						double revisedDistance = vehicleDistance - 2.0;

						// check enough room
						if (revisedDistance > TahoeParams.VL)
						{
							// set the updated speed command
							reverseBehavior.SpeedCommand = new StopAtDistSpeedCommand(revisedDistance, true);
						}
						// not enough room
						else
						{
							// output not enough room because of vehicle
							ArbiterOutput.Output("Blockage recovery, not enough room in rear because of rear vehicle, waiting for it to clear");
							return null;
						}
					}
				}
			}

			#endregion

			// all clear, return reverse maneuver, set cooldown
			return reverseBehavior;
		}

		/// <summary>
		/// Determine a change lanes recovery behavior
		/// </summary>
		/// <param name="current"></param>
		/// <param name="vs"></param>
		/// <param name="shouldWait"></param>
		/// <returns></returns>
		private ChangeLaneBehavior ChangeLanesRecoveryBehavior(ArbiterLane current, VehicleState vs, out bool shouldWait, out IState completionState)
		{
			// set defaults
			shouldWait = false;
			completionState = null;

			// check partition type to make sure normal
			if(current.GetClosestPartition(vs.Front).Type != PartitionType.Normal)
				return null;

			// make sure not in a safety zone			
			foreach(ArbiterSafetyZone asz in current.SafetyZones)
			{
				if(asz.IsInSafety(vs.Front))
					return null;
			}

			// check not inside an intersection safety zone
			foreach(ArbiterIntersection aInter in current.Way.Segment.RoadNetwork.ArbiterIntersections.Values)
			{
				if(aInter.IntersectionPolygon.IsInside(vs.Front) && (aInter.StoppedExits != null && aInter.StoppedExits.Count > 0))
					return null;
			}
				
			// get the distance to the next lane major
			ArbiterWaypoint nextWaypoint = current.GetClosestPartition(vs.Front).Final;
			List<WaypointType> majorLaneTypes = new List<WaypointType>(new WaypointType[]{ WaypointType.End, WaypointType.Stop});
			double distanceToNextMajor = current.DistanceBetween(vs.Front, current.GetNext(nextWaypoint, majorLaneTypes, new List<ArbiterWaypoint>()).Position);

			// check distance > 3.0
			if(distanceToNextMajor >  30.0)
			{
				// check clear on right, or if it does not exist here
				ILateralReasoning rightLateral = this.tacticalUmbrella.roadTactical.rightLateralReasoning;
				bool useRight = rightLateral.Exists && rightLateral.ExistsExactlyHere(vs) && rightLateral is LateralReasoning;

				// check clear on left, or if it does not exist here
				ILateralReasoning leftLateral = this.tacticalUmbrella.roadTactical.leftLateralReasoning;
				bool useLeft = leftLateral.Exists && leftLateral.ExistsExactlyHere(vs) && leftLateral is LateralReasoning;

				// notify
				ArbiterOutput.Output("Blockage recovery: lane change left ok to use: " + useLeft.ToString() + ", use righrt: " + useRight.ToString());

				#region Test Right

				// determine if should use, should wait, or should not use
				LaneChangeMonitorResult rightLCMR = LaneChangeMonitorResult.DontUse;

				// check usability of the right lateral maneuver
				if(useRight && rightLateral is LateralReasoning)
				{
					// check adj and rear clear
					bool adjRearClear = rightLateral.AdjacentAndRearClear(vs);

					// wait maybe if not clear
					if(!adjRearClear && (rightLateral.AdjacentVehicle == null || rightLateral.AdjacentVehicle.QueuingState.Queuing != QueuingState.Failed))
						rightLCMR = LaneChangeMonitorResult.Wait;
					else if(adjRearClear)
						rightLCMR = LaneChangeMonitorResult.Use;
					else
						rightLCMR = LaneChangeMonitorResult.DontUse;

					// check down
					if (rightLCMR == LaneChangeMonitorResult.Wait)
					{
						ArbiterOutput.Output("Blockage lane changes waiting for right lateral lane to clear");
						shouldWait = true;
						return null;
					}
					// check use
					else if (rightLCMR == LaneChangeMonitorResult.Use)
					{
						// get the behavior
						ChangeLaneBehavior rightClb = new ChangeLaneBehavior(current.LaneId, rightLateral.LateralLane.LaneId, false,
							TahoeParams.VL * 1.5, new ScalarSpeedCommand(2.24), new List<int>(), current.LanePath(), rightLateral.LateralLane.LanePath(),
							current.Width, rightLateral.LateralLane.Width, current.NumberOfLanesLeft(vs.Front, true), current.NumberOfLanesRight(vs.Front, true));

						// test
						CompletionReport rightClbCr;
						bool successCL = CoreCommon.Communications.TestExecute(rightClb, out rightClbCr);
						if (successCL && rightClbCr is SuccessCompletionReport)
						{
							ArbiterOutput.Output("Blockage lane changes found good behavior to right lateral lane");
							shouldWait = false;
							completionState = new StayInLaneState(rightLateral.LateralLane, CoreCommon.CorePlanningState);
							return rightClb;
						}
					}
				}

				#endregion

				#region Test Left Forwards

				if(useLeft && leftLateral is LateralReasoning)
				{
					// determine if should use, should wait, or should not use
					LaneChangeMonitorResult leftLCMR = LaneChangeMonitorResult.DontUse;

					// check usability of the left lateral maneuver
					if (useLeft && leftLateral is LateralReasoning)
					{
						// check adj and rear clear
						bool adjRearClear = leftLateral.AdjacentAndRearClear(vs);

						// wait maybe if not clear
						if (!adjRearClear && (leftLateral.AdjacentVehicle == null || leftLateral.AdjacentVehicle.QueuingState.Queuing != QueuingState.Failed))
							leftLCMR = LaneChangeMonitorResult.Wait;
						else if (adjRearClear)
							leftLCMR = LaneChangeMonitorResult.Use;
						else
							leftLCMR = LaneChangeMonitorResult.DontUse;

						ArbiterOutput.Output("Blockage recovery, lane change left: " + leftLCMR.ToString());

						// check down
						if (leftLCMR == LaneChangeMonitorResult.Wait)
						{
							ArbiterOutput.Output("Blockage recovery, Blockage lane changes waiting for left lateral lane to clear");
							shouldWait = true;
							return null;
						}
						// check use
						else if (leftLCMR == LaneChangeMonitorResult.Use)
						{
							// get the behavior
							ChangeLaneBehavior leftClb = new ChangeLaneBehavior(current.LaneId, leftLateral.LateralLane.LaneId, false,
								TahoeParams.VL * 1.5, new ScalarSpeedCommand(2.24), new List<int>(), current.LanePath(), leftLateral.LateralLane.LanePath(),
								current.Width, leftLateral.LateralLane.Width, current.NumberOfLanesLeft(vs.Front, true), current.NumberOfLanesLeft(vs.Front, true));

							// test
							CompletionReport leftClbCr;
							bool successCL = CoreCommon.Communications.TestExecute(leftClb, out leftClbCr);
							if (successCL && leftClbCr is SuccessCompletionReport)
							{
								ArbiterOutput.Output("Blockage recovery, Blockage lane changes found good behavior to left lateral lane");
								completionState = new StayInLaneState(leftLateral.LateralLane, CoreCommon.CorePlanningState);
								shouldWait = false;
								return leftClb;
							}
						}
					}
				}

				#endregion
			}

			// fallout return null
			return null;
		}

		#endregion

		#region Opposing Lane Recovery

		/// <summary>
		/// Maneuver for recovering from a lane blockage, used for lane changes as well
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="vehicleState"></param>
		/// <param name="vehicleSpeed"></param>
		/// <param name="defcon"></param>
		/// <param name="saudi"></param>
		/// <returns></returns>
		public Maneuver OpposingLaneRecoveryManeuver(ArbiterLane lane, VehicleState vehicleState, double vehicleSpeed,
			INavigationalPlan plan, BlockageRecoveryState brs, bool failedVehiclesPersistentlyOnly)
		{
			// get the blockage
			ITacticalBlockage laneBlockageReport = brs.EncounteredState.TacticalBlockage;

			// get the turn state
			OpposingLanesState sils = (OpposingLanesState)brs.EncounteredState.PlanningState;

			#region Reverse

			// check the state of the recovery for being the initial state
			if (brs.Defcon == BlockageRecoveryDEFCON.INITIAL)
			{
				// determine if the reverse behavior is recommended
				if (laneBlockageReport.BlockageReport.ReverseRecommended)
				{
					// notify
					ArbiterOutput.Output("Initial encounter, reverse recommended, reversing");
					
					// path
					LinePath revPath = lane.LanePath().Clone();
					revPath.Reverse();

					// dist to reverse
					double distToReverse;
					if (double.IsNaN(laneBlockageReport.BlockageReport.DistanceToBlockage))
						distToReverse = TahoeParams.VL * 1.5;
					else if (laneBlockageReport.BlockageReport.DistanceToBlockage < 0 || laneBlockageReport.BlockageReport.DistanceToBlockage > 2 * TahoeParams.VL)
						distToReverse = TahoeParams.VL * 1.5;
					else
						distToReverse = TahoeParams.VL - laneBlockageReport.BlockageReport.DistanceToBlockage;						

					// get reverse behavior
					StayInLaneBehavior reverseRecovery = new StayInLaneBehavior(lane.LaneId,
						new StopAtDistSpeedCommand(TahoeParams.VL * 2.0, true), new List<int>(),
						revPath, lane.Width, lane.NumberOfLanesLeft(vehicleState.Front, false), lane.NumberOfLanesRight(vehicleState.Front, false));

					// get recovery state
					BlockageRecoveryState brsT = new BlockageRecoveryState(
						reverseRecovery, CoreCommon.CorePlanningState, new OpposingLanesState(lane, false, CoreCommon.CorePlanningState, vehicleState),
						brs.Defcon = BlockageRecoveryDEFCON.REVERSE, brs.EncounteredState, BlockageRecoverySTATUS.EXECUTING);
					brsT.CompletionState = brsT;

					// reset blockages
					BlockageHandler.SetDefaultBlockageCooldown();

					// maneuver
					return new Maneuver(reverseRecovery, brsT, TurnDecorators.HazardDecorator, vehicleState.Timestamp);
				}
			}

			#endregion

			#region Recovery Escalation

			// plan forward reasoning
			this.tacticalUmbrella.roadTactical.opposingReasoning.ForwardManeuver(lane, 
				((OpposingLanesState)brs.EncounteredState.PlanningState).ClosestGoodLane,
				vehicleState, (RoadPlan)plan, new List<ITacticalBlockage>());

			// check clear on right, or if it does nto exist here
			ILateralReasoning rightLateral = this.tacticalUmbrella.roadTactical.rightLateralReasoning;
			bool rightClear = !rightLateral.Exists || !rightLateral.ExistsExactlyHere(vehicleState) ||
				rightLateral.AdjacentAndRearClear(vehicleState);

			// check clear on left, or if it does nto exist here
			ILateralReasoning leftLateral = this.tacticalUmbrella.roadTactical.leftLateralReasoning;
			bool leftClear = !leftLateral.Exists || !leftLateral.ExistsExactlyHere(vehicleState) ||
				leftLateral.AdjacentAndRearClear(vehicleState);
			if (leftClear && leftLateral is OpposingLateralReasoning)
				leftClear = leftLateral.ForwardClear(vehicleState, TahoeParams.VL * 3.0, 2.24,
					new LaneChangeInformation(LaneChangeReason.FailedForwardVehicle, null),
					lane.LanePath().AdvancePoint(lane.LanePath().GetClosestPoint(vehicleState.Front), TahoeParams.VL * 3.0).Location);

			// check both clear to widen
			bool widenOk = rightClear && leftClear;

			// Notify
			ArbiterOutput.Output("Blockage tactical recovery escalation: rightClear: " + rightClear.ToString() + ", leftClear: " + leftClear.ToString());

			// path
			LinePath revPath2 = lane.LanePath().Clone();
			revPath2.Reverse();
			
			// check widen
			if (widenOk)
			{
				// output
				ArbiterOutput.Output("Lane Blockage Recovery: Adjacent areas clear");

				if (brs.Defcon != BlockageRecoveryDEFCON.CHANGELANES_FORWARD)
				{
					// check change lanes back to good
					ChangeLaneBehavior clb = new ChangeLaneBehavior(
						lane.LaneId, sils.ClosestGoodLane.LaneId, false, TahoeParams.VL * 3.0, new StopAtDistSpeedCommand(TahoeParams.VL * 3.0), new List<int>(),
						revPath2, sils.ClosestGoodLane.LanePath(), lane.Width, sils.ClosestGoodLane.Width,
						lane.NumberOfLanesLeft(vehicleState.Front, false), lane.NumberOfLanesRight(vehicleState.Front, false));

					// change lanes
					brs.Defcon = BlockageRecoveryDEFCON.CHANGELANES_FORWARD;

					// cehck ok
					CompletionReport clROk;
					bool clCheck = CoreCommon.Communications.TestExecute(clb, out clROk);

					// check change lanes ok
					if (clCheck)
					{
						ArbiterOutput.Output("Change lanes behaviro ok, executing");
						// return manevuer
						return new Maneuver(clb, brs, TurnDecorators.RightTurnDecorator, vehicleState.Timestamp);
					}
					else
					{
						ArbiterOutput.Output("Change lanes behaviro not ok");
					}
				}

				// if we can't widen for some reason just go through with no widen
				StayInLaneBehavior silb = new StayInLaneBehavior(lane.LaneId,
				new StopAtDistSpeedCommand(TahoeParams.VL * 2.0), new List<int>(),
				revPath2,
				lane.Width, lane.NumberOfLanesLeft(vehicleState.Front, false), lane.NumberOfLanesRight(vehicleState.Front, false));

				// mini icrease width
				silb.LaneWidth = silb.LaneWidth + TahoeParams.T;

				// check execute none saudi
				CompletionReport l0Cr;
				bool l0TestOk = CoreCommon.Communications.TestExecute(silb, out l0Cr);

				// check mini ok
				if (l0TestOk)
				{
					// notify
					ArbiterOutput.Output("Lane Blockage Recovery: Test Tahoe.T lane widen ok");

					// update the current state
					BlockageRecoveryState brsL0 = new BlockageRecoveryState(
						silb, sils, sils, BlockageRecoveryDEFCON.WIDENBOUNDS, brs.EncounteredState, BlockageRecoverySTATUS.EXECUTING);
					return new Maneuver(silb, brsL0, TurnDecorators.NoDecorators, vehicleState.Timestamp);
				}
				else
				{
					// notify
					ArbiterOutput.Output("Lane Blockage Recovery: Test Tahoe.T lane widen failed, moving to large widen");					

					// increase width
					silb.LaneWidth = silb.LaneWidth * 3.0;

					// check execute l1
					CompletionReport l1Cr;
					bool l1TestOk = CoreCommon.Communications.TestExecute(silb, out l1Cr);

					// check ok
					if (l1TestOk)
					{
						// notify
						ArbiterOutput.Output("Lane Blockage Recovery: Test 3LW lane large widen ok");

						// update the current state
						BlockageRecoveryState brsL1 = new BlockageRecoveryState(
							silb, sils, sils, BlockageRecoveryDEFCON.WIDENBOUNDS, brs.EncounteredState, BlockageRecoverySTATUS.EXECUTING);
						return new Maneuver(silb, brsL1, TurnDecorators.NoDecorators, vehicleState.Timestamp);
					}
					// go to l2 for all the marbles
					else
					{
						// notify
						ArbiterOutput.Output("Lane Blockage Recovery: Test 3LW lane large widen failed, moving to 3LW, L1 Saudi");

						ShutUpAndDoItDecorator saudi2 = new ShutUpAndDoItDecorator(SAUDILevel.L1);
						List<BehaviorDecorator> saudi2bds = new List<BehaviorDecorator>(new BehaviorDecorator[] { saudi2 });
						silb.Decorators = saudi2bds;
						BlockageRecoveryState brsL2 = new BlockageRecoveryState(
							silb, sils, sils, BlockageRecoveryDEFCON.WIDENBOUNDS, brs.EncounteredState, BlockageRecoverySTATUS.EXECUTING);

						// check execute l1
						CompletionReport l2Cr;
						bool l2TestOk = CoreCommon.Communications.TestExecute(silb, out l1Cr);

						// go
						return new Maneuver(silb, brsL2, saudi2bds, vehicleState.Timestamp);
					}
				}
			}

			#endregion

			// fallout goes back to lane state
			return new Maneuver(new HoldBrakeBehavior(), CoreCommon.CorePlanningState, TurnDecorators.NoDecorators, vehicleState.Timestamp);
		}

		#endregion

		/// <summary>
		/// ALl polygons of all obstacles and stopped vehicles
		/// </summary>
		/// <param name="vehicleState"></param>
		/// <returns></returns>
		public static List<Polygon> AllObstacleAndStoppedVehiclePolygons(VehicleState vehicleState, bool failedVehiclesPersistentlyOnly)
		{
			// all polygons to include
			List<Polygon> allPolys = new List<Polygon>();

			// get list of all point collections
			List<Coordinates[]> includePolys = new List<Coordinates[]>();

			// get all stopped vehicle polys
			SceneEstimatorTrackedClusterCollection setcc = CoreCommon.Communications.GetObservedVehicles();
			for (int i = 0; i < setcc.clusters.Length; i++)
			{
				// check failed vehicle
				if (failedVehiclesPersistentlyOnly && TacticalDirector.ValidVehicles.ContainsKey(setcc.clusters[i].id))
				{
					VehicleAgent va = TacticalDirector.ValidVehicles[setcc.clusters[i].id];
					if (va.QueuingState.Queuing == QueuingState.Failed)
					{
						if (setcc.clusters[i].relativePoints.Length > 2)
							includePolys.Add(setcc.clusters[i].relativePoints);
					}
				}
				// check stopped
				else if (setcc.clusters[i].relativePoints.Length > 2 && setcc.clusters[i].isStopped)
					includePolys.Add(setcc.clusters[i].relativePoints);
			}

			// get all static polygons
			SceneEstimatorUntrackedClusterCollection seucc = CoreCommon.Communications.GetObservedObstacles();
			for (int i = 0; i < seucc.clusters.Length; i++)
			{
				if (seucc.clusters[i].points.Length > 2)
					includePolys.Add(seucc.clusters[i].points);
			}

			// loop through point collections
			VehicleAgent tmp = new VehicleAgent(-1);
			foreach (Coordinates[] points in includePolys)
			{
				List<Coordinates> polyPoints = new List<Coordinates>();
				for (int j = 0; j < points.Length; j++)
				{
					polyPoints.Add(tmp.TransformCoordAbs(points[j], vehicleState));
				}
				allPolys.Add(Polygon.GrahamScan(polyPoints));
			}

			return allPolys;
		}

		public static List<Polygon> AllForwardPolygons(VehicleState vehicleState, bool failedVehiclesPersistentlyOnly)
		{
			try
			{
				List<Polygon> final = new List<Polygon>();
				List<Polygon> allPolys = AllObstacleAndStoppedVehiclePolygons(vehicleState, failedVehiclesPersistentlyOnly);
				Polygon forwardPoly = vehicleState.SmallForwardPolygon;
				foreach (Polygon p in allPolys)
				{
					if (forwardPoly.IsInside(p))
						final.Add(p);
				}
				return final;
			}
			catch (Exception) { }

			return new List<Polygon>();
		}

		public void Reset()
		{
			if (uTurnTimer == null)
				uTurnTimer = new Stopwatch();

			uTurnTimer.Stop();
			uTurnTimer.Reset();

			if (ExecutionTimer == null)
				ExecutionTimer = new Stopwatch();

			ExecutionTimer.Stop();
			ExecutionTimer.Reset();
		}
	}

	/// <summary>
	/// Result of lane change test
	/// </summary>
	public enum LaneChangeMonitorResult
	{
		Wait,
		DontUse,
		Use
	}
}
