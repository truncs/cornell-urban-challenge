using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using UrbanChallenge.OperationalUI.Common.Map.Tools;
using UrbanChallenge.OperationalUI.Common;
using UrbanChallenge.OperationalUI.Common.RunControl;
using UrbanChallenge.OperationalUI.Controls.DisplayObjects;
using UrbanChallenge.OperationalUI.Common.DataItem;
using UrbanChallenge.Common;
using UrbanChallenge.OperationalUI.Common.Map;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.Sensors;
using UrbanChallenge.MessagingService;
using Dataset.Units;
using UrbanChallenge.OperationalUI.Controls;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.OperationalUI.DisplayObjects;
using UrbanChallenge.Operational.Common;
using UrbanChallenge.Common.Pose;

namespace UrbanChallenge.OperationalUI {
	public partial class formMap : Form {
		private List<VehicleStateService> stateServices;
		private GridDisplayObject grid;

		private UntrackedClustersDisplayObject untrackedClustersDisplayObject;
		private ChannelListenerAttacher<SceneEstimatorUntrackedClusterCollection> untrackedClusterAttacher;
		private IChannel obstacleChannel;

		private List<IDisposable> disposables = new List<IDisposable>();

		public formMap() {
			InitializeComponent();

			InitializeServices();
			InitializeTools();
			InitializeDisplayObjects();
			InitializeStatusLabels();
			UpdateStepModeState();
		}

		#region Services

		private void InitializeServices() {
			Services.MapService = drawingSurface;

			Services.RunControlService.RunModeChanged += new EventHandler(RunControlService_RunModeChanged);

			VehicleStateService posteriorPoseService = new VehicleStateService("posterior pose", OperationalInterface.Dataset.ItemAs<double>("posterior heading"), OperationalInterface.Dataset.ItemAs<Coordinates>("posterior pose"));
			VehicleStateService poseService = new VehicleStateService("pose", OperationalInterface.Dataset.ItemAs<double>("heading"), OperationalInterface.Dataset.ItemAs<Coordinates>("xy"));

			stateServices = new List<VehicleStateService>();
			stateServices.Add(posteriorPoseService);
			stateServices.Add(poseService);

			foreach (VehicleStateService stateService in stateServices) {
				ToolStripMenuItem item = new ToolStripMenuItem(stateService.Name, null, trackService_Click);
				item.Tag = stateService;
				buttonTrackSource.DropDownItems.Add(item);
			}

			// initialize state service to posterior pose
			Services.VehicleStateService = posteriorPoseService;

			UpdateTrackSource(posteriorPoseService);
		}

		

		void RunControlService_RunModeChanged(object sender, EventArgs e) {
			buttonPause.Checked = Services.RunControlService.RunMode == RunMode.Paused;
			buttonStep.Enabled = buttonPause.Checked;
		}

		private void buttonStep_Click(object sender, EventArgs e) {
			Services.RunControlService.Step();
		}

		private void buttonPause_Click(object sender, EventArgs e) {
			if (Services.RunControlService.RunMode == RunMode.Paused) {
				Services.RunControlService.RunMode = RunMode.Realtime;
			}
			else {
				Services.RunControlService.RunMode = RunMode.Paused;
			}
		}

		private void formMap_Load(object sender, EventArgs e) {
			
		}

		private void trackService_Click(object sender, EventArgs e) {
			ToolStripMenuItem item = sender as ToolStripMenuItem;
			if (item == null)
				return;

			VehicleStateService stateService = item.Tag as VehicleStateService;
			if (stateService != null) {
				Services.VehicleStateService = stateService;

				UpdateTrackSource(stateService);
			}
		}

		private void UpdateTrackSource(VehicleStateService source) {
			foreach (ToolStripMenuItem item in buttonTrackSource.DropDownItems) {
				item.Checked = item.Tag == source;
			}
		}

		#endregion

		#region Tools

		private void InitializeTools() {
			buttonSelectTool.Tag = new SelectTool();
			buttonRulerTool.Tag = new RulerTool();
			buttonZoomTool.Tag = new ZoomTool();
			TrackTool tracker = new TrackTool();
			tracker.OffsetFraction = -0.5f;
			buttonTrackTool.Tag = tracker;

			drawingSurface.CurrentTool = (ITool)buttonSelectTool.Tag;

			UpdateSelectedTool(drawingSurface.CurrentTool);
		}

