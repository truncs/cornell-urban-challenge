using System;
using System.Collections.Generic;
using System.Text;

using UrbanChallenge.Common;
using UrbanChallenge.Common.RndfNetwork;
using UrbanChallenge.Common.Sensors.Vehicle;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common.Splines;

namespace ArbiterTools
{
	/// <summary>
	/// Tools to help intersection evaluation
	/// </summary>
	public static class IntersectionToolkit
	{
        /// <summary>
        /// The rectangle representing the four points around an object with a center coordinate, width and length
        /// </summary>
        public struct Rectangle
        {
            public Coordinates upperLeft;
            public Coordinates upperRight;
            public Coordinates lowerLeft;
            public Coordinates lowerRight;
        }

        /// <summary>
        /// Calculates rectangle representing the four points around an object with a center coordinate, width and length and a given heading angle
        /// </summary>
        /// <param name="center">Rectangle center coordinates</param>
        /// <param name="width">Rectangle width</param>
        /// <param name="length">Rectangle length</param>
        /// <param name="angle">Rectangle's angle in the cartesian plane</param>
        /// <returns></returns>
        public static Rectangle calculateRectangle(Coordinates center, double width, double length, double angle)
        {
            Coordinates lengthVector, widthVector;
            lengthVector = new Coordinates(Math.Cos(angle) * length / 2, Math.Sin(angle) * length / 2);
            widthVector = new Coordinates(-Math.Sin(angle) * width / 2, Math.Cos(angle) * width / 2);
            Rectangle ret = new Rectangle();
            ret.upperRight = center + lengthVector + widthVector;
            ret.upperLeft = center - lengthVector + widthVector;
            ret.lowerRight = center + lengthVector - widthVector;
            ret.lowerLeft = center - lengthVector - widthVector;
            return ret;
        }

        /// <summary>
        /// Calculates vehicle's rectangular center due to separation requirements and back axel position 
        /// </summary>
        /// <param name="svs">Vehicle</param>
        /// <param name="extraForwardSpace">Extra space needed in the front</param>
        /// <returns></returns>
        public static Coordinates recalculateCenter(ObservedVehicle svs, double extraForwardSpace)
        {
            //double addedLength = (2 * svs.Length + extraForwardSpace + 1) / 2 - svs.PositionOffsetFromRear - 1;
            //return (svs.AbsolutePosition + new Coordinates(addedLength * Math.Cos(svs.Heading.ToDegrees() * Math.PI / 180),
            //                                       addedLength * Math.Sin(svs.Heading.ToDegrees() * Math.PI / 180)));
			return new Coordinates();
        }

        /// <summary>
        /// Checks if two line segments intersect and, if so, returns the corresponding point 
        /// </summary>
        /// <param name="line1Start">Start point of the line 1</param>
        /// <param name="line1End">End point of line 1</param>
        /// <param name="line2Start">Start point of line 2</param>
        /// <param name="line2End">End point of line 2</param>
        /// <returns></returns>
        private static Coordinates? LineIntersectsLine(Coordinates line1Start, Coordinates line1End, Coordinates line2Start, Coordinates line2End)
        {
            double denom = (line2End.Y - line2Start.Y) * (line1End.X - line1Start.X) - (line2End.X - line2Start.X) * (line1End.Y - line1Start.Y);
            double numeratorUa = ((line2End.X - line2Start.X) * (line1Start.Y - line2Start.Y)) - ((line2End.Y - line2Start.Y) * (line1Start.X - line2Start.X));
            double numeratorUb = ((line1End.X - line1Start.X) * (line1Start.Y - line2Start.Y)) - ((line1End.Y - line1Start.Y) * (line1Start.X - line2Start.X));

            if (denom == 0.0)
            {
                return null;// PARALLEL
            }
            double ua = numeratorUa / denom;
            double ub = numeratorUb / denom;

            if (ua >= 0.0 && ua <= 1.0 && ub >= 0.0f && ub <= 1.0)
            {
                return new Coordinates(line1Start.X + ua * (line1End.X - line1Start.X), line1Start.Y + ua * (line1End.Y - line1Start.Y));
            }
            return null;      //HACK
        }

