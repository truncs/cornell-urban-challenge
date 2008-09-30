using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Behaviors.CompletionReport;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common.Pose;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.Vehicle;

using OperationalLayer.Tracking;
using OperationalLayer.Tracking.SpeedControl;
using OperationalLayer.Tracking.Steering;
using OperationalLayer.RoadModel;
using OperationalLayer.PathPlanning;
using OperationalLayer.Pose;
using OperationalLayer.Utilities;

using UrbanChallenge.PathSmoothing;
using SimOperationalService;
using OperationalLayer.Obstacles;
using OperationalLayer.CarTime;
using UrbanChallenge.Operational.Common;

namespace OperationalLayer.OperationalBehaviors {
	class SupraStayInLane : PlanningBase, IDisposable {
		private enum PlanningPhase {
			/// <summary>
			/// perform a regular stay in lane behavior on the starting lane
			/// </summary>
			StartingLane,
			/// <summary>
			/// Plan out the initial path through the turn
			/// </summary>
			BeginEnteringTurn,
			/// <summary>
			/// perform a combination of stay in lane and turn using the starting
			/// lane boundaries and adding the starting orientation of the ending lane
			/// </summary>
			EnteringTurn,
			/// <summary>
			/// Plan out initial path through the turn only if we're starting up in the turn
			/// </summary>
			BeginMidTurn,
			/// <summary>
			/// perform a regular turn behavior but derive the speeds from the ending lane
			/// </summary>
			MidTurn,
			/// <summary>
			/// perform a regular stay in lane behavior on the ending lane
			/// </summary>
			EndingLane
		}

		private const string commandLabel = "SupraStayInLane/normal";

		private const double starting_lane_trim_dist = 2.5;

		private ArbiterLaneId startingLaneID;
		private LinePath startingLanePath;
		private double startingLaneWidth;
		private int startingLanesLeft;
		private int startingLanesRight;

		private ArbiterLaneId endingLaneID;
		private LinePath endingLanePath;
		private double endingLaneWidth;
		private int endingLanesLeft;
		private int endingLanesRight;

		private Polygon intersectionPolygon;

		private LinePath turnBasePath;

		private PlanningPhase planningPhase;

		private double extraWidth;

		private bool opposingLaneVehicleExists;
		private double opposingLaneVehicleDist;
		private double opposingLaneVehicleSpeed;

		private bool oncomingVehicleExists;
		private double oncomingVehicleDist;
		private double oncomingVehicleSpeed;
		private SpeedCommand oncomingVehicleSpeedCommand;

		private double prevCurvature = double.NaN;
		private Coordinates goalPoint;

		public override void OnBehaviorReceived(Behavior b) {
			if (b is SupraLaneBehavior) {
				SupraLaneBehavior cb = (SupraLaneBehavior)b;

				// check if this is for the same starting/ending lane as we're currently executing
				if (object.Equals(cb.StartingLaneId, startingLaneID) && object.Equals(cb.EndingLaneId, endingLaneID)) {
					// queue up a param object with the new parameter values
					BehaviorManager.TraceSource.TraceEvent(TraceEventType.Information, 0, "received new SupraLaneBehavior for same starting/ending lane: {0}, speed {1}, timestamp {2}", cb.ToString(), cb.SpeedCommand, cb.TimeStamp);

					if (cb.SpeedCommand is ScalarSpeedCommand && double.IsNaN(((ScalarSpeedCommand)cb.SpeedCommand).Speed)) {
						BehaviorManager.TraceSource.TraceEvent(TraceEventType.Error, 0, "received NaN speed command!!");
					}

					Services.UIService.PushAbsolutePath(cb.StartingLanePath, cb.TimeStamp, "original path1");
					Services.UIService.PushAbsolutePath(cb.EndingLanePath, cb.TimeStamp, "original path2");

					// queue the parameter
					Services.BehaviorManager.QueueParam(cb);
				}
				else {
					// the lanes did not match, so execute as a new behavior
					BehaviorManager.TraceSource.TraceEvent(TraceEventType.Information, 0, "received new SupraLaneBehavior for different starting/ending lane: {0}", cb.ToString());

					Services.BehaviorManager.Execute(b, null, false);
				}
			}
			else {
				BehaviorManager.TraceSource.TraceEvent(TraceEventType.Information, 0, "in SupraLaneBehavior, received different behavior: {0}", b);
				Services.BehaviorManager.Execute(b, null, false);
			}
		}

		public override void OnTrackingCompleted(TrackingCompletedEventArgs e) {
			BehaviorManager.TraceSource.TraceEvent(TraceEventType.Information, 0, "in supra lane behavior, received tracking complete: {0}, result {1}", e.Command, e.Result);
			if (e.Command.Label == commandLabel) {
				// we've finished coming to a stop, cancel the behavior and execute a hold brake
				Services.BehaviorManager.Execute(new HoldBrakeBehavior(), null, true);

				// send a completion report
				Stopwatch timer = Stopwatch.StartNew();
				ForwardCompletionReport(new SuccessCompletionReport(typeof(SupraLaneBehavior)));
				timer.Stop();
				BehaviorManager.TraceSource.TraceEvent(TraceEventType.Verbose, 0, "SendCompletionReport took {0} ms", timer.ElapsedMilliseconds);
			}
		}

