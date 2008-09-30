using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.RndfNetwork
{
	/// <summary>
	/// Identification infromation for the lane
	/// </summary>
	[Serializable]
	public class LaneID
	{
		private WayID wayID;
		private int laneNumber;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="wayID">Way identification information</param>
		/// <param name="laneNumber">Number of Lane in the Way</param>
		public LaneID(WayID wayID, int laneNumber)
		{
			this.wayID = wayID;
			this.laneNumber = laneNumber;
		}

		/// <summary>
		/// Number of the Lane in the Way
		/// </summary>
		public int LaneNumber
		{
			get { return laneNumber; }
			set { laneNumber = value; }
		}

		/// <summary>
		/// Way identification information
		/// </summary>
		public WayID WayID
		{
			get { return wayID; }
			set { wayID = value; }
		}

		/// <summary>
		/// String representing the ID of this Lane
		/// </summary>
		/// <returns>Lane ID as a string</returns>
		public override string ToString()
		{
			return wayID.ToString() + "." + laneNumber.ToString();
		}

		public override bool Equals(object obj)
		{
			if (obj is LaneID)
			{
				LaneID lid = (LaneID)obj;
				return lid.laneNumber == laneNumber && lid.WayID.Equals(WayID);
			}
			else
			{
				return false;
			}
		}

		public override int GetHashCode()
		{
			return laneNumber << 16 + wayID.GetHashCode();
		}
		
	}
}
