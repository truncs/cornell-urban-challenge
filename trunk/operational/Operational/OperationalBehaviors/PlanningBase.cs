using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using UrbanChallenge.Common;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common.Pose;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Behaviors;
using UrbanChallenge.PathSmoothing;
using UrbanChallenge.Operational.Common;

using OperationalLayer.Pose;
using OperationalLayer.CarTime;
using OperationalLayer.Tracking;
using OperationalLayer.Tracking.Steering;
using OperationalLayer.Tracking.SpeedControl;
using OperationalLayer.RoadModel;
using OperationalLayer.Obstacles;
using OperationalLayer.PathPlanning;
using UrbanChallenge.Behaviors.CompletionReport;
using UrbanChallenge.Common.Sensors;
using UrbanChallenge.Common.Utility;

namespace OperationalLayer.OperationalBehaviors {
	abstract class PlanningBase : IOperationalBehavior {
		protected enum SmootherInstructions {
			RunNormalSmoothing,
			UseSmootherSpeedCommands,
			UsePreviousPath,
			UseCommandGenerator
		}

		protected class BlockageInstructions {
			public SmootherInstructions smootherInstructions;
			public ISpeedCommandGenerator speedCommand;
			public ISteeringCommandGenerator steeringCommand;
			public bool pathBlocked;

			public string commandLabel;
		}

		/// <summary>
		/// Target deceleration in m/s^2 to use when calculating maximum speed
		/// </summary>
		protected const double target_decel = 0.5;
		/// <summary>
		/// Distance to advance along the path when determining max speed
		/// </summary>
		protected const double speed_path_advance_dist = 120;

		/// <summary>
		/// Target deceleration in m/s^2 to use when calculating planning distance
		/// </summary>
		private const double planning_dist_decel = 2;
		/// <summary>
		/// Time in seconds to use for the system latency when calculating planning distance
		/// </summary>
		private const double planning_dist_sys_latency = 0.5;

		/// <summary>
		/// Minimum planning distance in m
		/// </summary>
		private const double planning_dist_min = 15;
		/// <summary>
		/// Maximum planning distance in m
		/// </summary>
		private const double planning_dist_max = 30;

		private const double stopped_decel_target = 1.5;
		private const double stopped_spacing_dist = TahoeParams.VL*1.5;

		private const double default_lane_min_spacing = 0.05;
		private const double default_lane_des_spacing = 0.1;
		private const double default_lane_alpha_s = 0.1;

		protected const double default_lane_alpha_w = 0.000025;

		private const double blockage_dynamic_tol = 1;

		protected LinePath prevSmoothedPath;
		protected CarTimestamp prevSmoothedPathTimestamp;

		// path planning options
		protected PathPlanner planner;
		protected PlanningSettings settings;
		protected LinePath smootherBasePath;
		protected LinePath.PointOnPath maxSmootherBasePathAdvancePoint;
		protected LinePath avoidanceBasePath;
		protected LinePath.PointOnPath maxAvoidanceBasePathAdvancePoint;
		protected List<Boundary> smootherTargetPaths;
		protected List<Boundary> leftLaneBounds;
		protected List<Boundary> rightLaneBounds;
		protected List<Boundary> additionalLeftBounds;
		protected List<Boundary> additionalRightBounds;
		protected List<Obstacle> extraObstacles;
		protected bool disablePathAngleCheck;
		protected double pathAngleCheckMax;
		protected double pathAngleMax;
		protected bool useAvoidancePath;
		protected double laneWidthAtPathEnd;
		protected double smootherSpacingAdjust;
		protected bool sparse;

		protected List<int> ignorableObstacles = new List<int>();
		protected CarTimestamp behaviorTimestamp;
		protected SpeedCommand speedCommand;
		protected static double? approachSpeed;
		protected bool reverseGear;

		protected DateTime? lastDynInfeasibleTime;
		protected DateTime? lastBlockageTime;
		protected bool lastBlockageDynamic;

		protected CarTimestamp curTimestamp;

		protected bool cancelled;

		protected OperationalVehicleState vs;

		#region IOperationalBehavior Members

		public abstract void OnBehaviorReceived(Behavior b);

		public abstract void OnTrackingCompleted(TrackingCompletedEventArgs e);

		public virtual void Initialize(Behavior b) {
			cancelled = false;

			planner = new PathPlanner();

			curTimestamp = Services.RelativePose.CurrentTimestamp;
		}

		public abstract void Process(object param);

		protected void HandleSpeedCommand(SpeedCommand cmd) {
			string speedCommandString = null;
			this.speedCommand = cmd;
			reverseGear = false;
			if (cmd is ScalarSpeedCommand) {
				Services.Dataset.ItemAs<double>("scalar speed").Add(((ScalarSpeedCommand)cmd).Speed, curTimestamp);

				speedCommandString = string.Format("Scalar Speed ({0:F1} m/s)", ((ScalarSpeedCommand)speedCommand).Speed);
			}
			else if (cmd is StopAtDistSpeedCommand) {
				StopAtDistSpeedCommand stopCmd = (StopAtDistSpeedCommand)cmd;
				if (stopCmd.Reverse) {
					reverseGear = true;
				}
				else {
					reverseGear = false;
				}

				speedCommandString = string.Format("Stop At Dist ({0}{1:F1} m)", reverseGear ? "rev " : "", stopCmd.Distance);
			}
			else if (cmd is StopAtLineSpeedCommand) {
				speedCommandString = string.Format("Stop At Line ({0:F1} m)", Services.Stopline.DistanceToStopline());
			}

			if (speedCommandString != null) {
				Services.Dataset.ItemAs<string>("speed command").Add(speedCommandString, curTimestamp);
			}
			Services.Dataset.ItemAs<double>("commanded stop distance").Add(GetSpeedCommandStopDistance(), curTimestamp);
		}

		public virtual void Cancel() {
			cancelled = true;
		}

		public virtual string GetName() {
			return this.GetType().Name;
		}

		#endregion

		protected bool BeginProcess() {
			if (cancelled)
				return false;

			curTimestamp = Services.RelativePose.CurrentTimestamp;

			vs = Services.StateProvider.GetVehicleState();

			// check if we're currently in drive
			bool transBypassed = Services.Dataset.ItemAs<bool>("trans bypassed").CurrentValue;
			if (!Settings.TestMode && !transBypassed && ((!reverseGear && !vs.IsInDrive) || (reverseGear && vs.transGear != TransmissionGear.Reverse))) {
				BehaviorManager.TraceSource.TraceEvent(TraceEventType.Information, 0, "shifting to drive");
				// execute a shift command
				Services.TrackingManager.QueueCommand(TrackingCommandBuilder.GetShiftTransmissionCommand(reverseGear ? TransmissionGear.Reverse : TransmissionGear.First, null, null, false));
				// don't process anything else
				return false;
			}

			InitializePlanningSettings();

			// return true to indicate that processing should continue
			return true;
		}

