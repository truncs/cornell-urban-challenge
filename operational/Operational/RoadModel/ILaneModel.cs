using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common;

namespace OperationalLayer.RoadModel {
	interface ILaneModel {
		LinePath LinearizeCenterLine(LinearizationOptions options);
		LinePath LinearizeLeftBound(LinearizationOptions options);
		LinePath LinearizeRightBound(LinearizationOptions options);

		string LaneID { get; }
		CarTimestamp Timestamp { get; }
		double Width { get; }
	}
}
