using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Vehicle;

namespace UrbanChallenge.Arbiter.Core.Common.State
{
	public class NoGoalsLeftState : OtherState, IState
	{
		#region IState Members

		public string ShortDescription()
		{
			return "NoGoalsLeftState";
		}

		public string StateInformation()
		{
			return "";
		}

		public string LongDescription()
		{
			return "No Goals Left in Mission";
		}

		public UrbanChallenge.Behaviors.Behavior Resume(VehicleState currentState, double speed)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public bool CanResume()
		{
			return false;
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
