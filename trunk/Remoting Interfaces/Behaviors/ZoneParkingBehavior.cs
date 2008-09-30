using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common.Shapes;

namespace UrbanChallenge.Behaviors
{
	/// <summary>
	/// Parking in a parking spot
	/// </summary>
	[Serializable]
	public class ZoneParkingBehavior : ZoneBehavior
	{
		/// <summary>
		/// Path representing hte parking spot
		/// </summary>
		public LinePath ParkingSpotPath;

		/// <summary>
		/// Left Bound of the parking spot
		/// </summary>
		public LinePath SpotLeftBound;

		/// <summary>
		/// Right Bound of the parking spot
		/// </summary>
		public LinePath SpotRightBound;

		/// <summary>
		/// Id of the parking spot
		/// </summary>
		public ArbiterParkingSpotId ParkingSpotId;

		/// <summary>
		/// Extra distance to pull past the end of hte parking 
		/// spot path along the same line if possible
		/// </summary>
		public double ExtraDistance;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="zoneId"></param>
		/// <param name="zonePerimeter"></param>
		/// <param name="stayOutside"></param>
		/// <param name="speedCommand"></param>
		/// <param name="parkingSpotPath"></param>
		/// <param name="spotLeftBound"></param>
		/// <param name="spotRightBound"></param>
		/// <param name="spotId"></param>
		/// <param name="extraDistance"></param>
		public ZoneParkingBehavior(ArbiterZoneId zoneId, Polygon zonePerimeter, Polygon[] stayOutside, ScalarSpeedCommand speedCommand,
			LinePath parkingSpotPath, LinePath spotLeftBound, LinePath spotRightBound, ArbiterParkingSpotId spotId, double extraDistance)
			: base(zoneId, zonePerimeter, stayOutside, speedCommand)
		{
			this.ParkingSpotPath = parkingSpotPath;
			this.SpotLeftBound = spotLeftBound;
			this.SpotRightBound = spotRightBound;
			this.ParkingSpotId = spotId;
			this.ExtraDistance = extraDistance;
		}

		public override string ToShortString()
		{
			return "ZoneParkingBehavior";
		}

		public override string ShortBehaviorInformation()
		{
			return this.ParkingSpotId.ToString();
		}
	}
}