		private void HandleBehavior(SupraLaneBehavior cb) {
			// get the transform to make the paths vehicle-relative
			AbsoluteTransformer transform = Services.StateProvider.GetAbsoluteTransformer(cb.TimeStamp);
			this.behaviorTimestamp = transform.Timestamp;

			this.startingLaneID = cb.StartingLaneId;
			this.startingLanePath = cb.StartingLanePath.Transform(transform);
			this.startingLaneWidth = cb.StartingLaneWidth;
			this.startingLanesLeft = cb.StartingNumLanesLeft;
			this.startingLanesRight = cb.StartingNumLanesRight;

			this.endingLaneID = cb.EndingLaneId;
			this.endingLanePath = cb.EndingLanePath.Transform(transform);
			this.endingLaneWidth = cb.EndingLaneWidth;
			this.endingLanesLeft = cb.EndingNumLanesLeft;
			this.endingLanesRight = cb.EndingNumLanesRight;

			this.ignorableObstacles = cb.IgnorableObstacles;
			Services.ObstaclePipeline.LastIgnoredObstacles = cb.IgnorableObstacles;

			if (cb.IntersectionPolygon != null) {
				this.intersectionPolygon = cb.IntersectionPolygon.Transform(transform);

				// set the polygon to the ui
				Services.UIService.PushPolygon(cb.IntersectionPolygon, cb.TimeStamp, "intersection polygon", false);
			}
			else {
				this.intersectionPolygon = null;
			}

			HandleSpeedCommand(cb.SpeedCommand);

			opposingLaneVehicleExists = false;
			oncomingVehicleExists = false;
			extraWidth = 0;
			if (cb.Decorators != null) {
				foreach (BehaviorDecorator d in cb.Decorators) {
					if (d is OpposingLaneDecorator) {
						opposingLaneVehicleExists = true;
						opposingLaneVehicleDist = ((OpposingLaneDecorator)d).Distance;
						opposingLaneVehicleSpeed = ((OpposingLaneDecorator)d).Speed;
					}
					else if (d is OncomingVehicleDecorator) {
						oncomingVehicleExists = true;
						oncomingVehicleDist = ((OncomingVehicleDecorator)d).TargetDistance;
						oncomingVehicleSpeed = ((OncomingVehicleDecorator)d).TargetSpeed;
						oncomingVehicleSpeedCommand = ((OncomingVehicleDecorator)d).SecondarySpeed;
					}
					else if (d is WidenBoundariesDecorator) {
						extraWidth = ((WidenBoundariesDecorator)d).ExtraWidth;
					}
				}
			}

			if (oncomingVehicleExists) {
				double timeToCollision = oncomingVehicleDist / Math.Abs(oncomingVehicleSpeed);
				if (oncomingVehicleDist > 30 || timeToCollision > 20 || startingLanesRight > 0) {
					oncomingVehicleExists = false;
				}
				else {
					HandleSpeedCommand(oncomingVehicleSpeedCommand);
				}
			}

			if (startingLaneID != null && Services.RoadNetwork != null) {
				ArbiterLane lane = Services.RoadNetwork.ArbiterSegments[startingLaneID.SegmentId].Lanes[startingLaneID];
				AbsolutePose pose = Services.StateProvider.GetAbsolutePose();
				sparse = (lane.GetClosestPartition(pose.xy).Type == PartitionType.Sparse);

				if (sparse) {
					LinePath.PointOnPath closest = cb.StartingLanePath.GetClosestPoint(pose.xy);
					goalPoint = cb.StartingLanePath[closest.Index+1];
				}
			}

			if (sparse) {
				if (Services.ObstaclePipeline.ExtraSpacing == 0) {
					Services.ObstaclePipeline.ExtraSpacing = 0.5;
				}
				Services.ObstaclePipeline.UseOccupancyGrid = false;
			}
			else {
				Services.ObstaclePipeline.ExtraSpacing = 0;
				smootherSpacingAdjust = 0;
				Services.ObstaclePipeline.UseOccupancyGrid = true;
			}
		}

		public override void Initialize(Behavior b) {
			base.Initialize(b);

			SupraLaneBehavior cb = (SupraLaneBehavior)b;

			Services.UIService.PushAbsolutePath(cb.StartingLanePath, cb.TimeStamp, "original path1");
			Services.UIService.PushAbsolutePath(cb.EndingLanePath, cb.TimeStamp, "original path2");

			HandleBehavior(cb);

			// determine which phase we're in
			// get the closest point on the starting lane
			LinePath.PointOnPath startingLanePoint = startingLanePath.ZeroPoint;
			double remainingDistance = startingLanePath.DistanceBetween(startingLanePoint, startingLanePath.EndPoint);

			curTimestamp = behaviorTimestamp;

			// immediately switch to the mid turn mode if the remaining distance is less than the front-length
			// this prevents problems with the lane linearization extending out past the end of the lane
			if (startingLanePoint == startingLanePath.EndPoint) {
				this.planningPhase = PlanningPhase.EndingLane;
			}
			else if (remainingDistance < TahoeParams.FL) {
				this.planningPhase = PlanningPhase.BeginMidTurn;
			}
			else if (remainingDistance < GetPlanningDistance()) {
				this.planningPhase = PlanningPhase.BeginEnteringTurn;
			}
			else {
				this.planningPhase = PlanningPhase.StartingLane;
			}
		}

		public override void Process(object param) {
			if (!BeginProcess()) {
				return;
			}

			// check if we were given a parameter
			if (param != null && param is SupraLaneBehavior) {
				HandleBehavior((SupraLaneBehavior)param);
			}

			RelativeTransform relTransform = Services.RelativePose.GetTransform(behaviorTimestamp, curTimestamp);

			LinePath curStartingLane = startingLanePath.Transform(relTransform);
			LinePath curEndingLane = endingLanePath.Transform(relTransform);

			if (planningPhase == PlanningPhase.StartingLane) {
				planningPhase = PlanStartingLane(curStartingLane, curEndingLane);
			}
			else if (planningPhase == PlanningPhase.BeginEnteringTurn) {
				planningPhase = PlanBeginningTurn(curStartingLane, curEndingLane);
			}
			else if (planningPhase == PlanningPhase.EnteringTurn) {
				planningPhase = PlanEnteringTurn(curStartingLane, curEndingLane);
			}
			else if (planningPhase == PlanningPhase.BeginMidTurn) {
				planningPhase = PlanBeginMidTurn(curStartingLane, curEndingLane);
			}
			else if (planningPhase == PlanningPhase.MidTurn) {
				planningPhase = PlanMidTurn(curStartingLane, curEndingLane);
			}
			else if (planningPhase == PlanningPhase.EndingLane) {
				planningPhase = PlanEndingLane(curEndingLane);
			}
		}

