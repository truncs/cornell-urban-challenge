using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Utility;

namespace OperationalLayer.CarTime {
	class LocalCarTimeProvider : ICarTimeProvider {
		private static DateTime start = HighResDateTime.Now;

		public LocalCarTimeProvider() {
		}

		#region ICarTimeProvider Members

		public CarTimestamp Now {
			get {
				return Services.RelativePose.CurrentTimestamp;
			}
		}

		#endregion

		public static CarTimestamp LocalNow {
			get {
				TimeSpan elapsed = HighResDateTime.Now - start;
				return new CarTimestamp(elapsed.TotalSeconds);
			}
		}
	}
}
