using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Sensors;
using System.Threading;
using UrbanChallenge.MessagingService;
using OperationalLayer.Communications;
using UrbanChallenge.Common.Pose;
using OperationalLayer.CarTime;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common.Mapack;
using UrbanChallenge.Common.Utility;
using UrbanChallenge.PathSmoothing;
using OperationalLayer.Tracing;
using UrbanChallenge.Behaviors;

namespace OperationalLayer.Obstacles {
	class ObstaclePipeline {
		private AutoResetEvent newDataEvent;
		private SceneEstimatorUntrackedClusterCollection currentUntrackedClusters;
		private SceneEstimatorTrackedClusterCollection currentTrackedClusters;
		private SideObstacles currentLeftSideObstacles;
		private SideObstacles currentRightSideObstacles;

		private SceneEstimatorUntrackedClusterCollection queuedUntrackedClusters;
		private SceneEstimatorTrackedClusterCollection queuedTrackedClusters;

		private bool haveReceivedTrackedClusters = false;

		private ObstacleCollection processedObstacles;

		private Thread processThread;

		private bool useOccupancyGrid = false;
		private int occupancyDeletedCount = 0;

		private const double extrusion_reverse_dist = 8;
		private const double extrusion_extra_width = 0.2;
		private const double prediction_time = 0.5; // seconds

		private const double target_class_ignore_dist = 10;
		private const double target_class_ignore_age = 20;

		private const double split_area_threshold = 1;
		private const double split_length_threshold = 6;

		private const double merge_expansion_size = 0.3;
		private const double L1_area_threshold = 0.25;

		private static readonly double[] min_spacing;
		private static readonly double[] des_spacing;
		private static Polygon[] conv_polygon;

		private double extraSpacing = 0;
		private List<int> lastIgnoredObstacles = null;
		private bool externalUseOccupancyGrid = true;

		static ObstaclePipeline() {
			min_spacing = new double[6];
			min_spacing[(int)ObstacleClass.StaticSmall] = 0.2;
			min_spacing[(int)ObstacleClass.StaticLarge] = 0.4;
			min_spacing[(int)ObstacleClass.DynamicStopped] = 0.5;
			min_spacing[(int)ObstacleClass.DynamicCarlike] = 0.75;
			min_spacing[(int)ObstacleClass.DynamicNotCarlike] = 0.3;
			min_spacing[(int)ObstacleClass.DynamicUnknown] = 0.3;

			des_spacing = new double[6];
			des_spacing[(int)ObstacleClass.StaticSmall] = 0.4;
			des_spacing[(int)ObstacleClass.StaticLarge] = 0.6;
			des_spacing[(int)ObstacleClass.DynamicStopped] = 0.75;
			des_spacing[(int)ObstacleClass.DynamicCarlike] = 1.0;
			des_spacing[(int)ObstacleClass.DynamicNotCarlike] = 0.5;
			des_spacing[(int)ObstacleClass.DynamicUnknown] = 0.5;

			conv_polygon = new Polygon[6];
			for (int i = 0; i < 6; i++) {
				conv_polygon[i] = MakeConvPolygon(min_spacing[i]);
			}
		}

		private static Polygon MakeConvPolygon(double spacing) {
			Circle c = new Circle(TahoeParams.T/2.0 + spacing, Coordinates.Zero);
			return c.ToPolygon(24);
		}

		public ObstaclePipeline() {
			// initialize the new wait event
			newDataEvent = new AutoResetEvent(false);

			processThread = new Thread(ObstacleThread);
			processThread.IsBackground = true;
			processThread.Name = "Obstacle Processing Thread";
		}

		public bool UseOccupancyGrid {
			get { return externalUseOccupancyGrid; }
			set { externalUseOccupancyGrid = value; }
		}

		public double ExtraSpacing {
			get { return extraSpacing; }
			set {
				if (value < 0)
					value = 0;
				if (extraSpacing != value) {
					Polygon[] new_conv_polygon = new Polygon[6];
					for (int i = 0; i < 6; i++) {
						new_conv_polygon[i] = MakeConvPolygon(min_spacing[i] + value);
					}

					conv_polygon = new_conv_polygon;
					extraSpacing = value;
				}
			}
		}

		public List<int> LastIgnoredObstacles {
			get { return lastIgnoredObstacles; }
			set { lastIgnoredObstacles = value; }
		}

		public void Start() {
			if ((processThread.ThreadState & ThreadState.Unstarted) == ThreadState.Unstarted) {
				processThread.Start();
			}
		}

		public void OnUntrackedClustersReceived(SceneEstimatorUntrackedClusterCollection clusters) {
			currentUntrackedClusters = clusters;
			newDataEvent.Set();
		}

