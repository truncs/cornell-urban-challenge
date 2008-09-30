using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace UrbanChallenge.Common.Mapack {
	[Serializable]
	public struct Matrix4 {
		private double d0, d1, d2, d3;
		private double d4, d5, d6, d7;
		private double d8, d9, d10, d11;
		private double d12, d13, d14, d15;

		public Matrix4(
			double m11, double m12, double m13, double m14,
			double m21, double m22, double m23, double m24,
			double m31, double m32, double m33, double m34,
			double m41, double m42, double m43, double m44) {

			d0 = m11; d1 = m12; d2 = m13; d3 = m14;
			d4 = m21; d5 = m22; d6 = m23; d7 = m24;
			d8 = m31; d9 = m32; d10= m33; d11= m34;
			d12= m41; d13= m42; d14= m43; d15= m44;
		}

		public static Matrix4 FromSubmatrix(
			Matrix2 A, Matrix2 B,
			Matrix2 C, Matrix2 D) {

			Matrix4 ret = new Matrix4();

			for (int i = 0; i < 2; i++) {
				for (int j = 0; j < 2; j++) {
					ret[i, j] = A[i, j];
					ret[i+2, j] = C[i, j];
					ret[i, j+2] = B[i, j];
					ret[i+2, j+2] = D[i, j];
				}
			}

			return ret;
		}

		public static Matrix4 Eye() {
			return new Matrix4(
				1, 0, 0, 0,
				0, 1, 0, 0,
				0, 0, 1, 0,
				0, 0, 0, 1
				);
		}

		public static Matrix4 Translation(double dx, double dy, double dz) {
			return new Matrix4(
				1, 0, 0, dx,
				0, 1, 0, dy,
				0, 0, 1, dz,
				0, 0, 0,  1
				);
		}

		public static Matrix4 YPR(double yaw, double pitch, double roll) {
			double cy = Math.Cos(yaw), sy = Math.Sin(yaw);
			double cp = Math.Cos(pitch), sp = Math.Sin(pitch);
			double cr = Math.Cos(roll), sr = Math.Sin(roll);

			Matrix4 rz = new Matrix4(
				 cy, -sy, 0, 0,
				 sy,  cy, 0, 0,
				  0,   0, 1, 0,
				  0,   0, 0, 1
				);

			Matrix4 ry = new Matrix4(
				 cp, 0, sp, 0,
				  0, 1,  0, 0,
				-sp, 0, cp, 0,
				  0, 0,  0, 1
				);

			Matrix4 rx = new Matrix4(
				1,   0,   0, 0,
				0,  cr, -sr, 0,
				0,  sr,  cr, 0,
				0,   0,   0, 1
				);

			return rx*ry*rz;
		}

		public double this[int i, int j] {
			get {
				Debug.Assert(i >= 0 && i < 4 && j >= 0 && j < 4);
				switch (i) {
					case 0:
						switch (j) {
							case 0: return d0;
							case 1: return d1;
							case 2: return d2;
							case 3: return d3;
						}
						break;

					case 1:
						switch (j) {
							case 0: return d4;
							case 1: return d5;
							case 2: return d6;
							case 3: return d7;
						}
						break;

					case 2:
						switch (j) {
							case 0: return d8;
							case 1: return d9;
							case 2: return d10;
							case 3: return d11;
						}
						break;

					case 3:
						switch (j) {
							case 0: return d12;
							case 1: return d13;
							case 2: return d14;
							case 3: return d15;
						}
						break;
				}

				throw new ArgumentOutOfRangeException();
			}
			set {
				Debug.Assert(i >= 0 && i < 4 && j >= 0 && j < 4);
				switch (i) {
					case 0:
						switch (j) {
							case 0: d0 = value; break;
							case 1: d1 = value; break;
							case 2: d2 = value; break;
							case 3: d3 = value; break;
						}
						break;

					case 1:
						switch (j) {
							case 0: d4 = value; break;
							case 1: d5 = value; break;
							case 2: d6 = value; break;
							case 3: d7 = value; break;
						}
						break;

					case 2:
						switch (j) {
							case 0: d8 = value; break;
							case 1: d9 = value; break;
							case 2: d10 = value; break;
							case 3: d11 = value; break;
						}
						break;

					case 3:
						switch (j) {
							case 0: d12 = value; break;
							case 1: d13 = value; break;
							case 2: d14 = value; break;
							case 3: d15 = value; break;
						}
						break;
				}
			}
		}

		public Matrix2 SubmatrixA() {
			return new Matrix2(d0, d1, d4, d5);
		}

		public Matrix2 SubmatrixB() {
			return new Matrix2(d2, d3, d6, d7);
		}

		public Matrix2 SubmatrixC() {
			return new Matrix2(d8, d9, d12, d13);
		}

		public Matrix2 SubmatrixD() {
			return new Matrix2(d10, d11, d14, d15);
		}

		public Matrix3 SubmatrixRotation() {
			return new Matrix3(
				d0, d1, d2,
				d4, d5, d6,
				d8, d9, d10
				);
		}

		public double Determinant() {
			Matrix2 D = SubmatrixD();
			double detD = D.Determinant();

			Matrix2 invD = D.Inverse();

			Matrix2 iA = (SubmatrixA() - SubmatrixB() * invD * SubmatrixC());
			return detD * iA.Determinant();
		}

		public Matrix4 Inverse() {
			Matrix2 invA = SubmatrixA().Inverse();
			Matrix2 B = SubmatrixB();
			Matrix2 C = SubmatrixC();

			Matrix2 invSchurComp = (SubmatrixD() - C * invA * B).Inverse();

			return Matrix4.FromSubmatrix(
				invA + invA*B*invSchurComp*C*invA, -invA*B*invSchurComp,
				-invSchurComp*C*invA, invSchurComp);
		}

		public override string ToString() {
			return ToString("F4", 8);
		}

		public string ToString(string format, int alignment) {
			string formatString = "{0," + alignment + ":" + format + "}";
			StringBuilder sb = new StringBuilder();

			for (int row = 0; row < 4; row++) {
				sb.Append("[");
				for (int col = 0; col < 4; col++) {
					sb.AppendFormat(formatString, this[row, col]);

					if (col != 3) {
						sb.Append(", ");
					}
				}

				sb.Append("]");

				if (row != 3) {
					sb.AppendLine();
				}
			}

			return sb.ToString();
		}

		#region operators

		public static Matrix4 operator *(Matrix4 l, Matrix4 r) {
			// [      ld0*rd0+ld1*rd4+ld2*rd8+ld3*rd12,      ld0*rd1+ld1*rd5+ld2*rd9+ld3*rd13,     ld0*rd2+ld1*rd6+ld2*rd10+ld3*rd14,     ld0*rd3+ld1*rd7+ld2*rd11+ld3*rd15]
			// [      ld4*rd0+ld5*rd4+ld6*rd8+ld7*rd12,      ld4*rd1+ld5*rd5+ld6*rd9+ld7*rd13,     ld4*rd2+ld5*rd6+ld6*rd10+ld7*rd14,     ld4*rd3+ld5*rd7+ld6*rd11+ld7*rd15]
			// [    ld8*rd0+ld9*rd4+ld10*rd8+ld11*rd12,    ld8*rd1+ld9*rd5+ld10*rd9+ld11*rd13,   ld8*rd2+ld9*rd6+ld10*rd10+ld11*rd14,   ld8*rd3+ld9*rd7+ld10*rd11+ld11*rd15]
			// [  ld12*rd0+ld13*rd4+ld14*rd8+ld15*rd12,  ld12*rd1+ld13*rd5+ld14*rd9+ld15*rd13, ld12*rd2+ld13*rd6+ld14*rd10+ld15*rd14, ld12*rd3+ld13*rd7+ld14*rd11+ld15*rd15]
			Matrix4 ret = new Matrix4();
			for (int i = 0; i < 4; i++) {
				for (int j = 0; j < 4; j++) {
					ret[i, j] = l[i,0]*r[0,j] + l[i,1]*r[1,j] + l[i,2]*r[2,j] + l[i,3]*r[3,j];
				}
			}

			return ret;
		}

		public static Vector4 operator *(Matrix4 l, Vector4 r) {
			return new Vector4(
				l[0,0]*r.X + l[0,1]*r.Y + l[0,2]*r.Z + l[0,3]*r.W,
				l[1,0]*r.X + l[1,1]*r.Y + l[1,2]*r.Z + l[1,3]*r.W,
				l[2,0]*r.X + l[2,1]*r.Y + l[2,2]*r.Z + l[2,3]*r.W,
				l[3,0]*r.X + l[3,1]*r.Y + l[3,2]*r.Z + l[3,3]*r.W
				);
		}

		public static Vector4 operator *(Vector4 l, Matrix4 r) {
			return new Vector4(
				r[0,0]*l.X + r[1,0]*l.Y + r[2,0]*l.Z + r[3,0]*l.W,
				r[0,1]*l.X + r[1,1]*l.Y + r[2,1]*l.Z + r[3,1]*l.W,
				r[0,2]*l.X + r[1,2]*l.Y + r[2,2]*l.Z + r[3,2]*l.W,
				r[0,3]*l.X + r[1,3]*l.Y + r[2,3]*l.Z + r[3,3]*l.W);
		}

		public static Matrix4 operator -(Matrix4 l) {
			return new Matrix4(
				-l.d0, -l.d1, -l.d2, -l.d3,
				-l.d4, -l.d5, -l.d6, -l.d7,
				-l.d8, -l.d9, -l.d10,-l.d11,
				-l.d12,-l.d13,-l.d14,-l.d15);
		}

		public static Matrix4 operator -(Matrix4 l, Matrix4 r) {
			return new Matrix4(
				l.d0-r.d0,l.d1-r.d1,l.d2-r.d2,l.d3-r.d3,
				l.d4-r.d4,l.d5-r.d5,l.d6-r.d6,l.d7-r.d7,
				l.d8-r.d8,l.d9-r.d9,l.d10-r.d10,l.d11-r.d11,
				l.d12-r.d12,l.d13-r.d13,l.d14-r.d14,l.d15-r.d15);
		}

		public static Matrix4 operator +(Matrix4 l, Matrix4 r) {
			return new Matrix4(
				l.d0+r.d0,l.d1+r.d1,l.d2+r.d2,l.d3+r.d3,
				l.d4+r.d4,l.d5+r.d5,l.d6+r.d6,l.d7+r.d7,
				l.d8+r.d8,l.d9+r.d9,l.d10+r.d10,l.d11+r.d11,
				l.d12+r.d12,l.d13+r.d13,l.d14+r.d14,l.d15+r.d15);
		}

		public static Matrix4 operator *(Matrix4 l, double c) {
			return new Matrix4(
				c*l.d0, c*l.d1, c*l.d2, c*l.d3,
				c*l.d4, c*l.d5, c*l.d6, c*l.d7,
				c*l.d8, c*l.d9, c*l.d10, c*l.d11,
				c*l.d12, c*l.d13, c*l.d14, c*l.d15);
		}

		public static Matrix4 operator *(double c, Matrix4 l) {
			return l * c;
		}

		public static Matrix4 operator /(Matrix4 l, double c) {
			return l * (1 / c);
		}

		#endregion
	}
}
