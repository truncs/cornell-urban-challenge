using System;
using System.Collections.Generic;

namespace UrbanChallenge.WorldSimulation {

	public interface IWorldListener {

		void UpdateVehicles(ICollection<Vehicle> vehicles);

	}

}
