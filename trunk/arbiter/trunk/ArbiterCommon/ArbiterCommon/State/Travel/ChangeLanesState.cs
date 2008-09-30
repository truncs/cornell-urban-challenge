using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.RndfNetwork;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common.Sensors.Vehicle;

namespace UrbanChallenge.Arbiter.ArbiterCommon
{
	public enum ChangeLanesDirection
	{
		Left,
		Right
	}

	/// <summary>
	/// if the vehicle is changing lanes
	/// </summary>
	public class ChangeLanesState : TravelState, IState
	{
		private bool initialIsOncoming;
		private LaneDescription initialLaneDescription;
		private LaneDescription finalLaneDescription;
		public Path ChangeLanesPath;
		public PointOnPath InitialPosition;
		public PointOnPath LowerBound;
		public PointOnPath UpperBound;
		public Path InitialLanePath;
		public Path FinalLanePath;
		public ChangeLanesDirection LaneChangeDirection;
		public ObservedVehicle[] AssumedVehicles;


		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="initialLane"></param>
		/// <param name="finalLane"></param>
		public ChangeLanesState(bool initialIsOncoming, LaneID initialLane, LaneID finalLane, Path changeLanesPath,
			PointOnPath initialPosition, PointOnPath lowerBound, PointOnPath upperBound) : base(initialLane, finalLane)
		{
			this.initialIsOncoming = initialIsOncoming;
			this.ChangeLanesPath = changeLanesPath;
			this.InitialPosition = initialPosition;
			this.LowerBound = lowerBound;
			this.UpperBound = upperBound;
		}

		/// <summary>
		/// description of the initial lane
		/// </summary>
		public LaneDescription InitialLaneDescription
		{
			get { return initialLaneDescription; }
			set { initialLaneDescription = value; }
		}

		/// <summary>
		/// description of the final lane
		/// </summary>
		public LaneDescription FinalLaneDescription
		{
			get { return finalLaneDescription; }
			set { finalLaneDescription = value; }
		}
		
		/// <summary>
		/// Notification that the initial lane was an oncoming lane
		/// </summary>
		public bool InitialIsOncoming
		{
			get { return initialIsOncoming; }
		}

		/// <summary>
		/// Short description of the state
		/// </summary>
		/// <returns></returns>
		public override string ShortDescription()
		{
			return("ChangeLanes State: " + this.InitialLane.ToString() + " - " + this.FinalLane.ToString());
		}

		/// <summary>
		/// Long description of the state
		/// </summary>
		/// <returns></returns>
		public override string LongDescription()
		{
			return ("Stae: ChangeLanes. " + "Initial: " + this.InitialLane.ToString() + ". - " + "Final: " + this.FinalLane.ToString());
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
