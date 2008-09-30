using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.RndfNetwork
{
	/// <summary>
	/// Identification information for the UserPartition
	/// </summary>
	[Serializable]
	public class UserPartitionID
	{
		private LanePartitionID lanePartitionID;
		private int userPartitionNumber;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="lanePartitionID">ID of the lanePartition that holds this UserPartition</param>
		/// <param name="userPartitionNumber">Number of this user partition within the LanePartition</param>
		public UserPartitionID(LanePartitionID lanePartitionID, int userPartitionNumber)
		{
			this.lanePartitionID = lanePartitionID;
			this.userPartitionNumber = userPartitionNumber;
		}

		/// <summary>
		/// Number of the UserPartition within the LanePartition
		/// </summary>
		public int UserPartitionNumber
		{
			get { return userPartitionNumber; }
			set { userPartitionNumber = value; }
		}

		/// <summary>
		/// Identification information of the LanePartition which holds this UserPartition
		/// </summary>
		public LanePartitionID LanePartitionID
		{
			get { return lanePartitionID; }
			set { lanePartitionID = value; }
		}

		/// <summary>
		/// String representation of the UserpartitionID
		/// </summary>
		/// <returns>UserPartitionID as a String</returns>
		public override string ToString()
		{
			return lanePartitionID.ToString() + "." + userPartitionNumber.ToString();
		}

		public override bool Equals(object obj)
		{
			if (obj is UserPartitionID)
			{
				UserPartitionID upid = (UserPartitionID)obj;
				return userPartitionNumber == upid.userPartitionNumber && lanePartitionID.Equals(upid.lanePartitionID);
			}
			else
			{
				return false;
			}
		}

		public override int GetHashCode()
		{
			return userPartitionNumber << 32 + lanePartitionID.GetHashCode();
		}
	}
}
