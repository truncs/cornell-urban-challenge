using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.RndfNetwork;

namespace UrbanChallenge.Arbiter.ArbiterCommon
{
	/// <summary>
	/// Traveling on the road
	/// </summary>
	public class TravelState : IState
	{
		protected LaneID initialLane;
		protected LaneID finalLane;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="initialLane"></param>
		/// <param name="finalLane"></param>
		public TravelState(LaneID initialLane, LaneID finalLane)
		{
			this.initialLane = initialLane;
			this.finalLane = finalLane;
		}

		/// <summary>
		/// Initital Lane
		/// </summary>
		public UrbanChallenge.Common.RndfNetwork.LaneID InitialLane
		{
			get { return this.initialLane; }
		}

		/// <summary>
		/// Final Lane
		/// </summary>
		public UrbanChallenge.Common.RndfNetwork.LaneID FinalLane
		{
			get { return this.finalLane; }
		}

		/// <summary>
		/// Short description of the state
		/// </summary>
		/// <returns></returns>
		public virtual string ShortDescription()
		{
			return("Travel State: " + this.InitialLane.ToString() + " - " + this.FinalLane.ToString());
		}

		/// <summary>
		/// Long description of the state
		/// </summary>
		/// <returns></returns>
		public virtual string LongDescription()
		{
			return ("State: Travel. " + "Initial: " + this.InitialLane.ToString() + ". - " + "Final: " + this.FinalLane.ToString());
		}

		/// <summary>
		/// String representation of the state
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return ("Travel State: " + this.InitialLane.ToString() + " - " + this.FinalLane.ToString());
		}
	}
}