		private PlanningPhase PlanStartingLane(LinePath curStartingLane, LinePath curEndingLane) {
			ILaneModel startingLaneModel = Services.RoadModelProvider.GetLaneModel(curStartingLane, startingLaneWidth + extraWidth, curTimestamp, startingLanesLeft, startingLanesRight);

			// figure out where we are along the starting lane path
			LinePath.PointOnPath startingLanePoint = curStartingLane.ZeroPoint;

			// calculate the planning distance
			double planningDistance = GetPlanningDistance();

			if (sparse && planningDistance > 10) {
				planningDistance = 10;
			}

			// get the distance to the end point of the lane from our current point
			double remainingDistance = curStartingLane.DistanceBetween(startingLanePoint, curStartingLane.EndPoint);

			double? boundStartDistMin;
			double? boundEndDistMax;
			DetermineSparseStarting(planningDistance, out boundStartDistMin, out boundEndDistMax);

			double avoidanceExtra = sparse ? 5 : 7.5;

			// linearize the lane model
			LinePath centerLine, leftBound, rightBound;
			if (boundEndDistMax != null || boundStartDistMin != null) {
				if (boundEndDistMax != null) {
					boundEndDistMax = Math.Min(boundEndDistMax.Value, remainingDistance - starting_lane_trim_dist);
				}
				else {
					boundEndDistMax = remainingDistance - starting_lane_trim_dist;
				}
				LinearizeStayInLane(startingLaneModel, planningDistance + avoidanceExtra, null, boundEndDistMax, boundStartDistMin, null, curTimestamp, out centerLine, out leftBound, out rightBound);
			}
			else {
				LinearizeStayInLane(startingLaneModel, planningDistance + avoidanceExtra, null, remainingDistance - starting_lane_trim_dist, null, curTimestamp,
					out centerLine, out leftBound, out rightBound);
			}
			
			BehaviorManager.TraceSource.TraceEvent(TraceEventType.Verbose, 0, "in stay in lane, center line count: {0}, left bound count: {1}, right bound count: {2}", centerLine.Count, leftBound.Count, rightBound.Count);

			// add to the planning setup
			avoidanceBasePath = centerLine;
			double targetDist = Math.Max(centerLine.PathLength-avoidanceExtra, planningDistance);
			smootherBasePath = centerLine.SubPath(centerLine.StartPoint, targetDist);

			// calculate time to collision of opposing obstacle if it exists
			LinePath targetLine = centerLine;
			double targetWeight = default_lane_alpha_w;
			if (sparse) {
				targetWeight = 0.00000025;
			}
			else if (oncomingVehicleExists) {
				double shiftDist = -(TahoeParams.T + 1);
				targetLine = centerLine.ShiftLateral(shiftDist);
				targetWeight = 0.01;
			}
			else if (opposingLaneVehicleExists) {
				double timeToCollision = opposingLaneVehicleDist/(Math.Abs(vs.speed)+Math.Abs(opposingLaneVehicleSpeed));
				Services.Dataset.ItemAs<double>("time to collision").Add(timeToCollision, curTimestamp);
				if (timeToCollision < 5) {
					// shift to the right
					double laneWidth = startingLaneModel.Width;
					double shiftDist = -Math.Min(TahoeParams.T/2.0, laneWidth/2.0 - TahoeParams.T/2.0);
					targetLine = centerLine.ShiftLateral(shiftDist);
				}
			}

			// set up the planning
			AddTargetPath(targetLine, targetWeight);
			if (!sparse || boundStartDistMin != null || boundEndDistMax != null) {
				// add the lane bounds
				AddLeftBound(leftBound, true);
				if (!oncomingVehicleExists) {
					AddRightBound(rightBound, false);
				}
			}

			Services.UIService.PushLineList(centerLine, curTimestamp, "subpath", true);
			Services.UIService.PushLineList(leftBound, curTimestamp, "left bound", true);
			Services.UIService.PushLineList(rightBound, curTimestamp, "right bound", true);

			// calculate max speed for the path over the next 120 meters
			double advanceDist = speed_path_advance_dist;
			LinePath.PointOnPath startingLaneEndPoint = curStartingLane.AdvancePoint(startingLanePoint, ref advanceDist);
			LinePath combinedPath = curStartingLane.SubPath(startingLanePoint, startingLaneEndPoint);

			Coordinates endingLaneFirstPoint = curEndingLane[0];
			Coordinates startingLaneLastPoint = curStartingLane.EndPoint.Location;
			double intersectionDist = startingLaneLastPoint.DistanceTo(endingLaneFirstPoint);

			// add a portion of the ending lane of the appropriate length
			LinePath.PointOnPath endingLanePoint = curEndingLane.AdvancePoint(curEndingLane.StartPoint, Math.Max(advanceDist - intersectionDist, 5));
			int endIndex = endingLanePoint.Index + 1;
			combinedPath.AddRange(curEndingLane.GetSubpathEnumerator(0, endIndex));

			// add to the planning setup
			settings.maxSpeed = GetMaxSpeed(combinedPath, combinedPath.StartPoint);
			settings.maxEndingSpeed = GetMaxSpeed(combinedPath, combinedPath.AdvancePoint(combinedPath.StartPoint, planningDistance));
			if (sparse) {
				// limit to 5 mph
				laneWidthAtPathEnd = 20;
				pathAngleCheckMax = 50;
				pathAngleMax = 5 * Math.PI / 180.0;
				settings.maxSpeed = Math.Min(settings.maxSpeed, 2.2352);

				LinePath leftEdge = RoadEdge.GetLeftEdgeLine();
				LinePath rightEdge = RoadEdge.GetRightEdgeLine();
				if (leftEdge != null) {
					leftLaneBounds.Add(new Boundary(leftEdge, 0.1, 1, 100, false));
				}
				if (rightEdge != null) {
					rightLaneBounds.Add(new Boundary(rightEdge, 0.1, 1, 100, false));
				}

				maxAvoidanceBasePathAdvancePoint = avoidanceBasePath.AdvancePoint(avoidanceBasePath.EndPoint, -2);
				//maxSmootherBasePathAdvancePoint = smootherBasePath.AdvancePoint(smootherBasePath.EndPoint, -2);

				PlanningResult result = new PlanningResult();
				ISteeringCommandGenerator commandGenerator = SparseArcVoting.SparcVote(ref prevCurvature, goalPoint);
				if (commandGenerator == null) {
					// we have a block, report dynamically infeasible
					result = OnDynamicallyInfeasible(null, null);
				}
				else {
					result.steeringCommandGenerator = commandGenerator;
					result.commandLabel = commandLabel;
				}

				Track(result, commandLabel);

				if (planningDistance > remainingDistance) {
					return PlanningPhase.BeginEnteringTurn;
				}
				else {
					return PlanningPhase.StartingLane;
				}
			}
			else if (oncomingVehicleExists) {
				laneWidthAtPathEnd = 10;
			}

			BehaviorManager.TraceSource.TraceEvent(TraceEventType.Information, 0, "max speed set to {0}", settings.maxSpeed);

			SmoothAndTrack(commandLabel, true);

			if (cancelled) return PlanningPhase.StartingLane;

			// check if we need to transition to the turn phase for the next iteration
			// if the planning distance is greater than the remaining distance, i.e. we want to plan into the intersection, then 
			//   we want to partially run the turn behavior next iteration
			if (planningDistance > remainingDistance) {
				return PlanningPhase.BeginEnteringTurn;
			}
			else {
				return PlanningPhase.StartingLane;
			}
		}

