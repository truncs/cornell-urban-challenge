using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Intersection.Monitors
{
	public interface IDominantMonitor : IIntersectionQueueable
	{
		IntersectionInvolved Involved
		{
			get;
		}

		int CompareToOtherDominant(IDominantMonitor other);

		bool Waiting
		{
			get;
		}

		bool WaitingTimerRunning
		{
			get;
		}

		bool ExcessiveWaiting
		{
			get;
		}

		int SecondsWaiting
		{
			get;
		}
	}
}
