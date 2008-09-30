using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common;

namespace OperationalLayer.Pose {
	class OperationalVehicleState {
		public readonly double speed;
		public readonly double steeringAngle;
		public readonly TransmissionGear transGear;
		public readonly double pitch;
		public readonly double brakePressure;
		public readonly double engineTorque;
		public readonly double engineRPM;
		public readonly double pedalPosition;
		public readonly CarTimestamp timestamp;

		public OperationalVehicleState(double speed, double steeringAngle, TransmissionGear transGear, double pitch, double brakePressure, double engineTorque, double engineRPM, double pedalPosition, CarTimestamp timestamp) {
			this.speed = speed;
			this.steeringAngle = steeringAngle;
			this.transGear = transGear;
			this.pitch = pitch;
			this.engineTorque = engineTorque;
			this.engineRPM = engineRPM;
			this.brakePressure = brakePressure;
			this.pedalPosition = pedalPosition;
			this.timestamp = timestamp;
		}

		public bool IsInDrive {
			get { return transGear >= TransmissionGear.First && transGear <= TransmissionGear.Fourth; }
		}

		public bool IsStopped {
			get { return Math.Abs(speed) <= 0.01; }
		}
	}
}
