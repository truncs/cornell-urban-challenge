using System;
using System.Collections.Generic;
using System.Text;

using UrbanChallenge.Common.Path;
using UrbanChallenge.Behaviors;

namespace UrbanChallenge.Arbiter.ArbiterCommon
{
	/// <summary>
	/// Maneuver for the operational layer to execute
	/// </summary>
	public struct Maneuver
	{
		private Behavior behavior;
		private IState next;

		/// <summary>
		/// Maneuver to execute
		/// </summary>
		/// <param name="path">Path to Follow</param>
		/// <param name="behavior">Type of Behavior</param>
		public Maneuver(Behavior behavior, IState next)
		{
			this.next = next;
			this.behavior = behavior;
		}

		/// <summary>
		/// Behavior to execute
		/// </summary>
		public Behavior Behavior
		{
			get { return behavior; }
		}

		public IState Next
		{
			get { return next; }
		}
	}
}
