using System;
using System.Collections.Generic;
using System.Text;
using OperationalLayer.Tracking;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common;
using UrbanChallenge.OperationalUIService.Behaviors;
using OperationalLayer.PathPlanning;
using OperationalLayer.Pose;
using UrbanChallenge.Common.Pose;
using OperationalLayer.Tracking.Steering;
using System.Threading;
using UrbanChallenge.Common.Utility;
using OperationalLayer.Tracking.SpeedControl;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common.Path;
using OperationalLayer.Obstacles;
using System.Diagnostics;
using UrbanChallenge.PathSmoothing;
using UrbanChallenge.Operational.Common;	// hik

namespace OperationalLayer.OperationalBehaviors {
	class SimpleStayInLane : IOperationalBehavior, IDisposable {
		private LinePath basePath;
		private LinePath leftBound;
		private LinePath rightBound;
		private double maxSpeed;
		private CarTimestamp pathTime;

		private PathPlanner planner;
		private bool cancelled;

		private ObstacleManager obstacleManager; // hik

		#region IOperationalBehavior Members

		public string GetName() {
			return "SimpleStayInLane";
		}

		public void OnBehaviorReceived(Behavior b) {
			// check if this is a valid state transition
			if (b is HoldBrakeBehavior || b is SimpleStayInLaneBehavior) {
				Services.BehaviorManager.Execute(b, null, true);
			}
			else {
				throw new InvalidBehaviorException();
			}
		}

		public void OnTrackingCompleted(TrackingCompletedEventArgs e) {
			// we shouldn't be stopping or anything
		}

		public void Initialize(Behavior b) {
			SimpleStayInLaneBehavior sb = (SimpleStayInLaneBehavior)b;

			// store the base path
			basePath = ConvertPath(sb.BasePath);

			// get the lower, upper bound
			leftBound = FindBoundary(sb.LaneWidth/2);
			rightBound = FindBoundary(-sb.LaneWidth/2);

			// convert everything to be vehicle relative
			AbsoluteTransformer absTransform = Services.StateProvider.GetAbsoluteTransformer();
			pathTime = absTransform.Timestamp;

			basePath = basePath.Transform(absTransform);
			leftBound = leftBound.Transform(absTransform);
			rightBound = rightBound.Transform(absTransform);

			// send the left and right bounds
			Services.UIService.PushRelativePath(leftBound, pathTime, "left bound");
			Services.UIService.PushRelativePath(rightBound, pathTime, "right bound");
			
			maxSpeed = sb.MaxSpeed;

			obstacleManager = new ObstacleManager();	// hik
		}

		private LinePath ConvertPath(UrbanChallenge.Common.Path.Path p) {
			LinePath lineList = new LinePath();
			lineList.Add(p[0].Start);
			for (int i = 0; i < p.Count; i++) {
				lineList.Add(p[i].End);
			}

			return lineList;
		}

		private LinePath FindBoundary(double offset) {
			// create a list of shift line segments
			List<LineSegment> segs = new List<LineSegment>();

			foreach (LineSegment ls in basePath.GetSegmentEnumerator()) {
				segs.Add(ls.ShiftLateral(offset));
			}

			// find the intersection points between all of the segments
			LinePath boundPoints = new LinePath();
			// add the first point
			boundPoints.Add(segs[0].P0);

			// loop through the stuff
			for (int i = 0; i < segs.Count-1; i++) {
				// find the intersection
				Line l0 = (Line)segs[i];
				Line l1 = (Line)segs[i+1];

				Coordinates pt;
				if (l0.Intersect(l1, out pt)) {
					// figure out the angle of the intersection
					double angle = Math.Acos(l0.UnitVector.Dot(l1.UnitVector));

					// calculate the length factor
					// 90 deg, 3x width
					// 0 deg, 2x width
					double widthFactor = Math.Pow(angle/(Math.PI/2.0),2)*2 + 1;

					// get the line formed by pt and the corresponding path point
					Coordinates boundLine = pt - basePath[i+1];

					boundPoints.Add(widthFactor*Math.Abs(offset)*boundLine.Normalize() + basePath[i+1]);
				}
				else {
					boundPoints.Add(segs[i].P1);
				}
			}

			// add the last point
			boundPoints.Add(segs[segs.Count-1].P1);

			return boundPoints;
		}

