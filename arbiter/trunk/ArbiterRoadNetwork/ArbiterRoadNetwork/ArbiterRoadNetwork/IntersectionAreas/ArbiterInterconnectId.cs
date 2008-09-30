using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Id of a connection between two waypoints
	/// </summary>
	[Serializable]
	public class ArbiterInterconnectId : IConnectAreaWaypointsId, INetworkObject
	{
		#region Interconnect Id Members

		private IAreaSubtypeWaypointId initialId;
		private IAreaSubtypeWaypointId finalId;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="initial"></param>
		/// <param name="final"></param>
		public ArbiterInterconnectId(IAreaSubtypeWaypointId initialId, IAreaSubtypeWaypointId finalId)
		{
			this.initialId = initialId;
			this.finalId = finalId;
		}

		#endregion

		#region IConnectAreaWaypointsId Members

		/// <summary>
		/// Id of the initial waypoint
		/// </summary>
		public IAreaSubtypeWaypointId InitialGenericId
		{
			get { return initialId; }
		}

		/// <summary>
		/// Id of the final waypoint
		/// </summary>
		public IAreaSubtypeWaypointId FinalGenericId
		{
			get { return finalId; }
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
			if (obj is ArbiterInterconnectId)
			{
				// cast
				ArbiterInterconnectId aii = (ArbiterInterconnectId)obj;

				// check if equal
				return aii.InitialGenericId.Equals(this.InitialGenericId) && aii.FinalGenericId.Equals(this.FinalGenericId);
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
			return this.initialId.GetHashCode() << 40 + this.finalId.GetHashCode();
		}

		/// <summary>
		/// String representation of id
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return this.initialId.ToString() + "-" + this.finalId.ToString();
		}

		#endregion
	}
}
