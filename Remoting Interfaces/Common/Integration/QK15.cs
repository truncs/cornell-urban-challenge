using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.Integration {
	public static class QK15 {
		static readonly double[] xgk = new double[]   /* abscissae of the 15-point kronrod rule */
			{
				0.991455371120812639206854697526329,
				0.949107912342758524526189684047851,
				0.864864423359769072789712788640926,
				0.741531185599394439863864773280788,
				0.586087235467691130294144838258730,
				0.405845151377397166906606412076961,
				0.207784955007898467600689403773245,
				0.000000000000000000000000000000000
			};

		/* xgk[1], xgk[3], ... abscissae of the 20-point gauss rule. 
			 xgk[0], xgk[2], ... abscissae to optimally extend the 20-point gauss rule */

		static readonly double[] wg = new double[]    /* weights of the 7-point gauss rule */
			{
				0.129484966168869693270611432679082,
				0.279705391489276667901467771423780,
				0.381830050505118944950369775488975,
				0.417959183673469387755102040816327
			};

		static readonly double[] wgk = new double[]  /* weights of the 15-point kronrod rule */
			{
				0.022935322010529224963732008058970,
				0.063092092629978553290700663189204,
				0.104790010322250183839876322541518,
				0.140653259715525918745189590510238,
				0.169004726639267902826583426598550,
				0.190350578064785409913256402421014,
				0.204432940075298892414161999234649,
				0.209482141084727828012999174891714
			};

		public static void QK15Rule(QuadFunction f, double a, double b, out double result, out double abserr, out double resabs, out double resasc) {
			double[] fv1 = new double[8];
			double[] fv2 = new double[8];
			QuadKronrod.IntegrationQK(8, xgk, wg, wgk, fv1, fv2, f, a, b, out result, out abserr, out resabs, out resasc);
		}
	}
}
