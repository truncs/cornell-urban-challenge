using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common;
using UrbanChallenge.Arbiter.Core.Common.Reasoning;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Common.Vehicle;

namespace UrbanChallenge.Arbiter.Core.Common.State
{
	public class StoppingState : TravelState, IState
	{
		private ArbiterLane lane;
		private Coordinates stopPoint;
		private Coordinates currentPosition;
		private double timeStamp;
		private InternalState internalState;
		private bool resetLaneAgent = false;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="stopPoint"></param>
		/// <param name="currentPosition"></param>
		/// <param name="timeStamp"></param>
		public StoppingState(ArbiterLane lane, Coordinates stopPoint, Coordinates currentPosition, double timeStamp)
		{
			this.lane = lane;
			this.stopPoint = stopPoint;
			this.currentPosition = currentPosition;
			this.timeStamp = timeStamp;
			this.internalState = new InternalState(lane.LaneId, lane.LaneId);
		}

		#region IState Members

		public string ShortDescription()
		{
			return "Stopping State";
		}

		public string StateInformation()
		{
			return "";
		}

		public string LongDescription()
		{
			return "Stopping State for: " + stopPoint.ToString();			
		}

		public UrbanChallenge.Behaviors.Behavior Resume(VehicleState currentState, double speed)
		{
			double dist = lane.PartitionPath.DistanceBetween(lane.PartitionPath.GetClosest(currentPosition), lane.PartitionPath.GetClosest(stopPoint));
			return new StayInLaneBehavior(lane.LaneId, new StopAtDistSpeedCommand(dist), new List<int>(), lane.LanePath(), lane.Width, lane.NumberOfLanesLeft(currentState.Position, true), lane.NumberOfLanesRight(currentState.Position, true));
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
			get { return true; }
		}

		public InternalState InternalLaneState
		{
			get
			{
				return internalState;
			}
			set
			{
				this.internalState = value;
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
