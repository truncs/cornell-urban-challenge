using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence
{
	/// <summary>
	/// Current state of the core
	/// </summary>
	public enum CoreState
	{
		/// <summary>
		/// Paused
		/// </summary>
		Paused,

		/// <summary>
		/// Nominal Operations
		/// </summary>
		Running,

		/// <summary>
		/// Resuming Operations
		/// </summary>
		Resuming
	}
}
