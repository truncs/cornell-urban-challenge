using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Arbiter.Core.Common.State;
using UrbanChallenge.Arbiter.ArbiterRoads;

namespace UrbanChallenge.Arbiter.Core.Common
{
	/// <summary>
	/// Maneuver for the operational layer to execute
	/// </summary>
	public struct Maneuver
	{
		/// <summary>
		/// Next behavior
		/// </summary>
		public Behavior PrimaryBehavior;

		/// <summary>
		/// Next state
		/// </summary>
		public IState PrimaryState;		
	
		/// <summary>
		/// Secondary behavior to follow (desire or abort)
		/// </summary>
		public Behavior SecondaryBehavior;

		/// <summary>
		/// Secondary state to follow (desire or abort)
		/// </summary>
		public IState SecondaryState;		

		/// <summary>
		/// Maneuver to execute
		/// </summary>
		/// <param name="primaryBehavior"></param>
		/// <param name="primaryState"></param>
		/// <param name="primaryDecorators"></param>
		/// <param name="timeStamp"></param>
		public Maneuver(Behavior primaryBehavior, IState primaryState, List<BehaviorDecorator> primaryDecorators, double timeStamp)
		{
			this.PrimaryBehavior = primaryBehavior;
			this.PrimaryState = primaryState;
			this.PrimaryBehavior.Decorators = primaryDecorators;
			this.PrimaryBehavior.TimeStamp = timeStamp;

			this.SecondaryBehavior = null;
			this.SecondaryState = null;
		}

		/// <summary>
		/// Maneuver to execute
		/// </summary>
		/// <param name="primaryBehavior"></param>
		/// <param name="primaryState"></param>
		/// <param name="primaryDecorators"></param>
		/// <param name="secondaryBehavior"></param>
		/// <param name="secondaryState"></param>
		/// <param name="secondaryDecorators"></param>
		/// <param name="timeStamp"></param>
		public Maneuver(Behavior primaryBehavior, IState primaryState, List<BehaviorDecorator> primaryDecorators,
			Behavior secondaryBehavior, IState secondaryState, List<BehaviorDecorator> secondaryDecorators, double timeStamp)
		{
			this.PrimaryState = primaryState;
			this.PrimaryBehavior = primaryBehavior;
			this.PrimaryBehavior.Decorators = primaryDecorators;
			this.PrimaryBehavior.TimeStamp = timeStamp;

			this.SecondaryBehavior = secondaryBehavior;
			this.SecondaryState = secondaryState;
			this.SecondaryBehavior.Decorators = secondaryDecorators;
			this.SecondaryBehavior.TimeStamp = timeStamp;
		}	
	}
}
