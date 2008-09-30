using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.RndfNetwork
{
	/// <summary>
	/// Identification information about the Segment
	/// </summary>
	[Serializable]
	public class SegmentID
	{
		private int segmentNumber;
		private string name;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="segmentNumber">ID number of the segment</param>
		public SegmentID(int segmentNumber)
		{
			this.segmentNumber = segmentNumber;
			this.name = "";
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="segmentNumber">ID number of the segment</param>
		/// <param name="name">Name of the Road the Segment represents</param>
		public SegmentID(int segmentNumber, string name)
		{
			this.segmentNumber = segmentNumber;
			this.name = name;
		}

		/// <summary>
		/// The name of the road the Segment represents
		/// </summary>
		public string Name
		{
			get { return name; }
			set { name = value; }
		}

		/// <summary>
		/// Number of the Segment
		/// </summary>
		public int SegmentNumber
		{
			get { return segmentNumber; }
			set { segmentNumber = value; }
		}

		/// <summary>
		/// Returns a string representation of the ID
		/// </summary>
		/// <returns>Segment Number</returns>
		public override string ToString()
		{
			return segmentNumber.ToString();
		}

		public override bool Equals(object obj)
		{
			if (obj is SegmentID)
			{
				return ((SegmentID)obj).segmentNumber == segmentNumber;
			}
			else
			{
				return false;
			}
		}

		public override int GetHashCode()
		{
			return segmentNumber;
		}
	}
}
