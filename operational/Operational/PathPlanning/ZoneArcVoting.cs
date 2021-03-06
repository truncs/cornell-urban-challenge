using System;
using System.Collections.Generic;
using System.Text;
using OperationalLayer.Tracking.Steering;
using UrbanChallenge.Common;
using OperationalLayer.Pose;
using OperationalLayer.Obstacles;
using UrbanChallenge.Common.Sensors;
using UrbanChallenge.Operational.Common;
using UrbanChallenge.Common.Shapes;
using OperationalLayer.CarTime;
using UrbanChallenge.Common.Vehicle;

namespace OperationalLayer.PathPlanning {
	static class ZoneArcVoting {
		private const int num_arcs = 41;
		private const double max_curvature = 1/8.0; // minimum turning radius of 10 m

		private const double obstacle_weight = 10;
		private const double hysteresis_weight = 1;
		private const double straight_weight = 1;
		private const double goal_weight = 7;

		public static ISteeringCommandGenerator SparcVote(ref double prevCurvature, Coordinates relativeGoalPoint, List<Polygon> perimeterPolygons) {
			double newCurvature = FindBestCurvature(prevCurvature, relativeGoalPoint, perimeterPolygons);
			prevCurvature = newCurvature;
			if (double.IsNaN(newCurvature)) {
				return null;
			}
			else {
				return new ConstantSteeringCommandGenerator(SteeringUtilities.CurvatureToSteeringWheelAngle(newCurvature, 2), false);
			}
		}

		private static double FindBestCurvature(double prevCurvature, Coordinates relativeGoalPoint, List<Polygon> perimeterPolygons) {
			CarTimestamp curTimestamp = Services.RelativePose.CurrentTimestamp;

			AbsoluteTransformer absTransform = Services.StateProvider.GetAbsoluteTransformer();

			// get a list of obstacles
			ObstacleCollection obstacles = Services.ObstaclePipeline.GetProcessedObstacles(curTimestamp, UrbanChallenge.Behaviors.SAUDILevel.None);
			List<Polygon> obstaclePolygons = new List<Polygon>();
			foreach (Obstacle obs in obstacles.obstacles){
				obstaclePolygons.Add(obs.cspacePolygon);
			}

			obstaclePolygons.AddRange(perimeterPolygons);

			List<ArcResults> arcs = new List<ArcResults>();
			double maxUtility = double.MinValue;
			ArcResults selectedArc = null;

			// recalculate weights
			double totalWeights = obstacle_weight+hysteresis_weight+straight_weight+goal_weight;
			double obstacleWeight = obstacle_weight/totalWeights;
			double hysteresisWeight = hysteresis_weight/totalWeights;
			double straightWeight = straight_weight/totalWeights;
			double goalWeight = goal_weight/totalWeights;

			int start = num_arcs/2;
			double curvatureStep = max_curvature/start;
			for (int i = -start; i <= start; i++) {
				double curvature = i*curvatureStep;

				double collisionDist, clearanceDist, collisionUtility;
				bool vetoed;
				EvaluateObstacleUtility(curvature, 20, obstaclePolygons, out collisionDist, out clearanceDist, out collisionUtility, out vetoed);

				double hystersisUtility = EvaluateHysteresisUtility(curvature, prevCurvature);
				double straightUtility = EvaluateStraightUtility(curvature);
				double goalUtility = EvaluateGoalUtility(curvature, relativeGoalPoint);

				double totalUtility = collisionUtility*obstacleWeight + hystersisUtility*hysteresisWeight + straightUtility*straightWeight +
					goalUtility*goalWeight;

				ArcResults result = new ArcResults();
				result.curvature = curvature;
				result.vetoed = vetoed;
				result.totalUtility = totalUtility;
				result.obstacleHitDistance = collisionDist;
				result.obstacleClearanceDistance = clearanceDist;
				result.obstacleUtility = collisionUtility;
				result.hysteresisUtility = hystersisUtility;
				result.straightUtility = straightUtility;
				result.goalUtility = goalUtility;

				arcs.Add(result);

				if (!vetoed && totalUtility > maxUtility) {
					maxUtility = totalUtility;
					selectedArc = result;
				}
			}

			ArcVotingResults results = new ArcVotingResults();
			results.arcResults = arcs;
			results.selectedArc = selectedArc;

			Services.Dataset.ItemAs<ArcVotingResults>("arc voting results").Add(results, LocalCarTimeProvider.LocalNow);

			if (selectedArc == null) {
				return double.NaN;
			}
			else {
				return selectedArc.curvature;
			}
		}

