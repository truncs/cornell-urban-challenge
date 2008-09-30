using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Represents a unique identifier for a top-level area's subtype's waypoint
	/// </summary>
	public interface IAreaSubtypeWaypointId
	{
		/// <summary>
		/// Identifier of the subtype
		/// </summary>
		IAreaSubtypeId AreaSubtypeId
		{
			get;
		}

		/// <summary>
		/// Unique identifier of waypoint wthin the subtype
		/// </summary>
		int Number
		{
			get;
		}
	}
}