		protected void InitializePlanningSettings() {
			settings = new PlanningSettings();
			smootherBasePath = null;
			maxSmootherBasePathAdvancePoint = LinePath.PointOnPath.Invalid;
			avoidanceBasePath = null;
			maxAvoidanceBasePathAdvancePoint = LinePath.PointOnPath.Invalid;
			smootherTargetPaths = new List<Boundary>();
			leftLaneBounds = new List<Boundary>();
			rightLaneBounds = new List<Boundary>();
			additionalLeftBounds = new List<Boundary>();
			additionalRightBounds = new List<Boundary>();
			extraObstacles = new List<Obstacle>();
			disablePathAngleCheck = false;
			pathAngleCheckMax = 20;
			pathAngleMax = 15 * Math.PI / 180.0;
			useAvoidancePath = false;
			laneWidthAtPathEnd = 3.5;

			settings.Options.alpha_w = 0.00025;
			settings.Options.alpha_d = 10;
			settings.Options.alpha_c = 10;
		}

		protected void AddTargetPath(LinePath targetPath, double alpha_w) {
			smootherTargetPaths.Add(new Boundary(targetPath, alpha_w));
		}

		protected void AddLeftBound(LinePath targetPath, bool checkFrontBumper) {
			leftLaneBounds.Add(new Boundary(targetPath, default_lane_min_spacing, default_lane_des_spacing, default_lane_alpha_s, false));
		}

		protected void AddRightBound(LinePath targetPath, bool checkFrontBumper) {
			rightLaneBounds.Add(new Boundary(targetPath, default_lane_min_spacing, default_lane_des_spacing, default_lane_alpha_s, false));
		}

		protected virtual double GetMaxSpeed(LinePath rndfPath, LinePath.PointOnPath startingPoint) {
			double maxSpeed;
			if (speedCommand is ScalarSpeedCommand) {
				maxSpeed = Math.Min(((ScalarSpeedCommand)speedCommand).Speed, 15);

				// calculate the swerving max speed 
				double deltaSteeringSigma = Services.TrackingManager.DeltaSteeringStdDev*180/Math.PI;

				if (deltaSteeringSigma > 3) {
					double swerveMax = Math.Max(18 - 0.2*deltaSteeringSigma, 2.5);
					maxSpeed = Math.Min(maxSpeed, swerveMax);
				}

				approachSpeed = null;
			}
			else {
				// assume that it's a stop at dist or stop at line
				// either way, we do the same thing
				if (approachSpeed == null || (double)approachSpeed < 1) {
					approachSpeed = Math.Max(vs.speed, 1);
					BehaviorManager.TraceSource.TraceEvent(TraceEventType.Information, 0, "approach speed set to {0}", approachSpeed.Value);
				}
				maxSpeed = approachSpeed.Value;
			}

			if (rndfPath != null) {
				// get the angles
				List<Pair<int, double>> angles = rndfPath.GetIntersectionAngles(startingPoint.Index, rndfPath.Count-1);

				foreach (Pair<int, double> angleValue in angles) {
					// calculate the desired speed to take the point
					double dist = Math.Max(rndfPath.DistanceBetween(startingPoint, rndfPath.GetPointOnPath(angleValue.Left))-TahoeParams.FL, 0);
					// calculate the desired speed at the intersection
					double desiredSpeed = 1.5/(angleValue.Right/(Math.PI/2));
					// limit the desired speed to 2.5
					desiredSpeed = Math.Max(2.5, desiredSpeed);
					// calculate the speed we would be allowed to go right now
					double desiredMaxSpeed = Math.Sqrt(desiredSpeed*desiredSpeed + 2*target_decel*dist);
					// check if the desired max speed is lower
					if (desiredMaxSpeed < maxSpeed) {
						maxSpeed = desiredMaxSpeed;
					}
				}
			}

			return maxSpeed;
		}

		protected double GetPlanningDistance() {
			if (speedCommand is ScalarSpeedCommand) {
				// figure out the commanded speed and plan for the stopping distance
				double commandedSpeed = ((ScalarSpeedCommand)speedCommand).Speed;

				// calculate expected stopping distance (actual deceleration at 2 m/s^2 + system latency + tahoe rear-axle to front-bumper length)
				double planningDist = commandedSpeed*commandedSpeed/(2*planning_dist_decel) + commandedSpeed*planning_dist_sys_latency + TahoeParams.FL;
				if (planningDist < planning_dist_min) {
					planningDist = planning_dist_min;
				}
				else if (planningDist > planning_dist_max) {
					planningDist = planning_dist_max;
				}

				return planningDist;
			}
			else if (speedCommand is StopAtDistSpeedCommand) {
				// transform the distance to the current timestamp
				double origDist = ((StopAtDistSpeedCommand)speedCommand).Distance;
				double remainingDist = origDist - Services.TrackedDistance.GetDistanceTravelled(behaviorTimestamp, curTimestamp);
				if (remainingDist < 0) {
					remainingDist = 0;
				}
				return remainingDist + TahoeParams.FL;
			}
			else if (speedCommand is StopAtLineSpeedCommand) {
				double remainingDist = Services.Stopline.DistanceToStopline();
				return remainingDist + TahoeParams.FL;
			}
			else {
				throw new InvalidOperationException();
			}
		}

		protected double GetSpeedCommandStopDistance() {
			if (speedCommand is ScalarSpeedCommand) {
				return double.PositiveInfinity;
			}
			else if (speedCommand is StopAtDistSpeedCommand) {
				// transform the distance to the current timestamp
				double origDist = ((StopAtDistSpeedCommand)speedCommand).Distance;
				double remainingDist = origDist - Services.TrackedDistance.GetDistanceTravelled(behaviorTimestamp, curTimestamp);
				if (remainingDist < 0) {
					remainingDist = 0;
				}
				return remainingDist;
			}
			else if (speedCommand is StopAtLineSpeedCommand) {
				double remainingDist = Services.Stopline.DistanceToStopline();
				return remainingDist;
			}

			return double.PositiveInfinity;
		}

		protected virtual void SmoothAndTrack(string commandLabel, bool doAvoidance) {
			PlanningResult result = Smooth(doAvoidance);
			Track(result, commandLabel);
		}

