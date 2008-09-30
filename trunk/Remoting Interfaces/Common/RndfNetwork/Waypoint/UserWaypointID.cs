using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.RndfNetwork
{
	/// <summary>
	/// Identification information about the UserWaypoint
	/// </summary>
	[Serializable]
	public class UserWaypointID : IWaypointID
	{
		private LanePartitionID lanePartitionID;
		private int userWaypointNumber;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="lanePartitionID">ID of the LanePartition that contains the userWaypoint</param>
		/// <param name="userWaypointNumber">Number of the userWaypoint within the LanePartition</param>
		public UserWaypointID(LanePartitionID lanePartitionID, int userWaypointNumber)
		{
			this.lanePartitionID = lanePartitionID;
			this.userWaypointNumber = userWaypointNumber;
		}

		/// <summary>
		/// Number of the userWaypoint within the LanePartition
		/// </summary>
		public int UserWaypointNumber
		{
			get { return userWaypointNumber; }
			set { userWaypointNumber = value; }
		}

		/// <summary>
		/// ID of the LanePartition that contains the userWaypoint
		/// </summary>
		public LanePartitionID LanePartitionID
		{
			get { return lanePartitionID; }
			set { lanePartitionID = value; }
		}

		public override bool Equals(object obj)
		{
			if (obj is UserWaypointID)
			{
				UserWaypointID wpID = (UserWaypointID)obj;
				return (this.userWaypointNumber == wpID.UserWaypointNumber && this.lanePartitionID.Equals(wpID.lanePartitionID));
			}
			else
			{
				throw new ArgumentException("Wrong type to compare", "obj");
			}
		}

		public override int GetHashCode()
		{
			return this.userWaypointNumber << 32 + lanePartitionID.GetHashCode();
		}

		/// <summary>
		/// String representation of the userWaypoint ID
		/// </summary>
		/// <returns>userWaypoint ID as a string</returns>
		public override string ToString()
		{
			return lanePartitionID.ToString() + "." + userWaypointNumber.ToString();
		}
	}
}
