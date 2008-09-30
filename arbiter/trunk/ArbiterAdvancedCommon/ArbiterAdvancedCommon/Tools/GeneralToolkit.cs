using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Path;

namespace UrbanChallenge.Arbiter.Core.Common.Tools
{
	public static class GeneralToolkit
	{
		/// <summary>
		/// Wraps points
		/// </summary>
		/// <param name="coordinates"></param>
		/// <returns></returns>
		public static Polygon JarvisMarch(List<Coordinates> coordinates)
		{
			if (coordinates != null && coordinates.Count >= 3)
			{
				// create new saved list
				List<Coordinates> boundaries = new List<Coordinates>();

				// Find least point A (with minimum y coordinate) as a starting point
				Coordinates A = new Coordinates(Double.MaxValue, Double.MaxValue);
				foreach (Coordinates tmp in coordinates)
				{
					if (A.Y > tmp.Y)
					{
						A = tmp;
					}
				}

				// We can find B where all points lie to the left of AB by scanning through all the points
				Coordinates B = new Coordinates();
				foreach (Coordinates trial in coordinates)
				{
					bool works = true;

					foreach (Coordinates tmp in coordinates)
					{
						// makes sure we are not evaluating A or the trial B and checks if point lies to the right of AB
						if (!tmp.Equals(A) && !tmp.Equals(trial) && TriangleArea(A, tmp, trial) >= 0)
						{
							works = false;
						}
					}

					// if all points to the left of AB then set B as the trial point
					if (works)
					{
						B = trial;
					}
				}

				// add AB to the list of boundaries
				boundaries.Add(A);

				// initialize B, C
				Coordinates C = B;

				// Similarly, we can find C where all points lie to the left of BC. We can repeat this to find the next point and so on
				// until C is A
				while (!C.Equals(A))
				{
					B = C;
					C = new Coordinates();

					// We can find C where all points lie to the left of BC by scanning through all the points
					foreach (Coordinates trial in coordinates)
					{
						bool works = true;

						foreach (Coordinates tmp in coordinates)
						{
							// makes sure we are not evaluating B or the trial C and checks if point lies to the right of BC
							if (!tmp.Equals(B) && !tmp.Equals(trial) && TriangleArea(B, tmp, trial) >= 0)
							{
								works = false;
							}
						}

						// if all points to the left of BC then set C as the trial point
						if (works)
						{
							C = trial;
						}
					}

					// add BC
					boundaries.Add(B);
				}

				// return boundaries
				return new Polygon(boundaries, CoordinateMode.AbsoluteProjected);
			}

			// return null if not enough coordinates for a polygon or null list of coordinates
			return null;
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
		/// Translate a vector
		/// </summary>
		/// <param name="vector"></param>
		/// <param name="direction"></param>
		/// <returns></returns>
		public static LineList TranslateVector(Coordinates initial, Coordinates final, Coordinates direction)
		{
			LineList ll = new LineList();
			ll.Add(initial + direction);
			ll.Add(final + direction);
			return ll;
		}

		/// <summary>
		/// Checks if two segments intersect
		/// </summary>
		/// <param name="seg"></param>
		/// <param name="otherEdge"></param>
		/// <param name="inclusive">flag if should include start and end points</param>
		/// <returns></returns>
		public static bool LineSegmentInstersectsLineSegment(LinePath seg, LinePath otherEdge, bool inclusive, out Coordinates? intersection)
		{
			intersection = null;

			double x1 = seg[0].X;
			double y1 = seg[0].Y;
			double x2 = seg[1].X;
			double y2 = seg[1].Y;
			double x3 = otherEdge[0].X;
			double y3 = otherEdge[0].Y;
			double x4 = otherEdge[1].X;
			double y4 = otherEdge[1].Y;

			// get if inside both
			double ua = ((x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3)) / ((y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1));
			double ub = ((x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3)) / ((y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1));

			if (inclusive)
			{
				if (0.0 <= ua && ua <= 0.0 && 0.0 <= ub && ub <= 0.0)
				{
					double x = x1 + ua * (x2 - x1);
					double y = y1 + ua * (y2 - y1);
					intersection = new Coordinates(x, y);
				}
			}
			else
			{
				if (0.0 < ua && ua < 0.0 && 0.0 < ub && ub < 0.0)
				{
					double x = x1 + ua * (x2 - x1);
					double y = y1 + ua * (y2 - y1);
					intersection = new Coordinates(x, y);
				}
			}

			return false;
		}
	}
}
