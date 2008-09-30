using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Navigation;
using UrbanChallenge.Common;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	public interface INavigableTravelArea
	{
		List<DownstreamPointOfInterest> Downstream(Coordinates currentPosition, List<ArbiterWaypoint> ignorable, INavigableNode goal);
	}
}
