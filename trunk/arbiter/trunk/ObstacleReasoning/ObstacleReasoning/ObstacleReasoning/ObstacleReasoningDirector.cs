using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.RndfNetwork;
using UrbanChallenge.Common.Sensors.Vehicle;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Sensors.Obstacle;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common.Splines;

namespace UrbanChallenge.ObstacleReasoning {

	/// <summary>
	/// Description of path
	/// </summary>
	public enum AboutPath {
		Null,				// no path found / error
		Normal,			// safe path found, i.e. no conflicts
		Stop,				// stop at end of path
		Avoidance		// path to avoid obstacles
	}

	public enum TargetLaneChangeType {
		Left,	// target lane is to the left
		Right	// target lane is to the right
	}

	/// <summary>
	/// Controls obstacle reasoning
	/// </summary>
	public class ObstacleReasoningDirector {

		private Coordinates vehiclePosition;
		private double vehicleSpeed, vehicleHeading;

		private bool leftLaneValid, rightLaneValid;
		private PointOnPath leftLanePosition, currentLanePosition, rightLanePosition;
		private double leftLaneWidth, currentLaneWidth, rightLaneWidth;
		
		private double maxPlanDist = 40;

		public Polygon sensorPolygon;

		public List<Coordinates> staticObstaclesIn;
		public List<Coordinates> staticObstaclesOut;
		public List<Coordinates> staticObstaclesFake;
		
		private List<ObservedVehicle> dynamicObstacles;
		private List<Path> dynamicObstaclesPaths;

		/// <summary>
		/// ObstacleReasoningDirector class constructor
		/// </summary>
		public ObstacleReasoningDirector() {
			staticObstaclesIn			= new List<Coordinates>();
			staticObstaclesOut		= new List<Coordinates>();
			staticObstaclesFake		= new List<Coordinates>();
			dynamicObstacles			= new List<ObservedVehicle>();
			dynamicObstaclesPaths = new List<Path>();
		}

		/// <summary>
		/// Initialise vehicle and lane information
		/// </summary>
		private void InitialiseInformation(Coordinates position, Coordinates heading, double speed, 
																			 Path leftLanePath, Path currentLanePath, Path rightLanePath) {
			// set up vehicle information
			vehiclePosition = position;
			vehicleSpeed		= speed;
			vehicleHeading	= heading.ArcTan;

			// set up lane valid flags
			leftLaneValid	 = leftLanePath  != null ? true : false;
			rightLaneValid = rightLanePath != null ? true : false;

			// set up positions on lanes
			currentLanePosition = currentLanePath.GetClosest(vehiclePosition);
			leftLanePosition		= leftLaneValid  ? leftLanePath.GetClosest(vehiclePosition)  : new PointOnPath();
			rightLanePosition		= rightLaneValid ? rightLanePath.GetClosest(vehiclePosition) : new PointOnPath();

			// set up lane information
			leftLaneWidth  = leftLaneValid  ? leftLanePosition.pt.DistanceTo(currentLanePosition.pt)  : double.NaN;
			rightLaneWidth = rightLaneValid ? rightLanePosition.pt.DistanceTo(currentLanePosition.pt) : double.NaN;
			if (leftLaneValid)
				currentLaneWidth = leftLaneWidth;
			else if (rightLaneValid)
				currentLaneWidth = rightLaneWidth;
			else
				currentLaneWidth = TahoeParams.T + 1.0;
		}

		/// <summary>
		/// Reason about travelling the lane ahead
		/// </summary>
		/// <param name="currentLanePath">lane path that vehicle is following</param>
		/// <param name="leftLanePath">lane path to the left of vehicle</param>
		/// <param name="rightLanePath">lane path to the right of vehicle</param>
		/// <param name="observedObstacles">static obstacles</param>
		/// <param name="observedVehicles">observed vehicles</param>
		/// <param name="position">		vehicle absolute position in m</param>
		/// <param name="heading">		vehicle heading as a vector</param>
		/// <param name="speed">			vehicle speed in m/s</param>
		/// <param name="aboutPath">	type of path being returned</param>
		/// <param name="forwardPath">forward path</param>
		public void ForwardPlanSimple(Path currentLanePath, Path leftLanePath, Path rightLanePath,
														ObservedObstacles observedObstacles,
														ObservedVehicle[] observedVehicles,
														Coordinates position, Coordinates heading, double speed,
														out AboutPath aboutPath, out Path forwardPath) {

			// set up vehicle and lane information
			InitialiseInformation(position, heading, speed, leftLanePath, currentLanePath, rightLanePath);

			// manage static and dynamic dynamic obstacles
			InitialiseObstacles(leftLanePath, currentLanePath, rightLanePath,
													observedObstacles, observedVehicles);

			double projectionDist = Math.Max(vehicleSpeed * 3, 10) + TahoeParams.FL;
			double origProjectionDist = projectionDist;
			double pathRisk, pathRiskDist, pathSepDist;

			do {
				// lookahead point
				double lookaheadDist = projectionDist;
				PointOnPath lookaheadPt = currentLanePath.AdvancePoint(currentLanePosition, ref lookaheadDist);

				// extend point if at end of path
				Coordinates offsetVec = new Coordinates(0, 0);
				if (lookaheadDist > 0.5)
					offsetVec = lookaheadPt.segment.Tangent(lookaheadPt).Normalize(lookaheadDist);

				// prepare ctrl points for spline path
				Coordinates startPoint = vehiclePosition;
				Coordinates endPoint = lookaheadPt.pt + offsetVec;
				Coordinates startVec = new Coordinates(1, 0).Rotate(vehicleHeading).Normalize(Math.Max(vehicleSpeed, 2.0));
				Coordinates endVec = lookaheadPt.segment.Tangent(lookaheadPt).Normalize(Math.Max(vehicleSpeed, 2.0));

				// generate spline path
				forwardPath = GenerateBezierPath(startPoint, endPoint, startVec, endVec);

				// determine risk of spline path
				CheckPathRisk(forwardPath, out pathRisk, out pathRiskDist, out pathSepDist);

				if (pathRisk != 0)
					projectionDist = Math.Max(pathRiskDist - 1, 0);

			} while (pathRisk != 0 && projectionDist != 0);

			if (pathRisk == 0 && projectionDist == origProjectionDist)
				aboutPath = AboutPath.Normal;
			else if (projectionDist != 0)
				aboutPath = AboutPath.Stop;
			else
				aboutPath = AboutPath.Null;
		}

		/// <summary>
		/// Reason about travelling the lane ahead
		/// </summary>
		/// <param name="currentLanePath">lane path that vehicle is following</param>
		/// <param name="leftLanePath">lane path to the left of vehicle</param>
		/// <param name="rightLanePath">lane path to the right of vehicle</param>
		/// <param name="observedObstacles">static obstacles</param>
		/// <param name="observedVehicles">observed vehicles</param>
		/// <param name="position">		vehicle absolute position in m</param>
		/// <param name="heading">		vehicle heading as a vector</param>
		/// <param name="speed">			vehicle speed in m/s</param>
		/// <param name="aboutPath">	type of path being returned</param>
		/// <param name="forwardPath">forward path</param>
		public void ForwardPlan(Path currentLanePath, Path leftLanePath, Path rightLanePath,
														ObservedObstacles observedObstacles,
														ObservedVehicle[] observedVehicles,
														Coordinates position, Coordinates heading, double speed,
														out AboutPath aboutPath, out Path forwardPath) {

			// set up vehicle and lane information
			InitialiseInformation(position, heading, speed, leftLanePath, currentLanePath, rightLanePath);
						
			// manage static and dynamic dynamic obstacles
			InitialiseObstacles(leftLanePath, currentLanePath, rightLanePath,
													observedObstacles, observedVehicles);

			double projectionDist = Math.Max(vehicleSpeed * 3, 10) + TahoeParams.FL;
			double origProjectionDist = projectionDist;

			double spacing = 0.4;
			int numPaths = (int)Math.Round(currentLaneWidth / spacing);
			numPaths -= (int)Math.IEEERemainder((double)numPaths, 2.0);
			int midPathIndex = (numPaths - 1) / 2;
			int selectedPathIndex;
			double[] pathsRisk = new double[numPaths];
			double[] pathsRiskDist = new double[numPaths];
			double[] pathsSepDist = new double[numPaths];
			double[] pathsCost = new double[numPaths];
			Path[] paths = new Path[numPaths];

			do {
				// lookahead point
				double lookaheadDist = projectionDist;
				PointOnPath lookaheadPt = currentLanePath.AdvancePoint(currentLanePosition, ref lookaheadDist);

				// extend point if at end of path
				Coordinates offsetVec = new Coordinates(0, 0);
				if (lookaheadDist > 0.5)
				  offsetVec = lookaheadPt.segment.Tangent(lookaheadPt).Normalize(lookaheadDist);

				// prepare ctrl points for spline path
				Coordinates startPoint = vehiclePosition;
				Coordinates endPoint	 = lookaheadPt.pt + offsetVec;
				Coordinates startVec	 = new Coordinates(1, 0).Rotate(vehicleHeading).Normalize(Math.Max(vehicleSpeed, 2.0));
				Coordinates endVec		 = lookaheadPt.segment.Tangent(lookaheadPt).Normalize(Math.Max(vehicleSpeed, 2.0));

				Coordinates rVec = endVec.Rotate90();

				// generate multiple spline paths and evaluate their risks
				for (int i = 0; i < numPaths; i++) {
					// generate spline path
					paths[i] = GenerateBezierPath(startPoint, endPoint + rVec.Normalize((i - midPathIndex) * spacing), startVec, endVec);

					// determine risk of spline path
					CheckPathRisk(paths[i], out pathsRisk[i], out pathsRiskDist[i], out pathsSepDist[i]);
				}

				// find minimum path risk (0 means it is a safe path, non-zero means it has some risk
				double minPathRisk = -1;
				for (int i = 0; i < numPaths; i++) {
					pathsRisk[i] = Math.Round(pathsRisk[i], 3);
					if (pathsRisk[i] < minPathRisk || minPathRisk == -1)
						minPathRisk = pathsRisk[i];
				}

				// select candidate paths and set up their cost
				for (int i = 0; i < numPaths; i++) {
					if (pathsRisk[i] == minPathRisk)
						pathsCost[i] = 0;
					else
						pathsCost[i] = -1;
				}

				// find cost of candidate paths
				double weightDev = 5;   // weight for path deviation penalty
				double weightDir = 1;   // weight for left path penalty
				for (int i = 0; i < numPaths; i++) {

					// skip paths with risk in first spline
					if (pathsCost[i] < 0)
						continue;

					double dir;
					if (i < midPathIndex)
						dir = 1;
					else
						dir = 0;

					pathsCost[i] = weightDev * Math.Abs(i - midPathIndex) +
												 weightDir * dir;
				}

				// find index of path to select
				selectedPathIndex = -1;
				double minPathCost = -1;
				for (int i = 0; i < numPaths; i++) {
					if (pathsCost[i] < 0)
						continue;

					if (pathsCost[i] < minPathCost || minPathCost == -1) {
						selectedPathIndex = i;
						minPathCost = pathsCost[i];
					}
				}

				if (pathsRisk[selectedPathIndex] != 0)
					projectionDist = Math.Max(pathsRiskDist[selectedPathIndex] - 1, 0);

			} while (pathsRisk[selectedPathIndex] != 0 && projectionDist > 7.5);

			// prepare safest path
			forwardPath = new Path();
			forwardPath.Add((BezierPathSegment)(paths[selectedPathIndex][0]));

			if (pathsRisk[selectedPathIndex] == 0)
				aboutPath = AboutPath.Normal;
			else if (projectionDist != 0)
				aboutPath = AboutPath.Stop;
			else
				aboutPath = AboutPath.Null;
		}

