using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.Path;

namespace UrbanChallenge.Behaviors
{
	/// <summary>
	/// Pull out of a parking space
	/// </summary>
	[Serializable]
	public class ZoneParkingPullOutBehavior : ZoneParkingBehavior
	{
		/// <summary>
		/// Path to pull out parking spot -> final orientation
		/// </summary>
		public LinePath RecommendedPullOutPath;

		/// <summary>
		/// Left bound of final orientation
		/// </summary>
		public LinePath EndingLeftBound;

		/// <summary>
		/// Right bound
		/// </summary>
		public LinePath EndingRightBound;

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
		/// <param name="endingPolygon"></param>
		/// <param name="orientations"></param>
		public ZoneParkingPullOutBehavior(ArbiterZoneId zoneId, Polygon zonePerimeter, Polygon[] stayOutside, ScalarSpeedCommand speedCommand,
			LinePath parkingSpotPath, LinePath spotLeftBound, LinePath spotRightBound, ArbiterParkingSpotId spotId, 
			LinePath reversePath, LinePath finalLeftBound, LinePath finalRightBound)
			: base(zoneId, zonePerimeter, stayOutside, speedCommand, parkingSpotPath, spotLeftBound, spotRightBound, spotId, 0.0)
		{
			this.RecommendedPullOutPath = reversePath;
			this.EndingLeftBound = finalLeftBound;
			this.EndingRightBound = finalRightBound;
		}

		public override string ToShortString()
		{
			return "ZoneParkingPullOut";
		}
	}
}