		protected void Track(PlanningResult planningResult, string commandLabel) {
			Services.Dataset.ItemAs<bool>("route feasible").Add(!planningResult.pathBlocked, LocalCarTimeProvider.LocalNow);

			if (planningResult.smoothedPath != null && !planningResult.dynamicallyInfeasible) {
				prevSmoothedPath = new LinePath(planningResult.smoothedPath);
				prevSmoothedPathTimestamp = curTimestamp;
			}

			if (planningResult.smoothedPath != null) {
				Services.UIService.PushLineList(prevSmoothedPath, curTimestamp, "smoothed path", true);
			}

			if (cancelled) return;

			ISpeedCommandGenerator speedCommandGenerator = planningResult.speedCommandGenerator;
			ISteeringCommandGenerator steeringCommandGenerator = planningResult.steeringCommandGenerator;

			if (speedCommandGenerator == null) {
				// we've planned out the path, now build up the command
				ISpeedGenerator speedGenerator = null;
				if (speedCommand is ScalarSpeedCommand) {
					// extract the max speed from the planning settings
					double maxSpeed = settings.maxSpeed;

					if (planningResult.smoothedPath != null) {
						SmoothedPath path = planningResult.smoothedPath;
						maxSpeed = Math.Min(maxSpeed, PostProcessSpeed(path));
					}

					speedGenerator = new ConstantSpeedGenerator(maxSpeed, null);
				}
				else if (speedCommand is StopAtDistSpeedCommand) {
					StopAtDistSpeedCommand stopCommand = (StopAtDistSpeedCommand)speedCommand;
					IDistanceProvider distProvider = new TravelledDistanceProvider(behaviorTimestamp, stopCommand.Distance);
					speedGenerator = new StopSpeedGenerator(distProvider, approachSpeed.Value);

					BehaviorManager.TraceSource.TraceEvent(TraceEventType.Verbose, 0, "stay in lane - remaining stop stop dist {0}", distProvider.GetRemainingDistance());

				}
				else if (speedCommand is StopAtLineSpeedCommand) {
					IDistanceProvider distProvider = new StoplineDistanceProvider();
					speedGenerator = new StopSpeedGenerator(distProvider, approachSpeed.Value);

					BehaviorManager.TraceSource.TraceEvent(TraceEventType.Verbose, 0, "stay in lane - remaining stop stop dist {0}", distProvider.GetRemainingDistance());
				}
				else if (speedCommand == null) {
					throw new InvalidOperationException("Speed command is null");
				}
				else {
					throw new InvalidOperationException("Speed command " + speedCommand.GetType().FullName + " is not supported");
				}

				speedCommandGenerator = new FeedbackSpeedCommandGenerator(speedGenerator);
			}

			if (cancelled) return;

			// build up the command
			TrackingCommand trackingCommand = new TrackingCommand(speedCommandGenerator, steeringCommandGenerator, false);
			trackingCommand.Label = planningResult.commandLabel ?? commandLabel;

			// queue it to execute
			Services.TrackingManager.QueueCommand(trackingCommand);
			
		}

