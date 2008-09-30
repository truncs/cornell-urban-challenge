using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.RndfNetwork;
using UrbanChallenge.Behaviors;

namespace UrbanChallenge.Behaviors {
	[Serializable]
	public class TurnBehavior : Behavior {
		private RndfWaypointID exitPoint;
		private RndfWaypointID entryPoint;
		private double maxSpeed;

		public TurnBehavior(RndfWaypointID exitPoint, RndfWaypointID entryPoint, double maxSpeed)
		{
			this.exitPoint = exitPoint;
			this.entryPoint = entryPoint;
			this.maxSpeed = maxSpeed;
		}

		public RndfWaypointID ExitPoint
		{
			get { return exitPoint; }
		}

		public RndfWaypointID EntryPoint
		{
			get { return entryPoint; }
		}

		public double MaxSpeed {
			get { return maxSpeed; }
		}

		public override Behavior NextBehavior {
			get {
				return new StayInLaneBehavior(entryPoint.LaneID, new DefaultLaneSpeedCommand(maxSpeed, 0));
			}
		}

		public override bool Equals(object obj)
		{
			if (obj is TurnBehavior)
			{
				TurnBehavior b = (TurnBehavior)obj;
				return exitPoint.Equals(b.exitPoint) && entryPoint.Equals(b.entryPoint) && maxSpeed == b.maxSpeed;
			}
			else
			{
				return false;
			}
		}

		public override int GetHashCode()
		{
			return exitPoint.GetHashCode() ^ entryPoint.GetHashCode() ^ maxSpeed.GetHashCode();
		}

		public override string ToString()
		{
			return "TurnBehavior";
		}
	}
}
