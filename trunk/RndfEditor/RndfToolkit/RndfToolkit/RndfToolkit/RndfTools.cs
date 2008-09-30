using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Splines;
using UrbanChallenge.Common;
using UrbanChallenge.Common.RndfNetwork;
using UrbanChallenge.DarpaRndf;
using UrbanChallenge.Common.EarthModel;
using System.IO;
using Parser;
using UrbanChallenge.Arbiter.ArbiterRoads;

namespace RndfToolkit
{
	/// <summary>
	/// General Set of Tools to be used with Rndfs
	/// </summary>
	public static class RndfTools
	{
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
		/// Parses an rndf
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns></returns>
		public static IRndf GenerateGpsRndf(string fileName)
		{
			// Convert file to a FileStream
			FileStream fs = new FileStream(fileName, FileMode.Open);

			// Create new Parser
			RndfParser parser = new RndfParser();

			// Create a new Gps Rndf
			IRndf gpsRndf = parser.createRndf(fs);

			// dispose the fs
			fs.Dispose();

			// return the rndf
			return gpsRndf;
		}

		/// <summary>
		/// Opens an rndf file and transforms the points to xy around a central projection
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="rndf"></param>
		/// <param name="projection"></param>
		public static void GenerateXyRndfAndProjection(string fileName, out IRndf rndf, out PlanarProjection projection)
		{
			// generate the gps rndf
			IRndf gpsRndf = GenerateGpsRndf(fileName);

			// get the planar projection
			projection = GetProjection(gpsRndf);

			// create an xy rndf
			IRndf xyRndf = gpsRndf;

			#region Apply transform to all point

			// Get coordinates from segments			
			foreach (SimpleSegment segment in xyRndf.Segments)
			{
				// Get coordinates from lanes
				List<SimpleLane> lanes = (List<SimpleLane>)segment.Lanes;
				foreach (SimpleLane lane in lanes)
				{
					// Get coordinates from waypoints
					List<SimpleWaypoint> waypoints = (List<SimpleWaypoint>)lane.Waypoints;
					foreach (SimpleWaypoint waypoint in waypoints)
					{
						// Transform the gps coordinate into an xy xoordinate
						GpsCoordinate position = new GpsCoordinate(waypoint.Position.X, waypoint.Position.Y);
						Coordinates tmpXY = projection.ECEFtoXY(WGS84.LLAtoECEF(GpsTools.DegreesToLLA(position)));

						// Add position to the coordinate list
						waypoint.Position = tmpXY;
					}
				}
			}

			foreach (SimpleZone zone in xyRndf.Zones)
			{
				// Get coordinates from perimeter
				ZonePerimeter perimeter = zone.Perimeter;
				List<PerimeterPoint> perimeterPoints = (List<PerimeterPoint>)perimeter.PerimeterPoints;
				foreach (PerimeterPoint perimeterPoint in perimeterPoints)
				{
					// Transform the gps coordinate into an xy xoordinate
					GpsCoordinate position = new GpsCoordinate(perimeterPoint.position.X, perimeterPoint.position.Y);
					Coordinates tmpXY = projection.ECEFtoXY(WGS84.LLAtoECEF(GpsTools.DegreesToLLA(perimeterPoint.position)));

					// Add position to the coordinate list
					perimeterPoint.position = tmpXY;
				}

				// Get coordiantes from parking spots
				List<ParkingSpot> parkingSpots = (List<ParkingSpot>)zone.ParkingSpots;
				foreach (ParkingSpot parkingSpot in parkingSpots)
				{
					// Transform the gps coordinate into an xy xoordinate
					GpsCoordinate position = new GpsCoordinate(parkingSpot.Waypoint1.Position.X, parkingSpot.Waypoint1.Position.Y);
					Coordinates tmpXY = projection.ECEFtoXY(WGS84.LLAtoECEF(GpsTools.DegreesToLLA(parkingSpot.Waypoint1.Position)));

					// wp1 position set
					parkingSpot.Waypoint1.Position = tmpXY;

					// Transform the gps coordinate into an xy xoordinate
					position = new GpsCoordinate(parkingSpot.Waypoint2.Position.X, parkingSpot.Waypoint2.Position.Y);
					Coordinates tmpXY2 = projection.ECEFtoXY(WGS84.LLAtoECEF(GpsTools.DegreesToLLA(parkingSpot.Waypoint2.Position)));

					// wp1 position set
					parkingSpot.Waypoint2.Position = tmpXY2;
				}
			}

			# endregion

			// set the return xy rndf
			rndf = xyRndf;
		}

		/// <summary>
		/// Converts an xy rndf to a Rndf Network
		/// </summary>
		/// <param name="rndf"></param>
		/// <param name="projection"></param>
		/// <returns></returns>
		public static RndfNetwork ConvertXyRndfToRndfNetwork(IRndf rndf, PlanarProjection projection)
		{
			// create rndf network generator
			RndfNetworkGenerator networkGenerator = new RndfNetworkGenerator();

			// create the rndf network
			RndfNetwork rndfNetwork = networkGenerator.CreateRndfNetwork(rndf, projection);

			// return the rndf
			return rndfNetwork;
		}

