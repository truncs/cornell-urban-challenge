using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;

namespace UrbanChallenge.Arbiter.Core.Common.Reasoning
{
	/// <summary>
	/// Hidden evidence of what lane we think we're in
	/// </summary>
	public struct InternalState
	{
		/// <summary>
		/// Initial lane
		/// </summary>
		public ArbiterLaneId Initial;

		/// <summary>
		/// Target lane
		/// </summary>
		public ArbiterLaneId Target;

		/// <summary>
		/// Confidence in evidence
		/// </summary>
		public Probability Confidence;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="initial"></param>
		/// <param name="target"></param>
		public InternalState(ArbiterLaneId initial, ArbiterLaneId target)
		{
			this.Initial = initial;
			this.Target = target;
			this.Confidence = new Probability(0.8, 0.2);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="initial"></param>
		/// <param name="target"></param>
		public InternalState(ArbiterLaneId initial, ArbiterLaneId target, Probability confidence)
		{
			this.Initial = initial;
			this.Target = target;
			this.Confidence = confidence;
		}
	}
}
