using System;
using System.Collections.Generic;
using System.Text;


using UrbanChallenge.Common.Path;
using UrbanChallenge.Behaviors;

namespace UrbanChallenge.Arbiter.ArbiterCommon
{
	/// <summary>
	/// Defines the Exit Data leaving the Arbiter
	/// </summary>
    public class ArbiterExitData
    {
		private Behavior behavior;
		private IPath path;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="behavior">Behavior to accomplish</param>
		/// <param name="path">General path to take to accomplish Behavior</param>
		public ArbiterExitData(Behavior behavior, IPath path)
		{
			this.behavior = behavior;
			this.path = path;
		}

		/// <summary>
		/// Path to Follow
		/// </summary>
		public IPath Path
		{
			get { return path; }
		}

		/// <summary>
		/// Behavior to accomplish
		/// </summary>
		public Behavior Behavior
		{
			get { return behavior; }
		}
    }
}
