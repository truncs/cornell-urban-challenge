using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Vehicle;

namespace OperationalLayer.Tracking.Steering {
	static class SteeringUtilities {
		public static double CurvatureToSteeringWheelAngle(double c, double v) {
			return AckermanAngleToSteeringWheelAngle(CurvatureToAckermanAngle(c,v));
		}

		public static double CurvatureToAckermanAngle(double c, double v) {
			// compute ackerman angle
			double ca = Math.Abs(c);
			double db1 = Math.Atan(TahoeParams.L*ca / (Math.Sqrt(1 - Math.Pow(ca, 2)*Math.Pow(TahoeParams.Lr, 2)) + 0.5*ca*TahoeParams.T));
			double db2 = Math.Atan(TahoeParams.L*ca / (Math.Sqrt(1 - Math.Pow(ca, 2)*Math.Pow(TahoeParams.Lr, 2)) - 0.5*ca*TahoeParams.T));
			double dbar = Math.Sign(c)*(0.5*(db1 + db2)) + TahoeParams.cs0*Math.Pow(v, 2)*c;

			return dbar;
		}

		public static double AckermanAngleToSteeringWheelAngle(double dbar) {
			// compute steering wheel angle
			double steeringWheelAngle = TahoeParams.s0 + TahoeParams.s1*dbar + TahoeParams.s3*Math.Pow(dbar, 3);

			// limit steering wheel angle
			if (steeringWheelAngle > TahoeParams.SW_max)  steeringWheelAngle = TahoeParams.SW_max;
			if (steeringWheelAngle < -TahoeParams.SW_max) steeringWheelAngle = -TahoeParams.SW_max;

			return steeringWheelAngle;
		}
	}
}