		private void UpdateSelectedTool(ITool tool) {
			buttonSelectTool.Checked = object.Equals(buttonSelectTool.Tag, tool);
			buttonRulerTool.Checked = object.Equals(buttonRulerTool.Tag, tool);
			buttonZoomTool.Checked = object.Equals(buttonZoomTool.Tag, tool);
			buttonTrackTool.Checked = object.Equals(buttonTrackTool.Tag, tool);
		}

		private void ToolButton_Click(object sender, EventArgs e) {
			ToolStripItem button = sender as ToolStripItem;
			ITool tool = button.Tag as ITool;

			if (tool != null) {
				drawingSurface.CurrentTool = tool;

				UpdateSelectedTool(tool);
			}
		}

		#endregion

		#region Display Objects

		private void InitializeDisplayObjects() {
			grid = new GridDisplayObject();
			// add seperate from display object service
			Services.DisplayObjectService.Add(grid, true);

			// car object
			CarDisplayObject carDisplayObject = new CarDisplayObject("tahoe", Color.SkyBlue);
			carDisplayObject.AttachToVehicleState = true;
			Services.DisplayObjectService.Add(carDisplayObject, true);
			
			// tracked paths
			TrackedPathDisplayObject posePath = new TrackedPathDisplayObject("paths/pose", Color.LightBlue);
			disposables.Add(DataItemAttacher.Attach(posePath, OperationalInterface.Dataset.ItemAs<Coordinates>("xy")));
			Services.DisplayObjectService.Add(posePath, true);

			TrackedPathDisplayObject posteriorPosePath = new TrackedPathDisplayObject("paths/posterior pose", Color.DarkGreen);
			disposables.Add(DataItemAttacher.Attach(posteriorPosePath, OperationalInterface.Dataset.ItemAs<Coordinates>("posterior pose")));
			Services.DisplayObjectService.Add(posteriorPosePath, true);

			TrackedPathDisplayObject hpPath = new TrackedPathDisplayObject("paths/hp", Color.HotPink);
			disposables.Add(DataItemAttacher.Attach(hpPath, OperationalInterface.Dataset.ItemAs<Coordinates>("hp xy")));
			Services.DisplayObjectService.Add(hpPath, true);

			TrackedPathDisplayObject sepPath = new TrackedPathDisplayObject("paths/septentrio", Color.DarkRed);
			disposables.Add(DataItemAttacher.Attach(sepPath, OperationalInterface.Dataset.ItemAs<Coordinates>("gps xy")));
			Services.DisplayObjectService.Add(sepPath, true);

			// path point
			PointDisplayObject pathPoint = new PointDisplayObject("tracking/path point", "path point", Color.Green, ControlPointStyle.LargeX, ContentAlignment.BottomRight);
			disposables.Add(DataItemAttacher.Attach(pathPoint, OperationalInterface.Dataset.ItemAs<Coordinates>("path point")));
			Services.DisplayObjectService.Add(pathPoint, true);

			LineListDisplayObject trackingPath = new LineListDisplayObject("tracking/tracking path", Color.DeepPink);
			disposables.Add(DataItemAttacher.Attach(trackingPath, OperationalInterface.Dataset.ItemAs<LineList>("tracking path")));
			Services.DisplayObjectService.Add(trackingPath, false);

			// u-turn stuff
			CircleDisplayObject uturnCircle = new CircleDisplayObject("u-turn/planned circle", Color.Magenta);
			disposables.Add(DataItemAttacher.Attach(uturnCircle, OperationalInterface.Dataset.ItemAs<Circle>("uturn circle")));
			Services.DisplayObjectService.Add(uturnCircle, true);

			PointDisplayObject uturnStopPoint = new PointDisplayObject("u-turn/stop point", null, Color.Black, ControlPointStyle.SmallX, ContentAlignment.BottomRight);
			disposables.Add(DataItemAttacher.Attach(uturnStopPoint, OperationalInterface.Dataset.ItemAs<Coordinates>("uturn stop point")));
			Services.DisplayObjectService.Add(uturnStopPoint, true);

			PolygonDisplayObject uturnPolygon = new PolygonDisplayObject("u-turn/polygon", Color.BurlyWood, false);
			disposables.Add(DataItemAttacher.Attach(uturnPolygon, OperationalInterface.Dataset.ItemAs<Polygon>("uturn polygon")));
			Services.DisplayObjectService.Add(uturnPolygon, true);

			// original path objects (paths frank sends)
			LineListDisplayObject originalPath1 = new LineListDisplayObject("planning/original path 1", Color.DarkOrchid);
			disposables.Add(DataItemAttacher.Attach(originalPath1, OperationalInterface.Dataset.ItemAs<LineList>("original path1")));
			Services.DisplayObjectService.Add(originalPath1, true);

			LineListDisplayObject originalpath2 = new LineListDisplayObject("planning/original path 2", Color.DarkSeaGreen);
			disposables.Add(DataItemAttacher.Attach(originalpath2, OperationalInterface.Dataset.ItemAs<LineList>("original path2")));
			Services.DisplayObjectService.Add(originalpath2, true);

			// planned path objects
			LineListDisplayObject smoothedPath = new LineListDisplayObject("planning/smoothed path", Color.OrangeRed);
			disposables.Add(DataItemAttacher.Attach(smoothedPath, OperationalInterface.Dataset.ItemAs<LineList>("smoothed path")));
			Services.DisplayObjectService.Add(smoothedPath, true);

			LineListDisplayObject leftBound = new LineListDisplayObject("planning/left bound", Color.Blue);
			disposables.Add(DataItemAttacher.Attach(leftBound, OperationalInterface.Dataset.ItemAs<LineList>("left bound")));
			Services.DisplayObjectService.Add(leftBound, true);

			LineListDisplayObject rightBound = new LineListDisplayObject("planning/right bound", Color.Red);
			disposables.Add(DataItemAttacher.Attach(rightBound, OperationalInterface.Dataset.ItemAs<LineList>("right bound")));
			Services.DisplayObjectService.Add(rightBound, true);

			LineListDisplayObject subPath = new LineListDisplayObject("planning/subpath", Color.SaddleBrown);
			disposables.Add(DataItemAttacher.Attach(subPath, OperationalInterface.Dataset.ItemAs<LineList>("subpath")));
			Services.DisplayObjectService.Add(subPath, false);

			LineListDisplayObject predictionPath = new LineListDisplayObject("planning/prediction", Color.DarkViolet);
			disposables.Add(DataItemAttacher.Attach(predictionPath, OperationalInterface.Dataset.ItemAs<LineList>("prediction path")));
			Services.DisplayObjectService.Add(predictionPath, false);

			LineListDisplayObject predictionPath2 = new LineListDisplayObject("planning/prediction2", Color.Magenta);
			disposables.Add(DataItemAttacher.Attach(predictionPath2, OperationalInterface.Dataset.ItemAs<LineList>("prediction path2")));
			Services.DisplayObjectService.Add(predictionPath2, false);

			LineListDisplayObject subPath2 = new LineListDisplayObject("planning/subpath2", Color.Lime);
			disposables.Add(DataItemAttacher.Attach(subPath2, OperationalInterface.Dataset.ItemAs<LineList>("subpath2")));
			Services.DisplayObjectService.Add(subPath2, true);

			PointsDisplayObject leftPoints = new PointsDisplayObject("planning/left hits", Color.Blue, ControlPointStyle.LargeX);
			disposables.Add(DataItemAttacher.Attach(leftPoints, OperationalInterface.Dataset.ItemAs<Coordinates[]>("left bound points")));
			Services.DisplayObjectService.Add(leftPoints, true);

			PointsDisplayObject rightPoints = new PointsDisplayObject("planning/right hits", Color.Red, ControlPointStyle.LargeX);
			disposables.Add(DataItemAttacher.Attach(rightPoints, OperationalInterface.Dataset.ItemAs<Coordinates[]>("right bound points")));
			Services.DisplayObjectService.Add(rightPoints, true);

			LineListDisplayObject avoidancePath = new LineListDisplayObject("planning/avoidance path", Color.DarkSalmon);
			disposables.Add(DataItemAttacher.Attach(avoidancePath, OperationalInterface.Dataset.ItemAs<LineList>("avoidance path")));
			Services.DisplayObjectService.Add(avoidancePath, true);

			// extended debugging stuff
			PlanningGridExtDisplayObject gridDisplay = new PlanningGridExtDisplayObject("planning/grid", Color.DarkOrchid, Color.LightSkyBlue);
			Services.DisplayObjectService.Add(gridDisplay, false);

			SmoothingResultsDisplayObject smoothingDisplay = new SmoothingResultsDisplayObject("planning/smoothing");
			Services.DisplayObjectService.Add(smoothingDisplay, false);

			ArcDisplayObject arcs = new ArcDisplayObject();
			disposables.Add(DataItemAttacher.Attach(arcs, OperationalInterface.Dataset.ItemAs<ArcVotingResults>("arc voting results")));
			Services.DisplayObjectService.Add(arcs, false);

			// obstacle data

			untrackedClustersDisplayObject = new UntrackedClustersDisplayObject("obstacles/untracked points", true, Color.DarkMagenta);
			untrackedClusterAttacher = new ChannelListenerAttacher<SceneEstimatorUntrackedClusterCollection>(untrackedClustersDisplayObject, null);
			Services.DisplayObjectService.Add(untrackedClustersDisplayObject, true);

			ObstacleDisplayObject perimeterObstacles = new ObstacleDisplayObject("obstacles/perimeter obstacles");
			disposables.Add(DataItemAttacher.Attach(perimeterObstacles, OperationalInterface.Dataset.ItemAs<OperationalObstacle[]>("perimeter obstacles")));
			Services.DisplayObjectService.Add(perimeterObstacles, true);

			ObstacleDisplayObject wrappedClusters = new ObstacleDisplayObject("obstacles/wrapped obstacles");
			disposables.Add(DataItemAttacher.Attach(wrappedClusters, OperationalInterface.Dataset.ItemAs<OperationalObstacle[]>("obstacles")));
			Services.DisplayObjectService.Add(wrappedClusters, true);

			// subscribe to the attached event so we can bind the untracked clusters data appropriately
			OperationalInterface.Attached += OperationalInterface_Attached;

			// lane models
			LocalLaneModelDisplayObject centerLane = new LocalLaneModelDisplayObject("lanes/center", Color.DarkGreen);
			disposables.Add(DataItemAttacher.Attach(centerLane, OperationalInterface.Dataset.ItemAs<LocalLaneModel>("center lane")));
			Services.DisplayObjectService.Add(centerLane, false);

			LocalLaneModelDisplayObject leftLane = new LocalLaneModelDisplayObject("lanes/left", Color.Goldenrod);
			disposables.Add(DataItemAttacher.Attach(leftLane, OperationalInterface.Dataset.ItemAs<LocalLaneModel>("left lane")));
			Services.DisplayObjectService.Add(leftLane, false);

			LocalLaneModelDisplayObject rightLane = new LocalLaneModelDisplayObject("lanes/right", Color.LightCoral);
			disposables.Add(DataItemAttacher.Attach(rightLane, OperationalInterface.Dataset.ItemAs<LocalLaneModel>("right lane")));
			Services.DisplayObjectService.Add(rightLane, false);

			LineListDisplayObject leftRoadEdge = new LineListDisplayObject("lanes/left road edge", Color.Firebrick);
			disposables.Add(DataItemAttacher.Attach(leftRoadEdge, OperationalInterface.Dataset.ItemAs<LineList>("left road edge")));
			Services.DisplayObjectService.Add(leftRoadEdge, true);

			LineListDisplayObject rightRoadEdge = new LineListDisplayObject("lanes/right road edge", Color.DarkViolet);
			disposables.Add(DataItemAttacher.Attach(rightRoadEdge, OperationalInterface.Dataset.ItemAs<LineList>("right road edge")));
			Services.DisplayObjectService.Add(rightRoadEdge, true);

			LineListDisplayObject roadBearing = new LineListDisplayObject("lanes/road bearing", Color.Orange);
			disposables.Add(DataItemAttacher.Attach(roadBearing, OperationalInterface.Dataset.ItemAs<LineList>("road bearing")));
			Services.DisplayObjectService.Add(roadBearing, true);

			// intersections
			LineListDisplayObject intersectionPath = new LineListDisplayObject("intersections/pull path", Color.DarkCyan);
			disposables.Add(DataItemAttacher.Attach(intersectionPath, OperationalInterface.Dataset.ItemAs<LineList>("intersection path")));
			Services.DisplayObjectService.Add(intersectionPath, true);

			PolygonDisplayObject intersectionPolygon = new PolygonDisplayObject("intersections/polygon", Color.DarkGoldenrod, false);
			disposables.Add(DataItemAttacher.Attach(intersectionPolygon, OperationalInterface.Dataset.ItemAs<Polygon>("intersection polygon")));
			Services.DisplayObjectService.Add(intersectionPolygon, true);

			// zones
			PolygonSetDisplayObject zoneBadRegions = new PolygonSetDisplayObject("zone/bad regions", Color.DarkMagenta, false);
			disposables.Add(DataItemAttacher.Attach(zoneBadRegions, OperationalInterface.Dataset.ItemAs<Polygon[]>("zone bad regions")));
			Services.DisplayObjectService.Add(zoneBadRegions, true);

			PolygonDisplayObject zonePerimeter = new PolygonDisplayObject("zone/perimeter", Color.DarkSeaGreen, false);
			disposables.Add(DataItemAttacher.Attach(zonePerimeter, OperationalInterface.Dataset.ItemAs<Polygon>("zone perimeter")));
			Services.DisplayObjectService.Add(zonePerimeter, true);

			PointDisplayObject frontLeftPoint = new PointDisplayObject("zone/parker front left", "front left", Color.DarkBlue, ControlPointStyle.LargeX, ContentAlignment.MiddleRight);
			disposables.Add(DataItemAttacher.Attach(frontLeftPoint, OperationalInterface.Dataset.ItemAs<Coordinates>("front left point")));
			Services.DisplayObjectService.Add(frontLeftPoint, true);

			PointDisplayObject rearRightPoint = new PointDisplayObject("zone/parker rear right", "rear right", Color.DarkOrchid, ControlPointStyle.LargeX, ContentAlignment.MiddleRight);
			disposables.Add(DataItemAttacher.Attach(rearRightPoint, OperationalInterface.Dataset.ItemAs<Coordinates>("rear right point")));
			Services.DisplayObjectService.Add(rearRightPoint, true);

			CircleSegmentDisplayObject parkPath = new CircleSegmentDisplayObject("zone/parking path", Color.DarkRed);
			disposables.Add(DataItemAttacher.Attach(parkPath, OperationalInterface.Dataset.ItemAs<CircleSegment>("parking path")));
			Services.DisplayObjectService.Add(parkPath, true);

			Services.DisplayObjectService.MoveToBefore(gridDisplay, carDisplayObject);
		}

