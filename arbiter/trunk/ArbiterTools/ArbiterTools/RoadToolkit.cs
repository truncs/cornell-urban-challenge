using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common.Sensors.Vehicle;
using UrbanChallenge.Common;
using UrbanChallenge.Common.RndfNetwork;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common.Splines;
using System.Collections;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Arbiter.ArbiterCommon;
using ArbiterTools.Data;
using ArbiterTools.Tools;
using System.Diagnostics;
using UrbanChallenge.Common.Mapack;

namespace ArbiterTools
{
	/// <summary>
	/// Tools to help road evaluation
	/// </summary>
	public static class RoadToolkit
	{
		/// <summary>
		/// Get the Rndf Relative location of a Coordinate to a given LaneID
		/// </summary>
		/// <param name="laneEstimate"></param>
		/// <param name="absolutePosition"></param>
		/// <returns></returns>
		/// <remarks>Algorithm already done, needs cleanup</remarks>
		public static RndfLocation RndfLocation(LaneID laneEstimate, Coordinates absolutePosition)
		{
			throw new Exception("This method has not yet been implemented!");
		}

		/// <summary>
		/// Gets the closest position to the coordinates on any lane in the rndf network
		/// </summary>
		/// <param name="coordinate"></param>
		/// <param name="rndfNetwork"></param>
		/// <returns></returns>
		public static LocationAnalysis ClosestRndfRelativeLanePartition(Coordinates coordinate, RndfNetwork rndfNetwork)
		{
			// use the point analysis tool
			return PointAnalysis.GetClosestLanePartition(coordinate, rndfNetwork);
		}

		/// <summary>
		/// Gets the closest position to the coordinates on a specific lane in the rndf network
		/// </summary>
		/// <param name="coordinate"></param>
		/// <param name="lane"></param>
		/// <param name="rndfNetwork"></param>
		/// <returns></returns>
		public static LocationAnalysis ClosestRndfRelativeParition(Coordinates coordinate, LaneID lane, RndfNetwork rndfNetwork)
		{
			// use point analysis tool
			return PointAnalysis.ClosestPartitionOnLane(coordinate, lane, rndfNetwork);
		}

		/// <summary>
		/// Transformas a path into a vehicle relative path
		/// </summary>
		/// <param name="spline"></param>
		/// <param name="position"></param>
		/// <param name="heading"></param>
		/// <returns></returns>
		public static IPath VehicleRelativePath(List<Coordinates> absolutePath, Coordinates position, Coordinates heading)
		{
			// 1. Transform the absolute path into a vehicle relative path w.r.t position
			List<Coordinates> translatedPath = new List<Coordinates>();
			foreach (Coordinates coordinate in absolutePath)
			{
				Coordinates translatedCoordinate = coordinate - position;
				translatedPath.Add(translatedCoordinate);
			}

			// 2. Rotate the translated path into a vehicle relative path w.r.t heading
			List<Coordinates> rotatedPath = new List<Coordinates>();
			Coordinates zeroAngle = new Coordinates(1,0);
			foreach (Coordinates coordinate in translatedPath)
			{
				Coordinates rotatedCoordinate = coordinate.Rotate(zeroAngle.ArcTan - heading.ArcTan);
				rotatedPath.Add(rotatedCoordinate);
			}

			// 3. Create cubic spline based upon the coordinates
			List<CubicBezier> spline = SplineC2FromPoints(rotatedPath);

			// 4. Create the Path Segments
			List<IPathSegment> bezierPathSegments = new List<IPathSegment>();
			foreach(CubicBezier bezier in spline)
			{
				bezierPathSegments.Add(new BezierPathSegment(bezier, null, false));
			}

			// 5. Create the path
			Path path = new Path(bezierPathSegments, CoordinateMode.VehicleRelative);
			path.CoordinateMode = CoordinateMode.VehicleRelative;

			// 6. Return
			return path;
		}

		/// <summary>
		/// Generates a C2 spline from a list of input points
		/// </summary>
		/// <param name="coordinates"></param>
		/// <returns></returns>
		public static List<CubicBezier> SplineC2FromPoints(List<Coordinates> coordinates)
		{
			// final list of beziers
			List<CubicBezier> spline = new List<CubicBezier>();

			// generate spline
			CubicBezier[] bez = SmoothingSpline.BuildC2Spline(coordinates.ToArray(), null, null, 0.5);

			// loop through individual beziers
			foreach (CubicBezier cb in bez)
			{
				// add to final spline
				spline.Add(cb);
			}

			// return final list of beziers
			return spline;
		}

