using System;
using System.Collections.Generic;
using System.Text;

using UrbanChallenge.Common;
using UrbanChallenge.Common.Pose;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Behaviors.CompletionReport;

using OperationalLayer.Tracking;
using OperationalLayer.Tracking.SpeedControl;
using OperationalLayer.Tracking.Steering;
using OperationalLayer.RoadModel;
using OperationalLayer.PathPlanning;
using OperationalLayer.Pose;
using System.Diagnostics;
using UrbanChallenge.Common.Path;
using UrbanChallenge.PathSmoothing;

namespace OperationalLayer.OperationalBehaviors {
	class Turn : PlanningBase {
		private const string commandLabel = "Turn/normal";

		private ArbiterLaneId targetLaneID;
		private LinePath targetPath;
		private LinePath leftBound;
		private LinePath rightBound;

		private Polygon intersectionPolygon;

		private LinePath turnBasePath;
		private LinePath pseudoStartLane;

		public override void OnBehaviorReceived(Behavior b) {
			if (b is TurnBehavior) {
				TurnBehavior cb = (TurnBehavior)b;

				if (object.Equals(cb.TargetLane, targetLaneID)) {
					Services.BehaviorManager.QueueParam(cb);
				}
				else {
					Services.BehaviorManager.Execute(b, null, false);
				}
			}
			else {
				Services.BehaviorManager.Execute(b, null, false);
			}
		}

		public override void OnTrackingCompleted(TrackingCompletedEventArgs e) {
			BehaviorManager.TraceSource.TraceEvent(TraceEventType.Information, 0, "in turn behavior, received tracking complete: {0}, result {1}", e.Command, e.Result);
			if (e.Command.Label == commandLabel) {
				// we've come to a stop
				// transition to hold brake
				Services.BehaviorManager.Execute(new HoldBrakeBehavior(), null, true);

				// send a completion report
				ForwardCompletionReport(new SuccessCompletionReport(typeof(TurnBehavior)));
			}
		}

		protected override void OnSmoothSuccess(ref PlanningResult result) {
			if (!(result.pathBlocked || result.dynamicallyInfeasible)) {
				// check the path, see if the first segment goes 180 off
				if (result.smoothedPath != null && result.smoothedPath.Count >= 2 && Math.Abs(result.smoothedPath.GetSegment(0).UnitVector.ArcTan) > 90*Math.PI/180) {
					result = OnDynamicallyInfeasible(null, null, true);
					Console.WriteLine("turn is 180 deg off, returning infeasible");
					return;
				}
			}

			base.OnSmoothSuccess(ref result);
		}

		private void HandleTurnBehavior(TurnBehavior cb) {
			// get the transformer to take us from absolute to relative coordinates
			AbsoluteTransformer absTransform = Services.StateProvider.GetAbsoluteTransformer(cb.TimeStamp);

			targetPath = cb.TargetLanePath.Transform(absTransform).RemoveZeroLengthSegments();
			if (cb.LeftBound != null) {
				leftBound = new LinePath(cb.LeftBound).Transform(absTransform);
			}
			else {
				leftBound = null;
			}

			if (cb.RightBound != null) {
				rightBound = new LinePath(cb.RightBound).Transform(absTransform);
			}
			else {
				rightBound = null;
			}

			if (targetPath.PathLength < 12) {
				double pathLength = targetPath.PathLength;
				double ext = 12 - pathLength;
				targetPath.Add(targetPath.EndPoint.Location + targetPath.EndSegment.Vector.Normalize(ext));
			}

			behaviorTimestamp = absTransform.Timestamp;

			if (cb.IntersectionPolygon != null) {
				intersectionPolygon = cb.IntersectionPolygon.Transform(absTransform);

				// set the polygon to the ui
				Services.UIService.PushPolygon(cb.IntersectionPolygon, cb.TimeStamp, "intersection polygon", false);
			}
			else {
				intersectionPolygon = null;
			}

			this.ignorableObstacles = cb.IgnorableObstacles;
			Services.ObstaclePipeline.LastIgnoredObstacles = cb.IgnorableObstacles;

			HandleSpeedCommand(cb.SpeedCommand);
		}

		public override void Initialize(Behavior b) {
			base.Initialize(b);

			Services.ObstaclePipeline.ExtraSpacing = 0;
			Services.ObstaclePipeline.UseOccupancyGrid = true;

			// extract the relevant information
			TurnBehavior cb = (TurnBehavior)b;
			targetLaneID = cb.TargetLane;

			// create a fake start lane so we can do the intersection pull stuff
			pseudoStartLane = new LinePath();
			pseudoStartLane.Add(new Coordinates(-1, 0));
			pseudoStartLane.Add(Coordinates.Zero);

			AbsoluteTransformer absTransform = Services.StateProvider.GetAbsoluteTransformer(cb.TimeStamp);
			pseudoStartLane = pseudoStartLane.Transform(absTransform.Invert());

			HandleTurnBehavior(cb);

			curTimestamp = cb.TimeStamp;

			// do an initial plan without obstacle avoidance
			DoInitialPlan();

			BehaviorManager.TraceSource.TraceEvent(TraceEventType.Verbose, 0, "turn behavior - initialized");
		}

