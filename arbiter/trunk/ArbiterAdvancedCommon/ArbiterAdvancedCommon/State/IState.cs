using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Arbiter.Core.Common.Reasoning;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Vehicle;

namespace UrbanChallenge.Arbiter.Core.Common.State
{
	/// <summary>
	/// Inteface for keeping hte state of the vehicle
	/// </summary>
	public interface IState
	{		
		/// <summary>
		/// Short Description of the state
		/// </summary>
		/// <returns></returns>
		string ShortDescription();

		/// <summary>
		/// Long Description of the state
		/// </summary>
		/// <returns></returns>
		string LongDescription();

		/// <summary>
		/// Information about the state
		/// </summary>
		/// <returns></returns>
		string StateInformation();

		/// <summary>
		/// Resume the behavior
		/// </summary>
		Behavior Resume(VehicleState currentState, double speed);

		/// <summary>
		/// Can resume from here
		/// </summary>
		/// <returns></returns>
		bool CanResume();

		/// <summary>
		/// Decorators for when we resume
		/// </summary>
		List<BehaviorDecorator> DefaultStateDecorators
		{
			get;
		}

		/// <summary>
		/// Whether to use lane agent or not
		/// </summary>
		bool UseLaneAgent
		{
			get;
		}

		/// <summary>
		/// Lane's we should be in
		/// </summary>
		InternalState InternalLaneState
		{
			get;
			set;
		}

		/// <summary>
		/// Whether or not to reset the lane agent
		/// </summary>
		bool ResetLaneAgent
		{
			get;
			set;
		}
	}
}
