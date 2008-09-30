using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common
{
	[Serializable]
	public struct Vector3
	{
		public double X, Y, Z;

		public Vector3(double x, double y, double z)
		{
			this.X = x;
			this.Y = y;
			this.Z = z;
		}

		public Vector3 Cross(Vector3 v)
		{
			return new Vector3(
				Y * v.Z - Z * v.Y,
				Z * v.X - X * v.Z,
				X * v.Y - Y * v.X);
		}

		public double Dot(Vector3 v)
		{
			return X * v.X + Y * v.Y + Z * v.Z;
		}

		public double Length
		{
			get { return Math.Sqrt(LengthSq); }
		}

		public double LengthSq
		{
			get { return Dot(this); }
		}

		public Vector3 Normalize()
		{
			return Normalize(1);
		}

		public Vector3 Normalize(double l)
		{
			return this * (l / Length);
		}

		public static Vector3 operator /(Vector3 v, double d)
		{
			return new Vector3(v.X / d, v.Y / d, v.Z / d);
		}

		public static Vector3 operator *(Vector3 v, double d)
		{
			return new Vector3(v.X * d, v.Y * d, v.Z * d);
		}

		public static Vector3 operator *(double d, Vector3 v)
		{
			return v * d;
		}

		public static bool operator ==(Vector3 l, Vector3 r)
		{
			return l.X == r.X && l.Y == r.Y && l.Z == r.Z;
		}

		public static bool operator !=(Vector3 l, Vector3 r)
		{
			return l.X != r.X || l.Y != r.Y || l.Z != r.Z;
		}

		public static Vector3 operator +(Vector3 l, Vector3 r) {
			return new Vector3(l.X + r.X, l.Y + r.Y, l.Z + r.Z);
		}

		public static Vector3 operator -(Vector3 l, Vector3 r) {
			return new Vector3(l.X - r.X, l.Y - r.Y, l.Z - r.Z);
		}

		public override bool Equals(object obj)
		{
			if (obj is Vector3)
			{
				return (Vector3)obj == this;
			}
			else
			{
				return false;
			}
		}

		public override int GetHashCode()
		{
			return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
		}

		
	}
}
