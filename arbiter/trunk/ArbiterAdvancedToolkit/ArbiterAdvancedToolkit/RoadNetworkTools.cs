using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common.Shapes;

namespace UrbanChallenge.Arbiter.Core.Tools
{
	/// <summary>
	/// Tools for itneracting with the road network
	/// </summary>
	public static class RoadNetworkTools
	{
		/// <summary>
		/// Gets closest area in the road network
		/// </summary>
		/// <param name="c"></param>
		/// <returns></returns>
		public static IAreaSubtypeId GetClosest(ArbiterRoadNetwork arn, Coordinates c)
		{
			foreach (ArbiterZone az in arn.ArbiterZones.Values)
			{
				if (az.Perimeter.PerimeterPolygon.IsInside(c))
					return az.Perimeter.PerimeterId;
			}

			double curDist = double.MaxValue;
			IAreaSubtypeId curId = null;

			foreach(ArbiterSegment asg in arn.ArbiterSegments.Values)
			{
				foreach (ArbiterLane al in asg.Lanes.Values)
				{
					PointOnPath? closest = null;
					
					if(al.IsInside(c))
						closest = al.PartitionPath.GetClosest(c);

					if (closest.HasValue)
					{
						double dist = closest.Value.pt.DistanceTo(c);

						if (dist < curDist)
						{
							curDist = dist;
							curId = al.LaneId;
						}
					}
				}
			}
			/*
			foreach (ArbiterInterconnect ai in arn.ArbiterInterconnects.Values)
			{
				PointOnPath pop = ai.InterconnectPath.GetClosest(c);
				double dist = pop.pt.DistanceTo(c);
				if (dist < curDist && curDist - dist > 5)
				{
					curDist = dist;
					curId = ai.InterconnectId;
				}
			}*/

			return curId;
		}

		/// <summary>
		/// Gets the next stop
		/// </summary>
		/// <param name="al"></param>
		/// <param name="alp"></param>
		/// <param name="c"></param>
		/// <returns></returns>
		public static void NextStop(ArbiterLane al, ArbiterLanePartition alp, Coordinates c, out ArbiterWaypoint waypoint, out double distance)
		{
			ArbiterWaypoint current = alp.Final;

			while (current != null)
			{
				if (current.IsStop)
				{
					distance = al.PartitionPath.DistanceBetween(
						al.PartitionPath.GetClosest(c),
						al.PartitionPath.GetClosest(current.Position));

					waypoint = current;
					return;
				}
				else
				{
					current = current.NextPartition != null ? current.NextPartition.Final : null;
				}
			}

			waypoint = null;
			distance = Double.MaxValue;
		}
/*
		/// <summary>
		/// Generates the bounding path along a lane starting from a specific waypoint and going a ceratin distance along the lane
		/// </summary>
		/// <param name="aw"></param>
		/// <param name="distance"></param>
		/// <param name="lanePath"></param>
		/// <param name="leftBound"></param>
		/// <param name="rightBound"></param>
		public static void LanePath(ArbiterWaypoint aw, double distance, out Path lanePath, out LineList leftBound, out LineList rightBound)
		{
			// initialize holders
			List<Coordinates> pathCoords = new List<Coordinates>();
			leftBound = new LineList();
			LineList rightBound = new LineList();

			// dtermine how the road looks around



			// first get the path

			// create lsit of coordinates to be used for hte line list on each side during generation

		}*/
	}
}
