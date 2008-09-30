using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.RndfNetwork
{
	/// <summary>
	/// Type of line that bounds the lane
	/// </summary>
	[Serializable]
	public enum LaneBoundary
	{
		/// <summary>
		/// Double Yellow Line
		/// </summary>
		DoubleYellow,

		/// <summary>
		/// Solid White Line
		/// </summary>
		SolidWhite,

		/// <summary>
		/// Broken White Line
		/// </summary>
		BrokenWhite,

		/// <summary>
		/// None
		/// </summary>
		None,

		/// <summary>
		/// Unknown
		/// </summary>
		Unknown
	}

	/// <summary>
	/// A Lane is a specific Lane inside a Way
	/// </summary>
	[Serializable]
	public class Lane
	{
		private LaneID laneID;
		private Lane onLeft;
		private Lane onRight;
		private LaneBoundary leftBoundary;
		private LaneBoundary rightBoundary;
		private double laneWidth;
		private Way way;
		private List<LanePartition> lanePartitions;
		private Dictionary<RndfWaypointID, RndfWayPoint> waypoints;
		private int numWaypoints;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="laneID">Identification information for this lane</param>
		/// <param name="way">Way this lane belongs to</param>
		public Lane(LaneID laneID, Way way)
		{
			this.laneID = laneID;
			this.way = way;
			waypoints = new Dictionary<RndfWaypointID, RndfWayPoint>();			
		}

		/// <summary>
		/// Number of waypoints in the lane
		/// </summary>
		public int NumWaypoints
		{
			get { return numWaypoints; }
			set { numWaypoints = value; }
		}

		/// <summary>
		/// All the Waypoints within the Lane
		/// </summary>
		public Dictionary<RndfWaypointID, RndfWayPoint> Waypoints
		{
			get { return waypoints; }
			set { waypoints = value; }
		}

		/// <summary>
		/// Parts the Lane is broken into
		/// </summary>
		public List<LanePartition> LanePartitions
		{
			get { return lanePartitions; }
			set { lanePartitions = value; }
		}		

		/// <summary>
		/// The parent Way of the Lane
		/// </summary>
		public Way Way
		{
			get { return way; }
			set { way = value; }
		}

		/// <summary>
		/// Lane width in meters
		/// </summary>
		public double LaneWidth
		{
			get { return laneWidth; }
			set { laneWidth = value; }
		}

		/// <summary>
		/// Type of Line bounding the lane on the right
		/// </summary>
		public LaneBoundary RightBoundary
		{
			get { return rightBoundary; }
			set { rightBoundary = value; }
		}

		/// <summary>
		/// Type of Line bounding the lane on the left
		/// </summary>
		public LaneBoundary LeftBoundary
		{
			get { return leftBoundary; }
			set { leftBoundary = value; }
		}

		/// <summary>
		/// Lane to the Right of this Lane
		/// </summary>
		public Lane OnRight
		{
			get { return onRight; }
			set { onRight = value; }
		}

		/// <summary>
		/// Lane to the Left of this Lane
		/// </summary>
		public Lane OnLeft
		{
			get { return onLeft; }
			set { onLeft = value; }
		}


		/// <summary>
		/// Identification information about the lane
		/// </summary>
		public LaneID LaneID
		{
			get { return laneID; }
			set { laneID = value; }
		}

		public override bool Equals(object obj)
		{
			if (obj is Lane)
			{
				return ((Lane)obj).LaneID.Equals(LaneID);
			}
			else
			{
				return false;
			}
		}

		public override int GetHashCode()
		{
			return LaneID.GetHashCode();
		}

		public override string ToString()
		{
			return LaneID.ToString();
		}
	}
}
