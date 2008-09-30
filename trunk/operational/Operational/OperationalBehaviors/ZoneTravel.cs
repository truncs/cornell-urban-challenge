using System;
using System.Collections.Generic;
using System.Text;

using UrbanChallenge.Behaviors;

using UrbanChallenge.Common;
using UrbanChallenge.Common.Sensors;
using UrbanChallenge.Common.Path;

using OperationalLayer.Tracking;
using UrbanChallenge.Common.Pose;
using OperationalLayer.Pose;
using UrbanChallenge.Behaviors.CompletionReport;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Operational.Common;
using UrbanChallenge.Common.Vehicle;
using OperationalLayer.Obstacles;
using OperationalLayer.PathPlanning;
using OperationalLayer.Tracking.Steering;
using System.Diagnostics;

namespace OperationalLayer.OperationalBehaviors {
	class ZoneTravel : ZoneBase {
		private string command_label = "zone travel/normal";
		private string reverse_label = "zone travel/reverse";

		private LinePath recommendedPath;

		private double reverseDist;
		private CarTimestamp reverseTimestamp;

		private bool arcMode = false;

		private double prevCurvature = double.NaN;

		private Stopwatch arcModeTimer;

		public override void OnBehaviorReceived(Behavior b) {
			if (b is ZoneTravelingBehavior) {
				Services.BehaviorManager.QueueParam(b);
			}
			else {
				Services.BehaviorManager.Execute(b, null, false);
			}
		}

		public override void OnTrackingCompleted(TrackingCompletedEventArgs e) {
			// we're done and stuff
			if (e.Command.Label == command_label) {
				// we've finished coming to a stop, cancel the behavior and execute a hold brake
				Services.BehaviorManager.Execute(new HoldBrakeBehavior(), null, true);

				// send a completion report
				Services.BehaviorManager.ForwardCompletionReport(new SuccessCompletionReport(typeof(ZoneTravelingBehavior)));
			}
			else if (e.Command.Label == reverse_label) {
				reverseGear = false;
			}
		}

		public override void Initialize(Behavior b) {
			base.Initialize(b);

			Services.ObstaclePipeline.ExtraSpacing = 0.5;

			ZoneTravelingBehavior cb = (ZoneTravelingBehavior)b;

			HandleBehavior(cb);

			// set up a bogus speed command
			HandleSpeedCommand(recommendedSpeed);
		}

