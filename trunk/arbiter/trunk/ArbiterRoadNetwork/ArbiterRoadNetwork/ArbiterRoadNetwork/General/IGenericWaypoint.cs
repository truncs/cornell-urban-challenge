using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Represents the most generic waypoint
	/// </summary>
	public interface IGenericWaypoint
	{
		/// <summary>
		/// Generic id of the waypoint
		/// </summary>
		Object GenericId
		{
			get;
		}

		/// <summary>
		/// Position of the waypoint
		/// </summary>
		Coordinates Position
		{
			get;
			set;
		}
	}
}
