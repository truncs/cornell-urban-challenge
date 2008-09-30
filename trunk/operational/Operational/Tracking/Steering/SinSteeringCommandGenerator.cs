using System;
using System.Collections.Generic;
using System.Text;

namespace OperationalLayer.Tracking.Steering {
	class SinSteeringCommandGenerator : ISteeringCommandGenerator {
		#region ISteeringCommandGenerator Members

		public void GetSteeringCommand(ref double? steeringAngle) {
			// calculate a sinusoidal curvature
			double theta = (DateTime.Now.Second + DateTime.Now.Millisecond/1000.0)/30*2*Math.PI;
			double curvature = 0.075*Math.Sin(theta);

			steeringAngle = SteeringUtilities.CurvatureToSteeringWheelAngle(curvature, 1);
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
