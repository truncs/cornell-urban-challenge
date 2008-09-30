using System;
using System.Collections.Generic;
using System.Text;


namespace UrbanChallenge.Behaviors {
	[Serializable]
	public class PauseBehavior : Behavior {
		public override Behavior NextBehavior {
			get { return null; }
		}

		public override bool Equals(object obj) {
			return obj is PauseBehavior;
		}

		public override int GetHashCode() {
			return 0x23AB60C5;
		}
	}
}
