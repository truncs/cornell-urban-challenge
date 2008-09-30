using System;
using System.Collections.Generic;
using System.Text;

using UrbanChallenge.Common;
using UrbanChallenge.Common.RndfNetwork;
using UrbanChallenge.Common.Route;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common.Sensors;
using UrbanChallenge.Behaviors;



namespace UrbanChallenge.Arbiter.Communication
{
	/// <summary>
	/// Broadcast by the ai over the messaging service
	/// </summary>
	[Serializable]
	public class AiRemoteListener
	{
		public Routes LocalRoute;
		public FullRoute BestFullRoute;
		public VehicleState VehicleState;
		public DynamicObstacles DynamicObstacles;
		public StaticObstacles StaticObstacles;
        public Behavior behavior;
        public IPath path;

		/// <summary>
		/// Default Constructor
		/// </summary>
		public AiRemoteListener()
		{
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="localRoute"></param>
		/// <param name="bestFullRoute"></param>
		/// <param name="vehicleState"></param>
		/// <param name="maneuver"></param>
		public AiRemoteListener(Routes localRoute, FullRoute bestFullRoute, VehicleState vehicleState, Behavior behavior,
			IPath path, DynamicObstacles dynamicObstacles, StaticObstacles staticObstacles)
		{
			this.LocalRoute = localRoute;
			this.BestFullRoute = bestFullRoute;
			this.VehicleState = vehicleState;
            this.behavior = behavior;
            this.path = path;
			this.DynamicObstacles = dynamicObstacles;
			this.StaticObstacles = staticObstacles;
		}
	}
}