		public void OnTrackedClustersReceived(SceneEstimatorTrackedClusterCollection clusters) {
			currentTrackedClusters = clusters;
			newDataEvent.Set();
		}

		public void OnSideObstaclesReceived(SideObstacles obstacles) {
			if (obstacles.side == SideObstacleSide.Passenger)
				currentRightSideObstacles = obstacles;
			else
				currentLeftSideObstacles = obstacles;
		}

		private void ObstacleThread() {
			for (;;) {
				try {
					SceneEstimatorUntrackedClusterCollection newUntrackedClusters = Interlocked.Exchange(ref currentUntrackedClusters, null);
					SceneEstimatorTrackedClusterCollection newTrackedClusters = Interlocked.Exchange(ref currentTrackedClusters, null);

					if (newUntrackedClusters == null && newTrackedClusters == null) {
						if (!Services.DebuggingService.StepMode) {
							newDataEvent.WaitOne();
						}
						else {
							Services.DebuggingService.WaitOnSequencer(typeof(ObstaclePipeline));
						}

						continue;
					}

					// check if we have a matching pair
					if (newUntrackedClusters != null) {
						queuedUntrackedClusters = newUntrackedClusters;
					}

					if (newTrackedClusters != null) {
						haveReceivedTrackedClusters = true;
						queuedTrackedClusters = newTrackedClusters;
					}

					if (queuedUntrackedClusters == null || (haveReceivedTrackedClusters && (queuedTrackedClusters == null || queuedTrackedClusters.timestamp != queuedUntrackedClusters.timestamp))) {
						continue;
					}

					Rect vehicleBox = DetermineVehicleEraseBox();

					// load in the appropriate stuff to the occupancy grid
					useOccupancyGrid = (Services.OccupancyGrid != null && !Services.OccupancyGrid.IsDisposed);
					if (useOccupancyGrid) {
						double occup_ts = Services.OccupancyGrid.LoadNewestGrid();
						if (occup_ts < 0) {
							useOccupancyGrid = false;
						}
						else {
							double delta_ts = occup_ts - queuedUntrackedClusters.timestamp;
							Services.Dataset.ItemAs<double>("occupancy delta ts").Add(delta_ts, queuedUntrackedClusters.timestamp);
						}
					}
					occupancyDeletedCount = 0;

					List<Obstacle> trackedObstacles;
					if (queuedTrackedClusters == null) {
						trackedObstacles = new List<Obstacle>();
					}
					else {
						trackedObstacles = ProcessTrackedClusters(queuedTrackedClusters, vehicleBox);
					}
					List<Obstacle> untrackedObstacles = ProcessUntrackedClusters(queuedUntrackedClusters, trackedObstacles, vehicleBox);
					List<Obstacle> finalObstacles = FinalizeProcessing(trackedObstacles, untrackedObstacles, queuedUntrackedClusters.timestamp);
					processedObstacles = new ObstacleCollection(queuedUntrackedClusters.timestamp, finalObstacles);

					Services.Dataset.ItemAs<int>("occupancy deleted count").Add(occupancyDeletedCount, queuedUntrackedClusters.timestamp);

					queuedUntrackedClusters = null;
					queuedTrackedClusters = null;

					if (Services.DebuggingService.StepMode) {
						Services.DebuggingService.SetCompleted(typeof(ObstaclePipeline));
					}

					Services.Dataset.MarkOperation("obstacle rate", LocalCarTimeProvider.LocalNow);
				}
				catch (Exception ex) {
					OperationalTrace.WriteError("error processing obstacles: {0}", ex);
				}
			}
		}

