using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.Sensors.Vehicle
{
	/// <summary>
	/// Holder for the multitude of Observed Vehicles from the Scene Estimator
	/// </summary>
	[Serializable]
	public struct ObservedVehicles
	{
		/// <summary>
		/// The vehicles observed by the scene estimator
		/// </summary>
		public ObservedVehicle[] Vehicles;

		/// <summary>
		/// The time the vehicles were observed
		/// </summary>
		public DateTime TimeObserved;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="vehicles"></param>
		/// <param name="timeObserved"></param>
		public ObservedVehicles(ObservedVehicle[] vehicles, DateTime timeObserved)
		{
			this.Vehicles = vehicles;
			this.TimeObserved = timeObserved;
		}
	}
}
