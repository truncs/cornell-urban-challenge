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
using OperationalLayer.Obstacles;

namespace OperationalLayer.OperationalBehaviors {
	class UTurn : IOperationalBehavior {
		private enum UTurnPass {
			Initializing,
			Forward,
			Backward,
			Final
		}

		private const string forwardLabel = "UTurn/forward";
		private const string backwardLabel = "UTurn/backward";

		private const double steeringRate = 720 * Math.PI / 180;
		private const double uturnSpeed = 1.7;
		private static readonly double minRadius = 1.1/Math.Min(Math.Abs(TahoeParams.CalculateCurvature(TahoeParams.SW_max, uturnSpeed)), Math.Abs(TahoeParams.CalculateCurvature(-TahoeParams.SW_max, uturnSpeed)));

		private Coordinates originalPoint;
		private Polygon polygon;
		private LineSegment finalOrientation;
		private CarTimestamp polygonTimestamp;

		private ArbiterLaneId finalLane;
		private SpeedCommand finalSpeedCommand;

		private double stopDistance;
		private CarTimestamp stopTimestamp;
		private double curvature;

		private UTurnPass pass;
		private bool passCompleted;

		private bool stopOnLine;
		private bool checkMode;

		private bool cancelled;

		private List<Polygon> stayOutPolygons;
		
		#region IOperationalBehavior Members

		public string GetName() {
			return "UTurn";
		}

		private void HandleBehavior(UTurnBehavior cb) {
			// get the absolute transform
			AbsoluteTransformer absTransform = Services.StateProvider.GetAbsoluteTransformer(cb.TimeStamp);

			this.polygonTimestamp = absTransform.Timestamp;
			this.polygon = cb.Boundary.Transform(absTransform);
			this.finalLane = cb.EndingLane;
			this.finalSpeedCommand = cb.EndingSpeedCommand;
			this.stopOnLine = cb.StopOnEndingPath;
			this.stayOutPolygons = cb.StayOutPolygons;

			// run constant checking if we're supposed to stop on the final line
			if (stopOnLine) {
				checkMode = true;
			}
			else {
				checkMode = false;
			}

			// transform the path
			LinePath.PointOnPath closestPoint = cb.EndingPath.GetClosestPoint(originalPoint);
			// relativize the path
			LinePath relFinalPath = cb.EndingPath.Transform(absTransform);
			// get the ending orientation
			finalOrientation = new LineSegment(relFinalPath[closestPoint.Index], relFinalPath[closestPoint.Index+1]);

			Services.UIService.PushLineList(cb.EndingPath, cb.TimeStamp, "original path1", false);
		}

		public void OnBehaviorReceived(Behavior b) {
			if (b is UTurnBehavior) {
				UTurnBehavior cb = (UTurnBehavior)b;

				Services.BehaviorManager.QueueParam(cb);
			}
			else {
				Services.BehaviorManager.Execute(b, null, false);
			}
		}

		public void OnTrackingCompleted(TrackingCompletedEventArgs e) {
			passCompleted = true;
		}

		public void Initialize(Behavior b) {
			UTurnBehavior cb = (UTurnBehavior)b;

			Services.ObstaclePipeline.ExtraSpacing = 0;
			Services.ObstaclePipeline.UseOccupancyGrid = true;

			// get the absolute state at the time of the behavior
			AbsolutePose absPose = Services.StateProvider.GetAbsolutePose(cb.TimeStamp);
			originalPoint = absPose.xy;

			HandleBehavior(cb);

			this.passCompleted = true;
			this.pass = UTurnPass.Initializing;

			this.cancelled = false;
		}

