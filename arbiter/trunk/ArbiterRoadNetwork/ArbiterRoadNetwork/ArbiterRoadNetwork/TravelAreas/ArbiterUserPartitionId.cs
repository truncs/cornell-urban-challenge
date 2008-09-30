using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Unique id of this partition
	/// </summary>
	[Serializable]
	public class ArbiterUserPartitionId : INetworkObject
	{
		#region User Partition Members

		private int waypointNumber;

		/// <summary>
		/// Id of the partition containing this use partition
		/// </summary>
		public IConnectAreaWaypointsId PartitionId;

		/// <summary>
		/// Id of the initial waypoint of the partition
		/// </summary>
		public Object InitialId;

		/// <summary>
		/// Id of the final waypoint of the partition
		/// </summary>
		public Object FinalId;

		/// <summary>
		/// COnstructor
		/// </summary>
		/// <param name="lanePartitionId"></param>
		/// <param name="initialGenericId"></param>
		/// <param name="finalGenericId"></param>
		public ArbiterUserPartitionId(IConnectAreaWaypointsId partitionId,
			Object initialId, Object finalId)
		{
			this.PartitionId = partitionId;
			this.InitialId = initialId;
			this.FinalId = finalId;

			if (this.InitialId is ArbiterWaypointId)
				waypointNumber = ((ArbiterWaypointId)this.InitialId).Number;
			else
				waypointNumber = ((ArbiterUserWaypointId)this.InitialId).Number;
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
			if (obj is ArbiterUserPartitionId)
			{
				// cast
				ArbiterUserPartitionId aupi = (ArbiterUserPartitionId)obj;

				// check if equal
				return aupi.FinalId.Equals(this.FinalId) && aupi.InitialId.Equals(this.InitialId) && aupi.PartitionId.Equals(this.PartitionId);
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
			return waypointNumber << 40 + this.PartitionId.GetHashCode();
		}

		/// <summary>
		/// String representation of id
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return this.PartitionId.ToString() + "." + waypointNumber.ToString();
		}

		#endregion
	}
}
