using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Generates what partitions are adjacent to other partitions
	/// </summary>
	[Serializable]
	public class AdjacencyGeneration
	{
		/// <summary>
		/// Generates all adjacency mappings for the road network
		/// </summary>
		/// <param name="arn"></param>
		/// <returns></returns>
		public ArbiterRoadNetwork GenerateAdjacencies(ArbiterRoadNetwork arn)
		{
			// generate lane adjacency
			this.GenerateLaneAdjacency(arn);

			// generate partition adjacency
			this.GenerateLanePartitionAdjacency(arn);

			// generate entry adjacency
			this.GenerateNavigationalAdjacency(arn);

			// return
			return arn;
		}

		/// <summary>
		/// Generates entry adjacency 
		/// </summary>
		/// <param name="arn"></param>
		/// <remarks>Determines for every entry in a segment, the closest, reachable
		/// waypoints on lanes in the same way if it exists</remarks>
		private void GenerateNavigationalAdjacency(ArbiterRoadNetwork arn)
		{
			// loop over segments
			foreach(ArbiterSegment asg in arn.ArbiterSegments.Values)
			{
				// loop over segment waypoints
				foreach(ArbiterWaypoint aw in asg.Waypoints.Values)
				{
					#region Next Waypoint

					// check if has next
					if (aw.NextPartition != null)
					{
						// add next waypoint
						aw.OutgoingConnections.Add(aw.NextPartition);
					}

					#endregion

					#region Exits

					// check if exit
					if (aw.IsExit)
					{
						// loop over interconnect
						foreach (ArbiterInterconnect ai in aw.Exits)
						{
							// add entries
							aw.OutgoingConnections.Add(ai);
						}
					}

					#endregion

					#region Adjacent Lanes

					// check if entry
					if (aw.IsEntry)
					{
						// foreach lane test in same way as aw
						foreach (ArbiterLane al in aw.Lane.Way.Lanes.Values)
						{
							// check if not same lane
							if (!aw.Lane.Equals(al) && al.RelativelyInside(aw.Position))
							{
								// check ok
								if ((aw.Lane.LaneOnLeft != null && aw.Lane.LaneOnLeft.Equals(al) && aw.Lane.BoundaryLeft != ArbiterLaneBoundary.SolidWhite) ||
									(aw.Lane.LaneOnRight != null && aw.Lane.LaneOnRight.Equals(al) && aw.Lane.BoundaryRight != ArbiterLaneBoundary.SolidWhite))
								{
									// get closest partition to aw's point
									ArbiterLanePartition alp = al.GetClosestPartition(aw.Position);

									// check downstream from this lane?
									if (aw.Lane.DistanceBetween(aw.Position, alp.Final.Position) >= -0.01 ||
										(alp.Final.NextPartition != null && aw.Lane.DistanceBetween(aw.Position, alp.Final.NextPartition.Final.Position) >= -0.01))
									{
										// new list of contained partitions
										List<IConnectAreaWaypoints> containedPartitions = new List<IConnectAreaWaypoints>();
										containedPartitions.Add(alp);
										containedPartitions.Add(aw.NextPartition);

										// set aw as linking to that partition's final waypoint
										aw.OutgoingConnections.Add(new NavigableEdge(false, null, true, al.Way.Segment, containedPartitions, aw, alp.Final));
									}
								}
							}
						}
					}

					#endregion
				}

				#region Adjacent starting in middle

				// loop over segment lanes
				foreach (ArbiterLane al in asg.Lanes.Values)
				{
					// get init wp
					ArbiterWaypoint initial = al.WaypointList[0];

					// get adjacent lanes
					foreach (ArbiterLane adj in al.Way.Lanes.Values)
					{
						if (!adj.Equals(al))
						{
							// check if initial is inside other
							if (adj.RelativelyInside(initial.Position))
							{
								if ((adj.LaneOnLeft != null && adj.LaneOnLeft.Equals(al) && adj.BoundaryLeft != ArbiterLaneBoundary.SolidWhite) ||
									(adj.LaneOnRight != null && adj.LaneOnRight.Equals(al) && adj.BoundaryRight != ArbiterLaneBoundary.SolidWhite))
								{
									// closest partition
									ArbiterLanePartition alp = adj.GetClosestPartition(initial.Position);

									// new list of contained partitions
									List<IConnectAreaWaypoints> containedPartitions = new List<IConnectAreaWaypoints>();
									containedPartitions.Add(alp);
									containedPartitions.Add(initial.NextPartition);

									// set aw as linking to that partition's final waypoint
									alp.Initial.OutgoingConnections.Add(new NavigableEdge(false, null, true, al.Way.Segment, containedPartitions, alp.Initial, initial));
								}
							}
						}
					}
				}

				#endregion
			}

			// loop over zones
			foreach (ArbiterZone az in arn.ArbiterZones.Values)
			{
				#region Perimeter Point adjacency

				// loop over zone perimeter points
				foreach (ArbiterPerimeterWaypoint apw in az.Perimeter.PerimeterPoints.Values)
				{
					#region Old Connectivity
					// check if this is an entry
					/*if (apw.IsEntry)
					{
						#region Link Perimeter Points

						// loop over perimeter points
						foreach(ArbiterPerimeterWaypoint apwTest in az.Perimeter.PerimeterPoints.Values)
						{
							// check not this and is exit
							if (!apw.Equals(apwTest) && apwTest.IsExit)
							{
								// add to connections
								apw.OutgoingConnections.Add(new NavigableEdge(true, apw.Perimeter.Zone, false, null, new List<IConnectAreaWaypoints>(), apw, apwTest));
							}
						}

						#endregion

						#region Link Checkpoints

						// loop over parking spot waypoints
						foreach (ArbiterParkingSpotWaypoint apsw in az.ParkingSpotWaypoints.Values)
						{
							// check if checkpoint
							if (apsw.IsCheckpoint)
							{
								// add to connections
								apw.OutgoingConnections.Add(new NavigableEdge(true, apw.Perimeter.Zone, false, null, new List<IConnectAreaWaypoints>(), apw, apsw));
							}
						}

						#endregion
					}*/

					#endregion

					// check if the point is an exit
					if (apw.IsExit)
					{
						foreach (ArbiterInterconnect ai in apw.Exits)
						{
							// add to connections
							apw.OutgoingConnections.Add(ai);
						}
					}
				}

				#endregion

				#region Checkpoint adjacency

				// loop over parking spot waypoints
				foreach (ArbiterParkingSpotWaypoint apsw in az.ParkingSpotWaypoints.Values)
				{
					if (apsw.ParkingSpot.NormalWaypoint.Equals(apsw))
					{
						apsw.OutgoingConnections.Add(
							new NavigableEdge(true, az, false, null, new List<IConnectAreaWaypoints>(), apsw, apsw.ParkingSpot.Checkpoint));
					}
					else
					{
						apsw.OutgoingConnections.Add(
							new NavigableEdge(true, az, false, null, new List<IConnectAreaWaypoints>(), apsw, apsw.ParkingSpot.NormalWaypoint));
					}
				}

				#region Old
				/*
				// loop over parking spot waypoints
				foreach (ArbiterParkingSpotWaypoint apsw in az.ParkingSpotWaypoints.Values)
				{
					// check if checkpoint
					if (apsw.IsCheckpoint)
					{
						#region Link Perimeter Points

						// loop over perimeter points
						foreach (ArbiterPerimeterWaypoint apwTest in az.Perimeter.PerimeterPoints.Values)
						{
							// check not this and is exit
							if (apwTest.IsExit)
							{
								// add to connections
								apsw.OutgoingConnections.Add(new NavigableEdge(true, apsw.ParkingSpot.Zone, false, null, new List<IConnectAreaWaypoints>(), apsw, apwTest));
							}
						}

						#endregion

						#region Link Checkpoints

						// loop over parking spot waypoints
						foreach (ArbiterParkingSpotWaypoint apswTest in az.ParkingSpotWaypoints.Values)
						{
							// check if checkpoint
							if (!apsw.Equals(apswTest) && apswTest.IsCheckpoint)
							{
								// add to connections
								apsw.OutgoingConnections.Add(new NavigableEdge(true, apsw.ParkingSpot.Zone, false, null, new List<IConnectAreaWaypoints>(), apsw, apswTest));
							}
						}

						#endregion
					}
				}
				*/
				#endregion

				#endregion
			}
		}

		/// <summary>
		/// Generates lane partition adjacency
		/// </summary>
		/// <param name="arn"></param>
		private void GenerateLanePartitionAdjacency(ArbiterRoadNetwork arn)
		{
			// loop over segments
			foreach (ArbiterSegment asg in arn.ArbiterSegments.Values)
			{
				// loop over lanes
				foreach (ArbiterLane al in asg.Lanes.Values)
				{
					// loop over lane partitions
					foreach (ArbiterLanePartition alp in al.Partitions)
					{
						// left lane
						if (al.LaneOnLeft != null)
						{
							this.GenerateSinglePartitionAdjacency(alp, al.LaneOnLeft);
						}

						// right lane
						if (al.LaneOnRight != null)
						{
							this.GenerateSinglePartitionAdjacency(alp, al.LaneOnRight);
						}
					}
				}
			}
		}

		/// <summary>
		/// Generates adjacency of a partition to another lane
		/// </summary>
		/// <param name="alp"></param>
		/// <param name="al"></param>
		private void GenerateSinglePartitionAdjacency(ArbiterLanePartition alp, ArbiterLane al)
		{
			// fake path
			List<IPathSegment> fakePathSegments = new List<IPathSegment>();
			fakePathSegments.Add(new LinePathSegment(alp.Initial.Position, alp.Final.Position));
			Path fakePath = new Path(fakePathSegments);

			// partitions adjacent
			List<ArbiterLanePartition> adjacent = new List<ArbiterLanePartition>();

			// tracks
			PointOnPath current = fakePath.StartPoint;
			double increment = 0.5;
			double tmpInc = 0;

			// increment along
			while (tmpInc == 0)
			{
				// loop over partitions
				foreach (ArbiterLanePartition alpar in al.Partitions)
				{
					// get fake path for partition
					List<IPathSegment> ips = new List<IPathSegment>();
					ips.Add(new LinePathSegment(alpar.Initial.Position, alpar.Final.Position));
					Path alpPath = new Path(ips);

					// get closest point on tmp partition to current
					PointOnPath closest = alpPath.GetClosest(current.pt);

					// check general distance
					if (closest.pt.DistanceTo(current.pt) < 20)
					{
						// check not start or end
						if (!closest.Equals(alpPath.StartPoint) && !closest.Equals(alpPath.EndPoint))
						{
							// check not already added
							if (!adjacent.Contains(alpar))
							{
								// add
								adjacent.Add(alpar);
							}
						}
					}
				}

				// set inc
				tmpInc = increment;

				// increment point
				current = fakePath.AdvancePoint(current, ref tmpInc);
			}

			// add adjacent
			alp.NonLaneAdjacentPartitions.AddRange(adjacent);
		}

		/// <summary>
		/// Generates lane adjacency in the network
		/// </summary>
		/// <param name="arn"></param>
		private void GenerateLaneAdjacency(ArbiterRoadNetwork arn)
		{
			// loop over segments
			foreach (ArbiterSegment asg in arn.ArbiterSegments.Values)
			{
				// check if both ways exist for first algorithm
				if (asg.Way1.IsValid && asg.Way2.IsValid)
				{
					// lanes of the segment
					Dictionary<ArbiterLaneId, ArbiterLane> segLanes = asg.Lanes;

					// get a sample lane from way 1
					Dictionary<ArbiterLaneId, ArbiterLane>.Enumerator way1Enum = asg.Way1.Lanes.GetEnumerator();
					way1Enum.MoveNext();
					ArbiterLane way1Sample = way1Enum.Current.Value;

					// get a sample lane from way 2
					Dictionary<ArbiterLaneId, ArbiterLane>.Enumerator way2Enum = asg.Way2.Lanes.GetEnumerator();
					way2Enum.MoveNext();
					ArbiterLane way2Sample = way2Enum.Current.Value;

					// direction, 1 means way1 has lower # lanes
					int modifier = 1;

					// check if way 2 has lower lane numbers
					if (way1Sample.LaneId.Number > way2Sample.LaneId.Number)
					{
						// set modifier to -1 so count other way
						modifier = -1;
					}

					// loop over lanes
					foreach (ArbiterLane al in segLanes.Values)
					{
						// if not lane 1
						if (al.LaneId.Number != 1)
						{
							// get lower # lane in way 1
							ArbiterLaneId lowerNumWay1Id = new ArbiterLaneId(al.LaneId.Number - 1, asg.Way1.WayId);

							// check if the segment contains this lane
							if (segLanes.ContainsKey(lowerNumWay1Id))
							{
								// get lane
								ArbiterLane lowerNumWay1 = segLanes[lowerNumWay1Id];

								// check if current lane is also in way 1
								if (lowerNumWay1.Way.WayId.Equals(al.Way.WayId))
								{
									// check modifier for 1 => lower is to right
									if (modifier == 1)
									{
										al.LaneOnRight = lowerNumWay1;
										lowerNumWay1.LaneOnLeft = al;
									}
									// otherwise -1 => lane is to left
									else
									{
										al.LaneOnLeft = lowerNumWay1;
										lowerNumWay1.LaneOnRight = al;
									}
								}
								// otherwise the current lane is in a different way
								else
								{
									// the lane is to the left by default
									al.LaneOnLeft = lowerNumWay1;
									lowerNumWay1.LaneOnLeft = al;
								}
							}
							// otherwise the lowe lane is in way 2
							else
							{
								// set lane
								ArbiterLane lowerNumWay2 = segLanes[new ArbiterLaneId(al.LaneId.Number - 1, asg.Way2.WayId)];

								// check if current lane is also in way 2
								if (lowerNumWay2.Way.WayId.Equals(al.Way.WayId))
								{
									// check modifier for 1 => lower is to left
									if (modifier == 1)
									{
										al.LaneOnLeft = lowerNumWay2;
										lowerNumWay2.LaneOnRight = al;
									}
									// otherwise -1 => lane is to right
									else
									{
										al.LaneOnRight = lowerNumWay2;
										lowerNumWay2.LaneOnLeft = al;
									}
								}
								// otherwise the current lane is in a different way
								else
								{
									// the lane is to the left by default
									al.LaneOnLeft = lowerNumWay2;
									lowerNumWay2.LaneOnLeft = al;
								}
							}
						}
					}  // loop over lanes
				} // both ways valid
				// otherwise only a single way is valid
				else
				{
					// lanes of the segment
					Dictionary<ArbiterLaneId, ArbiterLane> segLanes = asg.Lanes;

					// make sure more than one lane exists
					if(segLanes.Count > 1)
					{
						// loop over lanes
						foreach (ArbiterLane al in segLanes.Values)
						{
							// get theoretical id of lane one number up
							ArbiterLaneId ali = new ArbiterLaneId(al.LaneId.Number + 1, al.LaneId.WayId);

							// check if lane one number up exists
							if(segLanes.ContainsKey(ali))
							{
								// get lane one number up
								ArbiterLane alu = segLanes[ali];

								// check # waypoints
								if (al.Waypoints.Count > 1 && alu.Waypoints.Count > 1)
								{
									// get closest points on this lane and other lane
									PointOnPath p1;
									PointOnPath p2;
									double distance;
									CreationTools.GetClosestPoints(al.PartitionPath, alu.PartitionPath, out p1, out p2, out distance);

									// get partition points of closest point on this lane
									Coordinates partitionStart = p1.segment.Start;
									Coordinates partitionEnd = p1.segment.End;

									// get area of partition triangle
									double triangeArea = CreationTools.TriangleArea(partitionStart, p2.pt, partitionEnd);

									// determine if closest point on other lane is to the left or right of partition
									bool onLeft = true;
									if (triangeArea >= 0)
									{
										onLeft = false;
									}

									// set adjacency accordingly for both lanes
									if (onLeft)
									{
										al.LaneOnLeft = alu;
										alu.LaneOnRight = al;
									}
									// otherwise on right
									else
									{
										al.LaneOnRight = alu;
										alu.LaneOnLeft = al;
									}
								}
							}
						}
					}
				} // end single way only valid

				// loop over lanes to print info on adjacency
				/*foreach (ArbiterLane al in asg.Lanes.Values)
				{
					Console.Write(al.LaneId.ToString() + ": ");

					if (al.LaneOnLeft != null)
					{
						Console.Write("Left-" + al.LaneOnLeft.LaneId.ToString());
					}

					if (al.LaneOnRight != null)
					{
						Console.Write("Right-" + al.LaneOnRight.LaneId.ToString());
					}

					Console.Write("\n");
				}
				 */

			} // segment loop
		}

	} // class
} // namespace
 