		public void Process(object param) {
			if (cancelled) return;

			if (!passCompleted && !checkMode)
				return;

			if (param != null && param is UTurnBehavior) {
				HandleBehavior((UTurnBehavior)param);
			}

			ITrackingCommand trackingCommand = null;

			if (pass == UTurnPass.Initializing) {
				pass = CheckInitialAlignment();
			}
			else if (CheckFinalExit()) {
				pass = UTurnPass.Final;
			}

			// determine which pass we just finished
			if (pass == UTurnPass.Backward) {
				if (passCompleted) {
					// build the forward pass and run it
					bool finalPass;
					trackingCommand = BuildForwardPass(out finalPass);

					if (finalPass)
						pass = UTurnPass.Final;
					else
						pass = UTurnPass.Forward;

					passCompleted = false;
				}
				else if (checkMode) {
					CarTimestamp curTimestamp = Services.RelativePose.CurrentTimestamp;
					double remainingDist = GetRemainingStopDistance(curTimestamp);
					double collisionDist = GetObstacleCollisionDistance(false, curvature, remainingDist, GetObstacles(curTimestamp));
					if (collisionDist < remainingDist && collisionDist < 3) {
						Services.TrackingManager.QueueCommand(TrackingCommandBuilder.GetHoldBrakeCommand());
						passCompleted = true;
					}
				}
			}
			else if (pass == UTurnPass.Forward) {
				if (passCompleted) {
					trackingCommand = BuildBackwardPass();
					pass = UTurnPass.Backward;
					passCompleted = false;
				}
				else if (checkMode) {
					CarTimestamp curTimestamp = Services.RelativePose.CurrentTimestamp;
					double remainingDist = GetRemainingStopDistance(curTimestamp);
					double collisionDist = GetObstacleCollisionDistance(true, curvature, remainingDist, GetObstacles(curTimestamp));
					if (collisionDist < remainingDist && collisionDist < 3) {
						Services.TrackingManager.QueueCommand(TrackingCommandBuilder.GetHoldBrakeCommand());
						passCompleted = true;
					}
				}
			}
			else if (pass == UTurnPass.Final) {
				// execute a stay in lane behavior
				Services.BehaviorManager.Execute(new StayInLaneBehavior(finalLane, finalSpeedCommand, null), null, false);
				passCompleted = false;

				Services.BehaviorManager.ForwardCompletionReport(new SuccessCompletionReport(typeof(UTurnBehavior)));
			}

			if (trackingCommand != null) {
				Services.TrackingManager.QueueCommand(trackingCommand);
			}
		}

		public void Cancel() {
			cancelled = true;
		}

		#endregion

		/// <summary>
		/// Return true if we're approximately aligned with the final lane
		/// </summary>
		/// <returns></returns>
		private UTurnPass CheckInitialAlignment() {
			// rotate the polygon into the current relative frame
			CarTimestamp curTimestamp = Services.RelativePose.CurrentTimestamp;
			RelativeTransform relTransform = Services.RelativePose.GetTransform(polygonTimestamp, curTimestamp);
			LineSegment finalLine = finalOrientation.Transform(relTransform);
			double finalAngle = finalLine.UnitVector.ArcTan;
			if (finalAngle < 15*Math.PI/180.0 && finalAngle > -30*Math.PI/180.0) {
				// we can just execute a stay in lane
				return UTurnPass.Final;
			}
			else if (finalAngle <= -30*Math.PI/180.0 && finalAngle >= -90*Math.PI/180.0) {
				// want to plan a backward pass, so indicate that we just completed a forward pass
				return UTurnPass.Forward;
			}
			else {
				return UTurnPass.Backward;
			}
		}

		private bool CheckFinalExit() {
			// rotate the polygon into the current relative frame
			CarTimestamp curTimestamp = Services.RelativePose.CurrentTimestamp;
			RelativeTransform relTransform = Services.RelativePose.GetTransform(polygonTimestamp, curTimestamp);
			LineSegment finalLine = finalOrientation.Transform(relTransform);

			double finalAngle = finalLine.UnitVector.ArcTan;
			if (Math.Abs(finalAngle) < 15*Math.PI/180.0 && finalLine.ClosestPoint(Coordinates.Zero).Length < 2) {
				// we can just execute a stay in lane
				return true;
			}

			return false;
		}

