using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Behaviors
{
	[Serializable]
	public enum SAUDILevel {
		/// <summary>
		/// Process all obstacles as normal
		/// </summary>
		None,
		/// <summary>
		/// Ignore small, low obstacles
		/// </summary>
		L1,
		/// <summary>
		/// Ignore low obstacles
		/// </summary>
		L2,
		/// <summary>
		/// Ignore all obstacles (except old tracked car-like vehicles)
		/// </summary>
		L3
	}

	/// <summary>
	/// Ignore most stuff and complete the behavior
	/// </summary>
	[Serializable]
	public class ShutUpAndDoItDecorator : BehaviorDecorator
	{
		private SAUDILevel level;

		public ShutUpAndDoItDecorator() {
			level = SAUDILevel.L1;
		}

		public ShutUpAndDoItDecorator(SAUDILevel level) {
			this.level = level;
		}

		public SAUDILevel Level {
			get { return level; }
		}
	}
}
