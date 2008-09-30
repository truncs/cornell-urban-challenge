using System;
using System.Collections.Generic;
using System.Text;
using Simulator.Engine;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Arbiter.Core.Tools;
using UrbanChallenge.Common.Path;
using Simulator.Engine.Obstacles;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.Sensors.LocalRoadEstimate;
using UrbanChallenge.Common.Mapack;
using SimOperationalService;
using UrbanChallenge.Common.Sensors;

namespace UrbanChallenge.Simulator.Client.World
{
	/// <summary>
	/// Provides helpers for determining the information needed to be sent out to respective vehicle
	/// </summary>
	public class WorldService
	{
		/// <summary>
		/// State of the world
		/// </summary>
		public WorldState worldState;

		/// <summary>
		/// Road Network we are using
		/// </summary>
		public ArbiterRoadNetwork RoadNetwork;

		/// <summary>
		/// Lidar sensor
		/// </summary>
		public SimSensor lidar;
		public SimSensor lidar2;

		/// <summary>
		/// World service
		/// </summary>
		/// <param name="arn"></param>
		public WorldService(ArbiterRoadNetwork arn)
		{
			this.RoadNetwork = arn;
			this.worldState = new WorldState();
			this.lidar = new SimSensor(Math.PI/3, -Math.PI/3, 0.5*Math.PI/180.0, SensorType.Scan, 50);
			this.lidar2 = new SimSensor(4 * Math.PI / 3, 2 * Math.PI / 3, 0.5 * Math.PI / 180.0, SensorType.Scan, 30);
		}

		/// <summary>
		/// Distance to the next stop line
		/// </summary>
		/// <param name="simVehicleState"></param>
		/// <returns></returns>
		public double? DistanceToNextStop(SimVehicleState simVehicleState)
		{
			if (this.RoadNetwork == null)
				return null;
			else
			{
				IAreaSubtypeId iasi = this.GetAreaId(simVehicleState);

				if (iasi == null)
					return null;
				else if (iasi is ArbiterPerimeterId)
					return null;
				else
				{
					ArbiterLaneId ali = (ArbiterLaneId)iasi;

					// get closest
					ArbiterLanePartition alp = this.RoadNetwork.ArbiterSegments[ali.SegmentId].Lanes[ali].GetClosestPartition(simVehicleState.Position);

					ArbiterWaypoint waypoint;
					double distance;

					RoadNetworkTools.NextStop(alp.Lane, alp, simVehicleState.Position, out waypoint, out distance);

					if (waypoint == null)
						return null;
					else
						return distance;
				}
			}
		}

		public PathRoadModel GetPathRoadEstimate(SimVehicleState state) {
			PathRoadModel ret = new PathRoadModel(new List<PathRoadModel.LaneEstimate>(), (double)SimulatorClient.GetCurrentTimestamp);

			ArbiterLanePartition closestPartition = this.RoadNetwork.ClosestLane(state.Position).GetClosestPartition(state.Position);

			LinePath path;

			if (IsPartitionSameDirection(closestPartition, state.Heading)) {
				path = BuildPath(closestPartition, true, 10, 40);
			}
			else {
				path = BuildPath(closestPartition, false, 10, 40);
			}

			Matrix3 absTransform = Matrix3.Rotation(-state.Heading.ArcTan)*Matrix3.Translation(-state.Position.X, -state.Position.Y);
			path.TransformInPlace(absTransform);

			PathRoadModel.LaneEstimate center = new PathRoadModel.LaneEstimate(path, closestPartition.Lane.Width, closestPartition.PartitionId.ToString());
			ret.laneEstimates.Add(center);

			// get the lane to the left
			if (closestPartition.Lane.LaneOnLeft != null) {
				ArbiterLanePartition partition = closestPartition.Lane.LaneOnLeft.GetClosestPartition(state.Position);
				if (closestPartition.NonLaneAdjacentPartitions.Contains(partition)) {
					if (IsPartitionSameDirection(partition, state.Heading)) {
						path = BuildPath(partition, true, 10, 25);
					}
					else {
						path = BuildPath(partition, false, 10, 25);
					}

					path.TransformInPlace(absTransform);

					PathRoadModel.LaneEstimate left = new PathRoadModel.LaneEstimate(path, partition.Lane.Width, partition.PartitionId.ToString());
					ret.laneEstimates.Add(left);
				}
			}

			if (closestPartition.Lane.LaneOnRight != null) {
				ArbiterLanePartition partition = closestPartition.Lane.LaneOnRight.GetClosestPartition(state.Position);
				if (closestPartition.NonLaneAdjacentPartitions.Contains(partition)) {
					if (IsPartitionSameDirection(partition, state.Heading)) {
						path = BuildPath(partition, true, 10, 25);
					}
					else {
						path = BuildPath(partition, false, 10, 25);
					}

					path.TransformInPlace(absTransform);

					PathRoadModel.LaneEstimate right = new PathRoadModel.LaneEstimate(path, partition.Lane.Width, partition.PartitionId.ToString());
					ret.laneEstimates.Add(right);
				}
			}

			return ret;
		}

