namespace RemoraAdvanced.Forms
{
	partial class LaneAgentBrowser
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LaneAgentBrowser));
			this.laneAgentData = new System.Windows.Forms.ListView();
			this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
			this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
			this.columnHeader3 = new System.Windows.Forms.ColumnHeader();
			this.SuspendLayout();
			// 
			// laneAgentData
			// 
			this.laneAgentData.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3});
			this.laneAgentData.FullRowSelect = true;
			this.laneAgentData.GridLines = true;
			this.laneAgentData.Location = new System.Drawing.Point(12, 12);
			this.laneAgentData.Name = "laneAgentData";
			this.laneAgentData.Size = new System.Drawing.Size(313, 123);
			this.laneAgentData.TabIndex = 1;
			this.laneAgentData.UseCompatibleStateImageBehavior = false;
			this.laneAgentData.View = System.Windows.Forms.View.Details;
			// 
			// columnHeader1
			// 
			this.columnHeader1.Text = "Lane";
			this.columnHeader1.Width = 88;
			// 
			// columnHeader2
			// 
			this.columnHeader2.Text = "Description";
			this.columnHeader2.Width = 133;
			// 
			// columnHeader3
			// 
			this.columnHeader3.Text = "Confidence";
			this.columnHeader3.Width = 87;
			// 
			// LaneAgentBrowser
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(337, 147);
			this.Controls.Add(this.laneAgentData);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "LaneAgentBrowser";
			this.ShowInTaskbar = false;
			this.Text = "Lane Agent Browser";
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ListView laneAgentData;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.ColumnHeader columnHeader3;


	}
}