		private List<Obstacle> ProcessTrackedClusters(SceneEstimatorTrackedClusterCollection clusters, Rect vehicleBox) {
			List<Obstacle> obstacles = new List<Obstacle>(clusters.clusters.Length);

			// get the list of previous id's
			SortedList<int, Obstacle> previousID;
			if (processedObstacles != null) {
				previousID = new SortedList<int, Obstacle>(processedObstacles.obstacles.Count);
				foreach (Obstacle obs in processedObstacles.obstacles) {
					if (obs != null && obs.trackID != -1 && !previousID.ContainsKey(obs.trackID)) {
						previousID.Add(obs.trackID, obs);
					}
				}
			}
			else {
				previousID = new SortedList<int, Obstacle>();
			}

			List<Coordinates> goodPoints = new List<Coordinates>(1500);

			Circle mergeCircle = new Circle(merge_expansion_size, Coordinates.Zero);
			Polygon mergePolygon = mergeCircle.ToPolygon(24);

			foreach (SceneEstimatorTrackedCluster cluster in clusters.clusters) {
				// ignore deleted targets
				if (cluster.statusFlag == SceneEstimatorTargetStatusFlag.TARGET_STATE_DELETED || cluster.statusFlag == SceneEstimatorTargetStatusFlag.TARGET_STATE_OCCLUDED_FULL || cluster.relativePoints == null || cluster.relativePoints.Length < 3)
					continue;

				Obstacle obs = new Obstacle();

				obs.trackID = cluster.id;
				obs.speed = cluster.speed;
				obs.speedValid = cluster.speedValid;
				obs.occuluded = cluster.statusFlag != SceneEstimatorTargetStatusFlag.TARGET_STATE_ACTIVE;

				// update the age
				Obstacle prevTrack = null;
				previousID.TryGetValue(cluster.id, out prevTrack);

				goodPoints.Clear();

				int numOccupancyDeleted = 0;
				foreach (Coordinates pt in cluster.relativePoints) {
					if (!vehicleBox.IsInside(pt)) {
						if (useOccupancyGrid && Services.OccupancyGrid.GetOccupancy(pt) == OccupancyStatus.Free) {
							occupancyDeletedCount++;
							numOccupancyDeleted++;
						}
						else {
							goodPoints.Add(pt);
						}
					}
				}

				if (goodPoints.Count < 3) {
					continue;
				}

				IList<Polygon> polys;
				if (obs.occuluded && numOccupancyDeleted > 0) {
					polys = WrapAndSplit(goodPoints, 1, 2.5);
				}
				else {
					polys = new Polygon[] { Polygon.GrahamScan(goodPoints) };
				}

				obs.absoluteHeadingValid = cluster.headingValid;
				obs.absoluteHeading = cluster.absoluteHeading;

				// set the obstacle polygon for calculate obstacle distance
				Polygon obsPoly = Polygon.GrahamScan(goodPoints);
				double targetDistance = GetObstacleDistance(obsPoly);

				ObstacleClass impliedClass = ObstacleClass.DynamicUnknown;
				switch (cluster.targetClass) {
					case SceneEstimatorTargetClass.TARGET_CLASS_CARLIKE:
						if (cluster.isStopped) {
							impliedClass = ObstacleClass.DynamicStopped;
						}
						else {
							impliedClass = ObstacleClass.DynamicCarlike;
						}
						break;

					case SceneEstimatorTargetClass.TARGET_CLASS_NOTCARLIKE:
						impliedClass = ObstacleClass.DynamicNotCarlike;
						break;

					case SceneEstimatorTargetClass.TARGET_CLASS_UNKNOWN:
						impliedClass = ObstacleClass.DynamicUnknown;
						break;
				}

				if (prevTrack == null) {
					obs.age = 1;
					// we haven't seen this track before, determine what the implied class is
					if (targetDistance < target_class_ignore_dist) {
						impliedClass = ObstacleClass.DynamicUnknown;
					}
				}
				else {
					obs.age = prevTrack.age+1;
					// if we've seen this target before and we've labelled it as unknown and it is labelled as car-like now, check the distance
					if (prevTrack.obstacleClass == ObstacleClass.DynamicUnknown && targetDistance < target_class_ignore_dist && obs.age < target_class_ignore_age) {
						impliedClass = ObstacleClass.DynamicUnknown;
					}
				}

				// get the off-road percentage
				double offRoadPercent = GetPercentOffRoad(obs.obstaclePolygon);

				if (offRoadPercent > 0.65) {
					obs.offroadAge = obs.age;
				}

				// now check if we're labelling the obstacle as car-like if it has been off-road in the last second
				if ((impliedClass == ObstacleClass.DynamicCarlike || impliedClass == ObstacleClass.DynamicStopped) && (obs.age - obs.offroadAge) > 10 && obs.offroadAge > 0) {
					// label as not car like
					impliedClass = ObstacleClass.DynamicNotCarlike;
				}

				obs.obstacleClass = impliedClass;

				foreach (Polygon poly in polys) {
					Obstacle newObs = obs.ShallowClone();

					newObs.obstaclePolygon = poly;

					// determine what to do with the cluster
					if (cluster.targetClass == SceneEstimatorTargetClass.TARGET_CLASS_CARLIKE && !cluster.isStopped) {
						// if the heading is valid, extrude the car polygon and predict forward
						if (cluster.headingValid) {
							newObs.extrudedPolygon = ExtrudeCarPolygon(newObs.obstaclePolygon, cluster.relativeheading);
						}
					}

					try {
						newObs.mergePolygon = Polygon.ConvexMinkowskiConvolution(mergePolygon, newObs.AvoidancePolygon);
					}
					catch (Exception) {
					}

					obstacles.Add(newObs);
				}
			}

			return obstacles;
		}

