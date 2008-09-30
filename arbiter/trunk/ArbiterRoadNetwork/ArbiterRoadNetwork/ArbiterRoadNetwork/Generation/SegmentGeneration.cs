using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.DarpaRndf;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Generates segments from an xyRndf
	/// </summary>
	[Serializable]
	public class SegmentGeneration
	{
		private ICollection<SimpleSegment> segments;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="segments"></param>
		public SegmentGeneration(ICollection<SimpleSegment> segments)
		{
			this.segments = segments;
		}

		/// <summary>
		/// Generates the xySegments into segments and inputs them into the input road network
		/// </summary>
		/// <param name="arn"></param>
		/// <returns></returns>
		public ArbiterRoadNetwork GenerateSegments(ArbiterRoadNetwork arn)
		{
			foreach (SimpleSegment ss in segments)
			{
				// seg
				ArbiterSegmentId asi = new ArbiterSegmentId(int.Parse(ss.Id));
				ArbiterSegment asg = new ArbiterSegment(asi);
				arn.ArbiterSegments.Add(asi, asg);
				asg.RoadNetwork = arn;
				asg.SpeedLimits = new ArbiterSpeedLimit();
				asg.SpeedLimits.MaximumSpeed = 13.4112; // 30mph max speed

				// way1
				ArbiterWayId awi1 = new ArbiterWayId(1, asi);
				ArbiterWay aw1 = new ArbiterWay(awi1);
				aw1.Segment = asg;
				asg.Ways.Add(awi1, aw1);
				asg.Way1 = aw1;

				// way2
				ArbiterWayId awi2 = new ArbiterWayId(2, asi);
				ArbiterWay aw2 = new ArbiterWay(awi2);
				aw2.Segment = asg;
				asg.Ways.Add(awi2, aw2);
				asg.Way2 = aw2;

				// make lanes
				foreach (SimpleLane sl in ss.Lanes)
				{
					// lane
					ArbiterLaneId ali;
					ArbiterLane al;

					// get way of lane id
					if (ss.Way1Lanes.Contains(sl))
					{
						ali = new ArbiterLaneId(GenerationTools.GetId(sl.Id)[1], awi1);
						al = new ArbiterLane(ali);
						aw1.Lanes.Add(ali, al);
						al.Way = aw1;
					}
					else
					{
						ali = new ArbiterLaneId(GenerationTools.GetId(sl.Id)[1], awi2);
						al = new ArbiterLane(ali);
						aw2.Lanes.Add(ali, al);
						al.Way = aw2;
					}

					// add to display
					arn.DisplayObjects.Add(al);

					// width
					al.Width = sl.LaneWidth == 0 ? TahoeParams.T * 2.0 : sl.LaneWidth * 0.3048;

					if(sl.LaneWidth == 0)
						Console.WriteLine("lane: " + ali.ToString() + " contains no lane width, setting to 4m");

					// lane boundaries
					al.BoundaryLeft = this.GenerateLaneBoundary(sl.LeftBound);
					al.BoundaryRight = this.GenerateLaneBoundary(sl.RightBound);

					// add lane to seg
					asg.Lanes.Add(ali, al);

					// waypoints
					List<ArbiterWaypoint> waypointList = new List<ArbiterWaypoint>();

					// generate waypoints
					foreach (SimpleWaypoint sw in sl.Waypoints)
					{
						// waypoint
						ArbiterWaypointId awi = new ArbiterWaypointId(GenerationTools.GetId(sw.ID)[2], ali);
						ArbiterWaypoint aw = new ArbiterWaypoint(sw.Position, awi);
						aw.Lane = al;

						// stop
						if (sl.Stops.Contains(sw.ID))
						{
							aw.IsStop = true;
						}

						// checkpoint
						foreach (SimpleCheckpoint sc in sl.Checkpoints)
						{
							if (sw.ID == sc.WaypointId)
							{
								aw.IsCheckpoint = true;
								aw.CheckpointId = int.Parse(sc.CheckpointId);
								arn.Checkpoints.Add(aw.CheckpointId, aw);
							}
						}

						// add
						asg.Waypoints.Add(awi, aw);
						arn.ArbiterWaypoints.Add(awi, aw);
						al.Waypoints.Add(awi, aw);
						waypointList.Add(aw);
						arn.DisplayObjects.Add(aw);
						arn.LegacyWaypointLookup.Add(sw.ID, aw);
					}

					al.WaypointList = waypointList;

					// lane partitions
					List<ArbiterLanePartition> alps = new List<ArbiterLanePartition>();
					al.Partitions = alps;

					// generate lane partitions
					for (int i = 0; i < waypointList.Count-1; i++)
					{
						// create lane partition
						ArbiterLanePartitionId alpi = new ArbiterLanePartitionId(waypointList[i].WaypointId, waypointList[i + 1].WaypointId, ali);
						ArbiterLanePartition alp = new ArbiterLanePartition(alpi, waypointList[i], waypointList[i+1], asg);
						alp.Lane = al;
						waypointList[i].NextPartition = alp;
						waypointList[i + 1].PreviousPartition = alp;
						alps.Add(alp);
						arn.DisplayObjects.Add(alp);

						// crete initial user partition
						ArbiterUserPartitionId aupi = new ArbiterUserPartitionId(alp.PartitionId, waypointList[i].WaypointId, waypointList[i + 1].WaypointId);
						ArbiterUserPartition aup = new ArbiterUserPartition(aupi, alp, waypointList[i], waypointList[i + 1]);
						List<ArbiterUserPartition> aups = new List<ArbiterUserPartition>();
						aups.Add(aup);
						alp.UserPartitions = aups;
						alp.SetDefaultSparsePolygon();
						arn.DisplayObjects.Add(aup);
					}

					// path segments of lane		
					List<IPathSegment> ips = new List<IPathSegment>();
					List<Coordinates> pathSegments = new List<Coordinates>();
					pathSegments.Add(alps[0].Initial.Position); 				
					
					// loop 
					foreach(ArbiterLanePartition alPar in alps)
					{
						ips.Add(new LinePathSegment(alPar.Initial.Position, alPar.Final.Position));
						// make new segment
						pathSegments.Add(alPar.Final.Position);
					}

					// generate lane partition path
					LinePath partitionPath = new LinePath(pathSegments);
					al.SetLanePath(partitionPath);
					al.PartitionPath = new Path(ips, CoordinateMode.AbsoluteProjected);

					// safeto zones
					foreach (ArbiterWaypoint aw in al.Waypoints.Values)
					{
						if(aw.IsStop)
						{
							LinePath.PointOnPath end = al.GetClosestPoint(aw.Position);
							double dist = -30;
							LinePath.PointOnPath begin = al.LanePath().AdvancePoint(end, ref dist);
							if (dist != 0)
							{	
								begin = al.LanePath().StartPoint;
							}
							ArbiterSafetyZone asz = new ArbiterSafetyZone(al, end, begin);
							asz.isExit = true;
							asz.Exit = aw;
							al.SafetyZones.Add(asz);
							arn.DisplayObjects.Add(asz);
							arn.ArbiterSafetyZones.Add(asz);
						}
					}
				}
			}

			return arn;
		}

		/// <summary>
		/// Gets lane boundary
		/// </summary>
		/// <param name="bt"></param>
		/// <returns></returns>
		private ArbiterLaneBoundary GenerateLaneBoundary(string bt)
		{
			if (bt == null)
				return ArbiterLaneBoundary.None;
			else if (bt == "")
				return ArbiterLaneBoundary.None;
			else if (bt == "solid_yellow")
				return ArbiterLaneBoundary.SolidYellow;
			else if (bt == "double_yellow")
				return ArbiterLaneBoundary.DoubleYellow;
			else if (bt == "solid_white")
				return ArbiterLaneBoundary.SolidWhite;
			else if (bt == "broken_white")
				return ArbiterLaneBoundary.BrokenWhite;
			else
				return ArbiterLaneBoundary.Unknown;
		}
	}
}
