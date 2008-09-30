using System;
using System.Collections.Generic;
using UrbanChallenge.Common;

namespace UrbanChallenge.Common.RndfNetwork {

	/// <summary>
	/// The abstract, atomic nodes in road networks. All connectivity in the road
	/// network ultimately boils down to Links between WayPoints.
	/// </summary>
	[Serializable]
	public class RndfWayPoint : IWaypoint
	{
		private RndfWaypointID waypointID;
		private Coordinates position;
		private bool isStop;
		private bool isExit;
		private bool isEntry;
		private List<Interconnect> exits;
		private List<Interconnect> entries;
		private LanePartition previousLanePartition;
		private LanePartition nextLanePartition;
		private Lane lane;
		private bool isCheckpoint;
		private int checkpointNumber;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="waypointID">RndfWaypoint Identification Information</param>
		/// <param name="lane">Lane this wypoint belongs to</param>
		public RndfWayPoint(RndfWaypointID waypointID, Lane lane)
		{
			this.waypointID = waypointID;
			this.lane = lane;
		}

		/// <summary>
		/// Checkpoint number of waypoint
		/// </summary>
		public int CheckpointNumber
		{
			get { return checkpointNumber; }
			set { checkpointNumber = value; }
		}

		/// <summary>
		/// identifies the waypoint as a checkpoint
		/// </summary>
		public bool IsCheckpoint
		{
			get { return isCheckpoint; }
			set { isCheckpoint = value; }
		}

		/// <summary>
		/// lane this waypoint belongs to
		/// </summary>
		public Lane Lane
		{
			get { return lane; }
			set { lane = value; }
		}		

		/// <summary>
		/// the lane partition that follows this waypoint
		/// </summary>
		public LanePartition NextLanePartition
		{
			get { return nextLanePartition; }
			set { nextLanePartition = value; }
		}

		/// <summary>
		/// the lane partition that precedes this waypoint
		/// </summary>
		public LanePartition PreviousLanePartition
		{
			get { return previousLanePartition; }
			set { previousLanePartition = value; }
		}

		/// <summary>
		/// Interconnects leaving this waypoint
		/// </summary>
		public List<Interconnect> Exits
		{
			get { return exits; }
			set { exits = value; }
		}

		/// <summary>
		/// Interconnects incoming to this waypoint
		/// </summary>
		public List<Interconnect> Entries
		{
			get { return entries; }
			set { entries = value; }
		}

		/// <summary>
		/// identifies this waypoint as having interconnect incoming
		/// </summary>		
		public bool IsEntry
		{
			get { return isEntry; }
			set { isEntry = value; }
		}

		/// <summary>
		/// identifies this waypoint as connecting to other segments (or uturn)
		/// </summary>
		public bool IsExit
		{
			get { return isExit; }
			set { isExit = value; }
		}

		/// <summary>
		/// identifies this waypoint as a stop sign
		/// </summary>
		public bool IsStop
		{
			get { return isStop; }
			set { isStop = value; }
		}

		/// <summary>
		/// absolute position of this waypoint
		/// </summary>
		public Coordinates Position
		{
			get { return position; }
			set { position = value; }
		}

		/// <summary>
		/// ID of the waypoint
		/// </summary>
		public RndfWaypointID WaypointID
		{
			get { return waypointID; }
			set { waypointID = value; }
		}

		public override bool Equals(object obj)
		{
			if (obj is RndfWayPoint)
			{
				return ((RndfWayPoint)obj).waypointID.Equals(waypointID);
			}
			else
			{
				return false;
			}
		}

		public override int GetHashCode()
		{
			return waypointID.GetHashCode();
		}
	}
}
