using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Defines a node which can be navigated over
	/// </summary>
	public interface INavigableNode : IPriorityNode
	{
		/// <summary>
		/// Approximate time it would take to get to here, seconds
		/// </summary>
		double TimeToHere
		{
			get;
			set;
		}

		/// <summary>
		/// Approximate time from here to the goal, seconds
		/// </summary>
		double TimeToGoal
		{
			get;
			set;
		}

		/// <summary>
		/// Approximate total time of the route going through this INavigableNode
		/// </summary>
		double Time
		{
			get;
			set;
		}

		/// <summary>
		/// The INavigableNode this one was visited from
		/// </summary>
		INavigableNode Previous
		{
			get;
			set;
		}

		/// <summary>
		/// Nodes reachable leaving this node
		/// </summary>
		List<NavigableEdge> OutgoingConnections
		{
			get;
			set;
		}

		/// <summary>
		/// Exacting method for determining the time it would take to travel the current IPlanable node to reach a specific connection
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		double TimeThroughAdjacent(NavigableEdge edge);

		/// <summary>
		/// Heuristic for approximating the time from the current IPlanable node to any node on the graph
		/// </summary>
		/// <param name="goal"></param>
		/// <returns></returns>
		double TimeTo(INavigableNode node);

		/// <summary>
		/// As multiple routes will be planned in each iteration, make sure to node add on costs from multiple routes to the cost of one
		/// </summary>
		void ResetPlanningCosts();

		/// <summary>
		/// Extra time cost going through this node
		/// </summary>
		double ExtraTimeCost
		{
			get;
		}

		/// <summary>
		/// Position of node
		/// </summary>
		Coordinates Position
		{
			get;
			set;
		}

		/// <summary>
		/// If equals node
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		bool EqualsNode(INavigableNode node);
	}
}
