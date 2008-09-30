using System;
using System.Collections.Generic;
using System.Text;
using OperationalLayer.Obstacles;
using OperationalLayer.Pose;
using UrbanChallenge.Behaviors;
using OperationalLayer.Tracking;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Parking;
using System.Diagnostics;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common;
using UrbanChallenge.Behaviors.CompletionReport;
using OperationalLayer.Tracking.Steering;
using OperationalLayer.Tracking.SpeedControl;
using UrbanChallenge.Common.Sensors;
using UrbanChallenge.Common.Mapack;

namespace OperationalLayer.OperationalBehaviors {
	class ZoneParkingBase : ZoneBase {
		enum PlanningPhase {
			Initial,
			WaitingForClear,
			CoolDown,
			Parking
		}

		private const double steeringRate = 720 * Math.PI / 180;
		private const double parking_speed = 1;
		private const string command_label = "zone parking/normal";

		private static readonly double minRadius = 1.1/Math.Min(Math.Abs(TahoeParams.CalculateCurvature(TahoeParams.SW_max, parking_speed)), Math.Abs(TahoeParams.CalculateCurvature(-TahoeParams.SW_max, parking_speed)));

		private bool pulloutMode = false;

		private LineSegment parkingSpotLine;
		private double parkingSpotExtraDist;
		private LinePath parkingSpotLeftBound;
		private LinePath parkingSpotRightBound;
		private Coordinates pulloutPoint;
		private Line endingLine;

		private bool passCompleted;

		private bool finalPass;

		private ParkerMovingOrder movingOrder;
		private ParkerWrapper parker;

		private PlanningPhase phase;

		private Stopwatch timer;
		private Stopwatch totalStopTimer;
		private Stopwatch totalTimer;

		private double stopDist;
		private CarTimestamp stopTimestamp;

		protected List<Obstacle> GetObstacles(CarTimestamp curTimestamp) {
			ObstacleCollection col = Services.ObstaclePipeline.GetProcessedObstacles(curTimestamp, SAUDILevel.None);
			AbsoluteTransformer transform = Services.StateProvider.GetAbsoluteTransformer(curTimestamp).Invert();

			List<Obstacle> ret = new List<Obstacle>(col.obstacles.Count);
			foreach (Obstacle obs in col.obstacles) {
				Obstacle newObs = obs.ShallowClone();

				if (newObs.cspacePolygon != null) newObs.cspacePolygon = newObs.cspacePolygon.Transform(transform);
				if (newObs.extrudedPolygon != null) newObs.extrudedPolygon = newObs.extrudedPolygon.Transform(transform);
				if (newObs.obstaclePolygon != null) newObs.obstaclePolygon = newObs.obstaclePolygon.Transform(transform);
				if (newObs.predictedPolygon != null) newObs.predictedPolygon = newObs.predictedPolygon.Transform(transform);

				ret.Add(newObs);
			}

			return ret;
		}

		private void HandleBehavior(ZoneParkingBehavior b) {
			base.HandleBaseBehavior(b);

			this.parkingSpotLine = new LineSegment(b.ParkingSpotPath[0], b.ParkingSpotPath[1]);
			this.parkingSpotExtraDist = b.ExtraDistance;
			this.parkingSpotLeftBound = b.SpotLeftBound;
			this.parkingSpotRightBound = b.SpotRightBound;

			if (b is ZoneParkingPullOutBehavior) {
				ZoneParkingPullOutBehavior pb = (ZoneParkingPullOutBehavior)b;
				this.endingLine = new Line(pb.RecommendedPullOutPath[3], pb.RecommendedPullOutPath[2]);
				Coordinates parkPoint = parkingSpotLine.P1 + parkingSpotLine.UnitVector.Normalize(parkingSpotExtraDist);
				this.pulloutPoint = parkPoint + endingLine.UnitVector*10;
				this.pulloutMode = true;
			}
			else {
				this.pulloutMode = false;
			}

			Services.UIService.PushLineList(b.ParkingSpotPath, b.TimeStamp, "original path1", false);

			Services.UIService.PushLineList(parkingSpotLeftBound, b.TimeStamp, "left bound", false);
			Services.UIService.PushLineList(parkingSpotRightBound, b.TimeStamp, "right bound", false);
		}

