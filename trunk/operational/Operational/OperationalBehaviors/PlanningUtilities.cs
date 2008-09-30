using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Common.Vehicle;
using OperationalLayer.Obstacles;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Operational.Common;
using OperationalLayer.PathPlanning;
using UrbanChallenge.PathSmoothing;
using OperationalLayer.Tracking.SpeedControl;
using OperationalLayer.Tracking;
using OperationalLayer.Tracking.Steering;
using System.Diagnostics;
using OperationalLayer.CarTime;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.Splines;
using OperationalLayer.Pose;

namespace OperationalLayer.OperationalBehaviors {
	static class PlanningUtilities {
		/// <summary>
		/// Target deceleration in m/s^2 to use when calculating planning distance
		/// </summary>
		private const double planning_dist_decel = 2;
		/// <summary>
		/// Time in seconds to use for the system latency when calculating planning distance
		/// </summary>
		private const double planning_dist_sys_latency = 0.3;

		/// <summary>
		/// Minimum planning distance in m
		/// </summary>
		private const double planning_dist_min = 15;
		/// <summary>
		/// Maximum planning distance in m
		/// </summary>
		private const double planning_dist_max = 30;

		public static double GetPlanningDistance(CarTimestamp curTimestamp, SpeedCommand speedCommand, CarTimestamp speedCommandTimestamp) {
			if (speedCommand is ScalarSpeedCommand) {
				// figure out the commanded speed and plan for the stopping distance
				double commandedSpeed = ((ScalarSpeedCommand)speedCommand).Speed;

				// calculate expected stopping distance (actual deceleration at 2 m/s^2 + system latency + tahoe rear-axle to front-bumper length)
				double planningDist = commandedSpeed*commandedSpeed/(2*planning_dist_decel) + commandedSpeed*planning_dist_sys_latency + TahoeParams.FL;
				//if (planningDist < TahoeParams.FL + TahoeParams.VL) {
				//  planningDist = TahoeParams.FL + TahoeParams.VL;
				//}
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
				double remainingDist = origDist - Services.TrackedDistance.GetDistanceTravelled(speedCommandTimestamp, curTimestamp);
				if (remainingDist < 0) {
					remainingDist = 0;
				}
				return remainingDist + TahoeParams.FL;
			}
			else if (speedCommand is StopAtLineSpeedCommand) {
				double remainingDist = Services.Stopline.DistanceToStopline();
				return remainingDist;
			}
			else {
				throw new InvalidOperationException();
			}
		}

		public static void SmoothAndTrack(LinePath basePath, bool useTargetPath, 
			LinePath leftBound, LinePath rightBound, 
			double maxSpeed, double? endingHeading, bool endingOffsetBound,
			CarTimestamp curTimestamp, bool doAvoidance, 
			SpeedCommand speedCommand, CarTimestamp behaviorTimestamp,
			string commandLabel, ref bool cancelled, 
			ref LinePath previousSmoothedPath, ref CarTimestamp previousSmoothedPathTimestamp, ref double? approachSpeed) {
			SmoothAndTrack(basePath, useTargetPath, new LinePath[] { leftBound }, new LinePath[] { rightBound }, maxSpeed, endingHeading, endingOffsetBound, curTimestamp, doAvoidance, speedCommand, behaviorTimestamp, commandLabel, ref cancelled, ref previousSmoothedPath, ref previousSmoothedPathTimestamp, ref approachSpeed);
		}

