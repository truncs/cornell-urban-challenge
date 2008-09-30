using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Unique id for any item that connects two area waypoints
	/// </summary>
	public interface IConnectAreaWaypointsId
	{
		/// <summary>
		/// Generic id of the initial waypoint
		/// </summary>
		IAreaSubtypeWaypointId InitialGenericId
		{
			get;
		}

		/// <summary>
		/// Generic id of the final waypoint
		/// </summary>
		IAreaSubtypeWaypointId FinalGenericId
		{
			get;
		}
	}
}
