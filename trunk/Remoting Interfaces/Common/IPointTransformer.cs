using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common {
	public interface IPointTransformer {
		Coordinates TransformPoint(Coordinates c);
		Coordinates[] TransformPoints(Coordinates[] c);
		Coordinates[] TransformPoints(ICollection<Coordinates> c);

		void TransformPointsInPlace(Coordinates[] c);
		void TransformPointsInPlace(IList<Coordinates> c);
	}
}
