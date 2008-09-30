using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.Vehicle;

namespace UrbanChallenge.Arbiter.Core.Common.State
{
	public class TurnState : IntersectionState, IState
	{
		public ArbiterInterconnect Interconnect;
		public ArbiterTurnDirection turnDirection;
		public ArbiterLane TargetLane;
		public LinePath EndingPath;
		public LineList LeftBound;
		public LineList RightBound;
		public SpeedCommand SpeedCommand;
		public SAUDILevel Saudi;
		public bool UseTurnBounds;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="interconnect"></param>
		/// <param name="turn"></param>
		public TurnState(ArbiterInterconnect interconnect, ArbiterTurnDirection turn, ArbiterLane target, LinePath endingPath, LineList left,
			LineList right, SpeedCommand speed)
		{
			this.Interconnect = interconnect;
			this.turnDirection = turn;
			this.TargetLane = target;
			this.EndingPath = endingPath;
			this.LeftBound = left;
			this.RightBound = right;
			this.SpeedCommand = speed;
			this.Saudi = SAUDILevel.None;
			this.UseTurnBounds = true;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="interconnect"></param>
		/// <param name="turn"></param>
		public TurnState(ArbiterInterconnect interconnect, ArbiterTurnDirection turn, ArbiterLane target, LinePath endingPath, LineList left,
			LineList right, SpeedCommand speed, SAUDILevel saudi, bool useTurnBounds)
		{
			this.Interconnect = interconnect;
			this.turnDirection = turn;
			this.TargetLane = target;
			this.EndingPath = endingPath;			
			this.LeftBound = left;
			this.RightBound = right;
			this.SpeedCommand = speed;
			this.Saudi = saudi;
			this.UseTurnBounds = useTurnBounds;
		}

		#region IState Members

		public string ShortDescription()
		{
			return "TurnState";
		}

		public string StateInformation()
		{
			return Interconnect.ToString();
		}

		public string LongDescription()
		{
			return "Turn State: " + Interconnect.ToString();
		}

		public UrbanChallenge.Behaviors.Behavior Resume(VehicleState currentState, double speed)
		{
			TurnBehavior turnBehavior = null;
			if(TargetLane != null)
				turnBehavior = new TurnBehavior(TargetLane.LaneId, EndingPath, LeftBound, RightBound, SpeedCommand, this.Interconnect.InterconnectId);
			else
				turnBehavior = new TurnBehavior(null, EndingPath, LeftBound, RightBound, SpeedCommand, this.Interconnect.InterconnectId);

			turnBehavior.TimeStamp = currentState.Timestamp;
			return turnBehavior;
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
				throw new Exception("The method or operation is not implemented.");
			}
			set
			{
				throw new Exception("The method or operation is not implemented.");
			}
		}

		#endregion
	}
}
