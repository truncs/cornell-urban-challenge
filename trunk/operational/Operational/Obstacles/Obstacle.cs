using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Sensors;

namespace OperationalLayer.Obstacles {
	enum AvoidanceStatus {
		Unknown,
		Left,
		Right,
		Ignore,
		Collision
	}

	class Obstacle : IComparable<Obstacle>, IEquatable<Obstacle> {
		private static int obstacle_counter = 0;

		/// <summary>
		/// Convex hull of the obstacle with no expansion or prediction
		/// </summary>
		public Polygon obstaclePolygon;
		/// <summary>
		/// For moving car-like obstacles, this the polygon extruded out to have the proper aspect ratio as a car
		/// </summary>
		public Polygon extrudedPolygon;
		/// <summary>
		/// For moving obstacles, this is the time-projected polygon of their position
		/// </summary>
		public Polygon predictedPolygon;
		/// <summary>
		/// For static obstacles, this is the expanded polygon used for determining age over multiple iterations
		/// </summary>
		public Polygon mergePolygon;
		/// <summary>
		/// For all obstacles, this is the expanded version of the polygon we want to avoid (C-Space esque duder)
		/// </summary>
		public Polygon cspacePolygon;
		/// <summary>
		/// Minimum spacing around the obstacle
		/// </summary>
		public double minSpacing;
		/// <summary>
		/// Desired spacing around the obstacle
		/// </summary>
		public double desSpacing;
		/// <summary>
		/// Number of iterations we've seen this obstacle (or this piece of the obstacle as the case may be)
		/// </summary>
		public int age;
		/// <summary>
		/// Class of the obstacle
		/// </summary>
		public ObstacleClass obstacleClass;
		/// <summary>
		/// ID assigned by track generator
		/// </summary>
		public int trackID;
		public bool occuluded;

		/// <summary>
		/// Currently ignored by the arbiter
		/// </summary>
		public bool ignored;

		/// <summary>
		/// last iteration that the obstacle was off-road, 0 if never off road
		/// </summary>
		public int offroadAge;

		/// <summary>
		/// Flag indicating if the absolute heading value is valid
		/// </summary>
		public bool absoluteHeadingValid;
		/// <summary>
		/// Absolute heading of the obstacle
		/// </summary>
		public double absoluteHeading;

		public bool speedValid;
		public double speed;

		/// <summary>
		/// Internal ID used for checking equality
		/// </summary>
		public int internalID;

		/// <summary>
		/// Used by the avoidance code to label the result of processing this obstacle
		/// </summary>
		public AvoidanceStatus avoidanceStatus;

		/// <summary>
		/// Populated by the obstacle manager to hold the points the obstacle had a collision with the path
		/// </summary>
		public List<Coordinates> collisionPoints;

		/// <summary>
		/// Distance to the obstacle
		/// </summary>
		public double obstacleDistance;
		public double requiredDeceleration;

		public Obstacle() {
			internalID = obstacle_counter++;
			trackID = -1;
			avoidanceStatus = AvoidanceStatus.Unknown;
		}

		public Obstacle ShallowClone() {
			return (Obstacle)this.MemberwiseClone();
		}

		public Polygon AvoidancePolygon {
			get {
				switch (obstacleClass) {
					case ObstacleClass.DynamicCarlike:
					case ObstacleClass.DynamicNotCarlike:
					case ObstacleClass.DynamicUnknown:
						return predictedPolygon ?? obstaclePolygon;

					case ObstacleClass.StaticLarge:
					case ObstacleClass.StaticSmall:
					case ObstacleClass.DynamicStopped:
					default:
						return obstaclePolygon;
				}
			}
		}

		#region IComparable<Obstacle> Members

		public int CompareTo(Obstacle other) {
			return internalID.CompareTo(other.internalID);
		}

		#endregion

		#region IEquatable<Obstacle> Members

		public bool Equals(Obstacle other) {
			return internalID == other.internalID;
		}

		#endregion

		public override bool Equals(object obj) {
			Obstacle obs = obj as Obstacle;
			if (obs != null) {
				return Equals(obs);
			}
			else {
				return false;
			}
		}

		public override int GetHashCode() {
			return internalID;
		}
	}
}
