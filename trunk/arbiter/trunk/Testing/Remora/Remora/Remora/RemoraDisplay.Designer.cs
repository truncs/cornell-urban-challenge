namespace Remora
{
	partial class RemoraDisplay
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RemoraDisplay));
			this.statusStrip1 = new System.Windows.Forms.StatusStrip();
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.loadRndfToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.loadMdfToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator10 = new System.Windows.Forms.ToolStripSeparator();
			this.LoadRecentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.GenerateGpsToolStripItem = new System.Windows.Forms.ToolStripMenuItem();
			this.updateMdfToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
			this.retrieveRndfToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.retrieveMdfToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.readmeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
			this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStrip1 = new System.Windows.Forms.ToolStrip();
			this.InitializeCommunicationsButton = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this.ConnectArbiter = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.BeginDataStream = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.StopDataStreamButton = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
			this.TrackVehicle = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.ZoomIn = new System.Windows.Forms.ToolStripButton();
			this.ZoomStandard = new System.Windows.Forms.ToolStripButton();
			this.ZoomOut = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.PingArbiterToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
			this.SyncNetworkDataToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
			this.PoseLogToolStriplabel = new System.Windows.Forms.ToolStripLabel();
			this.LogPoseToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.RestartPoseLog = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator9 = new System.Windows.Forms.ToolStripSeparator();
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.splitContainer5 = new System.Windows.Forms.SplitContainer();
			this.roadDisplay1 = new Remora.Display.RoadDisplay();
			this.splitContainer4 = new System.Windows.Forms.SplitContainer();
			this.toolStrip2 = new System.Windows.Forms.ToolStrip();
			this.OutputToolStrip2Button = new System.Windows.Forms.ToolStripButton();
			this.richTextBox1 = new System.Windows.Forms.RichTextBox();
			this.splitContainer2 = new System.Windows.Forms.SplitContainer();
			this.RestartArbiterButton = new System.Windows.Forms.Button();
			this.Stop = new System.Windows.Forms.Button();
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.GeneralTab = new System.Windows.Forms.TabPage();
			this.OperationalGroupBox = new System.Windows.Forms.GroupBox();
			this.CarModeLabelTextGeneralTab = new System.Windows.Forms.Label();
			this.CarModeLabelGeneralTab = new System.Windows.Forms.Label();
			this.SceneEstimatorGroupBoxGeneralTab = new System.Windows.Forms.GroupBox();
			this.StaticObstaclesTextGeneralTab = new System.Windows.Forms.Label();
			this.DynamicObstaclesTextGeneralTab = new System.Windows.Forms.Label();
			this.DynamicObstaclesLabelGeneralTab = new System.Windows.Forms.Label();
			this.StaticObstaclesLabelGeneralTab = new System.Windows.Forms.Label();
			this.ArbiterGroupBoxGeneralTab = new System.Windows.Forms.GroupBox();
			this.GeneralTabGoalsLeftLabelText = new System.Windows.Forms.Label();
			this.GoalsLeftLabelGeneralTab = new System.Windows.Forms.Label();
			this.GeneralTabRouteTimeText = new System.Windows.Forms.Label();
			this.GeneralTabArbiterRouteDistanceText = new System.Windows.Forms.Label();
			this.GeneralTabCurrentGoalText = new System.Windows.Forms.Label();
			this.GeneralTabArbiterCarModeText = new System.Windows.Forms.Label();
			this.ArbiterStateTextGeneralTab = new System.Windows.Forms.Label();
			this.GeneralTabRouteTimeLabel = new System.Windows.Forms.Label();
			this.RouteDistanceLabelGeneralTab = new System.Windows.Forms.Label();
			this.GeneralTabCurrentGoalLabel = new System.Windows.Forms.Label();
			this.ArbiterCarModeLabelGeneralTab = new System.Windows.Forms.Label();
			this.ArbiterStateLabelGeneralTab = new System.Windows.Forms.Label();
			this.GeneralPosteriorPoseGroupBox = new System.Windows.Forms.GroupBox();
			this.GeneralTabApproximateAcceleration = new System.Windows.Forms.Label();
			this.GeneralTabApproximateAccelerationTitle = new System.Windows.Forms.Label();
			this.GeneralTabLaneEstimateConfidence = new System.Windows.Forms.Label();
			this.GeneralTabLaneEstimateConfidenceTitle = new System.Windows.Forms.Label();
			this.GeneralTabFinalLane = new System.Windows.Forms.Label();
			this.GeneralTabFinalLaneTitle = new System.Windows.Forms.Label();
			this.GeneralTabInitialLane = new System.Windows.Forms.Label();
			this.GeneralTabInitialLaneTitle = new System.Windows.Forms.Label();
			this.GeneralTabSpeed = new System.Windows.Forms.Label();
			this.GeneralTabSpeedTitle = new System.Windows.Forms.Label();
			this.GeneralTabHeading = new System.Windows.Forms.Label();
			this.GeneralTabHeadingTitle = new System.Windows.Forms.Label();
			this.GeneralTabPosition = new System.Windows.Forms.Label();
			this.GeneralTabPositionTitle = new System.Windows.Forms.Label();
			this.NavigationTab = new System.Windows.Forms.TabPage();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.RoadTab = new System.Windows.Forms.TabPage();
			this.IntersectionTab = new System.Windows.Forms.TabPage();
			this.ZoneTab = new System.Windows.Forms.TabPage();
			this.OptionsTab = new System.Windows.Forms.TabPage();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.GeneralGroupBox = new System.Windows.Forms.GroupBox();
			this.PoseLogDisplayOptionsTab = new System.Windows.Forms.CheckBox();
			this.OperationalLanePathDisplayCheckBoxOptionsTab = new System.Windows.Forms.CheckBox();
			this.ArbiterLanePathDisplayCheckBoxOptionsTab = new System.Windows.Forms.CheckBox();
			this.NavigationGroupBox = new System.Windows.Forms.GroupBox();
			this.FullRouteDisplayCheckBoxOptionsTab = new System.Windows.Forms.CheckBox();
			this.RoadGroupBox = new System.Windows.Forms.GroupBox();
			this.IntersectionGroupBox = new System.Windows.Forms.GroupBox();
			this.ZoneGroupBox = new System.Windows.Forms.GroupBox();
			this.DisplayGroupBox = new System.Windows.Forms.GroupBox();
			this.IntersectionBoundsCheckBoxOptionsTab = new System.Windows.Forms.CheckBox();
			this.DisplayInterconnectSplinesCheckBoxOptionsTab = new System.Windows.Forms.CheckBox();
			this.DisplayLaneSplinesCheckBoxOptionsTab = new System.Windows.Forms.CheckBox();
			this.DisplayGoalsCheckBoxOptionsTab = new System.Windows.Forms.CheckBox();
			this.DisplayUserWaypointIdCheckBox = new System.Windows.Forms.CheckBox();
			this.DisplayRndfWaypointIdCheckBox = new System.Windows.Forms.CheckBox();
			this.DisplayInterconnectsCheckBox = new System.Windows.Forms.CheckBox();
			this.DisplayUserWaypointsCheckBox = new System.Windows.Forms.CheckBox();
			this.DisplayRndfWaypointsCheckBox = new System.Windows.Forms.CheckBox();
			this.DisplayUserPartitionsCheckBox = new System.Windows.Forms.CheckBox();
			this.DisplayLanePartitionCheckBox = new System.Windows.Forms.CheckBox();
			this.DisplayRndfCheckBox = new System.Windows.Forms.CheckBox();
			this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
			this.DisplayDeletedVehiclesBoxOptions = new System.Windows.Forms.CheckBox();
			this.menuStrip1.SuspendLayout();
			this.toolStrip1.SuspendLayout();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			this.splitContainer5.Panel1.SuspendLayout();
			this.splitContainer5.Panel2.SuspendLayout();
			this.splitContainer5.SuspendLayout();
			this.splitContainer4.Panel1.SuspendLayout();
			this.splitContainer4.Panel2.SuspendLayout();
			this.splitContainer4.SuspendLayout();
			this.toolStrip2.SuspendLayout();
			this.splitContainer2.Panel1.SuspendLayout();
			this.splitContainer2.Panel2.SuspendLayout();
			this.splitContainer2.SuspendLayout();
			this.tabControl1.SuspendLayout();
			this.GeneralTab.SuspendLayout();
			this.OperationalGroupBox.SuspendLayout();
			this.SceneEstimatorGroupBoxGeneralTab.SuspendLayout();
			this.ArbiterGroupBoxGeneralTab.SuspendLayout();
			this.GeneralPosteriorPoseGroupBox.SuspendLayout();
			this.NavigationTab.SuspendLayout();
			this.OptionsTab.SuspendLayout();
			this.tableLayoutPanel1.SuspendLayout();
			this.GeneralGroupBox.SuspendLayout();
			this.NavigationGroupBox.SuspendLayout();
			this.DisplayGroupBox.SuspendLayout();
			this.SuspendLayout();
			// 
			// statusStrip1
			// 
			this.statusStrip1.Location = new System.Drawing.Point(0, 656);
			this.statusStrip1.Name = "statusStrip1";
			this.statusStrip1.Size = new System.Drawing.Size(828, 22);
			this.statusStrip1.TabIndex = 0;
			this.statusStrip1.Text = "statusStrip1";
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.GenerateGpsToolStripItem,
            this.helpToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(828, 24);
			this.menuStrip1.TabIndex = 1;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.loadRndfToolStripMenuItem,
            this.loadMdfToolStripMenuItem,
            this.toolStripSeparator10,
            this.LoadRecentToolStripMenuItem,
            this.toolStripMenuItem1,
            this.exitToolStripMenuItem});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(35, 20);
			this.fileToolStripMenuItem.Text = "&File";
			// 
			// loadRndfToolStripMenuItem
			// 
			this.loadRndfToolStripMenuItem.Image = global::Remora.Properties.Resources.Component;
			this.loadRndfToolStripMenuItem.Name = "loadRndfToolStripMenuItem";
			this.loadRndfToolStripMenuItem.Size = new System.Drawing.Size(134, 22);
			this.loadRndfToolStripMenuItem.Text = "Load &Rndf";
			this.loadRndfToolStripMenuItem.Click += new System.EventHandler(this.loadRndfToolStripMenuItem_Click);
			// 
			// loadMdfToolStripMenuItem
			// 
			this.loadMdfToolStripMenuItem.Image = global::Remora.Properties.Resources.Component2;
			this.loadMdfToolStripMenuItem.Name = "loadMdfToolStripMenuItem";
			this.loadMdfToolStripMenuItem.Size = new System.Drawing.Size(134, 22);
			this.loadMdfToolStripMenuItem.Text = "Load &Mdf";
			this.loadMdfToolStripMenuItem.Click += new System.EventHandler(this.loadMdfToolStripMenuItem_Click);
			// 
			// toolStripSeparator10
			// 
			this.toolStripSeparator10.Name = "toolStripSeparator10";
			this.toolStripSeparator10.Size = new System.Drawing.Size(131, 6);
			// 
			// LoadRecentToolStripMenuItem
			// 
			this.LoadRecentToolStripMenuItem.Name = "LoadRecentToolStripMenuItem";
			this.LoadRecentToolStripMenuItem.Size = new System.Drawing.Size(134, 22);
			this.LoadRecentToolStripMenuItem.Text = "Load Re&cent";
			this.LoadRecentToolStripMenuItem.Click += new System.EventHandler(this.LoadRecentToolStripMenuItem_Click);
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size(131, 6);
			// 
			// exitToolStripMenuItem
			// 
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(134, 22);
			this.exitToolStripMenuItem.Text = "E&xit";
			this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
			// 
			// GenerateGpsToolStripItem
			// 
			this.GenerateGpsToolStripItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.updateMdfToolStripMenuItem,
            this.toolStripMenuItem3,
            this.retrieveRndfToolStripMenuItem,
            this.retrieveMdfToolStripMenuItem});
			this.GenerateGpsToolStripItem.Name = "GenerateGpsToolStripItem";
			this.GenerateGpsToolStripItem.Size = new System.Drawing.Size(44, 20);
			this.GenerateGpsToolStripItem.Text = "&Tools";
			// 
			// updateMdfToolStripMenuItem
			// 
			this.updateMdfToolStripMenuItem.Name = "updateMdfToolStripMenuItem";
			this.updateMdfToolStripMenuItem.Size = new System.Drawing.Size(141, 22);
			this.updateMdfToolStripMenuItem.Text = "Update &Mdf";
			this.updateMdfToolStripMenuItem.Click += new System.EventHandler(this.updateMdfToolStripMenuItem_Click);
			// 
			// toolStripMenuItem3
			// 
			this.toolStripMenuItem3.Name = "toolStripMenuItem3";
			this.toolStripMenuItem3.Size = new System.Drawing.Size(138, 6);
			// 
			// retrieveRndfToolStripMenuItem
			// 
			this.retrieveRndfToolStripMenuItem.Name = "retrieveRndfToolStripMenuItem";
			this.retrieveRndfToolStripMenuItem.Size = new System.Drawing.Size(141, 22);
			this.retrieveRndfToolStripMenuItem.Text = "Retrieve &Rndf";
			this.retrieveRndfToolStripMenuItem.Click += new System.EventHandler(this.retrieveRndfToolStripMenuItem_Click);
			// 
			// retrieveMdfToolStripMenuItem
			// 
			this.retrieveMdfToolStripMenuItem.Name = "retrieveMdfToolStripMenuItem";
			this.retrieveMdfToolStripMenuItem.Size = new System.Drawing.Size(141, 22);
			this.retrieveMdfToolStripMenuItem.Text = "Retrieve M&df";
			this.retrieveMdfToolStripMenuItem.Click += new System.EventHandler(this.retrieveMdfToolStripMenuItem_Click);
			// 
			// helpToolStripMenuItem
			// 
			this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.readmeToolStripMenuItem,
            this.toolStripMenuItem2,
            this.aboutToolStripMenuItem});
			this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
			this.helpToolStripMenuItem.Size = new System.Drawing.Size(40, 20);
			this.helpToolStripMenuItem.Text = "&Help";
			// 
			// readmeToolStripMenuItem
			// 
			this.readmeToolStripMenuItem.Name = "readmeToolStripMenuItem";
			this.readmeToolStripMenuItem.Size = new System.Drawing.Size(113, 22);
			this.readmeToolStripMenuItem.Text = "&Readme";
			this.readmeToolStripMenuItem.Click += new System.EventHandler(this.readmeToolStripMenuItem_Click);
			// 
			// toolStripMenuItem2
			// 
			this.toolStripMenuItem2.Name = "toolStripMenuItem2";
			this.toolStripMenuItem2.Size = new System.Drawing.Size(110, 6);
			// 
			// aboutToolStripMenuItem
			// 
			this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
			this.aboutToolStripMenuItem.Size = new System.Drawing.Size(113, 22);
			this.aboutToolStripMenuItem.Text = "&About";
			this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
			// 
			// toolStrip1
			// 
			this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.InitializeCommunicationsButton,
            this.toolStripSeparator5,
            this.ConnectArbiter,
            this.toolStripSeparator4,
            this.BeginDataStream,
            this.toolStripSeparator1,
            this.StopDataStreamButton,
            this.toolStripSeparator6,
            this.TrackVehicle,
            this.toolStripSeparator2,
            this.ZoomIn,
            this.ZoomStandard,
            this.ZoomOut,
            this.toolStripSeparator3,
            this.PingArbiterToolStripButton,
            this.toolStripSeparator7,
            this.SyncNetworkDataToolStripButton,
            this.toolStripSeparator8,
            this.PoseLogToolStriplabel,
            this.LogPoseToolStripButton,
            this.RestartPoseLog,
            this.toolStripSeparator9});
			this.toolStrip1.Location = new System.Drawing.Point(0, 24);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.Size = new System.Drawing.Size(828, 25);
			this.toolStrip1.TabIndex = 2;
			this.toolStrip1.Text = "toolStrip1";
			// 
			// InitializeCommunicationsButton
			// 
			this.InitializeCommunicationsButton.Image = global::Remora.Properties.Resources.Refresh;
			this.InitializeCommunicationsButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.InitializeCommunicationsButton.Name = "InitializeCommunicationsButton";
			this.InitializeCommunicationsButton.Size = new System.Drawing.Size(95, 22);
			this.InitializeCommunicationsButton.Text = "Communicator";
			this.InitializeCommunicationsButton.Click += new System.EventHandler(this.InitializeCommunicationsButton_Click);
			// 
			// toolStripSeparator5
			// 
			this.toolStripSeparator5.Name = "toolStripSeparator5";
			this.toolStripSeparator5.Size = new System.Drawing.Size(6, 25);
			// 
			// ConnectArbiter
			// 
			this.ConnectArbiter.Image = global::Remora.Properties.Resources.Connect;
			this.ConnectArbiter.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.ConnectArbiter.Name = "ConnectArbiter";
			this.ConnectArbiter.Size = new System.Drawing.Size(103, 22);
			this.ConnectArbiter.Text = "Connect Arbiter";
			this.ConnectArbiter.Click += new System.EventHandler(this.ConnectArbiter_Click);
			// 
			// toolStripSeparator4
			// 
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			this.toolStripSeparator4.Size = new System.Drawing.Size(6, 25);
			// 
			// BeginDataStream
			// 
			this.BeginDataStream.CheckOnClick = true;
			this.BeginDataStream.Image = global::Remora.Properties.Resources.CabinetOpen;
			this.BeginDataStream.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.BeginDataStream.Name = "BeginDataStream";
			this.BeginDataStream.Size = new System.Drawing.Size(87, 22);
			this.BeginDataStream.Text = "Stream Data";
			this.BeginDataStream.Click += new System.EventHandler(this.BeginDataStream_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
			// 
			// StopDataStreamButton
			// 
			this.StopDataStreamButton.Image = global::Remora.Properties.Resources.NoAccess;
			this.StopDataStreamButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.StopDataStreamButton.Name = "StopDataStreamButton";
			this.StopDataStreamButton.Size = new System.Drawing.Size(75, 22);
			this.StopDataStreamButton.Text = "Stop Data";
			this.StopDataStreamButton.Click += new System.EventHandler(this.StopDataStreamButton_Click);
			// 
			// toolStripSeparator6
			// 
			this.toolStripSeparator6.Name = "toolStripSeparator6";
			this.toolStripSeparator6.Size = new System.Drawing.Size(6, 25);
			// 
			// TrackVehicle
			// 
			this.TrackVehicle.Checked = true;
			this.TrackVehicle.CheckOnClick = true;
			this.TrackVehicle.CheckState = System.Windows.Forms.CheckState.Checked;
			this.TrackVehicle.Image = global::Remora.Properties.Resources.Car;
			this.TrackVehicle.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.TrackVehicle.Name = "TrackVehicle";
			this.TrackVehicle.Size = new System.Drawing.Size(53, 22);
			this.TrackVehicle.Text = "Track";
			this.TrackVehicle.Click += new System.EventHandler(this.TrackVehicle_Click);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
			// 
			// ZoomIn
			// 
			this.ZoomIn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.ZoomIn.Image = global::Remora.Properties.Resources.ZoomIn;
			this.ZoomIn.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.ZoomIn.Name = "ZoomIn";
			this.ZoomIn.Size = new System.Drawing.Size(23, 22);
			this.ZoomIn.Text = "Zoom In";
			this.ZoomIn.Click += new System.EventHandler(this.ZoomIn_Click);
			// 
			// ZoomStandard
			// 
			this.ZoomStandard.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.ZoomStandard.Image = global::Remora.Properties.Resources.Zoom11;
			this.ZoomStandard.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.ZoomStandard.Name = "ZoomStandard";
			this.ZoomStandard.Size = new System.Drawing.Size(23, 22);
			this.ZoomStandard.Text = "Zoom Standard";
			this.ZoomStandard.Click += new System.EventHandler(this.ZoomStandard_Click);
			// 
			// ZoomOut
			// 
			this.ZoomOut.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.ZoomOut.Image = global::Remora.Properties.Resources.ZoomOut;
			this.ZoomOut.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.ZoomOut.Name = "ZoomOut";
			this.ZoomOut.Size = new System.Drawing.Size(23, 22);
			this.ZoomOut.Text = "Zoom Out";
			this.ZoomOut.Click += new System.EventHandler(this.ZoomOut_Click);
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
			// 
			// PingArbiterToolStripButton
			// 
			this.PingArbiterToolStripButton.Image = global::Remora.Properties.Resources.Speaker;
			this.PingArbiterToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.PingArbiterToolStripButton.Name = "PingArbiterToolStripButton";
			this.PingArbiterToolStripButton.Size = new System.Drawing.Size(83, 22);
			this.PingArbiterToolStripButton.Text = "Ping Arbiter";
			this.PingArbiterToolStripButton.Click += new System.EventHandler(this.PingArbiterToolStripButton_Click);
			// 
			// toolStripSeparator7
			// 
			this.toolStripSeparator7.Name = "toolStripSeparator7";
			this.toolStripSeparator7.Size = new System.Drawing.Size(6, 25);
			// 
			// SyncNetworkDataToolStripButton
			// 
			this.SyncNetworkDataToolStripButton.Image = global::Remora.Properties.Resources.Sync;
			this.SyncNetworkDataToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.SyncNetworkDataToolStripButton.Name = "SyncNetworkDataToolStripButton";
			this.SyncNetworkDataToolStripButton.Size = new System.Drawing.Size(50, 22);
			this.SyncNetworkDataToolStripButton.Text = "Sync";
			this.SyncNetworkDataToolStripButton.Click += new System.EventHandler(this.SyncNetworkDataToolStripButton_Click);
			// 
			// toolStripSeparator8
			// 
			this.toolStripSeparator8.Name = "toolStripSeparator8";
			this.toolStripSeparator8.Size = new System.Drawing.Size(6, 25);
			// 
			// PoseLogToolStriplabel
			// 
			this.PoseLogToolStriplabel.Name = "PoseLogToolStriplabel";
			this.PoseLogToolStriplabel.Size = new System.Drawing.Size(34, 22);
			this.PoseLogToolStriplabel.Text = "Pose:";
			// 
			// LogPoseToolStripButton
			// 
			this.LogPoseToolStripButton.Checked = true;
			this.LogPoseToolStripButton.CheckOnClick = true;
			this.LogPoseToolStripButton.CheckState = System.Windows.Forms.CheckState.Checked;
			this.LogPoseToolStripButton.Image = global::Remora.Properties.Resources.Notepage;
			this.LogPoseToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.LogPoseToolStripButton.Name = "LogPoseToolStripButton";
			this.LogPoseToolStripButton.Size = new System.Drawing.Size(44, 22);
			this.LogPoseToolStripButton.Text = "Log";
			this.LogPoseToolStripButton.Click += new System.EventHandler(this.LogPoseToolStripButton_Click);
			// 
			// RestartPoseLog
			// 
			this.RestartPoseLog.Image = global::Remora.Properties.Resources.Refresh;
			this.RestartPoseLog.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.RestartPoseLog.Name = "RestartPoseLog";
			this.RestartPoseLog.Size = new System.Drawing.Size(65, 22);
			this.RestartPoseLog.Text = "Refresh";
			this.RestartPoseLog.Click += new System.EventHandler(this.RestartPoseLog_Click);
			// 
			// toolStripSeparator9
			// 
			this.toolStripSeparator9.Name = "toolStripSeparator9";
			this.toolStripSeparator9.Size = new System.Drawing.Size(6, 25);
			// 
			// splitContainer1
			// 
			this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
			this.splitContainer1.IsSplitterFixed = true;
			this.splitContainer1.Location = new System.Drawing.Point(0, 49);
			this.splitContainer1.Name = "splitContainer1";
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.splitContainer5);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
			this.splitContainer1.Size = new System.Drawing.Size(828, 607);
			this.splitContainer1.SplitterDistance = 551;
			this.splitContainer1.TabIndex = 3;
			// 
			// splitContainer5
			// 
			this.splitContainer5.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer5.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
			this.splitContainer5.Location = new System.Drawing.Point(0, 0);
			this.splitContainer5.Name = "splitContainer5";
			this.splitContainer5.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer5.Panel1
			// 
			this.splitContainer5.Panel1.Controls.Add(this.roadDisplay1);
			// 
			// splitContainer5.Panel2
			// 
			this.splitContainer5.Panel2.Controls.Add(this.splitContainer4);
			this.splitContainer5.Size = new System.Drawing.Size(551, 607);
			this.splitContainer5.SplitterDistance = 473;
			this.splitContainer5.TabIndex = 0;
			// 
			// roadDisplay1
			// 
			this.roadDisplay1.BackColor = System.Drawing.Color.White;
			this.roadDisplay1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.roadDisplay1.Location = new System.Drawing.Point(0, 0);
			this.roadDisplay1.Name = "roadDisplay1";
			this.roadDisplay1.Size = new System.Drawing.Size(551, 473);
			this.roadDisplay1.TabIndex = 1;
			this.roadDisplay1.Zoom = 6F;
			// 
			// splitContainer4
			// 
			this.splitContainer4.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer4.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this.splitContainer4.IsSplitterFixed = true;
			this.splitContainer4.Location = new System.Drawing.Point(0, 0);
			this.splitContainer4.Name = "splitContainer4";
			this.splitContainer4.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer4.Panel1
			// 
			this.splitContainer4.Panel1.BackColor = System.Drawing.SystemColors.Control;
			this.splitContainer4.Panel1.Controls.Add(this.toolStrip2);
			this.splitContainer4.Panel1MinSize = 5;
			// 
			// splitContainer4.Panel2
			// 
			this.splitContainer4.Panel2.Controls.Add(this.richTextBox1);
			this.splitContainer4.Size = new System.Drawing.Size(551, 130);
			this.splitContainer4.SplitterDistance = 24;
			this.splitContainer4.SplitterWidth = 2;
			this.splitContainer4.TabIndex = 1;
			// 
			// toolStrip2
			// 
			this.toolStrip2.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.OutputToolStrip2Button});
			this.toolStrip2.Location = new System.Drawing.Point(0, 0);
			this.toolStrip2.Name = "toolStrip2";
			this.toolStrip2.Padding = new System.Windows.Forms.Padding(5, 2, 1, 0);
			this.toolStrip2.Size = new System.Drawing.Size(551, 25);
			this.toolStrip2.TabIndex = 0;
			this.toolStrip2.Text = "toolStrip2";
			// 
			// OutputToolStrip2Button
			// 
			this.OutputToolStrip2Button.Checked = true;
			this.OutputToolStrip2Button.CheckState = System.Windows.Forms.CheckState.Checked;
			this.OutputToolStrip2Button.Image = global::Remora.Properties.Resources.Notepage;
			this.OutputToolStrip2Button.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.OutputToolStrip2Button.Name = "OutputToolStrip2Button";
			this.OutputToolStrip2Button.Size = new System.Drawing.Size(61, 20);
			this.OutputToolStrip2Button.Text = "Output";
			// 
			// richTextBox1
			// 
			this.richTextBox1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.richTextBox1.Location = new System.Drawing.Point(0, 0);
			this.richTextBox1.Name = "richTextBox1";
			this.richTextBox1.Size = new System.Drawing.Size(551, 104);
			this.richTextBox1.TabIndex = 0;
			this.richTextBox1.Text = "";
			this.richTextBox1.WordWrap = false;
			// 
			// splitContainer2
			// 
			this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this.splitContainer2.Location = new System.Drawing.Point(0, 0);
			this.splitContainer2.Name = "splitContainer2";
			this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer2.Panel1
			// 
			this.splitContainer2.Panel1.Controls.Add(this.RestartArbiterButton);
			this.splitContainer2.Panel1.Controls.Add(this.Stop);
			// 
			// splitContainer2.Panel2
			// 
			this.splitContainer2.Panel2.Controls.Add(this.tabControl1);
			this.splitContainer2.Size = new System.Drawing.Size(273, 607);
			this.splitContainer2.SplitterDistance = 93;
			this.splitContainer2.TabIndex = 0;
			// 
			// RestartArbiterButton
			// 
			this.RestartArbiterButton.BackColor = System.Drawing.Color.RoyalBlue;
			this.RestartArbiterButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.RestartArbiterButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.RestartArbiterButton.ForeColor = System.Drawing.Color.White;
			this.RestartArbiterButton.Image = global::Remora.Properties.Resources.Add1;
			this.RestartArbiterButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.RestartArbiterButton.Location = new System.Drawing.Point(0, 47);
			this.RestartArbiterButton.Name = "RestartArbiterButton";
			this.RestartArbiterButton.Padding = new System.Windows.Forms.Padding(15, 0, 0, 0);
			this.RestartArbiterButton.Size = new System.Drawing.Size(273, 45);
			this.RestartArbiterButton.TabIndex = 3;
			this.RestartArbiterButton.Text = "RESTART";
			this.RestartArbiterButton.UseVisualStyleBackColor = false;
			this.RestartArbiterButton.Click += new System.EventHandler(this.RestartArbiterButton_Click);
			// 
			// Stop
			// 
			this.Stop.BackColor = System.Drawing.Color.Maroon;
			this.Stop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.Stop.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Bold);
			this.Stop.ForeColor = System.Drawing.Color.White;
			this.Stop.Image = global::Remora.Properties.Resources.NoAccess2;
			this.Stop.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.Stop.Location = new System.Drawing.Point(0, 3);
			this.Stop.Name = "Stop";
			this.Stop.Padding = new System.Windows.Forms.Padding(15, 0, 0, 0);
			this.Stop.Size = new System.Drawing.Size(273, 45);
			this.Stop.TabIndex = 1;
			this.Stop.Text = "STOP";
			this.Stop.UseVisualStyleBackColor = false;
			this.Stop.Click += new System.EventHandler(this.Stop_Click);
			// 
			// tabControl1
			// 
			this.tabControl1.Controls.Add(this.GeneralTab);
			this.tabControl1.Controls.Add(this.NavigationTab);
			this.tabControl1.Controls.Add(this.RoadTab);
			this.tabControl1.Controls.Add(this.IntersectionTab);
			this.tabControl1.Controls.Add(this.ZoneTab);
			this.tabControl1.Controls.Add(this.OptionsTab);
			this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabControl1.Location = new System.Drawing.Point(0, 0);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size(273, 510);
			this.tabControl1.TabIndex = 1;
			// 
			// GeneralTab
			// 
			this.GeneralTab.Controls.Add(this.OperationalGroupBox);
			this.GeneralTab.Controls.Add(this.SceneEstimatorGroupBoxGeneralTab);
			this.GeneralTab.Controls.Add(this.ArbiterGroupBoxGeneralTab);
			this.GeneralTab.Controls.Add(this.GeneralPosteriorPoseGroupBox);
			this.GeneralTab.Location = new System.Drawing.Point(4, 22);
			this.GeneralTab.Name = "GeneralTab";
			this.GeneralTab.Padding = new System.Windows.Forms.Padding(3);
			this.GeneralTab.Size = new System.Drawing.Size(265, 484);
			this.GeneralTab.TabIndex = 0;
			this.GeneralTab.Text = "General";
			this.GeneralTab.UseVisualStyleBackColor = true;
			// 
			// OperationalGroupBox
			// 
			this.OperationalGroupBox.Controls.Add(this.CarModeLabelTextGeneralTab);
			this.OperationalGroupBox.Controls.Add(this.CarModeLabelGeneralTab);
			this.OperationalGroupBox.Location = new System.Drawing.Point(7, 233);
			this.OperationalGroupBox.Name = "OperationalGroupBox";
			this.OperationalGroupBox.Size = new System.Drawing.Size(250, 45);
			this.OperationalGroupBox.TabIndex = 4;
			this.OperationalGroupBox.TabStop = false;
			this.OperationalGroupBox.Text = "Operational";
			// 
			// CarModeLabelTextGeneralTab
			// 
			this.CarModeLabelTextGeneralTab.AutoSize = true;
			this.CarModeLabelTextGeneralTab.Location = new System.Drawing.Point(59, 22);
			this.CarModeLabelTextGeneralTab.Name = "CarModeLabelTextGeneralTab";
			this.CarModeLabelTextGeneralTab.Size = new System.Drawing.Size(10, 13);
			this.CarModeLabelTextGeneralTab.TabIndex = 1;
			this.CarModeLabelTextGeneralTab.Text = " ";
			// 
			// CarModeLabelGeneralTab
			// 
			this.CarModeLabelGeneralTab.AutoSize = true;
			this.CarModeLabelGeneralTab.Location = new System.Drawing.Point(6, 22);
			this.CarModeLabelGeneralTab.Name = "CarModeLabelGeneralTab";
			this.CarModeLabelGeneralTab.Size = new System.Drawing.Size(56, 13);
			this.CarModeLabelGeneralTab.TabIndex = 0;
			this.CarModeLabelGeneralTab.Text = "Car Mode:";
			// 
			// SceneEstimatorGroupBoxGeneralTab
			// 
			this.SceneEstimatorGroupBoxGeneralTab.Controls.Add(this.StaticObstaclesTextGeneralTab);
			this.SceneEstimatorGroupBoxGeneralTab.Controls.Add(this.DynamicObstaclesTextGeneralTab);
			this.SceneEstimatorGroupBoxGeneralTab.Controls.Add(this.DynamicObstaclesLabelGeneralTab);
			this.SceneEstimatorGroupBoxGeneralTab.Controls.Add(this.StaticObstaclesLabelGeneralTab);
			this.SceneEstimatorGroupBoxGeneralTab.Location = new System.Drawing.Point(7, 164);
			this.SceneEstimatorGroupBoxGeneralTab.Name = "SceneEstimatorGroupBoxGeneralTab";
			this.SceneEstimatorGroupBoxGeneralTab.Size = new System.Drawing.Size(250, 62);
			this.SceneEstimatorGroupBoxGeneralTab.TabIndex = 3;
			this.SceneEstimatorGroupBoxGeneralTab.TabStop = false;
			this.SceneEstimatorGroupBoxGeneralTab.Text = "Scene Estimator";
			// 
			// StaticObstaclesTextGeneralTab
			// 
			this.StaticObstaclesTextGeneralTab.AutoSize = true;
			this.StaticObstaclesTextGeneralTab.Location = new System.Drawing.Point(97, 20);
			this.StaticObstaclesTextGeneralTab.Name = "StaticObstaclesTextGeneralTab";
			this.StaticObstaclesTextGeneralTab.Size = new System.Drawing.Size(10, 13);
			this.StaticObstaclesTextGeneralTab.TabIndex = 3;
			this.StaticObstaclesTextGeneralTab.Text = " ";
			// 
			// DynamicObstaclesTextGeneralTab
			// 
			this.DynamicObstaclesTextGeneralTab.AutoSize = true;
			this.DynamicObstaclesTextGeneralTab.Location = new System.Drawing.Point(106, 38);
			this.DynamicObstaclesTextGeneralTab.Name = "DynamicObstaclesTextGeneralTab";
			this.DynamicObstaclesTextGeneralTab.Size = new System.Drawing.Size(10, 13);
			this.DynamicObstaclesTextGeneralTab.TabIndex = 2;
			this.DynamicObstaclesTextGeneralTab.Text = " ";
			// 
			// DynamicObstaclesLabelGeneralTab
			// 
			this.DynamicObstaclesLabelGeneralTab.AutoSize = true;
			this.DynamicObstaclesLabelGeneralTab.Location = new System.Drawing.Point(7, 38);
			this.DynamicObstaclesLabelGeneralTab.Name = "DynamicObstaclesLabelGeneralTab";
			this.DynamicObstaclesLabelGeneralTab.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
			this.DynamicObstaclesLabelGeneralTab.Size = new System.Drawing.Size(101, 18);
			this.DynamicObstaclesLabelGeneralTab.TabIndex = 1;
			this.DynamicObstaclesLabelGeneralTab.Text = "Dynamic Obstacles:";
			// 
			// StaticObstaclesLabelGeneralTab
			// 
			this.StaticObstaclesLabelGeneralTab.AutoSize = true;
			this.StaticObstaclesLabelGeneralTab.Location = new System.Drawing.Point(7, 20);
			this.StaticObstaclesLabelGeneralTab.Name = "StaticObstaclesLabelGeneralTab";
			this.StaticObstaclesLabelGeneralTab.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
			this.StaticObstaclesLabelGeneralTab.Size = new System.Drawing.Size(87, 18);
			this.StaticObstaclesLabelGeneralTab.TabIndex = 0;
			this.StaticObstaclesLabelGeneralTab.Text = "Static Obstacles:";
			// 
			// ArbiterGroupBoxGeneralTab
			// 
			this.ArbiterGroupBoxGeneralTab.Controls.Add(this.GeneralTabGoalsLeftLabelText);
			this.ArbiterGroupBoxGeneralTab.Controls.Add(this.GoalsLeftLabelGeneralTab);
			this.ArbiterGroupBoxGeneralTab.Controls.Add(this.GeneralTabRouteTimeText);
			this.ArbiterGroupBoxGeneralTab.Controls.Add(this.GeneralTabArbiterRouteDistanceText);
			this.ArbiterGroupBoxGeneralTab.Controls.Add(this.GeneralTabCurrentGoalText);
			this.ArbiterGroupBoxGeneralTab.Controls.Add(this.GeneralTabArbiterCarModeText);
			this.ArbiterGroupBoxGeneralTab.Controls.Add(this.ArbiterStateTextGeneralTab);
			this.ArbiterGroupBoxGeneralTab.Controls.Add(this.GeneralTabRouteTimeLabel);
			this.ArbiterGroupBoxGeneralTab.Controls.Add(this.RouteDistanceLabelGeneralTab);
			this.ArbiterGroupBoxGeneralTab.Controls.Add(this.GeneralTabCurrentGoalLabel);
			this.ArbiterGroupBoxGeneralTab.Controls.Add(this.ArbiterCarModeLabelGeneralTab);
			this.ArbiterGroupBoxGeneralTab.Controls.Add(this.ArbiterStateLabelGeneralTab);
			this.ArbiterGroupBoxGeneralTab.Location = new System.Drawing.Point(7, 284);
			this.ArbiterGroupBoxGeneralTab.Name = "ArbiterGroupBoxGeneralTab";
			this.ArbiterGroupBoxGeneralTab.Size = new System.Drawing.Size(251, 133);
			this.ArbiterGroupBoxGeneralTab.TabIndex = 1;
			this.ArbiterGroupBoxGeneralTab.TabStop = false;
			this.ArbiterGroupBoxGeneralTab.Text = "Arbiter";
			// 
			// GeneralTabGoalsLeftLabelText
			// 
			this.GeneralTabGoalsLeftLabelText.AutoSize = true;
			this.GeneralTabGoalsLeftLabelText.Location = new System.Drawing.Point(64, 110);
			this.GeneralTabGoalsLeftLabelText.Name = "GeneralTabGoalsLeftLabelText";
			this.GeneralTabGoalsLeftLabelText.Size = new System.Drawing.Size(10, 13);
			this.GeneralTabGoalsLeftLabelText.TabIndex = 11;
			this.GeneralTabGoalsLeftLabelText.Text = " ";
			// 
			// GoalsLeftLabelGeneralTab
			// 
			this.GoalsLeftLabelGeneralTab.AutoSize = true;
			this.GoalsLeftLabelGeneralTab.Location = new System.Drawing.Point(7, 110);
			this.GoalsLeftLabelGeneralTab.Name = "GoalsLeftLabelGeneralTab";
			this.GoalsLeftLabelGeneralTab.Size = new System.Drawing.Size(58, 13);
			this.GoalsLeftLabelGeneralTab.TabIndex = 10;
			this.GoalsLeftLabelGeneralTab.Text = "Goals Left:";
			// 
			// GeneralTabRouteTimeText
			// 
			this.GeneralTabRouteTimeText.AutoSize = true;
			this.GeneralTabRouteTimeText.Location = new System.Drawing.Point(78, 92);
			this.GeneralTabRouteTimeText.Name = "GeneralTabRouteTimeText";
			this.GeneralTabRouteTimeText.Size = new System.Drawing.Size(10, 13);
			this.GeneralTabRouteTimeText.TabIndex = 9;
			this.GeneralTabRouteTimeText.Text = " ";
			// 
			// GeneralTabArbiterRouteDistanceText
			// 
			this.GeneralTabArbiterRouteDistanceText.AutoSize = true;
			this.GeneralTabArbiterRouteDistanceText.Location = new System.Drawing.Point(97, 74);
			this.GeneralTabArbiterRouteDistanceText.Name = "GeneralTabArbiterRouteDistanceText";
			this.GeneralTabArbiterRouteDistanceText.Size = new System.Drawing.Size(10, 13);
			this.GeneralTabArbiterRouteDistanceText.TabIndex = 8;
			this.GeneralTabArbiterRouteDistanceText.Text = " ";
			// 
			// GeneralTabCurrentGoalText
			// 
			this.GeneralTabCurrentGoalText.AutoSize = true;
			this.GeneralTabCurrentGoalText.Location = new System.Drawing.Point(77, 56);
			this.GeneralTabCurrentGoalText.Name = "GeneralTabCurrentGoalText";
			this.GeneralTabCurrentGoalText.Size = new System.Drawing.Size(10, 13);
			this.GeneralTabCurrentGoalText.TabIndex = 7;
			this.GeneralTabCurrentGoalText.Text = " ";
			// 
			// GeneralTabArbiterCarModeText
			// 
			this.GeneralTabArbiterCarModeText.AutoSize = true;
			this.GeneralTabArbiterCarModeText.Location = new System.Drawing.Point(97, 38);
			this.GeneralTabArbiterCarModeText.Name = "GeneralTabArbiterCarModeText";
			this.GeneralTabArbiterCarModeText.Size = new System.Drawing.Size(10, 13);
			this.GeneralTabArbiterCarModeText.TabIndex = 6;
			this.GeneralTabArbiterCarModeText.Text = " ";
			// 
			// ArbiterStateTextGeneralTab
			// 
			this.ArbiterStateTextGeneralTab.AutoSize = true;
			this.ArbiterStateTextGeneralTab.Location = new System.Drawing.Point(73, 20);
			this.ArbiterStateTextGeneralTab.Name = "ArbiterStateTextGeneralTab";
			this.ArbiterStateTextGeneralTab.Size = new System.Drawing.Size(10, 13);
			this.ArbiterStateTextGeneralTab.TabIndex = 5;
			this.ArbiterStateTextGeneralTab.Text = " ";
			// 
			// GeneralTabRouteTimeLabel
			// 
			this.GeneralTabRouteTimeLabel.AutoSize = true;
			this.GeneralTabRouteTimeLabel.Location = new System.Drawing.Point(7, 92);
			this.GeneralTabRouteTimeLabel.Name = "GeneralTabRouteTimeLabel";
			this.GeneralTabRouteTimeLabel.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
			this.GeneralTabRouteTimeLabel.Size = new System.Drawing.Size(65, 18);
			this.GeneralTabRouteTimeLabel.TabIndex = 4;
			this.GeneralTabRouteTimeLabel.Text = "Route Time:";
			// 
			// RouteDistanceLabelGeneralTab
			// 
			this.RouteDistanceLabelGeneralTab.AutoSize = true;
			this.RouteDistanceLabelGeneralTab.Location = new System.Drawing.Point(7, 74);
			this.RouteDistanceLabelGeneralTab.Name = "RouteDistanceLabelGeneralTab";
			this.RouteDistanceLabelGeneralTab.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
			this.RouteDistanceLabelGeneralTab.Size = new System.Drawing.Size(84, 18);
			this.RouteDistanceLabelGeneralTab.TabIndex = 3;
			this.RouteDistanceLabelGeneralTab.Text = "Route Distance:";
			// 
			// GeneralTabCurrentGoalLabel
			// 
			this.GeneralTabCurrentGoalLabel.AutoSize = true;
			this.GeneralTabCurrentGoalLabel.Location = new System.Drawing.Point(7, 56);
			this.GeneralTabCurrentGoalLabel.Name = "GeneralTabCurrentGoalLabel";
			this.GeneralTabCurrentGoalLabel.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
			this.GeneralTabCurrentGoalLabel.Size = new System.Drawing.Size(69, 18);
			this.GeneralTabCurrentGoalLabel.TabIndex = 2;
			this.GeneralTabCurrentGoalLabel.Text = "Current Goal:";
			// 
			// ArbiterCarModeLabelGeneralTab
			// 
			this.ArbiterCarModeLabelGeneralTab.AutoSize = true;
			this.ArbiterCarModeLabelGeneralTab.Location = new System.Drawing.Point(7, 38);
			this.ArbiterCarModeLabelGeneralTab.Name = "ArbiterCarModeLabelGeneralTab";
			this.ArbiterCarModeLabelGeneralTab.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
			this.ArbiterCarModeLabelGeneralTab.Size = new System.Drawing.Size(89, 18);
			this.ArbiterCarModeLabelGeneralTab.TabIndex = 1;
			this.ArbiterCarModeLabelGeneralTab.Text = "Arbiter Car Mode:";
			// 
			// ArbiterStateLabelGeneralTab
			// 
			this.ArbiterStateLabelGeneralTab.AutoSize = true;
			this.ArbiterStateLabelGeneralTab.Location = new System.Drawing.Point(7, 20);
			this.ArbiterStateLabelGeneralTab.Name = "ArbiterStateLabelGeneralTab";
			this.ArbiterStateLabelGeneralTab.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
			this.ArbiterStateLabelGeneralTab.Size = new System.Drawing.Size(68, 18);
			this.ArbiterStateLabelGeneralTab.TabIndex = 0;
			this.ArbiterStateLabelGeneralTab.Text = "Arbiter State:";
			// 
			// GeneralPosteriorPoseGroupBox
			// 
			this.GeneralPosteriorPoseGroupBox.Controls.Add(this.GeneralTabApproximateAcceleration);
			this.GeneralPosteriorPoseGroupBox.Controls.Add(this.GeneralTabApproximateAccelerationTitle);
			this.GeneralPosteriorPoseGroupBox.Controls.Add(this.GeneralTabLaneEstimateConfidence);
			this.GeneralPosteriorPoseGroupBox.Controls.Add(this.GeneralTabLaneEstimateConfidenceTitle);
			this.GeneralPosteriorPoseGroupBox.Controls.Add(this.GeneralTabFinalLane);
			this.GeneralPosteriorPoseGroupBox.Controls.Add(this.GeneralTabFinalLaneTitle);
			this.GeneralPosteriorPoseGroupBox.Controls.Add(this.GeneralTabInitialLane);
			this.GeneralPosteriorPoseGroupBox.Controls.Add(this.GeneralTabInitialLaneTitle);
			this.GeneralPosteriorPoseGroupBox.Controls.Add(this.GeneralTabSpeed);
			this.GeneralPosteriorPoseGroupBox.Controls.Add(this.GeneralTabSpeedTitle);
			this.GeneralPosteriorPoseGroupBox.Controls.Add(this.GeneralTabHeading);
			this.GeneralPosteriorPoseGroupBox.Controls.Add(this.GeneralTabHeadingTitle);
			this.GeneralPosteriorPoseGroupBox.Controls.Add(this.GeneralTabPosition);
			this.GeneralPosteriorPoseGroupBox.Controls.Add(this.GeneralTabPositionTitle);
			this.GeneralPosteriorPoseGroupBox.Location = new System.Drawing.Point(7, 7);
			this.GeneralPosteriorPoseGroupBox.Name = "GeneralPosteriorPoseGroupBox";
			this.GeneralPosteriorPoseGroupBox.Size = new System.Drawing.Size(250, 151);
			this.GeneralPosteriorPoseGroupBox.TabIndex = 0;
			this.GeneralPosteriorPoseGroupBox.TabStop = false;
			this.GeneralPosteriorPoseGroupBox.Text = "Posterior Pose";
			// 
			// GeneralTabApproximateAcceleration
			// 
			this.GeneralTabApproximateAcceleration.AutoSize = true;
			this.GeneralTabApproximateAcceleration.Location = new System.Drawing.Point(112, 74);
			this.GeneralTabApproximateAcceleration.Name = "GeneralTabApproximateAcceleration";
			this.GeneralTabApproximateAcceleration.Size = new System.Drawing.Size(0, 13);
			this.GeneralTabApproximateAcceleration.TabIndex = 13;
			// 
			// GeneralTabApproximateAccelerationTitle
			// 
			this.GeneralTabApproximateAccelerationTitle.AutoSize = true;
			this.GeneralTabApproximateAccelerationTitle.Location = new System.Drawing.Point(7, 74);
			this.GeneralTabApproximateAccelerationTitle.Name = "GeneralTabApproximateAccelerationTitle";
			this.GeneralTabApproximateAccelerationTitle.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
			this.GeneralTabApproximateAccelerationTitle.Size = new System.Drawing.Size(105, 18);
			this.GeneralTabApproximateAccelerationTitle.TabIndex = 12;
			this.GeneralTabApproximateAccelerationTitle.Text = "Approx Acceleration:";
			// 
			// GeneralTabLaneEstimateConfidence
			// 
			this.GeneralTabLaneEstimateConfidence.AutoSize = true;
			this.GeneralTabLaneEstimateConfidence.Location = new System.Drawing.Point(147, 128);
			this.GeneralTabLaneEstimateConfidence.Name = "GeneralTabLaneEstimateConfidence";
			this.GeneralTabLaneEstimateConfidence.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
			this.GeneralTabLaneEstimateConfidence.Size = new System.Drawing.Size(0, 18);
			this.GeneralTabLaneEstimateConfidence.TabIndex = 11;
			// 
			// GeneralTabLaneEstimateConfidenceTitle
			// 
			this.GeneralTabLaneEstimateConfidenceTitle.AutoSize = true;
			this.GeneralTabLaneEstimateConfidenceTitle.Location = new System.Drawing.Point(7, 128);
			this.GeneralTabLaneEstimateConfidenceTitle.Name = "GeneralTabLaneEstimateConfidenceTitle";
			this.GeneralTabLaneEstimateConfidenceTitle.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
			this.GeneralTabLaneEstimateConfidenceTitle.Size = new System.Drawing.Size(134, 18);
			this.GeneralTabLaneEstimateConfidenceTitle.TabIndex = 10;
			this.GeneralTabLaneEstimateConfidenceTitle.Text = "Lane Estimate Confidence:";
			// 
			// GeneralTabFinalLane
			// 
			this.GeneralTabFinalLane.AutoSize = true;
			this.GeneralTabFinalLane.Location = new System.Drawing.Point(75, 110);
			this.GeneralTabFinalLane.Name = "GeneralTabFinalLane";
			this.GeneralTabFinalLane.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
			this.GeneralTabFinalLane.Size = new System.Drawing.Size(0, 18);
			this.GeneralTabFinalLane.TabIndex = 9;
			// 
			// GeneralTabFinalLaneTitle
			// 
			this.GeneralTabFinalLaneTitle.AutoSize = true;
			this.GeneralTabFinalLaneTitle.Location = new System.Drawing.Point(7, 110);
			this.GeneralTabFinalLaneTitle.Name = "GeneralTabFinalLaneTitle";
			this.GeneralTabFinalLaneTitle.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
			this.GeneralTabFinalLaneTitle.Size = new System.Drawing.Size(59, 18);
			this.GeneralTabFinalLaneTitle.TabIndex = 8;
			this.GeneralTabFinalLaneTitle.Text = "Final Lane:";
			// 
			// GeneralTabInitialLane
			// 
			this.GeneralTabInitialLane.AutoSize = true;
			this.GeneralTabInitialLane.Location = new System.Drawing.Point(75, 92);
			this.GeneralTabInitialLane.Name = "GeneralTabInitialLane";
			this.GeneralTabInitialLane.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
			this.GeneralTabInitialLane.Size = new System.Drawing.Size(0, 18);
			this.GeneralTabInitialLane.TabIndex = 7;
			// 
			// GeneralTabInitialLaneTitle
			// 
			this.GeneralTabInitialLaneTitle.AutoSize = true;
			this.GeneralTabInitialLaneTitle.Location = new System.Drawing.Point(7, 92);
			this.GeneralTabInitialLaneTitle.Name = "GeneralTabInitialLaneTitle";
			this.GeneralTabInitialLaneTitle.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
			this.GeneralTabInitialLaneTitle.Size = new System.Drawing.Size(61, 18);
			this.GeneralTabInitialLaneTitle.TabIndex = 6;
			this.GeneralTabInitialLaneTitle.Text = "Initial Lane:";
			// 
			// GeneralTabSpeed
			// 
			this.GeneralTabSpeed.AutoSize = true;
			this.GeneralTabSpeed.Location = new System.Drawing.Point(64, 56);
			this.GeneralTabSpeed.Name = "GeneralTabSpeed";
			this.GeneralTabSpeed.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
			this.GeneralTabSpeed.Size = new System.Drawing.Size(10, 18);
			this.GeneralTabSpeed.TabIndex = 5;
			this.GeneralTabSpeed.Text = " ";
			// 
			// GeneralTabSpeedTitle
			// 
			this.GeneralTabSpeedTitle.AutoSize = true;
			this.GeneralTabSpeedTitle.Location = new System.Drawing.Point(7, 56);
			this.GeneralTabSpeedTitle.Name = "GeneralTabSpeedTitle";
			this.GeneralTabSpeedTitle.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
			this.GeneralTabSpeedTitle.Size = new System.Drawing.Size(41, 18);
			this.GeneralTabSpeedTitle.TabIndex = 4;
			this.GeneralTabSpeedTitle.Text = "Speed:";
			// 
			// GeneralTabHeading
			// 
			this.GeneralTabHeading.AutoSize = true;
			this.GeneralTabHeading.Location = new System.Drawing.Point(61, 38);
			this.GeneralTabHeading.Name = "GeneralTabHeading";
			this.GeneralTabHeading.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
			this.GeneralTabHeading.Size = new System.Drawing.Size(0, 18);
			this.GeneralTabHeading.TabIndex = 3;
			// 
			// GeneralTabHeadingTitle
			// 
			this.GeneralTabHeadingTitle.AutoSize = true;
			this.GeneralTabHeadingTitle.Location = new System.Drawing.Point(7, 38);
			this.GeneralTabHeadingTitle.Name = "GeneralTabHeadingTitle";
			this.GeneralTabHeadingTitle.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
			this.GeneralTabHeadingTitle.Size = new System.Drawing.Size(50, 18);
			this.GeneralTabHeadingTitle.TabIndex = 2;
			this.GeneralTabHeadingTitle.Text = "Heading:";
			// 
			// GeneralTabPosition
			// 
			this.GeneralTabPosition.AutoSize = true;
			this.GeneralTabPosition.Location = new System.Drawing.Point(61, 19);
			this.GeneralTabPosition.Name = "GeneralTabPosition";
			this.GeneralTabPosition.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
			this.GeneralTabPosition.Size = new System.Drawing.Size(0, 18);
			this.GeneralTabPosition.TabIndex = 1;
			// 
			// GeneralTabPositionTitle
			// 
			this.GeneralTabPositionTitle.AutoSize = true;
			this.GeneralTabPositionTitle.Location = new System.Drawing.Point(7, 20);
			this.GeneralTabPositionTitle.Name = "GeneralTabPositionTitle";
			this.GeneralTabPositionTitle.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
			this.GeneralTabPositionTitle.Size = new System.Drawing.Size(47, 18);
			this.GeneralTabPositionTitle.TabIndex = 0;
			this.GeneralTabPositionTitle.Text = "Position:";
			// 
			// NavigationTab
			// 
			this.NavigationTab.Controls.Add(this.groupBox1);
			this.NavigationTab.Location = new System.Drawing.Point(4, 22);
			this.NavigationTab.Name = "NavigationTab";
			this.NavigationTab.Padding = new System.Windows.Forms.Padding(3);
			this.NavigationTab.Size = new System.Drawing.Size(265, 484);
			this.NavigationTab.TabIndex = 1;
			this.NavigationTab.Text = "Nav";
			this.NavigationTab.UseVisualStyleBackColor = true;
			// 
			// groupBox1
			// 
			this.groupBox1.Location = new System.Drawing.Point(7, 7);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(250, 168);
			this.groupBox1.TabIndex = 0;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Route";
			// 
			// RoadTab
			// 
			this.RoadTab.Location = new System.Drawing.Point(4, 22);
			this.RoadTab.Name = "RoadTab";
			this.RoadTab.Padding = new System.Windows.Forms.Padding(3);
			this.RoadTab.Size = new System.Drawing.Size(265, 484);
			this.RoadTab.TabIndex = 2;
			this.RoadTab.Text = "Road";
			this.RoadTab.UseVisualStyleBackColor = true;
			// 
			// IntersectionTab
			// 
			this.IntersectionTab.Location = new System.Drawing.Point(4, 22);
			this.IntersectionTab.Name = "IntersectionTab";
			this.IntersectionTab.Padding = new System.Windows.Forms.Padding(3);
			this.IntersectionTab.Size = new System.Drawing.Size(265, 484);
			this.IntersectionTab.TabIndex = 3;
			this.IntersectionTab.Text = "Inter";
			this.IntersectionTab.UseVisualStyleBackColor = true;
			// 
			// ZoneTab
			// 
			this.ZoneTab.Location = new System.Drawing.Point(4, 22);
			this.ZoneTab.Name = "ZoneTab";
			this.ZoneTab.Padding = new System.Windows.Forms.Padding(3);
			this.ZoneTab.Size = new System.Drawing.Size(265, 484);
			this.ZoneTab.TabIndex = 4;
			this.ZoneTab.Text = "Zone";
			this.ZoneTab.UseVisualStyleBackColor = true;
			// 
			// OptionsTab
			// 
			this.OptionsTab.AutoScroll = true;
			this.OptionsTab.Controls.Add(this.tableLayoutPanel1);
			this.OptionsTab.Location = new System.Drawing.Point(4, 22);
			this.OptionsTab.Name = "OptionsTab";
			this.OptionsTab.Padding = new System.Windows.Forms.Padding(3);
			this.OptionsTab.Size = new System.Drawing.Size(265, 484);
			this.OptionsTab.TabIndex = 5;
			this.OptionsTab.Text = "Options";
			this.OptionsTab.UseVisualStyleBackColor = true;
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.ColumnCount = 1;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Controls.Add(this.GeneralGroupBox, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.NavigationGroupBox, 0, 2);
			this.tableLayoutPanel1.Controls.Add(this.RoadGroupBox, 0, 3);
			this.tableLayoutPanel1.Controls.Add(this.IntersectionGroupBox, 0, 4);
			this.tableLayoutPanel1.Controls.Add(this.ZoneGroupBox, 0, 5);
			this.tableLayoutPanel1.Controls.Add(this.DisplayGroupBox, 0, 0);
			this.tableLayoutPanel1.Location = new System.Drawing.Point(7, 7);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 6;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 165F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 155F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 160F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 160F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 160F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 160F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(235, 1000);
			this.tableLayoutPanel1.TabIndex = 0;
			// 
			// GeneralGroupBox
			// 
			this.GeneralGroupBox.Controls.Add(this.DisplayDeletedVehiclesBoxOptions);
			this.GeneralGroupBox.Controls.Add(this.PoseLogDisplayOptionsTab);
			this.GeneralGroupBox.Controls.Add(this.OperationalLanePathDisplayCheckBoxOptionsTab);
			this.GeneralGroupBox.Controls.Add(this.ArbiterLanePathDisplayCheckBoxOptionsTab);
			this.GeneralGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.GeneralGroupBox.Location = new System.Drawing.Point(3, 168);
			this.GeneralGroupBox.Name = "GeneralGroupBox";
			this.GeneralGroupBox.Size = new System.Drawing.Size(229, 149);
			this.GeneralGroupBox.TabIndex = 0;
			this.GeneralGroupBox.TabStop = false;
			this.GeneralGroupBox.Text = "General";
			// 
			// PoseLogDisplayOptionsTab
			// 
			this.PoseLogDisplayOptionsTab.AutoSize = true;
			this.PoseLogDisplayOptionsTab.Checked = true;
			this.PoseLogDisplayOptionsTab.CheckState = System.Windows.Forms.CheckState.Checked;
			this.PoseLogDisplayOptionsTab.Location = new System.Drawing.Point(7, 68);
			this.PoseLogDisplayOptionsTab.Name = "PoseLogDisplayOptionsTab";
			this.PoseLogDisplayOptionsTab.Size = new System.Drawing.Size(108, 17);
			this.PoseLogDisplayOptionsTab.TabIndex = 2;
			this.PoseLogDisplayOptionsTab.Text = "Pose Log Display";
			this.PoseLogDisplayOptionsTab.UseVisualStyleBackColor = true;
			this.PoseLogDisplayOptionsTab.CheckedChanged += new System.EventHandler(this.PoseLogDisplayOptionsTab_CheckedChanged);
			// 
			// OperationalLanePathDisplayCheckBoxOptionsTab
			// 
			this.OperationalLanePathDisplayCheckBoxOptionsTab.AutoSize = true;
			this.OperationalLanePathDisplayCheckBoxOptionsTab.Location = new System.Drawing.Point(7, 44);
			this.OperationalLanePathDisplayCheckBoxOptionsTab.Name = "OperationalLanePathDisplayCheckBoxOptionsTab";
			this.OperationalLanePathDisplayCheckBoxOptionsTab.Size = new System.Drawing.Size(132, 17);
			this.OperationalLanePathDisplayCheckBoxOptionsTab.TabIndex = 1;
			this.OperationalLanePathDisplayCheckBoxOptionsTab.Text = "Operational Lane Path";
			this.OperationalLanePathDisplayCheckBoxOptionsTab.UseVisualStyleBackColor = true;
			this.OperationalLanePathDisplayCheckBoxOptionsTab.CheckedChanged += new System.EventHandler(this.OperationalLanePathDisplayCheckBoxOptionsTab_CheckedChanged);
			// 
			// ArbiterLanePathDisplayCheckBoxOptionsTab
			// 
			this.ArbiterLanePathDisplayCheckBoxOptionsTab.AutoSize = true;
			this.ArbiterLanePathDisplayCheckBoxOptionsTab.Location = new System.Drawing.Point(6, 20);
			this.ArbiterLanePathDisplayCheckBoxOptionsTab.Name = "ArbiterLanePathDisplayCheckBoxOptionsTab";
			this.ArbiterLanePathDisplayCheckBoxOptionsTab.Size = new System.Drawing.Size(108, 17);
			this.ArbiterLanePathDisplayCheckBoxOptionsTab.TabIndex = 0;
			this.ArbiterLanePathDisplayCheckBoxOptionsTab.Text = "Arbiter Lane Path";
			this.ArbiterLanePathDisplayCheckBoxOptionsTab.UseVisualStyleBackColor = true;
			this.ArbiterLanePathDisplayCheckBoxOptionsTab.CheckedChanged += new System.EventHandler(this.ArbiterLanePathDisplayCheckBoxOptionsTab_CheckedChanged);
			// 
			// NavigationGroupBox
			// 
			this.NavigationGroupBox.Controls.Add(this.FullRouteDisplayCheckBoxOptionsTab);
			this.NavigationGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.NavigationGroupBox.Location = new System.Drawing.Point(3, 323);
			this.NavigationGroupBox.Name = "NavigationGroupBox";
			this.NavigationGroupBox.Size = new System.Drawing.Size(229, 154);
			this.NavigationGroupBox.TabIndex = 1;
			this.NavigationGroupBox.TabStop = false;
			this.NavigationGroupBox.Text = "Navigation";
			// 
			// FullRouteDisplayCheckBoxOptionsTab
			// 
			this.FullRouteDisplayCheckBoxOptionsTab.AutoSize = true;
			this.FullRouteDisplayCheckBoxOptionsTab.Checked = true;
			this.FullRouteDisplayCheckBoxOptionsTab.CheckState = System.Windows.Forms.CheckState.Checked;
			this.FullRouteDisplayCheckBoxOptionsTab.Location = new System.Drawing.Point(7, 20);
			this.FullRouteDisplayCheckBoxOptionsTab.Name = "FullRouteDisplayCheckBoxOptionsTab";
			this.FullRouteDisplayCheckBoxOptionsTab.Size = new System.Drawing.Size(74, 17);
			this.FullRouteDisplayCheckBoxOptionsTab.TabIndex = 0;
			this.FullRouteDisplayCheckBoxOptionsTab.Text = "Full Route";
			this.FullRouteDisplayCheckBoxOptionsTab.UseVisualStyleBackColor = true;
			this.FullRouteDisplayCheckBoxOptionsTab.CheckedChanged += new System.EventHandler(this.FullRouteDisplayCheckBoxOptionsTab_CheckedChanged);
			// 
			// RoadGroupBox
			// 
			this.RoadGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.RoadGroupBox.Location = new System.Drawing.Point(3, 483);
			this.RoadGroupBox.Name = "RoadGroupBox";
			this.RoadGroupBox.Size = new System.Drawing.Size(229, 154);
			this.RoadGroupBox.TabIndex = 2;
			this.RoadGroupBox.TabStop = false;
			this.RoadGroupBox.Text = "Road Tactical";
			// 
			// IntersectionGroupBox
			// 
			this.IntersectionGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.IntersectionGroupBox.Location = new System.Drawing.Point(3, 643);
			this.IntersectionGroupBox.Name = "IntersectionGroupBox";
			this.IntersectionGroupBox.Size = new System.Drawing.Size(229, 154);
			this.IntersectionGroupBox.TabIndex = 3;
			this.IntersectionGroupBox.TabStop = false;
			this.IntersectionGroupBox.Text = "Intersection Tactical";
			// 
			// ZoneGroupBox
			// 
			this.ZoneGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ZoneGroupBox.Location = new System.Drawing.Point(3, 803);
			this.ZoneGroupBox.Name = "ZoneGroupBox";
			this.ZoneGroupBox.Size = new System.Drawing.Size(229, 194);
			this.ZoneGroupBox.TabIndex = 4;
			this.ZoneGroupBox.TabStop = false;
			this.ZoneGroupBox.Text = "Zone Tactical";
			// 
			// DisplayGroupBox
			// 
			this.DisplayGroupBox.Controls.Add(this.IntersectionBoundsCheckBoxOptionsTab);
			this.DisplayGroupBox.Controls.Add(this.DisplayInterconnectSplinesCheckBoxOptionsTab);
			this.DisplayGroupBox.Controls.Add(this.DisplayLaneSplinesCheckBoxOptionsTab);
			this.DisplayGroupBox.Controls.Add(this.DisplayGoalsCheckBoxOptionsTab);
			this.DisplayGroupBox.Controls.Add(this.DisplayUserWaypointIdCheckBox);
			this.DisplayGroupBox.Controls.Add(this.DisplayRndfWaypointIdCheckBox);
			this.DisplayGroupBox.Controls.Add(this.DisplayInterconnectsCheckBox);
			this.DisplayGroupBox.Controls.Add(this.DisplayUserWaypointsCheckBox);
			this.DisplayGroupBox.Controls.Add(this.DisplayRndfWaypointsCheckBox);
			this.DisplayGroupBox.Controls.Add(this.DisplayUserPartitionsCheckBox);
			this.DisplayGroupBox.Controls.Add(this.DisplayLanePartitionCheckBox);
			this.DisplayGroupBox.Controls.Add(this.DisplayRndfCheckBox);
			this.DisplayGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.DisplayGroupBox.Location = new System.Drawing.Point(3, 3);
			this.DisplayGroupBox.Name = "DisplayGroupBox";
			this.DisplayGroupBox.Size = new System.Drawing.Size(229, 159);
			this.DisplayGroupBox.TabIndex = 5;
			this.DisplayGroupBox.TabStop = false;
			this.DisplayGroupBox.Text = "Rndf";
			// 
			// IntersectionBoundsCheckBoxOptionsTab
			// 
			this.IntersectionBoundsCheckBoxOptionsTab.AutoSize = true;
			this.IntersectionBoundsCheckBoxOptionsTab.Checked = true;
			this.IntersectionBoundsCheckBoxOptionsTab.CheckState = System.Windows.Forms.CheckState.Checked;
			this.IntersectionBoundsCheckBoxOptionsTab.Location = new System.Drawing.Point(108, 134);
			this.IntersectionBoundsCheckBoxOptionsTab.Name = "IntersectionBoundsCheckBoxOptionsTab";
			this.IntersectionBoundsCheckBoxOptionsTab.Size = new System.Drawing.Size(120, 17);
			this.IntersectionBoundsCheckBoxOptionsTab.TabIndex = 11;
			this.IntersectionBoundsCheckBoxOptionsTab.Text = "Intersection Bounds";
			this.IntersectionBoundsCheckBoxOptionsTab.UseVisualStyleBackColor = true;
			this.IntersectionBoundsCheckBoxOptionsTab.CheckedChanged += new System.EventHandler(this.IntersectionBoundsCheckBoxOptionsTab_CheckedChanged);
			// 
			// DisplayInterconnectSplinesCheckBoxOptionsTab
			// 
			this.DisplayInterconnectSplinesCheckBoxOptionsTab.AutoSize = true;
			this.DisplayInterconnectSplinesCheckBoxOptionsTab.Location = new System.Drawing.Point(108, 112);
			this.DisplayInterconnectSplinesCheckBoxOptionsTab.Name = "DisplayInterconnectSplinesCheckBoxOptionsTab";
			this.DisplayInterconnectSplinesCheckBoxOptionsTab.Size = new System.Drawing.Size(123, 17);
			this.DisplayInterconnectSplinesCheckBoxOptionsTab.TabIndex = 10;
			this.DisplayInterconnectSplinesCheckBoxOptionsTab.Text = "Interconnect Splines";
			this.DisplayInterconnectSplinesCheckBoxOptionsTab.UseVisualStyleBackColor = true;
			this.DisplayInterconnectSplinesCheckBoxOptionsTab.CheckedChanged += new System.EventHandler(this.DisplayInterconnectSplinesCheckBoxOptionsTab_CheckedChanged);
			// 
			// DisplayLaneSplinesCheckBoxOptionsTab
			// 
			this.DisplayLaneSplinesCheckBoxOptionsTab.AutoSize = true;
			this.DisplayLaneSplinesCheckBoxOptionsTab.Checked = true;
			this.DisplayLaneSplinesCheckBoxOptionsTab.CheckState = System.Windows.Forms.CheckState.Checked;
			this.DisplayLaneSplinesCheckBoxOptionsTab.Location = new System.Drawing.Point(6, 66);
			this.DisplayLaneSplinesCheckBoxOptionsTab.Name = "DisplayLaneSplinesCheckBoxOptionsTab";
			this.DisplayLaneSplinesCheckBoxOptionsTab.Size = new System.Drawing.Size(87, 17);
			this.DisplayLaneSplinesCheckBoxOptionsTab.TabIndex = 9;
			this.DisplayLaneSplinesCheckBoxOptionsTab.Text = "Lane Splines";
			this.DisplayLaneSplinesCheckBoxOptionsTab.UseVisualStyleBackColor = true;
			this.DisplayLaneSplinesCheckBoxOptionsTab.CheckedChanged += new System.EventHandler(this.DisplayLaneSplinesCheckBoxOptionsTab_CheckedChanged);
			// 
			// DisplayGoalsCheckBoxOptionsTab
			// 
			this.DisplayGoalsCheckBoxOptionsTab.AutoSize = true;
			this.DisplayGoalsCheckBoxOptionsTab.Location = new System.Drawing.Point(108, 89);
			this.DisplayGoalsCheckBoxOptionsTab.Name = "DisplayGoalsCheckBoxOptionsTab";
			this.DisplayGoalsCheckBoxOptionsTab.Size = new System.Drawing.Size(53, 17);
			this.DisplayGoalsCheckBoxOptionsTab.TabIndex = 8;
			this.DisplayGoalsCheckBoxOptionsTab.Text = "Goals";
			this.DisplayGoalsCheckBoxOptionsTab.UseVisualStyleBackColor = true;
			this.DisplayGoalsCheckBoxOptionsTab.CheckedChanged += new System.EventHandler(this.DisplayGoalsCheckBoxOptionsTab_CheckedChanged);
			// 
			// DisplayUserWaypointIdCheckBox
			// 
			this.DisplayUserWaypointIdCheckBox.AutoSize = true;
			this.DisplayUserWaypointIdCheckBox.Location = new System.Drawing.Point(108, 43);
			this.DisplayUserWaypointIdCheckBox.Name = "DisplayUserWaypointIdCheckBox";
			this.DisplayUserWaypointIdCheckBox.Size = new System.Drawing.Size(115, 17);
			this.DisplayUserWaypointIdCheckBox.TabIndex = 7;
			this.DisplayUserWaypointIdCheckBox.Text = "User Waypoint Id\'s";
			this.DisplayUserWaypointIdCheckBox.UseVisualStyleBackColor = true;
			// 
			// DisplayRndfWaypointIdCheckBox
			// 
			this.DisplayRndfWaypointIdCheckBox.AutoSize = true;
			this.DisplayRndfWaypointIdCheckBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.2F);
			this.DisplayRndfWaypointIdCheckBox.Location = new System.Drawing.Point(6, 135);
			this.DisplayRndfWaypointIdCheckBox.Name = "DisplayRndfWaypointIdCheckBox";
			this.DisplayRndfWaypointIdCheckBox.Size = new System.Drawing.Size(102, 16);
			this.DisplayRndfWaypointIdCheckBox.TabIndex = 6;
			this.DisplayRndfWaypointIdCheckBox.Text = "Rndf Waypoint Id\'s";
			this.DisplayRndfWaypointIdCheckBox.UseVisualStyleBackColor = true;
			this.DisplayRndfWaypointIdCheckBox.CheckedChanged += new System.EventHandler(this.DisplayRndfWaypointIdCheckBox_CheckedChanged);
			// 
			// DisplayInterconnectsCheckBox
			// 
			this.DisplayInterconnectsCheckBox.AutoSize = true;
			this.DisplayInterconnectsCheckBox.Location = new System.Drawing.Point(108, 66);
			this.DisplayInterconnectsCheckBox.Name = "DisplayInterconnectsCheckBox";
			this.DisplayInterconnectsCheckBox.Size = new System.Drawing.Size(91, 17);
			this.DisplayInterconnectsCheckBox.TabIndex = 5;
			this.DisplayInterconnectsCheckBox.Text = "Interconnects";
			this.DisplayInterconnectsCheckBox.UseVisualStyleBackColor = true;
			this.DisplayInterconnectsCheckBox.CheckedChanged += new System.EventHandler(this.DisplayInterconnectsCheckBox_CheckedChanged);
			// 
			// DisplayUserWaypointsCheckBox
			// 
			this.DisplayUserWaypointsCheckBox.AutoSize = true;
			this.DisplayUserWaypointsCheckBox.Location = new System.Drawing.Point(108, 20);
			this.DisplayUserWaypointsCheckBox.Name = "DisplayUserWaypointsCheckBox";
			this.DisplayUserWaypointsCheckBox.Size = new System.Drawing.Size(101, 17);
			this.DisplayUserWaypointsCheckBox.TabIndex = 4;
			this.DisplayUserWaypointsCheckBox.Text = "User Waypoints";
			this.DisplayUserWaypointsCheckBox.UseVisualStyleBackColor = true;
			// 
			// DisplayRndfWaypointsCheckBox
			// 
			this.DisplayRndfWaypointsCheckBox.AutoSize = true;
			this.DisplayRndfWaypointsCheckBox.Checked = true;
			this.DisplayRndfWaypointsCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.DisplayRndfWaypointsCheckBox.Location = new System.Drawing.Point(6, 112);
			this.DisplayRndfWaypointsCheckBox.Name = "DisplayRndfWaypointsCheckBox";
			this.DisplayRndfWaypointsCheckBox.Size = new System.Drawing.Size(102, 17);
			this.DisplayRndfWaypointsCheckBox.TabIndex = 3;
			this.DisplayRndfWaypointsCheckBox.Text = "Rndf Waypoints";
			this.DisplayRndfWaypointsCheckBox.UseVisualStyleBackColor = true;
			this.DisplayRndfWaypointsCheckBox.CheckedChanged += new System.EventHandler(this.DisplayRndfWaypointsCheckBox_CheckedChanged);
			// 
			// DisplayUserPartitionsCheckBox
			// 
			this.DisplayUserPartitionsCheckBox.AutoSize = true;
			this.DisplayUserPartitionsCheckBox.Checked = true;
			this.DisplayUserPartitionsCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.DisplayUserPartitionsCheckBox.Location = new System.Drawing.Point(6, 89);
			this.DisplayUserPartitionsCheckBox.Name = "DisplayUserPartitionsCheckBox";
			this.DisplayUserPartitionsCheckBox.Size = new System.Drawing.Size(94, 17);
			this.DisplayUserPartitionsCheckBox.TabIndex = 2;
			this.DisplayUserPartitionsCheckBox.Text = "User Partitions";
			this.DisplayUserPartitionsCheckBox.UseVisualStyleBackColor = true;
			this.DisplayUserPartitionsCheckBox.CheckedChanged += new System.EventHandler(this.DisplayUserPartitionsCheckBox_CheckedChanged);
			// 
			// DisplayLanePartitionCheckBox
			// 
			this.DisplayLanePartitionCheckBox.AutoSize = true;
			this.DisplayLanePartitionCheckBox.Location = new System.Drawing.Point(6, 43);
			this.DisplayLanePartitionCheckBox.Name = "DisplayLanePartitionCheckBox";
			this.DisplayLanePartitionCheckBox.Size = new System.Drawing.Size(96, 17);
			this.DisplayLanePartitionCheckBox.TabIndex = 1;
			this.DisplayLanePartitionCheckBox.Text = "Lane Partitions";
			this.DisplayLanePartitionCheckBox.UseVisualStyleBackColor = true;
			this.DisplayLanePartitionCheckBox.CheckedChanged += new System.EventHandler(this.DisplayLanePartitionCheckBox_CheckedChanged);
			// 
			// DisplayRndfCheckBox
			// 
			this.DisplayRndfCheckBox.AutoSize = true;
			this.DisplayRndfCheckBox.Checked = true;
			this.DisplayRndfCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.DisplayRndfCheckBox.Location = new System.Drawing.Point(6, 20);
			this.DisplayRndfCheckBox.Name = "DisplayRndfCheckBox";
			this.DisplayRndfCheckBox.Size = new System.Drawing.Size(49, 17);
			this.DisplayRndfCheckBox.TabIndex = 0;
			this.DisplayRndfCheckBox.Text = "Rndf";
			this.DisplayRndfCheckBox.UseVisualStyleBackColor = true;
			this.DisplayRndfCheckBox.CheckedChanged += new System.EventHandler(this.DisplayRndfCheckBox_CheckedChanged);
			// 
			// openFileDialog1
			// 
			this.openFileDialog1.FileName = "openFileDialog1";
			// 
			// DisplayDeletedVehiclesBoxOptions
			// 
			this.DisplayDeletedVehiclesBoxOptions.AutoSize = true;
			this.DisplayDeletedVehiclesBoxOptions.Location = new System.Drawing.Point(7, 92);
			this.DisplayDeletedVehiclesBoxOptions.Name = "DisplayDeletedVehiclesBoxOptions";
			this.DisplayDeletedVehiclesBoxOptions.Size = new System.Drawing.Size(106, 17);
			this.DisplayDeletedVehiclesBoxOptions.TabIndex = 3;
			this.DisplayDeletedVehiclesBoxOptions.Text = "Deleted Vehicles";
			this.DisplayDeletedVehiclesBoxOptions.UseVisualStyleBackColor = true;
			this.DisplayDeletedVehiclesBoxOptions.CheckedChanged += new System.EventHandler(this.DisplayDeletedVehiclesBoxOptions_CheckedChanged);
			// 
			// RemoraDisplay
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(828, 678);
			this.Controls.Add(this.splitContainer1);
			this.Controls.Add(this.toolStrip1);
			this.Controls.Add(this.statusStrip1);
			this.Controls.Add(this.menuStrip1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "RemoraDisplay";
			this.Text = "Remora";
			this.Load += new System.EventHandler(this.RemoraDisplay_Load);
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel2.ResumeLayout(false);
			this.splitContainer1.ResumeLayout(false);
			this.splitContainer5.Panel1.ResumeLayout(false);
			this.splitContainer5.Panel2.ResumeLayout(false);
			this.splitContainer5.ResumeLayout(false);
			this.splitContainer4.Panel1.ResumeLayout(false);
			this.splitContainer4.Panel1.PerformLayout();
			this.splitContainer4.Panel2.ResumeLayout(false);
			this.splitContainer4.ResumeLayout(false);
			this.toolStrip2.ResumeLayout(false);
			this.toolStrip2.PerformLayout();
			this.splitContainer2.Panel1.ResumeLayout(false);
			this.splitContainer2.Panel2.ResumeLayout(false);
			this.splitContainer2.ResumeLayout(false);
			this.tabControl1.ResumeLayout(false);
			this.GeneralTab.ResumeLayout(false);
			this.OperationalGroupBox.ResumeLayout(false);
			this.OperationalGroupBox.PerformLayout();
			this.SceneEstimatorGroupBoxGeneralTab.ResumeLayout(false);
			this.SceneEstimatorGroupBoxGeneralTab.PerformLayout();
			this.ArbiterGroupBoxGeneralTab.ResumeLayout(false);
			this.ArbiterGroupBoxGeneralTab.PerformLayout();
			this.GeneralPosteriorPoseGroupBox.ResumeLayout(false);
			this.GeneralPosteriorPoseGroupBox.PerformLayout();
			this.NavigationTab.ResumeLayout(false);
			this.OptionsTab.ResumeLayout(false);
			this.tableLayoutPanel1.ResumeLayout(false);
			this.GeneralGroupBox.ResumeLayout(false);
			this.GeneralGroupBox.PerformLayout();
			this.NavigationGroupBox.ResumeLayout(false);
			this.NavigationGroupBox.PerformLayout();
			this.DisplayGroupBox.ResumeLayout(false);
			this.DisplayGroupBox.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.StatusStrip statusStrip1;
		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.Button Stop;
		private System.Windows.Forms.ToolStripButton ConnectArbiter;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripButton ZoomIn;
		private System.Windows.Forms.ToolStripButton ZoomStandard;
		private System.Windows.Forms.ToolStripButton ZoomOut;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripButton TrackVehicle;
		private System.Windows.Forms.ToolStripButton BeginDataStream;
		private System.Windows.Forms.ToolStripMenuItem loadRndfToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem loadMdfToolStripMenuItem;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.SplitContainer splitContainer5;
        private Remora.Display.RoadDisplay roadDisplay1;
		private System.Windows.Forms.SplitContainer splitContainer4;
		private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripButton InitializeCommunicationsButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        private System.Windows.Forms.ToolStripButton StopDataStreamButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem readmeToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
		private System.Windows.Forms.Button RestartArbiterButton;
		private System.Windows.Forms.ToolStrip toolStrip2;
		private System.Windows.Forms.ToolStripButton OutputToolStrip2Button;
		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage GeneralTab;
		private System.Windows.Forms.GroupBox SceneEstimatorGroupBoxGeneralTab;
		private System.Windows.Forms.Label StaticObstaclesTextGeneralTab;
		private System.Windows.Forms.Label DynamicObstaclesTextGeneralTab;
		private System.Windows.Forms.Label DynamicObstaclesLabelGeneralTab;
		private System.Windows.Forms.Label StaticObstaclesLabelGeneralTab;
		private System.Windows.Forms.GroupBox ArbiterGroupBoxGeneralTab;
		private System.Windows.Forms.Label GeneralTabRouteTimeText;
		private System.Windows.Forms.Label GeneralTabArbiterRouteDistanceText;
		private System.Windows.Forms.Label GeneralTabCurrentGoalText;
		private System.Windows.Forms.Label GeneralTabArbiterCarModeText;
		private System.Windows.Forms.Label ArbiterStateTextGeneralTab;
		private System.Windows.Forms.Label GeneralTabRouteTimeLabel;
		private System.Windows.Forms.Label RouteDistanceLabelGeneralTab;
		private System.Windows.Forms.Label GeneralTabCurrentGoalLabel;
		private System.Windows.Forms.Label ArbiterCarModeLabelGeneralTab;
		private System.Windows.Forms.Label ArbiterStateLabelGeneralTab;
		private System.Windows.Forms.GroupBox GeneralPosteriorPoseGroupBox;
		private System.Windows.Forms.Label GeneralTabApproximateAcceleration;
		private System.Windows.Forms.Label GeneralTabApproximateAccelerationTitle;
		private System.Windows.Forms.Label GeneralTabLaneEstimateConfidence;
		private System.Windows.Forms.Label GeneralTabLaneEstimateConfidenceTitle;
		private System.Windows.Forms.Label GeneralTabFinalLane;
		private System.Windows.Forms.Label GeneralTabFinalLaneTitle;
		private System.Windows.Forms.Label GeneralTabInitialLane;
		private System.Windows.Forms.Label GeneralTabInitialLaneTitle;
		private System.Windows.Forms.Label GeneralTabSpeed;
		private System.Windows.Forms.Label GeneralTabSpeedTitle;
		private System.Windows.Forms.Label GeneralTabHeading;
		private System.Windows.Forms.Label GeneralTabHeadingTitle;
		private System.Windows.Forms.Label GeneralTabPosition;
		private System.Windows.Forms.Label GeneralTabPositionTitle;
		private System.Windows.Forms.TabPage OptionsTab;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.GroupBox GeneralGroupBox;
		private System.Windows.Forms.GroupBox NavigationGroupBox;
		private System.Windows.Forms.GroupBox RoadGroupBox;
		private System.Windows.Forms.GroupBox IntersectionGroupBox;
		private System.Windows.Forms.GroupBox ZoneGroupBox;
		private System.Windows.Forms.GroupBox DisplayGroupBox;
		private System.Windows.Forms.CheckBox DisplayInterconnectSplinesCheckBoxOptionsTab;
		private System.Windows.Forms.CheckBox DisplayLaneSplinesCheckBoxOptionsTab;
		private System.Windows.Forms.CheckBox DisplayGoalsCheckBoxOptionsTab;
		private System.Windows.Forms.CheckBox DisplayUserWaypointIdCheckBox;
		private System.Windows.Forms.CheckBox DisplayRndfWaypointIdCheckBox;
		private System.Windows.Forms.CheckBox DisplayInterconnectsCheckBox;
		private System.Windows.Forms.CheckBox DisplayUserWaypointsCheckBox;
		private System.Windows.Forms.CheckBox DisplayRndfWaypointsCheckBox;
		private System.Windows.Forms.CheckBox DisplayUserPartitionsCheckBox;
		private System.Windows.Forms.CheckBox DisplayLanePartitionCheckBox;
		private System.Windows.Forms.CheckBox DisplayRndfCheckBox;
		private System.Windows.Forms.TabPage NavigationTab;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.TabPage RoadTab;
		private System.Windows.Forms.TabPage IntersectionTab;
		private System.Windows.Forms.TabPage ZoneTab;
		private System.Windows.Forms.CheckBox IntersectionBoundsCheckBoxOptionsTab;
		private System.Windows.Forms.ToolStripMenuItem GenerateGpsToolStripItem;
		private System.Windows.Forms.ToolStripMenuItem updateMdfToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem3;
		private System.Windows.Forms.ToolStripMenuItem retrieveRndfToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem retrieveMdfToolStripMenuItem;
		private System.Windows.Forms.ToolStripButton PingArbiterToolStripButton;
		private System.Windows.Forms.ToolStripButton SyncNetworkDataToolStripButton;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
		private System.Windows.Forms.GroupBox OperationalGroupBox;
		private System.Windows.Forms.Label CarModeLabelTextGeneralTab;
		private System.Windows.Forms.Label CarModeLabelGeneralTab;
		private System.Windows.Forms.Label GeneralTabGoalsLeftLabelText;
		private System.Windows.Forms.Label GoalsLeftLabelGeneralTab;
		private System.Windows.Forms.CheckBox OperationalLanePathDisplayCheckBoxOptionsTab;
		private System.Windows.Forms.CheckBox ArbiterLanePathDisplayCheckBoxOptionsTab;
		private System.Windows.Forms.CheckBox FullRouteDisplayCheckBoxOptionsTab;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
		private System.Windows.Forms.ToolStripButton LogPoseToolStripButton;
		private System.Windows.Forms.ToolStripButton RestartPoseLog;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator9;
		private System.Windows.Forms.CheckBox PoseLogDisplayOptionsTab;
		private System.Windows.Forms.ToolStripLabel PoseLogToolStriplabel;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator10;
		private System.Windows.Forms.ToolStripMenuItem LoadRecentToolStripMenuItem;
		private System.Windows.Forms.CheckBox DisplayDeletedVehiclesBoxOptions;
	}
}

