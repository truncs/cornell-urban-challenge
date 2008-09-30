using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.RndfNetwork
{
	/// <summary>
	/// Interface for Items that connect two Waypoints.
	/// </summary>
	/// <remarks>Used for Intersections</remarks>
	public interface IConnectWaypoints
	{
		/// <summary>
		/// Initial Waypoint in the connection
		/// </summary>
		RndfWayPoint InitialWaypoint
		{
			get;
			set;
		}

		/// <summary>
		/// Ending Waypoint in the connection
		/// </summary>
		RndfWayPoint FinalWaypoint
		{
			get;
			set;
		}

		/// <summary>
		/// Precise definition of turn
		/// </summary>
		List<UserPartition> UserPartitions
		{
			get;
			set;
		}

		/// <summary>
		/// Put blockages into this connection
		/// </summary>
		List<Blockage> Blockages
		{
			get;
			set;
		}
	}
}