		void OperationalInterface_Attached(object sender, OperationalInterface.AttachEventArgs e) {
			try {
				// clean up the old channels if they're set up
				if (obstacleChannel != null) {
					obstacleChannel.Dispose();
					obstacleChannel = null;
				}

				/*if (trackedObstacleChannel != null) {
					trackedObstacleChannel.Dispose();
					trackedObstacleChannel = null;
				}*/
			}
			catch (Exception) {
			}

			try {
				// get the channel factory
				IChannelFactory factory = (IChannelFactory)OperationalInterface.ObjectDirectory.Resolve("ChannelFactory");

				if (string.IsNullOrEmpty(e.Suffix)) {
					// get the scene estimator all channel
					obstacleChannel = factory.GetChannel(SceneEstimatorObstacleChannelNames.AnyClusterChannelName, ChannelMode.UdpMulticast);
					//trackedObstacleChannel = null;
				}
				else {
					obstacleChannel = factory.GetChannel(SceneEstimatorObstacleChannelNames.UntrackedClusterChannelName + "_" + e.Suffix, ChannelMode.UdpMulticast);
					//trackedObstacleChannel = factory.GetChannel(SceneEstimatorObstacleChannelNames.TrackedClusterChannelName + "_" + suffix, ChannelMode.UdpMulticast);
				}
			}
			catch (Exception ex) {
				MessageBox.Show("Could not connect to scene estimator cluster channels: " + ex.Message);
				return;
			}

			if (obstacleChannel != null) {
				obstacleChannel.Subscribe(untrackedClusterAttacher);
			}

			/*if (trackedObstacleChannel != null) {
				trackedObstacleChannel.Subscribe(clusters);
			}*/
		}