		public LocalRoadEstimate GetLocalRoadEstimate(SimVehicleState state) {
			LocalRoadEstimate lre = new LocalRoadEstimate();

			/*ArbiterLanePartition closestPartition = this.RoadNetwork.ClosestLane(state.Position).GetClosestPartition(state.Position);

			LinePath path;

			if (IsPartitionSameDirection(closestPartition, state.Heading)) {
				path = BuildPath(closestPartition, true, 25, 25);
			}
			else {
				path = BuildPath(closestPartition, false, 25, 25);
			}

			Matrix3 absTransform = Matrix3.Rotation(-state.Heading.ArcTan)*Matrix3.Translation(-state.Position.X, -state.Position.Y);
			path.TransformInPlace(absTransform);

			// get the closest point
			LinePath.PointOnPath p0 = path.ZeroPoint;

			List<Coordinates> points = new List<Coordinates>(5);
			List<double> dists = new List<double>();
			dists.Add(0);
			points.Add(path.GetPoint(p0));
			// iterate through and add points every five meteres
			LinePath.PointOnPath pt = p0;
			double sumDist = 0;
			for (int i = 0; i < 3; i++) {
				double dist = 5;
				pt = path.AdvancePoint(pt, ref dist);

				if (dist != 5) {
					sumDist += (5-dist);

					dists.Add(sumDist);
					points.Add(path.GetPoint(pt));
				}

				if (dist > 0)
					break;
			}

			pt = p0;
			sumDist = 0;
			for (int i = 0; i < 2; i++) {
				double dist = -5;
				pt = path.AdvancePoint(pt, ref dist);

				if (dist != -5) {
					sumDist += (5+dist);

					dists.Insert(0, sumDist);
					points.Insert(0, path.GetPoint(pt));
				}

				if (dist < 0)
					break;
			}

			// do a least squares fit over the points
			Matrix X = new Matrix(points.Count, 3);
			Matrix W = new Matrix(points.Count, points.Count);
			Matrix Y = new Matrix(points.Count, 1);
			for (int i = 0; i < points.Count; i++) {
				X[i, 0] = 1;
				X[i, 1] = points[i].X;
				X[i, 2] = points[i].X*points[i].X;
				W[i, i] = 1/(dists[i] + 1);
				Y[i, 0] = points[i].Y;
			}

			Matrix beta = null;
			try {
				Matrix X_T = X.Transpose();
				Matrix inv = (X_T*W*X).Inverse;
				beta = inv*X_T*W*Y;
			}
			catch (Exception) {
			}

			if (beta != null) {
				lre.isModelValid = true;
				lre.roadCurvature = beta[2, 0];
				lre.roadCurvatureVar = double.MaxValue;
				lre.roadHeading = beta[1, 0];
				lre.roadHeadingVar = 0;

				lre.centerLaneEstimate = new UrbanChallenge.Common.Sensors.LocalRoadEstimate.LaneEstimate();
				lre.centerLaneEstimate.center = beta[0, 0];
				lre.centerLaneEstimate.centerVar = 0;
				lre.centerLaneEstimate.exists = true;
				lre.centerLaneEstimate.id = closestPartition.PartitionId.ToString();
				lre.centerLaneEstimate.width = closestPartition.Lane.Width;
				lre.centerLaneEstimate.widthVar = 0;

				lre.leftLaneEstimate = new UrbanChallenge.Common.Sensors.LocalRoadEstimate.LaneEstimate();
				lre.leftLaneEstimate.exists = false;
				if (closestPartition.Lane.LaneOnLeft != null) {
					ArbiterLanePartition leftParition = closestPartition.Lane.LaneOnLeft.GetClosestPartition(state.Position);
					if (closestPartition.NonLaneAdjacentPartitions.Contains(leftParition)) {
						double dist;

						if (IsPartitionSameDirection(leftParition, state.Heading)) {
							dist = (state.Position-leftParition.Initial.Position).Cross(leftParition.Vector().Normalize());
						}
						else {
							dist = (state.Position-leftParition.Final.Position).Cross(leftParition.Vector().Rotate180().Normalize());
						}

						lre.leftLaneEstimate.center = dist;
						lre.leftLaneEstimate.centerVar = 0;
						lre.leftLaneEstimate.exists = true;
						lre.leftLaneEstimate.id = leftParition.PartitionId.ToString();
						lre.leftLaneEstimate.width = leftParition.Lane.Width;
						lre.leftLaneEstimate.widthVar = 0;
					}
				}

				if (closestPartition.Lane.LaneOnRight != null) {
					ArbiterLanePartition rightPartition = closestPartition.Lane.LaneOnRight.GetClosestPartition(state.Position);
					if (closestPartition.NonLaneAdjacentPartitions.Contains(rightPartition)) {
						double dist;
						if (IsPartitionSameDirection(rightPartition, state.Heading)) {
							dist = (state.Position-rightPartition.Initial.Position).Cross(rightPartition.Vector().Normalize());
						}
						else {
							dist = (state.Position-rightPartition.Final.Position).Cross(rightPartition.Vector().Rotate180().Normalize());
						}

						lre.rightLaneEstimate.center = dist;
						lre.rightLaneEstimate.centerVar = 0;
						lre.rightLaneEstimate.exists = true;
						lre.rightLaneEstimate.id = rightPartition.PartitionId.ToString();
						lre.rightLaneEstimate.width = rightPartition.Lane.Width;
						lre.rightLaneEstimate.widthVar = 0;
					}
				}
			}
			else {
				lre.isModelValid = false;
			}*/
			lre.centerLaneEstimate.exists = false;
			lre.leftLaneEstimate.exists = false;
			lre.rightLaneEstimate.exists = false;

			lre.stopLineEstimate = new StopLineEstimate();
			double? distToStopline = DistanceToNextStop(state);
			lre.stopLineEstimate.stopLineExists = distToStopline.HasValue;
			lre.stopLineEstimate.distToStopline = distToStopline.GetValueOrDefault(double.MaxValue);
			lre.stopLineEstimate.distToStoplineVar = 0;

			lre.timestamp = (double)SimulatorClient.GetCurrentTimestamp;

			return lre;
		}

