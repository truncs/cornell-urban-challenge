using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Vehicle;
using OperationalLayer.Tracking.SpeedControl;
using OperationalLayer.Tracking.Steering;
using OperationalLayer.PathPlanning;
using UrbanChallenge.Common;

namespace OperationalLayer.Tracking {
	static class TrackingCommandBuilder {
		public static TrackingCommand GetHoldBrakeCommand() {
			return new TrackingCommand(
				new ConstantSpeedCommandGenerator(0, TahoeParams.brake_hold),
				new ConstantSteeringCommandGenerator(), true);
		}

		public static TrackingCommand GetHoldBrakeCommand(int brakePressure) {
			return new TrackingCommand(
				new ConstantSpeedCommandGenerator(0, brakePressure),
				new ConstantSteeringCommandGenerator(), true);
		}

		public static TrackingCommand GetShiftTransmissionCommand(TransmissionGear gear, double? steeringAngle, double? rateLimit, bool waitBoth) {
			return new TrackingCommand(new ShiftSpeedCommand(gear), new ConstantSteeringCommandGenerator(steeringAngle, rateLimit, true), waitBoth);
		}

		public static TrackingCommand GetConstantSteeringConstantSpeedCommand(double steering, double speed) {
			return new TrackingCommand(
				new FeedbackSpeedCommandGenerator(new ConstantSpeedGenerator(speed, null)),
				new ConstantSteeringCommandGenerator(steering, false),
				true);
		}

		public static TrackingCommand GetSmoothedPathVelocityCommand(SmoothedPath path) {
			return new TrackingCommand(new FeedbackSpeedCommandGenerator(path), new PathSteeringCommandGenerator(path), false);
		}

		public static TrackingCommand GetStopAtStoplinePathCommand(IRelativePath path, double baseSpeed) {
			return new TrackingCommand(new FeedbackSpeedCommandGenerator(new StopSpeedGenerator(new StoplineDistanceProvider(), baseSpeed)),
				new PathSteeringCommandGenerator(path), false);
		}

		public static TrackingCommand GetConstantSteeringStopAtDistPathCommand(double steering, double dist, CarTimestamp distTimestamp, double baseSpeed) {
			return new TrackingCommand(new FeedbackSpeedCommandGenerator(new StopSpeedGenerator(new TravelledDistanceProvider(distTimestamp, dist), baseSpeed)),
				new ConstantSteeringCommandGenerator(steering, false), false);
		}
	}
}
