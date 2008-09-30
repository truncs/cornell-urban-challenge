using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common;

namespace SimOperationalService {
	[Serializable]
	public class PathRoadModel {
		[Serializable]
		public class LaneEstimate {
			public LaneEstimate(LinePath path, double width, string partitionID) {
				this.path = path;
				this.width = width;
				this.paritionID = partitionID;
			}

			public LinePath path;
			public double width;
			public string paritionID;
		}

		public List<LaneEstimate> laneEstimates;
		public double timestamp;

		public PathRoadModel(List<LaneEstimate> laneEstimates, double timestamp) {
			this.laneEstimates = laneEstimates;
			this.timestamp = timestamp;
		}
	}
}
