using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Utility;

namespace OperationalLayer.Pose {
	class HeadingBiasEstimator {
		private const double auto_corr_time = 600; // 10 minute auto-correlation time
		private const double nom_heading_sigma = 1*Math.PI/180.0;
		private const double measurement_sigma_0 = 4*Math.PI/180.0;
		private const double measurement_sigma_wz = 0.1;

		private double mean;
		private double var;

		public HeadingBiasEstimator() {
			mean = 0;
			var = Math.Pow(nom_heading_sigma,2); // 1 deg variance
		}

		public void Update(double yawRate, double heading, Vector3 velENU, double dt, CarTimestamp ts) {
			bool inReverse = Services.StateProvider.GetVehicleState().transGear == UrbanChallenge.Common.Vehicle.TransmissionGear.Reverse;
			// predict forward the state
			double lamda = Math.Exp(-dt/auto_corr_time);
			mean *= lamda;
			var = lamda*lamda*var + (1 - lamda*lamda)*nom_heading_sigma*nom_heading_sigma;

			// calculate the measurement 
			double ve = velENU.X;
			double vn = velENU.Y;
			double v = Math.Sqrt(ve*ve + vn*vn);

			// ignore if we're going less than 2.5 m/s (approx 5 mph)
			if (v < 2.5) return;

			// calculate measurement 
			double vel_heading = Math.Atan2(vn, ve);
			if (inReverse) {
				vel_heading += Math.PI;
			}
			vel_heading = MathUtil.WrapAngle(vel_heading, heading);
			Services.Dataset.ItemAs<double>("vel heading").Add(vel_heading, ts);

			double heading_bias = vel_heading - heading;

			// calculate measurement variance
			double measurement_var = measurement_sigma_0*measurement_sigma_0;
			if (v < 7.5) {
				// add in 3 deg noise at 2.5, 0 at 7.5
				double sigma_v_fac = Math.Pow(5*Math.PI/180.0,2)/5;
				measurement_var += sigma_v_fac*(7.5-v);
			}

			// add in noise for yaw rate
			measurement_var += Math.Abs(yawRate)*measurement_sigma_wz;

			double innov = heading_bias - mean;
			double innov_var = var + measurement_var;
			double gain = var/innov_var;
			mean += gain*innov;
			var *= (1-gain);
		}

		public double CorrectHeading(double receviedHeading) {
			return receviedHeading + mean;
		}

		public double HeadingBias {
			get { return mean; }
		}
	}
}
