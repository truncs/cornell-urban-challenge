using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Unique identifier for a parking spot waypoint
	/// </summary>
	[Serializable]
	public class ArbiterParkingSpotWaypointId : IAreaSubtypeWaypointId, INetworkObject
	{
		private int waypointNumber;

		/// <summary>
		/// Id of the parking spot
		/// </summary>
		public ArbiterParkingSpotId SpotId;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="number"></param>
		/// <param name="spotId"></param>
		public ArbiterParkingSpotWaypointId(int number, ArbiterParkingSpotId spotId)
		{
			waypointNumber = number;
			SpotId = spotId;
		}

		#region IAreaSubtypeWaypointId Members

		/// <summary>
		/// Id of the parking spot
		/// </summary>
		public IAreaSubtypeId AreaSubtypeId
		{
			get { return SpotId; }
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
			if (obj is ArbiterParkingSpotWaypointId)
			{
				// cast
				ArbiterParkingSpotWaypointId apswi = (ArbiterParkingSpotWaypointId)obj;

				// check if the numbers are equal
				return apswi.Number == this.waypointNumber && apswi.SpotId.Equals(this.SpotId);
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
			return waypointNumber << 24 + SpotId.GetHashCode();
		}

		/// <summary>
		/// String representation of id
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return SpotId.ToString() + "." + waypointNumber.ToString();
		}

		#endregion
	}
}
