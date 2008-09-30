namespace RndfEditor.Forms
{
	partial class SparsePartitionToolbox
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
			this.toolStrip1 = new System.Windows.Forms.ToolStrip();
			this.sparseToolboxSelectPartition = new System.Windows.Forms.ToolStripButton();
			this.sparsetoolboxWrapPolygonButton = new System.Windows.Forms.ToolStripButton();
			this.SparseToolboxResetSparsePolygon = new System.Windows.Forms.ToolStripButton();
			this.toolStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// toolStrip1
			// 
			this.toolStrip1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.sparseToolboxSelectPartition,
            this.sparsetoolboxWrapPolygonButton,
            this.SparseToolboxResetSparsePolygon});
			this.toolStrip1.Location = new System.Drawing.Point(0, 0);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.Size = new System.Drawing.Size(404, 50);
			this.toolStrip1.TabIndex = 0;
			this.toolStrip1.Text = "toolStrip1";
			// 
			// sparseToolboxSelectPartition
			// 
			this.sparseToolboxSelectPartition.CheckOnClick = true;
			this.sparseToolboxSelectPartition.Image = global::RndfEditor.Properties.Resources.ColorPicker1;
			this.sparseToolboxSelectPartition.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.sparseToolboxSelectPartition.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.sparseToolboxSelectPartition.Name = "sparseToolboxSelectPartition";
			this.sparseToolboxSelectPartition.Size = new System.Drawing.Size(122, 47);
			this.sparseToolboxSelectPartition.Text = "Select Partition";
			this.sparseToolboxSelectPartition.Click += new System.EventHandler(this.sparseToolboxSelectPartition_Click);
			// 
			// sparsetoolboxWrapPolygonButton
			// 
			this.sparsetoolboxWrapPolygonButton.CheckOnClick = true;
			this.sparsetoolboxWrapPolygonButton.Image = global::RndfEditor.Properties.Resources.Box;
			this.sparsetoolboxWrapPolygonButton.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.sparsetoolboxWrapPolygonButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.sparsetoolboxWrapPolygonButton.Name = "sparsetoolboxWrapPolygonButton";
			this.sparsetoolboxWrapPolygonButton.Size = new System.Drawing.Size(118, 47);
			this.sparsetoolboxWrapPolygonButton.Text = "Wrap Polygon";
			this.sparsetoolboxWrapPolygonButton.Click += new System.EventHandler(this.sparsetoolboxWrapPolygonButton_Click);
			// 
			// SparseToolboxResetSparsePolygon
			// 
			this.SparseToolboxResetSparsePolygon.Image = global::RndfEditor.Properties.Resources.Refresh1;
			this.SparseToolboxResetSparsePolygon.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.SparseToolboxResetSparsePolygon.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.SparseToolboxResetSparsePolygon.Name = "SparseToolboxResetSparsePolygon";
			this.SparseToolboxResetSparsePolygon.Size = new System.Drawing.Size(118, 47);
			this.SparseToolboxResetSparsePolygon.Text = "Reset Polygon";
			this.SparseToolboxResetSparsePolygon.Click += new System.EventHandler(this.SparseToolboxResetSparsePolygon_Click);
			// 
			// SparsePartitionToolbox
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(404, 50);
			this.Controls.Add(this.toolStrip1);
			this.Name = "SparsePartitionToolbox";
			this.Text = "Sparse Toolbox";
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ToolStrip toolStrip1;
		private System.Windows.Forms.ToolStripButton sparseToolboxSelectPartition;
		private System.Windows.Forms.ToolStripButton sparsetoolboxWrapPolygonButton;
		private System.Windows.Forms.ToolStripButton SparseToolboxResetSparsePolygon;
	}
}