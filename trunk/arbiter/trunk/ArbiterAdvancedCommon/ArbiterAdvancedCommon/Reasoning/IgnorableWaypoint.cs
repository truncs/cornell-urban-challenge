using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;

namespace UrbanChallenge.Arbiter.Core.Common.Reasoning
{
	/// <summary>
	/// Waypoint we can ignore
	/// </summary>
	/// <remarks>Also has num cycles we have ignored this waypoint</remarks>
	public struct IgnorableWaypoint
	{
		/// <summary>
		/// Number of cycles ignored
		/// </summary>
		public int numberOfCyclesIgnored;

		/// <summary>
		/// Waypoint to ignore
		/// </summary>
		public ArbiterWaypoint Waypoint;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="aw"></param>
		public IgnorableWaypoint(ArbiterWaypoint aw)
		{
			this.Waypoint = aw;
			this.numberOfCyclesIgnored = 0;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="aw"></param>
		/// <param name="cycles"></param>
		public IgnorableWaypoint(ArbiterWaypoint aw, int cycles)
		{
			this.Waypoint = aw;
			this.numberOfCyclesIgnored = cycles;
		}
	}
}
