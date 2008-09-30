using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// General interface for any waypoint
	/// </summary>
	public interface IArbiterWaypoint : INavigableNode
	{
		/// <summary>
		/// Generic id of the waypoint
		/// </summary>
		IAreaSubtypeWaypointId AreaSubtypeWaypointId
		{
			get;
		}

		/// <summary>
		/// Position of the waypoint
		/// </summary>
		new Coordinates Position
		{
			get;
			set;
		}

		/// <summary>
		/// Whether or not the waypoint is a checkpoint
		/// </summary>
		bool IsCheckpoint
		{
			get;
			set;
		}

		/// <summary>
		/// Number of the checkpoint
		/// </summary>
		int CheckpointId
		{
			get;
			set;
		}
	}
}
