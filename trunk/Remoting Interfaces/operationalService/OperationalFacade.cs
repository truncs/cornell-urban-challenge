using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Common.EarthModel;
using UrbanChallenge.Common;
using NameService;
using UrbanChallenge.Arbiter.ArbiterRoads;

namespace UrbanChallenge.OperationalService {
	/// <summary>
	/// Interface to the operational layer
	/// </summary>
	[Serializable]
	public abstract class OperationalFacade : MarshalByRefObject, IPingable {
		public const string ServiceName = "OperationalService";

		/// <summary>
		/// Sets and immediately begins executing the supplied behavior.
		/// </summary>
		/// <param name="b">Behavior to execute</param>
		public abstract void ExecuteBehavior(Behavior b);

		/// <summary>
		/// Retrieves the behavior current executing in the operational layer
		/// </summary>
		/// <returns>Currently executing behavior</returns>
		public abstract Type GetCurrentBehaviorType();

		/// <summary>
		/// Returns the current car mode reported by the lower level systems
		/// </summary>
		/// <returns>Car Mode reported by the lower level systems</returns>
		public abstract CarMode GetCarMode();

		/// <summary>
		/// Projection origin for computing (X,Y) coordinates
		/// </summary>
		/// <param name="lat">Latitude in radians</param>
		/// <param name="lon">Longitude in radians</param>
		[Obsolete]
		public abstract void SetProjection(PlanarProjection proj);

		public abstract void SetRoadNetwork(ArbiterRoadNetwork roadNetwork);

		public abstract void RegisterListener(OperationalListener listener);
		public abstract void UnregisterListener(OperationalListener listener);

		public abstract void Ping();
	}
}
