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

using UrbanChallenge.PathSmoothing;
using SimOperationalService;
using OperationalLayer.Obstacles;
using OperationalLayer.CarTime;
using UrbanChallenge.Operational.Common;

namespace OperationalLayer.OperationalBehaviors {
	class StayInLane : PlanningBase, IDisposable {
		private const string commandLabel = "StayInLane/normal";

		private ArbiterLaneId laneID;

		private LinePath rndfPath;
		private double rndfPathWidth;
		private int numLanesLeft;
		private int numLanesRight;

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
			// check if the transition is valid
			if (b is StayInLaneBehavior) {
				StayInLaneBehavior sb = (StayInLaneBehavior)b;

				// check if the lane id's match
				if (object.Equals(sb.TargetLane, laneID)) {
					// check if the speed command changed
					BehaviorManager.TraceSource.TraceEvent(TraceEventType.Verbose, 0, "received new stay in lane speed command: {0}", sb.SpeedCommand);

					// queue the new speed command to the behavior
					Services.BehaviorManager.QueueParam(sb);
				}
				else {
					BehaviorManager.TraceSource.TraceEvent(TraceEventType.Verbose, 0, "recevied new stay in lane behavior, different target lane: {0}", sb);
					// the lane id's changed
					// re-initialize the behavior 
					Services.BehaviorManager.Execute(b, null, false);
				}
			}
			else {
				BehaviorManager.TraceSource.TraceEvent(TraceEventType.Verbose, 0, "in stay in lane behavior, received {0}", b);
				// this is cool to execute
				Services.BehaviorManager.Execute(b, null, false);
			}
		}

		public override void OnTrackingCompleted(TrackingCompletedEventArgs e) {
			BehaviorManager.TraceSource.TraceEvent(TraceEventType.Information, 0, "in stay in lane behavior, received tracking complete: {0}, result {1}", e.Command, e.Result);
			if (e.Command.Label == commandLabel) {
				// we've finished coming to a stop, cancel the behavior and execute a hold brake
				Services.BehaviorManager.Execute(new HoldBrakeBehavior(), null, true);

				// send a completion report
				Stopwatch timer = Stopwatch.StartNew();
				ForwardCompletionReport(new SuccessCompletionReport(typeof(StayInLaneBehavior)));
				timer.Stop();
				BehaviorManager.TraceSource.TraceEvent(TraceEventType.Verbose, 0, "SendCompletionReport took {0} ms", timer.ElapsedMilliseconds);
			}
		}

		public override void Initialize(Behavior b) {
			base.Initialize(b);

			// extract the stay in lane behavior
			StayInLaneBehavior sb = (StayInLaneBehavior)b;
			
			Services.UIService.PushAbsolutePath(sb.BackupPath, sb.TimeStamp, "original path1");
			Services.UIService.PushAbsolutePath(new LineList(), sb.TimeStamp, "original path2");

			HandleBehavior(sb);
		}

