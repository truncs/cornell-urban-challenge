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
	/// Stop at a stop
	/// </summary>
	public class StoppingAtStopState : TravelState, IState
	{
		private ArbiterLane lane;
		public ArbiterWaypoint waypoint;
		public ArbiterTurnDirection turnDirection;
		private bool isNavigationExit;
		public ArbiterInterconnect desiredExit;
		private InternalState internalLaneState;
		private bool resetLaneAgent = false;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="waypoint"></param>
		/// <param name="turnDirection"></param>
		/// <param name="isNavigationExit"></param>
		/// <param name="desiredExit"></param>
		public StoppingAtStopState(ArbiterLane lane, ArbiterWaypoint waypoint, ArbiterTurnDirection turnDirection,
			bool isNavigationExit, ArbiterInterconnect desiredExit)
		{
			this.lane = lane;
			this.waypoint = waypoint;
			this.turnDirection = turnDirection;
			this.isNavigationExit = isNavigationExit;
			this.desiredExit = desiredExit;
			this.internalLaneState = new InternalState(lane.LaneId, lane.LaneId);
		}

		#region IState Members

		public string ShortDescription()
		{
			return "StoppingAtStopState";
		}

		public string StateInformation()
		{
			return waypoint.ToString();
		}

		public string LongDescription()
		{
			return "Stopping At Stop: " + waypoint.ToString();
		}

		public UrbanChallenge.Behaviors.Behavior Resume(VehicleState currentState, double speed)
		{
			return new StayInLaneBehavior(lane.LaneId, new StopAtLineSpeedCommand(), new List<int>(), lane.LanePath(), lane.Width, lane.NumberOfLanesLeft(currentState.Position, true), lane.NumberOfLanesRight(currentState.Position, true));
		}

		public bool CanResume()
		{
			return true;
		}

		public List<UrbanChallenge.Behaviors.BehaviorDecorator> DefaultStateDecorators
		{
			get 
			{
				switch (turnDirection)
				{
					case ArbiterTurnDirection.Left:
						return TurnDecorators.LeftTurnDecorator;
					case ArbiterTurnDirection.Right:
						return TurnDecorators.RightTurnDecorator;
					case ArbiterTurnDirection.Straight:
						return TurnDecorators.NoDecorators;
				}

				return TurnDecorators.NoDecorators;
			}
		}

		public bool UseLaneAgent
		{
			get { return false; }
		}

		public InternalState InternalLaneState
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
	}
}
