using System;
using System.Collections.Generic;
using System.Text;

using UrbanChallenge.Common.RndfNetwork;

namespace UrbanChallenge.Arbiter.ArbiterCommon
{
	/// <summary>
	/// Specific State of making a turn
	/// </summary>
	public class TurnState : IntersectionState, IState
	{
		public TurnDirection TurnDirection;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="initialLane"></param>
		/// <param name="finalLane"></param>
		/// <param name="interconnect"></param>
		public TurnState(LaneID initialLane, LaneID finalLane, InterconnectID interconnect, TurnDirection turnDirection) : 
			base(initialLane, finalLane, interconnect)
		{
			this.TurnDirection = turnDirection;
		}		

		/// <summary>
		/// Short Description of the state
		/// </summary>
		/// <returns></returns>
		public new string ShortDescription()
		{
			return ("Turn: " + this.InitialLane.ToString() + " - " + this.FinalLane.ToString());
		}

		/// <summary>
		/// Long Description of the state
		/// </summary>
		/// <returns></returns>
		public new string LongDescription()
		{
			return ("State: Turn. Initial: " + this.InitialLane.ToString() + ". Final: " + this.FinalLane.ToString());
		}

		/// <summary>
		/// String representation of the state
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return this.ShortDescription();
		}
	}
}
