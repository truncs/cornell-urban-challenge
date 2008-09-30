using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Behaviors;
using OperationalLayer.Tracking;

namespace OperationalLayer.OperationalBehaviors {
	class HoldBrake : IOperationalBehavior {
		#region IOperationalBehavior Members

		public void OnBehaviorReceived(Behavior b) {
			// just have the behavior manager execute the new behavior
			Services.BehaviorManager.Execute(b, null, false);
		}

		public string GetName() {
			return "HoldBrake";
		}

		public void OnTrackingCompleted(TrackingCompletedEventArgs e) {
			// nothing to do
		}

		public void Initialize(Behavior b) {
			Services.ObstaclePipeline.ExtraSpacing = 0;
			Services.ObstaclePipeline.UseOccupancyGrid = true;

			// set the tracking manager to run a constant braking behavior
			Services.TrackingManager.QueueCommand(TrackingCommandBuilder.GetHoldBrakeCommand());
		}

		public void Process(object param) {
			// nothing to do
		}

		public void Cancel() {
			// nothing to do
		}

		#endregion
	}
}
