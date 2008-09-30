using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.RndfNetwork
{
	/// <summary>
	/// ID of a Waypoint
	/// </summary>
	public interface IWaypointID
	{
		/// <summary>
		/// String representation of the Waypoint
		/// </summary>
		/// <returns>String representing the ID</returns>
		string ToString();
	}
}
