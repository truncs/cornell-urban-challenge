using System;
using System.Collections.Generic;
using System.Text;
using OperationalLayer.Tracking.Steering;
using OperationalLayer.Tracking.SpeedControl;

namespace OperationalLayer.PathPlanning {
	class PlanningResult {
		public SmoothedPath smoothedPath;
		public ISteeringCommandGenerator steeringCommandGenerator;
		public ISpeedCommandGenerator speedCommandGenerator;

		public string commandLabel;

		public bool pathBlocked;
		public bool dynamicallyInfeasible;
	}
}
