using System;
using System.Collections.Generic;
using System.Text;

using UrbanChallenge.Common;
using UrbanChallenge.Common.Pose;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Behaviors;

using OperationalLayer.Tracking;
using OperationalLayer.Tracking.SpeedControl;
using OperationalLayer.Tracking.Steering;
using OperationalLayer.RoadModel;
using OperationalLayer.PathPlanning;
using OperationalLayer.Pose;
using UrbanChallenge.Behaviors.CompletionReport;
using UrbanChallenge.Common.Path;
using System.Diagnostics;
using UrbanChallenge.PathSmoothing;
using SimOperationalService;

namespace OperationalLayer.OperationalBehaviors {
	class ChangeLanes : PlanningBase {
		private const string commandLabel = "ChangeLanes/normal";

		private ArbiterLaneId startingLaneID;
		private LinePath startingLanePath;
		private double startingLaneWidth;
		private int startingNumLanesLeft;
		private int startingNumLanesRight;

		private ArbiterLaneId endingLaneID;
		private LinePath endingLanePath;
		private double endingLaneWidth;

		private bool changeLeft;

		private double dist;
		private double prevNominalDist;
		private CarTimestamp startTimestamp;

		public override void OnBehaviorReceived(Behavior b) {
			if (b is ChangeLaneBehavior) {
				ChangeLaneBehavior cb = (ChangeLaneBehavior)b;

				if (cb.StartLane.Equals(startingLaneID) && cb.TargetLane.Equals(endingLaneID)) {

					LinePath startingLanePath = cb.BackupStartLanePath;
					LinePath endingLanePath = cb.BackupTargetLanePath;

					Services.UIService.PushAbsolutePath(startingLanePath, cb.TimeStamp, "original path1");
					Services.UIService.PushAbsolutePath(endingLanePath, cb.TimeStamp, "original path2");

					Services.BehaviorManager.QueueParam(cb);

					BehaviorManager.TraceSource.TraceEvent(TraceEventType.Information, 0, "in change lanes -- got change lanes behavior with same start/end lane");
				}
				else {
					// this is a new behavior
					Services.BehaviorManager.Execute(b, null, false);
					BehaviorManager.TraceSource.TraceEvent(TraceEventType.Information, 0, "in change lanes -- got change lanes with different start/end lane ({0}/{1})", cb.StartLane, cb.TargetLane);
				}
			}
			else {
				Services.BehaviorManager.Execute(b, null, false);
			}
		}

		public override void OnTrackingCompleted(TrackingCompletedEventArgs e) {
			BehaviorManager.TraceSource.TraceEvent(TraceEventType.Verbose, 0, "in change lanes -- tracking completed, result {0}", e.Result);
			if (e.Command.Label == commandLabel) {
				// we've finished coming to a stop, cancel the behavior and execute a hold brake
				Services.BehaviorManager.Execute(new HoldBrakeBehavior(), null, true);

				// send a completion report
				ForwardCompletionReport(new SuccessCompletionReport(typeof(ChangeLaneBehavior)));
			}
		}

		private void HandleBehavior(ChangeLaneBehavior cb) {
			AbsoluteTransformer absTransform = Services.StateProvider.GetAbsoluteTransformer(cb.TimeStamp);
			startingLanePath = cb.BackupStartLanePath.Transform(absTransform);
			endingLanePath = cb.BackupTargetLanePath.Transform(absTransform);

			startingNumLanesLeft = cb.StartingNumLanesLeft;
			startingNumLanesRight = cb.StartingNumLaneRights;

			startingLaneWidth = cb.StartLaneWidth;
			endingLaneWidth = cb.TargetLaneWidth;

			startingLaneID = cb.StartLane;
			endingLaneID = cb.TargetLane;
			changeLeft = cb.ChangeLeft;

			if (cb.MaxDist > -TahoeParams.FL) {
				dist = cb.MaxDist + TahoeParams.FL;
			}
			else {
				dist = 0;
			}
			
			speedCommand = cb.SpeedCommand;

			ignorableObstacles = cb.IgnorableObstacles;
			Services.ObstaclePipeline.LastIgnoredObstacles = cb.IgnorableObstacles;

			behaviorTimestamp = absTransform.Timestamp;
		}