		private List<Obstacle> ProcessUntrackedClusters(SceneEstimatorUntrackedClusterCollection clusters, List<Obstacle> trackedObstacles, Rect vehicleBox) {
			List<Obstacle> obstacles = new List<Obstacle>();

			SortedList<Obstacle, List<Coordinates>> point_splits = new SortedList<Obstacle, List<Coordinates>>();
			List<Coordinates> unclaimed_points = new List<Coordinates>(1500);

			foreach (SceneEstimatorUntrackedCluster cluster in clusters.clusters) {
				// clear out stored variables
				point_splits.Clear();
				unclaimed_points.Clear();

				// now determine if the point belongs to an old obstacle
				ObstacleClass targetClass;
				if (cluster.clusterClass == SceneEstimatorClusterClass.SCENE_EST_HighObstacle)
					targetClass = ObstacleClass.StaticLarge;
				else
					targetClass = ObstacleClass.StaticSmall;

				// only add points that are not within a tracked obstacle's extruded polygon
				for (int i = 0; i < cluster.points.Length; i++) {
					Coordinates pt = cluster.points[i];
					// iterate over all tracked cluster
					bool did_hit = false;
					if (useOccupancyGrid && Services.OccupancyGrid.GetOccupancy(pt) == OccupancyStatus.Free) {
						occupancyDeletedCount++;
						did_hit = true;
					}
					else if (vehicleBox.IsInside(pt)) {
						did_hit = true;
					} 
					else if (trackedObstacles != null) {
						foreach (Obstacle trackedObs in trackedObstacles) {
							if (trackedObs.extrudedPolygon != null && trackedObs.extrudedPolygon.BoundingCircle.IsInside(pt) && trackedObs.extrudedPolygon.IsInside(pt)) {
								did_hit = true;
								break;
							}
						}
					}

					// if there was a hit, skip this point
					if (!did_hit) {
						unclaimed_points.Add(pt);
					}
					//if (did_hit)
					//  continue;

					//Obstacle oldObstacle = FindIntersectingCluster(pt, targetClass, previousObstacles);

					//if (oldObstacle != null) {
					//  List<Coordinates> obstacle_points;
					//  if (!point_splits.TryGetValue(oldObstacle, out obstacle_points)) {
					//    obstacle_points = new List<Coordinates>(100);
					//    point_splits.Add(oldObstacle, obstacle_points);
					//  }

					//  obstacle_points.Add(pt);
					//}
					//else {
					//  unclaimed_points.Add(pt);
					//}
				}

				// we've split up all the points appropriately
				// now construct the obstacles
				
				// we'll start with the obstacle belonging to an existing polygon
				//foreach (KeyValuePair<Obstacle, List<Coordinates>> split in point_splits) {
				//  if (split.Value != null && split.Value.Count >= 3) {
				//    // the obstacle will inherit most of the properties of the old obstacle
				//    Obstacle obs = new Obstacle();
				//    obs.age = split.Key.age+1;
				//    obs.obstacleClass = split.Key.obstacleClass;

				//    // don't bother doing a split operation on these clusters -- they have already been split
				//    obs.obstaclePolygon = Polygon.GrahamScan(split.Value);

				//    obstacles.Add(obs);
				//  }
				//}

				// handle the unclaimed points
				IList<Polygon> polygons = WrapAndSplit(unclaimed_points, split_area_threshold, split_length_threshold);

				foreach (Polygon poly in polygons) {
					// create a new obstacle
					Obstacle obs = new Obstacle();
					obs.age = 1;
					obs.obstacleClass = targetClass;

					obs.obstaclePolygon = poly;

					obstacles.Add(obs);
				}
				
			}

			// test all old obstacles and see if they intersect any new obstacles
			// project the previous static obstacles to the current time frame
			
			if (processedObstacles != null) {
				try {
					// get the relative transform
					List<Obstacle> carryOvers = new List<Obstacle>();
					Circle mergeCircle = new Circle(merge_expansion_size, Coordinates.Zero);
					Polygon mergePolygon = mergeCircle.ToPolygon(24);

					RelativeTransform transform = Services.RelativePose.GetTransform(processedObstacles.timestamp, clusters.timestamp);
					foreach (Obstacle prevObs in processedObstacles.obstacles) {
						if (prevObs.obstacleClass == ObstacleClass.StaticLarge || prevObs.obstacleClass == ObstacleClass.StaticSmall) {
							prevObs.obstaclePolygon = prevObs.obstaclePolygon.Transform(transform);
							prevObs.age++;

							if (prevObs.age < 20) {
								Coordinates centroid = prevObs.obstaclePolygon.GetCentroid();
								double dist = GetObstacleDistance(prevObs.obstaclePolygon);
								double angle = centroid.ArcTan;
								if (dist < 30 && dist > 6 && Math.Abs(centroid.Y) < 15 && Math.Abs(angle) < Math.PI/2.0) {
									try {
										prevObs.mergePolygon = Polygon.ConvexMinkowskiConvolution(mergePolygon, prevObs.obstaclePolygon);
										if (!TestIntersection(prevObs.mergePolygon, obstacles)) {
											bool dropObstacle = false;
											for (int i = 0; i < prevObs.obstaclePolygon.Count; i++) {
												Coordinates pt = prevObs.obstaclePolygon[i];
												// iterate over all tracked cluster

												if (vehicleBox.IsInside(pt)) {
													dropObstacle = true;
												}
												else if (useOccupancyGrid && externalUseOccupancyGrid && Services.OccupancyGrid.GetOccupancy(pt) == OccupancyStatus.Free) {
												  dropObstacle = true;
												}
												else if (trackedObstacles != null) {
													foreach (Obstacle trackedObs in trackedObstacles) {
														if (trackedObs.obstacleClass == ObstacleClass.DynamicCarlike) {
															Polygon testPoly = trackedObs.extrudedPolygon ?? trackedObs.mergePolygon;
															
															if (testPoly != null && testPoly.BoundingCircle.IsInside(pt) && testPoly.IsInside(pt)) {
																dropObstacle = true;
																break;
															}
														}
													}
												}

												if (dropObstacle) {
													break;
												}
											}

											if (!dropObstacle) {
												carryOvers.Add(prevObs);
											}
										}
									}
									catch (Exception) {
									}
								}
							}
						}
					}

					obstacles.AddRange(carryOvers);
				}
				catch (Exception) {
				}
			}

			// create the merge polygon for all these duder
			/*Circle mergeCircle = new Circle(merge_expansion_size, Coordinates.Zero);
			Polygon mergePolygon = mergeCircle.ToPolygon(24);

			foreach (Obstacle obs in obstacles) {
				obs.mergePolygon = Polygon.ConvexMinkowskiConvolution(mergePolygon, obs.obstaclePolygon);
			}*/

			return obstacles;
		}

