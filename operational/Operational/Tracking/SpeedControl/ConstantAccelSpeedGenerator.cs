using System;
using System.Collections.Generic;
using System.Text;

namespace OperationalLayer.Tracking.SpeedControl {
	class ConstantAccelSpeedGenerator : ISpeedGenerator {
		private double commandedAccel;

		public ConstantAccelSpeedGenerator(double commandedAccel) {
			this.commandedAccel = commandedAccel;
		}

		#region ISpeedGenerator Members

		public SpeedControlData GetCommandedSpeed() {
			return new SpeedControlData(null, commandedAccel);
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
			
		}

		#endregion
	}
}
