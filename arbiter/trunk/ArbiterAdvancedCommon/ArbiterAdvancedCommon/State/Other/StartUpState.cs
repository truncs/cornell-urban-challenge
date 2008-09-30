using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.Core.Common.State;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Arbiter.Core.Common.Reasoning;
using UrbanChallenge.Common.Vehicle;

namespace UrbanChallenge.Arbiter.Core.Common.State
{
	/// <summary>
	/// The ai has just started up
	/// </summary>
	public class StartUpState : OtherState, IState
	{
		/// <summary>
		/// Constructor
		/// </summary>
		public StartUpState()
		{
		}

		#region IState Members

		/// <summary>
		/// Short Description of the state
		/// </summary>
		/// <returns></returns>
		public string ShortDescription()
		{
			return ("StartUp State");
		}

		public string StateInformation()
		{
			return "";
		}

		/// <summary>
		/// Long Description of the state
		/// </summary>
		/// <returns></returns>
		public string LongDescription()
		{
			return ("State: StartUp");
		}

		/// <summary>
		/// String representation of the state
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return this.ShortDescription();
		}

		public Behavior Resume(VehicleState currentState, double speed)
		{
			// decorators
			List<BehaviorDecorator> bds = new List<BehaviorDecorator>();

			// zero decorators
			bds.Add(new TurnSignalDecorator(TurnSignal.Off));

			// get no op
			Behavior b = new NullBehavior();
			
			// set decorators
			b.Decorators = bds;

			// return
			return b;
		}

		public bool CanResume()
		{
			return false;
		}

		public List<BehaviorDecorator> DefaultStateDecorators
		{
			get { return TurnDecorators.NoDecorators; }
		}

		public bool UseLaneAgent
		{
			get { return false; }
		}

		public InternalState InternalLaneState
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
