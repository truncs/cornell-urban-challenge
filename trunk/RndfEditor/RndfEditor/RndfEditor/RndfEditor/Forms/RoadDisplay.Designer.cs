namespace RndfEditor.Forms
{
	partial class RoadDisplay
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.intersectionContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.removeIntersectionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.printToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.zoneContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.redoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.undoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
			this.removeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.safetyZoneContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.safetyZoneRemoveToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
			this.partitionContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.normalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.sparsePartitionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.startupPartitionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
			this.insertUserWaypointToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.userWaypointContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.intersectionContextMenuStrip.SuspendLayout();
			this.zoneContextMenuStrip.SuspendLayout();
			this.safetyZoneContextMenu.SuspendLayout();
			this.partitionContextMenuStrip.SuspendLayout();
			this.userWaypointContextMenuStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// intersectionContextMenuStrip
			// 
			this.intersectionContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.removeIntersectionToolStripMenuItem,
            this.printToolStripMenuItem});
			this.intersectionContextMenuStrip.Name = "intersectionContextMenuStrip";
			this.intersectionContextMenuStrip.Size = new System.Drawing.Size(149, 48);
			this.intersectionContextMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.intersectionContextMenuStrip_Opening);
			// 
			// removeIntersectionToolStripMenuItem
			// 
			this.removeIntersectionToolStripMenuItem.Image = global::RndfEditor.Properties.Resources.Undo1;
			this.removeIntersectionToolStripMenuItem.Name = "removeIntersectionToolStripMenuItem";
			this.removeIntersectionToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
			this.removeIntersectionToolStripMenuItem.Text = "&Remove";
			this.removeIntersectionToolStripMenuItem.Click += new System.EventHandler(this.removeIntersectionToolStripMenuItem_Click);
			// 
			// printToolStripMenuItem
			// 
			this.printToolStripMenuItem.Image = global::RndfEditor.Properties.Resources.Printer;
			this.printToolStripMenuItem.Name = "printToolStripMenuItem";
			this.printToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
			this.printToolStripMenuItem.Text = "&Print Priorities";
			this.printToolStripMenuItem.Click += new System.EventHandler(this.printToolStripMenuItem_Click);
			// 
			// zoneContextMenuStrip
			// 
			this.zoneContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.redoToolStripMenuItem,
            this.undoToolStripMenuItem,
            this.toolStripMenuItem1,
            this.removeToolStripMenuItem});
			this.zoneContextMenuStrip.Name = "zoneContextMenuStrip";
			this.zoneContextMenuStrip.Size = new System.Drawing.Size(118, 76);
			// 
			// redoToolStripMenuItem
			// 
			this.redoToolStripMenuItem.Image = global::RndfEditor.Properties.Resources.Redo_16_h_p;
			this.redoToolStripMenuItem.Name = "redoToolStripMenuItem";
			this.redoToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
			this.redoToolStripMenuItem.Text = "R&edo";
			this.redoToolStripMenuItem.Click += new System.EventHandler(this.redoToolStripMenuItem_Click);
			// 
			// undoToolStripMenuItem
			// 
			this.undoToolStripMenuItem.Image = global::RndfEditor.Properties.Resources.Undo_16_h_p;
			this.undoToolStripMenuItem.Name = "undoToolStripMenuItem";
			this.undoToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
			this.undoToolStripMenuItem.Text = "&Undo";
			this.undoToolStripMenuItem.Click += new System.EventHandler(this.undoToolStripMenuItem_Click);
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size(114, 6);
			// 
			// removeToolStripMenuItem
			// 
			this.removeToolStripMenuItem.Image = global::RndfEditor.Properties.Resources.Delete;
			this.removeToolStripMenuItem.Name = "removeToolStripMenuItem";
			this.removeToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
			this.removeToolStripMenuItem.Text = "&Remove";
			this.removeToolStripMenuItem.Click += new System.EventHandler(this.removeToolStripMenuItem_Click);
			// 
			// safetyZoneContextMenu
			// 
			this.safetyZoneContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.safetyZoneRemoveToolStripMenuItem1});
			this.safetyZoneContextMenu.Name = "safetyZoneContextMenu";
			this.safetyZoneContextMenu.Size = new System.Drawing.Size(118, 26);
			// 
			// safetyZoneRemoveToolStripMenuItem1
			// 
			this.safetyZoneRemoveToolStripMenuItem1.Image = global::RndfEditor.Properties.Resources.Delete;
			this.safetyZoneRemoveToolStripMenuItem1.Name = "safetyZoneRemoveToolStripMenuItem1";
			this.safetyZoneRemoveToolStripMenuItem1.Size = new System.Drawing.Size(117, 22);
			this.safetyZoneRemoveToolStripMenuItem1.Text = "Remove";
			this.safetyZoneRemoveToolStripMenuItem1.Click += new System.EventHandler(this.safetyZoneRemoveToolStripMenuItem1_Click);
			// 
			// partitionContextMenuStrip
			// 
			this.partitionContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.normalToolStripMenuItem,
            this.sparsePartitionToolStripMenuItem,
            this.startupPartitionToolStripMenuItem,
            this.toolStripMenuItem2,
            this.insertUserWaypointToolStripMenuItem});
			this.partitionContextMenuStrip.Name = "partitionContextMenuStrip";
			this.partitionContextMenuStrip.Size = new System.Drawing.Size(184, 98);
			this.partitionContextMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.partitionContextMenuStrip_Opening);
			// 
			// normalToolStripMenuItem
			// 
			this.normalToolStripMenuItem.Name = "normalToolStripMenuItem";
			this.normalToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
			this.normalToolStripMenuItem.Text = "Normal";
			this.normalToolStripMenuItem.Click += new System.EventHandler(this.normalToolStripMenuItem_Click);
			// 
			// sparsePartitionToolStripMenuItem
			// 
			this.sparsePartitionToolStripMenuItem.Name = "sparsePartitionToolStripMenuItem";
			this.sparsePartitionToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
			this.sparsePartitionToolStripMenuItem.Text = "Sparse";
			this.sparsePartitionToolStripMenuItem.Click += new System.EventHandler(this.sparsePartitionToolStripMenuItem_Click);
			// 
			// startupPartitionToolStripMenuItem
			// 
			this.startupPartitionToolStripMenuItem.Name = "startupPartitionToolStripMenuItem";
			this.startupPartitionToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
			this.startupPartitionToolStripMenuItem.Text = "Startup";
			this.startupPartitionToolStripMenuItem.Click += new System.EventHandler(this.startupPartitionToolStripMenuItem_Click);
			// 
			// toolStripMenuItem2
			// 
			this.toolStripMenuItem2.Name = "toolStripMenuItem2";
			this.toolStripMenuItem2.Size = new System.Drawing.Size(180, 6);
			// 
			// insertUserWaypointToolStripMenuItem
			// 
			this.insertUserWaypointToolStripMenuItem.Image = global::RndfEditor.Properties.Resources.Linkbreak;
			this.insertUserWaypointToolStripMenuItem.Name = "insertUserWaypointToolStripMenuItem";
			this.insertUserWaypointToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
			this.insertUserWaypointToolStripMenuItem.Text = "Insert User Waypoint";
			this.insertUserWaypointToolStripMenuItem.Click += new System.EventHandler(this.insertUserWaypointToolStripMenuItem_Click);
			// 
			// userWaypointContextMenuStrip
			// 
			this.userWaypointContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.deleteToolStripMenuItem});
			this.userWaypointContextMenuStrip.Name = "userWaypointContextMenuStrip";
			this.userWaypointContextMenuStrip.Size = new System.Drawing.Size(153, 48);
			// 
			// deleteToolStripMenuItem
			// 
			this.deleteToolStripMenuItem.Image = global::RndfEditor.Properties.Resources.Delete;
			this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
			this.deleteToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.deleteToolStripMenuItem.Text = "&Delete";
			this.deleteToolStripMenuItem.Click += new System.EventHandler(this.deleteToolStripMenuItem_Click);
			// 
			// RoadDisplay
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.White;
			this.Name = "RoadDisplay";
			this.intersectionContextMenuStrip.ResumeLayout(false);
			this.zoneContextMenuStrip.ResumeLayout(false);
			this.safetyZoneContextMenu.ResumeLayout(false);
			this.partitionContextMenuStrip.ResumeLayout(false);
			this.userWaypointContextMenuStrip.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ContextMenuStrip intersectionContextMenuStrip;
		private System.Windows.Forms.ToolStripMenuItem removeIntersectionToolStripMenuItem;
		private System.Windows.Forms.ContextMenuStrip zoneContextMenuStrip;
		private System.Windows.Forms.ToolStripMenuItem redoToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem undoToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
		public System.Windows.Forms.ToolStripMenuItem removeToolStripMenuItem;
		private System.Windows.Forms.ContextMenuStrip safetyZoneContextMenu;
		private System.Windows.Forms.ToolStripMenuItem safetyZoneRemoveToolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem printToolStripMenuItem;
		private System.Windows.Forms.ContextMenuStrip partitionContextMenuStrip;
		private System.Windows.Forms.ToolStripMenuItem sparsePartitionToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem startupPartitionToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem normalToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
		private System.Windows.Forms.ToolStripMenuItem insertUserWaypointToolStripMenuItem;
		private System.Windows.Forms.ContextMenuStrip userWaypointContextMenuStrip;
		private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
	}
}
