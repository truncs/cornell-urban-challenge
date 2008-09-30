using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.MessagingService;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common.Sensors.LocalRoadEstimate;
using UrbanChallenge.Common;
using OperationalLayer.RoadModel;
using SimOperationalService;
using OperationalLayer.Pose;
using UrbanChallenge.Common.Sensors;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common.Shapes;
using OperationalLayer.Obstacles;

namespace OperationalLayer.Communications {
	class SceneEstimatorListener : IChannelListener {
		private IChannel localRoadChannel;
		private IChannel localRoadChannel2;
		private IChannel posteriorPoseChannel;
		private IChannel clusterChannel;
		private IChannel sideObstacleChannel;
		private IChannel sideRoadEdgeChannel;
		private IChannel roadBearingChannel;

		public SceneEstimatorListener() {
			// get the local road estimate channel
			this.localRoadChannel = CommBuilder.GetChannel(UrbanChallenge.Common.Sensors.LocalRoadEstimate.LocalRoadEstimate.ChannelName);
			this.localRoadChannel2 = CommBuilder.GetChannel(UrbanChallenge.Common.Sensors.LocalRoadModelChannelNames.LocalRoadModelChannelName);
			this.posteriorPoseChannel = CommBuilder.GetChannel(VehicleState.ChannelName);
			this.clusterChannel = CommBuilder.GetChannel(SceneEstimatorObstacleChannelNames.AnyClusterChannelName);
			this.sideObstacleChannel = CommBuilder.GetChannel("SideObstacleChannel");
			this.sideRoadEdgeChannel = CommBuilder.GetChannel("SideRoadEdgeChannel");
			this.roadBearingChannel = CommBuilder.GetChannel("RoadBearingChannel");

			// add ourselves as a listener
			this.localRoadChannel.Subscribe(this);
			this.localRoadChannel2.Subscribe(this);
			this.posteriorPoseChannel.Subscribe(this);
			this.clusterChannel.Subscribe(this);
			this.sideObstacleChannel.Subscribe(this);
			this.sideRoadEdgeChannel.Subscribe(this);
			this.roadBearingChannel.Subscribe(this);
		}