		private void HandleBehavior(StayInLaneBehavior b) {
			AbsoluteTransformer absTransform = Services.StateProvider.GetAbsoluteTransformer(b.TimeStamp);
			this.behaviorTimestamp = absTransform.Timestamp;

			this.rndfPath = b.BackupPath.Transform(absTransform);
			this.rndfPathWidth = b.LaneWidth;
			this.numLanesLeft = b.NumLaneLeft;
			this.numLanesRight = b.NumLanesRight;

			this.laneID = b.TargetLane;

			this.ignorableObstacles = b.IgnorableObstacles;
			Services.ObstaclePipeline.LastIgnoredObstacles = b.IgnorableObstacles;

			HandleSpeedCommand(b.SpeedCommand);

			opposingLaneVehicleExists = false;
			oncomingVehicleExists = false;
			extraWidth = 0;
			if (b.Decorators != null) {
				foreach (BehaviorDecorator d in b.Decorators) {
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
				double timeToCollision = oncomingVehicleDist/Math.Abs(oncomingVehicleSpeed);
				if (oncomingVehicleDist > 30 || timeToCollision > 20 || numLanesRight > 0) {
					oncomingVehicleExists = false;
				}
				else {
					HandleSpeedCommand(oncomingVehicleSpeedCommand);
				}
			}

			if (laneID != null && Services.RoadNetwork != null) {
				ArbiterLane lane = Services.RoadNetwork.ArbiterSegments[laneID.SegmentId].Lanes[laneID];
				AbsolutePose pose = Services.StateProvider.GetAbsolutePose();
				sparse = (lane.GetClosestPartition(pose.xy).Type == PartitionType.Sparse);

				if (sparse){
					LinePath.PointOnPath closest = b.BackupPath.GetClosestPoint(pose.xy);
					goalPoint = b.BackupPath[closest.Index+1];
				}
			}

			Services.UIService.PushAbsolutePath(b.BackupPath, b.TimeStamp, "original path1");
			Services.UIService.PushAbsolutePath(new LineList(), b.TimeStamp, "original path2");

			if (sparse) {
				if (Services.ObstaclePipeline.ExtraSpacing == 0) {
					Services.ObstaclePipeline.ExtraSpacing = 0.5;
				}
				Services.ObstaclePipeline.UseOccupancyGrid = false;
			}
			else {
				Services.ObstaclePipeline.ExtraSpacing = 0;
				Services.ObstaclePipeline.UseOccupancyGrid = true;
				smootherSpacingAdjust = 0;

				prevCurvature = double.NaN;
			}
		}

		public override void Process(object param) {
			// inform the base that we're beginning processing
			if (!BeginProcess()) {
				return;
			}

			// check if we were given a parameter
			if (param != null && param is StayInLaneBehavior) {
				HandleBehavior((StayInLaneBehavior)param);
			}

			if (reverseGear) {
				ProcessReverse();
				return;
			}

			// figure out the planning distance
			double planningDistance = GetPlanningDistance();

			if (sparse && planningDistance > 10) {
				planningDistance = 10;
			}

			double? boundStartDistMin = null;
			double? boundEndDistMax = null;
			if (laneID != null && Services.RoadNetwork != null) {
				ArbiterLane lane = Services.RoadNetwork.ArbiterSegments[laneID.SegmentId].Lanes[laneID];
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

			BehaviorManager.TraceSource.TraceEvent(TraceEventType.Information, 0, "in stay in lane, planning distance {0}", planningDistance);

			// update the rndf path
			RelativeTransform relTransfrom = Services.RelativePose.GetTransform(behaviorTimestamp, curTimestamp);
			LinePath curRndfPath = rndfPath.Transform(relTransfrom);
			ILaneModel centerLaneModel = Services.RoadModelProvider.GetLaneModel(curRndfPath, rndfPathWidth + extraWidth, curTimestamp, numLanesLeft, numLanesRight);

			double avoidanceExtra = sparse ? 5 : 7.5;

			LinePath centerLine, leftBound, rightBound;
			if (boundEndDistMax != null || boundStartDistMin != null) {
				LinearizeStayInLane(centerLaneModel, planningDistance+avoidanceExtra, null, boundEndDistMax, boundStartDistMin, null, curTimestamp, out centerLine, out leftBound, out rightBound);
			}
			else {
				LinearizeStayInLane(centerLaneModel, planningDistance+avoidanceExtra, curTimestamp, out centerLine, out leftBound, out rightBound);
			}
			BehaviorManager.TraceSource.TraceEvent(TraceEventType.Verbose, 0, "in stay in lane, center line count: {0}, left bound count: {1}, right bound count: {2}", centerLine.Count, leftBound.Count, rightBound.Count);

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
					double shiftDist = -TahoeParams.T/2.0;
					targetLine = centerLine.ShiftLateral(shiftDist);
				}
			}

			// set up the planning
			AddTargetPath(targetLine, targetWeight);
			if (!sparse || boundStartDistMin != null || boundEndDistMax != null) {
				AddLeftBound(leftBound, true);
				if (!oncomingVehicleExists) {
					AddRightBound(rightBound, false);
				}
			}


			double targetDist = Math.Max(centerLine.PathLength-avoidanceExtra, planningDistance);
			smootherBasePath = centerLine.SubPath(centerLine.StartPoint, targetDist);
			maxSmootherBasePathAdvancePoint = smootherBasePath.EndPoint;

			double extraDist = (planningDistance+avoidanceExtra)-centerLine.PathLength;
			extraDist = Math.Min(extraDist, 5);

			if (extraDist > 0) {
				centerLine.Add(centerLine[centerLine.Count-1] + centerLine.EndSegment.Vector.Normalize(extraDist));
			}
			avoidanceBasePath = centerLine;

			Services.UIService.PushLineList(centerLine, curTimestamp, "subpath", true);
			Services.UIService.PushLineList(leftBound, curTimestamp, "left bound", true);
			Services.UIService.PushLineList(rightBound, curTimestamp, "right bound", true);

			// get the overall max speed looking forward from our current point
			settings.maxSpeed = GetMaxSpeed(curRndfPath, curRndfPath.AdvancePoint(curRndfPath.ZeroPoint, vs.speed*TahoeParams.actuation_delay));
			// get the max speed at the end point
			settings.maxEndingSpeed = GetMaxSpeed(curRndfPath, curRndfPath.AdvancePoint(curRndfPath.ZeroPoint, planningDistance));
			useAvoidancePath = false;
			if (sparse) {
				// limit to 5 mph
				laneWidthAtPathEnd = 20;
				pathAngleCheckMax = 50;
				pathAngleMax = 5 * Math.PI / 180.0;
				settings.maxSpeed = Math.Min(settings.maxSpeed, 2.2352);
				maxAvoidanceBasePathAdvancePoint = avoidanceBasePath.AdvancePoint(avoidanceBasePath.EndPoint, -2);
				//maxSmootherBasePathAdvancePoint = smootherBasePath.AdvancePoint(smootherBasePath.EndPoint, -2);

				LinePath leftEdge = RoadEdge.GetLeftEdgeLine();
				LinePath rightEdge = RoadEdge.GetRightEdgeLine();
				if (leftEdge != null) {
					leftLaneBounds.Add(new Boundary(leftEdge, 0.1, 1, 100, false));
				}
				if (rightEdge != null) {
					rightLaneBounds.Add(new Boundary(rightEdge, 0.1, 1, 100, false));
				}

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
				return;
			}
			else if (oncomingVehicleExists) {
				laneWidthAtPathEnd = 10;
			}

			BehaviorManager.TraceSource.TraceEvent(TraceEventType.Information, 0, "max speed set to {0}", settings.maxSpeed);

			disablePathAngleCheck = false;

			SmoothAndTrack(commandLabel, true);
		}

