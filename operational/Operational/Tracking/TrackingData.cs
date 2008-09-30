using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Vehicle;

namespace OperationalLayer.Tracking {
	class TrackingData {
		public double? engineTorque;
		public double? brakePressure;
		public TransmissionGear? gear;
		public double? steeringAngle;
		public CompletionResult result;

		public TrackingData(double? engineTorque, double? brakePressure, TransmissionGear? gear, double? steeringAngle, CompletionResult result) {
			this.engineTorque = engineTorque;
			this.brakePressure = brakePressure;
			this.gear = gear;
			this.steeringAngle = steeringAngle;
			this.result = result;
		}
	}
}