		public static void SmoothAndTrack(LinePath basePath, bool useTargetPath, 
			IList<LinePath> leftBounds, IList<LinePath> rightBounds, 
			double maxSpeed, double? endingHeading, bool endingOffsetBound, 
			CarTimestamp curTimestamp, bool doAvoidance, 
			SpeedCommand speedCommand, CarTimestamp behaviorTimestamp,
			string commandLabel, ref bool cancelled,
			ref LinePath previousSmoothedPath, ref CarTimestamp previousSmoothedPathTimestamp, ref double? approachSpeed) {

			// if we're in listen mode, just return for now
			if (OperationalBuilder.BuildMode == BuildMode.Listen) {
				return;
			}

			PathPlanner.PlanningResult result;

			double curSpeed = Services.StateProvider.GetVehicleState().speed;
			LinePath targetPath = new LinePath();
			double initialHeading = 0;
			// get the part we just used to make a prediction
			if (useTargetPath && previousSmoothedPath != null) {
				//targetPath = previousSmoothedPath.Transform(Services.RelativePose.GetTransform(previousSmoothedPathTimestamp, curTimestamp));
				// interpolate the path with a smoothing spline
				//targetPath = targetPath.SplineInterpolate(0.05);
				//Services.UIService.PushRelativePath(targetPath, curTimestamp, "prediction path2");
				// calculate the point speed*dt ahead
				/*double lookaheadDist = curSpeed*0.20;
				if (lookaheadDist > 0.1) {
					LinePath.PointOnPath pt = targetPath.AdvancePoint(targetPath.ZeroPoint, lookaheadDist);
					// get the heading
					initialHeading = targetPath.GetSegment(pt.Index).UnitVector.ArcTan;
					// adjust the base path start point to be the predicted location
					basePath[0] = pt.Location;

					// get the part we just used to make a prediction
					predictionPath = targetPath.SubPath(targetPath.ZeroPoint, pt);
					// push to the UI
					Services.UIService.PushRelativePath(predictionPath, curTimestamp, "prediction path");
					//basePath[0] = new Coordinates(lookaheadDist, 0);
					Services.UIService.PushRelativePath(basePath, curTimestamp, "subpath2");
					Services.Dataset.ItemAs<double>("initial heading").Add(initialHeading, curTimestamp);
					// calculate a piece of the sub path
					//targetPath = targetPath.SubPath(targetPath.ZeroPoint, 7);
				}*/

				// get the tracking manager to predict stuff like whoa
				AbsolutePose absPose;
				OperationalVehicleState vehicleState;
				Services.TrackingManager.ForwardPredict(out absPose, out vehicleState);
				// insert the stuff stuff
				basePath[0] = absPose.xy;
				initialHeading = absPose.heading;

				// start walking down the path until the angle is cool
				double angle_threshold = 30*Math.PI/180.0;
				double dist;
				LinePath.PointOnPath newPoint = new LinePath.PointOnPath();
				for (dist = 0; dist < 10; dist += 1) {
					// get the point advanced from the 2nd point on the base path by dist
					double distTemp = dist;
					newPoint = basePath.AdvancePoint(basePath.GetPointOnPath(1), ref distTemp);
					
					// check if we're past the end
					if (distTemp > 0) {
						break;
					}

					// check if the angle is coolness or not
					double angle = Math.Acos((newPoint.Location-basePath[0]).Normalize().Dot(basePath.GetSegment(newPoint.Index).UnitVector));

					if (Math.Acos(angle) < angle_threshold) {
						break;
					}
				}

				// create a new version of the base path with the stuff section removed
				basePath = basePath.RemoveBetween(basePath.StartPoint, newPoint);

				Services.UIService.PushRelativePath(basePath, curTimestamp, "subpath2");

				// zero that stuff out
				targetPath = new LinePath();
			}

			StaticObstacles obstacles = null;
			// only do the planning is we're in a lane scenario
			// otherwise, the obstacle grid will be WAY too large
			if (doAvoidance && leftBounds.Count == 1 && rightBounds.Count == 1) {
				// get the obstacles predicted to the current timestamp
				obstacles = Services.ObstaclePipeline.GetProcessedObstacles(curTimestamp);
			}

			// start the planning timer
			Stopwatch planningTimer = Stopwatch.StartNew();

			// check if there are any obstacles
			if (obstacles != null && obstacles.polygons != null && obstacles.polygons.Count > 0) {
				if (cancelled) return;

				// we need to do the full obstacle avoidance
				// execute the obstacle manager
				LinePath avoidancePath;
				List<ObstacleManager.ObstacleType> obstacleSideFlags;
				bool success;
				Services.ObstacleManager.ProcessObstacles(basePath, leftBounds, rightBounds, obstacles.polygons,
					out avoidancePath, out obstacleSideFlags, out success);

				// check if we have success
				if (success) {
					// build the boundary lists
					// start with the lanes
					List<Boundary> leftSmootherBounds  = new List<Boundary>();
					List<Boundary> rightSmootherBounds = new List<Boundary>();

					double laneMinSpacing = 0.1;
					double laneDesiredSpacing = 0.1;
					double laneAlphaS = 0.1;
					leftSmootherBounds.Add(new Boundary(leftBounds[0], laneMinSpacing, laneDesiredSpacing, laneAlphaS));
					rightSmootherBounds.Add(new Boundary(rightBounds[0], laneMinSpacing, laneDesiredSpacing, laneAlphaS));

					// sort out obstacles as left and right
					double obstacleMinSpacing = 0.8;
					double obstacleDesiredSpacing = 0.8;
					double obstacleAlphaS = 100;
					int totalObstacleClusters = obstacles.polygons.Count;
					for (int i = 0; i < totalObstacleClusters; i++) {
						if (obstacleSideFlags[i] == ObstacleManager.ObstacleType.Left) {
							Boundary bound = new Boundary(obstacles.polygons[i], obstacleMinSpacing, obstacleDesiredSpacing, obstacleAlphaS);
							bound.CheckFrontBumper = true;
							leftSmootherBounds.Add(bound);
						}
						else if (obstacleSideFlags[i] == ObstacleManager.ObstacleType.Right) {
							Boundary bound = new Boundary(obstacles.polygons[i], obstacleMinSpacing, obstacleDesiredSpacing, obstacleAlphaS);
							bound.CheckFrontBumper = true;
							rightSmootherBounds.Add(bound);
						}
					}

					if (cancelled) return;

					// execute the smoothing
					PathPlanner planner = new PathPlanner();
					planner.Options.alpha_w = 0;
					planner.Options.alpha_d = 10;
					planner.Options.alpha_c = 10;
					result = planner.PlanPath(avoidancePath, targetPath, leftSmootherBounds, rightSmootherBounds, initialHeading, maxSpeed, Services.StateProvider.GetVehicleState().speed, endingHeading, curTimestamp, endingOffsetBound);
				}
				else {
					// mark that we did not succeed
					result = new PathPlanner.PlanningResult(SmoothResult.Infeasible, null);
				}
			}
			else {

				if (cancelled) return;

				// do the path smoothing
				PathPlanner planner = new PathPlanner();

				List<LineList> leftList = new List<LineList>();
				foreach (LinePath ll in leftBounds) leftList.Add(ll);
				List<LineList> rightList = new List<LineList>();
				foreach (LinePath rl in rightBounds) rightList.Add(rl);

				planner.Options.alpha_w = 0;
				planner.Options.alpha_s = 0.1;
				planner.Options.alpha_d = 10;
				planner.Options.alpha_c = 10;

				result = planner.PlanPath(basePath, targetPath, leftList, rightList, initialHeading, maxSpeed, Services.StateProvider.GetVehicleState().speed, endingHeading, curTimestamp, endingOffsetBound);
			}

			planningTimer.Stop();

			BehaviorManager.TraceSource.TraceEvent(TraceEventType.Verbose, 0, "planning took {0} ms", planningTimer.ElapsedMilliseconds);

			Services.Dataset.ItemAs<bool>("route feasible").Add(result.result == SmoothResult.Sucess, LocalCarTimeProvider.LocalNow);

			if (result.result == SmoothResult.Sucess) {
				// insert the point (-1,0) so we make sure that the zero point during tracking is at the vehicle
				//Coordinates startingVec = result.path[1].Point-result.path[0].Point;
				//Coordinates insertPoint = result.path[0].Point-startingVec.Normalize();
				//result.path.Insert(0, new OperationalLayer.PathPlanning.PathPoint(insertPoint, maxSpeed));

				previousSmoothedPath = new LinePath(result.path);
				previousSmoothedPathTimestamp = curTimestamp;
				Services.UIService.PushLineList(previousSmoothedPath, curTimestamp, "smoothed path", true);

				if (cancelled) return;

				// we've planned out the path, now build up the command
				ISpeedGenerator speedGenerator;
				if (speedCommand is ScalarSpeedCommand) {
					/*if (result.path.HasSpeeds) {
						speedGenerator = result.path;
					}
					else {*/
					speedGenerator = new ConstantSpeedGenerator(maxSpeed, null);
					//}
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

				if (cancelled) return;

				// build up the command
				TrackingCommand trackingCommand = new TrackingCommand(new FeedbackSpeedCommandGenerator(speedGenerator), new PathSteeringCommandGenerator(result.path), false);
				trackingCommand.Label = commandLabel;

				// queue it to execute
				Services.TrackingManager.QueueCommand(trackingCommand);
			}
		}
	}
}
