using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Mapack;

namespace UrbanChallenge.Common.Splines {
	public static class SmoothingSpline {
		public static CubicBezier[] BuildCatmullRomSpline(Coordinates[] pts, Coordinates? m0, Coordinates? mn) {
			return BuildCardinalSpline(pts, m0, mn, 0);
		}

		public static CubicBezier[] BuildCardinalSpline(Coordinates[] pts, Coordinates? m0, Coordinates? mn, double tension) {
			if (pts.Length < 2) {
				throw new ArgumentException("There must be at least 2 points to construct cardinal spline", "pts");
			}

			// compute reasonable starting and ending tangents if not supplied
			if (m0 == null)
				m0 = tension * (pts[1] - pts[0]);

			if (mn == null)
				mn = tension * (pts[pts.Length - 1] - pts[pts.Length - 2]);

			if (pts.Length == 2) {
				return new CubicBezier[] { CubicBezier.FromCubicHermite(pts[0], m0.Value, pts[1], mn.Value) };
			}

			// we have > 2 points, so we will have n-1 beziers
			CubicBezier[] beziers = new CubicBezier[pts.Length - 1];

			// fill in the first bezier
			beziers[0] = CubicBezier.FromCubicHermite(pts[0], m0.Value, pts[1], tension * (pts[2] - pts[0]));

			// iterate through and fill in all but the last
			for (int i = 1; i < pts.Length - 2; i++) {
				beziers[i] = CubicBezier.FromCubicHermite(pts[i], tension * (pts[i + 1] - pts[i - 1]), pts[i + 1], tension * (pts[i + 2] - pts[i]));
			}

			// fill int the last bezier
			beziers[pts.Length - 2] = CubicBezier.FromCubicHermite(pts[pts.Length - 2], tension * (pts[pts.Length - 1] - pts[pts.Length - 3]), pts[pts.Length - 1], mn.Value);

			// return the beziers
			return beziers;
		}

		public static CubicBezier[] BuildC2Spline(Coordinates[] pts, Coordinates? m0, Coordinates? mn, double tension) {
			if (pts.Length < 2) {
				throw new ArgumentException("There must be at least 2 points to construct cardinal spline", "pts");
			}

			// compute reasonable starting and ending tangents if not supplied
			if (m0 == null)
				m0 = tension * (pts[1] - pts[0]);

			if (mn == null)
				mn = tension * (pts[pts.Length - 1] - pts[pts.Length - 2]);

			if (pts.Length == 2) {
				return new CubicBezier[] { CubicBezier.FromCubicHermite(pts[0], m0.Value, pts[1], mn.Value) };
			}

			int n = pts.Length - 1;

			// we have > 2 points, so we will have n-1 beziers
			CubicBezier[] beziers = new CubicBezier[n];

			// build up constraint matrix
			Matrix A = new Matrix(2 * n, 2 * n, 0);

			Matrix b = new Matrix(2 * n, 2);

			// matrix row index value
			int idx = 0;

			// add starting/ending tangent constraint
			Coordinates p01 = m0.Value / 3.0 + pts[0];
			A[idx, 0] = 1;
			b[idx, 0] = p01.X; b[idx, 1] = p01.Y;
			idx++;

			Coordinates pn2 = -mn.Value / 3.0 + pts[n];
			A[idx, 2 * n - 1] = 1;
			b[idx, 0] = pn2.X; b[idx, 1] = pn2.Y;
			idx++;

			// add C1 constraints
			for (int i = 0; i < n-1; i++) {
				A[idx, i * 2 + 1] = 1;
				A[idx, (i + 1) * 2] = 1;
				b[idx, 0] = pts[i + 1].X * 2; b[idx, 1] = pts[i + 1].Y * 2;
				idx++;
			}

			// add C2 constraints
			for (int i = 0; i < n-1; i++) {
				A[idx, i * 2] = 1;
				A[idx, i * 2 + 1] = -2;
				A[idx, (i + 1) * 2] = 2;
				A[idx, (i + 1) * 2 + 1] = -1;
				b[idx, 0] = 0; b[idx, 1] = 0;
				idx++;
			}

			// build the LUDecomposition of A to solve system A*P = b;
			LuDecomposition lu = new LuDecomposition(A);
			Matrix P = lu.Solve(b);

			// work back the Parameters
			for (int i = 0; i < n; i++) {
				beziers[i] = new CubicBezier(pts[i], new Coordinates(P[2 * i, 0], P[2 * i, 1]), new Coordinates(P[2 * i + 1, 0], P[2 * i + 1, 1]), pts[i + 1]);
			}

			return beziers;
		}
	}
}