		private ITrackingCommand BuildForwardPass(out bool finalPass) {
			BehaviorManager.TraceSource.TraceEvent(TraceEventType.Information, 0, "UTurn Behavior: Determine Forward Pass");

			// rotate the polygon into the current relative frame
			CarTimestamp curTimestamp = Services.RelativePose.CurrentTimestamp;
			RelativeTransform relTransform = Services.RelativePose.GetTransform(polygonTimestamp, curTimestamp);
			relTransform.TransformPointsInPlace(polygon);
			finalOrientation = finalOrientation.Transform(relTransform);
			polygonTimestamp = curTimestamp;

			// retrieve the vehicle state
			Coordinates headingVec = new Coordinates(1, 0);
			Coordinates headingVec90 = headingVec.Rotate90();

			// check if we can make it out now
			Line curLine = new Line(new Coordinates(0,0), headingVec);
			Coordinates intersectionPoint;
			LineSegment finalLine = finalOrientation;
			Circle outCircle = Circle.FromLines(curLine, (Line)finalLine, out intersectionPoint);

			double steeringCommand;

			if (!outCircle.Equals(Circle.Infinite) && outCircle.r > minRadius) {
				// we found an out circle
				BehaviorManager.TraceSource.TraceEvent(TraceEventType.Information, 0, "found final pass output");

				// build the circle segment 
				double hitAngle = (intersectionPoint - outCircle.center).ArcTan;
				double startAngle = (-outCircle.center).ArcTan;
				if (hitAngle < startAngle)
					hitAngle += 2*Math.PI;

				hitAngle -= startAngle;

				if (stopOnLine) {
					// get the angle of the end point of the line segment
					double endPointAngle = (finalLine.P1 - outCircle.center).ArcTan;
					if (endPointAngle < startAngle) endPointAngle += 2*Math.PI;
					endPointAngle -= startAngle;

					if (endPointAngle < hitAngle) {
						hitAngle = endPointAngle;
					}
				}

				// get the obstacles
				Coordinates frontLeftPoint = headingVec * TahoeParams.FL + headingVec90 * TahoeParams.T / 2;
				Coordinates frontRightPoint = headingVec * TahoeParams.FL - headingVec90 * TahoeParams.T / 2.0;
				Coordinates rearRightPoint = -headingVec * TahoeParams.RL - headingVec90 * TahoeParams.T / 2.0;
				List<Polygon> obstacles = GetObstacles(curTimestamp);
				GetObstacleHitAngle(frontLeftPoint, outCircle.center, obstacles, ref hitAngle);
				GetObstacleHitAngle(frontRightPoint, outCircle.center, obstacles, ref hitAngle);
				GetObstacleHitAngle(rearRightPoint, outCircle.center, obstacles, ref hitAngle);

				// calculate stopping distance
				stopDistance = outCircle.r*hitAngle;
				stopTimestamp = curTimestamp;
				curvature = 1/outCircle.r;

				intersectionPoint = outCircle.center + Coordinates.FromAngle(startAngle + hitAngle)*outCircle.r;

				// calculate steering angle
				steeringCommand = SteeringUtilities.CurvatureToSteeringWheelAngle(1/outCircle.r, uturnSpeed);

				// mark that this will be the final pass
				finalPass = true;

				Services.UIService.PushCircle(outCircle, curTimestamp, "uturn circle", true);
				Services.UIService.PushPoint(intersectionPoint, curTimestamp, "uturn stop point", true);
				Services.UIService.PushPolygon(polygon, curTimestamp, "uturn polygon", true);
			}
			else {
				finalPass = false;
				// draw out 3 circles
				//  - front right wheel
				//  - front left wheel
				//  - rear left wheel

				// figure out center point of turn
				Circle rearAxleCircle = Circle.FromPointSlopeRadius(Coordinates.Zero, headingVec, minRadius);
				Coordinates center = rearAxleCircle.center;

				// calculate the points of the wheels
				Coordinates rearLeftPoint = headingVec90*TahoeParams.T/2;
				Coordinates rearRightPoint = -headingVec90*TahoeParams.T/2;
				Coordinates frontLeftPoint = headingVec*TahoeParams.L + headingVec90*TahoeParams.T/2;
				Coordinates frontRightPoint = headingVec*TahoeParams.L - headingVec90*TahoeParams.T/2;

				// initialize min hit angle to slightly less than 90 degrees
				double minHit = Math.PI/2.1;
				//GetMinHitAngle(rearLeftPoint, center, true, ref minHit);
				//GetMinHitAngle(rearRightPoint, center, true, ref minHit);
				GetMinHitAngle(frontLeftPoint, center, ref minHit);
				GetMinHitAngle(frontRightPoint, center, ref minHit);

				// get the obstacles
				List<Polygon> obstacles = GetObstacles(curTimestamp);
				frontLeftPoint = headingVec*TahoeParams.FL + headingVec90*TahoeParams.T/2;
				frontRightPoint = headingVec*TahoeParams.FL - headingVec90*TahoeParams.T/2.0;
				rearRightPoint = -headingVec*TahoeParams.RL - headingVec90*TahoeParams.T/2.0;
				GetObstacleHitAngle(frontLeftPoint, center, obstacles, ref minHit);
				GetObstacleHitAngle(frontRightPoint, center, obstacles, ref minHit);
				GetObstacleHitAngle(rearRightPoint, center, obstacles, ref minHit);

				// trim some off the hit for safety
				//if (minHit > 0.5/minRadius)
				minHit -= (0.5/minRadius);
				minHit = Math.Max(minHit, 0.6 / minRadius);

				double startAngle = (Coordinates.Zero-center).ArcTan;
				double hitAngle = startAngle + minHit;

				// set the stopping point at the min hit point
				Coordinates stopPoint = rearAxleCircle.GetPoint(hitAngle);

				// calculate the stop distance
				stopDistance = minRadius*minHit;

				// calculate the required steering angle
				steeringCommand = SteeringUtilities.CurvatureToSteeringWheelAngle(1/minRadius, uturnSpeed);

				Services.UIService.PushCircle(new Circle(minRadius, center), curTimestamp, "uturn circle", true);
				Services.UIService.PushPoint(stopPoint, curTimestamp, "uturn stop point", true);
				Services.UIService.PushPolygon(polygon, curTimestamp, "uturn polygon", true);
			}

			// build the command
			ISpeedCommandGenerator shiftSpeedCommand = new ShiftSpeedCommand(TransmissionGear.First);
			ISteeringCommandGenerator initialSteeringCommand = new ConstantSteeringCommandGenerator(steeringCommand, steeringRate, true);

			ISpeedCommandGenerator passSpeedCommand = new FeedbackSpeedCommandGenerator(new StopSpeedGenerator(new TravelledDistanceProvider(curTimestamp, stopDistance), uturnSpeed));
			ISteeringCommandGenerator passSteeringCommand = new ConstantSteeringCommandGenerator(steeringCommand, null, false);

			ChainedTrackingCommand cmd = new ChainedTrackingCommand(
				new TrackingCommand(shiftSpeedCommand, initialSteeringCommand, true),
				new TrackingCommand(passSpeedCommand, passSteeringCommand, false));
			cmd.Label = forwardLabel;

			return cmd;
		}