		private PlanningPhase PlanBeginningTurn(LinePath curStartingLane, LinePath curEndingLane) {
			// figure out where we are along the starting lane path
			LinePath.PointOnPath startingLanePoint = curStartingLane.ZeroPoint;

			// get the distance to the end point of the lane from our current point
			double remainingDistance = curStartingLane.DistanceBetween(startingLanePoint, curStartingLane.EndPoint);

			// get the lane mode for the starting lane
			ILaneModel startingLaneModel = Services.RoadModelProvider.GetLaneModel(curStartingLane, startingLaneWidth + extraWidth, curTimestamp, startingLanesLeft, startingLanesRight);

			double? boundStartDistMin;
			double? boundEndDistMax;
			DetermineSparseStarting(remainingDistance - starting_lane_trim_dist, out boundStartDistMin, out boundEndDistMax);

			// linearize the lane model
			LinePath centerLine, leftBound, rightBound;
			if (boundEndDistMax != null || boundStartDistMin != null) {
				if (boundEndDistMax != null) {
					boundEndDistMax = Math.Min(boundEndDistMax.Value, remainingDistance - starting_lane_trim_dist);
				}
				else {
					boundEndDistMax = remainingDistance - starting_lane_trim_dist;
				}
				LinearizeStayInLane(startingLaneModel, remainingDistance, 0, boundEndDistMax, boundStartDistMin, null, curTimestamp, out centerLine, out leftBound, out rightBound);
			}
			else {
				LinearizeStayInLane(startingLaneModel, remainingDistance, 0, remainingDistance - starting_lane_trim_dist, null, curTimestamp,
					out centerLine, out leftBound, out rightBound);
			}
			//LinearizeLane(curStartingLane, startingLaneWidth, TahoeParams.FL, remainingDistance, 5, remainingDistance - 5, out centerLine, out leftBound, out rightBound);

			// create a copy so when we add to the center line later we don't change this version
			LinePath startingCenterLine = centerLine.Clone();

			// calculate time to collision of opposing obstacle if it exists
			LinePath targetLine = startingCenterLine;
			if (opposingLaneVehicleExists) {
				double timeToCollision = opposingLaneVehicleDist/(Math.Abs(vs.speed)+Math.Abs(opposingLaneVehicleSpeed));
				if (timeToCollision < 3) {
					// shift to the right
					double shiftDist = -TahoeParams.T/2.0;
					targetLine = centerLine.ShiftLateral(shiftDist);
				}
			}

			// set up the planning
			AddTargetPath(targetLine, default_lane_alpha_w);
			if (!sparse || boundStartDistMin != null || boundEndDistMax != null) {
				// add the lane bounds
				AddLeftBound(leftBound, true);
				if (!oncomingVehicleExists) {
					AddRightBound(rightBound, false);
				}
			}

			LinePath endingCenterLine, endingLeftBound, endingRightBound;
			LinearizeLane(curEndingLane, endingLaneWidth, 0, 5, 2, 6, out endingCenterLine, out endingLeftBound, out endingRightBound);

			// add the ending lane center line to the target paths
			AddTargetPath(endingCenterLine, default_lane_alpha_w);
			// add the lane bounds
			AddLeftBound(endingLeftBound, false);
			AddRightBound(endingRightBound, false);
			
			// intert a small portion of the ending lane
			centerLine.AddRange(endingCenterLine);

			// add to the planning setup
			smootherBasePath = centerLine;
			maxSmootherBasePathAdvancePoint = smootherBasePath.GetClosestPoint(startingCenterLine.EndPoint.Location);

			// get the intersection pull path
			LinePath intersectionPullPath = new LinePath();
			double pullWeighting = 0;
			GetIntersectionPullPath(startingCenterLine, endingCenterLine, intersectionPolygon, true, true, intersectionPullPath, ref pullWeighting);

			if (intersectionPullPath.Count > 0) {
				// add as a weighting term
				AddTargetPath(intersectionPullPath, pullWeighting);
			}

			BehaviorManager.TraceSource.TraceEvent(TraceEventType.Verbose, 0, "in stay in lane, center line count: {0}, left bound count: {1}, right bound count: {2}", centerLine.Count, leftBound.Count, rightBound.Count);

			// calculate max speed for the path over the next 120 meters
			double advanceDist = speed_path_advance_dist;
			LinePath.PointOnPath startingLaneEndPoint = curStartingLane.AdvancePoint(startingLanePoint, ref advanceDist);
			LinePath combinedPath = curStartingLane.SubPath(startingLanePoint, startingLaneEndPoint);
			if (advanceDist > 0) {
				Coordinates endingLaneFirstPoint = curEndingLane[0];
				Coordinates startingLaneLastPoint = curStartingLane.EndPoint.Location;
				double intersectionDist = startingLaneLastPoint.DistanceTo(endingLaneFirstPoint);
				double addDist = Math.Max(advanceDist - intersectionDist, 5);

				// add a portion of the ending lane of the appropriate length
				LinePath.PointOnPath endingLanePoint = curEndingLane.AdvancePoint(curEndingLane.StartPoint, addDist);
				int endIndex = endingLanePoint.Index + 1;
				combinedPath.AddRange(curEndingLane.GetSubpathEnumerator(0, endIndex));
			}

			// set the planning settings max speed
			settings.maxSpeed = GetMaxSpeed(combinedPath, combinedPath.StartPoint);
			if (sparse) {
				// limit to 5 mph
				//laneWidthAtPathEnd = 20;
				settings.maxSpeed = Math.Min(settings.maxSpeed, 2.2352);
			}

			BehaviorManager.TraceSource.TraceEvent(TraceEventType.Information, 0, "max speed set to {0}", settings.maxSpeed);

			// set auxillary settings
			settings.endingHeading = centerLine.EndSegment.UnitVector.ArcTan;

			PlanningResult planningResult = Smooth(false);

			if (!planningResult.pathBlocked && !planningResult.dynamicallyInfeasible) {
				AbsoluteTransformer absTransform = Services.StateProvider.GetAbsoluteTransformer(curTimestamp);
				turnBasePath = planningResult.smoothedPath.Transform(absTransform.Invert());
				return PlanningPhase.EnteringTurn;
			}
			else {
				// keep trying I guess
				return PlanningPhase.BeginEnteringTurn;
			}
		}

