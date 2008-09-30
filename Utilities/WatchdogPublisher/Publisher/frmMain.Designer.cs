namespace Publisher
{
	partial class frmMain
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
			this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
			this.ctxPublishes = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.tsMenuTitle = new System.Windows.Forms.ToolStripMenuItem();
			this.skynet10ToolStripMenuItem = new System.Windows.Forms.ToolStripSeparator();
			this.executeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.stopToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.republishToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.startRemoteDebuggerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.launchVNCToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.startWatchdogToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.stopWatchdogToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this.refreshAvailablePublishesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.syncWithRemoteDefinitionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.tsEditPublish = new System.Windows.Forms.ToolStripMenuItem();
			this.deployNewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.newPublishToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.tmrGrid = new System.Windows.Forms.Timer(this.components);
			this.tmrInvalidate = new System.Windows.Forms.Timer(this.components);
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.refreshToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.reconnectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.importSettingsFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.changeRepositoryLocationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
			this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.hideToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.hideTrayIconToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.allComputerTasksToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.mountNetworkDriveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.deployToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.clearAllPublishesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.unmountNetworkDrivesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.allWatchdogsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.killAllWatchdogsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.jumpstartAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.setLocalSecurityPolicyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.allHealthMonsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.killToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.jumpstartToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.autoBackupPublishesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
			this.ctxNotify = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.tabPage1 = new System.Windows.Forms.TabPage();
			this.dgPublishes = new System.Windows.Forms.DataGridView();
			this.MachineName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.AvailablePublishes = new System.Windows.Forms.DataGridViewComboBoxColumn();
			this.Execute = new System.Windows.Forms.DataGridViewButtonColumn();
			this.Stop = new System.Windows.Forms.DataGridViewButtonColumn();
			this.RePublish = new System.Windows.Forms.DataGridViewButtonColumn();
			this.StatusDetail = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.Status = new System.Windows.Forms.DataGridViewImageColumn();
			this.tabPage2 = new System.Windows.Forms.TabPage();
			this.btnExecutePublishLocally = new System.Windows.Forms.Button();
			this.btnDeleteRemoteLocation = new System.Windows.Forms.Button();
			this.txtPublish = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.cmbRemoteLocations = new System.Windows.Forms.ComboBox();
			this.label1 = new System.Windows.Forms.Label();
			this.cmbPublishes = new System.Windows.Forms.ComboBox();
			this.btnCreateNewRemoteLocation = new System.Windows.Forms.Button();
			this.btnDeletePublish = new System.Windows.Forms.Button();
			this.btnCreateNewPublish = new System.Windows.Forms.Button();
			this.btnEditPublish = new System.Windows.Forms.Button();
			this.txtDebug = new System.Windows.Forms.TextBox();
			this.btnClearConsole = new System.Windows.Forms.Button();
			this.timerResyncMicro = new System.Windows.Forms.Timer(this.components);
			this.publishDefitionOnlyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ctxPublishes.SuspendLayout();
			this.menuStrip1.SuspendLayout();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			this.tabControl1.SuspendLayout();
			this.tabPage1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.dgPublishes)).BeginInit();
			this.tabPage2.SuspendLayout();
			this.SuspendLayout();
			// 
			// openFileDialog1
			// 
			this.openFileDialog1.FileName = "openFileDialog1";
			// 
			// ctxPublishes
			// 
			this.ctxPublishes.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsMenuTitle,
            this.skynet10ToolStripMenuItem,
            this.executeToolStripMenuItem,
            this.stopToolStripMenuItem,
            this.republishToolStripMenuItem,
            this.publishDefitionOnlyToolStripMenuItem,
            this.startRemoteDebuggerToolStripMenuItem,
            this.launchVNCToolStripMenuItem,
            this.toolStripSeparator4,
            this.startWatchdogToolStripMenuItem,
            this.stopWatchdogToolStripMenuItem,
            this.toolStripSeparator5,
            this.refreshAvailablePublishesToolStripMenuItem,
            this.syncWithRemoteDefinitionToolStripMenuItem,
            this.tsEditPublish,
            this.deployNewToolStripMenuItem});
			this.ctxPublishes.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.VerticalStackWithOverflow;
			this.ctxPublishes.Name = "ctxPublishes";
			this.ctxPublishes.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
			this.ctxPublishes.Size = new System.Drawing.Size(209, 330);
			this.ctxPublishes.Text = "Publisher";
			// 
			// tsMenuTitle
			// 
			this.tsMenuTitle.Name = "tsMenuTitle";
			this.tsMenuTitle.Size = new System.Drawing.Size(208, 22);
			this.tsMenuTitle.Text = "--Skynet 10--";
			// 
			// skynet10ToolStripMenuItem
			// 
			this.skynet10ToolStripMenuItem.Name = "skynet10ToolStripMenuItem";
			this.skynet10ToolStripMenuItem.Size = new System.Drawing.Size(205, 6);
			// 
			// executeToolStripMenuItem
			// 
			this.executeToolStripMenuItem.Name = "executeToolStripMenuItem";
			this.executeToolStripMenuItem.Size = new System.Drawing.Size(208, 22);
			this.executeToolStripMenuItem.Text = "Execute";
			this.executeToolStripMenuItem.Click += new System.EventHandler(this.executeToolStripMenuItem_Click);
			// 
			// stopToolStripMenuItem
			// 
			this.stopToolStripMenuItem.Name = "stopToolStripMenuItem";
			this.stopToolStripMenuItem.Size = new System.Drawing.Size(208, 22);
			this.stopToolStripMenuItem.Text = "Stop";
			this.stopToolStripMenuItem.Click += new System.EventHandler(this.stopToolStripMenuItem_Click);
			// 
			// republishToolStripMenuItem
			// 
			this.republishToolStripMenuItem.Name = "republishToolStripMenuItem";
			this.republishToolStripMenuItem.Size = new System.Drawing.Size(208, 22);
			this.republishToolStripMenuItem.Text = "Publish";
			this.republishToolStripMenuItem.Click += new System.EventHandler(this.republishToolStripMenuItem_Click);
			// 
			// startRemoteDebuggerToolStripMenuItem
			// 
			this.startRemoteDebuggerToolStripMenuItem.Name = "startRemoteDebuggerToolStripMenuItem";
			this.startRemoteDebuggerToolStripMenuItem.Size = new System.Drawing.Size(208, 22);
			this.startRemoteDebuggerToolStripMenuItem.Text = "Start Remote Debugger";
			this.startRemoteDebuggerToolStripMenuItem.Click += new System.EventHandler(this.startRemoteDebuggerToolStripMenuItem_Click);
			// 
			// launchVNCToolStripMenuItem
			// 
			this.launchVNCToolStripMenuItem.Name = "launchVNCToolStripMenuItem";
			this.launchVNCToolStripMenuItem.Size = new System.Drawing.Size(208, 22);
			this.launchVNCToolStripMenuItem.Text = "Launch VNC";
			this.launchVNCToolStripMenuItem.Click += new System.EventHandler(this.launchVNCToolStripMenuItem_Click);
			// 
			// toolStripSeparator4
			// 
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			this.toolStripSeparator4.Size = new System.Drawing.Size(205, 6);
			// 
			// startWatchdogToolStripMenuItem
			// 
			this.startWatchdogToolStripMenuItem.Name = "startWatchdogToolStripMenuItem";
			this.startWatchdogToolStripMenuItem.Size = new System.Drawing.Size(208, 22);
			this.startWatchdogToolStripMenuItem.Text = "Start Monitoring";
			this.startWatchdogToolStripMenuItem.Click += new System.EventHandler(this.startWatchdogToolStripMenuItem_Click);
			// 
			// stopWatchdogToolStripMenuItem
			// 
			this.stopWatchdogToolStripMenuItem.Name = "stopWatchdogToolStripMenuItem";
			this.stopWatchdogToolStripMenuItem.Size = new System.Drawing.Size(208, 22);
			this.stopWatchdogToolStripMenuItem.Text = "Stop Monitoring";
			this.stopWatchdogToolStripMenuItem.Click += new System.EventHandler(this.stopWatchdogToolStripMenuItem_Click);
			// 
			// toolStripSeparator5
			// 
			this.toolStripSeparator5.Name = "toolStripSeparator5";
			this.toolStripSeparator5.Size = new System.Drawing.Size(205, 6);
			// 
			// refreshAvailablePublishesToolStripMenuItem
			// 
			this.refreshAvailablePublishesToolStripMenuItem.Name = "refreshAvailablePublishesToolStripMenuItem";
			this.refreshAvailablePublishesToolStripMenuItem.Size = new System.Drawing.Size(208, 22);
			this.refreshAvailablePublishesToolStripMenuItem.Text = "Refresh Available Publishes";
			this.refreshAvailablePublishesToolStripMenuItem.Click += new System.EventHandler(this.refreshAvailablePublishesToolStripMenuItem_Click);
			// 
			// syncWithRemoteDefinitionToolStripMenuItem
			// 
			this.syncWithRemoteDefinitionToolStripMenuItem.Name = "syncWithRemoteDefinitionToolStripMenuItem";
			this.syncWithRemoteDefinitionToolStripMenuItem.Size = new System.Drawing.Size(208, 22);
			this.syncWithRemoteDefinitionToolStripMenuItem.Text = "Sync with Remote Definition";
			this.syncWithRemoteDefinitionToolStripMenuItem.Click += new System.EventHandler(this.syncWithRemoteDefinitionToolStripMenuItem_Click);
			// 
			// tsEditPublish
			// 
			this.tsEditPublish.Name = "tsEditPublish";
			this.tsEditPublish.Size = new System.Drawing.Size(208, 22);
			this.tsEditPublish.Text = "Edit Publish";
			this.tsEditPublish.Click += new System.EventHandler(this.tsEditPublish_Click_1);
			// 
			// deployNewToolStripMenuItem
			// 
			this.deployNewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newPublishToolStripMenuItem});
			this.deployNewToolStripMenuItem.Name = "deployNewToolStripMenuItem";
			this.deployNewToolStripMenuItem.Size = new System.Drawing.Size(208, 22);
			this.deployNewToolStripMenuItem.Text = "Deploy New Publish";
			// 
			// newPublishToolStripMenuItem
			// 
			this.newPublishToolStripMenuItem.Name = "newPublishToolStripMenuItem";
			this.newPublishToolStripMenuItem.Size = new System.Drawing.Size(143, 22);
			this.newPublishToolStripMenuItem.Text = "New Publish...";
			this.newPublishToolStripMenuItem.Click += new System.EventHandler(this.newPublishToolStripMenuItem_Click);
			// 
			// tmrGrid
			// 
			this.tmrGrid.Enabled = true;
			this.tmrGrid.Interval = 1000;
			this.tmrGrid.Tick += new System.EventHandler(this.tmrGrid_Tick);
			// 
			// tmrInvalidate
			// 
			this.tmrInvalidate.Enabled = true;
			this.tmrInvalidate.Interval = 1500;
			this.tmrInvalidate.Tick += new System.EventHandler(this.tmrInvalidate_Tick);
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.toolsToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(611, 24);
			this.menuStrip1.TabIndex = 14;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.refreshToolStripMenuItem,
            this.reconnectToolStripMenuItem,
            this.toolStripSeparator3,
            this.openToolStripMenuItem,
            this.importSettingsFileToolStripMenuItem,
            this.toolStripSeparator2,
            this.changeRepositoryLocationToolStripMenuItem,
            this.toolStripSeparator,
            this.saveToolStripMenuItem,
            this.toolStripSeparator1,
            this.hideToolStripMenuItem,
            this.hideTrayIconToolStripMenuItem,
            this.exitToolStripMenuItem});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(35, 20);
			this.fileToolStripMenuItem.Text = "&File";
			// 
			// refreshToolStripMenuItem
			// 
			this.refreshToolStripMenuItem.Name = "refreshToolStripMenuItem";
			this.refreshToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F5;
			this.refreshToolStripMenuItem.Size = new System.Drawing.Size(221, 22);
			this.refreshToolStripMenuItem.Text = "Refresh Local Publishes";
			this.refreshToolStripMenuItem.Click += new System.EventHandler(this.refreshToolStripMenuItem_Click);
			// 
			// reconnectToolStripMenuItem
			// 
			this.reconnectToolStripMenuItem.Name = "reconnectToolStripMenuItem";
			this.reconnectToolStripMenuItem.Size = new System.Drawing.Size(221, 22);
			this.reconnectToolStripMenuItem.Text = "Reconnect";
			this.reconnectToolStripMenuItem.Click += new System.EventHandler(this.reconnectToolStripMenuItem_Click);
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(218, 6);
			// 
			// openToolStripMenuItem
			// 
			this.openToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("openToolStripMenuItem.Image")));
			this.openToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.openToolStripMenuItem.Name = "openToolStripMenuItem";
			this.openToolStripMenuItem.Size = new System.Drawing.Size(221, 22);
			this.openToolStripMenuItem.Text = "Import Publishes";
			this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
			// 
			// importSettingsFileToolStripMenuItem
			// 
			this.importSettingsFileToolStripMenuItem.Name = "importSettingsFileToolStripMenuItem";
			this.importSettingsFileToolStripMenuItem.Size = new System.Drawing.Size(221, 22);
			this.importSettingsFileToolStripMenuItem.Text = "Import Settings File";
			this.importSettingsFileToolStripMenuItem.Click += new System.EventHandler(this.importSettingsFileToolStripMenuItem_Click);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(218, 6);
			// 
			// changeRepositoryLocationToolStripMenuItem
			// 
			this.changeRepositoryLocationToolStripMenuItem.Name = "changeRepositoryLocationToolStripMenuItem";
			this.changeRepositoryLocationToolStripMenuItem.Size = new System.Drawing.Size(221, 22);
			this.changeRepositoryLocationToolStripMenuItem.Text = "Change Repository Location...";
			this.changeRepositoryLocationToolStripMenuItem.Click += new System.EventHandler(this.changeRepositoryLocationToolStripMenuItem_Click);
			// 
			// toolStripSeparator
			// 
			this.toolStripSeparator.Name = "toolStripSeparator";
			this.toolStripSeparator.Size = new System.Drawing.Size(218, 6);
			// 
			// saveToolStripMenuItem
			// 
			this.saveToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("saveToolStripMenuItem.Image")));
			this.saveToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
			this.saveToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
			this.saveToolStripMenuItem.Size = new System.Drawing.Size(221, 22);
			this.saveToolStripMenuItem.Text = "&Save Configuration";
			this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(218, 6);
			// 
			// hideToolStripMenuItem
			// 
			this.hideToolStripMenuItem.Name = "hideToolStripMenuItem";
			this.hideToolStripMenuItem.Size = new System.Drawing.Size(221, 22);
			this.hideToolStripMenuItem.Text = "Hide Main Window";
			this.hideToolStripMenuItem.Click += new System.EventHandler(this.hideToolStripMenuItem_Click);
			// 
			// hideTrayIconToolStripMenuItem
			// 
			this.hideTrayIconToolStripMenuItem.CheckOnClick = true;
			this.hideTrayIconToolStripMenuItem.Name = "hideTrayIconToolStripMenuItem";
			this.hideTrayIconToolStripMenuItem.Size = new System.Drawing.Size(221, 22);
			this.hideTrayIconToolStripMenuItem.Text = "Hide Tray Icon";
			this.hideTrayIconToolStripMenuItem.Click += new System.EventHandler(this.hideTrayIconToolStripMenuItem_Click);
			// 
			// exitToolStripMenuItem
			// 
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(221, 22);
			this.exitToolStripMenuItem.Text = "E&xit";
			this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
			// 
			// toolsToolStripMenuItem
			// 
			this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.allComputerTasksToolStripMenuItem,
            this.allWatchdogsToolStripMenuItem,
            this.allHealthMonsToolStripMenuItem,
            this.autoBackupPublishesToolStripMenuItem});
			this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
			this.toolsToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
			this.toolsToolStripMenuItem.Text = "&Tools";
			// 
			// allComputerTasksToolStripMenuItem
			// 
			this.allComputerTasksToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mountNetworkDriveToolStripMenuItem,
            this.deployToolStripMenuItem,
            this.clearAllPublishesToolStripMenuItem,
            this.unmountNetworkDrivesToolStripMenuItem});
			this.allComputerTasksToolStripMenuItem.Name = "allComputerTasksToolStripMenuItem";
			this.allComputerTasksToolStripMenuItem.Size = new System.Drawing.Size(181, 22);
			this.allComputerTasksToolStripMenuItem.Text = "All Computer Tasks";
			// 
			// mountNetworkDriveToolStripMenuItem
			// 
			this.mountNetworkDriveToolStripMenuItem.Name = "mountNetworkDriveToolStripMenuItem";
			this.mountNetworkDriveToolStripMenuItem.Size = new System.Drawing.Size(193, 22);
			this.mountNetworkDriveToolStripMenuItem.Text = "Mount Network Drive";
			this.mountNetworkDriveToolStripMenuItem.Click += new System.EventHandler(this.mountNetworkDriveToolStripMenuItem_Click);
			// 
			// deployToolStripMenuItem
			// 
			this.deployToolStripMenuItem.Name = "deployToolStripMenuItem";
			this.deployToolStripMenuItem.Size = new System.Drawing.Size(193, 22);
			this.deployToolStripMenuItem.Text = "Deploy...";
			this.deployToolStripMenuItem.Click += new System.EventHandler(this.deployToolStripMenuItem_Click);
			// 
			// clearAllPublishesToolStripMenuItem
			// 
			this.clearAllPublishesToolStripMenuItem.Name = "clearAllPublishesToolStripMenuItem";
			this.clearAllPublishesToolStripMenuItem.Size = new System.Drawing.Size(193, 22);
			this.clearAllPublishesToolStripMenuItem.Text = "Clear All Publishes";
			this.clearAllPublishesToolStripMenuItem.Click += new System.EventHandler(this.clearAllPublishesToolStripMenuItem_Click);
			// 
			// unmountNetworkDrivesToolStripMenuItem
			// 
			this.unmountNetworkDrivesToolStripMenuItem.Name = "unmountNetworkDrivesToolStripMenuItem";
			this.unmountNetworkDrivesToolStripMenuItem.Size = new System.Drawing.Size(193, 22);
			this.unmountNetworkDrivesToolStripMenuItem.Text = "Unmount Network Drives";
			this.unmountNetworkDrivesToolStripMenuItem.Click += new System.EventHandler(this.unmountNetworkDrivesToolStripMenuItem_Click);
			// 
			// allWatchdogsToolStripMenuItem
			// 
			this.allWatchdogsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.killAllWatchdogsToolStripMenuItem,
            this.jumpstartAllToolStripMenuItem,
            this.setLocalSecurityPolicyToolStripMenuItem});
			this.allWatchdogsToolStripMenuItem.Name = "allWatchdogsToolStripMenuItem";
			this.allWatchdogsToolStripMenuItem.Size = new System.Drawing.Size(181, 22);
			this.allWatchdogsToolStripMenuItem.Text = "All Watchdogs";
			// 
			// killAllWatchdogsToolStripMenuItem
			// 
			this.killAllWatchdogsToolStripMenuItem.Name = "killAllWatchdogsToolStripMenuItem";
			this.killAllWatchdogsToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
			this.killAllWatchdogsToolStripMenuItem.Text = "Kill All";
			this.killAllWatchdogsToolStripMenuItem.Click += new System.EventHandler(this.killAllWatchdogsToolStripMenuItem_Click);
			// 
			// jumpstartAllToolStripMenuItem
			// 
			this.jumpstartAllToolStripMenuItem.Name = "jumpstartAllToolStripMenuItem";
			this.jumpstartAllToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
			this.jumpstartAllToolStripMenuItem.Text = "Jumpstart All";
			this.jumpstartAllToolStripMenuItem.Click += new System.EventHandler(this.jumpstartAllToolStripMenuItem_Click);
			// 
			// setLocalSecurityPolicyToolStripMenuItem
			// 
			this.setLocalSecurityPolicyToolStripMenuItem.Name = "setLocalSecurityPolicyToolStripMenuItem";
			this.setLocalSecurityPolicyToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
			this.setLocalSecurityPolicyToolStripMenuItem.Text = "Set Local Security Policy";
			this.setLocalSecurityPolicyToolStripMenuItem.Click += new System.EventHandler(this.setLocalSecurityPolicyToolStripMenuItem_Click);
			// 
			// allHealthMonsToolStripMenuItem
			// 
			this.allHealthMonsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.killToolStripMenuItem,
            this.jumpstartToolStripMenuItem});
			this.allHealthMonsToolStripMenuItem.Name = "allHealthMonsToolStripMenuItem";
			this.allHealthMonsToolStripMenuItem.Size = new System.Drawing.Size(181, 22);
			this.allHealthMonsToolStripMenuItem.Text = "All HealthMons";
			// 
			// killToolStripMenuItem
			// 
			this.killToolStripMenuItem.Name = "killToolStripMenuItem";
			this.killToolStripMenuItem.Size = new System.Drawing.Size(136, 22);
			this.killToolStripMenuItem.Text = "Kill All";
			this.killToolStripMenuItem.Click += new System.EventHandler(this.killHealthMonitorToolStripMenuItem_Click);
			// 
			// jumpstartToolStripMenuItem
			// 
			this.jumpstartToolStripMenuItem.Name = "jumpstartToolStripMenuItem";
			this.jumpstartToolStripMenuItem.Size = new System.Drawing.Size(136, 22);
			this.jumpstartToolStripMenuItem.Text = "Jumpstart All";
			this.jumpstartToolStripMenuItem.Click += new System.EventHandler(this.jumpstartHealthMonitorToolStripMenuItem_Click);
			// 
			// autoBackupPublishesToolStripMenuItem
			// 
			this.autoBackupPublishesToolStripMenuItem.Checked = global::Publisher.Properties.Settings.Default.autoBackupPublishes;
			this.autoBackupPublishesToolStripMenuItem.CheckOnClick = true;
			this.autoBackupPublishesToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
			this.autoBackupPublishesToolStripMenuItem.Name = "autoBackupPublishesToolStripMenuItem";
			this.autoBackupPublishesToolStripMenuItem.Size = new System.Drawing.Size(181, 22);
			this.autoBackupPublishesToolStripMenuItem.Text = "Auto Backup Publishes";
			this.autoBackupPublishesToolStripMenuItem.Click += new System.EventHandler(this.autoBackupPublishesToolStripMenuItem_Click);
			// 
			// notifyIcon1
			// 
			this.notifyIcon1.BalloonTipText = "Publisher";
			this.notifyIcon1.ContextMenuStrip = this.ctxNotify;
			this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
			this.notifyIcon1.Text = "Publisher - Ready";
			this.notifyIcon1.Visible = true;
			this.notifyIcon1.DoubleClick += new System.EventHandler(this.notifyIcon1_DoubleClick);
			this.notifyIcon1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon1_MouseDoubleClick);
			// 
			// ctxNotify
			// 
			this.ctxNotify.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.VerticalStackWithOverflow;
			this.ctxNotify.Name = "ctxPublishes";
			this.ctxNotify.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
			this.ctxNotify.Size = new System.Drawing.Size(61, 4);
			this.ctxNotify.Text = "Publisher";
			this.ctxNotify.Opening += new System.ComponentModel.CancelEventHandler(this.ctxNotify_Opening);
			// 
			// splitContainer1
			// 
			this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer1.Location = new System.Drawing.Point(0, 24);
			this.splitContainer1.Name = "splitContainer1";
			this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.tabControl1);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.txtDebug);
			this.splitContainer1.Panel2.Controls.Add(this.btnClearConsole);
			this.splitContainer1.Size = new System.Drawing.Size(611, 356);
			this.splitContainer1.SplitterDistance = 296;
			this.splitContainer1.TabIndex = 13;
			// 
			// tabControl1
			// 
			this.tabControl1.Controls.Add(this.tabPage1);
			this.tabControl1.Controls.Add(this.tabPage2);
			this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabControl1.Location = new System.Drawing.Point(0, 0);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size(611, 296);
			this.tabControl1.TabIndex = 12;
			// 
			// tabPage1
			// 
			this.tabPage1.Controls.Add(this.dgPublishes);
			this.tabPage1.Location = new System.Drawing.Point(4, 22);
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage1.Size = new System.Drawing.Size(603, 270);
			this.tabPage1.TabIndex = 0;
			this.tabPage1.Text = "Existing Publishes";
			this.tabPage1.UseVisualStyleBackColor = true;
			// 
			// dgPublishes
			// 
			this.dgPublishes.AllowUserToAddRows = false;
			this.dgPublishes.AllowUserToDeleteRows = false;
			dataGridViewCellStyle1.BackColor = System.Drawing.Color.WhiteSmoke;
			dataGridViewCellStyle1.ForeColor = System.Drawing.Color.Black;
			this.dgPublishes.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
			this.dgPublishes.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.dgPublishes.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders;
			this.dgPublishes.BackgroundColor = System.Drawing.SystemColors.ControlLight;
			this.dgPublishes.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
			this.dgPublishes.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.MachineName,
            this.AvailablePublishes,
            this.Execute,
            this.Stop,
            this.RePublish,
            this.StatusDetail,
            this.Status});
			dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle4.Padding = new System.Windows.Forms.Padding(2);
			dataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.dgPublishes.DefaultCellStyle = dataGridViewCellStyle4;
			this.dgPublishes.Dock = System.Windows.Forms.DockStyle.Fill;
			this.dgPublishes.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
			this.dgPublishes.GridColor = System.Drawing.SystemColors.ActiveBorder;
			this.dgPublishes.Location = new System.Drawing.Point(3, 3);
			this.dgPublishes.MultiSelect = false;
			this.dgPublishes.Name = "dgPublishes";
			this.dgPublishes.RowHeadersVisible = false;
			this.dgPublishes.RowTemplate.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
			this.dgPublishes.RowTemplate.DefaultCellStyle.ForeColor = System.Drawing.Color.Black;
			this.dgPublishes.RowTemplate.DefaultCellStyle.NullValue = null;
			this.dgPublishes.RowTemplate.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.WhiteSmoke;
			this.dgPublishes.RowTemplate.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Black;
			this.dgPublishes.RowTemplate.DefaultCellStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.dgPublishes.RowTemplate.Height = 32;
			this.dgPublishes.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.dgPublishes.ShowCellErrors = false;
			this.dgPublishes.ShowEditingIcon = false;
			this.dgPublishes.ShowRowErrors = false;
			this.dgPublishes.Size = new System.Drawing.Size(597, 264);
			this.dgPublishes.TabIndex = 10;
			this.dgPublishes.CellMouseDown += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dgPublishes_CellMouseDown);
			this.dgPublishes.ColumnHeaderMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dgPublishes_ColumnHeaderMouseClick);
			this.dgPublishes.CellMouseDoubleClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dgPublishes_CellMouseDoubleClick);
			this.dgPublishes.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.dgPublishes_DataError);
			this.dgPublishes.CellMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dgPublishes_CellMouseClick);
			// 
			// MachineName
			// 
			dataGridViewCellStyle2.Format = "N0";
			dataGridViewCellStyle2.NullValue = null;
			this.MachineName.DefaultCellStyle = dataGridViewCellStyle2;
			this.MachineName.FillWeight = 10F;
			this.MachineName.HeaderText = "Machine Name";
			this.MachineName.MinimumWidth = 100;
			this.MachineName.Name = "MachineName";
			this.MachineName.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
			// 
			// AvailablePublishes
			// 
			dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			this.AvailablePublishes.DefaultCellStyle = dataGridViewCellStyle3;
			this.AvailablePublishes.FillWeight = 30F;
			this.AvailablePublishes.HeaderText = "Existing Publishes";
			this.AvailablePublishes.MinimumWidth = 80;
			this.AvailablePublishes.Name = "AvailablePublishes";
			// 
			// Execute
			// 
			this.Execute.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.Execute.FillWeight = 1F;
			this.Execute.HeaderText = "";
			this.Execute.MinimumWidth = 70;
			this.Execute.Name = "Execute";
			this.Execute.Text = "Execute";
			this.Execute.Width = 70;
			// 
			// Stop
			// 
			this.Stop.FillWeight = 1F;
			this.Stop.HeaderText = "";
			this.Stop.MinimumWidth = 70;
			this.Stop.Name = "Stop";
			// 
			// RePublish
			// 
			this.RePublish.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.RePublish.FillWeight = 1F;
			this.RePublish.HeaderText = "";
			this.RePublish.MinimumWidth = 70;
			this.RePublish.Name = "RePublish";
			this.RePublish.Text = "RePublish";
			this.RePublish.Width = 70;
			// 
			// StatusDetail
			// 
			this.StatusDetail.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.StatusDetail.FillWeight = 30F;
			this.StatusDetail.HeaderText = "Status Detail";
			this.StatusDetail.Name = "StatusDetail";
			this.StatusDetail.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			// 
			// Status
			// 
			this.Status.FillWeight = 1F;
			this.Status.HeaderText = "";
			this.Status.MinimumWidth = 20;
			this.Status.Name = "Status";
			// 
			// tabPage2
			// 
			this.tabPage2.Controls.Add(this.btnExecutePublishLocally);
			this.tabPage2.Controls.Add(this.btnDeleteRemoteLocation);
			this.tabPage2.Controls.Add(this.txtPublish);
			this.tabPage2.Controls.Add(this.label2);
			this.tabPage2.Controls.Add(this.cmbRemoteLocations);
			this.tabPage2.Controls.Add(this.label1);
			this.tabPage2.Controls.Add(this.cmbPublishes);
			this.tabPage2.Controls.Add(this.btnCreateNewRemoteLocation);
			this.tabPage2.Controls.Add(this.btnDeletePublish);
			this.tabPage2.Controls.Add(this.btnCreateNewPublish);
			this.tabPage2.Controls.Add(this.btnEditPublish);
			this.tabPage2.Location = new System.Drawing.Point(4, 22);
			this.tabPage2.Name = "tabPage2";
			this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage2.Size = new System.Drawing.Size(603, 270);
			this.tabPage2.TabIndex = 1;
			this.tabPage2.Text = "Manage Publishes/Remote Locations";
			this.tabPage2.UseVisualStyleBackColor = true;
			// 
			// btnExecutePublishLocally
			// 
			this.btnExecutePublishLocally.Image = global::Publisher.Properties.Resources.Gear;
			this.btnExecutePublishLocally.Location = new System.Drawing.Point(152, 79);
			this.btnExecutePublishLocally.Name = "btnExecutePublishLocally";
			this.btnExecutePublishLocally.Size = new System.Drawing.Size(113, 23);
			this.btnExecutePublishLocally.TabIndex = 19;
			this.btnExecutePublishLocally.Text = "Execute Local";
			this.btnExecutePublishLocally.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this.btnExecutePublishLocally.UseVisualStyleBackColor = true;
			this.btnExecutePublishLocally.Click += new System.EventHandler(this.btnExecutePublishLocally_Click);
			// 
			// btnDeleteRemoteLocation
			// 
			this.btnDeleteRemoteLocation.Image = global::Publisher.Properties.Resources.Delete;
			this.btnDeleteRemoteLocation.Location = new System.Drawing.Point(7, 210);
			this.btnDeleteRemoteLocation.Name = "btnDeleteRemoteLocation";
			this.btnDeleteRemoteLocation.Size = new System.Drawing.Size(92, 22);
			this.btnDeleteRemoteLocation.TabIndex = 18;
			this.btnDeleteRemoteLocation.Text = "Delete";
			this.btnDeleteRemoteLocation.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this.btnDeleteRemoteLocation.UseVisualStyleBackColor = true;
			this.btnDeleteRemoteLocation.Click += new System.EventHandler(this.btnDeleteRemoteLocation_Click);
			// 
			// txtPublish
			// 
			this.txtPublish.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
									| System.Windows.Forms.AnchorStyles.Left)
									| System.Windows.Forms.AnchorStyles.Right)));
			this.txtPublish.Location = new System.Drawing.Point(292, 17);
			this.txtPublish.Multiline = true;
			this.txtPublish.Name = "txtPublish";
			this.txtPublish.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.txtPublish.Size = new System.Drawing.Size(295, 246);
			this.txtPublish.TabIndex = 16;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(8, 167);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(96, 13);
			this.label2.TabIndex = 14;
			this.label2.Text = "Remote Locations:";
			// 
			// cmbRemoteLocations
			// 
			this.cmbRemoteLocations.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbRemoteLocations.FormattingEnabled = true;
			this.cmbRemoteLocations.Location = new System.Drawing.Point(8, 183);
			this.cmbRemoteLocations.Name = "cmbRemoteLocations";
			this.cmbRemoteLocations.Size = new System.Drawing.Size(256, 21);
			this.cmbRemoteLocations.TabIndex = 13;
			this.cmbRemoteLocations.SelectedIndexChanged += new System.EventHandler(this.cmbRemoteLocations_SelectedIndexChanged);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(6, 12);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(55, 13);
			this.label1.TabIndex = 2;
			this.label1.Text = "Publishes:";
			// 
			// cmbPublishes
			// 
			this.cmbPublishes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbPublishes.FormattingEnabled = true;
			this.cmbPublishes.Location = new System.Drawing.Point(9, 28);
			this.cmbPublishes.Name = "cmbPublishes";
			this.cmbPublishes.Size = new System.Drawing.Size(256, 21);
			this.cmbPublishes.TabIndex = 0;
			this.cmbPublishes.SelectedIndexChanged += new System.EventHandler(this.cmbPublishes_SelectedIndexChanged);
			// 
			// btnCreateNewRemoteLocation
			// 
			this.btnCreateNewRemoteLocation.Image = global::Publisher.Properties.Resources.RecordNew;
			this.btnCreateNewRemoteLocation.Location = new System.Drawing.Point(151, 210);
			this.btnCreateNewRemoteLocation.Name = "btnCreateNewRemoteLocation";
			this.btnCreateNewRemoteLocation.Size = new System.Drawing.Size(113, 22);
			this.btnCreateNewRemoteLocation.TabIndex = 17;
			this.btnCreateNewRemoteLocation.Text = "Create New";
			this.btnCreateNewRemoteLocation.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this.btnCreateNewRemoteLocation.UseVisualStyleBackColor = true;
			this.btnCreateNewRemoteLocation.Click += new System.EventHandler(this.btnCreateNewRemoteLocation_Click);
			// 
			// btnDeletePublish
			// 
			this.btnDeletePublish.Image = global::Publisher.Properties.Resources.Delete;
			this.btnDeletePublish.Location = new System.Drawing.Point(6, 79);
			this.btnDeletePublish.Name = "btnDeletePublish";
			this.btnDeletePublish.Size = new System.Drawing.Size(94, 22);
			this.btnDeletePublish.TabIndex = 15;
			this.btnDeletePublish.Text = "Delete";
			this.btnDeletePublish.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this.btnDeletePublish.UseVisualStyleBackColor = true;
			this.btnDeletePublish.Click += new System.EventHandler(this.btnDeletePublish_Click);
			// 
			// btnCreateNewPublish
			// 
			this.btnCreateNewPublish.Image = global::Publisher.Properties.Resources.RecordNew;
			this.btnCreateNewPublish.Location = new System.Drawing.Point(152, 53);
			this.btnCreateNewPublish.Name = "btnCreateNewPublish";
			this.btnCreateNewPublish.Size = new System.Drawing.Size(113, 22);
			this.btnCreateNewPublish.TabIndex = 11;
			this.btnCreateNewPublish.Text = "Create New";
			this.btnCreateNewPublish.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this.btnCreateNewPublish.UseVisualStyleBackColor = true;
			this.btnCreateNewPublish.Click += new System.EventHandler(this.btnCreateNewPublish_Click);
			// 
			// btnEditPublish
			// 
			this.btnEditPublish.Image = global::Publisher.Properties.Resources.Pencil;
			this.btnEditPublish.Location = new System.Drawing.Point(8, 53);
			this.btnEditPublish.Name = "btnEditPublish";
			this.btnEditPublish.Size = new System.Drawing.Size(92, 22);
			this.btnEditPublish.TabIndex = 4;
			this.btnEditPublish.Text = "Edit...";
			this.btnEditPublish.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this.btnEditPublish.UseVisualStyleBackColor = true;
			this.btnEditPublish.Click += new System.EventHandler(this.btnEditPublish_Click);
			// 
			// txtDebug
			// 
			this.txtDebug.Dock = System.Windows.Forms.DockStyle.Fill;
			this.txtDebug.Location = new System.Drawing.Point(0, 0);
			this.txtDebug.Multiline = true;
			this.txtDebug.Name = "txtDebug";
			this.txtDebug.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.txtDebug.Size = new System.Drawing.Size(590, 56);
			this.txtDebug.TabIndex = 19;
			// 
			// btnClearConsole
			// 
			this.btnClearConsole.Dock = System.Windows.Forms.DockStyle.Right;
			this.btnClearConsole.Location = new System.Drawing.Point(590, 0);
			this.btnClearConsole.Name = "btnClearConsole";
			this.btnClearConsole.Size = new System.Drawing.Size(21, 56);
			this.btnClearConsole.TabIndex = 20;
			this.btnClearConsole.Text = "CLR";
			this.btnClearConsole.UseVisualStyleBackColor = true;
			this.btnClearConsole.Click += new System.EventHandler(this.btnClearConsole_Click);
			// 
			// timerResyncMicro
			// 
			this.timerResyncMicro.Interval = 2000;
			// 
			// publishDefitionOnlyToolStripMenuItem
			// 
			this.publishDefitionOnlyToolStripMenuItem.Name = "publishDefitionOnlyToolStripMenuItem";
			this.publishDefitionOnlyToolStripMenuItem.Size = new System.Drawing.Size(208, 22);
			this.publishDefitionOnlyToolStripMenuItem.Text = "Publish Defition Only";
			this.publishDefitionOnlyToolStripMenuItem.Click += new System.EventHandler(this.publishDefitionOnlyToolStripMenuItem_Click);
			// 
			// frmMain
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(611, 380);
			this.Controls.Add(this.splitContainer1);
			this.Controls.Add(this.menuStrip1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "frmMain";
			this.Text = "Publisher Glutrinox .99c";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMain_FormClosing);
			this.Load += new System.EventHandler(this.frmMain_Load);
			this.ctxPublishes.ResumeLayout(false);
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel2.ResumeLayout(false);
			this.splitContainer1.Panel2.PerformLayout();
			this.splitContainer1.ResumeLayout(false);
			this.tabControl1.ResumeLayout(false);
			this.tabPage1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.dgPublishes)).EndInit();
			this.tabPage2.ResumeLayout(false);
			this.tabPage2.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.OpenFileDialog openFileDialog1;
		private System.Windows.Forms.Timer tmrGrid;
		private System.Windows.Forms.SplitContainer splitContainer1;
		private System.Windows.Forms.TextBox txtDebug;
		private System.Windows.Forms.Timer tmrInvalidate;
		private System.Windows.Forms.ContextMenuStrip ctxPublishes;
		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator;
		private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem allComputerTasksToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem mountNetworkDriveToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem deployToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem clearAllPublishesToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem unmountNetworkDrivesToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem allWatchdogsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem killAllWatchdogsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem jumpstartAllToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem setLocalSecurityPolicyToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem changeRepositoryLocationToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem refreshToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem stopToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem republishToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem startWatchdogToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem executeToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem stopWatchdogToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem allHealthMonsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem killToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem jumpstartToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem tsMenuTitle;
		private System.Windows.Forms.ToolStripSeparator skynet10ToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem deployNewToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem newPublishToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem tsEditPublish;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripMenuItem importSettingsFileToolStripMenuItem;
		private System.Windows.Forms.Button btnClearConsole;
		private System.Windows.Forms.ToolStripMenuItem startRemoteDebuggerToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem refreshAvailablePublishesToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
		private System.Windows.Forms.ToolStripMenuItem launchVNCToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem reconnectToolStripMenuItem;
		private System.Windows.Forms.NotifyIcon notifyIcon1;
		private System.Windows.Forms.Timer timerResyncMicro;
		private System.Windows.Forms.ContextMenuStrip ctxNotify;
		private System.Windows.Forms.ToolStripMenuItem hideToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem hideTrayIconToolStripMenuItem;
		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage tabPage1;
		private System.Windows.Forms.DataGridView dgPublishes;
		private System.Windows.Forms.DataGridViewTextBoxColumn MachineName;
		private System.Windows.Forms.DataGridViewComboBoxColumn AvailablePublishes;
		private System.Windows.Forms.DataGridViewButtonColumn Execute;
		private System.Windows.Forms.DataGridViewButtonColumn Stop;
		private System.Windows.Forms.DataGridViewButtonColumn RePublish;
		private System.Windows.Forms.DataGridViewTextBoxColumn StatusDetail;
		private System.Windows.Forms.DataGridViewImageColumn Status;
		private System.Windows.Forms.TabPage tabPage2;
		private System.Windows.Forms.Button btnExecutePublishLocally;
		private System.Windows.Forms.Button btnDeleteRemoteLocation;
		private System.Windows.Forms.TextBox txtPublish;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.ComboBox cmbRemoteLocations;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ComboBox cmbPublishes;
		private System.Windows.Forms.Button btnCreateNewRemoteLocation;
		private System.Windows.Forms.Button btnDeletePublish;
		private System.Windows.Forms.Button btnCreateNewPublish;
		private System.Windows.Forms.Button btnEditPublish;
		private System.Windows.Forms.ToolStripMenuItem syncWithRemoteDefinitionToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem autoBackupPublishesToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem publishDefitionOnlyToolStripMenuItem;
	}
}

