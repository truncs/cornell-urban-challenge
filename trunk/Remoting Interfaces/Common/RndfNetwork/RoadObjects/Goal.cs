using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.RndfNetwork
{
	/// <summary>
	/// Darpa defined goal
	/// </summary>
	[Serializable]
	public class Goal
	{
		private int goalNumber;
		private RndfWayPoint waypoint;
		private RndfWaypointID waypointID;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="goalNumber">id number of goal in checkpoint</param>
		/// <param name="waypoint">rndf waypoint this checkpoint identifies</param>
		public Goal(int goalNumber, RndfWayPoint waypoint)
		{
			this.goalNumber = goalNumber;
			this.waypoint = waypoint;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="goalNumber">id number of goal in checkpoint</param>
		/// <param name="waypointID">rndf waypoint this checkpoint identifies</param>
		public Goal(int goalNumber, RndfWaypointID waypointID)
		{
			this.goalNumber = goalNumber;
			this.waypointID = waypointID;
		}

		/// <summary>
		/// Waypoint related to this goal
		/// </summary>
		public RndfWayPoint Waypoint
		{
			get { return waypoint; }
			set { waypoint = value; }
		}

		/// <summary>
		/// Waypoint related to this goal
		/// </summary>
		public RndfWaypointID WaypointID
		{
			get { return waypointID; }
			set { waypointID = value; }
		}

		/// <summary>
		/// The id number of the goal
		/// </summary>
		public int GoalNumber
		{
			get { return goalNumber; }
			set { goalNumber = value; }
		}
	}
}
