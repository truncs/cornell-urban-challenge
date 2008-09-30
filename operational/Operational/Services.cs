using System;
using System.Collections.Generic;
using System.Text;
using Dataset.Source;
using UrbanChallenge.OperationalUIService.Parameters;
using OperationalLayer.Pose;
using OperationalLayer.CarTime;
using OperationalLayer.Communications;
using OperationalLayer.Tracking;
using UrbanChallenge.Common.Pose;
using UrbanChallenge.Common.EarthModel;
using OperationalLayer.OperationalBehaviors;
using OperationalLayer.RoadModel;
using OperationalLayer.Obstacles;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.PathSmoothing;

namespace OperationalLayer {
	static class Services {
		public static DatasetSource Dataset;
		public static TunableParamTable Params;

		public static RelativeTransformBuilder RelativePose;
		public static VehicleStateProvider StateProvider;
		public static ICarTimeProvider CarTime;
		public static TrackedDistance TrackedDistance;
		public static StopLineService Stopline;
		public static AbsolutePoseQueue AbsolutePose;
		public static AbsolutePoseQueue AbsolutePosteriorPose;

		public static CombinedRoadModelProvider RoadModelProvider;
		public static LaneModelProvider LaneModels;

		public static PlanarProjection Projection;
		public static ArbiterRoadNetwork RoadNetwork;

		public static ICommandTransport CommandTransport;
		public static TrackingManager TrackingManager;
		public static BehaviorManager BehaviorManager;

		public static UIService UIService;
		public static OperationalService Operational;
		public static DebuggingService DebuggingService;

		public static SceneEstimatorListener SceneEstListener;
		public static PoseListener PoseListener;

		public static ObstaclePipeline ObstaclePipeline;
		public static ObstacleManager ObstacleManager;
		public static OccupancyGrid OccupancyGrid;
	}
}
