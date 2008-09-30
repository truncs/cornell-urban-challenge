using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Mapack;

namespace UrbanChallenge.Common.Pose {
	[Serializable]
	public class PoseRelData {
		public CarTimestamp timestamp;

		public double dt;

		public Matrix4 transform;
	}
}
