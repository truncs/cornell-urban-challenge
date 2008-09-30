using System;
using UrbanChallenge.Common;
using UrbanChallenge.Common.RoadNetwork;
using UrbanChallenge.Tactical;

namespace UrbanChallenge.TacticalSimulation.Tactical {

	public abstract class Facade : MarshalByRefObject {

		public abstract IVehicle CornellVehicle { get; }

	}

}
