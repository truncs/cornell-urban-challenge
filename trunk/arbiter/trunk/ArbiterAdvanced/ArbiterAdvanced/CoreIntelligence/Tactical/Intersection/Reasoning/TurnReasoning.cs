using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Arbiter.Core.Common;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Navigation;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Arbiter.Core.Common.State;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Intersection.Monitors;
using UrbanChallenge.Arbiter.Core.Common.Tools;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Tools;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Vehicles;
using UrbanChallenge.Arbiter.Core.Communications;
using UrbanChallenge.Behaviors.CompletionReport;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Intersection.Reasoning
{
	/// <summary>
	/// Provides reasoning when in a turn
	/// </summary>
	public class TurnReasoning
	{
		/// <summary>
		/// turn we are reasoning about
		/// </summary>
		private ArbiterInterconnect turn;		

		/// <summary>
		/// Navigator for the turn
		/// </summary>
		private Navigator navigation;

		/// <summary>
		/// Forward monitor of the turn
		/// </summary>
		private TurnForwardMonitor forwardMonitor;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="turn"></param>
		public TurnReasoning(ArbiterInterconnect turn, IEntryAreaMonitor entryAreaMonitor)
		{
			this.turn = turn;
			this.navigation = new Navigator();
			this.forwardMonitor = new TurnForwardMonitor(turn, entryAreaMonitor);
		}

		/// <summary>
		/// Gets primary maneuver given our position and the turn we are traveling upon
		/// </summary>
		/// <param name="vehicleState"></param>
		/// <returns></returns>
		public Maneuver PrimaryManeuver(VehicleState vehicleState, List<ITacticalBlockage> blockages, TurnState turnState)
		{
			#region Check are planning over the correct turn

			if (CoreCommon.CorePlanningState is TurnState)
			{
				TurnState ts = (TurnState)CoreCommon.CorePlanningState;
				if (this.turn == null || !this.turn.Equals(ts.Interconnect))
				{
					this.turn = ts.Interconnect;
					this.forwardMonitor = new TurnForwardMonitor(ts.Interconnect, null);
				}
				else if (this.forwardMonitor.turn == null || !this.forwardMonitor.turn.Equals(ts.Interconnect))
				{
					this.forwardMonitor = new TurnForwardMonitor(ts.Interconnect, null);
				}
			}

			#endregion

			#region Blockages

			// check blockages
			if (blockages != null && blockages.Count > 0 && blockages[0] is TurnBlockage)
			{
				// create the blockage state
				EncounteredBlockageState ebs = new EncounteredBlockageState(blockages[0], CoreCommon.CorePlanningState);

				// check not at highest level already
				if (turnState.Saudi != SAUDILevel.L1 || turnState.UseTurnBounds)
				{
					// check not from a dynamicly moving vehicle
					if (blockages[0].BlockageReport.BlockageType != BlockageType.Dynamic ||
						(TacticalDirector.ValidVehicles.ContainsKey(blockages[0].BlockageReport.TrackID) &&
						TacticalDirector.ValidVehicles[blockages[0].BlockageReport.TrackID].IsStopped))
					{
						// go to a blockage handling tactical
						return new Maneuver(new NullBehavior(), ebs, TurnDecorators.NoDecorators, vehicleState.Timestamp);
					}
					else
						ArbiterOutput.Output("Turn blockage reported for moving vehicle, ignoring");
				}
				else
					ArbiterOutput.Output("Turn blockage, but recovery escalation already at highest state, ignoring report");
			}

			#endregion

			#region Intersection Check

			if (!this.CanGo(vehicleState))
			{
				if (turn.FinalGeneric is ArbiterWaypoint)
				{
					TravelingParameters tp = this.GetParameters(0.0, 0.0, (ArbiterWaypoint)turn.FinalGeneric, vehicleState, false);
					return new Maneuver(tp.Behavior, CoreCommon.CorePlanningState, tp.NextState.DefaultStateDecorators, vehicleState.Timestamp);
				}
				else
				{
					// get turn params
					LinePath finalPath;
					LineList leftLL;
					LineList rightLL;
					IntersectionToolkit.ZoneTurnInfo(this.turn, (ArbiterPerimeterWaypoint)this.turn.FinalGeneric, out finalPath, out leftLL, out rightLL);

					// hold brake
					IState nextState = new TurnState(this.turn, this.turn.TurnDirection, null, finalPath, leftLL, rightLL, new ScalarSpeedCommand(0.0));
					TurnBehavior b = new TurnBehavior(null, finalPath, leftLL, rightLL, new ScalarSpeedCommand(0.0), this.turn.InterconnectId);
					return new Maneuver(b, CoreCommon.CorePlanningState, nextState.DefaultStateDecorators, vehicleState.Timestamp);
				}
			}

			#endregion

			#region Final is Lane Waypoint

			if (turn.FinalGeneric is ArbiterWaypoint)
			{
				// final point
				ArbiterWaypoint final = (ArbiterWaypoint)turn.FinalGeneric;

				// plan down entry lane
				RoadPlan rp = navigation.PlanNavigableArea(final.Lane, final.Position, 
					CoreCommon.RoadNetwork.ArbiterWaypoints[CoreCommon.Mission.MissionCheckpoints.Peek().WaypointId], new List<ArbiterWaypoint>());

				// point of interest downstream
				DownstreamPointOfInterest dpoi = rp.BestPlan.laneWaypointOfInterest;

				// get path this represents
				List<Coordinates> pathCoordinates = new List<Coordinates>();
				pathCoordinates.Add(vehicleState.Position);
				foreach(ArbiterWaypoint aw in final.Lane.WaypointsInclusive(final, final.Lane.WaypointList[final.Lane.WaypointList.Count-1]))
					pathCoordinates.Add(aw.Position);
				LinePath lp = new LinePath(pathCoordinates);

				// list of all parameterizations
				List<TravelingParameters> parameterizations = new List<TravelingParameters>();

				// get lane navigation parameterization
				TravelingParameters navigationParameters = this.NavigationParameterization(vehicleState, dpoi, final, lp);
				parameterizations.Add(navigationParameters);

				// update forward tracker and get vehicle parameterizations if forward vehicle exists
				this.forwardMonitor.Update(vehicleState, final, lp);
				if (this.forwardMonitor.ShouldUseForwardTracker())
				{
					// get vehicle parameterization
					TravelingParameters vehicleParameters = this.VehicleParameterization(vehicleState, lp, final);
					parameterizations.Add(vehicleParameters);
				}

				// sort and return funal
				parameterizations.Sort();

				// get the final behavior
				TurnBehavior tb = (TurnBehavior)parameterizations[0].Behavior;

				// get vehicles to ignore
				tb.VehiclesToIgnore = this.forwardMonitor.VehiclesToIgnore;

				// add persistent information about saudi level
				if (turnState.Saudi == SAUDILevel.L1)
				{
					tb.Decorators = new List<BehaviorDecorator>(tb.Decorators.ToArray());
					tb.Decorators.Add(new ShutUpAndDoItDecorator(SAUDILevel.L1));
				}

				// add persistent information about turn bounds
				if (!turnState.UseTurnBounds)
				{
					tb.LeftBound = null;
					tb.RightBound = null;
				}

				//  return the behavior
				return new Maneuver(tb, CoreCommon.CorePlanningState, tb.Decorators, vehicleState.Timestamp);
			}

			#endregion

			#region Final is Zone Waypoint

			else if (turn.FinalGeneric is ArbiterPerimeterWaypoint)
			{
				// get inteconnect path
				Coordinates entryVec = ((ArbiterPerimeterWaypoint)turn.FinalGeneric).Perimeter.PerimeterPolygon.BoundingCircle.center - 
					turn.FinalGeneric.Position;
				entryVec = entryVec.Normalize(TahoeParams.VL / 2.0);
				LinePath ip = new LinePath(new Coordinates[] { turn.InitialGeneric.Position, turn.FinalGeneric.Position, entryVec + this.turn.FinalGeneric.Position });

				// get distance from end
				double d = ip.DistanceBetween(
					ip.GetClosestPoint(vehicleState.Front),
					ip.EndPoint);

				// get speed command
				SpeedCommand sc = null;
				if (d < TahoeParams.VL)
					sc = new StopAtDistSpeedCommand(d);
				else
					sc = new ScalarSpeedCommand(SpeedTools.GenerateSpeed(d - TahoeParams.VL, 1.7, turn.MaximumDefaultSpeed));

				// final perimeter waypoint
				ArbiterPerimeterWaypoint apw = (ArbiterPerimeterWaypoint)this.turn.FinalGeneric;

				// get turn params
				LinePath finalPath;
				LineList leftLL;
				LineList rightLL;
				IntersectionToolkit.ZoneTurnInfo(this.turn, (ArbiterPerimeterWaypoint)this.turn.FinalGeneric, out finalPath, out leftLL, out rightLL);

				// hold brake
				IState nextState = new TurnState(this.turn, this.turn.TurnDirection, null, finalPath, leftLL, rightLL, sc);
				TurnBehavior tb = new TurnBehavior(null, finalPath, leftLL, rightLL, sc, null, new List<int>(), this.turn.InterconnectId);

				// add persistent information about saudi level
				if (turnState.Saudi == SAUDILevel.L1)
				{
					tb.Decorators = new List<BehaviorDecorator>(tb.Decorators.ToArray());
					tb.Decorators.Add(new ShutUpAndDoItDecorator(SAUDILevel.L1));
				}

				// add persistent information about turn bounds
				if (!turnState.UseTurnBounds)
				{
					tb.LeftBound = null;
					tb.RightBound = null;
				}

				// return maneuver
				return new Maneuver(tb, CoreCommon.CorePlanningState, tb.Decorators, vehicleState.Timestamp);
			}

			#endregion

			#region Unknown

			else
			{
				throw new Exception("unrecognized type: " + turn.FinalGeneric.ToString());
			}

			#endregion
		}

		/// <summary>
		/// Check if we can go
		/// </summary>
		/// <param name="vs"></param>
		private bool CanGo(VehicleState vs)
		{
			#region Moving Vehicles Inside Turn

			// check if we can still go through this turn
			if (TacticalDirector.VehicleAreas.ContainsKey(this.turn))
			{
				// get the subpath of the interconnect we care about
				LinePath.PointOnPath frontPos = this.turn.InterconnectPath.GetClosestPoint(vs.Front);
				LinePath aiSubpath = this.turn.InterconnectPath.SubPath(frontPos, this.turn.InterconnectPath.EndPoint);

				if (aiSubpath.PathLength > 4.0)
				{
					aiSubpath = aiSubpath.SubPath(aiSubpath.StartPoint, aiSubpath.PathLength - 2.0);

					// get vehicles
					List<VehicleAgent> turnVehicles = TacticalDirector.VehicleAreas[this.turn];

					// loop vehicles
					foreach (VehicleAgent va in turnVehicles)
					{
						// check if inside turn
						LinePath.PointOnPath vaPop = aiSubpath.GetClosestPoint(va.ClosestPosition);
						if (!va.IsStopped && this.turn.TurnPolygon.IsInside(va.ClosestPosition) && !vaPop.Equals(aiSubpath.StartPoint) && !vaPop.Equals(aiSubpath.EndPoint))
						{
							ArbiterOutput.Output("Vehicle seen inside our turn: " + va.ToString() + ", stopping");
							return false;
						}
					}
				}
			}

			#endregion

			// test if this turn is part of an intersection
			if (CoreCommon.RoadNetwork.IntersectionLookup.ContainsKey(this.turn.InitialGeneric.AreaSubtypeWaypointId))
			{
				// intersection
				ArbiterIntersection inter = CoreCommon.RoadNetwork.IntersectionLookup[this.turn.InitialGeneric.AreaSubtypeWaypointId];

				// check if priority lanes exist for this interconnect
				if(inter.PriorityLanes.ContainsKey(this.turn))
				{
					// get all the default priority lanes
					List<IntersectionInvolved> priorities = inter.PriorityLanes[this.turn];

					// get the subpath of the interconnect we care about
					//LinePath.PointOnPath frontPos = this.turn.InterconnectPath.GetClosestPoint(vs.Front);
					LinePath aiSubpath = new LinePath(new List<Coordinates>(new Coordinates[] { vs.Front, this.turn.FinalGeneric.Position }));//this.turn.InterconnectPath.SubPath(frontPos, this.turn.InterconnectPath.EndPoint);

					// check if path ended
					if (aiSubpath.Count < 2)
						return true;

					// determine all of the new priority lanes
					List<IntersectionInvolved> updatedPriorities = new List<IntersectionInvolved>();

					#region Determine new priority areas given position

					// loop through old priorities
					foreach (IntersectionInvolved ii in priorities)
					{
						// check ii lane
						if(ii.Area is ArbiterLane)
						{
							#region Lane Intersects Turn Path w/ Point of No Return

							// check if the waypoint is not the last waypoint in the lane
							if (ii.Exit == null || ((ArbiterWaypoint)ii.Exit).NextPartition != null)
							{
								// check where line intersects path
								Coordinates? intersect = this.LaneIntersectsPath(ii, aiSubpath, this.turn.FinalGeneric);

								// check for an intersection
								if (intersect.HasValue)
								{
									// distance to intersection
									double distanceToIntersection = (intersect.Value.DistanceTo(vs.Front) + ((ArbiterLane)ii.Area).LanePath().GetClosestPoint(vs.Front).Location.DistanceTo(vs.Front)) / 2.0;

									// determine if we can stop before or after the intersection
									double distanceToStoppage = RoadToolkit.DistanceToStop(CoreCommon.Communications.GetVehicleSpeed().Value);

									// check dist to intersection > distance to stoppage
									if (distanceToIntersection > distanceToStoppage)
									{
										// add updated priority
										updatedPriorities.Add(new IntersectionInvolved(new ArbiterWaypoint(intersect.Value, new ArbiterWaypointId(0, ((ArbiterLane)ii.Area).LaneId)),
											ii.Area, ArbiterTurnDirection.Straight));
									}
									else
									{
										ArbiterOutput.Output("Passed point of No Return for Lane: " + ii.Area.ToString());
									}
								}
							}

							#endregion

							// we know there is an exit and it is the last waypoint of the segment, fuxk!
							else
							{
								#region Turns Intersect

								// get point to look at if exists
								ArbiterInterconnect interconnect;
								Coordinates? intersect = this.TurnIntersects(aiSubpath, ii.Exit, out interconnect);

								// check for the intersect
								if (intersect.HasValue)
								{
									ArbiterLane al = (ArbiterLane)ii.Area;
									LinePath lp = al.LanePath().SubPath(al.LanePath().StartPoint, al.LanePath().GetClosestPoint(ii.Exit.Position));
									lp.Add(interconnect.InterconnectPath.EndPoint.Location);

									// get our time to the intersection point
									//double ourTime = Math.Min(4.0, Math.Abs(CoreCommon.Communications.GetVehicleSpeed().Value) < 0.001 ? aiSubpath.PathLength / 1.0 : aiSubpath.PathLength / Math.Abs(CoreCommon.Communications.GetVehicleSpeed().Value));

									// get our time to the intersection point
									double ourSpeed = Math.Abs(CoreCommon.Communications.GetVehicleSpeed().Value);
									double stoppedTime = ourSpeed < 1.0 ? 1.5 : 0.0;
									double extraTime = 1.5;
									double interconnectTime = aiSubpath.PathLength / this.turn.MaximumDefaultSpeed;
									double ourTime = Math.Min(6.5, stoppedTime + extraTime + interconnectTime);

									// get closest vehicle in that lane to the intersection
									List<VehicleAgent> toLook = new List<VehicleAgent>();
									if (TacticalDirector.VehicleAreas.ContainsKey(ii.Area))
									{
										foreach (VehicleAgent tmpVa in TacticalDirector.VehicleAreas[ii.Area])
										{
											double upstreamDist = al.DistanceBetween(tmpVa.ClosestPosition, ii.Exit.Position);
											if (upstreamDist > 0 && tmpVa.PassedDelayedBirth)
												toLook.Add(tmpVa);
										}
									}
									if (TacticalDirector.VehicleAreas.ContainsKey(interconnect))
										toLook.AddRange(TacticalDirector.VehicleAreas[interconnect]);

									// check length of stuff to look at
									if (toLook.Count > 0)
									{
										foreach (VehicleAgent va in toLook)
										{
											// distance along path to location of intersect
											double distToIntersect = lp.DistanceBetween(lp.GetClosestPoint(va.ClosestPosition), lp.GetClosestPoint(aiSubpath.GetClosestPoint(va.ClosestPosition).Location));

											double speed = va.Speed == 0.0 ? 0.01 : va.Speed;
											double vaTime = distToIntersect / Math.Abs(speed);
											if (vaTime > 0 && vaTime < ourTime)
											{
												ArbiterOutput.Output("va: " + va.ToString() + " CollisionTimer: " + vaTime.ToString("f2") + " < TimeUs: " + ourTime.ToString("F2") + ", NOGO");
												return false;
											}
										}
									}
								}

								#endregion
							}
						}
					}

					#endregion

					#region Updated Priority Intersection Code

					// loop through updated priorities
					bool updatedPrioritiesClear = true;
					foreach (IntersectionInvolved ii in updatedPriorities)
					{
						// lane
						ArbiterLane al = (ArbiterLane)ii.Area;

						// get our time to the intersection point
						double ourSpeed = Math.Abs(CoreCommon.Communications.GetVehicleSpeed().Value);
						double stoppedTime = ourSpeed < 1.0 ? 1.5 : 0.0;
						double extraTime = 1.5;
						double interconnectTime = aiSubpath.PathLength / this.turn.MaximumDefaultSpeed;
						double ourTime = Math.Min(6.5, stoppedTime + extraTime + interconnectTime);

							// double outTime = Math.Min(4.0, Math.Abs(CoreCommon.Communications.GetVehicleSpeed().Value) < 0.001 ? aiSubpath.PathLength / 1.0 : aiSubpath.PathLength / Math.Abs(CoreCommon.Communications.GetVehicleSpeed().Value));

						// get closest vehicle in that lane to the intersection
						if (TacticalDirector.VehicleAreas.ContainsKey(ii.Area))
						{
							// get lane vehicles
							List<VehicleAgent> vas = TacticalDirector.VehicleAreas[ii.Area];

							// determine for all
							VehicleAgent closestLaneVa = null;
							double closestDistanceVa = Double.MaxValue;
							double closestTime = Double.MaxValue;
							foreach (VehicleAgent testVa in vas)
							{
								// check upstream
								double distance = al.DistanceBetween(testVa.ClosestPosition, ii.Exit.Position);

								// get speed
								double speed = testVa.Speed;
								double time = testVa.StateMonitor.Observed.speedValid ? distance / Math.Abs(speed) : distance / al.Way.Segment.SpeedLimits.MaximumSpeed;

								// check distance > 0
								if (distance > 0)
								{
									// check if closer or none other exists
									if (closestLaneVa == null || time < closestTime)
									{
										closestLaneVa = testVa;
										closestDistanceVa = distance;
										closestTime = time;
									}
								}
							}

							// check if closest exists
							if (closestLaneVa != null)
							{
								// set va
								VehicleAgent va = closestLaneVa;
								double distance = closestDistanceVa;

								// check dist and birth time
								if (distance > 0 && va.PassedDelayedBirth)
								{
									// check time
									double speed = va.Speed == 0.0 ? 0.01 : va.Speed;
									double time = va.StateMonitor.Observed.speedValid ? distance / Math.Abs(speed) : distance / al.Way.Segment.SpeedLimits.MaximumSpeed;

									// too close
									if (!al.LanePolygon.IsInside(CoreCommon.Communications.GetVehicleState().Front) &&
										distance < 25 && (!va.StateMonitor.Observed.speedValid || !va.StateMonitor.Observed.isStopped) &&
										CoreCommon.Communications.GetVehicleState().Front.DistanceTo(va.ClosestPosition) < 20)
									{
										ArbiterOutput.Output("Turn, NOGO, Lane: " + al.ToString() + " vehicle: " + va.ToString() + " possibly moving to close, stopping");
										//return false;
										updatedPrioritiesClear = false;
										return false;
									}
									else if (time > 0 && time < ourTime)
									{
										ArbiterOutput.Output("Turn, NOGO, Lane: " + al.ToString() + ", va: " + va.ToString() + ", stopped: " + va.IsStopped.ToString() + ", timeUs: " + ourTime.ToString("f2") + ", timeThem: " + time.ToString("f2"));
										//return false;
										updatedPrioritiesClear = false;
										return false;
									}
									else
									{
										ArbiterOutput.Output("Turn, CANGO, Lane: " + al.ToString() + ", va: " + va.ToString() + ", stopped: " + va.IsStopped.ToString() + ", timeUs: " + ourTime.ToString("f2") + ", timeThem: " + time.ToString("f2"));
										//return true;
									}
								}
							}
							else
							{
								ArbiterOutput.Output("Turn, CANGO, Lane: " + al.ToString() + " has no traffic vehicles");
							}
						}
					}
					return updatedPrioritiesClear;

					#endregion
				}
			}

			// fall through fine to go
			ArbiterOutput.Output("In Turn, CAN GO, Clear of vehicles upstream");
			return true;
		}

		/// <summary>
		/// Check where turns intersect the subpath furthese along it
		/// </summary>
		/// <param name="aiSubpath"></param>
		/// <param name="iTraversableWaypoint"></param>
		/// <returns></returns>
		private Coordinates? TurnIntersects(LinePath aiSubpath, ITraversableWaypoint iTraversableWaypoint, out ArbiterInterconnect interconnect)
		{
			Line aiSub = new Line(aiSubpath.StartPoint.Location, aiSubpath.EndPoint.Location);
			double distance = 0.0;
			Coordinates? furthest = null;
			interconnect = null;

			foreach (ArbiterInterconnect ai in iTraversableWaypoint.OutgoingConnections)
			{
				Line iLine = new Line(ai.InterconnectPath.StartPoint.Location, ai.InterconnectPath.EndPoint.Location);
				Coordinates intersect;
				bool doesIntersect = aiSub.Intersect(iLine, out intersect);
				if (doesIntersect && (ai.IsInside(intersect) || ai.InterconnectPath.GetClosestPoint(intersect).Equals(ai.InterconnectPath.EndPoint)))
				{
					if (!furthest.HasValue)
					{
						furthest = intersect;
						distance = intersect.DistanceTo(aiSubpath.EndPoint.Location);
						interconnect = ai;
					}
					else
					{
						double tmpDist = intersect.DistanceTo(aiSubpath.EndPoint.Location);
						if (tmpDist < distance)
						{
							furthest = intersect;
							distance = tmpDist;
							interconnect = ai;
						}
					}
				}
			}

			if (furthest.HasValue)
				return furthest.Value;
			else
				return null;
		}

		/// <summary>
		/// Gets priotiy lane determination
		/// </summary>
		/// <param name="ii"></param>
		/// <param name="path"></param>
		/// <param name="end"></param>
		/// <returns></returns>
		private Coordinates? LaneIntersectsPath(IntersectionInvolved ii, LinePath path, IArbiterWaypoint end)
		{
			ArbiterLane al = (ArbiterLane)ii.Area;
			LinePath.PointOnPath current = path.StartPoint;
			bool go = true;
			while (go)// && !(current.Location.DistanceTo(path.EndPoint.Location) < 0.1))
			{
				Coordinates alClose = al.LanePath().GetClosestPoint(current.Location).Location;
				double alDist = alClose.DistanceTo(current.Location);
				if (alDist <= 0.05)
				{
					return al.LanePath().GetClosestPoint(current.Location).Location;
				}

				if(current.Location.Equals(path.EndPoint.Location))
					go = false;

				current = path.AdvancePoint(current, 0.1);
			}

			/*if (ii.Exit != null)
			{
				ITraversableWaypoint laneExit = ii.Exit;
				foreach (ArbiterInterconnect tmpAi in laneExit.OutgoingConnections)
				{
					if (tmpAi.FinalGeneric.Equals(end))
					{
						return tmpAi.FinalGeneric.Position;
					}
				}
			}*/

			return null;
		}

		/// <summary>
		/// Gets parameters to follow vehicle
		/// </summary>
		/// <param name="vehicleState"></param>
		/// <param name="final"></param>
		/// <returns></returns>
		private TravelingParameters VehicleParameterization(VehicleState vehicleState, LinePath fullPath, ArbiterWaypoint final)
		{
			// get simple following Speed and distance for following
			double followingSpeed;
			double distanceToGood;
			double xSeparation;
			this.forwardMonitor.Follow(vehicleState, fullPath, final.Lane, this.turn, out followingSpeed, out distanceToGood, out xSeparation);

			// get parameterization
			TravelingParameters vehicleParams = this.GetParameters(followingSpeed, distanceToGood, final, vehicleState, false);

			// add to current parames to arbiter information
			CoreCommon.CurrentInformation.FVTBehavior = vehicleParams.Behavior.ToShortString();
			CoreCommon.CurrentInformation.FVTSpeed = vehicleParams.RecommendedSpeed.ToString("F3");
			CoreCommon.CurrentInformation.FVTSpeedCommand = vehicleParams.Behavior.SpeedCommandString();
			CoreCommon.CurrentInformation.FVTDistance = vehicleParams.DistanceToGo.ToString("F2");
			CoreCommon.CurrentInformation.FVTState = vehicleParams.NextState.ShortDescription();
			CoreCommon.CurrentInformation.FVTStateInfo = vehicleParams.NextState.StateInformation();
			CoreCommon.CurrentInformation.FVTXSeparation = xSeparation.ToString("F3");
			CoreCommon.CurrentInformation.FVTIgnorable = this.forwardMonitor.VehiclesToIgnore.ToArray();

			// return the parameterization
			return vehicleParams;
		}

		/// <summary>
		/// Plans if we need secondary maneuver if intersection breaks down and need to abort
		/// </summary>
		/// <param name="vehicleState"></param>
		/// <param name="intersectionPlan"></param>
		/// <returns></returns>
		public Maneuver? SecondaryManeuver(VehicleState vehicleState, IntersectionPlan intersectionPlan)
		{
			// don't say anything
			return null;
		}

		/// <summary>
		/// Navigation parameterization given point of intereset and our vehicle state
		/// </summary>
		/// <param name="vehicleState"></param>
		/// <param name="dpoi"></param>
		/// <returns></returns>
		private TravelingParameters NavigationParameterization(VehicleState vehicleState, DownstreamPointOfInterest dpoi, ArbiterWaypoint final, LinePath fullPath)
		{
			// stop waypoint and distance
			ArbiterWaypoint stopWaypoint = dpoi.PointOfInterest;
			double distanceToStop = final.Lane.DistanceBetween(final.Position, stopWaypoint.Position) + vehicleState.Front.DistanceTo(this.turn.FinalGeneric.Position);

			// checks if we need to stop at this point
			bool isStop = dpoi.IsExit || 
				(dpoi.IsGoal && dpoi.PointOfInterest.IsCheckpoint && 
				dpoi.PointOfInterest.CheckpointId.Equals(CoreCommon.Mission.MissionCheckpoints.Peek().CheckpointNumber) &&
				CoreCommon.Mission.MissionCheckpoints.Count == 1);

			// get next lane type stop
			List<WaypointType> stopTypes = new List<WaypointType>();
			stopTypes.Add(WaypointType.End);
			stopTypes.Add(WaypointType.Stop);
			ArbiterWaypoint nextStop = final.Lane.GetNext(final, stopTypes, new List<ArbiterWaypoint>());			

			// get total distance to lane stop
			double distanceToLaneStop = final.Lane.DistanceBetween(final.Position, nextStop.Position) + vehicleState.Front.DistanceTo(this.turn.FinalGeneric.Position);

			// check that we have nearest and correct stop
			if (!isStop || (isStop && distanceToLaneStop <= distanceToStop))
			{
				stopWaypoint = nextStop;
				distanceToStop = distanceToLaneStop;
			}

			// get speed to stop
			double stopSpeed = this.StoppingParameters(distanceToStop, final.Lane.Way.Segment.SpeedLimits.MaximumSpeed);

			// get params
			TravelingParameters navParams = this.GetParameters(stopSpeed, distanceToStop, final, vehicleState, true);

			// add to current parames to arbiter information
			CoreCommon.CurrentInformation.FQMBehavior = navParams.Behavior.ToShortString();
			CoreCommon.CurrentInformation.FQMBehaviorInfo = navParams.Behavior.ShortBehaviorInformation();
			CoreCommon.CurrentInformation.FQMSpeedCommand = navParams.Behavior.SpeedCommandString();
			CoreCommon.CurrentInformation.FQMDistance = navParams.DistanceToGo.ToString("F6");
			CoreCommon.CurrentInformation.FQMSpeed = navParams.RecommendedSpeed.ToString("F6");
			CoreCommon.CurrentInformation.FQMState = navParams.NextState.ShortDescription();
			CoreCommon.CurrentInformation.FQMStateInfo = navParams.NextState.StateInformation();
			CoreCommon.CurrentInformation.FQMStopType = "Dpoi: " + dpoi.PointOfInterest.Equals(stopWaypoint) + ", Stop: " + isStop.ToString();
			CoreCommon.CurrentInformation.FQMWaypoint = stopWaypoint.ToString();
			CoreCommon.CurrentInformation.FQMSegmentSpeedLimit = Math.Min(final.Lane.CurrentMaximumSpeed(vehicleState.Front), this.turn.MaximumDefaultSpeed).ToString("F2");

			// return 
			return navParams;
		}

		/// <summary>
		/// Gets speed we are supposed to be at given we are stopping at some distance
		/// </summary>
		/// <param name="distanceToDpoi"></param>
		/// <returns></returns>
		private double StoppingParameters(double distanceToDpoi, double finalAreaSpeed)
		{
			// subtract distance based upon type to help calculate speed
			double stopSpeedDistance = distanceToDpoi - CoreCommon.OperationalStopDistance;

			// check if we are positive distance away
			if (stopSpeedDistance >= 0)
			{
				// segment max speed
				double segmentMaxSpeed = Math.Min(turn.MaximumDefaultSpeed, finalAreaSpeed);

				// distance to stop from max v given desired acceleration
				//double stopEnvelopeLength = (Math.Pow(CoreCommon.OperationalStopSpeed, 2) - Math.Pow(segmentMaxSpeed, 2)) /	(2.0 * -CoreCommon.DesiredAcceleration);

				// check if we are within profile
				if (stopSpeedDistance > 0.0)
				{
					// shifted speed
					//double shiftedDistanceToEnd = stopEnvelopeLength - stopSpeedDistance;

					// get speed along profile
					double speedGen = SpeedTools.GenerateSpeed(stopSpeedDistance, CoreCommon.OperationalStopSpeed, segmentMaxSpeed);
					return speedGen;
					//return CoreCommon.OperationalStopSpeed + (shiftedMaxSpeed - ((stopEnvelopeLength - shiftedDistanceToEnd)/stopEnvelopeLength * shiftedMaxSpeed));
				}
				else
				{
					return segmentMaxSpeed;
				}
			}
			else
			{
				// inside stop dist spool to 0
				double stopSpeed = (distanceToDpoi / CoreCommon.OperationalStopDistance) * CoreCommon.OperationalStopSpeed;
				stopSpeed = stopSpeed < 0 ? 0.0 : stopSpeed;
				return stopSpeed;
			}
		}

		/// <summary>
		/// Gets parameterization
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="speed"></param>
		/// <param name="distance"></param>
		/// <param name="state"></param>
		/// <returns></returns>
		public TravelingParameters GetParameters(double speed, double distance, ArbiterWaypoint final, VehicleState state, bool canUseDistance)
		{
			double distanceCutOff = CoreCommon.OperationalStopDistance;
			SpeedCommand sc;
			bool usingSpeed = true;
			Maneuver m;

			#region Generate Next State

			// get turn params
			LinePath finalPath;
			LineList leftLL;
			LineList rightLL;
			IntersectionToolkit.TurnInfo(final, out finalPath, out leftLL, out rightLL);
			TurnState ts = new TurnState(this.turn, this.turn.TurnDirection, final.Lane, finalPath, leftLL, rightLL, new ScalarSpeedCommand(speed));

			#endregion

			#region Distance Cutoff

			// check if distance is less than cutoff
			if (distance < distanceCutOff && canUseDistance)
			{
				// default behavior
				sc = new StopAtDistSpeedCommand(distance);				

				// stopping so not using speed param
				usingSpeed = false;
			}

			#endregion

			#region Outisde Distance Envelope

			// not inside distance envalope
			else
			{
				// default behavior
				sc = new ScalarSpeedCommand(speed);
			}

			#endregion

			#region Generate Maneuver

			if (ts.Interconnect.InitialGeneric is ArbiterWaypoint &&
						CoreCommon.RoadNetwork.IntersectionLookup.ContainsKey(((ArbiterWaypoint)ts.Interconnect.InitialGeneric).AreaSubtypeWaypointId))
			{
				Polygon p = CoreCommon.RoadNetwork.IntersectionLookup[((ArbiterWaypoint)ts.Interconnect.InitialGeneric).AreaSubtypeWaypointId].IntersectionPolygon;

				// behavior
				Behavior b = new TurnBehavior(ts.TargetLane.LaneId, ts.EndingPath, ts.LeftBound, ts.RightBound, sc, p, this.forwardMonitor.VehiclesToIgnore, ts.Interconnect.InterconnectId);
				m = new Maneuver(b, ts, ts.DefaultStateDecorators, state.Timestamp);
			}
			else
			{
				// behavior
				Behavior b = new TurnBehavior(ts.TargetLane.LaneId, ts.EndingPath, ts.LeftBound, ts.RightBound, sc, null, this.forwardMonitor.VehiclesToIgnore, ts.Interconnect.InterconnectId);
				m = new Maneuver(b, ts, ts.DefaultStateDecorators, state.Timestamp);
			}

			#endregion

			#region Parameterize

			// create new params
			TravelingParameters tp = new TravelingParameters();
			tp.Behavior = m.PrimaryBehavior;
			tp.Decorators = m.PrimaryBehavior.Decorators;
			tp.DistanceToGo = distance;
			tp.NextState = m.PrimaryState;
			tp.RecommendedSpeed = speed;
			tp.Type = TravellingType.Navigation;
			tp.UsingSpeed = usingSpeed;
			tp.SpeedCommand = sc;
			tp.VehiclesToIgnore = this.forwardMonitor.VehiclesToIgnore;

			#endregion

			// return navigation params
			return tp;
		}
	}
}
