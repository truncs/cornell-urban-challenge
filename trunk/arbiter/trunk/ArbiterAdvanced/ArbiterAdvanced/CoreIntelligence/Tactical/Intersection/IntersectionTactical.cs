using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.Core.Common;
using UrbanChallenge.Arbiter.Core.Common.State;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Navigation;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common.Sensors.Vehicle;
using UrbanChallenge.Common.Sensors.Obstacle;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Arbiter.Core.Common.Reasoning;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Arbiter.Core.Common.Tools;
using UrbanChallenge.Arbiter.Core.Common.Arbiter;
using UrbanChallenge.Arbiter.Core.Communications;
using UrbanChallenge.Common.Sensors;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Intersection.Reasoning;
using UrbanChallenge.Behaviors.CompletionReport;
using UrbanChallenge.Common;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Intersection
{
	/// <summary>
	/// Intersection tactical director
	/// </summary>
	public class IntersectionTactical
	{
		/// <summary>
		/// Monitor of the intersection
		/// </summary>
		public static IntersectionMonitor IntersectionMonitor;

		/// <summary>
		/// Reasoning for a turn
		/// </summary>
		public TurnReasoning TurnReasoning;

		/// <summary>
		/// Plans what maneuer we should take next
		/// </summary>
		/// <param name="planningState"></param>
		/// <param name="navigationalPlan"></param>
		/// <param name="vehicleState"></param>
		/// <param name="vehicles"></param>
		/// <param name="obstacles"></param>
		/// <param name="blockage"></param>
		/// <returns></returns>
		public Maneuver Plan(IState planningState, INavigationalPlan navigationalPlan, VehicleState vehicleState,
			SceneEstimatorTrackedClusterCollection vehicles, SceneEstimatorUntrackedClusterCollection obstacles, List<ITacticalBlockage> blockages)
		{
			#region Waiting At Intersection Exit

			if (planningState is WaitingAtIntersectionExitState)
			{
				// state
				WaitingAtIntersectionExitState waies = (WaitingAtIntersectionExitState)planningState;
				
				// get intersection plan
				IntersectionPlan ip = (IntersectionPlan)navigationalPlan;				

				// nullify turn reasoning
				this.TurnReasoning = null;

				#region Intersection Monitor Updates

				// check correct intersection monitor
				if (CoreCommon.RoadNetwork.IntersectionLookup.ContainsKey(waies.exitWaypoint.AreaSubtypeWaypointId) &&
					(IntersectionTactical.IntersectionMonitor == null ||
					!IntersectionTactical.IntersectionMonitor.OurMonitor.Waypoint.Equals(waies.exitWaypoint)))
				{
					// create new intersection monitor
					IntersectionTactical.IntersectionMonitor = new IntersectionMonitor(
						waies.exitWaypoint, 
						CoreCommon.RoadNetwork.IntersectionLookup[waies.exitWaypoint.AreaSubtypeWaypointId], 
						vehicleState, 
						ip.BestOption);
				}

				// update if exists
				if (IntersectionTactical.IntersectionMonitor != null)
				{
					// update monitor
					IntersectionTactical.IntersectionMonitor.Update(vehicleState);

					// print current
					ArbiterOutput.Output(IntersectionTactical.IntersectionMonitor.IntersectionStateString());
				}

				#endregion

				#region Desired Behavior

				// get best option from previously saved				
				IConnectAreaWaypoints icaw = null;

				if (waies.desired != null)
				{
					ArbiterInterconnect tmpInterconnect = waies.desired;
					if (waies.desired.InitialGeneric is ArbiterWaypoint)
					{
						ArbiterWaypoint init = (ArbiterWaypoint)waies.desired.InitialGeneric;
						if (init.NextPartition != null && init.NextPartition.Final.Equals(tmpInterconnect.FinalGeneric))
							icaw = init.NextPartition;
						else
							icaw = waies.desired;
					}
					else
						icaw = waies.desired;
				}
				else
				{
					icaw = ip.BestOption;
					waies.desired = icaw.ToInterconnect;
				}

				#endregion

				#region Turn Feasibility Reasoning

				// check uturn
				if (waies.desired.TurnDirection == ArbiterTurnDirection.UTurn)
					waies.turnTestState = TurnTestState.Completed;

				// check already determined feasible
				if (waies.turnTestState == TurnTestState.Unknown ||
					waies.turnTestState == TurnTestState.Failed)
				{
					#region Determine Behavior to Accomplish Turn

					// get default turn behavior
					TurnBehavior testTurnBehavior = this.DefaultTurnBehavior(icaw);

					// set saudi decorator
					if (waies.saudi != SAUDILevel.None)
						testTurnBehavior.Decorators.Add(new ShutUpAndDoItDecorator(waies.saudi));

					// set to ignore all vehicles
					testTurnBehavior.VehiclesToIgnore = new List<int>(new int[]{-1});

					#endregion

					#region Check Turn Feasible

					// check if we have completed
					CompletionReport turnCompletionReport;
					bool completedTest = CoreCommon.Communications.TestExecute(testTurnBehavior, out turnCompletionReport);//CoreCommon.Communications.AsynchronousTestHasCompleted(testTurnBehavior, out turnCompletionReport, true);
					
					// if we have completed the test
					if(completedTest || ((TrajectoryBlockedReport)turnCompletionReport).BlockageType != BlockageType.Dynamic)
					{
						#region Can Complete

						// check success
						if (turnCompletionReport.Result == CompletionResult.Success)
						{
							// set completion state of the turn
							waies.turnTestState = TurnTestState.Completed;
						}

						#endregion

						#region No Saudi Level, Found Initial Blockage

						// otherwise we cannot do the turn, check if saudi is still none
						else if(waies.saudi == SAUDILevel.None)
						{
							// notify
							ArbiterOutput.Output("Increased Saudi Level of Turn to L1");

							// up the saudi level, set as turn failed and no other option
							waies.saudi = SAUDILevel.L1;
							waies.turnTestState = TurnTestState.Failed;
						}

						#endregion

						#region Already at L1 Saudi

						else if(waies.saudi == SAUDILevel.L1)
						{
							// notify
							ArbiterOutput.Output("Turn with Saudi L1 Level failed");

							// get an intersection plan without this interconnect
							IntersectionPlan testPlan = CoreCommon.Navigation.PlanIntersectionWithoutInterconnect(
								waies.exitWaypoint,
								CoreCommon.RoadNetwork.ArbiterWaypoints[CoreCommon.Mission.MissionCheckpoints.Peek().WaypointId], 
								waies.desired);

							// check that the plan exists
							if (!testPlan.BestOption.ToInterconnect.Equals(waies.desired) && 
								testPlan.BestRouteTime < double.MaxValue - 1.0)
							{	
								// get the desired interconnect
								ArbiterInterconnect reset = testPlan.BestOption.ToInterconnect;

								#region Check that the reset interconnect is feasible

								// test the reset interconnect
								TurnBehavior testResetTurnBehavior = this.DefaultTurnBehavior(reset);

								// set to ignore all vehicles
								testResetTurnBehavior.VehiclesToIgnore = new List<int>(new int[] { -1 });

								// check if we have completed
								CompletionReport turnResetCompletionReport;
								bool completedResetTest = CoreCommon.Communications.TestExecute(testResetTurnBehavior, out turnResetCompletionReport);

								// check to see if this is feasible
								if (completedResetTest && turnResetCompletionReport is SuccessCompletionReport && reset.Blockage.ProbabilityExists < 0.95)
								{
									// notify
									ArbiterOutput.Output("Found clear interconnect: " + reset.ToString() + " adding blockage to current interconnect: " + waies.desired.ToString());

									// set the interconnect as being blocked
									CoreCommon.Navigation.AddInterconnectBlockage(waies.desired);

									// reset all
									waies.desired = reset;
									waies.turnTestState = TurnTestState.Completed;
									waies.saudi = SAUDILevel.None;
									waies.useTurnBounds = true;
									IntersectionMonitor.ResetDesired(reset);
								}

								#endregion

								#region No Lane Bounds

								// otherwise try without lane bounds
								else
								{
									// notify
									ArbiterOutput.Output("Had to fallout to using no turn bounds");

									// up the saudi level, set as turn failed and no other option
									waies.saudi = SAUDILevel.L1;
									waies.turnTestState = TurnTestState.Completed;
									waies.useTurnBounds = false;
								}

								#endregion
							}

							#region No Lane Bounds

							// otherwise try without lane bounds
							else
							{
								// up the saudi level, set as turn failed and no other option
								waies.saudi = SAUDILevel.L1;
								waies.turnTestState = TurnTestState.Unknown;
								waies.useTurnBounds = false;
							}

							#endregion
						}

						#endregion

						// want to reset ourselves
						return new Maneuver(new HoldBrakeBehavior(), CoreCommon.CorePlanningState, TurnDecorators.NoDecorators, vehicleState.Timestamp);
					}

					#endregion
				}

				#endregion

				#region Entry Monitor Blocked

				// checks the entry monitor vehicle for failure
				if (IntersectionMonitor != null && IntersectionMonitor.EntryAreaMonitor != null &&
					IntersectionMonitor.EntryAreaMonitor.Vehicle != null && IntersectionMonitor.EntryAreaMonitor.Failed)
				{
					ArbiterOutput.Output("Entry area blocked");

					// get an intersection plan without this interconnect
					IntersectionPlan testPlan = CoreCommon.Navigation.PlanIntersectionWithoutInterconnect(
						waies.exitWaypoint,
						CoreCommon.RoadNetwork.ArbiterWaypoints[CoreCommon.Mission.MissionCheckpoints.Peek().WaypointId], 
						waies.desired,
						true);

					// check that the plan exists
					if (!testPlan.BestOption.ToInterconnect.Equals(waies.desired) &&
						testPlan.BestRouteTime < double.MaxValue - 1.0)
					{
						// get the desired interconnect
						ArbiterInterconnect reset = testPlan.BestOption.ToInterconnect;

						#region Check that the reset interconnect is feasible

						// test the reset interconnect
						TurnBehavior testResetTurnBehavior = this.DefaultTurnBehavior(reset);

						// set to ignore all vehicles
						testResetTurnBehavior.VehiclesToIgnore = new List<int>(new int[] { -1 });

						// check if we have completed
						CompletionReport turnResetCompletionReport;
						bool completedResetTest = CoreCommon.Communications.TestExecute(testResetTurnBehavior, out turnResetCompletionReport);

						// check to see if this is feasible
						if (reset.TurnDirection == ArbiterTurnDirection.UTurn || (completedResetTest && turnResetCompletionReport is SuccessCompletionReport && reset.Blockage.ProbabilityExists < 0.95))
						{
							// notify
							ArbiterOutput.Output("Found clear interconnect: " + reset.ToString() + " adding blockage to all possible turns into final");

							// set all the interconnects to the final as being blocked
							if (((ITraversableWaypoint)waies.desired.FinalGeneric).IsEntry)
							{
								foreach (ArbiterInterconnect toBlock in ((ITraversableWaypoint)waies.desired.FinalGeneric).Entries)
									CoreCommon.Navigation.AddInterconnectBlockage(toBlock);
							}

							// check if exists previous partition to block
							if (waies.desired.FinalGeneric is ArbiterWaypoint)
							{
								ArbiterWaypoint finWaypoint = (ArbiterWaypoint)waies.desired.FinalGeneric;
								if (finWaypoint.PreviousPartition != null)
									CoreCommon.Navigation.AddBlockage(finWaypoint.PreviousPartition, finWaypoint.Position, false);
							}

							// reset all
							waies.desired = reset;
							waies.turnTestState = TurnTestState.Completed;
							waies.saudi = SAUDILevel.None;
							waies.useTurnBounds = true;
							IntersectionMonitor.ResetDesired(reset);

							// want to reset ourselves
							return new Maneuver(new HoldBrakeBehavior(), CoreCommon.CorePlanningState, TurnDecorators.NoDecorators, vehicleState.Timestamp);
						}

						#endregion
					}
					else
					{
						ArbiterOutput.Output("Entry area blocked, but no otehr valid route found");
					}
				}

				#endregion

				// check if can traverse
				if (IntersectionTactical.IntersectionMonitor == null || IntersectionTactical.IntersectionMonitor.CanTraverse(icaw, vehicleState))
				{
					#region If can traverse the intersection

					// quick check not interconnect
					if (!(icaw is ArbiterInterconnect))
						icaw = icaw.ToInterconnect;

					// get interconnect
					ArbiterInterconnect ai = (ArbiterInterconnect)icaw;

					// clear all old completion reports
					CoreCommon.Communications.ClearCompletionReports();

					// check if uturn
					if (ai.InitialGeneric is ArbiterWaypoint && ai.FinalGeneric is ArbiterWaypoint && ai.TurnDirection == ArbiterTurnDirection.UTurn)
					{
						// go into turn
						List<ArbiterLane> involvedLanes = new List<ArbiterLane>();
						involvedLanes.Add(((ArbiterWaypoint)ai.InitialGeneric).Lane);
						involvedLanes.Add(((ArbiterWaypoint)ai.FinalGeneric).Lane);
						uTurnState nextState = new uTurnState(((ArbiterWaypoint)ai.FinalGeneric).Lane, 
							IntersectionToolkit.uTurnBounds(vehicleState, involvedLanes));
						nextState.Interconnect = ai;

						// hold brake
						Behavior b = new HoldBrakeBehavior();

						// return maneuver
						return new Maneuver(b, nextState, nextState.DefaultStateDecorators, vehicleState.Timestamp);
					}
					else
					{
						if (ai.FinalGeneric is ArbiterWaypoint)
						{
							ArbiterWaypoint finalWaypoint = (ArbiterWaypoint)ai.FinalGeneric;

							// get turn params
							LinePath finalPath;
							LineList leftLL;
							LineList rightLL;
							IntersectionToolkit.TurnInfo(finalWaypoint, out finalPath, out leftLL, out rightLL);

							// go into turn
							IState nextState = new TurnState(ai, ai.TurnDirection, finalWaypoint.Lane, finalPath, leftLL, rightLL, new ScalarSpeedCommand(2.5), waies.saudi, waies.useTurnBounds);

							// hold brake
							Behavior b = new HoldBrakeBehavior();

							// return maneuver
							return new Maneuver(b, nextState, nextState.DefaultStateDecorators, vehicleState.Timestamp);
						}
						else
						{
							// final perimeter waypoint
							ArbiterPerimeterWaypoint apw = (ArbiterPerimeterWaypoint)ai.FinalGeneric;

							// get turn params
							LinePath finalPath;
							LineList leftLL;
							LineList rightLL;
							IntersectionToolkit.ZoneTurnInfo(ai, apw, out finalPath, out leftLL, out rightLL);

							// go into turn
							IState nextState = new TurnState(ai, ai.TurnDirection, null, finalPath, leftLL, rightLL, new ScalarSpeedCommand(2.5), waies.saudi, waies.useTurnBounds);

							// hold brake
							Behavior b = new HoldBrakeBehavior();

							// return maneuver
							return new Maneuver(b, nextState, nextState.DefaultStateDecorators, vehicleState.Timestamp);
						}
					}

					#endregion
				}
				// otherwise need to wait
				else
				{
					IState next = waies;
					return new Maneuver(new HoldBrakeBehavior(), next, next.DefaultStateDecorators, vehicleState.Timestamp);
				}
			}

			#endregion

			#region Stopping At Exit

			else if (planningState is StoppingAtExitState)
			{
				// cast to exit stopping
				StoppingAtExitState saes = (StoppingAtExitState)planningState;
				saes.currentPosition = vehicleState.Front;
				
				// get intersection plan
				IntersectionPlan ip = (IntersectionPlan)navigationalPlan;

				// if has an intersection
				if (CoreCommon.RoadNetwork.IntersectionLookup.ContainsKey(saes.waypoint.AreaSubtypeWaypointId))
				{
					// create new intersection monitor
					IntersectionTactical.IntersectionMonitor = new IntersectionMonitor(
						saes.waypoint,
						CoreCommon.RoadNetwork.IntersectionLookup[saes.waypoint.AreaSubtypeWaypointId],
						vehicleState,
						ip.BestOption);

					// update it
					IntersectionTactical.IntersectionMonitor.Update(vehicleState);
				}
				else
					IntersectionTactical.IntersectionMonitor = null;
								
				// otherwise update the stop parameters
				saes.currentPosition = vehicleState.Front;
				Behavior b = saes.Resume(vehicleState, CoreCommon.Communications.GetVehicleSpeed().Value);
				return new Maneuver(b, saes, saes.DefaultStateDecorators, vehicleState.Timestamp);
			}

			#endregion

			#region In uTurn

			else if (planningState is uTurnState)
			{
				// get state
				uTurnState uts = (uTurnState)planningState;

				// check if in other lane
				if (CoreCommon.Communications.HasCompleted((new UTurnBehavior(null, null, null, null)).GetType()))
				{
					// quick check
					if (uts.Interconnect != null && uts.Interconnect.FinalGeneric is ArbiterWaypoint)
					{
						// get the closest partition to the new lane
						ArbiterLanePartition alpClose = uts.TargetLane.GetClosestPartition(vehicleState.Front);

						// waypoints
						ArbiterWaypoint partitionInitial = alpClose.Initial;
						ArbiterWaypoint uturnEnd = (ArbiterWaypoint)uts.Interconnect.FinalGeneric;

						// check initial past end
						if (partitionInitial.WaypointId.Number > uturnEnd.WaypointId.Number)
						{
							// get waypoints inclusive
							List<ArbiterWaypoint> inclusive = uts.TargetLane.WaypointsInclusive(uturnEnd, partitionInitial);
							bool found = false;

							// loop through
							foreach (ArbiterWaypoint aw in inclusive)
							{
								if (!found && aw.WaypointId.Equals(CoreCommon.Mission.MissionCheckpoints.Peek().WaypointId))
								{
									// notiofy
									ArbiterOutput.Output("removed checkpoint: " + CoreCommon.Mission.MissionCheckpoints.Peek().CheckpointNumber.ToString() + " as passed over in uturn");

									// remove
									CoreCommon.Mission.MissionCheckpoints.Dequeue();

									// set found
									found = true;
								}
							}
						}
						// default check
						else if (uts.Interconnect.FinalGeneric.Equals(CoreCommon.RoadNetwork.ArbiterWaypoints[CoreCommon.Mission.MissionCheckpoints.Peek().WaypointId]))
						{
							// notiofy
							ArbiterOutput.Output("removed checkpoint: " + CoreCommon.Mission.MissionCheckpoints.Peek().CheckpointNumber.ToString() + " as end of uturn");

							// remove
							CoreCommon.Mission.MissionCheckpoints.Dequeue();
						}
					}
					// check if the uturn is for a blockage
					else if (uts.Interconnect == null)
					{
						// get final lane
						ArbiterLane targetLane = uts.TargetLane;

						// check has opposing
						if (targetLane.Way.Segment.Lanes.Count > 1)
						{
							// check the final checkpoint is in our lane
							if (CoreCommon.Mission.MissionCheckpoints.Peek().WaypointId.AreaSubtypeId.Equals(targetLane.LaneId))
							{
								// check that the final checkpoint is within the uturn polygon
								if (uts.Polygon != null &&
									uts.Polygon.IsInside(CoreCommon.RoadNetwork.ArbiterWaypoints[CoreCommon.Mission.MissionCheckpoints.Peek().WaypointId].Position))
								{
									// remove the checkpoint
									ArbiterOutput.Output("Found checkpoint: " + CoreCommon.Mission.MissionCheckpoints.Peek().WaypointId.ToString() + " inside blockage uturn area, dequeuing");
									CoreCommon.Mission.MissionCheckpoints.Dequeue();
								}
							}
						}
					}

					// stay in target lane
					IState nextState = new StayInLaneState(uts.TargetLane, new Probability(0.8, 0.2), true, CoreCommon.CorePlanningState);
					Behavior b = new StayInLaneBehavior(uts.TargetLane.LaneId, new ScalarSpeedCommand(2.0), new List<int>(), uts.TargetLane.LanePath(), uts.TargetLane.Width, uts.TargetLane.NumberOfLanesLeft(vehicleState.Front, true), uts.TargetLane.NumberOfLanesRight(vehicleState.Front, true));
					return new Maneuver(b, nextState, TurnDecorators.NoDecorators, vehicleState.Timestamp);
				}
				// otherwise continue uturn
				else
				{
					// get polygon
					Polygon p = uts.Polygon;

					// add polygon to observable
					CoreCommon.CurrentInformation.DisplayObjects.Add(new ArbiterInformationDisplayObject(p, ArbiterInformationDisplayObjectType.uTurnPolygon));

					// check the type of uturn
					if (!uts.singleLaneUturn)
					{
						// get ending path
						LinePath endingPath = uts.TargetLane.LanePath();

						// next state is current
						IState nextState = uts;

						// behavior
						Behavior b = new UTurnBehavior(p, endingPath, uts.TargetLane.LaneId, new ScalarSpeedCommand(2.0));

						// maneuver
						return new Maneuver(b, nextState, null, vehicleState.Timestamp);
					}
					else
					{
						// get ending path
						LinePath endingPath = uts.TargetLane.LanePath().Clone();
						endingPath = endingPath.ShiftLateral(-2.0);//uts.TargetLane.Width);

						// add polygon to observable
						CoreCommon.CurrentInformation.DisplayObjects.Add(new ArbiterInformationDisplayObject(endingPath, ArbiterInformationDisplayObjectType.leftBound));

						// next state is current
						IState nextState = uts;

						// behavior
						Behavior b = new UTurnBehavior(p, endingPath, uts.TargetLane.LaneId, new ScalarSpeedCommand(2.0));

						// maneuver
						return new Maneuver(b, nextState, null, vehicleState.Timestamp);
					}
				}
			}

			#endregion

			#region In Turn

			else if (planningState is TurnState)
			{
				// get state
				TurnState ts = (TurnState)planningState;
					
				// add bounds to observable
				if (ts.LeftBound != null && ts.RightBound != null)
				{
					CoreCommon.CurrentInformation.DisplayObjects.Add(new ArbiterInformationDisplayObject(ts.LeftBound, ArbiterInformationDisplayObjectType.leftBound));
					CoreCommon.CurrentInformation.DisplayObjects.Add(new ArbiterInformationDisplayObject(ts.RightBound, ArbiterInformationDisplayObjectType.rightBound));
				}

				// create current turn reasoning
				if(this.TurnReasoning == null)
					this.TurnReasoning = new TurnReasoning(ts.Interconnect,
					IntersectionTactical.IntersectionMonitor != null ? IntersectionTactical.IntersectionMonitor.EntryAreaMonitor : null);

				// get primary maneuver
				Maneuver primary = this.TurnReasoning.PrimaryManeuver(vehicleState, blockages, ts);

				// get secondary maneuver
				Maneuver? secondary = this.TurnReasoning.SecondaryManeuver(vehicleState, (IntersectionPlan)navigationalPlan);

				// return the manevuer
				return secondary.HasValue ? secondary.Value : primary;
			}

			#endregion

			#region Itnersection Startup

			else if (planningState is IntersectionStartupState)
			{
				// state and plan
				IntersectionStartupState iss = (IntersectionStartupState)planningState;
				IntersectionStartupPlan isp = (IntersectionStartupPlan)navigationalPlan;

				// initial path
				LinePath vehiclePath = new LinePath(new Coordinates[] { vehicleState.Rear, vehicleState.Front });
				List<ITraversableWaypoint> feasibleEntries = new List<ITraversableWaypoint>();

				// vehicle polygon forward of us
				Polygon vehicleForward = vehicleState.ForwardPolygon;
				
				// best waypoint
				ITraversableWaypoint best = null;
				double bestCost = Double.MaxValue;

				// given feasible choose best, no feasible choose random
				if (feasibleEntries.Count == 0)
				{
					foreach (ITraversableWaypoint itw in iss.Intersection.AllEntries.Values)
					{
						if (vehicleForward.IsInside(itw.Position))
						{
							feasibleEntries.Add(itw);
						}
					}

					if (feasibleEntries.Count == 0)
					{
						foreach (ITraversableWaypoint itw in iss.Intersection.AllEntries.Values)
						{
							feasibleEntries.Add(itw);
						}
					}
				}

				// get best
				foreach (ITraversableWaypoint itw in feasibleEntries)
				{
					if (isp.NodeTimeCosts.ContainsKey(itw))
					{
						KeyValuePair<ITraversableWaypoint, double> lookup = new KeyValuePair<ITraversableWaypoint, double>(itw, isp.NodeTimeCosts[itw]);

						if (best == null || lookup.Value < bestCost)
						{
							best = lookup.Key;
							bestCost = lookup.Value;
						}
					}
				}
				
				// get something going to this waypoint
				ArbiterInterconnect interconnect = null;
				if(best.IsEntry)
				{
					ArbiterInterconnect closest = null;
					double closestDistance = double.MaxValue;

					foreach (ArbiterInterconnect ai in best.Entries)
					{
						double dist = ai.InterconnectPath.GetClosestPoint(vehicleState.Front).Location.DistanceTo(vehicleState.Front);
						if (closest == null || dist < closestDistance)
						{
							closest = ai;
							closestDistance = dist;
						}
					}

					interconnect = closest;
				}
				else if(best is ArbiterWaypoint && ((ArbiterWaypoint)best).PreviousPartition != null)
				{
					interconnect = ((ArbiterWaypoint)best).PreviousPartition.ToInterconnect;
				}

				// get state
				if (best is ArbiterWaypoint)
				{
					// go to this turn state
					LinePath finalPath;
					LineList leftBound;
					LineList rightBound;
					IntersectionToolkit.TurnInfo((ArbiterWaypoint)best, out finalPath, out leftBound, out rightBound);
					return new Maneuver(new HoldBrakeBehavior(), new TurnState(interconnect, interconnect.TurnDirection, ((ArbiterWaypoint)best).Lane,
						finalPath, leftBound, rightBound, new ScalarSpeedCommand(2.0)), TurnDecorators.NoDecorators, vehicleState.Timestamp);
				}
				else
				{
					// go to this turn state
					LinePath finalPath;
					LineList leftBound;
					LineList rightBound;
					IntersectionToolkit.ZoneTurnInfo(interconnect, (ArbiterPerimeterWaypoint)best, out finalPath, out leftBound, out rightBound);
					return new Maneuver(new HoldBrakeBehavior(), new TurnState(interconnect, interconnect.TurnDirection, null,
						finalPath, leftBound, rightBound, new ScalarSpeedCommand(2.0)), TurnDecorators.NoDecorators, vehicleState.Timestamp);
				}
			}

			#endregion

			#region Unknown

			else
			{
				throw new Exception("Unknown planning state in intersection tactical plan: " + planningState.ToString());
			}

			#endregion
		}

		#region Helpers

		private TurnBehavior DefaultTurnBehavior(IConnectAreaWaypoints icaw)
		{
			#region Determine Behavior to Accomplish Turn

			// get interconnect
			ArbiterInterconnect ai = icaw.ToInterconnect;

			// behavior we wish to accomplish
			TurnBehavior testTurnBehavior = null;
			TurnState testTurnState = null;

			#region Turn to Lane

			if (ai.FinalGeneric is ArbiterWaypoint)
			{
				// get final wp
				ArbiterWaypoint finalWaypoint = (ArbiterWaypoint)ai.FinalGeneric;

				// get turn params
				LinePath finalPath;
				LineList leftLL;
				LineList rightLL;
				IntersectionToolkit.TurnInfo(finalWaypoint, out finalPath, out leftLL, out rightLL);

				// go into turn
				testTurnState = new TurnState(ai, ai.TurnDirection, finalWaypoint.Lane, finalPath, leftLL, rightLL, new ScalarSpeedCommand(2.5));
			}

			#endregion

			#region Turn to Zone

			else
			{
				// final perimeter waypoint
				ArbiterPerimeterWaypoint apw = (ArbiterPerimeterWaypoint)ai.FinalGeneric;

				// get turn params
				LinePath finalPath;
				LineList leftLL;
				LineList rightLL;
				IntersectionToolkit.ZoneTurnInfo(ai, apw, out finalPath, out leftLL, out rightLL);

				// go into turn
				testTurnState = new TurnState(ai, ai.TurnDirection, null, finalPath, leftLL, rightLL, new ScalarSpeedCommand(2.5));
			}

			#endregion

			// get behavior
			testTurnBehavior = (TurnBehavior)testTurnState.Resume(CoreCommon.Communications.GetVehicleState(), CoreCommon.Communications.GetVehicleSpeed().Value);
			testTurnBehavior.TimeStamp = CoreCommon.Communications.GetVehicleState().Timestamp;

			// return the behavior
			return testTurnBehavior;

			#endregion
		}

		#endregion
	}
}