		public override void Process(object param) {
			if (!base.BeginProcess()) {
				return;
			}

			if (param is ZoneTravelingBehavior) {
				HandleBaseBehavior((ZoneTravelingBehavior)param);
			}

			extraObstacles = GetPerimeterObstacles();

			if (reverseGear) {
				ProcessReverse();
				return;
			}

			AbsoluteTransformer absTransform = Services.StateProvider.GetAbsoluteTransformer(curTimestamp);

			// get the vehicle relative path
			LinePath relRecommendedPath = recommendedPath.Transform(absTransform);
			LinePath.PointOnPath zeroPoint = relRecommendedPath.ZeroPoint;

			// get the distance to the end point
			double distToEnd = relRecommendedPath.DistanceBetween(zeroPoint, relRecommendedPath.EndPoint);

			// get the planning distance
			double planningDist = GetPlanningDistance();
			planningDist = Math.Max(planningDist, 20);
			planningDist -= zeroPoint.Location.Length;
			if (planningDist < 2.5)
				planningDist = 2.5;

			if (distToEnd < planningDist) {
				// make the speed command at stop speed command
				behaviorTimestamp = curTimestamp;
				speedCommand = new StopAtDistSpeedCommand(distToEnd - TahoeParams.FL);
				planningDist = distToEnd;
				approachSpeed = recommendedSpeed.Speed;

				settings.endingHeading = relRecommendedPath.EndSegment.UnitVector.ArcTan;
				settings.endingPositionFixed = true;
				settings.endingPositionMax = 2;
				settings.endingPositionMin = -2;
			}
			else {
				speedCommand = new ScalarSpeedCommand(recommendedSpeed.Speed);
			}

			// get the distance of the path segment we care about
			LinePath pathSegment = relRecommendedPath.SubPath(zeroPoint, planningDist);
			double avoidanceDist = planningDist + 5;
			avoidanceBasePath = relRecommendedPath.SubPath(zeroPoint, ref avoidanceDist);
			if (avoidanceDist > 0) {
				avoidanceBasePath.Add(avoidanceBasePath.EndPoint.Location + avoidanceBasePath.EndSegment.Vector.Normalize(avoidanceDist));
			}

			// test if we should clear out of arc mode
			if (arcMode) {
				if (TestNormalModeClear(relRecommendedPath, zeroPoint)) {
					prevCurvature = double.NaN;
					arcMode = false;
				}
			}

			if (Math.Abs(zeroPoint.AlongtrackDistance(Coordinates.Zero)) > 1) {
				pathSegment.Insert(0, Coordinates.Zero);
			}
			else {
				if (pathSegment[0].DistanceTo(pathSegment[1]) < 1) {
					pathSegment.RemoveAt(0);
				}
				pathSegment[0] = Coordinates.Zero;
			}

			if (arcMode) {
				Coordinates relativeGoalPoint = relRecommendedPath.EndPoint.Location;
				ArcVoteZone(relativeGoalPoint, extraObstacles);
				return;
			}

			double pathLength = pathSegment.PathLength;
			if (pathLength < 6) {
				double additionalDist = 6.25 - pathLength;
				pathSegment.Add(pathSegment.EndPoint.Location + pathSegment.EndSegment.Vector.Normalize(additionalDist));
			}

			// determine if polygons are to the left or right of the path
			for (int i = 0; i < zoneBadRegions.Length; i++) {
				Polygon poly = zoneBadRegions[i].Transform(absTransform);

				int numLeft = 0;
				int numRight = 0;
				foreach (LineSegment ls in pathSegment.GetSegmentEnumerator()) {
					for (int j = 0; j < poly.Count; j++) {
						if (ls.IsToLeft(poly[j])) {
							numLeft++;
						}
						else {
							numRight++;
						}
					}
				}

				if (numLeft > numRight) {
					// we'll consider this polygon on the left of the path
					//additionalLeftBounds.Add(new Boundary(poly, 0.1, 0.1, 0));
				}
				else {
					//additionalRightBounds.Add(new Boundary(poly, 0.1, 0.1, 0));
				}
			}

			// TODO: add zone perimeter
			disablePathAngleCheck = false;
			laneWidthAtPathEnd = 7;
			settings.Options.w_diff = 3;
			smootherBasePath = new LinePath();
			smootherBasePath.Add(Coordinates.Zero);
			smootherBasePath.Add(pathSegment.EndPoint.Location);
			AddTargetPath(pathSegment, 0.005);

			settings.maxSpeed = recommendedSpeed.Speed;
			useAvoidancePath = false;

			SmoothAndTrack(command_label, true);
		}

		private void InitializeReverse() {
			// reverse one vehicle length
			reverseDist = TahoeParams.VL;
			reverseTimestamp = curTimestamp;
			prevCurvature = double.NaN;

			reverseGear = true;

			behaviorTimestamp = curTimestamp;
			approachSpeed = 1;
			speedCommand = new StopAtDistSpeedCommand(reverseDist, true);
		}

		private void ProcessReverse() {
			double planningDistance = reverseDist - Services.TrackedDistance.GetDistanceTravelled(reverseTimestamp, curTimestamp);

			// update the rndf path
			AbsoluteTransformer absTransform = Services.StateProvider.GetAbsoluteTransformer(curTimestamp);

			// get the vehicle relative path
			LinePath relRecommendedPath = recommendedPath.Transform(absTransform);
			LinePath targetPath;
			if (relRecommendedPath.ZeroPoint.Location.Length > 10) {
				targetPath = new LinePath();
				targetPath.Add(new Coordinates(0, 0));
				targetPath.Add(new Coordinates(-(planningDistance+TahoeParams.RL+2), 0));
			}
			else {
				// get the path in reverse
				double dist = -(planningDistance + TahoeParams.RL + 2);
				targetPath = relRecommendedPath.SubPath(relRecommendedPath.ZeroPoint, ref dist);
				if (dist < 0) {
					targetPath.Add(relRecommendedPath[0] - relRecommendedPath.GetSegment(0).Vector.Normalize(-dist));
				}
			}

			AddTargetPath(targetPath, 0.005);

			avoidanceBasePath = targetPath;
			double targetDist = Math.Max(targetPath.PathLength-(TahoeParams.RL + 2), planningDistance);
			smootherBasePath = new LinePath();
			smootherBasePath.Add(Coordinates.Zero);
			smootherBasePath.Add(targetPath.AdvancePoint(targetPath.StartPoint, targetDist).Location);

			settings.maxSpeed = recommendedSpeed.Speed;
			settings.Options.reverse = true;
			settings.Options.w_diff = 3;
			laneWidthAtPathEnd = 5;
			useAvoidancePath = false;

			Services.UIService.PushLineList(smootherBasePath, curTimestamp, "subpath", true);

			SmoothAndTrack(reverse_label, true);
		}