		public override void OnBehaviorReceived(Behavior b) {
			if ((pulloutMode && b is ZoneParkingPullOutBehavior) || (!pulloutMode && !(b is ZoneParkingPullOutBehavior))) {
				Services.BehaviorManager.QueueParam(b);
			}
			else {
				Services.BehaviorManager.Execute(b, null, false);
			}
		}

		public override void OnTrackingCompleted(TrackingCompletedEventArgs e) {
			// we're done and stuff
			if (e.Command.Label == command_label) {
				passCompleted = true;
			}
		}

		public override string GetName() {
			if (pulloutMode)
				return "ZoneParking Pull-out";
			else
				return "ZoneParking Pull-in";
		}

		public override void Initialize(Behavior b) {
			base.Initialize(b);

			ZoneParkingBehavior cb = (ZoneParkingBehavior)b;

			HandleBehavior(cb);

			this.totalTimer = Stopwatch.StartNew();

			phase = PlanningPhase.Initial;
		}

		public override void Process(object param) {
			curTimestamp = Services.RelativePose.CurrentTimestamp;

			if (totalTimer.Elapsed > TimeSpan.FromMinutes(2)) {
				Type type;
				if (pulloutMode) {
					type = typeof(ZoneParkingPullOutBehavior);
				}
				else {
					type = typeof(ZoneParkingBehavior);
				}
				Services.BehaviorManager.ForwardCompletionReport(new SuccessCompletionReport(type));
				Services.BehaviorManager.Execute(new HoldBrakeBehavior(), null, false);
				return;
			}

			if (phase == PlanningPhase.Initial) {
				if (!pulloutMode && IsPassCompleted()) {
					Type type = typeof(ZoneParkingBehavior);
					Services.BehaviorManager.ForwardCompletionReport(new SuccessCompletionReport(type));
					Services.BehaviorManager.Execute(new HoldBrakeBehavior(), null, false);
					return;
				}

				totalStopTimer = Stopwatch.StartNew();
				timer = Stopwatch.StartNew();

				// determine if there are any moving obstacles within 20 m
				if (CheckAllClear()) {
					phase = PlanningPhase.CoolDown;
				}
				else {
					phase = PlanningPhase.WaitingForClear;
				}
			}
			else if (phase == PlanningPhase.CoolDown) {
				if (CheckAllClear()) {
					if (timer.ElapsedMilliseconds > 2000 || totalStopTimer.ElapsedMilliseconds > 20000) {
						// transition to parking phase
						InitializeParking();
						phase = PlanningPhase.Parking;
						totalStopTimer.Stop();
						timer.Stop();
					}
				}
				else {
					// we're not clear, transition to waiting for clear
					phase = PlanningPhase.WaitingForClear;
					timer.Reset();
					timer.Start();
				}
			}
			else if (phase == PlanningPhase.WaitingForClear) {
				if (CheckAllClear()) {
					// we're all clear to go, transition to cool-down
					phase = PlanningPhase.CoolDown;
					timer.Reset();
					timer.Start();
				}
				else {
					// check if the timer has elapsed
					if (timer.ElapsedMilliseconds > 10000 || totalStopTimer.ElapsedMilliseconds > 20000) {
						InitializeParking();
						phase = PlanningPhase.Parking;
						totalStopTimer.Stop();
						timer.Stop();
					}
				}
			}
			else if (phase == PlanningPhase.Parking) {
				bool reverse = false;
				if (movingOrder != null) {
					reverse = !movingOrder.Forward;
				}
				List<Obstacle> obstacles = GetObstacles(curTimestamp);

				double collisionDist = GetObstacleCollisionDistance(obstacles) - 0.3;
				double feelerDist = CheckFeelerDist(reverse, obstacles) - 0.3;
				double remainingDist = GetRemainingStopDistance();
				if ((!finalPass && collisionDist < remainingDist && collisionDist < 5) || feelerDist < remainingDist) {
					// wait for the stuff to clear

					if (collisionDist < remainingDist) {
						Console.WriteLine("collision violation: cd - {0}, rd {1}", collisionDist, remainingDist);
					}
					if (feelerDist < remainingDist) {
						Console.WriteLine("feeler violation: fd - {0}, rd {1}", feelerDist, remainingDist);
					}

					if (!pulloutMode && IsPassCompleted()) {
						Type type;
						if (pulloutMode) {
							type = typeof(ZoneParkingPullOutBehavior);
						}
						else {
							type = typeof(ZoneParkingBehavior);
						}
						Services.BehaviorManager.ForwardCompletionReport(new SuccessCompletionReport(type));
						Services.BehaviorManager.Execute(new HoldBrakeBehavior(), null, false);
					}

					if (!timer.IsRunning) {
						timer.Start();
					}
					else if (timer.ElapsedMilliseconds > 2000) {
						// re-initialize parking
						InitializeParking();
						timer.Stop();
					}

					Services.TrackingManager.QueueCommand(TrackingCommandBuilder.GetHoldBrakeCommand());
					passCompleted = true;
				}
				else if (passCompleted) {
					timer.Stop();
					timer.Reset();
					ExecutePass();
				}
			}
		}

