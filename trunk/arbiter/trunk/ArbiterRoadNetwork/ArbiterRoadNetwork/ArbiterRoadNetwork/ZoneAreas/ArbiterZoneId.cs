using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Unique identifier of a zone
	/// </summary>
	[Serializable]
	public class ArbiterZoneId : IAreaId, INetworkObject
	{
		private int zoneNumber;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="number"></param>
		public ArbiterZoneId(int number)
		{
			this.zoneNumber = number;
		}

		#region IAreaId Members

		/// <summary>
		/// Number of the zone
		/// </summary>
		public int Number
		{
			get { return zoneNumber; }
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
			if (obj is ArbiterZoneId)
			{
				// check if the numbers are equal
				return ((ArbiterZoneId)obj).Number == this.zoneNumber;
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
			return zoneNumber;
		}

		/// <summary>
		/// String representation of id
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			// this is just the zone number
			return zoneNumber.ToString();
		}

		#endregion
	}
}
