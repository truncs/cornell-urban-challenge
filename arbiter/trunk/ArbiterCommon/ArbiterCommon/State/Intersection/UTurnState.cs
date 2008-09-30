using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.RndfNetwork;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Behaviors;

namespace UrbanChallenge.Arbiter.ArbiterCommon
{
	public class UTurnState : TurnState
	{
		public UTurnBehavior UTurn;
		public bool isResume = false;

		public UTurnState(LaneID initialLane, LaneID finalLane, InterconnectID interconnect, UTurnBehavior uTurn)
			: base(initialLane, finalLane, interconnect, TurnDirection.Straight)
		{
			this.UTurn = uTurn;
		}

		/// <summary>
		/// Short Description of the state
		/// </summary>
		/// <returns></returns>
		public new string ShortDescription()
		{
			return ("UTurn: " + this.InitialLane.ToString() + " - " + this.FinalLane.ToString());
		}

		/// <summary>
		/// Long Description of the state
		/// </summary>
		/// <returns></returns>
		public new string LongDescription()
		{
			return ("State: UTurn. Initial: " + this.InitialLane.ToString() + ". Final: " + this.FinalLane.ToString());
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
