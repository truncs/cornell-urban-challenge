using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.Sensors {
	[Serializable]
	public class SparseRoadBearing {
		public double timestamp;
		public float Heading;
		public float Confidence;

		public SparseRoadBearing(double timestamp, float heading, float confidence) {
			this.timestamp = timestamp;
			this.Heading = heading;
			this.Confidence = confidence;
		}
	}
}
