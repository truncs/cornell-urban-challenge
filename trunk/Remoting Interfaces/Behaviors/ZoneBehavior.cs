using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Arbiter.ArbiterRoads;

namespace UrbanChallenge.Behaviors
{
	/// <summary>
	/// Zone behavior base class
	/// </summary>
	[Serializable]
	public class ZoneBehavior : Behavior
	{
		private Polygon zonePerimeter;
		private Polygon[] stayOutside;
		private ScalarSpeedCommand speedCommand;
		private ArbiterZoneId zoneId;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="zonePerimeter">Perimeter of hte zone</param>
		/// <param name="stayOutside">Polygons to stay outside of</param>
		/// <param name="speedCommand">Recommended Speed command</param>
		public ZoneBehavior(ArbiterZoneId zoneId, Polygon zonePerimeter, Polygon[] stayOutside, ScalarSpeedCommand recommendedSpeedCommand)
		{
			this.zoneId = zoneId;
			this.zonePerimeter = zonePerimeter;
			this.stayOutside = stayOutside;
			this.speedCommand = recommendedSpeedCommand;
		}

		/// <summary>
		/// Id of the zone
		/// </summary>
		public ArbiterZoneId ZoneId
		{
			get { return zoneId; }
			set { zoneId = value; }
		}

		/// <summary>
		/// Perimeter of zone
		/// </summary>
		public Polygon ZonePerimeter
		{
			get { return zonePerimeter; }
			set { zonePerimeter = value; }
		}

		/// <summary>
		/// Polygons to stay outside of
		/// </summary>
		public Polygon[] StayOutside
		{
			get { return stayOutside; }
			set { stayOutside = value; }
		}

		/// <summary>
		/// Recommended Speed to travel in the zone
		/// </summary>
		public ScalarSpeedCommand RecommendedSpeed
		{
			get { return speedCommand; }
			set { speedCommand = value; }
		}

		public override string ToShortString()
		{
			return "ZoneBehavior";
		}

		public override string ShortBehaviorInformation()
		{
			return this.zoneId.ToString();
		}

		public override string SpeedCommandString()
		{
			return this.speedCommand.ToString();
		}
	}
}