		private void HandleBehavior(ZoneTravelingBehavior b) {
			base.HandleBaseBehavior(b);

			this.recommendedPath = b.RecommendedPath;

			Services.UIService.PushLineList(b.RecommendedPath, b.TimeStamp, "original path1", false);
			Services.UIService.PushLineList(null, b.TimeStamp, "original path2", false);

		}

		private bool TestNormalModeClear(LinePath relativePath, LinePath.PointOnPath closestPoint) {
			if (arcModeTimer != null && arcModeTimer.ElapsedMilliseconds < 5000) return false;
			return Math.Abs(closestPoint.OfftrackDistance(Coordinates.Zero)) < 3 && Math.Abs(relativePath.GetSegment(closestPoint.Index).UnitVector.ArcTan) < 45*Math.PI/180.0;
		}

		protected override void OnSmoothSuccess(ref PlanningResult result) {
			if (!(result.pathBlocked || result.dynamicallyInfeasible) && !reverseGear) {
				// check the path, see if the first segment goes 180 off
				if (result.smoothedPath != null && result.smoothedPath.Count >= 2 && Math.Abs(result.smoothedPath.GetSegment(0).UnitVector.ArcTan) > 90*Math.PI/180) {
					result = OnDynamicallyInfeasible(null, null, true);
					Console.WriteLine("turn is 180 deg off, returning infeasible");
					return;
				}
			}

			base.OnSmoothSuccess(ref result);
		}

		protected override void ForwardCompletionReport(CompletionReport report) {
			// we only want to forward success completion reports 
			if (report is SuccessCompletionReport) {
				base.ForwardCompletionReport(report);
			}
			else {
				// otherwise, we have a blockage
				// if it's dynamically infeasible, then we want to do a reverse and try again
				Console.WriteLine("zone trizavel: intercepted blockage report");

				if (reverseGear) {
					Console.WriteLine("in reverse, switch to forward");
					// we're in reverse, switch back to forward
					reverseGear = false;
					lastDynInfeasibleTime = null;
					prevCurvature = double.NaN;
				}
				else {
					if (arcMode) {
						Console.WriteLine("arc mode blocked, switch to reverse");
						reverseGear = true;
						lastDynInfeasibleTime = null;
						prevCurvature = double.NaN;
						InitializeReverse();
					}
					else {
						Console.WriteLine("switch to arc mode");
						arcMode = true;
						lastDynInfeasibleTime = null;
						arcModeTimer = Stopwatch.StartNew();
						prevCurvature = double.NaN;
					}
				}
			}
		}

		private void ArcVoteZone(Coordinates goalPoint, List<Obstacle> perimeterObstacles) {
			List<Polygon> perimeterPolys = new List<Polygon>();
			foreach (Obstacle obs in perimeterObstacles) {
				perimeterPolys.Add(obs.cspacePolygon);
			}

			PlanningResult result;
			ISteeringCommandGenerator steeringCommand = ZoneArcVoting.SparcVote(ref prevCurvature, goalPoint, perimeterPolys);
			if (steeringCommand == null) {
				result = OnDynamicallyInfeasible(null, null);
			}
			else {
				result = new PlanningResult();
				result.commandLabel = command_label;
				result.steeringCommandGenerator = steeringCommand;
			}
			settings.maxSpeed = recommendedSpeed.Speed;
			Track(result, command_label);
		}

