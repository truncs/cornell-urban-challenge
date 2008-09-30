using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.Vehicle;

namespace UrbanChallenge.Arbiter.Core.Common.State
{
	public class uTurnState : IntersectionState, IState
	{
		/// <summary>
		/// Lane we wish to end up in
		/// </summary>
		public ArbiterLane TargetLane;

		/// <summary>
		/// Uturn polygon
		/// </summary>
		public Polygon Polygon;

		public ArbiterInterconnect Interconnect;

		public bool singleLaneUturn;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="target"></param>
		public uTurnState(ArbiterLane target, Polygon p)
		{
			this.TargetLane = target;
			this.Polygon = p;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="target"></param>
		public uTurnState(ArbiterLane target, Polygon p, bool singleLaneUturn)
		{
			this.TargetLane = target;
			this.Polygon = p;
			this.singleLaneUturn = singleLaneUturn;
		}

		/// <summary>
		/// Default behavior to produce the uturn
		/// </summary>
		public Behavior DefaultBehavior;

		#region IState Members

		public string ShortDescription()
		{
			return "uTurnState";
		}

		public string StateInformation()
		{
			return TargetLane.ToString();
		}

		public string LongDescription()
		{
			return "uTurn State: " + TargetLane.LaneId.ToString();
		}

		public Behavior Resume(VehicleState currentState, double speed)
		{
			return new UTurnBehavior(Polygon, TargetLane.LanePath(), TargetLane.LaneId, new ScalarSpeedCommand(2.0));
		}

		public bool CanResume()
		{
			return true;
		}

		public List<BehaviorDecorator> DefaultStateDecorators
		{
			get { return TurnDecorators.LeftTurnDecorator; }
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
				return false;
			}
			set
			{
				throw new Exception("The method or operation is not implemented.");
			}
		}

		#endregion
	}
}