		public override void Initialize(Behavior b) {
			base.Initialize(b);

			Services.ObstaclePipeline.ExtraSpacing = 0;
			Services.ObstaclePipeline.UseOccupancyGrid = true;

			ChangeLaneBehavior cb = (ChangeLaneBehavior)b;

			Services.UIService.PushAbsolutePath(cb.BackupStartLanePath, cb.TimeStamp, "original path1");
			Services.UIService.PushAbsolutePath(cb.BackupTargetLanePath, cb.TimeStamp, "original path2");

			startTimestamp = b.TimeStamp;

			HandleBehavior(cb);

			BehaviorManager.TraceSource.TraceEvent(TraceEventType.Information, 0, "in change lanes -- initializing, start timestamp {0}", b.TimeStamp);
		}

		public override void Process(object param) {
			try {
				Trace.CorrelationManager.StartLogicalOperation("ChangeLanes");

				if (!base.BeginProcess()){
					return;
				}

				// check if we were given a parameter
				if (param != null && param is ChangeLaneBehavior) {
					ChangeLaneBehavior clParam = (ChangeLaneBehavior)param;
					HandleBehavior(clParam);

					BehaviorManager.TraceSource.TraceEvent(TraceEventType.Verbose, 0, "got new param -- speed {0}, dist {1}, dist timestamp {2}", clParam.SpeedCommand, clParam.MaxDist, clParam.TimeStamp);
				}

				// project the lane paths up to the current time
				RelativeTransform relTransform = Services.RelativePose.GetTransform(behaviorTimestamp, curTimestamp);
				LinePath curStartingPath = startingLanePath.Transform(relTransform);
				LinePath curEndingPath = endingLanePath.Transform(relTransform);

				// get the starting and ending lane models
				ILaneModel startingLaneModel, endingLaneModel;
				Services.RoadModelProvider.GetLaneChangeModels(curStartingPath, startingLaneWidth, startingNumLanesLeft, startingNumLanesRight,
					curEndingPath, endingLaneWidth, changeLeft, behaviorTimestamp, out startingLaneModel, out endingLaneModel);
				
				// calculate the max speed
				// TODO: make this look ahead for slowing down
				settings.maxSpeed = GetMaxSpeed(endingLanePath, endingLanePath.ZeroPoint);
				
				// get the remaining lane change distance
				double remainingDist = GetRemainingLaneChangeDistance();

				BehaviorManager.TraceSource.TraceEvent(TraceEventType.Verbose, 0, "remaining distance is {0}", remainingDist);

				if (cancelled) return;

				// check if we're done
				if (remainingDist <= 0) {
					// create a new stay in lane behavior
					int deltaLanes = changeLeft ? -1 : 1;
					StayInLaneBehavior stayInLane = new StayInLaneBehavior(endingLaneID, speedCommand, null, endingLanePath, endingLaneWidth, startingNumLanesLeft + deltaLanes, startingNumLanesRight - deltaLanes);
					stayInLane.TimeStamp = behaviorTimestamp.ts;
					Services.BehaviorManager.Execute(stayInLane, null, false);

					// send completion report
					ForwardCompletionReport(new SuccessCompletionReport(typeof(ChangeLaneBehavior)));

					return;
				}

				// calculate the planning distance
				double planningDist = GetPlanningDistance();
				if (planningDist < remainingDist + TahoeParams.VL) {
					planningDist = remainingDist + TahoeParams.VL;
				}

				BehaviorManager.TraceSource.TraceEvent(TraceEventType.Verbose, 0, "planning dist {0}", planningDist);

				// create a linearization options structure
				LinearizationOptions laneOpts = new LinearizationOptions();
				laneOpts.Timestamp = curTimestamp;

				// get the center line of the target lane starting at the distance we want to enter into it
				laneOpts.StartDistance = remainingDist;
				laneOpts.EndDistance = planningDist;
				LinePath centerLine = endingLaneModel.LinearizeCenterLine(laneOpts);

				// add the final center line as a weighting
				AddTargetPath(centerLine.Clone(), default_lane_alpha_w);

				// pre-pend (0,0) as our position
				centerLine.Insert(0, new Coordinates(0, 0));

				LinePath leftBound = null;
				LinePath rightBound = null;
				// figure out if the lane is to the right or left of us
				laneOpts.EndDistance = planningDist;
				double leftEndingStartDist, rightEndingStartDist;
				GetLaneBoundStartDists(curEndingPath, endingLaneWidth, out leftEndingStartDist, out rightEndingStartDist);
				if (changeLeft) {
					// we're the target lane is to the left
					// get the left bound of the target lane

					laneOpts.StartDistance = Math.Max(leftEndingStartDist, TahoeParams.FL);
					leftBound = endingLaneModel.LinearizeLeftBound(laneOpts);
				}
				else {
					// we're changing to the right, get the right bound of the target lane
					laneOpts.StartDistance = Math.Max(rightEndingStartDist, TahoeParams.FL);
					rightBound = endingLaneModel.LinearizeRightBound(laneOpts);
				}

				// get the other bound as the starting lane up to 5m before the remaining dist
				laneOpts.StartDistance = TahoeParams.FL;
				laneOpts.EndDistance = Math.Max(0, remainingDist-5);
				if (changeLeft) {
					if (laneOpts.EndDistance > 0) {
						rightBound = startingLaneModel.LinearizeRightBound(laneOpts);
					}
					else {
						rightBound = new LinePath();
					}
				}
				else {
					if (laneOpts.EndDistance > 0) {
						leftBound = startingLaneModel.LinearizeLeftBound(laneOpts);
					}
					else {
						leftBound = new LinePath();
					}
				}

				// append on the that bound of the target lane starting at the remaining dist
				laneOpts.StartDistance = Math.Max(remainingDist, TahoeParams.FL);
				laneOpts.EndDistance = planningDist;
				if (changeLeft) {
					rightBound.AddRange(endingLaneModel.LinearizeRightBound(laneOpts));
				}
				else {
					leftBound.AddRange(endingLaneModel.LinearizeLeftBound(laneOpts));
				}

				// set up the planning
				smootherBasePath = centerLine;
				AddLeftBound(leftBound, !changeLeft);
				AddRightBound(rightBound, changeLeft);
				
				Services.UIService.PushLineList(centerLine, curTimestamp, "subpath", true);
				Services.UIService.PushLineList(leftBound, curTimestamp, "left bound", true);
				Services.UIService.PushLineList(rightBound, curTimestamp, "right bound", true);

				if (cancelled) return;

				// set auxillary options
				settings.endingHeading = centerLine.EndSegment.UnitVector.ArcTan;

				// smooth and track that stuff
				SmoothAndTrack(commandLabel, true);
			}
			finally {
				Trace.CorrelationManager.StopLogicalOperation();
			}
		}

