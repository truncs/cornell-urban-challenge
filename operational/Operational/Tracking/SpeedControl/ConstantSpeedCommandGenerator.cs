using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Vehicle;

namespace OperationalLayer.Tracking.SpeedControl {
	class ConstantSpeedCommandGenerator : ISpeedCommandGenerator {
		public static readonly ConstantSpeedCommandGenerator BrakeHold = new ConstantSpeedCommandGenerator(0, TahoeParams.brake_hold);
		public static readonly ConstantSpeedCommandGenerator BrakeHard = new ConstantSpeedCommandGenerator(0, TahoeParams.brake_hard);

		private double engineTorque;
		private double brakePressure;
		private TransmissionGear? transGear;

		public ConstantSpeedCommandGenerator(double engineTorque, double brakePressure) {
			this.engineTorque = engineTorque;
			this.brakePressure = brakePressure;
			this.transGear = null;
		}

		public ConstantSpeedCommandGenerator(double engineTorque, double brakePressure, TransmissionGear? transGear) {
			this.engineTorque = engineTorque;
			this.brakePressure = brakePressure;
			this.transGear = transGear;
		}

		#region ISpeedCommandGenerator Members

		public OperationalSpeedCommand GetSpeedCommand() {
			return new OperationalSpeedCommand(engineTorque, brakePressure, transGear);
		}

		#endregion

		#region ITrackingCommandBase Members

		public CompletionResult CompletionStatus {
			get { return CompletionResult.Working; }
		}

		public object FailureData {
			get { return null; }
		}

		public void BeginTrackingCycle(UrbanChallenge.Common.CarTimestamp timestamp) {
			// nothing to do
		}

		#endregion
	}
}