		private void InitializeParking() {
			// get the corners from sergei's algorithm
			AbsolutePose pose = Services.StateProvider.GetAbsolutePose();
			Coordinates[] rectPoints = null;

			Utilities.ObstacleUtilities.FindParkingInterval(GetObstacles(Services.RelativePose.CurrentTimestamp), parkingSpotLine, zonePerimeter, pose.xy, pose.heading, ref rectPoints);
			Polygon parkingRect = new Polygon(rectPoints);
			Services.UIService.PushPolygon(parkingRect, curTimestamp, "uturn polygon", false);

			// initialize parker
			ParkingSpaceParams spaceParams = new ParkingSpaceParams();
			// bottom left
			spaceParams.FrontWallPoint = rectPoints[0];
			spaceParams.BackWallPoint = rectPoints[2];
			spaceParams.ParkPoint = parkingSpotLine.P1 + parkingSpotLine.UnitVector.Normalize(parkingSpotExtraDist);
			spaceParams.ParkVector = parkingSpotLine.UnitVector;
			spaceParams.PulloutPoint = pulloutPoint;
			parker = new ParkerWrapper(spaceParams);

			Services.UIService.PushPoint(spaceParams.FrontWallPoint, curTimestamp, "front left point", false);
			Services.UIService.PushPoint(spaceParams.BackWallPoint, curTimestamp, "rear right point", false);
			Services.UIService.PushPoint(spaceParams.ParkPoint, curTimestamp, "uturn stop point", false);

			LineList backLine = new LineList();
			backLine.Add(spaceParams.BackWallPoint + parkingSpotLine.UnitVector.Rotate90() * 30);
			backLine.Add(spaceParams.BackWallPoint - parkingSpotLine.UnitVector.Rotate90() * 30);
			Services.UIService.PushLineList(backLine, curTimestamp, "original path2", false);

			stopTimestamp = CarTimestamp.Invalid;
			passCompleted = true;
			finalPass = false;
		}

