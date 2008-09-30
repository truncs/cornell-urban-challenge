using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using OperationalLayer.Pose;

namespace OperationalLayer.Tracking.SpeedControl {
	class FeedbackSpeedCommandGenerator : ISpeedCommandGenerator {
		private ISpeedGenerator speedGenerator;

		public FeedbackSpeedCommandGenerator(ISpeedGenerator speedGenerator) {
			if (speedGenerator == null) {
				throw new ArgumentNullException();
			}

			this.speedGenerator = speedGenerator;
		}

		#region ISpeedCommandGenerator Members

		public OperationalSpeedCommand GetSpeedCommand() {
			SpeedControlData speedData = speedGenerator.GetCommandedSpeed();

			double engineTorque;
			double brakeTorque;

			SpeedController.ComputeCommands(speedData, Services.StateProvider.GetVehicleState(), out engineTorque, out brakeTorque);

			return new OperationalSpeedCommand(engineTorque, brakeTorque, null);
		}

		#endregion

		#region ITrackingCommandBase Members

		public CompletionResult CompletionStatus {
			get { return speedGenerator.CompletionStatus; }
		}

		public object FailureData {
			get { return speedGenerator.FailureData; }
		}

		public void BeginTrackingCycle(CarTimestamp timestamp) {
			speedGenerator.BeginTrackingCycle(timestamp);
		}

		#endregion
	}
}