		protected PlanningResult Smooth(bool doAvoidance) {
			double curSpeed = vs.speed;

			Services.Dataset.ItemAs<double>("extra spacing").Add(Services.ObstaclePipeline.ExtraSpacing, curTimestamp);
			Services.Dataset.ItemAs<double>("smoother spacing adjust").Add(smootherSpacingAdjust, curTimestamp);
			
			// get the tracking manager to predict stuff like whoa
			AbsolutePose absPose;
			OperationalVehicleState vehicleState;
			double averageCycleTime = Services.BehaviorManager.AverageCycleTime;
			Services.TrackingManager.ForwardPredict(averageCycleTime, out absPose, out vehicleState);
			Services.Dataset.ItemAs<double>("planning cycle time").Add(averageCycleTime, LocalCarTimeProvider.LocalNow);
			settings.initHeading = absPose.heading;

			smootherBasePath = ReplaceFirstPoint(smootherBasePath, maxSmootherBasePathAdvancePoint, absPose.xy);
			if (avoidanceBasePath != null && avoidanceBasePath.Count > 0) {
				avoidanceBasePath = ReplaceFirstPoint(avoidanceBasePath, maxAvoidanceBasePathAdvancePoint, absPose.xy);
			}
			else {
				avoidanceBasePath = smootherBasePath;
			}

			if (smootherBasePath.EndPoint.Location.Length > 80) {
				return OnDynamicallyInfeasible(null, null);
			}

			Services.UIService.PushRelativePath(smootherBasePath, curTimestamp, "subpath2");

			IList<Obstacle> obstacles = null;
			if (doAvoidance && Settings.DoAvoidance) {
				// get the obstacles predicted to the current timestamp
				ObstacleCollection obs = Services.ObstaclePipeline.GetProcessedObstacles(curTimestamp, Services.BehaviorManager.SAUDILevel);
				if (obs != null)
					obstacles = obs.obstacles;
			}

			if (extraObstacles != null && extraObstacles.Count > 0) {
				if (obstacles == null) {
					obstacles = extraObstacles;
				}
				else {
					foreach (Obstacle obs in extraObstacles) {
						obstacles.Add(obs);
					}
				}
			}

			// start the planning timer
			Stopwatch planningTimer = Stopwatch.StartNew();

			List<Boundary> leftSmootherBounds  = new List<Boundary>();
			List<Boundary> rightSmootherBounds = new List<Boundary>();

			leftSmootherBounds.AddRange(leftLaneBounds);
			rightSmootherBounds.AddRange(rightLaneBounds);

			leftSmootherBounds.AddRange(additionalLeftBounds);
			rightSmootherBounds.AddRange(additionalRightBounds);

			BlockageInstructions blockageInstructions = new BlockageInstructions();
			blockageInstructions.smootherInstructions = SmootherInstructions.RunNormalSmoothing;

			bool pathBlocked = false;

			// check if there are any obstacles
			if (obstacles != null && obstacles.Count > 0) {
				// label the obstacles as ignored
				if (ignorableObstacles != null) {
					if (ignorableObstacles.Count == 1 && ignorableObstacles[0] == -1) {
						for (int i = 0; i < obstacles.Count; i++) {
							if (obstacles[i].obstacleClass == ObstacleClass.DynamicCarlike || obstacles[i].obstacleClass == ObstacleClass.DynamicStopped) {
								obstacles[i].ignored = true;
							}
						}
					}
					else {
						ignorableObstacles.Sort();

						for (int i = 0; i < obstacles.Count; i++) {
							if (obstacles[i].trackID != -1 && ignorableObstacles.BinarySearch(obstacles[i].trackID) >= 0) {
								obstacles[i].ignored = true;
							}
						}
					}
				}

				// we need to do the full obstacle avoidance
				// execute the obstacle manager
				LinePath avoidancePath;
				bool success;
				List<LinePath> shiftedLeftBounds = new List<LinePath>(leftLaneBounds.Count);
				foreach (Boundary bnd in leftLaneBounds) {
					LinePath lp = (LinePath)bnd.Coords;
					if (lp.Count >= 2) {
						shiftedLeftBounds.Add(lp.ShiftLateral(-(TahoeParams.T)/2.0+0.25));
					}
				}

				List<LinePath> shiftedRightBounds = new List<LinePath>(rightLaneBounds.Count);
				foreach (Boundary bnd in rightLaneBounds) {
					LinePath lp = (LinePath)bnd.Coords;
					if (lp.Count >= 2) {
						shiftedRightBounds.Add(lp.ShiftLateral((TahoeParams.T)/2.0-0.25));
					}
				}

				try {
					Services.ObstacleManager.ProcessObstacles(avoidanceBasePath, shiftedLeftBounds, shiftedRightBounds,
																										obstacles, laneWidthAtPathEnd, reverseGear, sparse,
																										out avoidancePath, out success);
				}
				catch (OutOfMemoryException ex) {
					Console.WriteLine("out of memory exception at " + ex.StackTrace);
					return OnDynamicallyInfeasible(obstacles, null);
				}

				if (!success) {
					// build out the distance
					DetermineBlockageDistancesAndDeceleration(obstacles, avoidanceBasePath);

					// call the on blocked stuff
					blockageInstructions = OnPathBlocked(obstacles);

					pathBlocked = blockageInstructions.pathBlocked;
				}

				if (blockageInstructions.smootherInstructions == SmootherInstructions.RunNormalSmoothing || blockageInstructions.smootherInstructions == SmootherInstructions.UseSmootherSpeedCommands) {
					// build the boundary lists
					// sort out obstacles as left and right
					double obstacleAlphaS = 100;
					int totalObstacleClusters = obstacles.Count;
					for (int i = 0; i < totalObstacleClusters; i++) {
						if (obstacles[i].ignored) continue;

						double minSpacing = Math.Max(obstacles[i].minSpacing + smootherSpacingAdjust, 0);
						double desSpacing = Math.Max(obstacles[i].desSpacing + smootherSpacingAdjust, 0);

						if (obstacles[i].avoidanceStatus == AvoidanceStatus.Left) {
							Boundary bound = new Boundary(obstacles[i].AvoidancePolygon, minSpacing, desSpacing, obstacleAlphaS);
							bound.CheckFrontBumper = true;
							leftSmootherBounds.Add(bound);
						}
						else if (obstacles[i].avoidanceStatus == AvoidanceStatus.Right) {
							Boundary bound = new Boundary(obstacles[i].AvoidancePolygon, minSpacing, desSpacing, obstacleAlphaS);
							bound.CheckFrontBumper = true;
							rightSmootherBounds.Add(bound);
						}
					}
				}

				// we could possibly replace the smoother base with the avoidance path or we can adjust the smoother base path
				// appropriately
				if (success && useAvoidancePath) {
					smootherBasePath = avoidancePath;
				}

				Services.UIService.PushLineList(avoidancePath, curTimestamp, "avoidance path", true);
			}

			PlanningResult planningResult = null;

			if (blockageInstructions.smootherInstructions == SmootherInstructions.RunNormalSmoothing || blockageInstructions.smootherInstructions == SmootherInstructions.UseSmootherSpeedCommands
				|| (blockageInstructions.smootherInstructions == SmootherInstructions.UsePreviousPath && prevSmoothedPath == null)) {
				// initialize settings that we're making easier for the derived classes
				settings.basePath = smootherBasePath;
				settings.targetPaths = smootherTargetPaths;
				settings.leftBounds = leftSmootherBounds;
				settings.rightBounds = rightSmootherBounds;
				settings.startSpeed = curSpeed;
				settings.timestamp = curTimestamp;

				PathPlanner.SmoothingResult result = planner.PlanPath(settings);

				if (result.result != SmoothResult.Sucess) {
					planningResult = OnDynamicallyInfeasible(obstacles, result.details);

					if (blockageInstructions.speedCommand != null) {
						planningResult.speedCommandGenerator = blockageInstructions.speedCommand;
					}
				}
				else {
					// build out the command
					planningResult = new PlanningResult();
					planningResult.pathBlocked = pathBlocked;
					planningResult.smoothedPath = result.path;

					if (blockageInstructions.smootherInstructions == SmootherInstructions.UseSmootherSpeedCommands) {
						planningResult.speedCommandGenerator = new FeedbackSpeedCommandGenerator(result.path);
					}
					else if (blockageInstructions.speedCommand != null) {
						planningResult.speedCommandGenerator = blockageInstructions.speedCommand;
					}

					if (blockageInstructions.smootherInstructions == SmootherInstructions.UseCommandGenerator) {
						planningResult.steeringCommandGenerator = blockageInstructions.steeringCommand;
					}
					else {
						planningResult.steeringCommandGenerator = new PathSteeringCommandGenerator(result.path);
					}
				}
			}
			else if (blockageInstructions.smootherInstructions == SmootherInstructions.UsePreviousPath){
				// transform the previously smoothed path into the current interval
				RelativeTransform transform = Services.RelativePose.GetTransform(prevSmoothedPathTimestamp, curTimestamp);
				SmoothedPath prevPath = new SmoothedPath(curTimestamp, prevSmoothedPath.Transform(transform), null);

				planningResult = new PlanningResult();
				planningResult.speedCommandGenerator = blockageInstructions.speedCommand;
				planningResult.smoothedPath = prevPath;
				planningResult.pathBlocked = pathBlocked;
				planningResult.steeringCommandGenerator = new PathSteeringCommandGenerator(prevPath);
			}
			else if (blockageInstructions.smootherInstructions == SmootherInstructions.UseCommandGenerator) {
				planningResult = new PlanningResult();
				planningResult.speedCommandGenerator = blockageInstructions.speedCommand;
				planningResult.steeringCommandGenerator = blockageInstructions.steeringCommand;
				planningResult.pathBlocked = pathBlocked;
				planningResult.smoothedPath = null;
			}

			planningTimer.Stop();

			BehaviorManager.TraceSource.TraceEvent(TraceEventType.Verbose, 0, "planning took {0} ms", planningTimer.ElapsedMilliseconds);

			planningResult.commandLabel = blockageInstructions.commandLabel;

			if (!planningResult.pathBlocked && !planningResult.dynamicallyInfeasible) {
				OnSmoothSuccess(ref planningResult);
			}

			return planningResult;
		}

