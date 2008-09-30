using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.RndfNetwork;
using UrbanChallenge.Behaviors;

namespace UrbanChallenge.Behaviors {
	[Serializable]
	public class StayInLaneBehavior : Behavior {
		private LaneID lane;
		private LaneSpeedCommand speedCommand;

		public StayInLaneBehavior(LaneID lane, LaneSpeedCommand speedCommand) {
			this.lane = lane;
			this.speedCommand = speedCommand;
		}

		public LaneID Lane {
			get { return lane; }
		}

		public LaneSpeedCommand SpeedCommand {
			get { return speedCommand; }
		}

		public override bool Equals(object obj)
		{
			if (obj is StayInLaneBehavior)
			{
				StayInLaneBehavior b = (StayInLaneBehavior)obj;
				return lane.Equals(b.lane) && speedCommand.Equals(b.speedCommand);
			}
			else
			{
				return false;
			}
		}

		public override int GetHashCode()
		{
			return lane.GetHashCode() ^ speedCommand.GetHashCode();
		}

		public override string ToString()
		{
			if (this.SpeedCommand is StopLineLaneSpeedCommand)
			{
				return "StopAtStopLine";
			}
			else if (this.SpeedCommand is StopLaneSpeedCommand)
			{
				return "StopInLane";
			}
			else
			{
				return "StayInLane";
			}
		}
	}
}
