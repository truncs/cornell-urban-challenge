using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.RndfNetwork;

namespace UrbanChallenge.Arbiter.ArbiterCommon
{
	/// <summary>
	/// State of the vehicle being stopped at an exit
	/// </summary>
	public class StoppedAtExitState : IntersectionState
	{
		/// <summary>
		/// descripltion of our current lane
		/// </summary>
		private LaneDescription currentLaneDescription;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="initialLane"></param>
		/// <param name="finalLane"></param>
		/// <param name="interconnect"></param>
		public StoppedAtExitState(LaneID initialLane, LaneID finalLane, InterconnectID interconnect, LaneDescription currentLaneDescription)
			: base(initialLane, finalLane, interconnect)
		{
			this.currentLaneDescription = currentLaneDescription;
		}

		/// <summary>
		/// description of our current lane
		/// </summary>
		public LaneDescription LaneDescription
		{
			get { return currentLaneDescription; }
			set { currentLaneDescription = value; }
		}
	}
}
