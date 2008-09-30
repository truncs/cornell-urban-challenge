using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace UrbanChallenge.Common.Utility {
	public static class HighResDateTime {
		private static DateTime startLocal;
		private static DateTime startUTC;
		private static Stopwatch stopwatch;

		static HighResDateTime() {
			stopwatch = new Stopwatch();
			startLocal = DateTime.Now;
			stopwatch.Start();

			startUTC = startLocal.ToUniversalTime();
		}

		public static DateTime Now {
			get {
				return startLocal + stopwatch.Elapsed;
			}
		}

		public static DateTime UtcNow {
			get {
				return startUTC + stopwatch.Elapsed;
			}
		}

		/// <summary>
		/// Returns true if the high performance counter is in use
		/// </summary>
		public static bool IsHighResolution {
			get { return Stopwatch.IsHighResolution; }
		}

		/// <summary>
		/// Returns the number of ticks per second
		/// </summary>
		public static long Frequency {
			get { return Stopwatch.Frequency; }
		}
	}
}