		/// <summary>
		/// Reason about changing lanes (simple version)
		/// </summary> 
		public void LaneChangePlan(Path changeLanesPath, Path initialLane, Path targetLane,
																		 ObservedObstacles observedObstacles,
																		 ObservedVehicle[] initialLaneObservedVehicles,
																		 ObservedVehicle[] targetLaneObservedVehicles,
																		 PointOnPath lowerBound, PointOnPath upperBound,
																		 Coordinates position, Coordinates heading, double speed,
																		 out AboutPath aboutPath, out Path laneChangePath) {

			// set up vehicle states
			vehiclePosition = position;
			vehicleSpeed = speed;
			vehicleHeading = heading.ArcTan;
			currentLanePosition = targetLane.GetClosest(vehiclePosition);
			leftLanePosition = initialLane != null ? initialLane.GetClosest(vehiclePosition) : new PointOnPath();
			rightLanePosition = initialLane != null ? initialLane.GetClosest(vehiclePosition) : new PointOnPath();

			//// set up lane information
			leftLaneWidth = initialLane != null ? leftLanePosition.pt.DistanceTo(currentLanePosition.pt) : double.NaN;
			rightLaneWidth = initialLane != null ? rightLanePosition.pt.DistanceTo(currentLanePosition.pt) : double.NaN;
			if (double.IsNaN(leftLaneWidth))
				if (double.IsNaN(rightLaneWidth))
					currentLaneWidth = 3;
				else
					currentLaneWidth = rightLaneWidth;
			else
				currentLaneWidth = leftLaneWidth;

			//// manage static and dynamic dynamic obstacles
			//ManageObstacles(targetLane, observedObstacles,
			//                initialLaneObservedVehicles, targetLaneObservedVehicles);

			double projectionDist = 15;
			double origProjDist = projectionDist;
			double pathRisk, pathRiskDist, pathSepDist;

			// lookahead point
			double lookaheadDist = projectionDist;
			PointOnPath lookaheadPt = targetLane.AdvancePoint(currentLanePosition, ref lookaheadDist);

			// extend point if at end of path
			Coordinates offsetVec = new Coordinates(0, 0);
			if (lookaheadDist > 0.5)
				offsetVec = lookaheadPt.segment.Tangent(lookaheadPt).Normalize(lookaheadDist);

			PointOnPath targetUpperBound = targetLane.GetClosest(upperBound.pt);

			// prepare ctrl points for spline path
			Coordinates startPoint = vehiclePosition;
			//Coordinates rVec = lookaheadPt.segment.Tangent(lookaheadPt).Rotate90().Normalize(endOffset);
			Coordinates endPoint = targetUpperBound.pt; // lookaheadPt.pt + offsetVec + rVec;
			Coordinates startVec = new Coordinates(1, 0).Rotate(vehicleHeading).Normalize(Math.Max(vehicleSpeed, 2.0));
			Coordinates endVec = targetLane.GetClosest(targetUpperBound.pt).segment.Tangent(targetUpperBound).Normalize(Math.Max(vehicleSpeed, 2.0));
			//Coordinates endVec = lookaheadPt.segment.Tangent(lookaheadPt).Normalize(Math.Max(vehicleSpeed, 2.0));

			// generate spline path
			laneChangePath = GenerateBezierPath(startPoint, endPoint, startVec, endVec);

			// determine risk of spline path
			CheckPathRisk(laneChangePath, out pathRisk, out pathRiskDist, out pathSepDist);

			if (pathRisk == 0)
				aboutPath = AboutPath.Normal;
			else
				aboutPath = AboutPath.Stop;
		}

		/// <summary>
		/// Reason about changing lanes
		/// </summary>
		/// <param name="previousChangeLanePath">previous change lane path</param>
		/// <param name="initialLanePath">lane path that vehicle is changing from</param>
		/// <param name="targetLanePath">	lane path that vehicle is changing to</param>
		/// <param name="targetType">	type for target lane, left or right</param>
		/// <param name="observedObstacles">static obstacles</param>
		/// <param name="observedVehicles">observed vehicles</param>
		/// <param name="lowerBound">	lower bound point on initial lane (similar to obstacle on target lane)</param>
		/// <param name="upperBound"> upper bound point on initial lane (similar to obstacle on initial lane)</param>
		/// <param name="position">		vehicle absolute position in m</param>
		/// <param name="heading">		vehicle heading as a vector</param>
		/// <param name="speed">			vehicle speed in m/s</param>
		/// <param name="aboutPath">	type of path being returned</param>
		/// <param name="currentChangeLanePath">change lane path</param>
		public void LaneChangePlanAdvance(Path previousChangeLanePath,
															 Path initialLanePath, Path targetLanePath, 
															 TargetLaneChangeType targetType,
															 ObservedObstacles observedObstacles, 
															 ObservedVehicle[] observedVehicles,
															 PointOnPath initialLaneLowerBound, PointOnPath initialLaneUpperBound,
															 Coordinates position, Coordinates heading, double speed,
															 out AboutPath aboutPath, out Path currentChangeLanePath) {

			// check if target lane is to the left or right
			if (targetType == TargetLaneChangeType.Left) {
				// set up vehicle and lane information
				InitialiseInformation(position, heading, speed, null, targetLanePath, initialLanePath);
				// manage static and dynamic dynamic obstacles
				InitialiseObstacles(null, targetLanePath, initialLanePath, observedObstacles, observedVehicles);
			}
			else {
				// set up vehicle and lane information
				InitialiseInformation(position, heading, speed, initialLanePath, targetLanePath, null);
				// manage static and dynamic dynamic obstacles
				InitialiseObstacles(initialLanePath, targetLanePath, null, observedObstacles, observedVehicles);
			}

			// determine risk of previous spline path, if provided
			double pathRisk, pathRiskDist, pathSepDist;
			if (previousChangeLanePath != null) {
				// check risk of  previous spline path
				CheckPathRisk(previousChangeLanePath, out pathRisk, out pathRiskDist, out pathSepDist);
				
				if (pathRisk == 0) {
					// no risk was found, return previous spline path
					currentChangeLanePath = previousChangeLanePath;
					aboutPath = AboutPath.Normal;
					return;
				}
			}

			PointOnPath targetLaneLowerBound = targetLanePath.GetClosest(initialLaneLowerBound.pt);
			PointOnPath targetLaneUpperBound = targetLanePath.GetClosest(initialLaneUpperBound.pt);
			double targetLaneLowerBoundDist = Math.Round(targetLanePath.DistanceBetween(currentLanePosition, targetLaneLowerBound),1);
			double targetLaneUpperBoundDist = Math.Round(targetLanePath.DistanceBetween(currentLanePosition, targetLaneUpperBound),1);

			// generate obstacles for lower and upper bound points
			Coordinates lowerBoundObstacle = targetLaneLowerBound.pt;
			Coordinates upperBoundObstacle = initialLaneUpperBound.pt;
			if (targetType == TargetLaneChangeType.Left) {
				lowerBoundObstacle += targetLaneLowerBound.segment.Tangent(targetLaneLowerBound).RotateM90().Normalize(0.5 * currentLaneWidth - 1.0);
				upperBoundObstacle += initialLaneUpperBound.segment.Tangent(initialLaneUpperBound).Rotate90().Normalize(0.5 * rightLaneWidth - 1.0);
			}
			else {
				lowerBoundObstacle += targetLaneLowerBound.segment.Tangent(targetLaneLowerBound).Rotate90().Normalize(0.5 * currentLaneWidth - 1.0);
				upperBoundObstacle += initialLaneUpperBound.segment.Tangent(initialLaneUpperBound).RotateM90().Normalize(0.5 * leftLaneWidth - 1.0);
			}
			staticObstaclesFake.Add(lowerBoundObstacle);
			staticObstaclesFake.Add(upperBoundObstacle);

			// path projection distance
			double projectionDist = Math.Max(targetLaneLowerBoundDist, TahoeParams.VL + TahoeParams.FL);
			double origProjectionDist = projectionDist;

			do {
			  // lookahead point
			  double lookaheadDist = projectionDist;
				PointOnPath lookaheadPt = targetLanePath.AdvancePoint(currentLanePosition, ref lookaheadDist);

			  // extend point if at end of path
				Coordinates offsetVec = new Coordinates(0, 0);
				if (lookaheadDist > 0.5)
				  offsetVec = lookaheadPt.segment.Tangent(lookaheadPt).Normalize(lookaheadDist);

			  // prepare ctrl points for spline path
			  Coordinates startPoint = vehiclePosition;
			  Coordinates endPoint = lookaheadPt.pt + offsetVec;
			  Coordinates startVec = new Coordinates(1, 0).Rotate(vehicleHeading).Normalize(Math.Max(vehicleSpeed, 2.0));
			  Coordinates endVec = lookaheadPt.segment.Tangent(lookaheadPt).Normalize(Math.Max(vehicleSpeed, 2.0));

			  // generate spline path
			  currentChangeLanePath = GenerateBezierPath(startPoint, endPoint, startVec, endVec);

			  // determine risk of spline path
				CheckPathRisk(currentChangeLanePath, out pathRisk, out pathRiskDist, out pathSepDist);

				// project further if current spline path has risk
				if (pathRisk != 0) {
					if (projectionDist == targetLaneUpperBoundDist + TahoeParams.RL)
						break;

					projectionDist = Math.Min(projectionDist + TahoeParams.VL / 2, targetLaneUpperBoundDist + TahoeParams.RL);
				}

			} while (pathRisk != 0 && projectionDist <= targetLaneUpperBoundDist + TahoeParams.RL);

			// check if path without risk was found
			if (pathRisk == 0)
				aboutPath = AboutPath.Normal;
			else
				aboutPath = AboutPath.Null;
		}

