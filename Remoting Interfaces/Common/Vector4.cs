using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common {
	[Serializable]
	public struct Vector4 {
		public double X, Y, Z, W;

		public Vector4(double x, double y, double z, double w) {
			this.X = x;
			this.Y = y;
			this.Z = z;
			this.W = w;
		}

		public double Dot(Vector4 r) {
			return X*r.X + Y*r.Y + Z*r.Z + W*r.W;
		}

		public double Length {
			get {
				return Math.Sqrt(LengthSq);
			}
		}

		public double LengthSq {
			get {
				return Dot(this);
			}
		}

		public Vector4 Normalize() {
			return this / Length;
		}

		public Vector4 Normalize(double l) {
			return this * (l / Length);
		}

		public static Vector4 operator /(Vector4 l, double c) {
			return new Vector4(l.X / c, l.Y / c, l.Z / c, l.W / c);
		}

		public static Vector4 operator *(Vector4 l, double c) {
			return new Vector4(l.X * c, l.Y * c, l.Z * c, l.W * c);
		}

		public static Vector4 operator *(double c, Vector4 l) {
			return new Vector4(l.X * c, l.Y * c, l.Z * c, l.W * c);
		}

		public static bool operator ==(Vector4 l, Vector4 r) {
			return l.X == r.X && l.Y == r.Y && l.Z == r.Z && l.W == r.W;
		}

		public static bool operator !=(Vector4 l, Vector4 r) {
			return l.X != r.X || l.Y != r.Y || l.Z != r.Z || l.W != r.W;
		}

		public static Vector4 operator +(Vector4 l, Vector4 r) {
			return new Vector4(l.X + r.X, l.Y + r.Y, l.Z + r.Z, l.W + r.W);
		}

		public static Vector4 operator -(Vector4 l, Vector4 r) {
			return new Vector4(l.X - r.X, l.Y - r.Y, l.Z - r.Z, l.W-r.W);
		}

		public override bool Equals(object obj) {
			if (obj is Vector4) {
				return (Vector4)obj == this;
			}
			else {
				return false;
			}
		}

		public override int GetHashCode() {
			return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode() + W.GetHashCode(); ;
		}
	}
}
