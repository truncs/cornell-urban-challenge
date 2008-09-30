using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;

namespace OperationalLayer.Tracking.SpeedControl {
	class ConstantSpeedGenerator : ISpeedGenerator {
		private double? speed;
		private double? accel;

		public ConstantSpeedGenerator(double? speed, double? accel) {
			this.speed = speed;
			this.accel = accel;
		}

		#region ISpeedGenerator Members

		public SpeedControlData GetCommandedSpeed() {
			return new SpeedControlData(speed, accel);
		}

		#endregion

		#region ITrackingCommandBase Members

		public CompletionResult CompletionStatus {
			get { return CompletionResult.Working; }
		}

		public object FailureData {
			get { return null; }
		}

		public void BeginTrackingCycle(CarTimestamp timestamp) {
			if (SpeedController.config != SpeedControllerConfig.Normal) {
				SpeedController.config = SpeedControllerConfig.Normal;
			}
		}

		#endregion
	}
}
