using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Defines a way of a segment
	/// </summary>
	[Serializable]
	public class ArbiterWay : INetworkObject
	{
		#region Way Members

		/// <summary>
		/// Id of the way
		/// </summary>
		public ArbiterWayId WayId;

		/// <summary>
		/// Segment the way is a part of
		/// </summary>
		public ArbiterSegment Segment;

		/// <summary>
		/// Lanes contained within the way
		/// </summary>
		public Dictionary<ArbiterLaneId, ArbiterLane> Lanes;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="wayId"></param>
		public ArbiterWay(ArbiterWayId wayId)
		{
			this.WayId = wayId;
			this.Lanes = new Dictionary<ArbiterLaneId, ArbiterLane>();
		}

		/// <summary>
		/// Determines if there are lanes in this way
		/// </summary>
		public bool IsValid
		{
			get
			{
				if (this.Lanes == null || this.Lanes.Count == 0)
				{
					return false;
				}
				else
				{
					return true;
				}
			}
		}

		#endregion

		#region Standard Equalities

		/// <summary>
		/// Check if two ways are equal
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			// make sure type same
			if (obj is ArbiterWay)
			{
				// check if the numbers are equal
				return ((ArbiterWay)obj).WayId.Equals(this.WayId);
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
			return this.WayId.GetHashCode();
		}

		/// <summary>
		/// String representation of id
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			// this is just the zone number
			return this.WayId.ToString();
		}

		#endregion
	}
}
