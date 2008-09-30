using System;
using System.Collections.Generic;
using System.Text;
using Dataset.Source;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common;

namespace OperationalLayer.Pose {
	class VehicleStateProvider {
		public OperationalVehicleState GetVehicleState() {
			DatasetSource ds = Services.Dataset;

			return new OperationalVehicleState(
				ds.ItemAs<double>("speed").CurrentValue,
				ds.ItemAs<double>("actual steering").CurrentValue,
				ds.ItemAs<TransmissionGear>("transmission gear").CurrentValue,
				ds.ItemAs<double>("pitch").CurrentValue,
				ds.ItemAs<double>("brake pressure").CurrentValue,
				ds.ItemAs<double>("engine torque").CurrentValue,
				ds.ItemAs<double>("rpm").CurrentValue,
				ds.ItemAs<double>("pedal position").CurrentValue,
				ds.ItemAs<double>("actual steering").CurrentTime);
		}

		public AbsoluteTransformer GetAbsoluteTransformer(CarTimestamp timestamp) {
			AbsolutePose pose;
			if (Settings.UsePosteriorPose) {
				pose = Services.AbsolutePosteriorPose.GetAbsolutePose(timestamp);
			}
			else {
				pose = Services.AbsolutePose.GetAbsolutePose(timestamp);
			}
			return new AbsoluteTransformer(pose.xy, pose.heading, pose.timestamp);
		}

		// Get the current transformation to take point from absolute to vehicle relative coordinates
		public AbsoluteTransformer GetAbsoluteTransformer() {
			return new AbsoluteTransformer(GetAbsolutePose());
		}

		public AbsolutePose GetAbsolutePose() {
			if (Settings.UsePosteriorPose) {
				return Services.AbsolutePosteriorPose.Current;
			}
			else {
				return Services.AbsolutePose.Current;
			}
		}

		public AbsolutePose GetAbsolutePose(CarTimestamp timestamp) {
			if (Settings.UsePosteriorPose) {
				return Services.AbsolutePosteriorPose.GetAbsolutePose(timestamp);
			}
			else {
				return Services.AbsolutePose.GetAbsolutePose(timestamp);
			}
		}
	}
}
