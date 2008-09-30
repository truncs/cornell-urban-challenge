using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.Path;

namespace UrbanChallenge.OperationalUIService.Behaviors {
	[Serializable]
	public class SimpleStayInLaneBehavior : Behavior {
		private Path basePath;
		private double laneWidth;
		private double maxSpeed;

		public SimpleStayInLaneBehavior(Path basePath, double laneWidth, double maxSpeed) {
			this.basePath = basePath;
			this.laneWidth = laneWidth;
			this.maxSpeed = maxSpeed;
		}

		public Path BasePath {
			get { return basePath; }
		}

		public double LaneWidth {
			get { return laneWidth; }
		}

		public double MaxSpeed {
			get { return maxSpeed; }
		}

		public override string ToShortString() {
			return "";
		}

		public override string ShortBehaviorInformation() {
			return "";
		}

		public override string SpeedCommandString() {
			return "";
		}
	}
}
