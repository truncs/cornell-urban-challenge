using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.PathSmoothing;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common;
using UrbanChallenge.Operational.Common;

namespace OperationalLayer.PathPlanning {
	class PlanningSettings {
		public SmootherOptions Options;
		public LineList basePath;
		public List<Boundary> targetPaths;
		public List<Boundary> leftBounds;
		public List<Boundary> rightBounds;

		public double initHeading;
		public double? endingHeading;
		public bool endingPositionFixed;
		public double endingPositionMax;
		public double endingPositionMin;

		public double maxSpeed;
		public double startSpeed;
		public double? maxEndingSpeed;

		public CarTimestamp timestamp;

		public PlanningSettings() {
			Options = PathSmoother.GetDefaultOptions();
			Options.alpha_s = 100;
			Options.alpha_d = 100;
			Options.alpha_w = 0;
			Options.num_passes = 2;
			Options.reverse = false;
		}
	}
}
