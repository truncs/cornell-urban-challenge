using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;

namespace OperationalLayer.Tracking.Steering {
	struct BadPointOnPath {
		/// <summary>
		/// Location of point
		/// </summary>
		/// <remarks>
		/// Only valid at the labeled timestamp. When the path is transformed to a different time, the point may no longer be in the same location.
		/// </remarks>
		public Coordinates xy;

		/// <summary>
		/// Path the point was determined on
		/// </summary>
		public IRelativePath path;

		/// <summary>
		/// Car-timestamp with the time of applicability
		/// </summary>
		public CarTimestamp timestamp;

		/// <summary>
		/// Path-specific data
		/// </summary>
		public object data;

		public BadPointOnPath(Coordinates xy, IRelativePath path, CarTimestamp timestamp) {
			this.xy = xy;
			this.path = path;
			this.data = null;
			this.timestamp = timestamp;
		}

		public BadPointOnPath(Coordinates xy, IRelativePath path, CarTimestamp timestamp, object data) {
			this.xy = xy;
			this.path = path;
			this.data = data;
			this.timestamp = timestamp;
		}
	}
}
