using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Integration;
using UrbanChallenge.Common.Roots;

namespace UrbanChallenge.Common.Splines {
	[Serializable]
	public class CubicBezier {
		public struct NearestPointResult {
			public Coordinates NearestPoint;
			public double tval;

			public NearestPointResult(Coordinates p, double t) {
				this.NearestPoint = p;
				this.tval = t;
			}
		}

		private Coordinates p0, p1, p2, p3;

		private double arclen = -1;

		/// <summary>
		/// Constructs a cubic Bezier spline with the specified control points.
		/// </summary>
		/// <param name="p0">First control point/initial point of the spline</param>
		/// <param name="p1">Second control point</param>
		/// <param name="p2">Third control point</param>
		/// <param name="p3">Fourth control point/final point of the spline</param>
		public CubicBezier(Coordinates p0, Coordinates p1, Coordinates p2, Coordinates p3) {
			this.p0 = p0;
			this.p1 = p1;
			this.p2 = p2;
			this.p3 = p3;
		}

		/// <summary>
		/// Instantiates a Bezier spline with the Parameters from a Cubic Hermite spline
		/// </summary>
		/// <param name="p0">Initial point of cubic Hermite spline</param>
		/// <param name="m0">Initial tangent/speed of cubic Hermite spline</param>
		/// <param name="p1">Ending point of cubic Hermite spline</param>
		/// <param name="m1">Ending tangent/speed of cubic Hermite spline</param>
		/// <returns></returns>
		public static CubicBezier FromCubicHermite(Coordinates p0, Coordinates m0, Coordinates p1, Coordinates m1) {
			return new CubicBezier(p0, p0 + m0 / 3.0, p1 - m1 / 3.0, p1);
		}

		public Coordinates P0 {
			get { return p0; }
		}

		public Coordinates P1 {
			get { return p1; }
		}

		public Coordinates P2 {
			get { return p2; }
		}

		public Coordinates P3 {
			get { return p3; }
		}

		/// <summary>
		/// Returns the arc-length of the curve
		/// </summary>
		/// <remarks>
		/// This is computed numerically out to a tolerance of 1e-5
		/// </remarks>
		public double ArcLength {
			get {
				if (arclen < 0) {
					arclen = PartialArcLength(1);
				}

				return arclen;
			}
		}

		/// <summary>
		/// Computes the arc-length to a given value of t
		/// </summary>
		/// <param name="t">Control parameter</param>
		/// <returns>Computed arc-length</returns>
		public double PartialArcLength(double t) {
			double result = 0, error = 0;
			AdaptiveGuassianQuad.Integrate(delegate(double tv) { return dBdt(tv).Length; }, 0, t, 1e-8, 0, 1024, out result, out error, QK61.QK61Rule);
			return result;
		}

		/// <summary>
		/// Find the value of t corresponding to the target arc-length
		/// </summary>
		/// <param name="target_len">Target arc-length of the curve</param>
		/// <returns>Numerical approximation of t-value</returns>
		/// <remarks>
		/// Computes t to tolerance 1e-4
		/// </remarks>
		public double FindT(double target_len) {
			double al = ArcLength;

			if (target_len >= al) {
				return 1;
			}
			else if (target_len <= 0) {
				return 0;
			}

			RootFunction f = delegate(double t) { return PartialArcLength(t) - target_len; };

			// use these as initial values to find 
			double tupper = (target_len / al) * 1.1;
			double tlower = (target_len / al) * 0.9;

			if (tupper > 1)
				tupper = 1;

			while (f(tupper) < 0 && tupper < 1) {
				tupper *= 1.1;
			}

			if (tupper > 1)
				tupper = 1;

			while (f(tlower) > 0) {
				tlower *= 0.9;
			}

			int iters = 0;
			BrentsMethod br = new BrentsMethod(f, tlower, tupper);

			double tval = 0;
			do {
				tval = br.Iterate(ref tlower, ref tupper);
				iters++;
			} while (tupper - tlower > 1e-8);

			return tval;
		}

		/// <summary>
		/// Evaluates the cubic Bezier at t
		/// </summary>
		/// <param name="t">Control parameter</param>
		/// <returns></returns>
		public Coordinates Bt(double t) {
			double t1 = 1 - t;
			return p0 * t1 * t1 * t1 + 3 * p1 * t1 * t1 * t + 3 * p2 * t1 * t * t + p3 * t * t * t;
		}

		/// <summary>
		/// Evalutes first derivative of Bezier at t
		/// </summary>
		/// <param name="t">Control parameter</param>
		/// <returns></returns>
		public Coordinates dBdt(double t) {
			double t1 = 1 - t;
			return p0 * (-3 * t1 * t1) + p1 * (3 * t1 * t1 - 6 * t1 * t) + p2 * (6 * t1 * t - 3 * t * t) + p3 * (3 * t * t);
		}