		/// <summary>
		/// Check for risk of path
		/// </summary>
		/// <param name="planPath">			path to be evaluated</param>
		/// <param name="pathRisk">			risk of path</param>
		/// <param name="pathRiskDist">	distance when risk is first encountered</param>
		/// <param name="pathSepDist">	minimum separation distance encountered</param>
		private void CheckPathRisk(Path planPath, out double pathRisk, out double pathRiskDist, out double pathSepDist) {

			double avoidDistInsideLane	= TahoeParams.T / 2 + 1.0;
			double avoidDistOutsideLane = TahoeParams.T / 2 + 0.3;
			double avoidDistFake				= TahoeParams.T / 2 + 1.0;

			// evaluate for static obstacles

			// obtain points along the path and their tangents
			double pathStepDist = 0.5;
			List<Coordinates> pathPoints, pathPointTangents;
			List<double> pathPointDistances;
			GetPointsOnPath(planPath, planPath.StartPoint, planPath.Length, pathStepDist, 
											out pathPoints, out pathPointTangents, out pathPointDistances);

			// find risk of path
			double sepDist;
			Coordinates pathPoint;
			
			// initialise path risk values
			pathRisk		 = 0;
			pathRiskDist = -1;
			pathSepDist	 = avoidDistInsideLane;

			// check all path points for separation
			for (int i = 1; i < pathPoints.Count; i++) {
				// check vehicle rear and front axle points
				for (int j = 0; j < 2; j++) {
					if (j == 0)
						pathPoint = pathPoints[i]; // rear axle point
					else
						pathPoint = pathPoints[i] + pathPointTangents[i].Normalize(TahoeParams.L); // front axle point
					
					// check for separation with obstacles inside lanes
					foreach (Coordinates obstacle in staticObstaclesIn) {
						if (Math.Abs(pathPoint.X - obstacle.X) > avoidDistInsideLane ||
								Math.Abs(pathPoint.Y - obstacle.Y) > avoidDistInsideLane)
							continue;

						// determine separation distance
						sepDist = pathPoint.DistanceTo(obstacle);

						// check if required separation distance is met
						if (sepDist < avoidDistInsideLane) {
							// update risk 
							pathRisk += (avoidDistInsideLane - sepDist) / pathPointDistances[i];

							// save distance of first occurence of risk
							if (pathRiskDist == -1)
								pathRiskDist = pathPointDistances[i];

							// save minimum separation distance encounter
							if (pathSepDist > sepDist || pathSepDist == -1)
								pathSepDist = sepDist;
						}
					}

					// check for separation with obstacles outside lanes
					foreach (Coordinates obstacle in staticObstaclesOut) {
						if (Math.Abs(pathPoint.X - obstacle.X) > avoidDistOutsideLane ||
								Math.Abs(pathPoint.Y - obstacle.Y) > avoidDistOutsideLane)
							continue;

						// determine separation distance
						sepDist = pathPoint.DistanceTo(obstacle);

						// check if required separation distance is met
						if (sepDist < avoidDistOutsideLane) {
							// update risk 
							pathRisk += (avoidDistOutsideLane - sepDist) / pathPointDistances[i];

							// save distance of first occurence of risk
							if (pathRiskDist == -1)
								pathRiskDist = pathPointDistances[i];

							// save minimum separation distance encounter
							if (pathSepDist > sepDist || pathSepDist == -1)
								pathSepDist = sepDist;
						}
					}

					// check for separation with fake obstacles
					foreach (Coordinates obstacle in staticObstaclesFake) {
						if (Math.Abs(pathPoint.X - obstacle.X) > avoidDistFake ||
								Math.Abs(pathPoint.Y - obstacle.Y) > avoidDistFake)
							continue;

						// determine separation distance
						sepDist = pathPoint.DistanceTo(obstacle);

						// check if required separation distance is met
						if (sepDist < avoidDistFake) {
							// update risk 
							pathRisk += (avoidDistFake - sepDist) / pathPointDistances[i];

							// save distance of first occurence of risk
							if (pathRiskDist == -1)
								pathRiskDist = pathPointDistances[i];

							// save minimum separation distance encounter
							if (pathSepDist > sepDist || pathSepDist == -1)
								pathSepDist = sepDist;
						}
					}
				}
			}
			
			// evaluate for dynamic obstacles

			// obtain points along the vehicle path and their tangents
			pathStepDist = vehicleSpeed * 0.1;
			pathPoints.Clear();
			pathPointTangents.Clear();
			pathPointDistances.Clear();
			GetPointsOnPath(planPath, planPath.StartPoint, Math.Min(vehicleSpeed * 5, planPath.Length), pathStepDist,
											out pathPoints, out pathPointTangents, out pathPointDistances);

			double dynObsPathStepDist;
			List<Coordinates> dynObsPathPoints, dynObsPathPointTangents;
			List<double> dynObsPathPointDistances;

			// check separation with dynamic obstacles
			for (int i = 0; i < dynamicObstacles.Count; i++) {

				// treat as zero speed if going slow
				double dynObsSpeed = 0;
				if (dynamicObstacles[i].Speed > 0.1)
					dynObsSpeed = dynamicObstacles[i].Speed;

				// obtain points along the dynamic obstacle and their tangents
				dynObsPathStepDist = dynObsSpeed * 0.1;
				GetPointsOnPath(dynamicObstaclesPaths[i], dynamicObstaclesPaths[i].StartPoint,
												Math.Min(dynObsSpeed * 5, dynamicObstaclesPaths[i].Length), 
												dynObsPathStepDist, out dynObsPathPoints, 
												out dynObsPathPointTangents, out dynObsPathPointDistances);

				int maxPoints;
				if (dynObsSpeed == 0)
					maxPoints = pathPoints.Count;
				else
					maxPoints = (int)Math.Min(pathPoints.Count, dynObsPathPoints.Count);

				for (int j = 0; j < maxPoints; j++) {

					// find points of dynamic obstacle
					Coordinates[] dynObsPoints = new Coordinates[4];
					Coordinates tVec, rVec;
					int dynObsPathIndex;
					if (dynObsSpeed == 0)
						dynObsPathIndex = 0;
					else
						dynObsPathIndex = j;
					tVec = dynObsPathPointTangents[dynObsPathIndex].Normalize(dynamicObstacles[i].Length / 2);
					rVec = dynObsPathPointTangents[dynObsPathIndex].Rotate90().Normalize(dynamicObstacles[i].Width / 2);
					dynObsPoints[0] = dynObsPathPoints[dynObsPathIndex] + tVec + rVec; // front left
					dynObsPoints[1] = dynObsPathPoints[dynObsPathIndex] + tVec - rVec; // front right
					dynObsPoints[2] = dynObsPathPoints[dynObsPathIndex] - tVec + rVec; // rear left
					dynObsPoints[3] = dynObsPathPoints[dynObsPathIndex] - tVec - rVec; // rear right

					// check vehicle rear and front axle points
					for (int k = 0; k < 2; k++) {
						if (k == 0)
							pathPoint = pathPoints[j]; // rear axle point
						else
							pathPoint = pathPoints[j] + pathPointTangents[j].Normalize(TahoeParams.L); // front axle point
						
						// check for separation with dynamic obstacles
						for (int d = 0; d < dynObsPoints.Length; d++) {
							sepDist = pathPoint.DistanceTo(dynObsPoints[d]);

							// check if required separation distance is met
							if (sepDist < avoidDistInsideLane) {
								pathRisk += (avoidDistInsideLane - sepDist) / pathPointDistances[j];

								// save distance of first occurence of risk
								if (pathRiskDist == -1)
									pathRiskDist = pathPointDistances[j];

								// save minimum separation distance encounter
								if (pathSepDist > sepDist || pathSepDist == -1)
									pathSepDist = sepDist;
							}
						}
					}
				}
			}

			// update minimum separation distance encountered
			pathSepDist = Math.Max(pathSepDist - TahoeParams.T / 2, 0);
		}

		/// <summary>
		/// Generate spline path with one bezier path segment
		/// </summary>
		/// <param name="startPoint">	start point of bezier path</param>
		/// <param name="endPoint">		end point of bezier path</param>
		/// <param name="startVec">		start tangent of bezier path where magnitude reflects speed</param>
		/// <param name="endVec">			end tangent of bezier path where magnitude reflects speed</param>
		/// <returns></returns>
		private Path GenerateBezierPath(Coordinates startPoint, Coordinates endPoint, Coordinates startVec, Coordinates endVec) {
			Coordinates p = startPoint - endPoint;
			Coordinates w = startVec - (-endVec);
			double d = w.Length;
			double time2cpa = 0;

			// find time to closest point of approach
			if (Math.Abs(d) > 1e-6)
				time2cpa = -p.Dot(w) / Math.Pow(d, 2);

			// set minimum time to 0.1 to prevent middle control points from 
			if (time2cpa < 0.1)
				time2cpa = 0.1;

			// generate bezier path
			double splineGain = 0.75;
			Coordinates p0 = startPoint;
			Coordinates p1 = startPoint + splineGain * time2cpa * startVec;
			Coordinates p2 = endPoint   + splineGain * time2cpa * (-endVec);
			Coordinates p3 = endPoint;
			Path bezierPath = new Path();
			bezierPath.Add(new BezierPathSegment(p0, p1, p2, p3, (double?)null, false));

			return bezierPath;
		}

