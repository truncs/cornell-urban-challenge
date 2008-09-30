using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.RndfNetwork;

namespace UrbanChallenge.Arbiter.ArbiterCommon
{
	/// <summary>
	/// Type of stop
	/// </summary>
	public enum StopType
	{
		Exit,
		StopLine,
		LastGoal
	}

	/// <summary>
	/// Type of action
	/// </summary>
	public enum ActionType
	{
		Goal,
		Exit,
		Stop,
		LastGoal
	}

	/// <summary>
	/// State of stopping for a certain waypoint
	/// </summary>
	public class StoppingState : TravelState
	{
		private StopType stopType;
		private RndfWaypointID waypoint;
		private LaneDescription currentLane;
		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="waypoint"></param>
		/// <param name="stopType"></param>
		public StoppingState(LaneID lane, RndfWaypointID waypoint, StopType stopType, LaneDescription currentLane)
			: base(lane, lane)
		{
			this.stopType = stopType;
			this.waypoint = waypoint;
			this.currentLane = currentLane;
		}

		/// <summary>
		/// Waypoint we are stopping at
		/// </summary>
		public RndfWaypointID Waypoint
		{
			get { return waypoint; }
			set { waypoint = value; }
		}

		/// <summary>
		/// Why we are stopping at the waypoint
		/// </summary>
		public StopType StopType
		{
			get { return stopType; }
			set { stopType = value; }
		}

		/// <summary>
		/// description of the current lane and our goal
		/// </summary>
		public LaneDescription CurrentLane
		{
			get { return currentLane; }
			set { currentLane = value; }
		}
	}
}