		/// <summary>
		/// Evaluates second derivative of Bezier at t
		/// </summary>
		/// <param name="t">Control parameter</param>
		/// <returns></returns>
		public Coordinates d2Bdt2(double t) {
			double t1 = 1 - t;
			return 6 * p0 * t1 + 3 * p1 * (-4 * t1 + 2 * t) + 3 * p2 * (2 * t1 - 4 * t) + 6 * p3 * t;
		}

		/// <summary>
		/// Evaluates the curvature of Bezier at t
		/// </summary>
		/// <param name="t">Control parameter</param>
		/// <returns></returns>
		public double Curvature(double t) {
			Coordinates dB = dBdt(t);
			Coordinates d2B = d2Bdt2(t);

			double l = dB.Length;

			double k = dB.Cross(d2B) / (l * l * l);

			return k;
		}

		public CubicBezier[] Subdivide(double t) {
			Coordinates[] pts = new Coordinates[4];
			pts[0] = p0;
			pts[1] = p1;
			pts[2] = p2;
			pts[3] = p3;

			Coordinates[] b0 = new Coordinates[4];
			Coordinates[] b1 = new Coordinates[4];
			
			const int n = 3;

			b0[0] = pts[0];
			b1[n] = pts[n];

			for (int k = 1; k <= n; k++) {
				for (int i = 0; i <= n - k; i++) {
					pts[i] = (1 - t) * pts[i] + t * pts[i + i];
				}

				b0[k] = pts[0];
				b1[n - k] = pts[n - k];
			}

			return new CubicBezier[] { new CubicBezier(b0[0], b0[1], b0[2], b0[3]), new CubicBezier(b1[0], b1[1], b1[2], b1[3]) };
		}

		/// <summary>
		/// Finds the closest point to the bezier
		/// </summary>
		/// <param name="pt">Point to evaluate</param>
		/// <returns></returns>
		public NearestPointResult GetClosestPoint(Coordinates pt) {
			return BezierRootFinder.NearestPointOnCurve(new Coordinates[] { p0, p1, p2, p3 }, pt);
		}

		private static class BezierRootFinder {

			// Precomputed "z" for cubics
			private static readonly double[,] z = { { 1.0f, 0.6f, 0.3f, 0.1f }, { 0.4f, 0.6f, 0.6f, 0.4f }, { 0.1f, 0.3f, 0.6f, 1.0f } };

			private const int DEGREE = 3;
			private const int W_DEGREE = 5;

			private const int MAXDEPTH = 32;

			private static readonly double EPS = (double)Math.Pow(2, -MAXDEPTH - 1);

			public static NearestPointResult NearestPointOnCurve(Coordinates[] b, Coordinates p) {
				// convert points to bezier form

				if (b.Length != 4)
					throw new ArgumentException();

				/*  Convert problem to 5th-degree Bezier form	*/
				Coordinates[] w = ConvertToBezierForm(p, b);

				/* Find all possible roots of 5th-degree equation */
				double[] t_candidate = new double[W_DEGREE];
				int n_soln = FindRoots(w, W_DEGREE, t_candidate, 0);

				/* Compare distances of P to all candidates, and to t=0, and t=1 */
				double t;
				double dist, new_dist;

				Coordinates p1;

				/* Check distance to beginning of curve, where t = 0	*/
				dist = (p - b[0]).VectorLength2;
				t = 0;

				/* Find distances for candidate points	*/
				for (int i = 0; i < n_soln; i++) {
					p1 = Bezier(b, DEGREE, t_candidate[i], null, null);
					new_dist = (p - p1).VectorLength2;
					if (new_dist < dist) {
						dist = new_dist;
						t = t_candidate[i];
					}
				}

				/* Finally, look at distance to end point, where t = 1.0 */
				new_dist = (p - b[DEGREE]).VectorLength2;
				if (new_dist < dist) {
					dist = new_dist;
					t = 1;
				}

				/*  Return the point on the curve at parameter value t */
				NearestPointResult r = new NearestPointResult();
				r.NearestPoint = Bezier(b, DEGREE, t, null, null);
				r.tval = t;

				return r;
			}

