using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.Vehicle;

namespace UrbanChallenge.Arbiter.Core.Common.Tools
{
	/// <summary>
	/// Tools for intersections
	/// </summary>
	public static class IntersectionToolkit
	{
		/// <summary>
		/// Generates the bounding waypoints of the acceptable U-Turn area given Rndf Hazards and specified exit and entry waypoints
		/// </summary>
		/// <returns></returns>
		public static Polygon uTurnBounds(Coordinates exit, ArbiterSegment segment)
		{
			// initialize the bounding box
			List<Coordinates> boundingBox = new List<Coordinates>();

			// put in coords for every available lane
			foreach (ArbiterLane al in segment.Lanes.Values)
			{
				PointOnPath? pop = null;

				if (!al.IsInside(exit))
				{
					ArbiterWaypoint aw = al.GetClosestWaypoint(exit, 10.0);
					if (aw != null)
						pop = al.PartitionPath.GetClosest(aw.Position);
				}
				else
					pop = al.PartitionPath.GetClosest(exit);
				
				if(pop != null)
				{
					ArbiterLanePartition alp = al.GetClosestPartition(exit);
					Coordinates vector = alp.Vector().Normalize(15);
					Coordinates back = pop.Value.pt - vector;
					vector = vector.Normalize(30);
					boundingBox.AddRange(InflatePartition(back, vector, alp.Lane.Width));
				}
			}

			// return the box
			return GeneralToolkit.JarvisMarch(boundingBox);
		}

		/// <summary>
		/// Generates the bounding waypoints of the acceptable U-Turn area given Rndf Hazards and specified exit and entry waypoints
		/// </summary>
		/// <returns></returns>
		public static Polygon uTurnBounds(VehicleState state, List<ArbiterLane> involved)
		{
			Coordinates exit = state.Front;

			// initialize the bounding box
			List<Coordinates> boundingBox = new List<Coordinates>();

			// put in coords for every available lane
			foreach (ArbiterLane al in involved)
			{
				PointOnPath? pop = null;

				if (!al.IsInside(exit))
				{
					ArbiterWaypoint aw = al.GetClosestWaypoint(exit, 10.0);
					if (aw != null)
						pop = al.PartitionPath.GetClosest(aw.Position);
				}
				else
					pop = al.PartitionPath.GetClosest(exit);

				if (pop != null)
				{
					ArbiterLanePartition alp = al.GetClosestPartition(exit);
					Coordinates vector = alp.Vector().Normalize(15);
					Coordinates back = pop.Value.pt - vector;
					vector = vector.Normalize(30);
					boundingBox.AddRange(InflatePartition(back, vector, alp.Lane.Width));
				}
			}

			// return the box
			return new Polygon(Polygon.GrahamScan(boundingBox));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="anchor"></param>
		/// <param name="vector"></param>
		/// <param name="width"></param>
		/// <returns></returns>
		private static List<Coordinates> InflatePartition(Coordinates anchor, Coordinates vector, double width)
		{
			List<Coordinates> coordList = new List<Coordinates>();
			Coordinates leftVec = vector.Rotate90().Normalize(width / 2.0);
			Coordinates rightVec = vector.RotateM90().Normalize(width / 2.0);
			coordList.Add(anchor + leftVec);
			coordList.Add(anchor + rightVec);
			coordList.Add(anchor + vector + leftVec);
			coordList.Add(anchor + vector + rightVec);
			return coordList;
		}

		/// <summary>
		/// Turn information
		/// </summary>
		/// <param name="entry"></param>
		/// <param name="finalPath"></param>
		/// <param name="leftBound"></param>
		/// <param name="rightBound"></param>
		public static void TurnInfo(ArbiterWaypoint entry, out LinePath finalPath, out LineList leftBound, out LineList rightBound)
		{
			if (entry.NextPartition != null)
			{
				double distance = entry.NextPartition.Length;

				// get left bound
				rightBound = GeneralToolkit.TranslateVector(entry.Position, entry.NextPartition.Final.Position,
					entry.NextPartition.Vector().Normalize(entry.Lane.Width / 2.0).RotateM90());

				// get right bound
				leftBound = GeneralToolkit.TranslateVector(entry.Position, entry.NextPartition.Final.Position,
					entry.NextPartition.Vector().Normalize(entry.Lane.Width / 2.0).Rotate90());

				ArbiterWaypoint current = entry.NextPartition.Final;
				while (current.NextPartition != null && distance < 50)
				{
					distance += current.NextPartition.Length;

					LineList rtTemp = GeneralToolkit.TranslateVector(current.Position, current.NextPartition.Final.Position,
					current.NextPartition.Vector().Normalize(current.Lane.Width / 2.0).RotateM90());
					rightBound.Add(rtTemp[rtTemp.Count - 1]);

					LineList ltTemp = GeneralToolkit.TranslateVector(current.Position, current.NextPartition.Final.Position,
					current.NextPartition.Vector().Normalize(current.Lane.Width / 2.0).Rotate90());
					leftBound.Add(ltTemp[ltTemp.Count - 1]);

					current = current.NextPartition.Final;
				}

				finalPath = entry.Lane.LanePath(entry, 50.0);
			}
			else
			{
				Coordinates final = entry.Position + entry.PreviousPartition.Vector().Normalize(TahoeParams.VL);
				finalPath = new LinePath(new Coordinates[] { entry.Position, final });
				LinePath lB = finalPath.ShiftLateral(entry.Lane.Width / 2.0);
				LinePath rB = finalPath.ShiftLateral(-entry.Lane.Width / 2.0);
				leftBound = new LineList(lB);
				rightBound = new LineList(rB);
			}
		}

		/// <summary>
		/// Turn information
		/// </summary>
		/// <param name="entry"></param>
		/// <param name="finalPath"></param>
		/// <param name="leftBound"></param>
		/// <param name="rightBound"></param>
		public static void ZoneTurnInfo(ArbiterInterconnect ai, ArbiterPerimeterWaypoint entry, out LinePath finalPath, out LineList leftBound, out LineList rightBound)
		{
			//Coordinates centerVec = entry.Perimeter.PerimeterPolygon.CalculateBoundingCircle().center - entry.Position;
			Coordinates centerVec = ai.InterconnectPath[1] - ai.InterconnectPath[0];
			centerVec = centerVec.Normalize(TahoeParams.VL);
			finalPath = new LinePath(new Coordinates[] { entry.Position, entry.Position + centerVec });

			leftBound = finalPath.ShiftLateral(TahoeParams.T * 2.0);
			rightBound = finalPath.ShiftLateral(-TahoeParams.T * 2.0);
		}
	}
}
