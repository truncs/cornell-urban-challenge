using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Mapack;

namespace UrbanChallenge.Common.Shapes {
	[Serializable]
	public struct Line : IEquatable<Line> {
		public Coordinates P0, P1;

		public Line(Coordinates p0, Coordinates p1) {
			this.P0 = p0;
			this.P1 = p1;
		}

		public bool Intersect(Line l, out Coordinates pt) {
			Coordinates K;
			return Intersect(l, out pt, out K);
		}

		public bool Intersect(Line l, out Coordinates pt, out Coordinates K) {
			Coordinates P = P1 - P0;
			Coordinates S = l.P1 - l.P0;

			double cross = P.Cross(S);
			if (Math.Abs(cross) < 1e-10) {
				pt = default(Coordinates);
				K = default(Coordinates);
				return false;
			}

			Matrix2 A = new Matrix2(
				P.X, -S.X,
				P.Y, -S.Y
				);
			K = A.Inverse()*(l.P0 - P0);
			pt = P0 + P*K.X;
			return true;
		}

		public bool Intersect(LineSegment l, out Coordinates pt) {
			return l.Intersect(this, out pt);
		}

		public bool Intersect(LineSegment l, out Coordinates pts, out Coordinates K) {
			return l.Intersect(this, out pts, out K);
		}

		public bool Intersect(Circle c, out Coordinates[] pts) {
			return c.Intersect(this, out pts);
		}

		public bool Intersect(Circle c, out Coordinates[] pts, out double[] K) {
			return c.Intersect(this, out pts, out K);
		}

		public Line Transform(IPointTransformer transformer) {
			if (transformer == null)
				throw new ArgumentNullException("transformer");

			return new Line(transformer.TransformPoint(P0), transformer.TransformPoint(P1));
		}

		public Coordinates ClosestPoint(Coordinates pt) {
			Coordinates t = UnitVector;
			return P0 + t*(t.Dot(pt - P0));
		}

		public bool IsToLeft(Coordinates pt) {
			return (pt-P0).Cross(P1-P0) > 0;
		}

		public Line ShiftLateral(double dist) {
			Coordinates norm = (P1 - P0).Normalize().Rotate90();
			return new Line(P0 + norm*dist, P1 + norm*dist);
		}

		public Coordinates UnitVector { get { return (P1-P0).Normalize(); } }

		#region IEquatable<Line> Members

		public bool Equals(Line other) {
			return P0.Equals(other.P0) && P1.Equals(other.P1);
		}

		#endregion

		public override bool Equals(object obj) {
			if (obj is Line)
				return Equals((Line)obj);
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
