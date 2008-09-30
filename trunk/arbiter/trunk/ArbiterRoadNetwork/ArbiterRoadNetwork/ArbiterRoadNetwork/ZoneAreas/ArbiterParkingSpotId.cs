using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Unique identifier for a parking spot
	/// </summary>
	[Serializable]
	public class ArbiterParkingSpotId : IAreaSubtypeId, INetworkObject
	{
		private int spotNumber;

		/// <summary>
		/// Id of the zone containing the spot
		/// </summary>
		public ArbiterZoneId ZoneId;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="number"></param>
		/// <param name="zoneId"></param>
		public ArbiterParkingSpotId(int number, ArbiterZoneId zoneId)
		{
			this.spotNumber = number;
			this.ZoneId = zoneId;
		}

		#region IAreaSubtypeId Members

		/// <summary>
		/// Id of the zone containing the spot
		/// </summary>
		public IAreaId AreadId
		{
			get { return ZoneId; }
		}

		/// <summary>
		/// Number of the spot
		/// </summary>
		public int Number
		{
			get { return spotNumber; }
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
			if (obj is ArbiterParkingSpotId)
			{
				// cast
				ArbiterParkingSpotId apsi = (ArbiterParkingSpotId)obj;

				// check if the numbers are equal
				return apsi.Number == this.spotNumber && apsi.ZoneId.Equals(this.ZoneId);
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
			return spotNumber << 16 + ZoneId.GetHashCode();
		}

		/// <summary>
		/// String representation of id
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return ZoneId.ToString() + "." + spotNumber.ToString();
		}

		#endregion
	}
}
