using System;
using System.Collections.Generic;
using System.Text;
using RndfEditor.Display.Utilities;
using UrbanChallenge.Common;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Node in a zone we can navigate with
	/// </summary>
	[Serializable]
	public class ArbiterZoneNavigableNode : INavigableNode
	{
		#region Zone Navigable Node Members

		/// <summary>
		/// Position of the node
		/// </summary>
		private Coordinates position;

		public ArbiterZoneNavigableNode(Coordinates position)
		{
			this.position = position;
			this.outgoingConnections = new List<NavigableEdge>();
		}

		public Coordinates Position
		{
			get { return position; }
			set { position = value; }
		}

		#endregion

		#region INavigableNode Members

		private double timeToHere;
		private double timeToGoal;
		private double time;
		private INavigableNode previous;
		List<NavigableEdge> outgoingConnections;

		public double TimeToHere
		{
			get
			{
				return this.timeToHere;
			}
			set
			{
				this.timeToHere = value;
			}
		}

		public double TimeToGoal
		{
			get
			{
				return this.timeToGoal;
			}
			set
			{
				this.timeToGoal = value;
			}
		}

		public double Time
		{
			get
			{
				return this.time;
			}
			set
			{
				this.time = value;
			}
		}

		public INavigableNode Previous
		{
			get
			{
				return this.previous;
			}
			set
			{
				this.previous = value;
			}
		}

		public List<NavigableEdge> OutgoingConnections
		{
			get
			{
				return this.outgoingConnections;
			}
			set
			{
				this.outgoingConnections = value;
			}
		}

		public double TimeThroughAdjacent(NavigableEdge edge)
		{
			return edge.TimeCost();
		}

		public double TimeTo(INavigableNode node)
		{
			// avg speed of 10mph from place to place
			return this.position.DistanceTo(node.Position) / 2.24;
		}

		public void ResetPlanningCosts()
		{
			this.timeToGoal = 0;
			this.timeToHere = 0;
			this.time = 0;
		}

		public double ExtraTimeCost
		{
			get
			{
				return 0.0;
			}
		}

		public bool EqualsNode(INavigableNode node)
		{
			return this.Equals(node);
		}

		#endregion

		#region IPriorityNode Members

		public string Name
		{
			get
			{
				return this.ToString();
			}
		}

		public double Value
		{
			get
			{
				return this.time;
			}
		}

		#endregion

		#region Standard Equalities

		public override bool Equals(object obj)
		{
			// make sure type same
			if (obj is ArbiterZoneNavigableNode)
			{
				return ((ArbiterZoneNavigableNode)obj).Position.Equals(this.position);
			}

			// otherwise not equal
			return false;
		}

		/// <summary>
		/// Hash code for id
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		/// <summary>
		/// String representation of id
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return this.position.ToString();
		}

		#endregion
	}
}
