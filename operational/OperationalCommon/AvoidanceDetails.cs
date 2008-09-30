using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Operational.Common {
	[Serializable]
	public class AvoidanceDetails {
		public SmoothingDetails smoothingDetails;
		public List<Boundary> leftBounds;
		public List<Boundary> rightBounds;
	}
}
