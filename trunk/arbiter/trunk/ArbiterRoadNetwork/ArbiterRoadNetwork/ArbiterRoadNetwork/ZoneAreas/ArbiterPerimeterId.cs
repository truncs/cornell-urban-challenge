using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Unique identifier for a zone perimeter
	/// </summary>
	[Serializable]
	public class ArbiterPerimeterId : IAreaSubtypeId, INetworkObject
	{
		private int perimeterNumber;

		/// <summary>
		/// Id of the zone containing the spot
		/// </summary>
		public ArbiterZoneId ZoneId;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="perimeterNumber"></param>
		/// <param name="zoneId"></param>
		public ArbiterPerimeterId(int perimeterNumber, ArbiterZoneId zoneId)
		{
			this.perimeterNumber = perimeterNumber;
			this.ZoneId = zoneId;
		}

		#region IAreaSubtypeId Members

		public IAreaId AreadId
		{
			get { return ZoneId; }
		}

		public int Number
		{
			get { return perimeterNumber; }
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
			if (obj is ArbiterPerimeterId)
			{
				// cast
				ArbiterPerimeterId api = (ArbiterPerimeterId)obj;

				// check if equal
				return api.Number == this.perimeterNumber && api.ZoneId.Equals(this.ZoneId);
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
			return perimeterNumber << 16 + ZoneId.GetHashCode();
		}

		/// <summary>
		/// String representation of id
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return ZoneId.ToString() + "." + perimeterNumber.ToString();
		}

		#endregion
	}
}
