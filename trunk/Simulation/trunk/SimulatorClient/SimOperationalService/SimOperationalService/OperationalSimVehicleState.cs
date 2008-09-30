using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Vehicle;

namespace SimOperationalService {
	[Serializable]
	public class OperationalSimVehicleState {
		public const string ChannelName = "OperationalSimState";

		private Coordinates position;
		private double speed;
		private double heading;
		private double steeringAngle;
		private TransmissionGear transGear;
		private double engineTorque;
		private double brakePressure;
		private double engineRPM;
		private CarMode carMode;
		private double stopLineDistance;
		private bool stoplineFound;
		private CarTimestamp timestamp;

		public OperationalSimVehicleState(Coordinates position, double speed, double heading, double steeringAngle, TransmissionGear transGear, double engineTorque, double brakePressure, double engineRPM, CarMode carMode, double stopLineDistance, bool stoplineFound, CarTimestamp timestamp) {
			this.position = position;
			this.speed = speed;
			this.heading = heading;
			this.steeringAngle = steeringAngle;
			this.transGear = transGear;
			this.engineTorque = engineTorque;
			this.brakePressure = brakePressure;
			this.engineRPM = engineRPM;
			this.carMode = carMode;
			this.stopLineDistance = stopLineDistance;
			this.stoplineFound = stoplineFound;
			this.timestamp = timestamp;
		}

		public Coordinates Position {
			get { return position; }
		}

		public double Speed {
			get { return speed; }
		}

		public double Heading {
			get { return heading; }
		}

		public double SteeringAngle {
			get { return steeringAngle; }
		}

		public TransmissionGear TransmissionGear {
			get { return transGear; }
		}

		public double EngineTorque {
			get { return engineTorque; }
		}

		public double BrakePressure {
			get { return brakePressure; }
		}

		public double EngineRPM {
			get { return engineRPM; }
		}

		public CarMode CarMode {
			get { return carMode; }
		}

		public double StopLineDistance {
			get { return stopLineDistance; }
		}

		public bool StopLineFound {
			get { return stoplineFound; }
		}

		public CarTimestamp Timestamp {
			get { return timestamp; }
		}
	}
}