		public void MessageArrived(string channelName, object message) {
			// check the method
			if (message is LocalRoadEstimate) {
				LocalRoadEstimate lre = (LocalRoadEstimate)message;
				CarTimestamp ct = lre.timestamp;

				// get the stop-line estimate
				Services.Dataset.ItemAs<bool>("stopline found").Add(lre.stopLineEstimate.stopLineExists, ct);
				Services.Dataset.ItemAs<double>("stopline distance").Add(lre.stopLineEstimate.distToStopline, ct);
				Services.Dataset.ItemAs<double>("stopline variance").Add(lre.stopLineEstimate.distToStoplineVar, ct);
			}
			else if (message is PathRoadModel && Settings.UsePathRoadModel) {
				PathRoadModel pathRoadModel = (PathRoadModel)message;

				List<ILaneModel> models = new List<ILaneModel>();
				foreach (PathRoadModel.LaneEstimate le in pathRoadModel.laneEstimates) {
					models.Add(new PathLaneModel(pathRoadModel.timestamp, le));
				}

				Services.LaneModels.SetLaneModel(models);
			}
			else if (message is VehicleState) {
				VehicleState vs = (VehicleState)message;
				AbsolutePose pose = new AbsolutePose(vs.Position, vs.Heading.ArcTan, vs.Timestamp);
				Services.AbsolutePosteriorPose.PushAbsolutePose(pose);

				Services.Dataset.ItemAs<Coordinates>("posterior pose").Add(vs.Position, vs.Timestamp);
				double heading = vs.Heading.ArcTan;
				if (Services.PoseListener != null && Services.PoseListener.BiasEstimator != null) {
					heading = Services.PoseListener.BiasEstimator.CorrectHeading(heading);
				}
				Services.Dataset.ItemAs<double>("posterior heading").Add(heading, vs.Timestamp);

				TimeoutMonitor.MarkData(OperationalDataSource.PosteriorPose);
			}
			else if (message is SceneEstimatorUntrackedClusterCollection) {
				// push to the obstacle pipeline
				Services.ObstaclePipeline.OnUntrackedClustersReceived((SceneEstimatorUntrackedClusterCollection)message);
				TimeoutMonitor.MarkData(OperationalDataSource.UntrackedClusters);
			}
			else if (message is SceneEstimatorTrackedClusterCollection) {
				Services.ObstaclePipeline.OnTrackedClustersReceived((SceneEstimatorTrackedClusterCollection)message);
				TimeoutMonitor.MarkData(OperationalDataSource.TrackedClusters);
			}
			//else if (message is LocalRoadModel) {
			//  LocalRoadModel roadModel = (LocalRoadModel)message;

			//  UrbanChallenge.Operational.Common.LocalLaneModel centerLaneModel = 
			//    new UrbanChallenge.Operational.Common.LocalLaneModel(
			//      new LinePath(roadModel.LanePointsCenter), ArrayConvert(roadModel.LanePointsCenterVariance), roadModel.laneWidthCenter,
			//      roadModel.laneWidthCenterVariance, roadModel.probabilityCenterLaneExists);
			//  UrbanChallenge.Operational.Common.LocalLaneModel leftLaneModel = 
			//    new UrbanChallenge.Operational.Common.LocalLaneModel(
			//      new LinePath(roadModel.LanePointsLeft), ArrayConvert(roadModel.LanePointsLeftVariance), roadModel.laneWidthLeft,
			//      roadModel.laneWidthLeftVariance, roadModel.probabilityLeftLaneExists);
			//  UrbanChallenge.Operational.Common.LocalLaneModel rightLaneModel = 
			//    new UrbanChallenge.Operational.Common.LocalLaneModel(
			//      new LinePath(roadModel.LanePointsRight), ArrayConvert(roadModel.LanePointsRightVariance), roadModel.laneWidthRight,
			//      roadModel.laneWidthRightVariance, roadModel.probabilityRightLaneExists);

			//  UrbanChallenge.Operational.Common.LocalRoadModel localRoadModel = new UrbanChallenge.Operational.Common.LocalRoadModel(
			//    roadModel.timestamp, roadModel.probabilityRoadModelValid, centerLaneModel, leftLaneModel, rightLaneModel);

			//  //Services.RoadModelProvider.LocalRoadModel = localRoadModel;

			//  // clone the lane models so when we transform to absolute coordinates for the ui, the lane model we have
			//  // stored doesn't get modified
			//  centerLaneModel = centerLaneModel.Clone();
			//  leftLaneModel = leftLaneModel.Clone();
			//  rightLaneModel = rightLaneModel.Clone();

			//  AbsoluteTransformer trans = Services.StateProvider.GetAbsoluteTransformer(roadModel.timestamp).Invert();
			//  centerLaneModel.LanePath.TransformInPlace(trans);
			//  leftLaneModel.LanePath.TransformInPlace(trans);
			//  rightLaneModel.LanePath.TransformInPlace(trans);

			//  // send to ui
			//  Services.Dataset.ItemAs<UrbanChallenge.Operational.Common.LocalLaneModel>("center lane").Add(centerLaneModel, roadModel.timestamp);
			//  Services.Dataset.ItemAs<UrbanChallenge.Operational.Common.LocalLaneModel>("left lane").Add(leftLaneModel, roadModel.timestamp);
			//  Services.Dataset.ItemAs<UrbanChallenge.Operational.Common.LocalLaneModel>("right lane").Add(rightLaneModel, roadModel.timestamp);
			//  Services.Dataset.ItemAs<double>("lane model probability").Add(roadModel.probabilityRoadModelValid, roadModel.timestamp);
			//}
			else if (message is SideObstacles) {
				Services.ObstaclePipeline.OnSideObstaclesReceived((SideObstacles)message);
			}
			else if (message is SideRoadEdge) {
				SideRoadEdge edge = (SideRoadEdge)message;

				RoadEdge.OnRoadEdge(edge);

				LineList line = RoadEdge.GetEdgeLine(edge);
				if (line == null)
					line = new LineList();

				string name = (edge.side == SideRoadEdgeSide.Driver) ? "left road edge" : "right road edge";

				Services.UIService.PushLineList(line, edge.timestamp, name, true);
			}
			else if (message is SparseRoadBearing) {
				SparseRoadBearing road = (SparseRoadBearing)message;
				RoadBearing.OnRoadBearing(road.timestamp, road.Heading, road.Confidence);
			}
		}

		private static double[] ArrayConvert(float[] source) {
			double[] ret = new double[source.Length];
			for (int i = 0; i < source.Length; i++) {
				ret[i] = source[i]/25;
			}

			return ret;
		}
	}
}
