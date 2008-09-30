using System;
using System.Collections.Generic;
using System.Text;

using UrbanChallenge.Common.RndfNetwork;

namespace UrbanChallenge.Common.Route
{
    /// <summary>
    /// Defines a link between a specific RndfWaypoint and Total Time until the next Goal.
	/// COntains specific information about actions at the action RndfWaypoint
    /// </summary>
    [Serializable]
    public class LocalRoute : IComparable, IComparer<LocalRoute>
    {
		private RndfWaypointID actionPoint;	// Waypoint at which vehicle exits the Segment or alternatively where vehicle needs to pass over goal
		private double timeToAction;		// Time to get to action point
		private double routeTime;			// Total route time to goal if go through this action point
		private LocalOption continuation;	// If we decide to continue on road after action point, this is route time to goal

		/// <summary>
		/// Constructor
		/// Defines a link between a specific RndfWaypoint and Total Time until the next Goal.
		/// COntains specific information about actions at the action RndfWaypoint
		/// </summary>
		/// <param name="actionPoint">Waypoint at which vehicle exits the Segment or alternatively where vehicle needs to pass over goal</param>
		/// <param name="timeToAction">Time to get to action point</param>
		public LocalRoute(RndfWaypointID actionPoint, double timeToAction)
		{
			this.actionPoint = actionPoint;
			this.timeToAction = timeToAction;
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
		public LocalRoute(RndfWaypointID actionPoint, double timeToAction, double routeTime, LocalOption continuation)
		{
			this.actionPoint = actionPoint;
			this.timeToAction = timeToAction;
			this.routeTime = routeTime;
			this.continuation = continuation;
		}

		/// <summary>
		/// Waypoint at which vehicle exits the Segment or alternatively where vehicle needs to pass over goal
		/// </summary>
		public RndfWaypointID ActionPoint
		{
			get { return actionPoint; }
			set { actionPoint = value; }
		}

		/// <summary>
		/// Time to get to the action point
		/// </summary>
		public double TimeToAction
		{
			get { return timeToAction; }
			set { timeToAction = value; }
		}

		/// <summary>
		/// Total route time to goal if go through this action point
		/// </summary>
		public double RouteTime
		{
			get { return routeTime; }
			set { routeTime = value; }
		}

		/// <summary>
		/// If we decide to continue on road after action point, this is route time to goal
		/// </summary>
		public LocalOption Continuation
		{
			get { return continuation; }
			set { continuation = value; }
		}

		#region IComparable Members

		/// <summary>
		/// Compares RouteSegments based upon cost
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public int CompareTo(object obj)
		{
			if (obj is LocalRoute)
			{
				LocalRoute other = (LocalRoute)obj;
				if (this.routeTime < other.RouteTime)
				{
					return -1;
				}
				else if (this.routeTime == other.RouteTime)
				{
					return 0;
				}
				else
				{
					return 1;
				}
			}
			else
			{
				throw new ArgumentException("Invalid object type", "obj");
			}
		}

		#endregion

		#region IComparer<LocalRoute> Members

		public int Compare(LocalRoute x, LocalRoute y)
		{
			if (x.RouteTime < y.RouteTime)
			{
				return -1;
			}
			else if (x.RouteTime == y.RouteTime)
			{
				return 0;
			}
			else
			{
				return 1;
			}
		}

		#endregion
	}
}
