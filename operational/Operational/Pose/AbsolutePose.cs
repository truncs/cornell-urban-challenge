using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;

namespace OperationalLayer.Pose {
	struct AbsolutePose {
		public Coordinates xy;
		public double heading;
		public CarTimestamp timestamp;

		public AbsolutePose(Coordinates xy, double heading, CarTimestamp timestamp) {
			this.xy = xy;
			this.heading = heading;
			this.timestamp = timestamp;
		}
	}
}
