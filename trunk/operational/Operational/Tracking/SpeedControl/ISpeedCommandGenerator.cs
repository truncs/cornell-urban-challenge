using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Vehicle;

namespace OperationalLayer.Tracking.SpeedControl {
	struct OperationalSpeedCommand {
		public double engineTorque;
		public double brakePressure;
		public TransmissionGear? transGear;

		public OperationalSpeedCommand(double engineTorque, double brakePressure, TransmissionGear? transGear) {
			this.engineTorque = engineTorque;
			this.brakePressure = brakePressure;
			this.transGear = transGear;
		}
	}

	interface ISpeedCommandGenerator : ITrackingCommandBase {
		OperationalSpeedCommand GetSpeedCommand();
	}
}
