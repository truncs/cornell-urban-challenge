namespace RndfEditor.Forms
{
	partial class ZoneToolbox
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ZoneToolbox));
			this.toolStrip1 = new System.Windows.Forms.ToolStrip();
			this.ZoneToolboxSelectZoneButton = new System.Windows.Forms.ToolStripButton();
			this.ZoneToolboxStayOutPolygonButton = new System.Windows.Forms.ToolStripButton();
			this.ZoneToolboxCreateNodesButton = new System.Windows.Forms.ToolStripButton();
			this.ZoneToolboxResetZoneButton = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
			this.ZoneToolboxSelectedZoneTextBox = new System.Windows.Forms.ToolStripTextBox();
			this.toolStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// toolStrip1
			// 
			this.toolStrip1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ZoneToolboxSelectZoneButton,
            this.ZoneToolboxStayOutPolygonButton,
            this.ZoneToolboxCreateNodesButton,
            this.ZoneToolboxResetZoneButton,
            this.toolStripSeparator1,
            this.toolStripLabel1,
            this.ZoneToolboxSelectedZoneTextBox});
			this.toolStrip1.Location = new System.Drawing.Point(0, 0);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.Size = new System.Drawing.Size(604, 40);
			this.toolStrip1.TabIndex = 0;
			this.toolStrip1.Text = "toolStrip1";
			// 
			// ZoneToolboxSelectZoneButton
			// 
			this.ZoneToolboxSelectZoneButton.CheckOnClick = true;
			this.ZoneToolboxSelectZoneButton.Image = global::RndfEditor.Properties.Resources.ColorPicker1;
			this.ZoneToolboxSelectZoneButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.ZoneToolboxSelectZoneButton.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.ZoneToolboxSelectZoneButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.ZoneToolboxSelectZoneButton.Name = "ZoneToolboxSelectZoneButton";
			this.ZoneToolboxSelectZoneButton.Size = new System.Drawing.Size(99, 37);
			this.ZoneToolboxSelectZoneButton.Text = "Select Zone";
			this.ZoneToolboxSelectZoneButton.Click += new System.EventHandler(this.ZoneToolboxSelectZoneButton_Click);
			// 
			// ZoneToolboxStayOutPolygonButton
			// 
			this.ZoneToolboxStayOutPolygonButton.CheckOnClick = true;
			this.ZoneToolboxStayOutPolygonButton.Image = global::RndfEditor.Properties.Resources.Box1;
			this.ZoneToolboxStayOutPolygonButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.ZoneToolboxStayOutPolygonButton.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.ZoneToolboxStayOutPolygonButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.ZoneToolboxStayOutPolygonButton.Name = "ZoneToolboxStayOutPolygonButton";
			this.ZoneToolboxStayOutPolygonButton.Size = new System.Drawing.Size(127, 37);
			this.ZoneToolboxStayOutPolygonButton.Text = "Stay Out Polygon";
			this.ZoneToolboxStayOutPolygonButton.Click += new System.EventHandler(this.ZoneToolboxStayOutPolygonButton_Click);
			// 
			// ZoneToolboxCreateNodesButton
			// 
			this.ZoneToolboxCreateNodesButton.CheckOnClick = true;
			this.ZoneToolboxCreateNodesButton.Image = global::RndfEditor.Properties.Resources.Pen2;
			this.ZoneToolboxCreateNodesButton.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.ZoneToolboxCreateNodesButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.ZoneToolboxCreateNodesButton.Name = "ZoneToolboxCreateNodesButton";
			this.ZoneToolboxCreateNodesButton.Size = new System.Drawing.Size(131, 37);
			this.ZoneToolboxCreateNodesButton.Text = "Create Nav Nodes";
			this.ZoneToolboxCreateNodesButton.Click += new System.EventHandler(this.ZoneToolboxCreateNodesButton_Click);
			// 
			// ZoneToolboxResetZoneButton
			// 
			this.ZoneToolboxResetZoneButton.Image = global::RndfEditor.Properties.Resources.Delete;
			this.ZoneToolboxResetZoneButton.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.ZoneToolboxResetZoneButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.ZoneToolboxResetZoneButton.Name = "ZoneToolboxResetZoneButton";
			this.ZoneToolboxResetZoneButton.Size = new System.Drawing.Size(98, 37);
			this.ZoneToolboxResetZoneButton.Text = "Reset Zone";
			this.ZoneToolboxResetZoneButton.Click += new System.EventHandler(this.ZoneToolboxResetZoneButton_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(6, 40);
			// 
			// toolStripLabel1
			// 
			this.toolStripLabel1.Name = "toolStripLabel1";
			this.toolStripLabel1.Size = new System.Drawing.Size(79, 37);
			this.toolStripLabel1.Text = "Selected Zone:";
			// 
			// ZoneToolboxSelectedZoneTextBox
			// 
			this.ZoneToolboxSelectedZoneTextBox.BackColor = System.Drawing.SystemColors.Window;
			this.ZoneToolboxSelectedZoneTextBox.Name = "ZoneToolboxSelectedZoneTextBox";
			this.ZoneToolboxSelectedZoneTextBox.Size = new System.Drawing.Size(30, 40);
			this.ZoneToolboxSelectedZoneTextBox.Text = "None";
			this.ZoneToolboxSelectedZoneTextBox.TextBoxTextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			// 
			// ZoneToolbox
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(604, 40);
			this.Controls.Add(this.toolStrip1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "ZoneToolbox";
			this.Text = "ZoneToolbox";
			this.Load += new System.EventHandler(this.ZoneToolbox_Load);
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ToolStrip toolStrip1;
		private System.Windows.Forms.ToolStripButton ZoneToolboxStayOutPolygonButton;
		private System.Windows.Forms.ToolStripButton ZoneToolboxSelectZoneButton;
		private System.Windows.Forms.ToolStripButton ZoneToolboxCreateNodesButton;
		private System.Windows.Forms.ToolStripButton ZoneToolboxResetZoneButton;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripLabel toolStripLabel1;
		private System.Windows.Forms.ToolStripTextBox ZoneToolboxSelectedZoneTextBox;
	}
}