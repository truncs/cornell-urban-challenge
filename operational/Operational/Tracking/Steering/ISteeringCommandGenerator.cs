using System;
using System.Collections.Generic;
using System.Text;

namespace OperationalLayer.Tracking.Steering {
	interface ISteeringCommandGenerator : ITrackingCommandBase {
		void GetSteeringCommand(ref double? steeringAngle);
	}
}
