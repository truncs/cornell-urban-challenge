using System;
using System.Collections.Generic;
using System.Text;

namespace OperationalLayer.Tracking {
	class NullTrackingCommand : ITrackingCommand {
		string label;

		public NullTrackingCommand() {
			label = "nullcommand";
		}

		public NullTrackingCommand(string label) {
			this.label = label;
		}

		#region ITrackingCommand Members

		public TrackingData Process() {
			return new TrackingData(null, null, null, null, CompletionResult.Working);
		}

		public string Label { get { return label; } }

		#endregion
	}
}
