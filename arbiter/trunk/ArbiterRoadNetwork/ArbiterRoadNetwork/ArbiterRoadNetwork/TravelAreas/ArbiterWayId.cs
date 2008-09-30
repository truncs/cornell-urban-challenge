using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Unique identifier for a Way
	/// </summary>
	[Serializable]
	public class ArbiterWayId : INetworkObject
	{
		#region Way Id Members

		private int wayNumber;

		/// <summary>
		/// Id of the Segment containing the way
		/// </summary>
		public ArbiterSegmentId SegmentId;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="wayNumber"></param>
		/// <param name="segmentId"></param>
		public ArbiterWayId(int wayNumber, ArbiterSegmentId segmentId)
		{
			this.wayNumber = wayNumber;
			this.SegmentId = segmentId;
		}

		/// <summary>
		/// Number of the way
		/// </summary>
		public int Number
		{
			get
			{
				return wayNumber;
			}
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
			if (obj is ArbiterWayId)
			{
				// cast
				ArbiterWayId awi = (ArbiterWayId)obj;

				// check if equal
				return awi.Number == this.wayNumber && awi.SegmentId.Equals(this.SegmentId);
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
			return wayNumber << 16 + SegmentId.GetHashCode();
		}

		/// <summary>
		/// String representation of id
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return this.SegmentId.ToString() + "." + wayNumber.ToString();
		}

		#endregion
	}
}
