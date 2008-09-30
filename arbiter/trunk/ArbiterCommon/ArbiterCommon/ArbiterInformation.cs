using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Common.Route;
using UrbanChallenge.Common.RndfNetwork;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common.Splines;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common.Sensors.Vehicle;

namespace UrbanChallenge.Arbiter.ArbiterCommon
{
	/// <summary>
	/// Information about the arbiter
	/// </summary>
	[Serializable]
	public class ArbiterInformation
	{
		public string CurrentArbiterState;
		public CarMode InternalCarMode;
		public FullRoute FullRoute;
		public double RouteTime;
		public RndfWaypointID ActionPoint;
		public RndfWaypointID CurrentGoal;
		public Queue<RndfWaypointID> Goals;
		public Behavior Behavior;
		public VehicleState PlanningState;

		/// <summary>
		/// Default constructor
		/// </summary>
		public ArbiterInformation()
		{
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="currentArbiterState"></param>
		/// <param name="internalCarMode"></param>
		/// <param name="fullRoute"></param>
		/// <param name="routeTime"></param>
		/// <param name="actionPoint"></param>
		/// <param name="currentGoal"></param>
		/// <param name="goals"></param>
		/// <param name="behavior"></param>
		public ArbiterInformation(string currentArbiterState, CarMode internalCarMode, FullRoute fullRoute, double routeTime,
				RndfWaypointID actionPoint, RndfWaypointID currentGoal, Queue<RndfWaypointID> goals, Behavior behavior, VehicleState planningState)
		{
			this.CurrentArbiterState = currentArbiterState;
			this.InternalCarMode = internalCarMode;
			this.FullRoute = fullRoute;
			this.RouteTime = routeTime;
			this.ActionPoint = actionPoint;
			this.CurrentGoal = currentGoal;
			this.Goals = goals;
			this.Behavior = behavior;
			this.PlanningState = planningState;
		}
	}
}
