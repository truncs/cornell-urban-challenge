using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Common.Vehicle;

namespace UrbanChallenge.Arbiter.Core.Common.State
{
	public enum TurnTestState
	{
		Unknown = 0,
		Completed = 1,
		Failed = 2
	}

	/// <summary>
	/// Waiting at an exit
	/// </summary>
	/// <remarks>imples right of way besides those interconnects in opposite way we travel over</remarks>
	public class WaitingAtIntersectionExitState : IntersectionState, IState
	{
		public ITraversableWaypoint exitWaypoint;
		private ArbiterTurnDirection turnDirection;
		public IntersectionDescription IntersectionDescription;
		public ArbiterInterconnect desired;
		public TurnTestState turnTestState;
		public SAUDILevel saudi = SAUDILevel.None;
		public bool useTurnBounds = true;

		public WaitingAtIntersectionExitState(ITraversableWaypoint exit, ArbiterTurnDirection turnDirection, IntersectionDescription description, ArbiterInterconnect desired)
		{
			this.exitWaypoint = exit;
			this.turnDirection = turnDirection;
			this.IntersectionDescription = description;
			this.desired = desired;
			this.turnTestState = TurnTestState.Unknown;
		}

		#region IState Members

		public string ShortDescription()
		{
			return "WaitingAtIntersectionExitState";
		}

		public string StateInformation()
		{
			return exitWaypoint.ToString();
		}

		public string LongDescription()
		{
			return "Waiting At Intersection Exit: " + exitWaypoint.ToString();
		}

		public UrbanChallenge.Behaviors.Behavior Resume(VehicleState currentState, double speed)
		{
			return new HoldBrakeBehavior();
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

		public UrbanChallenge.Arbiter.Core.Common.Reasoning.InternalState InternalLaneState
		{
			get
			{
				throw new Exception("The method or operation is not implemented.");
			}
			set
			{
				throw new Exception("The method or operation is not implemented.");
			}
		}

		public bool ResetLaneAgent
		{
			get
			{
				return true;
			}
			set
			{
				throw new Exception("The method or operation is not implemented.");
			}
		}

		#endregion
	}
}
