using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Navigation
{
	/// <summary>
	/// Interconnect we planned over
	/// </summary>
	public class PlanableInterconnect
	{
		/// <summary>
		/// Interconnect to take
		/// </summary>
		public ArbiterInterconnect Interconnect;

		/// <summary>
		/// Time cost from the itnerconnect
		/// </summary>
		public double TimeCost;

		/// <summary>
		/// Route plan from this
		/// </summary>
		public List<INavigableNode> PlannedNodes;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="interconnect"></param>
		/// <param name="timeCost"></param>
		/// <param name="nodes"></param>
		public PlanableInterconnect(ArbiterInterconnect interconnect, double timeCost, List<INavigableNode> nodes)
		{
			this.Interconnect = interconnect;
			this.TimeCost = timeCost;
			this.PlannedNodes = nodes;
		}
	}
}
