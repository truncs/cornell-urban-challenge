using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common.RndfNetwork;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Path;

namespace ArbiterTools
{
	/// <summary>
	/// General Tools to Help Evaluation
	/// </summary>
	public class GeneralToolkit
	{
		/// <summary>
		/// Transfers an mdf from mph to mps
		/// </summary>
		/// <param name="mdf"></param>
		/// <returns></returns>
		public static Mdf TransferMdfToMs(Mdf mdf)
		{
			// create converted speed list
			List<SpeedInformation> convertedSpeeds = new List<SpeedInformation>();

			// loop through speed limit
			foreach (SpeedInformation limit in mdf.SpeedLimits)
			{
				// create new speed information
				SpeedInformation speedInfo = new SpeedInformation();
				
				// transform them from mph to m/s
				speedInfo.MaxSpeed = limit.MaxSpeed * 0.44704;
				speedInfo.MinSpeed = limit.MinSpeed * 0.44704;

				// set id info
				speedInfo.SegmentID = limit.SegmentID;
			}

			// create updated mdf
			return new Mdf(mdf.Goals, convertedSpeeds);
		}

		/// <summary>
		/// Gets signed triangle area. 
		/// if the area is positive then the points occur in anti-clockwise order and P1 is to the left of the line P0P2
		/// </summary>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		/// <param name="p3">Test Point</param>
		/// <returns></returns>
		public static double TriangleArea(Coordinates p0, Coordinates p1, Coordinates p2)
		{
			double ans = 0.5 * (p0.X * (p1.Y - p2.Y) - p1.X * (p0.Y - p2.Y) + p2.X * (p0.Y - p1.Y));
			return ans;
		}

		/// <summary>
		/// Get the closest points on 2 paths
		/// </summary>
		/// <param name="path1"></param>
		/// <param name="path2"></param>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		public static void GetClosestPoints(Path path1, Path path2, out PointOnPath p1, out PointOnPath p2, out double distance)
		{
			PointOnPath closest1 = path1.StartPoint;
			PointOnPath closest2 = path2.StartPoint;
			double dist = Double.MaxValue;

			PointOnPath current = path1.StartPoint;
			double increment = 1;

			double tmpIncrement = 0;

			while (tmpIncrement == 0)
			{
				PointOnPath test = path2.GetClosest(current.pt);
				double tmpDist = test.pt.DistanceTo(current.pt);

				if (tmpDist < dist)
				{
					closest1 = current;
					closest2 = test;
					dist = tmpDist;
				}

				tmpIncrement = increment;
				current = path1.AdvancePoint(current, ref tmpIncrement);
			}

			p1 = closest1;
			p2 = closest2;
			distance = dist;
		}
	}
}
