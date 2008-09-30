using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.PathSmoothing;
using UrbanChallenge.Common;
using OperationalLayer.Tracing;
using OperationalLayer.Tracking.Steering;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Operational.Common;
using UrbanChallenge.Behaviors;

namespace OperationalLayer.PathPlanning {
	class PathPlanner {
		public static bool GenerateDetails = true;
		public static AvoidanceDetails LastAvoidanceDetails = null;

		public class SmoothingResult {
			public SmoothResult result;
			public SmoothedPath path;
			public AvoidanceDetails details;
			
			public SmoothingResult(SmoothResult result, SmoothedPath path, AvoidanceDetails details) {
				this.result = result;
				this.path = path;
				this.details = details;
			}
		}

		public PathPlanner() {
			this.Options = PathSmoother.GetDefaultOptions();
			this.Options.alpha_s = 100;
			this.Options.alpha_d = 100;
			this.Options.alpha_w = 0;
		}

		private PathSmoother smoother;
		public SmootherOptions Options;

		public SmoothingResult PlanPath(LineList basePath, LineList leftBound, LineList rightBound, double initHeading, double maxSpeed, double startSpeed, double? endingHeading, CarTimestamp time) {
			return PlanPath(basePath, leftBound, rightBound, initHeading, maxSpeed, startSpeed, endingHeading, time, false);
		}

		public SmoothingResult PlanPath(LineList basePath, LineList leftBound, LineList rightBound, double initHeading, double maxSpeed, double startSpeed, double? endingHeading, CarTimestamp time, bool endingOffsetFixed) {
			return PlanPath(basePath, null, new LineList[] { leftBound }, new LineList[] { rightBound }, initHeading, maxSpeed, startSpeed, endingHeading, time, endingOffsetFixed);
		}

		public SmoothingResult PlanPath(LineList basePath, LineList targetPath, IList<LineList> leftBounds, IList<LineList> rightBounds, double initHeading, double maxSpeed, double startSpeed, double? endingHeading, CarTimestamp time, bool endingOffsetFixed) {
			// create the boundary list
			List<Boundary> upperBound = new List<Boundary>();
			foreach (LineList leftBound in leftBounds) {
				Boundary ub0 = new Boundary();
				ub0.Coords = leftBound;
				ub0.DesiredSpacing = 0.5;
				ub0.MinSpacing = 0.1;
				ub0.Polygon = false;
				upperBound.Add(ub0);
			}

			List<Boundary> lowerBound = new List<Boundary>();
			foreach (LineList rightBound in rightBounds) {
				Boundary lb0 = new Boundary();
				lb0.Coords = rightBound;
				lb0.DesiredSpacing = 0.5;
				lb0.MinSpacing = 0.1;
				lb0.Polygon = false;
				lowerBound.Add(lb0);
			}

			return PlanPath(basePath, targetPath, upperBound, lowerBound, initHeading, maxSpeed, startSpeed, endingHeading, time, endingOffsetFixed);
		}

		public SmoothingResult PlanPath(LineList basePath, LineList targetPath, 
			List<Boundary> leftBounds, List<Boundary> rightBounds, double initHeading, 
			double maxSpeed, double startSpeed, double? endingHeading, CarTimestamp time, bool endingOffsetFixed) {
			PlanningSettings settings = new PlanningSettings();
			settings.basePath = basePath;
			settings.targetPaths = new List<Boundary>();
			settings.targetPaths.Add(new Boundary(targetPath, 0, 0, settings.Options.alpha_w));
			settings.leftBounds = leftBounds;
			settings.rightBounds = rightBounds;
			settings.initHeading = initHeading;
			settings.maxSpeed = maxSpeed;
			settings.startSpeed = startSpeed;
			settings.endingHeading = endingHeading;
			settings.timestamp = time;
			settings.endingPositionFixed = endingOffsetFixed;

			return PlanPath(settings);
		}

