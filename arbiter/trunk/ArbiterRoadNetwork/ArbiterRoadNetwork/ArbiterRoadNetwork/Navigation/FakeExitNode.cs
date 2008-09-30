using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Creates a fake exit node used only to plan over its interconnects
	/// </summary>
	[Serializable]
	public class FakeExitNode : INavigableNode
	{
		private INavigableNode exit;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="exit"></param>
		public FakeExitNode(INavigableNode exit)
		{
			this.exit = exit;
		}

		#region INavigableNode Members

		public double TimeToHere
		{
			get
			{
				return exit.TimeToHere;
			}
			set
			{
				exit.TimeToHere = value;
			}
		}

		public double TimeToGoal
		{
			get
			{
				return exit.TimeToGoal;
			}
			set
			{
				exit.TimeToGoal = value;
			}
		}

		public double Time
		{
			get
			{
				return exit.Time;
			}
			set
			{
				exit.Time = value;
			}
		}

		public INavigableNode Previous
		{
			get
			{
				return exit.Previous;
			}
			set
			{
				exit.Previous = value;
			}
		}

		public List<NavigableEdge> OutgoingConnections
		{
			get
			{
				List<NavigableEdge> nes = new List<NavigableEdge>();
				foreach (NavigableEdge ne in exit.OutgoingConnections)
				{
					if (ne is ArbiterInterconnect)
						nes.Add(ne);
				}
				return nes;
			}
			set
			{
				
			}
		}

		public double TimeThroughAdjacent(NavigableEdge edge)
		{
			return exit.TimeThroughAdjacent(edge);
		}

		public double TimeTo(INavigableNode node)
		{
			return exit.TimeTo(node);
		}

		public void ResetPlanningCosts()
		{
			exit.ResetPlanningCosts();
		}

		public double ExtraTimeCost
		{
			get { return exit.ExtraTimeCost; }
		}

		public UrbanChallenge.Common.Coordinates Position
		{
			get { return exit.Position; }
			set { exit.Position = value; }
		}

		public bool EqualsNode(INavigableNode node)
		{
			return exit.EqualsNode(node);
		}

		#endregion

		#region IPriorityNode Members

		public string Name
		{
			get { return exit.Name; }
		}

		public double Value
		{
			get { return exit.Value; }
		}

		#endregion

		/// <summary>
		/// Gets edge going to input node from this exit
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public ArbiterInterconnect GetEdge(INavigableNode node)
		{
			foreach (NavigableEdge ne in exit.OutgoingConnections)
			{
				if (ne is ArbiterInterconnect)
				{
					ArbiterInterconnect ai = (ArbiterInterconnect)ne;

					if (ai.FinalGeneric.Equals(node))
						return ai;
				}
			}

			return null;
		}

		public override string ToString()
		{
			return this.exit.ToString();
		}
	}
}
