using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Behaviors {
	/// <summary>
	/// Represents an empty behavior. Just used as a placeholder during initialization.
	/// </summary>
	[Serializable]
	public sealed class NullBehavior : Behavior {
		public override bool Equals(object obj) {
			return obj is NullBehavior;
		}

		public override int GetHashCode() {
			return 0x0617CC75;
		}

		public override string ToString() {
			return "Behavior: Null";
		}
	}
}
