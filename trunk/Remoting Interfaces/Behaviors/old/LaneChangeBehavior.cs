using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.RndfNetwork;
using UrbanChallenge.Behaviors;

namespace UrbanChallenge.Behaviors {
	[Serializable]
	public class LaneChangeBehavior : Behavior {
		private LaneID startLane;
		private LaneID endLane;
		private double distance;
		private LaneSpeedCommand speedCommand;

		public LaneChangeBehavior(LaneID startLane, LaneID endLane, double dist, LaneSpeedCommand speedCommand) {
			this.startLane = startLane;
			this.endLane = endLane;
			this.distance = dist;
			this.speedCommand = speedCommand;
		}

		public double Distance
		{
			get { return distance; }
		}

		public LaneID StartLane {
			get { return startLane; }
		}

		public LaneID EndLane {
			get { return endLane; }
		}

		public LaneSpeedCommand SpeedCommand {
			get { return speedCommand; }
		}

		public override Behavior NextBehavior {
			get {
				return new StayInLaneBehavior(endLane, speedCommand);
			}
		}

		public override bool Equals(object obj)
		{
			if (obj is LaneChangeBehavior)
			{
				LaneChangeBehavior lcb = (LaneChangeBehavior)obj;
				return startLane.Equals(lcb.startLane) && endLane.Equals(lcb.endLane) && speedCommand.Equals(lcb.speedCommand);
			}
			else
			{
				return false;
			}
		}

		public override int GetHashCode()
		{
			return startLane.GetHashCode() ^ endLane.GetHashCode() ^ speedCommand.GetHashCode();
		}
	}
}
