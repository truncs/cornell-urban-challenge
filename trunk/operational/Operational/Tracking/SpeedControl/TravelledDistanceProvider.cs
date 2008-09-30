using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;

namespace OperationalLayer.Tracking.SpeedControl {
	class TravelledDistanceProvider : IDistanceProvider {
		private CarTimestamp startTime;
		private CarTimestamp currentTime;
		private double totalDist;

		public TravelledDistanceProvider(CarTimestamp startTime, double totalDist) {
			this.startTime = startTime;
			this.currentTime = startTime;
			this.totalDist = totalDist;
		}

		#region IDistanceProvider Members

		public double GetRemainingDistance() {
			// do it from some the timestamps
			return totalDist - Services.TrackedDistance.GetDistanceTravelled(startTime, currentTime);
		}

		public void Transform(CarTimestamp timestamp) {
			currentTime = timestamp;
		}

		#endregion
	}
}