		/// <summary>
		/// Generates a C2 spline from a set of points and input derivatives
		/// </summary>
		/// <param name="initialDirection"></param>
		/// <param name="intial"></param>
		/// <param name="final"></param>
		/// <param name="finalDirection"></param>
		/// <returns></returns>
		public static List<CubicBezier> SplineC2FromSegmentAndDerivatives(List<Coordinates> coordinates, Coordinates d0, Coordinates dn)
		{
			// final list of beziers
			List<CubicBezier> spline = new List<CubicBezier>();

			// generate spline
			CubicBezier[] bez = SmoothingSpline.BuildC2Spline(coordinates.ToArray(), d0, dn, 0.5);

			// loop through individual beziers
			foreach (CubicBezier cb in bez)
			{
				// add to final spline
				spline.Add(cb);
			}

			// return final list of beziers
			return spline;
		}

		/// <summary>
		/// Generates a C2 spline from a list of input points
		/// </summary>
		/// <param name="waypoints"></param>
		/// <returns></returns>
		public static List<CubicBezier> SPlineC2FromRndfWaypoints(List<RndfWayPoint> waypoints)
		{
			// list of coordinates of all the waypoints
			List<Coordinates> coordinates = new List<Coordinates>();

			// loop through the waypoints
			foreach (RndfWayPoint waypoint in waypoints)
			{
				// add coordiantes to list of coordiantes
				coordinates.Add(waypoint.Position);
			}

			// generate and return c2 spline from coordinates
			return SplineC2FromPoints(coordinates);
		}

		/// <summary>
		/// Gets the desired final speed of the vehicle  over a timestep
		/// </summary>
		/// <param name="v0"></param>
		/// <param name="vf"></param>
		/// <param name="distance"></param>
		/// <param name="vMax"></param>
		/// <param name="aMin"></param>
		/// <param name="aMax"></param>
		/// <param name="dt"></param>
		/// <returns></returns>
		public static double InferFinalSpeed(double v0, double vf, double distance, double vMax, double aMax, double dt)
		{
			// check if the distance in is positive
			if (distance > 0)
			{
				// get required distance to get to final velocity
				double requiredDistance = RequiredDistance(v0, vf, -aMax);
				//Debug.WriteLine("Required Distance: " + requiredDistance.ToString());
				//Debug.WriteLine("Distance: " + distance.ToString());
				//Debug.WriteLine("Current Speed: " + v0);
				// the test distance will use the velocity as a gain parameter
				double testDistance = requiredDistance + (v0*dt); // HACK: GAIN?

				double r = distance;
				double d = (vf*vf - vMax * vMax)/(2*-aMax);

				// check if the distance is around the desired distance
				if (distance <= testDistance)
				{
					return (((d - r) / d) * (vf - vMax) + vMax);
					/*// slowdown
					//double a = (vf * vf - v0 * v0) / (2.0 * distance);
					double a = -aMax;
					Debug.WriteLine("Calculated a: " + a.ToString());
					double v = v0 + a * dt;
					Debug.WriteLine("Final Speed: " + v.ToString());
					Debug.WriteLine("");
					return v;*/
				}
				else if (distance <= testDistance + 3)
					return v0;
				else
				{
					// operational scales slowly
					//Debug.WriteLine("VMax Speed Returned: " + vMax);
					//Debug.WriteLine("");
					return vMax;
				}

				
			}
			else
			{
				throw new Exception("Negative input distance into speed control");
			}
		}

		/// <summary>
		/// Gets the distance required to hit a certin velocity given an 
		/// </summary>
		/// <param name="v0"></param>
		/// <param name="vf"></param>
		/// <param name="aMax"></param>
		/// <returns></returns>
		public static double RequiredDistance(double v0, double vf, double a)
		{
			return ((vf * vf - v0 * v0) / (2.0 * a));
		}

		/// <summary>
		/// Function to calculate distance until tell the operational layer to take over and stop
		/// </summary>
		/// <param name="distanceToStop"></param>
		/// <returns></returns>
		public static double DistanceUntilOperationalStop(double distanceToStop)
		{
			return distanceToStop - ArbiterComponents.OperationalStopDistance;
		}