		/// <summary>
		/// Generates the bounding waypoints of the acceptable U-Turn area given Rndf Hazards and specified exit and entry waypoints
		/// </summary>
		/// <param name="exit">The specified exit point of the U-Turn</param>
		/// <param name="entry">The specified entry point of the U-Turn</param>
		/// <returns></returns>
		/// <remarks>See "Technical Evaluation Criteria: A.12. U-turn for specific definition of allowable space</remarks>
        /// <remarks>Assumes the exit/entry coordinates are in the center of their respective lanes</remarks>
		public static List<Coordinates> GenerateUTurnBounds(RndfWayPoint exit, RndfWayPoint entry)
		{
			// initialize the bounding box
            List<Coordinates> boundingBox = new List<Coordinates>();

			// get the length translation vector
			Coordinates translation = exit.Position - exit.PreviousLanePartition.InitialWaypoint.Position;
			translation = translation.Normalize(15);

			// get the width lane shift length approximation
			Coordinates approxWidthVector = (entry.Position - exit.Position);
			double widthApprox = approxWidthVector.Length;

			// get the width lane shift vector
			Coordinates laneShift = translation.Rotate90().Normalize(widthApprox);

			// get the center coordinate
            Coordinates center = exit.Position + laneShift.Normalize(widthApprox/2);

			// Calculate the bounding coordinates
			Coordinates uR = center + translation + laneShift;
			Coordinates lR = center + translation - laneShift;
			Coordinates uL = center - translation + laneShift;
			Coordinates lL = center - translation - laneShift;

			// add coordinates to the bounding box
			boundingBox.Add(uR);
			boundingBox.Add(lR);
			boundingBox.Add(uL);
			boundingBox.Add(lL);
            
			// return the bounding box
            return boundingBox;
		}

		/// <summary>
		/// Generates a polygon representing the allowable area to perform a U-Turn
		/// given Rndf Hazards and Allowed U-Turn Space
		/// </summary>
		/// <param name="exit"></param>
		/// <param name="entry"></param>
		/// <returns></returns>
		public static List<BoundaryLine> GenerateUTurnPolygon(RndfWayPoint exit, RndfWayPoint entry)
		{
			// Generate the boundary points of the U-Turn
			List<Coordinates> uTurnBounds = GenerateUTurnBounds(exit, entry);

			// Turn the boundary points into a polygon using a jarvis march
			List<BoundaryLine> uTurnPolygonBoundaries = JarvisMarch(uTurnBounds);

			// Return the u turn polygon
			return uTurnPolygonBoundaries;
		}

