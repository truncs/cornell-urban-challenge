using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.RndfNetwork
{
	/// <summary>
	/// Partition of a Lane between two Rndf defined Waypoints
	/// </summary>
	[Serializable]
	public class LanePartitionID
	{
		private LaneID laneID;
		private int lanePartitionNumber;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="laneID">ID of Lane the partition is a part of</param>
		/// <param name="lanePartitionNumber">Number of LanePartition within the lane</param>
		public LanePartitionID(LaneID laneID, int lanePartitionNumber)
		{
			this.laneID = laneID;
			this.lanePartitionNumber = lanePartitionNumber;
		}

		/// <summary>
		/// Number of this LanePartition within the Lane
		/// </summary>
		public int LanePartitionNumber
		{
			get { return lanePartitionNumber; }
			set { lanePartitionNumber = value; }
		}

		/// <summary>
		/// Lane this partition is a part of
		/// </summary>
		public LaneID LaneID
		{
			get { return laneID; }
			set { laneID = value; }
		}

		/// <summary>
		/// String representation of the LanePartition's ID
		/// </summary>
		/// <returns>ID as a string</returns>
		public override string ToString()
		{
			return laneID.ToString() + "." + lanePartitionNumber.ToString();
		}

		public override bool Equals(object obj)
		{
			if (obj is LanePartitionID)
			{
				LanePartitionID lpid = (LanePartitionID)obj;
				return lpid.lanePartitionNumber == lanePartitionNumber && lpid.laneID.Equals(laneID);
			}
			else
			{
				return false;
			}
		}

		public override int GetHashCode()
		{
			return lanePartitionNumber << 24 + LaneID.GetHashCode();
		}
	}
}
