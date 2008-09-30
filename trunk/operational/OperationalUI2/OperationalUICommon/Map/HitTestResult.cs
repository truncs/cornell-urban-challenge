using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;

namespace UrbanChallenge.OperationalUI.Common.Map {
	public class HitTestResult {
		public static readonly HitTestResult NoHit = new HitTestResult(null, false);

		private IHittable target;
		private bool hit;
		private Coordinates snapPoint;
		private object data;

		public HitTestResult(IHittable target, bool hit) : this(target, hit, Coordinates.NaN, null) { }

		public HitTestResult(IHittable target, bool hit, object data) : this(target, hit, Coordinates.NaN, data) { }

		public HitTestResult(IHittable target, bool hit, Coordinates snapPoint, object data) {
			this.target = target;
			this.hit = hit;
			this.snapPoint = snapPoint;
			this.data = data;
		}

		public IHittable Target {
			get { return target; }
		}

		public bool Hit {
			get { return hit; }
		}

		public object Data {
			get { return data; }
		}

		public Coordinates SnapPoint {
			get { return snapPoint; }
		}
	}
}