		private Rect DetermineVehicleEraseBox() {
			// box where we don't want to consider points valid
			double unavoidableDist = (TahoeParams.actuation_delay+Services.BehaviorManager.AverageCycleTime)*Services.StateProvider.GetVehicleState().speed;
			return new Rect(-(TahoeParams.RL+1), -(TahoeParams.T+1)/2.0, TahoeParams.VL+2+unavoidableDist, TahoeParams.T+1);
		}

		private List<Obstacle> FinalizeProcessing(List<Obstacle> trackedObstacles, List<Obstacle> untrackedObstacles, CarTimestamp timestamp) {
			List<Obstacle> finalObstacles = new List<Obstacle>(trackedObstacles.Count+untrackedObstacles.Count);
			finalObstacles.AddRange(trackedObstacles);
			finalObstacles.AddRange(untrackedObstacles);

			Obstacle leftObstacle = GetSideObstacle(currentLeftSideObstacles);
			Obstacle rightObstacle = GetSideObstacle(currentRightSideObstacles);

			int extraCount = 0;
			if (leftObstacle != null) extraCount++;
			if (rightObstacle != null) extraCount++;

			List<int> ignoredObstacles = this.lastIgnoredObstacles;

			OperationalObstacle[] uiObstacles = new OperationalObstacle[finalObstacles.Count + extraCount];

			int i;
			for (i = 0; i < finalObstacles.Count; i++) {
				Obstacle obs = finalObstacles[i];

				obs.minSpacing = min_spacing[(int)obs.obstacleClass] + ExtraSpacing;
				obs.desSpacing = des_spacing[(int)obs.obstacleClass] + ExtraSpacing;

				try {
					obs.cspacePolygon = Polygon.ConvexMinkowskiConvolution(conv_polygon[(int)obs.obstacleClass], obs.AvoidancePolygon);
				}
				catch (Exception) {
					OperationalTrace.WriteWarning("error computing minkowski convolution in finalize processing");
					try {
						obs.cspacePolygon = obs.AvoidancePolygon.Inflate(TahoeParams.T/2.0+min_spacing[(int)obs.obstacleClass]);
					}
					catch (Exception) {
						obs.cspacePolygon = obs.AvoidancePolygon;
					}
				}

				OperationalObstacle uiObs = new OperationalObstacle();
				uiObs.age = obs.age;
				uiObs.obstacleClass = obs.obstacleClass;
				uiObs.poly = obs.AvoidancePolygon;

				uiObs.headingValid = obs.absoluteHeadingValid;
				uiObs.heading = obs.absoluteHeading;

				if (ignoredObstacles != null) {
					uiObs.ignored = ignoredObstacles.Contains(obs.trackID);
				}

				uiObstacles[i] = uiObs;
			}

			if (leftObstacle != null) {
				OperationalObstacle uiObs = new OperationalObstacle();
				uiObs.age = leftObstacle.age;
				uiObs.obstacleClass = leftObstacle.obstacleClass;
				uiObs.poly = leftObstacle.AvoidancePolygon;
				uiObstacles[i++] = uiObs;
			}

			if (rightObstacle != null) {
				OperationalObstacle uiObs = new OperationalObstacle();
				uiObs.age = rightObstacle.age;
				uiObs.obstacleClass = rightObstacle.obstacleClass;
				uiObs.poly = rightObstacle.AvoidancePolygon;
				uiObstacles[i++] = uiObs;
			}

			Services.UIService.PushObstacles(uiObstacles, timestamp, "obstacles", true);

			return finalObstacles;
		}