		/// <summary>
		/// Generates an rndf network from a file
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns></returns>
		public static ArbiterRoadNetwork GenerateRndfNetwork(string fileName)
		{
			// xy rndf and projection
			UrbanChallenge.DarpaRndf.IRndf xyRndf;
			PlanarProjection projection;

			// assign xy rndf and projection
			RndfTools.GenerateXyRndfAndProjection(fileName, out xyRndf, out projection);

			// determine ways
			xyRndf = RndfTools.DetermineWays(xyRndf);

			// make a new generator
			RoadNetworkGeneration rng = new RoadNetworkGeneration(xyRndf);

			// now get the rndf network
			ArbiterRoadNetwork arn = rng.GenerateRoadNetwork();

			// set the projection
			arn.PlanarProjection = projection;

			// return the rndf network
			return arn;
		}

		/// <summary>
		/// Averages all coordinates in the gps rndf and creates a planar projection
		/// </summary>
		/// <param name="rndf"></param>
		/// <returns></returns>
		public static PlanarProjection GetProjection(IRndf rndf)
		{
			// First group all the Coordinates
			List<Coordinates> coordinates = new List<Coordinates>();

			// Get coordinates from segments
			List<SimpleSegment> segments = (List<SimpleSegment>)rndf.Segments;
			foreach (SimpleSegment segment in segments)
			{
				// Get coordinates from lanes
				List<SimpleLane> lanes = (List<SimpleLane>)segment.Lanes;
				foreach (SimpleLane lane in lanes)
				{
					// Get coordinates from waypoints
					List<SimpleWaypoint> waypoints = (List<SimpleWaypoint>)lane.Waypoints;
					foreach (SimpleWaypoint waypoint in waypoints)
					{
						// Add position to the coordinate list
						coordinates.Add(waypoint.Position);
					}
				}
			}

			// Get coordinates from zones
			List<SimpleZone> zones = (List<SimpleZone>)rndf.Zones;
			foreach (SimpleZone zone in zones)
			{
				// Get coordinates from perimeter
				ZonePerimeter perimeter = zone.Perimeter;
				List<PerimeterPoint> perimeterPoints = (List<PerimeterPoint>)perimeter.PerimeterPoints;
				foreach (PerimeterPoint perimeterPoint in perimeterPoints)
				{
					// Add perimeterPont position to coordinate list
					coordinates.Add(perimeterPoint.position);
				}

				// Get coordiantes from parking spots
				List<ParkingSpot> parkingSpots = (List<ParkingSpot>)zone.ParkingSpots;
				foreach (ParkingSpot parkingSpot in parkingSpots)
				{
					// Add waypoints in parking spot to coordinate list
					coordinates.Add(parkingSpot.Waypoint1.Position);
					coordinates.Add(parkingSpot.Waypoint2.Position);
				}
			}

			// Get origin for planar projection
			List<GpsCoordinate> gpsCoordinates = new List<GpsCoordinate>();
			foreach (Coordinates coordinate in coordinates)
			{
				gpsCoordinates.Add(new GpsCoordinate(coordinate.X, coordinate.Y));
			}

			// get planar projection origin
			GpsCoordinate projectionOrigin = GpsTools.CalculateOrigin(gpsCoordinates);

			// Create new projection
			PlanarProjection projection = GpsTools.PlanarProjection(projectionOrigin, true);

			// return created projection
			return projection;
		}