			/// <summary>
			/// Given a point and a Bezier curve, generate a 5th-degree Bezier-format equation whose 
			/// solution finds the point on the curve nearest the user-defined point.
			/// </summary>
			/// <param name="P">The point to find t for</param>
			/// <param name="V">The control points</param>
			/// <returns>Control points of 5th-degree bezier curve</returns>
			private static Coordinates[] ConvertToBezierForm(Coordinates P, Coordinates[] V) {
				Coordinates[] c = new Coordinates[DEGREE + 1]; // V(i)'s - P
				Coordinates[] d = new Coordinates[DEGREE]; // V(i+1) - V(i)
				Coordinates[] w = new Coordinates[W_DEGREE + 1]; // Ctl pts of 5th-degree curve  
				double[,] cdTable = new double[3, 4];	// Dot product of c, d

				// Determine the c's -- these are vectors created by subtracting
				// point P from each of the control points
				for (int i = 0; i <= DEGREE; i++) {
					c[i] = V[i] - P;
				}

				// Determine the d's -- these are vectors created by subtracting
				// each control point from the next
				for (int i = 0; i <= DEGREE - 1; i++) {
					d[i] = (V[i + 1] - V[i]) * 3.0f;
				}

				// Create the c,d table -- this is a table of dot products of the 
				// c's and d's
				for (int row = 0; row <= DEGREE - 1; row++) {
					for (int col = 0; col <= DEGREE; col++) {
						cdTable[row, col] = d[row].Dot(c[col]);
					}
				}

				// Now, apply the z's to the dot products, on the skew diagonal
				// Also, set up the x-values, making these "points"
				for (int i = 0; i <= W_DEGREE; i++) {
					w[i].Y = 0.0f;
					w[i].X = i / (double)W_DEGREE;
				}

				int n = DEGREE;
				int m = DEGREE - 1;
				for (int k = 0; k <= n + m; k++) {
					int lb = Math.Max(0, k - m);
					int ub = Math.Min(k, n);

					for (int i = lb; i <= ub; i++) {
						int j = k - i;
						w[i + j].Y += cdTable[j, i] * z[j, i];
					}
				}

				return w;
			}

			/// <summary>
			/// Given a 5th-degree equation in Bernstein-Bezier form, find all of the roots in the interval [0, 1]. 
			/// </summary>
			/// <param name="w">The control points</param>
			/// <param name="degree">The degree of the polynomial</param>
			/// <param name="t">RETURN candidate t-values</param>
			/// <param name="depth">The depth of the recursion</param>
			/// <returns>The number of roots found.</returns>
			private static int FindRoots(Coordinates[] w, int degree, double[] t, int depth) {
				// New left and right control polygons
				Coordinates[] Left = new Coordinates[W_DEGREE + 1]; 
				Coordinates[] Right = new Coordinates[W_DEGREE + 1];

				// Solution count from children
				int left_count, right_count;
				// Solutions from children
				double[] left_t = new double[W_DEGREE + 1];
				double[] right_t = new double[W_DEGREE + 1];

				switch (CrossingCount(w, degree)) {
					case 0:
						// No solutions here
						return 0;

					case 1:
						// Unique solution
						// Stop recursion when the tree is deep enough
						// if deep enough, return 1 solution at midpoint 
						if (depth >= MAXDEPTH) {
							t[0] = (w[0].X + w[W_DEGREE].X) / 2.0f;
							return 1;
						}

						if (ControlPolygonFlatEnough(w, degree)) {
							t[0] = ComputeXIntercept(w, degree);
							return 1;
						}
						break;
				}

				// Otherwise, solve recursively after subdividing control polygon
				Bezier(w, degree, 0.5f, Left, Right);
				left_count = FindRoots(Left, degree, left_t, depth + 1);
				right_count = FindRoots(Right, degree, right_t, depth + 1);

				// Gather solutions together
				for (int i = 0; i < left_count; i++) {
					t[i] = left_t[i];
				}

				for (int i = 0; i < right_count; i++) {
					t[i + left_count] = right_t[i];
				}

				// Send back total number of solutions
				return left_count + right_count;
			}

			/// <summary>
			/// Count the number of times a Bezier control polygon crosses the 0-axis. This number is >= the number of roots.
			/// </summary>
			/// <param name="w">Control pts of Bezier curve</param>
			/// <param name="degree">Degreee of Bezier curve </param>
			/// <returns></returns>
			private static int CrossingCount(Coordinates[] w, int degree) {
				// Number of zero-crossings
				int n_crossings = 0;
				// Sign of coefficients
				int sign, old_sign;

				sign = old_sign = Math.Sign(w[0].Y);
				for (int i = 1; i <= degree; i++) {
					sign = Math.Sign(w[i].Y);
					if (sign != old_sign) n_crossings++;
					old_sign = sign;
				}

				return n_crossings;
			}