		private Polygon ExtrudeCarPolygon(Polygon obstaclePolygon, double relativeHeading) {
			// create a transform to counter rotate the polygon
			Matrix3 transform = Matrix3.Rotation(-relativeHeading);
			Polygon rotatedObstacle = obstaclePolygon.Transform(transform);

			// determine the extreme points along the heading and perpendicular to the heading
			Coordinates headingVec = new Coordinates(1, 0);
			Coordinates perpVec = new Coordinates(0, 1);

			Coordinates topPoint = rotatedObstacle.ExtremePoint(headingVec);
			Coordinates bottomPoint = rotatedObstacle.ExtremePoint(-headingVec);
			Coordinates leftPoint = rotatedObstacle.ExtremePoint(perpVec);
			Coordinates rightPoint = rotatedObstacle.ExtremePoint(-perpVec);

			double height = Math.Abs(topPoint.X-bottomPoint.X);
			double width = Math.Abs(leftPoint.Y-rightPoint.Y);

			// determine the aspect ratio
			double aspectRatio = height/width;

			// target aspect ratio is the tahoe aspect ratio
			double targetAspectRatio = TahoeParams.VL/TahoeParams.T;

			// TODO: fix this so we intelligently determine what directions to expand against instead
			// of just doing it based on adjusting height/width

			if (aspectRatio < targetAspectRatio) {
				// height is less than we would expect
				double newHeight = targetAspectRatio*width;

				// determine the translation vector
				// we'll use the bottom "x" coordinate and left/right average "y" coordinate
				Coordinates offsetVec;
				if (relativeHeading <= Math.PI) {
					offsetVec = new Coordinates(bottomPoint.X, ((leftPoint+rightPoint)/2.0).Y);
				}
				else {
					offsetVec = new Coordinates(topPoint.X-newHeight, ((leftPoint+rightPoint)/2.0).Y);
				}

				// create a new polygon with the target width/height
				Polygon poly = new Polygon(4);
				poly.Add((new Coordinates(-extrusion_reverse_dist, width/2.0 + extrusion_extra_width) + offsetVec).Rotate(relativeHeading));
				poly.Add((new Coordinates(-extrusion_reverse_dist, -width/2.0 - extrusion_extra_width) + offsetVec).Rotate(relativeHeading));
				poly.Add((new Coordinates(newHeight, -width/2.0 - extrusion_extra_width) + offsetVec).Rotate(relativeHeading));
				poly.Add((new Coordinates(newHeight, width/2.0 + extrusion_extra_width) + offsetVec).Rotate(relativeHeading));

				return poly;
			}
			else {
				// width is less than we would expect
				double newWidth = height/targetAspectRatio;

				// determine the translation vector
				// we'll use top/bottom average "x" coordinate and left "y" coordinate
				Coordinates offsetVec = new Coordinates(((topPoint+bottomPoint)/2.0).X, leftPoint.Y);

				// create a new polygon with the target width/height and appropriate offset/rotation
				Polygon poly = new Polygon(4);
				poly.Add((new Coordinates(-height/2.0-extrusion_reverse_dist, extrusion_extra_width)+offsetVec).Rotate(relativeHeading));
				poly.Add((new Coordinates(-height/2.0-extrusion_reverse_dist, -newWidth-extrusion_extra_width)+offsetVec).Rotate(relativeHeading));
				poly.Add((new Coordinates(height/2.0, -newWidth-extrusion_extra_width)+offsetVec).Rotate(relativeHeading));
				poly.Add((new Coordinates(height/2.0, extrusion_extra_width)+offsetVec).Rotate(relativeHeading));

				return poly;
			}
		}

