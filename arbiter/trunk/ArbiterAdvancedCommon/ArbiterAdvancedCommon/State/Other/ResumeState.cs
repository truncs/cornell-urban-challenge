using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Vehicle;

namespace UrbanChallenge.Arbiter.Core.Common.State
{
	public class ResumeState : OtherState, IState
	{
		/// <summary>
		/// State to resume in the resume state
		/// </summary>
		public IState StateToResume;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="stateToResume"></param>
		public ResumeState(IState stateToResume)
		{
			this.StateToResume = stateToResume;
		}

		#region IState Members

		public string ShortDescription()
		{
			return("ResumeState");
		}

		public string StateInformation()
		{
			return "Resuming: " + StateToResume.ShortDescription();
		}

		public string LongDescription()
		{
			return("Resuming: " + StateToResume.LongDescription());
		}

		public UrbanChallenge.Behaviors.Behavior Resume(VehicleState currentState, double speed)
		{
			if (StateToResume != null)
				return StateToResume.Resume(currentState, speed);
			else
				return null;
		}

		public bool CanResume()
		{
			if (StateToResume != null)
				return StateToResume.CanResume();
			else
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
