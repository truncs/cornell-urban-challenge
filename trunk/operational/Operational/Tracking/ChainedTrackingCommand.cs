using System;
using System.Collections.Generic;
using System.Text;

namespace OperationalLayer.Tracking {
	class ChainedTrackingCommand : ITrackingCommand {
		private ITrackingCommand first;
		private ITrackingCommand second;
		private string label;
		private bool firstCompleted;

		public ChainedTrackingCommand(ITrackingCommand first, ITrackingCommand second) {
			this.first = first;
			this.second = second;
			this.label = "<default>";
			firstCompleted = false;
		}

		#region ITrackingCommand Members

		public TrackingData Process() {
			if (!firstCompleted) {
				TrackingData td = first.Process();
				if (td.result == CompletionResult.Completed) {
					firstCompleted = true;
				}
				else {
					return td;
				}
			}

			return second.Process();
		}

		public string Label {
			get { return label; }
			set { label = value; }
		}

		#endregion
	}
}