		private void ExecutePass() {
			// determine what to do

			// get the moving order
			AbsolutePose pose = Services.StateProvider.GetAbsolutePose();
			curTimestamp = pose.timestamp;

			ParkerVehicleState state = new ParkerVehicleState(pose.xy, Coordinates.FromAngle(pose.heading));
			if (pulloutMode) {
				movingOrder = parker.GetNextPulloutOrder(state);
			}
			else {
				movingOrder = parker.GetNextParkingOrder(state);
			}

			// test for completion
			bool isPullinCompleted = false;
			if (parkingSpotLine.P0.DistanceTo(pose.xy) < 1.5 && Coordinates.FromAngle(pose.heading).Dot(parkingSpotLine.UnitVector) >= 0.5) {
				isPullinCompleted = true;
			}

			finalPass = false;
			if (movingOrder != null && movingOrder.DestState != null && parkingSpotLine.P0.DistanceTo(movingOrder.DestState.Loc) < 1.5 && parkingSpotLine.UnitVector.Dot(movingOrder.DestState.Heading) >= 0.5) {
				finalPass = true;
			}

			if (movingOrder.Completed || (isPullinCompleted && !pulloutMode)) {
				Type type;
				if (pulloutMode) {
					type = typeof(ZoneParkingPullOutBehavior);
				}
				else {
					type = typeof(ZoneParkingBehavior);
				}
				Services.BehaviorManager.ForwardCompletionReport(new SuccessCompletionReport(type));
				Services.BehaviorManager.Execute(new HoldBrakeBehavior(), null, false);
			}
			else {
				// determine the stopping distance
				TransmissionGear targetGear;
				if (movingOrder.Forward) {
					targetGear = TransmissionGear.First;
				}
				else {
					targetGear = TransmissionGear.Reverse;
				}

				bool counterClockwise = false;
				if ((movingOrder.Forward && movingOrder.TurningRadius > 0) || (!movingOrder.Forward && movingOrder.TurningRadius < 0)) {
					counterClockwise = true;
				}
				else {
					counterClockwise = false;
				}

				double radius = pose.xy.DistanceTo(movingOrder.CenterPoint);
				double startAngle = (pose.xy - movingOrder.CenterPoint).ArcTan;
				double endAngle = (movingOrder.DestState.Loc - movingOrder.CenterPoint).ArcTan;

				if (counterClockwise) {
					if (endAngle < startAngle) {
						endAngle += 2*Math.PI;
					}
				}
				else {
					if (endAngle > startAngle) {
						endAngle -= 2*Math.PI;
					}
				}

				double stopDistance;
				if (Math.Abs(radius) < 100) {
					stopDistance = Math.Abs(radius * (endAngle - startAngle));
				}
				else {
					stopDistance = pose.xy.DistanceTo(movingOrder.DestState.Loc);
				}

				this.stopDist = stopDistance;
				this.stopTimestamp = curTimestamp;

				Services.UIService.PushPoint(movingOrder.DestState.Loc, curTimestamp, "uturn stop point", false);

				//double sign = movingOrder.Forward ? 1 : -1;
				double steeringCommand = SteeringUtilities.CurvatureToSteeringWheelAngle(1/movingOrder.TurningRadius, parking_speed);
				ISpeedCommandGenerator shiftSpeedCommand = new ShiftSpeedCommand(targetGear);
				ISteeringCommandGenerator initialSteeringCommand = new ConstantSteeringCommandGenerator(steeringCommand, steeringRate, true);

				ISpeedCommandGenerator passSpeedCommand = new FeedbackSpeedCommandGenerator(new StopSpeedGenerator(new TravelledDistanceProvider(curTimestamp, stopDistance), parking_speed));
				ISteeringCommandGenerator passSteeringCommand = new ConstantSteeringCommandGenerator(steeringCommand, null, false);

				ChainedTrackingCommand cmd = new ChainedTrackingCommand(
					new TrackingCommand(shiftSpeedCommand, initialSteeringCommand, true),
					new TrackingCommand(passSpeedCommand, passSteeringCommand, false));
				cmd.Label = command_label;

				Services.TrackingManager.QueueCommand(cmd);
			}

			passCompleted = false;
		}

		private bool IsPassCompleted() {
			AbsolutePose pose = Services.StateProvider.GetAbsolutePose();
			if (parkingSpotLine.P0.DistanceTo(pose.xy) < 1.5 && Coordinates.FromAngle(pose.heading).Dot(parkingSpotLine.UnitVector) >= 0.5) {
				return true;
			}
			else {
				return false;
			}
		}

		private bool CheckAllClear() {
			List<Obstacle> obstacles = GetObstacles(curTimestamp);
			foreach (Obstacle obs in obstacles) {
				if (obs.obstacleClass == ObstacleClass.DynamicCarlike || (obs.obstacleClass == ObstacleClass.DynamicStopped && obs.speed > 1)) {
					// check the range to target
					double dist = GetObstacleDistance(obs.AvoidancePolygon);
					if (dist < 20) return false;
				}
			}

			return true;
		}

		private double GetObstacleDistance(Polygon obs) {
			double minDist = double.MaxValue;
			AbsolutePose pose = Services.StateProvider.GetAbsolutePose();

			for (int i = 0; i < obs.Count; i++) {
				double dist = pose.xy.DistanceTo(obs[i]);
				if (dist < minDist) {
					minDist = dist;
				}
			}

			return minDist;
		}

