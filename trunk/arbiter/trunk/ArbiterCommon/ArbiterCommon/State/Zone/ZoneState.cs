using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.RndfNetwork;

namespace UrbanChallenge.Arbiter.ArbiterCommon
{
	/// <summary>
	/// The vehicle is in a zone
	/// </summary>
	public class ZoneState : IState
	{
		private LaneID inLane;
		private LaneID outLane;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="lane">Lane to stay in</param>
		public ZoneState(LaneID inLane, LaneID outLane)
		{
			// set lane
			this.inLane = inLane;
			this.outLane = outLane;
		}

		/// <summary>
		/// Initial Lane the vehicle entered the zone from
		/// </summary>
		public UrbanChallenge.Common.RndfNetwork.LaneID InitialLane
		{
			get { return this.inLane; }
		}

		/// <summary>
		/// Final Lane the vehicle is going to exit the zone from
		/// </summary>
		public UrbanChallenge.Common.RndfNetwork.LaneID FinalLane
		{
			get { return this.outLane; }
		}

		/// <summary>
		/// Short Description of the state
		/// </summary>
		/// <returns></returns>
		public string ShortDescription()
		{
			return ("Zone: " + this.InitialLane.ToString() + " - " + this.FinalLane.ToString());
		}

		/// <summary>
		/// Long Description of the state
		/// </summary>
		/// <returns></returns>
		public string LongDescription()
		{
			return ("State: Zone. Initial: " + this.InitialLane.ToString() + ". Final: " + this.FinalLane.ToString());
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
