using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Behaviors {
	[Serializable]
	public abstract class LaneSpeedCommand : SpeedCommand {
		public abstract double MinSpacing { get; }
		public abstract double MaxSpeed { get; }
	}
}