		#endregion

		#region Misc UI

		private void buttonZoomIn_Click(object sender, EventArgs e) {
			drawingSurface.ZoomDelta(1);
		}

		private void buttonZoomOut_Click(object sender, EventArgs e) {
			drawingSurface.ZoomDelta(-1);
		}

		private void buttonClear_Click(object sender, EventArgs e) {
			drawingSurface.Clear();
		}

		private void buttonShowRightPanel_Click(object sender, EventArgs e) {
			if (splitContainer1.Panel2Collapsed) {
				splitContainer1.Panel2Collapsed = false;
				buttonShowRightPanel.Checked = true;
			}
			else {
				splitContainer1.Panel2Collapsed = true;
				buttonShowRightPanel.Checked = false;
			}
		}

		private void buttonShowGrid_Click(object sender, EventArgs e) {
			if (buttonShowGrid.Checked) {
				Services.DisplayObjectService.SetVisible(grid, false);
				buttonShowGrid.Checked = false;
			}
			else {
				Services.DisplayObjectService.SetVisible(grid, true);
				buttonShowGrid.Checked = true;
			}
		}

		private void buttonStepMode_ButtonClick(object sender, EventArgs e) {
			// refresh the step mode
			try {
				StepController.RefreshStepMode();
			}
			catch (Exception ex) {
				MessageBox.Show(this, "Could not refresh step mode:\n" + ex.Message, "OperationalUI");
				return;
			}

			try {
				// swap the shits
				StepController.StepMode = !StepController.StepMode;
			}
			catch (Exception ex) {
				MessageBox.Show(this, "Could not set step mode:\n" + ex.Message, "OperationalUI");
			}

			UpdateStepModeState();
		}

