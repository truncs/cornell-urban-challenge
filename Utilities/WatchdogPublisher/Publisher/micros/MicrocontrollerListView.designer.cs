namespace CarBrowser.Micros {
	partial class MicrocontrollerListView {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MicrocontrollerListView));
			System.Windows.Forms.ListViewGroup listViewGroup1 = new System.Windows.Forms.ListViewGroup("Microcontrollers", System.Windows.Forms.HorizontalAlignment.Left);
			System.Windows.Forms.ListViewGroup listViewGroup2 = new System.Windows.Forms.ListViewGroup("Power Only", System.Windows.Forms.HorizontalAlignment.Left);
			this.columnName = new System.Windows.Forms.ColumnHeader();
			this.columnAddress = new System.Windows.Forms.ColumnHeader();
			this.columnSyncPulse = new System.Windows.Forms.ColumnHeader();
			this.columnPowerStatus = new System.Windows.Forms.ColumnHeader();
			this.columnPingTime = new System.Windows.Forms.ColumnHeader();
			this.contextMenuMicros = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.menuRefreshSync = new System.Windows.Forms.ToolStripMenuItem();
			this.menuRefreshPulse = new System.Windows.Forms.ToolStripMenuItem();
			this.menuResync = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.menuPing = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.menuReset = new System.Windows.Forms.ToolStripMenuItem();
			this.menuEnable = new System.Windows.Forms.ToolStripMenuItem();
			this.menuDisable = new System.Windows.Forms.ToolStripMenuItem();
			this.timerPing = new System.Windows.Forms.Timer(this.components);
			this.contextMenuMicros.SuspendLayout();
			this.SuspendLayout();
			// 
			// columnName
			// 
			this.columnName.Text = "Name";
			this.columnName.Width = 150;
			// 
			// columnAddress
			// 
			this.columnAddress.Text = "Address";
			this.columnAddress.Width = 150;
			// 
			// columnSyncPulse
			// 
			this.columnSyncPulse.Text = "Sync/Pulse";
			this.columnSyncPulse.Width = 150;
			// 
			// columnPowerStatus
			// 
			this.columnPowerStatus.Text = "Power Status";
			this.columnPowerStatus.Width = 150;
			// 
			// columnPingTime
			// 
			this.columnPingTime.Text = "Ping";
			this.columnPingTime.Width = 120;
			// 
			// contextMenuMicros
			// 
			this.contextMenuMicros.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuRefreshSync,
            this.menuRefreshPulse,
            this.menuResync,
            this.toolStripSeparator2,
            this.menuPing,
            this.toolStripSeparator1,
            this.menuReset,
            this.menuEnable,
            this.menuDisable});
			this.contextMenuMicros.Name = "contextMenuMicros";
			this.contextMenuMicros.Size = new System.Drawing.Size(173, 192);
			this.contextMenuMicros.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuMicros_Opening);
			// 
			// menuRefreshSync
			// 
			this.menuRefreshSync.Name = "menuRefreshSync";
			this.menuRefreshSync.Size = new System.Drawing.Size(172, 22);
			this.menuRefreshSync.Text = "Refresh Sync";
			this.menuRefreshSync.Click += new System.EventHandler(this.menuRefreshSync_Click);
			// 
			// menuRefreshPulse
			// 
			this.menuRefreshPulse.Name = "menuRefreshPulse";
			this.menuRefreshPulse.Size = new System.Drawing.Size(172, 22);
			this.menuRefreshPulse.Text = "Refresh Pulse";
			this.menuRefreshPulse.Click += new System.EventHandler(this.menuRefreshPulse_Click);
			// 
			// menuResync
			// 
			this.menuResync.Name = "menuResync";
			this.menuResync.Size = new System.Drawing.Size(172, 22);
			this.menuResync.Text = "Resync";
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(169, 6);
			// 
			// menuPing
			// 
			this.menuPing.Name = "menuPing";
			this.menuPing.Size = new System.Drawing.Size(172, 22);
			this.menuPing.Text = "Ping";
			this.menuPing.Click += new System.EventHandler(this.menuPing_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(169, 6);
			// 
			// menuReset
			// 
			this.menuReset.Image = ((System.Drawing.Image)(resources.GetObject("menuReset.Image")));
			this.menuReset.Name = "menuReset";
			this.menuReset.Size = new System.Drawing.Size(172, 22);
			this.menuReset.Text = "Reset";
			this.menuReset.Click += new System.EventHandler(this.menuReset_Click);
			// 
			// menuEnable
			// 
			this.menuEnable.Image = ((System.Drawing.Image)(resources.GetObject("menuEnable.Image")));
			this.menuEnable.Name = "menuEnable";
			this.menuEnable.Size = new System.Drawing.Size(172, 22);
			this.menuEnable.Text = "Enable Power";
			this.menuEnable.Click += new System.EventHandler(this.menuEnable_Click);
			// 
			// menuDisable
			// 
			this.menuDisable.Image = ((System.Drawing.Image)(resources.GetObject("menuDisable.Image")));
			this.menuDisable.Name = "menuDisable";
			this.menuDisable.Size = new System.Drawing.Size(172, 22);
			this.menuDisable.Text = "Disable Power";
			this.menuDisable.Click += new System.EventHandler(this.menuDisable_Click);
			// 
			// timerPing
			// 
			this.timerPing.Interval = 2000;
			this.timerPing.Tick += new System.EventHandler(this.timerPing_Tick);
			// 
			// MicrocontrollerListView
			// 
			this.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnName,
            this.columnAddress,
            this.columnSyncPulse,
            this.columnPowerStatus,
            this.columnPingTime});
			this.ContextMenuStrip = this.contextMenuMicros;
			this.FullRowSelect = true;
			listViewGroup1.Header = "Microcontrollers";
			listViewGroup1.Name = "groupMicros";
			listViewGroup2.Header = "Power Only";
			listViewGroup2.Name = "groupPower";
			this.Groups.AddRange(new System.Windows.Forms.ListViewGroup[] {
            listViewGroup1,
            listViewGroup2});
			this.contextMenuMicros.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ColumnHeader columnName;
		private System.Windows.Forms.ColumnHeader columnAddress;
		private System.Windows.Forms.ColumnHeader columnSyncPulse;
		private System.Windows.Forms.ColumnHeader columnPowerStatus;
		private System.Windows.Forms.ColumnHeader columnPingTime;
		private System.Windows.Forms.ContextMenuStrip contextMenuMicros;
		private System.Windows.Forms.ToolStripMenuItem menuRefreshSync;
		private System.Windows.Forms.ToolStripMenuItem menuRefreshPulse;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem menuPing;
		private System.Windows.Forms.ToolStripMenuItem menuReset;
		private System.Windows.Forms.ToolStripMenuItem menuEnable;
		private System.Windows.Forms.ToolStripMenuItem menuDisable;
		private System.Windows.Forms.ToolStripMenuItem menuResync;
		private System.Windows.Forms.Timer timerPing;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
	}
}
