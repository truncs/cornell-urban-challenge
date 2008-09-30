using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.Sensors
{
	public enum SideObstacleSide : int
	{
		Driver = 0,
		Passenger = 1
	}

	public enum SideObstacleMsgID : int
	{
		Info = 0,
		ScanMsg = 1,
		Bad = 99
	}

	public class SideObstacles
	{
		/// <summary>
		/// The time this happened in vehicle time (seconds)
		/// </summary>
		public double timestamp;
		/// <summary>
		/// Indicates which side of the car this message pertains to.
		/// </summary>
		public SideObstacleSide side;
		/// <summary>
		/// The obstacles.
		/// </summary>
		public List<SideObstacle> obstacles;
	}
	public class SideObstacle
	{
		/// <summary>
		/// Distance (m) from the side of the car to the nearest obstacle on the side. This is NOT relative to the IMU.
		/// </summary>
		public double distance;

		/// <summary>
		/// Estimated height (m) of the obstacle on the left from the ground. Useful for harsher thresholding of cars. Sensor theshold is set at 20cm.
		/// </summary>
		public double height;
	}
}
