using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Unique identifier for a lane partition
	/// </summary>
	[Serializable]
	public class ArbiterLanePartitionId : IConnectAreaWaypointsId, INetworkObject
	{
		#region Lane Partition Id Members

		/// <summary>
		/// Lane the partition is a part of
		/// </summary>
		public ArbiterLaneId LaneId;

		/// <summary>
		/// Id of the initial waypoint
		/// </summary>
		public ArbiterWaypointId InitialId;

		/// <summary>
		/// Id of the final waypoint
		/// </summary>
		public ArbiterWaypointId FinalId;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="initialId"></param>
		/// <param name="finalId"></param>
		public ArbiterLanePartitionId(ArbiterWaypointId initialId, ArbiterWaypointId finalId, ArbiterLaneId laneId)
		{
			this.InitialId = initialId;
			this.FinalId = finalId;
			this.LaneId = laneId;
		}

		#endregion

		#region IConnectAreaWaypointsId Members

		public IAreaSubtypeWaypointId InitialGenericId
		{
			get
			{
				return this.InitialId;
			}
		}

		public IAreaSubtypeWaypointId FinalGenericId
		{
			get
			{
				return this.FinalId;
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
			if (obj is ArbiterLanePartitionId)
			{
				// cast
				ArbiterLanePartitionId alpi = (ArbiterLanePartitionId)obj;

				// check if equal
				return alpi.FinalId.Equals(this.FinalId) && alpi.InitialId.Equals(this.InitialId) && alpi.LaneId.Equals(this.LaneId);
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
			return this.InitialId.Number << 32 + this.LaneId.GetHashCode();
		}

		/// <summary>
		/// String representation of id
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return this.LaneId.ToString() + "." + this.InitialId.Number.ToString();
		}

		#endregion
	}
}
