using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.RndfNetwork
{
	/// <summary>
	/// Connects two Rndf Waypoints in a Lane
	/// </summary>
	[Serializable]
	public class LanePartition : IConnectWaypoints
	{
		private RndfWayPoint initialWaypoint;
		private RndfWayPoint finalWaypoint;
		private double length;
		private List<UserPartition> userPartitions;
		private LanePartitionID lanePartitionID;
		private Lane lane;
		private List<Blockage> blockages;
		private bool blocked;		

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="lanePartitionID">Identification information</param>
		/// <param name="lane">Lane this partition is a part of</param>
		public LanePartition(LanePartitionID lanePartitionID, Lane lane)
		{
			this.lanePartitionID = lanePartitionID;
			this.lane = lane;
		}

		/// <summary>
		/// Lane this parttiion is a part of
		/// </summary>
		public Lane Lane
		{
			get { return lane; }
			set { lane = value; }
		}

		/// <summary>
		/// Identification information about this LanePartition
		/// </summary>
		public LanePartitionID LanePartitionID
		{
			get { return lanePartitionID; }
			set { lanePartitionID = value; }
		}

		/// <summary>
		/// User partitions which split the LanePartitions into more precise parts
		/// </summary>
		public List<UserPartition> UserPartitions
		{
			get { return userPartitions; }
			set { userPartitions = value; }
		}

		/// <summary>
		/// Inserts a user RndfWayPoint between two Rndf Waypoints a certain distance from the beginning RndfWayPoint
		/// </summary>
		/// <param name="userWayPoint">UserWayPoint to add</param>
		/// <param name="distanceFromBeginning">distance to add RndfWayPoint from the beginning RndfWayPoint of the LanePartition</param>
		public void AddUserWayPoint(RndfWayPoint userWayPoint, double distanceFromBeginning)
		{
			throw new Exception("This method has not been implemented");
		}

		/// <summary>
		/// Length in meters between the beginning and ending waypoints
		/// </summary>
		public double Length
		{
			get 
			{
				if (length == 0)
					return initialWaypoint.Position.DistanceTo(finalWaypoint.Position);
				else
					return length;
			}
			set { length = value; }
		}

		/// <summary>
		/// End bound of the LanePrtition
		/// </summary>
		public RndfWayPoint FinalWaypoint
		{
			get { return finalWaypoint; }
			set { finalWaypoint = value; }
		}

		/// <summary>
		/// Beginning bound of the LaneParition
		/// </summary>
		public RndfWayPoint InitialWaypoint
		{
			get { return initialWaypoint; }
			set { initialWaypoint = value; }
		}

		/// <summary>
		/// Blockages held inside this partition
		/// </summary>
		public List<Blockage> Blockages
		{
			get { return blockages; }
			set { blockages = value; }
		}

		/// <summary>
		/// String representation of the object
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return lanePartitionID.ToString();
		}

		/// <summary>
		/// partition has blockages
		/// </summary>
		public bool Blocked
		{
			get { return blocked; }
			set { blocked = value; }
		}
	}
}
