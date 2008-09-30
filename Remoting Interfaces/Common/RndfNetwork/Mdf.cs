using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.RndfNetwork
{
	/// <summary>
	/// Darpa defined mdf
	/// </summary>
	[Serializable]
	public class Mdf
	{
		private Queue<Goal> goals;
		private List<SpeedInformation> speedLimits;

		/// <summary>
		/// Constructor
		/// </summary>
		public Mdf()
		{
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="goals">Queue of goals to follow in order</param>
		/// <param name="speedLimits">Speed information for segments on the rndf</param>
		public Mdf(Queue<Goal> goals, List<SpeedInformation> speedLimits)
		{
			this.goals = goals;
			this.speedLimits = speedLimits;
		}

		/// <summary>
		/// Speed information for segments on the rndf
		/// </summary>
		public List<SpeedInformation> SpeedLimits
		{
			get { return speedLimits; }
			set { speedLimits = value; }
		}

		/// <summary>
		/// Queue of goals to follow in order
		/// </summary>
		public Queue<Goal> Goals
		{
			get { return goals; }
			set { goals = value; }
		}
	}
}
