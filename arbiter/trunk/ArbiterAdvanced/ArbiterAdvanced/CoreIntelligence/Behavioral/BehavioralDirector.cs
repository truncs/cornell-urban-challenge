using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Arbiter.ArbiterMission;
using UrbanChallenge.Common.Sensors.Vehicle;
using UrbanChallenge.Common.Sensors.Obstacle;
using UrbanChallenge.Arbiter.Core.Common;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Navigation;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Initialization;
using UrbanChallenge.Common;
using UrbanChallenge.Arbiter.Core.Common.State;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.State;
using UrbanChallenge.Arbiter.Core.Communications;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road;
using UrbanChallenge.Arbiter.Core.Common.Reasoning;
using UrbanChallenge.Arbiter.Core.Common.Tools;
using UrbanChallenge.Arbiter.Core.Common.Arbiter;
using System.Diagnostics;
using UrbanChallenge.Common.Sensors;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Intersection;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Tools;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Behaviors.CompletionReport;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Behavioral
{
	/// <summary>
	/// The behavioral director initializes the planning cycle
	/// </summary>
	public class BehavioralDirector
	{
		#region Private Members

		/// <summary>
		///  Navigator
		/// </summary>
		private Navigator navigation;

		/// <summary>
		/// Tactical layer
		/// </summary>
		public TacticalDirector tactical;

		/// <summary>
		/// Initialization helper
		/// </summary>
		private InitializationComponent initialization;

		/// <summary>
		/// Handles blockages at a behavioral and tactical level
		/// </summary>
		public BlockageHandler blockageHandler;

		/// <summary>
		/// Lane agents
		/// </summary>
		private LaneAgent laneAgent;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		public BehavioralDirector()
		{
			laneAgent = new LaneAgent();
			navigation = new Navigator();
			tactical = new TacticalDirector();
			initialization = new InitializationComponent();
			CoreCommon.Navigation = navigation;
			
			// create and set blockage handler
			blockageHandler = new BlockageHandler();
		}

		#endregion

		#region Functions

		/// <summary>
		/// Plans the next maneuver
		/// </summary>
		/// <param name="roads"></param>
		/// <param name="mission"></param>
		/// <param name="vehicleState"></param>
		/// <param name="CoreCommon.CorePlanningState"></param>
		/// <param name="observedVehicles"></param>
		/// <param name="observedObstacles"></param>
		/// <param name="coreState"></param>
		/// <param name="carMode"></param>
		/// <returns></returns>
		public Maneuver Plan(VehicleState vehicleState, double vehicleSpeed,
			SceneEstimatorTrackedClusterCollection observedVehicles, SceneEstimatorUntrackedClusterCollection observedObstacles, 
			CarMode carMode, INavigableNode goal)
		{				
			// set blockages
			List<ITacticalBlockage> blockages = this.blockageHandler.DetermineBlockages(CoreCommon.CorePlanningState);

			#region Travel State

			if (CoreCommon.CorePlanningState is TravelState)
			{
				#region Stay in Lane State

				if (CoreCommon.CorePlanningState is StayInLaneState)
				{
					// get lane state
					StayInLaneState sils = (StayInLaneState)CoreCommon.CorePlanningState;

					#region Blockages

					// check blockages
					if (blockages != null && blockages.Count > 0 && blockages[0] is LaneBlockage)
					{
						// create the blockage state
						EncounteredBlockageState ebs = new EncounteredBlockageState(blockages[0], CoreCommon.CorePlanningState);

						// check not from a dynamicly moving vehicle
						if (blockages[0].BlockageReport.BlockageType != BlockageType.Dynamic)
						{
							// go to a blockage handling tactical
							return new Maneuver(new HoldBrakeBehavior(), ebs, TurnDecorators.NoDecorators, vehicleState.Timestamp);
						}
						else
							ArbiterOutput.Output("Lane blockage reported for moving vehicle, ignoring");
					}

					#endregion

					// update the total time ignorable have been seen
					sils.UpdateIgnoreList();

					// nav plan to find poi
					RoadPlan rp = navigation.PlanNavigableArea(sils.Lane, vehicleState.Position, goal, sils.WaypointsToIgnore);

					// check for unreachable route
					if (rp.BestPlan.laneWaypointOfInterest.BestRoute != null && 
						rp.BestPlan.laneWaypointOfInterest.BestRoute.Count == 0 && 
						rp.BestPlan.laneWaypointOfInterest.RouteTime >= Double.MaxValue - 1.0)
					{
						ArbiterOutput.Output("Removed Unreachable Checkpoint: " + CoreCommon.Mission.MissionCheckpoints.Peek().CheckpointNumber.ToString());
						CoreCommon.Mission.MissionCheckpoints.Dequeue();
						return new Maneuver(new NullBehavior(), CoreCommon.CorePlanningState, TurnDecorators.NoDecorators, vehicleState.Timestamp);
					}
					else if (rp.BestPlan.laneWaypointOfInterest.TimeCostToPoint >= Double.MaxValue - 1.0)
					{
						ArbiterOutput.Output("Best Lane Waypoint of Interest is END OF LANE WITH NO INTERCONNECTS, LEADING NOWHERE");
						ArbiterOutput.Output("Removed Unreachable Checkpoint: " + CoreCommon.Mission.MissionCheckpoints.Peek().CheckpointNumber.ToString());
						CoreCommon.Mission.MissionCheckpoints.Dequeue();
						return new Maneuver(new NullBehavior(), CoreCommon.CorePlanningState, TurnDecorators.NoDecorators, vehicleState.Timestamp);
					}

					#region Check Supra Lane Availability
					
					// if the poi is at the end of this lane, is not stop, leads to another lane, and has no overlapping lanes
					// or if the poi's best exit is an exit in this lane, is not a stop, has no overlapping lanes and leads to another lane
					// create supralane

					// check if navigation is corrent in saying we want to continue on the current lane and we're far enough along the lane, 30m for now
					if(rp.BestPlan.Lane.Equals(sils.Lane.LaneId))
					{
						// get navigation poi
						DownstreamPointOfInterest dpoi = rp.BestPlan.laneWaypointOfInterest;

						// check that the poi is not stop and is not the current checkpoint
						if(!dpoi.PointOfInterest.IsStop && !(CoreCommon.Mission.MissionCheckpoints.Peek().WaypointId.Equals(dpoi.PointOfInterest.WaypointId)))
						{
							// get the best exit or the poi
							ArbiterInterconnect ai = dpoi.BestExit;

							// check if exit goes into a lane and not a uturn							
							if(ai != null && ai.FinalGeneric is ArbiterWaypoint && ai.TurnDirection != ArbiterTurnDirection.UTurn)
							{
								// final lane or navigation poi interconnect
								ArbiterLane al = ((ArbiterWaypoint)ai.FinalGeneric).Lane;

								// check not same lane
								if (!al.Equals(sils.Lane))
								{
									// check if enough room to start
									bool enoughRoom = !sils.Lane.Equals(al) || sils.Lane.LanePath(sils.Lane.WaypointList[0].Position, vehicleState.Front).PathLength > 30;
									if (enoughRoom)
									{
										// try to get intersection associated with the exit
										ArbiterIntersection aInter = CoreCommon.RoadNetwork.IntersectionLookup.ContainsKey(dpoi.PointOfInterest.WaypointId) ?
											CoreCommon.RoadNetwork.IntersectionLookup[dpoi.PointOfInterest.WaypointId] : null;

										// check no intersection or no overlapping lanes
										if (aInter == null || !aInter.PriorityLanes.ContainsKey(ai) || aInter.PriorityLanes[ai].Count == 0)
										{
											// create the supra lane
											SupraLane sl = new SupraLane(sils.Lane, ai, al);

											// switch to the supra lane state
											StayInSupraLaneState sisls = new StayInSupraLaneState(sl, CoreCommon.CorePlanningState);
											sisls.UpdateState(vehicleState.Front);

											// set
											return new Maneuver(new NullBehavior(), sisls, TurnDecorators.NoDecorators, vehicleState.Timestamp);
										}
									}
								}
							}
						}
					}

					#endregion

					// plan final tactical maneuver
					Maneuver final = tactical.Plan(CoreCommon.CorePlanningState, rp, vehicleState, observedVehicles, observedObstacles, blockages, vehicleSpeed);

					// return final maneuver
					return final;
				}

				#endregion

				#region Stay in Supra Lane State

				else if (CoreCommon.CorePlanningState is StayInSupraLaneState)
				{
					// state
					StayInSupraLaneState sisls = (StayInSupraLaneState)CoreCommon.CorePlanningState;

					#region Blockages

					// check blockages
					if (blockages != null && blockages.Count > 0 && blockages[0] is LaneBlockage)
					{
						// create the blockage state
						EncounteredBlockageState ebs = new EncounteredBlockageState(blockages[0], CoreCommon.CorePlanningState);

						// check not from a dynamicly moving vehicle
						if (blockages[0].BlockageReport.BlockageType != BlockageType.Dynamic)
						{
							// go to a blockage handling tactical
							return new Maneuver(new HoldBrakeBehavior(), ebs, TurnDecorators.NoDecorators, vehicleState.Timestamp);
						}
						else
							ArbiterOutput.Output("Lane blockage reported for moving vehicle, ignoring");
					}

					#endregion

					// check if we are in the final lane
					if (sisls.Lane.ClosestComponent(vehicleState.Position) == SLComponentType.Final)
					{
						// go to stay in lane
						return new Maneuver(new NullBehavior(), new StayInLaneState(sisls.Lane.Final, sisls), TurnDecorators.NoDecorators, vehicleState.Timestamp);
					}

					// update ignorable
					sisls.UpdateIgnoreList();

					// nav plan to find points
					RoadPlan rp = navigation.PlanNavigableArea(sisls.Lane, vehicleState.Position, goal, sisls.WaypointsToIgnore);

					// check for unreachable route
					if (rp.BestPlan.laneWaypointOfInterest.BestRoute != null && 
						rp.BestPlan.laneWaypointOfInterest.BestRoute.Count == 0 && 
						rp.BestPlan.laneWaypointOfInterest.RouteTime >= Double.MaxValue - 1.0)
					{
						ArbiterOutput.Output("Removed Unreachable Checkpoint: " + CoreCommon.Mission.MissionCheckpoints.Peek().CheckpointNumber.ToString());
						CoreCommon.Mission.MissionCheckpoints.Dequeue();
						return new Maneuver(new NullBehavior(), CoreCommon.CorePlanningState, TurnDecorators.NoDecorators, vehicleState.Timestamp);
					}

					// plan
					Maneuver final = tactical.Plan(CoreCommon.CorePlanningState, rp, vehicleState, observedVehicles, observedObstacles, blockages, vehicleSpeed);

					// update current state
					sisls.UpdateState(vehicleState.Front);

					// return final maneuver
					return final;
				}

				#endregion

				#region Stopping At Stop State

				else if (CoreCommon.CorePlanningState is StoppingAtStopState)
				{
					// get state
					StoppingAtStopState sass = (StoppingAtStopState)CoreCommon.CorePlanningState;

					// check to see if we're stopped
					// check if in other lane
					if (CoreCommon.Communications.HasCompleted((new StayInLaneBehavior(null, null, null)).GetType()))
					{
						// update intersection monitor
						if (CoreCommon.RoadNetwork.IntersectionLookup.ContainsKey(sass.waypoint.AreaSubtypeWaypointId))
						{
							// nav plan
							IntersectionPlan ip = navigation.PlanIntersection(sass.waypoint, goal);

							// update intersection monitor
							this.tactical.Update(observedVehicles, vehicleState);
							IntersectionTactical.IntersectionMonitor = new IntersectionMonitor(
								sass.waypoint,
								CoreCommon.RoadNetwork.IntersectionLookup[sass.waypoint.AreaSubtypeWaypointId],
								vehicleState, ip.BestOption);
						}
						else
						{
							IntersectionTactical.IntersectionMonitor = null;
						}

						// check if we've hit goal if stop is cp
						if (sass.waypoint.WaypointId.Equals(CoreCommon.Mission.MissionCheckpoints.Peek().WaypointId))
						{
							ArbiterOutput.Output("Stopped at current goal: " + CoreCommon.Mission.MissionCheckpoints.Peek().WaypointId.ToString() + ", Removing");
							CoreCommon.Mission.MissionCheckpoints.Dequeue();

							if (CoreCommon.Mission.MissionCheckpoints.Count == 0)
							{
								return new Maneuver(new HoldBrakeBehavior(), new NoGoalsLeftState(), TurnDecorators.NoDecorators, vehicleState.Timestamp);
							}
						}

						// move to the intersection
						IState next = new WaitingAtIntersectionExitState(sass.waypoint, sass.turnDirection, new IntersectionDescription(), sass.desiredExit);
						Behavior b = new HoldBrakeBehavior();
						return new Maneuver(b, next, sass.DefaultStateDecorators, vehicleState.Timestamp);
					}
					else
					{
						// otherwise update the stop parameters
						Behavior b = sass.Resume(vehicleState, vehicleSpeed);
						return new Maneuver(b, CoreCommon.CorePlanningState, sass.DefaultStateDecorators, vehicleState.Timestamp);
					}
				}

				#endregion

				#region Change Lanes State

				else if (CoreCommon.CorePlanningState is ChangeLanesState)
				{
					// get state
					ChangeLanesState cls = (ChangeLanesState)CoreCommon.CorePlanningState;

					#region Blockages

					// check blockages
					if (blockages != null && blockages.Count > 0 && blockages[0] is LaneChangeBlockage)
					{
						// create the blockage state
						EncounteredBlockageState ebs = new EncounteredBlockageState(blockages[0], CoreCommon.CorePlanningState);

						// check not from a dynamicly moving vehicle
						if (blockages[0].BlockageReport.BlockageType != BlockageType.Dynamic)
						{
							// go to a blockage handling tactical
							return new Maneuver(new NullBehavior(), ebs, TurnDecorators.NoDecorators, vehicleState.Timestamp);
						}
						else
							ArbiterOutput.Output("Lane Change blockage reported for moving vehicle, ignoring");
					}

					#endregion

					// get a good lane
					ArbiterLane goodLane = null;
					if(!cls.Parameters.InitialOncoming)
						goodLane = cls.Parameters.Initial;
					else if(!cls.Parameters.TargetOncoming)
						goodLane = cls.Parameters.Target;
					else
						throw new Exception("not going from or to good lane");

					// nav plan to find poi
					#warning make goal better if there is none come to stop
					RoadPlan rp = navigation.PlanNavigableArea(goodLane, vehicleState.Front, 
						CoreCommon.RoadNetwork.ArbiterWaypoints[CoreCommon.Mission.MissionCheckpoints.Peek().WaypointId], new List<ArbiterWaypoint>());

					// check current behavior type
					bool done = CoreCommon.Communications.HasCompleted((new ChangeLaneBehavior(null, null, false, 0, null, null)).GetType());

					if (done)
					{
						if (cls.Parameters.TargetOncoming)
							return new Maneuver(
								new StayInLaneBehavior(cls.Parameters.Target.LaneId, 
									new ScalarSpeedCommand(cls.Parameters.Parameters.RecommendedSpeed),
									cls.Parameters.Parameters.VehiclesToIgnore,
									cls.Parameters.Target.ReversePath,
									cls.Parameters.Target.Width,
									cls.Parameters.Target.NumberOfLanesRight(vehicleState.Front, !cls.Parameters.InitialOncoming),
									cls.Parameters.Target.NumberOfLanesLeft(vehicleState.Front, !cls.Parameters.InitialOncoming)),
								new OpposingLanesState(cls.Parameters.Target, true, cls, vehicleState), TurnDecorators.NoDecorators, vehicleState.Timestamp);
						else
							return new Maneuver(
								new StayInLaneBehavior(cls.Parameters.Target.LaneId,
									new ScalarSpeedCommand(cls.Parameters.Parameters.RecommendedSpeed),
									cls.Parameters.Parameters.VehiclesToIgnore,
									cls.Parameters.Target.LanePath(),
									cls.Parameters.Target.Width,
									cls.Parameters.Target.NumberOfLanesLeft(vehicleState.Front, !cls.Parameters.InitialOncoming),
									cls.Parameters.Target.NumberOfLanesRight(vehicleState.Front, !cls.Parameters.InitialOncoming)), 
								new StayInLaneState(cls.Parameters.Target, CoreCommon.CorePlanningState), TurnDecorators.NoDecorators, vehicleState.Timestamp);
					}
					else
					{
						return tactical.Plan(cls, rp, vehicleState, observedVehicles, observedObstacles, blockages, vehicleSpeed);
					}
				}

				#endregion

				#region Opposing Lanes State

				else if (CoreCommon.CorePlanningState is OpposingLanesState)
				{
					// get state
					OpposingLanesState ols = (OpposingLanesState)CoreCommon.CorePlanningState;
					ols.SetClosestGood(vehicleState);

					#region Blockages

					// check blockages
					if (blockages != null && blockages.Count > 0 && blockages[0] is OpposingLaneBlockage)
					{
						// create the blockage state
						EncounteredBlockageState ebs = new EncounteredBlockageState(blockages[0], CoreCommon.CorePlanningState);

						// check not from a dynamicly moving vehicle
						if (blockages[0].BlockageReport.BlockageType != BlockageType.Dynamic)
						{
							// go to a blockage handling tactical
							return new Maneuver(new HoldBrakeBehavior(), ebs, TurnDecorators.NoDecorators, vehicleState.Timestamp);
						}
						else
							ArbiterOutput.Output("Opposing Lane blockage reported for moving vehicle, ignoring");
					}

					#endregion

					// check closest good null
					if (ols.ClosestGoodLane != null)
					{
						// nav plan to find poi
						RoadPlan rp = navigation.PlanNavigableArea(ols.ClosestGoodLane, vehicleState.Position, goal, new List<ArbiterWaypoint>());

						// plan final tactical maneuver
						Maneuver final = tactical.Plan(CoreCommon.CorePlanningState, rp, vehicleState, observedVehicles, observedObstacles, blockages, vehicleSpeed);

						// return final maneuver
						return final;
					}
					// otherwise need to make a uturn
					else
					{
						ArbiterOutput.Output("in opposing lane with no closest good, making a uturn");
						ArbiterLanePartition alp = ols.OpposingLane.GetClosestPartition(vehicleState.Front);
						Coordinates c1 = vehicleState.Front + alp.Vector().Normalize(8.0);
						Coordinates c2 = vehicleState.Front - alp.Vector().Normalize(8.0);
						LinePath lpTmp = new LinePath(new Coordinates[] { c1, c2 });
						List<Coordinates> pCoords = new List<Coordinates>();
						pCoords.AddRange(lpTmp.ShiftLateral(ols.OpposingLane.Width)); //* 1.5));
						pCoords.AddRange(lpTmp.ShiftLateral(-ols.OpposingLane.Width));// / 2.0));
						Polygon uturnPoly = Polygon.GrahamScan(pCoords);
						uTurnState uts = new uTurnState(ols.OpposingLane, uturnPoly, true);
						uts.Interconnect = alp.ToInterconnect;

						// plan final tactical maneuver
						Maneuver final = new Maneuver(new NullBehavior(), uts, TurnDecorators.LeftTurnDecorator, vehicleState.Timestamp);

						// return final maneuver
						return final;
					}
				}

				#endregion

				#region Starting up off of chute state

				else if (CoreCommon.CorePlanningState is StartupOffChuteState)
				{
					// cast the type
					StartupOffChuteState socs = (StartupOffChuteState)CoreCommon.CorePlanningState;
					
					// check if in lane part of chute
					if (CoreCommon.Communications.HasCompleted((new TurnBehavior(null, null, null, null, null, null)).GetType()))
					{
						// go to lane state
						return new Maneuver(new NullBehavior(), new StayInLaneState(socs.Final.Lane, new Probability(0.8, 0.2), true, socs), TurnDecorators.NoDecorators, vehicleState.Timestamp);
					}
					// otherwise continue
					else
					{
						// simple maneuver generation
						TurnBehavior tb = (TurnBehavior)socs.Resume(vehicleState, 1.4);

						// add bounds to observable
						CoreCommon.CurrentInformation.DisplayObjects.Add(new ArbiterInformationDisplayObject(tb.LeftBound, ArbiterInformationDisplayObjectType.leftBound));
						CoreCommon.CurrentInformation.DisplayObjects.Add(new ArbiterInformationDisplayObject(tb.RightBound, ArbiterInformationDisplayObjectType.rightBound));

						// final maneuver
						return new Maneuver(tb, socs, TurnDecorators.NoDecorators, vehicleState.Timestamp);
					}
				}

				#endregion

				#region Unknown

				else
				{
					// non-handled state
					throw new ArgumentException("Unknown state", "CoreCommon.CorePlanningState");
				}

				#endregion
			}

			#endregion

			#region Intersection State

			else if (CoreCommon.CorePlanningState is IntersectionState)
			{
				#region Waiting at Intersection Exit State

				if (CoreCommon.CorePlanningState is WaitingAtIntersectionExitState)
				{
					// get state
					WaitingAtIntersectionExitState waies = (WaitingAtIntersectionExitState)CoreCommon.CorePlanningState;

					// nav plan
					IntersectionPlan ip = navigation.PlanIntersection(waies.exitWaypoint, goal);

					// plan
					Maneuver final = tactical.Plan(waies, ip, vehicleState, observedVehicles, observedObstacles, blockages, vehicleSpeed);

					// return final maneuver
					return final;
				}

				#endregion

				#region Stopping At Exit State

				else if (CoreCommon.CorePlanningState is StoppingAtExitState)
				{
					// get state
					StoppingAtExitState saes = (StoppingAtExitState)CoreCommon.CorePlanningState;					

					// check to see if we're stopped
					if (CoreCommon.Communications.HasCompleted((new StayInLaneBehavior(null, null, null)).GetType()))
					{
						// check if we've hit goal if stop is cp
						if (saes.waypoint.WaypointId.Equals(CoreCommon.Mission.MissionCheckpoints.Peek().WaypointId))
						{
							ArbiterOutput.Output("Stopped at current goal: " + CoreCommon.Mission.MissionCheckpoints.Peek().WaypointId.ToString() + ", Removing");
							CoreCommon.Mission.MissionCheckpoints.Dequeue();

							if (CoreCommon.Mission.MissionCheckpoints.Count == 0)
							{
								return new Maneuver(new HoldBrakeBehavior(), new NoGoalsLeftState(), TurnDecorators.NoDecorators, vehicleState.Timestamp);
							}
						}

						// move to the intersection
						IState next = new WaitingAtIntersectionExitState(saes.waypoint, saes.turnDirection, new IntersectionDescription(), saes.desiredExit);
						Behavior b = new HoldBrakeBehavior();
						return new Maneuver(b, next, saes.DefaultStateDecorators, vehicleState.Timestamp);
					}
					else
					{
						// nav plan
						IntersectionPlan ip = navigation.PlanIntersection(saes.waypoint, goal);

						// update the intersection monitor
						if (CoreCommon.RoadNetwork.IntersectionLookup.ContainsKey(saes.waypoint.AreaSubtypeWaypointId))
						{
							IntersectionTactical.IntersectionMonitor = new IntersectionMonitor(
								saes.waypoint,
								CoreCommon.RoadNetwork.IntersectionLookup[saes.waypoint.AreaSubtypeWaypointId],
								vehicleState, ip.BestOption);
						}
						else
							IntersectionTactical.IntersectionMonitor = null;

						// plan final tactical maneuver
						Maneuver final = tactical.Plan(saes, ip, vehicleState, observedVehicles, observedObstacles, blockages, vehicleSpeed);

						// return final maneuver
						return final;
					}
				}

				#endregion

				#region Turn State

				else if (CoreCommon.CorePlanningState is TurnState)
				{
					// get state
					TurnState ts = (TurnState)CoreCommon.CorePlanningState;

					// check if in other lane
					if (CoreCommon.Communications.HasCompleted((new TurnBehavior(null, null, null, null, null, null)).GetType()))
					{
						if (ts.Interconnect.FinalGeneric is ArbiterWaypoint)
						{
							// get final wp, and if next cp, remove
							ArbiterWaypoint final = (ArbiterWaypoint)ts.Interconnect.FinalGeneric;
							if (CoreCommon.Mission.MissionCheckpoints.Peek().WaypointId.Equals(final.AreaSubtypeWaypointId))
								CoreCommon.Mission.MissionCheckpoints.Dequeue();

							// stay in target lane
							IState nextState = new StayInLaneState(ts.TargetLane, new Probability(0.8, 0.2), true, CoreCommon.CorePlanningState);
							Behavior b = new NullBehavior();
							return new Maneuver(b, nextState, TurnDecorators.NoDecorators, vehicleState.Timestamp);
						}
						else if (ts.Interconnect.FinalGeneric is ArbiterPerimeterWaypoint)
						{
							// stay in target lane
							IState nextState = new ZoneTravelingState(((ArbiterPerimeterWaypoint)ts.Interconnect.FinalGeneric).Perimeter.Zone, (INavigableNode)ts.Interconnect.FinalGeneric);
							Behavior b = new NullBehavior();
							return new Maneuver(b, nextState, TurnDecorators.NoDecorators, vehicleState.Timestamp);
						}
						else
							throw new Exception("unhandled unterconnect final wp type");
					}

					// get interconnect
					if (ts.Interconnect.FinalGeneric is ArbiterWaypoint)
					{
						// nav plan
						IntersectionPlan ip = navigation.PlanIntersection((ITraversableWaypoint)ts.Interconnect.InitialGeneric, goal);

						// plan
						Maneuver final = tactical.Plan(CoreCommon.CorePlanningState, ip, vehicleState, observedVehicles, observedObstacles, blockages, vehicleSpeed);

						// return final maneuver
						return final;
					}
					// else to zone
					else if (ts.Interconnect.FinalGeneric is ArbiterPerimeterWaypoint)
					{
						// plan
						Maneuver final = tactical.Plan(CoreCommon.CorePlanningState, null, vehicleState, observedVehicles, observedObstacles, blockages, vehicleSpeed);

						// return final maneuver
						return final;
					}
					else
					{
						throw new Exception("method not imp");
					}
				}

				#endregion

				#region uTurn State

				else if (CoreCommon.CorePlanningState is uTurnState)
				{
					// get state
					uTurnState uts = (uTurnState)CoreCommon.CorePlanningState;

					// plan over the target segment, ignoring the initial waypoint of the target lane
					ArbiterWaypoint initial = uts.TargetLane.GetClosestPartition(vehicleState.Position).Initial;
					List<ArbiterWaypoint> iws = RoadToolkit.WaypointsClose(initial.Lane.Way, vehicleState.Front, initial);
					RoadPlan rp = navigation.PlanRoads(uts.TargetLane, vehicleState.Front, goal, iws);

					// plan
					Maneuver final = tactical.Plan(CoreCommon.CorePlanningState, rp, vehicleState, observedVehicles, observedObstacles, blockages, vehicleSpeed);

					// return final maneuver
					return final;
				}

				#endregion

				#region Intersection Startup State

				else if (CoreCommon.CorePlanningState is IntersectionStartupState)
				{
					// get startup state
					IntersectionStartupState iss = (IntersectionStartupState)CoreCommon.CorePlanningState;

					// get intersection
					ArbiterIntersection ai = iss.Intersection;

					// get plan
					IEnumerable<ITraversableWaypoint> entries = ai.AllEntries.Values;
					IntersectionStartupPlan isp = navigation.PlanIntersectionStartup(entries, goal);

					// plan tac
					Maneuver final = tactical.Plan(iss, isp, vehicleState, observedVehicles, observedObstacles, blockages, vehicleSpeed);

					// return 
					return final;
				}

				#endregion

				#region Unknown

				else
				{
					// non-handled state
					throw new ArgumentException("Unknown state", "CoreCommon.CorePlanningState");
				}

				#endregion
			}

			#endregion

			#region Zone State

			else if (CoreCommon.CorePlanningState is ZoneState)
			{
				#region Zone Travelling State

				if (CoreCommon.CorePlanningState is ZoneTravelingState)
				{
					// set state
					ZoneTravelingState zts = (ZoneTravelingState)CoreCommon.CorePlanningState;

					// check to see if we're stopped
					if (CoreCommon.Communications.HasCompleted((new ZoneTravelingBehavior(null, null, new Polygon[0], null, null, null, null)).GetType()))
					{
						// plan over state and zone
						ZonePlan zp = this.navigation.PlanZone(zts.Zone, zts.Start, CoreCommon.RoadNetwork.ArbiterWaypoints[CoreCommon.Mission.MissionCheckpoints.Peek().WaypointId]);

						if (zp.ZoneGoal is ArbiterParkingSpotWaypoint)
						{
							// move to parking state
							ParkingState ps = new ParkingState(zp.Zone, ((ArbiterParkingSpotWaypoint)zp.ZoneGoal).ParkingSpot);
							return new Maneuver(new HoldBrakeBehavior(), ps, TurnDecorators.NoDecorators, vehicleState.Timestamp);

						}
						else if(zp.ZoneGoal is ArbiterPerimeterWaypoint)
						{
							// get plan
							IntersectionPlan ip = navigation.GetIntersectionExitPlan((ITraversableWaypoint)zp.ZoneGoal, goal);

							// move to exit
							WaitingAtIntersectionExitState waies = new WaitingAtIntersectionExitState((ITraversableWaypoint)zp.ZoneGoal, ip.BestOption.ToInterconnect.TurnDirection, new IntersectionDescription(), ip.BestOption.ToInterconnect);
							return new Maneuver(new HoldBrakeBehavior(), waies, TurnDecorators.NoDecorators, vehicleState.Timestamp);
						}
					}
					else
					{
						// plan over state and zone
						ZonePlan zp = this.navigation.PlanZone(zts.Zone, zts.Start, CoreCommon.RoadNetwork.ArbiterWaypoints[CoreCommon.Mission.MissionCheckpoints.Peek().WaypointId]);

						// plan
						Maneuver final = tactical.Plan(CoreCommon.CorePlanningState, zp, vehicleState, observedVehicles, observedObstacles, blockages, vehicleSpeed);

						// return final maneuver
						return final;
					}
				}

				#endregion

				#region Parking State

				else if (CoreCommon.CorePlanningState is ParkingState)
				{
					// set state
					ParkingState ps = (ParkingState)CoreCommon.CorePlanningState;

					// check to see if we're stopped
					if (CoreCommon.Communications.HasCompleted((new ZoneParkingBehavior(null, null, new Polygon[0], null, null, null, null, null, 0.0)).GetType()))
					{
						if (ps.ParkingSpot.Checkpoint.CheckpointId.Equals(CoreCommon.Mission.MissionCheckpoints.Peek().CheckpointNumber))
						{						
							ArbiterOutput.Output("Reached Goal, cp: " + ps.ParkingSpot.Checkpoint.CheckpointId.ToString());
							CoreCommon.Mission.MissionCheckpoints.Dequeue();
						}

						// pull out of the space
						PullingOutState pos = new PullingOutState(ps.Zone, ps.ParkingSpot);
						return new Maneuver(new HoldBrakeBehavior(), pos, TurnDecorators.NoDecorators, vehicleState.Timestamp);
					}
					else
					{
						// plan
						Maneuver final = tactical.Plan(CoreCommon.CorePlanningState, null, vehicleState, observedVehicles, observedObstacles, blockages, vehicleSpeed);

						// return final maneuver
						return final;
					}
				}

				#endregion

				#region Pulling Out State

				else if (CoreCommon.CorePlanningState is PullingOutState)
				{
					// set state
					PullingOutState pos = (PullingOutState)CoreCommon.CorePlanningState;

					// plan over state and zone
					ZonePlan zp = this.navigation.PlanZone(pos.Zone, pos.ParkingSpot.Checkpoint, goal);

					// check to see if we're stopped
					if (CoreCommon.Communications.HasCompleted((new ZoneParkingPullOutBehavior(null, null, new Polygon[0], null, null, null, null, null, null, null, null)).GetType()))
					{
						// maneuver to next place to go
						return new Maneuver(new HoldBrakeBehavior(), new ZoneTravelingState(pos.Zone, pos.ParkingSpot.Checkpoint), TurnDecorators.NoDecorators, vehicleState.Timestamp);
					}
					else
					{
						// plan
						Maneuver final = tactical.Plan(CoreCommon.CorePlanningState, zp, vehicleState, observedVehicles, observedObstacles, blockages, vehicleSpeed);

						// return final maneuver
						return final;
					}
				}

				#endregion

				#region Zone Startup State

				else if (CoreCommon.CorePlanningState is ZoneStartupState)
				{
					// feed through the plan from the zone tactical
					Maneuver final = tactical.Plan(CoreCommon.CorePlanningState, null, vehicleState, observedVehicles, observedObstacles, blockages, vehicleSpeed);

					// return final maneuver
					return final;
				}

				#endregion

				#region Zone Orientation

				else if (CoreCommon.CorePlanningState is ZoneOrientationState)
				{
					ZoneOrientationState zos = (ZoneOrientationState)CoreCommon.CorePlanningState;

					// add bounds to observable
					LinePath lp = new LinePath(new Coordinates[] { zos.final.Start.Position, zos.final.End.Position });
					CoreCommon.CurrentInformation.DisplayObjects.Add(new ArbiterInformationDisplayObject(lp.ShiftLateral(TahoeParams.T), ArbiterInformationDisplayObjectType.leftBound));
					CoreCommon.CurrentInformation.DisplayObjects.Add(new ArbiterInformationDisplayObject(lp.ShiftLateral(-TahoeParams.T), ArbiterInformationDisplayObjectType.rightBound));

					// check to see if we're stopped
					//if (CoreCommon.Communications.HasCompleted((new UTurnBehavior(null, null, null, null)).GetType()))
					//{
						// maneuver to next place to go
						return new Maneuver(new HoldBrakeBehavior(), new ZoneTravelingState(zos.Zone, zos.final.End), TurnDecorators.NoDecorators, vehicleState.Timestamp);
					//}
					// not stopped doing hte maneuver
					//else
					//	return new Maneuver(zos.Resume(vehicleState, 1.4), zos, TurnDecorators.NoDecorators, vehicleState.Timestamp);
				}

				#endregion

				#region Unknown

				else
				{
					// non-handled state
					throw new ArgumentException("Unknown state", "CoreCommon.CorePlanningState");
				}

				#endregion
			}

			#endregion

			#region Other State

			else if (CoreCommon.CorePlanningState is OtherState)
			{
				#region Start Up State

				if (CoreCommon.CorePlanningState is StartUpState)
				{
					// get state
					StartUpState sus = (StartUpState)CoreCommon.CorePlanningState;

					// make a new startup agent
					StartupReasoning sr = new StartupReasoning(this.laneAgent);

					// get final state
					IState nextState = sr.Startup(vehicleState, carMode);

					// return no op ad zero all decorators
					Behavior nextBehavior = sus.Resume(vehicleState, vehicleSpeed);

					// return maneuver
					return new Maneuver(nextBehavior, nextState, TurnDecorators.NoDecorators, vehicleState.Timestamp);					
				}

				#endregion

				#region Paused State

				else if (CoreCommon.CorePlanningState is PausedState)
				{
					// if switch back to run
					if (carMode == CarMode.Run)
					{
						// get state
						PausedState ps = (PausedState)CoreCommon.CorePlanningState;

						// get what we were previously doing
						IState previousState = ps.PreviousState();

						// check if can resume
						if (previousState != null && previousState.CanResume())
						{
							// resume state is next
							return new Maneuver(new HoldBrakeBehavior(), new ResumeState(previousState), TurnDecorators.NoDecorators, vehicleState.Timestamp);
						}
						// otherwise go to startup
						else
						{
							// next state is startup
							IState nextState = new StartUpState();

							// return no op
							Behavior nextBehavior = new HoldBrakeBehavior();

							// return maneuver
							return new Maneuver(nextBehavior, nextState, TurnDecorators.NoDecorators, vehicleState.Timestamp);
						}
					}
					// otherwise stay stopped
					else
					{
						// stay stopped and paused
						return new Maneuver(new HoldBrakeBehavior(), CoreCommon.CorePlanningState, TurnDecorators.NoDecorators, vehicleState.Timestamp);
					}
				}

				#endregion

				#region Human State

				else if (CoreCommon.CorePlanningState is HumanState)
				{
					// change to startup
					if (carMode == CarMode.Run)
					{
						// next is startup
						IState next = new StartUpState();

						// next behavior just stay iin place for now
						Behavior behavior = new HoldBrakeBehavior();

						// return startup maneuver
						return new Maneuver(behavior, next, TurnDecorators.NoDecorators, vehicleState.Timestamp);
					}					
					// in human mode still
					else
					{
						// want to remove old behavior stuff
						return new Maneuver(new NullBehavior(), CoreCommon.CorePlanningState, TurnDecorators.NoDecorators, vehicleState.Timestamp);
					}
				}

				#endregion

				#region Resume State

				else if (CoreCommon.CorePlanningState is ResumeState)
				{
					// get state
					ResumeState rs = (ResumeState)CoreCommon.CorePlanningState;

					// make sure can resume (this is simple action)
					if (rs.StateToResume != null && rs.StateToResume.CanResume())
					{
						// return old behavior
						Behavior nextBehavior = rs.Resume(vehicleState, vehicleSpeed);

						// return maneuver
						return new Maneuver(nextBehavior, rs.StateToResume, TurnDecorators.NoDecorators, vehicleState.Timestamp);
					}
					// otherwise just startup
					else
					{
						// startup
						return new Maneuver(new HoldBrakeBehavior(), new StartUpState(), TurnDecorators.NoDecorators, vehicleState.Timestamp);
					}
				}

				#endregion

				#region No Goals Left State

				else if (CoreCommon.CorePlanningState is NoGoalsLeftState)
				{
					// check if goals available
					if (CoreCommon.Mission.MissionCheckpoints.Count > 0)
					{
						// startup
						return new Maneuver(new HoldBrakeBehavior(), new StartUpState(), TurnDecorators.NoDecorators, vehicleState.Timestamp);
					}
					else
					{
						// stay paused
						return new Maneuver(new HoldBrakeBehavior(), CoreCommon.CorePlanningState, TurnDecorators.NoDecorators, vehicleState.Timestamp);
					}
				}

				#endregion

				#region eStopped State

				else if (CoreCommon.CorePlanningState is eStoppedState)
				{
					// change to startup
					if (carMode == CarMode.Run)
					{
						// next is startup
						IState next = new StartUpState();

						// next behavior just stay iin place for now
						Behavior behavior = new HoldBrakeBehavior();

						// return startup maneuver
						return new Maneuver(behavior, next, TurnDecorators.NoDecorators, vehicleState.Timestamp);
					}
					// in human mode still
					else
					{
						// want to remove old behavior stuff
						return new Maneuver(new NullBehavior(), CoreCommon.CorePlanningState, TurnDecorators.NoDecorators, vehicleState.Timestamp);
					}
				}

				#endregion

				#region Unknown

				else
				{
					// non-handled state
					throw new ArgumentException("Unknown OtherState type", "CoreCommon.CorePlanningState");
				}

				#endregion
			}

			#endregion

			#region Blockage State

			else if (CoreCommon.CorePlanningState is BlockageState)
			{
				#region Blockage State

				// something is blocked, in the encountered state we want to filter to base components of state
				if (CoreCommon.CorePlanningState is EncounteredBlockageState)
				{
					// cast blockage state
					EncounteredBlockageState bs = (EncounteredBlockageState)CoreCommon.CorePlanningState;

					// plan through the blockage state with no road plan as just a quick filter
					Maneuver final = tactical.Plan(bs, null, vehicleState, observedVehicles, observedObstacles, blockages, vehicleSpeed);

					// return the final maneuver
					return final;
				}

				#endregion

				#region Blockage Recovery State

				// recover from blockages
				else if (CoreCommon.CorePlanningState is BlockageRecoveryState)
				{
					// get the blockage recovery state
					BlockageRecoveryState brs = (BlockageRecoveryState)CoreCommon.CorePlanningState;

					#region Check Various Statuses of Completion

					// check successful completion report of behavior
					if (brs.RecoveryBehavior != null && CoreCommon.Communications.HasCompleted(brs.RecoveryBehavior.GetType()))
					{
						// set updated status
						ArbiterOutput.Output("Successfully received completion of behavior: " + brs.RecoveryBehavior.ToShortString() + ", " + brs.RecoveryBehavior.ShortBehaviorInformation());
						brs.RecoveryStatus = BlockageRecoverySTATUS.COMPLETED;

						// move to the tactical plan
						return this.tactical.Plan(brs, null, vehicleState, observedVehicles, observedObstacles, blockages, vehicleSpeed);
					}
					// check operational startup
					else if (CoreCommon.Communications.HasCompleted((new OperationalStartupBehavior()).GetType()))
					{
						// check defcon types
						if (brs.Defcon == BlockageRecoveryDEFCON.REVERSE)
						{
							// abort maneuver as operational has no state information
							return new Maneuver(new NullBehavior(), brs.AbortState, TurnDecorators.NoDecorators, vehicleState.Timestamp);
						}
					}

					#endregion

					#region Information

					// set recovery information					
					CoreCommon.CurrentInformation.FQMState = brs.EncounteredState.ShortDescription();
					CoreCommon.CurrentInformation.FQMStateInfo = brs.EncounteredState.StateInformation();
					CoreCommon.CurrentInformation.FQMBehavior = brs.RecoveryBehavior != null ? brs.RecoveryBehavior.ToShortString() : "NONE";
					CoreCommon.CurrentInformation.FQMBehaviorInfo = brs.RecoveryBehavior != null ? brs.RecoveryBehavior.ShortBehaviorInformation() : "NONE";
					CoreCommon.CurrentInformation.FQMSpeed = brs.RecoveryBehavior != null ? brs.RecoveryBehavior.SpeedCommandString() : "NONE";

					#endregion

					#region Blocked

					if (brs.RecoveryStatus == BlockageRecoverySTATUS.BLOCKED)
					{
						if (brs.RecoveryBehavior is ChangeLaneBehavior)
						{
							brs.RecoveryStatus = BlockageRecoverySTATUS.ENCOUNTERED;
							brs.Defcon = BlockageRecoveryDEFCON.CHANGELANES_FORWARD;
							return new Maneuver(new HoldBrakeBehavior(), brs, TurnDecorators.NoDecorators, vehicleState.Timestamp);
						}
						else
						{
							ArbiterOutput.Output("Recovery behavior blocked, reverting to abort state: " + brs.AbortState.ToString());
							return new Maneuver(new HoldBrakeBehavior(), brs.AbortState, TurnDecorators.NoDecorators, vehicleState.Timestamp);
						}
					}

					#endregion

					#region Navigational Plan

					// set navigational plan
					INavigationalPlan navPlan = null;
					
					#region Encountered

					// blockage
					if (brs.RecoveryStatus == BlockageRecoverySTATUS.ENCOUNTERED)
					{
						// get state
						if (brs.AbortState is StayInLaneState)
						{
							// lane state
							StayInLaneState sils = (StayInLaneState)brs.AbortState;
							navPlan = navigation.PlanNavigableArea(sils.Lane, vehicleState.Position, goal, sils.WaypointsToIgnore);
						}
					}

					#endregion

					#region Completion

					// blockage
					if (brs.RecoveryStatus == BlockageRecoverySTATUS.COMPLETED)
					{
						// get state
						if (brs.CompletionState is StayInLaneState)
						{
							// lane state
							StayInLaneState sils = (StayInLaneState)brs.CompletionState;
							navPlan = navigation.PlanNavigableArea(sils.Lane, vehicleState.Position, goal, sils.WaypointsToIgnore);
						}
					}

					#endregion

					#endregion

					// move to the tactical plan
					Maneuver final = this.tactical.Plan(brs, navPlan, vehicleState, observedVehicles, observedObstacles, blockages, vehicleSpeed);

					// return the final maneuver
					return final;
				}

				#endregion
			}

			#endregion

			#region Unknown

			else
			{
				// non-handled state
				throw new ArgumentException("Unknown state", "CoreCommon.CorePlanningState");
			}

			// for now, return null
			return new Maneuver();

			#endregion
		}

		/// <summary>
		/// Start behavioral threads
		/// </summary>
		public void Jumpstart()
		{
			try
			{
				// set blockage handler
				CoreCommon.Communications.ArbiterBlockageHandler = this.blockageHandler;

				// monitor blockages
				this.ResetTiming();
				this.navigation.JumpstartBlockageThread();
				this.navigation.currentTimes = new KeyValuePair<int, Dictionary<ArbiterWaypointId, DownstreamPointOfInterest>>();
			}
			catch (Exception e)
			{
				ArbiterOutput.Output("Exception found on behavioral jumpstart: " + e.ToString());
			}
		}

		/// <summary>
		/// Reset timings
		/// </summary>
		public void ResetTiming()
		{
			// reset the blockage handler
			this.blockageHandler.Reset();

			// reset the road tactical
			if (this.tactical.roadTactical != null)
				this.tactical.roadTactical.ResetAll();
		}

		#endregion
	}
}
