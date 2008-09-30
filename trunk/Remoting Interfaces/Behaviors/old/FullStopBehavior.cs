using System;
using System.Collections.Generic;
using System.Text;


namespace UrbanChallenge.Behaviors {
	/// <summary>
	/// Specifies that the vehicle should come to a stop ASAP. 
	/// </summary>
	/// <remarks>
	/// Only use for an error state! This will be implemented such that the 
	/// driver knows that this shouldn't really happen.
	/// </remarks>
	[Serializable]
	public class FullStopBehavior : Behavior {
		public override Behavior NextBehavior {
			get {
				// this behavior never completes
				return null;
			}
		}

		public override bool Equals(object obj) {
			return obj is FullStopBehavior;
		}

		public override int GetHashCode() {
			return 0x42E662B9;
		}

		public override string ToString() {
			return "Behavior: FullStop";
		}
	}
}
