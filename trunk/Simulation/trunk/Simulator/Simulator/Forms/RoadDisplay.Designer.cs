namespace Simulator
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
			this.vehicleContextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.bindToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.unbindToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
			this.trackToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.stopTrackToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.connectAiToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
			this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.obstacleContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.deleteObstacleContextToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
			this.vehicleContextMenuStrip1.SuspendLayout();
			this.obstacleContextMenuStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// vehicleContextMenuStrip1
			// 
			this.vehicleContextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.bindToolStripMenuItem,
            this.unbindToolStripMenuItem,
            this.toolStripMenuItem1,
            this.trackToolStripMenuItem,
            this.stopTrackToolStripMenuItem,
            this.toolStripSeparator1,
            this.connectAiToolStripMenuItem,
            this.toolStripMenuItem2,
            this.deleteToolStripMenuItem});
			this.vehicleContextMenuStrip1.Name = "contextMenuStrip1";
			this.vehicleContextMenuStrip1.Size = new System.Drawing.Size(127, 154);
			this.vehicleContextMenuStrip1.Opening += new System.ComponentModel.CancelEventHandler(this.vehicleContextMenuStrip1_Opening);
			// 
			// bindToolStripMenuItem
			// 
			this.bindToolStripMenuItem.Image = global::Simulator.Properties.Resources.Customize;
			this.bindToolStripMenuItem.Name = "bindToolStripMenuItem";
			this.bindToolStripMenuItem.Size = new System.Drawing.Size(126, 22);
			this.bindToolStripMenuItem.Text = "&Bind";
			this.bindToolStripMenuItem.Click += new System.EventHandler(this.bindToolStripMenuItem_Click);
			// 
			// unbindToolStripMenuItem
			// 
			this.unbindToolStripMenuItem.Image = global::Simulator.Properties.Resources.Cut;
			this.unbindToolStripMenuItem.Name = "unbindToolStripMenuItem";
			this.unbindToolStripMenuItem.Size = new System.Drawing.Size(126, 22);
			this.unbindToolStripMenuItem.Text = "&Unbind";
			this.unbindToolStripMenuItem.Click += new System.EventHandler(this.unbindToolStripMenuItem_Click);
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size(123, 6);
			// 
			// trackToolStripMenuItem
			// 
			this.trackToolStripMenuItem.Image = global::Simulator.Properties.Resources.TableSearch;
			this.trackToolStripMenuItem.Name = "trackToolStripMenuItem";
			this.trackToolStripMenuItem.Size = new System.Drawing.Size(126, 22);
			this.trackToolStripMenuItem.Text = "&Track";
			this.trackToolStripMenuItem.Click += new System.EventHandler(this.trackToolStripMenuItem_Click);
			// 
			// stopTrackToolStripMenuItem
			// 
			this.stopTrackToolStripMenuItem.Image = global::Simulator.Properties.Resources.Table;
			this.stopTrackToolStripMenuItem.Name = "stopTrackToolStripMenuItem";
			this.stopTrackToolStripMenuItem.Size = new System.Drawing.Size(126, 22);
			this.stopTrackToolStripMenuItem.Text = "&Stop Track";
			this.stopTrackToolStripMenuItem.Click += new System.EventHandler(this.stopTrackToolStripMenuItem_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(123, 6);
			// 
			// connectAiToolStripMenuItem
			// 
			this.connectAiToolStripMenuItem.Image = global::Simulator.Properties.Resources.Plus1;
			this.connectAiToolStripMenuItem.Name = "connectAiToolStripMenuItem";
			this.connectAiToolStripMenuItem.Size = new System.Drawing.Size(126, 22);
			this.connectAiToolStripMenuItem.Text = "&Connect Ai";
			this.connectAiToolStripMenuItem.Click += new System.EventHandler(this.connectAiToolStripMenuItem_Click);
			// 
			// toolStripMenuItem2
			// 
			this.toolStripMenuItem2.Name = "toolStripMenuItem2";
			this.toolStripMenuItem2.Size = new System.Drawing.Size(123, 6);
			// 
			// deleteToolStripMenuItem
			// 
			this.deleteToolStripMenuItem.Image = global::Simulator.Properties.Resources.Delete;
			this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
			this.deleteToolStripMenuItem.Size = new System.Drawing.Size(126, 22);
			this.deleteToolStripMenuItem.Text = "&Delete";
			this.deleteToolStripMenuItem.Click += new System.EventHandler(this.deleteToolStripMenuItem_Click);
			// 
			// obstacleContextMenuStrip
			// 
			this.obstacleContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.deleteObstacleContextToolStripMenuItem1});
			this.obstacleContextMenuStrip.Name = "obstacleContextMenuStrip";
			this.obstacleContextMenuStrip.Size = new System.Drawing.Size(153, 48);
			// 
			// deleteObstacleContextToolStripMenuItem1
			// 
			this.deleteObstacleContextToolStripMenuItem1.Image = global::Simulator.Properties.Resources.Delete;
			this.deleteObstacleContextToolStripMenuItem1.Name = "deleteObstacleContextToolStripMenuItem1";
			this.deleteObstacleContextToolStripMenuItem1.Size = new System.Drawing.Size(152, 22);
			this.deleteObstacleContextToolStripMenuItem1.Text = "&Delete";
			this.deleteObstacleContextToolStripMenuItem1.Click += new System.EventHandler(this.deleteObstacleContextToolStripMenuItem1_Click);
			// 
			// RoadDisplay
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.White;
			this.Name = "RoadDisplay";
			this.vehicleContextMenuStrip1.ResumeLayout(false);
			this.obstacleContextMenuStrip.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ContextMenuStrip vehicleContextMenuStrip1;
		private System.Windows.Forms.ToolStripMenuItem bindToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem unbindToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem trackToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem stopTrackToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
		private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem connectAiToolStripMenuItem;
		private System.Windows.Forms.ContextMenuStrip obstacleContextMenuStrip;
		private System.Windows.Forms.ToolStripMenuItem deleteObstacleContextToolStripMenuItem1;
	}
}