		private PlanningPhase PlanEnteringTurn(LinePath curStartingLane, LinePath curEndingLane) {
			// figure out where we are along the starting lane path
			LinePath.PointOnPath startingLanePoint = curStartingLane.ZeroPoint;

			// get the distance to the end point of the lane from our current point
			double remainingDistance = curStartingLane.DistanceBetween(startingLanePoint, curStartingLane.EndPoint);

			// immediately switch to the mid turn mode if the remaining distance is less than the front-length
			// this prevents problems with the lane linearization extending out past the end of the lane
			if (remainingDistance < TahoeParams.FL) {
				return PlanMidTurn(curStartingLane, curEndingLane);
			}

			// get the lane mode for the starting lane
			ILaneModel startingLaneModel = Services.RoadModelProvider.GetLaneModel(curStartingLane, startingLaneWidth + extraWidth, curTimestamp, startingLanesLeft, startingLanesRight);

			double? boundStartDistMin;
			double? boundEndDistMax;
			DetermineSparseStarting(remainingDistance - starting_lane_trim_dist, out boundStartDistMin, out boundEndDistMax);

			// linearize the lane model
			LinePath centerLine, leftBound, rightBound;
			if (boundEndDistMax != null || boundStartDistMin != null) {
				if (boundEndDistMax != null) {
					boundEndDistMax = Math.Min(boundEndDistMax.Value, remainingDistance - starting_lane_trim_dist);
				}
				else {
					boundEndDistMax = remainingDistance - starting_lane_trim_dist;
				}
				LinearizeStayInLane(startingLaneModel, remainingDistance, 0, boundEndDistMax, boundStartDistMin, null, curTimestamp, out centerLine, out leftBound, out rightBound);
			}
			else {
				LinearizeStayInLane(startingLaneModel, remainingDistance, 0, remainingDistance - starting_lane_trim_dist, null, curTimestamp,
					out centerLine, out leftBound, out rightBound);
			}

			// create a copy so when we add to the center line later we don't change this version
			LinePath startingCenterLine = centerLine.Clone();

			// calculate time to collision of opposing obstacle if it exists
			LinePath targetLine = startingCenterLine;
			double targetWeight = default_lane_alpha_w;
			if (oncomingVehicleExists) {
				double shiftDist = -(TahoeParams.T + 1);
				targetLine = centerLine.ShiftLateral(shiftDist);
				targetWeight = 0.01;
			}
			if (opposingLaneVehicleExists) {
				double timeToCollision = opposingLaneVehicleDist/(Math.Abs(vs.speed)+Math.Abs(opposingLaneVehicleSpeed));
				Services.Dataset.ItemAs<double>("time to collision").Add(timeToCollision, curTimestamp);
				if (timeToCollision < 5) {
					// shift to the right
					double laneWidth = startingLaneModel.Width;
					double shiftDist = -Math.Min(TahoeParams.T/2.0, laneWidth/2.0 - TahoeParams.T/2.0);
					targetLine = centerLine.ShiftLateral(shiftDist);
				}
			}

			// set up the planning
			AddTargetPath(targetLine, targetWeight);

			// add the lane bounds
			if (!sparse || boundStartDistMin != null || boundEndDistMax != null) {
				// add the lane bounds
				AddLeftBound(leftBound, true);
				if (!oncomingVehicleExists) {
					AddRightBound(rightBound, false);
				}
			}

			LinePath endingCenterLine, endingLeftBound, endingRightBound;
			LinearizeLane(curEndingLane, endingLaneWidth, 0, 5, 2, 6, out endingCenterLine, out endingLeftBound, out endingRightBound);

			// add the ending lane center line to the target paths
			AddTargetPath(endingCenterLine, 0.001);
			// add the lane bounds
			AddLeftBound(endingLeftBound, false);
			AddRightBound(endingRightBound, false);

			// intert a small portion of the ending lane
			centerLine.AddRange(endingCenterLine);

			// get the intersection pull path
			LinePath intersectionPullPath = new LinePath();
			double pullWeighting = 0;
			GetIntersectionPullPath(startingCenterLine, endingCenterLine, intersectionPolygon, true, true, intersectionPullPath, ref pullWeighting);

			if (intersectionPullPath.Count > 0) {
				// add as a weighting term
				AddTargetPath(intersectionPullPath, pullWeighting);
			}

			BehaviorManager.TraceSource.TraceEvent(TraceEventType.Verbose, 0, "in stay in lane, center line count: {0}, left bound count: {1}, right bound count: {2}", centerLine.Count, leftBound.Count, rightBound.Count);

			Services.UIService.PushLineList(centerLine, curTimestamp, "subpath", true);
			Services.UIService.PushLineList(leftBound, curTimestamp, "left bound", true);
			Services.UIService.PushLineList(rightBound, curTimestamp, "right bound", true);

			// calculate max speed for the path over the next 120 meters
			double advanceDist = speed_path_advance_dist;
			LinePath.PointOnPath startingLaneEndPoint = curStartingLane.AdvancePoint(startingLanePoint, ref advanceDist);
			LinePath combinedPath = curStartingLane.SubPath(startingLanePoint, startingLaneEndPoint);
			if (advanceDist > 0) {
				Coordinates endingLaneFirstPoint = curEndingLane[0];
				Coordinates startingLaneLastPoint = curStartingLane.EndPoint.Location;
				double intersectionDist = startingLaneLastPoint.DistanceTo(endingLaneFirstPoint);
				double addDist = Math.Max(advanceDist - intersectionDist, 5);

				// add a portion of the ending lane of the appropriate length
				LinePath.PointOnPath endingLanePoint = curEndingLane.AdvancePoint(curEndingLane.StartPoint, addDist);
				int endIndex = endingLanePoint.Index + 1;
				combinedPath.AddRange(curEndingLane.GetSubpathEnumerator(0, endIndex));
			}
			
			settings.maxSpeed = GetMaxSpeed(combinedPath, combinedPath.StartPoint);
			if (sparse) {
				// limit to 5 mph
				//laneWidthAtPathEnd = 20;
				settings.maxSpeed = Math.Min(settings.maxSpeed, 2.2352);

				LinePath leftEdge = RoadEdge.GetLeftEdgeLine();
				LinePath rightEdge = RoadEdge.GetRightEdgeLine();
				if (leftEdge != null) {
					additionalLeftBounds.Add(new Boundary(leftEdge, 0.1, 1, 100, false));
				}
				if (rightEdge != null) {
					additionalRightBounds.Add(new Boundary(rightEdge, 0.1, 1, 100, false));
				}
			}

			BehaviorManager.TraceSource.TraceEvent(TraceEventType.Information, 0, "max speed set to {0}", settings.maxSpeed);

			// set the avoidance path to be the previously smoothed path
			AbsoluteTransformer absTransform = Services.StateProvider.GetAbsoluteTransformer(curTimestamp);
			avoidanceBasePath = turnBasePath.Transform(absTransform);
			avoidanceBasePath = avoidanceBasePath.SubPath(avoidanceBasePath.ZeroPoint, avoidanceBasePath.EndPoint);
			smootherBasePath = avoidanceBasePath.Clone();
			maxSmootherBasePathAdvancePoint = smootherBasePath.GetClosestPoint(startingCenterLine.EndPoint.Location);

			// set auxillary settings
			settings.endingHeading = centerLine.EndSegment.UnitVector.ArcTan;
			//useAvoidancePath = true;

			SmoothAndTrack(commandLabel, true);

			if (cancelled) return PlanningPhase.EnteringTurn;

			// check if the remaining distance on the starting lane is less than some small tolerance. 
			// if it is, switch to the mid-turn phase
			if (remainingDistance <= TahoeParams.FL) {
				return PlanningPhase.MidTurn;
			}
			else {
				return PlanningPhase.EnteringTurn;
			}
		}

