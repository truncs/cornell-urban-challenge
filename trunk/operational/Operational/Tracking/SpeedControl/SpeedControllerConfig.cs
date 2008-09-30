using System;
using System.Collections.Generic;
using System.Text;

namespace OperationalLayer.Tracking.SpeedControl {
	class SpeedControllerConfig {
		public readonly double kp;
		public readonly double ki;
		public readonly double ki_cap;
		public readonly double ki_leak;
		public readonly double kd;

		public SpeedControllerConfig(double kp, double ki, double ki_cap, double ki_leak, double kd) {
			this.kp = kp;
			this.ki = ki;
			this.ki_cap = ki_cap;
			this.ki_leak = ki_leak;
			this.kd = kd;
		}

		public static readonly SpeedControllerConfig Normal = new SpeedControllerConfig(0.8, 0.02, 40.0, 0.9999, 0);
		public static readonly SpeedControllerConfig Stopping = new SpeedControllerConfig(1.5, 0.05, 40.0, 0.9999, 0);
	}
}