		/// <summary>
		/// Manage obstacle information
		/// </summary>
		/// <param name="currentPath"></param>
		/// <param name="observedObstacles"></param>
		/// <param name="currentLaneObservedVehicles"></param>
		/// <param name="leftLaneObservedVehicles"></param>
		/// <param name="rightLaneObservedVehicles"></param>
		private void InitialiseObstacles(Path leftPath, Path currentPath, Path rightPath, 
																		 ObservedObstacles observedObstacles,
																		 ObservedVehicle[] observedVehicles) {

			//SetDynamicObstacles(observedVehicles);

			SetStaticObstacles(currentPath, observedObstacles);
		}

		/// <summary>
		/// Set static obstacles given as vectors relative from imu
		/// </summary>
		/// <param name="obstacles">observed obstacles in relative vectors from imu</param>
		private void SetStaticObstacles(Path path, ObservedObstacles observedObstacles) {
			// clear static obstacles			
			staticObstaclesIn.Clear();
			staticObstaclesOut.Clear();
			staticObstaclesFake.Clear();

			// determine sensor region to group obstacles
			DefineSensorRegion(path);

			List<PointOnPath> pop = new List<PointOnPath>();

			// prepare static obstacles for obstacle reasoning
			for (int i = 0; i < observedObstacles.Obstacles.Length; i++) {
				// transform obstacle to absolute coordinates
				Coordinates obs = new Coordinates(TahoeParams.IL, 0);
				obs += observedObstacles.Obstacles[i].ObstacleVector;
				obs = obs.Rotate(vehicleHeading) + vehiclePosition;

				// sort out static obstacles
				if (sensorPolygon.IsInside(obs) == true)
					staticObstaclesIn.Add(obs);
				else
					staticObstaclesOut.Add(obs);
			}
		}

		/// <summary>
		/// Set dynamic obstacles given in absolute coordinates (Version 2a)
		/// </summary>
		/// <param name="obstacles"></param>
		public void SetDynamicObstacles(Path observedVehiclePath, ObservedVehicle[] observedVehicles) {

			// generate paths for 
			for (int i = 0; i < observedVehicles.Length; i++) {
				// add observed vehicle
				dynamicObstacles.Add(observedVehicles[i]);

				// lane position
				PointOnPath observedVehicleLanePosition = observedVehiclePath.GetClosest(observedVehicles[i].AbsolutePosition);

				// lookahead point
				double projectionDist = Math.Max(observedVehicles[i].Speed * 5, 10) + 0.5 * TahoeParams.VL;
				double lookaheadDist = projectionDist;
				PointOnPath projectionPoint = observedVehiclePath.AdvancePoint(observedVehicleLanePosition, ref lookaheadDist);

				// extend point if at end of path
				Coordinates offsetVec = new Coordinates(0, 0);
				if (lookaheadDist > 0.5)
					offsetVec = projectionPoint.segment.Tangent(projectionPoint).Normalize(lookaheadDist);

				// prepare ctrl points for spline path
				Coordinates startPoint = observedVehicles[i].AbsolutePosition;
				Coordinates endPoint = projectionPoint.pt + offsetVec;
				Coordinates startVec = observedVehicleLanePosition.segment.Tangent(observedVehicleLanePosition).Normalize(Math.Max(observedVehicles[i].Speed, 2.0));
				Coordinates endVec = projectionPoint.segment.Tangent(projectionPoint).Normalize(Math.Max(observedVehicles[i].Speed, 2.0));

				// generate mid points of path
				int midPointsTotal = (int)Math.Round(projectionDist / 5.0) - 1;
				double midPointStepDist = projectionDist / (midPointsTotal + 1);
				Coordinates[] midPoints = new Coordinates[midPointsTotal];
				Coordinates[] midVecs = new Coordinates[midPointsTotal];
				Coordinates[] midShiftVecs = new Coordinates[midPointsTotal];
				for (int j = 0; j < midPointsTotal; j++) {
					// lookahead point
					lookaheadDist = projectionDist * (j + 1) / (midPointsTotal + 1);
					projectionPoint = observedVehiclePath.AdvancePoint(observedVehicleLanePosition, ref lookaheadDist);

					// extend point if at end of path
					offsetVec = new Coordinates(0, 0);
					if (lookaheadDist > 0.5)
						offsetVec = projectionPoint.segment.Tangent(projectionPoint).Normalize(lookaheadDist);

					// prepare ctrl points for spline path
					midPoints[j]		= projectionPoint.pt + offsetVec;
					midVecs[j]			= projectionPoint.segment.Tangent(projectionPoint).Normalize(Math.Max(vehicleSpeed, 2.0));
					midShiftVecs[j] = midVecs[j].Rotate90();
				}

				// vehicle vector with respect to segment closest point
				Coordinates carVec = observedVehicles[i].AbsolutePosition - observedVehicleLanePosition.pt;
				// segment tangent vector
				Coordinates pathVec = observedVehicleLanePosition.segment.Tangent(observedVehicleLanePosition);
				// compute offtrack error
				double offtrackError = Math.Sign(carVec.Cross(pathVec)) * observedVehicleLanePosition.pt.DistanceTo(observedVehicles[i].AbsolutePosition);

				// path points
				Coordinates[] pathPoints = new Coordinates[midPointsTotal + 2];
				pathPoints[0] = startPoint;
				pathPoints[midPointsTotal + 1] = endPoint;
				for (int j = 0; j < midPointsTotal; j++) {
						double control = 0.0;
						if (j == 0)
							control = 0.35;
						else if (j == midPointsTotal - 1)
							control = -0.35;

						pathPoints[j + 1] = midPoints[j] - midShiftVecs[j].Normalize(offtrackError * 
																																				 (midPointsTotal - j + control) / 
																																				 (midPointsTotal + 1));
				}

				// generate spline path with points
				Path projectedPath = new Path();
				CubicBezier[] beziers = SmoothingSpline.BuildC2Spline(pathPoints, startVec.Normalize(0.5 * midPointStepDist),
																															endVec.Normalize(0.5 * midPointStepDist), 0.5);
				for (int j = 0; j < beziers.Length; j++) {
					projectedPath.Add(new BezierPathSegment(beziers[j], (double?)null, false));
				}

				// generate spline path
				dynamicObstaclesPaths.Add(projectedPath);

				// generate static obstacles if speed is close to zero
				if (observedVehicles[i].Speed < 1.0) {
					Coordinates tVec = observedVehicleLanePosition.segment.Tangent(observedVehicleLanePosition).Normalize(observedVehicles[i].Length / 2);
					Coordinates rVec = observedVehicleLanePosition.segment.Tangent(observedVehicleLanePosition).Rotate90().Normalize(observedVehicles[i].Width / 2);
					staticObstaclesIn.Add(observedVehicles[i].AbsolutePosition + tVec + rVec); // front left
					staticObstaclesIn.Add(observedVehicles[i].AbsolutePosition + tVec - rVec); // front right
					staticObstaclesIn.Add(observedVehicles[i].AbsolutePosition - tVec + rVec); // rear left
					staticObstaclesIn.Add(observedVehicles[i].AbsolutePosition - tVec - rVec); // rear right
				}
			}
		}

		/// <summary>
		/// Set dynamic obstacles given in absolute coordinates (Version 1)
		/// </summary>
		/// <param name="obstacles"></param>
		public void SetDynamicObstaclesVer1(Path observedVehiclePath, ObservedVehicle[] observedVehicles) {

			// generate paths for 
			for (int i = 0; i < observedVehicles.Length; i++) {
				// add observed vehicle
				dynamicObstacles.Add(observedVehicles[i]);

				// lane position
				PointOnPath observedVehiclePosition = observedVehiclePath.GetClosest(observedVehicles[i].AbsolutePosition);

				// lookahead point
				double projectionDist = Math.Max(vehicleSpeed * 3, 10) + TahoeParams.FL;
				double lookaheadDist = projectionDist;
				PointOnPath projectionPoint = observedVehiclePath.AdvancePoint(observedVehiclePosition, ref lookaheadDist);

				// extend point if at end of path
				Coordinates offsetVec = new Coordinates(0, 0);
				if (lookaheadDist > 0.5)
					offsetVec = projectionPoint.segment.Tangent(projectionPoint).Normalize(lookaheadDist);

				// prepare ctrl points for spline path
				Coordinates startPoint = observedVehicles[i].AbsolutePosition;
				Coordinates endPoint = projectionPoint.pt + offsetVec;
				Coordinates startVec = observedVehiclePosition.segment.Tangent(observedVehiclePosition).Normalize(Math.Max(observedVehicles[i].Speed, 2.0));
				Coordinates endVec = projectionPoint.segment.Tangent(projectionPoint).Normalize(Math.Max(observedVehicles[i].Speed, 2.0));

				// generate spline path
				dynamicObstaclesPaths.Add(GenerateBezierPath(startPoint, endPoint, startVec, endVec));

				// generate static obstacles if speed is close to zero
				if (observedVehicles[i].Speed < 1.0) {
					Coordinates tVec = observedVehiclePosition.segment.Tangent(observedVehiclePosition).Normalize(observedVehicles[i].Length / 2);
					Coordinates rVec = observedVehiclePosition.segment.Tangent(observedVehiclePosition).Rotate90().Normalize(observedVehicles[i].Width / 2);
					staticObstaclesIn.Add(observedVehicles[i].AbsolutePosition + tVec + rVec); // front left
					staticObstaclesIn.Add(observedVehicles[i].AbsolutePosition + tVec - rVec); // front right
					staticObstaclesIn.Add(observedVehicles[i].AbsolutePosition - tVec + rVec); // rear left
					staticObstaclesIn.Add(observedVehicles[i].AbsolutePosition - tVec - rVec); // rear right
				}
			}
		}

