using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.Sensors.Obstacle
{
	/// <summary>
	/// Holder for the multitude of Observed Obstacles from the Local Map
	/// </summary>
	[Serializable]
	public struct ObservedObstacles
	{
		/// <summary>
		/// The Obstacles observed by the local map
		/// </summary>
		public ObservedObstacle[] Obstacles;

		/// <summary>
		/// Time the obstacles were observed
		/// </summary>
		public DateTime TimeObserved;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="obstacles"></param>
		/// <param name="timeObserved"></param>
		public ObservedObstacles(ObservedObstacle[] obstacles, DateTime timeObserved)
		{
			this.Obstacles = obstacles;
			this.TimeObserved = timeObserved;			
		}
	}
}
