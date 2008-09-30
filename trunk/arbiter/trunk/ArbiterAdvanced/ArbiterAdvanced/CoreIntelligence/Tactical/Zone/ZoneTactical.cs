using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.Core.Common;
using UrbanChallenge.Arbiter.Core.Common.State;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Navigation;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common.Sensors;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Arbiter.Core.Common.Arbiter;
using UrbanChallenge.Common;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Arbiter.Core.Communications;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Zone
{
	/// <summary>
	/// Plan tactical behaviors in a zone
	/// </summary>
	public class ZoneTactical
	{
		/// <summary>
		/// Plans over the zone
		/// </summary>
		/// <param name="planningState"></param>
		/// <param name="navigationalPlan"></param>
		/// <param name="vehicleState"></param>
		/// <param name="vehicles"></param>
		/// <param name="obstacles"></param>
		/// <param name="blockages"></param>
		/// <returns></returns>
		public Maneuver Plan(IState planningState, INavigationalPlan navigationalPlan, 
			VehicleState vehicleState, SceneEstimatorTrackedClusterCollection vehicles, 
			SceneEstimatorUntrackedClusterCollection obstacles, List<ITacticalBlockage> blockages)
		{
			#region Zone Travelling State

			if (planningState is ZoneTravelingState)
			{
				// check blockages
				/*if (blockages != null && blockages.Count > 0 && blockages[0] is ZoneBlockage)
				{
					// create the blockage state
					EncounteredBlockageState ebs = new EncounteredBlockageState(blockages[0], CoreCommon.CorePlanningState);

					// go to a blockage handling tactical
					return new Maneuver(new NullBehavior(), ebs, TurnDecorators.NoDecorators, vehicleState.Timestamp);
				}*/

				// cast state
				ZoneState zs = (ZoneState)planningState;

				// plan over state and zone
				ZonePlan zp = (ZonePlan)navigationalPlan;

				// check zone path does not exist
				if (zp.RecommendedPath.Count < 2)
				{
					// zone startup again
					ZoneStartupState zss = new ZoneStartupState(zs.Zone, true);
					return new Maneuver(new HoldBrakeBehavior(), zss, TurnDecorators.NoDecorators, vehicleState.Timestamp);
				}

				// final path seg
				LinePath.PointOnPath endBack = zp.RecommendedPath.AdvancePoint(zp.RecommendedPath.EndPoint, -TahoeParams.VL);
				LinePath lp = zp.RecommendedPath.SubPath(endBack, zp.RecommendedPath.EndPoint);
				LinePath lB = lp.ShiftLateral(TahoeParams.T);
				LinePath rB = lp.ShiftLateral(-TahoeParams.T);

				// add to info
				CoreCommon.CurrentInformation.DisplayObjects.Add(new ArbiterInformationDisplayObject(lB, ArbiterInformationDisplayObjectType.leftBound));
				CoreCommon.CurrentInformation.DisplayObjects.Add(new ArbiterInformationDisplayObject(rB, ArbiterInformationDisplayObjectType.rightBound));

				// get speed command
				ScalarSpeedCommand sc = new ScalarSpeedCommand(2.24);

				// Behavior
				Behavior b = new ZoneTravelingBehavior(zp.Zone.ZoneId, zp.Zone.Perimeter.PerimeterPolygon, zp.Zone.StayOutAreas.ToArray(),
					sc, zp.RecommendedPath, lB, rB);

				// maneuver
				return new Maneuver(b, CoreCommon.CorePlanningState, TurnDecorators.NoDecorators, vehicleState.Timestamp);
			}

			#endregion

			#region Parking State

			else if (planningState is ParkingState)
			{
				// get state
				ParkingState ps = (ParkingState)planningState;

				// determine stay out areas to use
				List<Polygon> stayOuts = new List<Polygon>();
				foreach (Polygon p in ps.Zone.StayOutAreas)
				{
					if (!p.IsInside(ps.ParkingSpot.NormalWaypoint.Position) && !p.IsInside(ps.ParkingSpot.Checkpoint.Position))
						stayOuts.Add(p);
				}

				LinePath rB = ps.ParkingSpot.GetRightBound();
				LinePath lB = ps.ParkingSpot.GetLeftBound();

				// add to info
				CoreCommon.CurrentInformation.DisplayObjects.Add(new ArbiterInformationDisplayObject(lB, ArbiterInformationDisplayObjectType.leftBound));
				CoreCommon.CurrentInformation.DisplayObjects.Add(new ArbiterInformationDisplayObject(rB, ArbiterInformationDisplayObjectType.rightBound));				

				// create behavior
				ZoneParkingBehavior zpb = new ZoneParkingBehavior(ps.Zone.ZoneId, ps.Zone.Perimeter.PerimeterPolygon, stayOuts.ToArray(), new ScalarSpeedCommand(2.24), 
					ps.ParkingSpot.GetSpotPath(), lB, rB, ps.ParkingSpot.SpotId, 1.0);

				// maneuver
				return new Maneuver(zpb, ps, TurnDecorators.NoDecorators, vehicleState.Timestamp);
			}

			#endregion

			#region Pulling Out State

			else if (planningState is PullingOutState)
			{
				// get state
				PullingOutState pos = (PullingOutState)planningState;

				// plan over state and zone
				ZonePlan zp = (ZonePlan)navigationalPlan;
				
				// final path seg
				Coordinates endVec = zp.RecommendedPath[0] - zp.RecommendedPath[1];
				Coordinates endBack = zp.RecommendedPath[0] + endVec.Normalize(TahoeParams.VL * 2.0);
				LinePath rp = new LinePath(new Coordinates[]{pos.ParkingSpot.Checkpoint.Position, pos.ParkingSpot.NormalWaypoint.Position,
					zp.RecommendedPath[0], endBack});
				LinePath lp = new LinePath(new Coordinates[]{zp.RecommendedPath[0], endBack});
				LinePath lB = lp.ShiftLateral(TahoeParams.T * 2.0);
				LinePath rB = lp.ShiftLateral(-TahoeParams.T * 2.0);

				// add to info
				CoreCommon.CurrentInformation.DisplayObjects.Add(new ArbiterInformationDisplayObject(lB, ArbiterInformationDisplayObjectType.leftBound));
				CoreCommon.CurrentInformation.DisplayObjects.Add(new ArbiterInformationDisplayObject(rB, ArbiterInformationDisplayObjectType.rightBound));
				CoreCommon.CurrentInformation.DisplayObjects.Add(new ArbiterInformationDisplayObject(rp, ArbiterInformationDisplayObjectType.leftBound));

				// determine stay out areas to use
				List<Polygon> stayOuts = new List<Polygon>();
				foreach (Polygon p in pos.Zone.StayOutAreas)
				{
					if (!p.IsInside(pos.ParkingSpot.NormalWaypoint.Position) && !p.IsInside(pos.ParkingSpot.Checkpoint.Position))
						stayOuts.Add(p);
				}

				// get speed command
				ScalarSpeedCommand sc = new ScalarSpeedCommand(2.24);

				// Behavior
				Behavior b = new ZoneParkingPullOutBehavior(zp.Zone.ZoneId, zp.Zone.Perimeter.PerimeterPolygon, stayOuts.ToArray(),
					sc, pos.ParkingSpot.GetSpotPath(), pos.ParkingSpot.GetLeftBound(), pos.ParkingSpot.GetRightBound(), pos.ParkingSpot.SpotId,
					rp, lB, rB);

				// maneuver
				return new Maneuver(b, pos, TurnDecorators.NoDecorators, vehicleState.Timestamp);
			}

			#endregion

			#region Zone Startup State

			else if (planningState is ZoneStartupState)
			{
				// state
				ZoneStartupState zss = (ZoneStartupState)planningState;

				// get the zone
				ArbiterZone az = zss.Zone;

				// navigational edge
				INavigableNode inn = CoreCommon.RoadNetwork.ArbiterWaypoints[CoreCommon.Mission.MissionCheckpoints.Peek().WaypointId];

				// check over all the parking spaces
				foreach (ArbiterParkingSpot aps in az.ParkingSpots)
				{
					// inside both parts of spot
					if ((vehicleState.VehiclePolygon.IsInside(aps.NormalWaypoint.Position) && vehicleState.VehiclePolygon.IsInside(aps.Checkpoint.Position)) || 
						(vehicleState.VehiclePolygon.IsInside(aps.NormalWaypoint.Position)))
					{
						// want to just park in it again
						return new Maneuver(new HoldBrakeBehavior(), new ParkingState(az, aps), TurnDecorators.NoDecorators, vehicleState.Timestamp);
					}
				}

				Polygon forwardPolygon = vehicleState.ForwardPolygon;
				Polygon rearPolygon = vehicleState.RearPolygon;
				
				Navigator nav = CoreCommon.Navigation;
				List<ZoneNavigationEdgeSort> forwardForward = new List<ZoneNavigationEdgeSort>();
				List<ZoneNavigationEdgeSort> reverseReverse = new List<ZoneNavigationEdgeSort>();
				List<ZoneNavigationEdgeSort> perpendicularPerpendicular = new List<ZoneNavigationEdgeSort>();
				List<ZoneNavigationEdgeSort> allEdges = new List<ZoneNavigationEdgeSort>();
				List<ZoneNavigationEdgeSort> allEdgesNoLimits = new List<ZoneNavigationEdgeSort>();
				foreach (NavigableEdge ne in az.NavigableEdges)
				{
					if (!(ne.End is ArbiterParkingSpotWaypoint) && !(ne.Start is ArbiterParkingSpotWaypoint))
					{
						// get distance to edge
						LinePath lp = new LinePath(new Coordinates[] { ne.Start.Position, ne.End.Position });
						double distTmp = lp.GetClosestPoint(vehicleState.Front).Location.DistanceTo(vehicleState.Front);

						// get direction along segment
						DirectionAlong da = vehicleState.DirectionAlongSegment(lp);
						
						// check dist reasonable
						if (distTmp > TahoeParams.VL)
						{
							// zone navigation edge sort item
							ZoneNavigationEdgeSort znes = new ZoneNavigationEdgeSort(distTmp, ne, lp);

							// add to lists
							if (da == DirectionAlong.Forwards &&
								(forwardPolygon.IsInside(ne.Start.Position) || forwardPolygon.IsInside(ne.End.Position)))
								forwardForward.Add(znes);
							/*else if (da == DirectionAlong.Perpendicular &&
								!(forwardPolygon.IsInside(ne.Start.Position) || forwardPolygon.IsInside(ne.End.Position)) &&
								!(rearPolygon.IsInside(ne.Start.Position) || rearPolygon.IsInside(ne.End.Position)))
								perpendicularPerpendicular.Add(znes);
							else if (rearPolygon.IsInside(ne.Start.Position) || rearPolygon.IsInside(ne.End.Position))
								reverseReverse.Add(znes);*/

							// add to all edges
							allEdges.Add(znes);
						}
					}
				}

				// sort by distance
				forwardForward.Sort();
				reverseReverse.Sort();
				perpendicularPerpendicular.Sort();
				allEdges.Sort();

				ZoneNavigationEdgeSort bestZnes = null;

				for (int i = 0; i < allEdges.Count; i++)
				{
					// get line to initial
					Line toInitial = new Line(vehicleState.Front, allEdges[i].edge.Start.Position);
					Line toFinal = new Line(vehicleState.Front, allEdges[i].edge.End.Position);
					bool intersects = false;
					Coordinates[] interPts;
					foreach (Polygon sop in az.StayOutAreas)
					{
						if (!intersects &&
							(sop.Intersect(toInitial, out interPts) && sop.Intersect(toFinal, out interPts)))
							intersects = true;
					}

					if (!intersects)
					{
						allEdges[i].zp = nav.PlanZone(az, allEdges[i].edge.End, inn);
						allEdges[i].zp.Time += vehicleState.Front.DistanceTo(allEdges[i].lp.GetClosestPoint(vehicleState.Front).Location) / 2.24;
						ZoneNavigationEdgeSort tmpZnes = allEdges[i];
						if ((bestZnes == null && tmpZnes.zp.RecommendedPath.Count > 1) ||
								(bestZnes != null && tmpZnes.zp.RecommendedPath.Count > 1 && tmpZnes.zp.Time < bestZnes.zp.Time))
							bestZnes = tmpZnes;
					}

					if (i > allEdges.Count / 2 && bestZnes != null)
						break;
				}

				if (bestZnes != null)
				{
					ArbiterOutput.Output("Found good edge to start in zone");
					return new Maneuver(new HoldBrakeBehavior(), new ZoneOrientationState(az, bestZnes.edge), TurnDecorators.NoDecorators, vehicleState.Timestamp);
				}
				else
				{
					ArbiterOutput.Output("Could not find good edge to start, choosing random not blocked");

					List<ZoneNavigationEdgeSort> okZnes = new List<ZoneNavigationEdgeSort>();
					foreach (NavigableEdge tmpOk in az.NavigableEdges)
					{
						// get line to initial
						LinePath edgePath = new LinePath(new Coordinates[] { tmpOk.Start.Position, tmpOk.End.Position });
						double dist = edgePath.GetClosestPoint(vehicleState.Front).Location.DistanceTo(vehicleState.Front);
						ZoneNavigationEdgeSort tmpZnes = new ZoneNavigationEdgeSort(dist, tmpOk, edgePath);
						tmpZnes.zp = nav.PlanZone(az, tmpZnes.edge.End, inn);
						tmpZnes.zp.Time += vehicleState.Front.DistanceTo(tmpZnes.lp.GetClosestPoint(vehicleState.Front).Location) / 2.24;
						if (tmpZnes.zp.RecommendedPath.Count >= 2)
							okZnes.Add(tmpZnes);
					}

					if (okZnes.Count == 0)
						okZnes = allEdges;
					else
						okZnes.Sort();
					
					// get random close edge
					Random rand = new Random();
					int chosen = rand.Next(Math.Min(5, okZnes.Count));

					// get closest edge not part of a parking spot, get on it
					NavigableEdge closest = okZnes[chosen].edge;//null;
					//double distance = Double.MaxValue;
					/*foreach (NavigableEdge ne in az.NavigableEdges)
					{
						if (!(ne.End is ArbiterParkingSpotWaypoint) && !(ne.Start is ArbiterParkingSpotWaypoint))
						{
							// get distance to edge
							LinePath lp = new LinePath(new Coordinates[] { ne.Start.Position, ne.End.Position });
							double distTmp = lp.GetClosestPoint(vehicleState.Front).Location.DistanceTo(vehicleState.Front);
							if (closest == null || distTmp < distance)
							{
								closest = ne;
								distance = distTmp;
							}
						}
					}*/
					return new Maneuver(new HoldBrakeBehavior(), new ZoneOrientationState(az, closest), TurnDecorators.NoDecorators, vehicleState.Timestamp);
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

		public class ZoneNavigationEdgeSort : IComparable
		{
			public double distance;
			public NavigableEdge edge;
			public LinePath lp;
			public ZonePlan zp;

			public ZoneNavigationEdgeSort(double distance, NavigableEdge edge, LinePath lp)
			{
				this.distance = distance;
				this.edge = edge;
				this.lp = lp;
				this.zp = null;
			}

			public override string ToString()
			{
				return edge.Start.Position.ToString();
			}

			#region IComparable Members

			public int CompareTo(object obj)
			{
				if (obj is ZoneNavigationEdgeSort)
				{
					ZoneNavigationEdgeSort other = (ZoneNavigationEdgeSort)obj;
					return this.distance.CompareTo(other.distance);
				}
				else
					return -1;
			}

			#endregion
		}
	}
}
