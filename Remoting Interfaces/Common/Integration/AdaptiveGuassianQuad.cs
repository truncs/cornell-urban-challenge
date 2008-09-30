using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.Integration {
	public static class AdaptiveGuassianQuad {
		public static void Integrate(QuadFunction f, double a, double b, double epsabs, double epsrel, int limit, out double result, out double abserr, IntegrationRule q) {
			double area, errsum;
			double result0, abserr0, resabs0, resasc0;
			double tolerance;
			int iteration = 0;
			int roundoff_type1 = 0, roundoff_type2 = 0, error_type = 0;

			double round_off;

			/* Initialize results */
			IntegrationWorkspace workspace = new IntegrationWorkspace(limit, a, b);

			result = 0;
			abserr = 0;

			if (epsabs <= 0 && (epsrel < 50 * QuadConst.dbl_eps || epsrel < 0.5e-28)) {
				throw new ArgumentException("Tolerance cannot be achieved with given epsabs and epsrel");
			}

			/* perform the first integration */

			q(f, a, b, out result0, out abserr0, out resabs0, out resasc0);

			workspace.SetInitialResult(result0, abserr0);

			/* Test on accuracy */

			tolerance = Math.Max(epsabs, epsrel * Math.Abs(result0));

			/* need IEEE rounding here to match original quadpack behavior */

			round_off = 50 * QuadConst.dbl_eps * resabs0;

			if (abserr0 <= round_off && abserr0 > tolerance) {
				result = result0;
				abserr = abserr0;

				throw new InvalidOperationException("cannot reach tolerance because of roundoff error on first attempt");
			}
			else if ((abserr0 <= tolerance && abserr0 != resasc0) || abserr0 == 0.0) {
				result = result0;
				abserr = abserr0;

				//return GSL_SUCCESS;
				return;
			}
			else if (limit == 1) {
				result = result0;
				abserr = abserr0;

				throw new InvalidOperationException("a maximum of one iteration was insufficient");
			}

			area = result0;
			errsum = abserr0;

			iteration = 1;

			do {
				double a1, b1, a2, b2;
				double a_i, b_i, r_i, e_i;
				double area1 = 0, area2 = 0, area12 = 0;
				double error1 = 0, error2 = 0, error12 = 0;
				double resasc1, resasc2;
				double resabs1, resabs2;

				/* Bisect the subinterval with the largest error estimate */

				workspace.Retrieve(out a_i, out b_i, out r_i, out e_i);

				a1 = a_i;
				b1 = 0.5 * (a_i + b_i);
				a2 = b1;
				b2 = b_i;

				q(f, a1, b1, out area1, out error1, out resabs1, out resasc1);
				q(f, a2, b2, out area2, out error2, out resabs2, out resasc2);

				area12 = area1 + area2;
				error12 = error1 + error2;

				errsum += (error12 - e_i);
				area += area12 - r_i;

				if (resasc1 != error1 && resasc2 != error2) {
					double delta = r_i - area12;

					if (Math.Abs(delta) <= 1.0e-5 * Math.Abs(area12) && error12 >= 0.99 * e_i) {
						roundoff_type1++;
					}
					if (iteration >= 10 && error12 > e_i) {
						roundoff_type2++;
					}
				}

				tolerance = Math.Max(epsabs, epsrel * Math.Abs(area));

				if (errsum > tolerance) {
					if (roundoff_type1 >= 6 || roundoff_type2 >= 20) {
						error_type = 2;   /* round off error */
					}

					/* set error flag in the case of bad integrand behaviour at
						 a point of the integration range */

					if (workspace.SubintervalTooSmall(a1, a2, b2)) {
						error_type = 3;
					}
				}

				workspace.Update(a1, b1, area1, error1, a2, b2, area2, error2);

				workspace.Retrieve(out a_i, out b_i, out r_i, out e_i);

				iteration++;

			}
			while (iteration < limit && (error_type == 0) && errsum > tolerance);

			result = workspace.SumResults();
			abserr = errsum;

			if (errsum <= tolerance) {
				//return GSL_SUCCESS;
			}
			else if (error_type == 2) {
				throw new InvalidOperationException("roundoff error prevents tolerance from being achieved");
				/*GSL_ERROR ("roundoff error prevents tolerance from being achieved",
									 GSL_EROUND);*/
			}
			else if (error_type == 3) {
				throw new InvalidOperationException("bad integrand found in the integration interval");
				/*GSL_ERROR ("bad integrand behavior found in the integration interval",
									 GSL_ESING);*/
			}
			else if (iteration == limit) {
				//GSL_ERROR ("maximum number of subdivisions reached", GSL_EMAXITER);
				throw new InvalidOperationException("maximum number of subdivisions reached");
			}
			else {
				//GSL_ERROR ("could not integrate function", GSL_EFAILED);
				throw new InvalidOperationException("could not integrate function");
			}
		}
	}
}
