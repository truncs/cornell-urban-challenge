using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Defines waypoint we can traverse
	/// </summary>
	public interface ITraversableWaypoint : IArbiterWaypoint
	{
		/// <summary>
		/// Whether this is an exit
		/// </summary>
		bool IsExit
		{
			get;
			set;
		}

		/// <summary>
		/// Whether this is an entry
		/// </summary>
		bool IsEntry
		{
			get;
			set;
		}

		/// <summary>
		/// Exits from thsi waypoints
		/// </summary>
		List<ArbiterInterconnect> Exits
		{
			get;
			set;
		}

		/// <summary>
		/// Entries leading to this waypoint
		/// </summary>
		List<ArbiterInterconnect> Entries
		{
			get;
			set;
		}

		/// <summary>
		/// Checks if this is stop or not
		/// </summary>
		bool IsStop
		{
			get;
			set;
		}

		/// <summary>
		/// Vehicle area this waypoint is associated with
		/// </summary>
		IVehicleArea VehicleArea
		{
			get;
		}
	}
}
