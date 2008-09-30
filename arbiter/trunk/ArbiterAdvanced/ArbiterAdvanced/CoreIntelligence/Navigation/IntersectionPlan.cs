using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Arbiter.Core.Common.Arbiter;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Navigation
{
	/// <summary>
	/// Result of navigation plan at an intersection
	/// </summary>
	public class IntersectionPlan : INavigationalPlan
	{
		/// <summary>
		/// Waypoint we are exiting from
		/// </summary>
		public ITraversableWaypoint ExitWaypoint;

		/// <summary>
		/// Possible entries and their route costs
		/// </summary>
		public List<PlanableInterconnect> PossibleEntries;

		/// <summary>
		/// Plan if we stay in our current segment by planning from final waypoint of next partition in lane
		/// </summary>
		/// <remarks>null if end of road</remarks>
		public RoadPlan SegmentPlan;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="waypoint"></param>
		/// <param name="entries"></param>
		public IntersectionPlan(ITraversableWaypoint waypoint, List<PlanableInterconnect> entries, RoadPlan roadPlan)
		{
			this.ExitWaypoint = waypoint;
			this.PossibleEntries = entries;
			this.SegmentPlan = roadPlan;
		}

		/// <summary>
		/// Gets the best option from the plan
		/// </summary>
		public IConnectAreaWaypoints BestOption
		{
			get
			{
				PlanableInterconnect bestInter = null;

				foreach(PlanableInterconnect tmp in PossibleEntries)
				{
					if (bestInter == null || tmp.TimeCost < bestInter.TimeCost)
						bestInter = tmp;
				}

				if (SegmentPlan != null)
				{
					LanePlan lp = SegmentPlan.BestPlan;

					if (bestInter == null || (lp.laneWaypointOfInterest.TotalTime < bestInter.TimeCost))
						return ((ArbiterWaypoint)ExitWaypoint).NextPartition;
				}

				return bestInter != null ? bestInter.Interconnect : null;
			}
		}

		public double BestRouteTime
		{
			get
			{
				PlanableInterconnect bestInter = null;

				foreach (PlanableInterconnect tmp in PossibleEntries)
				{
					if (bestInter == null || tmp.TimeCost < bestInter.TimeCost)
						bestInter = tmp;
				}

				if (SegmentPlan != null)
				{
					LanePlan lp = SegmentPlan.BestPlan;

					if (bestInter == null || (lp.laneWaypointOfInterest.TotalTime < bestInter.TimeCost))
						return SegmentPlan.BestPlan.laneWaypointOfInterest.RouteTime;
				}

				return bestInter != null ? bestInter.TimeCost : double.MaxValue;
			}
		}
	}
}
