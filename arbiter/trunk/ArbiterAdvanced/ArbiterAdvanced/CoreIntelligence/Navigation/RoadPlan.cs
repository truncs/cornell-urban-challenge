using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Arbiter.Core.Common.Arbiter;
using UrbanChallenge.Common;
using UrbanChallenge.Arbiter.Core.Common.State;
using UrbanChallenge.Common.Path;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Navigation
{
	/// <summary>
	/// Plan over roads
	/// </summary>
	[Serializable]
	public class RoadPlan : INavigationalPlan
	{
		/// <summary>
		/// Plans for each lane
		/// </summary>
		public Dictionary<ArbiterLaneId, LanePlan> LanePlans;		

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="lanePlans"></param>
		public RoadPlan(Dictionary<ArbiterLaneId, LanePlan> lanePlans)
		{
			this.LanePlans = lanePlans;
		}

		/// <summary>
		/// Best lane plan
		/// </summary>
		public LanePlan BestPlan
		{
			get
			{
				double bestTime = Double.MaxValue;
				LanePlan best = null;

				foreach (LanePlan lp in LanePlans.Values)
				{
					if (best == null || lp.laneWaypointOfInterest.TotalTime < bestTime)
					{
						bestTime = lp.laneWaypointOfInterest.TotalTime;
						best = lp;
					}
				}

				return best;
			}
		}

		/// <summary>
		/// Gets route information for sending to remote listeners
		/// </summary>
		public List<RouteInformation> RouteInformation(Coordinates currentPosition)
		{
			List<RouteInformation> routes = new List<RouteInformation>();
			foreach (KeyValuePair<ArbiterLaneId, LanePlan> lp in this.LanePlans)
			{
				List<Coordinates> route = new List<Coordinates>();
				double time = lp.Value.laneWaypointOfInterest.TotalTime;

				// get lane coords
				ArbiterLanePartition current = lp.Value.laneWaypointOfInterest.PointOfInterest.Lane.GetClosestPartition(currentPosition);

				if (CoreCommon.CorePlanningState is StayInSupraLaneState)
				{
					StayInSupraLaneState sisls = (StayInSupraLaneState)CoreCommon.CorePlanningState;

					if(sisls.Lane.ClosestComponent(currentPosition) == SLComponentType.Initial)
					{
						LinePath p = sisls.Lane.LanePath(currentPosition, sisls.Lane.Interconnect.InitialGeneric.Position);
						route.AddRange(p);
						route.Add(sisls.Lane.Interconnect.FinalGeneric.Position);
						current = ((ArbiterWaypoint)sisls.Lane.Interconnect.FinalGeneric).NextPartition;
					}
				}
				
				while (current != null && current.Initial != lp.Value.laneWaypointOfInterest.PointOfInterest)
				{
					route.Add(current.Final.Position);
					current = current.Final.NextPartition;
				}

				// get route coords
				if (lp.Value.laneWaypointOfInterest.BestRoute != null)
				{
					foreach (INavigableNode inn in lp.Value.laneWaypointOfInterest.BestRoute)
					{
						route.Add(inn.Position);
					}
				}

				RouteInformation ri = new RouteInformation(route, time, lp.Value.laneWaypointOfInterest.PointOfInterest.WaypointId.ToString());
				routes.Add(ri);
			}

			routes.Sort();
			return routes;
		}
	}
}
