using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Mapack;

namespace OperationalLayer.Pose {
	class AbsoluteTransformer : IPointTransformer {
		private Matrix3 transform;
		private CarTimestamp timestamp;

		public AbsoluteTransformer(Coordinates loc, double heading, CarTimestamp timestamp) {
			transform = Matrix3.Rotation(-heading)*Matrix3.Translation(-loc.X, -loc.Y);
			this.timestamp = timestamp;
		}

		public AbsoluteTransformer(Matrix3 transform, CarTimestamp timestamp) {
			this.transform = transform;
			this.timestamp = timestamp;
		}

		public AbsoluteTransformer(AbsolutePose pose) : this(pose.xy, pose.heading, pose.timestamp) {
		}

		public AbsoluteTransformer Invert() {
			return new AbsoluteTransformer(transform.Inverse(), timestamp);
		}

		public CarTimestamp Timestamp {
			get { return timestamp; }
		}

		#region IPointTransformer Members

		public Coordinates TransformPoint(Coordinates c) {
			Vector3 v = transform*(new Vector3(c.X, c.Y, 1));
			return new Coordinates(v.X, v.Y);
		}

		public Coordinates[] TransformPoints(Coordinates[] c) {
			Coordinates[] ret = new Coordinates[c.Length];
			for (int i = 0; i < c.Length; i++) {
				ret[i] = TransformPoint(c[i]);
			}

			return ret;
		}

		public Coordinates[] TransformPoints(ICollection<Coordinates> c) {
			Coordinates[] ret = new Coordinates[c.Count];
			int i = 0;
			foreach (Coordinates pt in c) {
				ret[i++] = TransformPoint(pt);
			}

			return ret;
		}

		#endregion

		#region IPointTransformer Members


		public void TransformPointsInPlace(Coordinates[] c) {
			if (c == null)
				return;

			for (int i = 0; i < c.Length; i++) {
				Vector3 v = transform*(new Vector3(c[i].X, c[i].Y, 1));
				c[i] = new Coordinates(v.X, v.Y);
			}
		}

		public void TransformPointsInPlace(IList<Coordinates> c) {
			if (c == null)
				return;

			for (int i = 0; i < c.Count; i++) {
				Vector3 v = transform*(new Vector3(c[i].X, c[i].Y, 1));
				c[i] = new Coordinates(v.X, v.Y);
			}
		}

		#endregion
	}
}