		/// <summary>
		/// Gets the next point at which we must stop
		/// </summary>
		/// <param name="rndf"></param>
		/// <param name="vehicleLocation"></param>
		/// <returns></returns>
		public static RndfWayPoint NextStopOrEnd(RndfNetwork rndf, RndfLocation vehicleLocation)
		{	
			// set current as final waypoint on current partition
			RndfWayPoint current = vehicleLocation.Partition.FinalWaypoint;

			// iterate over waypoints
			while (!current.IsStop && current.NextLanePartition != null)
			{
				// update current
				current = current.NextLanePartition.FinalWaypoint;
			}

			// return current as stop or end of lane
			return current;			
		}

		public static RndfWayPoint NextStop(RndfNetwork rndf, RndfLocation vehicleLocation)
		{
			// set current as final waypoint on current partition
			RndfWayPoint current = vehicleLocation.Partition.FinalWaypoint;

			// iterate over waypoints
			while (!current.IsStop && current.NextLanePartition != null)
			{
				// update current
				current = current.NextLanePartition.FinalWaypoint;
			}

			if (current.IsStop)
			{
				// return current as stop or end of lane
				return current;
			}
			else
			{
				return null;
			}
		}

		public static void NextStop(RndfNetwork rndf, RndfLocation vehicleLocation,
			out RndfWayPoint nextStop, out double distance)
		{
			// set current as final waypoint on current partition
			RndfWayPoint current = vehicleLocation.Partition.FinalWaypoint;

			// set initial distance
			distance = vehicleLocation.AbsolutePositionOnPartition.DistanceTo(current.Position);

			// iterate over waypoints
			while (!current.IsStop && current.NextLanePartition != null)
			{
				// update distance
				distance += current.Position.DistanceTo(current.NextLanePartition.FinalWaypoint.Position);

				// update current
				current = current.NextLanePartition.FinalWaypoint;
			}

			if (current.IsStop)
			{
				nextStop = current;

				// return current as stop or end of lane
				return;
			}
			else
			{
				nextStop = null;

				return;
			}
		}

		/// <summary>
		/// Gets a path for this lane relative to the vehicle (as if the vehicle was travelling to (1,0)
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="vehicleState"></param>
		/// <param name="vehicleRelative"></param>
		/// <returns></returns>
		public static IPath LanePath(Lane lane, VehicleState vehicleState, bool vehicleRelative)
		{
			// 1. list of absolute positions of lane coordinates
			List<Coordinates> absoluteCoordinates = new List<Coordinates>();

			// 2. get lane coordinates
			absoluteCoordinates.AddRange(GetLaneWaypointCoordinates(lane, true));

			// 3. Generate path
			return GeneratePathFromCoordinates(absoluteCoordinates, vehicleState.Position, vehicleState.Heading, vehicleRelative);
		}

		/// <summary>
		/// Gets a path for this lane relative to the vehicle (as if the vehicle was travelling to (1,0)
		/// But only upto a certain distance if the lane goes that far
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="vehicleState"></param>
		/// <param name="vehicleRelative"></param>
		/// <returns></returns>
		public static IPath LanePath(Lane lane, VehicleState vehicleState, double distance, bool forwards, bool vehicleRelative)
		{
			// 1. list of absolute positions of lane coordinates
			List<Coordinates> absoluteCoordinates = new List<Coordinates>();

			// 2. get lane coordinates
			absoluteCoordinates.AddRange(GetLaneWaypointCoordinates(lane, true, distance, forwards));

			// 3. Generate path
			return GeneratePathFromCoordinates(absoluteCoordinates, vehicleState.Position, vehicleState.Heading, vehicleRelative);
		}

