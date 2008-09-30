using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using System.Diagnostics;

namespace OperationalLayer.Tracking.SpeedControl {
	class StopSpeedGenerator : ISpeedGenerator {
		private IDistanceProvider distProvider;
		private bool stopped;
		private double baseSpeed;
		private CarTimestamp curTimestamp;

		public StopSpeedGenerator(IDistanceProvider distProvider, double baseSpeed) {
			this.distProvider = distProvider;
			this.baseSpeed = baseSpeed;
			this.stopped = false;
		}

		#region ISpeedGenerator Members

		public SpeedControlData GetCommandedSpeed() {
			double commandedSpeed = 0;
			double remainingDist = distProvider.GetRemainingDistance();
			Services.Dataset.ItemAs<double>("requested stop distance").Add(remainingDist, curTimestamp);
			stopped = SpeedUtilities.GetStoppingSpeedCommand(remainingDist, baseSpeed, Services.StateProvider.GetVehicleState(), ref commandedSpeed);
			TrackingManager.TraceSource.TraceEvent(TraceEventType.Verbose, 0, "stop speed generator - remaining dist {0:F2}, base speed {1:F2}, commanded speed {2:F2}, stopped {3}", remainingDist, baseSpeed, commandedSpeed, stopped);

			return new SpeedControlData(commandedSpeed, null);
		}

		#endregion

		#region ITrackingCommandBase Members

		public CompletionResult CompletionStatus {
			get {
				if (stopped)
					return CompletionResult.Completed;
				else
					return CompletionResult.Working;
			}
		}

		public object FailureData {
			get { return null; }
		}

		public void BeginTrackingCycle(CarTimestamp timestamp) {
			if (SpeedController.config != SpeedControllerConfig.Stopping) {
				SpeedController.Reset();
				SpeedController.config = SpeedControllerConfig.Stopping;
			}

			distProvider.Transform(timestamp);

			curTimestamp = timestamp;
		}

		#endregion
	}
}
