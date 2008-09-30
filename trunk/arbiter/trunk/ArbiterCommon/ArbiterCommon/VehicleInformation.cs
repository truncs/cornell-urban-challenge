using System;
using System.Collections.Generic;
using System.Text;

using UrbanChallenge.Common;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common.Sensors;
using UrbanChallenge.Common.RndfNetwork;
using UrbanChallenge.Common.Sensors.Vehicle;

namespace UrbanChallenge.Arbiter.ArbiterCommon
{
	/// <summary>
	/// State of the vehicle on the rndf
	/// </summary>
	public struct VehicleInformation
	{
		/// <summary>
		/// Raw observations
		/// </summary>
		public ObservedVehicle Observed;	

		/// <summary>
		/// Analyzed Rndf Location
		/// </summary>
		public RndfLocation VehicleLocation;

		/// <summary>
		/// Teh Id of the vehicle
		/// </summary>
		public int Id
		{
			get { return Observed.Id; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="observed"></param>
		/// <param name="vehicleLocation"></param>
		public VehicleInformation(ObservedVehicle observed, RndfLocation vehicleLocation)
		{
			this.Observed = observed;
			this.VehicleLocation = vehicleLocation;
		}
	}
}


		