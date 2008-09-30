using System;
using System.Collections.Generic;
using System.Text;
using OperationalLayer.CarTime;
using OperationalLayer.Communications;
using OperationalLayer.Tracking;
using OperationalLayer.ActController;
using OperationalLayer.OperationalBehaviors;
using System.IO;
using OperationalLayer.Sim;
using OperationalLayer.Obstacles;
using UrbanChallenge.PathSmoothing;

namespace OperationalLayer {
	enum BuildMode {
		Realtime,
		FullSim,
		Listen
	}

	static class OperationalBuilder {
		public static BuildMode BuildMode;

		private static void BuildCommon() {
			// create the dataset
			Services.Dataset = new Dataset.Source.DatasetSource("operational", "net.xml");

			// initialize the communciations builder (gets object directory, channel factory)
			CommBuilder.InitComm();

			// create the tunable parameter table
			using (FileStream fs = new FileStream("params.xml", FileMode.OpenOrCreate, FileAccess.Read)) {
				Services.Params = new UrbanChallenge.OperationalUIService.Parameters.TunableParamTable(fs);
			}

			// build a default planar project
			Services.Projection = new UrbanChallenge.Common.EarthModel.PlanarProjection(Settings.DefaultOrigin.X*Math.PI/180.0, Settings.DefaultOrigin.Y*Math.PI/180.0);

			// create the relative pose builder
			// 1000 entries should create 10 seconds of queue space
			Services.RelativePose = new UrbanChallenge.Common.Pose.RelativeTransformBuilder(10000, true);

			// build the tracked distance provider
			Services.TrackedDistance = new OperationalLayer.Pose.TrackedDistance(10000);

			// build the stopline provider
			Services.Stopline = new OperationalLayer.Pose.StopLineService();

			// build the absolute pose service
			Services.AbsolutePose = new OperationalLayer.Pose.AbsolutePoseQueue(10000);

			// build the absolute posterior pose service
			Services.AbsolutePosteriorPose = new OperationalLayer.Pose.AbsolutePoseQueue(1000);

			// build the lane model provider
			Services.LaneModels = new OperationalLayer.RoadModel.LaneModelProvider();

			// build the road model provider
			Services.RoadModelProvider = new OperationalLayer.RoadModel.CombinedRoadModelProvider();

			// Create the vehicle state provider
			Services.StateProvider = new OperationalLayer.Pose.VehicleStateProvider();

			// create the obstacle manager
			Services.ObstacleManager = new ObstacleManager();

			// build the UI service
			Services.UIService = new UIService();

			TimeoutMonitor.Initialize();
		}

		public static void BuildRealtime(bool testMode) {
			// set the build mode
			BuildMode = BuildMode.Realtime;

			// set to use posterior pose as the absolute pose source
			Settings.UsePosteriorPose = true;

			// build the common services
			BuildCommon();

			// create the debugging service
			Services.DebuggingService = new DebuggingService(false);

			// create the obstacle pipeline
			Services.ObstaclePipeline = new ObstaclePipeline();
			
			// create the occupancy grid
			Services.OccupancyGrid = new OccupancyGrid();

			// the car time comes from the timeserver in real time
			Services.CarTime = new UdpCarTimeProvider();

			// build the relative pose interface
			Services.PoseListener = new OperationalLayer.Pose.PoseListener();
			Services.SceneEstListener = new SceneEstimatorListener();

			// build the command transport
			Services.CommandTransport = new ActuationTransport(true);

			// build the tracking manager
			Services.TrackingManager = new TrackingManager(Services.CommandTransport, testMode);

			// build the behavior manager
			Services.BehaviorManager = new BehaviorManager(testMode);

			// build the operational service
			Services.Operational = new OperationalService(Services.CommandTransport, !testMode, false);

			// start the services that need starting
			((ActuationTransport)Services.CommandTransport).Start();
			
			Services.Operational.Start();
			Services.PoseListener.Start();
			Services.ObstaclePipeline.Start();
		}

		public static void BuildFullSim(bool testMode) {
			// set the build mode
			BuildMode = BuildMode.FullSim;

			// set to use pose estimator as the absolute pose source (posterior pose doesn't exist in sim)
			Settings.UsePosteriorPose = false;

			// build the common services
			BuildCommon();

			// create the debugging service
			Services.DebuggingService = new DebuggingService(false);

			// create the obstacle pipeline
			Services.ObstaclePipeline = new ObstaclePipeline();

			// the car time comes from the timeserver in real time
			Services.CarTime = new LocalCarTimeProvider();

			// build the relative pose interface
			Services.PoseListener = null;
			Services.SceneEstListener = new SceneEstimatorListener();

			// build the command transport
			Services.CommandTransport = new FullSimTransport();

			// build the tracking manager
			Services.TrackingManager = new TrackingManager(Services.CommandTransport, testMode);

			// build the behavior manager
			Services.BehaviorManager = new BehaviorManager(false);

			// build the operational service
			Services.Operational = new OperationalService(Services.CommandTransport, false, false);

			// start the services that need starting
			Services.Operational.Start();
			Services.ObstaclePipeline.Start();
		}

		public static void BuildListen() {
			// set the build mode
			BuildMode = BuildMode.Listen;

			// set to use posterior pose as the absolute pose source
			Settings.UsePosteriorPose = true;

			// build the common services
			BuildCommon();

			// create the debugging service
			Services.DebuggingService = new DebuggingService(false);

			// create the obstacle pipeline
			Services.ObstaclePipeline = new ObstaclePipeline();

			// create the occupancy grid
			Services.OccupancyGrid = new OccupancyGrid();

			// the car time comes from the timeserver in real time
			Services.CarTime = new LocalCarTimeProvider();

			// build the relative pose interface
			Services.PoseListener = new OperationalLayer.Pose.PoseListener();

			Services.SceneEstListener = new SceneEstimatorListener();

			// build the command transport
			Services.CommandTransport = new ActuationTransport(true);

			// build the tracking manager
			Services.TrackingManager = new TrackingManager(Services.CommandTransport, false);

			// build the behavior manager
			Services.BehaviorManager = new BehaviorManager(false);

			// build the operational service
			Services.Operational = new OperationalService(Services.CommandTransport, false, true);

			// start the services that need starting
			((ActuationTransport)Services.CommandTransport).Start();
			Services.Operational.Start();
			Services.PoseListener.Start();
			Services.ObstaclePipeline.Start();
		}

		public static void Build() {
			BuildMode settingsBuildMode = OperationalLayer.Properties.Settings.Default.BuildMode;

			switch (settingsBuildMode) {
				case BuildMode.FullSim:
					BuildFullSim(Settings.TestMode);
					break;

				case BuildMode.Realtime:
					BuildRealtime(Settings.TestMode);
					break;

				case BuildMode.Listen:
					BuildListen();
					break;

				default:
					throw new NotSupportedException("Unsupported build mode");
			}
		}
	}
}
