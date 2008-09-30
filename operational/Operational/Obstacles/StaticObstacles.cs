using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common;

namespace OperationalLayer.Obstacles {
	class StaticObstacles {
		public List<Polygon> polygons;
		public CarTimestamp timestamp;

		public StaticObstacles(List<Polygon> polygons, CarTimestamp timestamp) {
			this.polygons = polygons;
			this.timestamp = timestamp;
		}
	}
}