		public void Process(object param) {
			if (cancelled) return;

			DateTime start = HighResDateTime.Now;

			OperationalVehicleState vs = Services.StateProvider.GetVehicleState();

			LinePath curBasePath = basePath;
			LinePath curLeftBound = leftBound;
			LinePath curRightBound = rightBound;

			// transform the base path to the current iteration
			CarTimestamp curTimestamp = Services.RelativePose.CurrentTimestamp;
			if (pathTime != curTimestamp) {
				RelativeTransform relTransform = Services.RelativePose.GetTransform(pathTime, curTimestamp);
				curBasePath = curBasePath.Transform(relTransform);
				curLeftBound = curLeftBound.Transform(relTransform);
				curRightBound = curRightBound.Transform(relTransform);
			}

			// get the distance between the zero point and the start point
			double distToStart = Coordinates.Zero.DistanceTo(curBasePath.GetPoint(curBasePath.StartPoint));

			// get the sub-path between 5 and 25 meters ahead
			double startDist = TahoeParams.FL;
			LinePath.PointOnPath startPoint = curBasePath.AdvancePoint(curBasePath.ZeroPoint, ref startDist);
			double endDist = 30;
			LinePath.PointOnPath endPoint = curBasePath.AdvancePoint(startPoint, ref endDist);

			if (startDist > 0) {
				// we've reached the end
				Services.BehaviorManager.Execute(new HoldBrakeBehavior(), null, false);
				return;
			}

			// get the sub-path
			LinePath subPath = curBasePath.SubPath(startPoint, endPoint);
			// add (0,0) as the starting point
			subPath.Insert(0, new Coordinates(0, 0));

			// do the same for the left, right bound
			startDist = TahoeParams.FL;
			endDist = 40;
			startPoint = curLeftBound.AdvancePoint(curLeftBound.ZeroPoint, startDist);
			endPoint = curLeftBound.AdvancePoint(startPoint, endDist);
			curLeftBound = curLeftBound.SubPath(startPoint, endPoint);

			startPoint = curRightBound.AdvancePoint(curRightBound.ZeroPoint, startDist);
			endPoint = curRightBound.AdvancePoint(startPoint, endDist);
			curRightBound = curRightBound.SubPath(startPoint, endPoint);

			if (cancelled) return;

			Services.UIService.PushRelativePath(subPath, curTimestamp, "subpath");
			Services.UIService.PushRelativePath(curLeftBound, curTimestamp, "left bound");
			Services.UIService.PushRelativePath(curRightBound, curTimestamp, "right bound");

			// run a path smoothing iteration
			lock (this) {
				planner = new PathPlanner();
			}

			////////////////////////////////////////////////////////////////////////////////////////////////////
			// start of obstacle manager - hik
			bool obstacleManagerEnable = true;

			PathPlanner.SmoothingResult result;

			if (obstacleManagerEnable == true) {
				
				// generate fake obstacles (for simulation testing only)
				double obsSize = 10.5 / 2;
				List<Coordinates> obstaclePoints = new List<Coordinates>();
				List<Obstacle> obstacleClusters = new List<Obstacle>();
				// fake left obstacles (for simulation only)
				int totalLeftObstacles = Math.Min(0, curLeftBound.Count - 1);
				for (int i = 0; i < totalLeftObstacles; i++) {
					obstaclePoints.Clear();
					obstaclePoints.Add(curLeftBound[i] + new Coordinates(obsSize, obsSize));
					obstaclePoints.Add(curLeftBound[i] + new Coordinates(obsSize, -obsSize));
					obstaclePoints.Add(curLeftBound[i] + new Coordinates(-obsSize, -obsSize));
					obstaclePoints.Add(curLeftBound[i] + new Coordinates(-obsSize, obsSize));
					obstacleClusters.Add(new Obstacle());
					obstacleClusters[obstacleClusters.Count - 1].obstaclePolygon = new Polygon(obstaclePoints);
				}
				// fake right obstacles (for simulation only)
				int totalRightObstacles = Math.Min(0, curRightBound.Count - 1);
				for (int i = 0; i < totalRightObstacles; i++) {
					obstaclePoints.Clear();
					obstaclePoints.Add(curRightBound[i] + new Coordinates(obsSize, obsSize));
					obstaclePoints.Add(curRightBound[i] + new Coordinates(obsSize, -obsSize));
					obstaclePoints.Add(curRightBound[i] + new Coordinates(-obsSize, -obsSize));
					obstaclePoints.Add(curRightBound[i] + new Coordinates(-obsSize, obsSize));
					obstacleClusters.Add(new Obstacle());
					obstacleClusters[obstacleClusters.Count - 1].obstaclePolygon = new Polygon(obstaclePoints);
				}
				// fake center obstacles (for simulation only)
				int totalCenterObstacles = Math.Min(0, subPath.Count - 1);
				for (int i = 2; i < totalCenterObstacles; i++) {
					obstaclePoints.Clear();
					obstaclePoints.Add(subPath[i] + new Coordinates(obsSize, obsSize));
					obstaclePoints.Add(subPath[i] + new Coordinates(obsSize, -obsSize));
					obstaclePoints.Add(subPath[i] + new Coordinates(-obsSize, -obsSize));
					obstaclePoints.Add(subPath[i] + new Coordinates(-obsSize, obsSize));
					obstacleClusters.Add(new Obstacle());
					obstacleClusters[obstacleClusters.Count - 1].obstaclePolygon = new Polygon(obstaclePoints);
				}

				obstaclePoints.Clear();
				obstaclePoints.Add(new Coordinates(10000, 10000));
				obstaclePoints.Add(new Coordinates(10000, 10001));
				obstaclePoints.Add(new Coordinates(10001, 10000));
				obstacleClusters.Add(new Obstacle());
				obstacleClusters[obstacleClusters.Count - 1].obstaclePolygon = new Polygon(obstaclePoints);

				obstaclePoints.Clear();
				obstaclePoints.Add(new Coordinates(1000, 1000));
				obstaclePoints.Add(new Coordinates(1000, 1001));
				obstaclePoints.Add(new Coordinates(1001, 1000));
				obstacleClusters.Add(new Obstacle());
				obstacleClusters[obstacleClusters.Count - 1].obstaclePolygon = new Polygon(obstaclePoints);

				obstaclePoints.Clear();
				obstaclePoints.Add(new Coordinates(-1000, -1000));
				obstaclePoints.Add(new Coordinates(-1000, -1001));
				obstaclePoints.Add(new Coordinates(-1001, -1000));
				obstacleClusters.Add(new Obstacle());
				obstacleClusters[obstacleClusters.Count - 1].obstaclePolygon = new Polygon(obstaclePoints);

				foreach (Obstacle obs in obstacleClusters) {
					obs.cspacePolygon = new Polygon(obs.obstaclePolygon.points);
				}

				// find obstacle path and left/right classification
				LinePath obstaclePath = new LinePath();
				//Boolean successFlag;
				//double laneWidthAtPathEnd = 10.0;
//#warning this currently doesn't work
				/*obstacleManager.ProcessObstacles(subPath, new LinePath[] { curLeftBound }, new LinePath[] { curRightBound }, 
					                               obstacleClusters, laneWidthAtPathEnd,
																				 out obstaclePath, out successFlag);
				 */

				// prepare left and right lane bounds
				double laneMinSpacing = 0.1;
				double laneDesiredSpacing = 0.5;
				double laneAlphaS = 10000;
				List<Boundary> leftBounds  = new List<Boundary>();
				List<Boundary> rightBounds = new List<Boundary>();
				leftBounds.Add(new Boundary(curLeftBound, laneMinSpacing, laneDesiredSpacing, laneAlphaS));
				rightBounds.Add(new Boundary(curRightBound, laneMinSpacing, laneDesiredSpacing, laneAlphaS));
				
				// sort out obstacles as left and right
				double obstacleMinSpacing = 0.1;
				double obstacleDesiredSpacing = 1.0;
				double obstacleAlphaS = 10000;
				Boundary bound;
				int totalObstacleClusters = obstacleClusters.Count;
				for (int i = 0; i < totalObstacleClusters; i++) {
					if (obstacleClusters[i].avoidanceStatus == AvoidanceStatus.Left) {
						// obstacle cluster is to the left of obstacle path
						bound = new Boundary(obstacleClusters[i].obstaclePolygon.points, obstacleMinSpacing, 
							                   obstacleDesiredSpacing, obstacleAlphaS, true);
						bound.CheckFrontBumper = true;
						leftBounds.Add(bound);
					}
					else if (obstacleClusters[i].avoidanceStatus == AvoidanceStatus.Right) {
						// obstacle cluster is to the right of obstacle path
						bound = new Boundary(obstacleClusters[i].obstaclePolygon.points, obstacleMinSpacing, 
							                   obstacleDesiredSpacing, obstacleAlphaS, true);
						bound.CheckFrontBumper = true;
						rightBounds.Add(bound);
					}
					else {
						// obstacle cluster is outside grid, hence ignore obstacle cluster
					}
				}				

				Stopwatch stopwatch = new Stopwatch();
				stopwatch.Start();

				// PlanPath function call with obstacle path and obstacles
				result = planner.PlanPath(obstaclePath, obstaclePath, leftBounds, rightBounds, 
																	0, maxSpeed, vs.speed, null, curTimestamp, false);
				
				stopwatch.Stop();
				Console.WriteLine("============================================================");
				Console.WriteLine("With ObstacleManager - Planner - Elapsed (ms): {0}", stopwatch.ElapsedMilliseconds);
				Console.WriteLine("============================================================");
			}
			else {
				Stopwatch stopwatch = new Stopwatch();
				stopwatch.Start();

				// original PlanPath function call
				result = planner.PlanPath(subPath, curLeftBound, curRightBound, 
																	0, maxSpeed, vs.speed, null, curTimestamp);

				stopwatch.Stop();
				Console.WriteLine("============================================================");
				Console.WriteLine("Without ObstacleManager - Planner - Elapsed (ms): {0}", stopwatch.ElapsedMilliseconds);
				Console.WriteLine("============================================================");
			}

			// end of obstacle manager - hik
			////////////////////////////////////////////////////////////////////////////////////////////////////

			//PathPlanner.PlanningResult result = planner.PlanPath(subPath, curLeftBound, curRightBound, 0, maxSpeed, vs.speed, null, curTimestamp);
			
			//SmoothedPath path = new SmoothedPath(pathTime);

			lock (this) {
				planner = null;
			}

			if (cancelled) return;

			if (result.result == UrbanChallenge.PathSmoothing.SmoothResult.Sucess) {
				// start tracking the path
				Services.TrackingManager.QueueCommand(TrackingCommandBuilder.GetSmoothedPathVelocityCommand(result.path));
				//Services.TrackingManager.QueueCommand(TrackingCommandBuilder.GetConstantSteeringConstantSpeedCommand(-.5, 2));

				/*TrackingCommand cmd = new TrackingCommand(
					new FeedbackSpeedCommandGenerator(new ConstantSpeedGenerator(2, null)),
					new SinSteeringCommandGenerator(),
					true);
				Services.TrackingManager.QueueCommand(cmd);*/

				// send the path's we're tracking to the UI
				Services.UIService.PushRelativePath(result.path, curTimestamp, "smoothed path");

				cancelled = true;
			}
		}

		public void Cancel() {
			//lock (this) {
			//  if (planner != null) {
			//    planner.Cancel();
			//  }

			//  cancelled = true;
			//}
		}

		public void Dispose() {
			if (obstacleManager != null) {
				obstacleManager.Dispose();
				obstacleManager = null;
			}
		}

		#endregion
	}
}
