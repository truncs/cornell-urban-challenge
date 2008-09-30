using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;

namespace OperationalLayer.Tracking {
	interface ITrackingCommandBase {
		CompletionResult CompletionStatus { get; }
		object FailureData { get; }

		void BeginTrackingCycle(CarTimestamp timestamp);
	}
}
