using System;
using System.Collections.Generic;
using System.Text;
using OperationalLayer.Pose;
using UrbanChallenge.Common;

namespace OperationalLayer.Tracking.Steering {
	class ConstantSteeringCommandGenerator : ISteeringCommandGenerator {
		private double? targetSteeringAngle;
		private double? rateLimit;
		private bool terminating;

		private bool completed;

		public ConstantSteeringCommandGenerator() {
			this.targetSteeringAngle = null;
		}

		public ConstantSteeringCommandGenerator(double? steeringAngle, bool terminating) {
			this.targetSteeringAngle = steeringAngle;
			this.terminating = terminating;
			this.rateLimit = null;

			completed = false;
		}

		public ConstantSteeringCommandGenerator(double? steeringAngle, double? rateLimit, bool terminating) {
			this.targetSteeringAngle = steeringAngle;
			this.rateLimit = rateLimit;
			this.terminating = terminating;

			completed = false;
		}

		#region ISteeringCommandGenerator Members

		public void GetSteeringCommand(ref double? steeringAngle) {
			if (targetSteeringAngle.HasValue) {
				double currentSteeringAngle = Services.StateProvider.GetVehicleState().steeringAngle;

				if (rateLimit.HasValue) {
					if (targetSteeringAngle.Value > currentSteeringAngle) {
						steeringAngle = Math.Min(targetSteeringAngle.Value, currentSteeringAngle + rateLimit.Value*0.1);
					}
					else {
						steeringAngle = Math.Max(targetSteeringAngle.Value, currentSteeringAngle - rateLimit.Value * 0.1);
					}
				}
				else {
					steeringAngle = this.targetSteeringAngle.Value;
				}

				// check if we're within 5 degrees
				if (Math.Abs(currentSteeringAngle-targetSteeringAngle.Value) < 5*Math.PI/180.0) {
					completed = true;
				}
			}
			else {
				steeringAngle = null;
			}
		}

		#endregion

		#region ITrackingCommandBase Members

		public CompletionResult CompletionStatus {
			get {
				if (terminating && completed) {
					return CompletionResult.Completed;
				}
				else {
					return CompletionResult.Working;
				}
			}
		}

		public object FailureData {
			get { return null; }
		}

		public void BeginTrackingCycle(CarTimestamp timestamp) {
			// nothing to do here
		}

		#endregion
	}
}
