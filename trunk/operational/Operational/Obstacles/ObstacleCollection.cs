using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;

namespace OperationalLayer.Obstacles {
	class ObstacleCollection {
		public CarTimestamp timestamp;
		public List<Obstacle> obstacles;

		public ObstacleCollection(CarTimestamp timestamp, List<Obstacle> obstacles) {
			this.timestamp = timestamp;
			this.obstacles = obstacles;
		}
	}
}
