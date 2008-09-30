using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Operational.Common {
	[Serializable]
	public class SmoothingDetails {
		public BoundInformation[] leftBounds;
		public BoundInformation[] rightBounds;

		public double[] k_constraint_lamda;
		public double[] a_constraint_lamda;
	}
}
