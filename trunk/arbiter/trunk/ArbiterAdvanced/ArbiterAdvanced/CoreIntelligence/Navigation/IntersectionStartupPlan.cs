using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Navigation
{
	public class IntersectionStartupPlan : INavigationalPlan
	{
		public Dictionary<ITraversableWaypoint, double> NodeTimeCosts;

		public IntersectionStartupPlan(Dictionary<ITraversableWaypoint, double> nodeTimeCosts)
		{
			this.NodeTimeCosts = nodeTimeCosts;
		}
	}
}
