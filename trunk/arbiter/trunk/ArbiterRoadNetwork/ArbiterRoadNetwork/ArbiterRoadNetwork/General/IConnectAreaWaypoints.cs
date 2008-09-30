using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// COnnects two main waypoints
	/// </summary>
	public interface IConnectAreaWaypoints
	{
		/// <summary>
		/// Id of the connection
		/// </summary>
		IConnectAreaWaypointsId ConnectionId
		{
			get;
		}

		/// <summary>
		/// Initial waypoint of the connection
		/// </summary>
		IArbiterWaypoint InitialGeneric
		{
			get;
		}

		/// <summary>
		/// Final waypoint of the connection
		/// </summary>
		IArbiterWaypoint FinalGeneric
		{
			get;
		}

		/// <summary>
		/// User Partitions of the connection
		/// </summary>
		List<ArbiterUserPartition> UserPartitions
		{
			get;
			set;
		}

		/// <summary>
		/// Blockage
		/// </summary>
		NavigationBlockage Blockage
		{
			get;
			set;
		}

		/// <summary>
		/// distance to a waypoint
		/// </summary>
		/// <param name="loc"></param>
		/// <returns></returns>
		double DistanceTo(Coordinates loc);

		/// <summary>
		/// distance to a IConnectPartition
		/// </summary>
		/// <param name="icaw"></param>		
		/// <returns></returns>
		double DistanceTo(IConnectAreaWaypoints icaw);

		/// <summary>
		/// Returns interconnect representation
		/// </summary>
		ArbiterInterconnect ToInterconnect
		{
			get;
		}
	}
}
