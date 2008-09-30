using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Behaviors {
	[Serializable]
	public class OperationalStartupBehavior : Behavior {
		public override string ToShortString() {
			return "Operational startup dummy behavior";
		}

		public override string ShortBehaviorInformation() {
			return string.Empty;
		}

		public override string SpeedCommandString() {
			return string.Empty;
		}
	}
}
