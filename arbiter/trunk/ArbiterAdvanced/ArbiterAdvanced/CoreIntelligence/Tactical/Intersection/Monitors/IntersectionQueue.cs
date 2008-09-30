using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Intersection
{
	public class IntersectionQueue
	{
		/// <summary>
		/// Exit the queue is monitoring if ready for
		/// </summary>
		public ITraversableWaypoint exit;

		/// <summary>
		/// Specific connection queueing for
		/// </summary>
		/// <remarks>Otherwise assume no specific connection</remarks>
		public ArbiterInterconnect specificConnection;

		/// <summary>
		/// Lanes that have priority over this
		/// </summary>
		public List<PriorityLane> priorityLanes;

		/// <summary>
		/// Possible dependencies of this queued exit
		/// </summary>
		public List<IntersectionQueue> possibleDependencies;
	}
}
