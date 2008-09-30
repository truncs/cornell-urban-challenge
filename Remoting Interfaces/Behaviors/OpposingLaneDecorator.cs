using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Behaviors {
	[Serializable]
	public class OpposingLaneDecorator : BehaviorDecorator {
		private double distance;
		private double speed;

		public OpposingLaneDecorator(double distance, double speed) {
			this.distance = distance;
			this.speed = speed;
		}

		public double Distance {
			get { return distance; }
		}

		public double Speed {
			get { return speed; }
		}
	}
}