		private PlanningPhase PlanBeginMidTurn(LinePath curStartingLane, LinePath curEndingLane) {
			// get the distance to the first point of the ending lane
			double remainingDistance = Coordinates.Zero.DistanceTo(curEndingLane[0]);
			// get the desired planning distance
			double planningDistance = GetPlanningDistance();

			// calculate the distance of the target lane that we want
			double targetLaneDist = Math.Max(planningDistance - remainingDistance, 5);

			// get the lane model
			ILaneModel endingLaneModel = Services.RoadModelProvider.GetLaneModel(curEndingLane, endingLaneWidth, curTimestamp, endingLanesLeft, endingLanesRight);

			// linearize the lane model
			LinePath centerLine, leftBound, rightBound;
			LinearizeStayInLane(endingLaneModel, targetLaneDist, 0, null, 2, 2, curTimestamp, out centerLine, out leftBound, out rightBound);

			// create a copy
			LinePath endingCenterLine = centerLine.Clone();
			// add as a target path
			AddTargetPath(endingCenterLine, default_lane_alpha_w);
			// add the lane bounds
			AddLeftBound(leftBound, false);
			AddRightBound(rightBound, false);

			// insert our position into the center line path
			centerLine.Insert(0, Coordinates.Zero);
			smootherBasePath = centerLine;

			// get the intersection pull path
			LinePath intersectionPullPath = new LinePath();
			double pullWeighting = 0;
			GetIntersectionPullPath(curStartingLane, endingCenterLine, intersectionPolygon, true, true, intersectionPullPath, ref pullWeighting);

			if (intersectionPullPath.Count > 0) {
				// add as a weighting term
				AddTargetPath(intersectionPullPath, pullWeighting);
			}

			BehaviorManager.TraceSource.TraceEvent(TraceEventType.Verbose, 0, "in stay in lane, center line count: {0}, left bound count: {1}, right bound count: {2}", centerLine.Count, leftBound.Count, rightBound.Count);

			Services.UIService.PushLineList(centerLine, curTimestamp, "subpath", true);
			Services.UIService.PushLineList(leftBound, curTimestamp, "left bound", true);
			Services.UIService.PushLineList(rightBound, curTimestamp, "right bound", true);

			settings.maxSpeed = GetMaxSpeed(null, LinePath.PointOnPath.Invalid);

			BehaviorManager.TraceSource.TraceEvent(TraceEventType.Information, 0, "max speed set to {0}", settings.maxSpeed);

			if (remainingDistance >= 6) {
				// get the ending lane heading
				settings.endingHeading = centerLine.EndSegment.UnitVector.ArcTan;
			}

			PlanningResult planningResult = Smooth(false);

			if (!planningResult.pathBlocked && !planningResult.dynamicallyInfeasible) {
				AbsoluteTransformer absTransform = Services.StateProvider.GetAbsoluteTransformer(curTimestamp);
				turnBasePath = planningResult.smoothedPath.Transform(absTransform.Invert());
				return PlanningPhase.MidTurn;
			}
			else {
				// keep trying I guess
				return PlanningPhase.BeginMidTurn;
			}
		}

