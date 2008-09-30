using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.Shapes {
	[Serializable]
	public class LineList : List<Coordinates> {

		public LineList() {
		}

		public LineList(IEnumerable<Coordinates> points)
			: base(points) {
		}

		public LineList(int capacity)
			: base(capacity) {
		}

		public LineList Transform(IPointTransformer transformer) {
			LineList ret = new LineList();
			ret.Capacity = this.Count;

			ret.AddRange(transformer.TransformPoints(this));

			return ret;
		}
	}
}
