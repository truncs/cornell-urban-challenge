using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;

namespace UrbanChallenge.Operational.Common {
	[Serializable]
	public struct BoundInformation {
		public int boundaryHitIndex;
		public Coordinates point;
		public double deviation;
		public double spacing;
		public double alpha_s;
		public double spacing_violation;
	}
}