		private bool IsPartitionSameDirection(ArbiterLanePartition partition, Coordinates heading) {
			return (partition.Vector().Normalize().Dot(heading) > 0);
		}

		private LinePath BuildPath(ArbiterLanePartition parition, bool forward, double distBackTarget, double distForwardTarget) {
			if (forward) {
				// iterate all the way backward until we have our distance
				ArbiterLanePartition backPartition = parition;
				double distBack = 0;
				while (backPartition.Initial.PreviousPartition != null && distBack < distBackTarget) {
					backPartition = backPartition.Initial.PreviousPartition;
					distBack += backPartition.Length;
				}

				LinePath path = new LinePath();
				// insert the backmost point
				while (backPartition != parition) {
					path.Add(backPartition.Initial.Position);
					backPartition = backPartition.Final.NextPartition;
				}

				// add our initial position
				path.Add(parition.Initial.Position);
				// add our final position
				path.Add(parition.Final.Position);

				double distForward = 0;
				while (parition.Final.NextPartition != null && distForward < distForwardTarget) {
					parition = parition.Final.NextPartition;
					distForward += parition.Length;
					path.Add(parition.Final.Position);
				}

				return path;
			}
			else {
				// iterate all the way backward until we have our distance
				ArbiterLanePartition backPartition = parition;
				double distBack = 0;
				while (backPartition.Final.NextPartition != null && distBack < distBackTarget) {
					backPartition = backPartition.Final.NextPartition;
					distBack += backPartition.Length;
				}

				LinePath path = new LinePath();
				// insert the backmost point
				while (backPartition != parition) {
					path.Add(backPartition.Final.Position);
					backPartition = backPartition.Initial.PreviousPartition;
				}

				// add our initial position
				path.Add(parition.Final.Position);
				// add our final position
				path.Add(parition.Initial.Position);

				double distForward = 0;
				while (parition.Initial.PreviousPartition != null && distForward < distForwardTarget) {
					parition = parition.Initial.PreviousPartition;
					distForward += parition.Length;
					path.Add(parition.Initial.Position);
				}

				return path;
			}
		}

		/// <summary>
		/// Updates the world given a new world state from the simulation
		/// </summary>
		/// <param name="worldState"></param>
		public void UpdateWorld(WorldState worldState)
		{
			this.worldState = worldState;
		}

