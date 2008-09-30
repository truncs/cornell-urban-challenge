using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;

namespace OperationalLayer.RoadModel {
	class LinearizationOptions {
		public double StartDistance;
		public double EndDistance;
		public CarTimestamp Timestamp;
		public double LaneShiftDistance;

		public LinearizationOptions() {
		}

		public LinearizationOptions(double startDist, double endDist, CarTimestamp timestamp) {
			this.StartDistance = startDist;
			this.EndDistance = endDist;
			this.Timestamp = timestamp;
			this.LaneShiftDistance = 0;
		}
	}
}
