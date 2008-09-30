using System;
using System.Collections.Generic;
using System.Text;

namespace OperationalLayer.Tracking {
	class TrackingCompletedEventArgs : EventArgs {
		private CompletionResult result;
		private object failureData;
		private ITrackingCommand command;

		public TrackingCompletedEventArgs(CompletionResult result, object failureData, ITrackingCommand command) {
			this.result = result;
			this.failureData = failureData;
			this.command = command;
		}

		public CompletionResult Result {
			get { return result; }
		}

		public object FailureData {
			get { return failureData; }
		}

		public ITrackingCommand Command {
			get { return command; }
		}
	}
}
