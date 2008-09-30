using System;
using System.Collections.Generic;
using System.Text;


namespace UrbanChallenge.Behaviors {
	/// <summary>
	/// This behaviors results in the actuation being disabled indefinitely.
	/// </summary>
	[Serializable]
	public class DisableBehavior : Behavior {
		public override Behavior NextBehavior {
			get { return null; }
		}

		public override bool Equals(object obj) {
			return obj is DisableBehavior;
		}

		public override int GetHashCode() {
			// bullshit random value
			return 0x4E04E5EC;
		}
	}
}
