using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.RndfNetwork;

namespace UrbanChallenge.Arbiter.ArbiterCommon
{
	/// <summary>
	/// Specific state of staying in the current  lane
	/// </summary>
	public class StayInLaneState : TravelState, IState
	{
		private LaneDescription currentLaneDescription;
		private LaneDescription leftLaneDescription;
		private LaneDescription rightLaneDescription;		

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="lane">Lane to stay in</param>
		public StayInLaneState(LaneID lane) : base(lane, lane)
		{
		}

		/// <summary>
		/// Description of the current Lane
		/// </summary>
		public LaneDescription CurrentLaneDescription
		{
			get { return currentLaneDescription; }
			set { currentLaneDescription = value; }
		}

		/// <summary>
		/// Description of the left Lane
		/// </summary>
		/// <remarks>Multi lane contains info about other lanes to left possibly</remarks>
		public LaneDescription LeftLaneDescription
		{
			get { return leftLaneDescription; }
			set { leftLaneDescription = value; }
		}

		/// <summary>
		/// Description of the multiple left lanes
		/// </summary>
		/// <remarks>Multi lane contains info about other lanes to left possibly</remarks>
		public MultiLaneDescription LeftLanesDescription
		{
			get { return (MultiLaneDescription)leftLaneDescription; }
			set { leftLaneDescription = value; }
		}

		/// <summary>
		/// Description of the right Lane
		/// </summary>
		/// <remarks>Multi lane contains info about other lanes to right possibly</remarks>
		public LaneDescription RightLaneDescription
		{
			get { return rightLaneDescription; }
			set { rightLaneDescription = value; }
		}

		/// <summary>
		/// Description of the multiple right lanes
		/// </summary>
		/// <remarks>Multi lane contains info about other lanes to right possibly</remarks>
		public MultiLaneDescription RightLanesDescription
		{
			get { return (MultiLaneDescription)rightLaneDescription; }
			set { rightLaneDescription = value; }
		}

		/// <summary>
		/// Initial Lane the vehicle is in
		/// </summary>
		public UrbanChallenge.Common.RndfNetwork.LaneID Lane
		{
			get { return this.initialLane; }
		}		

		/// <summary>
		/// Short Description of the state
		/// </summary>
		/// <returns></returns>
		public override string ShortDescription()
		{
			return ("StayInLane: " + this.InitialLane.ToString() + " - " + this.FinalLane.ToString());
		}

		/// <summary>
		/// Long Description of the state
		/// </summary>
		/// <returns></returns>
		public override string LongDescription()
		{
			return ("State: StayInLane. Initial: " + this.InitialLane.ToString() + ". Final: " + this.FinalLane.ToString());
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
