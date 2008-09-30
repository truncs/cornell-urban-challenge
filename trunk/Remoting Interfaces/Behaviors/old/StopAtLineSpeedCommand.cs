using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Behaviors {
	/// <summary>
	/// Specifies that the vehicle should stop at the nearest stop line
	/// </summary>
	/// <remarks>
	/// <para>The vehicle should already be going at the "search speed" (approx 5 mph) and should
	/// be approx 5-10 m away from the expected location of the stop line when this command 
	/// is executed.</para>
	/// <para>This command has no Parameters. It simply uses the distance to the nearest stop line
	/// as reported by the scene estimator. It could be augmented with a lane provided the scene esimator
	/// reports the distance to a stop line by lane.</para>
	/// </remarks>
	[Serializable]
	public class StopAtLineSpeedCommand : SpeedCommand {
		/// <summary>
		/// Constructs the stop at line speed command.
		/// </summary>
		public StopAtLineSpeedCommand() {
		}

		public override bool Equals(object obj) {
			return obj is StopAtLineSpeedCommand;
		}

		public override int GetHashCode() {
			// random hash code
			return 0x69ABEF15;
		}
	}
}
