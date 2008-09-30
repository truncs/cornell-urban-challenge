using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace UrbanChallenge.Common.Mapack {
	[Serializable]
	public unsafe struct Matrix3 : IPointTransformer {
		private double d0, d1, d2, d3, d4, d5, d6, d7, d8;

		public Matrix3(
			double d11, double d12, double d13,
			double d21, double d22, double d23,
			double d31, double d32, double d33) {

			d0 = d11; d1 = d12; d2 = d13;
			d3 = d21; d4 = d22; d5 = d23;
			d6 = d31; d7 = d32; d8 = d33;
		}

		public double this[int i, int j] {
			get {
				Debug.Assert(i >= 0 && i < 3 && j >= 0 && j < 3);
				switch (i) {
					case 0:
						switch (j) {
							case 0: return d0;
							case 1: return d1;
							case 2: return d2;
						}
						break;

					case 1:
						switch (j) {
							case 0: return d3;
							case 1: return d4;
							case 2: return d5;
						}
						break;

					case 2:
						switch (j) {
							case 0: return d6;
							case 1: return d7;
							case 2: return d8;
						}
						break;
				}

				throw new ArgumentOutOfRangeException();
			}
			set {
				Debug.Assert(i >= 0 && i < 3 && j >= 0 && j < 3);
				switch (i) {
					case 0:
						switch (j) {
							case 0: d0 = value; break;
							case 1: d1 = value; break;
							case 2: d2 = value; break;
						}
						break;

					case 1:
						switch (j) {
							case 0: d3 = value; break;
							case 1: d4 = value; break;
							case 2: d5 = value; break;
						}
						break;

					case 2:
						switch (j) {
							case 0: d6 = value; break;
							case 1: d7 = value; break;
							case 2: d8 = value; break;
						}
						break;
				}
			}
		}

		public double Determinant() {
			return d0*d4*d8 - d0*d5*d7 - d3*d1*d8 + d3*d2*d7 + d6*d1*d5 - d6*d2*d4;
		}

		public Matrix3 Inverse() {

			// d0*d4*d8 - d0*d5*d7 - d3*d1*d8 + d3*d2*d7 + d6*d1*d5 - d6*d2*d4
			double det = d0*d4*d8 - d0*d5*d7 - d3*d1*d8 + d3*d2*d7 + d6*d1*d5 - d6*d2*d4;

			// [  d4*d8-d5*d7, -d1*d8+d2*d7,  d1*d5-d2*d4]
			// [ -d3*d8+d5*d6,  d0*d8-d2*d6, -d0*d5+d2*d3]
			// [  d3*d7-d4*d6, -d0*d7+d1*d6,  d0*d4-d1*d3]
			return new Matrix3(
				( d4*d8 - d5*d7)/det, (-d1*d8 + d2*d7)/det, ( d1*d5 - d2*d4)/det,
				(-d3*d8 + d5*d6)/det, ( d0*d8 - d2*d6)/det, (-d0*d5 + d2*d3)/det,
				( d3*d7 - d4*d6)/det, (-d0*d7 + d1*d6)/det, ( d0*d4 - d1*d3)/det);
		}

		public Matrix3 Transpose() {
			return new Matrix3(
				d0, d3, d6,
				d1, d4, d7,
				d2, d5, d8);
		}

		public override string ToString() {
			return ToString("F4", 8);
		}

		public string ToString(string format, int alignment) {
			string formatString = "{0," + alignment + ":" + format + "}";
			StringBuilder sb = new StringBuilder();

			for (int row = 0; row < 3; row++) {
				sb.Append("[");
				for (int col = 0; col < 3; col++) {
					sb.AppendFormat(formatString, this[row, col]);

					if (col != 2) {
						sb.Append(", ");
					}
				}

				sb.Append("]");

				if (row != 2) {
					sb.AppendLine();
				}
			}

			return sb.ToString();
		}

		public Vector3 ExtractYPR() {
			double m11 = this[0, 0];
			double m12 = this[0, 1];
			double m13 = this[0, 2];
			double m23 = this[1, 2];
			double m33 = this[2, 2];
			double cos_pitch = Math.Sqrt(m11*m11 + m12*m12);
			double sin_pitch = -m13;
			double cos_roll = m33/cos_pitch;
			double sin_roll = m23/cos_pitch;
			double cos_yaw = m11/cos_pitch;
			double sin_yaw = m12/cos_pitch;

			return new Vector3(-Math.Atan2(sin_yaw, cos_yaw), Math.Atan2(sin_pitch, cos_pitch),- Math.Atan2(sin_roll, cos_roll));
		}

		#region operators

		public static Matrix3 operator *(Matrix3 l, Matrix3 r) {
			// [ ld0*rd0+ld1*rd3+ld2*rd6, ld0*rd1+ld1*rd4+ld2*rd7, ld0*rd2+ld1*rd5+ld2*rd8]
			// [ ld3*rd0+ld4*rd3+ld5*rd6, ld3*rd1+ld4*rd4+ld5*rd7, ld3*rd2+ld4*rd5+ld5*rd8]
			// [ ld6*rd0+ld7*rd3+ld8*rd6, ld6*rd1+ld7*rd4+ld8*rd7, ld6*rd2+ld7*rd5+ld8*rd8]
			return new Matrix3(
				l.d0*r.d0 + l.d1*r.d3 + l.d2*r.d6, l.d0*r.d1 + l.d1*r.d4 + l.d2*r.d7, l.d0*r.d2 + l.d1*r.d5 + l.d2*r.d8,
				l.d3*r.d0 + l.d4*r.d3 + l.d5*r.d6, l.d3*r.d1 + l.d4*r.d4 + l.d5*r.d7, l.d3*r.d2 + l.d4*r.d5 + l.d5*r.d8,
				l.d6*r.d0 + l.d7*r.d3 + l.d8*r.d6, l.d6*r.d1 + l.d7*r.d4 + l.d8*r.d7, l.d7*r.d2 + l.d7*r.d5 + l.d8*r.d8);
		}

		public static Vector3 operator *(Matrix3 l, Vector3 r) {
			return new Vector3(
				l.d0*r.X + l.d1*r.Y + l.d2*r.Z,
				l.d3*r.X + l.d4*r.Y + l.d5*r.Z,
				l.d6*r.X + l.d7*r.Y + l.d8*r.Z);
		}

		public static Vector3 operator *(Vector3 l, Matrix3 r) {
			return new Vector3(
				r.d0*l.X + r.d3*l.Y + r.d6*l.Z,
				r.d1*l.X + r.d4*l.Y + r.d7*l.Z,
				r.d2*l.X + r.d5*l.Y + r.d8*l.Z);
		}

		public static Matrix3 operator -(Matrix3 l) {
			return new Matrix3(
				-l.d0, -l.d1, -l.d2,
				-l.d3, -l.d4, -l.d5,
				-l.d6, -l.d7, -l.d8);
		}

		public static Matrix3 operator +(Matrix3 l, Matrix3 r) {
			return new Matrix3(
				l.d0 + r.d0, l.d1 + r.d1, l.d2 + r.d2,
				l.d3 + r.d3, l.d4 + r.d4, l.d5 + r.d5,
				l.d6 + r.d6, l.d7 + r.d7, l.d8 + r.d8);
		}

		public static Matrix3 operator -(Matrix3 l, Matrix3 r) {
			return new Matrix3(
				l.d0 - r.d0, l.d1 - r.d1, l.d2 - r.d2,
				l.d3 - r.d3, l.d4 - r.d4, l.d5 - r.d5,
				l.d6 - r.d6, l.d7 - r.d7, l.d8 - r.d8);
		}

		public static Matrix3 operator *(Matrix3 l, double c) {
			return new Matrix3(
				c*l.d0, c*l.d1, c*l.d2,
				c*l.d3, c*l.d4, c*l.d5,
				c*l.d6, c*l.d7, c*l.d8);
		}

		public static Matrix3 operator /(Matrix3 l, double c) {
			c = 1/c;
			return new Matrix3(
				c*l.d0, c*l.d1, c*l.d2,
				c*l.d3, c*l.d4, c*l.d5,
				c*l.d6, c*l.d7, c*l.d8);
		}

		#endregion

		#region builders

		public static Matrix3 Eye() {
			return new Matrix3(
				1, 0, 0,
				0, 1, 0,
				0, 0, 1
				);
		}

		public static Matrix3 Translation(double dx, double dy) {
			return new Matrix3(
				1, 0, dx,
				0, 1, dy,
				0, 0, 1
				);
		}

		public static Matrix3 Rotation(double theta) {
			double ct = Math.Cos(theta);
			double st = Math.Sin(theta);
			return new Matrix3(
				ct, -st, 0,
				st, ct, 0,
				0, 0, 1
				);
		}

		#endregion

		#region IPointTransformer Members

		public Coordinates TransformPoint(Coordinates pt) {
			Vector3 v0 = new Vector3(pt.X, pt.Y, 1);
			Vector3 v1 = this*v0;
			return new Coordinates(v1.X, v1.Y);
		}

		public Coordinates[] TransformPoints(Coordinates[] c) {
			if (c == null)
				return null;

			Coordinates[] ret = new Coordinates[c.Length];

			for (int i = 0; i < c.Length; i++) {
				ret[i] = TransformPoint(c[i]);
			}

			return ret;
		}

		public Coordinates[] TransformPoints(ICollection<Coordinates> c) {
			if (c == null)
				return null;

			Coordinates[] ret = new Coordinates[c.Count];
			int i = 0;
			foreach (Coordinates pt in c) {
				ret[i++] = TransformPoint(pt);
			}

			return ret;
		}

		public void TransformPointsInPlace(Coordinates[] c) {
			for (int i = 0; i < c.Length; i++) {
				c[i] = TransformPoint(c[i]);
			}
		}

		public void TransformPointsInPlace(IList<Coordinates> c) {
			for (int i = 0; i < c.Count; i++) {
				c[i] = TransformPoint(c[i]);
			}
		}

		#endregion
	}
}
