using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Vehicle;

using UrbanChallenge.OperationalUIService.Parameters;
using OperationalLayer.Pose;

namespace OperationalLayer.Tracking.Steering {
	static class CurvaturePathFollowing {
		// tunable Parameters
		private static TunableParam q1_param;
		private static TunableParam q2_param;
		private static TunableParam r_param;
		private static TunableParam offtrack_error_max_param;

		static CurvaturePathFollowing() {
			// obtain values of tunable Parameters
			TunableParamTable table = Services.Params;

			q1_param = table.GetParam("steering q1", "steering", 0);
			q2_param = table.GetParam("steering q2", "steering", 0);
			r_param	 = table.GetParam("steering r",  "steering", 10000);

			offtrack_error_max_param = table.GetParam("offtrack error max", "steering", 2);
		}

		public static void ComputeSteeringCommand(IRelativePath path, double dt, out double steeringWheelAngle) {
			ComputeSteeringCommand(path, dt, true, out steeringWheelAngle);
		}

		public static void ComputeSteeringCommand(IRelativePath path, double dt, bool outputDataset, out double steeringWheelAngle) {
			// get steering Parameters
			double q1 = q1_param.Value;
			double q2 = q2_param.Value;
			double r  = r_param.Value;

			OperationalVehicleState vs = Services.StateProvider.GetVehicleState();

			// get the feedback data
			SteeringControlData data = path.GetSteeringControlData(new SteeringControlDataOptions(vs.speed*TahoeParams.actuation_delay));

			double offtrackError = data.offtrackError;
			double headingError  = data.headingError;
			// for now, just use 0 for the curvature if it is null
			// later, we may want to switch to pure-pursuit in this case
			double curvature     = data.curvature.GetValueOrDefault(0);

			// cap the off-track error term
			if (offtrackError > offtrack_error_max_param.Value) {
				offtrackError = Math.Sign(offtrackError)*offtrack_error_max_param.Value;
			}

			// adjust the heading error 180 degrees if we're in reverse
			if (vs.transGear == TransmissionGear.Reverse) {
				headingError += Math.PI;
			}

			// wrap heading error between -pi and pi
			headingError = Math.IEEERemainder(headingError, 2*Math.PI);
			// check for numerical problems
			if (double.IsNaN(headingError)) {
				headingError = 0;
				Console.WriteLine("Error: Heading Error is NaN (In OpPathFollowingBehavior:Process)");
			}
			else if (Math.Abs(headingError) > Math.PI) {
				headingError -= Math.Sign(headingError)*2*Math.PI;
			}

			// compute desired curvature
			double desiredCurvature = (offtrackError * Math.Sqrt(q1 / r)) + 
                                (headingError  * Math.Sqrt(q2 / r)) + 
                                (curvature     );

			// reverse desired curvature if in reverse gear
			if (vs.transGear == TransmissionGear.Reverse) {
				desiredCurvature = -desiredCurvature;
			}

			// check desired curvature
			if (double.IsNaN(desiredCurvature)) {
				Console.WriteLine("Error: Commanded Curvature is NaN (In OpPathFollowingBehavior:Process)");
				desiredCurvature = 0;
			}

			// add the commands to the dataset
			if (outputDataset) {
				CarTimestamp now = Services.CarTime.Now;
				Services.Dataset.ItemAs<double>("offtrack error").Add(offtrackError, now);
				Services.Dataset.ItemAs<double>("heading error").Add(headingError, now);
				Services.Dataset.ItemAs<double>("target curvature").Add(curvature, now);
				Services.Dataset.ItemAs<double>("commanded curvature").Add(desiredCurvature, now);
				//Operational.Instance.Dataset.ItemAs<Coordinates>("path tangent").Add(curPoint.segment.Tangent(curPoint).Normalize(), now);
			}

			// convert the desired curvature to a steering wheel angle and return it
			steeringWheelAngle = SteeringUtilities.CurvatureToSteeringWheelAngle(desiredCurvature, vs.speed);
		}
	}
}
