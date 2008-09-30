using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.Sensors {
	[Flags]
	public enum ObstacleClass {
		StaticSmall=0,
		StaticLarge=1,
		DynamicUnknown=2,
		DynamicNotCarlike=3,
		DynamicStopped=4,
		DynamicCarlike=5
	}
}
