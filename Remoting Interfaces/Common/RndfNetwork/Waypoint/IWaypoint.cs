using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.RndfNetwork
{
	/// <summary>
	/// Standard Waypoint information
	/// </summary>
	public interface IWaypoint
	{
		/// <summary>
		/// Where the Waypoint is located
		/// </summary>
		Coordinates Position
		{
			get;
			set;
		}		
	}
}
