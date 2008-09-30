using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Arbiter.Core.Common.Reasoning;
using UrbanChallenge.Common.Vehicle;

namespace UrbanChallenge.Arbiter.Core.Common.State
{
	/// <summary>
	/// State of staying in a specified lane
	/// </summary>
	public class StayInLaneState : TravelState, IState
	{
		/// <summary>
		/// Lane to stay in
		/// </summary>
		public ArbiterLane Lane;

		/// <summary>
		/// Waypoints to ignore when planning
		/// </summary>
		public List<IgnorableWaypoint> IgnorableWaypoints;

		/// <summary>
		/// Lane state
		/// </summary>
		private InternalState internalLaneState;

		/// <summary>
		/// Whether or not to reset the lane agent
		/// </summary>
		private bool resetLaneAgent = false;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="lane"></param>
		public StayInLaneState(ArbiterLane lane, IState previous)
		{
			this.Lane = lane;
			this.internalLaneState = new InternalState(lane.LaneId, lane.LaneId, new Probability(0.8, 0.2));
			this.IgnorableWaypoints = new List<IgnorableWaypoint>();
			this.CheckPreviousState(previous);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="lane"></param>
		public StayInLaneState(ArbiterLane lane, Probability confidence, IState previous)
		{
			this.Lane = lane;
			this.internalLaneState = new InternalState(lane.LaneId, lane.LaneId, confidence);
			this.IgnorableWaypoints = new List<IgnorableWaypoint>();
			this.CheckPreviousState(previous);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="lane"></param>
		public StayInLaneState(ArbiterLane lane, Probability confidence, bool resetLaneAgent, IState previous)
		{
			this.Lane = lane;
			this.internalLaneState = new InternalState(lane.LaneId, lane.LaneId, confidence);
			this.resetLaneAgent = resetLaneAgent;
			this.IgnorableWaypoints = new List<IgnorableWaypoint>();
			this.CheckPreviousState(previous);
		}

		/// <summary>
		/// Adds waypoint to ignore list
		/// </summary>
		/// <param name="toIgnore"></param>
		public void IgnoreWaypoint(ArbiterWaypoint toIgnore)
		{
			this.IgnorableWaypoints.Add(new IgnorableWaypoint(toIgnore));
		}

		/// <summary>
		/// Updates waypoint ignore list
		/// </summary>
		public void UpdateIgnoreList()
		{
			List<IgnorableWaypoint> iws = new List<IgnorableWaypoint>();
			foreach (IgnorableWaypoint iw in this.IgnorableWaypoints)
			{
				IgnorableWaypoint iwTmp = new IgnorableWaypoint(iw.Waypoint, iw.numberOfCyclesIgnored + 1);
				if (iwTmp.numberOfCyclesIgnored < 100)
					iws.Add(iwTmp);
			}
			this.IgnorableWaypoints = iws;
		}

		/// <summary>
		/// Waypoints to ignore
		/// </summary>
		public List<ArbiterWaypoint> WaypointsToIgnore
		{
			get
			{
				List<ArbiterWaypoint> ignoreList = new List<ArbiterWaypoint>();
				foreach (IgnorableWaypoint iw in IgnorableWaypoints)
				{
					ignoreList.Add(iw.Waypoint);
				}
				return ignoreList;
			}
			set
			{
				List<IgnorableWaypoint> iws = new List<IgnorableWaypoint>();
				foreach (ArbiterWaypoint aw in value)
				{
					iws.Add(new IgnorableWaypoint(aw));
				}
				this.IgnorableWaypoints = iws;
			}
		}

		/// <summary>
		/// Checks previous state for data ro carry over
		/// </summary>
		public void CheckPreviousState(IState previous)
		{
			if (previous != null && previous is StayInLaneState)
			{
				StayInLaneState sils = (StayInLaneState)previous;
				this.WaypointsToIgnore = sils.WaypointsToIgnore;
			}
			else if (previous != null && previous is StayInSupraLaneState)
			{
				this.WaypointsToIgnore = ((StayInSupraLaneState)previous).WaypointsToIgnore;
			}
		}

		#region IState Members

		public string ShortDescription()
		{
			return "StayInLaneState";
		}

		public string StateInformation()
		{
			return this.Lane.LaneId.ToString();
		}

		public string LongDescription()
		{
			return "Staying in lane: " + Lane.ToString();
		}

		/// <summary>
		/// Resume from pause
		/// </summary>
		/// <returns></returns>
		public Behavior Resume(VehicleState currentState, double speed)
		{
			// new default stay in lane behavior
			Behavior b = new StayInLaneBehavior(Lane.LaneId, new ScalarSpeedCommand(0.0), new int[] { }, Lane.LanePath(), Lane.Width, Lane.NumberOfLanesLeft(currentState.Position, true), Lane.NumberOfLanesRight(currentState.Position, true));

			// set default blinkers
			b.Decorators = this.DefaultStateDecorators;

			// return behavior
			return b;
		}

		public bool CanResume()
		{
			return true;
		}

		public List<BehaviorDecorator> DefaultStateDecorators
		{
			get { return TurnDecorators.NoDecorators; }
		}

		public bool UseLaneAgent
		{
			get { return true; }
		}

		public InternalState InternalLaneState
		{
			get { return internalLaneState; }
			set { this.internalLaneState = value; }
		}

		public bool ResetLaneAgent
		{
			get
			{
				return this.resetLaneAgent;
			}
			set
			{
				this.resetLaneAgent = value;
			}
		}

		#endregion
	}
}