		private void ProcessReverse() {
			double planningDistance = GetPlanningDistance();

			// update the rndf path
			RelativeTransform relTransfrom = Services.RelativePose.GetTransform(behaviorTimestamp, curTimestamp);
			LinePath curRndfPath = rndfPath.Transform(relTransfrom);

			Console.WriteLine("cur rndf path count: " + curRndfPath.Count + ", " + curRndfPath.PathLength);
			Console.WriteLine("cur rndf path zero point valid: " + curRndfPath.ZeroPoint.Valid + ", loc: " + curRndfPath.ZeroPoint.Location+ ", index: " + curRndfPath.ZeroPoint.Index);
			Console.WriteLine("planning dist: " + planningDistance + ", stop dist: " + GetSpeedCommandStopDistance());

			// get the path in reverse
			double dist = -(planningDistance + TahoeParams.RL + 2);
			LinePath targetPath = curRndfPath.SubPath(curRndfPath.ZeroPoint, ref dist);
			if (dist < 0) {
				targetPath.Add(curRndfPath[0] - curRndfPath.GetSegment(0).Vector.Normalize(-dist));
			}

			Console.WriteLine("target path count: " + targetPath.Count + ", " + targetPath.PathLength);
			Console.WriteLine("target path zero point valud: " + targetPath.ZeroPoint.Valid);

			// generate a box by shifting the path
			LinePath leftBound = targetPath.ShiftLateral(rndfPathWidth/2.0);
			LinePath rightBound = targetPath.ShiftLateral(-rndfPathWidth/2.0);

			double leftStartDist, rightStartDist;
			GetLaneBoundStartDists(targetPath, rndfPathWidth, out leftStartDist, out rightStartDist);
			leftBound = leftBound.RemoveBefore(leftBound.AdvancePoint(leftBound.StartPoint, leftStartDist));
			rightBound = rightBound.RemoveBefore(rightBound.AdvancePoint(rightBound.StartPoint, rightStartDist));

			AddTargetPath(targetPath, 0.0025);
			AddLeftBound(leftBound, false);
			AddRightBound(rightBound, false);

			avoidanceBasePath = targetPath;
			double targetDist = Math.Max(targetPath.PathLength-(TahoeParams.RL + 2), planningDistance);
			smootherBasePath = targetPath.SubPath(targetPath.StartPoint, targetDist);

			settings.maxSpeed = GetMaxSpeed(null, LinePath.PointOnPath.Invalid);
			settings.endingPositionFixed = true;
			settings.endingPositionMax = rndfPathWidth/2.0;
			settings.endingPositionMin = -rndfPathWidth/2.0;
			settings.Options.reverse = true;

			Services.UIService.PushLineList(smootherBasePath, curTimestamp, "subpath", true);
			Services.UIService.PushLineList(leftBound, curTimestamp, "left bound", true);
			Services.UIService.PushLineList(rightBound, curTimestamp, "right bound", true);

			SmoothAndTrack(commandLabel, true);
		}