		public void AvoidancePath(Path currentLanePath, Path leftLanePath, Path rightLanePath,
															 ObservedObstacles observedObstacles,
															 ObservedVehicle[] observedVehicles,
															 Coordinates position, Coordinates heading, double speed,
															 out AboutPath aboutPath, out Path avoidancePath) {
			// set up vehicle and lane information
			InitialiseInformation(position, heading, speed, leftLanePath, currentLanePath, rightLanePath);

			// manage static and dynamic dynamic obstacles
			//SetDynamicObstacles(observedVehicles);
			SetStaticObstacles(currentLanePath, observedObstacles);

			// obtain points along the path and their tangents
			List<Coordinates> pathPoints, pathPointTangents;
			List<double> pathPointDistances;
			GetPointsOnPath(currentLanePath, currentLanePosition, 25, 1,
											out pathPoints, out pathPointTangents, out pathPointDistances);

			int binTotal = 41; // odd
			int binMidIndex = (binTotal - 1) / 2;
			double binSize = 0.2;
			double binRange = binTotal * binSize;
			int[] binSelectedIndexes = new int[pathPoints.Count];
			Coordinates[] pathShifts = new Coordinates[pathPoints.Count];

			for (int i = 0; i < pathPoints.Count; i++) {
				
				double[] bins = new double[binTotal];
				foreach (Coordinates obstacle in staticObstaclesIn) {
					if (Math.Abs(pathPoints[i].X - obstacle.X) > 5.0 ||
							Math.Abs(pathPoints[i].Y - obstacle.Y) > 5.0)
						continue;

					Coordinates obsTf = obstacle - pathPoints[i];
					obsTf = obsTf.Rotate(-pathPointTangents[i].ArcTan);

					if (Math.Abs(obsTf.Y) < 4.0 && Math.Abs(obsTf.X) < 4.0)
						bins[(int)Math.Round(-obsTf.Y / binSize) + binMidIndex] += 1;
				}

				double[] riskBins = new double[binTotal];
				for (int j = 0; j < binTotal; j++) {
					for (int k = Math.Max(j-10, 0); k < Math.Min(j+10, binTotal); k++) {
						riskBins[j] += bins[k];
					}
				}

				List<int> candidateBinIndexes = new List<int>();
				for (int j = 0; j < binTotal; j++) {
					if (riskBins[j] == 0)
						candidateBinIndexes.Add(j);
				}

				double weightDev = 5;   // weight for path deviation penalty
				double weightDir = 1;   // weight for left path penalty
				List<double> costBins = new List<double>();
				foreach (int candidateBinIndex in candidateBinIndexes) {
					double dir;
					if (candidateBinIndex < binMidIndex)
						dir = 1;
					else
						dir = 0;
					costBins.Add(weightDev * Math.Abs(candidateBinIndex - binMidIndex) +
											 weightDir * dir);
				}

				binSelectedIndexes[i] = -1;
				double minCost = -1;
				for (int j = 0; j < candidateBinIndexes.Count; j++) {
					if (costBins[j] < minCost || minCost == -1) {
						binSelectedIndexes[i] = candidateBinIndexes[j];
						minCost = costBins[j];
					}
				}

				pathShifts[i] += binSize * (binMidIndex - binSelectedIndexes[i]) * pathPointTangents[i].RotateM90().Normalize();
			}

			for (int i = 1; i < pathPoints.Count-1; i++) {
				if (binSelectedIndexes[i] != binSelectedIndexes[i - 1] && binSelectedIndexes[i] != binSelectedIndexes[i + 1])
					binSelectedIndexes[i] = binSelectedIndexes[i - 1];
			}

			for (int i = 0; i < pathPoints.Count; i++) {
				pathPoints[i] += binSize * (binSelectedIndexes[i] - binMidIndex) * pathPointTangents[i].RotateM90().Normalize();
			}

			// generate path with points
			CubicBezier[] beziers = SmoothingSpline.BuildC2Spline(pathPoints.ToArray(), null, null, 0.5);
			avoidancePath = new Path();
			for (int i = 0; i < beziers.Length; i++) {
				avoidancePath.Add(new BezierPathSegment(beziers[i], (double?)null, false));
			}
			aboutPath = AboutPath.Normal;
		}

		/// <summary>
		/// Defines the sensor region to group obstacles
		/// </summary>
		/// <param name="currentPath"></param>
		/// <returns></returns>
		private void DefineSensorRegion(Path currentPath) {

			// left and right limits of sensor region
			double leftLimit  = (leftLaneValid  ? leftLaneWidth  : 0) + currentLaneWidth / 2;
			double rightLimit = (rightLaneValid ? rightLaneWidth : 0) + currentLaneWidth / 2;

			// get polygon around path
			sensorPolygon = GetPathPolygon(currentPath, currentLanePosition, maxPlanDist + TahoeParams.FL, 5, leftLimit, rightLimit);
		}

		/// <summary>
		/// Get polygon surrounding a path
		/// </summary>
		/// <param name="path"></param>
		/// <param name="startPosition"></param>
		/// <param name="lookaheadDist"></param>
		/// <param name="stepDist"></param>
		/// <param name="leftLimit"></param>
		/// <param name="rightLimit"></param>
		/// <returns></returns>
		private Polygon GetPathPolygon(Path path, PointOnPath startPosition, 
																	 double lookaheadDist, double stepDist, double leftLimit, double rightLimit) {
			
			// obtain points along the path and their tangents
			List<Coordinates> pathPoints, pathPointTangents;
			List<double> pathPointDistances;
			GetPointsOnPath(path, startPosition, lookaheadDist, stepDist,
											out pathPoints, out pathPointTangents, out pathPointDistances);

			// left and right boundaries of region
			List<Coordinates> leftPoints = new List<Coordinates>();
			List<Coordinates> rightPoints = new List<Coordinates>();
			for (int i = 0; i < pathPoints.Count; i++) {
				leftPoints.Add(pathPoints[i] + pathPointTangents[i].Rotate90().Normalize(leftLimit));
				rightPoints.Insert(0, pathPoints[i] + pathPointTangents[i].RotateM90().Normalize(rightLimit));
			}

			// obtain points for sensor region
			leftPoints.AddRange(rightPoints);

			// prune points defining region
			Coordinates prevPoint;
			Coordinates nextPoint;
			double prevAngle;
			double nextAngle;
			double diffAngle;
			for (int i = 1; i < leftPoints.Count - 1; i++) {
				prevPoint = leftPoints[i - 1] - leftPoints[i];
				nextPoint = leftPoints[i + 1] - leftPoints[i];
				prevAngle = prevPoint.ArcTan;
				nextAngle = nextPoint.ArcTan;
				if (prevAngle < 0) prevAngle += 2 * Math.PI;
				if (nextAngle < 0) nextAngle += 2 * Math.PI;
				if (prevAngle > nextAngle)
					diffAngle = prevAngle - nextAngle;
				else
					diffAngle = nextAngle - prevAngle;

				if (Math.Abs(diffAngle - Math.PI) < 5 * Math.PI / 180) {
					leftPoints.RemoveAt(i);
					i--;
				}
			}

			// define sensor polygon
			return new Polygon(leftPoints);
		}

		/// <summary>
		/// Get the points along a path and their tangents and distances
		/// </summary>
		/// <param name="path">path to find points on</param>
		/// <param name="startPoint">point on path to start from</param>
		/// <param name="lookaheadDist">distance to lookahead for points</param>
		/// <param name="stepDist">step distance between points on path</param>
		/// <param name="pathPoints">return points on path</param>
		/// <param name="pathPointTangents">returns tangents of points on paths</param>
		private void GetPointsOnPath(Path path, PointOnPath startPoint, double lookaheadDist, double stepDist, 
																 out List<Coordinates> pathPoints,
																 out List<Coordinates> pathPointTangents,
																 out List<double>      pathPointDistances) {

			double tStep;
			double remDist, travelDist = 0;
			pathPoints = new List<Coordinates>();
			pathPointTangents  = new List<Coordinates>();
			pathPointDistances = new List<double>();

			// retrieve current segment
			int segmentIndex = path.IndexOf(startPoint.segment);
			BezierPathSegment segment = (BezierPathSegment)path[segmentIndex];

			// determine t for start point when segment length is normalized to 1
			double t = startPoint.dist / segment.Length;

			// add start point and tangent
			pathPoints.Add(startPoint.pt);
			pathPointTangents.Add(segment.Bezier.dBdt(t));
			pathPointDistances.Add(0);

			// travel along path to find points until lookahead distance is reached
			while (travelDist < lookaheadDist) {
				// determine time step for step distance when segment length is normalized to 1
				tStep = stepDist / segment.Length;

				// increment t to move along path by step distance
				t += tStep;

				// check if reached end of current segment
				if (t < 1) {
					// still on current segment

					// add path point
					pathPoints.Add(segment.Bezier.Bt(t));
					
					// update distance travelled along path
					travelDist += stepDist;
				}
				else {
					// reached end of current segment

					t -= 1; // remaining t
					
					// determine remaining distance on next segment
					remDist = t * segment.Length;

					// update distance travelled so far on current segment
					travelDist += stepDist - remDist;
					
					// increment segment index
					segmentIndex += 1;

					// check if reached end of path
					if (segmentIndex < path.Count) {
						// not at end of path yet

						// retrieve next segment
						segment = (BezierPathSegment)path[segmentIndex];

						// determine t for remaining distance when segment length is normalized to 1
						t = remDist / segment.Length;

						// add path point
						pathPoints.Add(segment.Bezier.Bt(t));
					}
					else {
						// at end of path

						remDist = lookaheadDist - travelDist;

						// add extended path point
						pathPoints.Add(segment.End + segment.Bezier.dBdt(t).Normalize(remDist));
					}

					// update remaining distance tarvelled along path
					travelDist += remDist;
				}

				// add path point tangent and distance
				pathPointTangents.Add(segment.Bezier.dBdt(t));
				pathPointDistances.Add(travelDist);
			}
		}

