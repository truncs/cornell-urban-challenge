using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common.Path;

namespace UrbanChallenge.Behaviors
{
	/// <summary>
	/// Travelling from our current position to a specified ending heading and orientation
	/// </summary>
	[Serializable]
	public class ZoneTravelingBehavior : ZoneBehavior
	{
		/// <summary>
		/// Recommended path to take to the final location (end of the path)
		/// </summary>
		/// <remarks>Last semgent represents optimal heading</remarks>
		public LinePath RecommendedPath;

		/// <summary>
		/// Left bound of the end of the path (for the final path segment)
		/// </summary>
		public LinePath EndingLeftBound;

		/// <summary>
		/// Right bound of the end of the path (for the final path segment)
		/// </summary>
		public LinePath EndingRightBound;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="zoneId"></param>
		/// <param name="zonePerimeter"></param>
		/// <param name="stayOutside"></param>
		/// <param name="speedCommand"></param>
		/// <param name="recommendedPath"></param>
		/// <param name="endingLeftBound"></param>
		/// <param name="endingRightBound"></param>
		public ZoneTravelingBehavior(ArbiterZoneId zoneId, Polygon zonePerimeter, Polygon[] stayOutside, ScalarSpeedCommand speedCommand,
			LinePath recommendedPath, LinePath endingLeftBound, LinePath endingRightBound)
			: base(zoneId, zonePerimeter, stayOutside, speedCommand)
		{
			this.RecommendedPath = recommendedPath;
			this.EndingLeftBound = endingLeftBound;
			this.EndingRightBound = endingRightBound;
		}

		public override string ToShortString()
		{
			return "ZoneTravelingBehavior";
		}
	}
}