		private static void EvaluateObstacleUtility(double curvature, double dist, IList<Polygon> obstacles, out double collisionDist, out double clearanceDist, out double utility, out bool vetoed) {
			TestObstacleCollision(curvature, dist, obstacles, out collisionDist, out clearanceDist);
			vetoed = collisionDist<10;
			
			// evaluate utility based on distance -- cost is 0 at 20 m, 1 at 10 m
			const double distMin = 10;
			const double distMax = 20;

			double collisionUtility = (collisionDist-distMax)/(distMax - distMin);
			if (collisionUtility > 0) collisionUtility = 0;
			if (collisionUtility < -1) collisionUtility = -1;

			const double clearanceMin = 0;
			const double clearanceMax = 1;
			double clearanceUtility = (clearanceDist-clearanceMax)/(clearanceMax-clearanceMin);
			if (clearanceUtility > 0.3) clearanceUtility = 0.3;
			if (clearanceUtility < -1) clearanceUtility = -1;

			if (collisionUtility == 0) {
				utility = clearanceUtility;
			}
			else {
				utility = Math.Min(clearanceUtility, collisionUtility);
			}
		}

		private static void TestObstacleCollision(double curvature, double dist, IList<Polygon> obstacles, out double collisionDist, out double clearanceDist) {
			if (Math.Abs(curvature) < 1e-10) {
				// process as a straight line
				// determine end point of circle -- s = rθ, θ = sk (s = arc len, r = radius, θ = angle, k = curvature (1/r)
				// this will always be very very near straight, so just process as straight ahead
				LineSegment rearAxleSegment = new LineSegment(new Coordinates(0, 0), new Coordinates(dist, 0));
				LineSegment frontAxleSegment = new LineSegment(new Coordinates(TahoeParams.FL, 0), new Coordinates(dist+TahoeParams.FL,0));
				collisionDist = Math.Min(TestObstacleCollisionStraight(rearAxleSegment, obstacles), TestObstacleCollisionStraight(frontAxleSegment, obstacles));
				clearanceDist = Math.Min(GetObstacleClearanceLine(rearAxleSegment, obstacles), GetObstacleClearanceLine(frontAxleSegment, obstacles));
			}
			else {
				// build out the circle formed by the rear and front axle
				bool leftTurn = curvature > 0;
				double radius = Math.Abs(1/curvature);
				double frontRadius = Math.Sqrt(TahoeParams.FL*TahoeParams.FL + radius*radius);

				CircleSegment rearSegment, frontSegment;

				if (leftTurn) {
					Coordinates center = new Coordinates(0, radius);
					rearSegment = new CircleSegment(radius, center, Coordinates.Zero, dist, true);
					frontSegment = new CircleSegment(frontRadius, center, new Coordinates(TahoeParams.FL, 0), dist, true);
				}
				else {
					Coordinates center = new Coordinates(0, -radius);
					rearSegment = new CircleSegment(radius, center, Coordinates.Zero, dist, false);
					frontSegment = new CircleSegment(frontRadius, center, new Coordinates(TahoeParams.FL, 0), dist, false);
				}

				collisionDist = Math.Min(TestObstacleCollisionCircle(rearSegment, obstacles), TestObstacleCollisionCircle(frontSegment, obstacles));
				clearanceDist = Math.Min(GetObstacleClearanceCircle(rearSegment, obstacles), GetObstacleClearanceCircle(frontSegment, obstacles)); 
			}
		}