		private void DoInitialPlan() {
			InitializePlanningSettings();

			curTimestamp = Services.RelativePose.CurrentTimestamp;

			vs = Services.StateProvider.GetVehicleState();

			LinePath curTargetPath = targetPath.Clone();
			LinePath curLeftBound = null;
			if (leftBound != null) {
				curLeftBound = leftBound.Clone();
			}
			LinePath curRightBound = null;
			if (rightBound != null) {
				curRightBound = rightBound.Clone();
			}

			// get the distance between our current location and the start point
			double distToStart = Coordinates.Zero.DistanceTo(curTargetPath[0]);

			// extract off the first 5 m of the target path
			LinePath.PointOnPath endTarget = curTargetPath.AdvancePoint(curTargetPath.StartPoint, 12);
			curTargetPath = curTargetPath.SubPath(curTargetPath.StartPoint, endTarget);

			AddTargetPath(curTargetPath.Clone(), 0.005);

			// adjust the left bound and right bounds starting distance
			if (curLeftBound != null) {
				LinePath.PointOnPath leftBoundStart = curLeftBound.AdvancePoint(curLeftBound.StartPoint, 2);
				curLeftBound = curLeftBound.SubPath(leftBoundStart, curLeftBound.EndPoint);
				AddLeftBound(curLeftBound, false);
			}
			if (curRightBound != null) {
				LinePath.PointOnPath rightBoundStart = curRightBound.AdvancePoint(curRightBound.StartPoint, 2);
				curRightBound = curRightBound.SubPath(rightBoundStart, curRightBound.EndPoint);
				AddRightBound(curRightBound, false);
			}

			// add the intersection pull path
			LinePath intersectionPullPath = new LinePath();
			double pullWeight = 0;
			AbsoluteTransformer trans = Services.StateProvider.GetAbsoluteTransformer(curTimestamp);
			GetIntersectionPullPath(pseudoStartLane.Transform(trans), curTargetPath, intersectionPolygon, true, true, intersectionPullPath, ref pullWeight);

			if (intersectionPullPath.Count > 0) {
				AddTargetPath(intersectionPullPath, pullWeight);
			}

			// set up planning details
			// add our position to the current target path
			curTargetPath.Insert(0, new Coordinates(0, 0));
			smootherBasePath = curTargetPath;
			// add the bounds

			// calculate max speed
			settings.maxSpeed = GetMaxSpeed(null, LinePath.PointOnPath.Invalid);

			// fill in auxiliary settings
			if (curLeftBound != null || curRightBound != null) {
				settings.endingHeading = curTargetPath.EndSegment.UnitVector.ArcTan;
			}
			disablePathAngleCheck = true;

			settings.Options.w_diff = 4;

			// do the planning
			PlanningResult planningResult = Smooth(false);

			// if the planning was a success, store the result
			if (!planningResult.dynamicallyInfeasible) {
				// transform to absolute coordinates
				AbsoluteTransformer absTransform = Services.StateProvider.GetAbsoluteTransformer(curTimestamp);
				turnBasePath = planningResult.smoothedPath.Transform(absTransform.Invert());
			}
		}