		protected override PlanningBase.BlockageInstructions OnPathBlocked(IList<Obstacle> obstacles) {
			if (sparse && Services.ObstaclePipeline.ExtraSpacing > 0) {
				Services.ObstaclePipeline.ExtraSpacing = Math.Max(0, Services.ObstaclePipeline.ExtraSpacing - 0.05);

				return base.OnPathBlocked(obstacles, false);
			}
			else {
				return base.OnPathBlocked(obstacles);
			}
		}

		protected override PlanningResult OnDynamicallyInfeasible(IList<Obstacle> obstacles, AvoidanceDetails details) {
			if (sparse && smootherSpacingAdjust > -1) {
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
					Services.ObstaclePipeline.ExtraSpacing = Math.Min(1, Services.ObstaclePipeline.ExtraSpacing + 0.02);
				}

				if (smootherSpacingAdjust < 0) {
					smootherSpacingAdjust = Math.Min(0, smootherSpacingAdjust + 0.01);
				}
			}

			base.OnSmoothSuccess(ref result);
		}

		protected override bool InSafetyZone() {
			if (laneID != null && Services.RoadNetwork != null) {
				ArbiterLane lane = Services.RoadNetwork.ArbiterSegments[laneID.SegmentId].Lanes[laneID];
				if (lane.SafetyZones != null) {
					AbsolutePose pose = Services.StateProvider.GetAbsolutePose();
					foreach (ArbiterSafetyZone safetyZone in lane.SafetyZones) {
						if (safetyZone.IsInSafety(pose.xy))
							return true;
					}
				}
			}

			return false;
		}

		protected override bool IsOffRoad() {
			// update the rndf path
			RelativeTransform relTransfrom = Services.RelativePose.GetTransform(behaviorTimestamp, curTimestamp);
			LinePath curRndfPath = rndfPath.Transform(relTransfrom);

			LinePath.PointOnPath closestPoint = curRndfPath.ZeroPoint;

			// determine if our heading is off-set from the lane greatly
			LineSegment curSegment = curRndfPath.GetSegment(closestPoint.Index);
			double relativeAngle = Math.Abs(curSegment.UnitVector.ArcTan);

			if (relativeAngle > 30 * Math.PI / 180.0) {
				return true;
			}

			// check the offtrack distance
			if (Math.Abs(closestPoint.OfftrackDistance(Coordinates.Zero)) / (rndfPathWidth / 2.0) > 1) {
				return true;
			}

			return false;
		}

		protected override void ForwardCompletionReport(CompletionReport report) {
			if (reverseGear && report is TrajectoryBlockedReport) {
				// just because we're assholes, replace the blockage with a success when we're in reverse and
				// execute a hold brake
				Services.BehaviorManager.Execute(new HoldBrakeBehavior(), null, true);
				base.ForwardCompletionReport(new SuccessCompletionReport(typeof(StayInLaneBehavior)));
			}
			else {
				base.ForwardCompletionReport(report);
			}
		}

		#region IDisposable Members

		public void Dispose() {
		}

		#endregion
	}
}