			/// <summary>
			/// Check if the control polygon of a Bezier curve is flat enough for recursive subdivision to bottom out.
			/// </summary>
			/// <param name="V">Control points</param>
			/// <param name="degree">Degree of polynomial</param>
			/// <returns></returns>
			private static bool ControlPolygonFlatEnough(Coordinates[] V, int degree) {
				// Distances from pts to line
				double[] distance = new double[degree + 1];
				// maximum of these
				double max_dist_above, max_dist_below;
				// Precision of root
				double error;
				double intercept_1, intercept_2, left_intercept, right_intercept;
				// Coefficients of implicit eqn for line from V[0]-V[deg]
				double a, b, c;

				double abSquared;

				// Find the perpendicular distance from each interior control point to line connecting V[0] and V[degree]

				// Derive the implicit equation for line connecting first and last control points 
				a = V[0].Y - V[degree].Y;
				b = V[degree].X - V[0].X;
				c = V[0].X * V[degree].Y - V[degree].X * V[0].Y;

				abSquared = (a * a) + (b * b);

				for (int i = 1; i < degree; i++) {
					// Compute distance from each of the points to that line
					distance[i] = a * V[i].X + b * V[i].Y + c;
					if (distance[i] > 0.0) {
						distance[i] = (distance[i] * distance[i]) / abSquared;
					}

					if (distance[i] < 0.0) {
						distance[i] = -((distance[i] * distance[i]) / abSquared);
					}
				}

				// Find the largest distance
				max_dist_above = 0;
				max_dist_below = 0;

				for (int i = 1; i < degree; i++) {
					if (distance[i] < 0.0) {
						max_dist_below = Math.Min(max_dist_below, distance[i]);
					}
					if (distance[i] > 0.0) {
						max_dist_above = Math.Max(max_dist_above, distance[i]);
					}
				}

				double det, dInv;
				double a1, b1, c1, a2, b2, c2;

				// Implicit equation for zero line 
				a1 = 0.0f;
				b1 = 1.0f;
				c1 = 0.0f;

				// Implicit equation for "above" line 
				a2 = a;
				b2 = b;
				c2 = c + max_dist_above;

				det = a1 * b2 - a2 * b1;
				dInv = 1.0f / det;

				intercept_1 = (b1 * c2 - b2 * c1) * dInv;

				// Implicit equation for "below" line 
				a2 = a;
				b2 = b;
				c2 = c + max_dist_below;

				det = a1 * b2 - a2 * b1;
				dInv = 1.0f / det;

				intercept_2 = (b1 * c2 - b2 * c1) * dInv;

				// Compute intercepts of bounding box
				left_intercept = Math.Min(intercept_1, intercept_2);
				right_intercept = Math.Max(intercept_1, intercept_2);

				error = 0.5f * (right_intercept - left_intercept);
				if (error < EPS) {
					return true;
				}
				else {
					return false;
				}
			}

			/// <summary>
			/// Compute intersection of chord from first control point to last with 0-axis.
			/// </summary>
			/// <param name="V">Control points</param>
			/// <param name="degree">Degree of curve</param>
			/// <returns></returns>
			private static double ComputeXIntercept(Coordinates[] V, int degree) {
				double XLK, YLK, XNM, YNM, XMK, YMK;
				double det, detInv;
				double S;
				double X;

				XLK = 1.0f - 0.0f;
				YLK = 0.0f - 0.0f;
				XNM = V[degree].X - V[0].X;
				YNM = V[degree].Y - V[0].Y;
				XMK = V[0].X - 0.0f;
				YMK = V[0].Y - 0.0f;

				det = XNM * YLK - YNM * XLK;
				detInv = 1.0f / det;

				S = (XNM * YMK - YNM * XMK) * detInv;
				X = 0.0f + XLK * S;

				return X;
			}

			/// <summary>
			/// Evaluate a Bezier curve at a particular parameter value
			/// Fill in control points for resulting sub-curves if "Left" and "Right" are non-null.
			/// </summary>
			/// <param name="V">Control pts</param>
			/// <param name="degree">Degree of bezier curve</param>
			/// <param name="t">Parameter value</param>
			/// <param name="Left">RETURN left half ctl pts</param>
			/// <param name="Right">RETURN right half ctl pts</param>
			/// <returns></returns>
			private static Coordinates Bezier(Coordinates[] V, int degree, double t, Coordinates[] Left, Coordinates[] Right) {
				Coordinates[,] Vtemp = new Coordinates[W_DEGREE + 1, W_DEGREE + 1];

				// Copy control points
				for (int j = 0; j <= degree; j++) {
					Vtemp[0, j] = V[j];
				}

				// Triangle computation
				for (int i = 1; i <= degree; i++) {
					for (int j = 0; j <= degree - 1; j++) {
						Vtemp[i, j] = (1 - t) * Vtemp[i - 1, j] + t * Vtemp[i - 1, j + 1];
					}
				}

				if (Left != null) {
					for (int j = 0; j <= degree; j++) {
						Left[j] = Vtemp[j, 0];
					}
				}

				if (Right != null) {
					for (int j = 0; j <= degree; j++) {
						Right[j] = Vtemp[degree - j, j];
					}
				}

				return Vtemp[degree, 0];
			}
		}
	}
}