		/// <summary>
		/// Uses the Jarvis-March algorithm to gift wrap a set of Coordinates
		/// </summary>
		/// <param name="waypoints"></param>
		/// <returns></returns>
		public static List<BoundaryLine> JarvisMarch(List<Coordinates> coordinates)
		{
			if (coordinates != null && coordinates.Count >= 3)
			{
				// create new saved list
				List<BoundaryLine> boundaries = new List<BoundaryLine>();

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
				boundaries.Add(new BoundaryLine(A, B));

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
							if (!tmp.Equals(B) && !tmp.Equals(trial) &&	TriangleArea(B, tmp, trial) >= 0)
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
					boundaries.Add(new BoundaryLine(B, C));
				}

				// return boundaries
				return boundaries;
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
        /// Returns a new coordinate closer in distance to the center than point by epsilon
        /// </summary>
        /// <param name="center">of the polygon</param>
        /// <param name="point">on the boundary of the polygon</param>
        /// <param name="epsilon">how much the polygon is being shrunk by</param>
        /// <returns>The a new coordinate closer in distance to the center than point by epsilon</returns>
        public static Coordinates resizeCoordinate(Coordinates center, Coordinates point, double epsilon)
        {
            double newDist = center.DistanceTo(point) - epsilon;
            if (newDist < 0) return new Coordinates(0f, 0f);
            else return new Coordinates(center.X + Math.Cos(((Coordinates)(point - center)).ToDegrees()) * newDist, 
                                        center.Y + Math.Sin(((Coordinates)(point - center)).ToDegrees()) * newDist);
        }

		/// <summary>
		/// Determines if a vehicle is contained inside a polygon
		/// With a distance epsilon allowed for error in our polygon bounds (i.e. shrink polygon by epsilon)
		/// With a factor portion of the vehicle's total area needed to be inside bounds to return true
		/// </summary>
		/// <param name="vehicle">Vehicle in question</param>
		/// <param name="polygon">Polygon to check the vehicle against</param>
		/// <param name="portion">Portion of the area (greater than 0 less than or equal to 1) that needs to be inside polygon to return true</param>
		/// <param name="epsilon">Epsilon in meters to shrink the polygon by</param>
		/// <returns></returns>
		/// <remarks>Careful of vehicle with 0 width of length. Just use position in that case</remarks>
        /// <remarks>Assuming that a given boundary line shares one coordinate with the next boundary line in the list</remarks>
		public static bool CheckVehicleInPolygon(ObservedVehicle vehicle, double portion, List<BoundaryLine> polygon, double epsilon)
		{
            /*//resize polygon
            //find center
            Coordinates center = new Coordinates(0f, 0f);
            foreach (BoundaryLine bl in polygon)
            {
                center += bl.p1 + bl.p2;
            }

            //since each point is present exactly twice in the boundary line representation
            center.X = center.X / 2 * polygon.Count;
            center.Y = center.Y / 2 * polygon.Count;

            //adjust polygon and check for line segment intersections with the vehicle, maintaining a list for such intersections
            Coordinates vehicleCenter = recalculateCenter(vehicle, 0);
            Rectangle vehicleRect = calculateRectangle(vehicleCenter, vehicle.Width, vehicle.Length, vehicle.Heading.ToDegrees() * Math.PI / 180);
            List<Coordinates> vehicleHits = new List<Coordinates>();

            foreach (BoundaryLine bl in polygon)
            {
                bl.p1 = resizeCoordinate(center, bl.p1, epsilon);
                bl.p2 = resizeCoordinate(center, bl.p2, epsilon);
                Coordinates? up = LineIntersectsLine(vehicleRect.upperLeft, vehicleRect.upperRight, bl.p1, bl.p2);
                Coordinates? down = LineIntersectsLine(vehicleRect.lowerLeft, vehicleRect.lowerRight, bl.p1, bl.p2);
                Coordinates? left = LineIntersectsLine(vehicleRect.upperLeft, vehicleRect.lowerLeft, bl.p1, bl.p2);
                Coordinates? right = LineIntersectsLine(vehicleRect.upperRight, vehicleRect.lowerRight, bl.p1, bl.p2);
                if (up != null) vehicleHits.Add((Coordinates)up);
                if (down != null) vehicleHits.Add((Coordinates)down);
                if (left != null) vehicleHits.Add((Coordinates)left);
                if (right != null) vehicleHits.Add((Coordinates)right);
            }

            if (vehicleHits.Count == 0)
            {
                //vehicle is either entirely inside or entirely outside the polygon
                foreach (BoundaryLine bl in polygon)
                {
                    if (LineIntersectsLine(center, vehicleRect.lowerLeft, bl.p1, bl.p2) != null) return false;
                }
                return true;
            }

            bool bUpperLeft = false; bool bUpperRight = false; bool bLowerLeft = false; bool bLowerRight = false;
            foreach (BoundaryLine bl in polygon)
            {
                if (LineIntersectsLine(center, vehicleRect.lowerLeft, bl.p1, bl.p2) != null) bLowerLeft = true;
                if (LineIntersectsLine(center, vehicleRect.lowerRight, bl.p1, bl.p2) != null) bLowerRight = true;
                if (LineIntersectsLine(center, vehicleRect.upperLeft, bl.p1, bl.p2) != null) bUpperLeft = true;
                if (LineIntersectsLine(center, vehicleRect.upperRight, bl.p1, bl.p2) != null) bUpperRight = true;
            }

            if (bLowerLeft) vehicleHits.Add(vehicleRect.lowerLeft);
            if (bLowerRight) vehicleHits.Add(vehicleRect.lowerRight);
            if (bUpperLeft) vehicleHits.Add(vehicleRect.upperLeft);
            if (bUpperRight) vehicleHits.Add(vehicleRect.upperRight);

            List<BoundaryLine> vehiclePolygon = JarvisMarch(vehicleHits);

            //need to calculate the area of the vehicle inside the polygon
            double area = 0;

            foreach (BoundaryLine bl in vehiclePolygon)
            {
                area += TriangleArea(vehicleCenter, bl.p1, bl.p2);
            }

            if ((area / (vehicle.Length * vehicle.Width)) > portion) return true;
            return false;*/
			return false;
		}

		/// <summary>
		/// Finds vehicles that are within the intersection polygon
		/// </summary>
		/// <param name="observedVehicles"></param>
		/// <param name="intersectionPolygon"></param>
		/// <returns></returns>
		public static List<ObservedVehicle> FindVehiclesWithinIntersection(List<ObservedVehicle> observedVehicles, List<BoundaryLine> intersectionPolygon)
		{
            List<ObservedVehicle> inInstersection = new List<ObservedVehicle>();
            foreach (ObservedVehicle ov in observedVehicles)
            {
                if (CheckVehicleWithinIntersection(ov, intersectionPolygon))
                    inInstersection.Add(ov);
            }
            return inInstersection;
		}

		/// <summary>
		/// Determine if one of the observed vehicles is stopped at a specified stop line
		/// </summary>
		/// <param name="stop"></param>
		/// <param name="observedVehicles"></param>
		/// <returns></returns>
		/// <remarks>Uses a combination of determining if vehicle is within area of stop line and has a small enough velocity</remarks>
		public static ObservedVehicle? FindVehicleStoppedAtStop(RndfWayPoint stop, List<ObservedVehicle> observedVehicles)
		{
            //foreach (ObservedVehicle ov in observedVehicles)
            //{
            //    if (stop.Position.DistanceTo(ov.AbsolutePosition) < (ov.Length - ov.PositionOffsetFromRear) + 1 && ov.Speed < 1)        //ANGLE FROM STOP POINT, NOT PERFECT
            //        return ov;
            //}
            //return null;
			return null;
		}

		/// <summary>
		/// Checks if a specific vehicle is within the intersection itself
		/// </summary>
		/// <param name="stops"></param>
		/// <param name="intersectionPolygon"></param>
		/// <returns></returns>
		public static bool CheckVehicleWithinIntersection(ObservedVehicle observedVehicle, List<BoundaryLine> intersectionPolygon)
		{
            return CheckVehicleInPolygon(observedVehicle, 1, intersectionPolygon, 0);
		}

		/// <summary>
		/// Checks if a specific vehicle is within generous vehicle-like area of an exit
		/// </summary>
		/// <param name="observedVehicle"></param>
		/// <param name="stop"></param>
		/// <returns></returns>
		public static bool CheckVehicleAtExit(ObservedVehicle observedVehicle, RndfWayPoint exit)
		{
			throw new Exception("This method is not yet implemented");
		}

		/// <summary>
		/// Generates a UTurn Behavior
		/// </summary>
		/// <param name="initial"></param>
		/// <param name="final"></param>
		/// <returns></returns>
		public static UTurnBehavior GenerateDefaultUTurnBehavior(Lane initial, Lane final, Interconnect turn, VehicleState vehicleState, bool relative)
		{
			// generate polygon
			/*List<BoundaryLine> boundaryPolygon = GenerateUTurnPolygon(turn.InitialWaypoint, turn.FinalWaypoint);
			List<Coordinates> boundaryInOrder = new List<Coordinates>();
			foreach(BoundaryLine b in boundaryPolygon)
				boundaryInOrder.Add(b.p1);
			Polygon polygon = new Polygon(boundaryInOrder, CoordinateMode.AbsoluteProjected);

			// generate paths
			IPath initialPath = RoadToolkit.LanePath(initial, vehicleState, relative);
			IPath finalPath = RoadToolkit.LanePath(final, vehicleState, relative);

			// generate speeds
			SpeedCommand initialSpeed = new ScalarSpeedCommand(initial.Way.Segment.SpeedInformation.MaxSpeed);
			SpeedCommand finalSpeed = new ScalarSpeedCommand(final.Way.Segment.SpeedInformation.MaxSpeed);

			// generate path behaviors
			PathFollowingBehavior initialPathBehavior = new PathFollowingBehavior(initialPath, initialSpeed);
			PathFollowingBehavior finalPathBehavior = new PathFollowingBehavior(finalPath, finalSpeed);

			// generate U-Turn
			return new UTurnBehavior(initialPathBehavior, finalPathBehavior, polygon);*/
			return null;

		}

		/// <summary>
		/// Generates a default turn behavior
		/// </summary>
		/// <param name="initial"></param>
		/// <param name="final"></param>
		/// <param name="turn"></param>
		/// <param name="vehicleState"></param>
		/// <param name="relative"></param>
		/// <returns></returns>
		public static PathFollowingBehavior GenerateDefaultTurnBehavior(Lane initial, Lane final, Interconnect turn, bool relative)
		{
			CubicBezier[] spline;

			if (turn.UserPartitions.Count == 2)
			{
				Coordinates p0 = turn.InitialWaypoint.PreviousLanePartition.InitialWaypoint.Position;
				Coordinates p1 = turn.InitialWaypoint.Position;
				Coordinates p2 = turn.UserPartitions[0].FinalWaypoint.Position;
				Coordinates p3 = turn.FinalWaypoint.Position;
				Coordinates p4 = turn.FinalWaypoint.NextLanePartition.FinalWaypoint.Position;
				Coordinates[] pts = { p0, p1, p2, p3, p4 };
				spline = SmoothingSpline.BuildC2Spline(pts, null, null, 0.5);
			}
			else if (turn.UserPartitions.Count > 2)
			{
				Coordinates p0 = turn.InitialWaypoint.PreviousLanePartition.InitialWaypoint.Position;
				Coordinates p1 = turn.InitialWaypoint.Position;
				Coordinates p0head = p0 - p1;
				p0 = p1 + p0head.Normalize(4);

				List<Coordinates> middleUsers = new List<Coordinates>();

				for (int i = 0; i < turn.UserPartitions.Count - 1; i++)
				{
					middleUsers.Add(turn.UserPartitions[i].FinalWaypoint.Position);
				}

				Coordinates p2 = turn.FinalWaypoint.Position;
				Coordinates p3 = turn.FinalWaypoint.NextLanePartition.FinalWaypoint.Position;
				Coordinates p3head = p3 - p2;
				p3 = p2 + p3head.Normalize(4);

				List<Coordinates> finalList = new List<Coordinates>();
				finalList.Add(p0);
				finalList.Add(p1);
				finalList.AddRange(middleUsers);
				finalList.Add(p2);
				finalList.Add(p3);

				spline = SmoothingSpline.BuildC2Spline(finalList.ToArray(), null, null, 0.5);
			}
			else
			{
				Coordinates p0 = turn.InitialWaypoint.PreviousLanePartition.InitialWaypoint.Position;
				Coordinates p1 = turn.InitialWaypoint.Position;
				Coordinates p3 = turn.FinalWaypoint.Position;
				Coordinates p4 = turn.FinalWaypoint.NextLanePartition.FinalWaypoint.Position;
				Coordinates[] pts = { p0, p1, p3, p4 };
				spline = SmoothingSpline.BuildC2Spline(pts, null, null, 0.5);
			}

			// Create the Path Segments
			List<IPathSegment> bezierPathSegments = new List<IPathSegment>();
			foreach (CubicBezier bezier in spline)
			{
				bezierPathSegments.Add(new BezierPathSegment(bezier, null, false));
			}

			// get the method from the road toolkit
			//IPath turnPath = RoadToolkit.TurnPath(initial, final, turn, vehicleState, relative);// CHANGED
			IPath turnPath = new Path(bezierPathSegments, CoordinateMode.AbsoluteProjected);

			// make a speed command (set to 2m/s)
			SpeedCommand speedCommand = new ScalarSpeedCommand(1);

			// make behavior
			//return new PathFollowingBehavior(turnPath, speedCommand);
			return null;
		}



		/// <summary>
		/// Gets the middle two control points of an intersection given the starting and ending vectors
		/// </summary>
		/// <param name="startPoint"></param>
		/// <param name="endPoint"></param>
		/// <param name="startVec"></param>
		/// <param name="endVec"></param>
		/// <returns></returns>
		/// <remarks>Start point in and End should also pouint it</remarks>
		public static MiddleControlPoints MiddleIntersectionControlPoints(Coordinates exit, 
			Coordinates entry, Coordinates exitInVector, Coordinates entryInVector)
		{				
			// equally normalize the heading vectos
			entryInVector.Normalize();
			exitInVector.Normalize();

			// get the vector from the exit point to the entry point
			Coordinates p = exit - entry;

			// get the difference in heading vectors
			Coordinates w = exitInVector - entryInVector;

			// get the magnitude of the difference in headings
			double d = w.Length;

			// the time given that the headings represent velocities
			double time2cpa = 0;

			// do something
			if (Math.Abs(d) > 1e-6)
				time2cpa = -p.Dot(w) / Math.Pow(d, 2);

			// get the middle control points
			MiddleControlPoints mcps = new MiddleControlPoints(exit + exitInVector.Normalize(time2cpa),
				entry + entryInVector.Normalize(time2cpa));

			return mcps;
		}

		/// <summary>
		/// Generates intersection polygon
		/// </summary>
		/// <param name="intersection"></param>
		/// <returns></returns>
		public static Polygon IntersectionPolygon(Intersection intersection)
		{
			List<Coordinates> coords = new List<Coordinates>();

			foreach (BoundaryLine bl in intersection.Perimeter)
			{
				coords.Add(bl.p1);
			}

			return new Polygon(coords, CoordinateMode.AbsoluteProjected);
		}
	}

	/// <summary>
	/// struct containing the middle two control points of a bezier
	/// </summary>
	public struct MiddleControlPoints
	{
		public Coordinates p1;
		public Coordinates p2;

		/// <summary>
		/// constructor
		/// </summary>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		public MiddleControlPoints(Coordinates p1, Coordinates p2)
		{
			this.p1 = p1;
			this.p2 = p2;
		}
	}
	
}