		/// <summary>
		/// Generates a path around a turn
		/// </summary>
		/// <param name="initial"></param>
		/// <param name="final"></param>
		/// <param name="turn"></param>
		/// <param name="vehicleState"></param>
		/// <param name="vehicleRelative"></param>
		/// <returns></returns>
		public static IPath TurnPath(Lane initial, Lane final, Interconnect turn, VehicleState vehicleState, bool vehicleRelative)
		{ 
            // 1. list of absolute positions of lane coordinates
			List<Coordinates> absoluteCoordinates = new List<Coordinates>();

			// make sure not turning onto same lane
			if(!initial.LaneID.Equals(final.LaneID))
			{
				// 2. loop through initial lane's waypoints
				absoluteCoordinates.AddRange(GetLaneWaypointCoordinates(initial, true));

				// 3. check if the turn has user partitions
				if (turn.UserPartitions != null)
				{
					// 3.1 loop through turn's waypoints if not rndfwaypoints (As will be included by lane coordinate generation
					foreach (UserPartition partition in turn.UserPartitions)
					{
						// add if not an rndf waypoint
						if (!(partition.InitialWaypoint is RndfWayPoint))
						{
							// add user waypoint's position
							absoluteCoordinates.Add(partition.InitialWaypoint.Position);
						}
					}
				}

				// 4. add the next lane's waypoints
				absoluteCoordinates.AddRange(GetLaneWaypointCoordinates(final, true));
			}
			else
			{
				// get lanegth of lane / 4
				// HACK
				double length = RoadToolkit.LanePath(initial, vehicleState, true).Length/4;

				// 2. loop through initial lane's waypoints
				absoluteCoordinates.AddRange(GetLaneWaypointCoordinates(initial, true, length, false));

				// 3. check if the turn has user partitions
				if (turn.UserPartitions != null)
				{
					// 3.1 loop through turn's waypoints if not rndfwaypoints (As will be included by lane coordinate generation
					foreach (UserPartition partition in turn.UserPartitions)
					{
						// add if not an rndf waypoint
						if (!(partition.InitialWaypoint is RndfWayPoint))
						{
							// add user waypoint's position
							absoluteCoordinates.Add(partition.InitialWaypoint.Position);
						}
					}
				}

				// 4. loop through final lane's waypoints
				absoluteCoordinates.AddRange(GetLaneWaypointCoordinates(final, true, length, true));
			}

			// 5. Generate path
			return GeneratePathFromCoordinates(absoluteCoordinates, vehicleState.Position, vehicleState.Heading, vehicleRelative);
		}

		/// <summary>
		/// Gets up to a certain distance of the lane's coordinates
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="includeUserWaypoint"></param>
		/// <param name="d"></param>
		/// <param name="forwards">whether or not to get the front or back of a lane</param>
		/// <returns></returns>
		public static List<Coordinates> GetLaneWaypointCoordinates(Lane lane, bool includeUserWaypoints, double d, bool forwards)
		{
			// 1. list of absolute positions of lane coordinates
			List<Coordinates> absoluteCoordinates = new List<Coordinates>();

			if (forwards)
			{
				RndfWayPoint current = lane.LanePartitions[0].InitialWaypoint;
				double distance = 0;

				// loop over waypoints while distance within params
				while (current != null && distance <= d)
				{
					// add rndf waypoint
					absoluteCoordinates.Add(current.Position);

					// update a coarse distance
					if (current.NextLanePartition != null)
					{
						// update distance
						distance += current.NextLanePartition.FinalWaypoint.Position.DistanceTo(current.Position);

						// check to see if we're including user waypoints
						if (includeUserWaypoints)
						{
							// add the user waypoints
							foreach (UserPartition userPartition in current.NextLanePartition.UserPartitions)
							{
								// check if not an rndfwaypoint
								if (!(userPartition.InitialWaypoint is RndfWayPoint))
								{
									// add user waypoint's position
									absoluteCoordinates.Add(userPartition.InitialWaypoint.Position);
								}
							}
						}

						// iterate
						current = (current.NextLanePartition.FinalWaypoint);
					}
					else
					{
						return absoluteCoordinates;
					}
				}

				return absoluteCoordinates;
			}
			else
			{
				RndfWayPoint current = lane.LanePartitions[lane.LanePartitions.Count-1].FinalWaypoint;
				double distance = 0;

				// loop over waypoints while distance within params
				while (current != null && distance <= d)
				{
					// add rndf waypoint
					absoluteCoordinates.Add(current.Position);

					// update a coarse distance
					if (current.PreviousLanePartition != null)
					{
						// update distance
						distance += current.PreviousLanePartition.InitialWaypoint.Position.DistanceTo(current.Position);

						// check to see if we're including user waypoints
						if (includeUserWaypoints && current.NextLanePartition != null)
						{
							// add the user waypoints
							foreach (UserPartition userPartition in current.NextLanePartition.UserPartitions)
							{
								List<Coordinates> forwardUserCoords = new List<Coordinates>();

								// check if not an rndfwaypoint
								if (!(userPartition.InitialWaypoint is RndfWayPoint))
								{
									// add user waypoint's position
									forwardUserCoords.Add(userPartition.InitialWaypoint.Position);
								}

								forwardUserCoords.Reverse();
								absoluteCoordinates.AddRange(forwardUserCoords);
							}
						}

						// iterate
						current = (current.PreviousLanePartition.InitialWaypoint);
					}
					else
					{
						absoluteCoordinates.Reverse();
						return absoluteCoordinates;
					}
				}

				absoluteCoordinates.Reverse();
				return absoluteCoordinates;
			}
		}