		// evaluate paths and select safest path
		private int EvaluatePaths(Path[] paths, int goalPathIndex,
															out double[] pathsRisk, 
															out double[] pathsRiskDist, 
															out double[] pathsSepDist,
															out double[] pathsCost) {
			
			int numPaths = paths.Length;
			int selectedPathIndex;
			pathsRisk			= new double[numPaths];
			pathsRiskDist = new double[numPaths];
			pathsSepDist	= new double[numPaths];
			pathsCost			= new double[numPaths];

			// determine risk of spline path
			for (int i = 0; i < numPaths; i++) {
				CheckPathRisk(paths[i], out pathsRisk[i], out pathsRiskDist[i], out pathsSepDist[i]);
			}

			// find minimum path risk (0 means it is a safe path, non-zero means it has some risk
			double minPathRisk = -1;
			for (int i = 0; i < numPaths; i++) {
				pathsRisk[i] = Math.Round(pathsRisk[i], 3);
				if (pathsRisk[i] < minPathRisk || minPathRisk == -1)
					minPathRisk = pathsRisk[i];
			}

			// select candidate paths and set up their cost
			for (int i = 0; i < numPaths; i++) {
				if (pathsRisk[i] == minPathRisk)
					pathsCost[i] = 0;
				else
					pathsCost[i] = -1;
			}

			// find cost of candidate paths
			double weightDev = 5;   // weight for path deviation penalty
			double weightDir = 1;   // weight for left path penalty
			for (int i = 0; i < numPaths; i++) {

				// skip paths with risk in first spline
				if (pathsCost[i] < 0)
					continue;

				double dir;
				if (i < goalPathIndex)
					dir = 1;
				else
					dir = 0;

				pathsCost[i] = weightDev * Math.Abs(i - goalPathIndex) +
											 weightDir * dir;
			}

			// find index of path to select
			selectedPathIndex  = -1;
			double minPathCost = -1;
			for (int i = 0; i < numPaths; i++) {
				if (pathsCost[i] < 0)
					continue;

				if (pathsCost[i] < minPathCost || minPathCost == -1) {
					selectedPathIndex = i;
					minPathCost = pathsCost[i];
				}
			}

			return selectedPathIndex;
		}

		#region  Site Visit Function Stubs

    #region Lane Obstacle Reasoning

    /// <summary>
    /// Produces an obstacle reasoning path for lane type situations (Version 1)
    /// </summary>
    /// <param name="leftLanePath">The path representing the left lane</param>
    /// <param name="leftLaneIsOncoming">Whether the left lane path is oncoming or not</param>
    /// <param name="leftLaneVehicles">The vehicles referenced to the left lane</param>
    /// <param name="currentLaneDefaultPath">The default path for the current lane</param>
    /// <param name="rightLanePath">The path of the right lane, always going in our same direction</param>
    /// <param name="rightLaneVehicles">The vehicles referenced to the right lane</param>
    /// <param name="vehicleState">Our current vehicle state</param>
    /// <returns>A modified lane path that avoids the vehicles in the adjacent lanes while staying in the current lane</returns>
    public Path LaneObstacleReasoningVer1(Path leftLanePath, bool leftLaneIsOncoming,
																					ObservedVehicle[] leftLaneVehicles, 
																				  Path currentLanePath, 
																					Path rightLanePath, 
																					ObservedVehicle[] rightLaneVehicles, 
																					VehicleState vehicleState) {

			// set up vehicle and lane information
			InitialiseInformation(vehicleState.xyPosition, vehicleState.heading, vehicleState.speed, 
														leftLanePath, currentLanePath, rightLanePath);

			// set up static obstacles (none for now)
			staticObstaclesIn.Clear();
			staticObstaclesOut.Clear();
			staticObstaclesFake.Clear();

			// set up dynamic obstacles
			dynamicObstacles.Clear();
			dynamicObstaclesPaths.Clear();
			SetDynamicObstacles(leftLanePath,  leftLaneVehicles);
			SetDynamicObstacles(rightLanePath, rightLaneVehicles);

			double projectionDist = Math.Max(vehicleSpeed * 3, 10) + TahoeParams.FL;
			double origProjectionDist = projectionDist;

			// set up number of paths based on lane width
			double spacing = 0.25;
			int numPaths = (int)Math.Round(currentLaneWidth / spacing);
			if ((int)Math.IEEERemainder((double)numPaths, 2.0) == 0)
				numPaths -= 1;

			// increase number of drift paths
			int midPathIndex;
			if (leftLaneIsOncoming == true) {
				midPathIndex = (numPaths - 1) / 2;
				numPaths += 6;
			}
			else {
				numPaths += 12;
				midPathIndex = (numPaths - 1) / 2;
			}			

			double[] pathsRisk, pathsRiskDist, pathsSepDist, pathsCost;
			Path[] paths = new Path[numPaths];
			int selectedPathIndex;

			do {
				// lookahead point
				double lookaheadDist = projectionDist;
				PointOnPath lookaheadPt = currentLanePath.AdvancePoint(currentLanePosition, ref lookaheadDist);

				// extend point if at end of path
				Coordinates offsetVec = new Coordinates(0, 0);
				if (lookaheadDist > 0.5)
					offsetVec = lookaheadPt.segment.Tangent(lookaheadPt).Normalize(lookaheadDist);

				// prepare ctrl points for spline path
				Coordinates startPoint = vehiclePosition;
				Coordinates endPoint = lookaheadPt.pt + offsetVec;
				Coordinates startVec = new Coordinates(1, 0).Rotate(vehicleHeading).Normalize(Math.Max(vehicleSpeed, 2.0));
				Coordinates endVec = lookaheadPt.segment.Tangent(lookaheadPt).Normalize(Math.Max(vehicleSpeed, 2.0));

				Coordinates shiftVec = endVec.Rotate90();

				// generate multiple spline paths
				for (int i = 0; i < numPaths; i++) {
					// generate spline path
					paths[i] = GenerateBezierPath(startPoint, endPoint - shiftVec.Normalize((i - midPathIndex) * spacing), startVec, endVec);
				}

				// evaluate paths and select safest path
				selectedPathIndex = EvaluatePaths(paths, midPathIndex, out pathsRisk, out pathsRiskDist, out pathsSepDist, out pathsCost);

				if (pathsRisk[selectedPathIndex] != 0)
					projectionDist = Math.Max(pathsRiskDist[selectedPathIndex] - 1, 0);

			} while (pathsRisk[selectedPathIndex] != 0 && projectionDist > 7.5);

			// return back safest path
			return paths[selectedPathIndex];
		}

		/// <summary>
		/// Produces an obstacle reasoning path for lane type situations (Version 2a)
		/// </summary>
		/// <param name="leftLanePath">The path representing the left lane</param>
		/// <param name="leftLaneIsOncoming">Whether the left lane path is oncoming or not</param>
		/// <param name="leftLaneVehicles">The vehicles referenced to the left lane</param>
		/// <param name="currentLaneDefaultPath">The default path for the current lane</param>
		/// <param name="rightLanePath">The path of the right lane, always going in our same direction</param>
		/// <param name="rightLaneVehicles">The vehicles referenced to the right lane</param>
		/// <param name="vehicleState">Our current vehicle state</param>
		/// <returns>A modified lane path that avoids the vehicles in the adjacent lanes while staying in the current lane</returns>
		public Path LaneObstacleReasoningVer2a(Path leftLanePath, bool leftLaneIsOncoming,
																					 ObservedVehicle[] leftLaneVehicles,
																					 Path currentLanePath,
																					 Path rightLanePath,
																					 ObservedVehicle[] rightLaneVehicles,
																					 VehicleState vehicleState) {

			// set up vehicle and lane information
			InitialiseInformation(vehicleState.xyPosition, vehicleState.heading, vehicleState.speed,
														leftLanePath, currentLanePath, rightLanePath);

			// set up static obstacles (none for now)
			staticObstaclesIn.Clear();
			staticObstaclesOut.Clear();
			staticObstaclesFake.Clear();

			// set up dynamic obstacles
			dynamicObstacles.Clear();
			dynamicObstaclesPaths.Clear();
			SetDynamicObstacles(leftLanePath, leftLaneVehicles);
			SetDynamicObstacles(rightLanePath, rightLaneVehicles);

			double projectionDist = Math.Max(vehicleSpeed * 3, 10) + TahoeParams.FL;
			double origProjectionDist = projectionDist;

			// set up number of paths based on lane width
			double spacing = 0.25;
			int numPaths = (int)Math.Round(currentLaneWidth / spacing);
			if ((int)Math.IEEERemainder((double)numPaths, 2.0) == 0)
				numPaths -= 1;

			// increase number of drift paths
			int midPathIndex;
			if (leftLaneIsOncoming == true) {
				midPathIndex = (numPaths - 1) / 2;
				numPaths += 6;
			}
			else {
				numPaths += 12;
				midPathIndex = (numPaths - 1) / 2;
			}			
			
			double[] pathsRisk, pathsRiskDist, pathsSepDist, pathsCost;
			Path[] paths = new Path[numPaths];
			int selectedPathIndex;

			do {
				// lookahead point
				double lookaheadDist = projectionDist;
				PointOnPath lookaheadPt = currentLanePath.AdvancePoint(currentLanePosition, ref lookaheadDist);

				// extend point if at end of path
				Coordinates offsetVec = new Coordinates(0, 0);
				if (lookaheadDist > 0.5)
					offsetVec = lookaheadPt.segment.Tangent(lookaheadPt).Normalize(lookaheadDist);

				// prepare ctrl points for spline path
				Coordinates startPoint = vehiclePosition;
				Coordinates endPoint = lookaheadPt.pt + offsetVec;
				Coordinates startVec = new Coordinates(1, 0).Rotate(vehicleHeading).Normalize(Math.Max(vehicleSpeed, 2.0));
				Coordinates endVec = lookaheadPt.segment.Tangent(lookaheadPt).Normalize(Math.Max(vehicleSpeed, 2.0));

				// lookahead point
				lookaheadDist = projectionDist / 2;
				lookaheadPt = currentLanePath.AdvancePoint(currentLanePosition, ref lookaheadDist);

				// extend point if at end of path
				offsetVec = new Coordinates(0, 0);
				if (lookaheadDist > 0.5)
					offsetVec = lookaheadPt.segment.Tangent(lookaheadPt).Normalize(lookaheadDist);

				// prepare ctrl points for spline path
				Coordinates midPoint = lookaheadPt.pt + offsetVec;
				Coordinates midVec = lookaheadPt.segment.Tangent(lookaheadPt).Normalize(Math.Max(vehicleSpeed, 2.0));

				Coordinates shiftMidVec = midVec.Rotate90();
				Coordinates shiftEndVec = endVec.Rotate90();

				// generate multiple spline paths
				for (int i = 0; i < numPaths; i++) {

					// vehicle vector with respect to segment closest point
					Coordinates carVec = vehiclePosition - currentLanePosition.pt;
					// segment tangent vector
					Coordinates pathVec = currentLanePosition.segment.Tangent(currentLanePosition);
					// compute offtrack error
					double offtrackError = Math.Sign(carVec.Cross(pathVec)) * currentLanePosition.pt.DistanceTo(vehiclePosition);

					// path points
					Coordinates[] pathPoints = new Coordinates[3];
					pathPoints[0] = startPoint;
					pathPoints[1] = midPoint - shiftMidVec.Normalize(((i - midPathIndex) * spacing + offtrackError) * 0.5);
					pathPoints[2] = endPoint - shiftEndVec.Normalize((i - midPathIndex) * spacing);

					// generate spline path with points
					paths[i] = new Path();
					CubicBezier[] beziers = SmoothingSpline.BuildC2Spline(pathPoints, startVec.Normalize(0.5 * projectionDist), 
																																endVec.Normalize(0.5 * projectionDist), 0.5);
					for (int j = 0; j < beziers.Length; j++) {
						paths[i].Add(new BezierPathSegment(beziers[j], (double?)null, false));
					}
				}

				// evaluate paths and select safest path
				selectedPathIndex = EvaluatePaths(paths, midPathIndex, out pathsRisk, out pathsRiskDist, out pathsSepDist, out pathsCost);

				if (pathsRisk[selectedPathIndex] != 0)
					projectionDist = Math.Max(pathsRiskDist[selectedPathIndex] - 1, 0);

			} while (pathsRisk[selectedPathIndex] != 0 && projectionDist > 7.5);

			// return back safest path
			return paths[selectedPathIndex];
		}