		private ITrackingCommand BuildBackwardPass() {
			BehaviorManager.TraceSource.TraceEvent(TraceEventType.Information, 0, "UTurn Behavior: Initialize Backward Pass");

			// rotate the polygon into the current relative frame
			CarTimestamp curTimestamp = Services.RelativePose.CurrentTimestamp;
			RelativeTransform relTransform = Services.RelativePose.GetTransform(polygonTimestamp, curTimestamp);
			relTransform.TransformPointsInPlace(polygon);
			finalOrientation = finalOrientation.Transform(relTransform);
			polygonTimestamp = curTimestamp;

			// retrieve the vehicle state
			Coordinates headingVec = new Coordinates(1, 0);
			Coordinates headingVec180 = headingVec.Rotate180();
			Coordinates headingVec90 = headingVec.Rotate90();

			// figure out center point of turn
			Circle rearAxleCircle = Circle.FromPointSlopeRadius(new Coordinates(0,0), headingVec180, minRadius);
			Coordinates center = rearAxleCircle.center;

			// calculate the points of the wheels
			Coordinates rearLeftPoint = headingVec90*TahoeParams.T/2;
			Coordinates rearRightPoint = -headingVec90*TahoeParams.T/2;
			Coordinates frontLeftPoint = headingVec*TahoeParams.L + headingVec90*TahoeParams.T/2;
			Coordinates frontRightPoint = headingVec*TahoeParams.L - headingVec90*TahoeParams.T/2;

			double minHit = Math.PI/2.1;
			GetMinHitAngle(rearLeftPoint, center, ref minHit);
			GetMinHitAngle(rearRightPoint, center, ref minHit);
			//GetMinHitAngle(frontLeftPoint, center, false, ref minHit);
			//GetMinHitAngle(frontRightPoint, center, false, ref minHit);

			
			frontLeftPoint = headingVec*TahoeParams.FL + headingVec90*TahoeParams.T/2;
			frontRightPoint = headingVec*TahoeParams.FL - headingVec90*TahoeParams.T/2.0;
			rearRightPoint = -headingVec*TahoeParams.RL - headingVec90*TahoeParams.T/2.0;
			rearLeftPoint = -headingVec*TahoeParams.RL + headingVec90*TahoeParams.T/2.0;
			List<Polygon> obstacles = GetObstacles(curTimestamp);
			GetObstacleHitAngle(frontLeftPoint, center, obstacles, ref minHit);
			GetObstacleHitAngle(rearLeftPoint, center, obstacles, ref minHit);
			GetObstacleHitAngle(rearRightPoint, center, obstacles, ref minHit);

			// trim some off the hit for safety'
			minHit -= (0.3/minRadius);
			// move at least 0.6 meters
			minHit = Math.Max(minHit, 0.6 / minRadius);

			// calculate the exit stopping point
			// shift the line by the minimum turning radius
			Coordinates u = finalOrientation.P1 - finalOrientation.P0;
			u = u.Normalize().Rotate90();
			Line offsetLine = new Line();
			offsetLine.P0 = finalOrientation.P0 + u*(minRadius+2);
			offsetLine.P1 = finalOrientation.P1 + u*(minRadius+2);

			// final the intersection of the current turn circle with a radius of twice the min turn radius and the offset line
			Circle twoTurn = new Circle(2*minRadius + 2, center);
			Coordinates[] intersections;

			double startAngle = (-center).ArcTan;
			if (twoTurn.Intersect(offsetLine, out intersections)) {
				// figure out where there were hits
				for (int i = 0; i < intersections.Length; i++) {
					// get the angle of the hit
					double angle = (intersections[i] - center).ArcTan;

					if (angle < startAngle)
						angle += 2*Math.PI;

					angle -= startAngle;

					if (angle < minHit)
						minHit = angle;
				}
			}

			minHit = Math.Max(minHit, 0.6 / minRadius);

			// set the stopping point at the min hit point
			Coordinates stopPoint = rearAxleCircle.GetPoint(startAngle+minHit);

			// calculate the stop distance
			stopDistance = rearAxleCircle.r*minHit;
			stopTimestamp = curTimestamp;
			curvature = 1/minRadius;
			// calculate the required steering angle
			double steeringCommand = SteeringUtilities.CurvatureToSteeringWheelAngle(-1/minRadius, uturnSpeed);

			ISpeedCommandGenerator shiftSpeedCommand = new ShiftSpeedCommand(TransmissionGear.Reverse);
			ISteeringCommandGenerator initialSteeringCommand = new ConstantSteeringCommandGenerator(steeringCommand, steeringRate, true);

			ISpeedCommandGenerator passSpeedCommand = new FeedbackSpeedCommandGenerator(new StopSpeedGenerator(new TravelledDistanceProvider(curTimestamp, stopDistance), uturnSpeed));
			ISteeringCommandGenerator passSteeringCommand = new ConstantSteeringCommandGenerator(steeringCommand, null, false);

			ChainedTrackingCommand cmd = new ChainedTrackingCommand(
				new TrackingCommand(shiftSpeedCommand, initialSteeringCommand, true),
				new TrackingCommand(passSpeedCommand, passSteeringCommand, false));
			cmd.Label = backwardLabel;

			Services.UIService.PushCircle(new Circle(minRadius, center), curTimestamp, "uturn circle", true);
			Services.UIService.PushPoint(stopPoint, curTimestamp, "uturn stop point", true);
			Services.UIService.PushPolygon(polygon, curTimestamp, "uturn polygon", true);

			return cmd;
		}

