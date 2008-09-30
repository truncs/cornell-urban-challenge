using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Behaviors;

namespace UrbanChallenge.Arbiter.Core.Common.State
{
	public class IntersectionStartupState : IntersectionState, IState
	{
		public ArbiterIntersection Intersection;

		public IntersectionStartupState(ArbiterIntersection ai)
		{
			this.Intersection = ai;
		}

		#region IState Members

		public string ShortDescription()
		{
			return "IntersectionStartupState";
		}

		public string LongDescription()
		{
			return ShortDescription() + ", " + this.LongDescription();
		}

		public string StateInformation()
		{
			return this.Intersection.ToString();
		}

		public UrbanChallenge.Behaviors.Behavior Resume(UrbanChallenge.Common.Vehicle.VehicleState currentState, double speed)
		{
			return new HoldBrakeBehavior();
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
