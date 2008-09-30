using System;
using UrbanChallenge.Common;
using System.Runtime.InteropServices;
using UrbanChallenge.Common.Sensors.Obstacle;

namespace UrbanChallenge.SceneEstimator {

	// Objects sent over the UDPChannel by the Scene Estimator are either
	// PositionEstimates or StoplineEstimates. Use "is" at receiver to
	// check for type at runtime.

	[Serializable]
	public struct Covariance {
		public double
			I11, I12, I13,
			I21, I22, I23,
			I31, I32, I33;
	}

	[Serializable]
	public struct Pose {
		public Coordinates Pos;
		public double Heading;
	}

	[Serializable]
	public struct PositionEstimate {
		public Pose Pose;
		public Covariance PoseCovariance;
		public int Lane;
	}

	[Serializable]
	public struct StoplineEstimate {
		public bool IsFound;
		public double Distance;
	}

}
