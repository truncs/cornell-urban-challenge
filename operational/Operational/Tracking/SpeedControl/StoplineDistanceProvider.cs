using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Vehicle;

namespace OperationalLayer.Tracking.SpeedControl {
	class StoplineDistanceProvider : IDistanceProvider {

		#region IDistanceProvider Members

		public double GetRemainingDistance() {
			// figure out the distance to the stop line
			return Services.Stopline.DistanceToStopline();
		}

		public void Transform(UrbanChallenge.Common.CarTimestamp timestamp) {
			// nothing to do
		}

		#endregion
	}
}
