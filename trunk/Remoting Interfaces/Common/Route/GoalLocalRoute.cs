using System;
using System.Collections.Generic;
using System.Text;

using UrbanChallenge.Common.RndfNetwork;

namespace UrbanChallenge.Common.Route
{
	/// <summary>
	/// Type of LocalRoute where a goal is the action point
	/// </summary>
	public class GoalLocalRoute : LocalRoute
	{
		private Goal goal;
		private List<RndfWaypointID> nodes;		

		/// <summary>
		/// Constructor
		/// Defines a link between a specific RndfWaypoint and Total Time until the next Goal.
		/// Contains specific information about actions at the action RndfWaypoint
		/// </summary>
		/// <param name="actionPoint">Waypoint at which vehicle exits the Segment or alternatively where vehicle needs to pass over goal</param>
		/// <param name="timeToAction">Time to get to action point</param>
		public GoalLocalRoute(RndfWaypointID actionPoint, double timeToAction, Goal goal)
			: base(actionPoint, timeToAction)
		{
			this.goal = goal;
			this.RouteTime = timeToAction;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="goalPoint">Waypoint at which vehicle needs to pass over goal</param>
		/// <param name="timeToAction">Time to get to goal</param>
		/// <param name="routeTime">Total route time to goal if go through this action point</param>
		/// <param name="continuation">If we decide to continue on road after action point, this is route time to goal</param>
		/// <param name="goal">goal the rndf waypoint represents</param>
		public GoalLocalRoute(RndfWaypointID goalPoint, double timeToAction, double routeTime, LocalOption continuation, Goal goal)
			: base(goalPoint, timeToAction, routeTime, continuation)
		{
			this.goal = goal;
		}
				
		/// <summary>
		/// Goal the action point represents
		/// </summary>
		public Goal Goal
		{
			get { return goal; }
			set { goal = value; }
		}

		/// <summary>
		/// nodes of the route
		/// </summary>
		public List<RndfWaypointID> Nodes
		{
			get { return nodes; }
			set { nodes = value; }
		}
	}
}