		/// <summary>
		/// Make a vehicle state from a sim vehicle state
		/// </summary>
		/// <param name="simState"></param>
		/// <returns></returns>
		public VehicleState VehicleStateFromSim(SimVehicleState simState)
		{
			// vehicle state
			VehicleState vs = new VehicleState();

			// area
			vs.Area = new List<AreaEstimate>();

			// ae
			AreaEstimate ae = new AreaEstimate();
			ae.Probability = 1;

			// get string id
			string id = "";

			// set id
			ae.AreaId = id;
			ae.AreaType = StateAreaType.Interconnect;

			// get area
			IAreaSubtypeId iasi = this.GetAreaId(simState);
			
			if (iasi is ArbiterPerimeterId)
			{
				id = ((ArbiterPerimeterId)iasi).ToString();
				ae.AreaType = StateAreaType.Zone;
			}
			else if (iasi is ArbiterLaneId)
			{
				ae.AreaId = ((ArbiterLaneId)iasi).ToString() + ".1";
				ae.AreaType = StateAreaType.Lane;
			}

			// add ae
			vs.Area.Add(ae);

			// set others
			vs.Heading = simState.Heading;
			vs.Position = simState.Position;

			// return
			return vs;			
		}

		/// <summary>
		/// Turn sim obstacle states into obstacles
		/// </summary>
		/// <returns></returns>
		public SceneEstimatorUntrackedClusterCollection ObstaclesFromWorld(SimVehicleId ours, Coordinates vehiclePosition, double vehicleHeading, double ts)
		{
			SceneEstimatorUntrackedClusterCollection seucc = new SceneEstimatorUntrackedClusterCollection();
			seucc.timestamp = (double)SimulatorClient.GetCurrentTimestamp;

			List<Polygon> obstaclePolygons = new List<Polygon>(worldState.Obstacles.Count);
			foreach (SimObstacleState obstacle in worldState.Obstacles.Values) {
				if(obstacle.ObstacleId.Number != ours.Number)
					obstaclePolygons.Add(obstacle.ToPolygon());
			}

			// use the lidar to get the hits
			lidar.GetHits(obstaclePolygons, vehiclePosition, vehicleHeading, seucc);
			lidar2.GetHits(obstaclePolygons, vehiclePosition, vehicleHeading, seucc);
			
			return seucc;			
		}

		/// <summary>
		/// Get the vehicles from the world and put them into sensors form
		/// </summary>
		/// <param name="ours"></param>
		/// <returns></returns>
		public SceneEstimatorTrackedClusterCollection VehiclesFromWorld(SimVehicleId ours, double ts)
		{
			// vehicle list
			List<SceneEstimatorTrackedCluster> vehicles = new List<SceneEstimatorTrackedCluster>();

			// get our vehicle
			SimVehicleState ourVS = null;
			foreach (SimVehicleState svs in this.worldState.Vehicles.Values)
			{
				if (svs.VehicleID.Equals(ours))
				{
					ourVS = svs;
				}
			}

			// generate "tracked" clusters
			foreach (SimVehicleState svs in this.worldState.Vehicles.Values)
			{
				// don't inclue our vehicle
				if (!svs.VehicleID.Equals(ours))
				{
					// construct cluster
					SceneEstimatorTrackedCluster setc = new SceneEstimatorTrackedCluster();

					// set heading valid
					setc.headingValid = true;

					// closest point
					Coordinates closestPoint = this.ClosestToPolygon(this.VehiclePolygon(svs), ourVS.Position);
					setc.closestPoint = closestPoint;

					// stopped
					bool isStopped = Math.Abs(svs.Speed) < 0.01;
					setc.isStopped = isStopped;

					// speed
					float speed = (float)Math.Abs(svs.Speed);
					setc.speed = speed;
					setc.speedValid = svs.SpeedValid;

					// absolute heading
					float absHeading = (float)(svs.Heading.ArcTan);
					setc.absoluteHeading = absHeading;

					// relative heading
					float relHeading = absHeading - (float)(ourVS.Heading.ArcTan);
					setc.relativeheading = relHeading;

					// set target class
					setc.targetClass = SceneEstimatorTargetClass.TARGET_CLASS_CARLIKE;

					// set id
					setc.id = svs.VehicleID.Number;

					// cluster partitions
					SceneEstimatorClusterPartition[] partitions = this.GetClusterPartitions(svs, ourVS);
					setc.closestPartitions = partitions;

					// state
					setc.statusFlag = SceneEstimatorTargetStatusFlag.TARGET_STATE_ACTIVE;

					// raw points
					Coordinates[] points = this.VehiclePointsRelative(svs, ourVS.Position, ourVS.Heading);
					setc.relativePoints = points;

					// add
					vehicles.Add(setc);
				}
			}

			// array of clusters
			SceneEstimatorTrackedClusterCollection setcc = new SceneEstimatorTrackedClusterCollection();
			setcc.clusters = vehicles.ToArray();
			setcc.timestamp = ts;

			// return the clusters
			return setcc;
		}

