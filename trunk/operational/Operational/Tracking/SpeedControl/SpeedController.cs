using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.OperationalService;
using UrbanChallenge.Common.Utility;

using OperationalLayer.Pose;

namespace OperationalLayer.Tracking.SpeedControl {
	/// <summary>
	/// Provides basic speed control feedback operations. This is a static class so that 
	/// the integral error does not get reset with every new command (which would effectively
	/// make the integral error irrelevant). 
	/// </summary>
	static class SpeedController {
		// tunable Parameters
		public static SpeedControllerConfig config = SpeedControllerConfig.Normal;

		private static double zero_speed_int = 0;
		private static double int_err = 0;
		private static double prev_err = double.NaN;

		private const double zero_speed_int_rate = 0.25;
		private const double zero_speed_int_min_speed = 0.2;
		private const double zero_speed_int_max_apply_speed = 1;
		private const double zero_speed_int_reset_speed = 2;

		private static DateTime zero_speed_time = DateTime.MinValue;

		static SpeedController() {
		}

		public static void ComputeCommands(SpeedControlData data, OperationalVehicleState vs, out double commandedEngineTorque, out double commandedBrakePressure) {
			Services.Dataset.ItemAs<double>("commanded speed").Add(data.speed.GetValueOrDefault(-1), Services.CarTime.Now);

			// speed (m/s)
			double v = Math.Abs(vs.speed);

			double max_accel;
			if (!vs.IsInDrive) {
				// we're in reverse
				max_accel = 0.65;
			}
			else if (v < 10*0.44704) {
				// below 10 mph
				max_accel = 1.0;
			}
			else if (v < 15*0.44704) {
				// below 15 mph
				double coeff = (v-10*0.44704)/((15-10)*0.44704);
				max_accel = 1.0 - coeff*0.25;
			}
			else {
				max_accel = 0.75;
			}

			if (data.speed.HasValue && data.speed.Value <= 0 && (!data.accel.HasValue || data.accel.Value <= 0)) {
				DateTime now = HighResDateTime.Now;
				if (zero_speed_time == DateTime.MinValue) {
					zero_speed_time = now;
				}

				if (now - zero_speed_time < TimeSpan.FromMilliseconds(500) || v < 1) {
					commandedEngineTorque = 0;
					commandedBrakePressure = TahoeParams.brake_hold;
					return;
				}
			}
			else {
				zero_speed_time = DateTime.MinValue;
			}

			// desired speed (m/s)
			double rv = Math.Abs(data.speed.GetValueOrDefault(v));

			double d_err = 0;
			double err = data.speed.HasValue ? v - rv : 0;
			if (data.speed.HasValue && !double.IsNaN(prev_err)) {
				d_err = (prev_err - err)*Settings.TrackingPeriod;
				Services.Dataset.ItemAs<double>("speed - d error").Add(d_err, Services.CarTime.Now);
			}

			prev_err = err;

			// commanded acceleration (m / s^2)
			double ra = data.accel.GetValueOrDefault(0) - config.kp*(err) - config.ki*int_err + config.kd*d_err;

			// cap the maximum acceleration
			if (ra > max_accel)
				ra = max_accel;

			// add in the zero speed integral term
			ra += zero_speed_int*Math.Max(0, 1 - v/zero_speed_int_max_apply_speed);

			Services.Dataset.ItemAs<double>("requested acceleration").Add(ra, Services.CarTime.Now);

			// compute commands to achieve the requested acceleration and determine if we could achieve those commands
			bool couldAchieveRA = SpeedUtilities.GetCommandForAcceleration(ra, vs, out commandedEngineTorque, out commandedBrakePressure);

			// only accumulate integral error if we could achieve the target acceleration
			if (couldAchieveRA) {
				if (ra < max_accel && data.speed.HasValue) {
					int_err *= config.ki_leak;
					int_err += (err)*Settings.TrackingPeriod;
					if (Math.Abs(int_err) > config.ki_cap) {
						int_err = Math.Sign(int_err) * config.ki_cap;
					}
				}

				if (v < zero_speed_int_min_speed && ra > 0.2) {
					zero_speed_int += zero_speed_int_rate*Settings.TrackingPeriod;
				}
			}

			if (v > zero_speed_int_reset_speed) {
				zero_speed_int = 0;
			}

			Services.Dataset.ItemAs<double>("speed - int error").Add(int_err, Services.CarTime.Now);
		}

		public static void Reset() {
			int_err = 0;
			prev_err = double.NaN;
		}
	}
}
