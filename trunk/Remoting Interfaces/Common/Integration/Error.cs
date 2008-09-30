using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.Integration {
	static class Error {
		public static double RescaleError(double err, double result_abs, double result_asc) {
			err = Math.Abs(err);

			if (result_asc != 0 && err != 0) {
				double scale = Math.Pow((200 * err / result_asc), 1.5);

				if (scale < 1) {
					err = result_asc * scale;
				}
				else {
					err = result_asc;
				}
			}
			if (result_abs > QuadConst.dbl_min_pos / (50 * QuadConst.dbl_eps)) {
				double min_err = 50 * QuadConst.dbl_eps * result_abs;

				if (min_err > err) {
					err = min_err;
				}
			}

			return err;
		}
	}
}
