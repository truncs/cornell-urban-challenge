using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;

namespace UrbanChallenge.Operational.Common {
	[Serializable]
	public class LocalRoadModel {
		private LocalLaneModel centerLane;
		private LocalLaneModel leftLane;
		private LocalLaneModel rightLane;

		private double modelProbability;
		private CarTimestamp timestamp;

		public LocalRoadModel(CarTimestamp timestamp, double modelProbability, LocalLaneModel centerLane, LocalLaneModel leftLane, LocalLaneModel rightLane) {
			this.timestamp = timestamp;
			this.modelProbability = modelProbability;
			this.centerLane = centerLane;
			this.leftLane = leftLane;
			this.rightLane = rightLane;
		}

		public CarTimestamp Timestamp {
			get { return timestamp; }
		}

		public double ModelProbability {
			get { return modelProbability; }
		}

		public LocalLaneModel CenterLaneModel {
			get { return centerLane; }
		}

		public LocalLaneModel LeftLane {
			get { return leftLane; }
		}

		public LocalLaneModel RightLane {
			get { return rightLane; }
		}
	}
}
