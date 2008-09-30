using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Identification of an intersection
	/// </summary>
	[Serializable]
	public class ArbiterIntersectionId : INetworkObject
	{
		#region Intersection Id Members

		// interconnect id this id is based upon
		private ArbiterInterconnectId interconnectId;

		public ArbiterIntersectionId(ArbiterInterconnectId aid)
		{
			this.interconnectId = aid;
		}

		#endregion

		#region Standard Equalities

		public override bool Equals(object obj)
		{
			if (obj is ArbiterIntersectionId)
			{
				return ((ArbiterIntersectionId)obj).interconnectId.Equals(this.interconnectId);
			}

			return false;
		}

		public override int GetHashCode()
		{
			return interconnectId.GetHashCode();
		}

		public override string ToString()
		{
			return interconnectId.ToString();
		}

		#endregion
	}
}
