using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Mapack;

namespace UrbanChallenge.Common.Pose {
	public sealed class RelativeTransform : IPointTransformer {
		private CarTimestamp originTimestamp;
		private CarTimestamp endTimestamp;

		private Matrix4 transform;

		public RelativeTransform(CarTimestamp originTimestamp, CarTimestamp endTimestamp, Matrix4 transform) {
			this.originTimestamp = originTimestamp;
			this.endTimestamp = endTimestamp;
			this.transform = transform;
		}

		public CarTimestamp OriginTimestamp {
			get { return originTimestamp; }
		}

		public CarTimestamp EndTimestamp {
			get { return endTimestamp; }
		}

		public Matrix4 Transform {
			get { return transform; }
		}

		public Vector3 GetRotationRate() {
			double sindy = transform[2, 0];
			double cosdy = Math.Sqrt(transform[2, 1]*transform[2, 1] + transform[2, 2]*transform[2, 2]);
			double sindx = -transform[2, 1]/cosdy;
			double cosdx = transform[2, 2]/cosdy;
			double sindz = -transform[1, 0]/cosdy;
			double cosdz = transform[0, 0]/cosdy;

			double dt = endTimestamp.ts - originTimestamp.ts;

			return new Vector3(Math.Atan2(sindx, cosdx)/dt, Math.Atan2(sindy, cosdy)/dt, Math.Atan2(sindz, cosdz)/dt);
		}

		public Coordinates TransformPoint(Coordinates c0) {
			Vector4 v = new Vector4(c0.X, c0.Y, 0, 1);
			v = transform*v;
			return new Coordinates(v.X, v.Y);
		}

		public Coordinates[] TransformPoints(Coordinates[] c0) {
			if (c0 == null) {
				return null;
			}

			Coordinates[] ret = new Coordinates[c0.Length];

			Vector4 v = new Vector4();
			for (int i = 0; i < ret.Length; i++) {
				v.X = c0[i].X;
				v.Y = c0[i].Y;
				v.Z = 0;
				v.W = 1;
				v = transform*v;
				ret[i] = new Coordinates(v.X, v.Y);
			}

			return ret;
		}

		public Coordinates[] TransformPoints(ICollection<Coordinates> c0) {
			if (c0 == null) {
				return null;
			}

			Coordinates[] ret = new Coordinates[c0.Count];

			int i = 0; 
			Vector4 v = new Vector4();
			foreach (Coordinates c in c0) {
				v.X = c.X;
				v.Y = c.Y;
				v.Z = 0;
				v.W = 1;
				v = transform*v;
				ret[i++] = new Coordinates(v.X, v.Y);
			}

			return ret;
		}

		public void TransformPointsInPlace(IList<Coordinates> c) {
			// just return if c is null
			if (c == null)
				return;

			for (int i = 0; i < c.Count; i++) {
				c[i] = TransformPoint(c[i]);
			}
		}

		public void TransformPointsInPlace(Coordinates[] c) {
			if (c == null)
				return;

			for (int i = 0; i < c.Length; i++) {
				c[i] = TransformPoint(c[i]);
			}
		}

		public Vector3 TransformPoint(Vector3 v0) {
			Vector4 v = new Vector4(v0.X, v0.Y, v0.Z, 1);
			v = transform*v;
			return new Vector3(v.X, v.Y, v.Z);
		}

		public Vector3[] TransformPoints(Vector3[] v0) {
			if (v0 == null)
				return null;

			Vector3[] ret = new Vector3[v0.Length];

			for (int i = 0; i < v0.Length; i++) {
				ret[i] = TransformPoint(v0[i]);
			}

			return ret;
		}

		public List<Vector3> TransformPoints(ICollection<Vector3> v0) {
			if (v0 == null)
				return null;

			List<Vector3> ret = new List<Vector3>();
			ret.Capacity = v0.Count;

			foreach (Vector3 c in v0) {
				ret.Add(TransformPoint(c));
			}

			return ret;
		}

		public void TransformPointsInPlace(IList<Vector3> c) {
			// just return if c is null
			if (c == null)
				return;

			for (int i = 0; i < c.Count; i++) {
				c[i] = TransformPoint(c[i]);
			}
		}
	}
}