		private PlanningPhase PlanMidTurn(LinePath curStartingLane, LinePath curEndingLane) {
			// get the distance to the first point of the ending lane
			double remainingDistance = Coordinates.Zero.DistanceTo(curEndingLane[0]);
			// get the desired planning distance
			double planningDistance = GetPlanningDistance();

			// calculate the distance of the target lane that we want
			double targetLaneDist = Math.Max(planningDistance - remainingDistance, 5);

			// get the lane model
			ILaneModel endingLaneModel = Services.RoadModelProvider.GetLaneModel(curEndingLane, endingLaneWidth, curTimestamp, endingLanesLeft, endingLanesRight);

			// linearize the lane model
			LinePath centerLine, leftBound, rightBound;
			LinearizeStayInLane(endingLaneModel, targetLaneDist, 0, null, 2, 2, curTimestamp, out centerLine, out leftBound, out rightBound);

			// create a copy
			LinePath endingCenterLine = centerLine.Clone();
			// add as a target path
			AddTargetPath(endingCenterLine, 0.001);
			// add the lane bounds
			AddLeftBound(leftBound, false);
			AddRightBound(rightBound, false);

			// insert our position into the center line path
			centerLine.Insert(0, Coordinates.Zero);

			// get the intersection pull path
			LinePath intersectionPullPath = new LinePath();
			double pullWeighting = 0;
			GetIntersectionPullPath(curStartingLane, endingCenterLine, intersectionPolygon, true, true, intersectionPullPath, ref pullWeighting);

			if (intersectionPullPath.Count > 0) {
				// add as a weighting term
				AddTargetPath(intersectionPullPath, pullWeighting);
			}

			BehaviorManager.TraceSource.TraceEvent(TraceEventType.Verbose, 0, "in stay in lane, center line count: {0}, left bound count: {1}, right bound count: {2}", centerLine.Count, leftBound.Count, rightBound.Count);

			Services.UIService.PushLineList(centerLine, curTimestamp, "subpath", true);
			Services.UIService.PushLineList(leftBound, curTimestamp, "left bound", true);
			Services.UIService.PushLineList(rightBound, curTimestamp, "right bound", true);

			// calculate max speed for the path over the next 120 meters
			double advanceDist = speed_path_advance_dist - remainingDistance;
			LinePath combinedPath = new LinePath();
			combinedPath.Add(Coordinates.Zero);
			if (advanceDist > 0) {
				// add the ending lane with the appropriate distance
				combinedPath.AddRange(curEndingLane.GetSubpathEnumerator(0, curEndingLane.AdvancePoint(curEndingLane.StartPoint, advanceDist).Index + 1));
			}
			else {
				// add the first segment of the ending lane
				combinedPath.Add(curEndingLane[0]);
				combinedPath.Add(curEndingLane[1]);
			}
			settings.maxSpeed = GetMaxSpeed(combinedPath, combinedPath.StartPoint);
			settings.maxEndingSpeed = GetMaxSpeed(combinedPath, combinedPath.AdvancePoint(combinedPath.StartPoint, planningDistance));

			BehaviorManager.TraceSource.TraceEvent(TraceEventType.Information, 0, "max speed set to {0}", settings.maxSpeed);

			if (remainingDistance >= 15) {
				// get the ending lane heading
				//settings.endingHeading = centerLine.EndSegment.UnitVector.ArcTan;
			}

			// set the avoidance path to be the previously smoothed path updated to the current time
			AbsoluteTransformer absTransform = Services.StateProvider.GetAbsoluteTransformer(curTimestamp);
			avoidanceBasePath = turnBasePath.Transform(absTransform);
			avoidanceBasePath = avoidanceBasePath.SubPath(avoidanceBasePath.ZeroPoint, avoidanceBasePath.EndPoint);
			//avoidanceBasePath = null;
			smootherBasePath = avoidanceBasePath.Clone();
			maxSmootherBasePathAdvancePoint = smootherBasePath.GetClosestPoint(endingCenterLine.StartPoint.Location);
			//useAvoidancePath = true;

			SmoothAndTrack(commandLabel, true);

			if (cancelled) return PlanningPhase.MidTurn;

			// check if the remaining distance on the starting lane is less than some small tolerance. 
			// if it is, switch to the mid-turn phase
			if (remainingDistance < 6) {
				return PlanningPhase.EndingLane;
			}
			else {
				return PlanningPhase.MidTurn;
			}
		}

		private PlanningPhase PlanEndingLane(LinePath curEndingLane) {
			// get the desired planning distance
			double planningDistance = GetPlanningDistance();

			double remainingDistance = Coordinates.Zero.DistanceTo(curEndingLane.ZeroPoint.Location);
			remainingDistance = Math.Max(0, remainingDistance);

			double laneStartDist = Math.Max(TahoeParams.FL - remainingDistance, 0);
			double boundStartDist = Math.Max(6 - remainingDistance, 2);

			// get the road model
			ILaneModel endingLaneModel = Services.RoadModelProvider.GetLaneModel(curEndingLane, endingLaneWidth, curTimestamp, endingLanesLeft, endingLanesRight);

			// linearize the lane model
			LinePath centerLine, leftBound, rightBound;
			//LinearizeLane(curEndingLane, endingLaneWidth, laneStartDist, planningDistance, boundStartDist, TahoeParams.FL + planningDistance, out centerLine, out leftBound, out rightBound);
			LinearizeStayInLane(endingLaneModel, planningDistance, laneStartDist, null, boundStartDist, curTimestamp, out centerLine, out leftBound, out rightBound);

			// add to the planning setup
			LinePath endingCenterLine = centerLine.Clone();
			AddTargetPath(endingCenterLine, default_lane_alpha_w);
			AddLeftBound(leftBound, true);
			AddRightBound(rightBound, false);

			// insert our position if we didn't include it already
			if (laneStartDist == 0) {
				centerLine.Insert(0, Coordinates.Zero);
			}

			smootherBasePath = centerLine;
			if (remainingDistance > 0) {
				maxSmootherBasePathAdvancePoint = centerLine.GetClosestPoint(endingCenterLine.StartPoint.Location);
			}

			BehaviorManager.TraceSource.TraceEvent(TraceEventType.Verbose, 0, "in stay in lane, center line count: {0}, left bound count: {1}, right bound count: {2}", centerLine.Count, leftBound.Count, rightBound.Count);

			Services.UIService.PushLineList(centerLine, curTimestamp, "subpath", true);
			Services.UIService.PushLineList(leftBound, curTimestamp, "left bound", true);
			Services.UIService.PushLineList(rightBound, curTimestamp, "right bound", true);

			// calculate max speed for the path over the next 60 meters
			LinePath combinedPath = curEndingLane.SubPath(curEndingLane.StartPoint, 120);
			//LaneSpeedHelper.CalculateMaxPathSpeed(combinedPath, combinedPath.StartPoint, combinedPath.EndPoint, ref maxSpeed);
			settings.maxSpeed = GetMaxSpeed(combinedPath, combinedPath.StartPoint);
			settings.maxEndingSpeed = GetMaxSpeed(combinedPath, combinedPath.AdvancePoint(combinedPath.StartPoint, planningDistance));

			BehaviorManager.TraceSource.TraceEvent(TraceEventType.Information, 0, "max speed set to {0}", settings.maxSpeed);

			SmoothAndTrack(commandLabel, true);
			
			return PlanningPhase.EndingLane;
		}