		public override void Process(object param) {
			if (!base.BeginProcess()) {
				return;
			}

			// check if we're given a param
			if (param != null && param is TurnBehavior) {
				TurnBehavior turnParam = (TurnBehavior)param;

				HandleTurnBehavior(turnParam);

				BehaviorManager.TraceSource.TraceEvent(TraceEventType.Information, 0, "turn behavior - got new param, speed command {0}", turnParam.SpeedCommand);
			}

			LinePath curTargetPath = targetPath.Clone();
			LinePath curLeftBound = null;
			if (leftBound != null) {
				curLeftBound = leftBound.Clone();
			}
			LinePath curRightBound = null;
			if (rightBound != null) {
				curRightBound = rightBound.Clone();
			}
			// transform the path into the current timestamp
			if (behaviorTimestamp != curTimestamp) {
				// get the relative transform
				RelativeTransform relTransform = Services.RelativePose.GetTransform(behaviorTimestamp, curTimestamp);
				BehaviorManager.TraceSource.TraceEvent(TraceEventType.Verbose, 0, "in turn behavior, transforming from {0}->{1}, wanted {2}->{3}", relTransform.OriginTimestamp, relTransform.EndTimestamp, behaviorTimestamp, curTimestamp);
				curTargetPath.TransformInPlace(relTransform);
				if (curLeftBound != null) {
					curLeftBound.TransformInPlace(relTransform);
				}
				if (curRightBound != null) {
					curRightBound.TransformInPlace(relTransform);
				}
			}

			// get the distance between our current location and the start point
			double distToStart = Coordinates.Zero.DistanceTo(curTargetPath[0]);

			double startDist = Math.Max(0, TahoeParams.FL-distToStart);

			// extract off the first 5 m of the target path
			LinePath.PointOnPath endTarget = curTargetPath.AdvancePoint(curTargetPath.StartPoint, TahoeParams.VL);
			curTargetPath = curTargetPath.SubPath(curTargetPath.StartPoint, endTarget);
			curTargetPath = curTargetPath.RemoveZeroLengthSegments();

			// adjust the left bound and right bounds starting distance
			if (curLeftBound != null) {
				LinePath.PointOnPath leftBoundStart = curLeftBound.AdvancePoint(curLeftBound.StartPoint, 2);
				curLeftBound = curLeftBound.SubPath(leftBoundStart, curLeftBound.EndPoint);
				AddLeftBound(curLeftBound, false);
				Services.UIService.PushLineList(curLeftBound, curTimestamp, "left bound", true);
			}

			if (curRightBound != null) {
				LinePath.PointOnPath rightBoundStart = curRightBound.AdvancePoint(curRightBound.StartPoint, 2);
				curRightBound = curRightBound.SubPath(rightBoundStart, curRightBound.EndPoint);
				AddRightBound(curRightBound, false);

				Services.UIService.PushLineList(curRightBound, curTimestamp, "right bound", true);
			}

			if (cancelled) return;

			BehaviorManager.TraceSource.TraceEvent(TraceEventType.Verbose, 0, "in turn behavior, dist to start: {0}", distToStart);
			if (distToStart < TahoeParams.FL * 0.75) {
				BehaviorManager.TraceSource.TraceEvent(TraceEventType.Information, 0, "in turn behavior, past start point of target path");
				// return a completion report
				ForwardCompletionReport(new SuccessCompletionReport(typeof(TurnBehavior)));

			}

			AddTargetPath(curTargetPath.Clone(), 0.005);

			// transform the pseudo start lane to the current timestamp
			AbsoluteTransformer trans = Services.StateProvider.GetAbsoluteTransformer(curTimestamp);
			LinePath curStartPath = pseudoStartLane.Transform(trans);

			// add the intersection pull path
			LinePath intersectionPullPath = new LinePath();
			double pullWeight = 0;
			GetIntersectionPullPath(curStartPath, curTargetPath, intersectionPolygon, true, true, intersectionPullPath, ref pullWeight);

			if (intersectionPullPath.Count > 0) {
				AddTargetPath(intersectionPullPath, pullWeight);
			}

			// set up planning details
			// add our position to the current target path
			LinePath origTargetPath = curTargetPath.Clone();
			curTargetPath.Insert(0, new Coordinates(0, 0));
			//curTargetPath.Insert(1, new Coordinates(1, 0));

			smootherBasePath = curTargetPath;
			// add the bounds

			// calculate max speed
			settings.maxSpeed = GetMaxSpeed(null, LinePath.PointOnPath.Invalid);
			settings.Options.w_diff = 4;

			BehaviorManager.TraceSource.TraceEvent(TraceEventType.Information, 0, "max speed set to {0}", settings.maxSpeed);

			if (cancelled) return;

			Services.UIService.PushLineList(curTargetPath, curTimestamp, "subpath", true);

			// set the avoidance path to the previously planned path
			// transform to the current timestamp
			disablePathAngleCheck = true;
			if (turnBasePath != null) {
				avoidanceBasePath = turnBasePath.Transform(trans);
				avoidanceBasePath = avoidanceBasePath.SubPath(avoidanceBasePath.GetClosestPoint(Coordinates.Zero), avoidanceBasePath.EndPoint);
				maxAvoidanceBasePathAdvancePoint = avoidanceBasePath.GetPointOnPath(1);

				if (avoidanceBasePath.PathLength > 7.5) {
					smootherBasePath = avoidanceBasePath.SubPath(avoidanceBasePath.StartPoint, avoidanceBasePath.AdvancePoint(avoidanceBasePath.EndPoint, -7.5));
				}
				else {
					smootherBasePath = avoidanceBasePath.Clone();
				}

				disablePathAngleCheck = false;
			}

			// fill in auxiliary settings
			if (curLeftBound != null || curRightBound != null) {
				settings.endingHeading = curTargetPath.EndSegment.UnitVector.ArcTan;
			}

			settings.endingPositionFixed = false;
			settings.endingPositionMax = 16;
			settings.endingPositionMin = -16;
			if (curLeftBound != null && curLeftBound.Count > 0 && curTargetPath.Count > 0) {
				double leftWidth = curLeftBound[0].DistanceTo(origTargetPath[0]) - TahoeParams.T/2;
				settings.endingPositionMax = leftWidth;
				settings.endingPositionFixed = true;
			}

			if (curRightBound != null && curRightBound.Count > 0 && curTargetPath.Count > 0) {
				double rightWidth = curRightBound[0].DistanceTo(origTargetPath[0]) - TahoeParams.T/2;
				settings.endingPositionFixed = true;
				settings.endingPositionMin = -rightWidth;
			}

			//disablePathAngleCheck = true;
			//useAvoidancePath = true;

			// do the planning
			SmoothAndTrack(commandLabel, true);
		}

		protected override LinePath GetPathBlockedSubPath(LinePath basePath, double stopDist) {
			return basePath;
		}

		protected override bool InIntersection() {
			return true;
		}
	}
}
