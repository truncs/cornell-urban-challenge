using System;
using System.Collections.Generic;
using System.Text;

namespace OperationalLayer.Tracking.Steering {
	class SteeringControlDataOptions {
		public double PathLookahead;

		public SteeringControlDataOptions(double curvatureLookahead) {
			this.PathLookahead = curvatureLookahead;
		}
	}
}
