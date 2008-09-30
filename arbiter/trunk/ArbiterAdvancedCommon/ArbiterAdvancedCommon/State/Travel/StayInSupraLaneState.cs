using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.Core.Common.Reasoning;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.Path;

namespace UrbanChallenge.Arbiter.Core.Common.State
{
	public class StayInSupraLaneState : TravelState, IState
	{
		/// <summary>
		/// Supra lane to stay in
		/// </summary>
		public SupraLane Lane;

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
		public StayInSupraLaneState(SupraLane lane, IState previous)
		{
			this.Lane = lane;
			this.internalLaneState = new InternalState(lane.Initial.LaneId, lane.Final.LaneId);
			this.IgnorableWaypoints = new List<IgnorableWaypoint>();
			this.CheckPreviousState(previous);
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
			if (previous != null)
			{
				if (previous is StayInLaneState)
				{
					StayInLaneState sils = (StayInLaneState)previous;
					this.WaypointsToIgnore = sils.WaypointsToIgnore;
				}
				else if (previous is StayInSupraLaneState)
				{
					StayInSupraLaneState sisls = (StayInSupraLaneState)previous;
					this.WaypointsToIgnore = sisls.WaypointsToIgnore;
				}
			}
		}

		#region IState Members

		private bool useLaneAgent = true;

		public string ShortDescription()
		{
			return "SupraLaneState";
		}

		public string LongDescription()
		{
			return "SupraLaneState: " + this.Lane.Initial.ToString() + " -> " + this.Lane.Final.ToString();
		}

		public string StateInformation()
		{
			return this.Lane.Initial.ToString() + " -> " + this.Lane.Final.ToString();
		}

		public UrbanChallenge.Behaviors.Behavior Resume(UrbanChallenge.Common.Vehicle.VehicleState currentState, double speed)
		{
			return new NullBehavior();
		}

		public bool CanResume()
		{
			return true;
		}

		public List<UrbanChallenge.Behaviors.BehaviorDecorator> DefaultStateDecorators
		{
			get { return TurnDecorators.NoDecorators; }
		}

		public bool UseLaneAgent
		{
			get { return this.useLaneAgent; }
		}

		public UrbanChallenge.Arbiter.Core.Common.Reasoning.InternalState InternalLaneState
		{
			get
			{
				return this.internalLaneState;
			}
			set
			{
				this.internalLaneState = value;
			}
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

		/// <summary>
		/// Gets the behavior
		/// </summary>
		/// <param name="sc"></param>
		/// <param name="fron"></param>
		/// <returns></returns>
		public SupraLaneBehavior GetBehavior(SpeedCommand sc, Coordinates front, List<int> ignorable)
		{
			LinePath initPath = this.Lane.InitialPath(front);
			LinePath finPath = this.Lane.FinalPath(front);

			int il = Lane.Initial.NumberOfLanesLeft(front, true);
			int ir = Lane.Initial.NumberOfLanesRight(front, true);
			int fl = Lane.Final.NumberOfLanesLeft(front, true);
			int fr = Lane.Final.NumberOfLanesRight(front, true);

			SupraLaneBehavior slb = new SupraLaneBehavior(
				Lane.Initial.LaneId, initPath, Lane.Initial.Width, il, ir,
				Lane.Final.LaneId, finPath, Lane.Final.Width, fl, fr, sc, ignorable, this.Lane.IntersectionPolygon);

			return slb;
		}

		/// <summary>
		/// Updates the state of the lane
		/// </summary>
		public void UpdateState(Coordinates loc)
		{
			if (Lane.ClosestComponent(loc) == SLComponentType.Interconnect)
				this.useLaneAgent = false;
			else
				this.useLaneAgent = true;
		}
	}
}
