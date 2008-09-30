using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;

namespace OperationalLayer.Tracking.SpeedControl {
	interface IDistanceProvider {
		double GetRemainingDistance();

		void Transform(CarTimestamp timestamp);
	}
}
