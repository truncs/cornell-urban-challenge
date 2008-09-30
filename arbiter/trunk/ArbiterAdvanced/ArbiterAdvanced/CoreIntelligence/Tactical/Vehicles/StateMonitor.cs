using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common.Sensors;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Vehicles
{
	/// <summary>
	/// Determines and holds state of the vehicle
	/// </summary>
	public class StateMonitor
	{
		/// <summary>
		/// State of the vehicle
		/// </summary>
		public SceneEstimatorTrackedCluster Observed;

		/// <summary>
		/// Updates the vehicle state as well as other values
		/// </summary>
		/// <param name="vehicleUpdate"></param>
		public void Update(SceneEstimatorTrackedCluster update)
		{
			// update state
			this.Observed = update;
			
			// calculate other values such as involved areas
		}
	}
}
