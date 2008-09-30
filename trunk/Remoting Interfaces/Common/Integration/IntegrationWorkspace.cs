using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.Integration {
	class IntegrationWorkspace {
		public int limit;
		public int size;
		public int nrmax;
		public int i;
		public int maximum_level;

		public double[] alist;
		public double[] blist;
		public double[] rlist;
		public double[] elist;
		public int[] order;
		public int[] level;

		public IntegrationWorkspace(int n, double a, double b) {
			limit = n;
			alist = new double[n];
			blist = new double[n];
			rlist = new double[n];
			elist = new double[n];
			order = new int[n];
			level = new int[n];

			i = 0;
			nrmax = 0;
			size = 0;
			maximum_level = 0;

			alist[0] = a;
			blist[0] = b;
			rlist[0] = 0;
			elist[0] = 0;
			order[0] = 0;
			level[0] = 0;
		}

		public void SetInitialResult(double result, double error) {
			size = 1;
			rlist[0] = result;
			elist[0] = error;
		}

		public void Retrieve(out double a, out double b, out double r, out double e) {
			a = alist[i];
			b = blist[i];
			r = rlist[i];
			e = elist[i];
		}

		public double SumResults() {
			int n = size;
			double result_sum = 0;

			for (int k = 0; k < n; k++) {
				result_sum += rlist[k];
			}

			return result_sum;
		}

		public bool SubintervalTooSmall(double a1, double a2, double b2) {
			double tmp = (1 + 100 * QuadConst.dbl_eps) * (Math.Abs(a2) + 1000 * QuadConst.dbl_min_pos);
			return Math.Abs(a1) <= tmp && Math.Abs(b2) <= tmp;
		}

		public void Update(double a1, double b1, double area1, double error1,
											 double a2, double b2, double area2, double error2) {
			int i_max = i;
			int i_new = size;
			int new_level = level[i_max] + 1;

			/* append the newly-created intervals to the list */

			if (error2 > error1) {
				alist[i_max] = a2;        /* blist[maxerr] is already == b2 */
				rlist[i_max] = area2;
				elist[i_max] = error2;
				level[i_max] = new_level;

				alist[i_new] = a1;
				blist[i_new] = b1;
				rlist[i_new] = area1;
				elist[i_new] = error1;
				level[i_new] = new_level;
			}
			else {
				blist[i_max] = b1;        /* alist[maxerr] is already == a1 */
				rlist[i_max] = area1;
				elist[i_max] = error1;
				level[i_max] = new_level;

				alist[i_new] = a2;
				blist[i_new] = b2;
				rlist[i_new] = area2;
				elist[i_new] = error2;
				level[i_new] = new_level;
			}

			size++;

			if (new_level > maximum_level) {
				maximum_level = new_level;
			}

			qpsrt();
		}

		private void qpsrt() {
			int last = size - 1;
			int limit = this.limit;

			double errmax;
			double errmin;
			int i, k, top;

			int i_nrmax = nrmax;
			int i_maxerr = order[i_nrmax];

			/* Check whether the list contains more than two error estimates */

			if (last < 2) {
				order[0] = 0;
				order[1] = 1;
				i = i_maxerr;
				return;
			}

			errmax = elist[i_maxerr];

			/* This part of the routine is only executed if, due to a difficult
				 integrand, subdivision increased the error estimate. In the normal
				 case the insert procedure should start after the nrmax-th largest
				 error estimate. */

			while (i_nrmax > 0 && errmax > elist[order[i_nrmax - 1]]) {
				order[i_nrmax] = order[i_nrmax - 1];
				i_nrmax--;
			}

			/* Compute the number of elements in the list to be maintained in
				 descending order. This number depends on the number of
				 subdivisions still allowed. */

			if (last < (limit / 2 + 2)) {
				top = last;
			}
			else {
				top = limit - last + 1;
			}

			/* Insert errmax by traversing the list top-down, starting
				 comparison from the element elist(order(i_nrmax+1)). */

			i = i_nrmax + 1;

			/* The order of the tests in the following line is important to
				 prevent a segmentation fault */

			while (i < top && errmax < elist[order[i]]) {
				order[i - 1] = order[i];
				i++;
			}

			order[i - 1] = i_maxerr;

			/* Insert errmin by traversing the list bottom-up */

			errmin = elist[last];

			k = top - 1;

			while (k > i - 2 && errmin >= elist[order[k]]) {
				order[k + 1] = order[k];
				k--;
			}

			order[k + 1] = last;

			/* Set i_max and e_max */

			i_maxerr = order[i_nrmax];

			this.i = i_maxerr;
			this.nrmax = i_nrmax;
		}
	}
}
