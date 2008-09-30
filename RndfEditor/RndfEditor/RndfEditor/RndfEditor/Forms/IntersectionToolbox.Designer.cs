namespace RndfEditor
{
	partial class IntersectionToolbox
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(IntersectionToolbox));
			this.intersectionToolboxToolStrip = new System.Windows.Forms.ToolStrip();
			this.setSafetyZonesIntersectionToolkitButton = new System.Windows.Forms.ToolStripButton();
			this.boxIntersectionToolkitButton = new System.Windows.Forms.ToolStripButton();
			this.AddIntersectionWrapHelperPoint = new System.Windows.Forms.ToolStripButton();
			this.IntersectionReParseAllButton = new System.Windows.Forms.ToolStripButton();
			this.intersectionToolboxToolStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// intersectionToolboxToolStrip
			// 
			this.intersectionToolboxToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.intersectionToolboxToolStrip.ImageScalingSize = new System.Drawing.Size(32, 32);
			this.intersectionToolboxToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.setSafetyZonesIntersectionToolkitButton,
            this.boxIntersectionToolkitButton,
            this.AddIntersectionWrapHelperPoint,
            this.IntersectionReParseAllButton});
			this.intersectionToolboxToolStrip.Location = new System.Drawing.Point(0, 0);
			this.intersectionToolboxToolStrip.Name = "intersectionToolboxToolStrip";
			this.intersectionToolboxToolStrip.Size = new System.Drawing.Size(534, 39);
			this.intersectionToolboxToolStrip.TabIndex = 0;
			this.intersectionToolboxToolStrip.Text = "toolStrip1";
			// 
			// setSafetyZonesIntersectionToolkitButton
			// 
			this.setSafetyZonesIntersectionToolkitButton.CheckOnClick = true;
			this.setSafetyZonesIntersectionToolkitButton.Image = global::RndfEditor.Properties.Resources.ColorPicker1;
			this.setSafetyZonesIntersectionToolkitButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.setSafetyZonesIntersectionToolkitButton.Name = "setSafetyZonesIntersectionToolkitButton";
			this.setSafetyZonesIntersectionToolkitButton.Size = new System.Drawing.Size(128, 36);
			this.setSafetyZonesIntersectionToolkitButton.Text = "Pick Safety Zones";
			this.setSafetyZonesIntersectionToolkitButton.Click += new System.EventHandler(this.setSafetyZonesIntersectionToolkitButton_Click);
			// 
			// boxIntersectionToolkitButton
			// 
			this.boxIntersectionToolkitButton.CheckOnClick = true;
			this.boxIntersectionToolkitButton.Image = global::RndfEditor.Properties.Resources.Box;
			this.boxIntersectionToolkitButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.boxIntersectionToolkitButton.Name = "boxIntersectionToolkitButton";
			this.boxIntersectionToolkitButton.Size = new System.Drawing.Size(130, 36);
			this.boxIntersectionToolkitButton.Text = "Wrap Intersection";
			this.boxIntersectionToolkitButton.Click += new System.EventHandler(this.boxIntersectionToolkitButton_Click);
			// 
			// AddIntersectionWrapHelperPoint
			// 
			this.AddIntersectionWrapHelperPoint.CheckOnClick = true;
			this.AddIntersectionWrapHelperPoint.Image = global::RndfEditor.Properties.Resources.Add;
			this.AddIntersectionWrapHelperPoint.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.AddIntersectionWrapHelperPoint.Name = "AddIntersectionWrapHelperPoint";
			this.AddIntersectionWrapHelperPoint.Size = new System.Drawing.Size(150, 36);
			this.AddIntersectionWrapHelperPoint.Text = "Add Wrapping Helpers";
			this.AddIntersectionWrapHelperPoint.Click += new System.EventHandler(this.AddIntersectionWrapHelperPoint_Click);
			// 
			// IntersectionReParseAllButton
			// 
			this.IntersectionReParseAllButton.Image = global::RndfEditor.Properties.Resources.Refresh1;
			this.IntersectionReParseAllButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.IntersectionReParseAllButton.Name = "IntersectionReParseAllButton";
			this.IntersectionReParseAllButton.Size = new System.Drawing.Size(101, 36);
			this.IntersectionReParseAllButton.Text = "Re-Parse All";
			this.IntersectionReParseAllButton.Click += new System.EventHandler(this.IntersectionReParseAllButton_Click);
			// 
			// IntersectionToolbox
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(534, 40);
			this.Controls.Add(this.intersectionToolboxToolStrip);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "IntersectionToolbox";
			this.Text = "Intersection Toolbox";
			this.intersectionToolboxToolStrip.ResumeLayout(false);
			this.intersectionToolboxToolStrip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ToolStrip intersectionToolboxToolStrip;
		public System.Windows.Forms.ToolStripButton boxIntersectionToolkitButton;
		public System.Windows.Forms.ToolStripButton setSafetyZonesIntersectionToolkitButton;
		private System.Windows.Forms.ToolStripButton AddIntersectionWrapHelperPoint;
		private System.Windows.Forms.ToolStripButton IntersectionReParseAllButton;
	}
}