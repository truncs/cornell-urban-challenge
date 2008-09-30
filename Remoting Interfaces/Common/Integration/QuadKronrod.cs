using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.Integration {
	static class QuadKronrod {
		public static void IntegrationQK(int n, double[] xgk, double[] wg, double[] wgk, double[] fv1, double[] fv2,
			QuadFunction f, double a, double b, out double result, out double abserr, out double resabs, out double resasc) {

			double center = 0.5 * (a + b);
			double half_length = 0.5 * (b - a);
			double abs_half_length = Math.Abs(half_length);
			double f_center = f(center);

			double result_gauss = 0;
			double result_kronrod = f_center * wgk[n - 1];

			double result_abs = Math.Abs(result_kronrod);
			double result_asc = 0;
			double mean = 0, err = 0;

			if (n % 2 == 0) {
				result_gauss = f_center * wg[n / 2 - 1];
			}

			for (int j = 0; j < (n - 1) / 2; j++) {
				int jtw = j * 2 + 1;
				double abscissa = half_length * xgk[jtw];
				double fval1 = f(center - abscissa);
				double fval2 = f(center + abscissa);
				double fsum = fval1 + fval2;

				fv1[jtw] = fval1;
				fv2[jtw] = fval2;
				result_gauss += wg[j] * fsum;
				result_kronrod += wgk[jtw] * fsum;
				result_abs += wgk[jtw] * (Math.Abs(fval1) + Math.Abs(fval2));
			}

			for (int j = 0; j < n / 2; j++) {
				int jtwm1 = j * 2;
				double abscissa = half_length * xgk[jtwm1];
				double fval1 = f(center - abscissa);
				double fval2 = f(center + abscissa);

				fv1[jtwm1] = fval1;
				fv2[jtwm1] = fval2;
				result_kronrod += wgk[jtwm1] * (fval1 + fval2);
				result_abs += wgk[jtwm1] * (Math.Abs(fval1) + Math.Abs(fval2));
			}

			mean = result_kronrod * 0.5;
			result_asc = wgk[n - 1] * Math.Abs(f_center - mean);

			for (int j = 0; j < n - 1; j++) {
				result_asc += wgk[j] * (Math.Abs(fv1[j] - mean) + Math.Abs(fv2[j] - mean));
			}

			/* scale by the width of the integration region */

			err = (result_kronrod - result_gauss) * half_length;

			result_kronrod *= half_length;
			result_abs *= abs_half_length;
			result_asc *= abs_half_length;

			result = result_kronrod;
			resabs = result_abs;
			resasc = result_asc;
			abserr = Error.RescaleError(err, result_abs, result_asc);
		}
	}
}
