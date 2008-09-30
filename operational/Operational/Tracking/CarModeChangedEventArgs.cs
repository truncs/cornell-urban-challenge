using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;

namespace OperationalLayer.Tracking {
	class CarModeChangedEventArgs : EventArgs {
		private CarTimestamp timestamp;
		private CarMode mode;

		public CarModeChangedEventArgs(CarMode mode, CarTimestamp ct) {
			this.mode = mode;
			this.timestamp = ct;
		}

		public CarTimestamp Timestamp {
			get { return timestamp; }
		}

		public CarMode Mode {
			get { return mode; }
		}
	}
}