		private Polygon PredictMovingObstacle(Polygon obstaclePolygon, double relativeHeading, double speed) {
			// calculate the offset vector
			Coordinates offfsetVector = Coordinates.FromAngle(relativeHeading)*(speed*prediction_time);

			// add all the obstacle polygon points to a list and then add the offset points
			List<Coordinates> totalPoints = new List<Coordinates>(obstaclePolygon.Count*2);
			totalPoints.AddRange(obstaclePolygon);

			foreach (Coordinates c in obstaclePolygon) {
				totalPoints.Add(c+offfsetVector);
			}

			// return the convex hull of the total points
			return Polygon.GrahamScan(totalPoints);
		}

		private IList<Polygon> WrapAndSplit(IList<Coordinates> points, double areaThreshold, double segmentThreshold) {
			if (points.Count < 3) {
				// return an empty polygon list
				return new Polygon[0];
			}

			Polygon poly = Polygon.GrahamScan(points);

			// calculate maximum segment length
			double maxSegLength = 0;
			foreach (LineSegment ls in poly.GetSegmentEnumerator()) {
				double len = ls.Length;
				if (len > maxSegLength) {
					maxSegLength = len;
				}
			}

			if (maxSegLength > segmentThreshold) {
				// perform a split
				// calculate the mean and covariance of points
				Coordinates mean;
				Matrix2 cov;
				MathUtil.ComputeMeanCovariance(points, out mean, out cov);

				// get the splitting line
				double[] eigenvalues;
				Coordinates[] eigenvectors;
				cov.SymmetricEigenDecomposition(out eigenvalues, out eigenvectors);

				Line splitLine = new Line(mean, mean+eigenvectors[1]);

				List<Coordinates> leftPoints = new List<Coordinates>(), rightPoints = new List<Coordinates>();

				foreach (Coordinates pt in points) {
					if (splitLine.IsToLeft(pt)) {
						leftPoints.Add(pt);
					}
					else {
						rightPoints.Add(pt);
					}
				}

				if (leftPoints.Count < 3 || rightPoints.Count < 3) {
					return new Polygon[] { poly };
				}

				// recursively split the points
				List<Polygon> subpolys = new List<Polygon>();
				subpolys.AddRange(WrapAndSplit(leftPoints, areaThreshold, segmentThreshold));
				subpolys.AddRange(WrapAndSplit(rightPoints, areaThreshold, segmentThreshold));

				return subpolys;
			}
			else {
				return new Polygon[] { poly };
			}
		}

		private bool TestIntersection(Polygon testPoly, List<Obstacle> obstacles) {
			foreach (Obstacle obs in obstacles) {
				if (Polygon.TestConvexIntersection(testPoly, obs.obstaclePolygon)) {
					return true;
				}
			}

			return false;
		}

		private Obstacle FindIntersectingCluster(Coordinates pt, ObstacleClass targetClass, List<Obstacle> previousObstacles) {
			int max_age = 0;
			double min_area = double.MaxValue;
			Obstacle best_obs = null;

			foreach (Obstacle obs in previousObstacles) {
				// check if we're in the bounding circle and then polygon
				if (obs.obstacleClass == targetClass && obs.mergePolygon.BoundingCircle.IsInside(pt) && obs.mergePolygon.IsInside(pt)) {
					// we're inside the polygon
					// check if this is a better match
					if (obs.age > max_age || (obs.age == max_age && obs.mergePolygon.GetArea() < min_area)) {
						best_obs = obs;
						max_age = obs.age;
						min_area = obs.mergePolygon.GetArea();
					}
				}
			}

			return best_obs;
		}

		private Obstacle GetSideObstacle(SideObstacles sideObstacles) {
			if (sideObstacles == null)
				return null;

			double minDist = 100;
			Obstacle minObstacle = null;

			foreach (SideObstacle obs in sideObstacles.obstacles) {
				if (obs.distance > 0.5 && obs.distance < minDist) {
					minDist = obs.distance;

					// create a polygon the specified distance away and 1 m in front of the rear axle and 0.5 m behind the front axle
					Polygon poly = new Polygon();
					if (sideObstacles.side == SideObstacleSide.Driver) {
						poly.Add(new Coordinates(1, obs.distance + TahoeParams.T/2.0));
						poly.Add(new Coordinates(TahoeParams.FL - 0.5, obs.distance + TahoeParams.T/2.0));
						poly.Add(new Coordinates(TahoeParams.FL - 0.5, obs.distance + TahoeParams.T/2.0 + 1));
						poly.Add(new Coordinates(1, obs.distance + TahoeParams.T/2.0 + 1));
					}
					else {
						poly.Add(new Coordinates(1, -(obs.distance + TahoeParams.T/2.0)));
						poly.Add(new Coordinates(TahoeParams.FL - 0.5, -(obs.distance + TahoeParams.T/2.0)));
						poly.Add(new Coordinates(TahoeParams.FL - 0.5, -(obs.distance + TahoeParams.T/2.0 + 1)));
						poly.Add(new Coordinates(1, -(obs.distance + TahoeParams.T/2.0 + 1)));
					}

					Obstacle finalObs = new Obstacle();
					finalObs.age = 1;
					finalObs.obstacleClass = ObstacleClass.StaticLarge;

					finalObs.obstaclePolygon = poly;

					finalObs.minSpacing = min_spacing[(int)finalObs.obstacleClass];
					finalObs.desSpacing = des_spacing[(int)finalObs.obstacleClass];

					finalObs.cspacePolygon = Polygon.ConvexMinkowskiConvolution(conv_polygon[(int)finalObs.obstacleClass], finalObs.AvoidancePolygon);

					minObstacle = finalObs;
				}
			}

			return minObstacle;
		}

