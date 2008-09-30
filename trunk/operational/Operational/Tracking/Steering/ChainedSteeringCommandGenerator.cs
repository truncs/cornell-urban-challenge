using System;
using System.Collections.Generic;
using System.Text;

namespace OperationalLayer.Tracking.Steering {
	class ChainedSteeringCommandGenerator : ISteeringCommandGenerator {
		private ISteeringCommandGenerator first, second;

		public ChainedSteeringCommandGenerator(ISteeringCommandGenerator first, ISteeringCommandGenerator second) {
			this.first = first;
			this.second = second;
		}

		#region ISteeringCommandGenerator Members

		public void GetSteeringCommand(ref double? steeringAngle) {
			first.GetSteeringCommand(ref steeringAngle);
			if (steeringAngle == null) {
				second.GetSteeringCommand(ref steeringAngle);
			}
		}

		#endregion

		#region ITrackingCommandBase Members

		public CompletionResult CompletionStatus {
			get {
				return CompletionResult.Working;
			}
		}

		public object FailureData {
			get { return null; }
		}

		public void BeginTrackingCycle(UrbanChallenge.Common.CarTimestamp timestamp) {
			first.BeginTrackingCycle(timestamp);
			second.BeginTrackingCycle(timestamp);
		}

		#endregion
	}
}