		/// <summary>
		/// determine and set the Ways of a segment usign the lane data already in xy
		/// </summary>
		/// <param name="segment"></param>
		/// <param name="Lanes"></param>
		public static IRndf DetermineWays(IRndf originalRndf)
		{
			// loop through segments
			foreach (SimpleSegment originalSegment in originalRndf.Segments)
			{
				// initialize ways
				originalSegment.Way1Lanes = new List<SimpleLane>();
				originalSegment.Way2Lanes = new List<SimpleLane>();

				// calculate directon of first lane	
				SimpleLane initialLane = originalSegment.Lanes[0];

				// get first and last waypoitns of lane
				Coordinates initialBeginning = initialLane.Waypoints[0].Position;
				Coordinates initialEnding = initialLane.Waypoints[initialLane.Waypoints.Count - 1].Position;

				// get change in position
				Coordinates initialDelta = initialEnding - initialBeginning;
				double initialAngle = initialDelta.ToDegrees();

				// loop through lanes
				foreach (SimpleLane tempLane in originalSegment.Lanes)
				{
					// get first and last waypoitns of lane
					Coordinates laneBeginning = tempLane.Waypoints[0].Position;
					Coordinates laneEnding = tempLane.Waypoints[tempLane.Waypoints.Count - 1].Position;

					// get change in position
					Coordinates laneDelta = laneEnding - laneBeginning;
					double laneAngle = laneDelta.ToDegrees();

					// calculate direction of each lane and add to Way1 if close to same dir as first
					if (initialAngle >= 40.0 && initialAngle <= 320.0)
					{
						if (Math.Abs(initialAngle - laneAngle) < 40.0)
							originalSegment.Way1Lanes.Add(tempLane);
						else
							originalSegment.Way2Lanes.Add(tempLane);
					}
					else if (initialAngle < 40.0)
					{
						if (laneAngle < 320.0)
						{
							if (Math.Abs(initialAngle - laneAngle) < 40.0)
								originalSegment.Way1Lanes.Add(tempLane);
							else
								originalSegment.Way2Lanes.Add(tempLane);
						}
						else
						{
							double reflectCurrent = 360 - laneAngle;
							if (initialAngle + reflectCurrent < 40.0)
								originalSegment.Way1Lanes.Add(tempLane);
							else
								originalSegment.Way2Lanes.Add(tempLane);
						}
					}
					else
					{
						if (laneAngle > 320.0)
						{
							if (Math.Abs(initialAngle - laneAngle) < 40.0)
								originalSegment.Way1Lanes.Add(tempLane);
							else
								originalSegment.Way2Lanes.Add(tempLane);
						}
						else
						{
							double reflectInitial = 360 - initialAngle;
							if (reflectInitial + laneAngle < 40.0)
								originalSegment.Way1Lanes.Add(tempLane);
							else
								originalSegment.Way2Lanes.Add(tempLane);
						}
					}
				}
			}

			// return the rndf
			return originalRndf;
		}

		/// <summary>
		/// Maximum speed over an interconnect
		/// </summary>
		/// <param name="ai"></param>
		/// <returns></returns>
		public static double MaximumInterconnectSpeed(ArbiterInterconnect ai)
		{
			// set the minimum maximum speed = 4mph
			double minSpeed = 1.78816;

			if (ai.InitialGeneric is ArbiterWaypoint && ai.FinalGeneric is ArbiterWaypoint)
			{
				// waypoint
				ArbiterWaypoint awI = (ArbiterWaypoint)ai.InitialGeneric;
				ArbiterWaypoint awF = (ArbiterWaypoint)ai.FinalGeneric;

				List<Coordinates> interCoords = new List<Coordinates>();
				Coordinates init = awI.Position - awI.PreviousPartition.Vector().Normalize(10.0);
				Coordinates fin = awF.Position + awF.NextPartition.Vector().Normalize(10.0);
				interCoords.Add(init);
				interCoords.Add(awI.Position);
				interCoords.Add(awF.Position);
				interCoords.Add(fin);

				double initMax = awI.Lane.Way.Segment.SpeedLimits.MaximumSpeed;
				double finalMax = awF.Lane.Way.Segment.SpeedLimits.MaximumSpeed;
				double curvatureMax = MaximumSpeed(interCoords, minSpeed);
				return Math.Min(Math.Min(initMax, finalMax), curvatureMax);
			}
			else
			{
				return minSpeed;
			}
		}

		/// <summary>
		/// Gets maximum speed over a path
		/// </summary>
		/// <param name="coordinatePath"></param>
		/// <returns></returns>
		public static double MaximumSpeed(List<Coordinates> coordinatePath, double minSpeed)
		{
			// generate path
			List<CubicBezier> cb = SplineC2FromPoints(coordinatePath);

			// get max curvature
			double? maxCurvature = Curvature(cb);

			// if curvature exists
			if (maxCurvature.HasValue)
			{
				// get minimum radius or curvature
				double r = Math.Abs(1.0 / maxCurvature.Value);

				// set the static friction
				double us = 0.2;

				// gravity value
				double g = 9.8;

				// get the maximum velocity based upon curvature or max v
				double maxSpeed = Math.Max(Math.Sqrt(us * r * g), minSpeed);

				// return the speed
				return maxSpeed;
			}
			else
			{
				return minSpeed;
			}
		}

		/// <summary>
		/// Gets the maximum curvature along the spline
		/// </summary>
		/// <param name="bezierSpline"></param>
		/// <param name="distanceAlong"></param>
		/// <returns></returns>
		private static double? Curvature(List<CubicBezier> bezierSpline)
		{
			double maxCurvature = Double.MinValue;

			for (int i = 0; i < bezierSpline.Count; i++)
			{
				double arcLength = bezierSpline[i].ArcLength;
				double increment = 0.5;
				double arcAlong = 0.0;

				while (arcAlong <= arcLength)
				{
					double curvature = bezierSpline[i].Curvature(arcAlong / arcLength);

					if (Math.Abs(curvature) > maxCurvature)
						maxCurvature = Math.Abs(curvature);

					arcAlong += increment;
				}
			}

			if(maxCurvature == Double.MinValue)
				return null;
			else
				return maxCurvature;
		}
	}
}
