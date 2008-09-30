using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Behaviors {
	[Serializable]
	public class StopLaneSpeedCommand : LaneSpeedCommand {
		protected double distance;
		protected double maxSpeed;

		public StopLaneSpeedCommand(double distance, double maxSpeed) {
			this.distance = distance;
			this.maxSpeed = maxSpeed;
		}

		public double Distance {
			get { return distance; }
		}

		public override double MaxSpeed
		{
			get { return maxSpeed; }
		}

		public override bool Equals(object obj)
		{
			if (obj is StopLaneSpeedCommand)
			{
				StopLaneSpeedCommand b = (StopLaneSpeedCommand)obj;
				return distance == b.distance && maxSpeed == b.maxSpeed;
			}
			else
			{
				return false;
			}
		}

		public override int GetHashCode()
		{
			return distance.GetHashCode() ^ maxSpeed.GetHashCode();
		}

		public override double MinSpacing
		{
			get { return 0; }
		}
	}
}
