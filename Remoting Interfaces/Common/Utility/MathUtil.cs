using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using UrbanChallenge.Common.Mapack;

namespace UrbanChallenge.Common.Utility {
	public static class MathUtil {
		public static int Choose(int n, int k) {
			Debug.Assert(n >= k);
			int[] b = new int[n + 1];
			b[0] = 1;

			for (int i = 1; i <= n; i++) {
				b[i] = 1;
				for (int j = i - 1; j > 0; j--) {
					b[j] += b[j - 1];
				}
			}

			return b[k];
		}

		public static double Round(double v, int n) {
			v *= Math.Pow(10, n);
			double ceil = Math.Ceiling(v);
			double floor = Math.Floor(v);
			if (Math.Abs(ceil - v) < Math.Abs(floor - v))
				return ceil / Math.Pow(10,n);
			else
				return floor / Math.Pow(10, n);
		}

		public static void ComputeMeanCovariance(IList<Coordinates> pts, out Coordinates mean, out Matrix2 cov) {
			// for better numerical stability, use a two-pass method where we first compute the mean
			double sum_x = 0, sum_y = 0;
			for (int i = 0; i < pts.Count; i++) {
				sum_x += pts[i].X;
				sum_y += pts[i].Y;
			}

			double avg_x = sum_x/(double)pts.Count;
			double avg_y = sum_y/(double)pts.Count;

			// now compute variances and covariances
			double sum_x2 = 0, sum_y2 = 0, sum_xy = 0;
			for (int i = 0; i < pts.Count; i++) {
				double dx = pts[i].X - avg_x;
				double dy = pts[i].Y - avg_y;

				sum_x2 += dx*dx;
				sum_y2 += dy*dy;
				sum_xy += dx*dy;
			}

			double var_x = sum_x2/(double)pts.Count;
			double var_y = sum_y2/(double)pts.Count;
			double cov_xy = sum_xy/(double)pts.Count;

			mean = new Coordinates(avg_x, avg_y);
			cov = new Matrix2(var_x, cov_xy, cov_xy, var_y);
		}

		public static double WrapAngle(double angle, double targetAngle) {
			double rAngle = Math.IEEERemainder(angle, 2*Math.PI);
			if (rAngle < 0.0) {
				rAngle += 2*Math.PI;
			}

			double nwrap;
			//find out the number of full 2*pi wraps the target angle has made
			nwrap = Math.Floor(targetAngle / (2*Math.PI));
			//wrap the input angle that many times
			rAngle += nwrap*(2*Math.PI);

			//if rAngle isn't within PI of iTargetAngle, it's within one wrap
			if (targetAngle - rAngle > Math.PI) {
				rAngle += 2*Math.PI;
			}
			if (targetAngle - rAngle < -Math.PI) {
				rAngle -= 2*Math.PI;
			}

			return rAngle;
		}
	}
}