		/// <summary>
		/// Produces an obstacle reasoning path for lane type situations (Version 2b - Latest)
		/// </summary>
		/// <param name="leftLanePath">The path representing the left lane</param>
		/// <param name="leftLaneIsOncoming">Whether the left lane path is oncoming or not</param>
		/// <param name="leftLaneVehicles">The vehicles referenced to the left lane</param>
		/// <param name="currentLaneDefaultPath">The default path for the current lane</param>
		/// <param name="rightLanePath">The path of the right lane, always going in our same direction</param>
		/// <param name="rightLaneVehicles">The vehicles referenced to the right lane</param>
		/// <param name="vehicleState">Our current vehicle state</param>
		/// <returns>A modified lane path that avoids the vehicles in the adjacent lanes while staying in the current lane</returns>
		public Path LaneObstacleReasoning(Path leftLanePath, bool leftLaneIsOncoming,
																			ObservedVehicle[] leftLaneVehicles,
																			Path currentLanePath,
																			Path rightLanePath,
																			ObservedVehicle[] rightLaneVehicles,
																			VehicleState vehicleState) {

			// set up vehicle and lane information
			InitialiseInformation(vehicleState.xyPosition, vehicleState.heading, vehicleState.speed,
														leftLanePath, currentLanePath, rightLanePath);

			// set up static obstacles (none for now)
			staticObstaclesIn.Clear();
			staticObstaclesOut.Clear();
			staticObstaclesFake.Clear();

			// set up dynamic obstacles
			dynamicObstacles.Clear();
			dynamicObstaclesPaths.Clear();
			SetDynamicObstacles(leftLanePath, leftLaneVehicles);
			SetDynamicObstacles(rightLanePath, rightLaneVehicles);

			double projectionDist = Math.Max(vehicleSpeed * 5, 10) + TahoeParams.FL;
			double origProjectionDist = projectionDist;

			// set up number of paths based on lane width
			double spacing = 0.25;
			int numPaths = (int)Math.Round(currentLaneWidth / spacing);
			if ((int)Math.IEEERemainder((double)numPaths, 2.0) == 0)
				numPaths -= 1;

			// increase number of drift paths
			int midPathIndex;
			if (leftLaneIsOncoming == true) {
				midPathIndex = (numPaths - 1) / 2;
				numPaths += 6;
			}
			else {
				numPaths += 12;
				midPathIndex = (numPaths - 1) / 2;
			}

			double[] pathsRisk, pathsRiskDist, pathsSepDist, pathsCost;
			Path[] paths = new Path[numPaths];
			int selectedPathIndex;

			do {
				// lookahead point
				double lookaheadDist = projectionDist;
				PointOnPath lookaheadPt = currentLanePath.AdvancePoint(currentLanePosition, ref lookaheadDist);

				// extend point if at end of path
				Coordinates offsetVec = new Coordinates(0, 0);
				if (lookaheadDist > 0.5)
					offsetVec = lookaheadPt.segment.Tangent(lookaheadPt).Normalize(lookaheadDist);

				// prepare ctrl points for spline path
				Coordinates startPoint = vehiclePosition;
				Coordinates endPoint = lookaheadPt.pt + offsetVec;
				Coordinates startVec = new Coordinates(1, 0).Rotate(vehicleHeading).Normalize(Math.Max(vehicleSpeed, 2.0));
				Coordinates endVec = lookaheadPt.segment.Tangent(lookaheadPt).Normalize(Math.Max(vehicleSpeed, 2.0));

				// generate mid points of path
				int midPointsTotal = (int)Math.Round(projectionDist / 5.0) - 1;
				double midPointStepDist = projectionDist / (midPointsTotal + 1);				
				Coordinates[] midPoints = new Coordinates[midPointsTotal];
				Coordinates[] midVecs		= new Coordinates[midPointsTotal];
				Coordinates[] midShiftVecs = new Coordinates[midPointsTotal];
				for (int i = 0; i < midPointsTotal; i++) {
					// lookahead point
					lookaheadDist = projectionDist * (i + 1) / (midPointsTotal + 1);
					lookaheadPt = currentLanePath.AdvancePoint(currentLanePosition, ref lookaheadDist);

					// extend point if at end of path
					offsetVec = new Coordinates(0, 0);
					if (lookaheadDist > 0.5)
						offsetVec = lookaheadPt.segment.Tangent(lookaheadPt).Normalize(lookaheadDist);

					// prepare ctrl points for spline path
					midPoints[i] = lookaheadPt.pt + offsetVec;
					midVecs[i]	 = lookaheadPt.segment.Tangent(lookaheadPt).Normalize(Math.Max(vehicleSpeed, 2.0));
					midShiftVecs[i] = midVecs[i].Rotate90();
				}

				Coordinates endShiftVec = endVec.Rotate90();

				// generate multiple spline paths
				for (int i = 0; i < numPaths; i++) {

					// vehicle vector with respect to segment closest point
					Coordinates carVec = vehiclePosition - currentLanePosition.pt;
					// segment tangent vector
					Coordinates pathVec = currentLanePosition.segment.Tangent(currentLanePosition);
					// compute offtrack error
					double offtrackError = Math.Sign(carVec.Cross(pathVec)) * currentLanePosition.pt.DistanceTo(vehiclePosition);

					// path points
					Coordinates[] pathPoints = new Coordinates[midPointsTotal + 2];
					pathPoints[0] = startPoint;
					pathPoints[midPointsTotal + 1] = endPoint - endShiftVec.Normalize((i - midPathIndex) * spacing);
					for (int j = 0; j < midPointsTotal; j++) {
						double control = 0.0;
						if (j == 0)
							control = 0.35;
						else if (j == midPointsTotal - 1)
							control = -0.35;

						pathPoints[j+1] = midPoints[j] -
							midShiftVecs[j].Normalize((i - midPathIndex) * spacing * (j + 1 - control) / (midPointsTotal + 1) +
																				offtrackError * (midPointsTotal - j + control) / (midPointsTotal + 1));
					}

					// generate spline path with points
					paths[i] = new Path();
					CubicBezier[] beziers = SmoothingSpline.BuildC2Spline(pathPoints, startVec.Normalize(0.5 * midPointStepDist),
																																endVec.Normalize(0.5 * midPointStepDist), 0.5);
					for (int j = 0; j < beziers.Length; j++) {
						paths[i].Add(new BezierPathSegment(beziers[j], (double?)null, false));
					}
				}

				// evaluate paths and select safest path
				selectedPathIndex = EvaluatePaths(paths, midPathIndex, out pathsRisk, out pathsRiskDist, out pathsSepDist, out pathsCost);

				if (pathsRisk[selectedPathIndex] != 0)
					projectionDist = Math.Max(pathsRiskDist[selectedPathIndex] - 1, 0);

			} while (pathsRisk[selectedPathIndex] != 0 && projectionDist > 7.5);

			// return back safest path
			//int index = DateTime.Now.Second;
			//selectedPathIndex = index - (paths.Length - 1) * (int)Math.Floor((double)index / (paths.Length - 1));
			return paths[selectedPathIndex];
		}

    #endregion

    #region Intersection Obstacle Reasoning

