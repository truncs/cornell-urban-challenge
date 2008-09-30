using System;
using UrbanChallenge.Common;
using UrbanChallenge.Common.RoadNetwork;

namespace UrbanChallenge.WorldSimulation {

	public interface IWorld {

		// Add a vehicle to the simulation that is controlled by a dummy arbiter.
		// Returns the ID of the new vehicle.
		UInt32 CreateVehicle();

		UInt32 RegisterWorldListener(IWorldListener listener);
		void UnregisterWorldListener(UInt32 token);

		DateTime LogicalTime { get; }

	}

}