		private void GetMinHitAngle(Coordinates startPt, Coordinates center, ref double minAngle) {
			// build the circle
			Circle circle = new Circle(startPt.DistanceTo(center), center);
			Coordinates[] hits = null;
			if (polygon.Intersect(circle, out hits)) {
				double startAngle = (startPt - center).ArcTan;

				foreach (Coordinates hit in hits) {
					double angle = (hit - center).ArcTan;

					if (angle <= startAngle)
						angle += 2*Math.PI;

					angle -= startAngle;

					if (angle < minAngle)
						minAngle = angle;

				}
			}
		}

		private List<Polygon> GetObstacles(CarTimestamp curTimestamp) {
			int total = 0;

			ObstacleCollection obstacles = Services.ObstaclePipeline.GetProcessedObstacles(curTimestamp, Services.BehaviorManager.SAUDILevel);
			total += obstacles.obstacles.Count;

			if (stayOutPolygons != null) 
				total += stayOutPolygons.Count;

			List<Polygon> polys = new List<Polygon>(total);

			foreach (Obstacle obs in obstacles.obstacles) {
				polys.Add(obs.AvoidancePolygon);
			}
			
			// transform the stay-out polygons
			if (stayOutPolygons != null) {
				AbsoluteTransformer absTransform = Services.StateProvider.GetAbsoluteTransformer(curTimestamp);
				foreach (Polygon poly in stayOutPolygons) {
					polys.Add(poly.Transform(absTransform));
				}
			}

			return polys;
		}

