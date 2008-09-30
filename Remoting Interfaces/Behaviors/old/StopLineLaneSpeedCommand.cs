using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.RndfNetwork;

namespace UrbanChallenge.Behaviors {
	[Serializable]
	public class StopLineLaneSpeedCommand : StopLaneSpeedCommand {
		private double searchSpeed;
		private RndfWaypointID waypointID;

		public StopLineLaneSpeedCommand(double expectedDistance, double maxSpeed, double searchSpeed, RndfWaypointID waypointID)
			: base(expectedDistance, maxSpeed) {
			this.searchSpeed = searchSpeed;
			this.waypointID = waypointID;
		}

		public RndfWaypointID WaypointID
		{
			get { return waypointID; }
		}

		public double SearchSpeed {
			get { return searchSpeed; }
		}

		public override bool Equals(object obj)
		{
			if (obj is StopLineLaneSpeedCommand)
			{
				StopLineLaneSpeedCommand b = (StopLineLaneSpeedCommand)obj;
				return base.Equals(obj) && searchSpeed == b.searchSpeed;
			}
			else
			{
				return false;
			}
		}

		public override int GetHashCode()
		{
			return base.GetHashCode() ^ searchSpeed.GetHashCode();
		}
	}
}
