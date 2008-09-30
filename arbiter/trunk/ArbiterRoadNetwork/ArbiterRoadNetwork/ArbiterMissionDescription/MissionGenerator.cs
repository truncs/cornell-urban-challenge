using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.DarpaRndf;
using UrbanChallenge.Arbiter.ArbiterRoads;

namespace UrbanChallenge.Arbiter.ArbiterMission
{
	/// <summary>
	/// Generates mission descriptions
	/// </summary>
	[Serializable]
	public class MissionGenerator
	{
		/// <summary>
		/// Generates mission
		/// </summary>
		/// <param name="mdf"></param>
		/// <returns></returns>
		public ArbiterMissionDescription GenerateMission(IMdf mdf, ArbiterRoadNetwork arn)
		{
			Queue<ArbiterCheckpoint> checks = new Queue<ArbiterCheckpoint>();
			List<ArbiterSpeedLimit> speeds = new List<ArbiterSpeedLimit>();

			// checkpoints
			foreach (string s in mdf.CheckpointOrder)
			{
				int num = int.Parse(s);
				checks.Enqueue(new ArbiterCheckpoint(num, arn.Checkpoints[num].AreaSubtypeWaypointId)); 
			}

			// speeds
			foreach (SpeedLimit sl in mdf.SpeedLimits)
			{
				ArbiterSpeedLimit asl = new ArbiterSpeedLimit();
				asl.MaximumSpeed = sl.MaximumVelocity * 0.44704;
				asl.MinimumSpeed = sl.MinimumVelocity * 0.44704;
				asl.Traveled = false;

				ArbiterSegmentId asi = new ArbiterSegmentId(int.Parse(sl.SegmentID));
				ArbiterZoneId azi = new ArbiterZoneId(int.Parse(sl.SegmentID));

				if (arn.ArbiterZones.ContainsKey(azi))
					asl.Area = azi;
				else if (arn.ArbiterSegments.ContainsKey(asi))
					asl.Area = asi;
				else
					throw new Exception("Unknown area id: " + sl.SegmentID);

				speeds.Add(asl);
			}

			// return
			return new ArbiterMissionDescription(checks, speeds);
		}
	}
}
