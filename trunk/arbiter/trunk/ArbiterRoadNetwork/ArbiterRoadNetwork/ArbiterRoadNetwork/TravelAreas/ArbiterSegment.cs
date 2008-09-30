using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Segment area in an rndf network
	/// </summary>
	[Serializable]
	public class ArbiterSegment : INetworkObject
	{
		#region Segment Members

		/// <summary>
		/// Unique identifier of the segment
		/// </summary>
		public ArbiterSegmentId SegmentId;

		/// <summary>
		/// Ways in the segment
		/// </summary>
		public Dictionary<ArbiterWayId, ArbiterWay> Ways;

		/// <summary>
		/// Lanes in the segment
		/// </summary>
		public Dictionary<ArbiterLaneId, ArbiterLane> Lanes;

		/// <summary>
		/// Waypoints in the segment
		/// </summary>
		public Dictionary<ArbiterWaypointId, ArbiterWaypoint> Waypoints;

		/// <summary>
		/// Speed limits
		/// </summary>
		public ArbiterSpeedLimit SpeedLimits;

		/// <summary>
		/// 1st direction of lanes
		/// </summary>
		public ArbiterWay Way1;

		/// <summary>
		/// 2nd direction of lanes
		/// </summary>
		public ArbiterWay Way2;

		/// <summary>
		/// The network the segment is a partof
		/// </summary>
		public ArbiterRoadNetwork RoadNetwork;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="segmentId"></param>
		public ArbiterSegment(ArbiterSegmentId segmentId)
		{
			this.SegmentId = segmentId;

			this.Waypoints = new Dictionary<ArbiterWaypointId, ArbiterWaypoint>();
			this.Ways = new Dictionary<ArbiterWayId, ArbiterWay>();
			this.Lanes = new Dictionary<ArbiterLaneId, ArbiterLane>();
		}

		#endregion

		#region Standard Equalities

		/// <summary>
		/// Check if two segments are equal
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			// make sure type same
			if (obj is ArbiterSegment)
			{
				// check if the numbers are equal
				return ((ArbiterSegment)obj).SegmentId.Equals(this.SegmentId);
			}

			// otherwise not equal
			return false;
		}

		/// <summary>
		/// Hash code for id
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			// for top levels is just the number
			return this.SegmentId.GetHashCode();
		}

		/// <summary>
		/// String representation of id
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			// this is just the zone number
			return this.SegmentId.ToString();
		}

		#endregion
	}
}
