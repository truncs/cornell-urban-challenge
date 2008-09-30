using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Unique identifier of a lane
	/// </summary>
	[Serializable]
	public class ArbiterLaneId : IAreaSubtypeId, INetworkObject
	{
		#region Lane Id members

		private int laneNumber;

		/// <summary>
		/// Id of Segment lane is a part of
		/// </summary>
		public ArbiterSegmentId SegmentId;

		/// <summary>
		/// Id of Way lane is a part of
		/// </summary>
		public ArbiterWayId WayId;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="laneNumber"></param>
		/// <param name="wayId"></param>
		public ArbiterLaneId(int laneNumber, ArbiterWayId wayId)
		{
			this.laneNumber = laneNumber;
			this.WayId = wayId;
			this.SegmentId = wayId.SegmentId;
		}

		#endregion

		#region IAreaSubtypeId Members

		/// <summary>
		/// id of segment lane is a part of
		/// </summary>
		public IAreaId AreadId
		{
			get { return SegmentId; }
		}

		/// <summary>
		/// number of lane
		/// </summary>
		public int Number
		{
			get { return this.laneNumber; }
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
			if (obj is ArbiterLaneId)
			{
				// cast
				ArbiterLaneId ali = (ArbiterLaneId)obj;

				// check if equal
				return ali.Number == this.laneNumber && ali.WayId.Equals(this.WayId);
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
			return laneNumber << 24 + WayId.GetHashCode();
		}

		/// <summary>
		/// String representation of id
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return WayId.ToString() + "." + laneNumber.ToString();
		}

		#endregion
	}
}
