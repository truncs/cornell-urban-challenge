using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;

namespace UrbanChallenge.Arbiter.Core.Common.Reasoning
{
	/// <summary>
	/// Poseterior evidence of what lane we are in
	/// </summary>
	public struct PosteriorEvidence
	{
		/// <summary>
		/// Probability of being in any lane
		/// </summary>
		public Dictionary<ArbiterLane, double> LaneProbabilities;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="laneProbabilities"></param>
		public PosteriorEvidence(Dictionary<ArbiterLane, double> laneProbabilities)
		{
			this.LaneProbabilities = laneProbabilities;
		}
	}
}
