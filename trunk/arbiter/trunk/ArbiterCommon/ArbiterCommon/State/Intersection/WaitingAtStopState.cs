using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.RndfNetwork;

namespace UrbanChallenge.Arbiter.ArbiterCommon
{
	/// <summary>
	/// State of being stopped at an intersection
	/// </summary>
	public class WaitingAtStopState : IntersectionState
	{
		public TurnDirection TurnDirection;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="initial"></param>
		/// <param name="final"></param>
		/// <param name="interconnect"></param>
		/// <param name="turnDirection"></param>
		public WaitingAtStopState(LaneID initial, LaneID final, InterconnectID interconnect, TurnDirection turnDirection)
			: base(initial, final, interconnect)
		{
			this.TurnDirection = turnDirection;
		}
	}
}