    /// <summary>
    /// Produces an obstacle reasoning path for an intersection situation
    /// </summary>
    /// <param name="originalTurnPath">The default turn path for the turn</param>
    /// <param name="entryAdjacentLanePath">The path defining the lane that is adjacent to the entry lane</param>
    /// <param name="entryPath">The path we will follow in the entry lane</param>
    /// <param name="entryAdjacentLaneVehicles">The vehicles in the lane adjacent to the entry we are traveling to</param>
    /// <param name="vehicleState">Our current vehicle state</param>
    /// <returns>A modified turn path to follow that avoids the obstacles in the entry's adjacent lane</returns>
		public Path IntersectionObstacleReasoning(Path originalTurnPath,
																							Path entryAdjacentLanePath, Path entryPath,
																							ObservedVehicle[] entryAdjacentLaneVehicles,
																							VehicleState vehicleState) {

			// set up vehicle and lane information
			InitialiseInformation(vehicleState.xyPosition, vehicleState.heading, vehicleState.speed,
														null, originalTurnPath, null);

			// set up static obstacles (none for now)
			staticObstaclesIn.Clear();
			staticObstaclesOut.Clear();
			staticObstaclesFake.Clear();

			// set up dynamic obstacles
			dynamicObstacles.Clear();
			dynamicObstaclesPaths.Clear();
			SetDynamicObstacles(entryAdjacentLanePath, entryAdjacentLaneVehicles);

			// set up number of paths based on lane width
			double spacing = 0.25;
			int numPaths = (int)Math.Round(currentLaneWidth / spacing);
			if ((int)Math.IEEERemainder((double)numPaths, 2.0) == 0)
				numPaths -= 1;

			// increase number of drift paths
			int midPathIndex;
			numPaths += 12;
			midPathIndex = (numPaths - 1) / 2;

			double[] pathsRisk, pathsRiskDist, pathsSepDist, pathsCost;
			Path[] paths = new Path[numPaths];

			// path shift vector
			Coordinates pathStartVec = originalTurnPath[0].Tangent(originalTurnPath.StartPoint);
			Coordinates pathEndVec	 = originalTurnPath[originalTurnPath.Count - 1].Tangent(originalTurnPath.EndPoint);
			Coordinates shiftVec = Math.Sign(pathEndVec.Cross(pathStartVec)) * originalTurnPath[0].Tangent(originalTurnPath.StartPoint);

			// generate multiple paths
			for (int i = 0; i < numPaths; i++) {
				// determine path shift vector
				Coordinates sVec = shiftVec.Normalize((i - midPathIndex) * spacing);

				// generate path
				paths[i] = new Path();
				BezierPathSegment bezSeg = (BezierPathSegment)originalTurnPath[0];
				paths[i].Add(new BezierPathSegment(bezSeg.cb.P0, bezSeg.cb.P1, 
																					 bezSeg.cb.P2 - sVec, bezSeg.cb.P3 - sVec, (double?)null, false));
				for (int j = 1; j < originalTurnPath.Count; j++) {
					bezSeg = (BezierPathSegment)originalTurnPath[j];
					paths[i].Add(new BezierPathSegment(bezSeg.cb.P0 - sVec, bezSeg.cb.P1 - sVec, 
																						 bezSeg.cb.P2 - sVec, bezSeg.cb.P3 - sVec, (double?)null, false));
				}
			}

			// evaluate paths and select safest path
			int selectedPathIndex = EvaluatePaths(paths, midPathIndex, out pathsRisk, out pathsRiskDist, out pathsSepDist, out pathsCost);

			// check if path without risk was found
			if (pathsRisk[selectedPathIndex] == 0)
				return paths[selectedPathIndex];
			else
				return null;
		}

    #endregion

		#region Lane Change Obstacle Reasoning

		/// <summary>
		/// Reason about changing lanes
		/// </summary>
		/// <param name="previousChangeLanePath">previous change lane path</param>
		/// <param name="initialLanePath">lane path that vehicle is changing from</param>
		/// <param name="targetLanePath">	lane path that vehicle is changing to</param>
		/// <param name="targetType">type for target lane, left or right</param>
		/// <param name="initialLaneVehicles">observed vehicles on initial lane</param>
		/// <param name="initialLaneLowerBound">lower bound point on initial lane (similar to obstacle on target lane)</param>
		/// <param name="initialLaneUpperBound">upper bound point on initial lane (similar to obstacle on initial lane)</param>
		/// <param name="vehicleState">vehicle state</param>
		public Path LaneChangeObstacleReasoning(Path previousChangeLanePath,
																						Path initialLanePath, Path targetLanePath,
																						TargetLaneChangeType targetType,
																						ObservedVehicle[] initialLaneVehicles,
																						PointOnPath initialLaneLowerBound, 
																						PointOnPath initialLaneUpperBound,
																						VehicleState vehicleState) {

			// check if target lane is to the left or right
			if (targetType == TargetLaneChangeType.Left) {
				// set up vehicle and lane information
				InitialiseInformation(vehicleState.xyPosition, vehicleState.heading, vehicleState.speed,
															null, targetLanePath, initialLanePath);
			}
			else {
				// set up vehicle and lane information
				InitialiseInformation(vehicleState.xyPosition, vehicleState.heading, vehicleState.speed,
															initialLanePath, targetLanePath, null);
			}

			// set up static obstacles (none for now)
			staticObstaclesIn.Clear();
			staticObstaclesOut.Clear();
			staticObstaclesFake.Clear();

			// set up dynamic obstacles
			dynamicObstacles.Clear();
			dynamicObstaclesPaths.Clear();
			SetDynamicObstacles(initialLanePath, initialLaneVehicles);

			// determine risk of previous spline path, if provided
			double pathRisk, pathRiskDist, pathSepDist;
			if (previousChangeLanePath != null) {
				// check risk of  previous spline path
				CheckPathRisk(previousChangeLanePath, out pathRisk, out pathRiskDist, out pathSepDist);

				// if no risk was found, return previous spline path
				if (pathRisk == 0)					
					return previousChangeLanePath;
			}

			// set up number of paths based on lane width
			double spacing = 0.25;
			int numPaths = (int)Math.Round(currentLaneWidth / spacing);
			if ((int)Math.IEEERemainder((double)numPaths, 2.0) == 0)
				numPaths -= 1;

			// increase number of drift paths
			int midPathIndex;
			numPaths += 12;
			midPathIndex = (numPaths - 1) / 2;

			double[] pathsRisk, pathsRiskDist, pathsSepDist, pathsCost;
			Path[] paths = new Path[numPaths];
			int selectedPathIndex;

			PointOnPath targetLaneLowerBound = targetLanePath.GetClosest(initialLaneLowerBound.pt);
			PointOnPath targetLaneUpperBound = targetLanePath.GetClosest(initialLaneUpperBound.pt);
			double targetLaneLowerBoundDist = Math.Round(targetLanePath.DistanceBetween(currentLanePosition, targetLaneLowerBound), 1);
			double targetLaneUpperBoundDist = Math.Round(targetLanePath.DistanceBetween(currentLanePosition, targetLaneUpperBound), 1);

			// generate obstacles for lower and upper bound points
			Coordinates lowerBoundObstacle = targetLaneLowerBound.pt;
			Coordinates upperBoundObstacle = initialLaneUpperBound.pt;
			if (targetType == TargetLaneChangeType.Left) {
				lowerBoundObstacle += targetLaneLowerBound.segment.Tangent(targetLaneLowerBound).RotateM90().Normalize(0.5 * currentLaneWidth - 1.0);
				upperBoundObstacle += initialLaneUpperBound.segment.Tangent(initialLaneUpperBound).Rotate90().Normalize(0.5 * rightLaneWidth - 1.0);
			}
			else {
				lowerBoundObstacle += targetLaneLowerBound.segment.Tangent(targetLaneLowerBound).Rotate90().Normalize(0.5 * currentLaneWidth - 1.0);
				upperBoundObstacle += initialLaneUpperBound.segment.Tangent(initialLaneUpperBound).RotateM90().Normalize(0.5 * leftLaneWidth - 1.0);
			}
			staticObstaclesFake.Add(lowerBoundObstacle);
			staticObstaclesFake.Add(upperBoundObstacle);

			// path projection distance
			double projectionDist = Math.Max(targetLaneLowerBoundDist, TahoeParams.VL + TahoeParams.FL);
			double origProjectionDist = projectionDist;

			Path currentChangeLanePath = new Path();

			do {
				// lookahead point
				double lookaheadDist = projectionDist;
				PointOnPath lookaheadPt = targetLanePath.AdvancePoint(currentLanePosition, ref lookaheadDist);

				// extend point if at end of path
				Coordinates offsetVec = new Coordinates(0, 0);
				if (lookaheadDist > 0.5)
					offsetVec = lookaheadPt.segment.Tangent(lookaheadPt).Normalize(lookaheadDist);

				// prepare ctrl points for first part of spline path
				Coordinates startPoint = vehiclePosition;
				Coordinates midPoint	 = lookaheadPt.pt + offsetVec;
				Coordinates startVec	 = new Coordinates(1, 0).Rotate(vehicleHeading).Normalize(Math.Max(vehicleSpeed, 2.0));
				Coordinates midVec		 = lookaheadPt.segment.Tangent(lookaheadPt).Normalize(Math.Max(vehicleSpeed, 2.0));

				// lookahead point (for end point)
				lookaheadDist = projectionDist + 10;
				lookaheadPt = targetLanePath.AdvancePoint(currentLanePosition, ref lookaheadDist);

				// extend point if at end of path (for end point)
				offsetVec = new Coordinates(0, 0);
				if (lookaheadDist > 0.5)
					offsetVec = lookaheadPt.segment.Tangent(lookaheadPt).Normalize(lookaheadDist);

				// prepare ctrl points for second part of spline path
				Coordinates endPoint = lookaheadPt.pt + offsetVec;
				Coordinates endVec	 = lookaheadPt.segment.Tangent(lookaheadPt).Normalize(Math.Max(vehicleSpeed, 2.0));

				/////////////////////////////////
				Coordinates shiftedMidPoint, shiftedEndPoint;
				Coordinates shiftMidVec = midVec.Rotate90();
				Coordinates shiftEndVec = endVec.Rotate90();

				// generate multiple spline paths
				for (int i = 0; i < numPaths; i++) {
					shiftedMidPoint = midPoint - shiftMidVec.Normalize((i - midPathIndex) * spacing);
					shiftedEndPoint = endPoint - shiftEndVec.Normalize((i - midPathIndex) * spacing);

					// generate spline path
					paths[i] = GenerateBezierPath(startPoint, shiftedMidPoint, startVec, midVec);
					
					// generate extension to spline path
					Path extPath = GenerateBezierPath(shiftedMidPoint, shiftedEndPoint, midVec, endVec);

					// add extension to path
					paths[i].Add((BezierPathSegment)extPath[0]);
				}

				// evaluate paths and select safest path
				selectedPathIndex = EvaluatePaths(paths, midPathIndex, out pathsRisk, out pathsRiskDist, out pathsSepDist, out pathsCost);

				// project further if current spline path has risk
				if (pathsRisk[selectedPathIndex] != 0) {
					if (projectionDist == targetLaneUpperBoundDist + TahoeParams.RL)
						break;

					projectionDist = Math.Min(projectionDist + TahoeParams.VL / 2, targetLaneUpperBoundDist + TahoeParams.RL);
				}

			} while (pathsRisk[selectedPathIndex] != 0 && projectionDist <= targetLaneUpperBoundDist + TahoeParams.RL);

			// check if path without risk was found
			if (pathsRisk[selectedPathIndex] == 0)
				return paths[selectedPathIndex];
			else
				return null;
		}

		#endregion

		#endregion
	}
}
