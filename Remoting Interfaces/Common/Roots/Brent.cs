using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Integration;

namespace UrbanChallenge.Common.Roots {
	/// <summary>
	/// Implements Brent's Method for finding roots roots of a function. Useful 
	/// when an explicit derivative cannot be computed.
	/// </summary>
	/// <remarks>
	/// This implementation was adapted from GSL (GNU Scientific Library). It appears
	/// to work, but I'm not sure if actually implements Brent's method.
	/// 
	/// General usage:
	/// Construct a class with a function and initial lower and upper bounds. The bounds
	/// must straddle the zero point (i.e. f(lower)*f(upper) &lt; 0)
	/// 
	/// Call iterate, passing in reference Parameters for new upper and lower bounds. When
	/// the bounds have converged within the desired tolerance, you are done.
	/// </remarks>
	public class BrentsMethod {
		private RootFunction f;
		private double a, b, c, d, e;
		private double fa, fb, fc;

		public BrentsMethod(RootFunction f, double tlower, double tupper) {
			this.f = f;

			this.a = tlower;
			double flower = this.fa = f(tlower);

			this.b = tupper;
			double fupper = this.fb = f(tupper);

			this.c = tupper;
			this.fc = fupper;

			this.d = tupper - tlower;
			this.e = tupper - tlower;

			if ((flower < 0 && fupper < 0) || (flower > 0 && fupper > 0)) {
				throw new ArgumentException("Endpoints do not straddle f = 0");
			}
		}

		public double Iterate(ref double x_lower, ref double x_upper) {
			double tol, m;

			bool ac_equal = false;

			if ((fb < 0 && fc < 0) || (fb > 0 && fc > 0)) {
				ac_equal = true;
				c = a;
				fc = fa;
				d = b - a;
				e = b - a;
			}

			if (Math.Abs(fc) < Math.Abs(fb)) {
				ac_equal = true;
				a = b;
				b = c;
				c = a;
				fa = fb;
				fb = fc;
				fc = fa;
			}

			tol = 0.5 * QuadConst.dbl_eps * Math.Abs(b);
			m = 0.5 * (c - b);

			if (fb == 0) {
				x_lower = b;
				x_upper = b;

				return b;
			}

			if (Math.Abs(m) <= tol) {
				if (b < c) {
					x_lower = b;
					x_upper = c;
				}
				else {
					x_lower = c;
					x_upper = b;
				}

				return b;
			}

			if (Math.Abs(e) < tol || Math.Abs(fa) <= Math.Abs(fb)) {
				d = m;            /* use bisection */
				e = m;
			}
			else {
				double p, q, r;   /* use inverse cubic interpolation */
				double s = fb / fa;

				if (ac_equal) {
					p = 2 * m * s;
					q = 1 - s;
				}
				else {
					q = fa / fc;
					r = fb / fc;
					p = s * (2 * m * q * (q - r) - (b - a) * (r - 1));
					q = (q - 1) * (r - 1) * (s - 1);
				}

				if (p > 0) {
					q = -q;
				}
				else {
					p = -p;
				}

				if (2 * p < Math.Min(3 * m * q - Math.Abs(tol * q), Math.Abs(e * q))) {
					e = d;
					d = p / q;
				}
				else {
					/* interpolation failed, fall back to bisection */

					d = m;
					e = m;
				}
			}

			a = b;
			fa = fb;

			if (Math.Abs(d) > tol) {
				b += d;
			}
			else {
				b += (m > 0 ? +tol : -tol);
			}

			fb = f(b);

			/* Update the best estimate of the root and bounds on each
				 iteration */

			if ((fb < 0 && fc < 0) || (fb > 0 && fc > 0)) {
				c = a;
			}

			if (b < c) {
				x_lower = b;
				x_upper = c;
			}
			else {
				x_lower = c;
				x_upper = b;
			}

			return b;
		}
	}
}
