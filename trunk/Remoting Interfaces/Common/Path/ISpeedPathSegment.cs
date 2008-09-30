using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.Path {
	public interface ISpeedPathSegment : IPathSegment {
		/// <summary>
		/// True if the speed for the end of the segment is specified
		/// </summary>
		bool EndSpeedSpecified { get; }

		/// <summary>
		/// The speed to reach at the end of the segment or 0 if not specified.
		/// </summary>
		double EndSpeed { get; }

		/// <summary>
		/// True if there is a stop line/the vehicle should come to a stop at the end of the segment.
		/// </summary>
		bool StopLine { get; }
	}
}
