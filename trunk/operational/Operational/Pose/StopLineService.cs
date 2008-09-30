using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Vehicle;

namespace OperationalLayer.Pose {
	class StopLineService {
		public double DistanceToStopline() {
			return DistanceToStopline(0);
		}

		public double DistanceToStopline(double sigma) {
			double stoplineDist = Services.Dataset.ItemAs<double>("stopline distance").CurrentValue;
			double stoplineVar = Services.Dataset.ItemAs<double>("stopline variance").CurrentValue;

			double stoplineStdDev = 0;
			if (stoplineVar > 0 && Math.Abs(sigma) > 1e-20) {
				stoplineStdDev = Math.Sqrt(stoplineVar)*sigma;
			}

			return stoplineDist + stoplineStdDev - TahoeParams.FL;
		}
	}
}