		private SceneEstimatorClusterPartition[] GetClusterPartitions(SimVehicleState svs, SimVehicleState ours)
		{
			List<SceneEstimatorClusterPartition> partitions = new List<SceneEstimatorClusterPartition>();
			
			foreach(IVehicleArea iva in this.RoadNetwork.VehicleAreas)
			{
				if (ours.Position.DistanceTo(svs.Position) < 55)
				{
					if (iva.ContainsVehicle(svs.Position, svs.Length, svs.Width, svs.Heading))
					{
						SceneEstimatorClusterPartition secp = new SceneEstimatorClusterPartition();
						secp.partitionID = iva.DefaultAreaId();
						secp.probability = 1;
						partitions.Add(secp);
					}
				}
				else
				{
					if (iva.ContainsVehicle(svs.Position, svs.Length, svs.Width * 4.0, svs.Heading))
					{
						SceneEstimatorClusterPartition secp = new SceneEstimatorClusterPartition();
						secp.partitionID = iva.DefaultAreaId();
						secp.probability = 1;
						partitions.Add(secp);
					}
				}
			}

			return partitions.ToArray();
		}

		private AreaEstimate[] GetAreaEstimates(SimVehicleState svs)
		{
			List<AreaEstimate> estimates = new List<AreaEstimate>();

			foreach (IVehicleArea iva in this.RoadNetwork.VehicleAreas)
			{
				if (iva.ContainsVehicle(svs.Position, svs.Length, svs.Width, svs.Heading))
				{
					AreaEstimate ae = new AreaEstimate();
					ae.AreaId = iva.DefaultAreaId();
					if(iva is ArbiterInterconnect)
						ae.AreaType = StateAreaType.Interconnect;
					else if(iva is ArbiterLane)
						ae.AreaType = StateAreaType.Lane;
					else
						ae.AreaType = StateAreaType.Zone;
					ae.Probability = 1;
					estimates.Add(ae);
				}
			}

			return estimates.ToArray();
		}

		/// <summary>
		/// Gets area id of a vehicle's integer area id
		/// </summary>
		/// <param name="vehicleState"></param>
		/// <returns></returns>
		public IAreaSubtypeId GetAreaId(SimVehicleState vehicleState)
		{
			return RoadNetworkTools.GetClosest(this.RoadNetwork, vehicleState.Position);
		}

		public Coordinates ClosestToPolygon(Polygon p, Coordinates c)
		{
			LinePath lp = new LinePath(p);
			return lp.GetPoint(lp.GetClosestPoint(c));
		}

		public Polygon VehiclePolygon(SimVehicleState sos)
		{
			Coordinates headingOffset = sos.Heading.Rotate90();
			Coordinates center = sos.Position + sos.Heading.Normalize(TahoeParams.FL - (sos.Length / 2.0));
			List<Coordinates> coords = new List<Coordinates>();
			coords.Add(center + sos.Heading.Normalize(sos.Length / 2.0) + headingOffset.Normalize(sos.Width / 2.0));
			coords.Add(center + sos.Heading.Normalize(sos.Length / 2.0) - headingOffset.Normalize(sos.Width / 2.0));
			coords.Add(center - sos.Heading.Normalize(sos.Length / 2.0) + headingOffset.Normalize(sos.Width / 2.0));
			coords.Add(center - sos.Heading.Normalize(sos.Length / 2.0) - headingOffset.Normalize(sos.Width / 2.0));
			Polygon p = new Polygon(coords, CoordinateMode.AbsoluteProjected);
			return p;
		}

		public Coordinates[] VehiclePoints(SimVehicleState svs)
		{
			Polygon p = VehiclePolygon(svs);
			LinePath lp = new LinePath(p);
			LinePath.PointOnPath c = lp.StartPoint;
			List<Coordinates> cs = new List<Coordinates>();

			while (!c.Equals(lp.EndPoint))
			{
				cs.Add(lp.GetPoint(c));
				c = lp.AdvancePoint(c, 0.1);
			}

			return cs.ToArray();
		}

		public Coordinates[] VehiclePointsRelative(SimVehicleState svs, Coordinates r, Coordinates h)
		{
			Coordinates[] cs = this.VehiclePoints(svs);

			for (int i = 0; i < cs.Length; i++)
			{
				cs[i] = cs[i] - r;
				cs[i] = cs[i].Rotate(-h.ArcTan);
			}

			return cs;
		}
	}
}