		private double PostProcessSpeed(SmoothedPath path) {
			const double a_lat_max = 1.5;
			const double lat_target_decel = 1;

			double maxSpeed = 20;

			// return an astronomical speed if there are no curvatures
			if (path.Count < 3)
				return maxSpeed;

			// calculate the curvature at each point
			// we want the lateral acceleration to be no more than a_lat_max, so calculate the speed we would need to be going now achieve that deceleration
			double dist = 0;
			
			for (int i = 1; i < path.Count-1; i++) {
				dist += path[i-1].DistanceTo(path[i]);

				// lateral acceleration = v^2*curvature
				double curvature = path.GetCurvature(i);
				double desiredSpeed = Math.Sqrt(a_lat_max/Math.Abs(curvature));
				double desiredMaxSpeed = Math.Sqrt(desiredSpeed*desiredSpeed + 2*lat_target_decel*dist);
				if (desiredMaxSpeed < maxSpeed) {
					maxSpeed = desiredMaxSpeed;
				}
			}

			return maxSpeed;
		}

		protected virtual void DetermineBlockageDistancesAndDeceleration(IList<Obstacle> obstacles, LinePath measurePath) {
			LinePath.PointOnPath zeroPoint = measurePath.ZeroPoint;
			foreach (Obstacle obs in obstacles) {
				if (obs.avoidanceStatus == AvoidanceStatus.Collision && obs.collisionPoints != null && obs.collisionPoints.Count > 0) {
					// project each obstacle point onto the measure path and get the distance
					double minDist = double.MaxValue;
					foreach (Coordinates pt in obs.collisionPoints) {
						LinePath.PointOnPath closestPt = measurePath.GetClosestPoint(pt);
						double dist = measurePath.DistanceBetween(zeroPoint, closestPt);
						if (dist < minDist) {
							minDist = dist;
						}
					}

					// the distance will be short by the amount we expanded out the c-space polygon
					minDist += TahoeParams.T / 2.0;
					// decrease the distance by the length to the front bumper
					minDist -= TahoeParams.FL;

					obs.obstacleDistance = minDist;

					minDist -= stopped_spacing_dist;

					// don't have negative distance
					if (minDist <= 0.01) {
						minDist = 0.01;
					}

					double decel = vs.speed*vs.speed/(2*minDist);
					obs.requiredDeceleration = decel;
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="obstacles"></param>
		/// <param name="result"></param>
		/// <returns>True if the smoothing should continue</returns>
		protected virtual BlockageInstructions OnPathBlocked(IList<Obstacle> obstacles) {
			return OnPathBlocked(obstacles, true);
		}

		protected virtual BlockageInstructions OnPathBlocked(IList<Obstacle> obstacles, bool sendBlockage) {
			BlockageInstructions inst = new BlockageInstructions();
			inst.smootherInstructions = SmootherInstructions.RunNormalSmoothing;
			inst.pathBlocked = true;

			// look at all the obstacles and see if we want to ignore any
			// for now, just figure out the worst-case distance
			double staticStopDist = double.MaxValue;
			double dynamicStopDist = double.MaxValue;
			bool dynamicIsStopped = false;
			double dynamicStopTol = stopped_spacing_dist;
			bool dynamicIsIgnored = false;
			bool dynamicIsOccluded = false;
			bool staticIsLow = false;
			int stopTrackID = -1;
			foreach (Obstacle obs in obstacles) {
				if (obs.avoidanceStatus == AvoidanceStatus.Collision || obs.ignored) {
					bool isDynamic = (obs.obstacleClass == ObstacleClass.DynamicCarlike || obs.obstacleClass == ObstacleClass.DynamicStopped);
					if (isDynamic) {
						// handle dynamic obstacle specially
						// if the obstacle is not ignored or the obstacle is stopped
						if (obs.obstacleDistance < dynamicStopDist && obs.obstacleDistance > 0) {
							dynamicStopDist = obs.obstacleDistance;
							stopTrackID = obs.trackID;
							dynamicIsIgnored = obs.ignored;
							dynamicIsStopped = (obs.obstacleClass == ObstacleClass.DynamicStopped);
							dynamicIsOccluded = obs.occuluded;

							// TODO: can adjust stop spacing for intersections
							if (obs.ignored)
								dynamicStopTol = TahoeParams.VL;
							else
								dynamicStopTol = stopped_spacing_dist;
						}
					}
					else if (!isDynamic && obs.obstacleDistance < staticStopDist && obs.obstacleDistance > 0) {
						staticStopDist = obs.obstacleDistance;

						staticIsLow = obs.obstacleClass == ObstacleClass.StaticSmall;
					}

					// reset the avoidance status to ignore
					obs.avoidanceStatus = AvoidanceStatus.Ignore;
				}
			}

			// determine what the deal is
			bool stopIsDynamic;
			double stopDist = Math.Min(dynamicStopDist, staticStopDist);

			if (stopDist == double.MaxValue) {
				inst.pathBlocked = false;
				return inst;
			}

			// allow dynamic obstacles to be the stopping point if they are up to 1 m closer
			double targetDecel;
			if (dynamicStopDist - blockage_dynamic_tol < staticStopDist) {
				stopIsDynamic = true;
				targetDecel = 3;

				// if the dynamic obstacle is ignored, then just break out because we don't really care
				if (dynamicIsIgnored  && !dynamicIsStopped) {
					inst.pathBlocked = false;
					return inst;
				}
			}
			else {
				targetDecel = 2;
				stopIsDynamic = false;
				stopTrackID = -1;
			}

			Services.Dataset.ItemAs<double>("blockage dist").Add(stopDist, curTimestamp);

			double origStopDist = stopDist;
			double speedCommandStopDist = GetSpeedCommandStopDistance();
			if (!double.IsInfinity(speedCommandStopDist) && speedCommandStopDist < stopDist) {
				if (speedCommandStopDist < stopDist - 2) {
					// we don't want to do anything special
					inst.pathBlocked = false;
					return inst;
				}
				else {
					stopDist -= 2;
				}
			}
			else if (stopIsDynamic) {
				stopDist -= dynamicStopTol;
			}
			else if (staticIsLow) {
				stopDist -= 5;
			}
			else {
				stopDist -= stopped_spacing_dist;
			}

			if (stopDist < 0.01) {
				stopDist = 0.01;
			}

			double stopSpeed = Math.Sqrt(2*targetDecel*stopDist);
			double stopDecel = vs.speed*vs.speed/(2*stopDist);

			Services.Dataset.ItemAs<double>("stop target speed").Add(stopSpeed, curTimestamp);

			double scalarSpeed = -1;
			if (speedCommand is ScalarSpeedCommand) {
				scalarSpeed = ((ScalarSpeedCommand)speedCommand).Speed;

				if (scalarSpeed <= stopSpeed) {
					// don't need to report a blockage or do anything since arbiter is already going slow enough
					inst.pathBlocked = false;
					return inst;
				}
			}

			if ((origStopDist < 25 && vs.IsStopped && scalarSpeed != 0 && (!stopIsDynamic || InIntersection() || dynamicIsOccluded)) || Services.BehaviorManager.TestMode) {
				// check the timing
				if (Services.BehaviorManager.TestMode) {
					sendBlockage = true;
				}
				else if (sendBlockage) {
					if (lastBlockageTime == null || lastBlockageDynamic != stopIsDynamic) {
						lastBlockageTime = HighResDateTime.Now;
						lastBlockageDynamic = stopIsDynamic;
						sendBlockage = false;
					}
					else if ((HighResDateTime.Now - lastBlockageTime.Value) < TimeSpan.FromSeconds(2)) {
						sendBlockage = false;
					}
				}

				if (sendBlockage) {
					// send a blockage report to AI
					UrbanChallenge.Behaviors.CompletionReport.CompletionResult result = (stopDecel > 5) ? UrbanChallenge.Behaviors.CompletionReport.CompletionResult.InevitableDeath : UrbanChallenge.Behaviors.CompletionReport.CompletionResult.Stopped;
					bool stopTooClose = origStopDist < TahoeParams.VL;
					BlockageType type;
					if (stopIsDynamic) {
						if (dynamicIsStopped && dynamicIsOccluded) {
							type = BlockageType.Static;
						}
						else {
							type = BlockageType.Dynamic;
						}
					}
					else {
						type = BlockageType.Static;
					}
					CompletionReport report = new TrajectoryBlockedReport(result, origStopDist, type, stopTrackID, stopTooClose || DetermineReverseRecommended(obstacles), Services.BehaviorManager.CurrentBehaviorType);
					ForwardCompletionReport(report);
				}
			}
			else {
				// clear out the last blockage time
				lastBlockageTime = null;
			}

			// at this point we've decided to handle the blockage and stop for it
			 
			// we want to consider stopping for this
			// calculate an approximate minimum stopping distance
			double minStoppingDist = vs.speed*vs.speed/10.0;

			// adjust the planning distance
			if (stopDist < minStoppingDist) {
				inst.smootherInstructions = SmootherInstructions.UsePreviousPath;
			}
			else {
				// adjust the smoother target path to be the adjusted planning distance length
				smootherBasePath = GetPathBlockedSubPath(smootherBasePath, stopDist);
				inst.smootherInstructions = SmootherInstructions.RunNormalSmoothing;
			}

			if (vs.IsStopped && speedCommand is ScalarSpeedCommand && ((ScalarSpeedCommand)speedCommand).Speed == 0) {
				// don't do anything
				inst.speedCommand = new ConstantSpeedCommandGenerator(0, TahoeParams.brake_hold);
				inst.steeringCommand = new ConstantSteeringCommandGenerator(null, null, false);
				inst.smootherInstructions = SmootherInstructions.UseCommandGenerator;
			}
			else if (Math.Abs(vs.speed) < 3.5 && stopDist < 7) {
				if (vs.IsStopped && stopDist < 0.5) {
					inst.speedCommand = new ConstantSpeedCommandGenerator(0, TahoeParams.brake_hold);
					inst.steeringCommand = new ConstantSteeringCommandGenerator(null, null, false);
					inst.smootherInstructions = SmootherInstructions.UseCommandGenerator;
				}
				else {
					if (approachSpeed == null) {
						approachSpeed = Math.Max(Math.Abs(vs.speed), 1.5);
					}

					// if the deceleration is less than some threshold, 
					inst.speedCommand = new FeedbackSpeedCommandGenerator(new StopSpeedGenerator(new TravelledDistanceProvider(curTimestamp, stopDist), approachSpeed.Value));
				}
			}
			else {
				approachSpeed = null;
				if (vs.speed - stopSpeed > 0.5) {
					// we're going too fast for the normal speed control to stop in time
					inst.speedCommand = new FeedbackSpeedCommandGenerator(new ConstantAccelSpeedGenerator(-stopDecel));
				}
				else {
					inst.speedCommand = new FeedbackSpeedCommandGenerator(new ConstantSpeedGenerator(stopSpeed, null));
				}
			}
			
			inst.commandLabel = "blockage stop";

			return inst;
		}

		protected virtual void ForwardCompletionReport(CompletionReport report) {
			Services.BehaviorManager.ForwardCompletionReport(report);
		}

		protected virtual bool InSafetyZone() {
			return false;
		}

		protected virtual bool InIntersection() {
			return false;
		}

		protected virtual LinePath GetPathBlockedSubPath(LinePath basePath, double stopDist) {
			return basePath.SubPath(basePath.StartPoint, stopDist + TahoeParams.FL);
		}

		protected virtual PlanningResult OnDynamicallyInfeasible(IList<Obstacle> obstacles, AvoidanceDetails details) {
			return OnDynamicallyInfeasible(obstacles, details, true);
		}

		protected virtual PlanningResult OnDynamicallyInfeasible(IList<Obstacle> obstacles, AvoidanceDetails details, bool sendBlockage) {
			double scalarSpeed = -1;
			if (speedCommand is ScalarSpeedCommand) {
				scalarSpeed = ((ScalarSpeedCommand)speedCommand).Speed;
			}

			// see if we can figure out a stop distance based on stuff crossing
			double stopDist = double.NaN;
			try {
				if (details != null) {
					double totalDist = 0;
					int numBounds = details.smoothingDetails.leftBounds.Length;
					for (int i = 0; i < numBounds; i++) {
						// left is less than right, we can't make it through
						if (details.smoothingDetails.leftBounds[i].deviation < details.smoothingDetails.rightBounds[i].deviation) {
							stopDist = totalDist;
							break;
						}

						totalDist += 0.5;
					}
				}
			}
			catch (Exception) {
			}

			if (vs.IsStopped && scalarSpeed != 0 && sendBlockage && !Services.BehaviorManager.TestMode) {
				if (lastDynInfeasibleTime == null) {
					lastDynInfeasibleTime = HighResDateTime.Now;
				}
				else if (HighResDateTime.Now - lastDynInfeasibleTime.Value > TimeSpan.FromSeconds(2)) {
					// send a completion report with the error
					bool stopTooClose = stopDist < TahoeParams.VL || double.IsNaN(stopDist);
					CompletionReport report = new TrajectoryBlockedReport(UrbanChallenge.Behaviors.CompletionReport.CompletionResult.Stopped, stopDist, BlockageType.Unknown, -1, stopTooClose || DetermineReverseRecommended(obstacles), Services.BehaviorManager.CurrentBehaviorType);
					ForwardCompletionReport(report);
				}
			}
			else {
				lastDynInfeasibleTime = null;
			}

			if (Services.BehaviorManager.TestMode) {
				bool stopTooClose = stopDist < TahoeParams.VL || double.IsNaN(stopDist);
				CompletionReport report = new TrajectoryBlockedReport(UrbanChallenge.Behaviors.CompletionReport.CompletionResult.Stopped, stopDist, BlockageType.Unknown, -1, stopTooClose || DetermineReverseRecommended(obstacles), Services.BehaviorManager.CurrentBehaviorType);
				ForwardCompletionReport(report);
			}

			stopDist -= 2;

			if (stopDist < 0.01) {
				stopDist = 0.01;
			}

			double nomDecel = 3;
			if (scalarSpeed == 0) {
				nomDecel = 4;
			}

			// nominal stopping acceleration is 3 m/s^2
			double nomStoppingDist = vs.speed*vs.speed/(2*nomDecel);
			if (double.IsNaN(stopDist) || stopDist > nomStoppingDist) {
				stopDist = nomStoppingDist;
			}

			// figure out the target deceleration
			double targetDecel = vs.speed*vs.speed/(2*stopDist);

			PlanningResult result = new PlanningResult();

			// figure out if arbiter is already stopping shorter
			double commandedStopDist = GetSpeedCommandStopDistance();
			if (!double.IsPositiveInfinity(commandedStopDist) && commandedStopDist < stopDist) {
				// don't need to do anything, we're already stopping appropriately
				result.speedCommandGenerator = null;
			}
			else if (vs.IsStopped) {
				// just hold the brakes with the standard pressure
				result.speedCommandGenerator = new ConstantSpeedCommandGenerator(0, TahoeParams.brake_hold+1);
			}
			else {
				// do a constant deceleration profile
				result.speedCommandGenerator = new FeedbackSpeedCommandGenerator(new ConstantAccelSpeedGenerator(-targetDecel));
			}

			if (prevSmoothedPath != null) {
				RelativeTransform transform = Services.RelativePose.GetTransform(prevSmoothedPathTimestamp, curTimestamp);
				SmoothedPath smoothedPath = new SmoothedPath(curTimestamp, prevSmoothedPath.Transform(transform), null);
				result.steeringCommandGenerator = new PathSteeringCommandGenerator(smoothedPath);
				result.smoothedPath = smoothedPath;
			}
			else {
				result.steeringCommandGenerator = new ConstantSteeringCommandGenerator(null, null, false);
			}

			result.dynamicallyInfeasible = true;
			result.commandLabel = "dynamically infeasible";

			return result;
		}

		protected virtual void OnSmoothSuccess(ref PlanningResult result) {
			lastDynInfeasibleTime = null;
			lastBlockageTime = null;
		}

		private bool DetermineReverseRecommended(IList<Obstacle> obstacles) {
			if (obstacles == null)
				return false;

			// determine if there are obstacles in front of us 
			Rect frontRect = new Rect(0, -(TahoeParams.T + 1)/2, TahoeParams.VL + 4, TahoeParams.T + 1);

			foreach (Obstacle obs in obstacles) {
				// iterate through each point
				foreach (Coordinates pt in obs.AvoidancePolygon) {
					if (frontRect.IsInside(pt)) {
						return true;
					}
				}
			}

			if (IsOffRoad())
				return true;

			return false;
		}

		protected virtual bool IsOffRoad() {
			return false;
		}

		private LinePath ReplaceFirstPoint(LinePath path, LinePath.PointOnPath maxAdvancePoint, Coordinates pt) {
			LinePath ret;
			if (!disablePathAngleCheck) {
				// start walking down the path until the angle is cool
				double angle_threshold = pathAngleMax;
				LinePath.PointOnPath newPoint = new LinePath.PointOnPath();
				for (double dist = 0; dist < pathAngleCheckMax; dist += 1) {
					// get the point advanced from the 2nd point on the base path by dist
					double distTemp = dist;
					newPoint = path.AdvancePoint(path.GetPointOnPath(1), ref distTemp);

					// check if we're past the end
					if (distTemp > 0) {
						break;
					}
					else if (maxAdvancePoint.Valid && newPoint >= maxAdvancePoint) {
						newPoint = maxAdvancePoint;
						break;
					}

					// check if the angle is coolness or not
					double angle = Math.Acos((newPoint.Location-pt).Normalize().Dot(path.GetSegment(newPoint.Index).UnitVector));

					if (angle < angle_threshold) {
						break;
					}
				}

				// create a new version of the base path with the stuff section removed
				ret = path.RemoveBetween(path.StartPoint, newPoint);
				ret[0] = pt;
			}
			else {
				ret = path.Clone();
				ret[0] = pt;
			}
			
			return ret;
		}

		protected void LinearizeStayInLane(ILaneModel laneModel, double laneDist, CarTimestamp curTimestamp,
			out LinePath centerLine, out LinePath leftBound, out LinePath rightBound) {
			LinearizeStayInLane(laneModel, laneDist, null, null, null, curTimestamp, out centerLine, out leftBound, out rightBound);
		}

		protected void LinearizeStayInLane(ILaneModel laneModel, double laneDist, 
			double? laneStartDistMax, double? boundDistMax, double? boundStartDistMax, CarTimestamp curTimestamp,
			out LinePath centerLine, out LinePath leftBound, out LinePath rightBound) {

			LinearizeStayInLane(laneModel, laneDist, laneStartDistMax, boundDistMax, null, boundStartDistMax, curTimestamp, out centerLine, out leftBound, out rightBound);
		}

		protected void LinearizeStayInLane(ILaneModel laneModel, double laneDist,
			double? laneStartDistMax, double? boundDistMax, double? boundStartDistMin, double? boundStartDistMax, CarTimestamp curTimestamp,
			out LinePath centerLine, out LinePath leftBound, out LinePath rightBound) {
			// get the center line, left bound and right bound
			LinearizationOptions laneOpts = new LinearizationOptions(0, laneDist, curTimestamp);
			centerLine = laneModel.LinearizeCenterLine(laneOpts);
			if (centerLine == null || centerLine.Count < 2) {
				BehaviorManager.TraceSource.TraceEvent(TraceEventType.Error, 0, "center line linearization SUCKS");
			}

			double leftStartDist = vs.speed*TahoeParams.actuation_delay + 1;
			double rightStartDist = vs.speed*TahoeParams.actuation_delay + 1;

			double offtrackDistanceBack = centerLine.ZeroPoint.OfftrackDistance(Coordinates.Zero);
			bool offtrackLeftBack = offtrackDistanceBack > 0;
			double additionalDistBack = 5*Math.Abs(offtrackDistanceBack)/laneModel.Width;
			if (offtrackLeftBack) {
				leftStartDist += additionalDistBack;
			}
			else {
				rightStartDist += additionalDistBack;
			}

			// now determine how to generate the left/right bounds
			// figure out the offtrack distance from the vehicle's front bumper
			double offtrackDistanceFront = centerLine.ZeroPoint.OfftrackDistance(new Coordinates(TahoeParams.FL, 0));

			// offtrackDistance > 0 => we're to left of path
			// offtrackDistance < 0 => we're to right of path
			bool offsetLeftFront = offtrackDistanceFront > 0;

			double additionalDistFront = 10*Math.Abs(offtrackDistanceFront)/laneModel.Width;
			if (offsetLeftFront) {
				leftStartDist += additionalDistFront;
			}
			else {
				rightStartDist += additionalDistFront;
			}

			if (boundStartDistMax.HasValue) {
				if (leftStartDist > boundStartDistMax.Value) {
					leftStartDist = boundStartDistMax.Value;
				}

				if (rightStartDist > boundStartDistMax.Value) {
					rightStartDist = boundStartDistMax.Value;
				}
			}

			if (boundStartDistMin.HasValue) {
				if (leftStartDist < boundStartDistMin.Value) {
					leftStartDist = boundStartDistMin.Value;
				}

				if (rightStartDist < boundStartDistMin.Value) {
					rightStartDist = boundStartDistMin.Value;
				}
			}

			double boundEndDist = laneDist + 5;
			if (boundDistMax.HasValue && boundDistMax.Value < boundEndDist) {
				boundEndDist = boundDistMax.Value;
			}

			laneOpts = new LinearizationOptions(leftStartDist, boundEndDist, curTimestamp);
			leftBound = laneModel.LinearizeLeftBound(laneOpts);
			laneOpts = new LinearizationOptions(rightStartDist, boundEndDist, curTimestamp);
			rightBound = laneModel.LinearizeRightBound(laneOpts);

			double laneStartDist = TahoeParams.FL;
			if (laneStartDistMax.HasValue && laneStartDistMax.Value < laneStartDist) {
				laneStartDist = laneStartDistMax.Value;
			}

			// remove the first FL distance of the center line
			if (laneStartDist > 0) {
				centerLine = centerLine.RemoveBefore(centerLine.AdvancePoint(centerLine.ZeroPoint, laneStartDist));
				centerLine.Insert(0, Coordinates.Zero);
			}
		}

		protected void GetLaneBoundStartDists(LinePath centerLine, double laneWidth, out double leftStartDist, out double rightStartDist) {
			leftStartDist = vs.speed*TahoeParams.actuation_delay + 1;
			rightStartDist = vs.speed*TahoeParams.actuation_delay + 1;

			double offtrackDistanceBack = centerLine.ZeroPoint.OfftrackDistance(Coordinates.Zero);
			bool offtrackLeftBack = offtrackDistanceBack > 0;
			double additionalDistBack = 5*Math.Abs(offtrackDistanceBack)/laneWidth;
			if (offtrackLeftBack) {
				leftStartDist += additionalDistBack;
			}
			else {
				rightStartDist += additionalDistBack;
			}

			// now determine how to generate the left/right bounds
			// figure out the offtrack distance from the vehicle's front bumper
			double offtrackDistanceFront = centerLine.ZeroPoint.OfftrackDistance(new Coordinates(TahoeParams.FL, 0));

			// offtrackDistance > 0 => we're to left of path
			// offtrackDistance < 0 => we're to right of path
			bool offsetLeftFront = offtrackDistanceFront > 0;

			double additionalDistFront = 10*Math.Abs(offtrackDistanceFront)/laneWidth;
			if (offsetLeftFront) {
				leftStartDist += additionalDistFront;
			}
			else {
				rightStartDist += additionalDistFront;
			}
		}

		protected void GetIntersectionPullPath(LinePath startingPath, LinePath endingPath, Polygon intersectionPolygon, bool addStartingPoint, bool addEndingPoint, LinePath targetPath, ref double pullWeight) {
			double angle = Math.Acos(startingPath.EndSegment.UnitVector.Dot(endingPath.GetSegment(0).UnitVector));

			// get the centroid of the intersection
			Coordinates centroid;

			// check if the angle is great than an threshold
			if (angle > 10*Math.PI/180.0) {
				// intersect the two lines formed by the starting and ending lanes
				Line startingLaneLine = new Line(startingPath[startingPath.Count-2], startingPath[startingPath.Count-1]);
				Line endingLaneLine = new Line(endingPath[1], endingPath[0]);

				// intersect them stuff and see if the point of intersection is between the two lines
				Coordinates K;
				if (!startingLaneLine.Intersect(endingLaneLine, out centroid, out K) || K.X <= 0 || K.Y <= 0)
					return;
			}
			else {
				// if there is no intersection polygon, there isn't much we can do
				if (intersectionPolygon == null || intersectionPolygon.Count < 3) {
					return;
				}

				centroid = intersectionPolygon.GetCentroid();
			}

			// calculate the pull weighting dependent on angle of intersection
			// angle 0 -> 0 weighting
			// angle 45 -> 0.00025 weighting
			// angle 90 -> 0.001 weighting
			pullWeight = Math.Pow(angle/(Math.PI/2), 2)*0.001;

			// get the relative transform from the behavior timestamp to the current timestamp
			RelativeTransform transform = Services.RelativePose.GetTransform(behaviorTimestamp, curTimestamp);
			centroid = transform.TransformPoint(centroid);

			if (addStartingPoint) {
				targetPath.Add(startingPath.EndPoint.Location);
			}
			// add the line from exit -> centroid (assuming that exit is already in the target path)
			targetPath.Add(centroid);
			if (addEndingPoint) {
				// add the line from centroid -> entrance
				targetPath.Add(endingPath[0]);
			}

			Services.UIService.PushLineList(targetPath, curTimestamp, "intersection path", true);
			Services.Dataset.ItemAs<double>("intersection weight").Add(pullWeight, curTimestamp);
		}
	}
}
