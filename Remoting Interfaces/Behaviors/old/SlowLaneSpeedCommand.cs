using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Behaviors
{
	[Serializable]
	public class SlowLaneSpeedCommand : LaneSpeedCommand
	{
		protected double distance;
		protected double maxSpeed;
		private double finalSpeed;		

		public SlowLaneSpeedCommand(double distance, double maxSpeed, double finalSpeed)
		{
			this.distance = distance;
			this.maxSpeed = maxSpeed;
			this.finalSpeed = finalSpeed;
		}

		public double Distance
		{
			get { return distance; }
		}

		public override double MaxSpeed
		{
			get { return maxSpeed; }
		}

		protected double FinalSpeed
		{
			get { return finalSpeed; }
		}

		public override bool Equals(object obj)
		{
			if (obj is SlowLaneSpeedCommand)
			{
				SlowLaneSpeedCommand b = (SlowLaneSpeedCommand)obj;
				return distance == b.distance && maxSpeed == b.maxSpeed && finalSpeed == b.finalSpeed;
			}
			else
			{
				return false;
			}
		}

		public override int GetHashCode()
		{
			return distance.GetHashCode() ^ maxSpeed.GetHashCode() ^ finalSpeed.GetHashCode();
		}

		public override double MinSpacing
		{
			get { return 0; }
		}
	}
}