		public SmoothingResult PlanPath(PlanningSettings settings) {
			SmootherOptions opts = settings.Options;
			// for now, just run the smoothing
			opts.init_heading = settings.initHeading;
			opts.set_init_heading = true;

			opts.min_init_velocity = settings.startSpeed*0.5;
			opts.set_min_init_velocity = true;

			opts.max_init_velocity = Math.Max(settings.maxSpeed, settings.startSpeed);
			opts.set_max_init_velocity = true;

			opts.min_velocity = 0.1;
			opts.max_velocity = Math.Max(opts.min_velocity+0.1, settings.maxSpeed);

			opts.k_max = Math.Min(TahoeParams.CalculateCurvature(-TahoeParams.SW_max, settings.startSpeed), TahoeParams.CalculateCurvature(TahoeParams.SW_max, settings.startSpeed)) * 0.97;

			opts.generate_details = true;// GenerateDetails;

			if (settings.endingHeading != null) {
				opts.set_final_heading = true;
				opts.final_heading = settings.endingHeading.Value;
			}
			else {
				opts.set_final_heading = false;
			}

			opts.set_final_offset = settings.endingPositionFixed;
			opts.final_offset_min = settings.endingPositionMin;
			opts.final_offset_max = settings.endingPositionMax;


			if (settings.maxEndingSpeed != null) {
				opts.set_final_velocity_max = true;
				opts.final_velocity_max = Math.Max(opts.min_velocity+0.1, settings.maxEndingSpeed.Value);
			}
			else {
				opts.set_final_velocity_max = false;
			}

			opts.a_lat_max = 6;

			// create the boundary list
			List<UrbanChallenge.PathSmoothing.PathPoint> ret = new List<UrbanChallenge.PathSmoothing.PathPoint>();
			smoother = new PathSmoother();
			OperationalTrace.WriteVerbose("calling smooth path");
			SmoothResult result = smoother.SmoothPath(settings.basePath, settings.targetPaths, settings.leftBounds, settings.rightBounds, opts, ret);

			if (result != SmoothResult.Sucess) {
				OperationalTrace.WriteWarning("smooth path result: {0}", result);
			}
			else {
				OperationalTrace.WriteVerbose("smooth path result: {0}", result);
			}

			AvoidanceDetails details = null;
			if (opts.generate_details) {
				details = new AvoidanceDetails();
				details.leftBounds = settings.leftBounds;
				details.rightBounds = settings.rightBounds;
				details.smoothingDetails = smoother.GetSmoothingDetails();
				LastAvoidanceDetails = details;

				// push out the points
				Coordinates[] leftPoints = new Coordinates[details.smoothingDetails.leftBounds.Length];
				for (int i = 0; i < leftPoints.Length; i++) {
					leftPoints[i] = details.smoothingDetails.leftBounds[i].point;
				}

				Coordinates[] rightPoints = new Coordinates[details.smoothingDetails.rightBounds.Length];
				for (int i = 0; i < rightPoints.Length; i++) {
					rightPoints[i] = details.smoothingDetails.rightBounds[i].point;
				}

				Services.UIService.PushPoints(leftPoints, settings.timestamp, "left bound points", true);
				Services.UIService.PushPoints(rightPoints, settings.timestamp, "right bound points", true);
			}

			//if (result == SmoothResult.Sucess) {
				Coordinates[] points = new Coordinates[ret.Count];
				double[] speeds = new double[ret.Count];
				for (int i = 0; i < ret.Count; i++) {
					points[i] = new Coordinates(ret[i].x, ret[i].y);
					speeds[i] = ret[i].v;
				}

				SmoothedPath path = new SmoothedPath(settings.timestamp, points, speeds);

				return new SmoothingResult(result, path, details);
			/*}
			else {
				SmoothedPath path = new SmoothedPath(settings.timestamp, settings.basePath, null);

				return new SmoothingResult(result, path);
			}*/
		}

		public void Cancel() {
			if (smoother != null)
				smoother.Cancel();
		}
	}
}
