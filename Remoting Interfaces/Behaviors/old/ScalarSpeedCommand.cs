using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace UrbanChallenge.Behaviors {
	/// <summary>
	/// Specifies a single speed to reach by the end of the next period
	/// </summary>
	[Serializable]
	public class ScalarSpeedCommand : SpeedCommand {
		/// <summary>
		/// Commanded speed in m/s
		/// </summary>
		private double speed;

		/// <summary>
		/// Constructs the speed command with the specified value
		/// </summary>
		/// <param name="speed">Commanded speed to reach by the next planning period in m/s. Must be non-negative.</param>
		public ScalarSpeedCommand(double speed) {
			// check that speed is valid
			if (speed < 0)
				throw new ArgumentOutOfRangeException("speed", "speed must be non-negative");

			this.speed = speed;
		}

		/// <summary>
		/// Commanded speed in m/s
		/// </summary>
		public double Speed {
			get { return speed; }
		}

		public override bool Equals(object obj) {
			if (obj is ScalarSpeedCommand) {
				return ((ScalarSpeedCommand)obj).speed == speed;
			}
			else {
				return false;
			}
		}

		public override int GetHashCode() {
			return speed.GetHashCode();
		}

		public override string ToString() {
			return "SpeedCommand: Scalar (" + speed.ToString("F1") + ")";
		}
	}
}
