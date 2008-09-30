using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.Sensors.LocalMap
{
	/// <summary>
	/// Box obstacles from Local Map. These are mature clusters.
	/// </summary>
	[Serializable]
	public class LocalMapCluster
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
		/// Heading of the cluster
		/// </summary>
		public Coordinates heading;

		/// <summary>
		/// Width of the cluster in meters
		/// </summary>
		public double width;

		public LocalMapCluster(Coordinates position, double speed, Coordinates heading, double width)
		{
			this.position = position;
			this.speed = speed;
			this.heading = heading;
			this.width = width;			
		}
	}
}