		private List<Obstacle> GetPerimeterObstacles() {
			// transform the polygon to relative coordinates
			AbsoluteTransformer absTransform = Services.StateProvider.GetAbsoluteTransformer();

			Polygon relPerimeter = zonePerimeter.Transform(absTransform);
			LinePath relRecommendedPath = recommendedPath.Transform(absTransform);

			if (relPerimeter.IsCounterClockwise) {
				relPerimeter = relPerimeter.Reverse();
			}

			// create a polygon for ourselves and see if we intersect any perimeters
			Polygon vehiclePoly = new Polygon();
			vehiclePoly.Add(new Coordinates(-TahoeParams.RL, -(TahoeParams.T/2.0)));
			vehiclePoly.Add(new Coordinates(TahoeParams.FL, -(TahoeParams.T/2.0)));
			vehiclePoly.Add(new Coordinates(TahoeParams.FL, TahoeParams.T/2.0));
			vehiclePoly.Add(new Coordinates(-TahoeParams.RL, TahoeParams.T/2.0));
			// inflate by about 2 m
			vehiclePoly = vehiclePoly.Inflate(2);

			// test if we intersect any of the perimeter points
			List<Obstacle> perimeterObstacles = new List<Obstacle>();
			List<OperationalObstacle> operationalObstacles = new List<OperationalObstacle>();
			List<LineSegment> segments = new List<LineSegment>();
			foreach (LineSegment ls1 in relPerimeter.GetSegmentEnumerator()) {
				segments.Clear();
				if (ls1.Length > 15) {
					// split into multiple segment
					double targetLength = 10;
					int numSegments = (int)Math.Round(ls1.Length/targetLength);
					double splitLength = ls1.Length/numSegments;

					Coordinates pt = ls1.P0;
					for (int i = 0; i < numSegments; i++) {
						Coordinates endPoint = pt + ls1.Vector.Normalize(splitLength);
						LineSegment seg = new LineSegment(pt, endPoint);
						segments.Add(seg);
						pt = endPoint;
					}
				}
				else {
					segments.Add(ls1);
				}

				foreach (LineSegment ls in segments) {
					bool pathTooClose = false;

					foreach (Coordinates pt in relRecommendedPath) {
						Coordinates closest = ls.ClosestPoint(pt);
						if (closest.DistanceTo(pt) < 1)
							pathTooClose = true;
					}

					if (!vehiclePoly.DoesIntersect(ls) && !pathTooClose) {
						Obstacle obs = CreatePerimeterObstacle(ls);
						perimeterObstacles.Add(obs);

						OperationalObstacle uiobs = new OperationalObstacle();
						uiobs.age = obs.age;
						uiobs.heading = 0;
						uiobs.headingValid = false;
						uiobs.ignored = false;
						uiobs.obstacleClass = obs.obstacleClass;
						uiobs.poly = obs.obstaclePolygon;

						operationalObstacles.Add(uiobs);
					}
				}
			}

			Services.UIService.PushObstacles(operationalObstacles.ToArray(), curTimestamp, "perimeter obstacles", true);

			return perimeterObstacles;
		}

		private Obstacle CreatePerimeterObstacle(LineSegment ls) {
			// perimeter points go clockwise, so shift this segment left to make it go outward
			LineSegment shifted = ls.ShiftLateral(1);

			// create a polygon for the obstacle
			Polygon obstaclePoly = new Polygon();
			obstaclePoly.Add(ls.P0);
			obstaclePoly.Add(ls.P1);
			obstaclePoly.Add(shifted.P1);
			obstaclePoly.Add(shifted.P0);

			Obstacle obs = new Obstacle();
			obs.obstaclePolygon = obstaclePoly;

			Circle tahoeCircle = new Circle(TahoeParams.T/2.0+0.1, Coordinates.Zero);
			Polygon tahoePoly = tahoeCircle.ToPolygon(24);
			obs.cspacePolygon = Polygon.ConvexMinkowskiConvolution(tahoePoly, obstaclePoly);

			obs.obstacleClass = ObstacleClass.StaticLarge;
			obs.desSpacing = 0.5;
			obs.minSpacing = 0.1;
			obs.age = 1;
			obs.trackID = -1;

			return obs;
		}
	}
}
