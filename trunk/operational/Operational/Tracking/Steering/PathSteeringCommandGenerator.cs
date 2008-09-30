using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Vehicle;

namespace OperationalLayer.Tracking.Steering {
	class PathSteeringCommandGenerator : ISteeringCommandGenerator {
		private IRelativePath path;

		public PathSteeringCommandGenerator(IRelativePath path) {
			if (path == null) {
				throw new ArgumentNullException("path");
			}

			this.path = path;
		}

		#region ISteeringCommandGenerator Members

		public void GetSteeringCommand(ref double? steeringAngle) {
			double angle;
			CurvaturePathFollowing.ComputeSteeringCommand(path, Settings.TrackingPeriod, out angle);

			if (double.IsNaN(angle)) {
				steeringAngle = null;
				return;
			}
			else if (angle > TahoeParams.SW_max) {
				angle = TahoeParams.SW_max;
			}
			else if (angle < -TahoeParams.SW_max) {
				angle = -TahoeParams.SW_max;
			}

			//if (Services.StateProvider.GetVehicleState().transGear == TransmissionGear.Reverse) {
			//  angle = -angle;
			//}

			steeringAngle = angle;
		}

		#endregion

		#region ITrackingCommandBase Members

		public CompletionResult CompletionStatus {
			get {
				if (path.IsPastEnd) {
					return CompletionResult.Completed;
				}
				else {
					return CompletionResult.Working;
				}
			}
		}

		public object FailureData {
			get { return null; }
		}

		public void BeginTrackingCycle(CarTimestamp timestamp) {
			if (path.CurrentTimestamp < timestamp) {
				// get the relative transform to take up to the current time
				path.TransformPath(Services.RelativePose.CurrentTimestamp);
			}
		}

		#endregion
	}
}