		private static double TestObstacleCollisionStraight(LineSegment segment, IList<Polygon> obstacles) {
			double minDist = double.MaxValue;
			foreach (Polygon obs in obstacles) {
				Coordinates[] pts;
				double[] K;
				if (obs.Intersect(segment, out pts, out K)) {
					// this is a potentially closest intersection
					for (int i = 0; i < K.Length; i++) {
						double dist = K[i]*segment.Length;
						if (dist < minDist) {
							minDist = dist;
						}
					}
				}
			}

			return minDist;
		}

		private static double TestObstacleCollisionCircle(CircleSegment segment, IList<Polygon> obstacles) {
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

			return minDist;
		}

		private static double GetObstacleClearanceLine(LineSegment segment, IList<Polygon> obstacles) {
			double minDist = double.MaxValue;
			foreach (Polygon obs in obstacles) {
				foreach (Coordinates pt in obs) {
					Coordinates closestPt = segment.ClosestPoint(pt);
					double dist = closestPt.DistanceTo(pt);
					if (dist < minDist)
						minDist = dist;
				}
			}

			return minDist;
		}

		private static double GetObstacleClearanceCircle(CircleSegment segment, IList<Polygon> obstacles) {
			double minDist = double.MaxValue;
			foreach (Polygon obs in obstacles) {
				foreach (Coordinates pt in obs) {
					Coordinates closestPt = segment.GetClosestPoint(pt);
					double dist = closestPt.DistanceTo(pt);
					if (dist < minDist)
						minDist = dist;
				}
			}

			return minDist;
		}

		private static double EvaluateGoalUtility(double curvature, Coordinates relativeGoalPoint) {
			// get the angle to the goal point
			double angle = relativeGoalPoint.ArcTan;

			const double angleMax = 45*Math.PI/180.0;
			double scaledAngle = angle/angleMax;
			if (scaledAngle > 1) scaledAngle = 1;
			if (scaledAngle < -1) scaledAngle = -1;

			// calculate the target curvature to hit the goal
			double targetCurvature = scaledAngle*max_curvature;

			// calculate a matching scale factor
			double scaleFactor = Math.Pow((curvature - targetCurvature)/0.01, 2);

			// calculate a distance weighting factor
			double distMin = 20;
			double distMax = 70;
			double distFactor = (relativeGoalPoint.Length - distMin)/(distMax - distMin);
			if (distFactor > 0.3) distFactor = 0.3;
			if (distFactor < 0) distFactor = 0;
			distFactor = 1-distFactor;

			double turnFactor = (1-scaleFactor*0.1);
			if (turnFactor < -1) turnFactor = -1;

			return turnFactor*distFactor;
		}

		private static double EvaluateHysteresisUtility(double curvature, double prevCurvature) {
			if (double.IsNaN(prevCurvature)) {
				return 0;
			}

			double scaleFactor = Math.Pow((curvature-prevCurvature)/0.01, 2);

			double turnFactor = 1-scaleFactor*0.1;
			if (turnFactor < 0) turnFactor = 0;
			return turnFactor;
		}

		private static double EvaluateStraightUtility(double curvature) {
			double scaleFactor = Math.Pow((curvature+0.01)/0.02, 2);
			double turnFactor = 1-scaleFactor*0.5;
			if (turnFactor < 0) turnFactor = 0;
			return turnFactor;
		}

		/// <summary>
		/// Returns the curvature we would need to take pass through the target point
		/// </summary>
		/// <param name="targetPoint">Point to hit in vehicle relative coordinates</param>
		/// <returns>Target curvature</returns>
		private static double GetPurePursuitCurvature(Coordinates targetPoint) {
			return 2*targetPoint.Y/targetPoint.VectorLength2;
		}
	}
}
