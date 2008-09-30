using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;

namespace UrbanChallenge.Arbiter.Core.Common.State
{
	/// <summary>
	/// Navigating from one place to another in a zone
	/// </summary>
	public class ZoneState
	{
		/// <summary>
		/// Zone
		/// </summary>
		public ArbiterZone Zone;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="zone"></param>
		public ZoneState(ArbiterZone zone)
		{
			this.Zone = zone;
		}
	}
}
