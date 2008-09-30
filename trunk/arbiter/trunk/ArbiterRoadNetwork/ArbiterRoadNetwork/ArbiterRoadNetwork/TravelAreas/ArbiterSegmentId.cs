using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Unique identifier of a segment
	/// </summary>
	[Serializable]
	public class ArbiterSegmentId : IAreaId, INetworkObject
	{
		private int segmentNumber;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="number"></param>
		public ArbiterSegmentId(int number)
		{
			this.segmentNumber = number;
		}

		#region IAreaId Members

		/// <summary>
		/// number of the segment
		/// </summary>
		public int Number
		{
			get { return this.segmentNumber; }
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
			if (obj is ArbiterSegmentId)
			{
				// check if the numbers are equal
				return ((ArbiterSegmentId)obj).Number == this.segmentNumber;
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
			return segmentNumber;
		}

		/// <summary>
		/// String representation of id
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			// this is just the zone number
			return segmentNumber.ToString();
		}

		#endregion
	}
}
