using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Intersection.Monitors
{
	public interface IEntryAreaMonitor : IIntersectionQueueable
	{
		bool Failed
		{
			get;
		}
	}
}
