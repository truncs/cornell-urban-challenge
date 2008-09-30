using System;
using System.Collections.Generic;
using System.Text;
using OperationalLayer.Tracking.SpeedControl;
using OperationalLayer.Tracking.Steering;
using UrbanChallenge.Common;

namespace OperationalLayer.Tracking {
	class TrackingCommand : ITrackingCommand {
		private ISpeedCommandGenerator speedCommandGenerator;
		private ISteeringCommandGenerator steeringCommandGenerator;
		private bool waitBoth;
		private string label;

		public TrackingCommand(ISpeedCommandGenerator speedCommandGenerator, ISteeringCommandGenerator steeringCommandGenerator, bool waitBoth) {
			this.speedCommandGenerator = speedCommandGenerator;
			this.steeringCommandGenerator = steeringCommandGenerator;
			this.waitBoth = waitBoth;
			this.label = "default";
		}

		public TrackingData Process() {
			// get the current time
			CarTimestamp curTime = Services.RelativePose.CurrentTimestamp;

			// notify the command generators that the planning cycle is beginning
			speedCommandGenerator.BeginTrackingCycle(curTime);
			steeringCommandGenerator.BeginTrackingCycle(curTime);

			// get the commands
			OperationalSpeedCommand speedCommand = speedCommandGenerator.GetSpeedCommand();
			double? steering = null;
			steeringCommandGenerator.GetSteeringCommand(ref steering);

			CompletionResult result;
			CompletionResult resultSteering = steeringCommandGenerator.CompletionStatus;
			CompletionResult resultSpeed = speedCommandGenerator.CompletionStatus;

			if (resultSteering == CompletionResult.Failed || resultSpeed == CompletionResult.Failed) {
				result = CompletionResult.Failed;
			}
			else if (waitBoth && (resultSteering == CompletionResult.Completed && resultSpeed == CompletionResult.Completed)) {
				result = CompletionResult.Completed;
			}
			else if (!waitBoth && (resultSteering == CompletionResult.Completed || resultSpeed == CompletionResult.Completed)) {
				result = CompletionResult.Completed;
			}
			else {
				result = CompletionResult.Working;
			}

			return new TrackingData(speedCommand.engineTorque, speedCommand.brakePressure, speedCommand.transGear, steering, result);
		}

		public string Label { 
			get { return label; }
			set { label = value; }
		}

		public ISpeedCommandGenerator SpeedCommandGenerator {
			get { return speedCommandGenerator; }
		}

		public ISteeringCommandGenerator SteeringCommandGenerator {
			get { return steeringCommandGenerator; }
		}
	}
}
