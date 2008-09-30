using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Boundary type of a lane
	/// </summary>
	[Serializable]
	public enum ArbiterLaneBoundary
	{
		/// <summary>
		/// Unknown
		/// </summary>
		Unknown,

		/// <summary>
		/// Double Yellow Line
		/// </summary>
		DoubleYellow,

		/// <summary>
		/// Solid yellow line
		/// </summary>
		SolidYellow,

		/// <summary>
		/// Solid White Line
		/// </summary>
		SolidWhite,

		/// <summary>
		/// Broken White Line
		/// </summary>
		BrokenWhite,

		/// <summary>
		/// None
		/// </summary>
		None
	}
}