		private double CheckFeelerDist(bool reverse, List<Obstacle> obstacles) {
			// create a "feeler box" of 3 m in front of the vehicle

			AbsolutePose pose = Services.StateProvider.GetAbsolutePose();

			Polygon feelerPoly = new Polygon(4);
			if (!reverse) {
				feelerPoly.Add(new Coordinates(TahoeParams.FL, (TahoeParams.T/2.0 + 0.5)));
				feelerPoly.Add(new Coordinates(TahoeParams.FL, -(TahoeParams.T/2.0 + 0.5)));
				feelerPoly.Add(new Coordinates(TahoeParams.FL + 2, -(TahoeParams.T/2.0 + 0.25)));
				feelerPoly.Add(new Coordinates(TahoeParams.FL + 2, (TahoeParams.T/2.0 + 0.25)));
			}
			else {
				feelerPoly.Add(new Coordinates(-TahoeParams.RL, -(TahoeParams.T/2.0 + 0.5)));
				feelerPoly.Add(new Coordinates(-TahoeParams.RL, (TahoeParams.T/2.0 + 0.5)));
				feelerPoly.Add(new Coordinates(-TahoeParams.RL - 2, (TahoeParams.T/2.0 + 0.25)));
				feelerPoly.Add(new Coordinates(-TahoeParams.RL - 2, -(TahoeParams.T/2.0 + 0.25)));
			}

			Matrix3 transform = Matrix3.Translation(pose.xy.X, pose.xy.Y)*Matrix3.Rotation(pose.heading);
			feelerPoly = feelerPoly.Transform(transform);

			double minDist = double.MaxValue;

			foreach (Obstacle obs in obstacles) {
				foreach (Coordinates pt in obs.AvoidancePolygon) {
					if (feelerPoly.IsInside(pt)) {
						double dist = pose.xy.DistanceTo(pt);
						if (dist < minDist) {
							minDist = dist;
						}
					}
				}
			}

			if (reverse) {
				return minDist - TahoeParams.RL;
			}
			else {
				return minDist - TahoeParams.FL;
			}
		}

		private double GetObstacleCollisionDistance(IList<Obstacle> obstacles) {
			if (movingOrder == null || movingOrder.DestState == null) return 100;

			AbsolutePose pose = Services.StateProvider.GetAbsolutePose();

			bool counterClockwise = false;
			if ((movingOrder.Forward && movingOrder.TurningRadius > 0) || (!movingOrder.Forward && movingOrder.TurningRadius < 0)) {
				counterClockwise = true;
			}
			else {
				counterClockwise = false;
			}

			CircleSegment rearSegment, frontSegment;
			double frontRadius = Math.Sqrt(movingOrder.TurningRadius*movingOrder.TurningRadius + TahoeParams.FL*TahoeParams.FL);
			Coordinates frontStartPoint = pose.xy + Coordinates.FromAngle(pose.heading)*TahoeParams.FL;
			Coordinates frontEndPoint = movingOrder.DestState.Loc + movingOrder.DestState.Heading.Normalize(TahoeParams.FL);
			if (counterClockwise) {
				rearSegment = new CircleSegment(Math.Abs(movingOrder.TurningRadius), movingOrder.CenterPoint, pose.xy, movingOrder.DestState.Loc, true);
				frontSegment = new CircleSegment(frontRadius, movingOrder.CenterPoint, frontStartPoint, frontEndPoint, true);
			}
			else {
				rearSegment = new CircleSegment(Math.Abs(movingOrder.TurningRadius), movingOrder.CenterPoint, pose.xy, movingOrder.DestState.Loc, false);
				frontSegment = new CircleSegment(frontRadius, movingOrder.CenterPoint, frontStartPoint, frontEndPoint, false);
			}

			Services.Dataset.ItemAs<CircleSegment>("parking path").Add(rearSegment, curTimestamp);

			return Math.Min(TestObstacleCollisionCircle(rearSegment, obstacles), TestObstacleCollisionCircle(frontSegment, obstacles));
		}

		private double TestObstacleCollisionCircle(CircleSegment segment, IList<Obstacle> obstacles) {
			double minDist = double.MaxValue;
			foreach (Obstacle obs in obstacles) {
				Coordinates[] pts;
				if (obs.AvoidancePolygon.Intersect(segment, out pts)) {
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

		private double GetRemainingStopDistance() {
			if (stopTimestamp.IsInvalid) {
				return 0;
			}

			try {
				return stopDist - Services.TrackedDistance.GetDistanceTravelled(stopTimestamp, curTimestamp);
			}
			catch (Exception) {
				// couldn't get the distance travelled, return a big number
				return 100;
			}
		}
	}
}
