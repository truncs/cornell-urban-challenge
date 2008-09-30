using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Shapes;

namespace UrbanChallenge.Common.Sensors {
	[Serializable]
	public class OperationalObstacle {
		public int age;
		public bool headingValid;
		public double heading;
		public bool ignored;
		public ObstacleClass obstacleClass;
		public Polygon poly;

		public OperationalObstacle ShallowClone() {
			return (OperationalObstacle)this.MemberwiseClone();
		}
	}
}
