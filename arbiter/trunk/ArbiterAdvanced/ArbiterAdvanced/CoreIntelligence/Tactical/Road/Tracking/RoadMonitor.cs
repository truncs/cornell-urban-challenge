using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Sensors.Vehicle;
using UrbanChallenge.Common.Sensors;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road
{
	/// <summary>
	/// Handles assignment of vehicles to areas and components
	/// </summary>
	public class RoadMonitor
	{
		private RoadTactical roadTactical;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="roadTactical"></param>
		public RoadMonitor(RoadTactical roadTactical)
		{
			this.roadTactical = roadTactical;
		}

		/// <summary>
		/// Resets held values
		/// </summary>
		public void Reset()
		{
		}

		/// <summary>
		/// Assigns vehicles to components
		/// </summary>
		/// <param name="vehicles"></param>
		public void Assign(SceneEstimatorTrackedClusterCollection vehicles)
		{
		}
	}
}
