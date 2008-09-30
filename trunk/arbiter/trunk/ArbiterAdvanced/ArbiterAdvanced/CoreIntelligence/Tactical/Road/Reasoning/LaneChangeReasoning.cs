using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.Core.Common;
using UrbanChallenge.Arbiter.Core.Common.State;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road.Reasoning;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road.Opposing.Reasoning;
using UrbanChallenge.Arbiter.Core.Common.Common;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Navigation;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Vehicles;
using UrbanChallenge.Common;
using UrbanChallenge.Arbiter.Core.Communications;
using UrbanChallenge.Common.Sensors;
using UrbanChallenge.Behaviors.CompletionReport;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road
{
	/// <summary>
	/// Reasons about making lane changes
	/// </summary>
	public class LaneChangeReasoning
	{
		/// <summary>
		/// Plan a lane change
		/// </summary>
		/// <param name="cls"></param>
		/// <param name="initialManeuver"></param>
		/// <param name="targetManeuver"></param>
		/// <returns></returns>
		public Maneuver PlanLaneChange(ChangeLanesState cls, VehicleState vehicleState, RoadPlan roadPlan, 
			List<ITacticalBlockage> blockages, List<ArbiterWaypoint> ignorable)
		{
			// check blockages
			if (blockages != null && blockages.Count > 0 && blockages[0] is LaneChangeBlockage)
			{
				// create the blockage state
				EncounteredBlockageState ebs = new EncounteredBlockageState(blockages[0], CoreCommon.CorePlanningState);

				// go to a blockage handling tactical
				return new Maneuver(new NullBehavior(), ebs, TurnDecorators.NoDecorators, vehicleState.Timestamp);
			}

			// lanes of the lane change
			ArbiterLane initial = cls.Parameters.Initial;
			ArbiterLane target = cls.Parameters.Target;

			#region Initial Forwards

			if (!cls.Parameters.InitialOncoming)
			{
				ForwardReasoning initialReasoning = new ForwardReasoning(new LateralReasoning(null, SideObstacleSide.Driver), new LateralReasoning(null, SideObstacleSide.Driver), initial);

				#region Target Forwards

				if (!cls.Parameters.TargetOncoming)
				{
					// target reasoning
					ForwardReasoning targetReasoning = new ForwardReasoning(new LateralReasoning(null, SideObstacleSide.Driver), new LateralReasoning(null, SideObstacleSide.Driver), target);

					#region Navigation

					if (cls.Parameters.Reason == LaneChangeReason.Navigation)
					{
						// parameters to follow
						List<TravelingParameters> tps = new List<TravelingParameters>();

						// vehicles to ignore
						List<int> ignorableVehicles = new List<int>();

						// params for forward lane
						initialReasoning.ForwardManeuver(initial, vehicleState, roadPlan, blockages, ignorable);
						TravelingParameters initialParams = initialReasoning.ForwardMonitor.ParameterizationHelper(initial, initial,
							CoreCommon.Mission.MissionCheckpoints.Peek().WaypointId.Equals(roadPlan.BestPlan.laneWaypointOfInterest.PointOfInterest.WaypointId) ?
							initial.WaypointList[initial.WaypointList.Count - 1].Position : roadPlan.BestPlan.laneWaypointOfInterest.PointOfInterest.Position, 
							vehicleState.Front, CoreCommon.CorePlanningState, vehicleState, initialReasoning.ForwardMonitor.ForwardVehicle.CurrentVehicle);

						ArbiterOutput.Output("initial dist to go: " + initialParams.DistanceToGo.ToString("f3"));

						if (initialParams.Type == TravellingType.Vehicle && !initialReasoning.ForwardMonitor.ForwardVehicle.CurrentVehicle.IsStopped)
						{
							tps.Add(initialParams);
						}
						else
							tps.Add(initialReasoning.ForwardMonitor.NavigationParameters);

						ignorableVehicles.AddRange(initialParams.VehiclesToIgnore);

						// get params for the final lane
						targetReasoning.ForwardManeuver(target, vehicleState, roadPlan, blockages, new List<ArbiterWaypoint>());
						TravelingParameters targetParams = targetReasoning.ForwardMonitor.CurrentParameters;
						tps.Add(targetParams);
						ignorableVehicles.AddRange(targetParams.VehiclesToIgnore);

						try
						{
							if (CoreCommon.Communications.GetVehicleSpeed().Value < 0.1 &&
								targetParams.Type == TravellingType.Vehicle &&
								targetReasoning.ForwardMonitor.ForwardVehicle.CurrentVehicle != null &&
								targetReasoning.ForwardMonitor.ForwardVehicle.CurrentVehicle.QueuingState.Queuing == QueuingState.Failed)
							{
								return new Maneuver(new HoldBrakeBehavior(), new StayInLaneState(target, CoreCommon.CorePlanningState), TurnDecorators.NoDecorators, vehicleState.Timestamp);
							}
						}
						catch (Exception) { }

						ArbiterOutput.Output("target dist to go: " + targetParams.DistanceToGo.ToString("f3"));

						// decorators
						List<BehaviorDecorator> decorators = initial.LaneOnLeft != null && initial.LaneOnLeft.Equals(target) ? TurnDecorators.LeftTurnDecorator : TurnDecorators.RightTurnDecorator;

						// distance
						double distanceToGo = initial.DistanceBetween(vehicleState.Front, cls.Parameters.DepartUpperBound);
						cls.Parameters.DistanceToDepartUpperBound = distanceToGo;

						// check if need to modify distance to go
						if (initialParams.Type == TravellingType.Vehicle && initialReasoning.ForwardMonitor.ForwardVehicle.CurrentVehicle.IsStopped)
						{
							double curDistToUpper = cls.Parameters.DistanceToDepartUpperBound;
							double newVhcDistToUpper = initial.DistanceBetween(vehicleState.Front, initialReasoning.ForwardMonitor.ForwardVehicle.CurrentVehicle.ClosestPosition) - 2.0;

							if (curDistToUpper > newVhcDistToUpper)
							{
								distanceToGo = newVhcDistToUpper;
							}
						}

						// get final
						tps.Sort();

						// get the proper speed command
						ScalarSpeedCommand sc = new ScalarSpeedCommand(tps[0].RecommendedSpeed);
						if (sc.Speed < 8.84)
							sc = new ScalarSpeedCommand(Math.Min(targetParams.RecommendedSpeed, 8.84));

						// continue the lane change with the proper speed command
						ChangeLaneBehavior clb = new ChangeLaneBehavior(initial.LaneId, target.LaneId, initial.LaneOnLeft != null && initial.LaneOnLeft.Equals(target), distanceToGo,
							sc, targetParams.VehiclesToIgnore, initial.LanePath(), target.LanePath(), initial.Width, target.Width, initial.NumberOfLanesLeft(vehicleState.Front, true),
							initial.NumberOfLanesRight(vehicleState.Front, true));

						// standard maneuver
						return new Maneuver(clb, CoreCommon.CorePlanningState, decorators, vehicleState.Timestamp);
					}

					#endregion

					#region Failed Forwards

					else if (cls.Parameters.Reason == LaneChangeReason.FailedForwardVehicle)
					{
						// parameters to follow
						List<TravelingParameters> tps = new List<TravelingParameters>();

						// vehicles to ignore
						List<int> ignorableVehicles = new List<int>();

						// params for forward lane
						initialReasoning.ForwardManeuver(initial, vehicleState, roadPlan, blockages, ignorable);
						TravelingParameters initialParams = initialReasoning.ForwardMonitor.ParameterizationHelper(initial, initial,
							CoreCommon.Mission.MissionCheckpoints.Peek().WaypointId.Equals(roadPlan.BestPlan.laneWaypointOfInterest.PointOfInterest.WaypointId) ?
							initial.WaypointList[initial.WaypointList.Count - 1].Position : roadPlan.BestPlan.laneWaypointOfInterest.PointOfInterest.Position,
							vehicleState.Front, CoreCommon.CorePlanningState, vehicleState, null);
						tps.Add(initialParams);
						ignorableVehicles.AddRange(initialParams.VehiclesToIgnore);

						// get params for the final lane
						targetReasoning.ForwardManeuver(target, vehicleState, roadPlan, blockages, new List<ArbiterWaypoint>());
						TravelingParameters targetParams = targetReasoning.ForwardMonitor.CurrentParameters;
						tps.Add(targetParams);
						ignorableVehicles.AddRange(targetParams.VehiclesToIgnore);

						// decorators
						List<BehaviorDecorator> decorators = initial.LaneOnLeft != null && initial.LaneOnLeft.Equals(target) ? TurnDecorators.LeftTurnDecorator : TurnDecorators.RightTurnDecorator;

						// distance
						double distanceToGo = initial.DistanceBetween(vehicleState.Front, cls.Parameters.DepartUpperBound);
						cls.Parameters.DistanceToDepartUpperBound = distanceToGo;

						// get final
						tps.Sort();

						// get the proper speed command
						SpeedCommand sc = new ScalarSpeedCommand(tps[0].RecommendedSpeed);

						// continue the lane change with the proper speed command
						ChangeLaneBehavior clb = new ChangeLaneBehavior(initial.LaneId, target.LaneId, initial.LaneOnLeft != null && initial.LaneOnLeft.Equals(target), distanceToGo,
							sc, targetParams.VehiclesToIgnore, initial.LanePath(), target.LanePath(), initial.Width, target.Width, initial.NumberOfLanesLeft(vehicleState.Front, true),
							initial.NumberOfLanesRight(vehicleState.Front, true));

						// standard maneuver
						return new Maneuver(clb, CoreCommon.CorePlanningState, decorators, vehicleState.Timestamp);
					}

					#endregion

					#region Slow

					else if (cls.Parameters.Reason == LaneChangeReason.SlowForwardVehicle)
					{
						// fallout exception
						throw new Exception("currently unsupported lane change type");
					}

					#endregion

					else
					{
						// fallout exception
						throw new Exception("currently unsupported lane change type");
					}
				}

				#endregion

				#region Target Oncoming

				else
				{
					OpposingReasoning targetReasoning = new OpposingReasoning(new OpposingLateralReasoning(null, SideObstacleSide.Driver), new OpposingLateralReasoning(null, SideObstacleSide.Driver), target);

					#region Failed Forward

					if (cls.Parameters.Reason == LaneChangeReason.FailedForwardVehicle)
					{
						// parameters to follow
						List<TravelingParameters> tps = new List<TravelingParameters>();

						// ignore the forward vehicle but keep params for forward lane
						initialReasoning.ForwardManeuver(initial, vehicleState, roadPlan, blockages, ignorable);
						TravelingParameters initialParams = initialReasoning.ForwardMonitor.ParameterizationHelper(initial, initial,
							CoreCommon.Mission.MissionCheckpoints.Peek().WaypointId.Equals(roadPlan.BestPlan.laneWaypointOfInterest.PointOfInterest.WaypointId) ?
							initial.WaypointList[initial.WaypointList.Count-1].Position : roadPlan.BestPlan.laneWaypointOfInterest.PointOfInterest.Position, vehicleState.Front, CoreCommon.CorePlanningState, vehicleState, null);
						tps.Add(initialParams);

						// get params for the final lane
						targetReasoning.ForwardManeuver(target, initial, vehicleState, roadPlan, blockages);
						TravelingParameters targetParams = targetReasoning.OpposingForwardMonitor.CurrentParamters.Value;
						tps.Add(targetParams);

						// decorators
						List<BehaviorDecorator> decorators = cls.Parameters.ToLeft ? TurnDecorators.LeftTurnDecorator : TurnDecorators.RightTurnDecorator;

						// distance
						double distanceToGo = initial.DistanceBetween(vehicleState.Front, cls.Parameters.DepartUpperBound);
						cls.Parameters.DistanceToDepartUpperBound = distanceToGo;

						// get final
						tps.Sort();

						// get the proper speed command
						SpeedCommand sc = new ScalarSpeedCommand(Math.Min(tps[0].RecommendedSpeed, 2.24));

						// check final for stopped failed opposing
						VehicleAgent forwardVa = targetReasoning.OpposingForwardMonitor.ForwardVehicle.CurrentVehicle;
						if (forwardVa != null)
						{
							// dist between
							double distToFV = -targetReasoning.Lane.DistanceBetween(vehicleState.Front, forwardVa.ClosestPosition);

							// check stopped
							bool stopped = Math.Abs(CoreCommon.Communications.GetVehicleSpeed().Value) < 0.5;

							// check distance
							bool distOk = distToFV < 2.5 * TahoeParams.VL;

							// check failed
							bool failed = forwardVa.QueuingState.Queuing == QueuingState.Failed;

							// notify
							ArbiterOutput.Output("Forward Vehicle: Stopped: " + stopped.ToString() + ", DistOk: " + distOk.ToString() + ", Failed: " + failed.ToString());

							// check all for failed
							if (stopped && distOk && failed)
							{								
								// check inside target
								if (target.LanePolygon.IsInside(vehicleState.Front))
								{
									// blockage recovery
									StayInLaneState sils = new StayInLaneState(initial, CoreCommon.CorePlanningState);
									StayInLaneBehavior silb = new StayInLaneBehavior(initial.LaneId, new StopAtDistSpeedCommand(TahoeParams.VL * 2.0, true), new List<int>(), initial.LanePath(), initial.Width, initial.NumberOfLanesLeft(vehicleState.Front, false), initial.NumberOfLanesRight(vehicleState.Front, false));
									BlockageRecoveryState brs = new BlockageRecoveryState(silb, sils, sils, BlockageRecoveryDEFCON.REVERSE, 
										new EncounteredBlockageState(new LaneBlockage(new TrajectoryBlockedReport(CompletionResult.Stopped, 4.0, BlockageType.Static, -1, true, silb.GetType())), sils, BlockageRecoveryDEFCON.INITIAL, SAUDILevel.None), 
										BlockageRecoverySTATUS.EXECUTING);
									return new Maneuver(silb, brs, TurnDecorators.HazardDecorator, vehicleState.Timestamp);
								}
								// check which lane we are in
								else
								{
									// return to forward lane
									return new Maneuver(new HoldBrakeBehavior(), new StayInLaneState(initial, CoreCommon.CorePlanningState), TurnDecorators.NoDecorators, vehicleState.Timestamp);
								}
							}
						}

						// continue the lane change with the proper speed command
						ChangeLaneBehavior clb = new ChangeLaneBehavior(initial.LaneId, target.LaneId, cls.Parameters.ToLeft, distanceToGo,
							sc, targetParams.VehiclesToIgnore, initial.LanePath(), target.ReversePath, initial.Width, target.Width, initial.NumberOfLanesLeft(vehicleState.Front, true),
							initial.NumberOfLanesRight(vehicleState.Front, true));
						
						// standard maneuver
						return new Maneuver(clb, CoreCommon.CorePlanningState, decorators, vehicleState.Timestamp);
					}

					#endregion

					#region Other

					else if (cls.Parameters.Reason == LaneChangeReason.Navigation)
					{
						// fallout exception
						throw new Exception("currently unsupported lane change type");
					}
					else if (cls.Parameters.Reason == LaneChangeReason.SlowForwardVehicle)
					{
						// fallout exception
						throw new Exception("currently unsupported lane change type");
					}
					else
					{
						// fallout exception
						throw new Exception("currently unsupported lane change type");
					}

					#endregion
				}

				#endregion
			}

			#endregion

			#region Initial Oncoming

			else
			{
				OpposingReasoning initialReasoning = new OpposingReasoning(new OpposingLateralReasoning(null, SideObstacleSide.Driver), new OpposingLateralReasoning(null, SideObstacleSide.Driver), initial);

				#region Target Forwards

				if (!cls.Parameters.TargetOncoming)
				{
					ForwardReasoning targetReasoning = new ForwardReasoning(new LateralReasoning(null, SideObstacleSide.Driver), new LateralReasoning(null, SideObstacleSide.Driver), target);

					if (cls.Parameters.Reason == LaneChangeReason.FailedForwardVehicle)
					{
						// fallout exception
						throw new Exception("currently unsupported lane change type");
					}

					#region Navigation

					else if (cls.Parameters.Reason == LaneChangeReason.Navigation)
					{
						// parameters to follow
						List<TravelingParameters> tps = new List<TravelingParameters>();

						// distance to the upper bound of the change
						double distanceToGo = target.DistanceBetween(vehicleState.Front, cls.Parameters.DepartUpperBound);
						cls.Parameters.DistanceToDepartUpperBound = distanceToGo;

						// get params for the initial lane
						initialReasoning.ForwardManeuver(initial, target, vehicleState, roadPlan, blockages);

						// current params of the fqm
						TravelingParameters initialParams = initialReasoning.OpposingForwardMonitor.CurrentParamters.Value;

						if (initialParams.Type == TravellingType.Vehicle)
						{
							if(!initialReasoning.OpposingForwardMonitor.ForwardVehicle.CurrentVehicle.IsStopped)
								tps.Add(initialParams);
							else
							{
								tps.Add(initialReasoning.OpposingForwardMonitor.NaviationParameters);
								distanceToGo = initial.DistanceBetween(initialReasoning.OpposingForwardMonitor.ForwardVehicle.CurrentVehicle.ClosestPosition, vehicleState.Front) - TahoeParams.VL;
							}
						}
						else
							tps.Add(initialReasoning.OpposingForwardMonitor.NaviationParameters);

						// get params for forward lane
						targetReasoning.ForwardManeuver(target, vehicleState, roadPlan, blockages, ignorable);
						TravelingParameters targetParams = targetReasoning.ForwardMonitor.ParameterizationHelper(target, target,
							CoreCommon.Mission.MissionCheckpoints.Peek().WaypointId.Equals(roadPlan.BestPlan.laneWaypointOfInterest.PointOfInterest.WaypointId) ?
							target.WaypointList[target.WaypointList.Count-1].Position : roadPlan.BestPlan.laneWaypointOfInterest.PointOfInterest.Position,
							vehicleState.Front, CoreCommon.CorePlanningState, vehicleState, targetReasoning.ForwardMonitor.ForwardVehicle.CurrentVehicle);
						tps.Add(targetParams);

						// ignoring vehicles add
						List<int> ignoreVehicles = initialParams.VehiclesToIgnore;
						ignoreVehicles.AddRange(targetParams.VehiclesToIgnore);

						// decorators
						List<BehaviorDecorator> decorators = !cls.Parameters.ToLeft ? TurnDecorators.RightTurnDecorator : TurnDecorators.LeftTurnDecorator;

						// get final
						tps.Sort();

						// get the proper speed command
						SpeedCommand sc = tps[0].SpeedCommand;

						if (sc is StopAtDistSpeedCommand)
						{
							sc = new ScalarSpeedCommand(0.0);
						}

						// check final for stopped failed opposing
						VehicleAgent forwardVa = targetReasoning.ForwardMonitor.ForwardVehicle.CurrentVehicle;
						if (forwardVa != null)
						{
							// dist between
							double distToFV = targetReasoning.Lane.DistanceBetween(vehicleState.Front, forwardVa.ClosestPosition);

							// check stopped
							bool stopped = Math.Abs(CoreCommon.Communications.GetVehicleSpeed().Value) < 0.5;

							// check distance
							bool distOk = distToFV < 2.5 * TahoeParams.VL;

							// check failed
							bool failed = forwardVa.QueuingState.Queuing == QueuingState.Failed;

							// notify
							ArbiterOutput.Output("Forward Vehicle: Stopped: " + stopped.ToString() + ", DistOk: " + distOk.ToString() + ", Failed: " + failed.ToString());

							// check all for failed
							if (stopped && distOk && failed)
							{	
								// check which lane we are in
								if (initial.LanePolygon.IsInside(vehicleState.Front))
								{
									// return to opposing lane
									return new Maneuver(new HoldBrakeBehavior(), new OpposingLanesState(initial, true, CoreCommon.CorePlanningState, vehicleState), TurnDecorators.NoDecorators, vehicleState.Timestamp);
								}
								else
								{
									// lane state
									return new Maneuver(new HoldBrakeBehavior(), new StayInLaneState(target, CoreCommon.CorePlanningState), TurnDecorators.NoDecorators, vehicleState.Timestamp);
								}
							}
						}

						// continue the lane change with the proper speed command
						ChangeLaneBehavior clb = new ChangeLaneBehavior(initial.LaneId, target.LaneId, cls.Parameters.ToLeft, distanceToGo,
							sc, ignoreVehicles, initial.ReversePath, target.LanePath(), initial.Width, target.Width, initial.NumberOfLanesLeft(vehicleState.Front, false),
							initial.NumberOfLanesRight(vehicleState.Front, false));

						// standard maneuver
						return new Maneuver(clb, CoreCommon.CorePlanningState, decorators, vehicleState.Timestamp);
					}

					#endregion

					else if (cls.Parameters.Reason == LaneChangeReason.SlowForwardVehicle)
					{
						// fallout exception
						throw new Exception("currently unsupported lane change type");
					}
					else
					{
						// fallout exception
						throw new Exception("currently unsupported lane change type");
					}
				}

				#endregion

				else
				{
					// fallout exception
					throw new Exception("currently unsupported lane change type");
				}
			}

			#endregion
		}

		/// <summary>
		/// Resets values held over time
		/// </summary>
		public void Reset()
		{
		}
	}
}
