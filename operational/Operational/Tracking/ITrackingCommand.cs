using System;
using System.Collections.Generic;
using System.Text;

namespace OperationalLayer.Tracking {
	interface ITrackingCommand {
		TrackingData Process();
		string Label { get; }
	}
}
