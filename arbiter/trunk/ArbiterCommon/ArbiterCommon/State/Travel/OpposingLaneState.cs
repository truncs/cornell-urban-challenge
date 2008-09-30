using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.RndfNetwork;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common.Sensors.Vehicle;

namespace UrbanChallenge.Arbiter.ArbiterCommon
{
	/// <summary>
	/// For when the vehicle is in an opposing lane (travelling in the opposite direction)
	/// </summary>
	public class OpposingLaneState : TravelState, IState
	{
		private LaneDescription currentLaneDescription;
		private LaneDescription closestGoodLaneDescription;
		private LaneID closestGoodLane;
		public Path OpposingPath;
		public PointOnPath ReturnPoint;
		public Path ClosestGoodPath;
		public ObservedVehicle[] AssumedVehicles;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="initialLane"></param>
		/// <param name="finalLane"></param>
		/// <param name="closestGoodLane"></param>
		public OpposingLaneState(LaneID lane, LaneID closestGoodLane)
			: base(lane, lane)			
		{
			this.closestGoodLane = closestGoodLane;
		}

		/// <summary>
		/// LaneID of the closest good lane to us
		/// </summary>
		public LaneID ClosestGoodLane
		{
			get { return closestGoodLane; }
			set { closestGoodLane = value; }
		}

		/// <summary>
		/// Closest lane going in the right direction
		/// </summary>
		public LaneDescription ClosestGoodLaneDescription
		{
			get { return closestGoodLaneDescription; }
			set { closestGoodLaneDescription = value; }
		}

		/// <summary>
		/// Current lane we are travelling in
		/// </summary>
		public LaneDescription CurrentLaneDescription
		{
			get { return currentLaneDescription; }
			set { currentLaneDescription = value; }
		}
	}
}
