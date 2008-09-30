using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Navigation
{
	/// <summary>
	/// Plan over a lane
	/// </summary>
	[Serializable]
	public class LanePlan
	{
		/// <summary>
		/// desired waypoint of action from this lane
		/// </summary>
		public DownstreamPointOfInterest laneWaypointOfInterest;

		/// <summary>
		/// constructor
		/// </summary>
		/// <param name="point"></param>
		public LanePlan(DownstreamPointOfInterest point)
		{
			this.laneWaypointOfInterest = point;
		}

		/// <summary>
		/// Lane id of the plane
		/// </summary>
		public ArbiterLaneId Lane
		{
			get
			{
				return this.laneWaypointOfInterest.PointOfInterest.Lane.LaneId;
			}
		}
	}
}
