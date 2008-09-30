using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Represents a unique identifier for a top-level area
	/// </summary>
	public interface IAreaId
	{
		/// <summary>
		/// Unique identifying number of the area
		/// </summary>
		int Number
		{
			get;
		}
	}
}
