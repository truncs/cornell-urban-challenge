using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Unique identifier of a user waypoint
	/// </summary>
	[Serializable]
	public class ArbiterUserWaypointId : INetworkObject
	{
		#region User Waypoint Id Members

		/// <summary>
		/// Number of the user waypoint
		/// </summary>
		public int Number;

		/// <summary>
		/// Partition this user waypoint is a member of
		/// </summary>
		public IConnectAreaWaypointsId PartitionId;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="number"></param>
		/// <param name="lanePartitionId"></param>
		public ArbiterUserWaypointId(int number, IConnectAreaWaypointsId partitionId)
		{
			this.Number = number;
			this.PartitionId = partitionId;
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
			if (obj is ArbiterUserWaypointId)
			{
				// cast
				ArbiterUserWaypointId auwi = (ArbiterUserWaypointId)obj;

				// check if equal
				return auwi.Number == this.Number && auwi.PartitionId.Equals(this.PartitionId);
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
			return this.Number << 40 + this.PartitionId.GetHashCode();
		}

		/// <summary>
		/// String representation of id
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return this.PartitionId.ToString() + "." + this.Number.ToString();
		}

		#endregion

	}
}
