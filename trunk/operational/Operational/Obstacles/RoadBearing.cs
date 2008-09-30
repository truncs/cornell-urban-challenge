using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common;

namespace OperationalLayer.Obstacles {
	public static class RoadBearing {
		private const double alpha = 0.85;

		private static double currentAngle;
		private static double currentConfidence;

		public static void OnRoadBearing(CarTimestamp timestamp, double angle, double confidence) {
			if (currentConfidence == 0) {
				currentAngle = angle;
				currentConfidence = confidence;
			}
			else if (confidence == 0) {
				currentConfidence *= 0.5;
			}
			else {
				currentAngle = alpha*angle + (1-alpha)*currentAngle;
				currentConfidence = confidence;
			}

			LineList roadBearing = new LineList(2);
			roadBearing.Add(new Coordinates(2, 0).Rotate(currentAngle));
			roadBearing.Add(new Coordinates(10, 0).Rotate(currentAngle));

			Services.UIService.PushLineList(roadBearing, timestamp, "road bearing", true);
			Services.Dataset.ItemAs<double>("road bearing confidence").Add(confidence, timestamp);
		}

		public static void GetCurrentData(out double angle, out double confidence) {
			angle = currentAngle;
			confidence = currentConfidence;
		}
	}
}
