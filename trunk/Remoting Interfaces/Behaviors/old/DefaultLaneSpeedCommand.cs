using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Behaviors {
	[Serializable]
	public class DefaultLaneSpeedCommand : LaneSpeedCommand {
		private double maxSpeed;
		private double minSpeed;

		public DefaultLaneSpeedCommand(double maxSpeed, double minSpeed) {
			this.maxSpeed = maxSpeed;
			this.minSpeed = minSpeed;
		}

		public override double MaxSpeed
		{
			get { return maxSpeed; }
		}

		public double MinSpeed {
			get { return minSpeed; }
		}

		public override bool Equals(object obj)
		{
			if (obj is DefaultLaneSpeedCommand)
			{
				DefaultLaneSpeedCommand dlsc = (DefaultLaneSpeedCommand)obj;
				return maxSpeed == dlsc.maxSpeed && minSpeed == dlsc.minSpeed;
			}
			else
			{
				return false;
			}
		}

		public override int GetHashCode()
		{
			return maxSpeed.GetHashCode() ^ minSpeed.GetHashCode();
		}

		public override double MinSpacing
		{
			get { return 5; }
		}
	}
}