		private void GetObstacleHitAngle(Coordinates startPt, Coordinates center, IEnumerable<Polygon> obstacles, ref double minAngle) {
			// build the circle
			Circle circle = new Circle(startPt.DistanceTo(center), center);
			Coordinates[] hits = null;
			foreach (Polygon poly in obstacles) {
				if (poly.Intersect(circle, out hits)) {
					double startAngle = (startPt - center).ArcTan;

					foreach (Coordinates hit in hits) {
						double angle = (hit - center).ArcTan;

						if (angle <= startAngle)
							angle += 2*Math.PI;

						angle -= startAngle;

						if (angle < minAngle)
							minAngle = angle;

					}
				}
			}
		}

		private double GetObstacleCollisionDistance(bool forward, double curvature, double remainingDistance, IList<Polygon> obstacles) {
			double radius = 1/Math.Abs(curvature);
			Coordinates centerPoint;
			if (forward) {
				centerPoint = new Coordinates(0, radius);
			}
			else {
				centerPoint = new Coordinates(0, -radius);
			}

			CircleSegment rearSegment, frontSegment;
			double frontRadius = Math.Sqrt(radius*radius + TahoeParams.FL*TahoeParams.FL);
			Coordinates frontStartPoint = new Coordinates(TahoeParams.FL, 0);

			rearSegment = new CircleSegment(radius, centerPoint, Coordinates.Zero, remainingDistance+2, true);
			frontSegment = new CircleSegment(frontRadius, centerPoint, frontStartPoint, remainingDistance+2, true);

			Services.Dataset.ItemAs<CircleSegment>("parking path").Add(rearSegment, Services.RelativePose.CurrentTimestamp);

			return Math.Min(TestObstacleCollisionCircle(rearSegment, obstacles), TestObstacleCollisionCircle(frontSegment, obstacles));
		}

		private double TestObstacleCollisionCircle(CircleSegment segment, IList<Polygon> obstacles) {
			double minDist = double.MaxValue;
			foreach (Polygon obs in obstacles) {
				Coordinates[] pts;
				if (obs.Intersect(segment, out pts)) {
					for (int i = 0; i < pts.Length; i++) {
						// get the distance from the start
						double dist = segment.DistFromStart(pts[i]);
						if (dist < minDist) {
							minDist = dist;
						}
					}
				}
			}

			return minDist - 0.5;
		}

		private double GetRemainingStopDistance(CarTimestamp curTimestamp) {
			if (stopTimestamp.IsInvalid) {
				return 0;
			}

			try {
				return stopDistance - Services.TrackedDistance.GetDistanceTravelled(stopTimestamp, curTimestamp);
			}
			catch (Exception) {
				// couldn't get the distance travelled, return a big number
				return 100;
			}
		}
	}
}