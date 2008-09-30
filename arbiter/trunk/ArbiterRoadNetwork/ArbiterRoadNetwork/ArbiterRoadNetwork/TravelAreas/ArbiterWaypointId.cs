using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Unique identifier for a segment waypoint
	/// </summary>
	[Serializable]
	public class ArbiterWaypointId : IAreaSubtypeWaypointId, INetworkObject
	{
		#region Waypoint Members

		private int waypointNumber;

		/// <summary>
		/// Id of lane waypoint is a part of
		/// </summary>
		public ArbiterLaneId LaneId;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="waypointNumber"></param>
		/// <param name="laneId"></param>
		public ArbiterWaypointId(int waypointNumber, ArbiterLaneId laneId)
		{
			this.waypointNumber = waypointNumber;
			this.LaneId = laneId;
		}

		/// <summary>
		/// generates a waypoint string without the way id
		/// </summary>
		/// <returns></returns> 
		public string ToStringNoWayId()
		{
			return this.LaneId.WayId.SegmentId.Number.ToString() + "." + this.LaneId.Number.ToString() + "." + this.waypointNumber.ToString();
		}

		private string quickName = null;

		#endregion

		#region IAreaSubtypeWaypointId Members

		public IAreaSubtypeId AreaSubtypeId
		{
			get { return LaneId; }
		}

		public int Number
		{
			get { return waypointNumber; }
		}

		#endregion

		#region Standard Equalities

		/// <summary>
		/// Check if two zones are equal
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			// make sure type same
			if (obj is ArbiterWaypointId)
			{
				// cast
				ArbiterWaypointId awi = (ArbiterWaypointId)obj;

				// check if equal
				return awi.Number == this.waypointNumber && awi.LaneId.Equals(this.LaneId);
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
			return waypointNumber << 32 + LaneId.GetHashCode();
		}

		/// <summary>
		/// String representation of id
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			if(quickName == null)
				quickName = LaneId.ToString() + "." + waypointNumber.ToString();

			return quickName;
		}

		#endregion
	}
}