		/// <summary>
		/// Gets the coordinates of a lane's waypoints
		/// </summary>
		/// <param name="includeuserWaypoints"></param>
		/// <returns></returns>
		public static List<Coordinates> GetLaneWaypointCoordinates(Lane lane, bool includeuserWaypoints)
		{
			// 1. list of absolute positions of lane coordinates
			List<Coordinates> absoluteCoordinates = new List<Coordinates>();

			// 2. loop through initial lane's waypoints
			foreach (RndfWayPoint waypoint in lane.Waypoints.Values)
			{
				// add the waypoint's position
				absoluteCoordinates.Add(waypoint.Position);

				// check to see if we're including user waypoints
				if (includeuserWaypoints && waypoint.NextLanePartition != null)
				{
					// add the user waypoints
					foreach (UserPartition userPartition in waypoint.NextLanePartition.UserPartitions)
					{
						// check if not an rndfwaypoint
						if (!(userPartition.InitialWaypoint is RndfWayPoint))
						{
							// add user waypoint's position
							absoluteCoordinates.Add(userPartition.InitialWaypoint.Position);
						}
					}
				}
			}

			// 3. return coordinates
			return absoluteCoordinates;
		}

		/// <summary>
		/// Generates a relative or absolute path based upon an input list of coordinates
		/// </summary>
		/// <param name="coordiantes"></param>
		/// <param name="position"></param>
		/// <param name="heading"></param>
		/// <param name="relative"></param>
		/// <returns></returns>
		public static IPath GeneratePathFromCoordinates(List<Coordinates> coordinates, Coordinates position, Coordinates heading, bool relative)
		{
			// 1. if this needs to be a vehicle relative path
			if (relative)
			{
				// use the relative path generation function to return
				IPath path = VehicleRelativePath(coordinates, position, heading);
				return path;

			}
			// 2. otherwise this needs to be an absolute path
			else
			{
				// Create cubic spline based upon the coordinates
				List<CubicBezier> spline = SplineC2FromPoints(coordinates);

				// Create the Path Segments
				List<IPathSegment> bezierPathSegments = new List<IPathSegment>();
				foreach (CubicBezier bezier in spline)
				{
					bezierPathSegments.Add(new BezierPathSegment(bezier, null, false));
				}

				// Create the path
				Path path = new Path(bezierPathSegments);
				path.CoordinateMode = CoordinateMode.AbsoluteProjected;

			/*if (relative)
			{
				Debug.WriteLine("-heading arc tan: -" + (heading.ArcTan * 180 / Math.PI));

				// compute the rotation matrix to add in our vehicles rotation
				Matrix3 rotMatrix = new Matrix3(
					Math.Cos(-heading.ArcTan), -Math.Sin(-heading.ArcTan), 0,
					Math.Sin(-heading.ArcTan), Math.Cos(-heading.ArcTan), 0,
					0, 0, 1);

				// compute the translation matrix to move our vehicle's location
				Matrix3 transMatrix = new Matrix3(
					1, 0, -position.X,
					0, 1, -position.Y,
					0, 0, 1);

				// compute the combined transformation matrix
				Matrix3 m = transMatrix * rotMatrix;

				// clone, transform and add each segment to our path
				path.Transform(m);
			}*/
				// Return
				return path;
			}
		}

