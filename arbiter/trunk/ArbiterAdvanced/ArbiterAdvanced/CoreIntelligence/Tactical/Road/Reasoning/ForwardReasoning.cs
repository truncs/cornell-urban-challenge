using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Arbiter.Core.Common;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Navigation;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road.Reasoning;
using UrbanChallenge.Common;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Vehicles;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road.Common;
using UrbanChallenge.Arbiter.Core.Common.State;
using UrbanChallenge.Arbiter.Core.Common.Common;
using UrbanChallenge.Arbiter.Core.Communications;
using UrbanChallenge.Common.Sensors;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road.Opposing.Reasoning;
using System.Diagnostics;
using UrbanChallenge.Arbiter.Core.Common.Tools;
using UrbanChallenge.Behaviors.CompletionReport;
using GpcWrapper;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Blockage;
using UrbanChallenge.Arbiter.ArbiterMission;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road
{
	/// <summary>
	/// Reasons about the world ahead of us
	/// </summary>
	public class ForwardReasoning
	{
		#region Private Members

		/// <summary>
		/// Reasons about the lane to the left of us
		/// </summary>
		private ILateralReasoning leftLateralReasoning;

		/// <summary>
		/// Reasons about the lane to the right of us
		/// </summary>
		private ILateralReasoning rightLateralReasoning;

		private ILateralReasoning secondaryLeftLateralReasoning;

		private Stopwatch blockageTimer;

		#endregion

		#region Public Members

		/// <summary>
		/// Lane this reasons upon
		/// </summary>
		public IFQMPlanable Lane;

		/// <summary>
		/// Monitors road ahead
		/// </summary>
		public ForwardQuadrantMonitor ForwardMonitor;

		/// <summary>
		/// Monitors the road behind us
		/// </summary>
		public RearQuadrantMonitor RearMonitor;

		#endregion

		#region Cosntructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="leftLateral"></param>
		/// <param name="rightLateral"></param>
		public ForwardReasoning(ILateralReasoning leftLateral, ILateralReasoning rightLateral, IFQMPlanable lane)
		{
			this.Lane = lane;
			this.leftLateralReasoning = leftLateral;
			this.rightLateralReasoning = rightLateral;
			this.ForwardMonitor = new ForwardQuadrantMonitor();
			this.blockageTimer = new Stopwatch();

			if (lane is ArbiterLane)
			{
				this.RearMonitor = new RearQuadrantMonitor((ArbiterLane)lane, SideObstacleSide.Driver);
			}
			else
			{
				this.RearMonitor = new RearQuadrantMonitor(((SupraLane)lane).Initial, SideObstacleSide.Driver);
			}
		}

		#endregion

		#region Functions

		/// <summary>
		/// Reset value held over time
		/// </summary>
		public void Reset()
		{
			if(this.ForwardMonitor != null)
				ForwardMonitor.Reset();

			if(this.RearMonitor != null)
				RearMonitor.Reset();

			if (this.leftLateralReasoning != null)
				this.leftLateralReasoning.Reset();

			if (this.rightLateralReasoning != null)
				this.rightLateralReasoning.Reset();

			if (this.secondaryLeftLateralReasoning != null)
				this.secondaryLeftLateralReasoning.Reset();

			this.blockageTimer.Stop();
			this.blockageTimer.Reset();
		}

		/// <summary>
		/// Plans the forward maneuver and secondary maneuver
		/// </summary>
		/// <param name="arbiterLane"></param>
		/// <param name="vehicleState"></param>
		/// <param name="p"></param>
		/// <param name="blockage"></param>
		/// <returns></returns>
		public Maneuver ForwardManeuver(IFQMPlanable arbiterLane, VehicleState vehicleState, RoadPlan roadPlan, 
			List<ITacticalBlockage> blockages, List<ArbiterWaypoint> ignorable)
		{
			// get primary parameterization
			TravelingParameters primary = this.ForwardMonitor.Primary(arbiterLane, vehicleState, roadPlan, blockages, ignorable, true);
			
			// return primary for now
			return new Maneuver(primary.Behavior, primary.NextState, primary.Decorators, vehicleState.Timestamp);
		}

		/// <summary>
		/// Makes use of parameterizations made from the initial forward maneuver plan
		/// </summary>
		/// <param name="arbiterLane"></param>
		/// <param name="vehicleState"></param>
		/// <param name="roadPlan"></param>
		/// <param name="blockages"></param>
		/// <param name="ignorable"></param>
		/// <returns></returns>
		public Maneuver? SecondaryManeuver(IFQMPlanable arbiterLane, VehicleState vehicleState, RoadPlan roadPlan,
			List<ITacticalBlockage> blockages, List<ArbiterWaypoint> ignorable, TypeOfTasks bestTask)
		{
			// check if we might be able to pass here
			bool validArea = arbiterLane is ArbiterLane || (((SupraLane)arbiterLane).ClosestComponent(vehicleState.Front) == SLComponentType.Initial);
			ArbiterLane ourForwardLane = arbiterLane is ArbiterLane ? (ArbiterLane)arbiterLane : ((SupraLane)arbiterLane).Initial;

			// check if the forward vehicle exists and we're in a valid area
			if (this.ForwardMonitor.ForwardVehicle.ShouldUseForwardTracker && validArea)
			{
				// check if we should pass the vehicle ahead
				LaneChangeInformation lci;
				bool sp = this.ForwardMonitor.ForwardVehicle.ShouldPass(out lci);

				// make sure we should do something before processing extras
				if(sp)
				{
					// available parameterizations for the lane change
					List<LaneChangeParameters> changeParams = new List<LaneChangeParameters>();

					// get lane
					ArbiterLane al = arbiterLane is ArbiterLane ? (ArbiterLane)arbiterLane : ((SupraLane)arbiterLane).Initial;

					// get the location we need to return by
					Coordinates absoluteUpperBound = arbiterLane is ArbiterLane ? 
						roadPlan.LanePlans[al.LaneId].laneWaypointOfInterest.PointOfInterest.Position : 
						((SupraLane)arbiterLane).Interconnect.InitialGeneric.Position;

					#region Failed Forward

					// if failed, parameterize ourselved if we're following them
					if (lci.Reason == LaneChangeReason.FailedForwardVehicle && this.ForwardMonitor.CurrentParameters.Type == TravellingType.Vehicle)
					{
						// notify
						ArbiterOutput.Output("Failed Forward Vehicle: " + this.ForwardMonitor.ForwardVehicle.CurrentVehicle.VehicleId.ToString());

						// get traveling params from FQM to make sure we stopped for vehicle, behind vehicle						
						double v = CoreCommon.Communications.GetVehicleSpeed().Value;
						TravelingParameters fqmParams = this.ForwardMonitor.CurrentParameters;
						double d = this.ForwardMonitor.ForwardVehicle.DistanceToVehicle(arbiterLane, vehicleState.Front);
						Coordinates departUpperBound = al.LanePath().AdvancePoint(al.LanePath().GetClosestPoint(vehicleState.Front), d - 3.0).Location;

						// check stopped behing failed forward
						try
						{
							if (fqmParams.Type == TravellingType.Vehicle && this.ForwardMonitor.ForwardVehicle.StoppedBehindForwardVehicle)
							{
								// check for checkpoint within 4VL of front of failed vehicle
								ArbiterCheckpoint acCurrecnt = CoreCommon.Mission.MissionCheckpoints.Peek();
								if (acCurrecnt.WaypointId.AreaSubtypeId.Equals(al.LaneId))
								{
									// check distance
									ArbiterWaypoint awCheckpoint = (ArbiterWaypoint)CoreCommon.RoadNetwork.ArbiterWaypoints[acCurrecnt.WaypointId];
									double cpDistacne = Lane.DistanceBetween(vehicleState.Front, awCheckpoint.Position);
									if (cpDistacne < d || cpDistacne - d < TahoeParams.VL * 4.5)
									{
										ArbiterOutput.Output("Removing checkpoint: " + acCurrecnt.WaypointId.ToString() + " as failed vehicle over it");
										CoreCommon.Mission.MissionCheckpoints.Dequeue();
										return new Maneuver(new NullBehavior(), CoreCommon.CorePlanningState, TurnDecorators.NoDecorators, vehicleState.Timestamp);
									}
								}
							}
						}catch (Exception) { }

						#region Right Lateral Reasoning Forwards

						// check right lateral reasoning for existence, if so parametrize
						if (rightLateralReasoning.Exists && fqmParams.Type == TravellingType.Vehicle && this.ForwardMonitor.ForwardVehicle.StoppedBehindForwardVehicle)
						{
							// get lane
							ArbiterLane lane = al;

							// determine failed vehicle lane change distance params									
							Coordinates defaultReturnLowerBound = al.LanePath().AdvancePoint(al.LanePath().GetClosestPoint(vehicleState.Front), d + (TahoeParams.VL * 2.0)).Location;
							Coordinates minimumReturnComplete = al.LanePath().AdvancePoint(al.LanePath().GetClosestPoint(vehicleState.Front), d + (TahoeParams.VL * 3.0)).Location;
							Coordinates defaultReturnUpperBound = al.LanePath().AdvancePoint(al.LanePath().GetClosestPoint(vehicleState.Front), d + (TahoeParams.VL * 5.0)).Location;							

							// get params for lane change
							LaneChangeParameters? lcp = this.LaneChangeParameterization(
								new LaneChangeInformation(LaneChangeReason.FailedForwardVehicle, null),
								lane, lane.LaneOnRight, false, roadPlan.BestPlan.laneWaypointOfInterest.PointOfInterest.Position,
								departUpperBound, defaultReturnLowerBound, minimumReturnComplete, defaultReturnUpperBound, blockages, ignorable,
								vehicleState, CoreCommon.Communications.GetVehicleSpeed().Value);

							// check if exists to generate full param
							if (lcp.HasValue)
							{
								// get param
								LaneChangeParameters tp = lcp.Value;

								// notify
								ArbiterOutput.WriteToLog("Failed Forward: Right Lateral Reasoning Forwards: Available: " + tp.Available.ToString() + ", Feasable: " + tp.Feasible.ToString());

								// get behavior
								ChangeLaneBehavior clb = new ChangeLaneBehavior(
									al.LaneId, al.LaneOnRight.LaneId, false, al.DistanceBetween(vehicleState.Front, departUpperBound),
									new ScalarSpeedCommand(tp.Parameters.RecommendedSpeed), tp.Parameters.VehiclesToIgnore,
									al.LanePath(), al.LaneOnRight.LanePath(), al.Width, al.LaneOnRight.Width, al.NumberOfLanesLeft(vehicleState.Front, true), al.NumberOfLanesRight(vehicleState.Front, true));
								tp.Behavior = clb;
								tp.Decorators = TurnDecorators.RightTurnDecorator;

								// next state
								ChangeLanesState cls = new ChangeLanesState(tp);
								tp.NextState = cls;

								// add parameterization to possible									
								changeParams.Add(tp);
							}
						}

						#endregion

						#region Left Lateral Reasoning

						// check left lateral reasoning
						if(leftLateralReasoning.Exists)
						{
							#region Left Lateral Opposing

							// check opposing
							ArbiterLane closestOpposingLane = this.GetClosestOpposing(ourForwardLane, vehicleState);
							if(closestOpposingLane != null && (leftLateralReasoning.IsOpposing || !closestOpposingLane.Equals(leftLateralReasoning.LateralLane)))
							{
								// check room of initial
								bool enoughRoom = 
									(arbiterLane is ArbiterLane && (!roadPlan.BestPlan.laneWaypointOfInterest.IsExit || ((ArbiterLane)arbiterLane).DistanceBetween(vehicleState.Front, roadPlan.BestPlan.laneWaypointOfInterest.PointOfInterest.Position) > TahoeParams.VL * 5.0)) ||
									(arbiterLane is SupraLane && ((SupraLane)arbiterLane).DistanceBetween(vehicleState.Front, ((SupraLane)arbiterLane).Interconnect.InitialGeneric.Position) > TahoeParams.VL * 5.0);

								// check opposing enough room
								bool oppEnough = closestOpposingLane.DistanceBetween(closestOpposingLane.LanePath().StartPoint.Location, vehicleState.Front) > TahoeParams.VL * 5.0;

								// check if enough room
								if (enoughRoom && oppEnough)
								{
									// check if we're stopped and the current trav params were for a vehicle and we're close to the vehicle
									bool stoppedBehindFV = fqmParams.Type == TravellingType.Vehicle && this.ForwardMonitor.ForwardVehicle.StoppedBehindForwardVehicle;

									// check that we're stopped behind forward vehicle before attempting to change lanes
									if (stoppedBehindFV)
									{
										#region Check Segment Blockage

										// check need to make uturn (hack)
										bool waitForUTurnCooldown;
										BlockageTactical bt = CoreCommon.BlockageDirector;
										StayInLaneBehavior tmpBlockBehavior = new StayInLaneBehavior(al.LaneId, new ScalarSpeedCommand(2.0), new List<int>(), al.LanePath(), al.Width, 0, 0);
										ITacticalBlockage itbTmp = new LaneBlockage(new TrajectoryBlockedReport(CompletionResult.Stopped, TahoeParams.VL, BlockageType.Static, -1, false, tmpBlockBehavior.GetType()));
										Maneuver tmpBlockManeuver = bt.LaneRecoveryManeuver(al, vehicleState, CoreCommon.Communications.GetVehicleSpeed().Value, roadPlan,
											new BlockageRecoveryState(tmpBlockBehavior,
											new StayInLaneState(al, CoreCommon.CorePlanningState), new StayInLaneState(al, CoreCommon.CorePlanningState), BlockageRecoveryDEFCON.REVERSE,
											new EncounteredBlockageState(itbTmp, CoreCommon.CorePlanningState, BlockageRecoveryDEFCON.REVERSE, SAUDILevel.None), BlockageRecoverySTATUS.ENCOUNTERED), true, out waitForUTurnCooldown);
										if (!waitForUTurnCooldown && tmpBlockManeuver.PrimaryBehavior is UTurnBehavior)
											return tmpBlockManeuver;
										else if (waitForUTurnCooldown)
											return null;

										#endregion

										// distance to forward vehicle too small
										double distToForwards = al.DistanceBetween(vehicleState.Front, this.ForwardMonitor.ForwardVehicle.CurrentVehicle.ClosestPosition);
										double distToReverse = Math.Max(1.0, 8.0 - distToForwards);
										if (distToForwards < 8.0)
										{
											// notify
											ArbiterOutput.WriteToLog("Secondary: NOT Properly Stopped Behind Forward Vehicle: " + this.ForwardMonitor.ForwardVehicle.CurrentVehicle.ToString() + " distance: " + distToForwards.ToString("f2"));

											this.RearMonitor = new RearQuadrantMonitor(al, SideObstacleSide.Driver);
											this.RearMonitor.Update(vehicleState);
											if (this.RearMonitor.CurrentVehicle != null)
											{
												double distToRearVehicle = al.DistanceBetween(this.RearMonitor.CurrentVehicle.ClosestPosition, vehicleState.Position) - TahoeParams.RL;
												double distNeedClear = distToReverse + 2.0;
												if (distToRearVehicle < distNeedClear)
												{
													// notify
													ArbiterOutput.Output("Secondary: Rear: Not enough room to clear in rear: " + distToRearVehicle.ToString("f2") + " < " + distNeedClear.ToString("f2"));
													return null;
												}
											}

											double distToLaneStart = al.DistanceBetween(al.LanePath().StartPoint.Location, vehicleState.Position) - TahoeParams.RL;
											if (distToReverse > distToLaneStart)
											{
												// notify
												ArbiterOutput.Output("Secondary: Rear: Not enough room in lane to reverse in rear: " + distToLaneStart.ToString("f2") + " < " + distToReverse.ToString("f2"));
												return null;
											}
											else
											{
												// notify
												ArbiterOutput.Output("Secondary: Reversing to pass Forward Vehicle: " + this.ForwardMonitor.ForwardVehicle.CurrentVehicle.ToString() + " reversing distance: " + distToReverse.ToString("f2"));
												StopAtDistSpeedCommand sadsc = new StopAtDistSpeedCommand(distToReverse, true);
												StayInLaneBehavior silb = new StayInLaneBehavior(al.LaneId, sadsc, new List<int>(), al.LanePath(), al.Width, al.NumberOfLanesLeft(vehicleState.Front, true), al.NumberOfLanesRight(vehicleState.Front, true));
												return new Maneuver(silb, CoreCommon.CorePlanningState, TurnDecorators.HazardDecorator, vehicleState.Timestamp);
											}
										}
										else
										{
											// notify
											ArbiterOutput.WriteToLog("Secondary: Left Lateral Opposing: Properly Stopped Behind Forward Vehicle: " + this.ForwardMonitor.ForwardVehicle.CurrentVehicle.ToString());

											// determine failed vehicle lane change distance params									
											Coordinates defaultReturnLowerBound = al.LanePath().AdvancePoint(al.LanePath().GetClosestPoint(vehicleState.Front), d + (TahoeParams.VL * 2.0)).Location;
											Coordinates minimumReturnComplete = al.LanePath().AdvancePoint(al.LanePath().GetClosestPoint(vehicleState.Front), d + (TahoeParams.VL * 3.5)).Location;
											Coordinates defaultReturnUpperBound = al.LanePath().AdvancePoint(al.LanePath().GetClosestPoint(vehicleState.Front), d + (TahoeParams.VL * 5.0)).Location;

											// check if enough room
											if (al.DistanceBetween(vehicleState.Front, defaultReturnUpperBound) >= d + TahoeParams.VL * 4.5)
											{
												// get hte closest oppoing
												ArbiterLane closestOpposing = this.GetClosestOpposing(al, vehicleState);

												// check exists
												if (closestOpposing != null)
												{
													// set/check secondary
													if (this.secondaryLeftLateralReasoning == null || !this.secondaryLeftLateralReasoning.LateralLane.Equals(closestOpposing))
														this.secondaryLeftLateralReasoning = new OpposingLateralReasoning(closestOpposing, SideObstacleSide.Driver);

													// check the state of hte lanes next to us
													if (this.leftLateralReasoning.LateralLane.Equals(closestOpposing) && this.leftLateralReasoning.ExistsExactlyHere(vehicleState))
													{
														#region Plan

														// need to make sure that we wait for 3 seconds with the blinker on (resetting with pause)
														if (this.ForwardMonitor.ForwardVehicle.CurrentVehicle.QueuingState.WaitTimer.ElapsedMilliseconds > 3000)
														{
															// notify
															ArbiterOutput.Output("Scondary: Left Lateral Opposing: Wait Timer DONE");

															// get parameterization
															LaneChangeParameters? tp = this.LaneChangeParameterization(
																new LaneChangeInformation(LaneChangeReason.FailedForwardVehicle, this.ForwardMonitor.ForwardVehicle.CurrentVehicle),
																al,
																leftLateralReasoning.LateralLane,
																true,
																roadPlan.BestPlan.laneWaypointOfInterest.PointOfInterest.Position,
																departUpperBound,
																defaultReturnLowerBound,
																minimumReturnComplete,
																defaultReturnUpperBound,
																blockages,
																ignorable,
																vehicleState,
																CoreCommon.Communications.GetVehicleSpeed().Value);

															// check if available and feasible
															if (tp.HasValue && tp.Value.Available && tp.Value.Feasible)
															{
																// notify
																ArbiterOutput.Output("Scondary: Left Lateral Opposing: AVAILABLE & FEASIBLE");

																LaneChangeParameters lcp = tp.Value;
																lcp.Behavior = this.ForwardMonitor.CurrentParameters.Behavior;
																lcp.Decorators = TurnDecorators.LeftTurnDecorator;
																lcp.Behavior.Decorators = lcp.Decorators;

																// next state
																ChangeLanesState cls = new ChangeLanesState(tp.Value);
																lcp.NextState = cls;

																// add parameterization to possible									
																changeParams.Add(lcp);
															}
															// check if not available now but still feasible
															else if (tp.HasValue && !tp.Value.Available && tp.Value.Feasible)
															{
																// notify
																ArbiterOutput.Output("Scondary: Left Lateral Opposing: NOT Available, Still FEASIBLE, WAITING");

																// wait and blink maneuver
																TravelingParameters tp2 = this.ForwardMonitor.CurrentParameters;
																tp2.Decorators = TurnDecorators.LeftTurnDecorator;
																tp2.Behavior.Decorators = tp2.Decorators;

																// create parameterization
																LaneChangeParameters lcp = new LaneChangeParameters(false, true, al, false, al.LaneOnLeft,
																	true, true, tp2.Behavior, 0.0, CoreCommon.CorePlanningState, tp2.Decorators, tp2, new Coordinates(),
																	new Coordinates(), new Coordinates(), new Coordinates(), LaneChangeReason.FailedForwardVehicle);

																// add parameterization to possible									
																changeParams.Add(lcp);
															}
														}
														// otherwise timer not running or not been long enough
														else
														{
															// check if timer running
															if (!this.ForwardMonitor.ForwardVehicle.CurrentVehicle.QueuingState.WaitTimer.IsRunning)
																this.ForwardMonitor.ForwardVehicle.CurrentVehicle.QueuingState.WaitTimer.Start();

															double waited = (double)(this.ForwardMonitor.ForwardVehicle.CurrentVehicle.QueuingState.WaitTimer.ElapsedMilliseconds / 1000.0);
															ArbiterOutput.Output("Waited for failed forwards: " + waited.ToString("F2") + " seconds");

															// wait and blink maneuver
															TravelingParameters tp = this.ForwardMonitor.CurrentParameters;
															tp.Decorators = TurnDecorators.LeftTurnDecorator;
															tp.Behavior.Decorators = tp.Decorators;

															// create parameterization
															LaneChangeParameters lcp = new LaneChangeParameters(false, true, al, false, al.LaneOnLeft,
																true, true, tp.Behavior, 0.0, CoreCommon.CorePlanningState, tp.Decorators, tp, new Coordinates(),
																new Coordinates(), new Coordinates(), new Coordinates(), LaneChangeReason.FailedForwardVehicle);

															// add parameterization to possible									
															changeParams.Add(lcp);
														}

														#endregion
													}
													else if (!this.leftLateralReasoning.LateralLane.Equals(closestOpposing) && !this.leftLateralReasoning.ExistsRelativelyHere(vehicleState))
													{
														// set and notify
														ArbiterOutput.Output("superceeded left lateral reasoning with override for non adjacent left lateral reasoning");
														ILateralReasoning tmpReasoning = this.leftLateralReasoning;
														this.leftLateralReasoning = this.secondaryLeftLateralReasoning;

														try
														{
															#region Plan

															// need to make sure that we wait for 3 seconds with the blinker on (resetting with pause)
															if (this.ForwardMonitor.ForwardVehicle.CurrentVehicle.QueuingState.WaitTimer.ElapsedMilliseconds > 3000)
															{
																// notify
																ArbiterOutput.Output("Scondary: Left Lateral Opposing: Wait Timer DONE");

																// get parameterization
																LaneChangeParameters? tp = this.LaneChangeParameterization(
																	new LaneChangeInformation(LaneChangeReason.FailedForwardVehicle, this.ForwardMonitor.ForwardVehicle.CurrentVehicle),
																	al,
																	leftLateralReasoning.LateralLane,
																	true,
																	roadPlan.BestPlan.laneWaypointOfInterest.PointOfInterest.Position,
																	departUpperBound,
																	defaultReturnLowerBound,
																	minimumReturnComplete,
																	defaultReturnUpperBound,
																	blockages,
																	ignorable,
																	vehicleState,
																	CoreCommon.Communications.GetVehicleSpeed().Value);

																// check if available and feasible
																if (tp.HasValue && tp.Value.Available && tp.Value.Feasible)
																{
																	// notify
																	ArbiterOutput.Output("Scondary: Left Lateral Opposing: AVAILABLE & FEASIBLE");

																	LaneChangeParameters lcp = tp.Value;
																	lcp.Behavior = this.ForwardMonitor.CurrentParameters.Behavior;
																	lcp.Decorators = TurnDecorators.LeftTurnDecorator;
																	lcp.Behavior.Decorators = TurnDecorators.LeftTurnDecorator;

																	// next state
																	ChangeLanesState cls = new ChangeLanesState(tp.Value);
																	lcp.NextState = cls;																	

																	// add parameterization to possible									
																	changeParams.Add(lcp);
																}
																// check if not available now but still feasible
																else if (tp.HasValue && !tp.Value.Available && tp.Value.Feasible)
																{
																	// notify
																	ArbiterOutput.Output("Scondary: Left Lateral Opposing: NOT Available, Still FEASIBLE, WAITING");

																	// wait and blink maneuver
																	TravelingParameters tp2 = this.ForwardMonitor.CurrentParameters;
																	tp2.Decorators = TurnDecorators.LeftTurnDecorator;
																	tp2.Behavior.Decorators = tp2.Decorators;

																	// create parameterization
																	LaneChangeParameters lcp = new LaneChangeParameters(false, true, al, false, this.leftLateralReasoning.LateralLane,
																		true, true, tp2.Behavior, 0.0, CoreCommon.CorePlanningState, tp2.Decorators, tp2, new Coordinates(),
																		new Coordinates(), new Coordinates(), new Coordinates(), LaneChangeReason.FailedForwardVehicle);

																	// add parameterization to possible									
																	changeParams.Add(lcp);
																}
															}
															// otherwise timer not running or not been long enough
															else
															{
																// check if timer running
																if (!this.ForwardMonitor.ForwardVehicle.CurrentVehicle.QueuingState.WaitTimer.IsRunning)
																	this.ForwardMonitor.ForwardVehicle.CurrentVehicle.QueuingState.WaitTimer.Start();

																double waited = (double)(this.ForwardMonitor.ForwardVehicle.CurrentVehicle.QueuingState.WaitTimer.ElapsedMilliseconds / 1000.0);
																ArbiterOutput.Output("Waited for failed forwards: " + waited.ToString("F2") + " seconds");

																// wait and blink maneuver
																TravelingParameters tp = this.ForwardMonitor.CurrentParameters;
																tp.Decorators = TurnDecorators.LeftTurnDecorator;
																tp.Behavior.Decorators = tp.Decorators;

																// create parameterization
																LaneChangeParameters lcp = new LaneChangeParameters(false, true, al, false, this.leftLateralReasoning.LateralLane,
																	true, true, tp.Behavior, 0.0, CoreCommon.CorePlanningState, tp.Decorators, tp, new Coordinates(),
																	new Coordinates(), new Coordinates(), new Coordinates(), LaneChangeReason.FailedForwardVehicle);

																// add parameterization to possible									
																changeParams.Add(lcp);
															}

															#endregion
														}
														catch (Exception ex)
														{
															ArbiterOutput.Output("Core intelligence thread caught exception in forward reasoning secondary maneuver when non-standard adjacent left: " + ex.ToString());
														}

														// restore
														this.leftLateralReasoning = tmpReasoning;
													}
												}
												else
												{
													// do nuttin
													ArbiterOutput.Output("no opposing adjacent");
												}
											}
											else
											{
												// notify
												ArbiterOutput.Output("Secondary: LeftLatOpp: Stopped Behind FV: " + this.ForwardMonitor.ForwardVehicle.CurrentVehicle.ToString() + ", but not enough room to pass");
											}
										}
									}
									else
									{
										this.ForwardMonitor.ForwardVehicle.CurrentVehicle.QueuingState.WaitTimer.Stop();
										this.ForwardMonitor.ForwardVehicle.CurrentVehicle.QueuingState.WaitTimer.Reset();

										// notify
										ArbiterOutput.Output("Secondary: Left Lateral Opposing: NOT Stopped Behind Forward Vehicle: " + this.ForwardMonitor.ForwardVehicle.CurrentVehicle.ToString());
									}
								}
								else
								{
									ArbiterOutput.Output("Secondary Opposing: enough room to pass opposing: initial: " + enoughRoom.ToString() + ", opposing: " + oppEnough.ToString());
								}
							}

							#endregion

							#region Left Lateral Forwards
							
							// otherwise parameterize
							else if(fqmParams.Type == TravellingType.Vehicle && this.ForwardMonitor.ForwardVehicle.StoppedBehindForwardVehicle)
							{
								// get lane
								ArbiterLane lane = al;

								// determine failed vehicle lane change distance params									
								Coordinates defaultReturnLowerBound = al.LanePath().AdvancePoint(al.LanePath().GetClosestPoint(vehicleState.Front), d + (TahoeParams.VL * 2.0)).Location;
								Coordinates minimumReturnComplete = al.LanePath().AdvancePoint(al.LanePath().GetClosestPoint(vehicleState.Front), d + (TahoeParams.VL * 3.0)).Location;
								Coordinates defaultReturnUpperBound = al.LanePath().AdvancePoint(al.LanePath().GetClosestPoint(vehicleState.Front), d + (TahoeParams.VL * 5.0)).Location;							

								// get params for lane change
								LaneChangeParameters? lcp = this.LaneChangeParameterization(
									new LaneChangeInformation(LaneChangeReason.FailedForwardVehicle, null),
									lane, lane.LaneOnLeft, false, roadPlan.BestPlan.laneWaypointOfInterest.PointOfInterest.Position,
									departUpperBound, defaultReturnLowerBound, minimumReturnComplete, defaultReturnUpperBound,
									blockages, ignorable, vehicleState, CoreCommon.Communications.GetVehicleSpeed().Value);

								// check if exists to generate full param
								if (lcp.HasValue)
								{
									// set param
									LaneChangeParameters tp = lcp.Value;

									// notify
									ArbiterOutput.Output("Secondary Failed Forward Reasoning Forwards: Available: " + tp.Available.ToString() + ", Feasible: " + tp.Feasible.ToString());

									// get behavior
									ChangeLaneBehavior clb = new ChangeLaneBehavior(
										al.LaneId, al.LaneOnLeft.LaneId, true, al.DistanceBetween(vehicleState.Front, departUpperBound),
										new ScalarSpeedCommand(tp.Parameters.RecommendedSpeed), tp.Parameters.VehiclesToIgnore,
										al.LanePath(), al.LaneOnLeft.LanePath(), al.Width, al.LaneOnLeft.Width, al.NumberOfLanesLeft(vehicleState.Front, true), al.NumberOfLanesRight(vehicleState.Front, true));
									tp.Behavior = clb;
									tp.Decorators = TurnDecorators.LeftTurnDecorator;

									// next state
									ChangeLanesState cls = new ChangeLanesState(tp);
									tp.NextState = cls;

									// add parameterization to possible									
									changeParams.Add(tp);
								}
							}

							#endregion
						}

						#endregion
					}

					#endregion

					#region Slow Forward

					// if pass, determine if should pass in terms or vehicles adjacent and in front then call lane change function for maneuver
					else if (lci.Reason == LaneChangeReason.SlowForwardVehicle)
					{
						// if left exists and is not opposing, parameterize
						if (leftLateralReasoning.Exists && !leftLateralReasoning.IsOpposing)
						{
							throw new Exception("slow forward vehicle pass not implemented yet");
						}

						// if right exists and is not opposing, parameterize
						if (rightLateralReasoning.Exists && !rightLateralReasoning.IsOpposing)
						{
							throw new Exception("slow forward vehicle pass not implemented yet");
						}
					}

					#endregion

					#region Parameterize

					// check params to see if any are good and available
					if(changeParams != null && changeParams.Count > 0)
					{
						// sort the parameterizations
						changeParams.Sort();

						// get first
						LaneChangeParameters final = changeParams[0];

						// notify
						ArbiterOutput.Output("Secondary Reasoning Final: Available: " + final.Available.ToString() + ", Feasible: " + final.Feasible.ToString());

						// make sure ok
						if (final.Available && final.Feasible)
						{
							// return best param
							return new Maneuver(changeParams[0].Behavior, changeParams[0].NextState, changeParams[0].Decorators, vehicleState.Timestamp);
						}
					}
					#endregion
				}
			}
			
			// fallout is null
			return null;
		}

		/// <summary>
		/// Distinctly want to make lane change, parameters for doing so
		/// </summary>
		/// <param name="arbiterLane"></param>
		/// <param name="left"></param>
		/// <param name="vehicleState"></param>
		/// <param name="roadPlan"></param>
		/// <param name="blockages"></param>
		/// <param name="ignorable"></param>
		/// <returns></returns>
		public Maneuver? LaneChangeManeuver(ArbiterLane lane, bool left, ArbiterWaypoint goal, VehicleState vehicleState,
			List<ITacticalBlockage> blockages, List<ArbiterWaypoint> ignorable, LaneChangeInformation laneChangeInformation, Maneuver? secondary,
			out LaneChangeParameters parameters)
		{
			// distance until the change is complete
			double distanceToUpperBound = lane.DistanceBetween(vehicleState.Front, goal.Position);
			double neededDistance = (1.5 * TahoeParams.VL * Math.Max(Math.Abs(goal.Lane.LaneId.Number - lane.LaneId.Number), 1)) + 
				(-Math.Pow(CoreCommon.Communications.GetVehicleSpeed().Value, 2) / (4 * CoreCommon.MaximumNegativeAcceleration));

			parameters = new LaneChangeParameters();
			if (distanceToUpperBound < neededDistance)
				return null;

			Coordinates upperBound = new Coordinates();
			Coordinates upperReturnBound = new Coordinates();
			Coordinates minimumReturnBound = new Coordinates();
			Coordinates defaultReturnBound = new Coordinates();

			if (laneChangeInformation.Reason == LaneChangeReason.FailedForwardVehicle)
			{
				double distToForwards = Math.Min(neededDistance, lane.DistanceBetween(vehicleState.Front, this.ForwardMonitor.ForwardVehicle.CurrentVehicle.ClosestPosition) - 2.0);
				upperBound = lane.LanePath().AdvancePoint(lane.LanePath().GetClosestPoint(vehicleState.Front), distToForwards).Location;
				defaultReturnBound = lane.LanePath().AdvancePoint(lane.LanePath().GetClosestPoint(this.ForwardMonitor.ForwardVehicle.CurrentVehicle.ClosestPosition), TahoeParams.VL * 4.0).Location;
			}

			// get params for lane change
			LaneChangeParameters? changeParams = this.LaneChangeParameterization(
				laneChangeInformation,
				lane, left ? lane.LaneOnLeft : lane.LaneOnRight, false, goal.Position, upperBound,
				new Coordinates(), new Coordinates(), defaultReturnBound, blockages, ignorable,
				vehicleState, CoreCommon.Communications.GetVehicleSpeed().Value);

			// set lane change params			
			parameters = changeParams.HasValue ? changeParams.Value : parameters = new LaneChangeParameters();

			// check if the lane change is available or recommended
			if (changeParams != null && changeParams.Value.Feasible)
			{
				// minimize parameterizations
				List<TravelingParameters> tps = new List<TravelingParameters>();

				tps.Add(this.ForwardMonitor.LaneParameters);
				tps.Add(changeParams.Value.Parameters);
				if(this.ForwardMonitor.FollowingParameters.HasValue)
					tps.Add(this.ForwardMonitor.FollowingParameters.Value);
				
				tps.Sort();

				// check if possible to make lane change
				if (changeParams.Value.Available)
				{
					// get traveling params from FQM to make sure we stopped for vehicle, behind vehicle						
					double v = CoreCommon.Communications.GetVehicleSpeed().Value;
						
					// just use other params with shorted distance bound
					TravelingParameters final = tps[0];

					// final behavior
					ChangeLaneBehavior clb = new ChangeLaneBehavior(
						lane.LaneId,
						parameters.Target.LaneId,
						left,
						final.DistanceToGo,
						final.SpeedCommand,
						final.VehiclesToIgnore,
						lane.LanePath(),
						parameters.Target.LanePath(),
						lane.Width,
						parameters.Target.Width,
						lane.NumberOfLanesLeft(vehicleState.Front, true), lane.NumberOfLanesRight(vehicleState.Front, true));

					// final state
					ChangeLanesState cls = new ChangeLanesState(changeParams.Value);

					// return maneuver
					return new Maneuver(clb, cls, left ? TurnDecorators.LeftTurnDecorator : TurnDecorators.RightTurnDecorator, vehicleState.Timestamp);
				}
				// otherwise plan for requirements of change coming up
				else
				{
					// check if secondary exists
					if (secondary != null)
					{
						return secondary;
					}
					// otherwise plan for upcoming
					else
					{
						// get params
						TravelingParameters final = tps[0];

						// return maneuver
						return new Maneuver(tps[0].Behavior, tps[0].NextState, this.ForwardMonitor.NavigationParameters.Decorators, vehicleState.Timestamp);
					}
				}
			}

			// return null over fallout			
			return null;
		}

		/// <summary>
		/// Generates the lane change parameterization
		/// </summary>
		/// <param name="information"></param>
		/// <param name="initial"></param>
		/// <param name="final"></param>
		/// <param name="goal"></param>
		/// <param name="departUpperBound"></param>
		/// <param name="defaultReturnLowerBound"></param>
		/// <param name="minimumReturnComplete"></param>
		/// <param name="defaultReturnUpperBound"></param>
		/// <param name="blockages"></param>
		/// <param name="ignorable"></param>
		/// <param name="state"></param>
		/// <param name="speed"></param>
		/// <returns></returns>
		public LaneChangeParameters? LaneChangeParameterization(LaneChangeInformation information, ArbiterLane initial, ArbiterLane target,
			bool targetOncoming, Coordinates goal, Coordinates departUpperBound, Coordinates defaultReturnLowerBound, Coordinates minimumReturnComplete,
			Coordinates defaultReturnUpperBound, List<ITacticalBlockage> blockages, List<ArbiterWaypoint> ignorable, VehicleState state, double speed)
		 {
			// check if the target lane exists here
			bool validTarget = target.LanePath().GetClosestPoint(state.Front).Location.DistanceTo(state.Front) < 17 && target.IsInside(state.Front);

			// params
			bool toLeft = initial.LaneOnLeft != null ? initial.LaneOnLeft.Equals(target) || (targetOncoming && !initial.Way.WayId.Equals(target.Way.WayId)) : false;

			// get appropriate lateral reasoning
			ILateralReasoning lateralReasoning = toLeft ? this.leftLateralReasoning : this.rightLateralReasoning;

			#region Target Lane Valid Here

			// check if the target is currently valid
			if (validTarget)
			{
				// lane change parameterizations
				List<LaneChangeParameters> lcps = new List<LaneChangeParameters>();

				// distance to the current goal (means different things for all)
				double xGoal = initial.DistanceBetween(state.Front, goal);

				// get next stop
				List<WaypointType> types = new List<WaypointType>();
				types.Add(WaypointType.Stop);
				types.Add(WaypointType.End);
				ArbiterWaypoint nextMajor = initial.GetNext(state.Front, types, ignorable);
				double xLaneMajor = initial.DistanceBetween(state.Front, nextMajor.Position);
				xGoal = Math.Min(xGoal, xLaneMajor);

				#region Failed Vehicle Lane Change

				if (information.Reason == LaneChangeReason.FailedForwardVehicle)
				{
					#region Target Opposing

					// check if target lane backwards
					if (targetOncoming)
					{
						// available and feasible
						bool avail = false;
						bool feas = false;

						// check if min return distance < goal distance						
						double xReturnMin = initial.DistanceBetween(state.Front, minimumReturnComplete);
						double xDepart = initial.DistanceBetween(state.Front, departUpperBound);

						// dist to upper bound along lane and dist to end of adjacent lane
						double adjLaneDist = initial.DistanceBetween(state.Front, minimumReturnComplete);
						
						// this is feasible
						feas = xGoal > xReturnMin ? true : false;

						// check if not feasible that the goal is the current checkpoint
						if (!feas && CoreCommon.RoadNetwork.ArbiterWaypoints[CoreCommon.Mission.MissionCheckpoints.Peek().WaypointId].Position.Equals(goal))
							feas = true;

						// check adj and rear clear
						bool adjRearClear = lateralReasoning.AdjacentAndRearClear(state);

						// check if forwards clear
						bool frontClear = lateralReasoning.ForwardClear(state, xReturnMin, 2.24, information, minimumReturnComplete);

						Console.WriteLine("Adjacent, Rear: " + adjRearClear.ToString() + ", Forward: " + frontClear.ToString());

						// if clear
						if (frontClear && adjRearClear)
						{
							// notify
							ArbiterOutput.Output("Lane Change Params: Target Oncoming Failed Vehicle: Adjacent, Rear, and Front Clear");

							// available
							avail = true;

							// get lateral parameterization
							TravelingParameters lateralParams = this.ForwardMonitor.ParameterizationHelper(initial, lateralReasoning.LateralLane, goal, 
								state.Front, CoreCommon.CorePlanningState, state, lateralReasoning.ForwardVehicle(state));

							// change into the opposing lane wih opposing forward parameterization
							LaneChangeParameters lcp = new LaneChangeParameters(avail, feas, initial, false, target, targetOncoming, toLeft, null,
								xDepart, null, toLeft ? TurnDecorators.LeftTurnDecorator : TurnDecorators.RightTurnDecorator, lateralParams,
								departUpperBound, defaultReturnLowerBound, minimumReturnComplete, defaultReturnUpperBound, information.Reason);

							// we have been forced
							lcp.ForcedOpposing = true;

							// return created params
							return lcp;
						}											

						// fell through for some reason, return parameterization explaining why
						LaneChangeParameters fallThroughParams = new LaneChangeParameters(avail, feas, initial, false, target, targetOncoming, toLeft, null,
							xDepart, null, toLeft ? TurnDecorators.LeftTurnDecorator : TurnDecorators.RightTurnDecorator, this.ForwardMonitor.LaneParameters,
							departUpperBound, defaultReturnLowerBound, minimumReturnComplete, defaultReturnUpperBound, information.Reason);

						// return fall through parameters
						return fallThroughParams;
					}

					#endregion

					#region Target Forwards

					// otherwise target lane forwards
					else
					{
						// check lateral clear and initial lane does not run out
						if (lateralReasoning.AdjacentAndRearClear(state) && !initial.GetClosestPoint(defaultReturnUpperBound).Equals(initial.LanePath().EndPoint))
						{
							// notify
							ArbiterOutput.Output("Lane Change Params: Failed Vehicle Target Forwards: Adjacent and Rear Clear");

							// dist to upper bound along lane and dist to end of adjacent lane
							double distToReturn = initial.DistanceBetween(state.Front, defaultReturnUpperBound);
							double adjLaneDist = initial.DistanceBetween(state.Front, target.LanePath().EndPoint.Location);

							// check enough lane room to pass
							if (distToReturn < adjLaneDist && distToReturn <= initial.DistanceBetween(state.Front, goal))
							{
								// check enough room to change lanes
								ArbiterWaypoint nextTargetMajor = target.GetNext(state.Front, types, ignorable);
								double xTargetLaneMajor = initial.DistanceBetween(state.Front, nextTargetMajor.Position);

								// check dist needed to complete
								double neededDistance = (1.5 * TahoeParams.VL * Math.Abs(initial.LaneId.Number - target.LaneId.Number)) + 
								(-Math.Pow(CoreCommon.Communications.GetVehicleSpeed().Value, 2) / (4 * CoreCommon.MaximumNegativeAcceleration));
								
								// check return dist
								if (distToReturn < xTargetLaneMajor && neededDistance <= xTargetLaneMajor)
								{
									// parameters for traveling in current lane to hit other
									List<TravelingParameters> tps = new List<TravelingParameters>();
									
									// update lateral
									((LateralReasoning)lateralReasoning).ForwardMonitor.ForwardVehicle.Update(target, state);

									// check lateral reasoning for forward vehicle badness
									if (!((LateralReasoning)lateralReasoning).ForwardMonitor.ForwardVehicle.ShouldUseForwardTracker || 
										!((LateralReasoning)lateralReasoning).ForwardMonitor.ForwardVehicle.CurrentVehicle.IsStopped ||
										initial.DistanceBetween(state.Front, ((LateralReasoning)lateralReasoning).ForwardMonitor.ForwardVehicle.CurrentVehicle.ClosestPosition) >= distToReturn)
									{
										// get parameterization for lateral lane
										TravelingParameters navParams = this.ForwardMonitor.ParameterizationHelper(initial, lateralReasoning.LateralLane,
											goal, state.Front, CoreCommon.CorePlanningState, state, lateralReasoning.ForwardVehicle(state));
										tps.Add(navParams);

										// get and add the vehicle parameterization for our lane							
										if (this.ForwardMonitor.FollowingParameters.HasValue)
											tps.Add(this.ForwardMonitor.FollowingParameters.Value);

										// get final
										tps.Sort();
										TravelingParameters final = tps[0];

										// check final distance > needed
										if (navParams.DistanceToGo > neededDistance)
										{
											// set ignorable vhcs
											final.VehiclesToIgnore = this.ForwardMonitor.FollowingParameters.HasValue ? this.ForwardMonitor.FollowingParameters.Value.VehiclesToIgnore : new List<int>();
											if (((LateralReasoning)lateralReasoning).ForwardMonitor.FollowingParameters.HasValue)
												final.VehiclesToIgnore.AddRange(((LateralReasoning)lateralReasoning).ForwardMonitor.FollowingParameters.Value.VehiclesToIgnore);

											// parameterize
											LaneChangeParameters lcp = new LaneChangeParameters();
											lcp.Decorators = TurnDecorators.RightTurnDecorator;
											lcp.Available = true;
											lcp.Feasible = true;
											lcp.Parameters = final;
											lcp.Initial = initial;
											lcp.InitialOncoming = false;
											lcp.Target = target;
											lcp.TargetOncoming = false;
											lcp.Reason = LaneChangeReason.FailedForwardVehicle;
											lcp.DefaultReturnLowerBound = defaultReturnLowerBound;
											lcp.DefaultReturnUpperBound = defaultReturnUpperBound;
											lcp.DepartUpperBound = departUpperBound;
											lcp.MinimumReturnComplete = minimumReturnComplete;
											return lcp;
										}
									}
								}
							}
						}
						
						// otherwise infeasible						
						return null;
					}

					#endregion
				}

				#endregion

				#region Navigation Lane Change

				else if (information.Reason == LaneChangeReason.Navigation)
				{
					// parameters for traveling in current lane to hit other
					List<TravelingParameters> tps = new List<TravelingParameters>();			

					// get navigation parameterization
					TravelingParameters lateralParams = this.ForwardMonitor.ParameterizationHelper(initial, lateralReasoning.LateralLane,
						goal, state.Front, CoreCommon.CorePlanningState, state, lateralReasoning.ForwardVehicle(state));
					tps.Add(lateralParams);

					// get and add the nav parameterization relative to our lane
					tps.Add(this.ForwardMonitor.NavigationParameters);

					// check avail
					bool adjRearAvailable = lateralReasoning.AdjacentAndRearClear(state);

					// if they are available we are in good shape, need to slow for nav, forward vehicles
					if (adjRearAvailable)
					{
						// notify
						ArbiterOutput.Output("Lane Change Params: Navigation: Adjacent and Rear Clear");

						#region Check Forward and Lateral Vehicles

						if (this.ForwardMonitor.CurrentParameters.Type == TravellingType.Vehicle && lateralParams.Type == TravellingType.Vehicle)
						{
							// check enough room to change lanes
							ArbiterWaypoint nextTargetMajor = target.GetNext(state.Front, types, ignorable);
							double xTargetLaneMajor = initial.DistanceBetween(state.Front, nextTargetMajor.Position);

							// distnace to goal
							double goalDist = initial.DistanceBetween(state.Front, goal);

							// check dist needed to complete
							double neededDistance = (1.5 * TahoeParams.VL * Math.Abs(initial.LaneId.Number - target.LaneId.Number)) +
							(-Math.Pow(CoreCommon.Communications.GetVehicleSpeed().Value, 2) / (4 * CoreCommon.MaximumNegativeAcceleration));

							// check for proper distances
							if (xTargetLaneMajor >= neededDistance && goalDist >= neededDistance && this.ForwardMonitor.NavigationParameters.DistanceToGo >= neededDistance)
							{
								// check distance to return (weeds out safety zone crap
								Coordinates lateralVehicle = ((LateralReasoning)lateralReasoning).ForwardMonitor.ForwardVehicle.CurrentVehicle.ClosestPosition;
								double distToReturn = initial.DistanceBetween(state.Front, initial.LanePath().AdvancePoint(initial.LanePath().GetClosestPoint(lateralVehicle), 30.0).Location);

								// check passing params
								LaneChangeInformation lci;
								bool shouldPass = ((LateralReasoning)lateralReasoning).ForwardMonitor.ForwardVehicle.ShouldPass(out lci);

								// check passing params
								LaneChangeInformation lciInit;
								bool shouldPassInit = this.ForwardMonitor.ForwardVehicle.ShouldPass(out lciInit);

								// check forward lateral stopped and enough distance to go around and not vehicles between it and goal close enough to stop
								if(shouldPass && lci.Reason == LaneChangeReason.FailedForwardVehicle && goalDist > distToReturn && 
									(!shouldPassInit || lciInit.Reason != LaneChangeReason.FailedForwardVehicle || this.ForwardMonitor.CurrentParameters.DistanceToGo > lateralParams.DistanceToGo + TahoeParams.VL * 5))
								{
									// return that we should pass it as normal in the initial lane
									return null;
								}

								// check get distance to upper
								double xUpper = this.ForwardMonitor.ForwardVehicle.CurrentVehicle.IsStopped ? Math.Min(goalDist, neededDistance) : this.ForwardMonitor.ForwardVehicle.ForwardControl.xSeparation - 2;
								Coordinates upper = initial.LanePath().AdvancePoint(initial.LanePath().GetClosestPoint(state.Front), xUpper).Location;

								// add current params if not stopped
								if (!this.ForwardMonitor.ForwardVehicle.CurrentVehicle.IsStopped)
									tps.Add(this.ForwardMonitor.CurrentParameters);

								// get final
								tps.Sort();
								TravelingParameters final = tps[0];

								// parameterize
								LaneChangeParameters lcp = new LaneChangeParameters(true, true, initial, false, target, false, toLeft,
									null, final.DistanceToGo - 3.0, null, toLeft ? TurnDecorators.LeftTurnDecorator : TurnDecorators.RightTurnDecorator, 
									final, upper, new Coordinates(), new Coordinates(), new Coordinates(), LaneChangeReason.Navigation);

								return lcp;
							}
						}

						#endregion

						#region Check Forward Vehicle

						else if (this.ForwardMonitor.CurrentParameters.Type == TravellingType.Vehicle)
						{
							// check enough room to change lanes
							ArbiterWaypoint nextTargetMajor = target.GetNext(state.Front, types, ignorable);
							double xTargetLaneMajor = initial.DistanceBetween(state.Front, nextTargetMajor.Position);

							// distnace to goal
							double goalDist = initial.DistanceBetween(state.Front, goal);

							// check dist needed to complete
							double neededDistance = (1.5 * TahoeParams.VL * Math.Abs(initial.LaneId.Number - target.LaneId.Number)) +
							(-Math.Pow(CoreCommon.Communications.GetVehicleSpeed().Value, 2) / (4 * CoreCommon.MaximumNegativeAcceleration));

							// check for proper distances
							if (xTargetLaneMajor >= neededDistance && goalDist >= neededDistance && this.ForwardMonitor.NavigationParameters.DistanceToGo >= neededDistance)
							{
								// add current params if not stopped
								if(!this.ForwardMonitor.ForwardVehicle.CurrentVehicle.IsStopped)
									tps.Add(this.ForwardMonitor.CurrentParameters);

								// get final
								tps.Sort();
								TravelingParameters final = tps[0];

								// check get distance to upper
								double xUpper = this.ForwardMonitor.ForwardVehicle.CurrentVehicle.IsStopped ? neededDistance : this.ForwardMonitor.ForwardVehicle.ForwardControl.xSeparation - 2;
								Coordinates upper = initial.LanePath().AdvancePoint(initial.LanePath().GetClosestPoint(state.Front), xUpper).Location;

								// parameterize
								LaneChangeParameters lcp = new LaneChangeParameters(true, true, initial, false, target, false, toLeft,
									null, final.DistanceToGo - 3.0, null, toLeft ? TurnDecorators.LeftTurnDecorator : TurnDecorators.RightTurnDecorator, 
									final, upper, new Coordinates(), new Coordinates(), new Coordinates(), LaneChangeReason.Navigation);

								return lcp;
							}
						}

						#endregion

						#region Lateral Vehicle

						// check to see if should use the forward tracker, i.e. forward vehicle exists
						else if (lateralParams.Type == TravellingType.Vehicle)
						{
							// add current params
							tps.Add(this.ForwardMonitor.CurrentParameters);

							// check enough room to change lanes
							ArbiterWaypoint nextTargetMajor = target.GetNext(state.Front, types, ignorable);
							double xTargetLaneMajor = initial.DistanceBetween(state.Front, nextTargetMajor.Position);

							// distnace to goal
							double goalDist = initial.DistanceBetween(state.Front, goal);

							// check dist needed to complete
							double neededDistance = (1.5 * TahoeParams.VL * Math.Abs(initial.LaneId.Number - target.LaneId.Number)) +
							(-Math.Pow(CoreCommon.Communications.GetVehicleSpeed().Value, 2) / (4 * CoreCommon.MaximumNegativeAcceleration));

							// check for proper distances
							if (xTargetLaneMajor >= neededDistance && goalDist >= neededDistance && this.ForwardMonitor.NavigationParameters.DistanceToGo >= neededDistance)
							{
								// check distance to return (weeds out safety zone crap
								Coordinates lateralVehicle = ((LateralReasoning)lateralReasoning).ForwardMonitor.ForwardVehicle.CurrentVehicle.ClosestPosition;
								double distToReturn = initial.DistanceBetween(state.Front, initial.LanePath().AdvancePoint(initial.LanePath().GetClosestPoint(lateralVehicle), 30.0).Location);

								// check passing params
								LaneChangeInformation lci;
								bool shouldPass = ((LateralReasoning)lateralReasoning).ForwardMonitor.ForwardVehicle.ShouldPass(out lci);

								// check forward lateral stopped and enough distance to go around and not vehicles between it and goal close enough to stop
								if (shouldPass && lci.Reason == LaneChangeReason.FailedForwardVehicle && goalDist > distToReturn)
								{
									// return that we should pass it as normal in the initial lane
									return null;
								}

								// check if we are already slowed for this vehicle and are at a good distance from it
								if (speed < lateralParams.RecommendedSpeed + 1.0)
								{
									// get final
									tps.Sort();
									TravelingParameters final = tps[0];

									// upper bound is nav bound
									Coordinates upper = initial.LanePath().AdvancePoint(initial.LanePath().GetClosestPoint(state.Front), Math.Min(neededDistance, final.DistanceToGo)).Location;

									// parameterize
									LaneChangeParameters lcp = new LaneChangeParameters(true, true, initial, false, target, false, toLeft,
										null, final.DistanceToGo - 3.0, null, TurnDecorators.LeftTurnDecorator, final, upper, new Coordinates(),
										new Coordinates(), new Coordinates(), LaneChangeReason.Navigation);

									return lcp;
								}
								// otherwise need to slow for it or other
								else
								{
									// get final
									tps.Sort();
									TravelingParameters final = tps[0];

									// upper bound is nav bound
									Coordinates upper = initial.LanePath().AdvancePoint(initial.LanePath().GetClosestPoint(state.Front), final.DistanceToGo).Location;

									// parameterize
									LaneChangeParameters lcp = new LaneChangeParameters(false, true, initial, false, target, false, toLeft,
										null, final.DistanceToGo - 3.0, null, TurnDecorators.LeftTurnDecorator, final, new Coordinates(), new Coordinates(),
										new Coordinates(), new Coordinates(), LaneChangeReason.Navigation);

									return lcp;
								}
							}
						}

						#endregion

						#region No forward or lateral

						// otherwise just go!
						else
						{
							// add current params
							tps.Add(this.ForwardMonitor.CurrentParameters);

							// check enough room to change lanes
							ArbiterWaypoint nextTargetMajor = target.GetNext(state.Front, types, ignorable);
							double xTargetLaneMajor = initial.DistanceBetween(state.Front, nextTargetMajor.Position);

							// distnace to goal
							double goalDist = initial.DistanceBetween(state.Front, goal);

							// check dist needed to complete
							double neededDistance = (1.5 * TahoeParams.VL * Math.Abs(initial.LaneId.Number - target.LaneId.Number)) +
							(-Math.Pow(CoreCommon.Communications.GetVehicleSpeed().Value, 2) / (4 * CoreCommon.MaximumNegativeAcceleration));

							// check for proper distances
							if (xTargetLaneMajor >= neededDistance && goalDist >= neededDistance && this.ForwardMonitor.NavigationParameters.DistanceToGo >= neededDistance)
							{
								// get final
								tps.Sort();
								TravelingParameters final = tps[0];

								// upper bound is nav bound
								Coordinates upper = initial.LanePath().AdvancePoint(initial.LanePath().GetClosestPoint(state.Front), Math.Min(neededDistance, final.DistanceToGo)).Location;

								// parameterize
								LaneChangeParameters lcp = new LaneChangeParameters(true, true, initial, false, target, false, toLeft,
									null, final.DistanceToGo, null, TurnDecorators.LeftTurnDecorator, final, upper, new Coordinates(),
									new Coordinates(), new Coordinates(), LaneChangeReason.Navigation);

								// return the parameterization
								return lcp;
							}
						}

						#endregion
					}
					// otherwise we need to determine how to make this available
					else
					{
						// notify
						ArbiterOutput.Output("Lane Change Params: Navigation Adjacent and Rear NOT Clear");

						// gets the adjacent vehicle
						VehicleAgent adjacent = lateralReasoning.AdjacentVehicle;

						// add current params
						tps.Add(this.ForwardMonitor.CurrentParameters);	

						#region Pass Adjacent

						// checks if it is failed for us to pass it
						if (adjacent != null && (adjacent.IsStopped || adjacent.Speed < 1.5))
						{
							// get final
							List<TravelingParameters> adjacentTravelingParams = new List<TravelingParameters>();
							adjacentTravelingParams.Add(this.ForwardMonitor.CurrentParameters);
							adjacentTravelingParams.Add(this.ForwardMonitor.ParameterizationHelper(initial, lateralReasoning.LateralLane, goal, state.Front, CoreCommon.CorePlanningState, state, null));

							adjacentTravelingParams.Sort();
							//tps.Sort();
							TravelingParameters final = adjacentTravelingParams[0];// tps[0];

							// parameterize
							LaneChangeParameters lcp = new LaneChangeParameters();
							lcp.Available = false;
							lcp.Feasible = true;
							lcp.Parameters = final;
							return lcp;
						}

						#endregion 

						#region Follow Adjacent

						// otherwise we need to follow it, as it is lateral, this means 0.0 speed
						else if (adjacent != null)
						{
							// get and add the vehicle parameterization relative to our lane
							TravelingParameters tmp = new TravelingParameters();
							tmp.Behavior = new StayInLaneBehavior(initial.LaneId, new ScalarSpeedCommand(0.0), this.ForwardMonitor.CurrentParameters.VehiclesToIgnore,
								initial.LanePath(), initial.Width, initial.NumberOfLanesLeft(state.Front, true), initial.NumberOfLanesRight(state.Front, true));
							tmp.NextState = CoreCommon.CorePlanningState;

							// parameterize
							LaneChangeParameters lcp = new LaneChangeParameters();
							lcp.Available = false;
							lcp.Feasible = true;
							lcp.Parameters = tmp;
							return lcp;
						}

						#endregion

						#region Wait for the rear vehicle

						else
						{
							TravelingParameters tp = new TravelingParameters();
							tp.SpeedCommand = new ScalarSpeedCommand(0.0);
							tp.UsingSpeed = true;
							tp.DistanceToGo = 0.0;
							tp.VehiclesToIgnore = new List<int>();
							tp.RecommendedSpeed = 0.0;
							tp.NextState = CoreCommon.CorePlanningState;
							tp.Behavior = new StayInLaneBehavior(initial.LaneId, tp.SpeedCommand, new List<int>(), initial.LanePath(), initial.Width, initial.NumberOfLanesLeft(state.Front, true), initial.NumberOfLanesRight(state.Front, true));

							// parameterize
							LaneChangeParameters lcp = new LaneChangeParameters();
							lcp.Available = false;
							lcp.Feasible = true;
							lcp.Parameters = tp;
							return lcp;
						}

						#endregion
					}
				}

				#endregion

				#region Passing Lane Change

				else if (information.Reason == LaneChangeReason.SlowForwardVehicle)
				{
					throw new Exception("passing slow vehicles not yet supported");
				}

				#endregion

				// fallout returns null
				return null;
			}

			#endregion

			#region Target Lane Not Valid, Plan Navigation

			// otherwise plan for when we approach target if this is a navigational change
			else if(information.Reason == LaneChangeReason.Navigation)
			{
				// parameters for traveling in current lane to hit other
				List<TravelingParameters> tps = new List<TravelingParameters>();
				
				// add current params
				tps.Add(this.ForwardMonitor.CurrentParameters);

				// distance between front of car and start of lane
				if (target.RelativelyInside(state.Front) ||
					initial.DistanceBetween(state.Front, target.LanePath().StartPoint.Location) > 0)
				{
					#region Vehicle	and Navigation

					// check to see if we're not looped around wierd
					if (lateralReasoning.LateralLane.LanePath().GetClosestPoint(state.Front).Equals(lateralReasoning.LateralLane.LanePath().StartPoint))
					{
						// initialize the forward tracker in the other lane
						ForwardVehicleTracker fvt = new ForwardVehicleTracker();
						fvt.Update(lateralReasoning.LateralLane, state);

						// check to see if should use the forward tracker
						if (fvt.ShouldUseForwardTracker)
						{
							// get navigation parameterization
							TravelingParameters navParams = this.ForwardMonitor.ParameterizationHelper(initial, lateralReasoning.LateralLane,
								goal, state.Front, CoreCommon.CorePlanningState, state, lateralReasoning.ForwardVehicle(state));
							tps.Add(navParams);
						}
						else
						{
							// get navigation parameterization
							TravelingParameters navParams = this.ForwardMonitor.ParameterizationHelper(initial, lateralReasoning.LateralLane,
								goal, state.Front, CoreCommon.CorePlanningState, state, null);
							tps.Add(navParams);
						}
					}

					#endregion

					#region Navigation

					// check to see that nav point is downstream of us
					else if (initial.DistanceBetween(state.Front, goal) > 0.0)
					{
						// get navigation parameterization
						TravelingParameters navParams = this.ForwardMonitor.ParameterizationHelper(initial, lateralReasoning.LateralLane,
							goal, state.Front, CoreCommon.CorePlanningState, state, null);
						tps.Add(navParams);
					}

					#endregion

					else
					{
						return null;
					}
				}
				else
					return null;

				// get final
				tps.Sort();
				TravelingParameters final = tps[0];

				// parameterize
				LaneChangeParameters lcp = new LaneChangeParameters();
				lcp.Available = false;
				lcp.Feasible = true;
				lcp.Parameters = final;
				return lcp;
			}

			#endregion

			// fallout return null
			return null;
		}		

		#endregion

		/// <summary>
		/// Gets the closest existing opposing lane to hte input lane
		/// </summary>
		/// <param name="al"></param>
		/// <param name="vs"></param>
		/// <returns></returns>
		public ArbiterLane GetClosestOpposing(ArbiterLane al, VehicleState vs)
		{
			ArbiterLane current = al.LaneOnLeft;
			while (current != null)
			{
				if (!current.Way.WayId.Equals(al.Way) && current.RelativelyInside(vs.Front))
				{
					return current;
				}

				if (!current.Way.WayId.Equals(al.Way.WayId))
					current = current.LaneOnRight;
				else
					current = current.LaneOnLeft;
			}

			return null;
		}

		/// <summary>
		/// Secondary maneuver when current lane is the desired lane
		/// </summary>
		/// <param name="arbiterLane"></param>
		/// <param name="vehicleState"></param>
		/// <param name="roadPlan"></param>
		/// <param name="blockages"></param>
		/// <param name="ignorable"></param>
		/// <returns></returns>
		public Maneuver? AdvancedSecondary(IFQMPlanable arbiterLane, VehicleState vehicleState, RoadPlan roadPlan,
			List<ITacticalBlockage> blockages, List<ArbiterWaypoint> ignorable, TypeOfTasks bestTask)
		{
			// check if we might be able to pass here
			bool validArea = arbiterLane is ArbiterLane || (((SupraLane)arbiterLane).ClosestComponent(vehicleState.Front) == SLComponentType.Initial);
			
			// check normal valid area
			if (validArea)
			{
				// get lane we are in
				ArbiterLane ourForwardLane = arbiterLane is ArbiterLane ? (ArbiterLane)arbiterLane : ((SupraLane)arbiterLane).Initial;

				// check sl
				if (arbiterLane is SupraLane)
				{
					Dictionary<ArbiterLaneId, LanePlan> tmpPlans = new Dictionary<ArbiterLaneId,LanePlan>();
					tmpPlans.Add(ourForwardLane.LaneId, roadPlan.LanePlans[ourForwardLane.LaneId]);
					RoadPlan tmp = new RoadPlan(tmpPlans);
					roadPlan = tmp;
				}
								
				// return the advanced plan
				ArbiterWaypoint goalWp = arbiterLane is SupraLane ? (ArbiterWaypoint)((SupraLane)arbiterLane).Interconnect.InitialGeneric : roadPlan.BestPlan.laneWaypointOfInterest.PointOfInterest;
				return this.AdvancedSecondaryNextCheck(ourForwardLane, goalWp, vehicleState, roadPlan, blockages, ignorable, bestTask);				
			}

			// fall through
			return null;
		}

		/// <summary>
		/// Secondary maneuver when current lane is the desired lane
		/// </summary>
		/// <param name="arbiterLane"></param>
		/// <param name="vehicleState"></param>
		/// <param name="roadPlan"></param>
		/// <param name="blockages"></param>
		/// <param name="ignorable"></param>
		/// <returns></returns>
		public Maneuver? AdvancedSecondaryNextCheck(ArbiterLane lane, ArbiterWaypoint laneGoal, VehicleState vehicleState, RoadPlan roadPlan,
			List<ITacticalBlockage> blockages, List<ArbiterWaypoint> ignorable, TypeOfTasks bestTask)
		{
			// check if the forward vehicle exists
			if (this.ForwardMonitor.ForwardVehicle.ShouldUseForwardTracker && this.ForwardMonitor.ForwardVehicle.CurrentVehicle.PassedLongDelayedBirth)
			{
				// distance to forward vehicle
				double distanceToForwardVehicle = lane.DistanceBetween(vehicleState.Front, this.ForwardMonitor.ForwardVehicle.CurrentVehicle.ClosestPosition);

				#region Distance From Forward Not Enough

				// check distance to forward vehicle
				if (this.ForwardMonitor.ForwardVehicle.CurrentVehicle.QueuingState.Queuing == QueuingState.Failed &&
					distanceToForwardVehicle < 8.0 && this.ForwardMonitor.ForwardVehicle.StoppedBehindForwardVehicle)
				{
					// set name
					ArbiterLane al = lane;

					// distance to forward vehicle too small
					double distToForwards = al.DistanceBetween(vehicleState.Front, this.ForwardMonitor.ForwardVehicle.CurrentVehicle.ClosestPosition);
					double distToReverse = Math.Max(1.0, 8.0 - distToForwards);
					if (distToForwards < 8.0)
					{
						// notify
						ArbiterOutput.Output("Secondary: NOT Properly Stopped Behind Forward Vehicle: " + this.ForwardMonitor.ForwardVehicle.CurrentVehicle.ToString() + " distance: " + distToForwards.ToString("f2"));

						this.RearMonitor = new RearQuadrantMonitor(al, SideObstacleSide.Driver);
						this.RearMonitor.Update(vehicleState);
						if (this.RearMonitor.CurrentVehicle != null)
						{
							double distToRearVehicle = al.DistanceBetween(this.RearMonitor.CurrentVehicle.ClosestPosition, vehicleState.Position) - TahoeParams.RL;
							double distNeedClear = distToReverse + 2.0;
							if (distToRearVehicle < distNeedClear)
							{
								// notify
								ArbiterOutput.Output("Secondary: Rear: Not enough room to clear in rear: " + distToRearVehicle.ToString("f2") + " < " + distNeedClear.ToString("f2"));
								return null;
							}
						}

						double distToLaneStart = al.DistanceBetween(al.LanePath().StartPoint.Location, vehicleState.Position) - TahoeParams.RL;
						if (distToReverse > distToLaneStart)
						{
							// notify
							ArbiterOutput.Output("Secondary: Rear: Not enough room in lane to reverse in rear: " + distToLaneStart.ToString("f2") + " < " + distToReverse.ToString("f2"));
							return null;
						}
						else
						{
							// notify
							ArbiterOutput.Output("Secondary: Reversing to pass Forward Vehicle: " + this.ForwardMonitor.ForwardVehicle.CurrentVehicle.ToString() + " reversing distance: " + distToReverse.ToString("f2"));
							StopAtDistSpeedCommand sadsc = new StopAtDistSpeedCommand(distToReverse, true);
							StayInLaneBehavior silb = new StayInLaneBehavior(al.LaneId, sadsc, new List<int>(), al.LanePath(), al.Width, al.NumberOfLanesLeft(vehicleState.Front, true), al.NumberOfLanesRight(vehicleState.Front, true));
							return new Maneuver(silb, CoreCommon.CorePlanningState, TurnDecorators.HazardDecorator, vehicleState.Timestamp);
						}
					}
				}

				#endregion

				// get distance to next lane major
				List<WaypointType> wts = new List<WaypointType>(new WaypointType[] { WaypointType.Stop, WaypointType.End });
				ArbiterWaypoint nextWaypoint = lane.GetNext(lane.GetClosestPartition(vehicleState.Position).Final, wts, new List<ArbiterWaypoint>());

				// check if the vehicle occurs before the next major thing
				double distanceToNextMajor = lane.DistanceBetween(vehicleState.Front, nextWaypoint.Position);

				// check if vehicle occurs before the next lane major
				if (distanceToForwardVehicle < distanceToNextMajor)
				{
					// check if forward vehicle exists in this lane and is closer then the lane goal 
					if (TacticalDirector.VehicleAreas.ContainsKey(lane) &&
						TacticalDirector.VehicleAreas[lane].Contains(this.ForwardMonitor.ForwardVehicle.CurrentVehicle) &&
						lane.DistanceBetween(vehicleState.Front, this.ForwardMonitor.ForwardVehicle.CurrentVehicle.ClosestPosition) < lane.DistanceBetween(vehicleState.Front, laneGoal.Position))
					{
						// some constants
						double desiredDistance = 50.0;

						// get the lane goal distance
						double distanceToLaneGoal = lane.DistanceBetween(vehicleState.Front, laneGoal.Position);

						// check if necessary to be in current lane
						if (distanceToLaneGoal <= desiredDistance)
						{
							// default secondary maneuver
							ArbiterOutput.WriteToLog("AdvancedSecondaryNextCheck: DistToLaneGoal < 50: " + desiredDistance.ToString("f2"));
							return this.SecondaryManeuver(lane, vehicleState, roadPlan, new List<ITacticalBlockage>(), new List<ArbiterWaypoint>(), bestTask);
						}
						// otherwise would be good but not necessary
						else
						{
							// return the maneuver from the vehicle being outside the min cap
							ArbiterOutput.WriteToLog("AdvancedSecondaryNextCheck: DistToLaneGoal > 50: " + desiredDistance.ToString("f2"));
							return this.AdvancedSecondaryOutsideMinCap(lane, laneGoal, vehicleState, roadPlan, blockages, ignorable, bestTask);
						}
					}
				}
			}

			// no secondary
			return null;
		}

		/// <summary>
		/// Secondary maneuver outside minimum cap
		/// </summary>
		/// <param name="arbiterLane"></param>
		/// <param name="vehicleState"></param>
		/// <param name="roadPlan"></param>
		/// <param name="blockages"></param>
		/// <param name="ignorable"></param>
		/// <returns></returns>
		public Maneuver? AdvancedSecondaryOutsideMinCap(ArbiterLane lane, ArbiterWaypoint laneGoal, VehicleState vehicleState, RoadPlan roadPlan,
			List<ITacticalBlockage> blockages, List<ArbiterWaypoint> ignorable, TypeOfTasks bestTask)
		{			
			// get proper reasoning component
			List<LateralReasoning> adjacentReasonings = new List<LateralReasoning>();
			if (bestTask != TypeOfTasks.Left)
			{
				if (this.rightLateralReasoning is LateralReasoning && this.rightLateralReasoning.Exists && this.rightLateralReasoning.ExistsExactlyHere(vehicleState))
					adjacentReasonings.Add((LateralReasoning)this.rightLateralReasoning);
				else if (this.leftLateralReasoning is LateralReasoning && this.leftLateralReasoning.Exists && this.leftLateralReasoning.ExistsExactlyHere(vehicleState))
					adjacentReasonings.Add((LateralReasoning)this.leftLateralReasoning);
			}
			else
			{
				if (this.leftLateralReasoning is LateralReasoning && this.leftLateralReasoning.Exists && this.leftLateralReasoning.ExistsExactlyHere(vehicleState))
					adjacentReasonings.Add((LateralReasoning)this.leftLateralReasoning);
				else if (this.rightLateralReasoning is LateralReasoning && this.rightLateralReasoning.Exists && this.rightLateralReasoning.ExistsExactlyHere(vehicleState))
					adjacentReasonings.Add((LateralReasoning)this.rightLateralReasoning);				
			}

			// loop through possible
			foreach(LateralReasoning adjacentReasoning in adjacentReasonings)
			{				
				// check if adjacent reasoning exists
				if (adjacentReasoning != null && adjacentReasoning.Exists)
				{
					// update adjacent reasoning					
					adjacentReasoning.ForwardMonitor.Primary(adjacentReasoning.LateralLane, vehicleState, roadPlan, blockages, ignorable, false);

					// otherwise check if forward vehicle slow
					LaneChangeInformation forwardVehicleSecondary;

					// check if should pass the forward vehicle
					if (this.ForwardMonitor.ForwardVehicle.ShouldPass(out forwardVehicleSecondary, lane))
					{
						// check if forward vehicle failed
						if (forwardVehicleSecondary.Reason == LaneChangeReason.FailedForwardVehicle)
						{
							ArbiterOutput.WriteToLog("AdvancedSecondaryOutsideMinCap: Vehicle: " + this.ForwardMonitor.ForwardVehicle.CurrentVehicle.ToString() + " failed");

							// make sure the failed vehicle is not within 50m of the goal
							double vehicleDistanceToGoal = lane.DistanceBetween(ForwardMonitor.ForwardVehicle.CurrentVehicle.ClosestPosition, laneGoal.Position);
							ArbiterOutput.Output("Failed FV: " + this.ForwardMonitor.ForwardVehicle.CurrentVehicle.ToString() + ", DistGoal: " + vehicleDistanceToGoal.ToString("f2") + ", speed: " + this.ForwardMonitor.ForwardVehicle.CurrentVehicle.Speed.ToString("f1"));
							if ((vehicleDistanceToGoal > 50))
							{
								// check if adjacent has no forward vehicle
								if (!adjacentReasoning.ForwardMonitor.ForwardVehicle.ShouldUseForwardTracker)
								{
									// plan the lane change
									return this.PlanLaneChange(lane, laneGoal, adjacentReasoning, vehicleState, this.ForwardMonitor.ForwardVehicle.CurrentVehicle, forwardVehicleSecondary);
								}
								// otherwise a forward vehicle exists
								else
								{
									// otherwise check if forward vehicle slow
									LaneChangeInformation lateralVehicleInformation;

									// check if lateral vehicle fine
									if (!adjacentReasoning.ForwardMonitor.ForwardVehicle.ShouldPass(out lateralVehicleInformation, adjacentReasoning.LateralLane))
									{
										// plan the lane change
										return this.PlanLaneChange(lane, laneGoal, adjacentReasoning, vehicleState, this.ForwardMonitor.ForwardVehicle.CurrentVehicle, new LaneChangeInformation(LaneChangeReason.Navigation, adjacentReasoning.LateralMonitor.CurrentVehicle));
									}
									// check if lateral vehicle failed or slow
									else
									{
										// check distance to lateral > distance to forward + 25
										double distToAdjacent = lane.DistanceBetween(vehicleState.Front, adjacentReasoning.ForwardMonitor.ForwardVehicle.CurrentVehicle.ClosestPosition);
										double distToForward = lane.DistanceBetween(vehicleState.Front, this.ForwardMonitor.ForwardVehicle.CurrentVehicle.ClosestPosition);
										if (distToAdjacent > distToForward + 25.0)
										{
											// plan the lane change
											return this.PlanLaneChange(lane, laneGoal, adjacentReasoning, vehicleState, this.ForwardMonitor.ForwardVehicle.CurrentVehicle, new LaneChangeInformation(LaneChangeReason.Navigation, adjacentReasoning.LateralMonitor.CurrentVehicle));
										}
									}
								}
							}
						}
						// otherwise check if they are slow
						else if (forwardVehicleSecondary.Reason == LaneChangeReason.SlowForwardVehicle)
						{
							ArbiterOutput.WriteToLog("AdvancedSecondaryOutsideMinCap: Vehicle: " + this.ForwardMonitor.ForwardVehicle.CurrentVehicle.ToString() + " slow");

							// make sure the slow vehicle is not within 50m of the goal if velocity is > 5mph
							double vehicleDistanceToGoal = lane.DistanceBetween(ForwardMonitor.ForwardVehicle.CurrentVehicle.ClosestPosition, laneGoal.Position);
							ArbiterOutput.Output("Slow FV: " + this.ForwardMonitor.ForwardVehicle.CurrentVehicle.ToString() + ", DistGoal: " + vehicleDistanceToGoal.ToString("f2") + ", speed: " + this.ForwardMonitor.ForwardVehicle.CurrentVehicle.Speed.ToString("f1"));
							if ((vehicleDistanceToGoal > 50 && this.ForwardMonitor.ForwardVehicle.CurrentVehicle.Speed < 2.24) || 
								(vehicleDistanceToGoal > 75 && this.ForwardMonitor.ForwardVehicle.CurrentVehicle.Speed < 4.48) || 
								(vehicleDistanceToGoal > 100 && this.ForwardMonitor.ForwardVehicle.CurrentVehicle.Speed < 6.72) || 
								(vehicleDistanceToGoal > 125 && this.ForwardMonitor.ForwardVehicle.CurrentVehicle.Speed < 8.96))
							{
								// check if adjacent has no forward vehicle
								if (!adjacentReasoning.ForwardMonitor.ForwardVehicle.ShouldUseForwardTracker)
								{
									// plan the lane change
									ArbiterOutput.WriteToLog("AdvancedSecondaryOutsideMinCap: No vehicle in adjacent");
									return this.PlanLaneChange(lane, laneGoal, adjacentReasoning, vehicleState, this.ForwardMonitor.ForwardVehicle.CurrentVehicle, new LaneChangeInformation(LaneChangeReason.Navigation, adjacentReasoning.LateralMonitor.CurrentVehicle));
								}
								// otherwise a forward vehicle exists
								else
								{
									// otherwise check if forward vehicle slow
									LaneChangeInformation lateralVehicleInformation;

									// check if lateral vehicle fine
									if (!adjacentReasoning.ForwardMonitor.ForwardVehicle.ShouldPass(out lateralVehicleInformation, adjacentReasoning.LateralLane))
									{
										// check distance to lateral > distance to forward + 25
										double distToAdjacent = lane.DistanceBetween(vehicleState.Front, adjacentReasoning.ForwardMonitor.ForwardVehicle.CurrentVehicle.ClosestPosition);
										double distToForward = lane.DistanceBetween(vehicleState.Front, this.ForwardMonitor.ForwardVehicle.CurrentVehicle.ClosestPosition);
										ArbiterOutput.WriteToLog("AdvancedSecondaryOutsideMinCap: Normal vehicle in adjacent: " + adjacentReasoning.ForwardMonitor.ForwardVehicle.CurrentVehicle.ToString() + "FV Dist: " + distToForward.ToString("f2") + ", Adj Dist: " + adjacentReasoning.ForwardMonitor.ForwardVehicle.CurrentVehicle.ToString());

										// check distance greater and adjacent speed greater than forward speed
										if (distToAdjacent > distToForward + 25.0 &&
											adjacentReasoning.ForwardMonitor.ForwardVehicle.CurrentVehicle.Speed > this.ForwardMonitor.ForwardVehicle.CurrentVehicle.Speed)
										{
											// plan the lane change
											return this.PlanLaneChange(lane, laneGoal, adjacentReasoning, vehicleState, this.ForwardMonitor.ForwardVehicle.CurrentVehicle, new LaneChangeInformation(LaneChangeReason.Navigation, adjacentReasoning.LateralMonitor.CurrentVehicle));
										}

										// plan the lane change
										//return this.PlanLaneChange(lane, laneGoal, adjacentReasoning, vehicleState, this.ForwardMonitor.ForwardVehicle.CurrentVehicle, new LaneChangeInformation(LaneChangeReason.Navigation, adjacentReasoning.LateralMonitor.CurrentVehicle));
									}
									else if (
										lane.DistanceBetween(this.ForwardMonitor.ForwardVehicle.CurrentVehicle.ClosestPosition, 
										adjacentReasoning.ForwardMonitor.ForwardVehicle.CurrentVehicle.ClosestPosition) > 65.0 &&
										(this.ForwardMonitor.ForwardVehicle.CurrentVehicle.IsStopped || 
										(this.ForwardMonitor.ForwardVehicle.CurrentVehicle.Speed + 4.48 < adjacentReasoning.ForwardMonitor.ForwardVehicle.CurrentVehicle.Speed)))
									{
										// plan the lane change
										return this.PlanLaneChange(lane, laneGoal, adjacentReasoning, vehicleState, this.ForwardMonitor.ForwardVehicle.CurrentVehicle, new LaneChangeInformation(LaneChangeReason.Navigation, adjacentReasoning.LateralMonitor.CurrentVehicle)); 
									}
								}
							}
						}
					}
				}				
			}
			
			// normal secondary parameterization as a fall through on the naviagation lane changes
			return this.SecondaryManeuver(lane, vehicleState, roadPlan, blockages, ignorable, bestTask);
		}

		/// <summary>
		/// Secondary maneuver inside our minimum cap
		/// </summary>
		/// <param name="arbiterLane"></param>
		/// <param name="vehicleState"></param>
		/// <param name="roadPlan"></param>
		/// <param name="blockages"></param>
		/// <param name="ignorable"></param>
		/// <returns></returns>
		public Maneuver? AdvancedSecondaryInsideMinCap(ArbiterLane lane, ArbiterWaypoint laneGoal, VehicleState vehicleState, RoadPlan roadPlan,
			List<ITacticalBlockage> blockages, List<ArbiterWaypoint> ignorable, TypeOfTasks bestTask)
		{
			return null;
		}

		/// <summary>
		/// Plans a lane change from the initial lane to the adjacent lane
		/// </summary>
		/// <param name="initial"></param>
		/// <param name="adjacent"></param>
		/// <param name="vehicleState"></param>
		/// <param name="forwardVehicle"></param>
		/// <returns></returns>
		public Maneuver? PlanLaneChange(ArbiterLane initial, ArbiterWaypoint goal, LateralReasoning adjacent, VehicleState vehicleState, VehicleAgent forwardVehicle, LaneChangeInformation lci)
		{
			bool toLeft = initial.LaneOnLeft != null && adjacent.LateralLane.Equals(initial.LaneOnLeft);
			LaneChangeParameters updatedLCP;
			return this.LaneChangeManeuver(initial, toLeft, goal, vehicleState, new List<ITacticalBlockage>(), new List<ArbiterWaypoint>(), lci, null, out updatedLCP); 
		}

		/// <summary>
		/// Distinctly want to make lane change, parameters for doing so
		/// </summary>
		/// <param name="arbiterLane"></param>
		/// <param name="left"></param>
		/// <param name="vehicleState"></param>
		/// <param name="roadPlan"></param>
		/// <param name="blockages"></param>
		/// <param name="ignorable"></param>
		/// <returns></returns>
		public Maneuver? AdvancedDesiredLaneChangeManeuver(ArbiterLane lane, bool left, ArbiterWaypoint goal, RoadPlan rp, VehicleState vehicleState,
			List<ITacticalBlockage> blockages, List<ArbiterWaypoint> ignorable, LaneChangeInformation laneChangeInformation, Maneuver? secondary,
			out LaneChangeParameters parameters)
		{
			// set aprams
			parameters = new LaneChangeParameters();

			// get final maneuver
			Maneuver? final = null;

			// check partition is not a startup chute
			if (lane.GetClosestPartition(vehicleState.Front).Type != PartitionType.Startup)
			{
				// get the lane goal distance
				double distanceToLaneGoal = lane.DistanceBetween(vehicleState.Front, goal.Position);

				// check if our distance is less than 50m to the goal
				if (distanceToLaneGoal < 50.0)
				{
					// use old
					final = this.LaneChangeManeuver(lane, left, goal, vehicleState, blockages, ignorable, laneChangeInformation, secondary, out parameters);

					try
					{
						// check final null
						if (final == null)
						{
							// check for checkpoint within 4VL of front of failed vehicle
							ArbiterCheckpoint acCurrecnt = CoreCommon.Mission.MissionCheckpoints.Peek();
							if (acCurrecnt.WaypointId is ArbiterWaypointId)
							{
								// get waypoint
								ArbiterWaypoint awCheckpoint = (ArbiterWaypoint)CoreCommon.RoadNetwork.ArbiterWaypoints[acCurrecnt.WaypointId];

								// check way
								if (awCheckpoint.Lane.Way.Equals(lane.Way))
								{
									// distance to wp
									double distToWp = lane.DistanceBetween(vehicleState.Front, awCheckpoint.Position);

									// check close to waypoint and stopped
									if (CoreCommon.Communications.GetVehicleSpeed().Value < 0.1 && distToWp < TahoeParams.VL * 1.0)
									{
										ArbiterOutput.Output("Removing checkpoint: " + acCurrecnt.WaypointId.ToString() + " Stopped next to it");
										CoreCommon.Mission.MissionCheckpoints.Dequeue();
										return new Maneuver(new NullBehavior(), CoreCommon.CorePlanningState, TurnDecorators.NoDecorators, vehicleState.Timestamp);
									}
								}
							}
						}
					}
					catch (Exception) { }
				}
				// no forward vehicle
				else if (this.ForwardMonitor.ForwardVehicle.CurrentVehicle == null)
				{
					// adjacent monitor
					LateralReasoning adjacent = null;
					if (left && this.leftLateralReasoning is LateralReasoning && this.leftLateralReasoning.Exists)
					{
						// update
						adjacent = (LateralReasoning)this.leftLateralReasoning;
					}
					else if (!left && this.rightLateralReasoning is LateralReasoning && this.rightLateralReasoning.Exists)
					{
						// update
						adjacent = (LateralReasoning)this.rightLateralReasoning;
					}

					// check adj
					if (adjacent != null)
					{
						// update
						adjacent.ForwardMonitor.Primary(adjacent.LateralLane, vehicleState, rp, new List<ITacticalBlockage>(), new List<ArbiterWaypoint>(), false);

						if (adjacent.ForwardMonitor.ForwardVehicle.CurrentVehicle == null && adjacent.AdjacentAndRearClear(vehicleState))
						{
							// use old
							final = this.LaneChangeManeuver(lane, left, goal, vehicleState, blockages, ignorable, laneChangeInformation, secondary, out parameters);
						}
					}
				}
			}

			if (!final.HasValue)
			{
				if (!secondary.HasValue)
				{
					List<TravelingParameters> falloutParams = new List<TravelingParameters>();
					TravelingParameters t1 = this.ForwardMonitor.ParameterizationHelper(lane, lane, goal.Position, vehicleState.Front, CoreCommon.CorePlanningState, vehicleState, null);
					falloutParams.Add(t1);
					falloutParams.Add(this.ForwardMonitor.LaneParameters);
					if (this.ForwardMonitor.FollowingParameters.HasValue)
						falloutParams.Add(this.ForwardMonitor.FollowingParameters.Value);
					falloutParams.Sort();
					TravelingParameters tpCatch = falloutParams[0];	
				
					return new Maneuver(tpCatch.Behavior, tpCatch.NextState, TurnDecorators.NoDecorators, vehicleState.Timestamp);
				}
				else
				{
					return secondary;
				}
			}
			else
			{
				return final;
			}			
		}
	}
}
