using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Shapes;

namespace UrbanChallenge.Common.Sensors.Obstacle
{
	/// <summary>
	/// Defines an obstacle observed by the Local Map
	/// </summary>
	[Serializable]
	public struct ObservedObstacle
	{
		/// <summary>
		/// Polygon representing the obstacle in vehicle relative coordinates
		/// </summary>
		public Polygon ObstaclePolygon;

		/// <summary>
		/// Obstacle observed by the vehicle
		/// </summary>
		/// <param name="ObstaclePolygon">vehicle relative polygon</param>
		public ObservedObstacle(Polygon ObstaclePolygon)
		{
			// set the obstacle poly
			this.ObstaclePolygon = ObstaclePolygon;
		}
	}
}
