namespace CarBrowser.Channels {
	partial class ChannelListView {
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
			System.Windows.Forms.ListViewGroup listViewGroup1 = new System.Windows.Forms.ListViewGroup("Dynamic Channels", System.Windows.Forms.HorizontalAlignment.Left);
			System.Windows.Forms.ListViewGroup listViewGroup2 = new System.Windows.Forms.ListViewGroup("Static Channels", System.Windows.Forms.HorizontalAlignment.Left);
			this.columnName = new System.Windows.Forms.ColumnHeader();
			this.columnMulticast = new System.Windows.Forms.ColumnHeader();
			this.columnPacketRate = new System.Windows.Forms.ColumnHeader();
			this.columnSenders = new System.Windows.Forms.ColumnHeader();
			this.timerUpdateTimer = new System.Windows.Forms.Timer(this.components);
			this.contextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.menuStartListening = new System.Windows.Forms.ToolStripMenuItem();
			this.menuStopListening = new System.Windows.Forms.ToolStripMenuItem();
			this.menuResetStats = new System.Windows.Forms.ToolStripMenuItem();
			this.contextMenu.SuspendLayout();
			this.SuspendLayout();
			// 
			// columnName
			// 
			this.columnName.Text = "Name";
			this.columnName.Width = 350;
			// 
			// columnMulticast
			// 
			this.columnMulticast.Text = "Multicast Address";
			this.columnMulticast.Width = 150;
			// 
			// columnPacketRate
			// 
			this.columnPacketRate.Text = "Packet Rate";
			this.columnPacketRate.Width = 120;
			// 
			// columnSenders
			// 
			this.columnSenders.Text = "Senders";
			this.columnSenders.Width = 250;
			// 
			// timerUpdateTimer
			// 
			this.timerUpdateTimer.Enabled = true;
			this.timerUpdateTimer.Interval = 1000;
			this.timerUpdateTimer.Tick += new System.EventHandler(this.timerUpdateTimer_Tick);
			// 
			// contextMenu
			// 
			this.contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuStartListening,
            this.menuStopListening,
            this.menuResetStats});
			this.contextMenu.Name = "contextMenu";
			this.contextMenu.Size = new System.Drawing.Size(155, 70);
			this.contextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenu_Opening);
			// 
			// menuStartListening
			// 
			this.menuStartListening.Name = "menuStartListening";
			this.menuStartListening.Size = new System.Drawing.Size(154, 22);
			this.menuStartListening.Text = "Start Listening";
			this.menuStartListening.Click += new System.EventHandler(this.menuStartListening_Click);
			// 
			// menuStopListening
			// 
			this.menuStopListening.Name = "menuStopListening";
			this.menuStopListening.Size = new System.Drawing.Size(154, 22);
			this.menuStopListening.Text = "Stop Listening";
			this.menuStopListening.Click += new System.EventHandler(this.menuStopListening_Click);
			// 
			// menuResetStats
			// 
			this.menuResetStats.Name = "menuResetStats";
			this.menuResetStats.Size = new System.Drawing.Size(154, 22);
			this.menuResetStats.Text = "Reset Stats";
			this.menuResetStats.Click += new System.EventHandler(this.menuResetStats_Click);
			// 
			// ChannelListView
			// 
			this.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnName,
            this.columnMulticast,
            this.columnPacketRate,
            this.columnSenders});
			this.ContextMenuStrip = this.contextMenu;
			this.FullRowSelect = true;
			listViewGroup1.Header = "Dynamic Channels";
			listViewGroup1.Name = "dynamic";
			listViewGroup2.Header = "Static Channels";
			listViewGroup2.Name = "static";
			this.Groups.AddRange(new System.Windows.Forms.ListViewGroup[] {
            listViewGroup1,
            listViewGroup2});
			this.View = System.Windows.Forms.View.Details;
			this.contextMenu.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ColumnHeader columnName;
		private System.Windows.Forms.ColumnHeader columnMulticast;
		private System.Windows.Forms.ColumnHeader columnPacketRate;
		private System.Windows.Forms.ColumnHeader columnSenders;
		private System.Windows.Forms.Timer timerUpdateTimer;
		private System.Windows.Forms.ContextMenuStrip contextMenu;
		private System.Windows.Forms.ToolStripMenuItem menuStartListening;
		private System.Windows.Forms.ToolStripMenuItem menuStopListening;
		private System.Windows.Forms.ToolStripMenuItem menuResetStats;
	}
}
