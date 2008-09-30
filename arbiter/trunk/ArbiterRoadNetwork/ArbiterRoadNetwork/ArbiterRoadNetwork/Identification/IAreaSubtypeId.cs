using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Represents a unique identifier for a top-level area's subtype
	/// </summary>
	public interface IAreaSubtypeId
	{
		/// <summary>
		/// Unique area this subtype is a part of
		/// </summary>
		IAreaId AreadId
		{
			get;
		}

		/// <summary>
		/// Unique identifier of the subtype within the area
		/// </summary>
		int Number
		{
			get;
		}
	}
}
