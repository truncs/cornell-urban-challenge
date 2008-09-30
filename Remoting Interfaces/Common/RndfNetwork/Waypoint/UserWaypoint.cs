using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.RndfNetwork
{
	/// <summary>
	/// User defined Waypoint within a LanePartition
	/// </summary>
	[Serializable]
	public class UserWaypoint : IWaypoint
	{
		private Coordinates position;
		private UserWaypointID waypointID;
		private UserPartition previousUserPartiton;
		private UserPartition nextUserPartition;

		/// <summary>
		/// The next user partition in the LaneParititon
		/// </summary>
		public UserPartition NextUserPartition
		{
			get { return nextUserPartition; }
			set { nextUserPartition = value; }
		}

		/// <summary>
		///  The previous UserPartition in the LanePartition
		/// </summary>
		public UserPartition PreviousUserPartition
		{
			get { return previousUserPartiton; }
			set { previousUserPartiton = value; }
		}

		/// <summary>
		/// ID information about the UserWaypoint
		/// </summary>
		public UserWaypointID WaypointID
		{
			get { return waypointID; }
			set { waypointID = value; }
		}

		/// <summary>
		/// Position information about the UserWaypoint
		/// </summary>
		public Coordinates Position
		{
			get { return position; }
			set { position = value; }
		}
	}
}
