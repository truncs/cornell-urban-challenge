using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.RndfNetwork;

namespace UrbanChallenge.Arbiter.ArbiterCommon
{
	/// <summary>
	/// State of the vehicle being stopped at a stop line
	/// </summary>
	/// <remarks>different than stopped as exit as requires intersection stop logic</remarks>
	public class StoppedAtStopState : IntersectionState
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
		public StoppedAtStopState(LaneID initialLane, LaneID finalLane, InterconnectID interconnect, LaneDescription currentLaneDescription)
			: base(initialLane, finalLane, interconnect)
		{
			this.currentLaneDescription = currentLaneDescription;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="initialLane"></param>
		/// <param name="finalLane"></param>
		/// <param name="interconnect"></param>
		public StoppedAtStopState(LaneID lane, LaneDescription currentLaneDescription)
			: base(lane, lane)
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
