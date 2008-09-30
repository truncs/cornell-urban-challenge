using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.RndfNetwork
{
	/// <summary>
	/// Identification information about the Way
	/// </summary>
	[Serializable]
	public class WayID
	{
		private SegmentID segmentID;
		private int wayNumber;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="segmentID">Semgent ID information</param>
		/// <param name="wayNumber">Number of this Way within the Segment</param>
		public WayID(SegmentID segmentID, int wayNumber)
		{
			this.segmentID = segmentID;
			this.wayNumber = wayNumber;
		}

		/// <summary>
		/// Number representing which Way this is in a segment
		/// </summary>
		public int WayNumber
		{
			get { return wayNumber; }
			set { wayNumber = value; }
		}

		/// <summary>
		/// ID of segment containing this Way
		/// </summary>
		public SegmentID SegmentID
		{
			get { return segmentID; }
			set { segmentID = value; }
		}

		/// <summary>
		/// Returns a string representation of the ID
		/// </summary>
		/// <returns>SegmentID.WayID</returns>
		public override string ToString()
		{
			return (segmentID.ToString() + "." + wayNumber.ToString());
		}

		public override bool Equals(object obj)
		{
			if (obj is WayID)
			{
				WayID wid = (WayID)obj;
				return wid.wayNumber == wayNumber && wid.segmentID.Equals(segmentID);
			}
			else
			{
				return false;
			}
		}

		public override int GetHashCode()
		{
			return wayNumber << 8 + segmentID.GetHashCode();
		}
	}
}
