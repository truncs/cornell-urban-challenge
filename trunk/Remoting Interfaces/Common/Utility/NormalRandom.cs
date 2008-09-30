using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.Utility {
	public class NormalRandom : Random {
		public NormalRandom() {
		}

		public NormalRandom(int seed)
			: base(seed) {
		}

		public override double NextDouble() {
			double u, v, s;
			do {
				u = base.NextDouble();
				v = base.NextDouble();

				s = u*u + v*v;
			} while (s == 0 || s > 1);

			return u*Math.Sqrt(-2*Math.Log(s)/s);
		}

		public double NextDouble(double mean, double sigma) {
			return NextDouble()*sigma + mean;
		}
	}
}