		private void menuRefreshStepMode_Click(object sender, EventArgs e) {
			try {
				StepController.RefreshStepMode();
			}
			catch (Exception ex) {
				MessageBox.Show(this, "Could not refresh step mode:\n" + ex.Message, "OperationalUI");
				return;
			}

			UpdateStepModeState();
		}

		private void UpdateStepModeState() {
			if (StepController.StepMode) {
				buttonStepMode.Text = "Set Realtime";
			}
			else {
				buttonStepMode.Text = "Set Step Mode";
			}

			buttonOperationalStep.Enabled = StepController.StepMode;
		}

		private void buttonOperationalStep_Click(object sender, EventArgs e) {
			try {
				StepController.Step();
			}
			catch (Exception ex) {
				MessageBox.Show("Error stepping:\n" + ex.Message);
			}
		}

		private void drawingSurface_KeyDown(object sender, KeyEventArgs e) {
			if (e.KeyCode == Keys.Right && Control.ModifierKeys == Keys.Control) {
				buttonOperationalStep_Click(this, EventArgs.Empty);
			}
		}

		#endregion 

		#region Status Labels

		private void InitializeStatusLabels() {
			disposables.Add(CallbackAttacher.Attach(OperationalInterface.Dataset.ItemAs<Coordinates>("xy"), delegate(Coordinates pt, string label) {
				labelActualPosition.Text = string.Format("{0:F1} m, {1:F1} m", pt.X, pt.Y);
			}));
			disposables.Add(NumericLabelAttacher.Attach(labelActualSpeed, "F1", OperationalInterface.Dataset.ItemAs<double>("speed"), "m/s", "m/s", "mph"));
			disposables.Add(NumericLabelAttacher.Attach(labelActualHeading, "F1", OperationalInterface.Dataset.ItemAs<double>("heading"), "rad", "rad", "deg"));
			disposables.Add(NumericLabelAttacher.Attach(labelActualSteering, "F1", OperationalInterface.Dataset.ItemAs<double>("actual steering"), "rad", "rad", "deg"));
			disposables.Add(BasicLabelAttacher.Attach(labelActualGear, OperationalInterface.Dataset.ItemAs<TransmissionGear>("transmission gear")));
			disposables.Add(NumericLabelAttacher.Attach(labelActualRPM, "F1", OperationalInterface.Dataset.ItemAs<double>("rpm")));
			disposables.Add(NumericLabelAttacher.Attach(labelActualEngineTorque, "F1", OperationalInterface.Dataset.ItemAs<double>("engine torque"), "N-m", "N-m", "ft-lbs"));
			disposables.Add(NumericLabelAttacher.Attach(labelActualBrakePressure, "F0", OperationalInterface.Dataset.ItemAs<double>("brake pressure")));
			disposables.Add(BasicLabelAttacher.Attach(labelCorrectionMode, OperationalInterface.Dataset.ItemAs<PoseCorrectionMode>("correction mode")));

			disposables.Add(NumericLabelAttacher.Attach(labelCommandedSpeed, "F1", OperationalInterface.Dataset.ItemAs<double>("commanded speed"), "m/s", "m/s", "mph"));
			disposables.Add(NumericLabelAttacher.Attach(labelCommandedSteering, "F1", OperationalInterface.Dataset.ItemAs<double>("commanded steering"), "rad", "rad", "deg"));
			disposables.Add(NumericLabelAttacher.Attach(labelCommandedEngineTorque, "F1", OperationalInterface.Dataset.ItemAs<double>("commanded engine torque"), "N-m", "N-m", "ft-lbs"));
			disposables.Add(NumericLabelAttacher.Attach(labelCommandedBrakePressure, "F0", OperationalInterface.Dataset.ItemAs<double>("commanded brake pressure")));
			disposables.Add(NumericLabelAttacher.Attach(labelRequestedAcceleration, "F2", OperationalInterface.Dataset.ItemAs<double>("requested acceleration")));
			disposables.Add(BasicLabelAttacher.Attach(labelCurrentBehavior, OperationalInterface.Dataset.ItemAs<string>("behavior string")));
			disposables.Add(BasicLabelAttacher.Attach(labelSpeedCommand, OperationalInterface.Dataset.ItemAs<string>("speed command")));

			disposables.Add(NumericLabelAttacher.Attach(labelPlanningRate, "F1", OperationalInterface.Dataset.ItemAs<double>("planning rate")));
			disposables.Add(NumericLabelAttacher.Attach(labelTrackingRate, "F1", OperationalInterface.Dataset.ItemAs<double>("tracking rate")));
			disposables.Add(NumericLabelAttacher.Attach(labelObstacleRate, "F1", OperationalInterface.Dataset.ItemAs<double>("obstacle rate")));
			disposables.Add(NumericLabelAttacher.Attach(labelBehaviorRate, "F1", OperationalInterface.Dataset.ItemAs<double>("behavior rate")));

			disposables.Add(CallbackAttacher.Attach(OperationalInterface.Dataset.ItemAs<bool>("route feasible"), delegate(bool val, string label) {
				labelPlanningResult.Text = val ? "Feasible" : "Infeasible";
				if (val) {
					labelPlanningResult.ForeColor = Color.Black;
				}
				else {
					labelPlanningResult.ForeColor = Color.Red;
				}
			}));
			disposables.Add(NumericLabelAttacher.Attach(labelBlockageDist, "F1", OperationalInterface.Dataset.ItemAs<double>("blockage dist"), "m", "m", "ft"));
		}

		#endregion

		private void formMap_FormClosed(object sender, FormClosedEventArgs e) {
			Services.RunControlService.RunModeChanged -= new EventHandler(RunControlService_RunModeChanged);
			OperationalInterface.Attached -= OperationalInterface_Attached;

			foreach (IDisposable obj in disposables) {
				obj.Dispose();
			}
		}

		private void buttonArcVoting_Click(object sender, EventArgs e) {
			formArcVoting formArc = new formArcVoting();
			formArc.Show();
		}
		
	}
}