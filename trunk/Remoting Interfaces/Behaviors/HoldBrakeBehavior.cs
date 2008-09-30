using System;
using System.Collections.Generic;
using System.Text;


namespace UrbanChallenge.Behaviors {
	[Serializable]
	public class HoldBrakeBehavior : Behavior {
		public override Behavior NextBehavior {
			get {
				return null;
			}
		}

		public override bool Equals(object obj) {
			return obj is HoldBrakeBehavior;
		}

		public override int GetHashCode() {
			// random hash code
			return 0x22BC14BE;
		}

		public override string ToString() {
			return "HoldBrakeBehavior";
		}

		public override string ToShortString()
		{
			return ("HoldBrakeBehavior");
		}

		public override string ShortBehaviorInformation()
		{
			return ("");
		}

		public override string SpeedCommandString()
		{
			return "";
		}
	}
}
