using System;
using System.Collections.Generic;
using System.Text;

using UrbanChallenge.Common.RndfNetwork;

namespace UrbanChallenge.Common.Route
{
	/// <summary>
	/// Type of localRoute where action point is an exit
	/// </summary>
	public class ExitLocalRoute : LocalRoute, IComparer<ExitLocalRoute>
	{
		private List<LocalOption> exits;
		private List<RndfWayPoint> nodes;

		public List<RndfWayPoint> Nodes
		{
		  get { return nodes; }
		  set { nodes = value; }
		}

		/// <summary>
		/// Constructor
		/// Defines a link between a specific RndfWaypoint and Total Time until the next Goal.
		/// COntains specific information about actions at the action RndfWaypoint
		/// </summary>
		/// <param name="actionPoint">Waypoint at which vehicle exits the Segment or alternatively where vehicle needs to pass over goal</param>
		/// <param name="timeToAction">Time to get to action point</param>
		public ExitLocalRoute(RndfWaypointID actionPoint, double timeToAction, List<RndfWayPoint> nodes)
			: base(actionPoint, timeToAction)
		{
			this.exits = new List<LocalOption>();
			this.nodes = nodes;
		}

		/// <summary>
		/// Constructor
		/// Defines a link between a specific RndfWaypoint and Total Time until the next Goal.
		/// COntains specific information about actions at the action RndfWaypoint
		/// </summary>
		/// <param name="actionPoint">Waypoint at which vehicle exits the Segment or alternatively where vehicle needs to pass over goal</param>
		/// <param name="timeToAction">Time to get to action point</param>
		/// <param name="routeTime">Total route time to goal if go through this action point</param>
		/// <param name="continuation">If we decide to continue on road after action point, this is route time to goal</param>
		public ExitLocalRoute(RndfWaypointID actionPoint, double timeToAction, double routeTime, LocalOption continuation, List<RndfWayPoint> nodes) 
			: base(actionPoint, timeToAction, routeTime, continuation)
		{
			this.exits = new List<LocalOption>();
			this.nodes = nodes;
		}

		/// <summary>
		/// Since the action point is an exit, this defines the exits out of this segment
		/// </summary>
		public List<LocalOption> Exits
		{
			get { return exits; }
			set { exits = value; }
		}

		#region IComparer<ExitLocalRoute> Members

		public int Compare(ExitLocalRoute x, ExitLocalRoute y)
		{
			return base.Compare(x, y);
		}

		#endregion
	}
}
