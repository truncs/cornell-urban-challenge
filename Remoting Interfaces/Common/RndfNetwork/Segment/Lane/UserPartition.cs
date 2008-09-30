using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.RndfNetwork
{
	/// <summary>
	/// Splits a Lanepartition into smaller partitions of user defined waypoints
	/// </summary>
	[Serializable]
	public class UserPartition
	{
		private IWaypoint initialWaypoint;
		private IWaypoint finalWaypoint;
		private IConnectWaypoints parentPartition;
		private UserPartitionID partitionID;

		public UserPartition(UserPartitionID id, IConnectWaypoints parent)
		{
			this.partitionID = id;
			this.parentPartition = parent;
		}

		/// <summary>
		/// The lane partition this user partition belongs to
		/// </summary>
		public IConnectWaypoints ParentPartition
		{
			get { return parentPartition; }
			set { parentPartition = value; }
		}

		/// <summary>
		/// End bound of the UserPrtition
		/// </summary>
		public IWaypoint FinalWaypoint
		{
			get { return finalWaypoint; }
			set { finalWaypoint = value; }
		}

		/// <summary>
		/// Beginning bound of the UserParition
		/// </summary>
		public IWaypoint InitialWaypoint
		{
			get { return initialWaypoint; }
			set { initialWaypoint = value; }
		}

		/// <summary>
		/// Identifier of partition.
		/// </summary>
		public UserPartitionID PartitionID {
			get { return partitionID; }
		}

		/// <summary>
		/// Inserts a user RndfWayPoint in the UserPartition a certain distance from the beginning RndfWayPoint
		/// </summary>
		/// <param name="userWayPoint">UserWayPoint to add</param>
		/// <param name="distanceFromBeginning">distance to add RndfWayPoint from the beginning RndfWayPoint of the LanePartition</param>
		public void AddUserWayPoint(UserWaypoint userWayPoint, double distanceFromBeginning)
		{
			throw new Exception("This method has not been implemented");
		}

		public override bool Equals(object obj)
		{
			if (obj is UserPartition)
			{
				return partitionID.Equals(((UserPartition)obj).partitionID);
			}
			else
			{
				return false;
			}
		}

		public override int GetHashCode()
		{
			return partitionID.GetHashCode();
		}
	}
}
