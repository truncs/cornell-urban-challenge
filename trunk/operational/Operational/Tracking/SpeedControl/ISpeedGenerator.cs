using System;
using System.Collections.Generic;
using System.Text;

namespace OperationalLayer.Tracking.SpeedControl {
	struct SpeedControlData {
		public double? speed;
		public double? accel;

		public SpeedControlData(double? speed, double? accel) {
			this.speed = speed;
			this.accel = accel;
		}
	}

	interface ISpeedGenerator : ITrackingCommandBase {
		SpeedControlData GetCommandedSpeed();
	}
}
