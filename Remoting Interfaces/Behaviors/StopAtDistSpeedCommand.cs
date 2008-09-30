using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using UrbanChallenge.Common;

namespace UrbanChallenge.Behaviors {
	/// <summary>
	/// Specifies that the vehicle should stop in the specified distance
	/// </summary>
	/// <remarks>
	/// Note that the vehicle already should be going fairly slowly (i.e. less than 5 mph) when this command is invoked
	/// and the distance should be relatively small (i.e. less than 10 m)
	/// </remarks>
	[Serializable]
	public class StopAtDistSpeedCommand : SpeedCommand {
		private double dist;
		private bool reverse;

		/// <summary>
		/// Constructs the stop command with the specified distance
		/// </summary>
		/// <param name="dist">Distance to stop at, in meters. Must be positive.</param>
		/// <param name="distTimestamp">Timestamp when the distance was calculated. This should be 
		/// the timestamp of the posterior pose packet used.</param>
		public StopAtDistSpeedCommand(double dist) {
			// check that dist is valid
			//if (dist < 0) // Hik: allowed dist = 0 to pass through (3 June) : originally dist <= 0
				//throw new ArgumentOutOfRangeException("dist", "Stop distance must be positive");

			this.dist = dist;
			this.reverse = false;
		}

		public StopAtDistSpeedCommand(double dist, bool reverse) {
			this.dist = dist;
			this.reverse = reverse;
		}

		/// <summary>
		/// Distance to stop at, in m
		/// </summary>
		public double Distance {
			get { return dist; }
		}

		public bool Reverse {
			get { return reverse; }
		}

		public override bool Equals(object obj) {
			if (obj is StopAtDistSpeedCommand) {
				return ((StopAtDistSpeedCommand)obj).dist == dist;
			}
			else {
				return false;
			}
		}

		public override int GetHashCode() {
			return dist.GetHashCode();
		}

		public override string ToString() {
			string neg = this.reverse ? "-" : "";
			return string.Format("stop at dist: " + neg + "({0:F2})", dist);
		}
	}
}
