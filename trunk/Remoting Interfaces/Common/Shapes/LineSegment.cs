using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Mapack;

namespace UrbanChallenge.Common.Shapes {
	[Serializable]
	public struct LineSegment {
		public Coordinates P0, P1;

		public LineSegment(Coordinates p0, Coordinates p1) {
			this.P0 = p0;
			this.P1 = p1;
		}

		public bool Intersect(LineSegment l, out Coordinates pt) {
			Coordinates K;
			return Intersect(l, out pt, out K);
		}

		public bool Intersect(LineSegment l, out Coordinates pt, out Coordinates K) {
			Coordinates P = P1 - P0;
			Coordinates S = l.P1 - l.P0;

			Matrix2 A = new Matrix2(P.X, -S.X, P.Y, -S.Y);

			if (Math.Abs(A.Determinant()) < 1e-10) {
				pt = default(Coordinates);
				K = default(Coordinates);
				return false;
			}

			K = A.Inverse()*(l.P0 - P0);

			if (K.X >= 0 && K.X <= 1 && K.Y >= 0 && K.Y <= 1) {
				pt = P0 + P*K.X;
				return true;
			}
			else {
				pt = default(Coordinates);
				return false;
			}
		}

		public bool Intersect(Line l, out Coordinates pt) {
			Coordinates K;
			return Intersect(l, out pt, out K);
		}

		public bool Intersect(Line l, out Coordinates pt, out Coordinates K) {
			Coordinates P = P1 - P0;
			Coordinates S = l.P1 - l.P0;

			double cross = S.Cross(P);
			if (Math.Abs(cross) < 1e-10) {
				pt = default(Coordinates);
				K = default(Coordinates);
				return false;
			}

			Matrix2 A = new Matrix2(-S.Y, S.X, -P.Y, P.X);
			K = A*(l.P0 - P0)/cross;

			if (K.X >= 0 && K.X <= 1) {
				pt = P0 + P*K.X;
				return true;
			}
			else {
				pt = default(Coordinates);
				return false;
			}
		}

		public bool Intersect(Circle c, out Coordinates[] pts, out double[] K) {
			return c.Intersect(this, out pts, out K);
		}

		public bool Intersect(Circle c, out Coordinates[] pts) {
			return c.Intersect(this, out pts);
		}

		public Coordinates ClosestPoint(Coordinates pt) {
			double K;
			return ClosestPoint(pt, out K);
		}

		public Coordinates ClosestPoint(Coordinates pt, out double K) {
			Coordinates t = P1 - P0;
			double l = t.Length;
			t /= l;
			K = t.Dot(pt - P0);

			if (K < 0) {
				K = 0;
				return P0;
			}
			else if (K > l) {
				K = 1;
				return P1;
			}
			else {
				pt = P0 + t * K;
				K /= l;
				return pt;
			}
		}

		public LineSegment Transform(IPointTransformer transformer) {
			return new LineSegment(transformer.TransformPoint(P0), transformer.TransformPoint(P1));
		}

		public LineSegment ShiftLateral(double offset) {
			Coordinates norm = (P1 - P0).Normalize().Rotate90();
			return new LineSegment(P0 + norm*offset, P1 + norm*offset);
		}

		public bool IsToLeft(Coordinates pt) {
			return (pt-P0).Cross(P1-P0) > 0;
		}

		public static explicit operator Line(LineSegment ls) {
			return new Line(ls.P0, ls.P1);
		}

		public double Length {
			get { return P0.DistanceTo(P1); }
		}

		public Coordinates UnitVector {
			get { return (P1-P0).Normalize(); }
		}

		public Coordinates Vector {
			get { return (P1-P0); }
		}

		#region IEquatable<LineSegment> Members

		public bool Equals(LineSegment other) {
			return P0.Equals(other.P0) && P1.Equals(other.P1);
		}

		#endregion

		public override bool Equals(object obj) {
			if (obj is LineSegment)
				return Equals((LineSegment)obj);
			else
				return false;
		}

		public override int GetHashCode() {
			return P0.GetHashCode() ^ P1.GetHashCode();
		}

		public override string ToString() {
			return "(" + P0.ToString() + ")->(" + P1.ToString() + ")";
		}
	}
}
