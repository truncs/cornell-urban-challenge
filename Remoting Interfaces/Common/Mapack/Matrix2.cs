using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace UrbanChallenge.Common.Mapack {
	[Serializable]
	public struct Matrix2 {
		private double d11, d12;
		private double d21, d22;

		public Matrix2(double d11, double d12, double d21, double d22) {
			this.d11 = d11;
			this.d12 = d12;
			this.d21 = d21;
			this.d22 = d22;
		}

		public double this[int i, int j] {
			get {
				Debug.Assert(i >= 0 && i <= 1 && j >= 0 && j <= 1);
				if (i == 0) {
					if (j == 0) {
						return d11;
					}
					else {
						return d12;
					}
				}
				else {
					if (j == 0) {
						return d21;
					}
					else {
						return d22;
					}
				}
			}
			set {
				Debug.Assert(i >= 0 && i <= 1 && j >= 0 && j <= 1);
				if (i == 0) {
					if (j == 0) {
						d11 = value;
					}
					else {
						d12 = value;
					}
				}
				else {
					if (j == 0) {
						d21 = value;
					}
					else {
						d22 = value;
					}
				}
			}
		}

		public double Determinant() {
			return d11 * d22 - d12 * d21;
		}

		public Matrix2 Inverse() {
			double det = d11 * d22 - d12 * d21;
			return new Matrix2(d22 / det, -d12 / det, -d21 / det, d11 / det);
		}

		public Matrix2 Transpose() {
			return new Matrix2(d11, d21, d12, d22);
		}

		public void SymmetricEigenDecomposition(out double[] eigenvalues, out Coordinates[] eigenvectors){
			double t = Math.Sqrt(4*d12*d12 + (d11-d22)*(d11-d22));
			double lamda1 = (d11+d22+t)/2;
			double lamda2 = (d11+d22-t)/2;

			Coordinates v1 = new Coordinates(1, (d22-d11+t)/(2*d12));
			Coordinates v2 = new Coordinates(1, (d22-d11-t)/(2*d12));

			eigenvalues = new double[] { lamda1, lamda2 };
			eigenvectors = new Coordinates[] { v1.Normalize(), v2.Normalize() };
		}

		public override string ToString() {
			return ToString("F4", 8);
		}

		public string ToString(string format, int alignment) {
			string formatString = "{0," + alignment + ":" + format + "}";
			StringBuilder sb = new StringBuilder();
			sb.Append("[");
			sb.AppendFormat(formatString, d11);
			sb.Append(", ");
			sb.AppendFormat(formatString, d12);
			sb.AppendLine("]");
			sb.Append("[");
			sb.AppendFormat(formatString, d21);
			sb.Append(", ");
			sb.AppendFormat(formatString, d22);
			sb.Append("]");

			return sb.ToString();
		}

		#region operators

		public static Matrix2 operator *(Matrix2 l, Matrix2 r) {
			return new Matrix2(
				l.d11 * r.d11 + l.d12 * r.d21, l.d11 * r.d12 + l.d12 * r.d22,
				l.d21 * r.d11 + l.d22 * r.d21, l.d21 * r.d12 + l.d22 * r.d22);
		}

		public static Coordinates operator *(Matrix2 l, Coordinates r) {
			return new Coordinates(
				l.d11 * r.X + l.d12 * r.Y,
				l.d21 * r.X + l.d22 * r.Y);
		}

		public static Coordinates operator *(Coordinates l, Matrix2 r) {
			return new Coordinates(
				r.d11*l.X + r.d21*l.Y,
				r.d12*l.X + r.d22*l.Y);
		}

		public static Matrix2 operator -(Matrix2 l) {
			return new Matrix2(-l.d11, -l.d12, -l.d21, -l.d22);
		}

		public static Matrix2 operator +(Matrix2 l, Matrix2 r) {
			return new Matrix2(
				l.d11 + r.d11, l.d12 + r.d12,
				l.d21 + r.d21, l.d22 + r.d22);
		}

		public static Matrix2 operator -(Matrix2 l, Matrix2 r) {
			return new Matrix2(
				l.d11 - r.d11, l.d12 - r.d12,
				l.d21 - r.d21, l.d22 - r.d22);
		}

		public static Matrix2 operator *(Matrix2 l, double c) {
			return new Matrix2(
				l.d11 * c, l.d12 * c, 
				l.d21 * c, l.d22 * c);
		}

		public static Matrix2 operator /(Matrix2 l, double c) {
			return new Matrix2(
				l.d11 / c, l.d12 / c,
				l.d21 / c, l.d22 / c);
		}

		#endregion
	}
}