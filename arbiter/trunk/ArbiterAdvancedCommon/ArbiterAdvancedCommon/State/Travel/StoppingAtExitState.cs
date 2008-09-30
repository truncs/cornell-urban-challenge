using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Common;
using UrbanChallenge.Arbiter.Core.Common.Reasoning;
using UrbanChallenge.Common.Vehicle;

namespace UrbanChallenge.Arbiter.Core.Common.State
{
	public class StoppingAtExitState : IntersectionState, IState
	{
		private ArbiterLane lane;
		public ArbiterWaypoint waypoint;
		public ArbiterTurnDirection turnDirection;
		private bool isNavigationExit;
		public ArbiterInterconnect desiredExit;
		private InternalState internalLaneState;
		private bool resetLaneAgent = false;
		private double timeStamp;
		public Coordinates currentPosition;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="waypoint"></param>
		/// <param name="turnDirection"></param>
		/// <param name="isNavigationExit"></param>
		/// <param name="desiredExit"></param>
		public StoppingAtExitState(ArbiterLane lane, ArbiterWaypoint waypoint, ArbiterTurnDirection turnDirection,
			bool isNavigationExit, ArbiterInterconnect desiredExit, double timeStamp, Coordinates currentPosition)
		{
			this.lane = lane;
			this.waypoint = waypoint;
			this.turnDirection = turnDirection;
			this.isNavigationExit = isNavigationExit;
			this.desiredExit = desiredExit;
			this.internalLaneState = new InternalState(lane.LaneId, lane.LaneId);
			this.timeStamp = timeStamp;
			this.currentPosition = currentPosition;
			this.internalLaneState = new InternalState(this.lane.LaneId, this.lane.LaneId);
		}

		#region IState Members

		public string ShortDescription()
		{
			return "StoppingAtExitState";
		}

		public string StateInformation()
		{
			return this.waypoint.ToString();;
		}

		public string LongDescription()
		{
			return "Stopping At Exit: " + waypoint.ToString();
		}

		public Behavior Resume(VehicleState currentState, double speed)
		{
			// get dist
			double dist = lane.PartitionPath.DistanceBetween(lane.PartitionPath.GetClosest(currentPosition), lane.PartitionPath.GetClosest(waypoint.Position));
			return new StayInLaneBehavior(lane.LaneId, new StopAtDistSpeedCommand(dist), new List<int>(), lane.LanePath(), lane.Width, lane.NumberOfLanesLeft(currentState.Position, true), lane.NumberOfLanesRight(currentState.Position, true));
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
