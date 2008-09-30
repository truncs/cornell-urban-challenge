using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common;

namespace OperationalLayer.Utilities {
	static class LaneSpeedHelper {
		private const double target_decel = 0.5;

		public static void CalculateMaxPathSpeed(LinePath path, LinePath.PointOnPath startPoint, LinePath.PointOnPath endPoint, ref double maxSpeed) {
			// get the angles
			List<Pair<int, double>> angles = path.GetIntersectionAngles(startPoint.Index, endPoint.Index);

			foreach (Pair<int, double> angleValue in angles) {
				// calculate the desired speed to take the point
				double dist = path.DistanceBetween(startPoint, path.GetPointOnPath(angleValue.Left));
				// calculate the desired speed at the intersection
				double desiredSpeed = 1.5/(angleValue.Right/(Math.PI/2));
				// limit the desired speed to 2.5
				desiredSpeed = Math.Max(2.5, desiredSpeed);
				// calculate the speed we would be allowed to go right now
				double desiredMaxSpeed = Math.Sqrt(desiredSpeed*desiredSpeed + 2*target_decel*dist);
				// check if the desired max speed is lower
				if (desiredMaxSpeed < maxSpeed) {
					maxSpeed = desiredMaxSpeed;
				}
			}
		}
	}
}