		/// <summary>
		/// Compute the remaining distance needed to complete a lane change
		/// </summary>
		/// <param name="curTimestamp"></param>
		/// <returns></returns>
		private double GetRemainingLaneChangeDistance() {
			// get the remaining distance
			double distTravelled = Services.TrackedDistance.GetDistanceTravelled(behaviorTimestamp, curTimestamp);
			double remainingDist = dist - distTravelled;

			BehaviorManager.TraceSource.TraceEvent(TraceEventType.Verbose, 0, "behavior dist {0}, travelled dist {1}, rem dist {2}, dist timestamp {3}, cur timestamp {4}", dist, distTravelled, remainingDist, behaviorTimestamp, curTimestamp);

			// figure out the distance to perform a lane change given the commanded speed
			double speed;
			if (speedCommand is ScalarSpeedCommand) {
				speed = ((ScalarSpeedCommand)speedCommand).Speed;
			}
			else {
				speed = approachSpeed.Value;
			}

			// calculate the nominal lane change distance
			double nomDistance = NominalLaneChangeDistance(speed);
			distTravelled = Services.TrackedDistance.GetDistanceTravelled(startTimestamp, curTimestamp);
			double remainingNomDistance = nomDistance - distTravelled;

			BehaviorManager.TraceSource.TraceEvent(TraceEventType.Verbose, 0, "nominal distance {0}, speed {1}, dist travelled {2}, rem distance {3}, start timestamp {4}, cur timestamp {5}", nomDistance, speed, distTravelled, remainingNomDistance, startTimestamp, curTimestamp);

			// take whatever is shorter of the nominal and requested lane change distance
			return Math.Min(remainingDist, remainingNomDistance);
		}

		private double NominalLaneChangeDistance(double speed) {
			prevNominalDist = Math.Max(prevNominalDist, Math.Max(4*speed + 10, 2*TahoeParams.VL));
			return prevNominalDist;
		}
	}
}