		/// <summary>
		/// Gets a default behavior for a path segment
		/// </summary>
		/// <param name="location"></param>
		/// <param name="vehicleState"></param>
		/// <param name="exit"></param>
		/// <param name="relative"></param>
		/// <param name="stopSpeed"></param>
		/// <param name="aMax"></param>
		/// <param name="dt">timestep in seconds</param>
		/// <returns></returns>
		public static PathFollowingBehavior DefaultStayInLaneBehavior(RndfLocation location, VehicleState vehicleState, 
			RndfWaypointID action, ActionType actionType, bool relative, double stopSpeed, double aMax, double dt,
			double maxSpeed, IPath path)
		{
			// get lane path
			//IPath path = RoadToolkit.LanePath(location.Partition.FinalWaypoint.Lane, vehicleState, relative);

			// check if the action is just a goal (note that exit and stop take precedence)
			if (actionType == ActionType.Goal)
			{
				// get maximum speed
				//double maxSpeed = location.Partition.FinalWaypoint.Lane.Way.Segment.SpeedInformation.MaxSpeed;
				//double maxSpeed = maxV;

				// generate path following behavior
				//return new PathFollowingBehavior(path, new ScalarSpeedCommand(maxSpeed));
				return null;
			}
			else
			{
				// get maximum speed
				//double maxSpeed = location.Partition.FinalWaypoint.Lane.Way.Segment.SpeedInformation.MaxSpeed;

				// get operational required distance to hand over to operational stop
				double distance = RoadToolkit.DistanceUntilOperationalStop(RoadToolkit.DistanceToWaypoint(location, action)-TahoeParams.FL);

				// get desired velocity
				double desiredSpeed = RoadToolkit.InferFinalSpeed(0, stopSpeed, distance, maxSpeed, aMax, dt);

				// generate path following behavior
				//return new PathFollowingBehavior(path, new ScalarSpeedCommand(desiredSpeed));
				return null;
			}
		}

		/// <summary>
		/// Gets the distance to a forward waypoint
		/// </summary>
		/// <param name="location"></param>
		/// <param name="waypoint"></param>
		/// <returns></returns>
		public static double DistanceToWaypoint(RndfLocation location, RndfWaypointID waypoint)
		{
			double distance = location.AbsolutePositionOnPartition.DistanceTo(location.Partition.FinalWaypoint.Position);
			RndfWayPoint current = location.Partition.FinalWaypoint;

			while (current != null)
			{
				if (current.WaypointID.Equals(waypoint))
				{
					return distance;
				}
				else if (current.NextLanePartition == null)
				{
					throw new Exception("waypoint not ahead of vehicle on current lane");
				}
				else
				{
					distance += current.Position.DistanceTo(current.NextLanePartition.FinalWaypoint.Position);
					current = current.NextLanePartition.FinalWaypoint;					
				}
			}

			throw new Exception("waypoint not ahead of vehicle on current lane");
		}


		/// <summary>
		/// Preprocesses the lane paths
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="forwardsPath"></param>
		/// <param name="backwardsPath"></param>
		public static void PreprocessLanePaths(Lane lane,
			out Path forwardsPath, out Path backwardsPath, out List<CubicBezier> forwardSpline)
		{
			// 1. list of absolute positions of lane coordinates
			List<Coordinates> forwardCoordinates = new List<Coordinates>();

			// 2. loop through waypoints
			foreach (RndfWayPoint rwp in lane.Waypoints.Values)
			{
				// add position
				forwardCoordinates.Add(rwp.Position);
			}

			// 3. generate spline
			CubicBezier[] bez = SmoothingSpline.BuildC2Spline(forwardCoordinates.ToArray(), null, null, 0.5);

			// 4. generate path
			List<IPathSegment> forwardPathSegments = new List<IPathSegment>();

			// spline generation
			List<CubicBezier> tmpForwardSpline = new List<CubicBezier>();

			// 5. loop through individual beziers
			foreach (CubicBezier cb in bez)
			{
				// add to spline
				tmpForwardSpline.Add(cb);

				// add to final spline
				forwardPathSegments.Add(new BezierPathSegment(cb, null, false));
			}

			// set spline
			forwardSpline = tmpForwardSpline;
			
			// 6. Create the forward path
			forwardsPath = new Path(forwardPathSegments, CoordinateMode.AbsoluteProjected);
			

			// 7. list of backwards coordinates
			List<Coordinates> backwardsCoordinates = forwardCoordinates;
			backwardsCoordinates.Reverse();

			// 8. generate spline
			bez = SmoothingSpline.BuildC2Spline(backwardsCoordinates.ToArray(), null, null, 0.5);

			// 9. generate path
			List<IPathSegment> backwardPathSegments = new List<IPathSegment>();

			// 10. loop through individual beziers
			foreach (CubicBezier cb in bez)
			{
				// add to final spline
				backwardPathSegments.Add(new BezierPathSegment(cb, null, false));
			}

			// 11. generate backwards path
			backwardsPath = new Path(backwardPathSegments, CoordinateMode.AbsoluteProjected);
		}

	}
}