		private void LinearizeLane(LinePath lane, double width, double centerStartDist, double centerEndDist, double boundStartDist, double boundEndDist, out LinePath center, out LinePath leftBound, out LinePath rightBound) {
			// create the center line model
			PathLaneModel laneModel = new PathLaneModel(CarTimestamp.Invalid, new PathRoadModel.LaneEstimate(lane, width, ""));

			// get the center line, left bound and right bound
			LinearizationOptions laneOpts = new LinearizationOptions(centerStartDist, centerEndDist, CarTimestamp.Invalid);
			center = laneModel.LinearizeCenterLine(laneOpts);
			if (center == null || center.Count < 2) {
				BehaviorManager.TraceSource.TraceEvent(TraceEventType.Error, 0, "center line linearization SUCKS");
			}

			laneOpts = new LinearizationOptions(boundStartDist, boundEndDist, CarTimestamp.Invalid);
			leftBound = laneModel.LinearizeLeftBound(laneOpts);
			rightBound = laneModel.LinearizeRightBound(laneOpts);
		}

		public void Dispose() {
			
		}

		protected override bool InIntersection() {
			return (planningPhase == PlanningPhase.MidTurn || planningPhase == PlanningPhase.BeginMidTurn);
		}

		protected override bool IsOffRoad() {
			if (planningPhase == PlanningPhase.StartingLane || planningPhase == PlanningPhase.EnteringTurn || planningPhase == PlanningPhase.BeginEnteringTurn) {
				// update the rndf path
				RelativeTransform relTransfrom = Services.RelativePose.GetTransform(behaviorTimestamp, curTimestamp);
				LinePath curRndfPath = startingLanePath.Transform(relTransfrom);

				LinePath.PointOnPath closestPoint = curRndfPath.ZeroPoint;

				// determine if our heading is off-set from the lane greatly
				LineSegment curSegment = curRndfPath.GetSegment(closestPoint.Index);
				double relativeAngle = Math.Abs(curSegment.UnitVector.ArcTan);

				if (relativeAngle > 30 * Math.PI / 180.0) {
					return true;
				}

				// check the offtrack distance
				if (Math.Abs(closestPoint.OfftrackDistance(Coordinates.Zero)) / (startingLaneWidth / 2.0) > 1) {
					return true;
				}
			}
			return false;
		}

		protected override PlanningBase.BlockageInstructions OnPathBlocked(IList<Obstacle> obstacles) {
			if (sparse && Services.ObstaclePipeline.ExtraSpacing > 0 && (planningPhase == PlanningPhase.StartingLane || planningPhase == PlanningPhase.EnteringTurn)) {
				Services.ObstaclePipeline.ExtraSpacing = Math.Max(0, Services.ObstaclePipeline.ExtraSpacing - 0.05);

				return base.OnPathBlocked(obstacles, false);
			}
			else {
				return base.OnPathBlocked(obstacles);
			}
		}

		protected override PlanningResult OnDynamicallyInfeasible(IList<Obstacle> obstacles, AvoidanceDetails details) {
			if (sparse && smootherSpacingAdjust > -1 && (planningPhase == PlanningPhase.StartingLane || planningPhase == PlanningPhase.EnteringTurn)) {
				smootherSpacingAdjust = Math.Max(-1, smootherSpacingAdjust - 0.05);

				return base.OnDynamicallyInfeasible(obstacles, details, false);
			}
			else {
				return base.OnDynamicallyInfeasible(obstacles, details);
			}
		}

		protected override void OnSmoothSuccess(ref PlanningResult result) {
			if (sparse) {
				if (Services.ObstaclePipeline.ExtraSpacing < 1) {
					Services.ObstaclePipeline.ExtraSpacing = Math.Min(1, Services.ObstaclePipeline.ExtraSpacing + 0.005);
				}

				if (smootherSpacingAdjust < 0) {
					smootherSpacingAdjust = Math.Min(0, smootherSpacingAdjust + 0.001);
				}
			}

			base.OnSmoothSuccess(ref result);
		}

		private void DetermineSparseStarting(double planningDistance, out double? boundStartDistMin, out double? boundEndDistMax) {
			boundStartDistMin = null;
			boundEndDistMax = null;
			if (startingLaneID != null && Services.RoadNetwork != null) {
				ArbiterLane lane = Services.RoadNetwork.ArbiterSegments[startingLaneID.SegmentId].Lanes[startingLaneID];
				AbsolutePose pose = Services.StateProvider.GetAbsolutePose();
				ArbiterLanePartition partition = lane.GetClosestPartition(pose.xy);
				LinePath partitionPath = partition.UserPartitionPath;
				LinePath.PointOnPath closestPoint = partitionPath.GetClosestPoint(pose.xy);
				double remainingDist = planningDistance;
				double totalDist = partitionPath.DistanceBetween(closestPoint, partitionPath.EndPoint);
				remainingDist -= totalDist;

				if (sparse) {
					// walk ahead and determine where sparsity ends
					bool nonSparseFound = false;

					while (remainingDist > 0) {
						// get the next partition
						partition = partition.Final.NextPartition;
						if (partition == null)
							break;

						if (partition.Type != PartitionType.Sparse) {
							nonSparseFound = true;
							break;
						}
						else {
							double dist = partition.UserPartitionPath.PathLength;
							totalDist += dist;
							remainingDist -= dist;
						}
					}

					if (nonSparseFound) {
						boundStartDistMin = totalDist;
					}
				}
				else {
					// determine if there is a sparse segment upcoming
					bool sparseFound = false;
					while (remainingDist > 0) {
						partition = partition.Final.NextPartition;

						if (partition == null) {
							break;
						}

						if (partition.Type == PartitionType.Sparse) {
							sparseFound = true;
							break;
						}
						else {
							double dist = partition.Length;
							totalDist += dist;
							remainingDist -= dist;
						}
					}

					if (sparseFound) {
						boundEndDistMax = totalDist;
						sparse = true;
					}
				}
			}
		}
	}
}
