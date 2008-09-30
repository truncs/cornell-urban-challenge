using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Common.EarthModel;
using UrbanChallenge.Common;
using NameService;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Behaviors.CompletionReport;

namespace UrbanChallenge.OperationalService
{
	/// <summary>
	/// Interface to the operational test componenet
	/// </summary>
	[Serializable]
	public abstract class OperationalTestComponentFacade : MarshalByRefObject, IPingable
	{
		public const string ServiceName = "OperationalTestComponentService";

		/// <summary>
		/// Sets and immediately begins executing the supplied behavior, blocks and returns completion report
		/// </summary>
		/// <param name="b">Behavior to execute</param>
		public abstract CompletionReport TestExecuteBehavior(Behavior b);

		/// <summary>
		/// Projection origin for computing (X,Y) coordinates
		/// </summary>
		/// <param name="lat">Latitude in radians</param>
		/// <param name="lon">Longitude in radians</param>
		[Obsolete]
		public abstract void SetProjection(PlanarProjection proj);
		public abstract void SetRoadNetwork(ArbiterRoadNetwork roadNetwork);
		public abstract void Ping();
	}
}
