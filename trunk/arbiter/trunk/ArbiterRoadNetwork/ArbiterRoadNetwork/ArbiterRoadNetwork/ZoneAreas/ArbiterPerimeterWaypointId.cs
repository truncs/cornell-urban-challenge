using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Unique identifier for a perimeter waypoint
	/// </summary>
	[Serializable]
	public class ArbiterPerimeterWaypointId : IAreaSubtypeWaypointId, INetworkObject
	{
		private int waypointNumber;

		/// <summary>
		/// Id of the perimeter
		/// </summary>
		public ArbiterPerimeterId PerimeterId;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="waypointNumber"></param>
		/// <param name="perimeterId"></param>
		public ArbiterPerimeterWaypointId(int waypointNumber, ArbiterPerimeterId perimeterId)
		{
			this.waypointNumber = waypointNumber;
			this.PerimeterId = perimeterId;
		}

		#region IAreaSubtypeWaypointId Members

		/// <summary>
		/// Id of the perimeter
		/// </summary>
		public IAreaSubtypeId AreaSubtypeId
		{
			get { return PerimeterId; }
		}

		/// <summary>
		/// Number of the waypoint
		/// </summary>
		public int Number
		{
			get { return waypointNumber; }
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
			if(obj is ArbiterPerimeterWaypointId)
			{
				// cast
				ArbiterPerimeterWaypointId apwi = (ArbiterPerimeterWaypointId)obj;

				// check if the numbers are equal
				return apwi.Number == this.waypointNumber && apwi.PerimeterId.Equals(this.PerimeterId);
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
			return waypointNumber << 24 + PerimeterId.GetHashCode();
		}

		/// <summary>
		/// String representation of id
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return PerimeterId.ToString() + "." + waypointNumber.ToString();
		}

		#endregion
	}
}
