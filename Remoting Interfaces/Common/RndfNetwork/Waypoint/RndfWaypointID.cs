using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.RndfNetwork
{
	/// <summary>
	/// lookup information for waypoints
	/// </summary>
	[Serializable]
	public class RndfWaypointID
	{
		private LaneID laneID;
		private int waypointNumber;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="laneID">Lane ident info</param>
		/// <param name="waypointNumber">number of this rndf waypoint in lane</param>
		public RndfWaypointID(LaneID laneID, int waypointNumber)
		{
			this.laneID = laneID;
			this.waypointNumber = waypointNumber;
		}

		/// <summary>
		/// Number Rndf waypoint in this lane
		/// </summary>
		public int WaypointNumber
		{
			get { return waypointNumber; }
			set { waypointNumber = value; }
		}

		/// <summary>
		/// Lane identification information
		/// </summary>
		public LaneID LaneID
		{
			get { return laneID; }
			set { laneID = value; }
		}

		/// <summary>
		/// String representation of this ID
		/// </summary>
		/// <returns>ID as a string</returns>
		public override string ToString()
		{
			return laneID.ToString() + "." + waypointNumber.ToString();
		}

		/// <summary>
		/// Checks if two RndfWaypointID's are equal
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			if (obj is RndfWaypointID)
			{
				RndfWaypointID wid = (RndfWaypointID)obj;
				return wid.waypointNumber == waypointNumber && wid.laneID.Equals(laneID);
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Specific hashcode for a rndfWaypoint ID object
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return waypointNumber << 24 + laneID.GetHashCode();
		}
		
	}
}