		private double GetObstacleDistance(Polygon obs) {
			double minDist = double.MaxValue;

			for (int i = 0; i < obs.Count; i++) {
				double dist = obs[i].Length;
				if (dist < minDist) {
					minDist = dist;
				}
			}

			return minDist;
		}

		private double GetPercentOffRoad(Polygon obs) {
			return 0;
		}

		public ObstacleCollection GetProcessedObstacles(CarTimestamp timestamp, SAUDILevel saudi) {
			List<Obstacle> transformedObstacles = new List<Obstacle>();

			if (Services.StateProvider.GetVehicleState().speed < 4.5) {
				Obstacle leftObstacle = GetSideObstacle(currentLeftSideObstacles);
				Obstacle rightObstacle = GetSideObstacle(currentRightSideObstacles);
				if (leftObstacle != null) {
					transformedObstacles.Add(leftObstacle);
				}
				if (rightObstacle != null) {
					transformedObstacles.Add(rightObstacle);
				}
			}

			ObstacleCollection processedObstacles = this.processedObstacles;

			if (processedObstacles != null) {
				if (transformedObstacles.Capacity < transformedObstacles.Count + processedObstacles.obstacles.Count) {
					transformedObstacles.Capacity = transformedObstacles.Count + processedObstacles.obstacles.Count;
				}

				RelativeTransform transform = Services.RelativePose.GetTransform(processedObstacles.timestamp, timestamp);
				foreach (Obstacle obs in processedObstacles.obstacles) {
					if (!Settings.IgnoreTracks || obs.obstacleClass == ObstacleClass.StaticLarge || obs.obstacleClass == ObstacleClass.StaticSmall) {
						Obstacle newObs = obs.ShallowClone();

						if (saudi == SAUDILevel.L1) {
							if (obs.obstacleClass == ObstacleClass.StaticSmall && Math.Abs(newObs.AvoidancePolygon.GetArea()) < L1_area_threshold)
								continue;
						}
						else if (saudi == SAUDILevel.L2) {
							if (obs.obstacleClass == ObstacleClass.StaticSmall)
								continue;
						}
						else if (saudi == SAUDILevel.L3) {
							if (obs.obstacleClass == ObstacleClass.StaticSmall || obs.obstacleClass == ObstacleClass.StaticLarge || obs.obstacleClass == ObstacleClass.DynamicNotCarlike || obs.obstacleClass == ObstacleClass.DynamicUnknown)
								continue;
						}

						if (newObs.cspacePolygon != null) newObs.cspacePolygon = newObs.cspacePolygon.Transform(transform);
						if (newObs.extrudedPolygon != null) newObs.extrudedPolygon = newObs.extrudedPolygon.Transform(transform);
						if (newObs.obstaclePolygon != null) newObs.obstaclePolygon = newObs.obstaclePolygon.Transform(transform);
						if (newObs.predictedPolygon != null) newObs.predictedPolygon = newObs.predictedPolygon.Transform(transform);

						transformedObstacles.Add(newObs);
					}
				}
			}

			return new ObstacleCollection(timestamp, transformedObstacles);
		}

		public SideObstacle GetLeftSideObstacle() {
			return GetMinSideObstacle(currentLeftSideObstacles);
		}

		public SideObstacle GetRightSideObstacle() {
			return GetMinSideObstacle(currentRightSideObstacles);
		}

		private SideObstacle GetMinSideObstacle(SideObstacles sideObstacles) {
			if (sideObstacles == null)
				return null;

			double minDist = 100;
			SideObstacle minObstacle = null;

			foreach (SideObstacle obs in sideObstacles.obstacles) {
				if (obs.distance > 0.5 && obs.distance < minDist) {
					minDist = obs.distance;
					minObstacle = obs;
				}
			}

			return minObstacle;
		}
	}
}
