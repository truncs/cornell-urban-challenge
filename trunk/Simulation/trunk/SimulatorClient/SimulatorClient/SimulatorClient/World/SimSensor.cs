using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Sensors;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Mapack;

namespace UrbanChallenge.Simulator.Client.World {
	public enum SensorType {
		/// <summary>
		/// Provides a series of ray-traced points spaced according to beam divergence
		/// </summary>
		/// <remarks>
		/// This would be like a LIDAR
		/// </remarks>
		Scan,

		/// <summary>
		/// Provides a single hit on an obstacle
		/// </summary>
		/// <remarks>
		/// This would be like a MobilEye or RADAR
		/// </remarks>
		SingleHit
	}

	public class SimSensor {
		private double leftAngularExtent;
		private double rightAngularExtent;
		private double beamResolution;
		private SensorType sensorType;
		private double range;

		public SimSensor(double leftAngularExtent, double rightAngularExtent, double beamResolution, SensorType sensorType, double range) {
			this.leftAngularExtent = leftAngularExtent;
			this.rightAngularExtent = rightAngularExtent;
			this.beamResolution = beamResolution;
			this.sensorType = sensorType;
			this.range = range;
		}

		private struct HitData {
			public LineSegment line;
			public Coordinates unitVec;
			public int obstacleIndex;
			public double distance;
			public Coordinates pt;
		}

		public void GetHits(IList<Polygon> obstacles, Coordinates vehicleLoc, double vehicleHeading, SceneEstimatorUntrackedClusterCollection clusters) {
			// for now, only support the Scan sensor type
			if (sensorType != SensorType.Scan)
				throw new NotSupportedException("Only Scan sensor type is supported at the moment");

			Matrix3 transform = Matrix3.Rotation(-vehicleHeading)*Matrix3.Translation(-vehicleLoc.X, -vehicleLoc.Y);

			// for each obstacle, get the bounding circle, see if it's in range
			List<Polygon> possibleTargets = new List<Polygon>(obstacles.Count);
			for (int i = 0; i < obstacles.Count; i++) {
				Circle boundingCircle = obstacles[i].BoundingCircle;

				double minDist = boundingCircle.center.DistanceTo(vehicleLoc) - boundingCircle.r;

				if (minDist < range) {
					possibleTargets.Add(obstacles[i].Transform(transform));
				}
			}

			// now start doing the actual rays
			int numRays = (int)Math.Round((leftAngularExtent-rightAngularExtent)/beamResolution);
			double beamStep = (leftAngularExtent-rightAngularExtent)/numRays;
			numRays++;
			HitData[] rays = new HitData[numRays];

			for (int rayNum = 0; rayNum < numRays; rayNum++) {
				double angle = rightAngularExtent + rayNum*beamStep;

				Coordinates unitVec = Coordinates.FromAngle(angle);
				LineSegment l = new LineSegment(Coordinates.Zero, unitVec*range);
				rays[rayNum].line = l;
				rays[rayNum].distance = double.MaxValue;
				rays[rayNum].obstacleIndex = -1;
				rays[rayNum].pt = Coordinates.NaN;
				rays[rayNum].unitVec = unitVec;
			}


			// go through and intersect with each polygon
			for (int rayInd = 0; rayInd < numRays; rayInd++) {
				HitData hd = rays[rayInd];

				for (int i = 0; i < possibleTargets.Count; i++) {
					Coordinates[] pts;
					double[] K;
					if (possibleTargets[i].Intersect(hd.line, out pts, out K)) {
						// find the min distance
						double minK = K[0];
						int minKind = 0;
						for (int j = 1; j < K.Length; j++) {
							if (K[j] < minK) {
								minK = K[j];
								minKind = j;
							}
						}

						// get the minimum point
						double dist = minK*range;

						if (dist < hd.distance) {
							hd.distance = dist;
							hd.obstacleIndex = i;
							hd.pt = pts[minKind];
						}
					}
				}

				rays[rayInd] = hd;
			}

			Random rand = new Random();

			// we have all the hits, form into clusters
			int indPrev = -1;
			List<Coordinates> clusterPoints = new List<Coordinates>();
			List<SceneEstimatorUntrackedCluster> newClusters = new List<SceneEstimatorUntrackedCluster>();
			for (int rayNum = 0; rayNum < numRays; rayNum++) {
				HitData hd = rays[rayNum];

				if (hd.obstacleIndex != indPrev && clusterPoints.Count > 0) {
					// test if any points are within 10 meters
					bool noneTooClose = true;
					
					if (noneTooClose)
					{
						SceneEstimatorUntrackedCluster cluster = new SceneEstimatorUntrackedCluster();
						cluster.points = clusterPoints.ToArray();
						newClusters.Add(cluster);

						clusterPoints.Clear();
					}
				}

				indPrev = hd.obstacleIndex;

				if (hd.obstacleIndex == -1) {
					continue;
				}

				// add noise to the point
				double noisyRange = hd.distance + (2*rand.NextDouble() - 1)*.25;
				Coordinates pt = hd.unitVec*noisyRange;

				clusterPoints.Add(pt);
			}

			if (indPrev != -1 && clusterPoints.Count > 0) {
				SceneEstimatorUntrackedCluster cluster = new SceneEstimatorUntrackedCluster();
				cluster.points = clusterPoints.ToArray();
				newClusters.Add(cluster);
			}

			SceneEstimatorUntrackedCluster[] allClusters;
			if (clusters.clusters != null && clusters.clusters.Length > 0) {
				allClusters = new SceneEstimatorUntrackedCluster[clusters.clusters.Length + newClusters.Count];
				Array.Copy(clusters.clusters, allClusters, clusters.clusters.Length);
				newClusters.CopyTo(allClusters, clusters.clusters.Length);
			}
			else {
				allClusters = newClusters.ToArray();
			}

			clusters.clusters = allClusters;
		}
	}
}
