using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.RndfNetwork;

namespace UrbanChallenge.Arbiter.ArbiterCommon
{
	/// <summary>
	/// State of the vehicle being in an intersection
	/// </summary>
	public class IntersectionState : IState
	{
		private LaneID initialLane;
		private LaneID finalLane;
		private InterconnectID interconnect;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="initialLane"></param>
		/// <param name="finalLane"></param>
		/// <param name="interconnect"></param>
		public IntersectionState(LaneID initialLane, LaneID finalLane, InterconnectID interconnect)
		{
			this.initialLane = initialLane;
			this.finalLane = finalLane;
			this.interconnect  = interconnect;
		}

		/// <summary>
		/// Constructor if we are to stay in the current lane
		/// </summary>
		/// <param name="initialLane"></param>
		/// <param name="finalLane"></param>
		/// <param name="interconnect"></param>
		public IntersectionState(LaneID initialLane, LaneID finalLane)
		{
			this.initialLane = initialLane;
			this.finalLane = finalLane;
		}

		/// <summary>
		/// ID of the interconnect we are on
		/// </summary>
		public InterconnectID Interconnect
		{
			get { return interconnect; }
		}

		/// <summary>
		/// Initial Lane the vehicle is in
		/// </summary>
		public LaneID InitialLane
		{
			get { return this.initialLane; }
		}

		/// <summary>
		/// Final Lane the vehicle is going to
		/// </summary>
		public LaneID FinalLane
		{
			get { return this.finalLane; }
		}

		/// <summary>
		/// Short Description of the state
		/// </summary>
		/// <returns></returns>
		public string ShortDescription()
		{
			return ("Intersection: " + this.InitialLane.ToString() + " - " + this.FinalLane.ToString());
		}

		/// <summary>
		/// Long Description of the state
		/// </summary>
		/// <returns></returns>
		public string LongDescription()
		{
			return ("State: Intersection. Initial: " + this.InitialLane.ToString() + ". Final: " + this.FinalLane.ToString());
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
