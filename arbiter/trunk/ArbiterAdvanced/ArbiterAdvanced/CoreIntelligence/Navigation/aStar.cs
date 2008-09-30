using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Navigation
{
	/// <summary>
	/// Plans a path from start to final INavigableNode nodes
	/// </summary>
	public class aStar
	{
		// Declare Required Structures
		private PriorityHeap open;
		private Dictionary<string, INavigableNode> closed;

		// Declare Required Nodes
		private INavigableNode start;
		private INavigableNode goal;

		// nodes not to plan over
		List<NavigableEdge> removedEdges;

		// Overall Path Time
		double totalTime;
		public double TotalTime
		{
			get { return totalTime; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="start">Node to start plan</param>
		/// <param name="goal">Node to end plan</param>
		/// <param name="removed">Nodes to not plan over</param>
		public aStar(INavigableNode start, INavigableNode goal, List<NavigableEdge> removed)
		{
			// initialize OPEN and CLOSED lists
			this.open = new PriorityHeap();
			this.closed = new Dictionary<string, INavigableNode>(CoreCommon.RoadNetwork.ArbiterWaypoints.Count);

			// set start and null backpointer
			this.start = start;
			this.start.Previous = null;
			this.start.ResetPlanningCosts();

			// set goal and null backpointer
			this.goal = goal;
			this.goal.Previous = null;
			this.goal.ResetPlanningCosts();

			// set removed
			this.removedEdges = removed;
		}

		/// <summary>
		/// Plans a path from start to goal
		/// </summary>
		/// <returns>List of INavigableNode nodes in order of travel</returns>
		public List<INavigableNode> Plan()
		{
			// 1. Add Start to Open list
			open.Push(start);

			// 2. While Open list not empty
			// Console.WriteLine("2");
			while (open.Count != 0)
			{
				// 3. Return and Remove node with lowest Cost from Open list
				INavigableNode current = (INavigableNode)open.Pop();

				// 4. If Current == Goal then we have found solution, terminate
				if (current.EqualsNode(goal))
				{
					goal = current;
					break;
				}

				// 5. Otherwise get Successors of Current
				List<NavigableEdge> successors = current.OutgoingConnections;

				// 6. For each successor
				foreach (NavigableEdge successor in successors)
				{
					// 6.5 make sure successor is allowed to be planned over
					if (this.removedEdges == null || !this.removedEdges.Contains(successor))
					{
						// 7. Get the cost estimate for the successor
						double timeToHere = current.TimeToHere + current.TimeThroughAdjacent(successor);
						double timeToGoal = successor.End.TimeTo(goal);
						double time = timeToHere + timeToGoal;

						// flag to skip this successor
						bool skip = false;

						// 8. Skip successor if node is closed and we've not found a better node
						if (closed.ContainsKey(successor.End.Name))
						{
							// 9. Get the matching node on the closed list
							INavigableNode closedCopy = closed[successor.End.Name];

							// 10. If we did not find a shorter route, skip; else remove from closed
							if (closedCopy.Time <= time)
							{
								skip = true;
							}
							else
							{
								closed.Remove(successor.End.Name);
							}
						}
						// 11. Skip if node is in open and we've not found a better node
						if (!skip && open.Contains(successor.End.Name))
						{
							// 12. Find record in open list
							INavigableNode openCopy = (INavigableNode)open.Find(successor.End.Name);

							// 13. If we did not find a shorter route, skip; else remove from Open
							if (openCopy.Time <= time)
							{
								skip = true;
							}
							else
							{
								open.Remove(successor.End.Name);
							}
						}
						// 14. If we're not to skip, it's an unvisited node or we've found a better path to a node we've previously visited
						if (!skip)
						{
							// 15. Set costs for successor
							successor.End.Previous = current;
							successor.End.TimeToHere = timeToHere;
							successor.End.TimeToGoal = timeToGoal;
							successor.End.Time = time;

							// 16. Add successor to open list
							open.Push(successor.End);
						}
					}
				}

				// 17. Add current to closed list
				closed.Add(current.Name, current);

			} // end while

			// check that we have planned to here
			if (goal.Previous != null)
			{
				// 18. Set total time to get to the goal
				totalTime = goal.TimeToHere;

				// 19. Reverse the route here TESTING purposes for the moment
				List<INavigableNode> route = new List<INavigableNode>();
				INavigableNode temp = goal;
				while (temp.Previous != null)
				{
					route.Add(temp);
					temp = temp.Previous;
				}
				route.Add(temp);
				route.Reverse();

				return route;
			}
			else
			{
				if (start.Equals(goal))
				{
					this.totalTime = 0.0;
					return new List<INavigableNode>();
				}
				else
				{
					this.totalTime = Double.MaxValue;
					return new List<INavigableNode>();
				}
			}

		} // end Plan()

	}
}
