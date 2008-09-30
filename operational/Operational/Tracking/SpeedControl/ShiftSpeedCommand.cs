using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Vehicle;
using OperationalLayer.Pose;
using UrbanChallenge.Common;

namespace OperationalLayer.Tracking.SpeedControl {
	class ShiftSpeedCommand : ISpeedCommandGenerator {
		private TransmissionGear gear;

		private enum Phase {
			Braking,
			Shifting
		}

		private Phase phase;
		private CompletionResult result;

		public ShiftSpeedCommand(TransmissionGear gear) {
			this.gear = gear;
			this.phase = Phase.Braking;
			this.result = CompletionResult.Working;
		}

		#region ISpeedCommandGenerator Members

		public OperationalSpeedCommand GetSpeedCommand() {
			OperationalVehicleState vs = Services.StateProvider.GetVehicleState();
			OperationalSpeedCommand cmd = new OperationalSpeedCommand();
			cmd.brakePressure = TahoeParams.brake_hold;
			cmd.engineTorque = 0;

			if (result != CompletionResult.Failed) {
				if (phase == Phase.Braking) {
					// chek if we can transition to shifting
					if (Math.Abs(vs.speed) < 0.05 && vs.brakePressure >= TahoeParams.brake_hold-1) {
						phase = Phase.Shifting;
					}
				}

				if (phase == Phase.Shifting) {
					cmd.transGear = gear;

					if (vs.transGear == gear) {
						result = CompletionResult.Completed;
					}
				}
			}

			return cmd;
		}

		#endregion

		#region ITrackingCommandBase Members

		public CompletionResult CompletionStatus {
			get { return result; }
		}

		public object FailureData {
			get {
				return null;
			}
		}

		public void BeginTrackingCycle(CarTimestamp timestamp) {
			// nothing to do
		}

		#endregion
	}
}
