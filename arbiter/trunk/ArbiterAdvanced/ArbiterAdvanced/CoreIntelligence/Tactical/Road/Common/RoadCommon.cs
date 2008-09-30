using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common.Vehicle;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road
{
	/// <summary>
	/// Standard functions across the road tactical
	/// </summary>
	public static class RoadCommon
	{
		/// <summary>
		/// Minimum distance ahead of us we can change lanes within
		/// </summary>
		public static double MinimumForwardChangeLanesDistance = TahoeParams.VL;
	}
}
