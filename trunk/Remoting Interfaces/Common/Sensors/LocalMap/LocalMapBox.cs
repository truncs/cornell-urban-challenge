using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.Sensors.LocalMap
{
	/// <summary>
	/// Box obstacles from Local Map. These are mature clusters.
	/// </summary>
	[Serializable]
	public class LocalMapBox
	{
		/// <summary>
		/// Position relative to the IMU
		/// This position is centered on the back axle!
		/// </summary>
		public Coordinates position;
		
		/// <summary>
		/// Speed in m/s
		/// </summary>
		public double speed;

		/// <summary>
		/// heading of the box
		/// </summary>
		public Coordinates heading;

		/// <summary>
		/// Length of the box in meters
		/// </summary>
		public double length;

		/// <summary>
		/// Width of the box in meters
		/// </summary>
		public double width;

		public LocalMapBox(Coordinates position, double speed, Coordinates heading, double length, double width)
		{
			this.position = position;
			this.speed = speed;
			this.heading = heading;
			this.width = width;
			this.length = length;
		}
	}
}
