using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;

namespace UrbanChallenge.Arbiter.ArbiterMission
{
	/// <summary>
	/// Mission description for the ai
	/// </summary>
	[Serializable]
	public class ArbiterMissionDescription
	{
		/// <summary>
		/// Checkpoints of the mission
		/// </summary>
		public Queue<ArbiterCheckpoint> MissionCheckpoints;

		/// <summary>
		/// Speed Limits of the mission areas
		/// </summary>
		public List<ArbiterSpeedLimit> SpeedLimits;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="checkpoints"></param>
		/// <param name="speedLimits"></param>
		public ArbiterMissionDescription(Queue<ArbiterCheckpoint> checkpoints, List<ArbiterSpeedLimit> speedLimits)
		{
			this.MissionCheckpoints = checkpoints;
			this.SpeedLimits = speedLimits;
		}
	}
}
