using System;
using System.Collections.Generic;
using System.Text;

namespace OperationalLayer.Tracking.Steering {
	struct SteeringControlData {
		/// <summary>
		/// Curvature of the path evaluated at the closest point to the center of the 
		/// vehicle's rear axle. Null if the curvature is not defined for the current point.
		/// </summary>
		public double? curvature;
		/// <summary>
		/// Distance of the vehicle off the path. Defined as the distance from the closest
		/// point on the path to the rear axle. Note that sign should be positive when the
		/// vehicle is to the left of the path.
		/// </summary>
		public double offtrackError;
		/// <summary>
		/// Deviation between the vehicle's heading and the instantaneous heading of the path
		/// evaluated at the closest point to the center of the rear axle.
		/// </summary>
		public double headingError;

		public SteeringControlData(double? curvature, double offtrackError, double headingError) {
			this.curvature = curvature;
			this.offtrackError = offtrackError;
			this.headingError = headingError;
		}
	}
}
