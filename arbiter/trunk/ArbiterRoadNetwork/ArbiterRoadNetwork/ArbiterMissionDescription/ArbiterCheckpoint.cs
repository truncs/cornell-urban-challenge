using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;

namespace UrbanChallenge.Arbiter.ArbiterMission
{
	public enum CheckpointType
	{
		Normal = 0,
		Inserted = 1,
		Blocked = 2
	}

	/// <summary>
	/// Checkpoint in the Arbiter Road Network
	/// </summary>
	[Serializable]
	public class ArbiterCheckpoint
	{
		/// <summary>
		/// The number of the checkpoint
		/// </summary>
		public int CheckpointNumber;

		/// <summary>
		/// Id of the checkpoint
		/// </summary>
		public IAreaSubtypeWaypointId WaypointId;

		/// <summary>
		/// The checkpoints type
		/// </summary>
		public CheckpointType Type;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="number"></param>
		/// <param name="id"></param>
		public ArbiterCheckpoint(int number, IAreaSubtypeWaypointId id)
		{
			this.CheckpointNumber = number;
			this.WaypointId = id;
			this.Type = CheckpointType.Normal;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="number"></param>
		/// <param name="id"></param>
		public ArbiterCheckpoint(int number, IAreaSubtypeWaypointId id, CheckpointType type)
		{
			this.CheckpointNumber = number;
			this.WaypointId = id;
			this.Type = type;
		}
	}
}
